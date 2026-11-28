//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The helpers the window, the command-line tool and the MCP server share: the sheet check,
    /// the SVG drawing, the page measures, the library folder and the bus use of every card.
    /// </summary>
    [TestFixture]
    public class SharedHelperTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private static ChipCatalog Catalog() => new(DemoSheetLayoutTests.Catalog);

        [Test]
        public void SheetCheck_TheRealSheets_HaveNoProblems_AndListTheirMissingChips()
        {
            SheetCheckResult a = SheetCheck.Run(NetlistReader.ReadFile(Sheet2A50), Catalog());
            Assert.That(a.Ok, Is.True, SheetCheck.Report(a));
            Assert.That(a.Levels, Is.EqualTo(1));
            Assert.That(a.Parts, Is.EqualTo(62));
            Assert.That(a.Pins, Is.EqualTo(612));
            Assert.That(a.Missing, Is.EqualTo(new List<string> { "DM8160", "VP12/12" }));
            SheetCheckResult b = SheetCheck.Run(NetlistReader.ReadFile(CardReadingsTests.Sheet3207), Catalog());
            Assert.That(b.Ok, Is.True, SheetCheck.Report(b));
            Assert.That(b.Levels, Is.EqualTo(11), "the top level and ten blocks");
            Assert.That(b.Missing, Does.Contain("Z80A_CPU"));
            Assert.That(b.MissingParts["Z80A_CPU"][0], Does.StartWith("Z80_COMP/"));
            Assert.That(SheetCheck.Report(b), Does.EndWith("0 problem(s)\n"));
        }

        [Test]
        public void SheetCheck_FindsAPinTheChipHasNot_AndASupplyPinOffTheSupply()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            // 13D is a 7442 (16 pins): pin 40 does not exist, pin 16 is its power pin
            n.Nets[0].Pins.Add(new PinReference { At = "13D.40", Confidence = Confidence.Read });
            Net ba0 = null!;
            for (int i = 0; i < n.Nets.Count; i++)
            {
                if (n.Nets[i].Name == "BA0")
                    ba0 = n.Nets[i];
            }
            ba0.Pins.Add(new PinReference { At = "13D.16", Confidence = Confidence.Read });
            SheetCheckResult r = SheetCheck.Run(n, Catalog());
            Assert.That(r.Ok, Is.False);
            string all = string.Join("\n", r.Problems);
            Assert.That(all, Does.Contain("top: 13D.40 on " + n.Nets[0].Name + ": a 7442 has no pin 40"));
            Assert.That(all, Does.Contain("top: 13D.16 is a power pin of 7442 but is on signal BA0"));
        }

        [Test]
        public void SheetRender_DrawsThePageAsked_AndRefusesAPageThatIsNot()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            List<SvgPage> pages = SheetRender.Svg(n, Catalog(), new SvgRequest());
            Assert.That(pages.Count, Is.EqualTo(1));
            Assert.That(pages[0].Id, Is.EqualTo("1"));
            Assert.That(pages[0].Svg, Does.Contain("<svg"));
            List<SvgPage> split = SheetRender.Svg(n, Catalog(), new SvgRequest { Page = "all", Mode = PageLayoutMode.Split });
            Assert.That(split.Count, Is.GreaterThan(1), "40 chips are more than one split page");
            Assert.Throws<System.ArgumentException>(() => SheetRender.Svg(n, Catalog(), new SvgRequest { Page = "9" }));
        }

        [Test]
        public void PageStats_ReportBoth_MeasuresACardWithoutPages_WithoutChangingIt()
        {
            Netlist demo = NetlistReader.ReadFile(DemoSheetLayoutTests.Demo);
            Assert.That(CardPages.HasPages(demo), Is.False);
            string report = PageStats.ReportBoth(demo, Catalog(), PageSet.DefaultChipLimit);
            Assert.That(report, Does.StartWith("As the original: 1 drawn page(s)"));
            Assert.That(report, Does.Contain("Split automatically (limit 16 chips)"));
            Assert.That(CardPages.HasPages(demo), Is.False, "measured on a copy");
        }

        [Test]
        public void TheLibrary_IsBesideTheCatalogue_AndBusUseReadsEveryCard()
        {
            string? folder = CardLibrary.FolderBeside(DemoSheetLayoutTests.Catalog);
            Assert.That(folder, Is.EqualTo(Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets")));
            Assert.That(CardLibrary.FolderBeside(null), Is.Null);
            StringBuilder note = new();
            List<BusSignalUse> all = BusUse.OfLibrary(folder, Catalog(), null, null, note, out int cards);
            Assert.That(cards, Is.EqualTo(2), "2A50 (two files, one card) and 3207; the demo has no bus");
            Assert.That(note.ToString(), Does.Contain("Card 2A50 is in two files; 2A50-sync-modem-buffer-1050.rsc is used."));
            bool seen2A50 = false, seen3207 = false;
            for (int i = 0; i < all.Count; i++)
            {
                seen2A50 |= all[i].Card == "2A50";
                seen3207 |= all[i].Card == "3207";
            }
            Assert.That(seen2A50 && seen3207, Is.True);
            // the open sheet stands in for its file
            Netlist open = NetlistReader.ReadFile(Sheet2A50);
            open.Sheet.Id = "2A50";
            List<BusSignalUse> withOpen = BusUse.OfLibrary(folder, Catalog(), open, "open.rsc", new StringBuilder(), out int again);
            Assert.That(again, Is.EqualTo(2));
            bool fromOpen = false;
            for (int i = 0; i < withOpen.Count; i++)
                fromOpen |= withOpen[i].SheetFile == "open.rsc";
            Assert.That(fromOpen, Is.True);
        }
    }
}
