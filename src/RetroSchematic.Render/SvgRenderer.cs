//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>Which symbol set the renderer draws gates with.</summary>
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
    /// Draws a netlist sheet as SVG. Each chip is placed by its board position as printed on
    /// the drawing (column number, row letter), which is where it sits on the card. Signals are
    /// shown as names at the pins, the way ND drawings end a line in a named diamond head,
    /// instead of routed wires. Gate positions measured on the scan are not used yet.
    /// </summary>
    public static class SvgRenderer
    {
        private const double CellWidth = 220;
        private const double CellHeight = 150;
        private const double Margin = 40;
        private const double GateWidth = 30;
        private const double GateHeight = 24;
        private const double GateSpacing = 34;

        /// <summary>Render a sheet.</summary>
        public static string Render(Netlist netlist, SymbolStyle style)
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

            double width = (2 * Margin) + (maxColumn * CellWidth);
            double height = (2 * Margin) + ((maxRow + 1) * CellHeight) + 30;

            StringBuilder svg = new();
            svg.Append(CultureInfo.InvariantCulture,
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width:0}\" height=\"{height:0}\" viewBox=\"0 0 {width:0} {height:0}\" font-family=\"monospace\" font-size=\"10\">\n");
            svg.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>\n");
            svg.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{Margin}\" y=\"{Margin - 15}\" font-size=\"16\">{Escape(netlist.Sheet.Id)}  {Escape(netlist.Sheet.Title)}  ({(style == SymbolStyle.NorskData ? "ND symbols" : "modern symbols")})</text>\n");

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
                DrawPart(svg, netlist, part, x, y, w, h, netByPin, style);
            }

            svg.Append("</svg>\n");
            return svg.ToString();
        }

        private static void DrawPart(StringBuilder svg, Netlist netlist, Part part, double x, double y, double w, double h,
            Dictionary<string, Net> netByPin, SymbolStyle style)
        {
            string dash = part.Confidence == Confidence.Read ? "" : " stroke-dasharray=\"4 3\"";
            svg.Append(CultureInfo.InvariantCulture,
                $"<g id=\"part-{Escape(part.Ref)}\"><rect x=\"{x:0.#}\" y=\"{y:0.#}\" width=\"{w:0.#}\" height=\"{h:0.#}\" fill=\"none\" stroke=\"#999999\"{dash}/>\n");
            svg.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{x + 4:0.#}\" y=\"{y + 12:0.#}\" font-weight=\"bold\">{Escape(part.Ref)} {Escape(part.Type)}</text>\n");

            double gy = y + 22;
            int gatesDrawn = 0;
            for (int i = 0; i < netlist.Gates.Count; i++)
            {
                Gate gate = netlist.Gates[i];
                if (gate.Part != part.Ref)
                    continue;
                DrawGate(svg, part, gate, x + 60, gy, netByPin, style);
                gy += GateSpacing;
                gatesDrawn++;
            }

            if (gatesDrawn == 0)
            {
                // No gates read yet: list the pins that are on a net.
                // Pins on a net, in columns, so a large chip (COM2601, 40 pins) shows every one.
                double ty = y + 26, tx = x + 6;
                for (int pin = 1; pin <= 64; pin++)
                {
                    if (netByPin.TryGetValue(part.Ref + "." + pin.ToString(CultureInfo.InvariantCulture), out Net? net))
                    {
                        if (ty > y + h - 18)
                        {
                            ty = y + 26;
                            tx += 100;
                        }
                        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{tx:0.#}\" y=\"{ty:0.#}\">{pin,2}: ");
                        AppendSignal(svg, net);
                        svg.Append("</text>\n");
                        ty += 11;
                    }
                }
                if (!string.IsNullOrEmpty(part.Function) && ty < y + h - 4)
                    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{x + 6:0.#}\" y=\"{y + h - 6:0.#}\" fill=\"#666666\">{Escape(Shorten(part.Function!, (int)(w / 6)))}</text>\n");
            }
            svg.Append("</g>\n");
        }

        private static void DrawGate(StringBuilder svg, Part part, Gate gate, double x, double y,
            Dictionary<string, Net> netByPin, SymbolStyle style)
        {
            string stroke = gate.Confidence == Confidence.Read ? "#000000" : "#b06000";
            svg.Append(CultureInfo.InvariantCulture, $"<g id=\"gate-{Escape(gate.Id)}\" fill=\"none\" stroke=\"{stroke}\">\n");
            if (style == SymbolStyle.NorskData)
                DrawNorskDataSymbol(svg, gate.Kind, x, y);
            else
                DrawModernSymbol(svg, gate.Kind, x, y);
            svg.Append("</g>\n");

            // Input stubs with pin numbers and signal names.
            int n = gate.Inputs.Count;
            for (int i = 0; i < n; i++)
            {
                double py = y + ((i + 1) * GateHeight / (n + 1));
                svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{x - 12:0.#}\" y1=\"{py:0.#}\" x2=\"{x:0.#}\" y2=\"{py:0.#}\" stroke=\"#000000\"/>\n");
                svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{x - 11:0.#}\" y=\"{py - 2:0.#}\" font-size=\"7\">{gate.Inputs[i]}</text>\n");
                AppendPinLabel(svg, part, gate.Inputs[i], x - 14, py + 3, "end", netByPin);
            }

            double outX = x + GateWidth + (style == SymbolStyle.NorskData && gate.Kind == GateKind.And ? 10 : 0);
            double oy = y + (GateHeight / 2);
            svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{outX:0.#}\" y1=\"{oy:0.#}\" x2=\"{outX + 14:0.#}\" y2=\"{oy:0.#}\" stroke=\"#000000\"/>\n");
            svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{outX + 3:0.#}\" y=\"{oy - 2:0.#}\" font-size=\"7\">{gate.Output}</text>\n");
            AppendPinLabel(svg, part, gate.Output, outX + 16, oy + 3, "start", netByPin);
        }

        /// <summary>ND-13.003.01 pages 2 and 4-6: straight lines only.</summary>
        private static void DrawNorskDataSymbol(StringBuilder svg, GateKind kind, double x, double y)
        {
            double r = x + GateWidth, b = y + GateHeight;
            Rect(svg, x, y, GateWidth, GateHeight);
            switch (kind)
            {
                case GateKind.Nand:
                    break;
                case GateKind.Nor:
                    Line(svg, x, y, r, b);
                    Line(svg, x, b, r, y);
                    break;
                case GateKind.Not:
                    Line(svg, x, b, r, y);
                    Line(svg, x + 4, y + 4, r - 4, y + (GateHeight / 2));
                    Line(svg, r - 4, y + (GateHeight / 2), x + 4, b - 4);
                    break;
                case GateKind.And:
                    // The NAND box followed by the inverter's triangle (7408).
                    Line(svg, r, y + 3, r + 10, y + (GateHeight / 2));
                    Line(svg, r + 10, y + (GateHeight / 2), r, b - 3);
                    break;
                default:
                    // OR and buffer symbols are on manual pages not transcribed yet: write the kind.
                    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{x + 3:0.#}\" y=\"{y + 15:0.#}\" stroke=\"none\" fill=\"#000000\" font-size=\"8\">{kind.ToString().ToUpperInvariant()}</text>\n");
                    break;
            }
        }

        private static void DrawModernSymbol(StringBuilder svg, GateKind kind, double x, double y)
        {
            double r = x + GateWidth, b = y + GateHeight, m = y + (GateHeight / 2);
            switch (kind)
            {
                case GateKind.And:
                case GateKind.Nand:
                    svg.Append(CultureInfo.InvariantCulture,
                        $"<path d=\"M{x:0.#},{y:0.#} H{r - 12:0.#} A12,12 0 0 1 {r - 12:0.#},{b:0.#} H{x:0.#} Z\"/>\n");
                    if (kind == GateKind.Nand) Bubble(svg, r + 3, m);
                    break;
                case GateKind.Or:
                case GateKind.Nor:
                    svg.Append(CultureInfo.InvariantCulture,
                        $"<path d=\"M{x:0.#},{y:0.#} Q{x + 18:0.#},{y:0.#} {r:0.#},{m:0.#} Q{x + 18:0.#},{b:0.#} {x:0.#},{b:0.#} Q{x + 8:0.#},{m:0.#} {x:0.#},{y:0.#} Z\"/>\n");
                    if (kind == GateKind.Nor) Bubble(svg, r + 3, m);
                    break;
                case GateKind.Not:
                case GateKind.Buffer:
                    svg.Append(CultureInfo.InvariantCulture,
                        $"<path d=\"M{x:0.#},{y:0.#} L{r - 4:0.#},{m:0.#} L{x:0.#},{b:0.#} Z\"/>\n");
                    if (kind == GateKind.Not) Bubble(svg, r - 1, m);
                    break;
                default:
                    Rect(svg, x, y, GateWidth, GateHeight);
                    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{x + 3:0.#}\" y=\"{y + 15:0.#}\" stroke=\"none\" fill=\"#000000\" font-size=\"8\">?</text>\n");
                    break;
            }
        }

        private static void AppendPinLabel(StringBuilder svg, Part part, int pin, double x, double y, string anchor,
            Dictionary<string, Net> netByPin)
        {
            if (!netByPin.TryGetValue(part.Ref + "." + pin.ToString(CultureInfo.InvariantCulture), out Net? net))
                return;
            svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{x:0.#}\" y=\"{y:0.#}\" text-anchor=\"{anchor}\">");
            AppendSignal(svg, net);
            svg.Append("</text>\n");
        }

        /// <summary>A signal name with its ND subscript: 1 = active high, 0 = active low.</summary>
        private static void AppendSignal(StringBuilder svg, Net net)
        {
            svg.Append(Escape(net.Name));
            if (net.Active != ActiveLevel.None)
                svg.Append("<tspan baseline-shift=\"sub\" font-size=\"7\">").Append(net.Active == ActiveLevel.High ? '1' : '0').Append("</tspan>");
        }

        private static void Rect(StringBuilder svg, double x, double y, double w, double h)
            => svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{x:0.#}\" y=\"{y:0.#}\" width=\"{w:0.#}\" height=\"{h:0.#}\"/>\n");

        private static void Line(StringBuilder svg, double x1, double y1, double x2, double y2)
            => svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{x1:0.#}\" y1=\"{y1:0.#}\" x2=\"{x2:0.#}\" y2=\"{y2:0.#}\"/>\n");

        private static void Bubble(StringBuilder svg, double cx, double cy)
            => svg.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{cx:0.#}\" cy=\"{cy:0.#}\" r=\"3\"/>\n");

        private static string Shorten(string text, int max) => text.Length <= max ? text : text.Substring(0, Math.Max(0, max - 3)) + "...";

        private static string Escape(string text)
            => text.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal).Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    /// <summary>
    /// A board position as printed on ND drawings: column number(s) then row letter(s),
    /// e.g. 13D, or 7-8C-D for a chip spanning two columns and two rows.
    /// </summary>
    public readonly struct BoardPosition
    {
        /// <summary>Create a position.</summary>
        public BoardPosition(int firstColumn, int lastColumn, int firstRow, int lastRow)
        {
            FirstColumn = firstColumn;
            LastColumn = lastColumn;
            FirstRow = firstRow;
            LastRow = lastRow;
        }

        /// <summary>First column number.</summary>
        public int FirstColumn { get; }

        /// <summary>Last column number.</summary>
        public int LastColumn { get; }

        /// <summary>First row, A = 0.</summary>
        public int FirstRow { get; }

        /// <summary>Last row, A = 0.</summary>
        public int LastRow { get; }

        /// <summary>Parse "13D" or "7-8C-D".</summary>
        /// <exception cref="FormatException">Not a board position.</exception>
        public static BoardPosition Parse(string text)
        {
            int i = 0;
            int c1 = ReadNumber(text, ref i);
            int c2 = c1;
            if (i < text.Length && text[i] == '-' && i + 1 < text.Length && char.IsDigit(text[i + 1]))
            {
                i++;
                c2 = ReadNumber(text, ref i);
            }
            if (i >= text.Length || !char.IsAsciiLetterUpper(text[i]))
                throw new FormatException($"\"{text}\" is not a board position (column number then row letter).");
            int r1 = text[i++] - 'A';
            int r2 = r1;
            if (i + 1 < text.Length && text[i] == '-' && char.IsAsciiLetterUpper(text[i + 1]))
            {
                r2 = text[i + 1] - 'A';
                i += 2;
            }
            if (i != text.Length || c1 < 1 || c2 < c1 || r2 < r1)
                throw new FormatException($"\"{text}\" is not a board position (column number then row letter).");
            return new BoardPosition(c1, c2, r1, r2);
        }

        private static int ReadNumber(string text, ref int i)
        {
            int start = i;
            while (i < text.Length && char.IsDigit(text[i]))
                i++;
            if (i == start)
                throw new FormatException($"\"{text}\" is not a board position (column number then row letter).");
            return int.Parse(text.AsSpan(start, i - start), CultureInfo.InvariantCulture);
        }
    }
}
