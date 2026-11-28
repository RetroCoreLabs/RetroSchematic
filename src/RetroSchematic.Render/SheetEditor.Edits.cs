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
    /// The edits that change what a part, a gate, a signal or the sheet's title block says,
    /// without placing or wiring anything: rename a part, set a part's facts, add or remove a
    /// gate, add a signal, rename a signal or set its facts, and set the sheet's title block. Each
    /// needs edit mode, is one undo step, and works on the level shown, like every other edit.
    /// </summary>
    public sealed partial class SheetEditor
    {
        /// <summary>
        /// Gives a part a new name (board position or printed name). Its gates (and their names,
        /// 13D/6 becomes 12D/6), its pins on every signal, a passive part serving it, and, on the
        /// top level, its symbol boxes and its hand-set board box follow.
        /// </summary>
        /// <param name="reference">
        /// The part's name now.
        /// </param>
        /// <param name="newReference">
        /// The new name.
        /// </param>
        /// <exception cref="SheetEditException">
        /// Read-only, no such part, the new name is taken, or it is not a usable name.
        /// </exception>
        public void RenamePart(string reference, string newReference)
        {
            CheckWritable();
            string to = newReference.Trim();
            if (FindPart(reference) == null)
                throw new SheetEditException("There is no part " + reference + " on this level.");
            if (to.Length == 0 || to.IndexOf('.') >= 0 || to.IndexOf(' ') >= 0 || to.IndexOf('/') >= 0)
                throw new SheetEditException("\"" + newReference + "\" cannot name a part: no dot, slash or space, and not empty.");
            if (ConnectorPins.IsConnector(to))
                throw new SheetEditException(to + " names a connector, not a part.");
            if (to == reference)
                return;
            if (FindPart(to) != null || Hierarchy.FindInstance(Netlist.Instances, to) != null)
                throw new SheetEditException("The name " + to + " is taken on this level.");
            Snapshot();
            FindPart(reference)!.Ref = to;
            for (int i = 0; i < Netlist.Parts.Count; i++)
            {
                if (Netlist.Parts[i].Serves == reference)
                    Netlist.Parts[i].Serves = to;
            }
            for (int i = 0; i < Netlist.Gates.Count; i++)
            {
                Gate g = Netlist.Gates[i];
                if (g.Part != reference)
                    continue;
                g.Part = to;
                if (g.Id.StartsWith(reference + "/", StringComparison.Ordinal))
                    g.Id = to + g.Id.Substring(reference.Length);
            }
            string prefix = reference + ".";
            for (int i = 0; i < Netlist.Nets.Count; i++)
            {
                List<PinReference> pins = Netlist.Nets[i].Pins;
                for (int j = 0; j < pins.Count; j++)
                {
                    if (pins[j].At.StartsWith(prefix, StringComparison.Ordinal) && pins[j].At.LastIndexOf('.') == reference.Length)
                        pins[j].At = to + pins[j].At.Substring(reference.Length);
                }
            }
            // the symbol boxes and the board boxes belong to the top level's parts
            if (_path.Count == 0)
            {
                for (int i = 0; Root.SymbolBoxes != null && i < Root.SymbolBoxes.Count; i++)
                {
                    if (Root.SymbolBoxes[i].Ref == reference)
                        Root.SymbolBoxes[i].Ref = to;
                }
                for (int i = 0; Root.Pcb?.Boxes != null && i < Root.Pcb.Boxes.Count; i++)
                {
                    if (Root.Pcb.Boxes[i].Ref == reference)
                        Root.Pcb.Boxes[i].Ref = to;
                }
            }
            Done();
        }

        /// <summary>
        /// Sets a part's facts. A null value keeps what is there; empty text removes an optional
        /// fact. Value and tolerance are only for passive parts and only as written on the drawing.
        /// </summary>
        /// <param name="reference">
        /// The part.
        /// </param>
        /// <param name="type">
        /// The new type, or null to keep it.
        /// </param>
        /// <param name="function">
        /// What it does, or null to keep, or empty to remove.
        /// </param>
        /// <param name="confidence">
        /// How sure the reading is, or null to keep.
        /// </param>
        /// <param name="note">
        /// Free text, or null to keep, or empty to remove.
        /// </param>
        /// <param name="value">
        /// A passive part's value as written, or null to keep, or empty to remove.
        /// </param>
        /// <param name="tolerance">
        /// A passive part's tolerance as written, or null to keep, or empty to remove.
        /// </param>
        /// <exception cref="SheetEditException">
        /// Read-only, no such part, an empty type, or a value on a chip.
        /// </exception>
        public void SetPartInfo(string reference, string? type, string? function, Confidence? confidence, string? note, string? value = null, string? tolerance = null)
        {
            CheckWritable();
            Part p = FindPart(reference) ?? throw new SheetEditException("There is no part " + reference + " on this level.");
            if (type != null && type.Trim().Length == 0)
                throw new SheetEditException("A part needs a type.");
            bool passive = PassiveParts.KindOf(type?.Trim() ?? p.Type) != PassiveKind.None;
            if (!passive && (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(tolerance)))
                throw new SheetEditException("Only a passive part (R, C, D ...) has a value or tolerance; " + reference + " is a " + (type ?? p.Type) + ".");
            Snapshot();
            p = FindPart(reference)!;
            if (type != null)
                p.Type = type.Trim();
            if (function != null)
                p.Function = function.Trim().Length == 0 ? null : function.Trim();
            if (confidence != null)
                p.Confidence = confidence.Value;
            if (note != null)
                p.Note = note.Trim().Length == 0 ? null : note.Trim();
            if (value != null)
                p.Value = value.Trim().Length == 0 ? null : value.Trim();
            if (tolerance != null)
                p.Tolerance = tolerance.Trim().Length == 0 ? null : tolerance.Trim();
            Done();
        }

        /// <summary>
        /// Adds a gate to a part. The gate is named after its output pin (13D/6) unless a name is
        /// given. When the catalogue has the chip's file, every pin must be on the chip.
        /// </summary>
        /// <param name="reference">
        /// The part.
        /// </param>
        /// <param name="kind">
        /// The kind of gate.
        /// </param>
        /// <param name="inputs">
        /// The input pin numbers.
        /// </param>
        /// <param name="output">
        /// The output pin number.
        /// </param>
        /// <param name="confidence">
        /// How sure the reading is.
        /// </param>
        /// <param name="note">
        /// Free text, or null.
        /// </param>
        /// <returns>
        /// The gate's name.
        /// </returns>
        /// <exception cref="SheetEditException">
        /// Read-only, no such part, no inputs, a pin the chip does not have, or the name is taken.
        /// </exception>
        public string AddGate(string reference, GateKind kind, List<int> inputs, int output, Confidence confidence, string? note = null)
        {
            CheckWritable();
            Part p = FindPart(reference) ?? throw new SheetEditException("There is no part " + reference + " on this level.");
            if (inputs.Count == 0)
                throw new SheetEditException("A gate needs at least one input pin.");
            string id = reference + "/" + output.ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < Netlist.Gates.Count; i++)
            {
                if (Netlist.Gates[i].Id == id)
                    throw new SheetEditException("There is a gate " + id + " already.");
            }
            ChipInfo? chip = Catalog?.Get(p.Type);
            if (chip != null)
            {
                for (int i = 0; i <= inputs.Count; i++)
                {
                    int pin = i < inputs.Count ? inputs[i] : output;
                    bool found = false;
                    for (int k = 0; k < chip.Pins.Count && !found; k++)
                        found = chip.Pins[k].Number == pin;
                    if (!found)
                        throw new SheetEditException("A " + p.Type + " has no pin " + pin.ToString(CultureInfo.InvariantCulture) + ".");
                }
            }
            Snapshot();
            Gate g = new() { Id = id, Part = reference, Kind = kind, Output = output, Confidence = confidence, Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim() };
            g.Inputs.AddRange(inputs);
            Netlist.Gates.Add(g);
            Done();
            return id;
        }

        /// <summary>
        /// Removes a gate. The part and its pins on the signals stay.
        /// </summary>
        /// <param name="id">
        /// The gate's name (13D/6).
        /// </param>
        /// <exception cref="SheetEditException">
        /// Read-only, or no such gate.
        /// </exception>
        public void RemoveGate(string id)
        {
            CheckWritable();
            int at = -1;
            for (int i = 0; i < Netlist.Gates.Count && at < 0; i++)
            {
                if (Netlist.Gates[i].Id == id)
                    at = i;
            }
            if (at < 0)
                throw new SheetEditException("There is no gate " + id + " on this level.");
            Snapshot();
            Netlist.Gates.RemoveAt(at);
            Done();
        }

        /// <summary>
        /// Adds a signal with no pins yet: for a signal that leaves a block through a port, or one
        /// a later connect fills.
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        /// <param name="active">
        /// Its active level.
        /// </param>
        /// <returns>
        /// The signal's key (BD3_0).
        /// </returns>
        /// <exception cref="SheetEditException">
        /// Read-only, an empty name, or the signal is there already.
        /// </exception>
        public string AddNet(string name, ActiveLevel active)
        {
            CheckWritable();
            string n = name.Trim();
            if (n.Length == 0)
                throw new SheetEditException("A signal needs a name.");
            if (FindNet(n, active) != null)
                throw new SheetEditException("There is a signal " + n + " of that level already.");
            Snapshot();
            Net net = new() { Name = n, Active = active };
            Netlist.Nets.Add(net);
            Done();
            return SheetLayout.NetKey(net);
        }

        /// <summary>
        /// Renames a signal or sets its facts. A null value keeps what is there; empty text removes
        /// an optional fact. A rename (name or level) is followed by its fixed wire path, its bus
        /// membership, the instance ports it is connected to on this level and, inside a block,
        /// the block's ports that carry it.
        /// </summary>
        /// <param name="key">
        /// The signal's key (BD3_0), or a name only one signal has.
        /// </param>
        /// <param name="newName">
        /// The new name, or null to keep it.
        /// </param>
        /// <param name="active">
        /// The new active level, or null to keep it.
        /// </param>
        /// <param name="printed">
        /// The name as printed on the drawing, or null to keep, or empty to remove.
        /// </param>
        /// <param name="note">
        /// Free text, or null to keep, or empty to remove.
        /// </param>
        /// <param name="pullUp">
        /// The pull-up resistor, or null to keep, or empty to remove.
        /// </param>
        /// <returns>
        /// The signal's key afterwards.
        /// </returns>
        /// <exception cref="SheetEditException">
        /// Read-only, no such signal, or the new name and level are taken.
        /// </exception>
        public string SetNet(string key, string? newName, ActiveLevel? active, string? printed, string? note, string? pullUp)
        {
            CheckWritable();
            Net n = FindNetByKey(key) ?? FindNet(key) ?? throw new SheetEditException("There is no signal " + key + " on this level.");
            string name = newName?.Trim() ?? n.Name;
            ActiveLevel level = active ?? n.Active;
            if (name.Length == 0)
                throw new SheetEditException("A signal needs a name.");
            bool renamed = name != n.Name || level != n.Active;
            if (renamed)
            {
                Net? other = FindNet(name, level);
                if (other != null && other != n)
                    throw new SheetEditException("There is a signal " + name + " of that level already; connect the pins to join them.");
            }
            string oldKey = SheetLayout.NetKey(n);
            Snapshot();
            n = FindNetByKey(oldKey)!;
            if (renamed)
                RenameReferences(n, name, level);
            n.Name = name;
            n.Active = level;
            if (printed != null)
                n.Printed = printed.Trim().Length == 0 ? null : printed.Trim();
            if (note != null)
                n.Note = note.Trim().Length == 0 ? null : note.Trim();
            if (pullUp != null)
                n.PullUp = pullUp.Trim().Length == 0 ? null : pullUp.Trim();
            Done();
            return SheetLayout.NetKey(n);
        }

        /// <summary>
        /// Sets the sheet's title block. A null value keeps what is there; empty text removes an
        /// optional fact (the number and title cannot be removed).
        /// </summary>
        /// <param name="id">
        /// The sheet number, or null to keep.
        /// </param>
        /// <param name="title">
        /// The title, or null to keep.
        /// </param>
        /// <param name="machine">
        /// The machine, or null to keep, or empty to remove.
        /// </param>
        /// <param name="drawn">
        /// Who drew it and when, or null to keep, or empty to remove.
        /// </param>
        /// <param name="updated">
        /// The last change written on it, or null to keep, or empty to remove.
        /// </param>
        /// <param name="company">
        /// The company, or null to keep, or empty to remove.
        /// </param>
        /// <param name="note">
        /// Free text, or null to keep, or empty to remove.
        /// </param>
        /// <param name="document">
        /// The source document it was read from, or null to keep.
        /// </param>
        /// <param name="pdfPage">
        /// The page in that document, or null to keep, or 0 to remove.
        /// </param>
        /// <exception cref="SheetEditException">
        /// Read-only, or an empty number or title.
        /// </exception>
        public void SetSheetInfo(string? id, string? title, string? machine, string? drawn, string? updated, string? company, string? note, string? document, int? pdfPage)
        {
            CheckWritable();
            if ((id != null && id.Trim().Length == 0) || (title != null && title.Trim().Length == 0))
                throw new SheetEditException("A sheet needs a number and a title.");
            Snapshot();
            Sheet s = Root.Sheet;
            if (id != null)
                s.Id = id.Trim();
            if (title != null)
                s.Title = title.Trim();
            if (machine != null)
                s.Machine = Optional(machine);
            if (drawn != null)
                s.Drawn = Optional(drawn);
            if (updated != null)
                s.Updated = Optional(updated);
            if (company != null)
                s.Company = Optional(company);
            if (note != null)
                s.Note = Optional(note);
            if (document != null)
                s.Source.Document = document.Trim();
            if (pdfPage != null)
                s.Source.PdfPage = pdfPage.Value == 0 ? null : pdfPage.Value;
            Done();
        }

        private static string? Optional(string text) => text.Trim().Length == 0 ? null : text.Trim();

        // every place on this level that names the signal follows a rename: its fixed wire path,
        // a bus member, an instance port connection and, inside a block, the block's own ports
        private void RenameReferences(Net n, string name, ActiveLevel level)
        {
            List<WirePath>? wires = Netlist.Layout?.Wires;
            for (int i = 0; wires != null && i < wires.Count; i++)
            {
                if (wires[i].Net == n.Name && (wires[i].Active == null || wires[i].Active == n.Active))
                {
                    wires[i].Net = name;
                    wires[i].Active = wires[i].Active == null ? null : level;
                }
            }
            List<BusLine>? buses = Netlist.Layout?.Buses;
            for (int i = 0; buses != null && i < buses.Count; i++)
            {
                for (int k = 0; k < buses[i].Members.Count; k++)
                {
                    BusMember m = buses[i].Members[k];
                    if (m.Net == n.Name && (m.Active == null || m.Active == n.Active))
                    {
                        m.Net = name;
                        m.Active = m.Active == null ? null : level;
                    }
                }
            }
            for (int i = 0; Netlist.Instances != null && i < Netlist.Instances.Count; i++)
            {
                List<PortConnection>? conns = Netlist.Instances[i].Connections;
                for (int c = 0; conns != null && c < conns.Count; c++)
                    Follow(conns[c].Nets, n, name, level);
            }
            BlockDefinition? inside = CurrentBlock();
            for (int i = 0; inside != null && i < inside.Ports.Count; i++)
                Follow(inside.Ports[i].Signals, n, name, level);
        }

        private void Follow(List<SignalRef> refs, Net n, string name, ActiveLevel level)
        {
            for (int k = 0; k < refs.Count; k++)
            {
                if (NetlistReader.FindNet(Netlist.Nets, refs[k]) != n)
                    continue;
                refs[k].Net = name;
                refs[k].Active = level;
            }
        }
    }
}
