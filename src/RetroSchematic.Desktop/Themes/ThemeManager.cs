//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
// RetroSchematic - viewer for Norsk Data circuit diagrams
//

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;        // ResourceDictionary
using Avalonia.Media;           // Color, SolidColorBrush
using Avalonia.Styling;         // ThemeVariant
using RetroSchematic.Render;    // SignalValue

namespace RetroSchematic.Desktop.Themes
{
    /// <summary>
    /// Runtime theming, the same design as RetroCommander's ThemeManager (RetroCommander repository,
    /// RetroCommander.Desktop/Themes/ThemeManager.cs), so the RetroCore Labs tools look and switch alike:
    ///   * Every brush the XAML uses is supplied PER theme through
    ///     <see cref="IResourceDictionary.ThemeDictionaries"/>.
    ///   * Switching theme only sets <see cref="Application.RequestedThemeVariant"/>; every
    ///     {DynamicResource ...} lookup follows, so the whole window re-colours at once.
    ///   * Custom themes are a <see cref="ThemeVariant"/> that INHERITS Light or Dark, so Avalonia's
    ///     own Fluent controls (buttons, scroll bars, DataGrid) keep drawing correctly.
    ///   * The chosen theme is saved in %APPDATA%\RetroSchematic\ui-settings.json.
    ///
    /// Added for this program: the drawing brushes (paper, ink, part frame, inferred reading, note,
    /// selection). The schematic canvas looks them up by key on every paint, so a theme change
    /// re-colours the drawing too. A brush is never cached in a static field - that captures one
    /// theme's colour for good.
    /// </summary>
    public sealed class ThemeManager
    {
        private readonly List<ThemeDefinition> _themes = new();
        private Application? _app;

        // The brush keys, in the same order as Palette.ToArray(). Keep the two lists aligned;
        // MainWindowTests.EveryTheme_GivesEveryBrush checks that every theme gives every key.
        private static readonly string[] s_brushKeys =
        {
            "FluentBackgroundPrimaryBrush",
            "FluentBackgroundSecondaryBrush",
            "FluentSurfaceAltBrush",
            "FluentBorderLightBrush",
            "FluentBorderDarkBrush",
            "FluentAccentBlueBrush",
            "FluentTextPrimaryBrush",
            "FluentTextSecondaryBrush",
            "FluentSelectionHighlightBrush",
            "FluentHoverHighlightBrush",
            "FluentSidebarBackgroundBrush",
            "FluentSidebarSeparatorBrush",
            "FluentWarningBackgroundBrush",
            "FluentWarningBorderBrush",
            "FluentSuccessBackgroundBrush",
            "FluentSuccessBorderBrush",
            "FluentErrorBackgroundBrush",
            "FluentErrorBorderBrush",
            "FluentErrorTextBrush",
            SchematicPaperKey,
            SchematicInkKey,
            SchematicFrameKey,
            SchematicInferredKey,
            SchematicNoteKey,
            SchematicSelectionKey,
            SchematicOpenKey,
            ValueUnknownKey,
            ValueZeroKey,
            ValueOneKey,
            ValueNotDrivenKey,
            ValueConflictKey,
            SchematicTraceKey,
            SchematicWireKey,
            SchematicBusKey,
            SchematicNetLabelKey,
            AccentHoverKey,
        };

        /// <summary>
        /// Brush key: the paper behind the drawing.
        /// </summary>
        public const string SchematicPaperKey = "SchematicPaperBrush";

        /// <summary>
        /// Brush key: symbols, pin lines and signal names that were READ off the drawing.
        /// </summary>
        public const string SchematicInkKey = "SchematicInkBrush";

        /// <summary>
        /// Brush key: the frame drawn round each chip at its board position.
        /// </summary>
        public const string SchematicFrameKey = "SchematicFrameBrush";

        /// <summary>
        /// Brush key: anything INFERRED rather than read (its own colour, as the plan asks).
        /// </summary>
        public const string SchematicInferredKey = "SchematicInferredBrush";

        /// <summary>
        /// Brush key: secondary text such as a chip's function.
        /// </summary>
        public const string SchematicNoteKey = "SchematicNoteBrush";

        /// <summary>
        /// Brush key: the picked part, gate or signal.
        /// </summary>
        public const string SchematicSelectionKey = "SchematicSelectionBrush";

        /// <summary>
        /// Brush key: a connection that is OPEN, not known (its own colour, apart from INFERRED).
        /// </summary>
        public const string SchematicOpenKey = "SchematicOpenBrush";

