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
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

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
