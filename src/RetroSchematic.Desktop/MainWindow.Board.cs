//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// The board in the main window: the Board window (File > Board info...), and the switch
    /// blocks, which are picked from the parts list and shown and edited in Details.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// The owner name of a switch block in the parts list: "switch:" and its board position.
        /// </summary>
        public const string SwitchOwner = "switch:";

        // the switch block shown in Details, or empty
        private string _pickedSwitch = "";

        private TextBox? _swFunction;
        private ComboBox? _swOnMeans;
        private TextBox? _swTypical;
        private readonly List<TextBox> _swSignals = new();
        private readonly List<TextBox> _swBits = new();
        private readonly List<ComboBox> _swSettings = new();

        private static readonly string[] OnMeansValues = { "0", "1", "open" };
        private static readonly string[] SettingValues = { "on", "off", "open" };

        /// <summary>
        /// The switch block shown in Details, or empty.
        /// </summary>
        public string PickedSwitch => _pickedSwitch;

        /// <summary>
        /// The panel under Details that edits the picked switch block (edit mode only).
        /// </summary>
        public StackPanel SwitchEditor => SwitchPanel;

        /// <summary>
        /// Shows a switch block in Details (and its editor in edit mode).
        /// </summary>
        /// <param name="reference">
        /// Its board position, such as 15E.
        /// </param>
        public void PickSwitch(string reference)
        {
            if (_session == null)
                return;
            // a switch is not on the drawing, so the drawing's pick is cleared first
            if (_session.Selected.Length > 0)
                _session.Select("");
            _pickedSwitch = reference;
            ShowDetails(_session, SwitchOwner + reference);
            _syncingLists = true;
            PartsList.SelectedIndex = _partOwners.IndexOf(SwitchOwner + reference);
            _syncingLists = false;
        }

        private void ShowSwitchEditor(SwitchBlock s)
        {
            SwitchPanel.Children.Clear();
            _swSignals.Clear();
            _swBits.Clear();
            _swSettings.Clear();
            SwitchPanel.IsVisible = _session != null && !_session.Editor.ReadOnly;
            if (!SwitchPanel.IsVisible)
                return;
            SwitchPanel.Children.Add(new TextBlock { Text = "Edit switch " + s.Ref, Classes = { "fluent-text-header" } });
            _swFunction = Box(s.Function, "What the block sets");
            SwitchPanel.Children.Add(_swFunction);
            _swOnMeans = new ComboBox { ItemsSource = OnMeansValues, SelectedItem = s.OnMeans ?? "open", HorizontalAlignment = HorizontalAlignment.Stretch };
            _swOnMeans.Classes.Add("fluent-combobox");
            ToolTip.SetTip(_swOnMeans, "The bit a lever that is on (closed) gives: 0, 1, or open when not known");
            SwitchPanel.Children.Add(Labelled("On means", _swOnMeans));
            _swTypical = Box(s.Typical, "The usual setting, and where that comes from");
            SwitchPanel.Children.Add(_swTypical);
            for (int i = 0; i < s.LeverCount; i++)
            {
                SwitchLever? l = LeverOf(s, i + 1);
                TextBox sig = Box(l?.Signal, "signal");
                TextBox bit = Box(l?.Bit, "bit");
                ComboBox set = new() { ItemsSource = SettingValues, SelectedItem = l?.Setting ?? "open", MinWidth = 74 };
                set.Classes.Add("fluent-combobox");
                Grid g = new() { ColumnSpacing = 4 };
                g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(52)));
                g.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                g.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                TextBlock name = new() { Text = "Lever " + (i + 1).ToString(CultureInfo.InvariantCulture), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(sig, 1);
                Grid.SetColumn(bit, 2);
                Grid.SetColumn(set, 3);
                g.Children.Add(name);
                g.Children.Add(sig);
                g.Children.Add(bit);
                g.Children.Add(set);
                SwitchPanel.Children.Add(g);
                _swSignals.Add(sig);
                _swBits.Add(bit);
                _swSettings.Add(set);
            }
            Button save = new() { Content = "Save switch" };
            save.Classes.Add("fluent-button");
            save.Click += OnSwitchSave;
            SwitchPanel.Children.Add(save);
        }

        /// <summary>
        /// Saves the switch editor's values into the sheet (edit mode; undoable). Levers keep
        /// their confidence and note; a lever typed in for the first time is "open".
        /// </summary>
        public void SaveSwitch()
        {
            if (_session == null || _pickedSwitch.Length == 0 || _swFunction == null)
                return;
            SwitchBlock? old = _session.Editor.FindSwitch(_pickedSwitch);
            if (old == null)
                return;
            SwitchBlock s = NetlistReader.Clone(old);
            s.Function = Clean(_swFunction.Text);
            s.OnMeans = _swOnMeans?.SelectedItem as string;
            s.Typical = Clean(_swTypical?.Text);
            List<SwitchLever> levers = new();
            for (int i = 0; i < _swSignals.Count; i++)
            {
                SwitchLever l = LeverOf(s, i + 1) ?? new SwitchLever { Number = i + 1, Confidence = Confidence.Open };
                l.Signal = Clean(_swSignals[i].Text);
                l.Bit = Clean(_swBits[i].Text);
                l.Setting = _swSettings[i].SelectedItem as string;
                levers.Add(l);
            }
            s.Levers = levers;
            try
            {
                _session.Editor.SetSwitch(s);
                StatusText.Text = "Saved switch " + s.Ref + ".";
            }
            catch (SheetEditException ex)
            {
                StatusText.Text = ex.Message;
            }
            ShowEditState();
        }

        private void OnSwitchSave(object? sender, RoutedEventArgs e) => SaveSwitch();

        /// <summary>
        /// Opens the Board window for the open sheet.
        /// </summary>
        /// <returns>
        /// The window, or null when no sheet is open.
        /// </returns>
        public BoardWindow? OpenBoardWindow()
        {
            if (_session == null)
                return null;
            BoardWindow w = new(_session);
            w.Show(this);
            return w;
        }

        private void OpenBoardWindowFromDetails() => OpenBoardWindow();

        private void OnBoardInfo(object? sender, RoutedEventArgs e) => OpenBoardWindow();

        /// <summary>
        /// Imports the board info from the ND PCB catalogue (edit mode; one undo step): the
        /// board facts with the catalogue as their source, the component- and solder-side
        /// photos, and the catalogue's switch text on the switch blocks it names.
        /// </summary>
        /// <param name="catalogue">
        /// The catalogue file, pcb.json.
        /// </param>
        /// <param name="key">
        /// A part or card number, or null for the sheet's own (part number first).
        /// </param>
        /// <returns>
        /// What was done, or why nothing was.
        /// </returns>
        public string ImportFromCatalogue(string catalogue, string? key = null)
        {
            if (_session == null)
                return "No sheet is open.";
            string message;
            try
            {
                PcbCatalogue c = PcbCatalogue.Load(catalogue);
                string k = string.IsNullOrWhiteSpace(key) ? _session.Editor.Root.Board?.PartNumber ?? _session.Editor.Root.Board?.Number ?? "" : key;
                List<CatalogueEntry> found = k.Length == 0 ? new List<CatalogueEntry>() : c.Find(k);
                if (k.Length == 0)
                    message = "The sheet has no part or card number to look up.";
                else if (found.Count == 0)
                    message = "There is no board " + k + " in the catalogue.";
                else if (found.Count > 1)
                    message = "Card number " + k + " is used by " + found.Count.ToString(CultureInfo.InvariantCulture) + " boards; give the part number.";
                else
                    message = _session.Editor.ImportFromCatalogue(found[0], _session.SheetPath);
            }
            catch (Exception ex) when (ex is SheetEditException || ex is System.IO.IOException || ex is UnauthorizedAccessException)
            {
                message = ex.Message;
            }
            StatusText.Text = message.Split('\n')[0];
            LoadPictures();
            ShowEditState();
            return message;
        }

        private async void OnImportCatalogue(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            if (_session.Editor.ReadOnly)
            {
                StatusText.Text = "Turn on edit mode (Ctrl+E) to import board info.";
                return;
            }
            IReadOnlyList<Avalonia.Platform.Storage.IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "The ND PCB catalogue (pcb.json)",
                AllowMultiple = false,
                FileTypeFilter = new[] { new Avalonia.Platform.Storage.FilePickerFileType("pcb.json") { Patterns = new[] { "*.json" } } },
            });
            string? path = files.Count == 0 ? null : files[0].TryGetLocalPath();
            if (path == null)
                return;
            BoardSection? b = _session.Editor.Root.Board;
            string? key = await AskBoardKey(b?.PartNumber ?? b?.Number ?? "");
            if (key != null)
                ImportFromCatalogue(path, key);
        }

        // a small dialog: which board (part or card number), filled with the sheet's own
        private async System.Threading.Tasks.Task<string?> AskBoardKey(string start)
        {
            TextBox box = Box(start, "part number (322049) or card number (1050)");
            Window dialog = new() { Title = "Which board?", Width = 400, SizeToContent = SizeToContent.Height };
            dialog.Bind(BackgroundProperty, dialog.GetResourceObservable("FluentBackgroundPrimaryBrush"));
            string? answer = null;
            StackPanel p = new() { Margin = new Avalonia.Thickness(14), Spacing = 8 };
            p.Children.Add(new TextBlock { Text = "The board's part number or card number in the catalogue:", TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            p.Children.Add(box);
            StackPanel buttons = new() { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
            Button ok = new() { Content = "Import", IsDefault = true };
            ok.Classes.Add("fluent-button");
            ok.Click += (s, e) => { answer = box.Text; dialog.Close(); };
            Button cancel = new() { Content = "Cancel", IsCancel = true };
            cancel.Classes.Add("fluent-button");
            cancel.Click += (s, e) => dialog.Close();
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            p.Children.Add(buttons);
            dialog.Content = p;
            await dialog.ShowDialog(this);
            return string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
        }

        private static SwitchLever? LeverOf(SwitchBlock s, int number)
        {
            for (int i = 0; i < s.Levers.Count; i++)
            {
                if (s.Levers[i].Number == number)
                    return s.Levers[i];
            }
            return null;
        }

        private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private static TextBox Box(string? text, string watermark)
        {
            TextBox t = new() { Text = text ?? "", Watermark = watermark, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            t.Classes.Add("fluent-textbox");
            return t;
        }

        private static Grid Labelled(string label, Control c)
        {
            Grid g = new() { ColumnSpacing = 6 };
            g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(80)));
            g.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            TextBlock l = new() { Text = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(c, 1);
            g.Children.Add(l);
            g.Children.Add(c);
            return g;
        }
    }
}
