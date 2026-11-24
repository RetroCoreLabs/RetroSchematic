//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// A card with pages (format version 3): the page tabs above the drawing, Page Up / Page
    /// Down, the card overview (its pages and the board photos), the Original view following
    /// the drawn page, and the menu items that convert a sheet, add a page and move a part to a
    /// page. The pages themselves are worked out in the library (<see cref="PageSet"/>,
    /// <see cref="SheetSession"/>); the window only shows them.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// The card overview (F4): the card's pages and the board photos.
        /// </summary>
        public const string ViewOverview = "overview";

        // the width the board photos are decoded to in the overview: small, so a card with many
        // photos does not hold every full-size picture in memory
        private const int OverviewPhotoWidth = 420;

        private SheetSession? _pagesOf;
        private bool _pageKeys;
        private string _overviewFrom = "";

        /// <summary>
        /// The page tabs (the overview tab first, then one per drawn page), for tests.
        /// </summary>
        public Panel PageTabStrip => PageTabs;

        /// <summary>
        /// The overview's content, for tests.
        /// </summary>
        public Panel Overview => OverviewPanel;

        /// <summary>
        /// True when the Original view is picked by hand and does not follow the drawn page.
        /// </summary>
        public bool OriginalPickedByHand => PageByHand.IsChecked == true;

        /// <summary>
        /// Shows a drawn page and the drawing (a page tab, the overview's page list).
        /// </summary>
        /// <param name="index">
        /// The drawn page's place, 0 = first.
        /// </param>
        public void ShowDrawnPage(int index)
        {
            if (_session == null)
                return;
            _session.SetPage(index);
            ShowView(ViewDrawing);
            FillPageTabs();
            Canvas.Focus();
        }

        // called when a session is shown: listen to its page changes, fill the tabs
        private void AttachPages()
        {
            if (_pagesOf != null)
                _pagesOf.PageChanged -= OnPageChanged;
            _pagesOf = _session;
            if (_session != null)
                _session.PageChanged += OnPageChanged;
            if (!_pageKeys)
            {
                // Page Up / Page Down anywhere a list or a text box has not used them
                AddHandler(KeyDownEvent, OnPageKeys, RoutingStrategies.Bubble);
                _pageKeys = true;
            }
            _overviewFrom = "";
            FillPageTabs();
            FollowOriginal();
        }

        private void OnPageKeys(object? sender, KeyEventArgs e)
        {
            if (e.Handled || _session == null || _session.PageCount < 2 || e.KeyModifiers != KeyModifiers.None)
                return;
            if (e.Key == Key.PageDown || e.Key == Key.PageUp)
            {
                _session.StepPage(e.Key == Key.PageDown ? 1 : -1);
                if (_view == ViewOverview)
                    ShowView(ViewDrawing);
                e.Handled = true;
            }
        }

        private void OnPageChanged(SheetSession session)
        {
            FillPageTabs();
            FollowOriginal();
            // another page is another drawing: show all of it
            Canvas.FitToWindow();
            // a page change is a step in the Back / Forward history
            Record(session);
            StatusText.Text = "Page " + (session.CurrentPage?.Id ?? "") + ": " + (session.CurrentPage?.Title ?? "");
        }

        private void OnNextPage(object? sender, RoutedEventArgs e) => _session?.StepPage(1);

        private void OnPreviousPage(object? sender, RoutedEventArgs e) => _session?.StepPage(-1);

        private void OnPageByHand(object? sender, RoutedEventArgs e) => FollowOriginal();

        // the Original view shows the source page the drawn page was read from, unless the
        // page picker is set by hand
        private void FollowOriginal()
        {
            if (_session == null || PageByHand.IsChecked == true)
                return;
            int? source = _session.CurrentPage?.Source;
            if (source == null || source < 1 || source > PagePicker.ItemCount || PagePicker.SelectedIndex == source - 1)
                return;
            PagePicker.SelectedIndex = source.Value - 1;
        }

        /// <summary>
        /// Fills the page tabs and the page menu items for the card shown; hidden for a card
        /// without pages (and inside a block).
        /// </summary>
        public void FillPageTabs()
        {
            PageTabs.Children.Clear();
            PageSet? set = _session?.Pages;
            bool paged = _session != null && CardPages.HasPages(_session.Editor.Root);
            PageTabs.IsVisible = set != null;
            if (set != null)
            {
                ToggleButton overview = Tab("Card", "The card overview: its pages and the board photos (F4)", _view == ViewOverview);
                overview.Click += (s, a) => ShowView(ViewOverview);
                PageTabs.Children.Add(overview);
                for (int i = 0; i < set.Pages.Count; i++)
                {
                    int index = i;
                    DrawnPage page = set.Pages[i];
                    string title = page.Title.Length > 28 ? page.Title.Substring(0, 27) + "..." : page.Title;
                    ToggleButton tab = Tab(page.Id + "  " + title, "Page " + page.Id + ": " + page.Title + " (Page Up / Page Down)",
                        _view != ViewOverview && index == _session!.PageIndex);
                    tab.Click += (s, a) => ShowDrawnPage(index);
                    PageTabs.Children.Add(tab);
                }
            }
            bool editing = _session != null && !_session.Editor.ReadOnly;
            ConvertPagesItem.IsEnabled = editing && !paged && _session!.Editor.LevelPath.Count == 0;
            AddPageItem.IsEnabled = editing && _session!.Editor.LevelPath.Count == 0;
            PartToPageItem.IsEnabled = editing && paged && _session!.Selected.StartsWith("part:", StringComparison.Ordinal);
            NextPageItem.IsEnabled = _session != null && _session.PageCount > 1;
            PreviousPageItem.IsEnabled = NextPageItem.IsEnabled;
            SplitPagesItem.IsEnabled = paged;
            SplitPagesItem.IsChecked = paged && _session!.Editor.Root.PageLayout == PageLayoutMode.Split;
            OverviewItem.IsEnabled = _session != null;
            if (_view == ViewOverview)
                FillOverview();
        }

        private static ToggleButton Tab(string text, string tip, bool on)
        {
            ToggleButton t = new() { Content = text, IsChecked = on, Padding = new Avalonia.Thickness(10, 3) };
            t.Classes.Add("toolbar-button");
            ToolTip.SetTip(t, tip);
            return t;
        }

        private void OnToggleSplitPages(object? sender, RoutedEventArgs e)
        {
            if (_session == null || !CardPages.HasPages(_session.Editor.Root))
                return;
            bool split = _session.Editor.Root.PageLayout != PageLayoutMode.Split;
            _session.SetPageLayout(split ? PageLayoutMode.Split : PageLayoutMode.AsOriginal);
            FillPageTabs();
            StatusText.Text = split
                ? "Crowded pages are split automatically: " + _session.PageCount.ToString(CultureInfo.InvariantCulture) + " drawn pages."
                : "One drawn page per original page.";
        }

        /// <summary>
        /// Fills the card overview: the card's title, its pages (click one to draw it), and its
        /// board photos, small. Filled again only when the card's pages or photos change.
        /// </summary>
        public void FillOverview()
        {
            if (_session == null)
                return;
            Netlist n = _session.Editor.Root;
            PageSet set = PageSet.Of(n);
            string from = Signature(n) + "|" + NetlistReader.ToJson(n.Pages) + "|" + set.Pages.Count.ToString(CultureInfo.InvariantCulture)
                + "|" + n.Parts.Count.ToString(CultureInfo.InvariantCulture);
            if (from == _overviewFrom)
                return;
            _overviewFrom = from;
            OverviewPanel.Children.Clear();
            OverviewPanel.Children.Add(Text(n.Sheet.Id + "  " + n.Sheet.Title, true));
            if (n.Board?.Title != null || n.Board?.PartNumber != null)
                OverviewPanel.Children.Add(Text("Board: " + (n.Board!.Title ?? "") + (n.Board.PartNumber == null ? "" : "  (part " + n.Board.PartNumber + ")"), false));

            OverviewPanel.Children.Add(Text(CardPages.HasPages(n)
                ? "Pages (" + n.Pages!.Count.ToString(CultureInfo.InvariantCulture) + ")"
                : "Pages: this sheet has no pages list (File > Convert to a card with pages)", true));
            Dictionary<string, string> partPages = CardPages.PartPages(n);
            for (int p = 0; n.Pages != null && p < n.Pages.Count; p++)
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
                string line = page.Id + ".  " + (page.Title ?? "(no title)");
                if (page.SheetNumber != null)
                    line += "   sheet " + page.SheetNumber;
                line += "   " + chips.ToString(CultureInfo.InvariantCulture) + " chips, " + parts.ToString(CultureInfo.InvariantCulture) + " parts";
                if (page.Source != null && n.Sources != null && page.Source <= n.Sources.Count)
                    line += "   read from " + PageTitle(n.Sources[page.Source.Value - 1], page.Source.Value - 1);
                Button b = new() { Content = line, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
                b.Classes.Add("fluent-button");
                int first = set.FirstOfCardPage(page.Id);
                b.Click += (s, a) => ShowDrawnPage(Math.Max(0, first));
                OverviewPanel.Children.Add(b);
                // a split page: its drawn pages under it
                for (int d = 0; d < set.Pages.Count; d++)
                {
                    if (set.Pages[d].CardPage != page.Id || set.Pages[d].Id == page.Id)
                        continue;
                    int index = d;
                    Button sub = new() { Content = "      drawn page " + set.Pages[d].Id + ": " + set.Pages[d].Parts.Count.ToString(CultureInfo.InvariantCulture) + " parts", HorizontalAlignment = HorizontalAlignment.Left };
                    sub.Classes.Add("fluent-button");
                    sub.Click += (s, a) => ShowDrawnPage(index);
                    OverviewPanel.Children.Add(sub);
                }
            }

            OverviewPanel.Children.Add(Text("Board photos", true));
            WrapPanel photos = new() { Orientation = Orientation.Horizontal };
            for (int i = 0; n.Photos != null && i < n.Photos.Count; i++)
            {
                BoardPhoto ph = n.Photos[i];
                StackPanel cell = new() { Margin = new Avalonia.Thickness(0, 0, 12, 12), Spacing = 4, Width = OverviewPhotoWidth };
                byte[]? bytes = _session.Editor.Files.Read(ph.File);
                Bitmap? bmp = null;
                if (bytes != null)
                {
                    try
                    {
                        using MemoryStream ms = new(bytes);
                        bmp = Bitmap.DecodeToWidth(ms, OverviewPhotoWidth);
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is IOException || ex is NotSupportedException)
                    {
                        // the photo cannot be decoded: its caption still says which it is
                        bmp = null;
                    }
                }
                if (bmp != null)
                    cell.Children.Add(new Image { Source = bmp, Width = OverviewPhotoWidth, Stretch = Stretch.Uniform });
                else
                    cell.Children.Add(Text("(the photo file " + ph.File + " cannot be shown)", false));
                // the caption usually names the side already
                string side = ph.Side == BoardSide.Component ? "Component side" : "Solder side";
                string caption = PhotoTitle(ph);
                cell.Children.Add(Text(caption.StartsWith(side, StringComparison.OrdinalIgnoreCase) ? caption : side + ": " + caption, false));
                photos.Children.Add(cell);
            }
            if (photos.Children.Count == 0)
                OverviewPanel.Children.Add(Text("No photos (File > Board info... adds them).", false));
            else
                OverviewPanel.Children.Add(photos);
        }

        private static TextBlock Text(string text, bool header)
        {
            TextBlock t = new() { Text = text, TextWrapping = TextWrapping.Wrap };
            t.Classes.Add(header ? "fluent-text-header" : "fluent-text");
            return t;
        }

        private void OnConvertToPages(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            try
            {
                StatusText.Text = _session.Editor.ConvertToPages()
                    ? "The card now has pages (format version 3): everything is on page 1. Save to keep it."
                    : "The card has pages already.";
            }
            catch (SheetEditException ex)
            {
                StatusText.Text = ex.Message;
            }
            FillPageTabs();
            ShowEditState();
        }

        private async void OnAddPage(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            string? title = await AskLine("New page", "The new page's title, as in its title block:", "");
            if (title == null)
                return;
            try
            {
                string id = _session.Editor.AddPage(title);
                int index = _session.Pages?.FirstOfCardPage(id) ?? -1;
                if (index >= 0)
                    ShowDrawnPage(index);
                StatusText.Text = "Added page " + id + ". Chips placed now go on it.";
            }
            catch (SheetEditException ex)
            {
                StatusText.Text = ex.Message;
            }
            ShowEditState();
        }

        private async void OnPartToPage(object? sender, RoutedEventArgs e)
        {
            if (_session == null || !_session.Selected.StartsWith("part:", StringComparison.Ordinal))
                return;
            string reference = _session.Selected.Substring(5);
            string? page = await AskLine("Put " + reference + " on a page", "The id of the page to put " + reference + " on:", "");
            if (page == null)
                return;
            try
            {
                _session.Editor.SetPartPage(reference, page);
                StatusText.Text = reference + " is on page " + page + " now.";
            }
            catch (SheetEditException ex)
            {
                StatusText.Text = ex.Message;
            }
            ShowEditState();
        }

        // one line of text from a small dialog, or null when cancelled
        private async Task<string?> AskLine(string title, string question, string start)
        {
            TextBox box = Box(start, "");
            Window dialog = new() { Title = title, Width = 420, SizeToContent = SizeToContent.Height };
            dialog.Bind(BackgroundProperty, dialog.GetResourceObservable("FluentBackgroundPrimaryBrush"));
            string? answer = null;
            StackPanel p = new() { Margin = new Avalonia.Thickness(14), Spacing = 8 };
            p.Children.Add(new TextBlock { Text = question, TextWrapping = TextWrapping.Wrap });
            p.Children.Add(box);
            StackPanel buttons = new() { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
            Button ok = new() { Content = "OK", IsDefault = true };
            ok.Classes.Add("fluent-button");
            ok.Click += (s, a) => { answer = box.Text; dialog.Close(); };
            Button cancel = new() { Content = "Cancel", IsCancel = true };
            cancel.Classes.Add("fluent-button");
            cancel.Click += (s, a) => dialog.Close();
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            p.Children.Add(buttons);
            dialog.Content = p;
            await dialog.ShowDialog(this);
            return string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
        }
    }
}
