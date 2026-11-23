//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using RetroSchematic.Model;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The machine bus in the window: the title bar, the new-sheet choice and the Settings row.
    /// </summary>
    [TestFixture]
    public class MachineBusWindowTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

        [AvaloniaTest]
        public void TheTitleBar_SaysTheBus()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet2A50);
            Assert.That(w.Title, Does.Contain("(NORD-10)"));
            w.Close();
        }

        [AvaloniaTest]
        public void ANewSheet_TakesItsBus_AndAnUnknownOneWarns()
        {
            MainWindow w = new();
            w.Show();
            w.NewSheet(MachineBus.Nd100);
            Assert.That(w.Session!.Bus, Is.EqualTo(MachineBus.Nd100));
            Assert.That(w.Title, Does.Contain("(ND-100)"));
            w.NewSheet();
            Assert.That(w.Title, Does.Contain("(unknown bus)"));
            Assert.That(w.FindControl<TextBlock>("StatusText")!.Text, Does.Contain("does not say which machine bus"));
            w.Close();
        }

        [AvaloniaTest]
        public void TheSettingsWindow_ShowsAndChangesTheSheetsBus()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet2A50);
            SettingsWindow s = w.OpenSettings();
            Assert.That(s.SheetControls["bus"].SelectedItem, Is.EqualTo("NORD-10"));
            s.SheetControls["bus"].SelectedIndex = 1;
            s.ApplyChoices();
            s.Close();
            Assert.That(w.Session!.Bus, Is.EqualTo(MachineBus.Nd100));
            Assert.That(w.Session.Editor.Modified, Is.True);
            Assert.That(w.Title, Does.Contain("(ND-100)"));
            w.Close();
        }
    }
}
