//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using NUnit.Framework;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// Every View switch, used the way a person uses it (a real mouse click on the toolbar
    /// button, or the menu item), goes off, on and off again, and the drawing has or lacks what
    /// it shows each time. Written for the bug where port arrows could be turned off but not
    /// back on.
    /// </summary>
    [TestFixture]
    public class ViewToggleTests
    {
        // the blocks demo has no buses of its own; 2A50 has two
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
        private static string Blocks => RetroSchematic.Tests.TestCards.Blocks2A50();

        [AvaloniaTest]
        public void PortArrows_OffOnOff_ByToolbarAndMenu() => OffOnOff(Blocks, "TbPortArrows", "PortArrowsItem", w => Count(w, DrawRole.PortArrow) > 0);

        [AvaloniaTest]
        public void PinNumbers_OffOnOff_ByToolbarAndMenu() => OffOnOff(Blocks, "TbPinNumbers", "PinNumbersItem", w => Count(w, DrawRole.PinNumber) > 0);

        [AvaloniaTest]
        public void BoardPositions_OffOnOff_ByToolbarAndMenu() => OffOnOff(Blocks, "TbBoardPositions", "BoardPositionsItem", w => Count(w, DrawRole.BoardPosition) > 0);

        [AvaloniaTest]
        public void ShowBuses_OffOnOff_ByToolbarAndMenu() => OffOnOff(Sheet2A50, "TbShowBuses", "ShowBusesItem", w => Count(w, DrawRole.Bus) > 0);

        [AvaloniaTest]
        public void PortArrows_TurnedOffForThisSheet_ComeBackOnWithTheToolbar()
        {
            // the owner's case: the sheet's own value says off (set in Settings, This sheet);
            // the toolbar must still bring the arrows back
            MainWindow w = Open(Blocks);
            SettingsWindow s = w.OpenSettings();
            s.SheetControls["arrows"].SelectedIndex = 2;
            s.ApplyChoices();
            s.Close();
            ToggleButton tb = w.FindControl<ToggleButton>("TbPortArrows")!;
            Assert.That(Count(w, DrawRole.PortArrow), Is.EqualTo(0));
            Assert.That(tb.IsChecked, Is.False);
            ClickButton(w, tb);
            Assert.That(Count(w, DrawRole.PortArrow), Is.GreaterThan(0), "on again");
            Assert.That(tb.IsChecked, Is.True);
            ClickButton(w, tb);
            Assert.That(Count(w, DrawRole.PortArrow), Is.EqualTo(0), "off again");
            w.Close();
        }

        [AvaloniaTest]
        public void TracePulse_OffOnOff_InTheSettingsWindow()
        {
            MainWindow w = Open(Blocks);
            Controls.SchematicCanvas canvas = w.FindControl<Controls.SchematicCanvas>("Canvas")!;
            for (int round = 0; round < 3; round++)
            {
                bool want = round % 2 == 1;
                SettingsWindow s = w.OpenSettings();
                s.DefaultControls["pulse"].SelectedIndex = want ? 0 : 1;
                s.ApplyChoices();
                s.Close();
                Assert.That(canvas.TracePulse, Is.EqualTo(want), "round " + round);
            }
            Restore(w);
        }

        /// <summary>
        /// Starts with the switch on (sheet: the one that has what it shows), then clicks it off, on, off with the toolbar button and
        /// again with the menu item, checking the button, the menu tick and the drawing each time.
        /// </summary>
        private static void OffOnOff(string sheet, string button, string item, Func<MainWindow, bool> shown)
        {
            MainWindow w = Open(sheet);
            ToggleButton tb = w.FindControl<ToggleButton>(button)!;
            MenuItem mi = w.FindControl<MenuItem>(item)!;
            Assert.That(shown(w), Is.True, "on at the start");
            Assert.That(tb.IsChecked, Is.True);
            for (int round = 0; round < 3; round++)
            {
                ClickButton(w, tb);
                bool want = round % 2 == 1;
                Assert.That(shown(w), Is.EqualTo(want), "toolbar, round " + round + ": drawing");
                Assert.That(tb.IsChecked, Is.EqualTo(want), "toolbar, round " + round + ": button");
                Assert.That(mi.IsChecked, Is.EqualTo(want), "toolbar, round " + round + ": menu tick");
            }
            // now off; the menu item three times: on, off, on
            for (int round = 0; round < 3; round++)
            {
                ClickMenu(mi);
                bool want = round % 2 == 0;
                Assert.That(shown(w), Is.EqualTo(want), "menu, round " + round + ": drawing");
                Assert.That(tb.IsChecked, Is.EqualTo(want), "menu, round " + round + ": button");
                Assert.That(mi.IsChecked, Is.EqualTo(want), "menu, round " + round + ": menu tick");
            }
            w.Close();
        }

        private static MainWindow Open(string sheet)
        {
            // every switch starts on
            UiSettings.Instance.PortArrows = true;
            UiSettings.Instance.PinNumbers = true;
            UiSettings.Instance.BoardPositions = true;
            UiSettings.Instance.ShowBuses = true;
            UiSettings.Instance.TracePulse = true;
            UiSettings.Instance.Save();
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(sheet);
            return w;
        }

        private static void Restore(MainWindow w)
        {
            UiSettings.Instance.TracePulse = true;
            UiSettings.Instance.Save();
            w.Close();
        }

        /// <summary>
        /// A real left click in the middle of a button, through the headless input.
        /// </summary>
        private static void ClickButton(Window w, Control c)
        {
            Point centre = c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), w)!.Value;
            w.MouseDown(centre, MouseButton.Left);
            w.MouseUp(centre, MouseButton.Left);
        }

        /// <summary>
        /// What a menu click does: a CheckBox menu item turns its own tick over, then raises Click.
        /// </summary>
        private static void ClickMenu(MenuItem mi)
        {
            mi.IsChecked = !mi.IsChecked;
            mi.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        private static int Count(MainWindow w, DrawRole role)
        {
            SchematicDrawing d = w.Session!.Drawing;
            int n = 0;
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Role == role)
                    n++;
            }
            return n;
        }
    }
}
