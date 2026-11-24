//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using RetroSchematic.Desktop.Controls;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// The four views of the middle of the window: the drawing, the original schematic page(s),
    /// and photos of the board's component side (front) and solder side (back). Each view zooms
    /// and pans like the drawing and keeps its own zoom. The pictures come from the sheet's
    /// "sources" and "photos" (docs/FORMAT.md), held inside a .rsx or linked from a .rsc.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// The drawing (F5).
        /// </summary>
        public const string ViewDrawing = "drawing";

        /// <summary>
        /// The original schematic page (F6).
        /// </summary>
        public const string ViewOriginal = "original";

        /// <summary>
        /// The photo of the component side (F7).
        /// </summary>
        public const string ViewFront = "front";

        /// <summary>
        /// The photo of the solder side (F8).
        /// </summary>
        public const string ViewBack = "back";

        // the resolution a PDF source page is shown at (packing a .rsx uses 300 dpi)
        private const int ShowDpi = 150;

        private string _view = ViewDrawing;

        // the files the pictures were loaded from, so an edit that does not touch them does
        // not decode them again
        private string _picturesFrom = "";
        private bool _fillingPages;

        // which photo of each side is shown, among that side's photos (0 = the first)
        private int _frontPick;
        private int _backPick;

        /// <summary>
        /// The view shown now: drawing, original, front or back.
        /// </summary>
        public string CurrentView => _view;

        /// <summary>
        /// The picture view of the original page.
        /// </summary>
        public ImageView Original => OriginalView;

        /// <summary>
        /// The picture view of the component side.
        /// </summary>
        public ImageView Front => FrontView;

        /// <summary>
        /// The picture view of the solder side.
        /// </summary>
        public ImageView Back => BackView;

        /// <summary>
        /// The page picker for a sheet with several original pages.
        /// </summary>
        public ComboBox Pages => PagePicker;

        /// <summary>
        /// Shows one of the four views. Each keeps its own zoom and place.
        /// </summary>
        /// <param name="view">
        /// drawing, original, front or back.
        /// </param>
        public void ShowView(string view)
        {
            if (view != ViewDrawing && view != ViewOriginal && view != ViewFront && view != ViewBack && view != ViewOverview)
                throw new ArgumentException("No view called " + view + ".", nameof(view));
            if (_session == null && view != ViewDrawing)
                return;
            _view = view;
            Canvas.IsVisible = view == ViewDrawing;
            OriginalView.IsVisible = view == ViewOriginal;
            FrontView.IsVisible = view == ViewFront;
            BackView.IsVisible = view == ViewBack;
            OverviewView.IsVisible = view == ViewOverview;
            if (view == ViewOverview)
                FillOverview();
            FillPageTabs();
            PagePickerBar.IsVisible = view == ViewOriginal && PagePicker.ItemCount > 1;
            ReadBar.IsVisible = _session != null && view == ViewOriginal;
            FillPhotoPicker();
            TbViewDrawing.IsChecked = view == ViewDrawing;
            TbViewOriginal.IsChecked = view == ViewOriginal;
            TbViewFront.IsChecked = view == ViewFront;
            TbViewBack.IsChecked = view == ViewBack;
        }

        private void OnViewButton(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton b && b.Tag is string view)
                ShowView(view);
        }

        private void OnViewItem(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.Tag is string view)
                ShowView(view);
        }

        private void OnPagePicked(object? sender, SelectionChangedEventArgs e)
        {
            if (!_fillingPages)
                ShowPage(PagePicker.SelectedIndex);
        }

        /// <summary>
        /// Which original page the Original view shows (0 = first), or -1.
        /// </summary>
        public int OriginalPageShown => PagePicker.SelectedIndex;

        /// <summary>
        /// Loads the original pages and the photos of the open sheet into their views (again
        /// only when the files named have changed).
        /// </summary>
        public void LoadPictures()
        {
            bool open = _session != null;
            TbViewOriginal.IsEnabled = open;
            TbViewFront.IsEnabled = open;
            TbViewBack.IsEnabled = open;
            BoardInfoItem.IsEnabled = open;
            ImportCatalogueItem.IsEnabled = open;
            if (_session == null)
                return;
            Netlist n = _session.Editor.Root;
            string from = Signature(n) + "|" + _session.Editor.Files.Folder;
            if (from == _picturesFrom)
            {
                // the parts or the grid may have changed even when the pictures did not
                RefreshPcb();
                return;
            }
            _picturesFrom = from;

            _fillingPages = true;
            List<string> titles = new();
            for (int i = 0; n.Sources != null && i < n.Sources.Count; i++)
                titles.Add(PageTitle(n.Sources[i], i));
            PagePicker.ItemsSource = titles;
            PagePicker.SelectedIndex = titles.Count > 0 ? 0 : -1;
            _fillingPages = false;
            ShowPage(titles.Count > 0 ? 0 : -1);
            // the original page of the drawn page shown, unless picked by hand
            FollowOriginal();
            _frontPick = 0;
            _backPick = 0;
            ShowPhoto(BoardSide.Component);
            ShowPhoto(BoardSide.Solder);
            PagePickerBar.IsVisible = _view == ViewOriginal && titles.Count > 1;
            FillPhotoPicker();
            RefreshPcb();
        }

        /// <summary>
        /// The photo picker for a side with several photos (the old and the new board, say).
        /// </summary>
        public ComboBox PhotoChoice => PhotoPicker;

        /// <summary>
        /// Which photo of a side its view shows: 0 = the first photo of that side in the sheet.
        /// </summary>
        /// <param name="side">
        /// The side.
        /// </param>
        /// <returns>
        /// The place among that side's photos.
        /// </returns>
        public int PhotoShown(BoardSide side) => side == BoardSide.Component ? _frontPick : _backPick;

        /// <summary>
        /// Shows another photo of a side in its view.
        /// </summary>
        /// <param name="side">
        /// The side.
        /// </param>
        /// <param name="which">
        /// The place among that side's photos, 0 first.
        /// </param>
        public void PickPhoto(BoardSide side, int which)
        {
            if (_session == null || which < 0 || which >= PhotosOf(_session.Editor.Root, side).Count)
                return;
            if (side == BoardSide.Component)
                _frontPick = which;
            else
                _backPick = which;
            ShowPhoto(side);
            FillPhotoPicker();
            RefreshPcb();
        }

        /// <summary>
        /// The name of a photo in the picker: its caption, else its file, and the variant.
        /// </summary>
        /// <param name="p">
        /// The photo.
        /// </param>
        /// <returns>
        /// The text.
        /// </returns>
        public static string PhotoTitle(BoardPhoto p)
        {
            string t = p.Caption ?? Path.GetFileName(p.File);
            if (p.Variant != null && p.Caption == null)
                t += " (" + p.Variant + ")";
            return t;
        }

        // the places in the sheet's list of the photos of one side
        private static List<int> PhotosOf(Netlist n, BoardSide side)
        {
            List<int> list = new();
            for (int i = 0; n.Photos != null && i < n.Photos.Count; i++)
            {
                if (n.Photos[i].Side == side)
                    list.Add(i);
            }
            return list;
        }

        private void ShowPhoto(BoardSide side)
        {
            ImageView view = side == BoardSide.Component ? FrontView : BackView;
            byte[]? b = PhotoBytes(_session!.Editor.Root, side, PhotoShown(side), out string? problem);
            view.SetPicture(b, problem ?? (side == BoardSide.Component
                ? "This sheet has no photo of the component side (File > Board info...)"
                : "This sheet has no photo of the solder side (File > Board info...)"));
        }

        // the picker lists the photos of the side shown; hidden when it has one or none
        private void FillPhotoPicker()
        {
            if (_session == null || (_view != ViewFront && _view != ViewBack))
            {
                PhotoPickerBar.IsVisible = false;
                return;
            }
            BoardSide side = _view == ViewFront ? BoardSide.Component : BoardSide.Solder;
            Netlist n = _session.Editor.Root;
            List<int> mine = PhotosOf(n, side);
            List<string> titles = new();
            for (int i = 0; i < mine.Count; i++)
                titles.Add(PhotoTitle(n.Photos![mine[i]]));
            _fillingPages = true;
            PhotoPicker.ItemsSource = titles;
            PhotoPicker.SelectedIndex = titles.Count == 0 ? -1 : PhotoShown(side);
            _fillingPages = false;
            PhotoPickerBar.IsVisible = titles.Count > 1;
        }

        private void OnPhotoPicked(object? sender, SelectionChangedEventArgs e)
        {
            if (_fillingPages || PhotoPicker.SelectedIndex < 0)
                return;
            PickPhoto(_view == ViewFront ? BoardSide.Component : BoardSide.Solder, PhotoPicker.SelectedIndex);
        }

        /// <summary>
        /// The title of a source page in the picker: its own title, else its file and page.
        /// </summary>
        /// <param name="s">
        /// The page.
        /// </param>
        /// <param name="index">
        /// Its place in the list, 0 first.
        /// </param>
        /// <returns>
        /// The text.
        /// </returns>
        public static string PageTitle(SourcePage s, int index)
        {
            string t = s.Title ?? Path.GetFileName(s.File);
            if (s.PdfPage != null)
                t += " (PDF page " + s.PdfPage.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
            return (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + ". " + t;
        }

        private void ShowPage(int index)
        {
            Netlist? n = _session?.Editor.Root;
            if (n?.Sources == null || index < 0 || index >= n.Sources.Count)
            {
                OriginalView.SetPicture(null, "This sheet has no original page (File > Board info... adds one)");
                return;
            }
            string? problem = null;
            byte[]? bytes = PictureOf(n.Sources[index], ref problem);
            OriginalView.SetPicture(bytes, problem ?? "");
            ShowReading();
        }

        // a source page as picture bytes: a PDF page is rendered, a picture is used as it is
        private byte[]? PictureOf(SourcePage s, ref string? problem)
        {
            byte[]? data = _session!.Editor.Files.Read(s.File);
            if (data == null)
            {
                problem = "The page file " + s.File + " cannot be found.";
                return null;
            }
            if (!SheetAttachments.IsPdf(s.File))
                return data;
            if (SheetEditor.Pdf == null || s.PdfPage == null)
            {
                problem = "The PDF page of " + s.File + " cannot be shown.";
                return null;
            }
            try
            {
                return SheetEditor.Pdf(data, s.PdfPage.Value, ShowDpi);
            }
            catch (IOException ex)
            {
                problem = ex.Message;
                return null;
            }
        }

        // one photo of a side (0 = its first), or null with the reason
        private byte[]? PhotoBytes(Netlist n, BoardSide side, int which, out string? problem)
        {
            problem = null;
            List<int> mine = PhotosOf(n, side);
            if (mine.Count == 0)
                return null;
            BoardPhoto p = n.Photos![mine[Math.Clamp(which, 0, mine.Count - 1)]];
            byte[]? b = _session!.Editor.Files.Read(p.File);
            if (b == null)
                problem = "The photo file " + p.File + " cannot be found.";
            return b;
        }

        private static string Signature(Netlist n)
        {
            System.Text.StringBuilder sb = new();
            for (int i = 0; n.Sources != null && i < n.Sources.Count; i++)
                sb.Append(n.Sources[i].File).Append('#').Append(n.Sources[i].PdfPage).Append('#').Append(n.Sources[i].Title).Append(';');
            for (int i = 0; n.Photos != null && i < n.Photos.Count; i++)
                sb.Append(n.Photos[i].File).Append('@').Append(n.Photos[i].Side).Append('#').Append(n.Photos[i].Caption).Append(';');
            return sb.ToString();
        }

        // the zoom buttons and Fit act on the view shown
        private ImageView? ShownPicture()
            => _view == ViewOriginal ? OriginalView : _view == ViewFront ? FrontView : _view == ViewBack ? BackView : null;
    }
}
