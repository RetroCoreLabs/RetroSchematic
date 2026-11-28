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
    /// The reading files of a card (docs/3207-readings): read, merged per block, checked and
    /// written into a sheet by CardReadings, with the same answers as tools/card-readings.py.
    /// The block counts below are the script's own output for the same files.
    /// </summary>
    [TestFixture]
    public class CardReadingsTests
    {
        internal static readonly string Readings3207 = Path.Combine(TestContext.CurrentContext.TestDirectory, "docs", "3207-readings");
        internal static readonly string Sheet3207 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nd-100", "3207-scsi-floppy.rsc");

        private static ChipCatalog Catalog() => new(DemoSheetLayoutTests.Catalog);

        [Test]
        public void TheReadingsOfCard3207_HaveNoProblems_AndTheScriptsCounts()
        {
            ReadingSet set = CardReadings.Load(Readings3207, Catalog());
            Assert.That(set.Problems, Is.Empty, string.Join("\n", set.Problems));
            string report = CardReadings.Report(set);
            // the lines tools/card-readings.py check prints for the same files
            Assert.That(report, Does.Contain("block F_ND_INT: sheets 6,7,8,9, 88 parts, 264 nets, 919 pins, 58 gates\n"));
            Assert.That(report, Does.Contain("block LO_FLOPPY: sheets 10,11, 93 parts, 174 nets, 487 pins, 38 gates\n"));
            Assert.That(report, Does.Contain("block Z80_COMP: sheets 12,13,14, 64 parts, 184 nets, 599 pins, 30 gates\n"));
            Assert.That(report, Does.Contain("block S_ND_INT: sheets 20,21,22,23,24, 91 parts, 308 nets, 1046 pins, 94 gates\n"));
            Assert.That(report, Does.EndWith("0 problem(s)\n"));
            // chips with no file are listed, never guessed; a 74LS112 reads the 74S112 file
            Assert.That(set.Missing, Does.Contain("NCR5386"));
            Assert.That(set.Missing, Does.Not.Contain("74LS112"));
            Assert.That(set.Missing, Does.Not.Contain("74F112"));
        }

        [Test]
        public void TheSchottkyFile_GivesTheFamilyPins_WhenNoPlainFileExists()
        {
            Assert.That(ChipCatalog.SchottkyType("74LS112"), Is.EqualTo("74S112"));
            Assert.That(ChipCatalog.SchottkyType("74F112"), Is.EqualTo("74S112"));
            Assert.That(ChipCatalog.SchottkyType("74112"), Is.EqualTo("74S112"));
            Assert.That(ChipCatalog.SchottkyType("74S112"), Is.Null, "a 74S part is its own file");
            Assert.That(ChipCatalog.SchottkyType("Z80A_CPU"), Is.Null);
            Assert.That(ChipCatalog.PlainType("74PCT240"), Is.EqualTo("74240"), "PCT is a family, as in the script");
            ChipCatalog c = Catalog();
            Assert.That(c.Get("74LS112")!.ReadFrom, Is.EqualTo("74S112"));
            Assert.That(c.Get("74LS157")!.ReadFrom, Is.EqualTo("74157"), "the plain file comes first");
            Assert.That(c.Get("74S112")!.ReadFrom, Is.Null);
        }

        [Test]
        public void Parse_ReadsEveryKindOfLine()
        {
            ReadingFile r = CardReadings.Parse("p01.txt",
                "# a comment\nblock B\npage 1\npart 1A 7400 10 20 30 40\npart R1 1KOHM\ngate 1A/3 nand 1,2>3\n"
                + "net X: 1A.1 1A.2   # comment after\nnet X: 1A.3\nport C X Y\nconnector 9Z sock use words here\n"
                + "open 1A.4\nnc 1A.5\nnote a # hash kept on a note\n");
            Assert.That(r.Block, Is.EqualTo("B"));
            Assert.That(r.Page, Is.EqualTo("1"));
            Assert.That(r.Parts["1A"], Is.EqualTo("7400"));
            Assert.That(r.Boxes["1A"], Is.EqualTo(new[] { 10, 20, 30, 40 }));
            Assert.That(r.Gates[0].Inputs, Is.EqualTo(new List<int> { 1, 2 }));
            Assert.That(r.Gates[0].Output, Is.EqualTo(3));
            Assert.That(r.Nets["X"].Count, Is.EqualTo(3), "two lines of one name are one net");
            Assert.That(r.Ports["C"], Is.EqualTo(new List<string> { "X", "Y" }));
            Assert.That(r.Connectors["9Z"].Use, Is.EqualTo("use words here"));
            Assert.That(r.Open.Contains("1A.4") && r.Nc.Contains("1A.5"), Is.True);
            Assert.That(r.Notes[0], Is.EqualTo("a # hash kept on a note"));
        }

        [Test]
        public void Parse_RefusesAnUnknownLine_AndAFileWithNoBlock()
        {
            ReadingException e = Assert.Throws<ReadingException>(() => CardReadings.Parse("p02.txt", "block B\npage 2\nwire 1A.1\n"))!;
            Assert.That(e.Message, Is.EqualTo("p02.txt:3: unknown line \"wire\""));
            e = Assert.Throws<ReadingException>(() => CardReadings.Parse("p03.txt", "page 3\n"))!;
            Assert.That(e.Message, Does.Contain("needs a block and a page line"));
        }

        [Test]
        public void Check_FindsEachKindOfProblem()
        {
            List<ReadingFile> files = new()
            {
                CardReadings.Parse("p01.txt", "block TOP\npage 1\nport B X NOPE\n"),
                CardReadings.Parse("p02.txt",
                    "block B\npage 2\npart 1A 7400\ngate 1A/3 nand 1,2>3\ngate 1A/5 nand 4,6>8\n"
                    + "net X: 1A.1 9Z.1\nnet Y: 1A.1\nnet Q: 1A.14\nnet Z: 1A.99\n"),
            };
            ReadingSet set = CardReadings.Check(files, Catalog());
            string all = string.Join("\n", set.Problems);
            Assert.That(all, Does.Contain("p02.txt:6: 9Z.1 in net X: no part 9Z in block B"));
            Assert.That(all, Does.Contain("p02.txt:7: 1A.1 is in net Y and in net X (p02.txt:6)"));
            Assert.That(all, Does.Contain("1A.14 is a power pin of 7400 but is in net Q"));
            Assert.That(all, Does.Contain("1A.99: 7400 (7400) has no pin 99"));
            Assert.That(all, Does.Contain("gate 1A/3 pin 2 is in no net"));
            Assert.That(all, Does.Contain("gate 1A/5 must be named 1A/8 (after its output pin)"));
            Assert.That(all, Does.Contain("p01.txt: port NOPE of block B is not a net in that block"));
        }

        [Test]
        public void Signal_ReadsTheTildeAsActiveLow()
        {
            OrderedDictionary<string, string> parts = new() { { "13D", "7400" } };
            Assert.That(CardReadings.Signal("BD~00", parts), Is.EqualTo(("BD00", ActiveLevel.Low, (string?)"BD~00")));
            Assert.That(CardReadings.Signal("13D_7", parts), Is.EqualTo(("13D_7", ActiveLevel.None, (string?)null)), "an unnamed net");
            Assert.That(CardReadings.Signal("+5V", parts), Is.EqualTo(("+5V", ActiveLevel.None, (string?)null)));
            Assert.That(CardReadings.Signal("TFRQ", parts), Is.EqualTo(("TFRQ", ActiveLevel.High, (string?)null)));
        }

        [Test]
        public void WriteInto_Card3207_GivesTheBlocksTheSheetHolds()
        {
            Netlist before = NetlistReader.ReadFile(Sheet3207);
            Netlist after = NetlistReader.ReadFile(Sheet3207);
            ReadingSet set = CardReadings.Load(Readings3207, Catalog());
            List<string> written = CardReadings.WriteInto(after, set, Catalog());
            Assert.That(written, Is.EqualTo(new List<string> { "F_ND_INT", "LO_FLOPPY", "Z80_COMP", "DIFF_SCSI", "SING_DIFF_SW", "S_INTERF", "S_ND_INT" }));
            Assert.That(after.Blocks!.Count, Is.EqualTo(before.Blocks!.Count), "every block was there already");
            for (int i = 0; i < before.Blocks.Count; i++)
            {
                BlockDefinition a = before.Blocks[i], b = after.Blocks[i];
                Assert.That(b.Name, Is.EqualTo(a.Name));
                // parts, gates, signals, pages and instances as the script wrote them into the file
                Assert.That(NetlistReader.ToJson(b.Parts), Is.EqualTo(NetlistReader.ToJson(a.Parts)), a.Name + " parts");
                Assert.That(NetlistReader.ToJson(b.Gates), Is.EqualTo(NetlistReader.ToJson(a.Gates)), a.Name + " gates");
                Assert.That(NetlistReader.ToJson(b.Nets), Is.EqualTo(NetlistReader.ToJson(a.Nets)), a.Name + " signals");
                Assert.That(NetlistReader.ToJson(b.Pages), Is.EqualTo(NetlistReader.ToJson(a.Pages)), a.Name + " pages");
                Assert.That(NetlistReader.ToJson(b.Instances), Is.EqualTo(NetlistReader.ToJson(a.Instances)), a.Name + " instances");
                Assert.That(b.Ports.Count, Is.EqualTo(a.Ports.Count), a.Name + " ports");
                for (int p = 0; p < a.Ports.Count; p++)
                {
                    Assert.That(b.Ports[p].Name, Is.EqualTo(a.Ports[p].Name));
                    Assert.That(NetlistReader.ToJson(b.Ports[p].Signals), Is.EqualTo(NetlistReader.ToJson(a.Ports[p].Signals)));
                }
            }
            Assert.That(NetlistReader.ToJson(after.Connectors), Is.EqualTo(NetlistReader.ToJson(before.Connectors)));
            // a port direction follows the catalogue as it is now (the script gives the same with
            // the same catalogue): RCW1~ only reaches chip inputs inside F_ND_INT
            BlockPort rcw = PortOf(after, "F_ND_INT", "RCW1_0");
            Assert.That(rcw.Direction, Is.EqualTo(PortDirection.Input));
            Assert.That(rcw.DirectionConfidence, Is.EqualTo(Confidence.Inferred));
            Assert.That(PortOf(after, "Z80_COMP", "BA00").Direction, Is.EqualTo(PortDirection.Output));
        }

        [Test]
        public void ApplyReadings_IsOneUndoStep_AndRefusedReadOnly()
        {
            SheetEditor e = new(NetlistReader.ReadFile(Sheet3207), Catalog());
            ReadingSet set = CardReadings.Load(Readings3207, Catalog());
            Assert.Throws<SheetEditException>(() => e.ApplyReadings(set));
            e.ReadOnly = false;
            string start = NetlistReader.Write(e.Root);
            Assert.That(e.ApplyReadings(set).Count, Is.EqualTo(7));
            Assert.That(e.Modified, Is.True);
            e.Undo();
            Assert.That(NetlistReader.Write(e.Root), Is.EqualTo(start));
            Assert.That(e.CanUndo, Is.False, "the whole import was one step");
        }

        private static BlockPort PortOf(Netlist n, string block, string port)
        {
            BlockDefinition b = Hierarchy.FindBlock(n, block)!;
            for (int i = 0; i < b.Ports.Count; i++)
            {
                if (b.Ports[i].Name == port)
                    return b.Ports[i];
            }
            Assert.Fail("no port " + port);
            return null!;
        }
    }
}
