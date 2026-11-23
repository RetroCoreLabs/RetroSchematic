//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Every display option with a value: what the viewer shows. The settings window has two
    /// columns of these: the defaults for all sheets (kept by the app) and the sheet's own values
    /// (kept in the sheet file's "display" section, see <see cref="SheetDisplay"/>), where any
    /// value may be "use default". <see cref="Resolve"/> gives what applies.
    /// </summary>
    public sealed class DisplaySettings
    {
        /// <summary>
        /// How active-low and active-high names are written.
        /// </summary>
        public SignalNameStyle NameStyle { get; set; } = SignalNames.Default;

        /// <summary>
        /// ND 1976 symbols or modern ones.
        /// </summary>
        public SymbolStyle Symbols { get; set; } = SymbolStyle.NorskData;

        /// <summary>
        /// The colour theme by name.
        /// </summary>
        public string Theme { get; set; } = "Light";

        /// <summary>
        /// Show pin numbers.
        /// </summary>
        public bool PinNumbers { get; set; } = true;

        /// <summary>
        /// Show board positions before chip types.
        /// </summary>
        public bool BoardPositions { get; set; } = true;

        /// <summary>
        /// Show direction arrows on block ports.
        /// </summary>
        public bool PortArrows { get; set; } = true;

        /// <summary>
        /// Draw the sheet's buses.
        /// </summary>
        public bool Buses { get; set; } = true;

        /// <summary>
        /// A traced signal pulses.
        /// </summary>
        public bool TracePulse { get; set; } = true;

        /// <summary>
        /// A copy.
        /// </summary>
        /// <returns>
        /// The copy.
        /// </returns>
        public DisplaySettings Clone() => (DisplaySettings)MemberwiseClone();

        /// <summary>
        /// What applies to a sheet: each of the sheet's own values where it has one, the default
        /// for every value it leaves out. A sheet value that cannot be read (an unknown style or
        /// theme name) is left out too, so the default applies.
        /// </summary>
        /// <param name="defaults">
        /// The defaults for all sheets.
        /// </param>
        /// <param name="sheet">
        /// The sheet's own values, or null for none.
        /// </param>
        /// <returns>
        /// The settings that apply.
        /// </returns>
        public static DisplaySettings Resolve(DisplaySettings defaults, SheetDisplay? sheet)
        {
            DisplaySettings r = defaults.Clone();
            if (sheet == null)
                return r;
            if (SignalNames.TryParseStyle(sheet.NameStyle, out SignalNameStyle ns))
                r.NameStyle = ns;
            if (TryParseSymbols(sheet.Symbols, out SymbolStyle sy))
                r.Symbols = sy;
            if (sheet.Theme != null && SchematicTheme.Find(sheet.Theme) != null)
                r.Theme = SchematicTheme.Find(sheet.Theme)!.Id;
            r.PinNumbers = sheet.PinNumbers ?? r.PinNumbers;
            r.BoardPositions = sheet.BoardPositions ?? r.BoardPositions;
            r.PortArrows = sheet.PortArrows ?? r.PortArrows;
            r.Buses = sheet.Buses ?? r.Buses;
            r.TracePulse = sheet.TracePulse ?? r.TracePulse;
            return r;
        }

        /// <summary>
        /// A name style as written in the file.
        /// </summary>
        /// <param name="style">
        /// The style.
        /// </param>
        /// <returns>
        /// "slash", "tilde" or "subscript".
        /// </returns>
        public static string NameStyleText(SignalNameStyle style) => style.ToString().ToLowerInvariant();

        /// <summary>
        /// A symbol style as written in the file.
        /// </summary>
        /// <param name="style">
        /// The style.
        /// </param>
        /// <returns>
        /// "nd" or "modern".
        /// </returns>
        public static string SymbolsText(SymbolStyle style) => style == SymbolStyle.Modern ? "modern" : "nd";

        /// <summary>
        /// Reads a symbol style as written in the file ("nd" or "modern").
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="style">
        /// The style read.
        /// </param>
        /// <returns>
        /// False when the text is neither.
        /// </returns>
        public static bool TryParseSymbols(string? text, out SymbolStyle style)
        {
            style = SymbolStyle.NorskData;
            if (string.Equals(text, "nd", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(text, "modern", StringComparison.OrdinalIgnoreCase))
            {
                style = SymbolStyle.Modern;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// The settings-window side of a session: the defaults it was given, the sheet's own values,
    /// and what applies.
    /// </summary>
    public sealed partial class SheetSession
    {
        private DisplaySettings _defaults = new();

        /// <summary>
        /// What applies to this sheet now (see <see cref="DisplaySettings.Resolve"/>).
        /// </summary>
        public DisplaySettings Display { get; private set; } = new();

        /// <summary>
        /// The defaults for all sheets this session was last given.
        /// </summary>
        public DisplaySettings Defaults => _defaults.Clone();

        /// <summary>
        /// The sheet's own display values (a copy; change them with <see cref="SetSheetDisplay"/>).
        /// </summary>
        public SheetDisplay SheetDisplay
        {
            get
            {
                SheetDisplay? s = Editor.Root.Display;
                return s == null ? new SheetDisplay() : Copy(s);
            }
        }

        /// <summary>
        /// Gives the session the app's defaults; the sheet's own values still win. Draws again.
        /// </summary>
        /// <param name="defaults">
        /// The defaults for all sheets.
        /// </param>
        public void SetDefaults(DisplaySettings defaults)
        {
            _defaults = defaults.Clone();
            ApplyDisplay();
        }

        /// <summary>
        /// Sets the sheet's own display values (null fields mean "use default"). This is a change
        /// to the sheet: it is marked as needing saving and can be undone. It never changes what is
        /// connected. Allowed in read-only mode too, since it is display only.
        /// </summary>
        /// <param name="sheet">
        /// The new values.
        /// </param>
        public void SetSheetDisplay(SheetDisplay sheet)
        {
            SheetDisplay? next = sheet.IsEmpty() ? null : Copy(sheet);
            Editor.SetDisplay(next);
            ApplyDisplay();
        }

        /// <summary>
        /// Puts what applies into the drawing options and draws again.
        /// </summary>
        private void ApplyDisplay()
        {
            Display = DisplaySettings.Resolve(_defaults, Editor.Root.Display);
            Style = Display.Symbols;
            _options.NameStyle = Display.NameStyle;
            _options.ShowPinNumbers = Display.PinNumbers;
            _options.ShowBoardPositions = Display.BoardPositions;
            _options.ShowPortArrows = Display.PortArrows;
            _options.ShowBuses = Display.Buses;
            Relayout();
        }

        private static SheetDisplay Copy(SheetDisplay s) => new()
        {
            NameStyle = s.NameStyle, Symbols = s.Symbols, Theme = s.Theme, PinNumbers = s.PinNumbers,
            BoardPositions = s.BoardPositions, PortArrows = s.PortArrows, Buses = s.Buses, TracePulse = s.TracePulse,
        };
    }
}
