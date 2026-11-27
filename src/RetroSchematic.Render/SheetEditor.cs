//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// The one way a sheet is changed, used by the window and by the MCP tool alike: place, move
    /// and delete chips, connect and disconnect pins, undo and redo, save. Every change is
    /// checked first and either happens whole or not at all (a refused change throws
    /// <see cref="SheetEditException"/> and leaves the sheet as it was).
    /// <para>
    /// A sheet opens READ-ONLY: every change is refused until <see cref="ReadOnly"/> is set to
    /// false. Undo keeps a copy of the whole sheet before each change.
    /// </para>
    /// </summary>
    public sealed partial class SheetEditor
    {
        private readonly List<string> _undo = new();
        private readonly List<string> _redo = new();

        /// <summary>
        /// Called after the sheet changed (an edit, undo or redo).
        /// </summary>
        /// <param name="editor">
        /// The editor whose sheet changed.
        /// </param>
        public delegate void ChangedHandler(SheetEditor editor);

        /// <summary>
        /// The sheet changed.
        /// </summary>
        public event ChangedHandler? Changed;

        /// <summary>
        /// Edits a sheet already read.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue, needed to place chips and to check pin numbers; null refuses placing.
        /// </param>
        public SheetEditor(Netlist netlist, ChipCatalog? catalog)
        {
            Root = netlist;
            Netlist = netlist;
            Catalog = catalog;
        }

        /// <summary>
        /// The level being looked at and edited: the whole sheet, or, after
        /// <see cref="EnterInstance"/>, the inside of a block seen as a sheet of its own (see
        /// <see cref="Hierarchy.ViewOf"/>). Undo and redo replace this object.
        /// </summary>
        public Netlist Netlist { get; private set; }

        /// <summary>
        /// The whole design: the top level with every block. Saving writes this.
        /// </summary>
        public Netlist Root { get; private set; }

        /// <summary>
        /// The chip catalogue, or null.
        /// </summary>
        public ChipCatalog? Catalog { get; }

        /// <summary>
        /// The card page being shown and edited (format version 3): a chip or passive placed now
        /// goes on it. Null for the first page, and for a card without pages. Not a change to
        /// the card.
        /// </summary>
        public string? Page { get; set; }

        /// <summary>
        /// True (the default) refuses every change.
        /// </summary>
        public bool ReadOnly { get; set; } = true;

        /// <summary>
        /// True when there is a change to undo.
        /// </summary>
        public bool CanUndo => _undo.Count > 0;

        /// <summary>
        /// True when there is an undone change to do again.
        /// </summary>
        public bool CanRedo => _redo.Count > 0;

        /// <summary>
        /// True when the sheet changed since it was opened or last saved.
        /// </summary>
        public bool Modified { get; private set; }

        /// <summary>
        /// An empty sheet in format version 1, for a new design.
        /// </summary>
        /// <param name="id">
        /// The sheet number, such as DEMO.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="bus">
        /// Which machine bus the card is for; it picks the bus catalogues.
        /// </param>
        /// <returns>
        /// A sheet with no parts and no signals.
        /// </returns>
        public static Netlist NewSheet(string id, string title, MachineBus bus = MachineBus.Unknown)
        {
            return new Netlist
            {
                Format = "nd-netlist",
                FormatVersion = NetlistReader.SupportedFormatVersion,
                Sheet = new Sheet
                {
                    Id = id,
                    Title = title,
                    Bus = bus,
                    Source = new SourceReference { Document = "none - drawn in the editor" },
                    Note = "Drawn in the RetroSchematic editor, not read off a scan.",
                },
            };
        }

        /// <summary>
        /// Places a chip from the catalogue. Its gates come from the chip file's formulas (see
        /// <see cref="ChipCatalog.GateFromFormula"/>); a chip whose formulas are not plain gates
        /// (a flip-flop, a counter) is placed with no gates and drawn with its pins listed.
        /// </summary>
        /// <param name="type">
        /// The chip type, such as 7400.
        /// </param>
        /// <param name="xMm">
        /// Where the frame's left edge goes, in millimetres.
        /// </param>
        /// <param name="yMm">
        /// Where the frame's top edge goes, in millimetres.
        /// </param>
        /// <param name="reference">
        /// The board position to give it (13D), or null for the first free one in row A, B, ...
        /// </param>
        /// <returns>
        /// The new part's reference.
        /// </returns>
        public string PlacePart(string type, double xMm, double yMm, string? reference = null)
        {
            CheckWritable();
            if (Catalog == null)
                throw new SheetEditException("No chip catalogue is open, so no chip can be placed.");
            ChipInfo? chip = Catalog.Get(type);
            if (chip == null)
                throw new SheetEditException($"The catalogue has no chip file with pins for {type}.");
            if (reference != null)
            {
                CheckBoardPosition(reference);
                if (FindPart(reference) != null)
                    throw new SheetEditException($"There is already a part {reference} on the sheet.");
            }
            else
                reference = FreeReference();

            Snapshot();
            Part part = new()
            {
                Ref = reference,
                Type = type,
                Confidence = Confidence.Read,
                Note = "Placed in the editor.",
                Position = new Point { X = xMm, Y = yMm },
            };
            Netlist.Parts.Add(part);
            for (int i = 0; i < chip.Gates.Count; i++)
            {
                Gate from = chip.Gates[i];
                Netlist.Gates.Add(new Gate
                {
                    Id = reference + "/" + (i + 1).ToString(CultureInfo.InvariantCulture),
                    Part = reference,
                    Kind = from.Kind,
                    Inputs = new List<int>(from.Inputs),
                    Output = from.Output,
                    Confidence = Confidence.Read,
                });
            }
            Done();
            return reference;
        }

        /// <summary>
        /// Adds a passive part (resistor, capacitor, switch block ..., see <see cref="PassiveParts"/>)
        /// with no pins wired yet. A part the drawing names keeps that name (R1, C3) and is
        /// <c>read</c>; a part the drawing does not name gets an automatic name made from the chip
        /// it serves (<see cref="PassiveParts.AutoName"/>, such as R-13D-1) and is <c>inferred</c>.
        /// It has no position: it is drawn beside the chip it serves.
        /// </summary>
        /// <param name="type">
        /// The passive type: R, RN-SIP-n, C, C-POL, D, STRAP or SW-DIP-n.
        /// </param>
        /// <param name="reference">
        /// The name on the drawing, or null when the drawing gives none.
        /// </param>
        /// <param name="serves">
        /// The board position of the chip it serves; needed when <paramref name="reference"/> is null.
        /// </param>
        /// <param name="value">
        /// The value as written on the drawing, or null.
        /// </param>
        /// <param name="tolerance">
        /// The tolerance as written on the drawing, or null.
        /// </param>
        /// <returns>
        /// The new part's reference.
        /// </returns>
        public string AddPassive(string type, string? reference, string? serves, string? value, string? tolerance)
        {
            CheckWritable();
            PassiveKind kind = PassiveParts.KindOf(type);
            if (kind == PassiveKind.None)
                throw new SheetEditException($"{type} is not a passive part type (R, RN-SIP-n, C, C-POL, D, STRAP, SW-DIP-n).");
            if (serves != null && FindPart(serves) == null)
                throw new SheetEditException($"There is no part {serves} on the sheet to serve.");
            Confidence confidence = Confidence.Read;
            if (reference == null)
            {
                if (serves == null)
                    throw new SheetEditException("A part with no name on the drawing needs the chip it serves, to make its name from.");
                HashSet<string> taken = new(StringComparer.Ordinal);
                for (int i = 0; i < Netlist.Parts.Count; i++)
                    taken.Add(Netlist.Parts[i].Ref);
                reference = PassiveParts.AutoName(kind, serves, taken);
                confidence = Confidence.Inferred;
            }
            else if (FindPart(reference) != null)
                throw new SheetEditException($"There is already a part {reference} on the sheet.");
            Snapshot();
            Netlist.Parts.Add(new Part
            {
                Ref = reference,
                Type = type,
                Confidence = confidence,
                Note = confidence == Confidence.Inferred ? "No name on the drawing; the name is made from the chip it serves." : null,
                Value = value,
                Tolerance = tolerance,
                Serves = confidence == Confidence.Inferred ? serves : null,
            });
            Done();
            return reference;
        }

        /// <summary>
        /// Moves a chip's frame so its top left corner is at a new place. Only the drawing changes;
        /// the signals stay as they are. Gates with a measured position move with the frame. A
        /// part whose gates all have a position (and no position of its own) keeps it that way:
        /// only its gates move.
        /// </summary>
        /// <param name="reference">
        /// The part, such as 13D.
        /// </param>
        /// <param name="xMm">
        /// New left edge in millimetres.
        /// </param>
        /// <param name="yMm">
        /// New top edge in millimetres.
        /// </param>
        public void MovePart(string reference, double xMm, double yMm)
        {
            CheckWritable();
            Part part = FindPart(reference) ?? throw new SheetEditException($"There is no part {reference} on the sheet.");
            // drawn on its own page (a card with pages), where the frame is measured
            SchematicDrawing now = SheetLayout.Build(Netlist, SymbolStyle.NorskData, LayoutFor(reference));
            PartFrame f = now.Frames[reference];
            double dx = xMm - SheetLayout.XToMm(now, f.X), dy = yMm - SheetLayout.ToMm(f.Y);
            if (dx == 0 && dy == 0)
                return;

            Snapshot();
            bool allGatesMeasured = part.Position == null;
            for (int i = 0; i < Netlist.Gates.Count && allGatesMeasured; i++)
            {
                if (Netlist.Gates[i].Part == reference && Netlist.Gates[i].Position == null)
                    allGatesMeasured = false;
            }
            bool hasGates = false;
            for (int i = 0; i < Netlist.Gates.Count; i++)
            {
                Gate g = Netlist.Gates[i];
                if (g.Part != reference)
                    continue;
                hasGates = true;
                if (g.Position != null)
                    g.Position = new Point { X = g.Position.X + dx, Y = g.Position.Y + dy };
            }
            if (!(allGatesMeasured && hasGates))
                part.Position = new Point { X = xMm, Y = yMm };
            Done();
        }

        /// <summary>
        /// Moves a connector box (P, F, P.A ...) to a place of its own, kept in the sheet's
        /// <c>connectors</c> list like a chip's position. The connector gets an entry there if it
        /// has none. Optionally sets which side of the box its pins are on.
        /// </summary>
        /// <param name="reference">
        /// The connector as pin references name it, such as P.
        /// </param>
        /// <param name="xMm">
        /// New left edge in millimetres.
        /// </param>
        /// <param name="yMm">
        /// New top edge in millimetres.
        /// </param>
        /// <param name="pinSide">
        /// "left" or "right" to set the pin side, null to keep it.
        /// </param>
        public void MoveConnector(string reference, double xMm, double yMm, string? pinSide = null)
        {
            CheckWritable();
            if (!ConnectorPins.IsConnector(reference))
                throw new SheetEditException($"{reference} is not a connector (P, F, P.A ...).");
            if (pinSide != null && pinSide != "left" && pinSide != "right")
                throw new SheetEditException("The pin side is left or right.");
            Snapshot();
            Netlist.Connectors ??= new List<ConnectorUse>();
            ConnectorUse? use = null;
            for (int i = 0; i < Netlist.Connectors.Count && use == null; i++)
            {
                if (Netlist.Connectors[i].Ref == reference)
                    use = Netlist.Connectors[i];
            }
            if (use == null)
            {
                use = new ConnectorUse { Ref = reference };
                Netlist.Connectors.Add(use);
            }
            use.Position = new Point { X = xMm, Y = yMm };
            if (pinSide != null)
                use.PinSide = pinSide;
            Done();
        }

        /// <summary>
        /// Joins two pins. Two free pins make a new signal; a free pin joins the other pin's
        /// signal. A pin can be on one signal only: two pins already on different signals are
        /// refused, never merged.
        /// </summary>
        /// <param name="pinA">
        /// One pin, such as 11D.3 (P.12 and F.3 for backplane and front pins).
        /// </param>
        /// <param name="pinB">
        /// The other pin.
        /// </param>
        /// <param name="newNetName">
        /// The name for a new signal; null names it N1, N2, ... Must be null or the existing
        /// signal's name when a pin is already on one.
        /// </param>
        /// <returns>
        /// The signal the two pins are now on.
        /// </returns>
        public string Connect(string pinA, string pinB, string? newNetName = null)
        {
            CheckWritable();
            CheckPin(pinA);
            CheckPin(pinB);
            if (pinA == pinB)
                throw new SheetEditException($"Pin {pinA} cannot be connected to itself.");
            Net? netA = NetOf(pinA), netB = NetOf(pinB);
            if (netA != null && netB != null)
            {
                if (netA == netB)
                    throw new SheetEditException($"Pins {pinA} and {pinB} are already both on signal {netA.Name}.");
                throw new SheetEditException($"Pin {pinA} is on signal {netA.Name} and pin {pinB} is on signal {netB.Name}. "
                    + "A pin can be on one signal only; disconnect one of them first.");
            }
            Net? existing = netA ?? netB;
            if (existing != null && newNetName != null && newNetName != existing.Name)
                throw new SheetEditException($"Pin {(netA != null ? pinA : pinB)} is already on signal {existing.Name}, not {newNetName}.");
            if (existing == null && newNetName != null && FindNet(newNetName) != null)
                throw new SheetEditException($"There is already a signal {newNetName}.");
            if (newNetName != null && newNetName.Trim().Length == 0)
                throw new SheetEditException("A signal needs a name.");

            Snapshot();
            if (existing == null)
            {
                existing = new Net { Name = newNetName ?? FreeNetName(), Active = ActiveLevel.None };
                existing.Pins.Add(new PinReference { At = pinA, Confidence = Confidence.Read });
                Netlist.Nets.Add(existing);
            }
            // a new signal already holds pin A, so B is the one to add; else the free one of the two
            bool fresh = netA == null && netB == null;
            existing.Pins.Add(new PinReference { At = fresh || netA != null ? pinB : pinA, Confidence = Confidence.Read });
            Done();
            return existing.Name;
        }

        /// <summary>
        /// Takes a pin off its signal. A signal left with no pins is removed.
        /// </summary>
        /// <param name="pin">
        /// The pin, such as 11D.3.
        /// </param>
        public void Disconnect(string pin)
        {
            CheckWritable();
            Net net = NetOf(pin) ?? throw new SheetEditException($"Pin {pin} is not on any signal.");
            Snapshot();
            for (int i = 0; i < net.Pins.Count; i++)
            {
                if (net.Pins[i].At == pin)
                {
                    net.Pins.RemoveAt(i);
                    break;
                }
            }
            if (net.Pins.Count == 0)
                Netlist.Nets.Remove(net);
            Done();
        }

        /// <summary>
        /// Removes a signal and every connection on it.
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        public void DeleteNet(string name)
        {
            CheckWritable();
            Net net = FindNetByKey(name) ?? throw new SheetEditException($"There is no signal {name}.");
            Snapshot();
            // a deleted signal also leaves its bus (a bus left empty goes too)
            BusLine? bus = BusOf(net);
            if (bus != null)
            {
                bus.Members.RemoveAt(MemberIndex(bus, net));
                if (bus.Members.Count == 0)
                    Netlist.Layout!.Buses!.Remove(bus);
                if (Netlist.Layout!.Buses!.Count == 0)
                    Netlist.Layout.Buses = null;
            }
            Netlist.Nets.Remove(net);
            RemoveWirePath(net.Name, net.Active);
            Done();
        }

        /// <summary>
        /// Fixes a signal's wire path in the sheet's layout section, so it is saved and drawn the
        /// same way next time (as long as it still crosses nothing). Drawing only: what is
        /// connected does not change.
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        /// <param name="branches">
        /// The branches, each a list of bend points in whole mm: the first joins two pins, each
        /// later one runs from a pin to the wire before it.
        /// </param>
        /// <param name="active">
        /// The signal's active level, when two signals share the name; null for the first of
        /// that name.
        /// </param>
        public void SetWirePath(string name, List<List<Point>> branches, ActiveLevel? active = null)
        {
            CheckWritable();
            if (FindNet(name, active) == null)
                throw new SheetEditException($"There is no signal {name}" + (active == null ? "." : $" active {active}."));
            if (branches.Count == 0)
                throw new SheetEditException("A wire path needs at least one branch.");
            for (int b = 0; b < branches.Count; b++)
            {
                List<Point> pts = branches[b];
                if (pts.Count < 2)
                    throw new SheetEditException($"Branch {b + 1} needs at least two points.");
                for (int i = 1; i < pts.Count; i++)
                {
                    if (pts[i].X != pts[i - 1].X && pts[i].Y != pts[i - 1].Y)
                        throw new SheetEditException($"Branch {b + 1} has a slanted piece; every piece must be level or upright.");
                }
            }
            Snapshot();
            // a card with pages keeps one path per page: the path is drawn on the page shown
            string? page = WirePage();
            RemoveWirePath(name, active);
            Netlist.Layout ??= new SheetLayoutSection();
            Netlist.Layout.Wires.Add(new WirePath { Net = name, Active = active, Branches = branches, Page = page });
            Done();
        }

        /// <summary>
        /// Lets a signal's wire be routed automatically again (drops its fixed path).
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        /// <param name="active">
        /// The signal's active level, or null.
        /// </param>
        public void ClearWirePath(string name, ActiveLevel? active = null)
        {
            CheckWritable();
            if (WirePathOf(name, active) == null)
                throw new SheetEditException($"Signal {name} has no fixed wire path.");
            Snapshot();
            RemoveWirePath(name, active);
            Done();
        }

        /// <summary>
        /// A signal's fixed wire path, or null.
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        /// <param name="active">
        /// The signal's active level, or null for any.
        /// </param>
        /// <returns>
        /// The stored path, or null when the signal is routed automatically.
        /// </returns>
        public WirePath? WirePathOf(string name, ActiveLevel? active = null)
        {
            List<WirePath>? wires = Netlist.Layout?.Wires;
            for (int i = 0; wires != null && i < wires.Count; i++)
            {
                if (wires[i].Net == name && (active == null || wires[i].Active == null || wires[i].Active == active) && OnWirePage(wires[i].Page))
                    return wires[i];
            }
            return null;
        }

        // the card page a wire path set now is drawn on: the page shown, at the top level of a
        // card with pages; null otherwise
        private string? WirePage()
            => LevelPath.Count == 0 && CardPages.HasPages(Netlist) ? CardPages.PageOrFirst(Netlist, Page != null && CardPages.Find(Netlist, Page) != null ? Page : null) : null;

        // true when a stored path (with its page, or null for the first page) belongs to the page shown
        private bool OnWirePage(string? page)
        {
            string? shown = WirePage();
            return shown == null || (page ?? CardPages.FirstPage(Netlist)) == shown;
        }

        private void RemoveWirePath(string name, ActiveLevel? active = null)
        {
            List<WirePath>? wires = Netlist.Layout?.Wires;
            for (int i = wires == null ? -1 : wires.Count - 1; i >= 0; i--)
            {
                if (wires![i].Net == name && (active == null || wires[i].Active == null || wires[i].Active == active) && OnWirePage(wires[i].Page))
                    wires.RemoveAt(i);
            }
            if (Netlist.Layout != null && Netlist.Layout.Wires.Count == 0 && Netlist.Layout.Buses == null)
                Netlist.Layout = null;
        }

        /// <summary>
        /// Removes a chip, its gates, and its pins from every signal. A signal left with no pins
        /// is removed.
        /// </summary>
        /// <param name="reference">
        /// The part, such as 13D.
        /// </param>
        public void DeletePart(string reference)
        {
            CheckWritable();
            Part part = FindPart(reference) ?? throw new SheetEditException($"There is no part {reference} on the sheet.");
            Snapshot();
            Netlist.Parts.Remove(part);
            for (int i = Netlist.Gates.Count - 1; i >= 0; i--)
            {
                if (Netlist.Gates[i].Part == reference)
                    Netlist.Gates.RemoveAt(i);
            }
            for (int i = Netlist.Nets.Count - 1; i >= 0; i--)
            {
                List<PinReference> pins = Netlist.Nets[i].Pins;
                for (int j = pins.Count - 1; j >= 0; j--)
                {
                    if (pins[j].PartRef == reference)
                        pins.RemoveAt(j);
                }
                if (pins.Count == 0)
                    Netlist.Nets.RemoveAt(i);
            }
            Done();
        }

        /// <summary>
        /// Takes back the last change. Refused in read-only mode, like every change.
        /// </summary>
        public void Undo()
        {
            CheckWritable();
            if (_undo.Count == 0)
                throw new SheetEditException("There is nothing to undo.");
            SyncBack();
            _redo.Add(NetlistReader.Write(Root));
            Root = NetlistReader.Parse(_undo[^1]);
            _undo.RemoveAt(_undo.Count - 1);
            ShowLevel();
            Modified = true;
            Changed?.Invoke(this);
        }

        /// <summary>
        /// Does the last undone change again.
        /// </summary>
        public void Redo()
        {
            CheckWritable();
            if (_redo.Count == 0)
                throw new SheetEditException("There is nothing to redo.");
            SyncBack();
            _undo.Add(NetlistReader.Write(Root));
            Root = NetlistReader.Parse(_redo[^1]);
            _redo.RemoveAt(_redo.Count - 1);
            ShowLevel();
            Modified = true;
            Changed?.Invoke(this);
        }

        /// <summary>
        /// Writes the sheet as nd-netlist JSON, with every position. The text is read back and
        /// checked before the file is written, so a sheet the reader would refuse is never saved.
        /// Saving is allowed in read-only mode (it changes nothing in the sheet).
        /// </summary>
        /// <param name="path">
        /// The file to write.
        /// </param>
        public void Save(string path)
        {
            SyncBack();
            // zipped with every source page and photo inside when the path ends in .rsx, else
            // plain text with the files linked (.rsc, or an old .json kept as it is)
            Netlist written = Files.Save(path, Root, Checked, Pdf);
            // the links as written (entries of the zip, or paths relative to the new folder)
            Root.Sources = written.Sources;
            Root.Photos = written.Photos;
            Modified = false;
        }

        /// <summary>
        /// The sheet's source pages and photos: held in memory or linked on disk.
        /// </summary>
        public SheetAttachments Files { get; set; } = new();

        /// <summary>
        /// Renders a PDF page as a picture: used to show a PDF source page and to store it in a
        /// .rsx. Null when no renderer is set; a PDF source then cannot be shown or packed.
        /// </summary>
        public static PdfPageRenderer? Pdf { get; set; } = PdfPages.Render;

        // the JSON of a sheet, read back and checked first, so a sheet the reader would refuse
        // is never saved
        private static string Checked(Netlist sheet)
        {
            string json = NetlistReader.Write(sheet);
            try
            {
                NetlistReader.Parse(json);
            }
            catch (NetlistException e)
            {
                throw new SheetEditException("The sheet is not valid and was not saved: " + e.Message, e);
            }
            return json + "\n";
        }

        /// <summary>
        /// The signal a pin is on, or null.
        /// </summary>
        /// <param name="pin">
        /// The pin, such as 11D.3.
        /// </param>
        /// <returns>
        /// The signal, or null.
        /// </returns>
        public Net? NetOf(string pin)
        {
            for (int i = 0; i < Netlist.Nets.Count; i++)
            {
                List<PinReference> pins = Netlist.Nets[i].Pins;
                for (int j = 0; j < pins.Count; j++)
                {
                    if (pins[j].At == pin)
                        return Netlist.Nets[i];
                }
            }
            return null;
        }

        /// <summary>
        /// A part by reference, or null.
        /// </summary>
        /// <param name="reference">
        /// The part, such as 13D.
        /// </param>
        /// <returns>
        /// The part, or null.
        /// </returns>
        public Part? FindPart(string reference)
        {
            for (int i = 0; i < Netlist.Parts.Count; i++)
            {
                if (Netlist.Parts[i].Ref == reference)
                    return Netlist.Parts[i];
            }
            return null;
        }

        /// <summary>
        /// A signal by its key on the drawing (<see cref="SheetLayout.NetKey"/>: BD15_0), or, when
        /// no signal has that key, the first signal with that name.
        /// </summary>
        /// <param name="key">
        /// The key or the name.
        /// </param>
        /// <returns>
        /// The signal, or null.
        /// </returns>
        public Net? FindNetByKey(string key)
        {
            for (int i = 0; i < Netlist.Nets.Count; i++)
            {
                if (SheetLayout.NetKey(Netlist.Nets[i]) == key)
                    return Netlist.Nets[i];
            }
            return FindNet(key);
        }

        /// <summary>
        /// A signal by name and, when given, active level (two signals may share a name).
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        /// <param name="active">
        /// Its active level, or null for the first of that name.
        /// </param>
        /// <returns>
        /// The signal, or null.
        /// </returns>
        public Net? FindNet(string name, ActiveLevel? active)
        {
            for (int i = 0; i < Netlist.Nets.Count; i++)
            {
                if (Netlist.Nets[i].Name == name && (active == null || Netlist.Nets[i].Active == active))
                    return Netlist.Nets[i];
            }
            return null;
        }

        /// <summary>
        /// A signal by name, or null.
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        /// <returns>
        /// The signal, or null.
        /// </returns>
        public Net? FindNet(string name)
        {
            for (int i = 0; i < Netlist.Nets.Count; i++)
            {
                if (Netlist.Nets[i].Name == name)
                    return Netlist.Nets[i];
            }
            return null;
        }

        /// <summary>
        /// Sets what is written about a signal: its description and where that came from, its
        /// function tags and its notes. Edit mode only; it can be undone and marks the sheet as
        /// changed. Nothing derived (drive, drivers, loads) is ever stored here.
        /// </summary>
        /// <param name="key">
        /// The signal's key, such as IOXE_0.
        /// </param>
        /// <param name="description">
        /// What it does, or null/empty to remove it.
        /// </param>
        /// <param name="source">
        /// Where the description comes from, or null.
        /// </param>
        /// <param name="note">
        /// Free notes, or null/empty to remove them.
        /// </param>
        /// <param name="function">
        /// Function tags, or null to leave them as they are.
        /// </param>
        public void SetNetInfo(string key, string? description, DescriptionSource? source, string? note, List<string>? function = null)
        {
            CheckWritable();
            Net n = FindNetByKey(key) ?? throw new SheetEditException($"There is no signal {key} on this level.");
            Snapshot();
            n = FindNetByKey(key)!;
            n.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            n.DescriptionSource = n.Description == null ? null : source;
            n.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            if (function != null)
                n.Function = function.Count == 0 ? null : function;
            Done();
        }

        private void CheckWritable()
        {
            if (ReadOnly)
                throw new SheetEditException("The sheet is open read-only; switch to edit mode first.");
        }

        private static void CheckBoardPosition(string reference)
        {
            try
            {
                BoardPosition.Parse(reference);
            }
            catch (FormatException e)
            {
                throw new SheetEditException(e.Message, e);
            }
        }

        /// <summary>
        /// A pin must name a part on the sheet (or a connector: P.32, P.A.32, F.81 - see ConnectorPins) and a
        /// pin number the chip has: from the chip file when the catalogue has it, else one of the
        /// part's gate pins.
        /// </summary>
        /// <param name="pin">
        /// The pin, such as 11D.3.
        /// </param>
        private void CheckPin(string pin)
        {
            int dot = pin.LastIndexOf('.');
            if (dot <= 0 || !ConnectorPins.TryParsePin(pin.AsSpan(dot + 1), out int number))
                throw new SheetEditException($"\"{pin}\" is not a pin: write the part, a dot and the pin number (11D.3).");
            string reference = pin.Substring(0, dot);
            // a connector pin may be 0 (NORD-10 backplane pins run 0-99) or have a row (P.C.a17);
            // a chip pin starts at 1 and has no row letter
            if (ConnectorPins.IsConnector(reference))
                return;
            if (number >= ConnectorPins.RowBase)
                throw new SheetEditException($"\"{pin}\": only a connector pin has a row letter; a chip pin is a plain number (11D.3).");
            if (number < 1)
                throw new SheetEditException($"\"{pin}\" is not a pin: write the part, a dot and the pin number (11D.3).");
            Part part = FindPart(reference) ?? throw new SheetEditException($"Pin {pin}: there is no part {reference} on the sheet.");
            ChipInfo? chip = Catalog?.Get(part.Type);
            if (chip != null)
            {
                for (int i = 0; i < chip.Pins.Count; i++)
                {
                    if (chip.Pins[i].Number == number)
                        return;
                }
                throw new SheetEditException($"Pin {pin}: a {part.Type} has no pin {number}.");
            }
            for (int i = 0; i < Netlist.Gates.Count; i++)
            {
                Gate g = Netlist.Gates[i];
                if (g.Part == reference && (g.Output == number || g.Inputs.Contains(number)))
                    return;
            }
            throw new SheetEditException($"Pin {pin}: the catalogue has no chip file for {part.Type}, and no gate of {reference} uses pin {number}.");
        }

        private string FreeReference()
        {
            for (int row = 0; row < 26; row++)
            {
                for (int column = 1; column <= 40; column++)
                {
                    string r = column.ToString(CultureInfo.InvariantCulture) + (char)('A' + row);
                    if (FindPart(r) == null)
                        return r;
                }
            }
            throw new SheetEditException("Every board position from 1A to 40Z is taken.");
        }

        private string FreeNetName()
        {
            for (int n = 1; ; n++)
            {
                string name = "N" + n.ToString(CultureInfo.InvariantCulture);
                if (FindNet(name) == null)
                    return name;
            }
        }

        /// <summary>
        /// Sets the sheet's own display values (the "display" section), or null to remove them.
        /// Display only, so allowed in read-only mode; it is still a change to the file, so it is
        /// marked as needing saving and can be undone.
        /// </summary>
        /// <param name="display">
        /// The values, or null.
        /// </param>
        public void SetDisplay(SheetDisplay? display)
        {
            Snapshot();
            Root.Display = display;
            Done();
        }

        /// <summary>
        /// Sets which machine bus the card is for (sheet.bus). Like the display values it does not
        /// change a connection, so it is allowed in read-only mode; it is marked as needing saving
        /// and can be undone.
        /// </summary>
        /// <param name="bus">
        /// The bus.
        /// </param>
        public void SetBus(MachineBus bus)
        {
            Snapshot();
            Root.Sheet.Bus = bus;
            Done();
        }

        /// <summary>
        /// Sets how the redrawn drawing is cut into pages (format version 3). Drawing only, so
        /// allowed in read-only mode; it is marked as needing saving and can be undone.
        /// </summary>
        /// <param name="mode">
        /// The layout; as the original is written as no value (the default).
        /// </param>
        public void SetPageLayout(PageLayoutMode mode)
        {
            if (!CardPages.HasPages(Root))
                throw new SheetEditException("The card has no pages list (format version 3); convert it first.");
            Snapshot();
            Root.PageLayout = mode == PageLayoutMode.AsOriginal ? null : mode;
            Done();
        }

        private void Snapshot()
        {
            SyncBack();
            _undo.Add(NetlistReader.Write(Root));
            _redo.Clear();
        }

        private void Done()
        {
            SyncBack();
            Modified = true;
            Changed?.Invoke(this);
        }
    }

    /// <summary>
    /// A change to a sheet that was refused; the message says why in plain words. The sheet is
    /// left as it was.
    /// </summary>
    public sealed class SheetEditException : Exception
    {
        /// <summary>
        /// Create with a message.
        /// </summary>
        /// <param name="message">
        /// Why the change was refused.
        /// </param>
        public SheetEditException(string message) : base(message) { }

        /// <summary>
        /// Create with a message and the cause.
        /// </summary>
        /// <param name="message">
        /// Why the change was refused.
        /// </param>
        /// <param name="inner">
        /// The error that showed it.
        /// </param>
        public SheetEditException(string message, Exception inner) : base(message, inner) { }
    }
}
