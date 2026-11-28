//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Mcp;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// Blocks (format version 2): version 1 files still load, a version 2 file is read and
    /// written back unchanged, bad files are refused with a reason, and above all making a
    /// block and dissolving it never change what is connected to what (the flattened netlist).
    /// </summary>
    [TestFixture]
    public class BlockTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        // the four groups of the demo design (docs/PLAN.md section 7)
        private static readonly string[] AddressDecode = { "13E", "13D", "11C", "9E", "11E" };
        private static readonly string[] ReadMux = { "3D", "3E", "5E", "5D", "13C", "5B", "7A", "7E", "5C" };
        private static readonly string[] Interrupts = { "3C", "1D", "3B", "5A", "1A", "1B", "1C", "3A" };
        private static readonly string[] Modem = { "13A", "15A", "17A", "19A", "15B" };

        private static SheetEditor Open()
            => new(NetlistReader.ReadFile(Sheet2A50), new ChipCatalog(ChipCatalog.Find(Sheet2A50)!)) { ReadOnly = false };

        [Test]
        public void TheCard_LoadsAsOneLevel_AndIsWrittenWithItsPages()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            Assert.That(n.Blocks, Is.Null);
            Assert.That(n.Instances, Is.Null);
            string written = NetlistReader.Write(n);
            // 2A50 has a pages list (one page), so it is written as version 3
            Assert.That(written, Does.Contain("\"formatVersion\": 3"));
            // a pin is written as "at" only; its part and number are worked out from it
            Assert.That(written, Does.Not.Contain("\"partRef\"").And.Not.Contain("\"pin\":"));
            // flattening a design with no blocks changes nothing
            Assert.That(Hierarchy.Flatten(n).Nets.Count, Is.EqualTo(n.Nets.Count));
        }

        [Test]
        public void MakeBlock_ThenDissolve_LeavesTheFlattenedNetlistExactlyAsItWas()
        {
            SheetEditor e = Open();
            string before = Hierarchy.Connections(e.Root);
            int parts = e.Netlist.Parts.Count;

            string u1 = e.MakeBlock("address-decode", null, new List<string>(AddressDecode), null);
            Assert.That(u1, Is.EqualTo("U1"));
            Assert.That(e.Netlist.Parts.Count, Is.EqualTo(parts - AddressDecode.Length));
            Assert.That(e.Root.Blocks![0].Ports.Count, Is.GreaterThan(0), "signals cross the edge, so there are ports");
            Assert.That(Hierarchy.Connections(e.Root), Is.EqualTo(before), "after make block");
            // the flattened names carry the instance path
            Netlist flat = Hierarchy.Flatten(e.Root);
            Assert.That(flat.Parts.Exists(p => p.Ref == "U1/13E"), Is.True);

            // a file with blocks is written (version 3, since 2A50 has pages), read back and
            // connects the same
            string json = NetlistReader.Write(e.Root);
            Assert.That(json, Does.Contain("\"formatVersion\": 3"));
            Netlist back = NetlistReader.Parse(json);
            Assert.That(Hierarchy.Connections(back), Is.EqualTo(before), "after a write and read");
            Assert.That(NetlistReader.Write(back), Is.EqualTo(json), "read and written back unchanged");

            e.DissolveInstance("U1");
            Assert.That(e.Root.Blocks, Is.Null, "the block goes when its last instance does");
            Assert.That(e.Netlist.Parts.Count, Is.EqualTo(parts));
            Assert.That(Hierarchy.Connections(e.Root), Is.EqualTo(before), "after dissolve");

            // undo brings the block back, undo again the single sheet
            e.Undo();
            Assert.That(e.Root.Instances![0].Name, Is.EqualTo("U1"));
            Assert.That(Hierarchy.Connections(e.Root), Is.EqualTo(before));
            e.Undo();
            Assert.That(e.Root.Blocks, Is.Null);
            Assert.That(Hierarchy.Connections(e.Root), Is.EqualTo(before));
        }

        [Test]
        public void AllFourDemoBlocks_AndABlockInsideABlock_KeepTheConnections()
        {
            SheetEditor e = Open();
            string before = Hierarchy.Connections(e.Root);
            e.MakeBlock("address-decode", "U1", new List<string>(AddressDecode), null);
            e.MakeBlock("read-multiplexers", "U2", new List<string>(ReadMux), null);
            e.MakeBlock("interrupt-logic", "U3", new List<string>(Interrupts), null);
            e.MakeBlock("modem-interface", "U4", new List<string>(Modem), null);
            Assert.That(Hierarchy.Connections(e.Root), Is.EqualTo(before), "four blocks");

            // a block holding two instances: nesting
            e.MakeBlock("buffer-core", "U5", new List<string> { "inst:U2", "inst:U3" }, null);
            Assert.That(Hierarchy.Connections(e.Root), Is.EqualTo(before), "a block inside a block");
            Assert.That(Hierarchy.Flatten(e.Root).Parts.Exists(p => p.Ref == "U5/U2/3D"), Is.True);

            // looking inside and coming back; an edit inside lands in the block
            e.EnterInstance("U5");
            Assert.That(e.LevelPath, Is.EqualTo(new[] { "U5" }));
            Assert.That(e.Netlist.Instances!.Count, Is.EqualTo(2));
            e.EnterInstance("U2");
            Assert.That(e.Netlist.Parts.Exists(p => p.Ref == "3D"), Is.True);
            e.MovePart("3D", 50, 50);
            e.LeaveInstance();
            e.LeaveInstance();
            Assert.That(e.LevelPath.Count, Is.EqualTo(0));
            Assert.That(Hierarchy.FindBlock(e.Root, "read-multiplexers")!.Parts.Find(p => p.Ref == "3D")!.Position!.X, Is.EqualTo(50));

            // dissolve everything again, outermost first
            e.DissolveInstance("U5");
            e.DissolveInstance("U1");
            e.DissolveInstance("U2");
            e.DissolveInstance("U3");
            e.DissolveInstance("U4");
            Assert.That(e.Root.Blocks, Is.Null);
            Assert.That(Hierarchy.Connections(e.Root), Is.EqualTo(before), "all dissolved");
        }

        [Test]
        public void PlannedPorts_GroupNumberedSignals_AndNamesCanBeChanged()
        {
            SheetEditor e = Open();
            List<string> members = new(ReadMux);
            List<PlannedPort> ports = e.PlanBlock(members);
            PlannedPort? bus = ports.Find(p => p.Signals.Count > 1);
            Assert.That(bus, Is.Not.Null, "numbered signals (BD.., IDATA..) become one bus port");
            for (int i = 1; i < bus!.Signals.Count; i++)
                Assert.That(bus.Signals[i].Net, Is.Not.EqualTo(bus.Signals[i - 1].Net));
            // automatic names (N_<chip>_<pin>) carry a pin number, not a bit: never a bus
            for (int i = 0; i < ports.Count; i++)
                Assert.That(ports[i].Signals.Count == 1 || !ports[i].Signals[0].Net.StartsWith("N_", System.StringComparison.Ordinal), Is.True, ports[i].ToString());
            ports[0].Name = "RENAMED";
            string before = Hierarchy.Connections(e.Root);
            e.MakeBlock("read-multiplexers", null, members, ports);
            Assert.That(e.Root.Blocks![0].Ports[0].Name, Is.EqualTo("RENAMED"));
            Assert.That(e.Root.Instances![0].Connections![0].Port, Is.EqualTo("RENAMED"));
            Assert.That(Hierarchy.Connections(e.Root), Is.EqualTo(before));
        }

        [Test]
        public void BadChanges_AreRefused_AndChangeNothing()
        {
            SheetEditor e = Open();
            Assert.Throws<SheetEditException>(() => e.MakeBlock("", null, new List<string>(Modem), null));
            Assert.Throws<SheetEditException>(() => e.MakeBlock("b", null, new List<string>(), null));
            Assert.Throws<SheetEditException>(() => e.MakeBlock("b", null, new List<string> { "NOPE" }, null));
            Assert.Throws<SheetEditException>(() => e.DissolveInstance("U9"));
            Assert.Throws<SheetEditException>(() => e.EnterInstance("U9"));
            Assert.That(e.CanUndo, Is.False, "refusals leave no undo step");
            e.MakeBlock("b", null, new List<string>(Modem), null);
            Assert.Throws<SheetEditException>(() => e.MakeBlock("b", null, new List<string>(Interrupts), null), "name taken");
            Assert.Throws<SheetEditException>(() => e.SetPortPosition("b", "NOPE", PortSide.Top, 0));
            e.ReadOnly = true;
            Assert.Throws<SheetEditException>(() => e.DissolveInstance("U1"));
            Assert.DoesNotThrow(() => e.EnterInstance("U1"), "looking inside is allowed read-only");
        }

        [Test]
        public void APort_MovesAlongItsEdge_AndToAnotherEdge()
        {
            SheetEditor e = Open();
            e.MakeBlock("m", null, new List<string>(Modem), null);
            BlockDefinition b = e.Root.Blocks![0];
            // the ports on the left edge, in drawing order
            List<BlockPort> left = b.Ports.FindAll(p => SheetLayout.SideOf(p) == PortSide.Left);
            Assert.That(left.Count, Is.GreaterThan(2));
            string first = left[0].Name, second = left[1].Name;
            Assert.Throws<SheetEditException>(() => e.MovePortAlong("m", first, -1), "already first");
            e.MovePortAlong("m", first, 1);
            Assert.That(left[0].Order, Is.EqualTo(1));
            Assert.That(left[1].Order, Is.EqualTo(0), second + " is now first");
            SchematicDrawing d = SheetLayout.Build(e.Netlist, SymbolStyle.NorskData, new LayoutOptions { Catalog = e.Catalog });
            Assert.That(PinY(d, "port:U1:" + second), Is.LessThan(PinY(d, "port:U1:" + first)), "drawn in the new order");
            e.SetPortPosition("m", first, PortSide.Right, null);
            Assert.That(SheetLayout.SideOf(left[0]), Is.EqualTo(PortSide.Right));
            e.Undo();
            e.Undo();
            Assert.That(left.Count, Is.GreaterThan(0));
            Assert.That(e.Root.Blocks![0].Ports.Find(p => p.Name == first)!.Order, Is.Null, "undo puts the port order back");
        }

        private static double PinY(SchematicDrawing d, string owner)
        {
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Owner == owner && d.Items[i].Kind == DrawKind.Line)
                    return d.Items[i].Y1;
            }
            return double.NaN;
        }

        [Test]
        public void TheReader_RefusesBrokenBlockFiles_WithAReason()
        {
            SheetEditor e = Open();
            e.MakeBlock("b", null, new List<string>(Modem), null);
            string good = NetlistReader.Write(e.Root);
            Assert.That(() => NetlistReader.Parse(good.Replace("\"formatVersion\": 3", "\"formatVersion\": 1")),
                Throws.TypeOf<NetlistException>().With.Message.Contains("version 2"));
            Assert.That(() => NetlistReader.Parse(good.Replace("\"block\": \"b\"", "\"block\": \"x\"")),
                Throws.TypeOf<NetlistException>().With.Message.Contains("not defined"));
        }

        [Test]
        public void AnInstance_IsDrawnWithItsPorts_AndArrowsFollowTheSetting()
        {
            SheetEditor e = Open();
            e.MakeBlock("modem-interface", null, new List<string>(Modem), null);
            e.SetPortPosition("modem-interface", e.Root.Blocks![0].Ports[0].Name, PortSide.Top, 0);
            LayoutOptions on = new() { Catalog = e.Catalog };
            SchematicDrawing d = SheetLayout.Build(e.Netlist, SymbolStyle.NorskData, on);
            Assert.That(d.InstanceFrames.ContainsKey("U1"), Is.True);
            int arrows = 0, ports = 0;
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Role == DrawRole.PortArrow)
                    arrows++;
                if (d.Items[i].Owner.StartsWith("port:U1:", System.StringComparison.Ordinal) && d.Items[i].Kind == DrawKind.Line)
                    ports++;
            }
            Assert.That(ports, Is.EqualTo(e.Root.Blocks[0].Ports.Count), "one pin line per port");
            Assert.That(arrows, Is.GreaterThanOrEqualTo(ports), "every port has an arrow (two for inout)");
            Assert.That(SvgRenderer.Write(d), Does.Contain("id=\"inst-U1\""));

            LayoutOptions off = new() { Catalog = e.Catalog, ShowPortArrows = false };
            SchematicDrawing d2 = SheetLayout.Build(e.Netlist, SymbolStyle.NorskData, off);
            for (int i = 0; i < d2.Items.Count; i++)
                Assert.That(d2.Items[i].Role, Is.Not.EqualTo(DrawRole.PortArrow), "arrows off");
            // drawing never changes the sheet
            Assert.That(e.Netlist.Nets.Exists(n => n.Pins.Exists(p => p.At.Contains(':'))), Is.False);
        }
    
        [Test]
        public void TheBlocksDemo_ConnectsExactlyWhat2A50Connects()
        {
            string demo = TestCards.Blocks2A50();
            Netlist blocks = NetlistReader.ReadFile(demo);
            Assert.That(blocks.Instances!.Count, Is.EqualTo(4));
            Assert.That(Hierarchy.Connections(blocks), Is.EqualTo(Hierarchy.Connections(NetlistReader.ReadFile(Sheet2A50))));
        }

        [Test]
        public void ATrace_FollowsASignalThroughThePorts_IntoTheBlocks()
        {
            string demo = TestCards.Blocks2A50();
            SheetSession s = new(demo, SymbolStyle.NorskData, ChipCatalog.Find(demo));
            // DEQL is used by three of the four blocks
            s.Trace("DEQL_1");
            List<string> levels = s.TracedLevels();
            Assert.That(levels[0], Is.EqualTo("DEQL"));
            Assert.That(levels, Does.Contain("U1/DEQL").And.Contain("U2/DEQL").And.Contain("U3/DEQL"));
            // the instance port pins light with the signal
            int lit = 0;
            for (int i = 0; i < s.Drawing.Items.Count; i++)
            {
                DrawItem it = s.Drawing.Items[i];
                if (it.Owner.StartsWith("port:U1:", System.StringComparison.Ordinal) && s.TraceOf(it) == TraceRole.Signal)
                    lit++;
            }
            Assert.That(lit, Is.GreaterThan(0));

            // inside U2 the same signal reaches back up to the top
            s.Editor.EnterInstance("U2");
            Net inner = NetlistReader.FindNet(s.Editor.Netlist.Nets, new SignalRef { Net = "DEQL" })!;
            Assert.That(Hierarchy.Reach(s.Editor.Root, s.Editor.LevelPath, inner), Does.Contain("DEQL").And.Contain("U1/DEQL"));
        }

        [Test]
        public void CtrlClick_AndADraggedBox_PickSeveralThings()
        {
            string demo = TestCards.Blocks2A50();
            SheetSession s = new(demo, SymbolStyle.NorskData, ChipCatalog.Find(demo));
            EditTool tool = new(s);
            PartFrame a = s.Drawing.Frames["11A"], b = s.Drawing.Frames["13B"];
            PartFrame u4 = s.Drawing.InstanceFrames["U4"];
            tool.PointerDown(a.X + (a.Width / 2), a.Y + (a.Height / 2), true);
            tool.PointerDown(b.X + (b.Width / 2), b.Y + (b.Height / 2), true);
            tool.PointerDown(u4.X + (u4.Width / 2), u4.Y + (u4.Height / 2), true);
            Assert.That(s.Picked, Is.EqualTo(new[] { "part:11A", "part:13B", "inst:U4" }));
            Assert.That(s.PickedMembers(), Is.EqualTo(new[] { "11A", "13B", "inst:U4" }));
            tool.PointerDown(b.X + (b.Width / 2), b.Y + (b.Height / 2), true);
            Assert.That(s.Picked.Count, Is.EqualTo(2), "a second Ctrl-click takes it out again");

            // a box dragged on empty paper round 13B picks just that chip
            tool.PointerDown(b.X - 3, b.Y - 3);
            Assert.That(tool.Gesture, Is.EqualTo(EditGesture.Band));
            Assert.That(s.Picked.Count, Is.EqualTo(0), "a plain click on paper empties the group");
            tool.PointerUp(b.X + b.Width + 3, b.Y + b.Height + 3);
            Assert.That(s.Picked, Is.EqualTo(new[] { "part:13B" }));
        }

        [Test]
        public void DroppingPickedParts_OnAnInstance_MovesThemInside_InOneUndoStep()
        {
            string demo = TestCards.Blocks2A50();
            SheetSession s = new(demo, SymbolStyle.NorskData, ChipCatalog.Find(demo));
            s.Editor.ReadOnly = false;
            string before = Hierarchy.Connections(s.Editor.Root);
            BlockDefinition modem = Hierarchy.FindBlock(s.Editor.Root, "modem-interface")!;
            string kept = modem.Ports[0].Name;
            s.Editor.SetPortPosition("modem-interface", kept, PortSide.Bottom, 0);
            EditTool tool = new(s);

            // move the U4 box
            PartFrame u4 = s.Drawing.InstanceFrames["U4"];
            tool.PointerDown(u4.X + 20, u4.Y + 20);
            Assert.That(tool.Gesture, Is.EqualTo(EditGesture.MoveInstance));
            tool.PointerUp(u4.X + 80, u4.Y + 20);
            Assert.That(tool.Message, Does.StartWith("Moved U4"));

            // drag chip 13B (with 17B picked too) onto U4
            PartFrame b = s.Drawing.Frames["13B"], c = s.Drawing.Frames["17B"];
            tool.PointerDown(c.X + (c.Width / 2), c.Y + (c.Height / 2), true);
            tool.PointerDown(b.X + (b.Width / 2), b.Y + (b.Height / 2), true);
            u4 = s.Drawing.InstanceFrames["U4"];
            tool.PointerDown(b.X + (b.Width / 2), b.Y + (b.Height / 2));
            Assert.That(tool.Gesture, Is.EqualTo(EditGesture.Move));
            tool.PointerUp(u4.X + 10, u4.Y + 10);
            Assert.That(tool.Message, Does.StartWith("Moved 17B, 13B into U4"));
            modem = Hierarchy.FindBlock(s.Editor.Root, "modem-interface")!;
            Assert.That(modem.Parts.Exists(p => p.Ref == "13B") && modem.Parts.Exists(p => p.Ref == "17B"), Is.True);
            Assert.That(s.Editor.Netlist.Parts.Exists(p => p.Ref == "13B"), Is.False);
            Assert.That(Hierarchy.Connections(s.Editor.Root), Is.EqualTo(before), "moving parts in keeps every connection");
            BlockPort? same = modem.Ports.Find(p => p.Name == kept);
            Assert.That(same, Is.Not.Null, "a port that carries the same signals keeps its name");
            Assert.That(same!.Side, Is.EqualTo(PortSide.Bottom), "and its edge");

            // one undo takes the whole move back
            s.Editor.Undo();
            Assert.That(s.Editor.Netlist.Parts.Exists(p => p.Ref == "13B"), Is.True);
            Assert.That(Hierarchy.FindBlock(s.Editor.Root, "modem-interface")!.Parts.Exists(p => p.Ref == "13B"), Is.False);
            Assert.That(Hierarchy.Connections(s.Editor.Root), Is.EqualTo(before));
            Assert.That(s.Editor.CanUndo, Is.True, "the instance move before it is still there to undo");
            s.Editor.Undo();
            Assert.That(s.Editor.CanUndo, Is.True, "and below it the port edge change");
        }

        [Test]
        public void OpeningAnInstance_ShowsTheBreadcrumb_AndUpComesBack()
        {
            string demo = TestCards.Blocks2A50();
            SheetSession s = new(demo, SymbolStyle.NorskData, ChipCatalog.Find(demo));
            s.TogglePick("part:11A");
            s.OpenInstance("U2");
            Assert.That(s.LevelNames(), Is.EqualTo(new[] { "Top", "U2 (read-multiplexers)" }));
            Assert.That(s.Picked.Count, Is.EqualTo(0), "the group picked named things on the level left");
            Assert.That(s.Drawing.Frames.ContainsKey("3D"), Is.True, "the drawing shows the inside");
            s.LeaveInstance();
            Assert.That(s.LevelNames(), Is.EqualTo(new[] { "Top" }));
            Assert.That(s.Drawing.InstanceFrames.ContainsKey("U2"), Is.True);
        }

        [Test]
        public void Mcp_MakesListsEntersAndDissolvesABlock()
        {
            SheetTools t = new(TestContext.CurrentContext.TestDirectory);
            t.OpenSheet(Sheet2A50, edit: true);
            string before = Hierarchy.Connections(t.Editor!.Root);
            Assert.That(t.MakeBlock("modem-interface", "13A 15A 17A 19A 15B", "M1"), Does.StartWith("Made block modem-interface as instance M1"));
            Assert.That(t.ListBlocks(), Does.Contain("Block modem-interface: 5 parts"));
            Assert.That(t.ListInstances(), Does.Contain("M1 (modem-interface)"));
            Assert.That(t.EnterBlock("M1"), Does.Contain("Inside M1"));
            Assert.That(t.LeaveBlock(), Is.EqualTo("At the top level."));
            string port = t.Editor.Root.Blocks![0].Ports[0].Name;
            Assert.That(t.SetPortPosition("modem-interface", port, "top", 0), Does.Contain("top edge"));
            Assert.Throws<ModelContextProtocol.McpException>(() => t.SetPortPosition("modem-interface", port, "middle"));
            Assert.Throws<ModelContextProtocol.McpException>(() => t.DissolveBlock("NOPE"));
            Assert.That(t.DissolveBlock("M1"), Does.StartWith("Dissolved M1"));
            Assert.That(Hierarchy.Connections(t.Editor.Root), Is.EqualTo(before));
        }
}
}
