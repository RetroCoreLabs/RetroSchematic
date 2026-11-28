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

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The block commands in the window: Make block is off until parts are picked in edit
    /// mode, the dialog makes the block with a renamed port, the right-click menu opens and
    /// dissolves, the bar above the drawing shows where you are, Up one level comes back, and
    /// the port arrow toggle follows the setting.
    /// </summary>
    [TestFixture]
    public class BlockWindowTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [AvaloniaTest]
        public void MakeBlock_Open_Up_AndDissolve_ThroughTheWindow()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet2A50);
            Button make = w.FindControl<Button>("TbMakeBlock")!;
            Button up = w.FindControl<Button>("TbUpLevel")!;
            string before = Hierarchy.Connections(w.Session!.Editor.Root);
            w.Session.TogglePick("part:13A");
            w.Session.TogglePick("part:15A");
            Assert.That(make.IsEnabled, Is.False, "read-only: no blocks");
            Assert.That(w.OpenMakeBlockDialog(), Is.Null);
            w.SetEditMode(true);
            Assert.That(make.IsEnabled, Is.True, "edit mode and parts picked");

            MakeBlockWindow dialog = w.OpenMakeBlockDialog()!;
            Assert.That(dialog.TryCreate(), Is.False, "no name: refused, reason shown");
            Assert.That(dialog.FindControl<TextBlock>("ProblemText")!.Text, Is.Not.Empty);
            dialog.FindControl<TextBox>("NameBox")!.Text = "modem";
            dialog.PortNames[0].Text = "FIRST";
            Assert.That(dialog.TryCreate(), Is.True);
            dialog.Close();
            Assert.That(dialog.Created, Is.EqualTo("U1"));
            Assert.That(Hierarchy.FindBlock(w.Session.Editor.Root, "modem")!.Ports[0].Name, Is.EqualTo("FIRST"));
            Assert.That(Hierarchy.Connections(w.Session.Editor.Root), Is.EqualTo(before));

            // the right-click menu on the instance
            ContextMenu menu = w.ShowDrawingMenu("inst:U1")!;
            // the new instance is the picked item, so it can itself be put in a block (nesting)
            Assert.That(menu.Items.Count, Is.EqualTo(3));
            Assert.That(((MenuItem)menu.Items[0]!).Header, Is.EqualTo("Make block..."));
            Assert.That(((MenuItem)menu.Items[1]!).Header, Is.EqualTo("Open U1"));
            Assert.That(((MenuItem)menu.Items[2]!).Header, Is.EqualTo("Dissolve U1 into this level"));
            menu.Close();
            // the right-click menu on a port pin places the port
            string port = Hierarchy.FindBlock(w.Session.Editor.Root, "modem")!.Ports[0].Name;
            ContextMenu portMenu = w.ShowDrawingMenu("port:U1:" + port)!;
            MenuItem? top = null;
            for (int i = 0; i < portMenu.Items.Count; i++)
            {
                if (portMenu.Items[i] is MenuItem m && (string?)m.Header == "Put port " + port + " on the top edge")
                    top = m;
            }
            Assert.That(top, Is.Not.Null, "the port menu offers the four edges");
            top!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
            portMenu.Close();
            Assert.That(Hierarchy.FindBlock(w.Session.Editor.Root, "modem")!.Ports[0].Side, Is.EqualTo(PortSide.Top));
            Assert.That(up.IsEnabled, Is.False);
            w.OpenInstance("U1");
            Assert.That(up.IsEnabled, Is.True);
            StackPanel bar = w.FindControl<StackPanel>("LevelBar")!;
            Assert.That(bar.IsVisible, Is.True);
            Assert.That(bar.Children.Count, Is.EqualTo(3), "Top > U1 (modem)");
            Assert.That(((Button)bar.Children[2]).Content, Is.EqualTo("U1 (modem)"));
            w.LeaveLevel();
            Assert.That(bar.IsVisible, Is.False);

            w.DissolveInstance("U1");
            Assert.That(w.Session.Editor.Root.Blocks, Is.Null);
            Assert.That(Hierarchy.Connections(w.Session.Editor.Root), Is.EqualTo(before));
        }

        [AvaloniaTest]
        public void Tab_CentresTheViewOnEachPlaceOfTheTracedSignal()
        {
            string blocks = RetroSchematic.Tests.TestCards.Blocks2A50();
            MainWindow w = new();
            w.Show();
            w.OpenSheet(blocks);
            Controls.SchematicCanvas canvas = w.FindControl<Controls.SchematicCanvas>("Canvas")!;
            w.Session!.Trace("DEQL_1");
            canvas.RaiseEvent(new Avalonia.Input.KeyEventArgs { RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Tab });
            string first = w.FindControl<TextBlock>("StatusText")!.Text!;
            Assert.That(first, Does.StartWith("At ").And.Contain("(DEQL_1)"));
            canvas.RaiseEvent(new Avalonia.Input.KeyEventArgs { RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Tab });
            Assert.That(w.FindControl<TextBlock>("StatusText")!.Text, Is.Not.EqualTo(first), "the next place");
        }

        [AvaloniaTest]
        public void ThePortArrowToggle_FollowsAndRemembersTheSetting()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet2A50);
            ToggleButton tb = w.FindControl<ToggleButton>("TbPortArrows")!;
            bool start = UiSettings.Instance.PortArrows;
            Assert.That(tb.IsChecked, Is.EqualTo(start));
            Assert.That(w.Session!.ShowPortArrows, Is.EqualTo(start));
            w.TogglePortArrows();
            Assert.That(UiSettings.Instance.PortArrows, Is.EqualTo(!start));
            Assert.That(tb.IsChecked, Is.EqualTo(!start));
            Assert.That(w.FindControl<MenuItem>("PortArrowsItem")!.IsChecked, Is.EqualTo(!start));
            Assert.That(w.Session.ShowPortArrows, Is.EqualTo(!start));
            w.TogglePortArrows();
            Assert.That(UiSettings.Instance.PortArrows, Is.EqualTo(start));
        }
    }
}
