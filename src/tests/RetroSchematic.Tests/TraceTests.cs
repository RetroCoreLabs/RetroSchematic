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
            string real = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
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
        public void EveryGateOutputOn2A50_IsDrawn_JoinedToItsSignal_AndTraceable([Values] SymbolStyle style)
        {
            // 3B (7451, AND-OR-INVERT) was drawn as an "OTHER" box; its output 3B.8 (N_3B_8 to
            // 7B.11) must be drawn and reachable like every other gate output
            string real = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
            SheetSession s = new(real, style, DemoSheetLayoutTests.Catalog);
            int checkedOutputs = 0;
            for (int g = 0; g < s.Netlist.Gates.Count; g++)
            {
                Model.Gate gate = s.Netlist.Gates[g];
                Assert.That(gate.Kind, Is.Not.EqualTo(Model.GateKind.Other), gate.Id + " has a real symbol");
                string at = gate.Part + "." + gate.Output.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Assert.That(s.Drawing.Pins.TryGetValue(at, out DrawPoint end), Is.True, at + " is drawn");
                Model.Net? net = null;
                for (int i = 0; i < s.Netlist.Nets.Count && net == null; i++)
                {
                    for (int j = 0; j < s.Netlist.Nets[i].Pins.Count; j++)
                    {
                        if (s.Netlist.Nets[i].Pins[j].At == at)
                            net = s.Netlist.Nets[i];
                    }
                }
                if (net == null)
                    continue;
                string key = SheetLayout.NetKey(net);
                // a wire or a label's pin line of that signal starts at the pin end
                bool joined = false;
                for (int i = 0; i < s.Drawing.Items.Count && !joined; i++)
                {
                    DrawItem it = s.Drawing.Items[i];
                    if (it.Kind != DrawKind.Line || it.Owner != "net:" + key)
                        continue;
                    joined = Near(it.X1, it.Y1, end) || Near(it.X2, it.Y2, end);
                }
                Assert.That(joined, Is.True, at + " is joined to " + key + " by a drawn line");
                s.Trace(key);
                Assert.That(s.TracedPins(), Does.Contain(at), at + " is lit by its trace");
                checkedOutputs++;
            }
            Assert.That(checkedOutputs, Is.GreaterThan(60));
            s.Trace("N_3B_8");
            Assert.That(s.TracedPins(), Is.EquivalentTo(new[] { "3B.8", "7B.11" }));
            // following it backwards: the 7451's four inputs, in the gate's input order
            SignalInfo info = SignalInfo.Of(s.Editor, s.Editor.FindNetByKey("N_3B_8")!, SignalNameStyle.Slash);
            Assert.That(info.DriverInputs.Count, Is.EqualTo(4));
            Assert.That(info.DriverInputs[0].At, Is.EqualTo("3B.9"));
            Assert.That(info.DriverInputs[3].At, Is.EqualTo("3B.1"));
            for (int i = 0; i < 4; i++)
                Assert.That(info.DriverInputs[i].NetKey, Is.Not.Empty, info.DriverInputs[i].At + " has a signal");
        }

        // a wire may stop at the pin dot's circle instead of its centre
        private static bool Near(double x, double y, DrawPoint p) => System.Math.Abs(x - p.X) <= 5 && System.Math.Abs(y - p.Y) <= 5;

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
