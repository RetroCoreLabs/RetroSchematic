//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The gold fingers of the NORD-10 card edge (catalog/boards/nord10-card.json edgeFingers,
    /// catalog/connectors/nord10-io-card-backplane.json pinMap): every pin 0-99 is on exactly one
    /// finger and once in the pin map, the solder side is the mirror of the component side, and
    /// the finger under a point is found.
    /// </summary>
    [TestFixture]
    public class EdgeFingerTests
    {
        private static readonly string Catalog = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog");

        private static EdgeFingerLayout Layout() => BoardTemplate.Find(Catalog, "nord10-card")!.EdgeFingers!;

        [Test]
        public void EveryPin0To99_IsOnExactlyOneFinger()
        {
            EdgeFingerLayout f = Layout();
            Assert.That(f.PerSide, Is.EqualTo(50));
            Assert.That(f.Connector, Is.EqualTo("nord10-io-card-backplane"));
            int[] seen = new int[100];
            int[] c = EdgeFingerMap.PinsOf(f, BoardSide.Component), s = EdgeFingerMap.PinsOf(f, BoardSide.Solder);
            for (int k = 0; k < 50; k++)
            {
                seen[c[k]]++;
                seen[s[k]]++;
            }
            for (int pin = 0; pin < 100; pin++)
            {
                Assert.That(seen[pin], Is.EqualTo(1), "pin " + pin);
                Assert.That(f.FingerOf(pin, out BoardSide side, out int finger), Is.True);
                Assert.That(f.PinOf(side, finger), Is.EqualTo(pin));
            }
            Assert.That(f.FingerOf(100, out _, out _), Is.False);
            // the mapping proven on the 2A50 photos: even pins on the component side, BA0 (32) on
            // finger 16 and BA1 (33) behind it; the five grounds 80-86, 94 on fingers 40-43, 47
            Assert.That(f.PinOf(BoardSide.Component, 16), Is.EqualTo(32));
            Assert.That(f.PinOf(BoardSide.Solder, 16), Is.EqualTo(33));
            Assert.That(f.PinOf(BoardSide.Component, 47), Is.EqualTo(94));
        }

        [Test]
        public void PinMap_HasEveryPinOnce_WithItsSideAndTheEarlierReadings()
        {
            ConnectorInfo info = ConnectorCatalog.Load(Catalog, MachineBus.Nord10).Get("nord10-io-card-backplane")!;
            Assert.That(info.PinMap.Count, Is.EqualTo(100));
            EdgeFingerLayout f = Layout();
            for (int pin = 0; pin < 100; pin++)
            {
                ConnectorPinInfo p = info.PinMap[pin];
                Assert.That(p.Pin, Is.EqualTo(pin));
                Assert.That(p.Signal, Is.Not.Empty, "pin " + pin);
                f.FingerOf(pin, out BoardSide side, out _);
                Assert.That(p.Side, Is.EqualTo(side == BoardSide.Component ? "component" : "solder"), "pin " + pin);
            }
            Assert.That(info.PinMap[32].Signal, Is.EqualTo("BA0"));
            Assert.That(info.PinMap[62].Signal, Is.EqualTo("-12V"));
            Assert.That(info.PinMap[66].Signal, Is.EqualTo("+12V"));
            Assert.That(info.PinMap[80].Signal, Is.EqualTo("GND"));
            Assert.That(info.PinMap[95].Signal, Is.EqualTo("TRANSM DATA"));
            Assert.That(info.PinMap[70].Signal, Is.EqualTo("(empty)"));
        }

        [Test]
        public void SolderSide_IsTheMirror_AndAPointFindsItsFinger()
        {
            EdgeFingerLayout f = Layout();
            BoardSize size = BoardTemplate.Find(Catalog, "nord10-card")!.Size;
            // a photo 1560 x 1490 pixels: 10 pixels per mm
            GridPoint[] corners = { new(0, 0), new(1560, 0), new(1560, 1490), new(0, 1490) };
            List<EdgeFinger> front = EdgeFingerMap.Build(f, null, size, corners, BoardSide.Component, null, null);
            List<EdgeFinger> back = EdgeFingerMap.Build(f, null, size, corners, BoardSide.Solder, null, null);
            double x0 = (front[0].Corners[0].X + front[0].Corners[1].X) / 2;
            Assert.That(x0, Is.EqualTo(157.7).Within(0.1), "finger 0 at the left of the component side");
            double xb = (back[0].Corners[0].X + back[0].Corners[1].X) / 2;
            Assert.That(xb, Is.EqualTo(1560 - 157.7).Within(0.1), "and at the right of the solder photo");
            Assert.That(back[0].Pin, Is.EqualTo(1));
            // hover: the middle of the finger and the gap beside it both find it
            Assert.That(EdgeFingerMap.At(front, new GridPoint(x0, 40))!.Pin, Is.EqualTo(0));
            Assert.That(EdgeFingerMap.At(front, new GridPoint(x0 + 25.4 * 16, 40))!.Pin, Is.EqualTo(32));
            Assert.That(EdgeFingerMap.At(back, new GridPoint(1560 - x0 - 25.4 * 16 + 11, 40))!.Pin, Is.EqualTo(33));
            Assert.That(EdgeFingerMap.At(front, new GridPoint(x0, 400)), Is.Null, "below the fingers");
            // a sheet correction moves the row
            List<EdgeFinger> moved = EdgeFingerMap.Build(f, new FingerAdjust { FirstCentreMm = 17 }, size, corners, BoardSide.Component, null, null);
            Assert.That((moved[0].Corners[0].X + moved[0].Corners[1].X) / 2, Is.EqualTo(170).Within(0.01));
        }
    }
}
