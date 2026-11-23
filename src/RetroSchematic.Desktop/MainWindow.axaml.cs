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
    /// The main window: search, parts and signals on the left, the drawing in the middle, details
    /// of the picked item on the right. It only connects controls to a <see cref="SheetSession"/>
    /// and its <see cref="SheetEditor"/>; what a click means, what is lit, what the details say
    /// and every change to the sheet all come from the library.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly List<string> _partOwners = new();
        private readonly List<string> _netOwners = new();
        private readonly List<string> _foundOwners = new();
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
            Canvas.ToolMessage += OnToolMessage;
            OpenDemoItem.IsEnabled = SheetFiles.FindDemo(AppContext.BaseDirectory) != null;
            if (UiSettings.Instance.LoadProblem != null)
                StatusText.Text = UiSettings.Instance.LoadProblem;
        }

        /// <summary>
        /// The open sheet, or null.
        /// </summary>
        public SheetSession? Session => _session;

        /// <summary>
        /// Opens a sheet file, read-only. The catalogue is found by walking up from the sheet to
        /// the folder that holds <c>catalog/</c> (the repository's <c>src</c> folder).
        /// </summary>
        /// <param name="path">
        /// The sheet file (a netlist JSON file).
        /// </param>
        public void OpenSheet(string path)
        {
            SymbolStyle style = UiSettings.Instance.NdSymbols ? SymbolStyle.NorskData : SymbolStyle.Modern;
            Show(new SheetSession(path, style, ChipCatalog.Find(path)));
            StatusText.Text = path + "  -  " + _session!.Netlist.Parts.Count + " parts, " + _session.Netlist.Gates.Count + " gates, "
                + _session.Netlist.Nets.Count + " signals";
            UiSettings.Instance.LastSheet = path;
            string? problem = UiSettings.Instance.Save();
            if (problem != null)
                StatusText.Text = problem;
        }

        /// <summary>
        /// Starts a new, empty design, already in edit mode. The catalogue is the one above the
        /// program's folder.
        /// </summary>
        public void NewSheet()
        {
            SymbolStyle style = UiSettings.Instance.NdSymbols ? SymbolStyle.NorskData : SymbolStyle.Modern;
            Show(new SheetSession(SheetEditor.NewSheet("NEW", "New design"), null, style, ChipCatalog.Find(AppContext.BaseDirectory)));
            SetEditMode(true);
            StatusText.Text = _session!.Editor.Catalog == null
                ? "New sheet - no chip catalogue found, so no chips can be placed."
                : "New sheet. Pick a chip to place.";
        }

        /// <summary>
        /// Switches between read-only (every change refused) and edit mode.
        /// </summary>
        /// <param name="on">
        /// True for edit mode.
        /// </param>
        public void SetEditMode(bool on)
        {
            if (_session == null)
                return;
            _session.Editor.ReadOnly = !on;
            if (!on && Canvas.Tool != null)
                Canvas.Tool.Cancel();
            ShowEditState();
        }

        private void Show(SheetSession s)
        {
            if (_session != null)
            {
                _session.SelectionChanged -= OnSelectionChanged;
                _session.DrawingChanged -= OnDrawingChanged;
            }
            _session = s;
            _session.SelectionChanged += OnSelectionChanged;
            _session.DrawingChanged += OnDrawingChanged;
            _session.SetShowPinNumbers(PinNumbersItem.IsChecked);
            _session.SetShowBoardPositions(BoardPositionsItem.IsChecked);
            Canvas.Session = s;
            FillLists();
            FillChipBox();
            DetailsText.Text = "Click a part, gate, signal or pin on the drawing.";
            SaveAsItem.IsEnabled = true;
            SaveSvgItem.IsEnabled = true;
            ExportPngItem.IsEnabled = true;
            EditModeItem.IsEnabled = true;
            ShowEditState();
        }

        private void ShowEditState()
        {
            bool editing = _session != null && !_session.Editor.ReadOnly;
            EditModeItem.IsChecked = editing;
            ModeText.Text = editing ? "EDIT MODE" : "Read-only";
            ChipBox.IsEnabled = editing && _session!.Editor.Catalog != null;
            UndoItem.IsEnabled = editing && _session!.Editor.CanUndo;
            RedoItem.IsEnabled = editing && _session!.Editor.CanRedo;
            DeleteItem.IsEnabled = editing;
            SaveItem.IsEnabled = _session != null && _session.SheetPath != null;
            if (_session != null)
                Title = "RetroSchematic - " + _session.Netlist.Sheet.Id + " " + _session.Netlist.Sheet.Title
                    + (_session.Editor.Modified ? " (changed)" : "") + (editing ? " [edit]" : "");
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
            FillFound();
        }

        private void FillFound()
        {
            _syncingLists = true;
            _foundOwners.Clear();
            List<string> shown = new();
            if (_session != null)
            {
                List<string> found = _session.Search(SearchBox.Text ?? "");
                for (int i = 0; i < found.Count; i++)
                {
                    _foundOwners.Add(found[i]);
                    shown.Add(found[i].Replace(":", "  ", StringComparison.Ordinal));
                }
            }
            FoundList.ItemsSource = shown;
            _syncingLists = false;
        }

        private void FillChipBox()
        {
            List<string> types = new() { "(none)" };
            if (_session?.Editor.Catalog != null)
                types.AddRange(_session.Editor.Catalog.Types());
            ChipBox.ItemsSource = types;
            ChipBox.SelectedIndex = 0;
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

        private void OnDrawingChanged(SheetSession session)
        {
            FillLists();
            if (session.Selected.Length > 0)
                DetailsText.Text = session.Describe();
            ShowEditState();
        }

        private void OnToolMessage(string message)
        {
            StatusText.Text = message;
            ShowEditState();
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

        private void OnFoundPicked(object? sender, SelectionChangedEventArgs e)
        {
            if (!_syncingLists && _session != null && FoundList.SelectedIndex >= 0)
                _session.Select(_foundOwners[FoundList.SelectedIndex]);
        }

        private void OnSearchChanged(object? sender, TextChangedEventArgs e)
        {
            FillFound();
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                ListTabs.SelectedItem = FoundTab;
        }

        private void OnChipPicked(object? sender, SelectionChangedEventArgs e)
        {
            if (Canvas.Tool == null)
                return;
            Canvas.Tool.PlaceType = ChipBox.SelectedIndex > 0 ? ChipBox.SelectedItem as string : null;
        }

        private void OnThemePicked(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string id)
            {
                ThemeManager.Instance.Apply(id);
                BuildThemeMenu();
            }
        }

        private void OnNew(object? sender, RoutedEventArgs e) => NewSheet();

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
            if (path != null)
                TryOpen(path);
        }

        private void OnOpenDemo(object? sender, RoutedEventArgs e)
        {
            string? path = SheetFiles.FindDemo(AppContext.BaseDirectory);
            if (path != null)
                TryOpen(path);
        }

        private void TryOpen(string path)
        {
            try
            {
                OpenSheet(path);
            }
            catch (Exception ex) when (ex is IOException || ex is FormatException || ex is System.Text.Json.JsonException || ex is NetlistException)
            {
                StatusText.Text = "Could not open " + path + ": " + ex.Message;
            }
        }

        private void OnSave(object? sender, RoutedEventArgs e)
        {
            if (_session?.SheetPath != null)
                SaveTo(_session.SheetPath);
        }

        private async void OnSaveAs(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save sheet as",
                DefaultExtension = "json",
                SuggestedFileName = _session.Netlist.Sheet.Id + ".json",
            });
            string? path = file?.TryGetLocalPath();
            if (path != null)
                SaveTo(path);
        }

        private void SaveTo(string path)
        {
            try
            {
                _session!.SaveAs(path);
                StatusText.Text = "Saved " + path;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SheetEditException)
            {
                StatusText.Text = "Could not save " + path + ": " + ex.Message;
            }
            ShowEditState();
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

        private async void OnExportPng(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export PNG",
                DefaultExtension = "png",
                SuggestedFileName = _session.Netlist.Sheet.Id + ".png",
            });
            string? path = file?.TryGetLocalPath();
            if (path == null)
                return;
            Canvas.ExportPng(path, 2);
            StatusText.Text = "Exported " + path;
        }

        private void OnToggleEditMode(object? sender, RoutedEventArgs e) => SetEditMode(_session != null && _session.Editor.ReadOnly);

        private void OnUndo(object? sender, RoutedEventArgs e)
        {
            Canvas.Tool?.Undo();
            OnToolMessage(Canvas.Tool?.Message ?? "");
        }

        private void OnRedo(object? sender, RoutedEventArgs e)
        {
            Canvas.Tool?.Redo();
            OnToolMessage(Canvas.Tool?.Message ?? "");
        }

        private void OnDelete(object? sender, RoutedEventArgs e)
        {
            Canvas.Tool?.DeleteSelected();
            OnToolMessage(Canvas.Tool?.Message ?? "");
        }

        private void OnTogglePinNumbers(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            _session.SetShowPinNumbers(!_session.ShowPinNumbers);
            PinNumbersItem.IsChecked = _session.ShowPinNumbers;
        }

        private void OnToggleBoardPositions(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            _session.SetShowBoardPositions(!_session.ShowBoardPositions);
            BoardPositionsItem.IsChecked = _session.ShowBoardPositions;
        }

        private void OnExit(object? sender, RoutedEventArgs e) => Close();

        private void OnNdStyle(object? sender, RoutedEventArgs e) => SetStyle(SymbolStyle.NorskData);

        private void OnModernStyle(object? sender, RoutedEventArgs e) => SetStyle(SymbolStyle.Modern);

        private void OnFit(object? sender, RoutedEventArgs e) => Canvas.FitToWindow();
    }
}
