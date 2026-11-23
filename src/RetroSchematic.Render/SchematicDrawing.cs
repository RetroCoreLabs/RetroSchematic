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
    /// What a drawing step shows. Every output (SVG, the window) picks the colour from the role,
    /// so the layout code never names a colour and every theme can recolour the whole sheet.
    /// </summary>
    public enum DrawRole
    {
        /// <summary>
        /// The sheet title line.
        /// </summary>
        Title,

        /// <summary>
        /// The frame around a chip at its board position.
        /// </summary>
        PartOutline,

        /// <summary>
        /// The chip's position code and type ("11D 7402").
        /// </summary>
        PartLabel,

        /// <summary>
        /// A gate symbol's own lines.
        /// </summary>
        Symbol,

        /// <summary>
        /// A pin line from a symbol out to its signal.
        /// </summary>
        PinLine,

        /// <summary>
        /// A small pin number beside a pin line.
        /// </summary>
        PinNumber,

        /// <summary>
        /// A signal name at a pin.
        /// </summary>
        SignalName,

        /// <summary>
        /// Lower-priority text such as a chip's function.
        /// </summary>
        Note,

        /// <summary>
        /// A routed wire between two pins of one signal.
        /// </summary>
        Wire,

        /// <summary>
        /// The small dot at the end of a pin line, where a wire starts; picks the pin itself.
        /// </summary>
        PinDot,

        /// <summary>
        /// The ND net-label diamond at the end of a pin's short stub, in front of the signal name:
        /// the signal goes on elsewhere on the sheet under that name, without a drawn wire.
        /// </summary>
        NetLabel,

        /// <summary>
        /// A datasheet pin name written inside a chip drawn as a box of pins.
        /// </summary>
        PinName,

        /// <summary>
        /// A virtual bus line and its name: drawn thicker than a wire, thinner than a chip frame.
        /// </summary>
        Bus,
    }

    /// <summary>
    /// Line widths shared by the window and the SVG output, so both draw the sheet the same way.
    /// Chip frames and gate symbols are drawn clearly thicker than signal lines, so the parts
    /// stand out from the wires.
    /// </summary>
    public static class LineWidths
    {
        /// <summary>
        /// A wire, a pin line, a net-label diamond.
        /// </summary>
        public const double Wire = 1;

        /// <summary>
        /// The outline of a chip frame or a gate symbol: 2.25 times a wire.
        /// </summary>
        public const double Outline = 2.25;

        /// <summary>
        /// A virtual bus line: between a wire and a chip outline.
        /// </summary>
        public const double Bus = 1.75;

        /// <summary>
        /// The width an item is drawn with.
        /// </summary>
        /// <param name="it">
        /// The drawing step.
        /// </param>
        /// <returns>
        /// Its line width in drawing units.
        /// </returns>
        public static double Of(DrawItem it)
            => it.Role == DrawRole.Symbol || it.Role == DrawRole.PartOutline ? Outline : it.Role == DrawRole.Bus ? Bus : Wire;
    }

    /// <summary>
    /// The kind of one drawing step.
    /// </summary>
    public enum DrawKind
    {
        /// <summary>
        /// Straight line from (X1,Y1) to (X2,Y2).
        /// </summary>
        Line,

        /// <summary>
        /// Rectangle at (X1,Y1) with width X2 and height Y2.
        /// </summary>
        Rect,

        /// <summary>
        /// Circle centred on (X1,Y1) with radius X2.
        /// </summary>
        Circle,

        /// <summary>
        /// Outline built from <see cref="DrawItem.Path"/>.
        /// </summary>
        Path,

        /// <summary>
        /// Text with its baseline at (X1,Y1).
        /// </summary>
        Text,

        /// <summary>
        /// Starts a named group (a part or a gate); <see cref="DrawItem.Owner"/> names it.
        /// </summary>
        GroupStart,

        /// <summary>
        /// Ends the most recent group.
        /// </summary>
        GroupEnd,
    }

    /// <summary>
    /// Where text sits relative to its anchor point.
    /// </summary>
    public enum TextAnchor
    {
        /// <summary>
        /// Text starts at the point.
        /// </summary>
        Start,

        /// <summary>
        /// Text is centred on the point.
        /// </summary>
        Middle,

        /// <summary>
        /// Text ends at the point.
        /// </summary>
        End,
    }

    /// <summary>
    /// One step of an outline path.
    /// </summary>
    public enum PathOp
    {
        /// <summary>
        /// Start a new figure at (X,Y).
        /// </summary>
        MoveTo,

        /// <summary>
        /// Straight line to (X,Y).
        /// </summary>
        LineTo,

        /// <summary>
        /// Quadratic curve with control point (CX,CY) ending at (X,Y).
        /// </summary>
        QuadTo,

        /// <summary>
        /// Circular arc of radius R ending at (X,Y), turning clockwise.
        /// </summary>
        ArcClockwiseTo,

        /// <summary>
        /// Close the figure back to its start.
        /// </summary>
        Close,
    }

    /// <summary>
    /// One step of a path outline.
    /// </summary>
    public readonly struct PathSegment
    {
        /// <summary>
        /// What the step does.
        /// </summary>
        public readonly PathOp Op;

        /// <summary>
        /// End point X.
        /// </summary>
        public readonly double X;

        /// <summary>
        /// End point Y.
        /// </summary>
        public readonly double Y;

        /// <summary>
        /// Curve control point X (QuadTo) or the radius (ArcClockwiseTo).
        /// </summary>
        public readonly double CX;

        /// <summary>
        /// Curve control point Y (QuadTo).
        /// </summary>
        public readonly double CY;

        /// <summary>
        /// Creates a path step.
        /// </summary>
        /// <param name="op">
        /// What the step does.
        /// </param>
        /// <param name="x">
        /// End point X.
        /// </param>
        /// <param name="y">
        /// End point Y.
        /// </param>
        /// <param name="cx">
        /// Curve control point X, or the arc radius.
        /// </param>
        /// <param name="cy">
        /// Curve control point Y.
        /// </param>
        public PathSegment(PathOp op, double x, double y, double cx = 0, double cy = 0)
        {
            Op = op;
            X = x;
            Y = y;
            CX = cx;
            CY = cy;
        }
    }

    /// <summary>
    /// A point in drawing units.
    /// </summary>
    public readonly struct DrawPoint
    {
        /// <summary>
        /// X in drawing units.
        /// </summary>
        public readonly double X;

        /// <summary>
        /// Y in drawing units.
        /// </summary>
        public readonly double Y;

        /// <summary>
        /// Creates a point.
        /// </summary>
        /// <param name="x">
        /// X in drawing units.
        /// </param>
        /// <param name="y">
        /// Y in drawing units.
        /// </param>
        public DrawPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// The frame round one chip, in drawing units.
    /// </summary>
    public readonly struct PartFrame
    {
        /// <summary>
        /// Left edge.
        /// </summary>
        public readonly double X;

        /// <summary>
        /// Top edge.
        /// </summary>
        public readonly double Y;

        /// <summary>
        /// Width.
        /// </summary>
        public readonly double Width;

        /// <summary>
        /// Height.
        /// </summary>
        public readonly double Height;

        /// <summary>
        /// Creates a frame.
        /// </summary>
        /// <param name="x">
        /// Left edge.
        /// </param>
        /// <param name="y">
        /// Top edge.
        /// </param>
        /// <param name="width">
        /// Width.
        /// </param>
        /// <param name="height">
        /// Height.
        /// </param>
        public PartFrame(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// One drawing step: a shape or a piece of text, with its role, how sure the reading is, and
    /// which part, gate or signal it belongs to (used to pick it with the mouse).
    /// </summary>
    public sealed class DrawItem
    {
        /// <summary>
        /// What kind of step this is.
        /// </summary>
        public DrawKind Kind;

        /// <summary>
        /// What it shows; chooses the colour.
        /// </summary>
        public DrawRole Role;

        /// <summary>
        /// How sure the reading behind it is. Inferred items are drawn in their own colour.
        /// </summary>
        public Confidence Confidence;

        /// <summary>
        /// First coordinate pair, see <see cref="DrawKind"/> for the meaning per kind.
        /// </summary>
        public double X1, Y1;

        /// <summary>
        /// Second coordinate pair, see <see cref="DrawKind"/> for the meaning per kind.
        /// </summary>
        public double X2, Y2;

        /// <summary>
        /// Dashed outline (a part whose type is not proved).
        /// </summary>
        public bool Dashed;

        /// <summary>
        /// The path steps for <see cref="DrawKind.Path"/>.
        /// </summary>
        public PathSegment[]? Path;

        /// <summary>
        /// The text for <see cref="DrawKind.Text"/>.
        /// </summary>
        public string Text = "";

        /// <summary>
        /// Text written after <see cref="Text"/> as a lowered, smaller subscript - ND's 1 (active
        /// high) or 0 (active low) on a signal name. Empty for none.
        /// </summary>
        public string Subscript = "";

        /// <summary>
        /// Font size in drawing units.
        /// </summary>
        public double FontSize = 10;

        /// <summary>
        /// Bold text.
        /// </summary>
        public bool Bold;

        /// <summary>
        /// Where the text sits relative to (X1,Y1).
        /// </summary>
        public TextAnchor Anchor;

        /// <summary>
        /// What this step belongs to, written as "part:11D", "gate:11D/1", "net:BD0" or "pin:11D.3". Empty when
        /// it belongs to nothing that can be picked (the title).
        /// </summary>
        public string Owner = "";
    }

    /// <summary>
    /// A whole sheet as a list of drawing steps, in drawing units. Built once by
    /// <see cref="SheetLayout"/>; written out by <see cref="SvgRenderer"/> and drawn on screen by the
    /// desktop viewer, so the two always show the same thing.
    /// </summary>
    public sealed class SchematicDrawing
    {
        /// <summary>
        /// Creates an empty drawing of the given size.
        /// </summary>
        /// <param name="width">
        /// Width in drawing units.
        /// </param>
        /// <param name="height">
        /// Height in drawing units.
        /// </param>
        public SchematicDrawing(double width, double height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Width in drawing units.
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// Height in drawing units.
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// The steps, in drawing order.
        /// </summary>
        public List<DrawItem> Items { get; } = new();

        /// <summary>
        /// Where each drawn pin ends ("11D.3"), which is where its wires start. Pins that are not
        /// drawn (backplane P., front F., or a chip with no catalogue file) are not in it.
        /// </summary>
        public Dictionary<string, DrawPoint> Pins { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// The frame of each chip, by part reference.
        /// </summary>
        public Dictionary<string, PartFrame> Frames { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Every wired signal's branches as laid (bend points in drawing units), so the editor can
        /// fix a wire's current path into the sheet's layout section.
        /// </summary>
        public Dictionary<string, List<List<DrawPoint>>> WireBranches { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Signals whose stored wire path was kept as it was.
        /// </summary>
        public List<string> KeptWires { get; } = new();

        /// <summary>
        /// Signals whose stored wire path broke a routing rule (most often: a chip was moved onto
        /// it) and was dropped; each got a fresh route, or labels.
        /// </summary>
        public List<string> Rerouted { get; } = new();

        /// <summary>
        /// For each signal in <see cref="Rerouted"/>, why its stored path was dropped ("CLK: branch
        /// 2 runs through a chip ... at (300, 120)").
        /// </summary>
        public List<string> RerouteReasons { get; } = new();

        /// <summary>
        /// Every drawn bus's path as laid (bend points in drawing units), by bus name.
        /// </summary>
        public Dictionary<string, List<DrawPoint>> BusPaths { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// What went wrong with a bus, in plain words: no path yet, a stored path through a chip
        /// that was routed afresh, a member pin that could not reach the bus (it keeps its label).
        /// </summary>
        public List<string> BusProblems { get; } = new();

        /// <summary>
        /// The owner ("part:11D", "gate:11D/1", "net:BD0", "pin:11D.3") of what lies under a point,
        /// or an empty string. The smaller, deliberate target wins: a pin dot, then a signal name
        /// or part label, then a gate, then a wire, then the rest of the part frame. A wire that
        /// runs across a label therefore does not hide the label.
        /// </summary>
        /// <param name="x">
        /// Point X in drawing units.
        /// </param>
        /// <param name="y">
        /// Point Y in drawing units.
        /// </param>
        /// <returns>
        /// The owner of the item under the point, or an empty string.
        /// </returns>
        public string HitTest(double x, double y)
        {
            string best = "";
            int bestRank = int.MaxValue;
            for (int i = 0; i < Items.Count; i++)
            {
                DrawItem it = Items[i];
                if (it.Owner.Length == 0 || it.Kind == DrawKind.GroupStart || it.Kind == DrawKind.GroupEnd)
                    continue;
                Bounds(it, out double left, out double top, out double right, out double bottom);
                if (x < left || x > right || y < top || y > bottom)
                    continue;
                int rank = Rank(it);
                if (rank < bestRank)
                {
                    bestRank = rank;
                    best = it.Owner;
                }
            }
            return best;
        }

        /// <summary>
        /// How strongly an item claims a click; lower wins. See <see cref="HitTest"/>.
        /// </summary>
        /// <param name="it">
        /// The drawing step.
        /// </param>
        /// <returns>
        /// 0 (pin dot) to 4 (part frame).
        /// </returns>
        private static int Rank(DrawItem it)
        {
            if (it.Role == DrawRole.PinDot)
                return 0;
            if (it.Role == DrawRole.SignalName || it.Role == DrawRole.NetLabel || it.Role == DrawRole.PartLabel || it.Role == DrawRole.PinName)
                return 1;
            if (it.Owner.StartsWith("gate:", StringComparison.Ordinal))
                return 2;
            if (it.Role == DrawRole.Wire)
                return 3;
            return 4;
        }

        /// <summary>
        /// The box an item covers. Text width is estimated at 0.6 x the font size per character,
        /// which is the monospace width the sheet is drawn with.
        /// </summary>
        /// <param name="it">
        /// The drawing step.
        /// </param>
        /// <param name="left">
        /// Left edge.
        /// </param>
        /// <param name="top">
        /// Top edge.
        /// </param>
        /// <param name="right">
        /// Right edge.
        /// </param>
        /// <param name="bottom">
        /// Bottom edge.
        /// </param>
        public static void Bounds(DrawItem it, out double left, out double top, out double right, out double bottom)
        {
            switch (it.Kind)
            {
                case DrawKind.Rect:
                    left = it.X1; top = it.Y1; right = it.X1 + it.X2; bottom = it.Y1 + it.Y2;
                    return;
                case DrawKind.Circle:
                    left = it.X1 - it.X2; top = it.Y1 - it.X2; right = it.X1 + it.X2; bottom = it.Y1 + it.X2;
                    return;
                case DrawKind.Text:
                    double w = 0.6 * it.FontSize * (it.Text.Length + (0.7 * it.Subscript.Length));
                    left = it.Anchor == TextAnchor.Start ? it.X1 : it.Anchor == TextAnchor.Middle ? it.X1 - (w / 2) : it.X1 - w;
                    right = left + w;
                    top = it.Y1 - it.FontSize;
                    bottom = it.Y1 + (0.3 * it.FontSize);
                    return;
                case DrawKind.Path:
                    left = double.MaxValue; top = double.MaxValue; right = double.MinValue; bottom = double.MinValue;
                    PathSegment[] p = it.Path!;
                    for (int i = 0; i < p.Length; i++)
                    {
                        if (p[i].Op == PathOp.Close)
                            continue;
                        left = Math.Min(left, p[i].X); right = Math.Max(right, p[i].X);
                        top = Math.Min(top, p[i].Y); bottom = Math.Max(bottom, p[i].Y);
                    }
                    return;
                default:
                    // a line: its box, widened by 2 units so a thin line can still be picked
                    left = Math.Min(it.X1, it.X2) - 2; right = Math.Max(it.X1, it.X2) + 2;
                    top = Math.Min(it.Y1, it.Y2) - 2; bottom = Math.Max(it.Y1, it.Y2) + 2;
                    return;
            }
        }
    }
}
