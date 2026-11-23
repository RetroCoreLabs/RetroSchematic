//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Junction dots: where a wire branches, a filled dot shows the branch is a real connection
    /// and not two wires that only cross.
    /// </summary>
    public static partial class SheetLayout
    {
        /// <summary>
        /// Adds a dot wherever one signal's wire segments meet three or more ways: three or more
        /// segment ends at one point, or a segment end lying on the middle of another segment of
        /// the same signal (a T). Wires of different signals that cross get no dot.
        /// </summary>
        /// <param name="d">
        /// The drawing, with its wires already routed.
        /// </param>
        internal static void AddJunctions(SchematicDrawing d)
        {
            // the wire segments of each signal
            Dictionary<string, List<DrawItem>> byNet = new(StringComparer.Ordinal);
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem it = d.Items[i];
                if (it.Kind != DrawKind.Line || it.Role != DrawRole.Wire)
                    continue;
                if (!byNet.TryGetValue(it.Owner, out List<DrawItem>? list))
                {
                    list = new List<DrawItem>();
                    byNet[it.Owner] = list;
                }
                list.Add(it);
            }
            List<DrawItem> dots = new();
            foreach (KeyValuePair<string, List<DrawItem>> kv in byNet)
            {
                List<DrawItem> segs = kv.Value;
                if (segs.Count < 2)
                    continue;
                // how many ways leave each point: a segment end counts one, a point in the
                // middle of a segment counts two
                Dictionary<(long, long), int> ways = new();
                Dictionary<(long, long), Confidence> sure = new();
                for (int i = 0; i < segs.Count; i++)
                {
                    AddWay(ways, sure, segs[i].X1, segs[i].Y1, segs[i].Confidence);
                    AddWay(ways, sure, segs[i].X2, segs[i].Y2, segs[i].Confidence);
                }
                List<(long, long)> ends = new(ways.Keys);
                for (int e = 0; e < ends.Count; e++)
                {
                    double x = ends[e].Item1 / 10.0, y = ends[e].Item2 / 10.0;
                    for (int i = 0; i < segs.Count; i++)
                    {
                        if (Inside(segs[i], x, y))
                            ways[ends[e]] += 2;
                    }
                }
                foreach (KeyValuePair<(long, long), int> w in ways)
                {
                    if (w.Value < 3)
                        continue;
                    dots.Add(new DrawItem
                    {
                        Kind = DrawKind.Circle, Role = DrawRole.Junction, Confidence = sure[w.Key], Owner = kv.Key,
                        X1 = w.Key.Item1 / 10.0, Y1 = w.Key.Item2 / 10.0, X2 = LineWidths.JunctionRadius,
                    });
                }
            }
            // in a fixed order, so the same sheet always gives the same file
            dots.Sort((a, b) => a.Owner != b.Owner ? string.CompareOrdinal(a.Owner, b.Owner) : a.X1 != b.X1 ? a.X1.CompareTo(b.X1) : a.Y1.CompareTo(b.Y1));
            for (int i = 0; i < dots.Count; i++)
                d.Items.Add(dots[i]);
        }

        private static void AddWay(Dictionary<(long, long), int> ways, Dictionary<(long, long), Confidence> sure, double x, double y, Confidence c)
        {
            // tenths of a unit: the layout snaps to whole units, so this is exact
            (long, long) key = ((long)Math.Round(x * 10), (long)Math.Round(y * 10));
            ways.TryGetValue(key, out int n);
            ways[key] = n + 1;
            // the dot is as sure as the least sure wire at it
            if (!sure.TryGetValue(key, out Confidence old) || c > old)
                sure[key] = c;
        }

        /// <summary>
        /// True when a point lies on a straight segment, not at either end.
        /// </summary>
        private static bool Inside(DrawItem s, double x, double y)
        {
            const double Eps = 0.05;
            if (Math.Abs(s.X1 - s.X2) < Eps && Math.Abs(x - s.X1) < Eps)
                return y > Math.Min(s.Y1, s.Y2) + Eps && y < Math.Max(s.Y1, s.Y2) - Eps;
            if (Math.Abs(s.Y1 - s.Y2) < Eps && Math.Abs(y - s.Y1) < Eps)
                return x > Math.Min(s.X1, s.X2) + Eps && x < Math.Max(s.X1, s.X2) - Eps;
            return false;
        }
    }
}
