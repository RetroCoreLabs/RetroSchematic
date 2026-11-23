//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// What the trace tool makes of one drawing step.
    /// </summary>
    public enum TraceRole
    {
        /// <summary>
        /// Nothing is traced: draw as usual.
        /// </summary>
        None,

        /// <summary>
        /// Part of the traced signal (a wire, label, name or pin): drawn thick in the trace colour.
        /// </summary>
        Signal,

        /// <summary>
        /// The outline or label of a chip or gate the signal joins: drawn in the trace colour.
        /// </summary>
        Joined,

        /// <summary>
        /// Everything else: drawn faint.
        /// </summary>
        Dimmed,
    }

    /// <summary>
    /// One open sheet and what the user is looking at: the symbol style, what is shown, the drawing
    /// laid out for it, and the picked part, gate, signal or pin. Changes to the sheet go through
    /// <see cref="Editor"/>; the drawing is laid out again after each one. The desktop window only
    /// shows this object; every action is a method here or on the editor, so the command-line
    /// tool, the MCP tool and tests can do the same things.
    /// </summary>
    public sealed class SheetSession
    {
        private readonly LayoutOptions _options = new();
        private readonly Dictionary<string, SignalValue> _values = new(StringComparer.Ordinal);
        private readonly HashSet<string> _tracePins = new(StringComparer.Ordinal);
        private readonly HashSet<string> _traceOwners = new(StringComparer.Ordinal);

        /// <summary>
        /// Called after the drawing was laid out again (new style, new options, or an edit).
        /// </summary>
        /// <param name="session">
        /// The session whose drawing changed.
        /// </param>
        public delegate void DrawingChangedHandler(SheetSession session);

        /// <summary>
        /// Called after the picked item changed; <paramref name="owner"/> is empty for none.
        /// </summary>
        /// <param name="session">
        /// The session whose pick changed.
        /// </param>
        /// <param name="owner">
        /// The new pick, or empty for none.
        /// </param>
        public delegate void SelectionChangedHandler(SheetSession session, string owner);

        /// <summary>
        /// The drawing was laid out again.
        /// </summary>
        public event DrawingChangedHandler? DrawingChanged;

        /// <summary>
        /// The picked item changed.
        /// </summary>
        public event SelectionChangedHandler? SelectionChanged;

        /// <summary>
        /// The traced signal changed (see <see cref="Trace"/>).
        /// </summary>
        public event DrawingChangedHandler? TraceChanged;

        /// <summary>
        /// Opens a sheet file, read-only. <paramref name="catalogFolder"/> is the catalogue's folder
        /// (the one holding <c>chips/</c>), used to show what a picked chip is, to list a gateless
        /// chip's pins and to place chips; null leaves that out.
        /// </summary>
        /// <param name="sheetPath">
        /// The sheet file.
        /// </param>
        /// <param name="style">
        /// The symbol set to start with.
        /// </param>
        /// <param name="catalogFolder">
        /// The catalogue folder (the one holding chips), or null.
        /// </param>
        public SheetSession(string sheetPath, SymbolStyle style, string? catalogFolder)
            : this(NetlistReader.ReadFile(sheetPath), sheetPath, style, catalogFolder)
        {
        }

        /// <summary>
        /// Shows a sheet already in memory (a new design has no file yet), read-only.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="sheetPath">
        /// The file it came from or will be saved to, or null for a new design not saved yet.
        /// </param>
        /// <param name="style">
        /// The symbol set to start with.
        /// </param>
        /// <param name="catalogFolder">
        /// The catalogue folder (the one holding chips), or null.
        /// </param>
        public SheetSession(Netlist netlist, string? sheetPath, SymbolStyle style, string? catalogFolder)
        {
            SheetPath = sheetPath;
            Style = style;
            _options.Catalog = catalogFolder == null ? null : new ChipCatalog(catalogFolder);
            Editor = new SheetEditor(netlist, _options.Catalog);
            Editor.Changed += OnEdited;
            Drawing = SheetLayout.Build(netlist, style, _options);
        }

        /// <summary>
        /// The sheet file, or null for a new design not saved yet.
        /// </summary>
        public string? SheetPath { get; private set; }

        /// <summary>
        /// The sheet's contents as they are now.
        /// </summary>
        public Netlist Netlist => Editor.Netlist;

        /// <summary>
        /// Every change to the sheet goes through this. It starts read-only.
        /// </summary>
        public SheetEditor Editor { get; }

        /// <summary>
        /// The symbol set in use.
        /// </summary>
        public SymbolStyle Style { get; private set; }

        /// <summary>
        /// True when pin numbers are drawn.
        /// </summary>
        public bool ShowPinNumbers => _options.ShowPinNumbers;

        /// <summary>
        /// True when the board position is written on each part label.
        /// </summary>
        public bool ShowBoardPositions => _options.ShowBoardPositions;

        /// <summary>
        /// The laid-out drawing for <see cref="Style"/>.
        /// </summary>
        public SchematicDrawing Drawing { get; private set; }

        /// <summary>
        /// The picked item ("part:11D", "gate:11D/1", "net:BD0", "pin:11D.3"), or empty.
        /// </summary>
        public string Selected { get; private set; } = "";

        /// <summary>
        /// The signal the trace tool shows, or empty for none.
        /// </summary>
        public string Traced { get; private set; } = "";

        /// <summary>
        /// Changes the symbol set and lays the sheet out again. The picked item stays picked.
        /// </summary>
        /// <param name="style">
        /// The symbol set to use.
        /// </param>
        public void SetStyle(SymbolStyle style)
        {
            if (style == Style)
                return;
            Style = style;
            Relayout();
        }

        /// <summary>
        /// Shows or hides the pin numbers.
        /// </summary>
        /// <param name="show">
        /// True to show them.
        /// </param>
        public void SetShowPinNumbers(bool show)
        {
            if (show == _options.ShowPinNumbers)
                return;
            _options.ShowPinNumbers = show;
            Relayout();
        }

        /// <summary>
        /// Shows or hides the board positions on the part labels.
        /// </summary>
        /// <param name="show">
        /// True to show them.
        /// </param>
        public void SetShowBoardPositions(bool show)
        {
            if (show == _options.ShowBoardPositions)
                return;
            _options.ShowBoardPositions = show;
            Relayout();
        }

        /// <summary>
        /// Saves the sheet (see <see cref="SheetEditor.Save"/>) and remembers the file for the next save.
        /// </summary>
        /// <param name="path">
        /// The file to write.
        /// </param>
        public void SaveAs(string path)
        {
            Editor.Save(path);
            SheetPath = path;
        }

        /// <summary>
        /// Traces the signal under a point: a wire, a net-label diamond, a signal name or a pin.
        /// Anything else (empty paper, a chip) clears the trace.
        /// </summary>
        /// <param name="x">
        /// Point X in drawing units.
        /// </param>
        /// <param name="y">
        /// Point Y in drawing units.
        /// </param>
        public void TraceAt(double x, double y)
        {
            string hit = Drawing.HitTest(x, y);
            string net = "";
            if (hit.StartsWith("net:", StringComparison.Ordinal))
                net = hit.Substring(4);
            else if (hit.StartsWith("pin:", StringComparison.Ordinal))
            {
                Net? n = Editor.NetOf(hit.Substring(4));
                net = n == null ? "" : SheetLayout.NetKey(n);
            }
            Trace(net);
        }

        /// <summary>
        /// Shows one whole signal: its wires, labels and pins lit, the chips and gates it joins
        /// outlined, everything else dimmed. Empty (or an unknown name) clears the trace.
        /// </summary>
        /// <param name="net">
        /// The signal's name, or empty.
        /// </param>
        public void Trace(string net)
        {
            Net? n = net.Length == 0 ? null : Editor.FindNetByKey(net);
            _tracePins.Clear();
            _traceOwners.Clear();
            Traced = n == null ? "" : SheetLayout.NetKey(n);
            if (n != null)
            {
                for (int i = 0; i < n.Pins.Count; i++)
                {
                    string at = n.Pins[i].At;
                    _tracePins.Add(at);
                    string part = n.Pins[i].PartRef;
                    _traceOwners.Add("part:" + part);
                    int pin = n.Pins[i].Pin;
                    for (int g = 0; g < Netlist.Gates.Count; g++)
                    {
                        Gate gate = Netlist.Gates[g];
                        if (gate.Part == part && (gate.Output == pin || gate.Inputs.Contains(pin)))
                            _traceOwners.Add("gate:" + gate.Id);
                    }
                }
            }
            TraceChanged?.Invoke(this);
        }

        /// <summary>
        /// What the trace makes of a drawing step.
        /// </summary>
        /// <param name="item">
        /// A drawing step.
        /// </param>
        /// <returns>
        /// <see cref="TraceRole.None"/> when nothing is traced.
        /// </returns>
        public TraceRole TraceOf(DrawItem item)
        {
            if (Traced.Length == 0)
                return TraceRole.None;
            if (item.Owner == "net:" + Traced)
                return TraceRole.Signal;
            if (item.Owner.StartsWith("pin:", StringComparison.Ordinal) && _tracePins.Contains(item.Owner.Substring(4)))
                return TraceRole.Signal;
            if ((item.Role == DrawRole.Symbol || item.Role == DrawRole.PartOutline || item.Role == DrawRole.PartLabel) && _traceOwners.Contains(item.Owner))
                return TraceRole.Joined;
            return TraceRole.Dimmed;
        }

        /// <summary>
        /// The pins of the traced signal, in the file's order, for the side panel.
        /// </summary>
        /// <returns>
        /// The pins (11D.3), or an empty list.
        /// </returns>
        public List<string> TracedPins()
        {
            List<string> list = new();
            Net? n = Traced.Length == 0 ? null : Editor.FindNetByKey(Traced);
            if (n != null)
            {
                for (int i = 0; i < n.Pins.Count; i++)
                    list.Add(n.Pins[i].At);
            }
            return list;
        }

        /// <summary>
        /// Fixes a signal's wire, as it is drawn now, into the sheet's layout section (see
        /// <see cref="SheetEditor.SetWirePath"/>).
        /// </summary>
        /// <param name="net">
        /// The signal's key on the drawing (<see cref="SheetLayout.NetKey"/>: BD15_0), or a name
        /// that only one signal has.
        /// </param>
        public void FixWire(string net)
        {
            Net? n = null;
            for (int i = 0; i < Netlist.Nets.Count && n == null; i++)
            {
                if (SheetLayout.NetKey(Netlist.Nets[i]) == net)
                    n = Netlist.Nets[i];
            }
            n ??= Editor.FindNet(net) ?? throw new SheetEditException($"There is no signal {net}.");
            if (!Drawing.WireBranches.TryGetValue(SheetLayout.NetKey(n), out List<List<DrawPoint>>? branches))
                throw new SheetEditException($"Signal {net} has no wire on the drawing (it is shown with labels).");
            List<List<Point>> mm = new();
            for (int b = 0; b < branches.Count; b++)
            {
                List<Point> pts = new();
                for (int i = 0; i < branches[b].Count; i++)
                    pts.Add(new Point { X = SheetLayout.ToMm(branches[b][i].X), Y = SheetLayout.ToMm(branches[b][i].Y) });
                mm.Add(pts);
            }
            Editor.SetWirePath(n.Name, mm, n.Active == ActiveLevel.None ? null : n.Active);
        }

        /// <summary>
        /// Where a pin is drawn: the middle of its pin line, for centring the view on it.
        /// </summary>
        /// <param name="at">
        /// The pin (11D.3).
        /// </param>
        /// <param name="x">
        /// The pin's X in drawing units, 0 when it is not drawn.
        /// </param>
        /// <param name="y">
        /// The pin's Y in drawing units, 0 when it is not drawn.
        /// </param>
        /// <returns>
        /// False when the pin is not drawn on this sheet (a backplane pin, or no such pin).
        /// </returns>
        public bool PinPoint(string at, out double x, out double y)
        {
            string owner = "pin:" + at;
            for (int i = 0; i < Drawing.Items.Count; i++)
            {
                if (Drawing.Items[i].Owner == owner)
                {
                    SchematicDrawing.Bounds(Drawing.Items[i], out double l, out double t, out double r, out double b);
                    x = (l + r) / 2;
                    y = (t + b) / 2;
                    return true;
                }
            }
            x = 0;
            y = 0;
            return false;
        }

        /// <summary>
        /// Gives a signal a simulated value; the window then draws its wires, labels and names in
        /// that value's colour. <see cref="SignalValue.NotSimulated"/> takes the value away.
        /// </summary>
        /// <param name="net">
        /// The signal's name.
        /// </param>
        /// <param name="value">
        /// Its value.
        /// </param>
        public void SetValue(string net, SignalValue value)
        {
            // kept by the signal's key, so two signals of the same name keep their own values
            Net? n = Editor.FindNetByKey(net);
            string key = n == null ? net : SheetLayout.NetKey(n);
            if (value == SignalValue.NotSimulated)
                _values.Remove(key);
            else
                _values[key] = value;
            DrawingChanged?.Invoke(this);
        }

        /// <summary>
        /// The simulated value of a signal, <see cref="SignalValue.NotSimulated"/> when it has none.
        /// </summary>
        /// <param name="net">
        /// The signal's name.
        /// </param>
        /// <returns>
        /// Its value.
        /// </returns>
        public SignalValue ValueOf(string net)
        {
            Net? n = Editor.FindNetByKey(net);
            return _values.TryGetValue(n == null ? net : SheetLayout.NetKey(n), out SignalValue v) ? v : SignalValue.NotSimulated;
        }

        /// <summary>
        /// The value an item shows: its signal's value for a wire, a net label or a signal name,
        /// and <see cref="SignalValue.NotSimulated"/> for everything else (chips, gates, pins).
        /// </summary>
        /// <param name="item">
        /// A drawing step.
        /// </param>
        /// <returns>
        /// The value to colour it by.
        /// </returns>
        public SignalValue ValueOf(DrawItem item)
        {
            if (_values.Count == 0 || !item.Owner.StartsWith("net:", StringComparison.Ordinal))
                return SignalValue.NotSimulated;
            if (item.Role != DrawRole.Wire && item.Role != DrawRole.NetLabel && item.Role != DrawRole.SignalName && item.Role != DrawRole.PinLine)
                return SignalValue.NotSimulated;
            return ValueOf(item.Owner.Substring(4));
        }

        /// <summary>
        /// Picks whatever lies under a point of the drawing (nothing picks nothing).
        /// </summary>
        /// <param name="x">
        /// Point X in drawing units.
        /// </param>
        /// <param name="y">
        /// Point Y in drawing units.
        /// </param>
        public void PickAt(double x, double y) => Select(Drawing.HitTest(x, y));

        /// <summary>
        /// Picks an item by owner name ("part:11D"); empty clears the pick.
        /// </summary>
        /// <param name="owner">
        /// The item to pick, such as part:11D, or empty for none.
        /// </param>
        public void Select(string owner)
        {
            if (owner == Selected)
                return;
            Selected = owner;
            SelectionChanged?.Invoke(this, owner);
        }

        /// <summary>
        /// True if an item belongs to the picked thing. Picking a signal lights its names, its
        /// wires and the dots of its pins; picking a gate lights the gate; picking a part lights the
        /// part and its gates.
        /// </summary>
        /// <param name="item">
        /// A drawing step.
        /// </param>
        /// <returns>
        /// True if the step belongs to the picked item.
        /// </returns>
        public bool IsLit(DrawItem item)
        {
            if (Selected.Length == 0 || item.Owner.Length == 0)
                return false;
            if (item.Owner == Selected)
                return true;
            // a gate belongs to its part: "gate:11D/1" is lit when "part:11D" is picked
            if (Selected.StartsWith("part:", StringComparison.Ordinal) && item.Owner.StartsWith("gate:", StringComparison.Ordinal))
                return OwnerStartsWithPart(item.Owner, 5, Selected.Substring(5), '/');
            // a picked bus lights its members' wires and names
            if (Selected.StartsWith("bus:", StringComparison.Ordinal) && item.Owner.StartsWith("net:", StringComparison.Ordinal))
            {
                Net? member = Editor.FindNetByKey(item.Owner.Substring(4));
                BusLine? bus = member == null ? null : Editor.BusOf(member);
                return bus != null && bus.Name == Selected.Substring(4);
            }
            // a pin belongs to the picked signal when the signal joins it
            if (Selected.StartsWith("net:", StringComparison.Ordinal) && item.Owner.StartsWith("pin:", StringComparison.Ordinal))
            {
                Net? net = Editor.NetOf(item.Owner.Substring(4));
                return net != null && SheetLayout.NetKey(net) == Selected.Substring(4);
            }
            return false;
        }

        /// <summary>
        /// Every part, signal and pin whose name contains the text (case does not matter): parts
        /// by reference or type, signals by name, pins written as 11D.3. Parts first, then signals,
        /// then pins, each in sheet order.
        /// </summary>
        /// <param name="text">
        /// What to look for.
        /// </param>
        /// <returns>
        /// The owner names found ("part:11D", "net:BD0", "pin:11D.3").
        /// </returns>
        public List<string> Search(string text)
        {
            List<string> found = new();
            string t = text.Trim();
            if (t.Length == 0)
                return found;
            Netlist n = Netlist;
            for (int i = 0; i < n.Parts.Count; i++)
            {
                if (Has(n.Parts[i].Ref, t) || Has(n.Parts[i].Type, t))
                    found.Add("part:" + n.Parts[i].Ref);
            }
            for (int i = 0; i < n.Nets.Count; i++)
            {
                if (Has(n.Nets[i].Name, t))
                    found.Add("net:" + SheetLayout.NetKey(n.Nets[i]));
            }
            // only text with a dot looks for pins, else "1" would list every pin 1
            if (t.IndexOf('.') >= 0)
            {
                for (int i = 0; i < n.Nets.Count; i++)
                {
                    List<PinReference> pins = n.Nets[i].Pins;
                    for (int j = 0; j < pins.Count; j++)
                    {
                        if (Has(pins[j].At, t))
                            found.Add("pin:" + pins[j].At);
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// Plain-text description of the picked item, for the side panel: what it is, how sure
        /// the reading is, and where it connects. Empty when nothing is picked.
        /// </summary>
        /// <returns>
        /// The description, one fact per line.
        /// </returns>
        public string Describe()
        {
            if (Selected.StartsWith("part:", StringComparison.Ordinal))
                return DescribePart(Selected.Substring(5));
            if (Selected.StartsWith("gate:", StringComparison.Ordinal))
                return DescribeGate(Selected.Substring(5));
            if (Selected.StartsWith("net:", StringComparison.Ordinal))
                return DescribeNet(Selected.Substring(4));
            if (Selected.StartsWith("pin:", StringComparison.Ordinal))
                return DescribePin(Selected.Substring(4));
            if (Selected.StartsWith("bus:", StringComparison.Ordinal))
                return DescribeBus(Selected.Substring(4));
            return "";
        }

        private static bool Has(string value, string text) => value.Contains(text, StringComparison.OrdinalIgnoreCase);

        private static bool OwnerStartsWithPart(string owner, int start, string part, char separator)
            => owner.Length > start + part.Length && owner[start + part.Length] == separator
                && string.CompareOrdinal(owner, start, part, 0, part.Length) == 0;

        private void Relayout()
        {
            Drawing = SheetLayout.Build(Netlist, Style, _options);
            DrawingChanged?.Invoke(this);
        }

        private void OnEdited(SheetEditor editor)
        {
            Relayout();
            if (Traced.Length > 0)
                Trace(Traced);
            // a pick that no longer exists (a deleted part) is dropped
            if (Selected.Length > 0 && Describe().Length == 0)
                Select("");
        }

        private string DescribePart(string reference)
        {
            StringBuilder sb = new();
            Part? p = Editor.FindPart(reference);
            if (p == null)
                return "";
            sb.Append("Part ").Append(p.Ref).Append(": ").Append(p.Type).Append('\n');
            if (!string.IsNullOrEmpty(p.Function))
                sb.Append(p.Function).Append('\n');
            sb.Append("Reading: ").Append(ConfidenceText(p.Confidence)).Append('\n');
            if (!string.IsNullOrEmpty(p.Note))
                sb.Append("Note: ").Append(p.Note).Append('\n');
            AppendSource(sb);
            ChipInfo? chip = _options.Catalog?.Get(p.Type);
            if (_options.Catalog != null)
            {
                if (chip == null)
                    sb.Append("Catalogue: no chip file for ").Append(p.Type).Append('\n');
                else
                {
                    sb.Append("Catalogue: ").Append(chip.Title).Append('\n');
                    for (int i = 0; i < chip.Formulas.Count; i++)
                        sb.Append("  ").Append(chip.Formulas[i]).Append('\n');
                }
            }

            // the pins and their signals: every pin of the chip file, else the pins on a signal
            sb.Append("Pins:\n");
            if (chip != null)
            {
                for (int i = 0; i < chip.Pins.Count; i++)
                    AppendPinLine(sb, p.Ref, chip.Pins[i].Number, chip.Pins[i].Name);
            }
            else
            {
                for (int pin = 1; pin <= 64; pin++)
                {
                    if (Editor.NetOf(p.Ref + "." + pin.ToString(CultureInfo.InvariantCulture)) != null)
                        AppendPinLine(sb, p.Ref, pin, "");
                }
            }
            return sb.ToString();
        }

        private void AppendPinLine(StringBuilder sb, string reference, int pin, string name)
        {
            string at = reference + "." + pin.ToString(CultureInfo.InvariantCulture);
            Net? net = Editor.NetOf(at);
            sb.Append("  ").Append(pin.ToString(CultureInfo.InvariantCulture).PadLeft(2));
            if (name.Length > 0)
                sb.Append(' ').Append(name.PadRight(5));
            sb.Append("  ").Append(net == null ? "-" : net.Name);
            if (net != null)
            {
                PinReference? r = FindPin(net, at);
                if (r != null && r.Confidence != Confidence.Read)
                    sb.Append("  (").Append(ConfidenceText(r.Confidence)).Append(')');
            }
            sb.Append('\n');
        }

        private void AppendSource(StringBuilder sb)
        {
            SourceReference s = Netlist.Sheet.Source;
            sb.Append("Source: sheet ").Append(Netlist.Sheet.Id).Append(", ").Append(s.Document);
            if (s.PdfPage.HasValue)
                sb.Append(", page ").Append(s.PdfPage.Value.ToString(CultureInfo.InvariantCulture));
            sb.Append('\n');
        }

        private string DescribeGate(string id)
        {
            StringBuilder sb = new();
            for (int i = 0; i < Netlist.Gates.Count; i++)
            {
                Gate g = Netlist.Gates[i];
                if (g.Id != id)
                    continue;
                sb.Append("Gate ").Append(g.Id).Append(": ").Append(g.Kind.ToString().ToUpperInvariant()).Append('\n');
                sb.Append("Inputs: pins ");
                for (int j = 0; j < g.Inputs.Count; j++)
                    sb.Append(j == 0 ? "" : ", ").Append(g.Inputs[j].ToString(CultureInfo.InvariantCulture));
                sb.Append("\nOutput: pin ").Append(g.Output.ToString(CultureInfo.InvariantCulture)).Append('\n');
                sb.Append("Reading: ").Append(ConfidenceText(g.Confidence)).Append('\n');
                if (!string.IsNullOrEmpty(g.Note))
                    sb.Append("Note: ").Append(g.Note).Append('\n');
            }
            return sb.ToString();
        }

        private string DescribeNet(string name)
        {
            StringBuilder sb = new();
            Net? n = Editor.FindNetByKey(name);
            if (n == null)
                return "";
            sb.Append("Signal ").Append(n.Name);
            if (n.Active != ActiveLevel.None)
                sb.Append(n.Active == ActiveLevel.High ? " (active high, subscript 1)" : " (active low, subscript 0)");
            sb.Append('\n');
            if (n.Drive.HasValue)
                sb.Append("Line type: ").Append(n.Drive.Value.ToString()).Append('\n');
            if (n.PullUp != null)
                sb.Append("Pull-up: ").Append(n.PullUp).Append('\n');
            sb.Append("Pins:\n");
            for (int j = 0; j < n.Pins.Count; j++)
                sb.Append("  ").Append(n.Pins[j].At).Append("  (").Append(ConfidenceText(n.Pins[j].Confidence)).Append(")\n");
            if (!string.IsNullOrEmpty(n.Note))
                sb.Append("Note: ").Append(n.Note).Append('\n');
            return sb.ToString();
        }

        private string DescribePin(string at)
        {
            int dot = at.LastIndexOf('.');
            if (dot <= 0)
                return "";
            string reference = at.Substring(0, dot);
            Part? part = Editor.FindPart(reference);
            if (part == null && reference != "P" && reference != "F")
                return "";
            StringBuilder sb = new();
            sb.Append("Pin ").Append(at);
            ChipInfo? chip = part == null ? null : _options.Catalog?.Get(part.Type);
            if (chip != null && int.TryParse(at.AsSpan(dot + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int number))
            {
                for (int i = 0; i < chip.Pins.Count; i++)
                {
                    if (chip.Pins[i].Number == number)
                        sb.Append(": ").Append(chip.Pins[i].Name).Append(" (").Append(chip.Pins[i].Role).Append(')');
                }
            }
            sb.Append('\n');
            Net? net = Editor.NetOf(at);
            if (net == null)
                sb.Append("Signal: none\n");
            else
            {
                sb.Append("Signal: ").Append(net.Name).Append('\n');
                PinReference? r = FindPin(net, at);
                if (r != null)
                {
                    sb.Append("Reading: ").Append(ConfidenceText(r.Confidence)).Append('\n');
                    if (!string.IsNullOrEmpty(r.Note))
                        sb.Append("Note: ").Append(r.Note).Append('\n');
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// The members of a bus, as their keys (BD15_0), for the "Bus members" panel.
        /// </summary>
        /// <param name="name">
        /// The bus.
        /// </param>
        /// <returns>
        /// The member keys, or an empty list for no such bus.
        /// </returns>
        public List<string> BusMembers(string name)
        {
            List<string> list = new();
            BusLine? bus = Editor.FindBus(name);
            for (int i = 0; bus != null && i < bus.Members.Count; i++)
            {
                Net? n = SheetLayout.FindMember(Netlist, bus.Members[i]);
                list.Add(n == null ? bus.Members[i].Net + " (not on the sheet)" : SheetLayout.NetKey(n));
            }
            return list;
        }

        private string DescribeBus(string name)
        {
            BusLine? bus = Editor.FindBus(name);
            if (bus == null)
                return "";
            StringBuilder sb = new();
            sb.Append("Bus ").Append(bus.Name).Append(" (drawing only: it does not change what is connected)\n");
            sb.Append("Signals:\n");
            List<string> members = BusMembers(name);
            for (int i = 0; i < members.Count; i++)
                sb.Append("  ").Append(members[i]).Append('\n');
            for (int i = 0; i < Drawing.BusProblems.Count; i++)
            {
                if (Drawing.BusProblems[i].StartsWith(bus.Name + ":", StringComparison.Ordinal))
                    sb.Append(Drawing.BusProblems[i]).Append('\n');
            }
            return sb.ToString();
        }

        private static PinReference? FindPin(Net net, string at)
        {
            for (int i = 0; i < net.Pins.Count; i++)
            {
                if (net.Pins[i].At == at)
                    return net.Pins[i];
            }
            return null;
        }

        private static string ConfidenceText(Confidence c) => c switch
        {
            Confidence.Read => "READ off the drawing",
            Confidence.Inferred => "INFERRED, not read",
            Confidence.Open => "OPEN, not known",
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, "a confidence value this program does not know"),
        };
    }
}
