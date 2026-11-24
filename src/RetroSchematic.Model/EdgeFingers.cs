//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;

namespace RetroSchematic.Model
{
    /// <summary>
    /// The gold fingers on a board type's backplane edge (catalog/boards/*.json,
    /// "edgeFingers"): how many on each side, how far apart, where the first one is, and which
    /// connector pin each finger is on each side. Positions are mm on the component side, x
    /// from the left edge, with the fingers at the top; the solder side has its fingers behind
    /// the component-side ones, so it is the same row seen mirrored.
    /// </summary>
    /// <remarks>
    /// For the NORD-10 card (worked out from the 2A50 photos, see
    /// catalog/connectors/nord10-io-card-backplane.json "fingers"): 50 per side, 2.54 mm apart,
    /// finger k from the left of the component side is pin 2k and the one behind it on the solder
    /// side is pin 2k+1.
    /// </remarks>
    public sealed class EdgeFingerLayout
    {
        /// <summary>
        /// The id of the connector file in catalog/connectors the pins belong to.
        /// </summary>
        public string? Connector { get; set; }

        /// <summary>
        /// Fingers on each side.
        /// </summary>
        public int PerSide { get; set; }

        /// <summary>
        /// Centre to centre, mm.
        /// </summary>
        public double PitchMm { get; set; }

        /// <summary>
        /// The centre of finger 0 (the leftmost on the component side), mm from the left edge.
        /// </summary>
        public double FirstCentreMm { get; set; }

        /// <summary>
        /// The width of one finger, mm.
        /// </summary>
        public double WidthMm { get; set; }

        /// <summary>
        /// The length of a finger from the board edge, mm.
        /// </summary>
        public double LengthMm { get; set; }

        /// <summary>
        /// The pin of finger 0 on the component side.
        /// </summary>
        public int ComponentFirstPin { get; set; }

        /// <summary>
        /// The pin of finger 0 on the solder side (the one behind component finger 0).
        /// </summary>
        public int SolderFirstPin { get; set; }

        /// <summary>
        /// How much the pin number grows from one finger to the next on one side.
        /// </summary>
        public int PinStep { get; set; } = 1;

        /// <summary>
        /// How sure the layout is.
        /// </summary>
        public Confidence Confidence { get; set; }

        /// <summary>
        /// Where it comes from.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// The pin of a finger.
        /// </summary>
        /// <param name="side">
        /// The side.
        /// </param>
        /// <param name="finger">
        /// The finger, 0 at the left of the component side.
        /// </param>
        /// <returns>
        /// The pin number.
        /// </returns>
        public int PinOf(BoardSide side, int finger)
            => (side == BoardSide.Component ? ComponentFirstPin : SolderFirstPin) + (finger * PinStep);

        /// <summary>
        /// Where a pin is: its side and finger.
        /// </summary>
        /// <param name="pin">
        /// The pin number.
        /// </param>
        /// <param name="side">
        /// The side it is on.
        /// </param>
        /// <param name="finger">
        /// The finger, 0 at the left of the component side.
        /// </param>
        /// <returns>
        /// False when no finger has that pin.
        /// </returns>
        public bool FingerOf(int pin, out BoardSide side, out int finger)
        {
            side = BoardSide.Component;
            finger = -1;
            for (int s = 0; s < 2; s++)
            {
                BoardSide bs = s == 0 ? BoardSide.Component : BoardSide.Solder;
                int first = bs == BoardSide.Component ? ComponentFirstPin : SolderFirstPin;
                int d = pin - first;
                if (PinStep == 0 || d < 0 || d % PinStep != 0 || d / PinStep >= PerSide)
                    continue;
                side = bs;
                finger = d / PinStep;
                return true;
            }
            return false;
        }

        /// <summary>
        /// The centre of a finger, mm from the left edge (component side).
        /// </summary>
        /// <param name="finger">
        /// The finger.
        /// </param>
        /// <param name="adjust">
        /// The sheet's own correction, or null.
        /// </param>
        /// <returns>
        /// The centre.
        /// </returns>
        public double CentreOf(int finger, FingerAdjust? adjust)
            => (adjust?.FirstCentreMm ?? FirstCentreMm) + (finger * (adjust?.PitchMm ?? PitchMm));
    }

    /// <summary>
    /// A sheet's own correction of where the fingers are on its photos (the sheet's
    /// "pcb.fingers"), set by dragging them in edit mode when the board type's numbers are off.
    /// </summary>
    public sealed class FingerAdjust
    {
        /// <summary>
        /// The centre of finger 0, mm from the left edge, or null for the board type's.
        /// </summary>
        public double? FirstCentreMm { get; set; }

