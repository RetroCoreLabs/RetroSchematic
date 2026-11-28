//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using ModelContextProtocol;
using NUnit.Framework;
using RetroSchematic.Mcp;
using RetroSchematic.Model;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The MCP tools added so that everything the window, the command-line tool and
    /// tools/card-readings.py do can be done through the server (docs/MCP-COVERAGE.md): lookup,
    /// create and edit, validate, and the bulk import of a reading set. Facts from the real
    /// 2A50 and 3207 sheets.
    /// </summary>
    [TestFixture]
    public class McpCoverageTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
        private string _out = "";

        [SetUp]
        public void SetUp()
        {
            _out = Path.Combine(TestContext.CurrentContext.WorkDirectory, "mcp-cov-" + TestContext.CurrentContext.Test.Name);
        }

        [TearDown]
        public void TearDown()
        {
            string dir = Path.GetDirectoryName(_out)!;
            string[] files = Directory.GetFiles(dir, Path.GetFileName(_out) + "*");
            for (int i = 0; i < files.Length; i++)
                File.Delete(files[i]);
        }

        private static SheetTools Tools() => new(TestContext.CurrentContext.TestDirectory);

        [Test]
        public void Lookup_SheetInfo_Describe_Search_Trace_PartInfo()
        {
            SheetTools t = Tools();
            t.OpenSheet(Sheet2A50);
            string info = t.GetSheetInfo();
            Assert.That(info, Does.StartWith("Sheet 2A50: "));
            Assert.That(info, Does.Contain("Machine: NORD-10; bus: NORD-10"));
            Assert.That(info, Does.Contain("Read-only"));
            Assert.That(info, Does.Contain("62 parts"));

            Assert.That(t.DescribeItem("13D"), Does.StartWith("Part 13D: 7442"));
            Assert.That(t.DescribeItem("gate:11D/1"), Does.StartWith("Gate 11D/1: NOR"));
            Assert.That(t.DescribeItem("pin:13D.15"), Does.Contain("Signal: BA0"));
            Assert.Throws<McpException>(() => t.DescribeItem("part:99Z"));

            string found = t.Search("7442");
            Assert.That(found, Does.Contain("part:13D"));
            Assert.That(t.Search("ZZZQ"), Does.StartWith("Nothing on this level"));

            string trace = t.TraceSignal("BA0_1");
            Assert.That(trace, Does.Contain("Pins: 11C.4 13D.15 13C.14"));
            Assert.That(trace, Does.Contain("Chips: 11C 13C 13D"));
            Assert.Throws<McpException>(() => t.TraceSignal("NOPE_1"));

            string part = t.GetPartInfo("11D");
            Assert.That(part, Does.StartWith("Part 11D: 7402"));
            Assert.That(part, Does.Contain("Does: sync register strobes ISCW, OSCW"));
            Assert.That(part, Does.Contain("ISCW"));
        }

        [Test]
        public void Lookup_ExportSvg_AndPageStats()
        {
            SheetTools t = Tools();
            t.OpenSheet(Sheet2A50);
            string svg = _out + ".svg";
            Assert.That(t.ExportSvg(svg), Does.StartWith("Wrote " + svg));
            Assert.That(File.ReadAllText(svg), Does.Contain("<svg"));
            Assert.Throws<McpException>(() => t.ExportSvg(svg, "9"));
            Assert.Throws<McpException>(() => t.ExportSvg(svg, null, "fancy"));
            Assert.That(t.PageStatsReport(), Does.StartWith("As the original: 1 drawn page(s)"));
        }

        [Test]
        public void Library_Cards_Families_BusUse_Chips()
        {
            SheetTools t = Tools();
            // no sheet open: the library tools still work
            string cards = t.ListCards();
            Assert.That(cards, Does.Contain("ND-100 3207: "));
            Assert.That(cards, Does.Contain("NORD-10 2A50: "));
            Assert.That(t.ListCards("ND-100"), Does.Not.Contain("2A50"));
            Assert.That(t.ListFamilies(), Does.Contain("ND-100: 1 card file(s)"));

            string use = t.BusUseOf(null, "2A50");
            Assert.That(use, Does.StartWith("2 card(s), "));
            Assert.That(use, Does.Contain("\n2A50 "));
            Assert.That(use, Does.Not.Contain("\n3207 "));
            Assert.Throws<McpException>(() => t.BusUseOf(null, null, "vme"));
            string md = _out + ".md";
            Assert.That(t.BusUseReport(md, null, "3207"), Is.EqualTo("Wrote " + md + "."));
            Assert.That(File.ReadAllText(md), Does.Contain("3207"));

            string chip = t.GetChip("74LS112");
            Assert.That(chip, Does.Contain("Pins and logic from 74S112.json"));
            Assert.That(t.GetChip("7400"), Does.StartWith("7400: "));
            McpException ex = Assert.Throws<McpException>(() => t.GetChip("Z80A_CPU"))!;
            Assert.That(ex.Message, Does.Contain("no chip file for Z80A_CPU"));
            Assert.That(t.ListChips("74S1"), Does.Contain("74S112"));
        }

        [Test]
        public void Validate_CheckSheet_MissingChips_CheckReadings()
        {
            SheetTools t = Tools();
            Assert.That(t.CheckSheet(CardReadingsTests.Sheet3207), Does.EndWith("0 problem(s)\n"));
            Assert.That(t.CheckSheet(Path.Combine(TestContext.CurrentContext.TestDirectory, "nothing.rsc")), Does.StartWith("NOT VALID: "));
            t.OpenSheet(Sheet2A50);
            Assert.That(t.CheckSheet(), Does.StartWith("1 level(s), 62 parts, 196 signals, 612 pins."));
            Assert.That(t.MissingChips(), Does.Contain("DM8160: "));
            string report = t.CheckReadings(CardReadingsTests.Readings3207);
            Assert.That(report, Does.Contain("block F_ND_INT: sheets 6,7,8,9, 88 parts, 264 nets, 919 pins, 58 gates"));
            Assert.That(report, Does.EndWith("0 problem(s)\n"));
            Assert.Throws<McpException>(() => t.CheckReadings(Path.Combine(TestContext.CurrentContext.TestDirectory, "no-such-folder")));
        }

        [Test]
        public void ImportReadings_IsRefusedReadOnly_ThenOneUndoStep_AndTheSaveRaisesTheStamp()
        {
            SheetTools t = Tools();
            t.OpenSheet(CardReadingsTests.Sheet3207);
            int revision = t.Editor!.Root.File?.Revision ?? 0;
            McpException ex = Assert.Throws<McpException>(() => t.ImportReadings(CardReadingsTests.Readings3207))!;
            Assert.That(ex.Message, Does.Contain("read-only"));
            t.SetEditMode(true);
            string done = t.ImportReadings(CardReadingsTests.Readings3207);
            Assert.That(done, Does.Contain("Wrote 7 block(s) into the sheet: F_ND_INT LO_FLOPPY Z80_COMP DIFF_SCSI SING_DIFF_SW S_INTERF S_ND_INT"));
            t.Undo();
            Assert.That(t.Editor.CanUndo, Is.False, "the import is one step");
            t.ImportReadings(CardReadingsTests.Readings3207);
            // saved beside the sheet, where its linked pages and photos are found
            string saved = Path.Combine(Path.GetDirectoryName(CardReadingsTests.Sheet3207)!, "mcp-import-test.rsc");
            try
            {
                t.SaveSheet(saved);
                Netlist back = NetlistReader.ReadFile(saved);
                Assert.That(back.File!.Revision, Is.EqualTo(revision + 1), "every save raises the revision");
                Assert.That(Hierarchy.FindBlock(back, "F_ND_INT")!.Parts.Count, Is.EqualTo(88));
            }
            finally
            {
                File.Delete(saved);
            }
        }

        [Test]
        public void Edit_PartGateNetSheetBusDisplayPages()
        {
            SheetTools t = Tools();
            t.OpenSheet(Sheet2A50);
            Assert.Throws<McpException>(() => t.RenamePart("11D", "40Z"), "read-only");
            t.SetEditMode(true);
            Assert.That(t.RenamePart("11D", "40Z"), Is.EqualTo("Renamed 11D to 40Z."));
            Assert.That(t.SetPart("40Z", null, "strobes", "inferred", "-"), Is.EqualTo("Set the facts of 40Z."));
            Assert.That(t.Editor!.FindPart("40Z")!.Function, Is.EqualTo("strobes"));
            Assert.Throws<McpException>(() => t.SetPart("40Z", null, null, "sure"));
            Assert.That(t.RemoveGate("40Z/1"), Is.EqualTo("Removed gate 40Z/1."));
            Assert.That(t.AddGate("40Z", "nor", "2,3", 1), Is.EqualTo("Added gate 40Z/1."));
            Assert.Throws<McpException>(() => t.AddGate("40Z", "xor", "2,3", 1));
            Assert.That(t.AddNet("SPARE", "0"), Is.EqualTo("Added signal SPARE_0."));
            Assert.That(t.SetNet("SPARE_0", "SPARE2", "1", "SPARE~2"), Is.EqualTo("The signal is now SPARE2_1."));
            Assert.That(t.SetSheetInfo(null, "Renamed title", null, null, null, null, null, null, -1), Is.EqualTo("Set the title block of 2A50."));
            Assert.That(t.Editor.Root.Sheet.Title, Is.EqualTo("Renamed title"));
            Assert.That(t.SetBus("nd-100"), Is.EqualTo("The card is for the ND-100 bus."));
            Assert.Throws<McpException>(() => t.SetBus("vme"));
            Assert.That(t.SetDisplay("{\"pinNumbers\":true}"), Is.EqualTo("Set the display section."));
            Assert.That(t.Editor.Root.Display!.PinNumbers, Is.True);
            Assert.That(t.ConvertToPages(), Is.EqualTo("The card has pages already."));
            Assert.That(t.SetPageInfo("1", "Main", 1, "2A50"), Is.EqualTo("Set page 1."));
            Assert.That(t.Editor.Root.Pages![0].Title, Is.EqualTo("Main"));

            SheetTools demo = Tools();
            demo.OpenSheet(DemoSheetLayoutTests.Demo, true);
            Assert.That(demo.ConvertToPages(), Does.StartWith("The card now has pages"));
        }

        [Test]
        public void Structure_BlockPortInstanceConnection()
        {
            SheetTools t = Tools();
            t.OpenSheet(Sheet2A50, true);
            Assert.That(t.CreateBlock("DEC"), Is.EqualTo("Made the empty block DEC."));
            Assert.That(t.AddInstance("U9", "DEC", 10, 20), Is.EqualTo("Put instance U9 of block DEC on this level."));
            t.EnterBlock("U9");
            t.AddNet("SEL", "0");
            t.LeaveBlock();
            Assert.Throws<McpException>(() => t.SetBlockPort("DEC", "SEL_0", "input", "NOPE"));
            Assert.That(t.SetBlockPort("DEC", "SEL_0", "input", "SEL_0", "inferred", "only a chip input inside"), Is.EqualTo("Set port SEL_0 of block DEC."));
            Assert.Throws<McpException>(() => t.SetBlockPort("DEC", "X", "sideways", "SEL_0"));
            Assert.That(t.ConnectPort("U9", "SEL_0", "BA2_0"), Is.EqualTo("Connected U9/SEL_0 to 1 signal(s)."));
            Assert.That(t.MoveInstance("U9", 30, 40), Is.EqualTo("Moved U9."));
            Assert.That(t.ConnectPort("U9", "SEL_0"), Is.EqualTo("Disconnected U9/SEL_0."));
            Assert.That(t.RemoveBlockPort("DEC", "SEL_0"), Is.EqualTo("Removed port SEL_0 of block DEC."));
            Assert.That(t.RemoveInstance("U9"), Is.EqualTo("Removed instance U9."));
            // parts moved into an existing block's instance
            t.AddInstance("U9", "DEC");
            Assert.That(t.MoveIntoBlock("13D", "U9"), Is.EqualTo("Moved 13D into U9."));
            Assert.That(t.Editor!.FindPart("13D"), Is.Null);
        }
    }
}
