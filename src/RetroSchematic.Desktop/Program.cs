//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
// RetroSchematic - viewer for Norsk Data circuit diagrams
//

using System;
using Avalonia;

namespace RetroSchematic.Desktop
{
    /// <summary>Start-up: <c>RetroSchematic [sheet.json]</c>.</summary>
    internal static class Program
    {
        /// <summary>Starts the window. Nothing touching Avalonia may run before this.</summary>
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        /// <summary>The Avalonia set-up; also used by the designer and the headless tests.</summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
    }
}
