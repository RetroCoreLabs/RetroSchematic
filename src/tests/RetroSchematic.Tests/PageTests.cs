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
    /// Format version 3, a card with pages (docs/FORMAT.md, "Format version 3: pages"): the
    /// reader's checks, the conversion from older files, the card-wide signal checks, the
    /// off-page markers, going from page to page with Back and Forward, and the automatic split.
    /// </summary>
    [TestFixture]
    public class PageTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private static readonly string Demo = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "sheets", "demo", "demo-gates.rsc");

        // the address decoder of 2A50 (DM8160 comparator, 7442 decoder) goes on page 2 in the
        // tests that need two pages
        private static readonly string[] Decoder = { "13E", "13D" };

        private static SheetEditor Open()
            => new(NetlistReader.ReadFile(Sheet2A50), new ChipCatalog(ChipCatalog.Find(Sheet2A50)!)) { ReadOnly = false };

        private static SheetEditor TwoPages()
        {
            SheetEditor e = Open();
            string p2 = e.AddPage("Address decoder", null, "2A50/2");
            for (int i = 0; i < Decoder.Length; i++)
                e.SetPartPage(Decoder[i], p2);
            return e;
        }

        [Test]
        public void TheCard_IsVersion3_WithOnePageHoldingEveryPart()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            Assert.That(n.FormatVersion, Is.EqualTo(3));
            Assert.That(n.Pages, Has.Count.EqualTo(1));
            Assert.That(n.Pages![0].Source, Is.EqualTo(1), "read from the one source page");
            for (int i = 0; i < n.Parts.Count; i++)
                Assert.That(n.Parts[i].Page, Is.EqualTo("1"), n.Parts[i].Ref);
        }

        [Test]
        public void TheReader_RefusesBrokenPageFiles_WithAReason()
        {
            string good = NetlistReader.Write(NetlistReader.ReadFile(Sheet2A50));
            // a version 3 file without a pages list
            Assert.That(() => NetlistReader.Parse(good.Replace("\"pages\":", "\"pagesX\":")),
                Throws.TypeOf<NetlistException>());
            // a part on a page that does not exist
            Assert.That(() => NetlistReader.Parse(ReplaceFirst(good, "\"page\": \"1\"", "\"page\": \"9\"")),
                Throws.TypeOf<NetlistException>().With.Message.Contains("not in the pages list"));
            // a part with no page
            Assert.That(() => NetlistReader.Parse(ReplaceFirst(good, ",\n      \"page\": \"1\"", "")),
                Throws.TypeOf<NetlistException>().With.Message.Contains("names no page"));
            // pages in an older version
            Assert.That(() => NetlistReader.Parse(good.Replace("\"formatVersion\": 3", "\"formatVersion\": 1")),
                Throws.TypeOf<NetlistException>().With.Message.Contains("version 3"));
            // a page read from a source the card does not have
            Netlist n = NetlistReader.Parse(good);
            n.Pages![0].Source = 5;
            Assert.That(() => NetlistReader.Parse(NetlistReader.Write(n)),
                Throws.TypeOf<NetlistException>().With.Message.Contains("source 5"));
            // two pages with one id
            n.Pages[0].Source = 1;
            n.Pages.Add(new CardPage { Id = "1" });
            Assert.That(() => NetlistReader.Parse(NetlistReader.Write(n)),
                Throws.TypeOf<NetlistException>().With.Message.Contains("listed twice"));
        }

        [Test]
        public void Conversion_KeepsEveryConnection_AndRoundTripsUnchanged()
        {
            Netlist old = NetlistReader.ReadFile(Demo);
            Assert.That(old.FormatVersion, Is.EqualTo(1), "the demo sheet stays version 1, so the old versions keep a real file");
            string before = Hierarchy.Connections(old);
            Assert.That(CardPages.ToVersion3(old), Is.True);
            Assert.That(CardPages.ToVersion3(old), Is.False, "a card with pages is left as it is");
            string json = NetlistReader.Write(old);
            Assert.That(json, Does.Contain("\"formatVersion\": 3"));
            Netlist back = NetlistReader.Parse(json);
            Assert.That(Hierarchy.Connections(back), Is.EqualTo(before));
            Assert.That(NetlistReader.Write(back), Is.EqualTo(json), "read and written back unchanged");
            Assert.That(back.Pages![0].Title, Is.EqualTo(old.Sheet.Title));
        }

        [Test]
        public void Conversion_ThroughTheEditor_CanBeUndone()
        {
            SheetEditor e = new(NetlistReader.ReadFile(Demo), null) { ReadOnly = false };
            Assert.That(e.ConvertToPages(), Is.True);
            Assert.That(e.Root.Pages, Has.Count.EqualTo(1));
            e.Undo();
            Assert.That(e.Root.Pages, Is.Null);
            Assert.That(NetlistReader.Write(e.Root), Does.Contain("\"formatVersion\": 1"));
        }

        [Test]
        public void TheChecks_AreCardWide()
        {
            SheetEditor e = TwoPages();
            Netlist n = NetlistReader.Parse(NetlistReader.Write(e.Root));
            // one pin in two signals is refused even when the signals' other parts are on
            // different pages: the signals belong to the card, not to a page
            Net a = n.Nets[0];
            Net b = n.Nets[1];
            b.Pins.Add(new PinReference { At = a.Pins[0].At, Confidence = Confidence.Read });
            Assert.That(() => NetlistReader.Parse(NetlistReader.Write(n)),
                Throws.TypeOf<NetlistException>().With.Message.Contains("is in two nets"));
        }

        [Test]
        public void ASignalAcrossPages_GetsOffPageMarkers_NamingTheOtherPage()
        {
            SheetEditor e = TwoPages();
            PageSet set = PageSet.Of(e.Root);
            Assert.That(set.Pages, Has.Count.EqualTo(2));
            SchematicDrawing p1 = SheetLayout.Build(e.Root, SymbolStyle.NorskData, new LayoutOptions { Catalog = e.Catalog, Pages = set, Page = 0 });
            SchematicDrawing p2 = SheetLayout.Build(e.Root, SymbolStyle.NorskData, new LayoutOptions { Catalog = e.Catalog, Pages = set, Page = 1 });
            Assert.That(p1.Frames.ContainsKey("13D"), Is.False, "13D is drawn on page 2 only");
            Assert.That(p2.Frames.ContainsKey("13D"), Is.True);
            Assert.That(p2.Frames.Count, Is.EqualTo(2));
            // every signal of the decoder that also reaches page 1 has a marker on page 2 naming
            // page 1, and the other way round
            Assert.That(p2.OffPage.Count, Is.GreaterThan(0));
            foreach (KeyValuePair<string, List<string>> kv in p2.OffPage)
            {
                Assert.That(kv.Value, Is.EqualTo(new List<string> { "1" }), kv.Key);
                Assert.That(p1.OffPage.ContainsKey(kv.Key), Is.True, kv.Key + " has a marker on page 1 too");
                Assert.That(p1.OffPage[kv.Key], Is.EqualTo(new List<string> { "2" }));
            }
            // a marker can be clicked (it is an item of its own)
            bool found = false;
            for (int i = 0; i < p2.Items.Count && !found; i++)
                found = p2.Items[i].Role == DrawRole.OffPage && p2.Items[i].Owner.StartsWith(SheetLayout.OffPageOwner, System.StringComparison.Ordinal);
            Assert.That(found, Is.True);
        }

        [Test]
        public void ClickingAnOffPageMarker_GoesToThePage_AndBackAndForwardFollow()
        {
            SheetEditor e = TwoPages();
            {
                SheetSession s = new(NetlistReader.Parse(NetlistReader.Write(e.Root)), null, SymbolStyle.NorskData, ChipCatalog.Find(Sheet2A50));
                Assert.That(s.PageCount, Is.EqualTo(2));
                Assert.That(s.CurrentPage!.Id, Is.EqualTo("1"));
                NavHistory history = new();
                history.Push(s.PlaceNow());

                // a marker on page 1
                DrawItem? marker = null;
                for (int i = 0; i < s.Drawing.Items.Count && marker == null; i++)
                {
                    if (s.Drawing.Items[i].Role == DrawRole.OffPage && s.Drawing.Items[i].Kind == DrawKind.Rect)
                        marker = s.Drawing.Items[i];
                }
                Assert.That(marker, Is.Not.Null);
                string key = marker!.Owner.Substring(SheetLayout.OffPageOwner.Length);
                s.TraceAt(marker.X1 + 2, marker.Y1 + 2);
                Assert.That(s.CurrentPage!.Id, Is.EqualTo("2"), "the marker took us to page 2");
                Assert.That(s.Selected, Is.EqualTo("net:" + key), "the signal is picked there");
                Assert.That(s.Traced, Is.EqualTo(key), "and traced");
                Assert.That(s.PagesOfSignal(key), Is.EqualTo(new List<string> { "1", "2" }));
                history.Push(s.PlaceNow());

                // Back goes to page 1 again, Forward to page 2
                s.GoTo(history.Back()!);
                Assert.That(s.CurrentPage!.Id, Is.EqualTo("1"));
                s.GoTo(history.Forward()!);
                Assert.That(s.CurrentPage!.Id, Is.EqualTo("2"));
                Assert.That(s.Selected, Is.EqualTo("net:" + key));

                // Page Up / Page Down stop at the ends
                s.StepPage(1);
                Assert.That(s.PageIndex, Is.EqualTo(1));
                s.StepPage(-1);
                Assert.That(s.PageIndex, Is.EqualTo(0));
                s.StepPage(-1);
                Assert.That(s.PageIndex, Is.EqualTo(0));

                // a search result on another page shows that page
                s.PickAndTrace("part:13E");
                Assert.That(s.CurrentPage!.Id, Is.EqualTo("2"));
                Assert.That(s.Selected, Is.EqualTo("part:13E"));
            }
        }

        [Test]
        public void APlacedPart_GoesOnThePageShown()
        {
            SheetEditor e = TwoPages();
            e.Page = "2";
            string r = e.PlacePart("7400", 40, 40);
            Assert.That(e.FindPart(r)!.Page, Is.EqualTo("2"));
            // and the file is still valid (every part is on a page)
            Assert.That(() => NetlistReader.Parse(NetlistReader.Write(e.Root)), Throws.Nothing);
        }

        [Test]
        public void ABlockMadeOnAPagedCard_KeepsTheFileValid()
        {
            SheetEditor e = TwoPages();
            e.MakeBlock("decoder", null, new List<string>(Decoder), null);
            Assert.That(() => NetlistReader.Parse(NetlistReader.Write(e.Root)), Throws.Nothing);
            Assert.That(e.Root.Blocks![0].Parts[0].Page, Is.Null, "a card page is taken off a part moved into a block");
            e.DissolveInstance("U1");
            Assert.That(e.FindPart("13E")!.Page, Is.Not.Null, "back on a page");
        }

        [Test]
        public void TheSplit_PutsEveryPartOnOnePage_AndKeepsPassivesWithTheirChip()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            PageSet set = PageSet.Of(n, PageLayoutMode.Split, 16);
            Assert.That(set.Pages.Count, Is.GreaterThan(1), "40 chips is over the limit of 16");
            int total = 0;
            for (int p = 0; p < set.Pages.Count; p++)
            {
                total += set.Pages[p].Parts.Count;
                Assert.That(set.Pages[p].Id, Is.EqualTo("1." + (p + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            Assert.That(total, Is.EqualTo(n.Parts.Count), "every part on exactly one page");
            for (int i = 0; i < n.Parts.Count; i++)
            {
                Part part = n.Parts[i];
                Assert.That(set.PageOfPart(part.Ref), Is.GreaterThanOrEqualTo(0), part.Ref);
                if (part.Serves != null)
                    Assert.That(set.PageOfPart(part.Ref), Is.EqualTo(set.PageOfPart(part.Serves)), part.Ref + " stays with " + part.Serves);
            }
            // the connector box is on one of the pages
            Assert.That(set.PageOfConnector("P"), Is.GreaterThanOrEqualTo(0));
            // the same card as the original: one page
            Assert.That(PageSet.Of(n, PageLayoutMode.AsOriginal, 16).Pages, Has.Count.EqualTo(1));
            // under the limit nothing is split
            Assert.That(PageSet.Of(n, PageLayoutMode.Split, 100).Pages, Has.Count.EqualTo(1));
        }

        [Test]
        public void TheSplit_KeepsARegisterGroupTogether()
        {
            // two chips of one type that share two signals (a common clock and clear) are one
            // unit, so they end on one page however the rest is grouped
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            List<Part> parts = n.Parts;
            List<List<Part>> groups = PageSet.Split(n, parts, 4);
            Dictionary<string, int> pageOf = new();
            for (int g = 0; g < groups.Count; g++)
            {
                for (int i = 0; i < groups[g].Count; i++)
                    pageOf[groups[g][i].Ref] = g;
            }
            // the four 74153 status multiplexers 3D, 3E, 5D, 5E share their two select lines
            // (the fifth 74153, 13C, shares none with them)
            string[] muxes = { "3D", "3E", "5D", "5E" };
            for (int i = 1; i < muxes.Length; i++)
                Assert.That(pageOf[muxes[i]], Is.EqualTo(pageOf[muxes[0]]), muxes[i] + " with " + muxes[0]);
        }

        [Test]
        public void SplitPages_AreMeasured()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            ChipCatalog cat = new(ChipCatalog.Find(Sheet2A50)!);
            List<PageMeasure> one = PageStats.Measure(n, cat, PageLayoutMode.AsOriginal, 16);
            List<PageMeasure> split = PageStats.Measure(n, cat, PageLayoutMode.Split, 16);
            Assert.That(one, Has.Count.EqualTo(1));
            Assert.That(one[0].OffPageMarkers, Is.EqualTo(0), "one page: nothing goes off it");
            Assert.That(split.Count, Is.GreaterThan(1));
            int markers = 0;
            for (int i = 0; i < split.Count; i++)
                markers += split[i].OffPageMarkers;
            Assert.That(markers, Is.GreaterThan(0));
        }

        private static string ReplaceFirst(string text, string what, string with)
        {
            string t = text.Replace("\r\n", "\n");
            int i = t.IndexOf(what, System.StringComparison.Ordinal);
            Assert.That(i, Is.GreaterThanOrEqualTo(0), "the test text has " + what);
            return t.Substring(0, i) + with + t.Substring(i + what.Length);
        }
    }
}
