//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The drawing is clean: no chip or gate frames overlap, no text overlaps other text, a frame
    /// or a wire, no wire crosses a chip or gate frame, and no two wires lie on top of each
    /// other. Checked on the real sheet 2A50 and the demo, in both symbol styles.
    /// </summary>
    [TestFixture]
    public class LayoutCleanTests
    {
        private static readonly string Real = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

        private static IEnumerable<TestCaseData> Drawings()
        {
            string[] sheets = { Real, DemoSheetLayoutTests.Demo };
            SymbolStyle[] styles = { SymbolStyle.NorskData, SymbolStyle.Modern };
            for (int i = 0; i < sheets.Length; i++)
            {
                for (int j = 0; j < styles.Length; j++)
                    yield return new TestCaseData(sheets[i], styles[j]).SetName(Path.GetFileNameWithoutExtension(sheets[i]) + "_" + styles[j]);
            }
        }

        [TestCaseSource(nameof(Drawings))]
        public void NoTwoFrames_Overlap(string sheet, SymbolStyle style)
        {
            SchematicDrawing d = Draw(sheet, style);
            List<Box> frames = Frames(d, partsOnly: true);
            List<string> bad = new();
            for (int i = 0; i < frames.Count; i++)
            {
                for (int j = i + 1; j < frames.Count; j++)
                {
                    if (frames[i].Overlaps(frames[j]))
                        bad.Add(frames[i].Owner + " / " + frames[j].Owner);
                }
            }
            Assert.That(bad, Is.Empty);
        }

        [TestCaseSource(nameof(Drawings))]
        public void NoText_OverlapsTextOrAFrameOrAWire(string sheet, SymbolStyle style)
        {
            SchematicDrawing d = Draw(sheet, style);
            List<Box> texts = new();
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem it = d.Items[i];
                // the kind written inside an unfinished gate symbol is part of the symbol
                if (it.Kind != DrawKind.Text || it.Role == DrawRole.Symbol)
                    continue;
                texts.Add(Box.Of(it));
            }
            List<Box> frames = Frames(d, partsOnly: false);
            List<DrawItem> wires = Wires(d);
            List<string> bad = new();
            for (int i = 0; i < texts.Count; i++)
            {
                for (int j = i + 1; j < texts.Count; j++)
                {
                    if (texts[i].Overlaps(texts[j]))
                        bad.Add("text " + texts[i].Text + " / " + texts[j].Text);
                }
                for (int j = 0; j < frames.Count; j++)
                {
                    // a pin name is written inside its own chip's box, on purpose
                    if (texts[i].Role == DrawRole.PinName && frames[j].Owner == "part:" + PartOf(texts[i].Owner))
                        continue;
                    if (texts[i].Overlaps(frames[j]))
                        bad.Add("text " + texts[i].Text + " / frame " + frames[j].Owner);
                }
                for (int j = 0; j < wires.Count; j++)
                {
                    if (texts[i].Crosses(wires[j]))
                        bad.Add("text " + texts[i].Text + " / wire " + wires[j].Owner);
                }
            }
            Assert.That(bad, Is.Empty);
        }

        [TestCaseSource(nameof(Drawings))]
        public void NoWire_CrossesAChipOrGateFrame(string sheet, SymbolStyle style)
        {
            SchematicDrawing d = Draw(sheet, style);
            List<Box> frames = Frames(d, partsOnly: false);
            List<DrawItem> wires = Wires(d);
            List<string> bad = new();
            for (int i = 0; i < wires.Count; i++)
            {
                for (int j = 0; j < frames.Count; j++)
                {
                    if (frames[j].Crosses(wires[i]))
                        bad.Add(wires[i].Owner + " / " + frames[j].Owner);
                }
            }
            Assert.That(bad, Is.Empty);
        }

        [TestCaseSource(nameof(Drawings))]
        public void NoTwoWires_LieOnTopOfEachOther(string sheet, SymbolStyle style)
        {
            SchematicDrawing d = Draw(sheet, style);
            List<DrawItem> wires = Wires(d);
            List<string> bad = new();
            for (int i = 0; i < wires.Count; i++)
            {
                for (int j = i + 1; j < wires.Count; j++)
                {
                    if (Collinear(wires[i], wires[j]))
                        bad.Add(wires[i].Owner + " / " + wires[j].Owner);
                }
            }
            Assert.That(bad, Is.Empty);
        }

        [Test]
        public void TheRealSheet_GetsSomeWires_AndLabelsForTheFarSignals()
        {
            SchematicDrawing d = Draw(Real, SymbolStyle.NorskData);
            Assert.That(Wires(d).Count, Is.GreaterThan(10));
            int diamonds = 0;
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Role == DrawRole.NetLabel)
                    diamonds++;
            }
            Assert.That(diamonds, Is.GreaterThan(50));
        }

        [Test]
        public void TheChecks_CatchABadDrawing()
        {
            // proves the checks above can fail: a wire through a frame, two texts on each other,
            // two wires on one line
            DrawItem frame = new() { Kind = DrawKind.Rect, Role = DrawRole.PartOutline, Owner = "part:1A", X1 = 0, Y1 = 0, X2 = 50, Y2 = 50 };
            DrawItem through = new() { Kind = DrawKind.Line, Role = DrawRole.Wire, X1 = -10, Y1 = 25, X2 = 60, Y2 = 25 };
            DrawItem along = new() { Kind = DrawKind.Line, Role = DrawRole.Wire, X1 = 30, Y1 = 25, X2 = 90, Y2 = 25 };
            DrawItem beside = new() { Kind = DrawKind.Line, Role = DrawRole.Wire, X1 = 50, Y1 = 60, X2 = 90, Y2 = 60 };
            DrawItem a = new() { Kind = DrawKind.Text, Text = "ABC", X1 = 100, Y1 = 100, FontSize = 9 };
            DrawItem b = new() { Kind = DrawKind.Text, Text = "DEF", X1 = 105, Y1 = 104, FontSize = 9 };
            Assert.That(Box.Of(frame).Crosses(through), Is.True);
            Assert.That(Box.Of(frame).Crosses(beside), Is.False);
            Assert.That(Collinear(through, along), Is.True);
            Assert.That(Collinear(through, beside), Is.False);
            Assert.That(Box.Of(a).Overlaps(Box.Of(b)), Is.True);
            Assert.That(Box.Of(a).Crosses(new DrawItem { X1 = 90, Y1 = 97, X2 = 130, Y2 = 97 }), Is.True);
            SchematicDrawing d = Draw(Real, SymbolStyle.NorskData);
            Assert.That(Frames(d, partsOnly: false).Count, Is.GreaterThan(Frames(d, partsOnly: true).Count), "gate boxes are checked too");
        }

        [Test]
        public void ChipOutlines_AreThickerThanWires()
        {
            Assert.That(LineWidths.Outline, Is.GreaterThanOrEqualTo(2 * LineWidths.Wire));
            Assert.That(LineWidths.Of(new DrawItem { Role = DrawRole.Symbol }), Is.EqualTo(LineWidths.Outline));
            Assert.That(LineWidths.Of(new DrawItem { Role = DrawRole.PartOutline }), Is.EqualTo(LineWidths.Outline));
            Assert.That(LineWidths.Of(new DrawItem { Role = DrawRole.Wire }), Is.EqualTo(LineWidths.Wire));
            // the SVG writes the outline width on a symbol and leaves wires at the default 1
            Assert.That(SvgRenderer.Write(Draw(DemoSheetLayoutTests.Demo, SymbolStyle.NorskData)), Does.Contain("stroke-width=\"2.25\""));
        }

        private static SchematicDrawing Draw(string sheet, SymbolStyle style)
            => SheetLayout.Build(NetlistReader.ReadFile(sheet), style, new LayoutOptions { Catalog = new ChipCatalog(DemoSheetLayoutTests.Catalog) });

        private static string PartOf(string owner)
        {
            string s = owner.Substring(owner.IndexOf(':') + 1);
            int dot = s.LastIndexOf('.');
            return dot < 0 ? s : s.Substring(0, dot);
        }

        internal static List<DrawItem> Wires(SchematicDrawing d)
        {
            List<DrawItem> list = new();
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Role == DrawRole.Wire && d.Items[i].Kind == DrawKind.Line)
                    list.Add(d.Items[i]);
            }
            return list;
        }

        /// <summary>
        /// Chip frames, and (unless partsOnly) the box round each gate symbol.
        /// </summary>
        internal static List<Box> Frames(SchematicDrawing d, bool partsOnly)
        {
            List<Box> list = new();
            Dictionary<string, int> gate = new(StringComparer.Ordinal);
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem it = d.Items[i];
                if (it.Role == DrawRole.PartOutline)
                    list.Add(Box.Of(it));
                else if (!partsOnly && it.Role == DrawRole.Symbol && it.Kind != DrawKind.Text)
                {
                    Box b = Box.Of(it);
                    if (gate.TryGetValue(it.Owner, out int k))
                        list[k] = list[k].Union(b);
                    else
                    {
                        gate[it.Owner] = list.Count;
                        list.Add(b);
                    }
                }
            }
            return list;
        }

        private static bool Collinear(DrawItem a, DrawItem b)
        {
            bool ah = a.Y1 == a.Y2, bh = b.Y1 == b.Y2;
            if (ah && bh && a.Y1 == b.Y1)
                return Overlap(a.X1, a.X2, b.X1, b.X2) > 0;
            bool av = a.X1 == a.X2, bv = b.X1 == b.X2;
            if (av && bv && a.X1 == b.X1)
                return Overlap(a.Y1, a.Y2, b.Y1, b.Y2) > 0;
            return false;
        }

        private static double Overlap(double a1, double a2, double b1, double b2)
            => Math.Min(Math.Max(a1, a2), Math.Max(b1, b2)) - Math.Max(Math.Min(a1, a2), Math.Min(b1, b2));

        internal readonly struct Box
        {
            public readonly double L, T, R, B;
            public readonly string Owner, Text;
            public readonly DrawRole Role;

            public Box(double l, double t, double r, double b, string owner, string text, DrawRole role)
            {
                L = l; T = t; R = r; B = b; Owner = owner; Text = text; Role = role;
            }

            public static Box Of(DrawItem it)
            {
                SchematicDrawing.Bounds(it, out double l, out double t, out double r, out double b);
                if (it.Kind == DrawKind.Line)
                {
                    // Bounds widens a line for picking; the real extent is wanted here
                    l = Math.Min(it.X1, it.X2); r = Math.Max(it.X1, it.X2);
                    t = Math.Min(it.Y1, it.Y2); b = Math.Max(it.Y1, it.Y2);
                }
                return new Box(l, t, r, b, it.Owner, it.Text, it.Role);
            }

            public Box Union(Box o) => new(Math.Min(L, o.L), Math.Min(T, o.T), Math.Max(R, o.R), Math.Max(B, o.B), Owner, Text, Role);

            /// <summary>
            /// Strict overlap: boxes that only touch do not count.
            /// </summary>
            public bool Overlaps(Box o) => L < o.R && o.L < R && T < o.B && o.T < B;

            /// <summary>
            /// Does a horizontal or vertical wire piece run through this box (touching an edge does
            /// not count)?
            /// </summary>
            public bool Crosses(DrawItem w)
            {
                double l = Math.Min(w.X1, w.X2), r = Math.Max(w.X1, w.X2), t = Math.Min(w.Y1, w.Y2), b = Math.Max(w.Y1, w.Y2);
                if (t == b)
                    return t > T && t < B && l < R && r > L;
                return l > L && l < R && t < B && b > T;
            }
        }
    }
}
