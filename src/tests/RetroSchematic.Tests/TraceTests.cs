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
            Assert.That(s.Traced, Is.EqualTo("IRQ_0"));
            List<string> pins = s.TracedPins();
            Assert.That(pins, Does.Contain("5A.3").And.Contain("5A.6"));
            Assert.That(s.PinPoint("5A.3", out double px, out double py), Is.True, "a drawn pin has a place to centre on");
            Assert.That(s.Drawing.HitTest(px, py), Is.EqualTo("pin:5A.3").Or.EqualTo("net:IRQ_0"));
            Assert.That(s.PinPoint("5A.99", out _, out _), Is.False);
            int signal = 0, joined = 0, dimmed = 0;
            for (int i = 0; i < s.Drawing.Items.Count; i++)
            {
                DrawItem it = s.Drawing.Items[i];
                TraceRole r = s.TraceOf(it);
                if (it.Owner == "net:IRQ_0")
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
        public void TwoSignalsOfTheSameName_AreTracedApart()
        {
            // 2A50 has two signals named BD15: subscript 0 and subscript 1
            string real = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");
            SheetSession s = new(real, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);
            List<string> keys = new();
            for (int i = 0; i < s.Netlist.Nets.Count; i++)
            {
                if (s.Netlist.Nets[i].Name == "BD15")
                    keys.Add(SheetLayout.NetKey(s.Netlist.Nets[i]));
            }
            Assert.That(keys.Count, Is.EqualTo(2));
            Assert.That(keys[0], Is.Not.EqualTo(keys[1]));
            s.Trace(keys[0]);
            List<string> pins0 = s.TracedPins();
            s.Trace(keys[1]);
            List<string> pins1 = s.TracedPins();
            for (int i = 0; i < pins0.Count; i++)
                Assert.That(pins1, Does.Not.Contain(pins0[i]), "the two BD15 signals share no pin");
            for (int i = 0; i < s.Drawing.Items.Count; i++)
            {
                DrawItem it = s.Drawing.Items[i];
                if (it.Owner == "net:" + keys[0])
                    Assert.That(s.TraceOf(it), Is.EqualTo(TraceRole.Dimmed), "the other BD15 is lit");
            }
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
                if (s.Drawing.Items[i].Role == DrawRole.NetLabel && s.Drawing.Items[i].Owner == "net:IRQ_0")
                    label = s.Drawing.Items[i];
            }
            Assert.That(label, Is.Not.Null);
            // the middle of the diamond
            SchematicDrawing.Bounds(label!, out double l, out double top, out double r, out double b);
            tool.PointerDown((l + r) / 2, (top + b) / 2);
            Assert.That(s.Traced, Is.EqualTo("IRQ_0"));
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
