//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// sheet.bus: which machine bus a card is for, and the bus catalogue it picks.
    /// </summary>
    [TestFixture]
    public class MachineBusTests
    {
        private static readonly string Catalog = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog");
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

        [Test]
        public void TwoA50_IsANord10Card()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            Assert.That(n.Sheet.Bus, Is.EqualTo(MachineBus.Nord10));
        }

        [Test]
        public void TheBus_RoundTripsAsItsFileText()
        {
            Netlist n = SheetEditor.NewSheet("X", "x", MachineBus.Nd100);
            string text = NetlistReader.Write(n);
            Assert.That(text, Does.Contain("\"bus\": \"nd-100\""));
            Assert.That(NetlistReader.Parse(text).Sheet.Bus, Is.EqualTo(MachineBus.Nd100));
            string old = text.Replace("\"bus\": \"nd-100\",", "");
            Assert.That(NetlistReader.Parse(old).Sheet.Bus, Is.Null, "an older file without the field still loads");
        }

        [Test]
        public void ANord10Sheet_NeverSeesAnNd100Signal_AndTheOtherWayRound()
        {
            BusCatalog n10 = BusCatalog.Load(Catalog, MachineBus.Nord10);
            BusCatalog n100 = BusCatalog.Load(Catalog, MachineBus.Nd100);
            Assert.That(n10.Signals.Count, Is.EqualTo(55));
            Assert.That(n100.Signals.Count, Is.EqualTo(68));
            // BA3 is a NORD-10 name only; BDAP an ND-100 name only
            Assert.That(n10.ByName("BA3"), Is.Not.Null);
            Assert.That(n100.ByName("BA3"), Is.Null);
            Assert.That(n100.ByName("BDAP"), Is.Not.Null);
            Assert.That(n10.ByName("BDAP"), Is.Null);
            Assert.That(BusCatalog.Load(Catalog, MachineBus.Unknown).Signals.Count, Is.EqualTo(0));
        }

        [Test]
        public void ACardName_AndABackplanePin_FindTheSameSignal()
        {
            BusCatalog n10 = BusCatalog.Load(Catalog, MachineBus.Nord10);
            BusSignal? ioxe = n10.ByName("IOXE");
            Assert.That(ioxe!.Name, Is.EqualTo("BIOXE"));
            Assert.That(n10.ByPin(null, 11), Is.SameAs(ioxe));
            Assert.That(n10.ByPin("A", 11), Is.Null, "a NORD-10 pin has no known connector letter");
            Assert.That(ioxe.Description, Does.StartWith("IOX enable"));
            Assert.That(ioxe.DescriptionConfidence, Is.EqualTo(Confidence.Inferred));
            Assert.That(n10.ByName("LEV12")!.Name, Is.EqualTo("BINT12"));
            Assert.That(n10.ByName("MC")!.Pins[0].Pin, Is.EqualTo(55));
        }

        [Test]
        public void SettingTheBus_MarksTheSheetChanged_PicksTheCatalogue_AndUndoes()
        {
            SheetSession s = new(SheetEditor.NewSheet("X", "x"), null, SymbolStyle.NorskData, Catalog);
            Assert.That(s.Bus, Is.EqualTo(MachineBus.Unknown));
            Assert.That(s.BusWarning, Is.Not.Null);
            Assert.That(s.BusSignals.Signals.Count, Is.EqualTo(0));
            s.SetBus(MachineBus.Nord10);
            Assert.That(s.Editor.Modified, Is.True);
            Assert.That(s.BusWarning, Is.Null);
            Assert.That(s.BusSignals.ByName("BIOXE"), Is.Not.Null);
            s.SetBus(MachineBus.Nd100);
            Assert.That(s.BusSignals.ByName("BA3"), Is.Null, "BA3 is NORD-10 only");
            s.Editor.ReadOnly = false; // undo is an edit-mode command
            s.Editor.Undo();
            Assert.That(s.Bus, Is.EqualTo(MachineBus.Nord10));
        }
    }
}
