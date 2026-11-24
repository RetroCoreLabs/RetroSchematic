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
    /// The chip grid on the board photos (the sheet's "pcb" section and each photo's corners).
    /// Every change needs edit mode and can be undone.
    /// </summary>
    public sealed partial class SheetEditor
    {
        /// <summary>
        /// Sets the pcb section (board type, rows, boxes), or null to remove it.
        /// </summary>
        /// <param name="pcb">
        /// The section.
        /// </param>
        public void SetPcb(PcbLayout? pcb)
        {
            CheckWritable();
            for (int i = 0; pcb?.Rows != null && i < pcb.Rows.Count; i++)
            {
                if (pcb.Rows[i].ToMm <= pcb.Rows[i].FromMm)
                    throw new SheetEditException("Row " + pcb.Rows[i].Name + " ends before it starts.");
            }
            Snapshot();
            Root.Pcb = pcb;
            Done();
        }

        /// <summary>
        /// Sets a chip's box by hand (it then wins over the grid cell), replacing an older one.
        /// </summary>
        /// <param name="box">
        /// The box, mm on the component side.
        /// </param>
        public void SetChipBox(ChipBox box)
        {
            CheckWritable();
            if (string.IsNullOrWhiteSpace(box.Ref))
                throw new SheetEditException("A chip box needs the part's position, such as 13D.");
            if (box.WidthMm <= 0 || box.HeightMm <= 0)
                throw new SheetEditException("A chip box needs a width and a height above 0.");
            Snapshot();
            Root.Pcb ??= new PcbLayout();
            Root.Pcb.Boxes ??= new List<ChipBox>();
            int at = IndexOfBox(box.Ref);
            if (at < 0)
                Root.Pcb.Boxes.Add(box);
            else
                Root.Pcb.Boxes[at] = box;
            Done();
        }

        /// <summary>
        /// Removes a chip's hand-set box; the grid cell is used again.
        /// </summary>
        /// <param name="reference">
        /// The part's position.
        /// </param>
        public void RemoveChipBox(string reference)
        {
            CheckWritable();
            int at = IndexOfBox(reference);
            if (at < 0)
                throw new SheetEditException("Part " + reference + " has no box set by hand.");
            Snapshot();
            Root.Pcb!.Boxes!.RemoveAt(at);
            if (Root.Pcb.Boxes.Count == 0)
                Root.Pcb.Boxes = null;
            Done();
        }

        /// <summary>
        /// Sets where the board's four corners are in a photo, or null to let the viewer find
        /// them again.
        /// </summary>
        /// <param name="index">
        /// The photo, 0 first.
        /// </param>
        /// <param name="corners">
        /// Top left, top right, bottom right, bottom left, in the photo's pixels; or null.
        /// </param>
        public void SetPhotoCorners(int index, IReadOnlyList<GridPoint>? corners)
        {
            CheckWritable();
            if (Root.Photos == null || index < 0 || index >= Root.Photos.Count)
                throw new SheetEditException("There is no photo " + (index + 1) + ".");
            if (corners != null && corners.Count != 4)
                throw new SheetEditException("A board outline has four corners.");
            Snapshot();
            if (corners == null)
            {
                Root.Photos[index].Corners = null;
            }
            else
            {
                List<PhotoPoint> list = new();
                for (int i = 0; i < 4; i++)
                    list.Add(new PhotoPoint { X = Math.Round(corners[i].X, 1), Y = Math.Round(corners[i].Y, 1) });
                Root.Photos[index].Corners = list;
            }
            Done();
        }

        /// <summary>
        /// Sets this sheet's correction of where the gold fingers are (finger 0's centre and the
        /// spacing, mm), or null to use the board type's numbers again.
        /// </summary>
        /// <param name="adjust">
        /// The correction, or null.
        /// </param>
        public void SetFingerAdjust(FingerAdjust? adjust)
        {
            CheckWritable();
            if (adjust?.PitchMm is double p && p <= 0)
                throw new SheetEditException("The finger spacing must be more than 0 mm.");
            Snapshot();
            if (adjust == null || (adjust.FirstCentreMm == null && adjust.PitchMm == null))
            {
                if (Root.Pcb != null)
                    Root.Pcb.Fingers = null;
            }
            else
            {
                Root.Pcb ??= new PcbLayout();
                Root.Pcb.Fingers = adjust;
            }
            Done();
        }

        private int IndexOfBox(string reference)
        {
            for (int i = 0; Root.Pcb?.Boxes != null && i < Root.Pcb.Boxes.Count; i++)
            {
                if (string.Equals(Root.Pcb.Boxes[i].Ref, reference, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }
    }
}
