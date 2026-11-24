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
    /// The named colours of a schematic drawing. Each theme gives every one a value
    /// (<see cref="SchematicTheme"/>); the window turns them into brushes with the same names
    /// (ThemeManager), the SVG writer writes them as #rrggbb.
    /// </summary>
    public enum SchematicColour
    {
        /// <summary>The paper behind everything.</summary>
        Paper,

        /// <summary>The sheet title.</summary>
        Title,

        /// <summary>The soft fill inside a chip box.</summary>
        ChipFill,

        /// <summary>A chip box's outline.</summary>
        ChipOutline,

        /// <summary>The soft fill inside a gate symbol.</summary>
        GateFill,

        /// <summary>A gate symbol's outline.</summary>
        GateOutline,

        /// <summary>The short line from a chip or gate to a pin end.</summary>
        PinLine,

        /// <summary>A pin number: small and muted.</summary>
        PinNumber,

        /// <summary>A pin's function name written inside a chip box.</summary>
        PinName,

        /// <summary>A chip's type above its box (7400), bold.</summary>
        PartTitle,

        /// <summary>A chip's board position (11D) before its type: muted.</summary>
        BoardPosition,

        /// <summary>A wire with no simulated value.</summary>
        Wire,

        /// <summary>A virtual bus line and its name.</summary>
        Bus,

        /// <summary>The diamond label at a pin end for a signal that has no wire.</summary>
        NetLabel,

        /// <summary>A signal's name.</summary>
        SignalName,

        /// <summary>The dot where a wire branches.</summary>
        Junction,

        /// <summary>The dot at a pin end.</summary>
        PinDot,

        /// <summary>The fill inside a block instance box.</summary>
        BlockFill,

        /// <summary>The band across the top of a block instance box, behind its name.</summary>
        BlockHeader,

        /// <summary>A block instance box's outline.</summary>
        BlockOutline,

        /// <summary>The direction arrows on block port pins.</summary>
        PortArrow,

        /// <summary>Notes and other secondary text.</summary>
        Note,

        /// <summary>What was inferred, not read off the drawing (drawn dashed as well).</summary>
        Inferred,

        /// <summary>What is not known at all: a warning colour (drawn dotted as well).</summary>
        Open,

        /// <summary>The picked item.</summary>
        Selection,

        /// <summary>The traced signal.</summary>
        Trace,
    }

    /// <summary>
    /// How a line is drawn. The reading's confidence shows here, not only in colour: read is
    /// solid, inferred is dashed, open is dotted.
    /// </summary>
    public enum LineStyle
    {
        /// <summary>An unbroken line: read off the drawing.</summary>
        Solid,

        /// <summary>Dashes: inferred.</summary>
        Dashed,

        /// <summary>Dots: not known.</summary>
        Dotted,
    }

    /// <summary>
    /// Everything needed to draw one step: colours by name, width, line style, boldness.
    /// </summary>
    public readonly struct ItemStyle
    {
        /// <summary>
        /// Creates a style.
        /// </summary>
        /// <param name="stroke">Line or text colour.</param>
        /// <param name="fill">Fill colour, or null for none.</param>
        /// <param name="width">Line width in drawing units.</param>
        /// <param name="line">Solid, dashed or dotted.</param>
        /// <param name="bold">True for bold text.</param>
        /// <param name="round">True for rounded line ends and joins.</param>
        public ItemStyle(SchematicColour stroke, SchematicColour? fill, double width, LineStyle line, bool bold, bool round)
        {
            Stroke = stroke;
            Fill = fill;
            Width = width;
            Line = line;
            Bold = bold;
            Round = round;
        }

        /// <summary>Line or text colour.</summary>
        public SchematicColour Stroke { get; }

        /// <summary>Fill colour, or null for none.</summary>
        public SchematicColour? Fill { get; }

        /// <summary>Line width in drawing units.</summary>
        public double Width { get; }

        /// <summary>Solid, dashed or dotted.</summary>
        public LineStyle Line { get; }

        /// <summary>True for bold text.</summary>
        public bool Bold { get; }

        /// <summary>True for rounded line ends and joins.</summary>
        public bool Round { get; }

        /// <summary>
        /// The dash pattern for a line style at a width, in drawing units (on, off, ...); empty
        /// for a solid line. Dots are drawn as zero-length dashes with round ends.
        /// </summary>
        /// <param name="line">The style.</param>
        /// <param name="width">The line width.</param>
        /// <returns>The pattern.</returns>
        public static double[] Dashes(LineStyle line, double width) => line switch
        {
            LineStyle.Dashed => new[] { 5 + (width * 1.5), 3 + width },
            LineStyle.Dotted => new[] { 0.01, 2.5 + (width * 1.5) },
            _ => Array.Empty<double>(),
        };
    }

    /// <summary>
    /// The schematic colours of one theme. The theme ids are the window's (Light, Dark,
    /// SolarizedDark, Nord, RetroGreen, AmberCrt, Synthwave, HighContrast); the command-line
    /// render takes the same ids. Light is the default: a modern schematic tool's look (soft
    /// yellow chip bodies with dark red outlines, teal wires, blue buses), not black on white.
    /// </summary>
    public sealed class SchematicTheme
    {
        private static readonly List<SchematicTheme> s_all = new();
        private readonly string[] _colours;

        static SchematicTheme()
        {
            // order of every row: SchematicColour order (see the enum)
            Add("Light", false,
                "#FAFAF7", "#1F2937", "#FFF8DB", "#8C1C1C", "#FFF3C4", "#8C1C1C", "#A04040", "#9CA3AF", "#4B5563", "#111827",
                "#6B7280", "#00796B", "#1D4ED8", "#7B1FA2", "#0D47A1", "#00796B", "#A04040", "#EEF3FB", "#C9D8F2", "#1E3A8A",
                "#2E7D32", "#6B7280", "#C2410C", "#DC2626", "#0078D7", "#FF8C00");
            Add("Dark", true,
                "#1E1E1E", "#E5E7EB", "#2D2B20", "#E0A458", "#36321F", "#E0A458", "#C08A50", "#7D7D7D", "#B8B8B8", "#F3F4F6",
                "#9A9A9A", "#4DD0C4", "#6EA8FE", "#C792EA", "#82B1FF", "#4DD0C4", "#C08A50", "#1E2A3D", "#2C3E5C", "#7AA2F7",
                "#66BB6A", "#9CA3AF", "#F0A050", "#FF5555", "#0A84FF", "#FF8C00");
            Add("SolarizedDark", true,
                "#002B36", "#EEE8D5", "#073642", "#B58900", "#0A3D4A", "#B58900", "#839496", "#586E75", "#93A1A1", "#FDF6E3",
                "#657B83", "#2AA198", "#268BD2", "#D33682", "#6C71C4", "#2AA198", "#839496", "#0B3A47", "#11505F", "#93A1A1",
                "#859900", "#657B83", "#CB4B16", "#DC322F", "#268BD2", "#FFAA00");
            Add("Nord", true,
                "#2E3440", "#ECEFF4", "#3B4252", "#D8DEE9", "#434C5E", "#D8DEE9", "#A3ABB9", "#7B88A1", "#D8DEE9", "#ECEFF4",
                "#616E88", "#88C0D0", "#81A1C1", "#B48EAD", "#8FBCBB", "#88C0D0", "#A3ABB9", "#353C4A", "#434C5E", "#5E81AC",
                "#D08770", "#9AA4B5", "#EBCB8B", "#BF616A", "#88C0D0", "#FFAA00");
            Add("RetroGreen", true,
                "#001100", "#66FF66", "#002200", "#00DD00", "#003300", "#00DD00", "#00AA00", "#007700", "#00BB00", "#88FF88",
                "#009900", "#33FF99", "#00FFCC", "#CCFF66", "#99FF99", "#33FF99", "#00AA00", "#001A08", "#003311", "#00FF66",
                "#FFFF00", "#00AA00", "#DDDD00", "#FF4040", "#FFFFFF", "#FFAA00");
            Add("AmberCrt", true,
                "#1A0F00", "#FFC040", "#2A1800", "#FFB000", "#332000", "#FFB000", "#CC8800", "#8A5A00", "#DDA030", "#FFD080",
                "#AA7000", "#FFCC66", "#FF9900", "#FFE0A0", "#FFD966", "#FFCC66", "#CC8800", "#241500", "#3A2400", "#FFA000",
                "#FF6A00", "#CC8800", "#FFFF00", "#FF4040", "#FFFFFF", "#FF5500");
            Add("Synthwave", true,
                "#262335", "#FFFFFF", "#2F2A45", "#FF7EDB", "#34294F", "#FF7EDB", "#B893CE", "#7E6A9A", "#D6C3E8", "#F4EEFF",
                "#9A86B8", "#36F9F6", "#72F1B8", "#FEDE5D", "#F97E72", "#36F9F6", "#B893CE", "#2A2140", "#3B2E5A", "#AF6DF9",
                "#FF8B39", "#B893CE", "#FFCC00", "#FE4450", "#FFFFFF", "#FFAA00");
            Add("HighContrast", true,
                "#000000", "#FFFFFF", "#101010", "#F0F0F0", "#1A1A1A", "#F0F0F0", "#E0E0E0", "#B0B0B0", "#E0E0E0", "#FFFFFF",
                "#C8C8C8", "#00FFFF", "#00A0FF", "#FF80FF", "#80FF80", "#00FFFF", "#E0E0E0", "#001030", "#002060", "#4080FF",
                "#FF9900", "#E0E0E0", "#FFFF00", "#FF3030", "#FF00FF", "#FFA500");
        }

        private SchematicTheme(string id, bool dark, string[] colours)
        {
            Id = id;
            IsDark = dark;
            _colours = colours;
        }

        /// <summary>
        /// Every theme, Light first.
        /// </summary>
        public static IReadOnlyList<SchematicTheme> All => s_all;

        /// <summary>
        /// The default theme: Light.
        /// </summary>
        public static SchematicTheme Default => s_all[0];

        /// <summary>
        /// The theme's id, as the window's theme menu and the command line name it.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// True for dark paper.
        /// </summary>
        public bool IsDark { get; }

        /// <summary>
        /// A theme by id (case does not matter), or null.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>The theme, or null.</returns>
        public static SchematicTheme? Find(string? id)
        {
            for (int i = 0; id != null && i < s_all.Count; i++)
            {
                if (string.Equals(s_all[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return s_all[i];
            }
            return null;
        }

        /// <summary>
        /// A colour of this theme as #RRGGBB.
        /// </summary>
        /// <param name="colour">Which colour.</param>
        /// <returns>The value.</returns>
        public string Get(SchematicColour colour) => _colours[(int)colour];

        /// <summary>
        /// The resource name of a colour's brush in the window (SchematicWireBrush, ...).
        /// </summary>
        /// <param name="colour">Which colour.</param>
        /// <returns>The name.</returns>
        public static string BrushKey(SchematicColour colour) => "Schematic" + colour + "Brush";

        private static void Add(string id, bool dark, params string[] colours)
        {
            if (colours.Length != Enum.GetValues<SchematicColour>().Length)
                throw new InvalidOperationException("Theme " + id + " gives " + colours.Length + " schematic colours.");
            s_all.Add(new SchematicTheme(id, dark, colours));
        }
    }

    /// <summary>
    /// What every drawing step looks like: one answer for the window and the SVG export, so they
    /// always agree. Colour comes from what the step is (its role), line style from how sure
    /// the reading is.
    /// </summary>
    public static class SchematicStyle
    {
        /// <summary>
        /// The style of a drawing step.
        /// </summary>
        /// <param name="it">The step.</param>
        /// <returns>Its style.</returns>
        public static ItemStyle Of(DrawItem it)
        {
            bool block = it.Owner.StartsWith("inst:", StringComparison.Ordinal);
            LineStyle line = it.Confidence == Confidence.Open ? LineStyle.Dotted
                : it.Confidence == Confidence.Inferred || it.Dashed ? LineStyle.Dashed : LineStyle.Solid;
            // an unknown line is drawn in the warning colour as well as dotted
            bool open = it.Confidence == Confidence.Open;
            switch (it.Role)
            {
                case DrawRole.Title:
                    return new ItemStyle(SchematicColour.Title, null, LineWidths.Wire, LineStyle.Solid, true, false);
                case DrawRole.PartOutline:
                    if (block)
                        return new ItemStyle(SchematicColour.BlockOutline, SchematicColour.BlockFill, LineWidths.Outline, LineStyle.Solid, false, true);
                    return new ItemStyle(open ? SchematicColour.Open : SchematicColour.ChipOutline, SchematicColour.ChipFill, LineWidths.Outline, line, false, true);
                case DrawRole.BlockHeader:
                    return new ItemStyle(SchematicColour.BlockOutline, SchematicColour.BlockHeader, LineWidths.Wire, LineStyle.Solid, false, false);
                case DrawRole.Symbol:
                    if (it.Kind == DrawKind.Text)
                        return new ItemStyle(SchematicColour.GateOutline, null, LineWidths.Wire, LineStyle.Solid, false, false);
                    return new ItemStyle(open ? SchematicColour.Open : SchematicColour.GateOutline, SchematicColour.GateFill, LineWidths.Outline, line, false, true);
                case DrawRole.PinLine:
                    return new ItemStyle(open ? SchematicColour.Open : SchematicColour.PinLine, null, LineWidths.Pin, line, false, false);
                case DrawRole.PinNumber:
                    return new ItemStyle(SchematicColour.PinNumber, null, LineWidths.Wire, LineStyle.Solid, false, false);
                case DrawRole.PinName:
                    return new ItemStyle(SchematicColour.PinName, null, LineWidths.Wire, LineStyle.Solid, false, false);
                case DrawRole.PartLabel:
                    return new ItemStyle(open ? SchematicColour.Open : SchematicColour.PartTitle, null, LineWidths.Wire, LineStyle.Solid, true, false);
                case DrawRole.BoardPosition:
                    return new ItemStyle(SchematicColour.BoardPosition, null, LineWidths.Wire, LineStyle.Solid, false, false);
                case DrawRole.Wire:
                    return new ItemStyle(open ? SchematicColour.Open : SchematicColour.Wire, null, LineWidths.Wire, line, false, true);
                case DrawRole.Junction:
                    return new ItemStyle(SchematicColour.Junction, SchematicColour.Junction, LineWidths.Wire, LineStyle.Solid, false, false);
                case DrawRole.Bus:
                    if (it.Kind == DrawKind.Text)
                        return new ItemStyle(SchematicColour.Bus, null, LineWidths.Wire, LineStyle.Solid, true, false);
                    return new ItemStyle(open ? SchematicColour.Open : SchematicColour.Bus, null, LineWidths.Bus, line, false, true);
                case DrawRole.NetLabel:
                    // A tag's diamond is always drawn solid. It is only about 23 units round, so
                    // the dash pattern of an inferred or open line (6.8 on, 4.2 off) cut whole
                    // pieces out of it and it looked broken (BUGS.md B2). How sure the reading is
                    // shows in its colour, the same as the name beside it.
                    return new ItemStyle(TextColour(it, SchematicColour.NetLabel), null, LineWidths.Label, LineStyle.Solid, true, true);
                case DrawRole.SignalName:
                    return new ItemStyle(TextColour(it, SchematicColour.SignalName), null, LineWidths.Wire, LineStyle.Solid, true, false);
                case DrawRole.PinDot:
                    return new ItemStyle(SchematicColour.PinDot, null, LineWidths.Pin, LineStyle.Solid, false, false);
                case DrawRole.PortArrow:
                    return new ItemStyle(SchematicColour.PortArrow, SchematicColour.PortArrow, LineWidths.Pin, LineStyle.Solid, false, true);
                case DrawRole.OffPage:
                    // the off-page box and its page numbers, in the block colour so it reads as
                    // a way to somewhere else, not as a signal name
                    if (it.Kind == DrawKind.Text)
                        return new ItemStyle(SchematicColour.BlockOutline, null, LineWidths.Wire, LineStyle.Solid, true, false);
                    return new ItemStyle(SchematicColour.BlockOutline, null, LineWidths.Pin, LineStyle.Solid, false, false);
                case DrawRole.Note:
                    return new ItemStyle(SchematicColour.Note, null, LineWidths.Wire, LineStyle.Solid, false, false);
                default:
                    return new ItemStyle(SchematicColour.Title, null, LineWidths.Wire, line, false, false);
            }
        }

        /// <summary>
        /// A signal's text colour: its own, or the warning colour when not known, or the inferred
        /// colour when inferred (text has no line style, so colour is all it has).
        /// </summary>
        private static SchematicColour TextColour(DrawItem it, SchematicColour own)
            => it.Confidence == Confidence.Open ? SchematicColour.Open : it.Confidence == Confidence.Inferred ? SchematicColour.Inferred : own;
    }
}
