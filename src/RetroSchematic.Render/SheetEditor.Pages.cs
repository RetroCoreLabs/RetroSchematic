//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Editing a card's pages (format version 3): converting an older file, adding a page, and
    /// putting a part or a connector box on a page. Every change can be undone.
    /// </summary>
    public sealed partial class SheetEditor
    {
        /// <summary>
        /// Turns a version 1 or 2 card into version 3 with one page holding everything (see
        /// <see cref="CardPages.ToVersion3"/>). Nothing that is connected changes.
        /// </summary>
        /// <returns>
        /// False when the card had pages already (nothing changed).
        /// </returns>
        public bool ConvertToPages()
        {
            CheckWritable();
            if (CardPages.HasPages(Root))
                return false;
            Snapshot();
            CardPages.ToVersion3(Root);
            Done();
            return true;
        }

        /// <summary>
        /// Adds a page at the end of the card's pages. A card without pages is converted first,
        /// in the same undo step.
        /// </summary>
        /// <param name="title">
        /// The page's title.
        /// </param>
        /// <param name="source">
        /// The original page it is read from (its place in sources, 1 = first), or null.
        /// </param>
        /// <param name="sheetNumber">
        /// The sheet number in its title block, or null.
        /// </param>
        /// <param name="id">
        /// The id to give it, or null for the next free number.
        /// </param>
        /// <returns>
        /// The new page's id.
        /// </returns>
        public string AddPage(string? title, int? source = null, string? sheetNumber = null, string? id = null)
        {
            CheckWritable();
            if (id != null && (id.Length == 0 || CardPages.Find(Root, id) != null))
                throw new SheetEditException($"There is already a page \"{id}\", or the id is empty.");
            int sources = Root.Sources?.Count ?? 0;
            if (source != null && (source < 1 || source > sources))
                throw new SheetEditException($"Source {source} does not exist: the card has {sources} source page(s).");
            Snapshot();
            CardPages.ToVersion3(Root);
            string newId = id ?? CardPages.FreeId(Root);
            Root.Pages!.Add(new CardPage { Id = newId, Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(), Source = source, SheetNumber = sheetNumber });
            Done();
            return newId;
        }

        /// <summary>
        /// Changes a page's title, source page or sheet number. A null value is left as it is;
        /// an empty title or sheet number removes it, a source of 0 removes it.
        /// </summary>
        /// <param name="id">
        /// The page.
        /// </param>
        /// <param name="title">
        /// The new title, or null.
        /// </param>
        /// <param name="source">
        /// The new source page, 0 for none, or null.
        /// </param>
        /// <param name="sheetNumber">
        /// The new sheet number, or null.
        /// </param>
        public void SetPageInfo(string id, string? title, int? source, string? sheetNumber)
        {
            CheckWritable();
            CardPage page = CardPages.Find(Root, id) ?? throw new SheetEditException($"There is no page \"{id}\".");
            int sources = Root.Sources?.Count ?? 0;
            if (source != null && source != 0 && (source < 1 || source > sources))
                throw new SheetEditException($"Source {source} does not exist: the card has {sources} source page(s).");
            Snapshot();
            page = CardPages.Find(Root, id)!;
            if (title != null)
                page.Title = title.Trim().Length == 0 ? null : title.Trim();
            if (source != null)
                page.Source = source == 0 ? null : source;
            if (sheetNumber != null)
                page.SheetNumber = sheetNumber.Trim().Length == 0 ? null : sheetNumber.Trim();
            Done();
        }

        /// <summary>
        /// Puts a part (a chip or a passive part) on a page. Its hand-set position, if any, is
        /// kept: it is a place on the new page. Its signals stay as they are; where one now
        /// crosses pages, the drawing shows off-page markers.
        /// </summary>
        /// <param name="reference">
        /// The part.
        /// </param>
        /// <param name="page">
        /// The page's id.
        /// </param>
        public void SetPartPage(string reference, string page)
        {
            CheckWritable();
            CheckTopLevel();
            if (!CardPages.HasPages(Root))
                throw new SheetEditException("The card has no pages list (format version 3); convert it first.");
            if (CardPages.Find(Root, page) == null)
                throw new SheetEditException($"There is no page \"{page}\".");
            if (FindPart(reference) == null)
                throw new SheetEditException($"There is no part {reference} on the card.");
            Snapshot();
            FindPart(reference)!.Page = page;
            Done();
        }

        /// <summary>
        /// Puts a connector box (P, F, P.A ...) on a page. The connector gets an entry in the
        /// card's connectors list if it has none.
        /// </summary>
        /// <param name="reference">
        /// The connector.
        /// </param>
        /// <param name="page">
        /// The page's id.
        /// </param>
        public void SetConnectorPage(string reference, string page)
        {
            CheckWritable();
            CheckTopLevel();
            if (!ConnectorPins.IsConnector(reference))
                throw new SheetEditException($"{reference} is not a connector (P, F, P.A ...).");
            if (!CardPages.HasPages(Root))
                throw new SheetEditException("The card has no pages list (format version 3); convert it first.");
            if (CardPages.Find(Root, page) == null)
                throw new SheetEditException($"There is no page \"{page}\".");
            Snapshot();
            Root.Connectors ??= new List<ConnectorUse>();
            ConnectorUse? use = null;
            for (int i = 0; i < Root.Connectors.Count && use == null; i++)
            {
                if (Root.Connectors[i].Ref == reference)
                    use = Root.Connectors[i];
            }
            if (use == null)
            {
                use = new ConnectorUse { Ref = reference };
                Root.Connectors.Add(use);
            }
            use.Page = page;
            Done();
        }

        /// <summary>
        /// The layout options that draw the page a part is on (the card's own page layout), or
        /// the whole level when it has no pages.
        /// </summary>
        /// <param name="reference">
        /// The part.
        /// </param>
        /// <returns>
        /// The options.
        /// </returns>
        public LayoutOptions LayoutFor(string reference)
        {
            LayoutOptions options = new() { Catalog = Catalog };
            if (LevelPath.Count == 0 && CardPages.HasPages(Netlist))
            {
                options.Pages = PageSet.Of(Netlist);
                options.Page = System.Math.Max(0, options.Pages.PageOfPart(reference));
            }
            return options;
        }

        private void CheckTopLevel()
        {
            if (LevelPath.Count > 0)
                throw new SheetEditException("Pages belong to the top level of the card; leave the block first.");
        }
    }
}
