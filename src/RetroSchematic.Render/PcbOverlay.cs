//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// One chip's box on a board photo, in the photo's pixels.
    /// </summary>
    public sealed class PcbBox
    {
        /// <summary>
        /// The part reference (position code), such as "13D".
        /// </summary>
        public string Ref { get; init; } = "";

        /// <summary>
        /// The owner name the drawing uses for it ("part:13D"), or "switch:15E" for a switch.
        /// </summary>
        public string Owner { get; init; } = "";

        /// <summary>
        /// The chip type, such as "7442", or "switch".
        /// </summary>
        public string? Type { get; init; }

        /// <summary>
        /// What it does, from the sheet.
        /// </summary>
        public string? Function { get; init; }

        /// <summary>
        /// The four corners in the photo, pixels, in order round the box.
        /// </summary>
        public GridPoint[] Corners { get; init; } = System.Array.Empty<GridPoint>();

        /// <summary>
        /// The corners in mm on the component side (before the photo mapping).
        /// </summary>
        public GridPoint[] BoardCorners { get; init; } = System.Array.Empty<GridPoint>();

        /// <summary>
        /// True when the box was set by hand, false when it is the grid cell.
        /// </summary>
        public bool ByHand { get; init; }

        /// <summary>
        /// The text shown on hover: position, type and function.
        /// </summary>
        public string Label => Ref + "  " + (Type ?? "?") + (Function == null ? "" : "  " + Function);
    }

    /// <summary>
    /// Builds the chip boxes of a board photo from the board type's grid, the sheet's rows and
    /// hand-set boxes, and the board's corners in the photo; and finds the chip under a point.
    /// </summary>
    public static class PcbOverlay
    {
        /// <summary>
        /// Every chip and switch block of the sheet that has an area, as boxes in the photo.
        /// </summary>
        /// <param name="root">
        /// The sheet (its top level).
        /// </param>
        /// <param name="template">
        /// The board type.
        /// </param>
        /// <param name="corners">
        /// The board's corners in the photo.
        /// </param>
        /// <param name="solder">
        /// True for a solder-side photo.
        /// </param>
        /// <returns>
        /// The boxes.
        /// </returns>
        public static List<PcbBox> Build(Netlist root, BoardTemplate template, IReadOnlyList<GridPoint> corners, bool solder)
        {
            List<PcbBox> boxes = new();
            for (int i = 0; i < root.Parts.Count; i++)
            {
                Part p = root.Parts[i];
                Add(boxes, root, template, corners, solder, p.Ref, "part:" + p.Ref, p.Type, p.Function);
            }
            for (int i = 0; root.Switches != null && i < root.Switches.Count; i++)
            {
                SwitchBlock s = root.Switches[i];
                Add(boxes, root, template, corners, solder, s.Ref, "switch:" + s.Ref, "switch", s.Function);
            }
            return boxes;
        }

        /// <summary>
        /// The box under a photo point: the smallest that holds it (a small chip inside a big
        /// hand-set box wins), or null.
        /// </summary>
        /// <param name="boxes">
        /// The boxes.
        /// </param>
        /// <param name="p">
        /// The point, photo pixels.
        /// </param>
        /// <returns>
        /// The box, or null.
        /// </returns>
        public static PcbBox? At(List<PcbBox> boxes, GridPoint p)
        {
            PcbBox? best = null;
            double bestArea = double.MaxValue;
            for (int i = 0; i < boxes.Count; i++)
            {
                if (!PcbGrid.Inside(boxes[i].Corners, p))
                    continue;
                double a = PcbGrid.AreaOf(boxes[i].Corners);
                if (a < bestArea)
                {
                    best = boxes[i];
                    bestArea = a;
                }
            }
            return best;
        }

        private static void Add(List<PcbBox> boxes, Netlist root, BoardTemplate template, IReadOnlyList<GridPoint> corners, bool solder, string reference, string owner, string? type, string? function)
        {
            if (!PcbGrid.Area(template, root.Pcb, reference, out GridPoint[] mm, out bool byHand))
                return;
            GridPoint[] px = new GridPoint[mm.Length];
            for (int i = 0; i < mm.Length; i++)
                px[i] = PcbGrid.ToPhoto(corners, template.Size, solder, mm[i]);
            boxes.Add(new PcbBox { Ref = reference, Owner = owner, Type = type, Function = function, Corners = px, BoardCorners = mm, ByHand = byHand });
        }
    }
}