        /// <summary>
        /// Brush key: a simulated signal with no value yet (Logisim grey).
        /// </summary>
        public const string ValueUnknownKey = "SchematicValueUnknownBrush";

        /// <summary>
        /// Brush key: a simulated 0 (Logisim dark green).
        /// </summary>
        public const string ValueZeroKey = "SchematicValueZeroBrush";

        /// <summary>
        /// Brush key: a simulated 1 (Logisim bright green).
        /// </summary>
        public const string ValueOneKey = "SchematicValueOneBrush";

        /// <summary>
        /// Brush key: a signal nothing drives (Logisim blue).
        /// </summary>
        public const string ValueNotDrivenKey = "SchematicValueNotDrivenBrush";

        /// <summary>
        /// Brush key: a signal driven both ways at once (Logisim red).
        /// </summary>
        public const string ValueConflictKey = "SchematicValueConflictBrush";

        /// <summary>
        /// Brush key: the signal shown by the trace tool, and the chips it joins.
        /// </summary>
        public const string SchematicTraceKey = "SchematicTraceBrush";

        /// <summary>
        /// Brush key: a wire between pins (a sure reading; inferred and open wires keep their
        /// reading colours).
        /// </summary>
        public const string SchematicWireKey = "SchematicWireBrush";

        /// <summary>
        /// Brush key: a drawn bus line.
        /// </summary>
        public const string SchematicBusKey = "SchematicBusBrush";

        /// <summary>
        /// Brush key: a net-label diamond and its signal name (a sure reading).
        /// </summary>
        public const string SchematicNetLabelKey = "SchematicNetLabelBrush";

        /// <summary>
        /// Brush key: an accent-coloured button under the pointer.
        /// </summary>
        public const string AccentHoverKey = "FluentAccentHoverBrush";

        /// <summary>
        /// The brush key for a simulated value, or null for <see cref="SignalValue.NotSimulated"/>
        /// (the item keeps its ink and reading colour).
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The brush key, or null.
        /// </returns>
        public static string? ValueKey(SignalValue value) => value switch
        {
            SignalValue.NotSimulated => null,
            SignalValue.Unknown => ValueUnknownKey,
            SignalValue.Zero => ValueZeroKey,
            SignalValue.One => ValueOneKey,
            SignalValue.NotDriven => ValueNotDrivenKey,
            SignalValue.Conflict => ValueConflictKey,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "a signal value this program does not know"),
        };

        private ThemeManager()
        {
            BuildThemes();
        }

        /// <summary>
        /// The single shared theme manager.
        /// </summary>
        public static ThemeManager Instance { get; } = new ThemeManager();

        /// <summary>
        /// Every brush key a theme must supply.
        /// </summary>
        public static IReadOnlyList<string> BrushKeys => s_brushKeys;

        /// <summary>
        /// All themes, in menu order.
        /// </summary>
        public IReadOnlyList<ThemeDefinition> Themes => _themes;

        /// <summary>
        /// The applied theme (set by <see cref="Initialize"/>).
        /// </summary>
        public ThemeDefinition Current { get; private set; } = null!;

        /// <summary>
        /// Registers every theme's brushes on the application and applies the saved theme (Light
        /// if none). Call once, before the first window opens.
        /// </summary>
        /// <param name="app">
        /// The running application, whose resources get the theme brushes.
        /// </param>
        public void Initialize(Application app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            for (int i = 0; i < _themes.Count; i++)
                app.Resources.ThemeDictionaries[_themes[i].Variant] = BuildBrushDictionary(_themes[i]);

            ThemeDefinition? theme = FindById(UiSettings.Instance.Theme) ?? _themes[0];
            Apply(theme.Id, persist: false);
        }

        /// <summary>
        /// Applies a theme at once; with <paramref name="persist"/> it is saved for next start.
        /// </summary>
        /// <param name="themeId">
        /// Id of one of the themes in Themes.
        /// </param>
        /// <param name="persist">
        /// True to save the choice for the next start.
        /// </param>
        public void Apply(string themeId, bool persist = true)
        {
            if (_app == null)
                return;
            ThemeDefinition? theme = FindById(themeId);
            if (theme == null)
                return;
            Current = theme;
            _app.RequestedThemeVariant = theme.Variant;
            if (persist)
            {
                UiSettings.Instance.Theme = theme.Id;
                UiSettings.Instance.Save();
            }
        }

