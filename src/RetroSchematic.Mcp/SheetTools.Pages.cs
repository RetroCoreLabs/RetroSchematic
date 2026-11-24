//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// The MCP tools for a card's pages (format version 3): list them, add one, put a part or a
    /// connector box on one, pick the page the other tools work on, and show one page.
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>
        /// Lists the card's pages.
        /// </summary>
        /// <returns>
        /// One line per card page, then the drawn pages.
        /// </returns>
        [McpServerTool(Name = "list_pages"), Description("List the card's pages (format version 3): id, title, the source page it was read from (number in list_sources), the sheet number in its title block, and how many chips and parts are on it; then the drawn pages for the card's page layout (as-original, or split with ids such as 1.2). Marks the page the other tools work on. Allowed read-only.")]
        public string ListPages()
        {
            SheetEditor e = Open();
            Netlist n = e.Root;
            if (!CardPages.HasPages(n))
                return "The card has no pages list (format version " + n.FormatVersion.ToString(CultureInfo.InvariantCulture) + "); add_page converts it to version 3.";
            Dictionary<string, string> partPages = CardPages.PartPages(n);
            string shown = CardPages.PageOrFirst(n, e.Page)!;
            StringBuilder sb = new();
            for (int p = 0; p < n.Pages!.Count; p++)
            {
                CardPage page = n.Pages[p];
                int chips = 0, parts = 0;
                for (int i = 0; i < n.Parts.Count; i++)
                {
                    if (partPages[n.Parts[i].Ref] != page.Id)
                        continue;
                    parts++;
                    if (!PassiveParts.IsPassive(n.Parts[i].Type))
                        chips++;
                }
                sb.Append(page.Id == shown ? "* " : "  ").Append(page.Id).Append(": ").Append(page.Title ?? "(no title)");
                if (page.Source != null)
                    sb.Append(", source ").Append(page.Source.Value.ToString(CultureInfo.InvariantCulture));
                if (page.SheetNumber != null)
                    sb.Append(", sheet ").Append(page.SheetNumber);
                sb.Append(CultureInfo.InvariantCulture, $", {chips} chips, {parts} parts\n");
            }
            PageSet set = PageSet.Of(n);
            sb.Append("Page layout: ").Append(set.Mode == PageLayoutMode.Split ? "split automatically" : "as the original").Append("; drawn pages:");
            for (int i = 0; i < set.Pages.Count; i++)
                sb.Append(' ').Append(set.Pages[i].Id);
            sb.Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Adds a page.
        /// </summary>
        /// <param name="title">
        /// The page's title.
        /// </param>
        /// <param name="source">
        /// The source page it is read from (number in list_sources), or 0.
        /// </param>
        /// <param name="sheetNumber">
        /// The sheet number in its title block, or empty.
        /// </param>
        /// <param name="id">
        /// The id to give it, or empty for the next number.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "add_page"), Description("Add a page at the end of the card's pages (edit mode; undoable). A card without pages (format version 1 or 2) is first turned into version 3 with one page holding every part, in the same undo step. The new page is empty; place parts on it with place_part/add_passive (page argument) or move them with set_part_page.")]
        public string AddPage([Description("The page's title, as in its title block")] string title,
            [Description("The original page it is read from: its number in list_sources, 0 for none")] int source = 0,
            [Description("The sheet number in the page's title block, empty if not read")] string? sheetNumber = null,
            [Description("The id to give it; empty for the next number")] string? id = null)
            => Run(e =>
            {
                bool converted = !CardPages.HasPages(e.Root);
                string pid = e.AddPage(title, source == 0 ? null : source, string.IsNullOrEmpty(sheetNumber) ? null : sheetNumber, string.IsNullOrEmpty(id) ? null : id);
                return (converted ? "Converted the card to format version 3 with page 1 holding every part. " : "") + "Added page " + pid + ".";
            });

        /// <summary>
        /// Puts a part or a connector box on a page.
        /// </summary>
        /// <param name="reference">
        /// The part (13D, R1) or connector (P, F, P.A).
        /// </param>
        /// <param name="page">
        /// The page's id.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_part_page"), Description("Put a part (chip or passive, by board position or name) or a connector box (P, F, P.A ...) on a page (edit mode; undoable). Signals stay as they are: a signal that now crosses pages is drawn with off-page markers. Several parts: separate them with commas or spaces.")]
        public string SetPartPage([Description("Part(s) or connector, e.g. 13D or \"13D, 13E\" or P")] string reference, [Description("The page's id")] string page)
            => Run(e =>
            {
                string[] refs = reference.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (refs.Length == 0)
                    throw new SheetEditException("Give a part or a connector.");
                for (int i = 0; i < refs.Length; i++)
                {
                    if (ConnectorPins.IsConnector(refs[i]))
                        e.SetConnectorPage(refs[i], page);
                    else
                        e.SetPartPage(refs[i], page);
                }
                return "Put " + string.Join(", ", refs) + " on page " + page + ".";
            });

        /// <summary>
        /// Picks the page the other tools work on.
        /// </summary>
        /// <param name="page">
        /// The page's id.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_page"), Description("Choose the card page the other tools work on: place_part and add_passive put new parts on it (unless given a page), set_wire_path, fix_wire and create_bus draw on it. Allowed read-only; not a change to the card.")]
        public string SetPage([Description("The page's id")] string page)
        {
            SheetEditor e = Open();
            if (CardPages.Find(e.Root, page) == null)
                throw new McpException("There is no page \"" + page + "\".");
            e.Page = page;
            return "Working on page " + page + ".";
        }

        /// <summary>
        /// Shows one page.
        /// </summary>
        /// <param name="page">
        /// A drawn page's id, or empty for the page the tools work on.
        /// </param>
        /// <returns>
        /// The page's parts, connector boxes, and the signals that leave it.
        /// </returns>
        [McpServerTool(Name = "get_page"), Description("One drawn page: its title and source page, its parts and connector boxes, the signals wholly on it, and every signal that crosses to another page with the pages it goes to (the off-page markers). page: a drawn page id (1, or 1.2 when split); empty for the page the tools work on. Allowed read-only.")]
        public string GetPage([Description("Drawn page id, e.g. 1 or 1.2; empty for the page the tools work on")] string? page = null)
        {
            SheetEditor e = Open();
            Netlist n = e.Root;
            if (!CardPages.HasPages(n))
                throw new McpException("The card has no pages list (format version 3); add_page converts it.");
            PageSet set = PageSet.Of(n);
            int index = string.IsNullOrEmpty(page) ? set.FirstOfCardPage(CardPages.PageOrFirst(n, e.Page)!) : set.IndexOf(page);
            if (index < 0)
                throw new McpException("There is no drawn page \"" + page + "\"; list_pages lists them.");
            DrawnPage d = set.Pages[index];
            StringBuilder sb = new();
            sb.Append("Page ").Append(d.Id).Append(": ").Append(d.Title);
            if (d.Source != null)
                sb.Append(" (source ").Append(d.Source.Value.ToString(CultureInfo.InvariantCulture)).Append(')');
            sb.Append("\nParts:");
            for (int i = 0; i < n.Parts.Count; i++)
            {
                if (d.Parts.Contains(n.Parts[i].Ref))
                    sb.Append(' ').Append(n.Parts[i].Ref).Append(' ').Append(n.Parts[i].Type).Append(';');
            }
            sb.Append("\nConnector boxes:");
            foreach (string c in d.Connectors)
                sb.Append(' ').Append(c);
            StringBuilder local = new(), crossing = new();
            int localCount = 0, crossCount = 0;
            for (int i = 0; i < n.Nets.Count; i++)
            {
                List<int> on = set.PagesOf(n.Nets[i]);
                if (!on.Contains(index))
                    continue;
                string key = SheetLayout.NetKey(n.Nets[i]);
                if (on.Count == 1)
                {
                    local.Append(' ').Append(key);
                    localCount++;
                    continue;
                }
                crossing.Append("\n  ").Append(key).Append(" -> page");
                for (int k = 0; k < on.Count; k++)
                {
                    if (on[k] != index)
                        crossing.Append(' ').Append(set.Pages[on[k]].Id);
                }
                crossCount++;
            }
            sb.Append(CultureInfo.InvariantCulture, $"\nSignals on this page only ({localCount}):").Append(local);
            sb.Append(CultureInfo.InvariantCulture, $"\nSignals that cross to other pages ({crossCount}):").Append(crossing).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Sets how the redrawn drawing is cut into pages.
        /// </summary>
        /// <param name="layout">
        /// as-original or split.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_page_layout"), Description("How the redrawn drawing is cut into pages (undoable; drawing only, allowed read-only): as-original = one drawn page per card page (default); split = a page with more than 16 chips is split into several drawn pages by grouping the parts that share the most signals, with off-page markers between them.")]
        public string SetPageLayout([Description("as-original or split")] string layout)
        {
            SheetEditor e = Open();
            PageLayoutMode mode = layout.Trim().ToLowerInvariant() switch
            {
                "as-original" => PageLayoutMode.AsOriginal,
                "split" => PageLayoutMode.Split,
                _ => throw new McpException("layout must be as-original or split, not '" + layout + "'."),
            };
            try
            {
                e.SetPageLayout(mode);
            }
            catch (SheetEditException ex)
            {
                throw new McpException("Refused: " + ex.Message, ex);
            }
            return "Page layout: " + layout + ".";
        }

        // runs a change with the page the tools work on set to the given one for its length
        private string OnPage(string? page, Func<SheetEditor, string> change)
            => Run(e =>
            {
                if (string.IsNullOrEmpty(page))
                    return change(e);
                if (CardPages.Find(e.Root, page) == null)
                    throw new SheetEditException("There is no page \"" + page + "\".");
                string? was = e.Page;
                e.Page = page;
                try
                {
                    return change(e);
                }
                finally
                {
                    e.Page = was;
                }
            });
    }
}
