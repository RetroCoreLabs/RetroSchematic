//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>Which symbol set the sheet is drawn with.</summary>
    public enum SymbolStyle
    {
        /// <summary>
        /// Norsk Data's own symbols, from ND-13.003.01 "Circuit Diagram Symbols" (July 1976):
        /// a plain box is NAND, a box with an X is NOR, a box with a diagonal and a small
        /// triangle is an inverter, a NAND box followed by the inverter's triangle is AND.
        /// </summary>
        NorskData,

        /// <summary>The usual distinctive-shape symbols (IEEE Std 91 shapes).</summary>
        Modern,
    }

    /// <summary>
    /// Lays a netlist sheet out as a <see cref="SchematicDrawing"/>. Each chip is placed by its
    /// board position as printed on the drawing (column number, row letter), which is where it
    /// sits on the card. Signals are shown as names at the pins, the way ND drawings end a line
    /// in a named diamond head, instead of routed wires. Gate positions measured on the scan are
    /// not used yet.
    /// </summary>
    public static class SheetLayout
    {
        private const double CellWidth = 220;
        private const double CellHeight = 150;
        private const double Margin = 40;
        private const double GateWidth = 30;
        private const double GateHeight = 24;
        private const double GateSpacing = 34;

        /// <summary>Lay out a whole sheet.</summary>
        public static SchematicDrawing Build(Netlist netlist, SymbolStyle style)
        {
            Dictionary<string, Net> netByPin = new(StringComparer.Ordinal);
            for (int i = 0; i < netlist.Nets.Count; i++)
            {
                Net net = netlist.Nets[i];
                for (int j = 0; j < net.Pins.Count; j++)
                    netByPin[net.Pins[j].At] = net;
            }

            // Board grid size: highest column, highest row letter.
            int maxColumn = 1, maxRow = 0;
            BoardPosition[] positions = new BoardPosition[netlist.Parts.Count];
            for (int i = 0; i < netlist.Parts.Count; i++)
            {
                positions[i] = BoardPosition.Parse(netlist.Parts[i].Ref);
                maxColumn = Math.Max(maxColumn, positions[i].LastColumn);
                maxRow = Math.Max(maxRow, positions[i].LastRow);
            }

            SchematicDrawing d = new((2 * Margin) + (maxColumn * CellWidth), (2 * Margin) + ((maxRow + 1) * CellHeight) + 30);
            Text(d, DrawRole.Title, Confidence.Read, "", Margin, Margin - 15,
                netlist.Sheet.Id + "  " + netlist.Sheet.Title + "  (" + (style == SymbolStyle.NorskData ? "ND symbols" : "modern symbols") + ")",
                16, TextAnchor.Start);

            for (int i = 0; i < netlist.Parts.Count; i++)
            {
                Part part = netlist.Parts[i];
                BoardPosition pos = positions[i];
                // Columns are drawn left to right in rising order. Which edge of the real board
                // column 1 is on has NOT been checked.
                double x = Margin + ((pos.FirstColumn - 1) * CellWidth);
                double y = Margin + (pos.FirstRow * CellHeight);
                double w = ((pos.LastColumn - pos.FirstColumn + 1) * CellWidth) - 20;
                double h = ((pos.LastRow - pos.FirstRow + 1) * CellHeight) - 20;
                LayPart(d, netlist, part, x, y, w, h, netByPin, style);
            }
            return d;
        }

        private static void LayPart(SchematicDrawing d, Netlist netlist, Part part, double x, double y, double w, double h,
            Dictionary<string, Net> netByPin, SymbolStyle style)
        {
            string owner = "part:" + part.Ref;
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupStart, Owner = owner });
            d.Items.Add(new DrawItem
            {
                Kind = DrawKind.Rect,
                Role = DrawRole.PartOutline,
                Confidence = part.Confidence,
                Owner = owner,
                X1 = x,
                Y1 = y,
                X2 = w,
                Y2 = h,
                Dashed = part.Confidence != Confidence.Read,
            });
            DrawItem label = Text(d, DrawRole.PartLabel, part.Confidence, owner, x + 4, y + 12, part.Ref + " " + part.Type, 10, TextAnchor.Start);
            label.Bold = true;

            double gy = y + 22;
            int gatesDrawn = 0;
            for (int i = 0; i < netlist.Gates.Count; i++)
            {
                Gate gate = netlist.Gates[i];
                if (gate.Part != part.Ref)
                    continue;
                LayGate(d, part, gate, x + 60, gy, netByPin, style);
                gy += GateSpacing;
                gatesDrawn++;
            }

            if (gatesDrawn == 0)
            {
                // No gates read yet: list the pins that are on a net, in columns, so a large chip
                // (COM2601, 40 pins) shows every one.
                double ty = y + 26, tx = x + 6;
                for (int pin = 1; pin <= 64; pin++)
                {
                    if (!netByPin.TryGetValue(part.Ref + "." + pin.ToString(CultureInfo.InvariantCulture), out Net? net))
                        continue;
                    if (ty > y + h - 18)
                    {
                        ty = y + 26;
                        tx += 100;
                    }
                    DrawItem t = Text(d, DrawRole.SignalName, Confidence.Read, "net:" + net.Name, tx, ty,
                        pin.ToString("##0", CultureInfo.InvariantCulture).PadLeft(2) + ": " + net.Name, 10, TextAnchor.Start);
                    t.Subscript = Subscript(net);
                    ty += 11;
                }
                if (!string.IsNullOrEmpty(part.Function) && ty < y + h - 4)
                    Text(d, DrawRole.Note, part.Confidence, owner, x + 6, y + h - 6, Shorten(part.Function!, (int)(w / 6)), 10, TextAnchor.Start);
            }
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupEnd, Owner = owner });
        }

        private static void LayGate(SchematicDrawing d, Part part, Gate gate, double x, double y,
            Dictionary<string, Net> netByPin, SymbolStyle style)
        {
            string owner = "gate:" + gate.Id;
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupStart, Owner = owner });
            if (style == SymbolStyle.NorskData)
                NorskDataSymbol(d, gate, owner, x, y);
            else
                ModernSymbol(d, gate, owner, x, y);
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupEnd, Owner = owner });

            // Input stubs with pin numbers and signal names.
            int n = gate.Inputs.Count;
            for (int i = 0; i < n; i++)
            {
                double py = y + ((i + 1) * GateHeight / (n + 1));
                Line(d, DrawRole.PinLine, gate.Confidence, owner, x - 12, py, x, py);
                Text(d, DrawRole.PinNumber, gate.Confidence, owner, x - 11, py - 2, gate.Inputs[i].ToString(CultureInfo.InvariantCulture), 7, TextAnchor.Start);
                PinLabel(d, part, gate.Inputs[i], x - 14, py + 3, TextAnchor.End, netByPin);
            }

            double outX = x + GateWidth + (style == SymbolStyle.NorskData && gate.Kind == GateKind.And ? 10 : 0);
            double oy = y + (GateHeight / 2);
            Line(d, DrawRole.PinLine, gate.Confidence, owner, outX, oy, outX + 14, oy);
            Text(d, DrawRole.PinNumber, gate.Confidence, owner, outX + 3, oy - 2, gate.Output.ToString(CultureInfo.InvariantCulture), 7, TextAnchor.Start);
            PinLabel(d, part, gate.Output, outX + 16, oy + 3, TextAnchor.Start, netByPin);
        }

        /// <summary>ND-13.003.01 pages 2 and 4-6: straight lines only.</summary>
        private static void NorskDataSymbol(SchematicDrawing d, Gate gate, string owner, double x, double y)
        {
            double r = x + GateWidth, b = y + GateHeight;
            Confidence c = gate.Confidence;
            d.Items.Add(new DrawItem { Kind = DrawKind.Rect, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = x, Y1 = y, X2 = GateWidth, Y2 = GateHeight });
            switch (gate.Kind)
            {
                case GateKind.Nand:
                    break;
                case GateKind.Nor:
                    Line(d, DrawRole.Symbol, c, owner, x, y, r, b);
                    Line(d, DrawRole.Symbol, c, owner, x, b, r, y);
                    break;
                case GateKind.Not:
                    Line(d, DrawRole.Symbol, c, owner, x, b, r, y);
                    Line(d, DrawRole.Symbol, c, owner, x + 4, y + 4, r - 4, y + (GateHeight / 2));
                    Line(d, DrawRole.Symbol, c, owner, r - 4, y + (GateHeight / 2), x + 4, b - 4);
                    break;
                case GateKind.And:
                    // The NAND box followed by the inverter's triangle (7408).
                    Line(d, DrawRole.Symbol, c, owner, r, y + 3, r + 10, y + (GateHeight / 2));
                    Line(d, DrawRole.Symbol, c, owner, r + 10, y + (GateHeight / 2), r, b - 3);
                    break;
                default:
                    // Not drawn by this layout yet: the kind is written in the box, so it shows as
                    // unfinished instead of looking like a NAND.
                    Text(d, DrawRole.Symbol, c, owner, x + 3, y + 15, gate.Kind.ToString().ToUpperInvariant(), 8, TextAnchor.Start);
                    break;
            }
        }

        private static void ModernSymbol(SchematicDrawing d, Gate gate, string owner, double x, double y)
        {
            double r = x + GateWidth, b = y + GateHeight, m = y + (GateHeight / 2);
            Confidence c = gate.Confidence;
            switch (gate.Kind)
            {
                case GateKind.And:
                case GateKind.Nand:
                    Path(d, c, owner, new[]
                    {
                        new PathSegment(PathOp.MoveTo, x, y),
                        new PathSegment(PathOp.LineTo, r - 12, y),
                        new PathSegment(PathOp.ArcClockwiseTo, r - 12, b, 12),
                        new PathSegment(PathOp.LineTo, x, b),
                        new PathSegment(PathOp.Close, x, y),
                    });
                    if (gate.Kind == GateKind.Nand)
                        Bubble(d, c, owner, r + 3, m);
                    break;
                case GateKind.Or:
                case GateKind.Nor:
                    Path(d, c, owner, new[]
                    {
                        new PathSegment(PathOp.MoveTo, x, y),
                        new PathSegment(PathOp.QuadTo, r, m, x + 18, y),
                        new PathSegment(PathOp.QuadTo, x, b, x + 18, b),
                        new PathSegment(PathOp.QuadTo, x, y, x + 8, m),
                        new PathSegment(PathOp.Close, x, y),
                    });
                    if (gate.Kind == GateKind.Nor)
                        Bubble(d, c, owner, r + 3, m);
                    break;
                case GateKind.Not:
                case GateKind.Buffer:
                    Path(d, c, owner, new[]
                    {
                        new PathSegment(PathOp.MoveTo, x, y),
                        new PathSegment(PathOp.LineTo, r - 4, m),
                        new PathSegment(PathOp.LineTo, x, b),
                        new PathSegment(PathOp.Close, x, y),
                    });
                    if (gate.Kind == GateKind.Not)
                        Bubble(d, c, owner, r - 1, m);
                    break;
                default:
                    d.Items.Add(new DrawItem { Kind = DrawKind.Rect, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = x, Y1 = y, X2 = GateWidth, Y2 = GateHeight });
                    Text(d, DrawRole.Symbol, c, owner, x + 3, y + 15, "?", 8, TextAnchor.Start);
                    break;
            }
        }

        private static void PinLabel(SchematicDrawing d, Part part, int pin, double x, double y, TextAnchor anchor,
            Dictionary<string, Net> netByPin)
        {
            if (!netByPin.TryGetValue(part.Ref + "." + pin.ToString(CultureInfo.InvariantCulture), out Net? net))
                return;
            DrawItem t = Text(d, DrawRole.SignalName, Confidence.Read, "net:" + net.Name, x, y, net.Name, 10, anchor);
            t.Subscript = Subscript(net);
        }

        /// <summary>ND's subscript on a signal name: 1 = active high, 0 = active low (manual page 2).</summary>
        private static string Subscript(Net net)
            => net.Active == ActiveLevel.None ? "" : net.Active == ActiveLevel.High ? "1" : "0";

        private static DrawItem Text(SchematicDrawing d, DrawRole role, Confidence c, string owner, double x, double y,
            string text, double size, TextAnchor anchor)
        {
            DrawItem t = new() { Kind = DrawKind.Text, Role = role, Confidence = c, Owner = owner, X1 = x, Y1 = y, Text = text, FontSize = size, Anchor = anchor };
            d.Items.Add(t);
            return t;
        }

        private static void Line(SchematicDrawing d, DrawRole role, Confidence c, string owner, double x1, double y1, double x2, double y2)
            => d.Items.Add(new DrawItem { Kind = DrawKind.Line, Role = role, Confidence = c, Owner = owner, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 });

        private static void Path(SchematicDrawing d, Confidence c, string owner, PathSegment[] path)
            => d.Items.Add(new DrawItem { Kind = DrawKind.Path, Role = DrawRole.Symbol, Confidence = c, Owner = owner, Path = path });

        private static void Bubble(SchematicDrawing d, Confidence c, string owner, double cx, double cy)
            => d.Items.Add(new DrawItem { Kind = DrawKind.Circle, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = cx, Y1 = cy, X2 = 3 });

        private static string Shorten(string text, int max) => text.Length <= max ? text : text.Substring(0, Math.Max(0, max - 3)) + "...";
    }
}
