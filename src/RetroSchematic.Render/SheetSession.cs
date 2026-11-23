//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// One open sheet and what the user is looking at: the symbol style, the drawing laid out for
    /// it, and the picked part, gate or signal. The desktop window only shows this object; every
    /// action is a method here, so the command-line tool and tests can do the same things.
    /// </summary>
    public sealed class SheetSession
    {
        private readonly string? _catalogFolder;

        /// <summary>
        /// Called after the drawing was laid out again (new sheet or new symbol style).
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
        /// Opens a sheet file. <paramref name="catalogFolder"/> is the catalogue's folder (the one
        /// holding <c>chips/</c>), used to show what a picked chip is; null leaves that out.
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
        {
            SheetPath = sheetPath;
            Netlist = NetlistReader.ReadFile(sheetPath);
            Style = style;
            _catalogFolder = catalogFolder;
            Drawing = SheetLayout.Build(Netlist, style);
        }

        /// <summary>
        /// The sheet file.
        /// </summary>
        public string SheetPath { get; }

        /// <summary>
        /// The sheet's contents.
        /// </summary>
        public Netlist Netlist { get; }

        /// <summary>
        /// The symbol set in use.
        /// </summary>
        public SymbolStyle Style { get; private set; }

        /// <summary>
        /// The laid-out drawing for <see cref="Style"/>.
        /// </summary>
        public SchematicDrawing Drawing { get; private set; }

        /// <summary>
        /// The picked item ("part:11D", "gate:11D/1", "net:BD0"), or empty.
        /// </summary>
        public string Selected { get; private set; } = "";

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
            Drawing = SheetLayout.Build(Netlist, style);
            DrawingChanged?.Invoke(this);
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
        /// True if an item belongs to the picked thing. Picking a signal lights every pin label of
        /// that signal; picking a gate lights the gate; picking a part lights the part and its gates.
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
            {
                string part = Selected.Substring(5);
                return item.Owner.Length > 5 + part.Length && item.Owner[5 + part.Length] == '/'
                    && string.CompareOrdinal(item.Owner, 5, part, 0, part.Length) == 0;
            }
            return false;
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
            return "";
        }

        private string DescribePart(string reference)
        {
            StringBuilder sb = new();
            for (int i = 0; i < Netlist.Parts.Count; i++)
            {
                Part p = Netlist.Parts[i];
                if (p.Ref != reference)
                    continue;
                sb.Append("Part ").Append(p.Ref).Append(": ").Append(p.Type).Append('\n');
                if (!string.IsNullOrEmpty(p.Function))
                    sb.Append(p.Function).Append('\n');
                sb.Append("Reading: ").Append(ConfidenceText(p.Confidence)).Append('\n');
                if (!string.IsNullOrEmpty(p.Note))
                    sb.Append("Note: ").Append(p.Note).Append('\n');
                AppendChip(sb, p.Type);
            }
            return sb.ToString();
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
            for (int i = 0; i < Netlist.Nets.Count; i++)
            {
                Net n = Netlist.Nets[i];
                if (n.Name != name)
                    continue;
                sb.Append("Signal ").Append(n.Name);
                if (n.Active != ActiveLevel.None)
                    sb.Append(n.Active == ActiveLevel.High ? " (active high, subscript 1)" : " (active low, subscript 0)");
                sb.Append('\n');
                if (n.Drive.HasValue)
                    sb.Append("Line type: ").Append(n.Drive.Value.ToString()).Append('\n');
                sb.Append("Pins:\n");
                for (int j = 0; j < n.Pins.Count; j++)
                    sb.Append("  ").Append(n.Pins[j].At).Append("  (").Append(ConfidenceText(n.Pins[j].Confidence)).Append(")\n");
                if (!string.IsNullOrEmpty(n.Note))
                    sb.Append("Note: ").Append(n.Note).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// The chip file's title and source, when the catalogue has the chip.
        /// </summary>
        /// <param name="sb">
        /// The description being built.
        /// </param>
        /// <param name="type">
        /// The chip type, such as 7442.
        /// </param>
        private void AppendChip(StringBuilder sb, string type)
        {
            if (_catalogFolder == null)
                return;
            string path = Path.Combine(_catalogFolder, "chips", type + ".json");
            if (!File.Exists(path))
            {
                sb.Append("Catalogue: no chip file for ").Append(type).Append('\n');
                return;
            }
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = doc.RootElement;
            if (root.TryGetProperty("title", out JsonElement title))
                sb.Append("Catalogue: ").Append(title.GetString()).Append('\n');
            if (root.TryGetProperty("functions", out JsonElement fs) && fs.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < fs.GetArrayLength(); i++)
                {
                    if (fs[i].TryGetProperty("formula", out JsonElement f) && f.ValueKind == JsonValueKind.String)
                        sb.Append("  ").Append(f.GetString()).Append('\n');
                }
            }
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
