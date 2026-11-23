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
    /// Tracing as the main way to read a sheet: a plain click with the Select tool on anything
    /// of a signal traces it (also read-only), the pulse rises and falls once a second, the bus
    /// a signal runs on is shown, block ports it goes through are marked and can be followed
    /// inside, and Tab steps through every place.
    /// </summary>
    [TestFixture]
    public class TracePulseTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
        private static readonly string Blocks = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050-blocks.rsc");

        [Test]
        public void ThePulse_RisesAndFallsOnceASecond()
        {
            Assert.That(SheetSession.PulseLevel(0), Is.EqualTo(0).Within(1e-9));
            Assert.That(SheetSession.PulseLevel(0.5), Is.EqualTo(1).Within(1e-9));
            Assert.That(SheetSession.PulseLevel(1), Is.EqualTo(0).Within(1e-9));
            Assert.That(SheetSession.PulseLevel(3.25), Is.EqualTo(SheetSession.PulseLevel(0.25)).Within(1e-9), "one second period");
            // smooth: no jump bigger than a small step between frames 40 ms apart
            for (double t = 0; t < 1; t += 0.04)
                Assert.That(System.Math.Abs(SheetSession.PulseLevel(t + 0.04) - SheetSession.PulseLevel(t)), Is.LessThan(0.13));
        }

        [Test]
        public void APlainClick_InReadOnlyMode_TracesWhatWasClicked()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, ChipCatalog.Find(Sheet2A50));
            Assert.That(s.Editor.ReadOnly, Is.True);
            EditTool tool = new(s);
            Net n = s.Netlist.Nets.Find(x => x.Pins.Count >= 2 && x.Pins.TrueForAll(p => p.PartRef != "P" && p.PartRef != "F"))!;
            string pin = n.Pins[0].At;
            DrawPoint at = s.Drawing.Pins[pin];
            tool.PointerDown(at.X, at.Y);
            Assert.That(s.Traced, Is.EqualTo(SheetLayout.NetKey(n)), "a click on a pin traces its signal");
            Assert.That(tool.Message, Does.StartWith("Tracing " + s.NameOf(n)));
            PartFrame f = s.Drawing.Frames["13B"];
            tool.PointerDown(f.X + (f.Width / 2), f.Y + (f.Height / 2));
            Assert.That(s.Traced, Is.Empty, "a click on a chip body clears the trace");
            Assert.That(s.SignalAt("net:" + SheetLayout.NetKey(n)), Is.EqualTo(SheetLayout.NetKey(n)), "a wire or name tag");
            Assert.That(s.SignalAt("part:13B"), Is.Empty);
        }

        [Test]
        public void TracingABusMember_ShowsItsBusLine()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, ChipCatalog.Find(Sheet2A50));
            s.Trace("BD3_0");
            int shown = 0;
            for (int i = 0; i < s.Drawing.Items.Count; i++)
            {
                DrawItem it = s.Drawing.Items[i];
                if (it.Owner == "bus:BD_0")
                {
                    Assert.That(s.TraceOf(it), Is.EqualTo(TraceRole.Joined));
                    shown++;
                }
                if (it.Owner == "bus:BD_1")
                    Assert.That(s.TraceOf(it), Is.EqualTo(TraceRole.Dimmed), "the other bus stays dim");
            }
            Assert.That(shown, Is.GreaterThan(0));
        }

        [Test]
        public void APort_IsMarked_ClickedPortsTrace_AndADoubleClickFollowsInside()
        {
            SheetSession s = new(Blocks, SymbolStyle.NorskData, ChipCatalog.Find(Blocks));
            // a click on an instance's port end traces the signal it is connected to
            Assert.That(s.SignalAt("pin:U1:DEQL.0"), Is.EqualTo("DEQL_1"));
            Assert.That(s.SignalAt("port:U1:DEQL"), Is.EqualTo("DEQL_1"));
            s.TraceOwner("port:U2:DEQL");
            List<TraceMark> marks = s.TracedPortMarks();
            List<string> where = new();
            for (int i = 0; i < marks.Count; i++)
                where.Add(marks[i].Instance + ":" + marks[i].Port);
            Assert.That(where, Does.Contain("U1:DEQL").And.Contain("U2:DEQL").And.Contain("U3:DEQL"), "a mark on every block it enters");
            DrawPoint edge = marks[0].At;
            PartFrame box = s.Drawing.InstanceFrames[marks[0].Instance];
            Assert.That(edge.X == box.X || edge.X == box.X + box.Width || edge.Y == box.Y || edge.Y == box.Y + box.Height, Is.True, "the mark is on the box edge");

            Assert.That(s.FollowAt("part:13B"), Is.False);
            Assert.That(s.FollowAt("port:U2:DEQL"), Is.True);
            Assert.That(s.Editor.LevelPath, Is.EqualTo(new[] { "U2" }));
            Assert.That(s.Traced, Is.EqualTo("DEQL_1"), "the same signal is traced inside");
            Assert.That(s.TracedPins().Count, Is.GreaterThan(0));
        }

        [Test]
        public void Tab_StepsThroughEveryPlace_AndWrapsRound()
        {
            SheetSession s = new(Blocks, SymbolStyle.NorskData, ChipCatalog.Find(Blocks));
            Assert.That(s.NextTracePlace(1, out _, out _), Is.False, "nothing traced");
            s.Trace("DEQL_1");
            int places = s.TracedPins().Count + s.TracedPortMarks().Count;
            List<string> seen = new();
            for (int i = 0; i < places; i++)
            {
                Assert.That(s.NextTracePlace(1, out _, out string what), Is.True);
                seen.Add(what);
            }
            s.NextTracePlace(1, out _, out string again);
            Assert.That(again, Is.EqualTo(seen[0]), "wraps round to the first");
            s.NextTracePlace(-1, out _, out string back);
            Assert.That(back, Is.EqualTo(seen[^1]), "Shift-Tab goes back");
            Assert.That(seen, Does.Contain("U2:DEQL"), "the block entries are places too");
        }
    }
}
