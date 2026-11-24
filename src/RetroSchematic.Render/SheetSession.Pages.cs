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
    /// The pages of a card (format version 3): which drawn page is shown, going to another page,
    /// and following a signal across pages through its off-page markers.
    /// </summary>
    public sealed partial class SheetSession
    {
        /// <summary>
        /// The drawn pages of the card, or null when the level shown has no pages (a card
        /// without pages, or the inside of a block).
        /// </summary>
        public PageSet? Pages => _options.Pages;

        /// <summary>
        /// Which drawn page is shown (0 = first); 0 when there are no pages.
        /// </summary>
        public int PageIndex => _options.Page;

        /// <summary>
        /// The drawn page shown, or null when there are no pages.
        /// </summary>
        public DrawnPage? CurrentPage => _options.Pages == null ? null : _options.Pages.Pages[_options.Page];

        /// <summary>
        /// The number of drawn pages (1 for a card without pages).
        /// </summary>
        public int PageCount => _options.Pages?.Pages.Count ?? 1;

        /// <summary>
        /// Called when another page is shown.
        /// </summary>
        public event DrawingChangedHandler? PageChanged;

        /// <summary>
        /// Shows another drawn page. The trace stays (it is card-wide) and is shown on the new
        /// page; a pick that is not on the new page is dropped.
        /// </summary>
        /// <param name="index">
        /// The page's place, 0 = first; kept inside the pages there are.
        /// </param>
        public void SetPage(int index)
        {
            if (_options.Pages == null)
                return;
            index = Math.Clamp(index, 0, _options.Pages.Pages.Count - 1);
            if (index == _options.Page)
                return;
            _options.Page = index;
            Editor.Page = _options.Pages.Pages[index].CardPage;
            Relayout();
            if (Traced.Length > 0)
                Trace(Traced);
            PageChanged?.Invoke(this);
            if (Selected.Length > 0 && !OnThisPage(Selected))
                Select("");
        }

        /// <summary>
        /// Shows a drawn page by id ("2", or "2.1" for part of a split page).
        /// </summary>
        /// <param name="id">
        /// The drawn page's id.
        /// </param>
        /// <returns>
        /// False when there is no such page.
        /// </returns>
        public bool SetPage(string id)
        {
            int i = _options.Pages?.IndexOf(id) ?? -1;
            if (i < 0)
                return false;
            SetPage(i);
            return true;
        }

        /// <summary>
        /// The next page (Page Down), or the one before (Page Up). Stops at the ends.
        /// </summary>
        /// <param name="step">
        /// 1 for the next page, -1 for the one before.
        /// </param>
        public void StepPage(int step) => SetPage(_options.Page + step);

        /// <summary>
        /// The ids of the drawn pages a signal is on, in page order; empty when there are no pages.
        /// </summary>
        /// <param name="key">
        /// The signal's key.
        /// </param>
        /// <returns>
        /// The page ids.
        /// </returns>
        public List<string> PagesOfSignal(string key)
        {
            List<string> ids = new();
            Net? net = Editor.FindNetByKey(key);
            if (_options.Pages == null || net == null)
                return ids;
            List<int> on = _options.Pages.PagesOf(net);
            for (int i = 0; i < on.Count; i++)
                ids.Add(_options.Pages.Pages[on[i]].Id);
            return ids;
        }

        /// <summary>
        /// Follows a signal to another page: the next page after this one that the signal is on
        /// (round to the first again), with the signal picked and traced there. This is what a
        /// click on an off-page marker does.
        /// </summary>
        /// <param name="key">
        /// The signal's key.
        /// </param>
        /// <returns>
        /// False when the signal is on no other page.
        /// </returns>
        public bool FollowOffPage(string key)
        {
            Net? net = Editor.FindNetByKey(key);
            if (_options.Pages == null || net == null)
                return false;
            List<int> on = _options.Pages.PagesOf(net);
            int target = -1;
            for (int i = 0; i < on.Count && target < 0; i++)
            {
                if (on[i] > _options.Page)
                    target = on[i];
            }
            if (target < 0 && on.Count > 0 && on[0] != _options.Page)
                target = on[0];
            if (target < 0)
                return false;
            SetPage(target);
            Trace(key);
            Select("net:" + key);
            return true;
        }

        /// <summary>
        /// Shows the page an item is on, before it is picked from a list or the search results:
        /// a part's or pin's own page; for a signal, this page when it is on it, else its first.
        /// </summary>
        /// <param name="owner">
        /// The owner name, such as part:11D or net:BD3_0.
        /// </param>
        public void ShowPageOf(string owner)
        {
            if (_options.Pages == null || OnThisPage(owner))
                return;
            int page = PageOfOwner(owner);
            if (page >= 0)
                SetPage(page);
        }

        /// <summary>
        /// True when an item is drawn on the page shown (always true without pages).
        /// </summary>
        /// <param name="owner">
        /// The owner name.
        /// </param>
        /// <returns>
        /// True when it is on this page.
        /// </returns>
        public bool OnThisPage(string owner)
        {
            PageSet? set = _options.Pages;
            if (set == null)
                return true;
            if (owner.StartsWith(SheetLayout.OffPageOwner, StringComparison.Ordinal))
                return true;
            if (owner.StartsWith("net:", StringComparison.Ordinal))
            {
                Net? n = Editor.FindNetByKey(owner.Substring(4));
                return n == null || set.PagesOf(n).Contains(_options.Page);
            }
            int p = PageOfOwner(owner);
            return p < 0 || p == _options.Page;
        }

        private int PageOfOwner(string owner)
        {
            PageSet set = _options.Pages!;
            if (owner.StartsWith("part:", StringComparison.Ordinal))
                return set.PageOfPart(owner.Substring(5));
            if (owner.StartsWith("gate:", StringComparison.Ordinal))
            {
                string id = owner.Substring(5);
                int slash = id.LastIndexOf('/');
                return set.PageOfPart(slash < 0 ? id : id.Substring(0, slash));
            }
            if (owner.StartsWith("pin:", StringComparison.Ordinal))
                return set.PageOfPin(owner.Substring(4));
            if (owner.StartsWith("conn:", StringComparison.Ordinal))
                return set.PageOfConnector(owner.Substring(5));
            if (owner.StartsWith("inst:", StringComparison.Ordinal))
                return set.PageOfInstance(owner.Substring(5));
            if (owner.StartsWith("net:", StringComparison.Ordinal))
            {
                Net? n = Editor.FindNetByKey(owner.Substring(4));
                List<int> on = n == null ? new List<int>() : set.PagesOf(n);
                return on.Count == 0 ? -1 : on[0];
            }
            return -1;
        }

        /// <summary>
        /// Sets how the redrawn drawing is cut into pages (the card's page layout setting):
        /// as the original pages, or split automatically. Drawing only; it is still a change to
        /// the file, so it can be undone and is marked as needing saving. Allowed read-only.
        /// </summary>
        /// <param name="mode">
        /// The layout.
        /// </param>
        public void SetPageLayout(PageLayoutMode mode) => Editor.SetPageLayout(mode);

        // the drawn pages for the level shown: only the top level of a card with pages has them.
        // The page shown stays the same card page where it can.
        private void PlanPages()
        {
            if (Editor.LevelPath.Count > 0 || !CardPages.HasPages(Editor.Root))
            {
                _options.Pages = null;
                _options.Page = 0;
                return;
            }
            string? was = _options.Pages == null ? null : _options.Pages.Pages[_options.Page].Id;
            string? wasCard = _options.Pages == null ? Editor.Page : _options.Pages.Pages[_options.Page].CardPage;
            PageSet set = PageSet.Of(Editor.Root);
            int i = was == null ? -1 : set.IndexOf(was);
            if (i < 0 && wasCard != null)
                i = set.FirstOfCardPage(wasCard);
            _options.Pages = set;
            _options.Page = Math.Max(0, i);
            Editor.Page = set.Pages[_options.Page].CardPage;
        }
    }
}
