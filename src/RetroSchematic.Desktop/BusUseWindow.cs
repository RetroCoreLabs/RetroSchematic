//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// Bus use, all cards: every backplane signal each card uses (ND-100 plugs A, B and C,
    /// NORD-10 card-edge fingers), and for the picked signal every chip pin it reaches through the
    /// card's blocks, with what the pin does. Clicking a chip opens its card, goes into its block
    /// and picks it. The list can be saved as a Markdown report.
    /// </summary>
    public sealed class BusUseWindow : Window
    {
        private readonly MainWindow _main;
        private readonly ComboBox _bus = new() { Width = 220 };
        private readonly ListBox _signals = new() { FontFamily = new FontFamily("Consolas,Menlo,monospace") };
        private readonly ListBox _chips = new() { FontFamily = new FontFamily("Consolas,Menlo,monospace") };
        private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };
        private readonly List<BusSignalUse> _all = new();
        private readonly List<BusSignalUse> _shown = new();

        /// <summary>
        /// Opens over the main window and reads every card.
        /// </summary>
        /// <param name="main">
        /// The main window, used to show a chip that is clicked.
        /// </param>
        public BusUseWindow(MainWindow main)
        {
            _main = main;
            Title = "Bus use - all cards";
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://RetroSchematic/Assets/RetroSchematic.ico")));
            Width = 1100;
            Height = 820;
            this.Bind(BackgroundProperty, this.GetResourceObservable("FluentBackgroundPrimaryBrush"));

            _bus.ItemsSource = new[] { "All buses", "ND-100 (plugs A, B, C)", "NORD-10 (edge fingers)" };
            _bus.SelectedIndex = 0;
            _bus.SelectionChanged += (s, e) => Fill();
            Button save = new() { Content = "Save report as Markdown..." };
            save.Click += async (s, e) => await SaveReport();
            Button reload = new() { Content = "Read the cards again" };
            reload.Click += (s, e) => Load();
            StackPanel bar = new() { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Avalonia.Thickness(10) };
            bar.Children.Add(_bus);
            bar.Children.Add(reload);
            bar.Children.Add(save);

            _signals.SelectionChanged += (s, e) => ShowChips();
            _chips.DoubleTapped += (s, e) => GoToChip();
            _chips.Tapped += (s, e) => GoToChip();

            Grid grid = new() { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,*,Auto"), Margin = new Avalonia.Thickness(10, 0, 10, 10) };
            TextBlock head1 = new() { Text = "Signals: card, label as printed, backplane pin(s), bus catalogue name, D = the card drives it, R = the card reads it", Margin = new Avalonia.Thickness(0, 4) };
            TextBlock head2 = new() { Text = "Chip pins of the picked signal (click one to show it on its card): block, part, type, pin, pin name, what it does", Margin = new Avalonia.Thickness(0, 8, 0, 4) };
            Grid.SetRow(bar, 0);
            Grid.SetRow(head1, 1);
            Grid.SetRow(_signals, 2);
            Grid.SetRow(head2, 3);
            Grid.SetRow(_chips, 4);
            Grid.SetRow(_status, 5);
            grid.Children.Add(bar);
            grid.Children.Add(head1);
            grid.Children.Add(_signals);
            grid.Children.Add(head2);
            grid.Children.Add(_chips);
            grid.Children.Add(_status);
            Content = grid;
            Load();
        }

        /// <summary>
        /// The signals of every card read, before the bus filter.
        /// </summary>
        public IReadOnlyList<BusSignalUse> All => _all;

        /// <summary>
        /// The lines in the signal list, as shown.
        /// </summary>
        public IReadOnlyList<BusSignalUse> Shown => _shown;

        /// <summary>
        /// The status line text.
        /// </summary>
        public string Status => _status.Text ?? "";

        /// <summary>
        /// Picks the bus filter: 0 all, 1 ND-100, 2 NORD-10.
        /// </summary>
        /// <param name="index">
        /// The filter.
        /// </param>
        public void PickBus(int index) => _bus.SelectedIndex = index;

        /// <summary>
        /// Reads every card: the sheets under the repository's <c>sheets</c> folder, and the open
        /// sheet as it is on screen (unsaved edits included). When one card is in several files
        /// (2A50 with and without blocks), the file with the shortest name is used and the status
        /// line says so.
        /// </summary>
        public void Load()
        {
            _all.Clear();
            StringBuilder note = new();
            SheetSession? open = _main.Session;
            string? catalogFolder = open?.Editor.Catalog?.Folder ?? ChipCatalog.Find(AppContext.BaseDirectory);
            ChipCatalog? catalog = catalogFolder == null ? null : new ChipCatalog(catalogFolder);
            // the library beside the catalogue, with the open sheet as it is on screen in place
            // of its file (BusUse.OfLibrary, shared with the MCP server)
            _all.AddRange(BusUse.OfLibrary(CardLibrary.FolderBeside(catalogFolder), catalog, open?.Editor.Root, open?.SheetPath, note, out int cardCount));
            _status.Text = cardCount + " card(s), " + _all.Count + " backplane signals. " + note;
            Fill();
        }

        private void Fill()
        {
            _shown.Clear();
            List<string> lines = new();
            int pick = _bus.SelectedIndex;
            for (int i = 0; i < _all.Count; i++)
            {
                BusSignalUse u = _all[i];
                if ((pick == 1 && u.Bus != MachineBus.Nd100) || (pick == 2 && u.Bus != MachineBus.Nord10))
                    continue;
                _shown.Add(u);
                lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0,-6} {1,-12} {2,-22} {3,-12} {4}{5}",
                    u.Card, u.Signal, string.Join(" ", u.BackplanePins), u.CatalogueName ?? "", u.Driven ? "D" : "-", u.Received ? "R" : "-"));
            }
            _signals.ItemsSource = lines;
            _chips.ItemsSource = Array.Empty<string>();
        }

        private void ShowChips()
        {
            int i = _signals.SelectedIndex;
            if (i < 0 || i >= _shown.Count)
            {
                _chips.ItemsSource = Array.Empty<string>();
                return;
            }
            List<string> lines = new();
            BusSignalUse u = _shown[i];
            for (int k = 0; k < u.Chips.Count; k++)
            {
                BusChipPin c = u.Chips[k];
                lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0,-28} {1,-7} {2,-12} {3,-4} {4,-7} {5}",
                    c.Path.Length == 0 ? "(top)" : c.Path, c.Part, c.Type + (c.ReadFrom != null ? "*" : ""), c.Pin, c.PinName, BusUse.RoleText(c)));
            }
            if (u.Chips.Count == 0)
                lines.Add("No chip pin on this signal (a supply, or not read yet).");
            _chips.ItemsSource = lines;
        }

        /// <summary>
        /// Shows the picked chip on its card in the main window.
        /// </summary>
        public void GoToChip()
        {
            int i = _signals.SelectedIndex, k = _chips.SelectedIndex;
            if (i < 0 || i >= _shown.Count || k < 0 || k >= _shown[i].Chips.Count)
                return;
            BusSignalUse u = _shown[i];
            _main.ShowBusChip(u.SheetFile, u.Chips[k].Path, u.Chips[k].Part);
        }

        /// <summary>
        /// Picks a signal and one of its chips (for tests and for keyboard use).
        /// </summary>
        /// <param name="signal">
        /// The index in <see cref="Shown"/>.
        /// </param>
        /// <param name="chip">
        /// The index in its chip list.
        /// </param>
        public void Pick(int signal, int chip)
        {
            _signals.SelectedIndex = signal;
            _chips.SelectedIndex = chip;
        }

        private async Task SaveReport()
        {
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save the bus use report",
                SuggestedFileName = "BUS-USE.md",
                DefaultExtension = "md",
                FileTypeChoices = new[] { new FilePickerFileType("Markdown") { Patterns = new[] { "*.md" } } },
            });
            string? path = file?.TryGetLocalPath();
            if (path == null)
                return;
            File.WriteAllText(path, BusUse.ToMarkdown(new List<BusSignalUse>(_shown)));
            _status.Text = "Saved " + path;
        }
    }
}
