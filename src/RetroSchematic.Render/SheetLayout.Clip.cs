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
    /// The one rule for lines and circles: a line never runs through a circle (a pin-end ring,
    /// a junction dot, an inversion bubble or anything else round). Every line is cut where it
    /// meets a circle's edge, so it stops there on each side. Done once, after everything is
    /// drawn, for every kind of line, so the window and the export both get it.
    /// </summary>
    public static partial class SheetLayout
    {
        /// <summary>
        /// Lines shorter than this after cutting are dropped: they would be a dot of ink.
        /// </summary>
        private const double ShortestPiece = 0.05;

        /// <summary>
        /// Cuts every straight line where it enters and leaves a circle, keeping only the parts
        /// outside every circle. A line that only touches a circle's edge is left as it is.
        /// </summary>
        /// <param name="d">
        /// The finished drawing.
        /// </param>
        internal static void ClipAtCircles(SchematicDrawing d)
        {
            List<DrawItem> circles = new();
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Kind == DrawKind.Circle)
                    circles.Add(d.Items[i]);
            }
            if (circles.Count == 0)
                return;
            List<DrawItem> output = new(d.Items.Count);
            List<double[]> pieces = new();
            List<double[]> next = new();
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem it = d.Items[i];
                if (it.Kind != DrawKind.Line)
                {
                    output.Add(it);
                    continue;
                }
                pieces.Clear();
                pieces.Add(new[] { it.X1, it.Y1, it.X2, it.Y2 });
                bool cut = false;
                for (int c = 0; c < circles.Count && pieces.Count > 0; c++)
                {
                    next.Clear();
                    for (int p = 0; p < pieces.Count; p++)
                        cut |= Outside(pieces[p], circles[c].X1, circles[c].Y1, circles[c].X2, next);
                    pieces.Clear();
                    pieces.AddRange(next);
                }
                if (!cut)
                {
                    output.Add(it);
                    continue;
                }
                for (int p = 0; p < pieces.Count; p++)
                {
                    double[] g = pieces[p];
                    if (Math.Abs(g[2] - g[0]) + Math.Abs(g[3] - g[1]) < ShortestPiece)
                        continue;
                    DrawItem copy = it.Clone();
                    copy.X1 = g[0];
                    copy.Y1 = g[1];
                    copy.X2 = g[2];
                    copy.Y2 = g[3];
                    output.Add(copy);
                }
            }
            d.Items.Clear();
            d.Items.AddRange(output);
        }

        /// <summary>
        /// True when a straight piece goes into the inside of a circle (not just touching it).
        /// </summary>
        /// <param name="x1">Start X.</param>
        /// <param name="y1">Start Y.</param>
        /// <param name="x2">End X.</param>
        /// <param name="y2">End Y.</param>
        /// <param name="cx">Circle centre X.</param>
        /// <param name="cy">Circle centre Y.</param>
        /// <param name="r">Circle radius.</param>
        /// <returns>True when part of the piece lies inside the circle.</returns>
        public static bool EntersCircle(double x1, double y1, double x2, double y2, double cx, double cy, double r)
        {
            if (!Inside(x1, y1, x2, y2, cx, cy, r, out double t0, out double t1))
                return false;
            return t1 - t0 > 1e-6;
        }

        /// <summary>
        /// Adds to <paramref name="keep"/> the parts of a piece outside a circle.
        /// </summary>
        /// <returns>True when the piece was cut.</returns>
        private static bool Outside(double[] g, double cx, double cy, double r, List<double[]> keep)
        {
            if (!Inside(g[0], g[1], g[2], g[3], cx, cy, r, out double t0, out double t1) || t1 - t0 <= 1e-6)
            {
                keep.Add(g);
                return false;
            }
            double dx = g[2] - g[0], dy = g[3] - g[1];
            if (t0 > 0)
                keep.Add(new[] { g[0], g[1], g[0] + (dx * t0), g[1] + (dy * t0) });
            if (t1 < 1)
                keep.Add(new[] { g[0] + (dx * t1), g[1] + (dy * t1), g[2], g[3] });
            return true;
        }

        /// <summary>
        /// Where along a piece (0 = start, 1 = end) it is inside a circle: from t0 to t1.
        /// </summary>
        /// <returns>False when no part is inside.</returns>
        private static bool Inside(double x1, double y1, double x2, double y2, double cx, double cy, double r, out double t0, out double t1)
        {
            t0 = t1 = 0;
            double dx = x2 - x1, dy = y2 - y1;
            double fx = x1 - cx, fy = y1 - cy;
            double a = (dx * dx) + (dy * dy);
            double c = (fx * fx) + (fy * fy) - (r * r);
            if (a < 1e-12)
                return c < -1e-9;
            double b = 2 * ((fx * dx) + (fy * dy));
            double disc = (b * b) - (4 * a * c);
            if (disc <= 0)
                return false;
            double root = Math.Sqrt(disc);
            t0 = Math.Max(0, (-b - root) / (2 * a));
            t1 = Math.Min(1, (-b + root) / (2 * a));
            return t1 > t0;
        }
    }
}
