//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// The lookup tools on the open sheet: what the window's side panel, search box, trace tool,
    /// Details panel and "Save as SVG" give, in words. Every one is allowed read-only and changes
    /// nothing in the sheet.
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>
        /// The sheet's title block, file stamp and size.
        /// </summary>
        /// <returns>
        /// One fact per line.
        /// </returns>
        [McpServerTool(Name = "get_sheet_info"), Description("The open sheet in short: number, title, machine and bus, source document, format version, file stamp (revision and last save time, UTC), the file it was opened from, edit mode, unsaved changes, the level shown (block path) and the card page the tools work on, and how many parts, gates, signals, blocks, pages, sources and photos it has. Allowed read-only.")]
        public string GetSheetInfo()
        {
            SheetEditor e = Open();
            Netlist r = e.Root;
            StringBuilder sb = new();
            CultureInfo c = CultureInfo.InvariantCulture;
            sb.Append("Sheet ").Append(r.Sheet.Id).Append(": ").Append(r.Sheet.Title).Append('\n');
            sb.Append("Machine: ").Append(r.Sheet.Machine ?? "not given").Append("; bus: ").Append(SheetSession.BusText(r.Sheet.Bus ?? MachineBus.Unknown)).Append('\n');
            sb.Append("Source: ").Append(r.Sheet.Source.Document);
            if (r.Sheet.Source.PdfPage != null)
                sb.Append(", page ").Append(r.Sheet.Source.PdfPage.Value.ToString(c));
            sb.Append('\n');
            sb.Append("Format version ").Append(r.FormatVersion.ToString(c));
            sb.Append(r.File == null ? "; never saved by the editor (no file stamp)" : "; revision " + r.File.Revision.ToString(c) + ", saved " + r.File.Modified);
            sb.Append('\n');
            sb.Append("File: ").Append(_path ?? "not saved yet").Append('\n');
            sb.Append(e.ReadOnly ? "Read-only" : "Edit mode").Append(e.Modified ? ", unsaved changes" : ", no unsaved changes").Append('\n');
            sb.Append("Level: ").Append(e.LevelPath.Count == 0 ? "top" : string.Join("/", e.LevelPath)).Append('\n');
            if (CardPages.HasPages(r))
                sb.Append("Page the tools work on: ").Append(CardPages.PageOrFirst(r, e.Page)).Append('\n');
            sb.Append(string.Format(c, "Top level: {0} parts, {1} gates, {2} signals, {3} instances. {4} blocks, {5} pages, {6} source pages, {7} photos.\n",
                r.Parts.Count, r.Gates.Count, r.Nets.Count, r.Instances?.Count ?? 0, r.Blocks?.Count ?? 0, r.Pages?.Count ?? 0, r.Sources?.Count ?? 0, r.Photos?.Count ?? 0));
            if (!string.IsNullOrWhiteSpace(r.Sheet.Note))
                sb.Append("Note: ").Append(r.Sheet.Note).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Describes one item as the side panel does.
        /// </summary>
        /// <param name="item">
        /// The item: part:13D, gate:13D/6, net:BD3_0, pin:13D.7, bus:NAME, inst:U1, conn:P, or a
        /// plain name.
        /// </param>
        /// <returns>
        /// The description, one fact per line.
        /// </returns>
        [McpServerTool(Name = "describe"), Description("Everything the side panel shows for one item on the current level: a part (pins, signals, catalogue formulas), gate, signal (description, drive, pins), pin (chip pin name and role, or the backplane signal for a connector pin), bus, instance (block and ports) or connector. Write it as part:13D, gate:13D/6, net:BD3_0, pin:13D.7, bus:DATA, inst:U1 or conn:P; a plain name is tried as a part, then a pin (it has a dot), then a signal. Allowed read-only.")]
        public string DescribeItem([Description("The item, e.g. part:13D, net:IOXE_0, pin:13D.7, or a plain 13D")] string item)
        {
            SheetSession s = LookupSession();
            string owner = OwnerOf(s.Editor, item.Trim());
            s.Select(owner);
            string text = s.Describe();
            if (text.Length == 0)
                throw new McpException("There is no " + item + " on this level.");
            return text;
        }

        /// <summary>
        /// Finds parts, signals and pins by part of their name.
        /// </summary>
        /// <param name="text">
        /// What to look for.
        /// </param>
        /// <returns>
        /// One item per line.
        /// </returns>
        [McpServerTool(Name = "search"), Description("Find, like the window's search box, every part (by board position or type), signal (any name style: /NAME, ~NAME, NAME_0, NAME) and pin (text with a dot: 13D.7) on the current level whose name holds the text; case does not matter. Each line is an item to give to describe. Allowed read-only.")]
        public string Search([Description("What to look for, e.g. 74LS240, IOXE or 13D.")] string text)
        {
            List<string> found = LookupSession().Search(text);
            if (found.Count == 0)
                return "Nothing on this level holds \"" + text + "\".";
            StringBuilder sb = new();
            for (int i = 0; i < found.Count; i++)
                sb.Append(found[i]).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Traces one signal as the window's trace tool does.
        /// </summary>
        /// <param name="net">
        /// The signal's key or name.
        /// </param>
        /// <returns>
        /// The pins, the chips and gates it joins, the instance ports it goes through and the
        /// levels it reaches.
        /// </returns>
        [McpServerTool(Name = "trace_signal"), Description("Trace a whole signal, as the window's trace tool lights it up: its pins in file order, the chips and gates it joins, the bus line it runs along, the instance ports it goes through into blocks, and every level it reaches through block ports (U2/BD3). Allowed read-only.")]
        public string TraceSignal([Description("Signal key (IOXE_0) or a name only one signal has")] string net)
        {
            SheetSession s = LookupSession();
            Net n = s.Editor.FindNetByKey(net) ?? s.Editor.FindNet(net) ?? throw new McpException("There is no signal " + net + " on this level.");
            s.Trace(SheetLayout.NetKey(n));
            StringBuilder sb = new();
            sb.Append("Signal ").Append(s.NameOf(n)).Append(" (key ").Append(SheetLayout.NetKey(n)).Append(")\n");
            sb.Append("Pins: ").Append(string.Join(" ", s.TracedPins())).Append('\n');
            List<string> owners = s.TracedOwners();
            AppendOwners(sb, "Chips", owners, "part:");
            AppendOwners(sb, "Gates", owners, "gate:");
            AppendOwners(sb, "Bus", owners, "bus:");
            AppendOwners(sb, "Instance ports", owners, "port:");
            List<string> levels = s.TracedLevels();
            if (levels.Count > 0)
                sb.Append("Levels reached: ").Append(string.Join(", ", levels)).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// A part's facts and every pin with its signal, as the Details panel shows them.
        /// </summary>
        /// <param name="reference">
        /// The part.
        /// </param>
        /// <returns>
        /// One fact per line, then one pin per line.
        /// </returns>
        [McpServerTool(Name = "get_part_info"), Description("A part on the current level as the Details panel shows it: type, what it does, the catalogue title, how sure the reading is, note, where it was read, passive kind, value and tolerance, the chip it serves, then every pin (number, name, direction, signal; from the chip file, else the pins that are on a signal). Allowed read-only.")]
        public string GetPartInfo([Description("Board position or name, e.g. 13D or R1")] string reference)
        {
            SheetSession s = LookupSession();
            PartInfo info = s.PartInfoOf(reference) ?? throw new McpException("There is no part " + reference + " on this level.");
            StringBuilder sb = new();
            sb.Append("Part ").Append(info.Ref).Append(": ").Append(info.Type).Append('\n');
            Line(sb, "Does", info.Function);
            Line(sb, "Catalogue", info.CatalogueTitle ?? "no chip file for " + info.Type);
            Line(sb, "Reading", info.Confidence.ToString().ToLowerInvariant());
            Line(sb, "Note", info.Note);
            Line(sb, "Source", info.Source);
            Line(sb, "Kind", info.Kind);
            Line(sb, "Value", info.Value == null ? null : info.Value + (info.ValueConfidence == null ? "" : " (" + info.ValueConfidence.Value.ToString().ToLowerInvariant() + ")"));
            Line(sb, "Tolerance", info.Tolerance);
            Line(sb, "Serves", info.Serves);
            sb.Append("Pins:\n");
            for (int i = 0; i < info.Pins.Count; i++)
            {
                PartPinRow p = info.Pins[i];
                sb.Append(CultureInfo.InvariantCulture, $"  {p.Number,2} {p.Name,-8} {p.Direction,-6} {(p.NetName.Length == 0 ? "-" : p.NetName)}\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Draws the current level as SVG.
        /// </summary>
        /// <param name="path">
        /// The file to write.
        /// </param>
        /// <param name="page">
        /// The drawn page, all, or empty for the first.
        /// </param>
        /// <param name="symbols">
        /// nd or modern.
        /// </param>
        /// <param name="theme">
        /// A theme id, or empty for the default.
        /// </param>
        /// <param name="buses">
        /// False draws every bus member on its own.
        /// </param>
        /// <param name="names">
        /// slash, tilde or subscript, or empty for the default.
        /// </param>
        /// <returns>
        /// The files written and what the layout reported.
        /// </returns>
        [McpServerTool(Name = "export_svg"), Description("Draw the current level as SVG, as File > Save as SVG and the command line's render do: with the chip catalogue, in the card's page layout. page: a drawn page id (1, or 1.2 when split), all (one file per page: NAME-<page>.svg), or empty for the first. symbols nd or modern; theme a theme id (empty: Light); names slash, tilde or subscript. Says which fixed wires had to be routed afresh and which buses could not be drawn. Allowed read-only (writes only the SVG file).")]
        public string ExportSvg([Description("Full path of the .svg file to write")] string path,
            [Description("Drawn page id, all, or empty for the first")] string? page = null,
            [Description("nd (ND 1976 symbols) or modern")] string symbols = "nd",
            [Description("Theme id; empty for the default")] string? theme = null,
            [Description("False draws every bus member on its own")] bool buses = true,
            [Description("slash, tilde or subscript; empty for the default")] string? names = null)
        {
            SheetEditor e = Open();
            SvgRequest request = new() { ShowBuses = buses, Page = string.IsNullOrWhiteSpace(page) ? null : page.Trim() };
            request.Style = symbols.Trim().ToLowerInvariant() switch
            {
                "nd" => SymbolStyle.NorskData,
                "modern" => SymbolStyle.Modern,
                _ => throw new McpException("symbols must be nd or modern."),
            };
            if (!string.IsNullOrWhiteSpace(theme))
                request.Theme = SchematicTheme.Find(theme.Trim()) ?? throw new McpException("There is no theme " + theme + ".");
            if (!string.IsNullOrWhiteSpace(names))
            {
                if (!SignalNames.TryParseStyle(names.Trim(), out SignalNameStyle style))
                    throw new McpException("names must be slash, tilde or subscript.");
                request.NameStyle = style;
            }
            List<SvgPage> pages;
            try
            {
                pages = SheetRender.Svg(e.Netlist, e.Catalog, request);
            }
            catch (ArgumentException ex)
            {
                throw new McpException(ex.Message, ex);
            }
            StringBuilder sb = new();
            for (int i = 0; i < pages.Count; i++)
            {
                string outPath = request.Page == "all"
                    ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path)) ?? "", Path.GetFileNameWithoutExtension(path) + "-" + pages[i].Id + ".svg")
                    : path;
                try
                {
                    File.WriteAllText(outPath, pages[i].Svg);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    throw new McpException("Could not write " + outPath + ": " + ex.Message, ex);
                }
                sb.Append("Wrote ").Append(outPath).Append('\n');
                for (int k = 0; k < pages[i].Rerouted.Count; k++)
                    sb.Append("Wire routed afresh: ").Append(pages[i].Rerouted[k]).Append('\n');
                for (int k = 0; k < pages[i].BusProblems.Count; k++)
                    sb.Append("Bus: ").Append(pages[i].BusProblems[k]).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Measures both page layouts.
        /// </summary>
        /// <param name="limit">
        /// The chip count above which the split layout splits a page.
        /// </param>
        /// <returns>
        /// Both reports.
        /// </returns>
        [McpServerTool(Name = "page_stats"), Description("Measure both page layouts of the current level, as the command line's pages does: per drawn page the chips, parts, wire length, pieces, crossings, labels and off-page markers, for as-original and for split. A card without pages is measured as if converted (nothing changes). Allowed read-only.")]
        public string PageStatsReport([Description("For split: the chip count above which a page is split; 0 for the default (16)")] int limit = 0)
        {
            SheetEditor e = Open();
            return PageStats.ReportBoth(e.Netlist, e.Catalog, limit <= 0 ? PageSet.DefaultChipLimit : limit);
        }

        // a plain name made into a drawing owner: a part, else a pin (it has a dot), else a signal
        private static string OwnerOf(SheetEditor e, string item)
        {
            if (item.IndexOf(':') > 0)
                return item;
            if (e.FindPart(item) != null)
                return "part:" + item;
            if (item.IndexOf('.') > 0)
                return "pin:" + item;
            Net? n = e.FindNetByKey(item) ?? e.FindNet(item);
            return n == null ? "net:" + item : "net:" + SheetLayout.NetKey(n);
        }

        private static void AppendOwners(StringBuilder sb, string title, List<string> owners, string prefix)
        {
            List<string> picked = new();
            for (int i = 0; i < owners.Count; i++)
            {
                if (owners[i].StartsWith(prefix, StringComparison.Ordinal))
                    picked.Add(owners[i].Substring(prefix.Length));
            }
            if (picked.Count > 0)
                sb.Append(title).Append(": ").Append(string.Join(" ", picked)).Append('\n');
        }

        private static void Line(StringBuilder sb, string title, string? value)
        {
            if (!string.IsNullOrEmpty(value))
                sb.Append(title).Append(": ").Append(value).Append('\n');
        }
    }
}
