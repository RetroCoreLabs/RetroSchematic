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
    /// No line is ever drawn through a circle (pin-end rings, junction dots, port rings,
    /// inversion bubbles): every line stops at the edge. Checked on every sheet in both symbol
    /// styles, and the block port names follow the name style.
    /// </summary>
    [TestFixture]
    public class CircleTests
    {
        private static readonly string Folder = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10");

        private static IEnumerable<TestCaseData> Sheets()
        {
            string[] names = { "2A50-sync-modem-buffer-1050.rsc", "2A50-sync-modem-buffer-1050-blocks.rsc" };
            for (int i = 0; i < names.Length; i++)
            {
                yield return new TestCaseData(Path.Combine(Folder, names[i]), SymbolStyle.NorskData).SetName("NoLineThroughACircle(" + names[i] + ", nd)");
                yield return new TestCaseData(Path.Combine(Folder, names[i]), SymbolStyle.Modern).SetName("NoLineThroughACircle(" + names[i] + ", modern)");
            }
            yield return new TestCaseData(DemoSheetLayoutTests.Demo, SymbolStyle.NorskData).SetName("NoLineThroughACircle(demo, nd)");
            yield return new TestCaseData(DemoSheetLayoutTests.Demo, SymbolStyle.Modern).SetName("NoLineThroughACircle(demo, modern)");
        }

        [TestCaseSource(nameof(Sheets))]
        public void NoLineThroughACircle(string sheet, SymbolStyle style)
        {
            string? catalog = ChipCatalog.Find(sheet);
            SchematicDrawing d = SheetLayout.Build(NetlistReader.ReadFile(sheet), style, new LayoutOptions { Catalog = catalog == null ? null : new ChipCatalog(catalog) });
            List<DrawItem> circles = new();
            int lines = 0;
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Kind == DrawKind.Circle)
                    circles.Add(d.Items[i]);
                if (d.Items[i].Kind == DrawKind.Line)
                    lines++;
            }
            Assert.That(circles.Count, Is.GreaterThan(0));
            Assert.That(lines, Is.GreaterThan(0));
            List<string> bad = new();
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem l = d.Items[i];
                if (l.Kind != DrawKind.Line)
                    continue;
                for (int c = 0; c < circles.Count; c++)
                {
                    // a hair inside the edge is allowed for rounding, nothing more
                    if (SheetLayout.EntersCircle(l.X1, l.Y1, l.X2, l.Y2, circles[c].X1, circles[c].Y1, circles[c].X2 - 0.01))
                        bad.Add(l.Role + " " + l.Owner + " through " + circles[c].Role + " " + circles[c].Owner);
                }
            }
            Assert.That(bad, Is.Empty);
        }

        [Test]
        public void ThePinLine_AndTheWire_StopAtThePinEndRing()
        {
            // the case that was wrong: U1's left port pins, line from box to ring, wire to the tag
            string sheet = Path.Combine(Folder, "2A50-sync-modem-buffer-1050-blocks.rsc");
            SchematicDrawing d = SheetLayout.Build(NetlistReader.ReadFile(sheet), SymbolStyle.NorskData, new LayoutOptions { Catalog = new ChipCatalog(ChipCatalog.Find(sheet)!) });
            DrawPoint ring = d.Pins["U1:DEQL.0"];
            int touching = 0;
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem l = d.Items[i];
                if (l.Kind != DrawKind.Line)
                    continue;
                if (System.Math.Abs(l.Y1 - ring.Y) < 0.01 && System.Math.Abs(l.Y2 - ring.Y) < 0.01
                    && (System.Math.Abs(System.Math.Abs(l.X1 - ring.X) - SheetLayout.PinDotRadius) < 0.01 || System.Math.Abs(System.Math.Abs(l.X2 - ring.X) - SheetLayout.PinDotRadius) < 0.01))
                    touching++;
            }
            Assert.That(touching, Is.GreaterThanOrEqualTo(1), "a line ends exactly on the ring's edge");
        }

        [Test]
        public void BlockPortNames_FollowTheNameStyle()
        {
            string sheet = Path.Combine(Folder, "2A50-sync-modem-buffer-1050-blocks.rsc");
            Netlist n = NetlistReader.ReadFile(sheet);
            ChipCatalog cat = new(ChipCatalog.Find(sheet)!);
            List<string> slash = PinNames(SheetLayout.Build(n, SymbolStyle.NorskData, new LayoutOptions { Catalog = cat, NameStyle = SignalNameStyle.Slash }));
            List<string> sub = PinNames(SheetLayout.Build(n, SymbolStyle.NorskData, new LayoutOptions { Catalog = cat, NameStyle = SignalNameStyle.Subscript }));
            Assert.That(slash, Does.Contain("CL").And.Contain("/BA[9:0]").And.Contain("/IOXE"), "a port named without a level takes its signal's level");
            Assert.That(slash, Does.Not.Contain("CL_1"));
            Assert.That(sub, Does.Contain("CL_1").And.Contain("BA_0[9:0]"));
        }

        private static List<string> PinNames(SchematicDrawing d)
        {
            List<string> t = new();
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Role == DrawRole.PinName && d.Items[i].Owner.StartsWith("port:", System.StringComparison.Ordinal))
                    t.Add(d.Items[i].Text);
            }
            return t;
        }
    }
}