        /// <summary>
        /// Centre to centre, mm, or null for the board type's.
        /// </summary>
        public double? PitchMm { get; set; }
    }

    /// <summary>
    /// What one connector pin carries, from the connector file's "pinMap" (every pin once).
    /// </summary>
    public sealed class ConnectorPinInfo
    {
        /// <summary>
        /// The pin number.
        /// </summary>
        public int Pin { get; set; }

        /// <summary>
        /// "component" or "solder": which side of the board its finger is on.
        /// </summary>
        public string? Side { get; set; }

        /// <summary>
        /// The signal's name as the backplane names it, or "(empty)".
        /// </summary>
        public string Signal { get; set; } = "";

        /// <summary>
        /// The group: address, data, power, modem (V.24), empty ...
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// Seen from the card: in, out, bidir, open ...
        /// </summary>
        public string? Direction { get; set; }

        /// <summary>
        /// low, high, none or open.
        /// </summary>
        public string? Active { get; set; }

        /// <summary>
        /// What it does, in words.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Where the facts come from.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// read, inferred or open.
        /// </summary>
        public string? Confidence { get; set; }

        /// <summary>
        /// Free text, such as what a photo shows.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// One gold finger as drawn on a photo: its pin, what it carries and where it is.
    /// </summary>
    public sealed class EdgeFinger
    {
        /// <summary>
        /// The side the finger is on.
        /// </summary>
        public BoardSide Side { get; init; }

        /// <summary>
        /// The finger, 0 at the left of the component side.
        /// </summary>
        public int Index { get; init; }

        /// <summary>
        /// The connector pin.
        /// </summary>
        public int Pin { get; init; }

        /// <summary>
        /// The signal name shown: the sheet's name when the card uses the pin, else the
        /// catalogue's, else "(empty)".
        /// </summary>
        public string Signal { get; init; } = "";

        /// <summary>
        /// The owner name of the sheet's signal on the pin ("net:BA0_0"), or null when the sheet
        /// does not use the pin.
        /// </summary>
        public string? NetOwner { get; init; }

        /// <summary>
        /// The catalogue's facts on the pin, or null.
        /// </summary>
        public ConnectorPinInfo? Info { get; init; }

        /// <summary>
        /// The four corners in mm on the component side, clockwise from the top left.
        /// </summary>
        public GridPoint[] BoardCorners { get; init; } = Array.Empty<GridPoint>();

        /// <summary>
        /// The four corners in the photo, pixels.
        /// </summary>
        public GridPoint[] Corners { get; init; } = Array.Empty<GridPoint>();

        /// <summary>
        /// The text shown on hover: pin, signal, direction and what it does.
        /// </summary>
        public string Label
        {
            get
            {
                string s = "Pin " + Pin.ToString(System.Globalization.CultureInfo.InvariantCulture) + " (" + (Side == BoardSide.Component ? "component" : "solder") + " side)  " + Signal;
                if (Info != null)
                {
                    if (!string.IsNullOrEmpty(Info.Direction) && Info.Direction != "open")
                        s += ", " + Info.Direction;
                    if (!string.IsNullOrEmpty(Info.Active) && Info.Active != "open" && Info.Active != "none")
                        s += ", active " + Info.Active;
                    if (!string.IsNullOrEmpty(Info.Description) && Info.Description != "open")
                        s += ". " + Info.Description;
                    if (!string.IsNullOrEmpty(Info.Note))
                        s += " " + Info.Note;
                }
                if (NetOwner == null)
                    s += " (not used on this card)";
                return s;
            }
        }
    }

