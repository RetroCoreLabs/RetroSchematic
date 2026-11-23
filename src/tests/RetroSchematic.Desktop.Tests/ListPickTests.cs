//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// Picking in the left lists does what a click on the drawing does, and brings the item into
    /// view: a signal is traced and its first pin is on screen; a part is picked and on screen.
    /// </summary>
    [TestFixture]
    public class ListPickTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

        [AvaloniaTest]
        public void PickingASignalInTheList_TracesIt_AndBringsItIntoView()
        {
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(Sheet2A50);
            Controls.SchematicCanvas canvas = w.FindControl<Controls.SchematicCanvas>("Canvas")!;
            // start far away, zoomed in on the opposite corner
            canvas.ZoomAt(new Avalonia.Point(0, 0), 4);
            canvas.CentreOn(0, 0);
            ListBox nets = w.FindControl<ListBox>("NetsList")!;
            int index = -1;
            System.Collections.IList items = (System.Collections.IList)nets.ItemsSource!;
            for (int i = 0; i < items.Count; i++)
            {
                if ((string)items[i]! == "/BD3")
                    index = i;
            }
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "the list shows the name style");
            nets.SelectedIndex = index;
            Assert.That(w.Session!.Traced, Is.EqualTo("BD3_0"), "traced, as a click on its wire");
            Assert.That(w.Session.Selected, Is.EqualTo("net:BD3_0"));
            Assert.That(w.FindControl<SelectableTextBlock>("DetailsText")!.Text, Does.StartWith("Signal /BD3"));
            Assert.That(w.Session.AreaOf("net:BD3_0", out _, out DrawPoint first), Is.True);
            Assert.That(canvas.IsOnScreen(first.X, first.Y), Is.True, "its first pin is on screen");
            Assert.That(w.FindControl<TextBlock>("StatusText")!.Text, Does.StartWith("Tracing /BD3"));
            w.Close();
        }

        [AvaloniaTest]
        public void PickingAPartInTheList_PicksIt_AndBringsItIntoView()
        {
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(Sheet2A50);
            Controls.SchematicCanvas canvas = w.FindControl<Controls.SchematicCanvas>("Canvas")!;
            canvas.ZoomAt(new Avalonia.Point(0, 0), 4);
            canvas.CentreOn(0, 0);
            ListBox parts = w.FindControl<ListBox>("PartsList")!;
            int index = -1;
            for (int i = 0; i < w.Session!.Netlist.Parts.Count; i++)
            {
                if (w.Session.Netlist.Parts[i].Ref == "17D")
                    index = i;
            }
            parts.SelectedIndex = index;
            Assert.That(w.Session.Selected, Is.EqualTo("part:17D"));
            PartFrame f = w.Session.Drawing.Frames["17D"];
            Assert.That(canvas.IsOnScreen(f.X + (f.Width / 2), f.Y + (f.Height / 2)), Is.True);
            w.Close();
        }
    }
}
