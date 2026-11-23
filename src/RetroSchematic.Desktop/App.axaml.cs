//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
// RetroSchematic - viewer for Norsk Data circuit diagrams
//

using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RetroSchematic.Desktop.Themes;

namespace RetroSchematic.Desktop
{
    /// <summary>The application: loads the styles, sets up the theme, opens the main window.</summary>
    public partial class App : Application
    {
        /// <inheritdoc/>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <inheritdoc/>
        public override void OnFrameworkInitializationCompleted()
        {
            // brushes first, so the first frame already has the right colours
            ThemeManager.Instance.Initialize(this);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindow window = new();
                // a sheet named on the command line, else the one open last time
                string? path = desktop.Args != null && desktop.Args.Length > 0 ? desktop.Args[0] : UiSettings.Instance.LastSheet;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    window.OpenSheet(path);
                desktop.MainWindow = window;
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
