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
    /// The editor's fact edits (rename a part, set a part's facts, add and remove a gate, add and
    /// rename a signal, the title block) and the piece-by-piece block structure (block, port,
    /// instance, port connection), on the real 2A50 sheet. Each is one undo step and refused
    /// read-only.
    /// </summary>
    [TestFixture]
    public class SheetEditorEditsTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private static SheetEditor Editor(bool edit = true)
            => new(NetlistReader.ReadFile(Sheet2A50), new ChipCatalog(DemoSheetLayoutTests.Catalog)) { ReadOnly = !edit };

        [Test]
        public void RenamePart_MovesItsGatesAndPins_AndUndoBringsItBack()
        {
            SheetEditor e = Editor();
            string start = NetlistReader.Write(e.Root);
            // 11D is a 7402: gate 11D/1 (2,3>1); pin 11D.1 is on ISCW
            e.RenamePart("11D", "40Z");
            Assert.That(e.FindPart("11D"), Is.Null);
            Assert.That(e.FindPart("40Z")!.Type, Is.EqualTo("7402"));
            Gate? g = null;
            for (int i = 0; i < e.Netlist.Gates.Count; i++)
            {
                if (e.Netlist.Gates[i].Id == "40Z/1")
                    g = e.Netlist.Gates[i];
            }
            Assert.That(g, Is.Not.Null);
            Assert.That(g!.Part, Is.EqualTo("40Z"));
            Assert.That(e.NetOf("40Z.1")!.Name, Is.EqualTo("ISCW"));
            Assert.That(e.NetOf("11D.1"), Is.Null);
            e.Undo();
            Assert.That(NetlistReader.Write(e.Root), Is.EqualTo(start));
        }

        [Test]
        public void RenamePart_RefusesATakenName_AndReadOnly()
        {
            Assert.Throws<SheetEditException>(() => Editor(false).RenamePart("11D", "40Z"));
            SheetEditor e = Editor();
            SheetEditException ex = Assert.Throws<SheetEditException>(() => e.RenamePart("11D", "13D"))!;
            Assert.That(ex.Message, Does.Contain("taken"));
            Assert.Throws<SheetEditException>(() => e.RenamePart("11D", "P"), "P names the backplane connector");
            Assert.That(e.CanUndo, Is.False);
        }

        [Test]
        public void SetPartInfo_ChangesOnlyWhatIsGiven()
        {
            SheetEditor e = Editor();
            e.SetPartInfo("13D", null, "decoder", Confidence.Inferred, "a note");
            Part p = e.FindPart("13D")!;
            Assert.That(p.Type, Is.EqualTo("7442"));
            Assert.That(p.Function, Is.EqualTo("decoder"));
            Assert.That(p.Confidence, Is.EqualTo(Confidence.Inferred));
            e.SetPartInfo("13D", null, "", null, null);
            Assert.That(e.FindPart("13D")!.Function, Is.Null, "empty removes");
            Assert.That(e.FindPart("13D")!.Note, Is.EqualTo("a note"), "null keeps");
            Assert.Throws<SheetEditException>(() => e.SetPartInfo("13D", null, null, null, null, "1k"), "a chip has no value");
        }

        [Test]
        public void AddGate_ChecksThePinsAgainstTheChipFile_AndRemoveGateTakesItAway()
        {
            SheetEditor e = Editor();
            SheetEditException ex = Assert.Throws<SheetEditException>(() => e.AddGate("11D", GateKind.Nor, new List<int> { 2, 3 }, 1, Confidence.Read))!;
            Assert.That(ex.Message, Does.Contain("already"));
            ex = Assert.Throws<SheetEditException>(() => e.AddGate("11D", GateKind.Nor, new List<int> { 2, 30 }, 31, Confidence.Read))!;
            Assert.That(ex.Message, Does.Contain("has no pin 30"));
            e.RemoveGate("11D/1");
            Assert.That(e.AddGate("11D", GateKind.Nor, new List<int> { 2, 3 }, 1, Confidence.Inferred), Is.EqualTo("11D/1"));
            Assert.Throws<SheetEditException>(() => e.RemoveGate("11D/77"));
        }

        [Test]
        public void SetNet_RenamesASignal_AndRefusesANameTaken()
        {
            SheetEditor e = Editor();
            // BA0 (high) is on 11C.4, 13D.15, 13C.14
            Assert.That(e.SetNet("BA0_1", "ADR0", null, "BA0", "renamed", null), Is.EqualTo("ADR0_1"));
            Net n = e.NetOf("13D.15")!;
            Assert.That(n.Name, Is.EqualTo("ADR0"));
            Assert.That(n.Printed, Is.EqualTo("BA0"));
            SheetEditException ex = Assert.Throws<SheetEditException>(() => e.SetNet("ADR0_1", "BA1", ActiveLevel.High, null, null, null))!;
            Assert.That(ex.Message, Does.Contain("already"));
            Assert.That(e.AddNet("SPARE", ActiveLevel.Low), Is.EqualTo("SPARE_0"));
            Assert.Throws<SheetEditException>(() => e.AddNet("SPARE", ActiveLevel.Low));
        }

        [Test]
        public void SetSheetInfo_SetsTheTitleBlock_AndKeepsWhatIsNotGiven()
        {
            SheetEditor e = Editor();
            e.SetSheetInfo(null, "New title", null, "RH 1977", null, null, null, null, null);
            Assert.That(e.Root.Sheet.Id, Is.EqualTo("2A50"));
            Assert.That(e.Root.Sheet.Title, Is.EqualTo("New title"));
            Assert.That(e.Root.Sheet.Drawn, Is.EqualTo("RH 1977"));
            Assert.That(e.Root.Sheet.Machine, Is.EqualTo("NORD-10"));
            Assert.Throws<SheetEditException>(() => e.SetSheetInfo(null, " ", null, null, null, null, null, null, null));
        }

        [Test]
        public void ABlockBuiltPieceByPiece_SavesAndReadsBack()
        {
            SheetEditor e = Editor();
            e.CreateBlock("DEC");
            Assert.Throws<SheetEditException>(() => e.CreateBlock("DEC"));
            e.AddInstance("U9", "DEC", 10, 20);
            e.EnterInstance("U9");
            Assert.Throws<SheetEditException>(() => e.AddInstance("U10", "DEC", null, null), "a block cannot hold itself");
            e.AddNet("SEL", ActiveLevel.Low);
            e.LeaveInstance();
            SheetEditException ex = Assert.Throws<SheetEditException>(() => e.SetBlockPort("DEC", "SEL", PortDirection.Input, Confidence.Read, new List<SignalRef> { new() { Net = "NOPE", Active = ActiveLevel.Low } }))!;
            Assert.That(ex.Message, Does.Contain("no signal NOPE inside block DEC"));
            e.SetBlockPort("DEC", "SEL_0", PortDirection.Input, Confidence.Read, new List<SignalRef> { new() { Net = "SEL", Active = ActiveLevel.Low } });
            // the top-level signal BA2 (low) is on P.34 and 13D.13
            Assert.Throws<SheetEditException>(() => e.ConnectPort("U9", "SEL_0", new List<SignalRef> { new() { Net = "BA2", Active = ActiveLevel.Low }, new() { Net = "BA1", Active = ActiveLevel.High } }));
            e.ConnectPort("U9", "SEL_0", new List<SignalRef> { new() { Net = "BA2", Active = ActiveLevel.Low } });
            e.SetNet("BA2_0", "BASEL", null, null, null, null);
            BlockInstance inst = Hierarchy.FindInstance(e.Netlist.Instances, "U9")!;
            Assert.That(inst.Connections![0].Nets[0].Net, Is.EqualTo("BASEL"), "a renamed signal keeps its port connection");

            // written out and read back: the reader accepts the structure
            Netlist back = NetlistReader.Parse(NetlistReader.Write(e.Root));
            BlockDefinition dec = Hierarchy.FindBlock(back, "DEC")!;
            Assert.That(dec.Ports[0].Name, Is.EqualTo("SEL_0"));
            Assert.That(Hierarchy.FindInstance(back.Instances, "U9")!.Position!.X, Is.EqualTo(10));

            e.RemoveBlockPort("DEC", "SEL_0");
            Assert.That(Hierarchy.FindInstance(e.Netlist.Instances, "U9")!.Connections, Is.Empty, "the connection to a removed port goes");
            e.RemoveInstance("U9");
            Assert.That(Hierarchy.FindInstance(e.Netlist.Instances, "U9"), Is.Null);
            Assert.That(Hierarchy.FindBlock(e.Root, "DEC"), Is.Not.Null, "the block stays");
        }
    }
}
