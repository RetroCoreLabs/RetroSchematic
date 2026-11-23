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
    /// The chip grid on the board photos (catalog/boards, the sheet's "pcb" section): position
    /// codes to cells, codes that span several cells, the mirrored solder side, placing the
    /// board on a photo by its four corners, a hand-set box winning over the cell, the chip
    /// under a point, finding the board in a photo, and the editor's undoable changes.
    /// </summary>
    [TestFixture]
    public class PcbGridTests
    {
        private static readonly string Catalog = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog");
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private static BoardTemplate Nord10() => BoardTemplate.Find(Catalog, "nord10-card")!;

        private static List<GridRow> Rows2A50()
            => new()
            {
                new GridRow { Name = "A", FromMm = 12, ToMm = 41.5 },
                new GridRow { Name = "B", FromMm = 41.5, ToMm = 65.5 },
                new GridRow { Name = "C", FromMm = 65.5, ToMm = 93.5 },
                new GridRow { Name = "D", FromMm = 93.5, ToMm = 120 },
                new GridRow { Name = "E", FromMm = 120, ToMm = 149 },
            };

        [Test]
        public void BoardTypes_Load_AndTheNord10ColumnsAreTheMeasuredSteps()
        {
            BoardTemplate t = Nord10();
            Assert.That(t.Size.WidthMm, Is.EqualTo(156));
            Assert.That(t.Size.HeightMm, Is.EqualTo(149));
            Assert.That(t.Columns!.PitchMm, Is.EqualTo(7.62));
            Assert.That(t.Columns.FirstCentreMm, Is.EqualTo(9.99));
            Assert.That(t.Confidence, Is.EqualTo(Confidence.Inferred));
            Assert.That(t.Rows, Is.Null, "the rows differ per board, so each sheet gives its own");
            BoardTemplate nd100 = BoardTemplate.Find(Catalog, "nd100-card")!;
            Assert.That(nd100.Columns, Is.Null, "no ND-100 sheet yet: the columns are open");
            Assert.That(BoardTemplate.Find(Catalog, "no-such-card"), Is.Null);
        }

        [Test]
        public void PositionCode_OneCell()
        {
            Assert.That(PcbGrid.Cell(Nord10(), Rows2A50(), "13D", out double x, out double y, out double w, out double h), Is.True);
            // column 13 centre = 9.99 + 12 * 7.62 = 101.43; the cell is one step wide round it
            Assert.That(x, Is.EqualTo(101.43 - 3.81).Within(1e-9));
            Assert.That(w, Is.EqualTo(7.62).Within(1e-9));
            Assert.That(y, Is.EqualTo(93.5));
            Assert.That(h, Is.EqualTo(26.5));
        }

        [Test]
        public void PositionCode_SpansColumnsAndRows()
        {
            Assert.That(PcbGrid.ParseCode("7-8C-D", out int c0, out int c1, out char r0, out char r1), Is.True);
            Assert.That((c0, c1, r0, r1), Is.EqualTo((7, 8, 'C', 'D')));
            Assert.That(PcbGrid.Cell(Nord10(), Rows2A50(), "7-8C-D", out double x, out double y, out double w, out double h), Is.True);
            Assert.That(x, Is.EqualTo(9.99 + (6 * 7.62) - 3.81).Within(1e-9));
            Assert.That(w, Is.EqualTo(15.24).Within(1e-9), "two steps: a 0.6 inch chip");
            Assert.That(y, Is.EqualTo(65.5));
            Assert.That(h, Is.EqualTo(120 - 65.5).Within(1e-9));
            Assert.That(PcbGrid.ParseCode("18-19E", out c0, out c1, out r0, out r1), Is.True);
            Assert.That((c0, c1, r0, r1), Is.EqualTo((18, 19, 'E', 'E')));
            Assert.That(PcbGrid.ParseCode("R12", out _, out _, out _, out _), Is.False);
            Assert.That(PcbGrid.Cell(Nord10(), Rows2A50(), "13F", out _, out _, out _, out _), Is.False, "no row F");
            Assert.That(PcbGrid.Cell(Nord10(), null, "13D", out _, out _, out _, out _), Is.False, "no rows at all");
        }

        [Test]
        public void ToPhoto_UsesTheFourCorners_AndMirrorsTheSolderSide()
        {
            BoardSize s = new() { WidthMm = 100, HeightMm = 50 };
            GridPoint[] c = { new(10, 20), new(210, 20), new(210, 120), new(10, 120) };
            GridPoint p = PcbGrid.ToPhoto(c, s, false, new GridPoint(25, 10));
            Assert.That(p.X, Is.EqualTo(60).Within(1e-9));
            Assert.That(p.Y, Is.EqualTo(40).Within(1e-9));
            GridPoint m = PcbGrid.ToPhoto(c, s, true, new GridPoint(25, 10));
            Assert.That(m.X, Is.EqualTo(160).Within(1e-9), "the solder photo shows the board turned over left to right");
            Assert.That(m.Y, Is.EqualTo(40).Within(1e-9));
            // a slanted photo: the corners are not a rectangle, the middle goes between them
            GridPoint[] slant = { new(0, 0), new(100, 10), new(110, 110), new(-10, 100) };
            GridPoint mid = PcbGrid.ToPhoto(slant, s, false, new GridPoint(50, 25));
            Assert.That(mid.X, Is.EqualTo(50).Within(1e-9));
            Assert.That(mid.Y, Is.EqualTo(55).Within(1e-9));
        }

        [Test]
        public void HandSetBox_WinsOverTheCell_AndCanBeTurned()
        {
            PcbLayout layout = new() { Template = "nord10-card", Rows = Rows2A50() };
            Assert.That(PcbGrid.Area(Nord10(), layout, "13D", out GridPoint[] cell, out bool byHand), Is.True);
            Assert.That(byHand, Is.False);
            Assert.That(cell[0].X, Is.EqualTo(97.62).Within(1e-9));
            layout.Boxes = new List<ChipBox> { new() { Ref = "13d", XMm = 100, YMm = 105, WidthMm = 8, HeightMm = 20 } };
            Assert.That(PcbGrid.Area(Nord10(), layout, "13D", out GridPoint[] box, out byHand), Is.True);
            Assert.That(byHand, Is.True);
            Assert.That(box[0].X, Is.EqualTo(96).Within(1e-9));
            Assert.That(box[0].Y, Is.EqualTo(95).Within(1e-9));
            layout.Boxes[0].RotationDeg = 90;
            PcbGrid.Area(Nord10(), layout, "13D", out GridPoint[] turned, out _);
            double x0 = double.MaxValue, x1 = double.MinValue;
            for (int i = 0; i < 4; i++)
            {
                x0 = System.Math.Min(x0, turned[i].X);
                x1 = System.Math.Max(x1, turned[i].X);
            }
            Assert.That(x1 - x0, Is.EqualTo(20).Within(1e-9), "turned a quarter: the 20 mm side runs across");
        }

        [Test]
        public void Overlay_FindsTheChipUnderAPoint_On2A50()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            Assert.That(n.Pcb, Is.Not.Null, "2A50 carries its measured rows");
            // a made-up photo: the board fills 1560 x 1490 pixels, 10 pixels per mm
            GridPoint[] c = { new(0, 0), new(1560, 0), new(1560, 1490), new(0, 1490) };
            List<PcbBox> boxes = PcbOverlay.Build(n, Nord10(), c, false);
            Assert.That(boxes.Count, Is.EqualTo(n.Parts.Count + n.Switches!.Count), "every part and switch has a position code");
            PcbBox? b = PcbOverlay.At(boxes, new GridPoint(1014, 1060));
            Assert.That(b!.Ref, Is.EqualTo("13D"));
            Assert.That(b.Owner, Is.EqualTo("part:13D"));
            Assert.That(b.Label, Does.StartWith("13D  7442"));
            Assert.That(PcbOverlay.At(boxes, new GridPoint(590, 900))!.Ref, Is.EqualTo("7-8C-D"));
            Assert.That(PcbOverlay.At(boxes, new GridPoint(100, 1350))!.Owner, Is.EqualTo("switch:1E"));
            Assert.That(PcbOverlay.At(boxes, new GridPoint(780, 50)), Is.Null, "the finger edge holds no chip");
            // the solder side: 13D is mirrored to the other half
            List<PcbBox> back = PcbOverlay.Build(n, Nord10(), c, true);
            Assert.That(PcbOverlay.At(back, new GridPoint(1560 - 1014, 1060))!.Ref, Is.EqualTo("13D"));
        }

        [Test]
        public void FindOutline_FindsTheBoardOnALightBackground_OrGivesUp()
        {
            // a 300 x 200 grey-white picture with a dark green board of 156 x 149 units at 40,20
            const int w = 300, h = 200;
            byte[] px = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = ((y * w) + x) * 4;
                    bool board = x >= 40 && x < 196 && y >= 20 && y < 169;
                    px[i] = board ? (byte)40 : (byte)225;
                    px[i + 1] = board ? (byte)90 : (byte)228;
                    px[i + 2] = board ? (byte)30 : (byte)230;
                    px[i + 3] = 255;
                }
            }
            BoardSize s = new() { WidthMm = 156, HeightMm = 149 };
            GridPoint[]? c = PcbGrid.FindOutline(px, w, h, w * 4, s);
            Assert.That(c, Is.Not.Null);
            Assert.That(c![0].X, Is.EqualTo(40).Within(1));
            Assert.That(c[0].Y, Is.EqualTo(20).Within(1));
            Assert.That(c[2].X, Is.EqualTo(196).Within(1));
            Assert.That(c[2].Y, Is.EqualTo(169).Within(1));
            // the wrong shape (a board of another size) is not trusted
            Assert.That(PcbGrid.FindOutline(px, w, h, w * 4, new BoardSize { WidthMm = 367, HeightMm = 280 }), Is.Null);
        }

        [Test]
        public void Editor_SetsBoxesAndCorners_OneUndoStepEach()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            SheetEditor e = new(n, null) { ReadOnly = false };
            Assert.Throws<SheetEditException>(() => e.SetChipBox(new ChipBox { Ref = "13D" }), "no size");
            e.SetChipBox(new ChipBox { Ref = "13D", XMm = 100, YMm = 105, WidthMm = 8, HeightMm = 20 });
            e.SetChipBox(new ChipBox { Ref = "13D", XMm = 101, YMm = 105, WidthMm = 8, HeightMm = 20 });
            Assert.That(e.Root.Pcb!.Boxes!.Count, Is.EqualTo(1), "a second box for the same part replaces the first");
            Assert.That(e.Root.Pcb.Boxes[0].XMm, Is.EqualTo(101));
            e.Undo();
            Assert.That(e.Root.Pcb!.Boxes![0].XMm, Is.EqualTo(100));
            e.RemoveChipBox("13D");
            Assert.That(e.Root.Pcb!.Boxes, Is.Null);
            Assert.That(e.Root.Pcb.Rows!.Count, Is.EqualTo(5), "the rows stay");

            e.SetPhotoCorners(0, new GridPoint[] { new(1, 2), new(3, 4), new(5, 6), new(7, 8) });
            Assert.That(e.Root.Photos![0].Corners![2].X, Is.EqualTo(5));
            Assert.Throws<SheetEditException>(() => e.SetPhotoCorners(0, new GridPoint[] { new(1, 2) }));
            e.SetPhotoCorners(0, null);
            Assert.That(e.Root.Photos[0].Corners, Is.Null);
        }
    }
}
