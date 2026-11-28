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
    /// Card 3207 SCSI/floppy controller (ND-100), read from ND-350007-A1 sheets 1-4: the top
    /// level with plugs A, B and C, and the first block level. Also the two fields that card
    /// needed: the source pages a block's inside is drawn on, and how sure a port direction is.
    /// </summary>
    [TestFixture]
    public class Card3207Tests
    {
        private static readonly string Sheet3207 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nd-100", "3207-scsi-floppy.rsc");

        private static BlockDefinition Block(Netlist n, string name)
        {
            BlockDefinition? b = Hierarchy.FindBlock(n, name);
            Assert.That(b, Is.Not.Null, name);
            return b!;
        }

        // every block of the card, reached the way the window reaches it: open the instance boxes
        // level by level from the top (BLOCKLEV -> SCSI/FLOPPY -> the blocks read from sheets 5-24)
        [TestCase("SCSI", "S_ND_INT", "13A", 91)]
        [TestCase("SCSI", "S_INTERF", "23C", 27)]
        [TestCase("SCSI", "DIFF_SCSI", "18C", 32)]
        [TestCase("FLOPPY", "F_ND_INT", null, 88)]
        [TestCase("FLOPPY", "LO_FLOPPY", null, 93)]
        [TestCase("FLOPPY", "Z80_COMP", null, 64)]
        public void EveryBlock_OpensInTheWindow_AndDrawsItsParts(string half, string block, string? chip, int parts)
        {
            SheetSession s = new(Sheet3207, SymbolStyle.NorskData, ChipCatalog.Find(Sheet3207));
            s.OpenInstance("BLOCKLEV");
            s.OpenInstance(half);
            s.OpenInstance(block);
            Assert.That(s.LevelNames().Count, Is.EqualTo(4));
            Assert.That(s.Drawing.Frames.Count, Is.GreaterThanOrEqualTo(parts), "every part of the block gets a frame");
            if (chip != null)
                Assert.That(s.Drawing.Frames.ContainsKey(chip), Is.True, chip);
        }

        [Test]
        public void TheAdapterSocket_IsABoardConnector()
        {
            SheetSession s = new(Sheet3207, SymbolStyle.NorskData, ChipCatalog.Find(Sheet3207));
            s.OpenInstance("BLOCKLEV");
            s.OpenInstance("SCSI");
            s.OpenInstance("SING_DIFF_SW");
            Assert.That(s.LevelNames().Count, Is.EqualTo(4));
        }

        [Test]
        public void TheSheet_Loads_WithEveryPlugPinRead()
        {
            Netlist n = NetlistReader.ReadFile(Sheet3207);
            int pins = 0;
            for (int i = 0; i < n.Nets.Count; i++)
                pins += n.Nets[i].Pins.Count;
            // plug A 66 + plug B 75 + plug C 39 signals, 6 GND, 3 +12 V, 6 +5 V, 3 standby
            Assert.That(pins, Is.EqualTo(198));
            Assert.That(n.Sheet.Bus, Is.EqualTo(MachineBus.Nd100));
        }

        [Test]
        public void ThePrintedLabel_IsKept_NextToTheStoredName()
        {
            Netlist n = NetlistReader.ReadFile(Sheet3207);
            Net? bd0 = null;
            for (int i = 0; i < n.Nets.Count; i++)
            {
                if (n.Nets[i].Name == "BD00")
                    bd0 = n.Nets[i];
            }
            Assert.That(bd0, Is.Not.Null);
            Assert.That(bd0!.Active, Is.EqualTo(ActiveLevel.Low));
            Assert.That(bd0.Printed, Is.EqualTo("BD~00"));
            Assert.That(bd0.Pins[0].At, Is.EqualTo("P.C.c3"));
        }

        [Test]
        public void TheBlockTree_IsTheOnePrintedInTheSheetMargins()
        {
            Netlist n = NetlistReader.ReadFile(Sheet3207);
            Assert.That(n.Instances![0].Block, Is.EqualTo("BLOCKLEV"));
            BlockDefinition top = Block(n, "BLOCKLEV");
            Assert.That(top.Pages![0].Id, Is.EqualTo("4"));
            Assert.That(top.Instances!.Count, Is.EqualTo(2));
            Assert.That(Block(n, "FLOPPY").Pages![0].Id, Is.EqualTo("5"));
            Assert.That(Block(n, "SCSI").Pages![0].Id, Is.EqualTo("15"));
        }

        [Test]
        public void APortDirection_SaysHowSureItIs()
        {
            Netlist n = NetlistReader.ReadFile(Sheet3207);
            BlockDefinition scsi = Block(n, "SCSI");
            BlockPort? ingrant = null, ladr1 = null;
            for (int i = 0; i < scsi.Ports.Count; i++)
            {
                if (scsi.Ports[i].Name == "INGRANT_0")
                    ingrant = scsi.Ports[i];
                if (scsi.Ports[i].Name == "LADR1")
                    ladr1 = scsi.Ports[i];
            }
            Assert.That(ingrant!.Direction, Is.EqualTo(PortDirection.Input));
            Assert.That(ingrant.DirectionConfidence, Is.EqualTo(Confidence.Inferred));
            Assert.That(ladr1!.DirectionConfidence, Is.EqualTo(Confidence.Open));
            Assert.That(ladr1.Note, Is.Not.Empty);
        }

        [Test]
        public void BlockPagesAndPortConfidence_SurviveWriteAndRead()
        {
            Netlist n = NetlistReader.ReadFile(Sheet3207);
            Netlist back = NetlistReader.Parse(NetlistReader.Write(n));
            Assert.That(Block(back, "FLOPPY").Pages![0].Note, Is.EqualTo(Block(n, "FLOPPY").Pages![0].Note));
            Assert.That(Block(back, "SCSI").Ports[0].DirectionConfidence, Is.EqualTo(Confidence.Inferred));
            Assert.That(Hierarchy.Connections(back), Is.EqualTo(Hierarchy.Connections(n)));
        }

        [Test]
        public void ABlockPage_FromASourceTheCardDoesNotHave_IsRefused()
        {
            Netlist n = NetlistReader.ReadFile(Sheet3207);
            // the card has 24 source pages; page 99 is not one of them
            Assert.That(n.Sources!.Count, Is.EqualTo(24));
            Block(n, "FLOPPY").Pages![0].Source = 99;
            Assert.That(() => NetlistReader.Parse(NetlistReader.Write(n)), Throws.TypeOf<NetlistException>().With.Message.Contains("Block FLOPPY"));
        }

        // a part inside FLOPPY drawn on one of FLOPPY's own sheets
        private static Netlist WithFloppyPart(string page)
        {
            Netlist n = NetlistReader.ReadFile(Sheet3207);
            BlockDefinition fl = Block(n, "FLOPPY");
            fl.Pages!.Add(new CardPage { Id = "6", Source = 6 });
            fl.Parts.Add(new Part { Ref = "T1", Type = "7400", Page = page });
            return n;
        }

        [Test]
        public void APartInsideABlock_NamesTheBlocksOwnSheet()
        {
            Netlist back = NetlistReader.Parse(NetlistReader.Write(WithFloppyPart("6")));
            Assert.That(Block(back, "FLOPPY").Parts[0].Page, Is.EqualTo("6"));
            // the editor's tidy-up after each edit keeps it
            CardPages.Normalize(back, null);
            Assert.That(Block(back, "FLOPPY").Parts[0].Page, Is.EqualTo("6"));
        }

        [Test]
        public void APartInsideABlock_OnACardPage_IsRefused()
        {
            // page 1 is a card page, not one of FLOPPY's
            Assert.That(() => NetlistReader.Parse(NetlistReader.Write(WithFloppyPart("1"))),
                Throws.TypeOf<NetlistException>().With.Message.Contains("not one of the block's pages"));
        }

        [Test]
        public void ABlockPageListedTwice_IsRefused()
        {
            Netlist n = NetlistReader.ReadFile(Sheet3207);
            Block(n, "FLOPPY").Pages!.Add(new CardPage { Id = "5" });
            Assert.That(() => NetlistReader.Parse(NetlistReader.Write(n)), Throws.TypeOf<NetlistException>().With.Message.Contains("listed twice"));
        }
    }
}
