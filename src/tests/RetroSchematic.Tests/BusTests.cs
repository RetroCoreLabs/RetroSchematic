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
    /// Virtual buses: drawing only (the nets never change), name patterns, every member pin
    /// either joins the bus with its name shown or is reported, a bus never crosses a chip, and
    /// the editor and MCP operations with undo.
    /// </summary>
    [TestFixture]
    public class BusTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [Test]
        public void TheNets_AreIdentical_WithAndWithoutBuses()
        {
            Netlist with = NetlistReader.ReadFile(Sheet2A50);
            Assert.That(with.Layout?.Buses, Is.Not.Null.And.Count.EqualTo(2), "2A50 carries the BD_0 and BD_1 buses");
            Netlist without = NetlistReader.ReadFile(Sheet2A50);
            without.Layout!.Buses = null;
            Assert.That(NetsText(with), Is.EqualTo(NetsText(without)));

            // and deleting a bus through the editor leaves the nets exactly as they were
            SheetEditor e = new(NetlistReader.ReadFile(Sheet2A50), null) { ReadOnly = false };
            string before = NetsText(e.Netlist);
            e.DeleteBus("BD_0");
            e.DeleteBus("BD_1");
            Assert.That(NetsText(e.Netlist), Is.EqualTo(before));
        }

        [Test]
        public void Patterns_MatchTheRightSignals()
        {
            SheetEditor e = new(NetlistReader.ReadFile(Sheet2A50), null);
            Assert.That(e.MatchSignals("BD*_0").Count, Is.EqualTo(16));
            Assert.That(e.MatchSignals("IDATA0-7").Count, Is.EqualTo(8));
            // BD10_1 to BD14_1 do not exist on 2A50: a range takes the ones that do
            Assert.That(e.MatchSignals("BD0-15_1").Count, Is.EqualTo(11));
            Assert.That(e.MatchSignals("BD15_0, BD15_1").Count, Is.EqualTo(2));
            Assert.That(e.MatchSignals("NOSUCH*"), Is.Empty);
        }

        [Test]
        public void On2A50_EveryMemberPin_JoinsTheBusWithItsName_OrIsReported()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);
            SchematicDrawing d = s.Drawing;
            Assert.That(d.BusPaths.ContainsKey("BD_0") && d.BusPaths.ContainsKey("BD_1"), Is.True, string.Join("\n", d.BusProblems));
            List<BusLine> buses = s.Netlist.Layout!.Buses!;
            int joined = 0, connectorsJoined = 0;
            for (int b = 0; b < buses.Count; b++)
            {
                for (int m = 0; m < buses[b].Members.Count; m++)
                {
                    Net n = SheetLayout.FindMember(s.Netlist, buses[b].Members[m])!;
                    string owner = "net:" + SheetLayout.NetKey(n);
                    int names = 0, labels = 0;
                    for (int i = 0; i < d.Items.Count; i++)
                    {
                        DrawItem it = d.Items[i];
                        if (it.Owner != owner)
                            continue;
                        if (it.Role == DrawRole.SignalName)
                        {
                            names++;
                            // its level shows in the default name style (/NAME for active low)
                            Assert.That(it.Text, Is.EqualTo(SignalNames.Format(n.Name, n.Active, SignalNames.Default, out string sub)), owner + " shows its level");
                            Assert.That(it.Subscript, Is.EqualTo(sub));
                        }
                        if (it.Role == DrawRole.NetLabel)
                            labels++;
                    }
                    int drawnPins = 0, reported = 0, connectorPins = 0;
                    for (int j = 0; j < n.Pins.Count; j++)
                    {
                        if (!d.Pins.ContainsKey(n.Pins[j].At))
                            continue;
                        // a connector pin joins the bus like a chip pin, with its own named entry
                        if (ConnectorPins.IsConnector(n.Pins[j].PartRef))
                            connectorPins++;
                        drawnPins++;
                        if (d.BusProblems.Exists(p => p.Contains("pin " + n.Pins[j].At + " ", System.StringComparison.Ordinal)))
                            reported++;
                    }
                    // a pin that joined has a name at its entry; one that did not keeps its label
                    // (diamond and name) and is listed as a problem
                    Assert.That(names, Is.EqualTo(drawnPins), owner);
                    Assert.That(labels, Is.EqualTo(reported), owner);
                    joined += drawnPins - reported;
                    connectorsJoined += connectorPins;
                }
            }
            Assert.That(joined, Is.GreaterThan(40), "most BD pins join their bus");
            Assert.That(connectorsJoined, Is.GreaterThan(0), "backplane BD pins are among the members");
        }

        [Test]
        public void ABusPathThroughAChip_IsRoutedAfresh_AndCrossesNoChip()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);
            s.Editor.ReadOnly = false;
            PartFrame f = s.Drawing.Frames["7B"];
            double y = SheetLayout.ToMm(f.Y + (f.Height / 2));
            // level through the middle of 7B, from well left of it to well right of it
            s.Editor.SetBusPath("BD_0", new List<Point> { new() { X = 122, Y = y }, new() { X = 175, Y = y } });
            SchematicDrawing d = s.Drawing;
            Assert.That(d.BusProblems.Exists(p => p.StartsWith("BD_0: the stored path is not used", System.StringComparison.Ordinal)), Is.True);
            List<LayoutCleanTests.Box> frames = LayoutCleanTests.Frames(d, partsOnly: false);
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Role != DrawRole.Bus || d.Items[i].Kind != DrawKind.Line)
                    continue;
                for (int j = 0; j < frames.Count; j++)
                    Assert.That(frames[j].Crosses(d.Items[i]), Is.False, "bus crosses " + frames[j].Owner);
            }
        }

        [Test]
        public void Editor_RefusesASignalOnTwoBuses_AndUndoWorks()
        {
            SheetEditor e = new(NetlistReader.ReadFile(Sheet2A50), null);
            Assert.Throws<SheetEditException>(() => e.CreateBus("X", new List<string> { "IDATA0-7" }, null), "read-only refuses");
            e.ReadOnly = false;
            Assert.Throws<SheetEditException>(() => e.CreateBus("X", new List<string> { "BD3_0" }, null));
            Assert.Throws<SheetEditException>(() => e.CreateBus("X", new List<string> { "IDATA0-7" },
                new List<Point> { new() { X = 1, Y = 1 }, new() { X = 5, Y = 9 } }), "slanted path");
            e.CreateBus("IDATA", new List<string> { "IDATA0-7" }, null);
            Assert.That(e.FindBus("IDATA")!.Members.Count, Is.EqualTo(8));
            e.RemoveBusMember("IDATA", "IDATA7_1");
            Assert.That(e.FindBus("IDATA")!.Members.Count, Is.EqualTo(7));
            e.AddBusMember("IDATA", "IDATA7_1");
            e.Undo();
            e.Undo();
            e.Undo();
            Assert.That(e.FindBus("IDATA"), Is.Null);
        }

        [Test]
        public void Mcp_CreatesListsAndDeletesABus()
        {
            SheetTools t = new(TestContext.CurrentContext.TestDirectory);
            t.OpenSheet(Sheet2A50, edit: true);
            Assert.That(t.CreateBus("DAT", "DAT0-7", ""), Does.Contain("8 signals").And.Contain("no path is set"));
            Assert.That(t.ListBuses(), Does.Contain("DAT path: members: DAT0_1"));
            Assert.That(t.DeleteBus("DAT"), Does.StartWith("Deleted"));
        }

        private static string NetsText(Netlist n)
        {
            Netlist copy = NetlistReader.Parse(NetlistReader.Write(n));
            copy.Layout = null;
            return NetlistReader.Write(copy);
        }
    }
}
