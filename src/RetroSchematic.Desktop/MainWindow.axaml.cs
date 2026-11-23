//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
// RetroSchematic - viewer for Norsk Data circuit diagrams
//

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// The main window: parts and signals on the left, the drawing in the middle, details of the
    /// picked item on the right. It only connects controls to a <see cref="SheetSession"/>; what
    /// a click means, what is lit and what the details say all come from the session.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<string> _partOwners = new();
        private readonly List<string> _netOwners = new();
        private SheetSession? _session;
        private bool _syncingLists;

        /// <summary>
        /// Creates the window and fills the theme menu.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            BuildThemeMenu();
            ShowStyleTicks(UiSettings.Instance.NdSymbols ? SymbolStyle.NorskData : SymbolStyle.Modern);
            if (UiSettings.Instance.LoadProblem != null)
                StatusText.Text = UiSettings.Instance.LoadProblem;
        }

        /// <summary>
        /// The open sheet, or null.
        /// </summary>
        public SheetSession? Session => _session;

        /// <summary>
        /// Opens a sheet file. The catalogue is found by walking up from the sheet to the folder
        /// that holds <c>catalog/</c> (the repository's <c>src</c> folder).
        /// </summary>
        /// <param name="path">
        /// The sheet file (a netlist JSON file).
        /// </param>
        public void OpenSheet(string path)
        {
            SymbolStyle style = UiSettings.Instance.NdSymbols ? SymbolStyle.NorskData : SymbolStyle.Modern;
            SheetSession s = new(path, style, FindCatalog(path));
            if (_session != null)
                _session.SelectionChanged -= OnSelectionChanged;
            _session = s;
            _session.SelectionChanged += OnSelectionChanged;
            Canvas.Session = s;
            FillLists();
            DetailsText.Text = "Click a part, gate or signal on the drawing.";
            SaveSvgItem.IsEnabled = true;
            Title = "RetroSchematic - " + s.Netlist.Sheet.Id + " " + s.Netlist.Sheet.Title;
            StatusText.Text = path + "  -  " + s.Netlist.Parts.Count + " parts, " + s.Netlist.Gates.Count + " gates, "
                + s.Netlist.Nets.Count + " signals";
            UiSettings.Instance.LastSheet = path;
            string? problem = UiSettings.Instance.Save();
            if (problem != null)
                StatusText.Text = problem;
        }

        private static string? FindCatalog(string sheetPath)
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(sheetPath));
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "catalog");
                if (Directory.Exists(Path.Combine(candidate, "chips")))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private void FillLists()
        {
            _syncingLists = true;
            _partOwners.Clear();
            _netOwners.Clear();
            List<string> parts = new();
            List<string> nets = new();
            Netlist n = _session!.Netlist;
            for (int i = 0; i < n.Parts.Count; i++)
            {
                parts.Add(n.Parts[i].Ref + "   " + n.Parts[i].Type);
                _partOwners.Add("part:" + n.Parts[i].Ref);
            }
            for (int i = 0; i < n.Nets.Count; i++)
            {
                nets.Add(n.Nets[i].Name);
                _netOwners.Add("net:" + n.Nets[i].Name);
            }
            PartsList.ItemsSource = parts;
            NetsList.ItemsSource = nets;
            _syncingLists = false;
        }

        private void BuildThemeMenu()
        {
            List<MenuItem> items = new();
            IReadOnlyList<ThemeDefinition> themes = ThemeManager.Instance.Themes;
            for (int i = 0; i < themes.Count; i++)
            {
                ThemeDefinition t = themes[i];
                MenuItem mi = new() { Header = t.DisplayName, Tag = t.Id, ToggleType = MenuItemToggleType.Radio };
                mi.IsChecked = ThemeManager.Instance.Current != null && ThemeManager.Instance.Current.Id == t.Id;
                mi.Click += OnThemePicked;
                items.Add(mi);
            }
            ThemeMenu.ItemsSource = items;
        }

        private void ShowStyleTicks(SymbolStyle style)
        {
            NdStyleItem.IsChecked = style == SymbolStyle.NorskData;
            ModernStyleItem.IsChecked = style == SymbolStyle.Modern;
        }

        private void SetStyle(SymbolStyle style)
        {
            UiSettings.Instance.NdSymbols = style == SymbolStyle.NorskData;
            UiSettings.Instance.Save();
            ShowStyleTicks(style);
            _session?.SetStyle(style);
        }

        private void OnSelectionChanged(SheetSession session, string owner)
        {
            DetailsText.Text = owner.Length == 0 ? "Nothing under the pointer." : session.Describe();
            // keep the lists in step with a pick made on the drawing
            _syncingLists = true;
            PartsList.SelectedIndex = _partOwners.IndexOf(owner);
            NetsList.SelectedIndex = _netOwners.IndexOf(owner);
            _syncingLists = false;
        }

        private void OnPartPicked(object? sender, SelectionChangedEventArgs e)
        {
            if (!_syncingLists && _session != null && PartsList.SelectedIndex >= 0)
                _session.Select(_partOwners[PartsList.SelectedIndex]);
        }

        private void OnNetPicked(object? sender, SelectionChangedEventArgs e)
        {
            if (!_syncingLists && _session != null && NetsList.SelectedIndex >= 0)
                _session.Select(_netOwners[NetsList.SelectedIndex]);
        }

        private void OnThemePicked(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string id)
            {
                ThemeManager.Instance.Apply(id);
                BuildThemeMenu();
            }
        }

        private async void OnOpen(object? sender, RoutedEventArgs e)
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open sheet",
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("ND netlist sheet") { Patterns = new[] { "*.json" } } },
            });
            if (files.Count == 0)
                return;
            string? path = files[0].TryGetLocalPath();
            if (path == null)
                return;
            try
            {
                OpenSheet(path);
            }
            catch (Exception ex) when (ex is IOException || ex is FormatException || ex is System.Text.Json.JsonException || ex is InvalidDataException)
            {
                StatusText.Text = "Could not open " + path + ": " + ex.Message;
            }
        }

        private async void OnSaveSvg(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save as SVG",
                DefaultExtension = "svg",
                SuggestedFileName = _session.Netlist.Sheet.Id + (_session.Style == SymbolStyle.NorskData ? "-nd" : "-modern") + ".svg",
            });
            string? path = file?.TryGetLocalPath();
            if (path == null)
                return;
            File.WriteAllText(path, SvgRenderer.Write(_session.Drawing));
            StatusText.Text = "Saved " + path;
        }

        private void OnExit(object? sender, RoutedEventArgs e) => Close();

        private void OnNdStyle(object? sender, RoutedEventArgs e) => SetStyle(SymbolStyle.NorskData);

        private void OnModernStyle(object? sender, RoutedEventArgs e) => SetStyle(SymbolStyle.Modern);

        private void OnFit(object? sender, RoutedEventArgs e) => Canvas.FitToWindow();
    }
}
