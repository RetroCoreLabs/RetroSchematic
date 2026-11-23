//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Which symbol set the sheet is drawn with.
    /// </summary>
    public enum SymbolStyle
    {
        /// <summary>
        /// Norsk Data's own symbols, from ND-13.003.01 "Circuit Diagram Symbols" (July 1976):
        /// a plain box is NAND, a box with an X is NOR, a box with a diagonal and a small
        /// triangle is an inverter, a NAND box followed by the inverter's triangle is AND.
        /// </summary>
        NorskData,

        /// <summary>
        /// The usual distinctive-shape symbols (IEEE Std 91 shapes).
        /// </summary>
        Modern,
    }

    /// <summary>
    /// What the layout shows besides the parts, gates and wires.
    /// </summary>
    public sealed class LayoutOptions
    {
        /// <summary>
        /// Write the small pin number beside each pin line.
        /// </summary>
        public bool ShowPinNumbers { get; set; } = true;

        /// <summary>
        /// Write the board position (13D) in front of the chip type on each part label.
        /// </summary>
        public bool ShowBoardPositions { get; set; } = true;

        /// <summary>
        /// The chip catalogue, used to list every pin of a chip that has no gates; null lists only
        /// the pins that are on a signal.
        /// </summary>
        public ChipCatalog? Catalog { get; set; }
    }

    /// <summary>
    /// Lays a netlist sheet out as a <see cref="SchematicDrawing"/>.
    /// <list type="bullet">
    /// <item><description>A gate with a measured <c>position</c> is drawn there (millimetres times
    /// <see cref="UnitsPerMm"/>, plus the margin).</description></item>
    /// <item><description>A part with its own <c>position</c> has its frame there; a part with no
    /// position whose gates all have one gets a frame round its gates; any other part sits at its
    /// board position (column number, row letter).</description></item>
    /// <item><description>Each signal is drawn as wires between the pins it joins, in straight
    /// horizontal and vertical pieces, and its name is still written at each pin.</description></item>
    /// </list>
    /// </summary>
    public static class SheetLayout
    {
        /// <summary>
        /// Drawing units per millimetre of a measured position.
        /// </summary>
        public const double UnitsPerMm = 4;

        /// <summary>
        /// The empty border round the sheet, in drawing units.
        /// </summary>
        public const double Margin = 40;

        private const double CellWidth = 220;
        private const double CellHeight = 150;
        private const double GateWidth = 30;
        private const double GateHeight = 24;
        private const double GateSpacing = 34;
        private const double PinRow = 11;
        private const double PinDotRadius = 2.5;

        /// <summary>
        /// Lay out a whole sheet with pin numbers and board positions shown.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="style">
        /// ND 1976 or modern symbols.
        /// </param>
        /// <returns>
        /// The sheet as drawing steps.
        /// </returns>
        public static SchematicDrawing Build(Netlist netlist, SymbolStyle style) => Build(netlist, style, new LayoutOptions());

        /// <summary>
        /// Lay out a whole sheet.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="style">
        /// ND 1976 or modern symbols.
        /// </param>
        /// <param name="options">
        /// What to show.
        /// </param>
        /// <returns>
        /// The sheet as drawing steps.
        /// </returns>
        public static SchematicDrawing Build(Netlist netlist, SymbolStyle style, LayoutOptions options)
        {
            Dictionary<string, Net> netByPin = new(StringComparer.Ordinal);
            Dictionary<string, Confidence> pinConfidence = new(StringComparer.Ordinal);
            for (int i = 0; i < netlist.Nets.Count; i++)
            {
                Net net = netlist.Nets[i];
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    netByPin[net.Pins[j].At] = net;
                    pinConfidence[net.Pins[j].At] = net.Pins[j].Confidence;
                }
            }

            PartFrame[] frames = new PartFrame[netlist.Parts.Count];
            double right = 400, bottom = 200;
            for (int i = 0; i < netlist.Parts.Count; i++)
            {
                frames[i] = FrameOf(netlist, netlist.Parts[i], options);
                right = Math.Max(right, frames[i].X + frames[i].Width);
                bottom = Math.Max(bottom, frames[i].Y + frames[i].Height);
            }

            SchematicDrawing d = new(right + Margin + 60, bottom + Margin + 30);
            Text(d, DrawRole.Title, Confidence.Read, "", Margin, Margin - 15,
                netlist.Sheet.Id + "  " + netlist.Sheet.Title + "  (" + (style == SymbolStyle.NorskData ? "ND symbols" : "modern symbols") + ")",
                16, TextAnchor.Start);

            for (int i = 0; i < netlist.Parts.Count; i++)
            {
                d.Frames[netlist.Parts[i].Ref] = frames[i];
                LayPart(d, netlist, netlist.Parts[i], frames[i], netByPin, style, options);
            }
            LayWires(d, netlist, pinConfidence);
            return d;
        }

        /// <summary>
        /// Turns a drawing coordinate into millimetres of a measured position (the inverse of
        /// how a position is drawn).
        /// </summary>
        /// <param name="units">
        /// X or Y in drawing units.
        /// </param>
        /// <returns>
        /// The same place in millimetres, rounded to 0.5 mm.
        /// </returns>
        public static double ToMm(double units) => Math.Round((units - Margin) / UnitsPerMm * 2, MidpointRounding.AwayFromZero) / 2;

        /// <summary>
        /// Turns millimetres of a measured position into a drawing coordinate.
        /// </summary>
        /// <param name="mm">
        /// X or Y in millimetres.
        /// </param>
        /// <returns>
        /// The same place in drawing units.
        /// </returns>
        public static double FromMm(double mm) => Margin + (mm * UnitsPerMm);

        /// <summary>
        /// The less sure of two readings: OPEN, then INFERRED, then READ.
        /// </summary>
        /// <param name="a">
        /// One reading.
        /// </param>
        /// <param name="b">
        /// The other.
        /// </param>
        /// <returns>
        /// The less sure one.
        /// </returns>
        public static Confidence Worse(Confidence a, Confidence b)
        {
            if (a == Confidence.Open || b == Confidence.Open)
                return Confidence.Open;
            if (a == Confidence.Inferred || b == Confidence.Inferred)
                return Confidence.Inferred;
            return Confidence.Read;
        }

        /// <summary>
        /// Where a part's frame goes and how big it is. See the class summary for the three cases.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="part">
        /// The part.
        /// </param>
        /// <param name="options">
        /// What to show; the catalogue decides how many pins a gateless chip lists.
        /// </param>
        /// <returns>
        /// The frame in drawing units.
        /// </returns>
        private static PartFrame FrameOf(Netlist netlist, Part part, LayoutOptions options)
        {
            int gates = 0, measured = 0;
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            for (int i = 0; i < netlist.Gates.Count; i++)
            {
                Gate g = netlist.Gates[i];
                if (g.Part != part.Ref)
                    continue;
                gates++;
                if (g.Position == null)
                    continue;
                measured++;
                double gx = FromMm(g.Position.X), gy = FromMm(g.Position.Y);
                minX = Math.Min(minX, gx); minY = Math.Min(minY, gy);
                maxX = Math.Max(maxX, gx); maxY = Math.Max(maxY, gy);
            }

            if (part.Position == null && gates > 0 && measured == gates)
            {
                // room for the pin lines and the signal names on both sides
                return new PartFrame(minX - 70, minY - 20, (maxX - minX) + GateWidth + 140, (maxY - minY) + GateHeight + 28);
            }

            double w = CellWidth - 20;
            double h = CellHeight - 20;
            if (part.Position != null)
            {
                // sized by what is in it
                int rows = gates > 0 ? 0 : PinCount(netlist, part, options);
                h = gates > 0 ? Math.Max(60, 26 + (gates * GateSpacing)) : Math.Max(60, 30 + (Math.Min(rows, 16) * PinRow));
                if (gates == 0 && rows > 16)
                    w = 100 * ((rows + 15) / 16);
                return new PartFrame(FromMm(part.Position.X), FromMm(part.Position.Y), Math.Max(w, 140), h);
            }

            BoardPosition pos = BoardPosition.Parse(part.Ref);
            // Columns are drawn left to right in rising order. Which edge of the real board
            // column 1 is on has NOT been checked.
            return new PartFrame(Margin + ((pos.FirstColumn - 1) * CellWidth), Margin + (pos.FirstRow * CellHeight),
                ((pos.LastColumn - pos.FirstColumn + 1) * CellWidth) - 20, ((pos.LastRow - pos.FirstRow + 1) * CellHeight) - 20);
        }

        /// <summary>
        /// How many pins a gateless part lists: every pin of its chip file, else the ones on a signal.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="part">
        /// The part.
        /// </param>
        /// <param name="options">
        /// Holds the catalogue.
        /// </param>
        /// <returns>
        /// The number of pin rows.
        /// </returns>
        private static int PinCount(Netlist netlist, Part part, LayoutOptions options)
        {
            ChipInfo? chip = options.Catalog?.Get(part.Type);
            if (chip != null)
                return chip.Pins.Count;
            int n = 0;
            string prefix = part.Ref + ".";
            for (int i = 0; i < netlist.Nets.Count; i++)
            {
                List<PinReference> pins = netlist.Nets[i].Pins;
                for (int j = 0; j < pins.Count; j++)
                {
                    if (pins[j].At.StartsWith(prefix, StringComparison.Ordinal))
                        n++;
                }
            }
            return n;
        }

        private static void LayPart(SchematicDrawing d, Netlist netlist, Part part, PartFrame f,
            Dictionary<string, Net> netByPin, SymbolStyle style, LayoutOptions options)
        {
            string owner = "part:" + part.Ref;
            double x = f.X, y = f.Y, w = f.Width, h = f.Height;
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupStart, Owner = owner });
            d.Items.Add(new DrawItem
            {
                Kind = DrawKind.Rect,
                Role = DrawRole.PartOutline,
                Confidence = part.Confidence,
                Owner = owner,
                X1 = x,
                Y1 = y,
                X2 = w,
                Y2 = h,
                Dashed = part.Confidence != Confidence.Read,
            });
            DrawItem label = Text(d, DrawRole.PartLabel, part.Confidence, owner, x + 4, y + 12,
                options.ShowBoardPositions ? part.Ref + " " + part.Type : part.Type, 10, TextAnchor.Start);
            label.Bold = true;

            double gy = y + 22;
            int gatesDrawn = 0;
            for (int i = 0; i < netlist.Gates.Count; i++)
            {
                Gate gate = netlist.Gates[i];
                if (gate.Part != part.Ref)
                    continue;
                if (gate.Position != null)
                    LayGate(d, part, gate, FromMm(gate.Position.X), FromMm(gate.Position.Y), netByPin, style, options);
                else
                {
                    LayGate(d, part, gate, x + 60, gy, netByPin, style, options);
                    gy += GateSpacing;
                }
                gatesDrawn++;
            }

            if (gatesDrawn == 0)
                LayPinList(d, part, x, y, w, h, netByPin, options);
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupEnd, Owner = owner });
        }

        /// <summary>
        /// A chip with no gates read: its pins in columns, so a large chip (COM2601, 40 pins) shows
        /// every one. With the chip file, every pin is listed with its datasheet name, so an
        /// unconnected pin can still be wired; without it, only the pins on a signal.
        /// </summary>
        /// <param name="d">
        /// The drawing to add to.
        /// </param>
        /// <param name="part">
        /// The part.
        /// </param>
        /// <param name="x">
        /// Frame left.
        /// </param>
        /// <param name="y">
        /// Frame top.
        /// </param>
        /// <param name="w">
        /// Frame width.
        /// </param>
        /// <param name="h">
        /// Frame height.
        /// </param>
        /// <param name="netByPin">
        /// The signal on each pin.
        /// </param>
        /// <param name="options">
        /// What to show, and the catalogue.
        /// </param>
        private static void LayPinList(SchematicDrawing d, Part part, double x, double y, double w, double h,
            Dictionary<string, Net> netByPin, LayoutOptions options)
        {
            ChipInfo? chip = options.Catalog?.Get(part.Type);
            int count = chip != null ? chip.Pins.Count : 64;
            double ty = y + 26, tx = x + 10;
            for (int k = 0; k < count; k++)
            {
                int pin = chip != null ? chip.Pins[k].Number : k + 1;
                string at = part.Ref + "." + pin.ToString(CultureInfo.InvariantCulture);
                netByPin.TryGetValue(at, out Net? net);
                if (net == null && chip == null)
                    continue;
                if (ty > y + h - 8)
                {
                    ty = y + 26;
                    tx += 100;
                }
                string number = options.ShowPinNumbers ? pin.ToString("##0", CultureInfo.InvariantCulture).PadLeft(2) + ": " : "";
                if (net != null)
                {
                    DrawItem t = Text(d, DrawRole.SignalName, Confidence.Read, "net:" + net.Name, tx, ty, number + net.Name, 10, TextAnchor.Start);
                    t.Subscript = Subscript(net);
                }
                else
                    Text(d, DrawRole.Note, part.Confidence, "pin:" + at, tx, ty, number + chip!.Pins[k].Name, 10, TextAnchor.Start);
                PinDot(d, at, tx - 5, ty - 3);
                ty += PinRow;
            }
            if (chip == null && !string.IsNullOrEmpty(part.Function) && ty < y + h - 4)
                Text(d, DrawRole.Note, part.Confidence, "part:" + part.Ref, x + 6, y + h - 6, Shorten(part.Function!, (int)(w / 6)), 10, TextAnchor.Start);
        }

        private static void LayGate(SchematicDrawing d, Part part, Gate gate, double x, double y,
            Dictionary<string, Net> netByPin, SymbolStyle style, LayoutOptions options)
        {
            string owner = "gate:" + gate.Id;
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupStart, Owner = owner });
            if (style == SymbolStyle.NorskData)
                NorskDataSymbol(d, gate, owner, x, y);
            else
                ModernSymbol(d, gate, owner, x, y);
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupEnd, Owner = owner });

            // Input stubs with pin numbers and signal names.
            int n = gate.Inputs.Count;
            for (int i = 0; i < n; i++)
            {
                double py = y + ((i + 1) * GateHeight / (n + 1));
                Line(d, DrawRole.PinLine, gate.Confidence, owner, x - 12, py, x, py);
                if (options.ShowPinNumbers)
                    Text(d, DrawRole.PinNumber, gate.Confidence, owner, x - 11, py - 2, gate.Inputs[i].ToString(CultureInfo.InvariantCulture), 7, TextAnchor.Start);
                PinLabel(d, part, gate.Inputs[i], x - 14, py + 3, TextAnchor.End, netByPin);
                PinDot(d, part.Ref + "." + gate.Inputs[i].ToString(CultureInfo.InvariantCulture), x - 12, py);
            }

            double outX = x + GateWidth + (style == SymbolStyle.NorskData && gate.Kind == GateKind.And ? 10 : 0);
            double oy = y + (GateHeight / 2);
            Line(d, DrawRole.PinLine, gate.Confidence, owner, outX, oy, outX + 14, oy);
            if (options.ShowPinNumbers)
                Text(d, DrawRole.PinNumber, gate.Confidence, owner, outX + 3, oy - 2, gate.Output.ToString(CultureInfo.InvariantCulture), 7, TextAnchor.Start);
            PinLabel(d, part, gate.Output, outX + 16, oy + 3, TextAnchor.Start, netByPin);
            PinDot(d, part.Ref + "." + gate.Output.ToString(CultureInfo.InvariantCulture), outX + 14, oy);
        }

        /// <summary>
        /// The dot at a pin's end, where its wires start, and the pin's place in
        /// <see cref="SchematicDrawing.Pins"/>.
        /// </summary>
        /// <param name="d">
        /// The drawing to add to.
        /// </param>
        /// <param name="at">
        /// The pin, such as 11D.3.
        /// </param>
        /// <param name="x">
        /// Pin end X.
        /// </param>
        /// <param name="y">
        /// Pin end Y.
        /// </param>
        private static void PinDot(SchematicDrawing d, string at, double x, double y)
        {
            d.Pins[at] = new DrawPoint(x, y);
            d.Items.Add(new DrawItem { Kind = DrawKind.Circle, Role = DrawRole.PinDot, Confidence = Confidence.Read, Owner = "pin:" + at, X1 = x, Y1 = y, X2 = PinDotRadius });
        }

        /// <summary>
        /// One wire per pair of neighbouring pins of each signal. The drawn pins of a signal are
        /// sorted left to right and each is joined to the next by a horizontal piece to half way,
        /// a vertical piece, and a horizontal piece to the pin. A wire is drawn as sure as the
        /// less sure of its two pins, so a pin whose connection is OPEN shows in the OPEN colour.
        /// Pins that are not drawn (backplane, front connector) get no wire; their names stay at
        /// the drawn pins.
        /// </summary>
        /// <param name="d">
        /// The drawing to add to; its pin places must be filled already.
        /// </param>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="pinConfidence">
        /// How sure each pin's connection is.
        /// </param>
        private static void LayWires(SchematicDrawing d, Netlist netlist, Dictionary<string, Confidence> pinConfidence)
        {
            List<string> drawn = new();
            for (int i = 0; i < netlist.Nets.Count; i++)
            {
                Net net = netlist.Nets[i];
                drawn.Clear();
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    if (d.Pins.ContainsKey(net.Pins[j].At))
                        drawn.Add(net.Pins[j].At);
                }
                if (drawn.Count < 2)
                    continue;
                // left to right; equal X by Y, then by name, so the order never depends on the file
                drawn.Sort((a, b) =>
                {
                    DrawPoint pa = d.Pins[a], pb = d.Pins[b];
                    int c = pa.X.CompareTo(pb.X);
                    if (c == 0)
                        c = pa.Y.CompareTo(pb.Y);
                    return c != 0 ? c : string.CompareOrdinal(a, b);
                });
                string owner = "net:" + net.Name;
                for (int j = 1; j < drawn.Count; j++)
                {
                    DrawPoint a = d.Pins[drawn[j - 1]], b = d.Pins[drawn[j]];
                    Confidence c = Worse(pinConfidence[drawn[j - 1]], pinConfidence[drawn[j]]);
                    double mid = Math.Round((a.X + b.X) / 2);
                    Wire(d, c, owner, a.X, a.Y, mid, a.Y);
                    Wire(d, c, owner, mid, a.Y, mid, b.Y);
                    Wire(d, c, owner, mid, b.Y, b.X, b.Y);
                }
            }
        }

        private static void Wire(SchematicDrawing d, Confidence c, string owner, double x1, double y1, double x2, double y2)
        {
            if (x1 == x2 && y1 == y2)
                return;
            d.Items.Add(new DrawItem { Kind = DrawKind.Line, Role = DrawRole.Wire, Confidence = c, Owner = owner, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Dashed = c == Confidence.Open });
        }

        /// <summary>
        /// ND-13.003.01 pages 2 and 4-6: straight lines only.
        /// </summary>
        /// <param name="d">
        /// The drawing to add to.
        /// </param>
        /// <param name="gate">
        /// The gate.
        /// </param>
        /// <param name="owner">
        /// Owner name for the steps.
        /// </param>
        /// <param name="x">
        /// Left edge of the symbol.
        /// </param>
        /// <param name="y">
        /// Top edge of the symbol.
        /// </param>
        private static void NorskDataSymbol(SchematicDrawing d, Gate gate, string owner, double x, double y)
        {
            double r = x + GateWidth, b = y + GateHeight;
            Confidence c = gate.Confidence;
            d.Items.Add(new DrawItem { Kind = DrawKind.Rect, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = x, Y1 = y, X2 = GateWidth, Y2 = GateHeight });
            switch (gate.Kind)
            {
                case GateKind.Nand:
                    break;
                case GateKind.Nor:
                    Line(d, DrawRole.Symbol, c, owner, x, y, r, b);
                    Line(d, DrawRole.Symbol, c, owner, x, b, r, y);
                    break;
                case GateKind.Not:
                    Line(d, DrawRole.Symbol, c, owner, x, b, r, y);
                    Line(d, DrawRole.Symbol, c, owner, x + 4, y + 4, r - 4, y + (GateHeight / 2));
                    Line(d, DrawRole.Symbol, c, owner, r - 4, y + (GateHeight / 2), x + 4, b - 4);
                    break;
                case GateKind.And:
                    // The NAND box followed by the inverter's triangle (7408).
                    Line(d, DrawRole.Symbol, c, owner, r, y + 3, r + 10, y + (GateHeight / 2));
                    Line(d, DrawRole.Symbol, c, owner, r + 10, y + (GateHeight / 2), r, b - 3);
                    break;
                default:
                    // Not drawn by this layout yet: the kind is written in the box, so it shows as
                    // unfinished instead of looking like a NAND.
                    Text(d, DrawRole.Symbol, c, owner, x + 3, y + 15, gate.Kind.ToString().ToUpperInvariant(), 8, TextAnchor.Start);
                    break;
            }
        }

        private static void ModernSymbol(SchematicDrawing d, Gate gate, string owner, double x, double y)
        {
            double r = x + GateWidth, b = y + GateHeight, m = y + (GateHeight / 2);
            Confidence c = gate.Confidence;
            switch (gate.Kind)
            {
                case GateKind.And:
                case GateKind.Nand:
                    Path(d, c, owner, new[]
                    {
                        new PathSegment(PathOp.MoveTo, x, y),
                        new PathSegment(PathOp.LineTo, r - 12, y),
                        new PathSegment(PathOp.ArcClockwiseTo, r - 12, b, 12),
                        new PathSegment(PathOp.LineTo, x, b),
                        new PathSegment(PathOp.Close, x, y),
                    });
                    if (gate.Kind == GateKind.Nand)
                        Bubble(d, c, owner, r + 3, m);
                    break;
                case GateKind.Or:
                case GateKind.Nor:
                    Path(d, c, owner, new[]
                    {
                        new PathSegment(PathOp.MoveTo, x, y),
                        new PathSegment(PathOp.QuadTo, r, m, x + 18, y),
                        new PathSegment(PathOp.QuadTo, x, b, x + 18, b),
                        new PathSegment(PathOp.QuadTo, x, y, x + 8, m),
                        new PathSegment(PathOp.Close, x, y),
                    });
                    if (gate.Kind == GateKind.Nor)
                        Bubble(d, c, owner, r + 3, m);
                    break;
                case GateKind.Not:
                case GateKind.Buffer:
                    Path(d, c, owner, new[]
                    {
                        new PathSegment(PathOp.MoveTo, x, y),
                        new PathSegment(PathOp.LineTo, r - 4, m),
                        new PathSegment(PathOp.LineTo, x, b),
                        new PathSegment(PathOp.Close, x, y),
                    });
                    if (gate.Kind == GateKind.Not)
                        Bubble(d, c, owner, r - 1, m);
                    break;
                default:
                    d.Items.Add(new DrawItem { Kind = DrawKind.Rect, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = x, Y1 = y, X2 = GateWidth, Y2 = GateHeight });
                    Text(d, DrawRole.Symbol, c, owner, x + 3, y + 15, "?", 8, TextAnchor.Start);
                    break;
            }
        }

        private static void PinLabel(SchematicDrawing d, Part part, int pin, double x, double y, TextAnchor anchor,
            Dictionary<string, Net> netByPin)
        {
            if (!netByPin.TryGetValue(part.Ref + "." + pin.ToString(CultureInfo.InvariantCulture), out Net? net))
                return;
            DrawItem t = Text(d, DrawRole.SignalName, Confidence.Read, "net:" + net.Name, x, y, net.Name, 10, anchor);
            t.Subscript = Subscript(net);
        }

        /// <summary>
        /// ND's subscript on a signal name: 1 = active high, 0 = active low (manual page 2).
        /// </summary>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <returns>
        /// "1", "0", or empty when the signal has no active level.
        /// </returns>
        private static string Subscript(Net net)
            => net.Active == ActiveLevel.None ? "" : net.Active == ActiveLevel.High ? "1" : "0";

        private static DrawItem Text(SchematicDrawing d, DrawRole role, Confidence c, string owner, double x, double y,
            string text, double size, TextAnchor anchor)
        {
            DrawItem t = new() { Kind = DrawKind.Text, Role = role, Confidence = c, Owner = owner, X1 = x, Y1 = y, Text = text, FontSize = size, Anchor = anchor };
            d.Items.Add(t);
            return t;
        }

        private static void Line(SchematicDrawing d, DrawRole role, Confidence c, string owner, double x1, double y1, double x2, double y2)
            => d.Items.Add(new DrawItem { Kind = DrawKind.Line, Role = role, Confidence = c, Owner = owner, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 });

        private static void Path(SchematicDrawing d, Confidence c, string owner, PathSegment[] path)
            => d.Items.Add(new DrawItem { Kind = DrawKind.Path, Role = DrawRole.Symbol, Confidence = c, Owner = owner, Path = path });

        private static void Bubble(SchematicDrawing d, Confidence c, string owner, double cx, double cy)
            => d.Items.Add(new DrawItem { Kind = DrawKind.Circle, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = cx, Y1 = cy, X2 = 3 });

        private static string Shorten(string text, int max) => text.Length <= max ? text : text.Substring(0, Math.Max(0, max - 3)) + "...";
    }
}