        private ThemeDefinition? FindById(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < _themes.Count; i++)
            {
                if (string.Equals(_themes[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return _themes[i];
            }
            return null;
        }

        private static ResourceDictionary BuildBrushDictionary(ThemeDefinition def)
        {
            ResourceDictionary rd = new();
            string[] hex = def.Palette.ToArray();
            for (int i = 0; i < s_brushKeys.Length; i++)
                rd[s_brushKeys[i]] = new SolidColorBrush(Color.Parse(hex[i]));
            return rd;
        }

        // The UI colours of every theme are RetroCommander's, unchanged, so both programs look
        // the same under one theme name. The Schematic* colours are new for this program.
        private void BuildThemes()
        {
            _themes.Add(new ThemeDefinition("Light", "Light", ThemeVariant.Light, new Palette
            {
                BackgroundPrimary = "#F3F3F3",
                BackgroundSecondary = "#FFFFFF",
                SurfaceAlt = "#F8F8F8",
                BorderLight = "#D0D0D0",
                BorderDark = "#3C3C3C",
                AccentBlue = "#0078D7",
                TextPrimary = "#202020",
                TextSecondary = "#666666",
                SelectionHighlight = "#CDE8FF",
                HoverHighlight = "#EAF6FF",
                SidebarBackground = "#F0F0F0",
                SidebarSeparator = "#E0E0E0",
                WarningBackground = "#FFF8DC",
                WarningBorder = "#FFD700",
                SuccessBackground = "#F0F8F0",
                SuccessBorder = "#90EE90",
                ErrorBackground = "#FDE7E9",
                ErrorBorder = "#E81123",
                ErrorText = "#C42B1C",
                // paper white and black ink, like the printed sheet
                SchematicPaper = "#FFFFFF",
                SchematicInk = "#000000",
                SchematicFrame = "#A0A0A0",
                SchematicInferred = "#B06000",
                SchematicNote = "#666666",
                SchematicSelection = "#0078D7",
                SchematicOpen = "#D00000",
            }));

            _themes.Add(new ThemeDefinition("Dark", "Dark", ThemeVariant.Dark, new Palette
            {
                BackgroundPrimary = "#1E1E1E",
                BackgroundSecondary = "#252526",
                SurfaceAlt = "#2D2D30",
                BorderLight = "#3F3F46",
                BorderDark = "#555555",
                AccentBlue = "#0A84FF",
                TextPrimary = "#E8E8E8",
                TextSecondary = "#A0A0A0",
                SelectionHighlight = "#094771",
                HoverHighlight = "#2A2D2E",
                SidebarBackground = "#252526",
                SidebarSeparator = "#3F3F46",
                WarningBackground = "#3A3320",
                WarningBorder = "#D7B600",
                SuccessBackground = "#1E3320",
                SuccessBorder = "#4EC94E",
                ErrorBackground = "#3A1D1D",
                ErrorBorder = "#C44C4C",
                ErrorText = "#FF99A4",
                SchematicPaper = "#1E1E1E",
                SchematicInk = "#E8E8E8",
                SchematicFrame = "#555555",
                SchematicInferred = "#E0A040",
                SchematicNote = "#A0A0A0",
                SchematicSelection = "#0A84FF",
                SchematicOpen = "#FF5555",
            }));

            _themes.Add(new ThemeDefinition("SolarizedDark", "Solarized Dark",
                new ThemeVariant("SolarizedDark", ThemeVariant.Dark), new Palette
                {
                    BackgroundPrimary = "#002B36",
                    BackgroundSecondary = "#073642",
                    SurfaceAlt = "#08404F",
                    BorderLight = "#094B5A",
                    BorderDark = "#586E75",
                    AccentBlue = "#268BD2",
                    TextPrimary = "#93A1A1",
                    TextSecondary = "#657B83",
                    SelectionHighlight = "#0B4F60",
                    HoverHighlight = "#0B4A59",
                    SidebarBackground = "#073642",
                    SidebarSeparator = "#094B5A",
                    WarningBackground = "#33301A",
                    WarningBorder = "#B58900",
                    SuccessBackground = "#14361F",
                    SuccessBorder = "#859900",
                    ErrorBackground = "#3A1F1A",
                    ErrorBorder = "#DC322F",
                    ErrorText = "#E8857F",
                    SchematicPaper = "#002B36",
                    SchematicInk = "#93A1A1",
                    SchematicFrame = "#586E75",
                    SchematicInferred = "#B58900",
                    SchematicNote = "#657B83",
                    SchematicSelection = "#268BD2",
                    SchematicOpen = "#DC322F",
                }));

            _themes.Add(new ThemeDefinition("Nord", "Nord",
                new ThemeVariant("Nord", ThemeVariant.Dark), new Palette
                {
                    BackgroundPrimary = "#2E3440",
                    BackgroundSecondary = "#3B4252",
                    SurfaceAlt = "#434C5E",
                    BorderLight = "#4C566A",
                    BorderDark = "#5E6877",
                    AccentBlue = "#88C0D0",
                    TextPrimary = "#ECEFF4",
                    TextSecondary = "#9AA4B5",
                    SelectionHighlight = "#434C5E",
                    HoverHighlight = "#3B4252",
                    SidebarBackground = "#3B4252",
                    SidebarSeparator = "#4C566A",
                    WarningBackground = "#4A3F2A",
                    WarningBorder = "#EBCB8B",
                    SuccessBackground = "#34402C",
                    SuccessBorder = "#A3BE8C",
                    ErrorBackground = "#43292C",
                    ErrorBorder = "#BF616A",
                    ErrorText = "#D99CA3",
                    SchematicPaper = "#2E3440",
                    SchematicInk = "#ECEFF4",
                    SchematicFrame = "#4C566A",
                    SchematicInferred = "#EBCB8B",
                    SchematicNote = "#9AA4B5",
                    SchematicSelection = "#88C0D0",
                    SchematicOpen = "#BF616A",
                }));

            _themes.Add(new ThemeDefinition("RetroGreen", "Retro Green",
                new ThemeVariant("RetroGreen", ThemeVariant.Dark), new Palette
                {
                    BackgroundPrimary = "#001100",
                    BackgroundSecondary = "#002200",
                    SurfaceAlt = "#001A00",
                    BorderLight = "#004400",
                    BorderDark = "#005500",
                    AccentBlue = "#00FF00",
                    TextPrimary = "#00FF00",
                    TextSecondary = "#00AA00",
                    SelectionHighlight = "#004400",
                    HoverHighlight = "#003300",
                    SidebarBackground = "#002200",
                    SidebarSeparator = "#004400",
                    WarningBackground = "#1A1A00",
                    WarningBorder = "#FFFF00",
                    SuccessBackground = "#002200",
                    SuccessBorder = "#00FF00",
                    ErrorBackground = "#220000",
                    ErrorBorder = "#FF3300",
                    ErrorText = "#FF6644",
                    SchematicPaper = "#001100",
                    SchematicInk = "#00FF00",
                    SchematicFrame = "#005500",
                    SchematicInferred = "#FFFF00",
                    SchematicNote = "#00AA00",
                    SchematicSelection = "#FFFFFF",
                    SchematicOpen = "#FF4040",
                }));

            _themes.Add(new ThemeDefinition("AmberCrt", "Amber CRT",
                new ThemeVariant("AmberCrt", ThemeVariant.Dark), new Palette
                {
                    BackgroundPrimary = "#1A0F00",
                    BackgroundSecondary = "#261800",
                    SurfaceAlt = "#221400",
                    BorderLight = "#553300",
                    BorderDark = "#664400",
                    AccentBlue = "#FFB000",
                    TextPrimary = "#FFB000",
                    TextSecondary = "#CC8800",
                    SelectionHighlight = "#443300",
                    HoverHighlight = "#332200",
                    SidebarBackground = "#261800",
                    SidebarSeparator = "#553300",
                    WarningBackground = "#262600",
                    WarningBorder = "#FFFF00",
                    SuccessBackground = "#261800",
                    SuccessBorder = "#FFB000",
                    ErrorBackground = "#220000",
                    ErrorBorder = "#FF3300",
                    ErrorText = "#FF6644",
                    SchematicPaper = "#1A0F00",
                    SchematicInk = "#FFB000",
                    SchematicFrame = "#664400",
                    SchematicInferred = "#FFFF00",
                    SchematicNote = "#CC8800",
                    SchematicSelection = "#FFFFFF",
                    SchematicOpen = "#FF4040",
                }));

            _themes.Add(new ThemeDefinition("Synthwave", "Synthwave '84",
                new ThemeVariant("Synthwave", ThemeVariant.Dark), new Palette
                {
                    BackgroundPrimary = "#262335",
                    BackgroundSecondary = "#2A2139",
                    SurfaceAlt = "#241B2F",
                    BorderLight = "#463465",
                    BorderDark = "#614D85",
                    AccentBlue = "#FF7EDB",
                    TextPrimary = "#FFFFFF",
                    TextSecondary = "#B893CE",
                    SelectionHighlight = "#463465",
                    HoverHighlight = "#34294F",
                    SidebarBackground = "#2A2139",
                    SidebarSeparator = "#463465",
                    WarningBackground = "#332B1F",
                    WarningBorder = "#FEDE5D",
                    SuccessBackground = "#12332A",
                    SuccessBorder = "#72F1B8",
                    ErrorBackground = "#351525",
                    ErrorBorder = "#FE4450",
                    ErrorText = "#F97E72",
                    SchematicPaper = "#262335",
                    SchematicInk = "#FFFFFF",
                    SchematicFrame = "#614D85",
                    SchematicInferred = "#FEDE5D",
                    SchematicNote = "#B893CE",
                    SchematicSelection = "#FF7EDB",
                    SchematicOpen = "#FE4450",
                }));

            _themes.Add(new ThemeDefinition("HighContrast", "High Contrast",
                new ThemeVariant("HighContrast", ThemeVariant.Dark), new Palette
                {
                    BackgroundPrimary = "#000000",
                    BackgroundSecondary = "#000000",
                    SurfaceAlt = "#1A1A1A",
                    BorderLight = "#FFFFFF",
                    BorderDark = "#FFFFFF",
                    AccentBlue = "#1F6FEB",
                    TextPrimary = "#FFFFFF",
                    TextSecondary = "#E0E0E0",
                    SelectionHighlight = "#1F6FEB",
                    HoverHighlight = "#333333",
                    SidebarBackground = "#000000",
                    SidebarSeparator = "#FFFFFF",
                    WarningBackground = "#000000",
                    WarningBorder = "#FFFF00",
                    SuccessBackground = "#000000",
                    SuccessBorder = "#00FF00",
                    ErrorBackground = "#000000",
                    ErrorBorder = "#FF0000",
                    ErrorText = "#FF6666",
                    SchematicPaper = "#000000",
                    SchematicInk = "#FFFFFF",
                    SchematicFrame = "#FFFFFF",
                    SchematicInferred = "#FFFF00",
                    SchematicNote = "#E0E0E0",
                    SchematicSelection = "#00FFFF",
                    SchematicOpen = "#FF0000",
                }));
        }
    }

    /// <summary>
    /// One selectable theme: id, name, Avalonia variant and colours.
    /// </summary>
    public sealed class ThemeDefinition
    {
        /// <summary>
        /// Creates a theme.
        /// </summary>
        /// <param name="id">
        /// The id saved in the settings file.
        /// </param>
        /// <param name="displayName">
        /// The name shown in the menu.
        /// </param>
        /// <param name="variant">
        /// The Avalonia theme variant for this theme.
        /// </param>
        /// <param name="palette">
        /// The colours.
        /// </param>
        public ThemeDefinition(string id, string displayName, ThemeVariant variant, Palette palette)
        {
            Id = id;
            DisplayName = displayName;
            Variant = variant;
            Palette = palette;
        }

        /// <summary>
        /// The id saved in the settings file ("Light", "SolarizedDark").
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The name shown in the menu.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The Avalonia theme variant that selects this theme's brushes.
        /// </summary>
        public ThemeVariant Variant { get; }

        /// <summary>
        /// The colours.
        /// </summary>
        public Palette Palette { get; }
    }

    /// <summary>
    /// A theme's colours as hex strings. The order of <see cref="ToArray"/> must match
    /// ThemeManager's brush key list.
    /// </summary>
    public sealed class Palette
    {
        /// <summary>
        /// Window background.
        /// </summary>
        public string BackgroundPrimary = "#000000";

        /// <summary>
        /// Panel background.
        /// </summary>
        public string BackgroundSecondary = "#000000";

        /// <summary>
        /// Alternative surface (headers).
        /// </summary>
        public string SurfaceAlt = "#000000";

        /// <summary>
        /// Thin borders.
        /// </summary>
        public string BorderLight = "#000000";

        /// <summary>
        /// Strong borders.
        /// </summary>
        public string BorderDark = "#000000";

        /// <summary>
        /// The theme's accent (not blue in every theme; the name is RetroCommander's).
        /// </summary>
        public string AccentBlue = "#000000";

        /// <summary>
        /// Main text.
        /// </summary>
        public string TextPrimary = "#FFFFFF";

        /// <summary>
        /// Secondary text.
        /// </summary>
        public string TextSecondary = "#FFFFFF";

        /// <summary>
        /// Selected row.
        /// </summary>
        public string SelectionHighlight = "#000000";

        /// <summary>
        /// Row under the mouse.
        /// </summary>
        public string HoverHighlight = "#000000";

        /// <summary>
        /// Side panel background.
        /// </summary>
        public string SidebarBackground = "#000000";

        /// <summary>
        /// Side panel separators.
        /// </summary>
        public string SidebarSeparator = "#000000";

        /// <summary>
        /// Warning banner background.
        /// </summary>
        public string WarningBackground = "#000000";

        /// <summary>
        /// Warning banner border.
        /// </summary>
        public string WarningBorder = "#000000";

        /// <summary>
        /// Success banner background.
        /// </summary>
        public string SuccessBackground = "#000000";

        /// <summary>
        /// Success banner border.
        /// </summary>
        public string SuccessBorder = "#000000";

        /// <summary>
        /// Error banner background.
        /// </summary>
        public string ErrorBackground = "#000000";

        /// <summary>
        /// Error banner border.
        /// </summary>
        public string ErrorBorder = "#000000";

        /// <summary>
        /// Error text.
        /// </summary>
        public string ErrorText = "#FFFFFF";

        /// <summary>
        /// Paper behind the drawing.
        /// </summary>
        public string SchematicPaper = "#FFFFFF";

        /// <summary>
        /// Lines and text read off the drawing.
        /// </summary>
        public string SchematicInk = "#000000";

        /// <summary>
        /// The frame round each chip.
        /// </summary>
        public string SchematicFrame = "#888888";

        /// <summary>
        /// Anything inferred, not read.
        /// </summary>
        public string SchematicInferred = "#B06000";

        /// <summary>
        /// Secondary drawing text.
        /// </summary>
        public string SchematicNote = "#666666";

        /// <summary>
        /// The picked item.
        /// </summary>
        public string SchematicSelection = "#0078D7";

        /// <summary>
        /// Anything OPEN, not known.
        /// </summary>
        public string SchematicOpen = "#D00000";

        /// <summary>
        /// A simulated signal with no value yet.
        /// </summary>
        public string ValueUnknown = "#808080";

        /// <summary>
        /// A simulated 0.
        /// </summary>
        public string ValueZero = "#006400";

        /// <summary>
        /// A simulated 1.
        /// </summary>
        public string ValueOne = "#00D200";

        /// <summary>
        /// A signal nothing drives.
        /// </summary>
        public string ValueNotDriven = "#2828FF";

        /// <summary>
        /// A signal driven both ways.
        /// </summary>
        public string ValueConflict = "#FF0000";

        /// <summary>
        /// The traced signal.
        /// </summary>
        public string SchematicTrace = "#FF8C00";

        /// <summary>
        /// A wire; null uses the theme's ink.
        /// </summary>
        public string? SchematicWire;

        /// <summary>
        /// A bus line; null uses the theme's ink.
        /// </summary>
        public string? SchematicBus;

        /// <summary>
        /// A net label; null uses the theme's ink.
        /// </summary>
        public string? SchematicNetLabel;

        /// <summary>
        /// An accent button under the pointer; null uses the accent colour.
        /// </summary>
        public string? AccentHover;

        /// <summary>
        /// The colours in brush-key order.
        /// </summary>
        /// <returns>
        /// The hex colours, one per brush key, in the key list's order.
        /// </returns>
        public string[] ToArray()
        {
            return new[]
            {
                BackgroundPrimary, BackgroundSecondary, SurfaceAlt, BorderLight, BorderDark, AccentBlue,
                TextPrimary, TextSecondary, SelectionHighlight, HoverHighlight, SidebarBackground,
                SidebarSeparator, WarningBackground, WarningBorder, SuccessBackground, SuccessBorder,
                ErrorBackground, ErrorBorder, ErrorText,
                SchematicPaper, SchematicInk, SchematicFrame, SchematicInferred, SchematicNote, SchematicSelection, SchematicOpen,
                ValueUnknown, ValueZero, ValueOne, ValueNotDriven, ValueConflict, SchematicTrace,
                SchematicWire ?? SchematicInk, SchematicBus ?? SchematicInk, SchematicNetLabel ?? SchematicInk,
                AccentHover ?? AccentBlue,
            };
        }
    }
}
