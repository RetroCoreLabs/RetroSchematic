//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using NUnit.Framework;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The trace tool: one click shows a whole signal, in read-only mode too.
    /// </summary>
    [TestFixture]
    public class TraceTests
    {
        private static SheetSession Open() => new(DemoSheetLayoutTests.Demo, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);

        [Test]
        public void TracingASignal_LightsItsItems_OutlinesItsChips_DimsTheRest()
        {
            SheetSession s = Open();
            Assert.That(s.Editor.ReadOnly, Is.True, "the trace works without edit mode");
            s.Trace("IRQ");
            Assert.That(s.Traced, Is.EqualTo("IRQ"));
            List<string> pins = s.TracedPins();
            Assert.That(pins, Does.Contain("5A.3").And.Contain("5A.6"));
            Assert.That(s.PinPoint("5A.3", out double px, out double py), Is.True, "a drawn pin has a place to centre on");
            Assert.That(s.Drawing.HitTest(px, py), Is.EqualTo("pin:5A.3").Or.EqualTo("net:IRQ"));
            Assert.That(s.PinPoint("5A.99", out _, out _), Is.False);
            int signal = 0, joined = 0, dimmed = 0;
            for (int i = 0; i < s.Drawing.Items.Count; i++)
            {
                DrawItem it = s.Drawing.Items[i];
                TraceRole r = s.TraceOf(it);
                if (it.Owner == "net:IRQ")
                    Assert.That(r, Is.EqualTo(TraceRole.Signal));
                if (it.Owner == "part:1A")
                    Assert.That(r, Is.EqualTo(TraceRole.Dimmed), "1A has no IRQ pin");
                if (r == TraceRole.Signal) signal++;
                if (r == TraceRole.Joined) joined++;
                if (r == TraceRole.Dimmed) dimmed++;
            }
            Assert.That(signal, Is.GreaterThan(0));
            Assert.That(joined, Is.GreaterThan(0), "5A's outline is lit");
            Assert.That(dimmed, Is.GreaterThan(signal));
        }

        [Test]
        public void ClickingAPin_TracesItsSignal_AndEmptyPaperClears()
        {
            SheetSession s = Open();
            int nets = s.Netlist.Nets.Count;
            EditTool tool = new(s) { TraceMode = true };
            DrawItem? label = null;
            for (int i = 0; i < s.Drawing.Items.Count && label == null; i++)
            {
                if (s.Drawing.Items[i].Role == DrawRole.NetLabel && s.Drawing.Items[i].Owner == "net:IRQ")
                    label = s.Drawing.Items[i];
            }
            Assert.That(label, Is.Not.Null);
            // the middle of the diamond
            SchematicDrawing.Bounds(label!, out double l, out double top, out double r, out double b);
            tool.PointerDown((l + r) / 2, (top + b) / 2);
            Assert.That(s.Traced, Is.EqualTo("IRQ"));
            Assert.That(s.Netlist.Nets.Count, Is.EqualTo(nets), "tracing changes nothing");
            tool.PointerDown(-1000, -1000);
            Assert.That(s.Traced, Is.Empty);
            s.Trace("IRQ");
            tool.Cancel();
            Assert.That(s.Traced, Is.Empty, "Esc clears the trace");
            for (int i = 0; i < s.Drawing.Items.Count; i++)
                Assert.That(s.TraceOf(s.Drawing.Items[i]), Is.EqualTo(TraceRole.None));
        }
    }
}
