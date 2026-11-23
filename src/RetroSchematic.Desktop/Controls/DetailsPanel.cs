//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Controls
{
    /// <summary>
    /// The Details panel on the right: what is picked, as cards with a header bar (icon, title,
    /// click to fold), label and value rows, badges and a pin table. It only lays out what the
    /// library hands it (<see cref="SignalInfo"/>, <see cref="PartInfo"/>); it works nothing out
    /// itself. Every colour comes from the theme through the styles in Styles/Details.axaml.
    /// </summary>
    public sealed class DetailsPanel : StackPanel
    {
        // which cards are folded, by title, so a fold stays while picking other things
        private readonly HashSet<string> _folded = new(StringComparer.Ordinal);

        /// <summary>
        /// Raised when a row of a pin table is clicked, with the pin (such as 13C.7).
        /// </summary>
        public event Action<string>? PinClicked;

        /// <summary>
        /// Raised when a signal name in a part's pin table is clicked, with the signal's key.
        /// </summary>
        public event Action<string>? SignalClicked;

        /// <summary>
        /// The titles of the cards shown now, in order (for the tests).
        /// </summary>
        public List<string> Sections { get; } = new();

        /// <summary>
        /// The pin of each pin-table row shown now, in order (for the tests).
        /// </summary>
        public List<string> PinRows { get; } = new();

        /// <summary>
        /// The text shown when the pick is not a signal or a part (a gate, a bus, a block).
        /// </summary>
        public string PlainText { get; private set; } = "";

        /// <summary>
        /// The key of the signal shown, or empty when the panel shows something else.
        /// </summary>
        public string SignalKey { get; private set; } = "";

        /// <summary>
        /// Shows a plain text in one card (gates, buses, blocks and the empty state).
        /// </summary>
        /// <param name="title">
        /// The card's title.
        /// </param>
        /// <param name="text">
        /// The text; empty lines separate paragraphs.
        /// </param>
        public void ShowText(string title, string text)
        {
            Clear();
            PlainText = text;
            StackPanel body = Body();
            SelectableTextBlock t = new() { Text = text.TrimEnd(), TextWrapping = TextWrapping.Wrap };
            t.Classes.Add("detail-value");
            body.Children.Add(t);
            Card("IconDescription", title, body);
        }

        /// <summary>
        /// Shows a signal: its name and level, what it does, its electrical facts, where it goes
        /// and its pins.
        /// </summary>
        /// <param name="s">
        /// The facts from the library.
        /// </param>
        public void ShowSignal(SignalInfo s)
        {
            Clear();
            SignalKey = SheetLayout.NetKey(s.Net);

            // the name, large, with its level
            StackPanel head = Body();
            WrapPanel nameLine = new() { Orientation = Orientation.Horizontal };
            TextBlock name = Text(s.Name, "detail-name");
            name.Margin = new Avalonia.Thickness(0, 0, 10, 0);
            nameLine.Children.Add(name);
            if (s.ActiveText != null)
                nameLine.Children.Add(Badge(s.ActiveText, "level"));
            head.Children.Add(nameLine);
            if (s.Net.Function != null && s.Net.Function.Count > 0)
                head.Children.Add(Row("Function", string.Join(", ", s.Net.Function)));
            Card("IconSignal", "Signal", head);

            // what it does, where that is written, how sure
            StackPanel desc = Body();
            if (s.Description == null)
            {
                desc.Children.Add(Text("Not described yet.", "detail-muted"));
            }
            else
            {
                desc.Children.Add(Text(s.Description, "detail-value"));
                WrapPanel src = new() { Orientation = Orientation.Horizontal };
                if (s.DescriptionConfidence is Confidence c)
                {
                    Border b = Badge(ConfidenceWord(c), ConfidenceWord(c));
                    b.Margin = new Avalonia.Thickness(0, 0, 8, 0);
                    src.Children.Add(b);
                }
                if (s.DescriptionSourcePlace != null)
                    src.Children.Add(Text(s.DescriptionSourcePlace, "detail-muted"));
                desc.Children.Add(src);
            }
            if (!string.IsNullOrEmpty(s.Net.Note))
                desc.Children.Add(Row("Notes", s.Net.Note!));
            Card("IconDescription", "Description", desc);

            // the electrical facts
            StackPanel el = Body();
            Grid rows = Rows();
            if (s.DriveBadge != null)
                AddRow(rows, "Drive", Badge(s.DriveBadge, "drive"));
            else
                AddRow(rows, "Drive", Text("not known", "detail-muted"));
            AddRow(rows, "Driven by", s.Drivers.Count == 0 ? Text("no driver on this level", "detail-muted") : Mono(DriverText(s)));
            AddRow(rows, "Loads", Text(s.Loads.ToString(CultureInfo.InvariantCulture), "detail-value"));
            if (s.Net.PullUp != null)
                AddRow(rows, "Pull-up", Text(s.Net.PullUp, "detail-value"));
            el.Children.Add(rows);
            if (s.DriverInputs.Count > 0)
                el.Children.Add(DriverInputRows(s.DriverInputs));
            if (s.MixedDrive)
                el.Children.Add(Warning("Mixed drivers: outputs of different kinds drive this line."));
            if (s.Conflict != null)
                el.Children.Add(Warning("Conflict: " + s.Conflict));
            Card("IconElectrical", "Electrical", el);

            // where else it goes
            List<string> places = s.Places;
            if (places.Count > 0)
            {
                StackPanel w = Body();
                WrapPanel chips = new() { Orientation = Orientation.Horizontal };
                for (int i = 0; i < places.Count; i++)
                {
                    Border chip = new() { Child = Text(places[i], "") };
                    chip.Classes.Add("place-chip");
                    chips.Children.Add(chip);
                }
                w.Children.Add(chips);
                Card("IconWhere", "Where it goes", w);
            }

            // the pin table
            StackPanel pins = Body();
            pins.Spacing = 0;
            pins.Children.Add(PinHead("Pin", "Chip", "Pin name", "Dir"));
            for (int i = 0; i < s.Pins.Count; i++)
            {
                SignalPin p = s.Pins[i];
                pins.Children.Add(PinRow(i, p.At, p.ChipType, p.PinName, p.Direction, p.Drives));
            }
            Card("IconPinTable", "Pins (" + s.Pins.Count.ToString(CultureInfo.InvariantCulture) + ")", pins);
        }

        /// <summary>
        /// Shows a part: its board position, type and function, where it was read, and every pin
        /// with the signal on it.
        /// </summary>
        /// <param name="p">
        /// The facts from the library.
        /// </param>
        public void ShowPart(PartInfo p)
        {
            Clear();
            StackPanel head = Body();
            WrapPanel nameLine = new() { Orientation = Orientation.Horizontal };
            TextBlock name = Text(p.Ref + "  " + p.Type, "detail-name");
            name.Margin = new Avalonia.Thickness(0, 0, 10, 0);
            nameLine.Children.Add(name);
            nameLine.Children.Add(Badge(ConfidenceWord(p.Confidence), ConfidenceWord(p.Confidence)));
            head.Children.Add(nameLine);
            if (p.Function != null)
                head.Children.Add(Text(p.Function, "detail-value"));
            Grid rows = Rows();
            if (p.CatalogueTitle != null)
                AddRow(rows, "Catalogue", Text(p.CatalogueTitle, "detail-value"));
            else
                AddRow(rows, "Catalogue", Text("no chip file", "detail-muted"));
            if (p.Source != null)
                AddRow(rows, "Read from", Text(p.Source, "detail-value"));
            if (p.Note != null)
                AddRow(rows, "Notes", Text(p.Note, "detail-value"));
            head.Children.Add(rows);
            Card("IconPlaceChip", "Part", head);

            StackPanel pins = Body();
            pins.Spacing = 0;
            pins.Children.Add(PartHead());
            for (int i = 0; i < p.Pins.Count; i++)
            {
                PartPinRow r = p.Pins[i];
                pins.Children.Add(PartRow(i, r));
            }
            Card("IconPinTable", "Pins (" + p.Pins.Count.ToString(CultureInfo.InvariantCulture) + ")", pins);
        }

        /// <summary>
        /// Shows the board when nothing is picked: the sheet, the board facts, connectors,
        /// original pages, photos and switches, and a button into the Board window.
        /// </summary>
        /// <param name="n">
        /// The whole sheet (its top level).
        /// </param>
        /// <param name="openBoard">
        /// Opens the Board window.
        /// </param>
        public void ShowBoard(Netlist n, Action openBoard)
        {
            Clear();
            StackPanel head = Body();
            head.Children.Add(Text(n.Sheet.Id + "  " + n.Sheet.Title, "detail-name"));
            head.Children.Add(Text("Nothing picked. Click a part, gate, signal or pin on the drawing.", "detail-muted"));
            Grid rows = Rows();
            BoardSection? b = n.Board;
            AddRow(rows, "Machine", Value(b?.NdSystem));
            AddRow(rows, "Title", Value(b?.Title));
            AddRow(rows, "Card no.", Value(b?.Number));
            AddRow(rows, "Part no.", Value(b?.PartNumber));
            AddRow(rows, "Print ver.", Value(b?.PrintVersion));
            AddRow(rows, "ECO ver.", Value(b?.EcoVersion));
            BoardSize? size = b?.Specifications?.Size;
            AddRow(rows, "Size", Value(size == null ? null
                : size.WidthMm.ToString(CultureInfo.InvariantCulture) + " x " + size.HeightMm.ToString(CultureInfo.InvariantCulture) + " mm"));
            AddRow(rows, "Layers", Value(b?.Specifications?.Layers?.ToString(CultureInfo.InvariantCulture)));
            if (b?.SourceUrl != null)
                AddRow(rows, "From", Value("ND PCB catalogue, " + b.SourceUrl));
            System.Text.StringBuilder conn = new();
            for (int i = 0; n.Connectors != null && i < n.Connectors.Count; i++)
            {
                ConnectorUse c = n.Connectors[i];
                if (conn.Length > 0)
                    conn.Append('\n');
                conn.Append(ConnectorPins.Name(c.Ref)).Append(c.Used == false ? ": not used" : c.Use == null ? "" : ": " + c.Use);
            }
            AddRow(rows, "Connectors", Value(conn.Length == 0 ? null : conn.ToString()));
            AddRow(rows, "Originals", Value(Count(n.Sources?.Count, "page", "pages")));
            int front = 0, back = 0;
            for (int i = 0; n.Photos != null && i < n.Photos.Count; i++)
            {
                if (n.Photos[i].Side == BoardSide.Component)
                    front++;
                else
                    back++;
            }
            AddRow(rows, "Photos", Value(n.Photos == null ? null
                : front.ToString(CultureInfo.InvariantCulture) + " component side, " + back.ToString(CultureInfo.InvariantCulture) + " solder side"));
            System.Text.StringBuilder sw = new();
            for (int i = 0; n.Switches != null && i < n.Switches.Count; i++)
            {
                if (sw.Length > 0)
                    sw.Append('\n');
                sw.Append(n.Switches[i].Ref).Append(n.Switches[i].Function == null ? "" : ": " + n.Switches[i].Function);
            }
            AddRow(rows, "Switches", Value(sw.Length == 0 ? null : sw.ToString()));
            head.Children.Add(rows);
            Button edit = new() { Content = "Board info...", Margin = new Avalonia.Thickness(0, 6, 0, 0) };
            edit.Classes.Add("fluent-button");
            edit.Click += (s, e) => openBoard();
            head.Children.Add(edit);
            Card("IconBoardPositions", "Board", head);
        }

        /// <summary>
        /// Shows a switch block: what it sets, what "on" means, the usual setting, and one row
        /// per lever with its signal, bit, how sure the reading is and its setting.
        /// </summary>
        /// <param name="s">
        /// The switch block.
        /// </param>
        public void ShowSwitch(SwitchBlock s)
        {
            Clear();
            StackPanel head = Body();
            head.Children.Add(Text(s.Ref + "  switch, " + s.LeverCount.ToString(CultureInfo.InvariantCulture) + " levers", "detail-name"));
            if (s.Function != null)
                head.Children.Add(Text(s.Function, "detail-value"));
            Grid rows = Rows();
            string on = s.OnMeans == null || s.OnMeans == "open" ? "not known" : "a lever that is on gives " + s.OnMeans;
            if (s.OnMeansConfidence != null)
                on += " (" + ConfidenceWord(s.OnMeansConfidence.Value) + ")";
            AddRow(rows, "On means", Text(on, "detail-value"));
            if (s.Typical != null)
                AddRow(rows, "Typical", Text(s.Typical, "detail-value"));
            if (s.Source != null)
                AddRow(rows, "Read from", Text((s.Source.Document ?? s.Source.Kind.ToString()) + (s.Source.Place == null ? "" : ", " + s.Source.Place), "detail-value"));
            // further sources, such as the ND PCB catalogue's switch text, word for word
            for (int i = 0; s.MoreSources != null && i < s.MoreSources.Count; i++)
            {
                DescriptionSource m = s.MoreSources[i];
                string said = (m.Quote == null ? "" : "\"" + m.Quote + "\"\n") + (m.Document ?? m.Kind.ToString()) + (m.Place == null ? "" : ", " + m.Place) + (m.Note == null ? "" : "\n" + m.Note);
                AddRow(rows, "Also", Text(said, "detail-value"));
            }
            if (s.Note != null)
                AddRow(rows, "Notes", Text(s.Note, "detail-value"));
            head.Children.Add(rows);
            Card("IconSettings", "Switch", head);

            StackPanel levers = Body();
            for (int i = 0; i < s.Levers.Count; i++)
            {
                SwitchLever l = s.Levers[i];
                Grid g = Rows();
                WrapPanel line = new() { Orientation = Orientation.Horizontal };
                TextBlock what = Text((l.Signal ?? "?") + "  ->  " + (l.Bit ?? "?") + "   ", "detail-value");
                line.Children.Add(what);
                line.Children.Add(Badge(ConfidenceWord(l.Confidence), ConfidenceWord(l.Confidence)));
                AddRow(g, "Lever " + l.Number.ToString(CultureInfo.InvariantCulture), line);
                AddRow(g, "Setting", Text(l.Setting == null || l.Setting == "open" ? "not known" : l.Setting, l.Setting == null || l.Setting == "open" ? "detail-muted" : "detail-value"));
                if (l.Note != null)
                    AddRow(g, "", Text(l.Note, "detail-muted"));
                levers.Children.Add(g);
            }
            Card("IconPinTable", "Levers (" + s.Levers.Count.ToString(CultureInfo.InvariantCulture) + ")", levers);
        }

        // a board value, or "not known" in the muted colour
        private static TextBlock Value(string? v) => v == null ? Text("not known", "detail-muted") : Text(v, "detail-value");

        private static string? Count(int? n, string one, string many)
            => n == null ? null : n.Value.ToString(CultureInfo.InvariantCulture) + " " + (n.Value == 1 ? one : many);

        /// <summary>
        /// Shows a connector, a bus or a block: its facts and the signals on it, each row a link
        /// that traces its signal.
        /// </summary>
        /// <param name="l">
        /// The list from the library.
        /// </param>
        public void ShowList(SignalList l)
        {
            Clear();
            StackPanel head = Body();
            head.Children.Add(Text(l.Title, "detail-name"));
            if (l.Facts.Count > 0)
            {
                Grid rows = Rows();
                for (int i = 0; i < l.Facts.Count; i++)
                    AddRow(rows, l.Facts[i].Key, Text(l.Facts[i].Value, "detail-value"));
                head.Children.Add(rows);
            }
            string icon = l.Kind == "Bus" ? "IconBus" : l.Kind == "Block" ? "IconMakeBlock" : "IconPinTable";
            Card(icon, l.Kind, head);

            StackPanel list = Body();
            list.Spacing = 0;
            Grid h = ListColumns();
            h.Classes.Add("pin-head");
            h.Margin = new Avalonia.Thickness(6, 0, 4, 4);
            for (int i = 0; i < 3; i++)
            {
                TextBlock t = new() { Text = l.Columns[i] };
                Grid.SetColumn(t, i);
                h.Children.Add(t);
            }
            list.Children.Add(h);
            for (int i = 0; i < l.Rows.Count; i++)
                list.Children.Add(ListRow(i, l.Rows[i]));
            Card("IconSignal", "Signals (" + l.Rows.Count.ToString(CultureInfo.InvariantCulture) + ")", list);
        }

        // a list row: the row's own name (monospace), the signal, the extra word
        private static Grid ListColumns()
        {
            Grid g = new();
            g.ColumnDefinitions.Add(new ColumnDefinition(new Avalonia.Controls.GridLength(62)));
            g.ColumnDefinitions.Add(new ColumnDefinition(1, Avalonia.Controls.GridUnitType.Star));
            g.ColumnDefinitions.Add(new ColumnDefinition(new Avalonia.Controls.GridLength(96)));
            return g;
        }

        // the whole row is the link: a click traces the row's signal
        private Button ListRow(int index, SignalLink r)
        {
            Grid g = ListColumns();
            TextBlock label = new() { Text = r.Label };
            label.Classes.Add("detail-mono");
            g.Children.Add(label);
            Cell(g, r.NetName.Length > 0 ? r.NetName : "-", 1, "");
            Cell(g, r.Extra, 2, "dir");
            Button row = new() { Content = g, Tag = r.NetKey, IsEnabled = r.NetKey.Length > 0 };
            row.Classes.Add("pin-row");
            if (index % 2 == 1)
                row.Classes.Add("alt");
            row.Click += (s, e) => SignalClicked?.Invoke(r.NetKey);
            PinRows.Add(r.Label);
            return row;
        }

        /// <summary>
        /// Every text in the panel, one per line, in order (for the tests and for copying).
        /// </summary>
        /// <returns>
        /// The texts.
        /// </returns>
        public string AllText()
        {
            System.Text.StringBuilder sb = new();
            Collect(this, sb);
            return sb.ToString();
        }

        private static void Collect(Control c, System.Text.StringBuilder sb)
        {
            if (c is TextBlock t && !string.IsNullOrEmpty(t.Text))
                sb.Append(t.Text).Append('\n');
            if (c is ContentControl cc && cc.Content is Control inner)
                Collect(inner, sb);
            if (c is Decorator dec && dec.Child != null)
                Collect(dec.Child, sb);
            if (c is Panel p)
            {
                for (int i = 0; i < p.Children.Count; i++)
                    Collect(p.Children[i], sb);
            }
        }

        /// <summary>
        /// Folds or unfolds a card by its title, as a click on its header does.
        /// </summary>
        /// <param name="title">
        /// The card's title.
        /// </param>
        /// <returns>
        /// True when the card is now folded.
        /// </returns>
        public bool ToggleFold(string title)
        {
            string key = FoldKey(title);
            if (!_folded.Remove(key))
                _folded.Add(key);
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] is Border card && card.Tag as string == key && card.Child is StackPanel inner && inner.Children.Count == 2)
                {
                    bool folded = _folded.Contains(key);
                    inner.Children[1].IsVisible = !folded;
                    if (inner.Children[0] is Button h && h.Content is DockPanel d && d.Children[0] is Path chevron)
                        chevron.Data = Geometry(folded ? "IconChevronRight" : "IconChevronDown");
                }
            }
            return _folded.Contains(key);
        }

        private void Clear()
        {
            Children.Clear();
            Sections.Clear();
            PinRows.Clear();
            PlainText = "";
            SignalKey = "";
            Spacing = 0;
        }

        // the fold is kept per kind of card ("Pins (12)" and "Pins (3)" are the same card)
        private static string FoldKey(string title)
        {
            int paren = title.IndexOf(" (", StringComparison.Ordinal);
            return paren < 0 ? title : title.Substring(0, paren);
        }

        private void Card(string icon, string title, Control body)
        {
            string key = FoldKey(title);
            bool folded = _folded.Contains(key);
            DockPanel bar = new();
            Path chevron = new() { Data = Geometry(folded ? "IconChevronRight" : "IconChevronDown"), Margin = new Avalonia.Thickness(0, 0, 8, 0) };
            chevron.Classes.Add("detail-chevron");
            DockPanel.SetDock(chevron, Dock.Left);
            Path glyph = new() { Data = Geometry(icon), Margin = new Avalonia.Thickness(0, 0, 8, 0) };
            glyph.Classes.Add("detail-icon");
            DockPanel.SetDock(glyph, Dock.Left);
            bar.Children.Add(chevron);
            bar.Children.Add(glyph);
            bar.Children.Add(Text(title, "detail-title"));
            Button header = new() { Content = bar };
            header.Classes.Add("detail-header");
            header.Click += (s, e) => ToggleFold(title);
            body.IsVisible = !folded;
            StackPanel inner = new();
            inner.Children.Add(header);
            inner.Children.Add(body);
            Border card = new() { Child = inner, Tag = key };
            card.Classes.Add("detail-card");
            Children.Add(card);
            Sections.Add(title);
        }

        private static Geometry? Geometry(string key)
        {
            return Avalonia.Application.Current != null && Avalonia.Application.Current.TryGetResource(key, null, out object? g) ? g as Geometry : null;
        }

        private static StackPanel Body()
        {
            StackPanel p = new();
            p.Classes.Add("detail-body");
            return p;
        }

        private static TextBlock Text(string text, string cls)
        {
            TextBlock t = new() { Text = text, TextWrapping = TextWrapping.Wrap };
            if (cls.Length > 0)
                t.Classes.Add(cls);
            return t;
        }

        private static TextBlock Mono(string text)
        {
            TextBlock t = Text(text, "detail-value");
            t.Classes.Add("detail-mono");
            return t;
        }

        private static Border Badge(string text, string kind)
        {
            Border b = new() { Child = new TextBlock { Text = text } };
            b.Classes.Add("badge");
            b.Classes.Add(kind);
            return b;
        }

        private static Border Warning(string text)
        {
            Border b = new() { Child = new TextBlock { Text = text } };
            b.Classes.Add("warning-box");
            return b;
        }

        private static Grid Rows()
        {
            Grid g = new() { RowSpacing = 5 };
            g.ColumnDefinitions.Add(new ColumnDefinition(new Avalonia.Controls.GridLength(78)));
            g.ColumnDefinitions.Add(new ColumnDefinition(1, Avalonia.Controls.GridUnitType.Star));
            return g;
        }

        private static StackPanel Row(string label, string value)
        {
            Grid g = Rows();
            AddRow(g, label, Text(value, "detail-value"));
            StackPanel p = new();
            p.Children.Add(g);
            return p;
        }

        private static void AddRow(Grid g, string label, Control value)
        {
            int row = g.RowDefinitions.Count;
            g.RowDefinitions.Add(new RowDefinition(Avalonia.Controls.GridLength.Auto));
            TextBlock l = Text(label, "detail-label");
            Grid.SetRow(l, row);
            Grid.SetRow(value, row);
            Grid.SetColumn(value, 1);
            g.Children.Add(l);
            g.Children.Add(value);
        }

        // "follow inputs": the driving gate's inputs as rows; a click traces that input's signal,
        // so a backwards trace steps through the gate body
        private StackPanel DriverInputRows(List<DriverInput> inputs)
        {
            StackPanel list = new() { Spacing = 0 };
            list.Children.Add(Text("Inputs of the driving gate (click to follow)", "detail-label"));
            for (int i = 0; i < inputs.Count; i++)
            {
                DriverInput d = inputs[i];
                Grid g = new() { ColumnDefinitions = new ColumnDefinitions("62,*") };
                TextBlock at = Text(d.At, "");
                at.Classes.Add("detail-mono");
                g.Children.Add(at);
                TextBlock name = Text(d.Name.Length == 0 ? "not connected" : d.Name, "");
                Grid.SetColumn(name, 1);
                g.Children.Add(name);
                Button row = new() { Content = g, Tag = d.NetKey };
                // looks like a pin-table row (Styles/Details.axaml), but is not one
                row.Classes.Add("driver-input");
                if (i % 2 == 1)
                    row.Classes.Add("alt");
                if (d.NetKey.Length > 0)
                {
                    string key = d.NetKey;
                    row.Click += (sender, e) => SignalClicked?.Invoke(key);
                }
                else
                {
                    row.IsEnabled = false;
                }
                list.Children.Add(row);
            }
            return list;
        }

        private static string DriverText(SignalInfo s)
        {
            string text = "";
            for (int i = 0; i < s.Drivers.Count; i++)
            {
                SignalPin d = s.Drivers[i];
                text += (i == 0 ? "" : "\n") + d.At + (d.ChipType.Length > 0 ? "  " + d.ChipType + " " + d.PinName : "");
            }
            return text;
        }

        private static string ConfidenceWord(Confidence c) => c == Confidence.Read ? "read" : c == Confidence.Inferred ? "inferred" : "open";

        // the pin table's columns: pin ref (monospace), two text columns, a narrow last one
        private static Grid Columns()
        {
            Grid g = new();
            g.ColumnDefinitions.Add(new ColumnDefinition(new Avalonia.Controls.GridLength(62)));
            g.ColumnDefinitions.Add(new ColumnDefinition(1, Avalonia.Controls.GridUnitType.Star));
            g.ColumnDefinitions.Add(new ColumnDefinition(1, Avalonia.Controls.GridUnitType.Star));
            g.ColumnDefinitions.Add(new ColumnDefinition(new Avalonia.Controls.GridLength(70)));
            return g;
        }

        private static Grid PinHead(string a, string b, string c, string d)
        {
            Grid g = Columns();
            g.Classes.Add("pin-head");
            g.Margin = new Avalonia.Thickness(6, 0, 4, 4);
            string[] texts = { a, b, c, d };
            for (int i = 0; i < texts.Length; i++)
            {
                TextBlock t = new() { Text = texts[i] };
                Grid.SetColumn(t, i);
                g.Children.Add(t);
            }
            return g;
        }

        // a part's table: pin number, pin name, direction, signal (the widest)
        private static Grid PartColumns()
        {
            Grid g = new();
            g.ColumnDefinitions.Add(new ColumnDefinition(new Avalonia.Controls.GridLength(34)));
            g.ColumnDefinitions.Add(new ColumnDefinition(new Avalonia.Controls.GridLength(64)));
            g.ColumnDefinitions.Add(new ColumnDefinition(new Avalonia.Controls.GridLength(54)));
            g.ColumnDefinitions.Add(new ColumnDefinition(1, Avalonia.Controls.GridUnitType.Star));
            return g;
        }

        private static Grid PartHead()
        {
            Grid g = PartColumns();
            g.Classes.Add("pin-head");
            g.Margin = new Avalonia.Thickness(6, 0, 4, 4);
            string[] texts = { "Pin", "Name", "Dir", "Signal" };
            for (int i = 0; i < texts.Length; i++)
            {
                TextBlock t = new() { Text = texts[i] };
                Grid.SetColumn(t, i);
                g.Children.Add(t);
            }
            return g;
        }

        // a signal's table: pin ref (monospace), chip, pin name, direction; the driver marked
        private Button PinRow(int index, string at, string chip, string pinName, string direction, bool driver)
        {
            Grid g = Columns();
            TextBlock pin = new() { Text = at };
            pin.Classes.Add("detail-mono");
            g.Children.Add(pin);
            Cell(g, chip, 1, "");
            Cell(g, pinName, 2, "");
            Cell(g, driver ? direction + " (driver)" : direction, 3, "dir");
            return Finish(index, at, g, driver);
        }

        // a part's table: pin number, pin name, direction, and the signal as a link that traces it
        private Button PartRow(int index, PartPinRow r)
        {
            Grid g = PartColumns();
            TextBlock pin = new() { Text = r.Number.ToString(CultureInfo.InvariantCulture) };
            pin.Classes.Add("detail-mono");
            g.Children.Add(pin);
            Cell(g, r.Name, 1, "");
            Cell(g, r.Direction, 2, "dir");
            Control signal;
            if (r.NetKey.Length > 0)
            {
                Button link = new() { Content = new TextBlock { Text = r.NetName }, Tag = r.NetKey };
                link.Classes.Add("signal-link");
                link.Click += (s, e) =>
                {
                    e.Handled = true;
                    SignalClicked?.Invoke(r.NetKey);
                };
                signal = link;
            }
            else
            {
                signal = new TextBlock { Text = "-" };
            }
            Grid.SetColumn(signal, 3);
            g.Children.Add(signal);
            Button row = Finish(index, r.At, g, false);
            // a pin with a signal: the row traces it, as a click on the wire does
            if (r.NetKey.Length > 0)
            {
                row.Click += (s, e) =>
                {
                    e.Handled = true;
                    SignalClicked?.Invoke(r.NetKey);
                };
            }
            return row;
        }

        private static void Cell(Grid g, string text, int column, string cls)
        {
            TextBlock t = new() { Text = text, Margin = new Avalonia.Thickness(0, 0, 4, 0) };
            if (cls.Length > 0)
                t.Classes.Add(cls);
            Grid.SetColumn(t, column);
            g.Children.Add(t);
        }

        private Button Finish(int index, string at, Grid g, bool driver)
        {
            Button row = new() { Content = g, Tag = at };
            row.Classes.Add("pin-row");
            if (index % 2 == 1)
                row.Classes.Add("alt");
            if (driver)
                row.Classes.Add("driver");
            row.Click += (s, e) =>
            {
                if (!e.Handled)
                    PinClicked?.Invoke(at);
            };
            PinRows.Add(at);
            return row;
        }
    }
}
