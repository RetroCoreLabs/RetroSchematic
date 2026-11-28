//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// What one drawn page measures: its size, wires, crossings, labels and off-page markers.
    /// Used to compare the two page layouts (as the original, split automatically).
    /// </summary>
    public sealed class PageMeasure
    {
        /// <summary>
        /// The drawn page's id.
        /// </summary>
        public string Id { get; init; } = "";

        /// <summary>
        /// Chips on it (passive parts not counted).
        /// </summary>
        public int Chips { get; init; }

        /// <summary>
        /// All parts on it.
        /// </summary>
        public int Parts { get; init; }

        /// <summary>
        /// The wires, measured by <see cref="WireStats"/>.
        /// </summary>
        public WireStats Wires { get; init; }

        /// <summary>
        /// Net-label diamonds drawn (signals not wired on this page).
        /// </summary>
        public int Labels { get; init; }

        /// <summary>
        /// Off-page markers drawn.
        /// </summary>
        public int OffPageMarkers { get; init; }

        /// <summary>
        /// Width of the drawing, in drawing units.
        /// </summary>
        public double Width { get; init; }

        /// <summary>
        /// Height of the drawing, in drawing units.
        /// </summary>
        public double Height { get; init; }
    }

    /// <summary>
    /// Measures every drawn page of a card in one page layout.
    /// </summary>
    public static class PageStats
    {
        /// <summary>
        /// Lays out and measures every drawn page.
        /// </summary>
        /// <param name="netlist">
        /// The card, with pages.
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue, or null.
        /// </param>
        /// <param name="mode">
        /// The page layout.
        /// </param>
        /// <param name="chipLimit">
        /// For a split: the chip count above which a page is split.
        /// </param>
        /// <returns>
        /// One measure per drawn page.
        /// </returns>
        public static List<PageMeasure> Measure(Netlist netlist, ChipCatalog? catalog, PageLayoutMode mode, int chipLimit)
        {
            List<PageMeasure> list = new();
            PageSet set = PageSet.Of(netlist, mode, chipLimit);
            LayoutOptions options = new() { Catalog = catalog, Pages = set };
            for (int p = 0; p < set.Pages.Count; p++)
            {
                options.Page = p;
                SchematicDrawing d = SheetLayout.Build(netlist, SymbolStyle.NorskData, options);
                int labels = 0, markers = 0, chips = 0;
                for (int i = 0; i < d.Items.Count; i++)
                {
                    DrawItem it = d.Items[i];
                    if (it.Role == DrawRole.NetLabel && it.Kind == DrawKind.Path)
                        labels++;
                    else if (it.Role == DrawRole.OffPage && it.Kind == DrawKind.Rect)
                        markers++;
                }
                for (int i = 0; i < netlist.Parts.Count; i++)
                {
                    if (set.Pages[p].Parts.Contains(netlist.Parts[i].Ref) && !PassiveParts.IsPassive(netlist.Parts[i].Type))
                        chips++;
                }
                list.Add(new PageMeasure
                {
                    Id = set.Pages[p].Id,
                    Chips = chips,
                    Parts = set.Pages[p].Parts.Count,
                    Wires = WireStats.Measure(d),
                    Labels = labels,
                    OffPageMarkers = markers,
                    Width = d.Width,
                    Height = d.Height,
                });
            }
            return list;
        }

        /// <summary>
        /// The measures as text, one line per page and a total, for the command line.
        /// </summary>
        /// <param name="netlist">
        /// The card, with pages.
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue, or null.
        /// </param>
        /// <param name="mode">
        /// The page layout.
        /// </param>
        /// <param name="chipLimit">
        /// For a split: the chip count above which a page is split.
        /// </param>
        /// <returns>
        /// The report.
        /// </returns>
        public static string Report(Netlist netlist, ChipCatalog? catalog, PageLayoutMode mode, int chipLimit)
        {
            return ReportOne(netlist, catalog, mode, chipLimit);
        }

        /// <summary>
        /// Both page layouts measured (as the original, then split): the command-line
        /// <c>pages</c> and the MCP tool <c>page_stats</c>. A card without pages is measured as if
        /// converted to format version 3, on a copy, so nothing is changed.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue, or null.
        /// </param>
        /// <param name="chipLimit">
        /// For a split: the chip count above which a page is split.
        /// </param>
        /// <returns>
        /// The two reports, one after the other.
        /// </returns>
        public static string ReportBoth(Netlist netlist, ChipCatalog? catalog, int chipLimit)
        {
            Netlist card = netlist;
            if (!CardPages.HasPages(card))
            {
                card = NetlistReader.Parse(NetlistReader.Write(netlist));
                CardPages.ToVersion3(card);
            }
            return ReportOne(card, catalog, PageLayoutMode.AsOriginal, chipLimit) + ReportOne(card, catalog, PageLayoutMode.Split, chipLimit);
        }

        private static string ReportOne(Netlist netlist, ChipCatalog? catalog, PageLayoutMode mode, int chipLimit)
        {
            List<PageMeasure> pages = Measure(netlist, catalog, mode, chipLimit);
            StringBuilder sb = new();
            CultureInfo c = CultureInfo.InvariantCulture;
            sb.Append(mode == PageLayoutMode.AsOriginal ? "As the original" : "Split automatically (limit " + chipLimit.ToString(c) + " chips)");
            sb.Append(": ").Append(pages.Count.ToString(c)).Append(" drawn page(s)\n");
            double length = 0, area = 0;
            int crossings = 0, labels = 0, markers = 0, pieces = 0;
            for (int i = 0; i < pages.Count; i++)
            {
                PageMeasure m = pages[i];
                sb.Append(string.Format(c, "  page {0,-5} {1,3} chips {2,3} parts  wire {3,6:0} steps in {4,4} pieces  {5,4} crossings  {6,4} labels  {7,4} off-page  {8:0} x {9:0}\n",
                    m.Id, m.Chips, m.Parts, m.Wires.Length / WireRouter.Step, m.Wires.Pieces, m.Wires.Crossings, m.Labels, m.OffPageMarkers, m.Width, m.Height));
                length += m.Wires.Length;
                crossings += m.Wires.Crossings;
                labels += m.Labels;
                markers += m.OffPageMarkers;
                pieces += m.Wires.Pieces;
                area += m.Width * m.Height;
            }
            sb.Append(string.Format(c, "  total: wire {0:0} steps in {1} pieces, {2} crossings, {3} labels, {4} off-page markers, area {5:0} square units\n",
                length / WireRouter.Step, pieces, crossings, labels, markers, area));
            return sb.ToString();
        }
    }
}
