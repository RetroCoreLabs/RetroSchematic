//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RetroSchematic.Model;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// Card library: every card file (.rsc or .rsx) in the repository's <c>sheets</c> folder,
    /// filtered by machine family. The families come from the files themselves (their
    /// <c>sheet.machine</c> field), so a new family such as ND-500 appears as soon as one file
    /// names it. Double-click a card, or pick it and press Open, to open it in the main window.
    /// </summary>
    public sealed class CardLibraryWindow : Window
    {
        private readonly MainWindow _main;
        private readonly ComboBox _family = new() { Width = 220 };
        private readonly ListBox _list = new() { FontFamily = new FontFamily("Consolas,Menlo,monospace") };
        private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 6, 0, 0) };
        private readonly List<CardEntry> _all = new();
        private readonly List<CardEntry> _shown = new();
        private List<string> _families = new();

        /// <summary>
        /// Opens over the main window and reads the library.
        /// </summary>
        /// <param name="main">
        /// The main window, where a picked card is opened.
        /// </param>
        public CardLibraryWindow(MainWindow main)
        {
            _main = main;
            Title = "Card library";
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://RetroSchematic/Assets/RetroSchematic.ico")));
            Width = 1000;
            Height = 600;
            this.Bind(BackgroundProperty, this.GetResourceObservable("FluentBackgroundPrimaryBrush"));

            _family.SelectionChanged += (s, e) => Fill();
            Button open = new() { Content = "Open" };
            open.Click += (s, e) => OpenPicked();
            Button reload = new() { Content = "Read the library again" };
            reload.Click += (s, e) => Load();
            StackPanel bar = new() { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Avalonia.Thickness(0, 0, 0, 8) };
            bar.Children.Add(new TextBlock { Text = "Family:", VerticalAlignment = VerticalAlignment.Center });
            bar.Children.Add(_family);
            bar.Children.Add(open);
            bar.Children.Add(reload);
            _list.DoubleTapped += (s, e) => OpenPicked();

            TextBlock head = new() { Text = "Family     Card    Title                                    Pages  Parts  File", FontFamily = new FontFamily("Consolas,Menlo,monospace"), Margin = new Avalonia.Thickness(4, 0, 0, 4) };
            Grid grid = new() { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"), Margin = new Avalonia.Thickness(10) };
            Grid.SetRow(bar, 0);
            Grid.SetRow(head, 1);
            Grid.SetRow(_list, 2);
            Grid.SetRow(_status, 3);
            grid.Children.Add(bar);
            grid.Children.Add(head);
            grid.Children.Add(_list);
            grid.Children.Add(_status);
            Content = grid;
            Load();
        }

        /// <summary>The library folder read, or null when none was found.</summary>
        public string? Folder { get; private set; }

        /// <summary>Every card read, before the family filter.</summary>
        public IReadOnlyList<CardEntry> All => _all;

        /// <summary>The cards in the list, as shown.</summary>
        public IReadOnlyList<CardEntry> Shown => _shown;

        /// <summary>The families in the filter box after "All families".</summary>
        public IReadOnlyList<string> Families => _families;

        /// <summary>The status line text.</summary>
        public string Status => _status.Text ?? "";

        /// <summary>
        /// Picks a family by name, or all families with null.
        /// </summary>
        /// <param name="family">
        /// The family, as written in the files.
        /// </param>
        public void PickFamily(string? family) => _family.SelectedIndex = family == null ? 0 : _families.IndexOf(family) + 1;

        /// <summary>
        /// Picks a line in the list.
        /// </summary>
        /// <param name="index">
        /// The index in <see cref="Shown"/>.
        /// </param>
        public void Pick(int index) => _list.SelectedIndex = index;

        /// <summary>
        /// Reads the library: the <c>sheets</c> folder beside the chip catalogue folder (the
        /// repository's <c>src/sheets</c>, or the copy beside the program).
        /// </summary>
        public void Load()
        {
            _all.Clear();
            List<string> problems = new();
            string? catalog = _main.Session?.Editor.Catalog?.Folder ?? ChipCatalog.Find(AppContext.BaseDirectory);
            // the catalogue folder is <root>/catalog; the library is beside it in <root>/sheets
            Folder = CardLibrary.FolderBeside(catalog);
            if (Folder != null)
                _all.AddRange(CardLibrary.Scan(Folder, problems));
            string keep = _family.SelectedItem as string ?? "";
            _families = CardLibrary.Families(_all);
            List<string> items = new() { "All families" };
            items.AddRange(_families);
            _family.ItemsSource = items;
            int at = items.IndexOf(keep);
            _family.SelectedIndex = at < 0 ? 0 : at;
            _status.Text = (Folder == null ? "No library folder found. " : Folder + ": ") + _all.Count + " card file(s) in "
                + _families.Count + " famil" + (_families.Count == 1 ? "y" : "ies") + "."
                + (problems.Count == 0 ? "" : " Not read: " + string.Join("; ", problems));
            Fill();
        }

        private void Fill()
        {
            _shown.Clear();
            int pick = _family.SelectedIndex;
            string? family = pick <= 0 || pick > _families.Count ? null : _families[pick - 1];
            List<string> lines = new();
            for (int i = 0; i < _all.Count; i++)
            {
                CardEntry c = _all[i];
                if (family != null && c.Family != family)
                    continue;
                _shown.Add(c);
                string title = c.BoardTitle != null && c.BoardTitle != c.Title ? c.Title + " (" + c.BoardTitle + ")" : c.Title;
                if (title.Length > 40)
                    title = title.Substring(0, 39) + "~";
                lines.Add(string.Format(CultureInfo.InvariantCulture, "{0,-10} {1,-7} {2,-40} {3,5}  {4,5}  {5}",
                    c.Family, c.Id, title, c.Pages, c.Parts, Folder != null ? Path.GetRelativePath(Folder, c.File) : c.File));
            }
            _list.ItemsSource = lines;
        }

        /// <summary>
        /// Opens the picked card in the main window.
        /// </summary>
        public void OpenPicked()
        {
            int i = _list.SelectedIndex;
            if (i < 0 || i >= _shown.Count)
                return;
            _main.OpenSheet(_shown[i].File);
        }
    }
}
