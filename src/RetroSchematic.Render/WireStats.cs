//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Two numbers that say how tidy a drawing's wires are: their total length and how many
    /// times a wire of one signal crosses a wire of another. Used to compare layouts before and
    /// after a change (lower is tidier for both).
    /// </summary>
    public readonly struct WireStats
    {
        /// <summary>
        /// The length of every wire piece added up, in drawing units (6 units = one grid step).
        /// </summary>
        public double Length { get; }

        /// <summary>
        /// How many places a wire of one signal crosses a wire of another signal. Two pieces that
        /// only touch at an end do not count.
        /// </summary>
        public int Crossings { get; }

        /// <summary>
        /// How many wire pieces there are.
        /// </summary>
        public int Pieces { get; }

        /// <summary>
        /// Creates the numbers.
        /// </summary>
        /// <param name="length">
        /// Total wire length.
        /// </param>
        /// <param name="crossings">
        /// Crossings between different signals.
        /// </param>
        /// <param name="pieces">
        /// Number of wire pieces.
        /// </param>
        public WireStats(double length, int crossings, int pieces)
        {
            Length = length;
            Crossings = crossings;
            Pieces = pieces;
        }

        /// <summary>
        /// Measures a drawing: every <see cref="DrawRole.Wire"/> line counts; bus lines, pin lines
        /// and labels do not.
        /// </summary>
        /// <param name="d">
        /// The drawing.
        /// </param>
        /// <returns>
        /// Its numbers.
        /// </returns>
        public static WireStats Measure(SchematicDrawing d)
        {
            List<DrawItem> wires = new();
            double length = 0;
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem it = d.Items[i];
                if (it.Kind != DrawKind.Line || it.Role != DrawRole.Wire)
                    continue;
                wires.Add(it);
                double dx = it.X2 - it.X1, dy = it.Y2 - it.Y1;
                length += Math.Sqrt((dx * dx) + (dy * dy));
            }
            int crossings = 0;
            for (int i = 0; i < wires.Count; i++)
            {
                for (int j = i + 1; j < wires.Count; j++)
                {
                    if (wires[i].Owner != wires[j].Owner && Cross(wires[i], wires[j]))
                        crossings++;
                }
            }
            return new WireStats(length, crossings, wires.Count);
        }

        /// <summary>
        /// True when two pieces cross inside both of them (not where one only touches the other's end).
        /// </summary>
        private static bool Cross(DrawItem a, DrawItem b)
        {
            double d1 = Side(b.X1, b.Y1, b.X2, b.Y2, a.X1, a.Y1);
            double d2 = Side(b.X1, b.Y1, b.X2, b.Y2, a.X2, a.Y2);
            double d3 = Side(a.X1, a.Y1, a.X2, a.Y2, b.X1, b.Y1);
            double d4 = Side(a.X1, a.Y1, a.X2, a.Y2, b.X2, b.Y2);
            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        /// <summary>
        /// Which side of the line through (x1,y1)-(x2,y2) the point (x,y) is on: the sign of the
        /// cross product; 0 when on the line.
        /// </summary>
        private static double Side(double x1, double y1, double x2, double y2, double x, double y)
            => ((x2 - x1) * (y - y1)) - ((y2 - y1) * (x - x1));
    }
}
