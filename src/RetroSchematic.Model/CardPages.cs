//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;

namespace RetroSchematic.Model
{
    /// <summary>
    /// The pages of a card (format version 3, docs/FORMAT.md "Format version 3: pages"): which
    /// page a part or a connector box is on, which pages a signal reaches, the reader's page
    /// checks, and the conversion of an older file to version 3.
    /// </summary>
    public static class CardPages
    {
        /// <summary>
        /// True when the file has a pages list (format version 3).
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <returns>
        /// True for a card with pages.
        /// </returns>
        public static bool HasPages(Netlist netlist) => netlist.Pages != null && netlist.Pages.Count > 0;

        /// <summary>
        /// The id of the first page, or null for a card without pages.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <returns>
        /// The id, or null.
        /// </returns>
        public static string? FirstPage(Netlist netlist) => HasPages(netlist) ? netlist.Pages![0].Id : null;

        /// <summary>
        /// A page by id, or null.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <param name="id">
        /// The page id.
        /// </param>
        /// <returns>
        /// The page, or null.
        /// </returns>
        public static CardPage? Find(Netlist netlist, string id)
        {
            for (int i = 0; netlist.Pages != null && i < netlist.Pages.Count; i++)
            {
                if (netlist.Pages[i].Id == id)
                    return netlist.Pages[i];
            }
            return null;
        }

        /// <summary>
        /// The page a part, a connector or an instance is on: its own page, or the first page
        /// when it names none. Null for a card without pages.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <param name="page">
        /// The page the item names, or null.
        /// </param>
        /// <returns>
        /// The page id, or null.
        /// </returns>
        public static string? PageOrFirst(Netlist netlist, string? page) => HasPages(netlist) ? page ?? netlist.Pages![0].Id : null;

        /// <summary>
        /// The page of each top-level part, by reference (the first page for a part naming none).
        /// Empty for a card without pages.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <returns>
        /// Part reference to page id.
        /// </returns>
        public static Dictionary<string, string> PartPages(Netlist netlist)
        {
            Dictionary<string, string> map = new(StringComparer.Ordinal);
            if (!HasPages(netlist))
                return map;
            string first = netlist.Pages![0].Id;
            for (int i = 0; i < netlist.Parts.Count; i++)
                map[netlist.Parts[i].Ref] = netlist.Parts[i].Page ?? first;
            return map;
        }

        /// <summary>
        /// The page of each connector box (P, F, P.A ...) the card's signals use: the page its
        /// <c>connectors</c> entry names, else the first page. Empty for a card without pages.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <returns>
        /// Connector reference to page id.
        /// </returns>
        public static Dictionary<string, string> ConnectorPages(Netlist netlist)
        {
            Dictionary<string, string> map = new(StringComparer.Ordinal);
            if (!HasPages(netlist))
                return map;
            string first = netlist.Pages![0].Id;
            for (int i = 0; i < netlist.Nets.Count; i++)
            {
                List<PinReference> pins = netlist.Nets[i].Pins;
                for (int j = 0; j < pins.Count; j++)
                {
                    string r = pins[j].PartRef;
                    if (ConnectorPins.IsConnector(r))
                        map[r] = first;
                }
            }
            for (int i = 0; netlist.Connectors != null && i < netlist.Connectors.Count; i++)
            {
                ConnectorUse use = netlist.Connectors[i];
                if (use.Page != null && map.ContainsKey(use.Ref))
                    map[use.Ref] = use.Page;
            }
            return map;
        }

        /// <summary>
        /// The pages a signal reaches, in page order: the pages of its parts and of the
        /// connector boxes of its connector pins. Empty for a card without pages.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <returns>
        /// The page ids.
        /// </returns>
        public static List<string> PagesOf(Netlist netlist, Net net) => PagesOf(netlist, net, PartPages(netlist), ConnectorPages(netlist));

