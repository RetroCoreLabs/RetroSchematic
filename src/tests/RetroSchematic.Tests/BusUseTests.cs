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
    /// The bus use report: every backplane signal of a card followed down through its blocks to
    /// the chip pins, with what each pin does. The facts checked are read off the drawings
    /// (card 3207 sheets 20-23, card 2A50).
    /// </summary>
    [TestFixture]
    public class BusUseTests
    {
        private static readonly string Sheet3207 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nd-100", "3207-scsi-floppy.rsc");
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private static List<BusSignalUse> Uses(string sheet)
            => BusUse.Of(NetlistReader.ReadFile(sheet), new ChipCatalog(ChipCatalog.Find(sheet)!), sheet);

        private static BusSignalUse Signal(List<BusSignalUse> uses, string printed)
        {
            for (int i = 0; i < uses.Count; i++)
            {
                if (uses[i].Signal == printed)
                    return uses[i];
            }
            Assert.Fail("no bus signal " + printed);
            return null!;
        }

        private static BusChipPin Chip(BusSignalUse s, string block, string part, string pin)
        {
            for (int i = 0; i < s.Chips.Count; i++)
            {
                BusChipPin c = s.Chips[i];
                if (c.Path.EndsWith(block, System.StringComparison.Ordinal) && c.Part == part && c.Pin == pin)
                    return c;
            }
            Assert.Fail(s.Signal + ": no " + block + " " + part + "." + pin);
            return null!;
        }

        [Test]
        public void BREQ_IsDrivenOpenCollector_ByBothHalvesOfTheCard()
        {
            BusSignalUse breq = Signal(Uses(Sheet3207), "BREQ~");
            Assert.That(breq.BackplanePins, Is.EqualTo(new[] { "P.C.c12" }));
            BusChipPin scsi = Chip(breq, "S_ND_INT", "9C", "3");
            Assert.That(scsi.Role, Is.EqualTo(BusRole.Drives));
            Assert.That(scsi.OutputType, Is.EqualTo("open-collector"));
            Assert.That(Chip(breq, "F_ND_INT", "5C", "3").Role, Is.EqualTo(BusRole.Drives));
            Assert.That(breq.Driven, Is.True);
        }

        [Test]
        public void ADataLine_HasAThreeStateDriver_AndAReceiver_ThroughThe74FamilyRule()
        {
            // sheet 20: 5A (74S240) drives BD~00 from pin 9 (2Y1); 8A (74S240) reads it on pin 11 (2A1)
            BusSignalUse bd0 = Signal(Uses(Sheet3207), "BD~00");
            BusChipPin drive = Chip(bd0, "S_ND_INT", "5A", "9");
            Assert.That(drive.Role, Is.EqualTo(BusRole.Drives));
            Assert.That(drive.OutputType, Is.EqualTo("three-state"));
            Assert.That(drive.PinName, Is.EqualTo("2Y1"));
            Assert.That(drive.ReadFrom, Is.EqualTo("74240"), "a 74S240 has no file of its own");
            Assert.That(Chip(bd0, "S_ND_INT", "8A", "11").Role, Is.EqualTo(BusRole.Receives));
        }

        [Test]
        public void INGRANT_IsOnlyRead_AndASupplyReachesNoChip()
        {
            List<BusSignalUse> uses = Uses(Sheet3207);
            BusSignalUse grant = Signal(uses, "INGRANT~");
            Assert.That(grant.Driven, Is.False);
            Assert.That(Chip(grant, "S_ND_INT", "4A", "2").Role, Is.EqualTo(BusRole.Receives));
            Assert.That(Signal(uses, "+5V").Chips, Is.Empty);
        }

        [Test]
        public void ANord10Card_IsReportedOnItsEdgeFingers()
        {
            List<BusSignalUse> uses = Uses(Sheet2A50);
            Assert.That(uses.Count, Is.GreaterThan(10));
            bool chip = false;
            for (int i = 0; i < uses.Count; i++)
            {
                Assert.That(uses[i].Bus, Is.EqualTo(MachineBus.Nord10));
                chip |= uses[i].Chips.Count > 0;
            }
            Assert.That(chip, Is.True);
        }

        [Test]
        public void TheReport_HasABusSectionAndATablePerSignal()
        {
            List<BusSignalUse> all = Uses(Sheet3207);
            all.AddRange(Uses(Sheet2A50));
            string md = BusUse.ToMarkdown(all);
            Assert.That(md, Does.Contain("## ND-100 bus"));
            Assert.That(md, Does.Contain("## NORD-10 bus (card-edge fingers)"));
            Assert.That(md, Does.Contain("### 3207 BREQ\\~ (P.C.c12)"));
            Assert.That(md, Does.Contain("| S_ND_INT | 9C | 7438 | 3 | 1Y | drives (open-collector) |"));
        }

        [TestCase("74LS240", "74240")]
        [TestCase("74F04", "7404")]
        [TestCase("74ALS00", "7400")]
        [TestCase("7400", null)]
        [TestCase("74LS", null)]
        [TestCase("NCR5386", null)]
        public void The74FamilyRule_FindsThePlainType(string type, string? plain)
        {
            Assert.That(ChipCatalog.PlainType(type), Is.EqualTo(plain));
        }
    }
}
