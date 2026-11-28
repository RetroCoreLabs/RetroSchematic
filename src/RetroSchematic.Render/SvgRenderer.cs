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
    /// <see cref="SheetLayout"/>, the same layout the desktop viewer draws, and every step's
    /// colour, fill, width and line style comes from <see cref="SchematicStyle"/> with the
    /// colours of a <see cref="SchematicTheme"/>, the same answer the window uses, so the file
    /// and the screen always agree.
    /// </summary>
    public static class SvgRenderer
    {
        /// <summary>
        /// Lay out and render a sheet in the default theme.
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
        /// Write an already laid-out drawing in the default theme.
        /// </summary>
        /// <param name="d">
        /// The laid-out drawing.
        /// </param>
        /// <returns>
        /// The SVG text.
        /// </returns>
        public static string Write(SchematicDrawing d) => Write(d, SchematicTheme.Default);

        /// <summary>
        /// Write an already laid-out drawing in a theme.
        /// </summary>
        /// <param name="d">
        /// The laid-out drawing.
        /// </param>
        /// <param name="theme">
        /// The colours to use.
        /// </param>
        /// <returns>
        /// The SVG text.
        /// </returns>
        public static string Write(SchematicDrawing d, SchematicTheme theme)
        {
            StringBuilder svg = new();
            svg.Append(CultureInfo.InvariantCulture,
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{d.Width:0}\" height=\"{d.Height:0}\" viewBox=\"0 0 {d.Width:0} {d.Height:0}\" font-family=\"Courier New, Consolas, monospace\" font-size=\"10\">\n");
            svg.Append("<rect width=\"100%\" height=\"100%\" fill=\"").Append(theme.Get(SchematicColour.Paper)).Append("\"/>\n");
            for (int i = 0; i < d.Items.Count; i++)
                WriteItem(svg, d.Items[i], theme, d.LineScale);
            svg.Append("</svg>\n");
            return svg.ToString();
        }

        /// <summary>
        /// The colour of a step in a theme, as #RRGGBB.
        /// </summary>
        /// <param name="it">
        /// The drawing step.
        /// </param>
        /// <param name="theme">
        /// The theme.
        /// </param>
        /// <returns>
        /// Its line or text colour.
        /// </returns>
        public static string ColourOf(DrawItem it, SchematicTheme theme) => theme.Get(SchematicStyle.Of(it).Stroke);

        private static void WriteItem(StringBuilder svg, DrawItem it, SchematicTheme theme, double scale)
        {
            ItemStyle st = SchematicStyle.Of(it);
            // the "Line width" setting thickens or thins every line by the same factor
            double width = st.Width * scale;
            string colour = theme.Get(st.Stroke);
            string fill = st.Fill == null ? "none" : theme.Get(st.Fill.Value);
            StringBuilder line = new();
            line.Append(CultureInfo.InvariantCulture, $" stroke=\"{colour}\" stroke-width=\"{width:0.##}\"");
            double[] dash = ItemStyle.Dashes(st.Line, width);
            if (dash.Length > 0)
                line.Append(CultureInfo.InvariantCulture, $" stroke-dasharray=\"{dash[0]:0.##} {dash[1]:0.##}\"");
            // dots need round ends to show at all; buses and outlines look better with round joins
            if (st.Round || st.Line == LineStyle.Dotted)
                line.Append(" stroke-linecap=\"round\" stroke-linejoin=\"round\"");
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
                        $"<line x1=\"{it.X1:0.#}\" y1=\"{it.Y1:0.#}\" x2=\"{it.X2:0.#}\" y2=\"{it.Y2:0.#}\"{line}/>\n");
                    break;
                case DrawKind.Rect:
                    svg.Append(CultureInfo.InvariantCulture,
                        $"<rect x=\"{it.X1:0.#}\" y=\"{it.Y1:0.#}\" width=\"{it.X2:0.#}\" height=\"{it.Y2:0.#}\" fill=\"{fill}\"{line}/>\n");
                    break;
                case DrawKind.Circle:
                    svg.Append(CultureInfo.InvariantCulture,
                        $"<circle cx=\"{it.X1:0.#}\" cy=\"{it.Y1:0.#}\" r=\"{it.X2:0.#}\" fill=\"{fill}\"{line}/>\n");
                    break;
                case DrawKind.Path:
                    svg.Append("<path d=\"");
                    PathSegment[] p = it.Path!;
                    for (int i = 0; i < p.Length; i++)
                        AppendSegment(svg, p[i]);
                    svg.Append("\" fill=\"").Append(fill).Append('"').Append(line).Append("/>\n");
                    break;
                case DrawKind.Text:
                    string anchor = it.Anchor == TextAnchor.Start ? "" : it.Anchor == TextAnchor.Middle ? " text-anchor=\"middle\"" : " text-anchor=\"end\"";
                    string size = it.FontSize == 10 ? "" : string.Create(CultureInfo.InvariantCulture, $" font-size=\"{it.FontSize:0.#}\"");
                    string bold = it.Bold || st.Bold ? " font-weight=\"bold\"" : "";
                    svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{it.X1:0.#}\" y=\"{it.Y1:0.#}\"{anchor}{size}{bold} fill=\"{colour}\">");
                    svg.Append(Escape(it.Text));
                    // the 0/1 level subscript: big enough to read, never smaller than 8
                    if (it.Subscript.Length > 0)
                        svg.Append(CultureInfo.InvariantCulture, $"<tspan baseline-shift=\"sub\" font-size=\"{DrawItem.SubscriptSize(it.FontSize):0.#}\">").Append(Escape(it.Subscript)).Append("</tspan>");
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
            if (!TryParse(text, out BoardPosition p))
                throw new FormatException($"\"{text}\" is not a board position (column number then row letter).");
            return p;
        }

        /// <summary>
        /// Reads a board position like <see cref="Parse"/>, but answers false for a reference that
        /// is not one: ND also names parts that have no grid position in their name (LD3, OSC1,
        /// TH1 on card 3207).
        /// </summary>
        /// <param name="text">
        /// The reference.
        /// </param>
        /// <param name="position">
        /// The position, when it is one.
        /// </param>
        /// <returns>
        /// True when the reference is a board position.
        /// </returns>
        public static bool TryParse(string text, out BoardPosition position)
        {
            position = default;
            int i = 0;
            if (!ReadNumber(text, ref i, out int c1))
                return false;
            int c2 = c1;
            if (i < text.Length && text[i] == '-' && i + 1 < text.Length && char.IsDigit(text[i + 1]))
            {
                i++;
                ReadNumber(text, ref i, out c2);
            }
            if (i >= text.Length || !char.IsAsciiLetterUpper(text[i]))
                return false;
            int r1 = text[i++] - 'A';
            int r2 = r1;
            if (i + 1 < text.Length && text[i] == '-' && char.IsAsciiLetterUpper(text[i + 1]))
            {
                r2 = text[i + 1] - 'A';
                i += 2;
            }
            if (i != text.Length || c1 < 1 || c2 < c1 || r2 < r1)
                return false;
            position = new BoardPosition(c1, c2, r1, r2);
            return true;
        }

        private static bool ReadNumber(string text, ref int i, out int value)
        {
            int start = i;
            while (i < text.Length && char.IsDigit(text[i]))
                i++;
            value = 0;
            if (i == start)
                return false;
            value = int.Parse(text.AsSpan(start, i - start), CultureInfo.InvariantCulture);
            return true;
        }
    }
}