    /// <summary>
    /// Builds the gold fingers of a photo and finds the finger under a point.
    /// </summary>
    public static class EdgeFingerMap
    {
        /// <summary>
        /// Every finger of one side, placed on the photo.
        /// </summary>
        /// <param name="layout">
        /// The board type's finger layout.
        /// </param>
        /// <param name="adjust">
        /// The sheet's own correction, or null.
        /// </param>
        /// <param name="size">
        /// The board size.
        /// </param>
        /// <param name="corners">
        /// The board's corners in the photo (top left, top right, bottom right, bottom left).
        /// </param>
        /// <param name="side">
        /// The side the photo shows; the solder side is drawn mirrored.
        /// </param>
        /// <param name="names">
        /// The sheet's signal name and owner per pin (pin to name, owner), or null.
        /// </param>
        /// <param name="pins">
        /// The catalogue's facts per pin, or null.
        /// </param>
        /// <returns>
        /// The fingers, finger 0 first.
        /// </returns>
        public static List<EdgeFinger> Build(EdgeFingerLayout layout, FingerAdjust? adjust, BoardSize size, IReadOnlyList<GridPoint> corners, BoardSide side,
            IReadOnlyDictionary<int, (string Name, string Owner)>? names, IReadOnlyDictionary<int, ConnectorPinInfo>? pins)
        {
            List<EdgeFinger> list = new(layout.PerSide);
            double hw = Math.Max(0.2, layout.WidthMm / 2), len = Math.Max(1, layout.LengthMm);
            for (int k = 0; k < layout.PerSide; k++)
            {
                int pin = layout.PinOf(side, k);
                double cx = layout.CentreOf(k, adjust);
                GridPoint[] mm = { new(cx - hw, 0), new(cx + hw, 0), new(cx + hw, len), new(cx - hw, len) };
                GridPoint[] px = new GridPoint[4];
                for (int i = 0; i < 4; i++)
                    px[i] = PcbGrid.ToPhoto(corners, size, side == BoardSide.Solder, mm[i]);
                ConnectorPinInfo? info = null;
                pins?.TryGetValue(pin, out info);
                string signal;
                string? owner = null;
                if (names != null && names.TryGetValue(pin, out (string Name, string Owner) n))
                {
                    signal = n.Name;
                    owner = n.Owner;
                }
                else
                {
                    signal = info?.Signal ?? "(empty)";
                }
                list.Add(new EdgeFinger { Side = side, Index = k, Pin = pin, Signal = signal, NetOwner = owner, Info = info, BoardCorners = mm, Corners = px });
            }
            return list;
        }

        /// <summary>
        /// The finger under a photo point, or null. The whole space between two fingers counts
        /// for the nearer one, so a finger is easy to hit at a normal zoom.
        /// </summary>
        /// <param name="fingers">
        /// The fingers.
        /// </param>
        /// <param name="p">
        /// The point, photo pixels.
        /// </param>
        /// <returns>
        /// The finger, or null.
        /// </returns>
        public static EdgeFinger? At(List<EdgeFinger> fingers, GridPoint p)
        {
            for (int i = 0; i < fingers.Count; i++)
            {
                if (PcbGrid.Inside(Widened(fingers, i), p))
                    return fingers[i];
            }
            return null;
        }

        /// <summary>
        /// The pin of every finger on a side, finger 0 first (for checks and tools).
        /// </summary>
        /// <param name="layout">
        /// The layout.
        /// </param>
        /// <param name="side">
        /// The side.
        /// </param>
        /// <returns>
        /// The pins.
        /// </returns>
        public static int[] PinsOf(EdgeFingerLayout layout, BoardSide side)
        {
            int[] pins = new int[layout.PerSide];
            for (int k = 0; k < pins.Length; k++)
                pins[k] = layout.PinOf(side, k);
            return pins;
        }

        // a finger's box made as wide as the gap to its neighbours (half the gap each way)
        private static GridPoint[] Widened(List<EdgeFinger> f, int i)
        {
            GridPoint[] c = f[i].Corners;
            GridPoint[] left = i > 0 ? f[i - 1].Corners : c;
            GridPoint[] right = i + 1 < f.Count ? f[i + 1].Corners : c;
            // corners: 0 top left, 1 top right, 2 bottom right, 3 bottom left (in board terms);
            // the neighbour before has the lower pin; on the solder photo it sits to the right,
            // which the midpoints follow either way
            GridPoint Mid(GridPoint a, GridPoint b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            GridPoint tl = i > 0 ? Mid(left[1], c[0]) : Extend(c[0], c[1], -0.5);
            GridPoint bl = i > 0 ? Mid(left[2], c[3]) : Extend(c[3], c[2], -0.5);
            GridPoint tr = i + 1 < f.Count ? Mid(c[1], right[0]) : Extend(c[1], c[0], -0.5);
            GridPoint br = i + 1 < f.Count ? Mid(c[2], right[3]) : Extend(c[2], c[3], -0.5);
            return new[] { tl, tr, br, bl };
        }

        // a point moved along the line from a towards b by a share of the distance (negative: away)
        private static GridPoint Extend(GridPoint a, GridPoint b, double share)
            => new(a.X + ((b.X - a.X) * share), a.Y + ((b.Y - a.Y) * share));
    }
}
