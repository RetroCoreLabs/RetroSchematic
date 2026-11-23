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
    /// <summary>
    /// Writes a <see cref="SchematicDrawing"/> as SVG. The layout itself is made by
    /// <see cref="SheetLayout"/>, the same layout the desktop viewer draws, so the file and the
    /// screen always agree. Colours here are the fixed print colours (black on white); the viewer
    /// takes them from its theme instead.
    /// </summary>
    public static class SvgRenderer
    {
        /// <summary>
        /// Lay out and render a sheet.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="style">
        /// ND 1976 or modern symbols.
        /// </param>
        /// <returns>
        /// The SVG text.
        /// </returns>
        public static string Render(Netlist netlist, SymbolStyle style) => Write(SheetLayout.Build(netlist, style));

        /// <summary>
        /// Write an already laid-out drawing.
        /// </summary>
        /// <param name="d">
        /// The laid-out drawing.
        /// </param>
        /// <returns>
        /// The SVG text.
        /// </returns>
        public static string Write(SchematicDrawing d)
        {
            StringBuilder svg = new();
            svg.Append(CultureInfo.InvariantCulture,
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{d.Width:0}\" height=\"{d.Height:0}\" viewBox=\"0 0 {d.Width:0} {d.Height:0}\" font-family=\"Courier New, Consolas, monospace\" font-size=\"10\">\n");
            svg.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>\n");
            for (int i = 0; i < d.Items.Count; i++)
                WriteItem(svg, d.Items[i]);
            svg.Append("</svg>\n");
            return svg.ToString();
        }

        /// <summary>
        /// The print colour of an item: what it is, and whether its reading is proved. Anything
        /// OPEN is red, inferred symbols and wires are brown, the part frame is grey.
        /// </summary>
        /// <param name="it">
        /// The drawing step.
        /// </param>
        /// <returns>
        /// The colour as #rrggbb.
        /// </returns>
        public static string PrintColour(DrawItem it)
        {
            if (it.Role == DrawRole.PartOutline)
                return "#999999";
            if (it.Role == DrawRole.Note)
                return "#666666";
            if (it.Confidence == Confidence.Open && it.Role != DrawRole.PinDot)
                return "#d00000";
            if (it.Confidence != Confidence.Read && (it.Role == DrawRole.Symbol || it.Role == DrawRole.Wire))
                return "#b06000";
            return "#000000";
        }

        private static void WriteItem(StringBuilder svg, DrawItem it)
        {
            string colour = PrintColour(it);
            string dash = (it.Dashed ? " stroke-dasharray=\"4 3\"" : "")
                + (LineWidths.Of(it) == 1 ? "" : string.Create(CultureInfo.InvariantCulture, $" stroke-width=\"{LineWidths.Of(it):0.##}\""));
            switch (it.Kind)
            {
                case DrawKind.GroupStart:
                    // "part:11D" -> id="part-11D", "gate:11D/1" -> id="gate-11D/1"
                    svg.Append("<g id=\"").Append(Escape(it.Owner.Replace(':', '-'))).Append("\">\n");
                    break;
                case DrawKind.GroupEnd:
                    svg.Append("</g>\n");
                    break;
                case DrawKind.Line:
                    svg.Append(CultureInfo.InvariantCulture,
                        $"<line x1=\"{it.X1:0.#}\" y1=\"{it.Y1:0.#}\" x2=\"{it.X2:0.#}\" y2=\"{it.Y2:0.#}\" stroke=\"{colour}\"{dash}/>\n");
                    break;
                case DrawKind.Rect:
                    svg.Append(CultureInfo.InvariantCulture,
                        $"<rect x=\"{it.X1:0.#}\" y=\"{it.Y1:0.#}\" width=\"{it.X2:0.#}\" height=\"{it.Y2:0.#}\" fill=\"none\" stroke=\"{colour}\"{dash}/>\n");
                    break;
                case DrawKind.Circle:
                    svg.Append(CultureInfo.InvariantCulture,
                        $"<circle cx=\"{it.X1:0.#}\" cy=\"{it.Y1:0.#}\" r=\"{it.X2:0.#}\" fill=\"none\" stroke=\"{colour}\"/>\n");
                    break;
                case DrawKind.Path:
                    svg.Append("<path d=\"");
                    PathSegment[] p = it.Path!;
                    for (int i = 0; i < p.Length; i++)
                        AppendSegment(svg, p[i]);
                    svg.Append(CultureInfo.InvariantCulture, $"\" fill=\"none\" stroke=\"{colour}\"{dash}/>\n");
                    break;
                case DrawKind.Text:
                    string anchor = it.Anchor == TextAnchor.Start ? "" : it.Anchor == TextAnchor.Middle ? " text-anchor=\"middle\"" : " text-anchor=\"end\"";
                    string size = it.FontSize == 10 ? "" : string.Create(CultureInfo.InvariantCulture, $" font-size=\"{it.FontSize:0.#}\"");
                    string bold = it.Bold ? " font-weight=\"bold\"" : "";
                    string fill = colour == "#000000" ? "" : " fill=\"" + colour + "\"";
                    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{it.X1:0.#}\" y=\"{it.Y1:0.#}\"{anchor}{size}{bold}{fill}>");
                    svg.Append(Escape(it.Text));
                    if (it.Subscript.Length > 0)
                        svg.Append("<tspan baseline-shift=\"sub\" font-size=\"7\">").Append(Escape(it.Subscript)).Append("</tspan>");
                    svg.Append("</text>\n");
                    break;
            }
        }

        private static void AppendSegment(StringBuilder svg, PathSegment s)
        {
            switch (s.Op)
            {
                case PathOp.MoveTo:
                    svg.Append(CultureInfo.InvariantCulture, $"M{s.X:0.#},{s.Y:0.#} ");
                    break;
                case PathOp.LineTo:
                    svg.Append(CultureInfo.InvariantCulture, $"L{s.X:0.#},{s.Y:0.#} ");
                    break;
                case PathOp.QuadTo:
                    svg.Append(CultureInfo.InvariantCulture, $"Q{s.CX:0.#},{s.CY:0.#} {s.X:0.#},{s.Y:0.#} ");
                    break;
                case PathOp.ArcClockwiseTo:
                    svg.Append(CultureInfo.InvariantCulture, $"A{s.CX:0.#},{s.CX:0.#} 0 0 1 {s.X:0.#},{s.Y:0.#} ");
                    break;
                case PathOp.Close:
                    svg.Append('Z');
                    break;
            }
        }

        private static string Escape(string text)
            => text.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal).Replace("\"", "&quot;", StringComparison.Ordinal);
    }

    /// <summary>
    /// A board position as printed on ND drawings: column number(s) then row letter(s),
    /// e.g. 13D, or 7-8C-D for a chip spanning two columns and two rows.
    /// </summary>
    public readonly struct BoardPosition
    {
        /// <summary>
        /// Create a position.
        /// </summary>
        /// <param name="firstColumn">
        /// First column number.
        /// </param>
        /// <param name="lastColumn">
        /// Last column number.
        /// </param>
        /// <param name="firstRow">
        /// First row, A = 0.
        /// </param>
        /// <param name="lastRow">
        /// Last row, A = 0.
        /// </param>
        public BoardPosition(int firstColumn, int lastColumn, int firstRow, int lastRow)
        {
            FirstColumn = firstColumn;
            LastColumn = lastColumn;
            FirstRow = firstRow;
            LastRow = lastRow;
        }

        /// <summary>
        /// First column number.
        /// </summary>
        public int FirstColumn { get; }

        /// <summary>
        /// Last column number.
        /// </summary>
        public int LastColumn { get; }

        /// <summary>
        /// First row, A = 0.
        /// </summary>
        public int FirstRow { get; }

        /// <summary>
        /// Last row, A = 0.
        /// </summary>
        public int LastRow { get; }

        /// <summary>
        /// Parse "13D" or "7-8C-D".
        /// </summary>
        /// <exception cref="FormatException">
        /// Not a board position.
        /// </exception>
        /// <param name="text">
        /// The position as printed, such as 13D or 7-8C-D.
        /// </param>
        /// <returns>
        /// The columns and rows it covers.
        /// </returns>
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
