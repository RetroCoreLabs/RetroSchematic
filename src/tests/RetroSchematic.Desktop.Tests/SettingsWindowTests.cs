//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The settings window and the Show buses switch in the window: defaults are saved and
    /// shown, a sheet's own value wins and marks the sheet changed, "Use default" gives the
    /// default back, and connections never change.
    /// </summary>
    [TestFixture]
    public class SettingsWindowTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

        [AvaloniaTest]
        public void TheShowBusesSwitch_HidesBuses_AndNeverChangesTheSheet()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet2A50);
            string saved = NetlistReader.Write(w.Session!.Netlist);
            bool start = UiSettings.Instance.ShowBuses;
            ToggleButton tb = w.FindControl<ToggleButton>("TbShowBuses")!;
            Assert.That(tb.IsChecked, Is.EqualTo(start));
            w.ToggleShowBuses();
            Assert.That(UiSettings.Instance.ShowBuses, Is.EqualTo(!start));
            Assert.That(w.Session.ShowBuses, Is.EqualTo(!start));
            Assert.That(tb.IsChecked, Is.EqualTo(!start));
            Assert.That(w.FindControl<MenuItem>("ShowBusesItem")!.IsChecked, Is.EqualTo(!start));
            Assert.That(w.Session.Editor.Modified, Is.False, "a default is not a change to the sheet");
            Assert.That(NetlistReader.Write(w.Session.Netlist), Is.EqualTo(saved));
            w.ToggleShowBuses();
            Assert.That(UiSettings.Instance.ShowBuses, Is.EqualTo(start));
            w.Close();
        }

        [AvaloniaTest]
        public void TheSettingsWindow_SetsDefaults_AndThisSheet_WithUseDefault()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet2A50);
            string connections = Hierarchy.Connections(w.Session!.Editor.Root);
            SettingsWindow s = w.OpenSettings();
            Assert.That(s.SheetControls["names"].SelectedIndex, Is.EqualTo(0), "the sheet has no value: Use default");

            // default: tilde
            s.DefaultControls["names"].SelectedIndex = (int)SignalNameStyle.Tilde;
            s.ApplyChoices();
            Assert.That(UiSettings.Instance.NameStyle, Is.EqualTo("tilde"));
            Assert.That(w.Session.NameStyle, Is.EqualTo(SignalNameStyle.Tilde));
            Assert.That(w.Session.Editor.Modified, Is.False);

            // this sheet: subscript, and buses off
            s.SheetControls["names"].SelectedIndex = (int)SignalNameStyle.Subscript + 1;
            s.SheetControls["buses"].SelectedIndex = 2;
            s.ApplyChoices();
            Assert.That(w.Session.NameStyle, Is.EqualTo(SignalNameStyle.Subscript), "the sheet's value wins");
            Assert.That(w.Session.ShowBuses, Is.False);
            Assert.That(w.Session.Editor.Modified, Is.True, "needs saving");
            Assert.That(w.Session.SheetDisplay.NameStyle, Is.EqualTo("subscript"));
            Assert.That(w.FindControl<ToggleButton>("TbShowBuses")!.IsChecked, Is.False, "the toolbar shows what applies");

            // back to Use default
            s.SheetControls["names"].SelectedIndex = 0;
            s.SheetControls["buses"].SelectedIndex = 0;
            s.ApplyChoices();
            Assert.That(w.Session.NameStyle, Is.EqualTo(SignalNameStyle.Tilde));
            Assert.That(w.Session.Editor.Root.Display, Is.Null);
            Assert.That(Hierarchy.Connections(w.Session.Editor.Root), Is.EqualTo(connections));

            s.DefaultControls["names"].SelectedIndex = (int)SignalNameStyle.Slash;
            s.ApplyChoices();
            s.Close();
            w.Close();
        }

        [AvaloniaTest]
        public void TheDefaults_AreSavedAndReadBack()
        {
            UiSettings.Instance.NameStyle = "subscript";
            UiSettings.Instance.TracePulse = false;
            Assert.That(UiSettings.Instance.Save(), Is.Null);
            UiSettings.Reset();
            Assert.That(UiSettings.Instance.NameStyle, Is.EqualTo("subscript"));
            Assert.That(UiSettings.Instance.ToDefaults().TracePulse, Is.False);
            UiSettings.Instance.NameStyle = "slash";
            UiSettings.Instance.TracePulse = true;
            UiSettings.Instance.Save();
        }
    }
}
