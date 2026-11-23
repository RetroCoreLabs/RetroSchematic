//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia;
using Avalonia.Headless;
using NUnit.Framework;
using RetroSchematic.Desktop;
using RetroSchematic.Desktop.Themes;

[assembly: AvaloniaTestApplication(typeof(RetroSchematic.Desktop.Tests.TestAppBuilder))]

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The headless Avalonia app the window tests run in. Real Skia drawing (not the headless
    /// stand-in), so a test can capture the drawn frame and read its pixels.
    /// </summary>
    public static class TestAppBuilder
    {
        /// <summary>A private settings folder, so no test reads or changes the user's own preferences.</summary>
        public static readonly string SettingsFolder = Path.Combine(Path.GetTempPath(), "RetroSchematic.Desktop.Tests-" + System.Environment.ProcessId);

        /// <summary>Used by Avalonia.Headless.NUnit.</summary>
        public static AppBuilder BuildAvaloniaApp()
        {
            // before App starts: ThemeManager reads the settings during start-up
            UiSettings.DirectoryOverride = SettingsFolder;
            UiSettings.Reset();
            return AppBuilder.Configure<App>()
                .UseSkia()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
        }
    }

    /// <summary>
    /// Removes the private settings folder after the run. A [TearDown] does not run under
    /// [AvaloniaTest], so the cleanup is here, once for the whole assembly.
    /// </summary>
    [SetUpFixture]
    public sealed class SettingsCleanup
    {
        /// <summary>Delete the settings folder.</summary>
        [OneTimeTearDown]
        public void RemoveSettingsFolder()
        {
            if (Directory.Exists(TestAppBuilder.SettingsFolder))
                Directory.Delete(TestAppBuilder.SettingsFolder, true);
        }
    }
}
