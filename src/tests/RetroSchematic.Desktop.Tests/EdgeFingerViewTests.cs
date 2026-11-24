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
using RetroSchematic.Model;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The "Edge fingers" overlay on the PCB photos: every finger is labelled, hover names the
    /// pin and signal, a click picks the card's signal, and the solder photo is the mirror.
    /// </summary>
    [TestFixture]
    public class EdgeFingerViewTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [AvaloniaTest]
        public void Fingers_HoverNamesThePin_ClickPicksTheSignal_BackIsMirrored()
        {
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(Sheet2A50);
            w.ShowView(MainWindow.ViewFront);
            Assert.That(w.FindControl<ToggleButton>("TbEdgeFingers")!.IsEnabled, Is.True);
            w.SetEdgeFingers(true);
            Assert.That(w.Front.ShowFingers, Is.True);
            IReadOnlyList<EdgeFinger> front = w.EdgeFingers(BoardSide.Component);
            IReadOnlyList<EdgeFinger> back = w.EdgeFingers(BoardSide.Solder);
            Assert.That(front.Count, Is.EqualTo(50));
            Assert.That(back.Count, Is.EqualTo(50));

            // finger 16 on the component side is pin 32, BA0 on this card (shown /BA0: active low, as the window names signals)
            EdgeFinger ba0 = front[16];
            Assert.That(ba0.Pin, Is.EqualTo(32));
            Assert.That(ba0.Signal, Is.EqualTo("/BA0"));
            GridPoint mid = Middle(ba0.Corners);
            Assert.That(w.HoverPcb(BoardSide.Component, mid), Does.StartWith("Pin 32 (component side)  /BA0"));
            Assert.That(w.Front.HotPin, Is.EqualTo(32));
            w.Front.ClickAt(mid);
            Assert.That(w.Session!.Selected, Does.StartWith("net:BA0"));
            Assert.That(w.Front.PickedNet, Is.EqualTo(w.Session.Selected));
            BoardViewTests.Save(w, "fingers-front-old");

            // the solder side: pin 33 behind finger 16, on the other half of the photo
            EdgeFinger ba1 = back[16];
            Assert.That(ba1.Pin, Is.EqualTo(33));
            Assert.That(ba1.Signal, Is.EqualTo("/BA1"));
            double width = w.Back.Bitmap!.PixelSize.Width;
            Assert.That(Middle(ba1.Corners).X, Is.GreaterThan(width / 2), "finger 16 lies right of the middle on the solder photo");
            Assert.That(Middle(ba0.Corners).X, Is.LessThan(w.Front.Bitmap!.PixelSize.Width / 2.0));
            Assert.That(Middle(back[0].Corners).X, Is.GreaterThan(Middle(back[49].Corners).X), "the solder row runs right to left");
            w.ShowView(MainWindow.ViewBack);
            Assert.That(w.HoverPcb(BoardSide.Solder, Middle(ba1.Corners)), Does.StartWith("Pin 33 (solder side)  /BA1"));
            BoardViewTests.Save(w, "fingers-back-old");

            // a pin the card does not use names the catalogue's facts and picks nothing
            EdgeFinger empty = front[35];
            Assert.That(empty.Pin, Is.EqualTo(70));
            Assert.That(empty.NetOwner, Is.Null);
            Assert.That(empty.Label, Does.Contain("(empty)").And.Contain("not used on this card"));
            string before = w.Session.Selected;
            Assert.That(w.ClickPcb(BoardSide.Component, Middle(empty.Corners)), Is.Null);
            Assert.That(w.Session.Selected, Is.EqualTo(before));

            // the new photos too
            w.ShowView(MainWindow.ViewFront);
            w.PhotoChoice.SelectedIndex = 1;
            BoardViewTests.Save(w, "fingers-front-new");
            w.ShowView(MainWindow.ViewBack);
            w.PhotoChoice.SelectedIndex = 1;
            BoardViewTests.Save(w, "fingers-back-new");
            w.SetEdgeFingers(false);
            Assert.That(w.Front.ShowFingers, Is.False);
            w.Close();
        }

        private static GridPoint Middle(GridPoint[] c)
        {
            double x = 0, y = 0;
            for (int i = 0; i < c.Length; i++)
            {
                x += c[i].X;
                y += c[i].Y;
            }
            return new GridPoint(x / c.Length, y / c.Length);
        }
    }
}
