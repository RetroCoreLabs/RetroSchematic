//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
// RetroSchematic - viewer for Norsk Data circuit diagrams
//

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// The settings window: every display option, as a default for all sheets and as this
    /// sheet's own value. It holds no rules of its own: the defaults go to
    /// <see cref="UiSettings"/>, the sheet's values to <see cref="SheetSession.SetSheetDisplay"/>,
    /// and which applies is <see cref="DisplaySettings.Resolve"/>.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        /// <summary>
        /// The keys of the rows, in order.
        /// </summary>
        public static readonly string[] Keys = { "names", "symbols", "theme", "pins", "positions", "arrows", "buses", "pulse" };

        private static readonly string[] s_labels =
        {
            "Active-low signal names", "Symbol style", "Theme", "Show pin numbers", "Show board positions",
            "Show port arrows", "Show buses", "Trace pulse",
        };

        private static readonly string[] s_nameChoices = { "/NAME (slash)", "~NAME (tilde)", "NAME with subscript 0/1 (ND)" };
        private static readonly string[] s_symbolChoices = { "ND (1976 manual)", "Modern" };
        private const string UseDefault = "Use default";

        private readonly SheetSession? _session;
        private readonly List<string> _themeIds = new();

        /// <summary>
        /// The default-for-all-sheets ComboBox of each row, by key (on/off rows: On, Off).
        /// </summary>
        public Dictionary<string, ComboBox> DefaultControls { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// The this-sheet ComboBox of each row, by key; item 0 is always "Use default".
        /// </summary>
        public Dictionary<string, ComboBox> SheetControls { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Raised after the settings were applied, so the main window can show them.
        /// </summary>
        public event Action? Applied;

        /// <summary>
        /// Needed by the XAML loader; use the constructor with a session.
        /// </summary>
        public SettingsWindow()
            : this(null)
        {
        }

        /// <summary>
        /// Creates the window for the open sheet, or for no sheet (then only the defaults can be
        /// changed).
        /// </summary>
        /// <param name="session">
        /// The open sheet, or null.
        /// </param>
        public SettingsWindow(SheetSession? session)
        {
            InitializeComponent();
            _session = session;
            IReadOnlyList<ThemeDefinition> themes = ThemeManager.Instance.Themes;
            List<string> themeNames = new();
            for (int i = 0; i < themes.Count; i++)
            {
                _themeIds.Add(themes[i].Id);
                themeNames.Add(themes[i].DisplayName);
            }
            AddHeader();
            for (int r = 0; r < Keys.Length; r++)
                AddRow(r + 1, Keys[r], s_labels[r], r == 0 ? s_nameChoices : r == 1 ? s_symbolChoices : r == 2 ? themeNames.ToArray() : null);
            Fill();
        }

        /// <summary>
        /// Stores the defaults and the sheet's values and tells the main window. The sheet is
        /// only changed when one of its values differs from what it holds.
        /// </summary>
        public void ApplyChoices()
        {
            UiSettings.Instance.FromDefaults(ReadDefaults());
            UiSettings.Instance.Save();
            if (_session != null)
            {
                SheetDisplay next = ReadSheet();
                if (!Same(next, _session.SheetDisplay))
                    _session.SetSheetDisplay(next);
            }
            Applied?.Invoke();
        }

        private void AddHeader()
        {
            Rows.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            AddCell(new TextBlock { Text = "Option", Classes = { "fluent-text" }, FontWeight = Avalonia.Media.FontWeight.Bold }, 0, 0);
            AddCell(new TextBlock { Text = "Defaults for all sheets", Classes = { "fluent-text" }, FontWeight = Avalonia.Media.FontWeight.Bold }, 0, 1);
            AddCell(new TextBlock { Text = _session == null ? "This sheet (none open)" : "This sheet", Classes = { "fluent-text" }, FontWeight = Avalonia.Media.FontWeight.Bold }, 0, 2);
        }

        private void AddRow(int row, string key, string label, string[]? choices)
        {
            Rows.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            AddCell(new TextBlock { Text = label, Classes = { "fluent-text" }, VerticalAlignment = VerticalAlignment.Center }, row, 0);
            ComboBox def;
            List<string> sheetItems = new() { UseDefault };
            if (choices == null)
            {
                // on/off as a drop-down too: the app's theme styles drop-downs, not tick boxes
                def = new ComboBox { ItemsSource = new[] { "On", "Off" }, Width = 180, Classes = { "fluent-combobox" } };
                sheetItems.Add("On");
                sheetItems.Add("Off");
            }
            else
            {
                def = new ComboBox { ItemsSource = choices, Width = 180, Classes = { "fluent-combobox" } };
                sheetItems.AddRange(choices);
            }
            ComboBox sheet = new() { ItemsSource = sheetItems, Width = 180, Classes = { "fluent-combobox" }, IsEnabled = _session != null };
            DefaultControls[key] = def;
            SheetControls[key] = sheet;
            AddCell(def, row, 1);
            AddCell(sheet, row, 2);
        }

        private void AddCell(Control c, int row, int column)
        {
            c.Margin = new Avalonia.Thickness(2, 4);
            Grid.SetRow(c, row);
            Grid.SetColumn(c, column);
            Rows.Children.Add(c);
        }

        /// <summary>
        /// Shows the current defaults and the sheet's own values.
        /// </summary>
        private void Fill()
        {
            DisplaySettings d = UiSettings.Instance.ToDefaults();
            (DefaultControls["names"]).SelectedIndex = (int)d.NameStyle;
            (DefaultControls["symbols"]).SelectedIndex = d.Symbols == SymbolStyle.Modern ? 1 : 0;
            (DefaultControls["theme"]).SelectedIndex = ThemeIndex(d.Theme);
            DefaultControls["pins"].SelectedIndex = d.PinNumbers ? 0 : 1;
            DefaultControls["positions"].SelectedIndex = d.BoardPositions ? 0 : 1;
            DefaultControls["arrows"].SelectedIndex = d.PortArrows ? 0 : 1;
            DefaultControls["buses"].SelectedIndex = d.Buses ? 0 : 1;
            DefaultControls["pulse"].SelectedIndex = d.TracePulse ? 0 : 1;

            SheetDisplay s = _session?.SheetDisplay ?? new SheetDisplay();
            SheetControls["names"].SelectedIndex = SignalNames.TryParseStyle(s.NameStyle, out SignalNameStyle ns) ? (int)ns + 1 : 0;
            SheetControls["symbols"].SelectedIndex = DisplaySettings.TryParseSymbols(s.Symbols, out SymbolStyle sy) ? (sy == SymbolStyle.Modern ? 2 : 1) : 0;
            SheetControls["theme"].SelectedIndex = s.Theme == null || ThemeIndex(s.Theme) < 0 ? 0 : ThemeIndex(s.Theme) + 1;
            SheetControls["pins"].SelectedIndex = OnOff(s.PinNumbers);
            SheetControls["positions"].SelectedIndex = OnOff(s.BoardPositions);
            SheetControls["arrows"].SelectedIndex = OnOff(s.PortArrows);
            SheetControls["buses"].SelectedIndex = OnOff(s.Buses);
            SheetControls["pulse"].SelectedIndex = OnOff(s.TracePulse);
        }

        private DisplaySettings ReadDefaults()
        {
            DisplaySettings d = new()
            {
                NameStyle = (SignalNameStyle)Math.Max(0, (DefaultControls["names"]).SelectedIndex),
                Symbols = (DefaultControls["symbols"]).SelectedIndex == 1 ? SymbolStyle.Modern : SymbolStyle.NorskData,
                PinNumbers = DefaultControls["pins"].SelectedIndex == 0,
                BoardPositions = DefaultControls["positions"].SelectedIndex == 0,
                PortArrows = DefaultControls["arrows"].SelectedIndex == 0,
                Buses = DefaultControls["buses"].SelectedIndex == 0,
                TracePulse = DefaultControls["pulse"].SelectedIndex == 0,
            };
            int t = (DefaultControls["theme"]).SelectedIndex;
            d.Theme = t >= 0 ? _themeIds[t] : "Light";
            return d;
        }

        private SheetDisplay ReadSheet()
        {
            SheetDisplay s = new();
            int i = SheetControls["names"].SelectedIndex;
            if (i > 0)
                s.NameStyle = DisplaySettings.NameStyleText((SignalNameStyle)(i - 1));
            i = SheetControls["symbols"].SelectedIndex;
            if (i > 0)
                s.Symbols = i == 2 ? "modern" : "nd";
            i = SheetControls["theme"].SelectedIndex;
            if (i > 0)
                s.Theme = _themeIds[i - 1];
            s.PinNumbers = ReadOnOff("pins");
            s.BoardPositions = ReadOnOff("positions");
            s.PortArrows = ReadOnOff("arrows");
            s.Buses = ReadOnOff("buses");
            s.TracePulse = ReadOnOff("pulse");
            return s;
        }

        private bool? ReadOnOff(string key)
        {
            int i = SheetControls[key].SelectedIndex;
            return i <= 0 ? null : i == 1;
        }

        private static int OnOff(bool? v) => v == null ? 0 : v.Value ? 1 : 2;

        private int ThemeIndex(string id)
        {
            for (int i = 0; i < _themeIds.Count; i++)
            {
                if (string.Equals(_themeIds[i], id, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static bool Same(SheetDisplay a, SheetDisplay b)
            => a.NameStyle == b.NameStyle && a.Symbols == b.Symbols && a.Theme == b.Theme && a.PinNumbers == b.PinNumbers
            && a.BoardPositions == b.BoardPositions && a.PortArrows == b.PortArrows && a.Buses == b.Buses && a.TracePulse == b.TracePulse;

        private void OnOk(object? sender, RoutedEventArgs e)
        {
            ApplyChoices();
            Close();
        }

        private void OnApply(object? sender, RoutedEventArgs e) => ApplyChoices();

        private void OnCancel(object? sender, RoutedEventArgs e) => Close();
    }
}
