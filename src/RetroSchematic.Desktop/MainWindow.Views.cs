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
            if (view != ViewDrawing && view != ViewOriginal && view != ViewFront && view != ViewBack)
                throw new ArgumentException("No view called " + view + ".", nameof(view));
            if (_session == null && view != ViewDrawing)
                return;
            _view = view;
            Canvas.IsVisible = view == ViewDrawing;
            OriginalView.IsVisible = view == ViewOriginal;
            FrontView.IsVisible = view == ViewFront;
            BackView.IsVisible = view == ViewBack;
            PagePickerBar.IsVisible = view == ViewOriginal && PagePicker.ItemCount > 1;
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
            if (_session == null)
                return;
            Netlist n = _session.Editor.Root;
            string from = Signature(n) + "|" + _session.Editor.Files.Folder;
            if (from == _picturesFrom)
                return;
            _picturesFrom = from;

            _fillingPages = true;
            List<string> titles = new();
            for (int i = 0; n.Sources != null && i < n.Sources.Count; i++)
                titles.Add(PageTitle(n.Sources[i], i));
            PagePicker.ItemsSource = titles;
            PagePicker.SelectedIndex = titles.Count > 0 ? 0 : -1;
            _fillingPages = false;
            ShowPage(titles.Count > 0 ? 0 : -1);
            FrontView.SetPicture(PhotoBytes(n, BoardSide.Component, out string? fp), fp ?? "This sheet has no photo of the component side (File > Board info...)");
            BackView.SetPicture(PhotoBytes(n, BoardSide.Solder, out string? bp), bp ?? "This sheet has no photo of the solder side (File > Board info...)");
            PagePickerBar.IsVisible = _view == ViewOriginal && titles.Count > 1;
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

        // the first photo of a side, or null with the reason
        private byte[]? PhotoBytes(Netlist n, BoardSide side, out string? problem)
        {
            problem = null;
            for (int i = 0; n.Photos != null && i < n.Photos.Count; i++)
            {
                if (n.Photos[i].Side != side)
                    continue;
                byte[]? b = _session!.Editor.Files.Read(n.Photos[i].File);
                if (b == null)
                    problem = "The photo file " + n.Photos[i].File + " cannot be found.";
                return b;
            }
            return null;
        }

        private static string Signature(Netlist n)
        {
            System.Text.StringBuilder sb = new();
            for (int i = 0; n.Sources != null && i < n.Sources.Count; i++)
                sb.Append(n.Sources[i].File).Append('#').Append(n.Sources[i].PdfPage).Append('#').Append(n.Sources[i].Title).Append(';');
            for (int i = 0; n.Photos != null && i < n.Photos.Count; i++)
                sb.Append(n.Photos[i].File).Append('@').Append(n.Photos[i].Side).Append(';');
            return sb.ToString();
        }

        // the zoom buttons and Fit act on the view shown
        private ImageView? ShownPicture()
            => _view == ViewOriginal ? OriginalView : _view == ViewFront ? FrontView : _view == ViewBack ? BackView : null;
    }
}
