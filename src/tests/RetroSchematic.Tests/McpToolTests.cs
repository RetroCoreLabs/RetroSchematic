//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using System.Reflection;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using NUnit.Framework;
using RetroSchematic.Mcp;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The MCP tool layer: the same edit rules as the window (read-only by default, refusals
    /// with a reason), a new design built from nothing and saved, and every tool registered.
    /// </summary>
    [TestFixture]
    public class McpToolTests
    {
        private string _scratch = "";

        [SetUp]
        public void SetUp()
        {
            _scratch = Path.Combine(TestContext.CurrentContext.WorkDirectory, "mcp-" + TestContext.CurrentContext.Test.Name + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_scratch))
                File.Delete(_scratch);
        }

        [Test]
        public void FixWire_ThenAPathThroughChips_IsReportedAndRoutedAfresh_ThenCleared()
        {
            SheetTools t = Tools();
            t.OpenSheet(DemoSheetLayoutTests.Demo);
            t.SetEditMode(true);
            // a wired signal on the demo: the session says which
            SheetSession s = new(DemoSheetLayoutTests.Demo, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);
            string key = new System.Collections.Generic.List<string>(s.Drawing.WireBranches.Keys)[0];
            string name = key.Substring(0, key.Length - 2);
            string level = key.Substring(key.Length - 1);
            string fixedText = t.FixWire(name, level);
            Assert.That(fixedText, Does.StartWith("Fixed the wire path of " + name + ": "));
            // set the same path again: drawn exactly as given
            string path = fixedText.Substring(fixedText.IndexOf(": ", System.StringComparison.Ordinal) + 2);
            Assert.That(t.SetWirePath(name, level, path), Does.Contain("drawn exactly as given"));
            // a path far out to the right edge and back runs through chips
            string[] first = path.Split(';')[0].Trim().Split(' ');
            string a = first[0], b = first[^1];
            string ay = a.Split(',')[1], by = b.Split(',')[1];
            string bad = a + " 2000," + ay + " 2000," + by + " " + b;
            Assert.That(t.SetWirePath(name, level, bad), Does.Contain("NOT used as given"));
            Assert.That(t.ClearWirePath(name, level), Does.Contain("routed automatically again"));
            McpException ex = Assert.Throws<McpException>(() => t.SetWirePath(name, level, "1,1 5,9"))!;
            Assert.That(ex.Message, Does.Contain("slanted"));
        }

        [Test]
        public void OpenedSheet_IsReadOnly_UntilEditModeIsOn()
        {
            SheetTools t = Tools();
            Assert.That(t.OpenSheet(DemoSheetLayoutTests.Demo), Does.Contain("read-only"));
            McpException ex = Assert.Throws<McpException>(() => t.Delete("part", "1A"))!;
            Assert.That(ex.Message, Does.Contain("read-only"));
            Assert.That(t.ListParts(), Does.Contain("1A 7400"));
            t.SetEditMode(true);
            Assert.That(t.Delete("part", "1A"), Is.EqualTo("Deleted part 1A."));
            Assert.That(t.ListParts(), Does.Not.Contain("1A 7400"));
            t.Undo();
            Assert.That(t.ListParts(), Does.Contain("1A 7400"));
        }

        [Test]
        public void NewDesign_FromNothing_PlaceConnectMoveSave()
        {
            SheetTools t = Tools();
            t.NewSheet("T1", "Tool test");
            Assert.That(t.PlacePart("7400", 10, 10), Is.EqualTo("Placed 7400 as 1A."));
            Assert.That(t.PlacePart("7474", 80, 10, "2B"), Is.EqualTo("Placed 7474 as 2B."));
            Assert.That(t.ConnectPins("1A.3", "2B.2", "D"), Is.EqualTo("Connected 1A.3 and 2B.2 on D."));
            Assert.That(t.ConnectPins("1A.6", "2B.3"), Does.EndWith("on N1."));
            t.MovePart("2B", 100, 20);
            Assert.That(t.ListParts(), Does.Contain("2B 7474 at (100, 20) mm"));
            Assert.That(t.ListNets(), Does.Contain("D: 1A.3 2B.2"));
            Assert.Throws<McpException>(() => t.SaveSheet(), "a new sheet needs a path the first time");
            Assert.That(t.SaveSheet(_scratch), Does.StartWith("Saved"));
            Netlist back = NetlistReader.ReadFile(_scratch);
            Assert.That(back.Parts.Count, Is.EqualTo(2));
            Assert.That(back.Nets.Count, Is.EqualTo(2));
        }

        [Test]
        public void Refusals_SayWhy_AndChangeNothing()
        {
            SheetTools t = Tools();
            Assert.Throws<McpException>(() => t.ListParts(), "nothing open yet");
            t.OpenSheet(DemoSheetLayoutTests.Demo, edit: true);
            McpException ex = Assert.Throws<McpException>(() => t.ConnectPins("1A.1", "1A.2"))!;
            Assert.That(ex.Message, Does.StartWith("Refused: ").And.Contain("one signal only"));
            Assert.Throws<McpException>(() => t.Delete("gate", "1A/1"));
            Assert.Throws<McpException>(() => t.PlacePart("NOSUCHCHIP", 0, 0));
            Assert.That(t.Editor!.CanUndo, Is.False);
            Assert.Throws<McpException>(() => t.OpenSheet(Path.Combine(TestContext.CurrentContext.WorkDirectory, "no-such-sheet.json")));
        }

        [Test]
        public void EveryPublicToolMethod_IsAnMcpTool_WithADescription()
        {
            MethodInfo[] methods = typeof(SheetTools).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            int tools = 0;
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].IsSpecialName)
                    continue; // property getters
                McpServerToolAttribute? a = methods[i].GetCustomAttribute<McpServerToolAttribute>();
                Assert.That(a, Is.Not.Null, methods[i].Name + " is public but not a tool");
                Assert.That(methods[i].GetCustomAttribute<System.ComponentModel.DescriptionAttribute>(), Is.Not.Null, methods[i].Name);
                tools++;
            }
            // every tool of every tools file; add_passive (passive parts, 2026-11-23) made it 49,
            // the page tools (list_pages, add_page, set_part_page, set_page, get_page,
            // set_page_layout; format version 3) 55
            Assert.That(tools, Is.EqualTo(55));
            Assert.That(typeof(SheetTools).GetCustomAttribute<McpServerToolTypeAttribute>(), Is.Not.Null);
        }

        [Test]
        public void TheDemoSheet_IsFoundFromTheTestFolder()
        {
            Assert.That(SheetFiles.FindDemo(TestContext.CurrentContext.TestDirectory), Is.EqualTo(DemoSheetLayoutTests.Demo));
        }

        [Test]
        public void SignalInfo_ComesFromTheBusCatalogue_UntilADescriptionIsSet()
        {
            SheetTools t = Tools();
            t.OpenSheet(System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc"));
            string info = t.GetSignalInfo("IOXE_0");
            Assert.That(info, Does.Contain("What it does: IOX enable"));
            Assert.That(info, Does.Contain("Source: bus catalogue, BIOXE"));
            Assert.That(Assert.Throws<ModelContextProtocol.McpException>(() => t.SetSignalDescription("IOXE_0", "x", "reading"))!.Message, Does.Contain("read-only"));
            t.SetEditMode(true);
            Assert.That(t.SetSignalDescription("IOXE_0", "Starts an IOX transfer.", "reading", "docs/2A50-reading.md", "IOXE", "inferred"), Does.StartWith("Set the description"));
            info = t.GetSignalInfo("IOXE_0");
            Assert.That(info, Does.Contain("What it does: Starts an IOX transfer."));
            Assert.That(info, Does.Contain("Source: reading document docs/2A50-reading.md, IOXE (inferred)"));
        }

        /// <summary>
        /// The board tools: read the 2A50 board facts and switches, refuse changes read-only,
        /// then in edit mode change the board, add a ND-100 plug, a switch, a photo, and remove
        /// them again; list_sources finds every linked file.
        /// </summary>
        [Test]
        public void BoardTools_ReadAndChange_TheBoardFacts()
        {
            SheetTools t = Tools();
            string sheet = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
            t.OpenSheet(sheet);
            Assert.That(t.GetBoardInfo(), Does.Contain("\"partNumber\": \"322049\"").And.Contain("\"pins\": \"0-99\""));
            Assert.That(t.GetSwitches(), Does.Contain("\"ref\": \"15E\"").And.Contain("\"signal\": \"IDENT5\""));
            string list = t.ListSources();
            Assert.That(list, Does.Contain("2A50-sync-modem-buffer-1050-p64.png").And.Contain("component side").And.Contain("solder side"));
            Assert.That(list, Does.Not.Contain("NOT FOUND"));
            Assert.Throws<McpException>(() => t.SetBoardInfo("{\"title\": \"x\"}"), "read-only");

            t.SetEditMode(true);
            t.SetBoardInfo("{\"ndSystem\":\"ND-100\",\"partNumber\":\"1\",\"specifications\":{\"layers\":4}}");
            Assert.That(t.Editor!.Root.Board!.Specifications!.Layers, Is.EqualTo(4));
            Assert.Throws<McpException>(() => t.SetBoardInfo("{\"ndSystem\":\"NORD-1\"}"));
            Assert.Throws<McpException>(() => t.SetBoardInfo("{\"colour\":\"green\"}"), "unknown field");
            t.SetConnector("{\"ref\":\"P.A\",\"use\":\"not used\",\"used\":false,\"standard\":\"DIN 41612\",\"rows\":[\"a\",\"b\",\"c\"],\"pinsPerRow\":32}");
            Assert.That(t.GetBoardInfo(), Does.Contain("\"pinsPerRow\": 32"));
            t.RemoveConnector("P.A");
            Assert.That(t.GetBoardInfo(), Does.Not.Contain("P.A"));
            t.SetSwitch("{\"ref\":\"9Z\",\"leverCount\":2,\"onMeans\":\"open\",\"levers\":[{\"number\":1,\"confidence\":\"open\",\"setting\":\"open\"}]}");
            Assert.That(t.GetSwitches(), Does.Contain("9Z"));
            Assert.Throws<McpException>(() => t.SetSwitch("{\"ref\":\"9Y\",\"leverCount\":2,\"levers\":[{\"number\":3,\"confidence\":\"open\"}]}"), "lever 3 of 2");
            t.RemoveSwitch("9Z");
            string photo = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-files", "photos", "322049-solder-side-I-old.jpg");
            Assert.That(t.AddPhoto(photo, "solder"), Does.Contain("2A50-files/photos/322049-solder-side-I-old.jpg"), "a .rsc links by a relative path");
            Assert.That(t.ListSources(), Does.Contain("5. 2A50-files/photos/322049-solder-side-I-old.jpg - solder side [linked]"));
            t.RemovePhoto(5);
            Assert.Throws<McpException>(() => t.AddSource(Path.Combine(TestContext.CurrentContext.TestDirectory, "no-such.png")));
        }

        /// <summary>
        /// The chip grid tools on 2A50: the areas are read, a hand-set box wins and is removed
        /// again, and a photo's corners are set and removed.
        /// </summary>
        [Test]
        public void PcbGridTools_ReadAreas_SetBoxAndCorners()
        {
            SheetTools t = Tools();
            string sheet = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
            t.OpenSheet(sheet);
            string grid = t.GetPcbGrid();
            Assert.That(grid, Does.Contain("Board type: nord10-card").And.Contain("  13D 7442: 97.62,93.5").And.Contain("  1E switch:"));
            Assert.Throws<McpException>(() => t.SetChipBox("13D", "{\"xMm\":100,\"yMm\":105,\"widthMm\":8,\"heightMm\":20}"), "read-only");
            t.SetEditMode(true);
            t.SetChipBox("13D", "{\"xMm\":100,\"yMm\":105,\"widthMm\":8,\"heightMm\":20}");
            Assert.That(t.GetPcbGrid(), Does.Contain("  13D 7442 (by hand): 96,95"));
            t.SetChipBox("13D");
            Assert.That(t.GetPcbGrid(), Does.Contain("  13D 7442: 97.62,93.5"));
            t.SetPhotoCorners(2, "10 20 30 40 50 60 70 80");
            Assert.That(t.GetPcbGrid(), Does.Contain(": corners 10,20 30,40 50,60 70,80"));
            Assert.Throws<McpException>(() => t.SetPhotoCorners(2, "1 2 3"));
            t.SetPhotoCorners(2);
            Assert.That(t.GetPcbGrid(), Does.Not.Contain(": corners 10,20"));
        }

        /// <summary>
        /// The page tools on 2A50: list the one page, add a second, move the address decoder
        /// onto it, place a chip there, and read back which signals cross between the pages.
        /// </summary>
        [Test]
        public void PageTools_AddAPage_MoveParts_AndShowTheCrossingSignals()
        {
            SheetTools t = Tools();
            string sheet = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
            t.OpenSheet(sheet);
            Assert.That(t.ListPages(), Does.StartWith("* 1: SYNCHRONOUS MODEM BUFFER 1050, source 1, sheet 2A50, 40 chips, 62 parts"));
            Assert.Throws<McpException>(() => t.AddPage("Decoder"), "read-only");
            t.SetEditMode(true);
            Assert.That(t.AddPage("Decoder", 0, "2A50/2"), Is.EqualTo("Added page 2."));
            Assert.That(t.SetPartPage("13D, 13E", "2"), Is.EqualTo("Put 13D, 13E on page 2."));
            Assert.Throws<McpException>(() => t.SetPartPage("13D", "9"));
            string placed = t.PlacePart("7400", 20, 20, null, "2");
            string reference = placed.Substring(placed.LastIndexOf(' ') + 1).TrimEnd('.');
            Assert.That(t.Editor!.FindPart(reference)!.Page, Is.EqualTo("2"));
            Assert.That(t.Editor.Page, Is.Null, "the page argument does not change the page the tools work on");
            string page2 = t.GetPage("2");
            Assert.That(page2, Does.StartWith("Page 2: Decoder").And.Contain(" 13D 7442;").And.Contain(" 13E DM8160;"));
            Assert.That(page2, Does.Contain("Signals that cross to other pages").And.Contain(" -> page 1"));
            Assert.That(t.ListPages(), Does.Contain("  2: Decoder, sheet 2A50/2, 3 chips, 3 parts"));
            Assert.That(t.SetPage("2"), Is.EqualTo("Working on page 2."));
            Assert.That(t.ListPages(), Does.Contain("* 2: Decoder"));
            t.SetPageLayout("split");
            Assert.That(t.ListPages(), Does.Contain("split automatically; drawn pages: 1.1 1.2"));
            t.Undo();
            Assert.That(t.ListPages(), Does.Contain("as the original; drawn pages: 1 2"));
        }

        private static SheetTools Tools() => new(TestContext.CurrentContext.TestDirectory);
    }
}
