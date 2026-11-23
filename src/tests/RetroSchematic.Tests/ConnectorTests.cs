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
    /// The card's connectors: pin references with and without the connector letter, and the
    /// connector boxes on the drawing.
    /// </summary>
    [TestFixture]
    public class ConnectorTests
    {
        private static readonly string Catalog = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog");
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [TestCase("P", true)]
        [TestCase("F", true)]
        [TestCase("P.A", true)]
        [TestCase("F.B", true)]
        [TestCase("13C", false)]
        [TestCase("PA", false)]
        [TestCase("P.A.B", false)]
        public void WhatIsAConnector(string reference, bool connector)
        {
            Assert.That(ConnectorPins.IsConnector(reference), Is.EqualTo(connector));
        }

        [Test]
        public void ThePinText_SaysTheLetter_OrThatItIsNotKnown()
        {
            Assert.That(ConnectorPins.Show("P.A", 32), Is.EqualTo("A.32"));
            Assert.That(ConnectorPins.Show("P", 32), Is.EqualTo("?.32"));
            Assert.That(ConnectorPins.Name("P"), Is.EqualTo("Backplane (connector not known)"));
            Assert.That(ConnectorPins.Name("P.A"), Is.EqualTo("Backplane A"));
        }

        [Test]
        public void ALetteredPin_AndPinZero_Load_AndAreDrawnInTheirOwnBox()
        {
            Netlist n = SheetEditor.NewSheet("X", "x", MachineBus.Nd100);
            n.Nets.Add(new Net { Name = "A", Active = ActiveLevel.Low, Pins = { new PinReference { At = "P.A.32" }, new PinReference { At = "P.0" } } });
            n = NetlistReader.Parse(NetlistReader.Write(n));
            Assert.That(n.Nets[0].Pins[0].PartRef, Is.EqualTo("P.A"));
            Assert.That(n.Nets[0].Pins[0].Pin, Is.EqualTo(32));
            SchematicDrawing d = SheetLayout.Build(n, SymbolStyle.NorskData, new LayoutOptions { Catalog = new ChipCatalog(Catalog) });
            Assert.That(d.Pins.ContainsKey("P.A.32") && d.Pins.ContainsKey("P.0"), Is.True);
            List<string> texts = Texts(d);
            Assert.That(texts, Does.Contain("Backplane A"));
            Assert.That(texts, Does.Contain("Backplane (connector not known)"));
            Assert.That(texts, Does.Contain("A.32"));
            Assert.That(texts, Does.Contain("0"));
        }

        [Test]
        public void On2A50_EveryBackplanePin_IsDrawn_WithTheCardName_AndTheBackplaneName()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Catalog);
            SchematicDrawing d = s.Drawing;
            int backplane = 0;
            for (int i = 0; i < s.Netlist.Nets.Count; i++)
            {
                for (int j = 0; j < s.Netlist.Nets[i].Pins.Count; j++)
                {
                    PinReference p = s.Netlist.Nets[i].Pins[j];
                    if (ConnectorPins.IsConnector(p.PartRef))
                    {
                        Assert.That(d.Pins.ContainsKey(p.At), Is.True, p.At + " is drawn");
                        backplane++;
                    }
                }
            }
            Assert.That(backplane, Is.EqualTo(57));
            List<string> texts = Texts(d);
            // pin 11: the card says IOXE, the backplane BIOXE - both are shown
            Assert.That(texts, Does.Contain("/IOXE"));
            Assert.That(texts, Does.Contain("= BIOXE"));
            Assert.That(texts, Does.Contain("= BINT12"));
            // BA3 is the same on both: no second name
            Assert.That(texts, Does.Not.Contain("= BA3"));
            // the front connector's catalogue adds the modem plug pin
            Assert.That(texts, Does.Contain("(V.24 17)"));
            Assert.That(texts, Does.Contain("NORD-10 I/O device card: connector to the backplane"));
        }

        [Test]
        public void TracingABackplaneSignal_IncludesItsConnectorPin_AndThePanelSaysWhatTheBusSays()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Catalog);
            s.TraceOwner("pin:P.11");
            Assert.That(s.TracedName, Is.EqualTo("/IOXE"));
            Assert.That(s.TracedPins(), Does.Contain("P.11"));
            s.Select("pin:P.11");
            string text = s.Describe();
            Assert.That(text, Does.Contain("Backplane (connector not known), pin ?.11"));
            Assert.That(text, Does.Contain("Backplane name: BIOXE"));
            Assert.That(text, Does.Contain("IOX enable"));
            s.Select("conn:P");
            Assert.That(s.Describe(), Does.StartWith("Backplane (connector not known)\nCatalogue: nord10-io-card-backplane"));
        }

        [Test]
        public void AnNd100Sheet_NeverTakesNord10Names_ForItsBackplanePins()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            n.Sheet.Bus = MachineBus.Nd100;
            SchematicDrawing d = SheetLayout.Build(n, SymbolStyle.NorskData, new LayoutOptions { Catalog = new ChipCatalog(Catalog) });
            List<string> texts = Texts(d);
            Assert.That(texts, Does.Not.Contain("= BIOXE"));
            Assert.That(texts, Does.Not.Contain("NORD-10 I/O device card: connector to the backplane"));
        }

        /// <summary>
        /// Every connector pin whose signal goes to another pin is joined to it on the drawing:
        /// by a wire or a bus entry that starts at the pin, or, where no wire could be routed, by
        /// a tag (diamond) beside it. Never neither, and never both a wire and a tag.
        /// </summary>
        [TestCase("nord-10/2A50-sync-modem-buffer-1050.rsc", SymbolStyle.NorskData, true)]
        [TestCase("nord-10/2A50-sync-modem-buffer-1050.rsc", SymbolStyle.Modern, true)]
        [TestCase("nord-10/2A50-sync-modem-buffer-1050.rsc", SymbolStyle.NorskData, false)]
        [TestCase("nord-10/2A50-sync-modem-buffer-1050-blocks.rsc", SymbolStyle.NorskData, true)]
        [TestCase("demo/demo-gates.rsc", SymbolStyle.NorskData, true)]
        public void EveryConnectorPin_HasAWire_OrATag_NeverNeitherNorBoth(string sheet, SymbolStyle style, bool buses)
        {
            string file = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", sheet);
            Netlist n = NetlistReader.ReadFile(file);
            SchematicDrawing d = SheetLayout.Build(n, style, new LayoutOptions { Catalog = new ChipCatalog(Catalog), ShowBuses = buses });
            int wiredPins = 0;
            for (int i = 0; i < n.Nets.Count; i++)
            {
                Net net = n.Nets[i];
                if (net.Pins.Count < 2)
                    continue;
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    PinReference p = net.Pins[j];
                    if (!ConnectorPins.IsConnector(p.PartRef) || !d.Pins.TryGetValue(p.At, out DrawPoint at))
                        continue;
                    bool wire = false, tag = false;
                    for (int k = 0; k < d.Items.Count; k++)
                    {
                        DrawItem it = d.Items[k];
                        if (it.Kind == DrawKind.Line && it.Role == DrawRole.Wire
                            && (Near(it.X1, it.Y1, at) || Near(it.X2, it.Y2, at)))
                            wire = true;
                        // a tag's diamond starts a few units out from the pin end, on its row
                        if (it.Role == DrawRole.NetLabel && it.Path != null && it.Path.Length > 0
                            && System.Math.Abs(it.Path[0].Y - at.Y) < 0.5 && System.Math.Abs(it.Path[0].X - at.X) < 12)
                            tag = true;
                    }
                    if (!wire && !tag)
                        Assert.Fail(p.At + " (" + net.Name + ") has neither a wire nor a tag;" + Dump(d, at));
                    Assert.That(wire && tag, Is.False, p.At + " (" + net.Name + ") has both a wire and a tag");
                    if (wire)
                        wiredPins++;
                }
            }
            if (sheet.StartsWith("nord-10/2A50-sync-modem-buffer-1050.rsc", System.StringComparison.Ordinal))
                Assert.That(wiredPins, Is.GreaterThan(30), "most connector pins of 2A50 get a real wire");
        }

        [Test]
        public void On2A50_Lev12AtP35_IsWiredTo5A8()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            SchematicDrawing d = SheetLayout.Build(n, SymbolStyle.NorskData, new LayoutOptions { Catalog = new ChipCatalog(Catalog) });
            Net? lev = null;
            for (int i = 0; i < n.Nets.Count; i++)
            {
                for (int j = 0; j < n.Nets[i].Pins.Count; j++)
                {
                    if (n.Nets[i].Pins[j].At == "P.35")
                        lev = n.Nets[i];
                }
            }
            Assert.That(lev, Is.Not.Null);
            Assert.That(lev!.Name, Is.EqualTo("LEV12"));
            bool found = false;
            foreach (KeyValuePair<string, List<List<DrawPoint>>> kv in d.WireBranches)
            {
                if (kv.Key.StartsWith("LEV12", System.StringComparison.Ordinal))
                    found = true;
            }
            Assert.That(found, Is.True, "LEV12 is drawn as a wire");
        }

        private static string Dump(SchematicDrawing d, DrawPoint at)
        {
            System.Text.StringBuilder sb = new();
            sb.Append(" pin at ").Append(at.X).Append(',').Append(at.Y);
            for (int k = 0; k < d.Items.Count; k++)
            {
                DrawItem it = d.Items[k];
                if (System.Math.Abs(it.Y1 - at.Y) < 20 && System.Math.Abs(it.X1 - at.X) < 40)
                    sb.Append("\n ").Append(it.Kind).Append(' ').Append(it.Role).Append(' ').Append(it.Owner).Append(' ').Append(it.X1).Append(',').Append(it.Y1).Append(' ').Append(it.X2).Append(',').Append(it.Y2);
            }
            return sb.ToString();
        }

        private static bool Near(double x, double y, DrawPoint p) => System.Math.Abs(x - p.X) < 6 && System.Math.Abs(y - p.Y) < 0.5;

        [Test]
        public void On2A50_TheBackplaneBox_IsLeftOfTheChips_WithItsPinsFacingThem()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Catalog);
            SchematicDrawing d = s.Drawing;
            PartFrame p = d.ConnectorFrames["P"], f = d.ConnectorFrames["F"];
            double chipsLeft = double.MaxValue, chipsRight = 0;
            foreach (KeyValuePair<string, PartFrame> kv in d.Frames)
            {
                chipsLeft = System.Math.Min(chipsLeft, kv.Value.X);
                chipsRight = System.Math.Max(chipsRight, kv.Value.X + kv.Value.Width);
            }
            // the bus receivers and drivers that the backplane signals go to sit in the left half
            Assert.That(p.X + p.Width, Is.LessThan(chipsLeft), "backplane box left of every chip");
            Assert.That(d.ShiftX, Is.GreaterThan(0), "room was made on the left");
            Assert.That(d.Pins["P.11"].X, Is.GreaterThan(p.X + p.Width), "its pins are on the side facing the chips");
            // the 75150/75154 line chips are in the right half
            Assert.That(f.X, Is.GreaterThan(chipsRight), "front box right of every chip");
            Assert.That(d.Pins["F.81"].X, Is.LessThan(f.X), "its pins face the chips");
        }

        [Test]
        public void On2A50_TheBackplaneBdPins_JoinAPieceOfTheBus_BesideTheBox()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Catalog);
            SchematicDrawing d = s.Drawing;
            PartFrame p = d.ConnectorFrames["P"];
            int near = 0;
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem it = d.Items[i];
                if (it.Kind == DrawKind.Line && it.Role == DrawRole.Bus && it.Owner == "bus:BD_0" && it.X1 == it.X2
                    && it.X1 > p.X + p.Width && it.X1 < p.X + p.Width + 120)
                    near++;
            }
            Assert.That(near, Is.EqualTo(1), "one upright piece of BD_0 just right of the backplane pins");
        }

        [Test]
        public void MovingAConnectorBox_KeepsItsPlace_InTheSheet_AndCanBeUndone()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Catalog);
            s.Editor.ReadOnly = false;
            EditTool tool = new(s);
            PartFrame before = s.Drawing.ConnectorFrames["F"];
            // press inside the box body (not on a pin), drag 20 grid steps down
            double x = before.X + 4, y = before.Y + 4;
            tool.PointerDown(x, y);
            Assert.That(tool.Gesture, Is.EqualTo(EditGesture.MoveConnector));
            tool.PointerUp(x, y + 120);
            ConnectorUse? use = s.Editor.Root.Connectors!.Find(c => c.Ref == "F");
            Assert.That(use?.Position, Is.Not.Null, tool.Message);
            PartFrame after = s.Drawing.ConnectorFrames["F"];
            Assert.That(after.X, Is.EqualTo(before.X));
            Assert.That(after.Y, Is.EqualTo(before.Y + 120));
            Assert.That(s.Drawing.ShiftX, Is.GreaterThan(0), "moving a box does not change the room on the left");
            s.Editor.Undo();
            Assert.That(s.Editor.Root.Connectors!.Find(c => c.Ref == "F")?.Position, Is.Null);
        }

        [Test]
        public void AStoredPinSide_PutsThePinsOnThatSide()
        {
            SheetEditor e = new(NetlistReader.ReadFile(Sheet2A50), new ChipCatalog(Catalog)) { ReadOnly = false };
            e.MoveConnector("P", 0, 200, "left");
            SchematicDrawing d = SheetLayout.Build(e.Netlist, SymbolStyle.NorskData, new LayoutOptions { Catalog = e.Catalog });
            PartFrame p = d.ConnectorFrames["P"];
            Assert.That(d.Pins["P.11"].X, Is.LessThan(p.X));
            Assert.That(p.Y, Is.EqualTo(SheetLayout.FromMm(200)));
            Assert.Throws<SheetEditException>(() => e.MoveConnector("13C", 0, 0));
            Assert.Throws<SheetEditException>(() => e.MoveConnector("P", 0, 0, "top"));
        }

        private static List<string> Texts(SchematicDrawing d)
        {
            List<string> list = new();
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Kind == DrawKind.Text)
                    list.Add(d.Items[i].Text);
            }
            return list;
        }
    }
}
