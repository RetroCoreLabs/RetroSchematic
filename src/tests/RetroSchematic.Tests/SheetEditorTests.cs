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
    /// The edit API that the window and the MCP tool share: read-only refuses everything, each
    /// change is checked, a pin is on one signal only, undo/redo, and saving round-trips.
    /// </summary>
    [TestFixture]
    public class SheetEditorTests
    {
        private string _scratch = "";

        [SetUp]
        public void SetUp()
        {
            _scratch = Path.Combine(TestContext.CurrentContext.WorkDirectory, "editor-" + TestContext.CurrentContext.Test.Name + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_scratch))
                File.Delete(_scratch);
        }

        [Test]
        public void ReadOnly_IsTheDefault_AndRefusesEveryChange()
        {
            SheetEditor e = Demo();
            Assert.That(e.ReadOnly, Is.True);
            string before = NetlistReader.Write(e.Netlist);
            Assert.Throws<SheetEditException>(() => e.PlacePart("7400", 10, 10));
            Assert.Throws<SheetEditException>(() => e.MovePart("1A", 50, 50));
            Assert.Throws<SheetEditException>(() => e.Connect("1A.13", "1A.12"));
            Assert.Throws<SheetEditException>(() => e.Disconnect("1A.1"));
            Assert.Throws<SheetEditException>(() => e.DeleteNet("AIN"));
            Assert.Throws<SheetEditException>(() => e.DeletePart("1A"));
            Assert.Throws<SheetEditException>(() => e.Undo());
            Assert.Throws<SheetEditException>(() => e.Redo());
            Assert.That(NetlistReader.Write(e.Netlist), Is.EqualTo(before));
            Assert.That(e.Modified, Is.False);
        }

        [Test]
        public void NewSheet_PlaceTwoChips_Wire_Save_ReadsBack()
        {
            SheetEditor e = new(SheetEditor.NewSheet("NEW", "New design"), new ChipCatalog(DemoSheetLayoutTests.Catalog)) { ReadOnly = false };
            string a = e.PlacePart("7400", 10, 10);
            string b = e.PlacePart("7404", 80, 10);
            Assert.That(a, Is.EqualTo("1A"));
            Assert.That(b, Is.EqualTo("2A"));
            Assert.That(e.Netlist.Gates.Count, Is.EqualTo(4 + 6), "gates come from the chip files' formulas");
            Assert.That(e.Netlist.Gates[0].Kind, Is.EqualTo(GateKind.Nand));
            Assert.That(e.Netlist.Gates[4].Kind, Is.EqualTo(GateKind.Not));
            string net = e.Connect("1A.3", "2A.1");
            Assert.That(net, Is.EqualTo("N1"));
            Assert.That(e.Connect("2A.3", "1A.3"), Is.EqualTo("N1"), "a free pin joins the other pin's signal");
            Assert.That(e.Connect("1A.1", "P.4", "INA"), Is.EqualTo("INA"));

            e.Save(_scratch);
            Netlist back = NetlistReader.ReadFile(_scratch);
            Assert.That(back.Parts.Count, Is.EqualTo(2));
            Assert.That(back.Parts[1].Position!.X, Is.EqualTo(80));
            Assert.That(back.Nets[0].Pins.Count, Is.EqualTo(3));
            Assert.That(File.ReadAllText(_scratch), Does.Not.Contain("null"), "absent fields must be left out, not written as null");
            Assert.That(e.Modified, Is.False);
        }

        [Test]
        public void APin_IsOnOneSignalOnly()
        {
            SheetEditor e = Demo();
            e.ReadOnly = false;
            // 1A.1 is on AIN, 1A.2 on BIN: joining them would put a pin on two signals
            SheetEditException ex = Assert.Throws<SheetEditException>(() => e.Connect("1A.1", "1A.2"))!;
            Assert.That(ex.Message, Does.Contain("one signal only"));
            Assert.Throws<SheetEditException>(() => e.Connect("1A.1", "1A.4"), "both already on AIN");
            Assert.Throws<SheetEditException>(() => e.Connect("1A.1", "1A.1"));
            Assert.Throws<SheetEditException>(() => e.Connect("1A.99", "1A.12"), "a 7400 has no pin 99");
            Assert.Throws<SheetEditException>(() => e.Connect("9Z.1", "1A.12"), "no part 9Z");
            Assert.Throws<SheetEditException>(() => e.Connect("1A.12", "1A.13", "AIN"), "the name is taken");
            Assert.That(e.CanUndo, Is.False, "a refused change leaves nothing to undo");
        }

        [Test]
        public void Disconnect_AndDeleteNet_AndDeletePart_TakeEverythingAway()
        {
            SheetEditor e = Demo();
            e.ReadOnly = false;
            e.Disconnect("4A.2");
            Assert.That(e.NetOf("4A.2"), Is.Null);
            Assert.That(e.FindNet("DATA")!.Pins.Count, Is.EqualTo(1));
            e.Disconnect("3A.2");
            Assert.That(e.FindNet("DATA"), Is.Null, "a signal with no pins left is removed");
            e.DeleteNet("AIN");
            Assert.That(e.NetOf("1A.1"), Is.Null);
            e.DeletePart("1A");
            Assert.That(e.FindPart("1A"), Is.Null);
            for (int i = 0; i < e.Netlist.Gates.Count; i++)
                Assert.That(e.Netlist.Gates[i].Part, Is.Not.EqualTo("1A"));
            Assert.That(e.NetOf("1A.6"), Is.Null);
            Assert.That(e.FindNet("NAB2")!.Pins.Count, Is.EqualTo(1), "NAB2 keeps 2A.3");
            Assert.Throws<SheetEditException>(() => e.DeletePart("1A"));
        }

        [Test]
        public void Undo_AndRedo_StepThroughTheChanges()
        {
            SheetEditor e = Demo();
            e.ReadOnly = false;
            string original = NetlistReader.Write(e.Netlist);
            e.DeletePart("5A");
            string afterDelete = NetlistReader.Write(e.Netlist);
            e.Connect("1A.13", "1A.12");
            e.Undo();
            Assert.That(NetlistReader.Write(e.Netlist), Is.EqualTo(afterDelete));
            e.Undo();
            Assert.That(NetlistReader.Write(e.Netlist), Is.EqualTo(original));
            Assert.That(e.CanUndo, Is.False);
            Assert.Throws<SheetEditException>(() => e.Undo());
            e.Redo();
            Assert.That(NetlistReader.Write(e.Netlist), Is.EqualTo(afterDelete));
            // a new change after an undo throws the redo list away
            e.Undo();
            e.MovePart("1A", 0, 0);
            Assert.That(e.CanRedo, Is.False);
        }

        [Test]
        public void MovePart_MovesOnlyTheDrawing_NotTheSignals()
        {
            SheetEditor e = Demo();
            e.ReadOnly = false;
            int nets = e.Netlist.Nets.Count;
            string signals = SignalsText(e.Netlist);
            double gx = e.Netlist.Gates[0].Position!.X;
            SchematicDrawing before = SheetLayout.Build(e.Netlist, SymbolStyle.NorskData);
            double fx = SheetLayout.ToMm(before.Frames["1A"].X);
            e.MovePart("1A", fx + 10, SheetLayout.ToMm(before.Frames["1A"].Y));
            Assert.That(e.Netlist.Gates[0].Position!.X, Is.EqualTo(gx + 10), "measured gates move with their chip");
            Assert.That(e.Netlist.Parts[0].Position, Is.Null, "a chip framed by its measured gates stays that way");
            Assert.That(e.Netlist.Nets.Count, Is.EqualTo(nets));
            Assert.That(SignalsText(e.Netlist), Is.EqualTo(signals));
            SchematicDrawing after = SheetLayout.Build(e.Netlist, SymbolStyle.NorskData);
            Assert.That(after.Frames["1A"].X, Is.EqualTo(before.Frames["1A"].X + (10 * SheetLayout.UnitsPerMm)));
        }

        [Test]
        public void PlacingAFlipFlop_GivesNoMadeUpGates_ButListsEveryPin()
        {
            SheetEditor e = new(SheetEditor.NewSheet("NEW", "x"), new ChipCatalog(DemoSheetLayoutTests.Catalog)) { ReadOnly = false };
            string ff = e.PlacePart("7474", 20, 20, "3C");
            Assert.That(ff, Is.EqualTo("3C"));
            Assert.That(e.Netlist.Gates.Count, Is.EqualTo(0), "1Q = state1 is not a plain gate");
            SchematicDrawing d = SheetLayout.Build(e.Netlist, SymbolStyle.NorskData, new LayoutOptions { Catalog = e.Catalog });
            for (int pin = 1; pin <= 14; pin++)
                Assert.That(d.Pins.ContainsKey("3C." + pin), "pin " + pin + " of the 7474 is not drawn");
            Assert.That(d.Frames["3C"].X, Is.EqualTo(SheetLayout.FromMm(20)));
            Assert.Throws<SheetEditException>(() => e.PlacePart("7474", 0, 0, "3C"), "3C is taken");
            Assert.Throws<SheetEditException>(() => e.PlacePart("7474", 0, 0, "X1"), "not a board position");
            Assert.Throws<SheetEditException>(() => e.PlacePart("NOSUCHCHIP", 0, 0));
        }

        [Test]
        public void GateFromFormula_KnowsThePlainGates_AndNothingElse()
        {
            ChipInfo c7400 = new ChipCatalog(DemoSheetLayoutTests.Catalog).Get("7400")!;
            Gate g = ChipCatalog.GateFromFormula("1Y = NOT(1A AND 1B)", "3", c7400.Pins)!;
            Assert.That(g.Kind, Is.EqualTo(GateKind.Nand));
            Assert.That(g.Inputs, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(g.Output, Is.EqualTo(3));
            Assert.That(ChipCatalog.GateFromFormula("1Y = 1A XOR 1B", "3", c7400.Pins), Is.Null);
            Assert.That(ChipCatalog.GateFromFormula("1Y = NOT(1A AND 1Q)", "3", c7400.Pins), Is.Null, "1Q is not an input pin");
            Assert.That(ChipCatalog.GateFromFormula("QA = x", "14,13", c7400.Pins), Is.Null);
            ChipInfo c7402 = new ChipCatalog(DemoSheetLayoutTests.Catalog).Get("7402")!;
            Assert.That(c7402.Gates[0].Kind, Is.EqualTo(GateKind.Nor));
            Assert.That(c7402.Gates[0].Output, Is.EqualTo(1));
        }

        private static SheetEditor Demo()
            => new(NetlistReader.ReadFile(DemoSheetLayoutTests.Demo), new ChipCatalog(DemoSheetLayoutTests.Catalog));

        private static string SignalsText(Netlist n)
        {
            System.Text.StringBuilder sb = new();
            for (int i = 0; i < n.Nets.Count; i++)
            {
                sb.Append(n.Nets[i].Name).Append(':');
                for (int j = 0; j < n.Nets[i].Pins.Count; j++)
                    sb.Append(n.Nets[i].Pins[j].At).Append(',');
                sb.Append(';');
            }
            return sb.ToString();
        }
    }
}
