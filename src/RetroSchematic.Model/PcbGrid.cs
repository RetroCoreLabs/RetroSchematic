//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace RetroSchematic.Model
{
    /// <summary>
    /// A board type's chip grid (catalog/boards/*.json): the board size and where ND's numbered
    /// columns lie, measured once from photos of several boards of the type. Positions are in
    /// millimetres on the component side, x from the left edge and y from the top edge, with
    /// the backplane edge (the gold fingers) at the top. The solder side is the same grid
    /// mirrored left to right.
    /// </summary>
    public sealed class BoardTemplate
    {
        /// <summary>
        /// The file name without ".json", such as "nord10-card".
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// A title, such as "NORD-10 card, 156 x 149 mm".
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The ND machine: NORD-10 or ND-100.
        /// </summary>
        public string? NdSystem { get; set; }

        /// <summary>
        /// The board size the grid is measured against.
        /// </summary>
        public BoardSize Size { get; set; } = new();

        /// <summary>
        /// How the board is turned for the coordinates, in words.
        /// </summary>
        public string? Orientation { get; set; }

        /// <summary>
        /// Where the columns lie, or null when not known.
        /// </summary>
        public GridColumns? Columns { get; set; }

        /// <summary>
        /// The rows when every board of the type shares them, else null (each sheet gives its
        /// own in its "pcb" section).
        /// </summary>
        public List<GridRow>? Rows { get; set; }

        /// <summary>
        /// What is known about the rows, in words.
        /// </summary>
        public string? RowsNote { get; set; }

        /// <summary>
        /// How the grid was measured.
        /// </summary>
        public string? Measured { get; set; }

        /// <summary>
        /// The part numbers of the boards measured.
        /// </summary>
        public List<string>? BoardsMeasured { get; set; }

        /// <summary>
        /// How sure the grid is (inferred: measured from photos, not from a drawing).
        /// </summary>
        public Confidence Confidence { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Reads a board type file.
        /// </summary>
        /// <param name="path">
        /// The .json file.
        /// </param>
        /// <returns>
        /// The template.
        /// </returns>
        /// <exception cref="NetlistException">
        /// The file is not a valid board type.
        /// </exception>
        public static BoardTemplate Load(string path)
        {
            BoardTemplate t = NetlistReader.FromJson<BoardTemplate>(File.ReadAllText(path));
            if (t.Size.WidthMm <= 0 || t.Size.HeightMm <= 0)
                throw new NetlistException(path + ": the board size is missing.");
            if (t.Columns != null && t.Columns.PitchMm <= 0)
                throw new NetlistException(path + ": the column pitch must be more than 0.");
            return t;
        }

        /// <summary>
        /// Finds a board type by id in a catalogue folder (the one with chips/ and boards/).
        /// </summary>
        /// <param name="catalog">
        /// The catalogue folder, or null.
        /// </param>
        /// <param name="id">
        /// The id, such as "nord10-card".
        /// </param>
        /// <returns>
        /// The template, or null when there is no such file.
        /// </returns>
        public static BoardTemplate? Find(string? catalog, string? id)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(id))
                return null;
            string path = Path.Combine(catalog, "boards", id + ".json");
            return File.Exists(path) ? Load(path) : null;
        }
    }

    /// <summary>
    /// The columns of a board type: ND numbers them 1, 2, 3 ... from the left, one number per
    /// step of the grid, so a chip one step wide sits on one number (13D) and one two steps
    /// wide on two (7-8C-D).
    /// </summary>
    public sealed class GridColumns
    {
        /// <summary>
        /// The first column number, normally 1.
        /// </summary>
        public int First { get; set; } = 1;

        /// <summary>
        /// The last column number on the board.
        /// </summary>
        public int Last { get; set; }

        /// <summary>
        /// The centre of the first column, mm from the left edge.
        /// </summary>
        public double FirstCentreMm { get; set; }

        /// <summary>
        /// The step from one column number to the next, mm.
        /// </summary>
        public double PitchMm { get; set; }
    }

    /// <summary>
    /// One row of chips: its letter and where it lies, mm from the top edge.
    /// </summary>
    public sealed class GridRow
    {
        /// <summary>
        /// The row letter, such as "A".
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The top of the row, mm from the top edge.
        /// </summary>
        public double FromMm { get; set; }

        /// <summary>
        /// The bottom of the row, mm from the top edge.
        /// </summary>
        public double ToMm { get; set; }
    }

    /// <summary>
    /// The sheet's "pcb" section: which board type grid the photos use, the board's own rows,
    /// and chip boxes drawn by hand.
    /// </summary>
    public sealed class PcbLayout
    {
        /// <summary>
        /// The board type id in catalog/boards, such as "nord10-card".
        /// </summary>
        public string? Template { get; set; }

        /// <summary>
        /// This board's rows (they differ between boards of one type).
        /// </summary>
        public List<GridRow>? Rows { get; set; }

        /// <summary>
        /// Where the rows were measured.
        /// </summary>
        public DescriptionSource? RowsSource { get; set; }

        /// <summary>
        /// Chip boxes set by hand; a box wins over the grid cell of its position code.
        /// </summary>
        public List<ChipBox>? Boxes { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// A chip's box set by hand: a rectangle in mm on the component side, turned by an angle
    /// round its centre. The solder side shows it mirrored.
    /// </summary>
    public sealed class ChipBox
    {
        /// <summary>
        /// The part (or switch block) reference, such as "13D".
        /// </summary>
        public string Ref { get; set; } = "";

        /// <summary>
        /// The centre, mm from the left edge.
        /// </summary>
        public double XMm { get; set; }

        /// <summary>
        /// The centre, mm from the top edge.
        /// </summary>
        public double YMm { get; set; }

        /// <summary>
        /// The width, mm.
        /// </summary>
        public double WidthMm { get; set; }

        /// <summary>
        /// The height, mm.
        /// </summary>
        public double HeightMm { get; set; }

        /// <summary>
        /// The turn in degrees, clockwise as seen on the component side.
        /// </summary>
        public double RotationDeg { get; set; }
    }

    /// <summary>
    /// A point in a photo, in its pixels from the top left.
    /// </summary>
    public sealed class PhotoPoint
    {
        /// <summary>
        /// Pixels from the left.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Pixels from the top.
        /// </summary>
        public double Y { get; set; }
    }

    /// <summary>
    /// A point in mm or pixels.
    /// </summary>
    /// <param name="X">
    /// Across.
    /// </param>
    /// <param name="Y">
    /// Down.
    /// </param>
    public readonly record struct GridPoint(double X, double Y);

    /// <summary>
    /// The grid arithmetic: position codes to cells, cells to photo pixels, and which chip is
    /// under a point.
    /// </summary>
    public static class PcbGrid
    {
        // 13D, 7-8C-D, 1E, 19C: column (or column range) then row letter (or range)
        private static readonly Regex s_code = new(@"^(\d{1,2})(?:-(\d{1,2}))?([A-Z])(?:-([A-Z]))?$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Reads a position code.
        /// </summary>
        /// <param name="code">
        /// Such as "13D" or "7-8C-D".
        /// </param>
        /// <param name="firstColumn">
        /// The first column number.
        /// </param>
        /// <param name="lastColumn">
        /// The last column number (the same for one column).
        /// </param>
        /// <param name="firstRow">
        /// The first row letter.
        /// </param>
        /// <param name="lastRow">
        /// The last row letter.
        /// </param>
        /// <returns>
        /// True when the code is in that form.
        /// </returns>
        public static bool ParseCode(string code, out int firstColumn, out int lastColumn, out char firstRow, out char lastRow)
        {
            firstColumn = lastColumn = 0;
            firstRow = lastRow = ' ';
            Match m = s_code.Match(code.Trim());
            if (!m.Success)
                return false;
            firstColumn = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            lastColumn = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : firstColumn;
            firstRow = m.Groups[3].Value[0];
            lastRow = m.Groups[4].Success ? m.Groups[4].Value[0] : firstRow;
            if (lastColumn < firstColumn)
                (firstColumn, lastColumn) = (lastColumn, firstColumn);
            if (lastRow < firstRow)
                (firstRow, lastRow) = (lastRow, firstRow);
            return true;
        }

        /// <summary>
        /// The cell(s) a position code names, as a rectangle in mm on the component side: from
        /// the left of its first column to the right of its last, the top of its first row to
        /// the bottom of its last. Each column number is one pitch wide, centred on its centre.
        /// </summary>
        /// <param name="template">
        /// The board type (columns).
        /// </param>
        /// <param name="rows">
        /// The rows (the sheet's own, else the template's).
        /// </param>
        /// <param name="code">
        /// The position code.
        /// </param>
        /// <param name="x">
        /// Left, mm.
        /// </param>
        /// <param name="y">
        /// Top, mm.
        /// </param>
        /// <param name="width">
        /// Width, mm.
        /// </param>
        /// <param name="height">
        /// Height, mm.
        /// </param>
        /// <returns>
        /// False when the code is not a position code, or a column or row is not on the grid.
        /// </returns>
        public static bool Cell(BoardTemplate template, List<GridRow>? rows, string code, out double x, out double y, out double width, out double height)
        {
            x = y = width = height = 0;
            GridColumns? c = template.Columns;
            if (c == null || rows == null || !ParseCode(code, out int c0, out int c1, out char r0, out char r1))
                return false;
            if (c0 < c.First || c1 > c.Last)
                return false;
            GridRow? top = RowOf(rows, r0), bottom = RowOf(rows, r1);
            if (top == null || bottom == null)
                return false;
            x = c.FirstCentreMm + ((c0 - c.First) * c.PitchMm) - (c.PitchMm / 2);
            width = (c1 - c0 + 1) * c.PitchMm;
            y = top.FromMm;
            height = bottom.ToMm - top.FromMm;
            return height > 0;
        }

        /// <summary>
        /// The four corners of a chip's area in mm on the component side, clockwise from the
        /// top left: its hand-set box when it has one, else the cell(s) of its position code.
        /// </summary>
        /// <param name="template">
        /// The board type.
        /// </param>
        /// <param name="layout">
        /// The sheet's pcb section, or null.
        /// </param>
        /// <param name="reference">
        /// The part reference (its position code).
        /// </param>
        /// <param name="corners">
        /// Four points.
        /// </param>
        /// <param name="byHand">
        /// True when the hand-set box was used.
        /// </param>
        /// <returns>
        /// False when the chip has no box and no cell.
        /// </returns>
        public static bool Area(BoardTemplate template, PcbLayout? layout, string reference, out GridPoint[] corners, out bool byHand)
        {
            byHand = false;
            corners = Array.Empty<GridPoint>();
            for (int i = 0; layout?.Boxes != null && i < layout.Boxes.Count; i++)
            {
                if (string.Equals(layout.Boxes[i].Ref, reference, StringComparison.OrdinalIgnoreCase))
                {
                    corners = BoxCorners(layout.Boxes[i]);
                    byHand = true;
                    return true;
                }
            }
            if (!Cell(template, layout?.Rows ?? template.Rows, reference, out double x, out double y, out double w, out double h))
                return false;
            corners = new[] { new GridPoint(x, y), new GridPoint(x + w, y), new GridPoint(x + w, y + h), new GridPoint(x, y + h) };
            return true;
        }

        /// <summary>
        /// The corners of a hand-set box, clockwise from its own top left, turned round its
        /// centre.
        /// </summary>
        /// <param name="b">
        /// The box.
        /// </param>
        /// <returns>
        /// Four points in mm.
        /// </returns>
        public static GridPoint[] BoxCorners(ChipBox b)
        {
            double a = b.RotationDeg * Math.PI / 180, cos = Math.Cos(a), sin = Math.Sin(a);
            double hw = b.WidthMm / 2, hh = b.HeightMm / 2;
            GridPoint[] p = new GridPoint[4];
            double[] dx = { -hw, hw, hw, -hw };
            double[] dy = { -hh, -hh, hh, hh };
            for (int i = 0; i < 4; i++)
                p[i] = new GridPoint(b.XMm + (dx[i] * cos) - (dy[i] * sin), b.YMm + (dx[i] * sin) + (dy[i] * cos));
            return p;
        }

        /// <summary>
        /// Where a board point (mm, component side) is in a photo whose board outline is given
        /// by its four corners as seen in the photo (top left, top right, bottom right, bottom
        /// left, with the backplane edge at the top). The point is placed by blending the four
        /// corners, which follows a photo taken a little at a slant. A solder-side photo shows
        /// the board turned over, so its left is the component side's right.
        /// </summary>
        /// <param name="corners">
        /// The board's corners in the photo, pixels.
        /// </param>
        /// <param name="size">
        /// The board size.
        /// </param>
        /// <param name="solder">
        /// True for a solder-side photo (mirrored).
        /// </param>
        /// <param name="p">
        /// The board point, mm.
        /// </param>
        /// <returns>
        /// The photo point, pixels.
        /// </returns>
        public static GridPoint ToPhoto(IReadOnlyList<GridPoint> corners, BoardSize size, bool solder, GridPoint p)
        {
            double u = p.X / size.WidthMm, v = p.Y / size.HeightMm;
            if (solder)
                u = 1 - u;
            GridPoint tl = corners[0], tr = corners[1], br = corners[2], bl = corners[3];
            double x = ((1 - u) * (1 - v) * tl.X) + (u * (1 - v) * tr.X) + (u * v * br.X) + ((1 - u) * v * bl.X);
            double y = ((1 - u) * (1 - v) * tl.Y) + (u * (1 - v) * tr.Y) + (u * v * br.Y) + ((1 - u) * v * bl.Y);
            return new GridPoint(x, y);
        }

        /// <summary>
        /// True when a point lies inside a four-cornered shape (either winding).
        /// </summary>
        /// <param name="quad">
        /// The corners in order round the shape.
        /// </param>
        /// <param name="p">
        /// The point.
        /// </param>
        /// <returns>
        /// True when inside.
        /// </returns>
        public static bool Inside(IReadOnlyList<GridPoint> quad, GridPoint p)
        {
            bool inside = false;
            for (int i = 0, j = quad.Count - 1; i < quad.Count; j = i++)
            {
                GridPoint a = quad[i], b = quad[j];
                if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < ((b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y)) + a.X)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>
        /// The area of a four-cornered shape (to let the smallest of two overlapping boxes win).
        /// </summary>
        /// <param name="quad">
        /// The corners.
        /// </param>
        /// <returns>
        /// The area.
        /// </returns>
        public static double AreaOf(IReadOnlyList<GridPoint> quad)
        {
            double s = 0;
            for (int i = 0, j = quad.Count - 1; i < quad.Count; j = i++)
                s += (quad[j].X * quad[i].Y) - (quad[i].X * quad[j].Y);
            return Math.Abs(s) / 2;
        }

        /// <summary>
        /// Finds the board in a photo of it on a plain light background: the columns and rows
        /// where more than half of the widest run is not background (light and grey). The
        /// result is trusted only when its width to height matches the board's within 3 %;
        /// otherwise null, and the caller uses the whole picture.
        /// </summary>
        /// <param name="bgra">
        /// The pixels, 4 bytes each (blue, green, red, alpha), row after row.
        /// </param>
        /// <param name="width">
        /// Pixels across.
        /// </param>
        /// <param name="height">
        /// Pixels down.
        /// </param>
        /// <param name="stride">
        /// Bytes per row.
        /// </param>
        /// <param name="size">
        /// The board size, for the width-to-height check.
        /// </param>
        /// <returns>
        /// The four corners (top left, top right, bottom right, bottom left), or null.
        /// </returns>
        public static GridPoint[]? FindOutline(ReadOnlySpan<byte> bgra, int width, int height, int stride, BoardSize size)
        {
            if (width < 8 || height < 8)
                return null;
            int[] colCount = new int[width];
            int[] rowCount = new int[height];
            for (int y = 0; y < height; y++)
            {
                ReadOnlySpan<byte> row = bgra.Slice(y * stride, width * 4);
                for (int x = 0; x < width; x++)
                {
                    int b = row[x * 4], g = row[(x * 4) + 1], r = row[(x * 4) + 2];
                    int max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
                    // background: bright and without colour (the white sheet the board lies on)
                    if (max > 170 && max - min < 40)
                        continue;
                    colCount[x]++;
                    rowCount[y]++;
                }
            }
            if (!Span(colCount, out int x0, out int x1) || !Span(rowCount, out int y0, out int y1))
                return null;
            double ratio = (double)(x1 - x0) / (y1 - y0);
            double want = size.WidthMm / size.HeightMm;
            if (Math.Abs(ratio - want) / want > 0.03)
                return null;
            return new[] { new GridPoint(x0, y0), new GridPoint(x1, y0), new GridPoint(x1, y1), new GridPoint(x0, y1) };
        }

        // the first and last places where the count is above half its largest value
        private static bool Span(int[] count, out int first, out int last)
        {
            int max = 0;
            for (int i = 0; i < count.Length; i++)
                max = Math.Max(max, count[i]);
            first = -1;
            last = -1;
            if (max == 0)
                return false;
            for (int i = 0; i < count.Length; i++)
            {
                if (count[i] * 2 > max)
                {
                    if (first < 0)
                        first = i;
                    last = i;
                }
            }
            return last > first;
        }

        private static GridRow? RowOf(List<GridRow> rows, char letter)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Name.Length == 1 && char.ToUpperInvariant(rows[i].Name[0]) == letter)
                    return rows[i];
            }
            return null;
        }
    }
}
