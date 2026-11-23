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
        /// The last sheet file opened, re-opened at start.
        /// </summary>
        public string? LastSheet { get; set; }

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
