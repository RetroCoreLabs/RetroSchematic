//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The bus commands in the window: the Bus button is off in read-only mode, the New bus
    /// dialog ticks signals by pattern and makes the bus, the path is clicked out on the canvas,
    /// and picking the bus shows its members panel.
    /// </summary>
    [TestFixture]
    public class BusWindowTests
    {
        private static readonly string Demo = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "demo", "demo-gates.json");

        [AvaloniaTest]
        public void NewBus_TicksByPattern_ThenThePathIsClickedOut_AndThePanelShowsTheMembers()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Demo);
            ToggleButton bus = w.FindControl<ToggleButton>("TbBus")!;
            Assert.That(bus.IsEnabled, Is.False, "no buses in read-only mode");
            Assert.That(w.OpenNewBusDialog(), Is.Null);
            w.SetEditMode(true);
            Assert.That(bus.IsEnabled, Is.True);

            NewBusWindow dialog = w.OpenNewBusDialog()!;
            List<string> keys = w.Session!.SignalKeys("");
            dialog.FindControl<TextBox>("NameBox")!.Text = "B";
            Assert.That(dialog.TryCreate(), Is.False, "no signal ticked: refused, reason shown");
            Assert.That(dialog.FindControl<TextBlock>("ProblemText")!.Text, Is.Not.Empty);
            dialog.TickPattern(keys[0] + " " + keys[1]);
            Assert.That(dialog.Ticked.Count, Is.EqualTo(2));
            Assert.That(dialog.TryCreate(), Is.True);
            dialog.Close();

            Render.EditTool tool = w.FindControl<Controls.SchematicCanvas>("Canvas")!.Tool!;
            Assert.That(tool.BusPathFor, Is.EqualTo("B"), "closing the dialog starts the path");
            // a free strip at the bottom right of the drawing: two clicks, then the last again
            double x = w.Session.Drawing.Width - 30, y1 = w.Session.Drawing.Height - 150, y2 = w.Session.Drawing.Height - 30;
            tool.PointerDown(x, y1);
            tool.PointerDown(x + 4, y2);
            Assert.That(tool.BusPoints[1].X, Is.EqualTo(tool.BusPoints[0].X), "made upright");
            tool.PointerDown(x + 4, y2);
            Assert.That(tool.BusPathFor, Is.Null, "the second click on the last point finishes");
            Assert.That(w.Session.Editor.FindBus("B")!.Path.Count, Is.EqualTo(2));
            Assert.That(w.Session.Selected, Is.EqualTo("bus:B"));
            Assert.That(w.FindControl<StackPanel>("BusPanel")!.IsVisible, Is.True);
            Assert.That(((List<string>)w.FindControl<ListBox>("BusMemberList")!.ItemsSource!).Count, Is.EqualTo(2));
            w.Close();
        }
    }
}
