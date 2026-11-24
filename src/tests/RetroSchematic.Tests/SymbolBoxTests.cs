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
    /// Symbol boxes on the original pages (docs/FORMAT.md, "Symbol boxes"): the reader's checks,
    /// writing and reading back, accepting a find of the page analyser as a new part or linking it
    /// to one, connecting a pin at a place on the page, and undo.
    /// </summary>
    [TestFixture]
    public class SymbolBoxTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private static SheetEditor Open()
            => new(NetlistReader.ReadFile(Sheet2A50), new ChipCatalog(ChipCatalog.Find(Sheet2A50)!)) { ReadOnly = false };

        private static SymbolBox Chip15B() => new()
        {
            Source = 1, X = 2219, Y = 1081, Width = 92, Height = 418, Kind = "chip", Score = 0.909,
        };

        private static string WithBoxes(string boxesJson)
        {
            string json = NetlistReader.Write(NetlistReader.ReadFile(Sheet2A50));
            int end = json.LastIndexOf('}');
            return json.Substring(0, end).TrimEnd() + ",\n  \"symbolBoxes\": " + boxesJson + "\n}";
        }

        [Test]
        public void ABoxIsWrittenAndReadBack()
        {
            SheetEditor e = Open();
            e.SetSymbolBox(new SymbolBox { Source = 1, X = 10, Y = 20, Width = 30, Height = 40, Kind = "gate", Ref = "11C", Gate = 2, GateKind = "not", By = "hand", Note = "n" });
            Netlist back = NetlistReader.Parse(NetlistReader.Write(e.Root));
            Assert.That(back.SymbolBoxes, Has.Count.EqualTo(1));
            SymbolBox b = back.SymbolBoxes![0];
            Assert.That((b.Source, b.X, b.Y, b.Width, b.Height, b.Kind, b.Ref, b.Gate, b.GateKind, b.By, b.Note),
                Is.EqualTo((1, 10.0, 20.0, 30.0, 40.0, "gate", "11C", (int?)2, "not", "hand", "n")));
            Assert.That(NetlistReader.Write(e.Root), Does.Contain("\"symbolBoxes\""));
        }

        [TestCase("[{\"source\":2,\"x\":1,\"y\":1,\"width\":5,\"height\":5,\"kind\":\"chip\",\"by\":\"hand\"}]", "source 2")]
        [TestCase("[{\"source\":1,\"x\":1,\"y\":1,\"width\":0,\"height\":5,\"kind\":\"chip\",\"by\":\"hand\"}]", "no size")]
        [TestCase("[{\"source\":1,\"x\":1,\"y\":1,\"width\":5,\"height\":5,\"kind\":\"bird\",\"by\":\"hand\"}]", "bird")]
        [TestCase("[{\"source\":1,\"x\":1,\"y\":1,\"width\":5,\"height\":5,\"kind\":\"chip\",\"ref\":\"99Z\",\"by\":\"hand\"}]", "99Z")]
        [TestCase("[{\"source\":1,\"x\":1,\"y\":1,\"width\":5,\"height\":5,\"kind\":\"gate\",\"gate\":1,\"by\":\"hand\"}]", "without a part")]
        [TestCase("[{\"source\":1,\"x\":1,\"y\":1,\"width\":5,\"height\":5,\"kind\":\"chip\",\"by\":\"robot\"}]", "robot")]
        public void TheReaderRefusesABadBox(string boxes, string says)
        {
            NetlistException? ex = Assert.Throws<NetlistException>(() => NetlistReader.Parse(WithBoxes(boxes)));
            Assert.That(ex!.Message, Does.Contain(says));
        }

        [Test]
        public void AVersion1FileHasNoBoxes()
        {
            string demo = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "demo", "demo-gates.rsc");
            System.Text.Json.Nodes.JsonNode n = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(demo))!;
            Assert.That((int)n["formatVersion"]!, Is.LessThan(3));
            n["sources"] = System.Text.Json.Nodes.JsonNode.Parse("[{\"file\":\"page.png\"}]");
            n["symbolBoxes"] = System.Text.Json.Nodes.JsonNode.Parse("[{\"source\":1,\"x\":1,\"y\":1,\"width\":5,\"height\":5,\"kind\":\"circle\",\"by\":\"hand\"}]");
            NetlistException? ex = Assert.Throws<NetlistException>(() => NetlistReader.Parse(n.ToJsonString()));
            Assert.That(ex!.Message, Does.Contain("format version 3"));
        }

        [Test]
        public void AcceptingAChipFindLinksItToThePartWithThatCode()
        {
            SheetEditor e = Open();
            int parts = e.Root.Parts.Count;
            string? part = e.AcceptFind(Chip15B(), "15B", null, null);
            Assert.That(part, Is.EqualTo("15B"));
            Assert.That(e.Root.Parts, Has.Count.EqualTo(parts), "no new part");
            SymbolBox b = e.Root.SymbolBoxes![0];
            Assert.That((b.Ref, b.By, b.Score), Is.EqualTo(("15B", "tool", (double?)0.909)));
            e.Undo();
            Assert.That(e.Root.SymbolBoxes, Is.Null);
        }

        [Test]
        public void AcceptingAFindWithANewCodeMakesTheChipAndItsGates()
        {
            SheetEditor e = Open();
            SymbolBox g = new() { Source = 1, X = 100, Y = 100, Width = 60, Height = 70, Kind = "gate", GateKind = "nand", Score = 0.8 };
            Assert.That(e.AcceptFind(g, "19E", "7400", null), Is.EqualTo("19E"));
            Part p = e.FindPart("19E")!;
            Assert.That((p.Type, p.Confidence, p.Page), Is.EqualTo(("7400", Confidence.Inferred, e.Root.Pages![0].Id)));
            int gates = 0;
            for (int i = 0; i < e.Root.Gates.Count; i++)
                gates += e.Root.Gates[i].Part == "19E" ? 1 : 0;
            Assert.That(gates, Is.EqualTo(4));
            Assert.That(e.Root.SymbolBoxes![0].Gate, Is.EqualTo(1));

            // a second gate of the same package goes on gate 2 of the same part
            SymbolBox g2 = new() { Source = 1, X = 100, Y = 200, Width = 60, Height = 70, Kind = "gate", GateKind = "nand" };
            Assert.That(e.AcceptFind(g2, "19E", null, null), Is.EqualTo("19E"));
            Assert.That(e.Root.SymbolBoxes![1].Gate, Is.EqualTo(2));
            Assert.That(e.Root.Parts.Count, Is.EqualTo(NetlistReader.ReadFile(Sheet2A50).Parts.Count + 1));

            // the sheet still passes the reader's checks
            Assert.DoesNotThrow(() => NetlistReader.Parse(NetlistReader.Write(e.Root)));

            // each accept is one undo step
            e.Undo();
            Assert.That(e.Root.SymbolBoxes, Has.Count.EqualTo(1));
            e.Undo();
            Assert.That(e.FindPart("19E"), Is.Null);
            Assert.That(e.Root.SymbolBoxes, Is.Null);
        }

        [Test]
        public void AFindCanBeLinkedToAnyExistingPart()
        {
            SheetEditor e = Open();
            Assert.That(e.AcceptFind(Chip15B(), "3X", null, "13D"), Is.EqualTo("13D"));
            Assert.That(e.Root.SymbolBoxes![0].Ref, Is.EqualTo("13D"));
            Assert.Throws<SheetEditException>(() => e.AcceptFind(Chip15B(), null, null, "99Z"));
        }

        [Test]
        public void ANewCodeWithoutATypeIsRefused()
        {
            SheetEditor e = Open();
            SheetEditException? ex = Assert.Throws<SheetEditException>(() => e.AcceptFind(Chip15B(), "19E", null, null));
            Assert.That(ex!.Message, Does.Contain("chip type"));
            Assert.That(e.Root.SymbolBoxes, Is.Null);
        }

        [Test]
        public void APinAtAPlaceOnThePageJoinsASignal()
        {
            SheetEditor e = Open();
            e.AcceptFind(new SymbolBox { Source = 1, X = 100, Y = 100, Width = 60, Height = 70, Kind = "gate" }, "19E", "7400", null);
            string signal = e.Root.Nets[0].Name;

            // a wire end just left of the box (inside the margin) picks part 19E
            (string pin, string net) = e.ConnectPinOnPage(1, 80, 120, 1, signal);
            Assert.That((pin, net), Is.EqualTo(("19E.1", signal)));
            Assert.That(e.NetOf("19E.1")!.Name, Is.EqualTo(signal));

            // two pins make a new signal
            (_, string made) = e.ConnectPinOnPage(1, 130, 130, 2, "13D.1");
            Assert.That(e.NetOf("19E.2")!.Name, Is.EqualTo(made));

            e.Undo();
            Assert.That(e.NetOf("19E.2")?.Name, Is.Not.EqualTo(made).Or.Null);
            e.Undo();
            Assert.That(e.NetOf("19E.1"), Is.Null);

            Assert.Throws<SheetEditException>(() => e.ConnectPinOnPage(1, 3000, 3000, 1, signal), "no part there");
        }

        [Test]
        public void ThePinoutComesFromTheCatalogue()
        {
            SheetEditor e = Open();
            List<ChipPin> pins = e.PinoutOf("13D");
            Assert.That(pins.Count, Is.EqualTo(16), "7442: 16 pins");
            Assert.That(e.PinoutOf("99Z"), Is.Empty);
        }
    }
}
