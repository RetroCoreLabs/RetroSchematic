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
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

        // the four groups of the demo design (docs/PLAN.md section 7)
        private static readonly string[] AddressDecode = { "13E", "13D", "11C", "9E", "11E" };
        private static readonly string[] ReadMux = { "3D", "3E", "5E", "5D", "13C", "5B", "7A", "7E", "5C" };
        private static readonly string[] Interrupts = { "3C", "1D", "3B", "5A", "1A", "1B", "1C", "3A" };
        private static readonly string[] Modem = { "13A", "15A", "17A", "19A", "15B" };

        private static SheetEditor Open()
            => new(NetlistReader.ReadFile(Sheet2A50), new ChipCatalog(ChipCatalog.Find(Sheet2A50)!)) { ReadOnly = false };

        [Test]
        public void AVersion1File_LoadsAsOneLevel_AndIsWrittenAsVersion1()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            Assert.That(n.Blocks, Is.Null);
            Assert.That(n.Instances, Is.Null);
            Assert.That(NetlistReader.Write(n), Does.Contain("\"formatVersion\": 1"));
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

            // a version 2 file is written, read back and connects the same
            string json = NetlistReader.Write(e.Root);
            Assert.That(json, Does.Contain("\"formatVersion\": 2"));
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
        public void TheReader_RefusesBrokenBlockFiles_WithAReason()
        {
            SheetEditor e = Open();
            e.MakeBlock("b", null, new List<string>(Modem), null);
            string good = NetlistReader.Write(e.Root);
            Assert.That(() => NetlistReader.Parse(good.Replace("\"formatVersion\": 2", "\"formatVersion\": 1")),
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
