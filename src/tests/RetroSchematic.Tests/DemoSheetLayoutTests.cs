//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The made-up demo sheet (sheets/demo/demo-gates.json) and what the layout makes of it:
    /// measured positions used, wires between pins, OPEN in its own colour, show/hide options.
    /// </summary>
    [TestFixture]
    public class DemoSheetLayoutTests
    {
        internal static readonly string Demo = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "demo", "demo-gates.json");

        internal static readonly string Catalog = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog");

        [Test]
        public void Demo_Reads_AndHasEveryConfidence_AndAnOpenCollectorLineWithAPullUp()
        {
            Netlist n = NetlistReader.ReadFile(Demo);
            Assert.That(n.Sheet.Note, Does.Contain("MADE-UP DEMO"));
            HashSet<Confidence> seen = new();
            Net? oc = null;
            for (int i = 0; i < n.Nets.Count; i++)
            {
                for (int j = 0; j < n.Nets[i].Pins.Count; j++)
                    seen.Add(n.Nets[i].Pins[j].Confidence);
                if (n.Nets[i].Drive == LineDrive.OpenCollector)
                    oc = n.Nets[i];
            }
            Assert.That(seen, Is.EquivalentTo(new[] { Confidence.Read, Confidence.Inferred, Confidence.Open }));
            Assert.That(oc, Is.Not.Null);
            Assert.That(oc!.PullUp, Is.Not.Null.And.Not.Empty);
            for (int i = 0; i < n.Gates.Count; i++)
                Assert.That(n.Gates[i].Position, Is.Not.Null, "demo gate " + n.Gates[i].Id + " has no position");
        }

        [Test]
        public void MeasuredGatePosition_IsWhereTheGateIsDrawn()
        {
            Netlist n = NetlistReader.ReadFile(Demo);
            SchematicDrawing d = SheetLayout.Build(n, SymbolStyle.NorskData);
            // 1A/1 is at (20 mm, 20 mm): its ND box starts at the margin plus 20 mm in units
            DrawItem box = First(d, "gate:1A/1", DrawKind.Rect);
            Assert.That(box.X1, Is.EqualTo(SheetLayout.FromMm(20)));
            Assert.That(box.Y1, Is.EqualTo(SheetLayout.FromMm(20)));
            // the part frame is drawn round its measured gates
            PartFrame f = d.Frames["1A"];
            Assert.That(f.X, Is.LessThan(box.X1));
            Assert.That(f.Y + f.Height, Is.GreaterThan(SheetLayout.FromMm(40)));
        }

        [Test]
        public void EverySignal_WithTwoDrawnPins_IsDrawnAsStraightWiresThatEndOnItsPins()
        {
            Netlist n = NetlistReader.ReadFile(Demo);
            SchematicDrawing d = SheetLayout.Build(n, SymbolStyle.NorskData);
            // NAB joins 1A.3, 3A.1 and 2A.2: wires must touch all three pin ends
            List<DrawItem> wires = Wires(d, "net:NAB");
            Assert.That(wires.Count, Is.GreaterThanOrEqualTo(2));
            for (int i = 0; i < wires.Count; i++)
                Assert.That(wires[i].X1 == wires[i].X2 || wires[i].Y1 == wires[i].Y2, "a wire piece is not horizontal or vertical");
            string[] pins = { "1A.3", "3A.1", "2A.2" };
            for (int p = 0; p < pins.Length; p++)
            {
                DrawPoint at = d.Pins[pins[p]];
                bool touched = false;
                for (int i = 0; i < wires.Count; i++)
                {
                    if ((wires[i].X1 == at.X && wires[i].Y1 == at.Y) || (wires[i].X2 == at.X && wires[i].Y2 == at.Y))
                        touched = true;
                }
                Assert.That(touched, "no wire ends on pin " + pins[p]);
            }
            // a signal with only one drawn pin (the rest on the backplane) still gets no wire to nowhere
            Assert.That(Wires(d, "net:IRQ").Count, Is.GreaterThan(0), "5A.3 and 5A.6 are both drawn");
        }

        [Test]
        public void AWireToAnOpenPin_IsOpen_AndToAnInferredPin_IsInferred()
        {
            Netlist n = NetlistReader.ReadFile(Demo);
            SchematicDrawing d = SheetLayout.Build(n, SymbolStyle.NorskData);
            // CLK: 2A.1 read, 4A.3 inferred, 5A.4 open
            List<DrawItem> clk = Wires(d, "net:CLK");
            bool open = false, inferred = false;
            for (int i = 0; i < clk.Count; i++)
            {
                open |= clk[i].Confidence == Confidence.Open;
                inferred |= clk[i].Confidence == Confidence.Inferred;
            }
            Assert.That(open, "no OPEN wire on CLK");
            Assert.That(inferred, "no INFERRED wire on CLK");
            List<DrawItem> data = Wires(d, "net:DATA");
            Assert.That(data[0].Confidence, Is.EqualTo(Confidence.Read));
            // the SVG prints OPEN in a colour of its own, apart from inferred brown
            DrawItem openWire = clk[0];
            for (int i = 0; i < clk.Count; i++)
            {
                if (clk[i].Confidence == Confidence.Open)
                    openWire = clk[i];
            }
            Assert.That(SvgRenderer.PrintColour(openWire), Is.EqualTo("#d00000"));
            Assert.That(SvgRenderer.PrintColour(new DrawItem { Role = DrawRole.Wire, Confidence = Confidence.Inferred }), Is.EqualTo("#b06000"));
        }

        [Test]
        public void HidingPinNumbersAndBoardPositions_LeavesThemOut()
        {
            Netlist n = NetlistReader.ReadFile(Demo);
            SchematicDrawing shown = SheetLayout.Build(n, SymbolStyle.NorskData);
            SchematicDrawing hidden = SheetLayout.Build(n, SymbolStyle.NorskData, new LayoutOptions { ShowPinNumbers = false, ShowBoardPositions = false });
            Assert.That(Count(shown, DrawRole.PinNumber), Is.GreaterThan(0));
            Assert.That(Count(hidden, DrawRole.PinNumber), Is.EqualTo(0));
            Assert.That(First(shown, "part:1A", DrawKind.Text).Text, Is.EqualTo("1A 7400"));
            Assert.That(First(hidden, "part:1A", DrawKind.Text).Text, Is.EqualTo("7400"));
        }

        [Test]
        public void ClickingAPinDot_PicksThePin()
        {
            SheetSession s = new(Demo, SymbolStyle.NorskData, Catalog);
            DrawPoint p = s.Drawing.Pins["3A.2"];
            s.PickAt(p.X, p.Y);
            Assert.That(s.Selected, Is.EqualTo("pin:3A.2"));
            Assert.That(s.Describe(), Does.StartWith("Pin 3A.2: 1Y").And.Contain("Signal: DATA"));
        }

        [Test]
        public void TheRealSheet_StillLaysOut_WithWires()
        {
            string real = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");
            SchematicDrawing d = SheetLayout.Build(NetlistReader.ReadFile(real), SymbolStyle.NorskData);
            Assert.That(Count(d, DrawRole.Wire), Is.GreaterThan(0));
        }

        internal static List<DrawItem> Wires(SchematicDrawing d, string owner)
        {
            List<DrawItem> list = new();
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Role == DrawRole.Wire && d.Items[i].Owner == owner)
                    list.Add(d.Items[i]);
            }
            return list;
        }

        private static int Count(SchematicDrawing d, DrawRole role)
        {
            int n = 0;
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Role == role)
                    n++;
            }
            return n;
        }

        private static DrawItem First(SchematicDrawing d, string owner, DrawKind kind)
        {
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Owner == owner && d.Items[i].Kind == kind)
                    return d.Items[i];
            }
            Assert.Fail("nothing of kind " + kind + " owned by " + owner);
            return null!;
        }
    }
}