        /// <summary>
        /// The pages a signal reaches, with the page maps worked out once by the caller.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <param name="partPages">
        /// From <see cref="PartPages"/>.
        /// </param>
        /// <param name="connectorPages">
        /// From <see cref="ConnectorPages"/>.
        /// </param>
        /// <returns>
        /// The page ids, in page order.
        /// </returns>
        public static List<string> PagesOf(Netlist netlist, Net net, Dictionary<string, string> partPages, Dictionary<string, string> connectorPages)
        {
            List<string> list = new();
            if (!HasPages(netlist))
                return list;
            HashSet<string> seen = new(StringComparer.Ordinal);
            for (int j = 0; j < net.Pins.Count; j++)
            {
                string r = net.Pins[j].PartRef;
                if (partPages.TryGetValue(r, out string? p) || connectorPages.TryGetValue(r, out p))
                    seen.Add(p);
            }
            for (int i = 0; i < netlist.Pages!.Count; i++)
            {
                if (seen.Contains(netlist.Pages[i].Id))
                    list.Add(netlist.Pages[i].Id);
            }
            return list;
        }

        /// <summary>
        /// The reader's page checks (docs/FORMAT.md, "Checks a version 3 reader makes"): a
        /// version 3 file has at least one page, page ids are unique, every top-level part names
        /// a page that exists, a connector, instance, wire path or bus names one that exists or
        /// none, a page's source is a place in the sources list, and no part inside a block names
        /// a page. A version 1 or 2 file has no page anywhere.
        /// </summary>
        /// <param name="netlist">
        /// The file as read.
        /// </param>
        /// <exception cref="NetlistException">
        /// A check fails.
        /// </exception>
        public static void Check(Netlist netlist)
        {
            bool v3 = netlist.FormatVersion == NetlistReader.PagesFormatVersion;
            if (!v3)
            {
                if (netlist.Pages != null || netlist.PageLayout != null)
                    throw new NetlistException("Pages need format version 3.");
                for (int b = 0; netlist.Blocks != null && b < netlist.Blocks.Count; b++)
                {
                    if (netlist.Blocks[b].Pages != null)
                        throw new NetlistException($"Block {netlist.Blocks[b].Name} lists pages; pages need format version 3.");
                }
                for (int i = 0; i < netlist.Parts.Count; i++)
                {
                    if (netlist.Parts[i].Page != null)
                        throw new NetlistException($"Part {netlist.Parts[i].Ref} names a page; pages need format version 3.");
                }
                for (int i = 0; netlist.Connectors != null && i < netlist.Connectors.Count; i++)
                {
                    if (netlist.Connectors[i].Page != null)
                        throw new NetlistException($"Connector {netlist.Connectors[i].Ref} names a page; pages need format version 3.");
                }
                return;
            }
            if (netlist.Pages == null || netlist.Pages.Count == 0)
                throw new NetlistException("A version 3 file lists at least one page.");
            HashSet<string> ids = new(StringComparer.Ordinal);
            int sources = netlist.Sources?.Count ?? 0;
            for (int i = 0; i < netlist.Pages.Count; i++)
            {
                CardPage page = netlist.Pages[i];
                if (page.Id.Length == 0)
                    throw new NetlistException($"Page {i + 1} has no id.");
                if (!ids.Add(page.Id))
                    throw new NetlistException($"Page {page.Id} is listed twice.");
                if (page.Source != null && (page.Source < 1 || page.Source > sources))
                    throw new NetlistException($"Page {page.Id} is read from source {page.Source}, but the card has {sources} source page(s).");
            }
            for (int i = 0; i < netlist.Parts.Count; i++)
            {
                Part part = netlist.Parts[i];
                if (part.Page == null)
                    throw new NetlistException($"Part {part.Ref} names no page; in a version 3 file every part is on a page.");
                if (!ids.Contains(part.Page))
                    throw new NetlistException($"Part {part.Ref} is on page {part.Page}, which is not in the pages list.");
            }
            for (int i = 0; netlist.Connectors != null && i < netlist.Connectors.Count; i++)
                CheckNamed(ids, "Connector " + netlist.Connectors[i].Ref, netlist.Connectors[i].Page);
            for (int i = 0; netlist.Instances != null && i < netlist.Instances.Count; i++)
                CheckNamed(ids, "Instance " + netlist.Instances[i].Name, netlist.Instances[i].Page);
            for (int i = 0; netlist.Layout != null && i < netlist.Layout.Wires.Count; i++)
                CheckNamed(ids, "The wire path of " + netlist.Layout.Wires[i].Net, netlist.Layout.Wires[i].Page);
            for (int i = 0; netlist.Layout?.Buses != null && i < netlist.Layout.Buses.Count; i++)
                CheckNamed(ids, "Bus " + netlist.Layout.Buses[i].Name, netlist.Layout.Buses[i].Page);
            for (int b = 0; netlist.Blocks != null && b < netlist.Blocks.Count; b++)
            {
                List<Part> parts = netlist.Blocks[b].Parts;
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i].Page != null)
                        throw new NetlistException($"Block {netlist.Blocks[b].Name}: part {parts[i].Ref} names a page; a block's inside has no pages (its instance has).");
                }
                // the original pages that draw the block's inside: ids unique in the block, each
                // source a place in the card's sources list
                List<CardPage>? blockPages = netlist.Blocks[b].Pages;
                HashSet<string> blockIds = new(StringComparer.Ordinal);
                for (int i = 0; blockPages != null && i < blockPages.Count; i++)
                {
                    CardPage page = blockPages[i];
                    if (page.Id.Length == 0)
                        throw new NetlistException($"Block {netlist.Blocks[b].Name}: page {i + 1} has no id.");
                    if (!blockIds.Add(page.Id))
                        throw new NetlistException($"Block {netlist.Blocks[b].Name}: page {page.Id} is listed twice.");
                    if (page.Source != null && (page.Source < 1 || page.Source > sources))
                        throw new NetlistException($"Block {netlist.Blocks[b].Name}: page {page.Id} is read from source {page.Source}, but the card has {sources} source page(s).");
                }
            }
        }

        private static void CheckNamed(HashSet<string> ids, string what, string? page)
        {
            if (page != null && !ids.Contains(page))
                throw new NetlistException($"{what} is on page {page}, which is not in the pages list.");
        }

        /// <summary>
        /// Turns a version 1 or 2 file into version 3 with one page: the page takes the sheet's
        /// title and number, is read from the first source page (when there is one), and holds
        /// every part and connector box. Nothing that is connected changes. A file that has pages
        /// already is left as it is.
        /// </summary>
        /// <param name="netlist">
        /// The card; changed in place.
        /// </param>
        /// <returns>
        /// True when it was changed.
        /// </returns>
        public static bool ToVersion3(Netlist netlist)
        {
            if (HasPages(netlist))
                return false;
            CardPage page = new()
            {
                Id = "1",
                Title = netlist.Sheet.Title,
                Source = netlist.Sources != null && netlist.Sources.Count > 0 ? 1 : null,
                SheetNumber = netlist.Sheet.Id.Length == 0 ? null : netlist.Sheet.Id,
            };
            netlist.Pages = new List<CardPage> { page };
            for (int i = 0; i < netlist.Parts.Count; i++)
                netlist.Parts[i].Page = page.Id;
            netlist.FormatVersion = NetlistReader.PagesFormatVersion;
            return true;
        }

        /// <summary>
        /// Puts every top-level part that names no page on a page (the given one, or the first),
        /// and takes the page off every part inside a block (a block's inside has no pages).
        /// Called after every edit, so a part placed, a block dissolved or a part moved into a
        /// block always ends up as the format wants it. Does nothing for a card without pages.
        /// </summary>
        /// <param name="netlist">
        /// The whole card.
        /// </param>
        /// <param name="page">
        /// The page new parts go on, or null for the first page.
        /// </param>
        public static void Normalize(Netlist netlist, string? page)
        {
            if (!HasPages(netlist))
                return;
            string target = page != null && Find(netlist, page) != null ? page : netlist.Pages![0].Id;
            for (int i = 0; i < netlist.Parts.Count; i++)
                netlist.Parts[i].Page ??= target;
            for (int b = 0; netlist.Blocks != null && b < netlist.Blocks.Count; b++)
            {
                List<Part> parts = netlist.Blocks[b].Parts;
                for (int i = 0; i < parts.Count; i++)
                    parts[i].Page = null;
            }
        }

        /// <summary>
        /// A page id not used yet: the next number after the highest numbered page.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <returns>
        /// The id.
        /// </returns>
        public static string FreeId(Netlist netlist)
        {
            int high = 0;
            for (int i = 0; netlist.Pages != null && i < netlist.Pages.Count; i++)
            {
                if (int.TryParse(netlist.Pages[i].Id, NumberStyles.None, CultureInfo.InvariantCulture, out int n))
                    high = Math.Max(high, n);
            }
            for (int n = high + 1; ; n++)
            {
                string id = n.ToString(CultureInfo.InvariantCulture);
                if (Find(netlist, id) == null)
                    return id;
            }
        }
    }
}
