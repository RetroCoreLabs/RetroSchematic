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
    /// The layout section: a wire path fixed by hand is saved, read back and drawn exactly as it
    /// was, and one that a moved chip now crosses is routed afresh.
    /// </summary>
    [TestFixture]
    public class SavedLayoutTests
    {
        private string _scratch = "";

        [SetUp]
        public void SetUp() => _scratch = Path.Combine(TestContext.CurrentContext.WorkDirectory, "layout-" + TestContext.CurrentContext.Test.Name + ".json");

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_scratch))
                File.Delete(_scratch);
        }

        [Test]
        public void AFixedWire_IsSaved_ReadBack_AndDrawnUnchanged()
        {
            SheetSession s = Open();
            string net = AWiredSignal(s);
            string before = Shape(s.Drawing, net);
            s.Editor.ReadOnly = false;
            s.FixWire(net);
            Assert.That(s.Netlist.Layout!.Wires.Count, Is.EqualTo(1));
            s.SaveAs(_scratch);
            Assert.That(File.ReadAllText(_scratch), Does.Contain("\"layout\"").And.Contain("\"branches\""));

            SheetSession back = new(_scratch, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);
            Assert.That(back.Drawing.KeptWires, Does.Contain(net), "a clean stored path is kept");
            Assert.That(back.Drawing.Rerouted, Is.Empty);
            Assert.That(Shape(back.Drawing, net), Is.EqualTo(before), "drawn exactly as before");
            // reading it back did not change the stored path either
            Assert.That(back.Netlist.Layout!.Wires[0].Branches.Count, Is.EqualTo(s.Netlist.Layout.Wires[0].Branches.Count));
        }

        [Test]
        public void EveryWireOn2A50_FixedAndReadBack_IsKeptExactly()
        {
            string real = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
            SheetSession s = new(real, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);
            s.Editor.ReadOnly = false;
            List<string> nets = new(s.Drawing.WireBranches.Keys);
            Assert.That(nets.Count, Is.GreaterThan(10));
            Dictionary<string, string> before = new();
            for (int i = 0; i < nets.Count; i++)
                before[nets[i]] = Shape(s.Drawing, nets[i]);
            for (int i = 0; i < nets.Count; i++)
                s.FixWire(nets[i]);
            s.SaveAs(_scratch);
            SheetSession back = new(_scratch, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);
            Assert.That(back.Drawing.RerouteReasons, Is.Empty);
            Assert.That(back.Drawing.KeptWires.Count, Is.EqualTo(nets.Count));
            for (int i = 0; i < nets.Count; i++)
                Assert.That(Shape(back.Drawing, nets[i]), Is.EqualTo(before[nets[i]]), nets[i]);
        }

        [Test]
        public void AStoredPathThroughAChip_IsRoutedAfresh_AndCrossesNothing()
        {
            SheetSession s = Open();
            string net = AWiredSignal(s);
            s.Editor.ReadOnly = false;
            // a path straight through the sheet: from the first pin, right across everything, then
            // down and back to the second pin - it runs through chip frames
            Net n = NetOf(s, net);
            DrawPoint a = s.Drawing.Pins[n.Pins[0].At], b = s.Drawing.Pins[n.Pins[1].At];
            double far = SheetLayout.ToMm(s.Drawing.Width) - 2;
            List<List<Point>> bad = new()
            {
                new List<Point>
                {
                    Mm(a.X, a.Y), new Point { X = far, Y = SheetLayout.ToMm(a.Y) }, new Point { X = far, Y = SheetLayout.ToMm(b.Y) }, Mm(b.X, b.Y),
                },
            };
            s.Editor.SetWirePath(n.Name, bad, n.Active);
            Assert.That(s.Drawing.Rerouted, Does.Contain(net), "the path crosses chips, so it is dropped");
            Assert.That(s.Drawing.KeptWires, Does.Not.Contain(net));
            List<LayoutCleanTests.Box> frames = LayoutCleanTests.Frames(s.Drawing, partsOnly: false);
            List<DrawItem> wires = LayoutCleanTests.Wires(s.Drawing);
            for (int i = 0; i < wires.Count; i++)
            {
                for (int j = 0; j < frames.Count; j++)
                    Assert.That(frames[j].Crosses(wires[i]), Is.False, wires[i].Owner + " crosses " + frames[j].Owner);
            }
            // the stored path stays in the file: moving the chip back would let it be used again
            Assert.That(s.Editor.WirePathOf(n.Name, n.Active), Is.Not.Null);
        }

        [Test]
        public void SetWirePath_RefusesASlantedPiece_AndClearWorks()
        {
            SheetSession s = Open();
            s.Editor.ReadOnly = false;
            string net = AWiredSignal(s);
            Net n = NetOf(s, net);
            List<List<Point>> slanted = new() { new List<Point> { new Point { X = 1, Y = 1 }, new Point { X = 5, Y = 9 } } };
            Assert.Throws<SheetEditException>(() => s.Editor.SetWirePath(n.Name, slanted, n.Active));
            s.FixWire(net);
            s.Editor.ClearWirePath(n.Name, n.Active);
            Assert.That(s.Editor.WirePathOf(n.Name, n.Active), Is.Null);
            Assert.That(s.Netlist.Layout, Is.Null, "an empty layout section is not written");
            s.Editor.Undo();
            Assert.That(s.Editor.WirePathOf(n.Name, n.Active), Is.Not.Null, "undo brings the fixed path back");
        }

        private static SheetSession Open() => new(DemoSheetLayoutTests.Demo, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);

        private static Point Mm(double x, double y) => new() { X = SheetLayout.ToMm(x), Y = SheetLayout.ToMm(y) };

        private static string AWiredSignal(SheetSession s)
        {
            foreach (KeyValuePair<string, List<List<DrawPoint>>> kv in s.Drawing.WireBranches)
                return kv.Key;
            Assert.Fail("the demo sheet has no wired signal");
            return "";
        }

        /// <summary>
        /// A signal's laid wire as text: every branch's bend points, in order.
        /// </summary>
        private static string Shape(SchematicDrawing d, string key)
        {
            Assert.That(d.WireBranches.ContainsKey(key), Is.True, key + " has no wire");
            List<List<DrawPoint>> branches = d.WireBranches[key];
            System.Text.StringBuilder sb = new();
            for (int b = 0; b < branches.Count; b++)
            {
                for (int i = 0; i < branches[b].Count; i++)
                    sb.Append(branches[b][i].X).Append(',').Append(branches[b][i].Y).Append("  ");
                sb.Append('|');
            }
            return sb.ToString();
        }

        private static Net NetOf(SheetSession s, string key)
        {
            for (int i = 0; i < s.Netlist.Nets.Count; i++)
            {
                if (SheetLayout.NetKey(s.Netlist.Nets[i]) == key)
                    return s.Netlist.Nets[i];
            }
            Assert.Fail("no signal " + key);
            return null!;
        }
    }
}
