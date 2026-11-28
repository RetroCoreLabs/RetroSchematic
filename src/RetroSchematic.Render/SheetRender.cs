//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// How to draw a sheet as SVG: symbols, theme, buses, name style and which drawn page.
    /// </summary>
    public sealed class SvgRequest
    {
        /// <summary>The symbol set (default: the ND 1976 symbols).</summary>
        public SymbolStyle Style { get; set; } = SymbolStyle.NorskData;

        /// <summary>The colours (default: the Light theme).</summary>
        public SchematicTheme Theme { get; set; } = SchematicTheme.Default;

        /// <summary>False draws every bus member on its own.</summary>
        public bool ShowBuses { get; set; } = true;

        /// <summary>How active-low names are written.</summary>
        public SignalNameStyle NameStyle { get; set; } = SignalNames.Default;

        /// <summary>The drawn page (1, or 1.2 when split), "all" for every page, or null for the first.</summary>
        public string? Page { get; set; }

        /// <summary>The page layout, or null for the card's own (as-original when it has none).</summary>
        public PageLayoutMode? Mode { get; set; }
    }

    /// <summary>
    /// One drawn page as SVG, with what the layout reports about it.
    /// </summary>
    public sealed class SvgPage
    {
        /// <summary>The drawn page's id, or empty for a card without pages.</summary>
        public string Id { get; init; } = "";

        /// <summary>The SVG text.</summary>
        public string Svg { get; init; } = "";

        /// <summary>The fixed wire paths that could not be used, each with its reason.</summary>
        public List<string> Rerouted { get; } = new();

        /// <summary>The buses that could not be drawn as given, each with its reason.</summary>
        public List<string> BusProblems { get; } = new();
    }

    /// <summary>
    /// Draws a sheet as SVG exactly as the window draws it: with the chip catalogue (a chip
    /// without gates then shows every pin) and, for a card with pages, in its page layout. The
    /// command-line <c>render</c> and the MCP tool <c>export_svg</c> both use it.
    /// </summary>
    public static class SheetRender
    {
        /// <summary>
        /// Draws one drawn page, or every one.
        /// </summary>
        /// <param name="netlist">
        /// The level to draw (the whole sheet, or a block's inside).
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue, or null.
        /// </param>
        /// <param name="request">
        /// How to draw it.
        /// </param>
        /// <returns>
        /// The pages drawn, in order.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The card has no drawn page of that id.
        /// </exception>
        public static List<SvgPage> Svg(Netlist netlist, ChipCatalog? catalog, SvgRequest request)
        {
            LayoutOptions options = new() { Catalog = catalog, ShowBuses = request.ShowBuses, NameStyle = request.NameStyle };
            if (CardPages.HasPages(netlist))
                options.Pages = PageSet.Of(netlist, request.Mode ?? netlist.PageLayout ?? PageLayoutMode.AsOriginal, PageSet.DefaultChipLimit);
            int first = 0, last = 0;
            if (options.Pages != null && request.Page == "all")
                last = options.Pages.Pages.Count - 1;
            else if (options.Pages != null && request.Page != null)
            {
                first = last = options.Pages.IndexOf(request.Page);
                if (first < 0)
                    throw new ArgumentException("No drawn page \"" + request.Page + "\".");
            }
            List<SvgPage> pages = new();
            for (int pg = first; pg <= last; pg++)
            {
                options.Page = pg;
                SchematicDrawing drawing = SheetLayout.Build(netlist, request.Style, options);
                SvgPage page = new() { Id = options.Pages == null ? "" : options.Pages.Pages[pg].Id, Svg = SvgRenderer.Write(drawing, request.Theme) };
                page.Rerouted.AddRange(drawing.RerouteReasons);
                page.BusProblems.AddRange(drawing.BusProblems);
                pages.Add(page);
            }
            return pages;
        }
    }
}
