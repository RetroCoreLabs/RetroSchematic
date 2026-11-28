//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
// RetroSchematic - viewer for Norsk Data circuit diagrams
//

using System;
using System.IO;
using System.Text.Json;

namespace RetroSchematic.Desktop.Themes
{
    /// <summary>
    /// Saved preferences, one JSON file at %APPDATA%\RetroSchematic\ui-settings.json (the same
    /// arrangement as RetroCommander). Change a property and call <see cref="Save"/>.
    /// </summary>
    public sealed class UiSettings
    {
        private static UiSettings? s_instance;

        /// <summary>
        /// For tests: when set, settings are read from and written to this folder instead of
        /// %APPDATA%, so a test run never touches the user's own preferences.
        /// </summary>
        internal static string? DirectoryOverride { get; set; }

        /// <summary>
        /// The loaded settings (read from disk on first use).
        /// </summary>
        public static UiSettings Instance => s_instance ??= Load();

        /// <summary>
        /// Theme id (see <see cref="ThemeManager.Themes"/>); null = Light.
        /// </summary>
        public string? Theme { get; set; }

        /// <summary>
        /// True to draw with ND's 1976 symbols, false for modern ones.
        /// </summary>
        public bool NdSymbols { get; set; } = true;

        /// <summary>
        /// True to draw direction arrows on block port pins.
        /// </summary>
        public bool PortArrows { get; set; } = true;

        /// <summary>
        /// Default name style for active-low signals: "slash" (/NAME), "tilde" (~NAME) or
        /// "subscript" (ND's 0 and 1).
        /// </summary>
        public string NameStyle { get; set; } = "slash";

        /// <summary>
        /// Default: show pin numbers.
        /// </summary>
        public bool PinNumbers { get; set; } = true;

        /// <summary>
        /// Default: show board positions.
        /// </summary>
        public bool BoardPositions { get; set; } = true;

        /// <summary>
        /// Default: draw the sheet's buses.
        /// </summary>
        public bool ShowBuses { get; set; } = true;

        /// <summary>
        /// Default: a traced signal pulses.
        /// </summary>
        public bool TracePulse { get; set; } = true;

        /// <summary>
        /// Default: how thick every line is drawn, as a factor of the normal widths (1 = normal).
        /// </summary>
        public double LineWidth { get; set; } = 1;

        /// <summary>
        /// The defaults for all sheets as the library's settings.
        /// </summary>
        /// <returns>
        /// The defaults.
        /// </returns>
        public RetroSchematic.Render.DisplaySettings ToDefaults()
        {
            RetroSchematic.Render.DisplaySettings d = new()
            {
                Symbols = NdSymbols ? RetroSchematic.Render.SymbolStyle.NorskData : RetroSchematic.Render.SymbolStyle.Modern,
                Theme = Theme ?? "Light",
                PinNumbers = PinNumbers,
                BoardPositions = BoardPositions,
                PortArrows = PortArrows,
                Buses = ShowBuses,
                TracePulse = TracePulse,
                LineWidth = RetroSchematic.Render.DisplaySettings.ClampLineWidth(LineWidth),
            };
            if (RetroSchematic.Render.SignalNames.TryParseStyle(NameStyle, out RetroSchematic.Render.SignalNameStyle ns))
                d.NameStyle = ns;
            return d;
        }

        /// <summary>
        /// Takes new defaults for all sheets (not saved until <see cref="Save"/>).
        /// </summary>
        /// <param name="d">
        /// The defaults.
        /// </param>
        public void FromDefaults(RetroSchematic.Render.DisplaySettings d)
        {
            NdSymbols = d.Symbols == RetroSchematic.Render.SymbolStyle.NorskData;
            Theme = d.Theme;
            PinNumbers = d.PinNumbers;
            BoardPositions = d.BoardPositions;
            PortArrows = d.PortArrows;
            ShowBuses = d.Buses;
            TracePulse = d.TracePulse;
            LineWidth = d.LineWidth;
            NameStyle = RetroSchematic.Render.DisplaySettings.NameStyleText(d.NameStyle);
        }

        /// <summary>
        /// The last sheet file opened, re-opened at start.
        /// </summary>
        public string? LastSheet { get; set; }

        /// <summary>
        /// The sheets opened most recently, newest first, at most <see cref="RecentMax"/>.
        /// </summary>
        public System.Collections.Generic.List<string> RecentSheets { get; set; } = new();

        /// <summary>
        /// How many sheets the recent list keeps.
        /// </summary>
        public const int RecentMax = 10;

        /// <summary>
        /// Puts a sheet first in the recent list (moved up when it is already there; the oldest
        /// falls off past <see cref="RecentMax"/>). Paths are compared without case, as Windows does.
        /// </summary>
        /// <param name="path">
        /// The sheet file.
        /// </param>
        public void AddRecent(string path)
        {
            string full = Path.GetFullPath(path);
            for (int i = RecentSheets.Count - 1; i >= 0; i--)
            {
                if (string.Equals(RecentSheets[i], full, StringComparison.OrdinalIgnoreCase))
                    RecentSheets.RemoveAt(i);
            }
            RecentSheets.Insert(0, full);
            if (RecentSheets.Count > RecentMax)
                RecentSheets.RemoveRange(RecentMax, RecentSheets.Count - RecentMax);
        }

        /// <summary>
        /// Why the settings file could not be read, or null. The program still starts with the
        /// defaults, and the main window shows this text in its status bar, so a broken settings
        /// file is seen instead of silently ignored.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string? LoadProblem { get; private set; }

        /// <summary>
        /// Writes the settings. Returns null, or the reason it failed (shown in the status bar).
        /// </summary>
        /// <returns>
        /// Null when saved, otherwise why it failed.
        /// </returns>
        public string? Save()
        {
            try
            {
                string file = GetSettingsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
                return null;
            }
            catch (IOException e)
            {
                return "Settings not saved: " + e.Message;
            }
            catch (UnauthorizedAccessException e)
            {
                return "Settings not saved: " + e.Message;
            }
        }

        /// <summary>
        /// For tests: forget the loaded settings so the next use reads them again.
        /// </summary>
        internal static void Reset() => s_instance = null;

        private static string GetSettingsPath()
        {
            string dir = DirectoryOverride ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RetroSchematic");
            return Path.Combine(dir, "ui-settings.json");
        }

        private static UiSettings Load()
        {
            string file = GetSettingsPath();
            if (!File.Exists(file))
                return new UiSettings();
            try
            {
                return JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(file)) ?? new UiSettings();
            }
            catch (JsonException e)
            {
                return new UiSettings { LoadProblem = "Settings file " + file + " could not be read (" + e.Message + "); defaults used." };
            }
            catch (IOException e)
            {
                return new UiSettings { LoadProblem = "Settings file " + file + " could not be read (" + e.Message + "); defaults used." };
            }
        }
    }
}
