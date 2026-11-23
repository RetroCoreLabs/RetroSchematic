//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.NUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using NUnit.Framework;
using RetroSchematic.Desktop.Themes;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The toolbar: every button has an icon and a tooltip, the icons follow the theme, and the
    /// Trace button traces in read-only mode and fills the side panel.
    /// </summary>
    [TestFixture]
    public class ToolbarTests
    {
        private static readonly string Sheet = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory,
            "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

        [AvaloniaTest]
        public void EveryToolbarButton_HasAnIconAndATooltip()
        {
            MainWindow w = new();
            w.Show();
            List<Button> buttons = ToolbarButtons(w);
            Assert.That(buttons.Count, Is.GreaterThanOrEqualTo(25));
            for (int i = 0; i < buttons.Count; i++)
            {
                Assert.That(buttons[i].Content, Is.InstanceOf<Path>(), buttons[i].Name + " has no vector icon");
                Assert.That(ToolTip.GetTip(buttons[i]) as string, Is.Not.Null.And.Not.Empty, buttons[i].Name + " has no tooltip");
            }
            w.Close();
        }

        [AvaloniaTest]
        public void TheIcons_TakeTheThemesTextColour_AndFollowASwitch()
        {
            ThemeManager.Instance.Apply("Light", persist: false);
            MainWindow w = new();
            w.Show();
            Path icon = (Path)w.FindControl<Button>("TbNew")!.Content!;
            Assert.That(((ISolidColorBrush)icon.Stroke!).Color, Is.EqualTo(Color.Parse(ThemeManager.Instance.Themes[0].Palette.TextPrimary)));
            ThemeManager.Instance.Apply("Dark", persist: false);
            Assert.That(((ISolidColorBrush)icon.Stroke!).Color, Is.EqualTo(Color.Parse(ThemeManager.Instance.Themes[1].Palette.TextPrimary)),
                "the icon kept the Light colour after switching to Dark");
            ThemeManager.Instance.Apply("Light", persist: false);
            w.Close();
        }

        [AvaloniaTest]
        public void TraceButton_WorksReadOnly_AndListsThePins()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet);
            Assert.That(w.Session!.Editor.ReadOnly, Is.True);
            ToggleButton trace = w.FindControl<ToggleButton>("TbTrace")!;
            Assert.That(trace.IsEnabled, Is.True, "Trace must be usable without edit mode");
            w.PickTool(trace);
            SchematicCanvasAccess(w).Tool!.PointerDown(-1000, -1000);
            Assert.That(w.Session.Traced, Is.Empty);
            string net = w.Session.Netlist.Nets[0].Name;
            w.Session.Trace(net);
            Assert.That(w.FindControl<StackPanel>("TracePanel")!.IsVisible, Is.True);
            ListBox pins = w.FindControl<ListBox>("TraceList")!;
            Assert.That(((List<string>)pins.ItemsSource!).Count, Is.EqualTo(w.Session.Netlist.Nets[0].Pins.Count));
            // choosing Select clears the trace
            w.PickTool(w.FindControl<ToggleButton>("TbSelect")!);
            Assert.That(w.Session.Traced, Is.Empty);
            Assert.That(w.FindControl<StackPanel>("TracePanel")!.IsVisible, Is.False);
            w.Close();
        }

        private static Controls.SchematicCanvas SchematicCanvasAccess(MainWindow w) => w.FindControl<Controls.SchematicCanvas>("Canvas")!;

        private static List<Button> ToolbarButtons(MainWindow w)
        {
            List<Button> list = new();
            Border bar = w.FindControl<Button>("TbNew")!.FindLogicalAncestorOfType<Border>()!;
            StackPanel row = (StackPanel)bar.Child!;
            for (int i = 0; i < row.Children.Count; i++)
            {
                if (row.Children[i] is Button b)
                    list.Add(b);
            }
            return list;
        }
    }
}
