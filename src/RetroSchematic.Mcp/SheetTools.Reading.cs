//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RetroSchematic.Model;
using RetroSchematic.Recognition;
using RetroSchematic.Render;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// The MCP tools for assisted reading of an original page: run the page analyser, accept a
    /// find as a part (or link it to one), set a symbol box by hand, and connect a pin of the part
    /// drawn at a place on the page to a signal. The same steps as the Original view's.
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>The dpi a PDF source page is rendered at for reading (the boxes then carry it).</summary>
        public const int ReadDpi = 300;

        private PageAnalyser? _analyser;
        private readonly Dictionary<int, (PageAnalysis Analysis, int? Dpi)> _analyses = new();

        /// <summary>
        /// Runs the page analyser on a source page.
        /// </summary>
        /// <param name="source">The source page (number in list_sources).</param>
        /// <param name="board">Board grid: empty for the sheet's bus, else nord-10 or nd-100.</param>
        /// <param name="style">Drawing style: empty to let the page decide, else hand or cad.</param>
        /// <returns>One line per find.</returns>
        [McpServerTool(Name = "analyse_page"), Description("Run the page analyser (offline symbol finder, models/README.md) on an original page: finds chips and gates with the position code and chip type read for each, passive parts, circled backplane pins (circle), net-label diamonds (offpage) and wire dots. Every find is a guess (inferred); 'sure' = the detector at least 0.6 and a code read at the symbol. Boxes are in the page's pixels (a PDF page: rendered at 300 dpi). A find that already has a symbol box says which part. Then accept_find, set_symbol_box and connect_pin_on_page. Allowed read-only (changes nothing).")]
        public string AnalysePage([Description("The source page: its number in list_sources")] int source = 1,
            [Description("Board grid for position codes: empty = from the sheet's bus, or nord-10 (1-19, A-E) or nd-100 (1-29, A-H, J)")] string? board = null,
            [Description("Drawing style: empty = the page-style rule decides, or hand or cad")] string? style = null)
        {
            SheetEditor e = Open();
            byte[] picture;
            int? dpi;
            try
            {
                picture = e.SourcePicture(source, ReadDpi, out dpi);
            }
            catch (SheetEditException ex)
            {
                throw new McpException("Refused: " + ex.Message, ex);
            }

            BoardGrid grid = GridOf(e.Root, board);
            PageStyle? st = string.IsNullOrEmpty(style) ? null
                : style == "hand" ? PageStyle.Hand
                : style == "cad" ? PageStyle.Cad
                : throw new McpException("style is hand, cad or empty.");
            _analyser ??= new PageAnalyser(PageAnalyser.DefaultModelsDir);
            HashSet<string> ids = new(StringComparer.Ordinal);
            if (e.Catalog != null)
                ids.UnionWith(e.Catalog.Types());
            PageAnalysis a = _analyser.Analyse(GreyImage.FromEncoded(picture), grid, ids, st);
            _analyses[source] = (a, dpi);
            return Report(e.Root, source, a);
        }

        /// <summary>
        /// Accepts a find of the last analysis of a page.
        /// </summary>
        /// <param name="id">The find's number.</param>
        /// <param name="code">The position code to use, empty for the one read.</param>
        /// <param name="type">The chip type, empty for the one read.</param>
        /// <param name="linkTo">An existing part to link to instead, or empty.</param>
        /// <param name="source">The source page analysed.</param>
        /// <returns>What was done.</returns>
        [McpServerTool(Name = "accept_find"), Description("Accept a find of the last analyse_page of a source page (edit mode; undoable, one step). The find's box is stored as a symbol box drawn by the tool. A chip or gate is tied to a part: link_to an existing part; else the part at the code (read, or as given) when there is one - a second gate of a package goes on its next gate; else a new part of the chip type (read, or as given; it must be in the catalogue) is made there with confidence inferred, on the page read from this source. A circle, diamond or dot is stored without a part. A card without pages is converted to format version 3 first.")]
        public string AcceptFind([Description("The find's number from analyse_page")] int id,
            [Description("Position code to use (13D); empty = the code read")] string? code = null,
            [Description("Chip type for a new part (74157); empty = the type read")] string? type = null,
            [Description("An existing part to link the box to instead; empty for none")] string? linkTo = null,
            [Description("The source page that was analysed")] int source = 1)
            => Run(e =>
            {
                PageFind f = FindOf(source, id);
                SymbolBox box = BoxOf(source, f);
                string? part = e.AcceptFind(box,
                    string.IsNullOrWhiteSpace(code) ? f.Code : code,
                    string.IsNullOrWhiteSpace(type) ? f.TypeId : type,
                    string.IsNullOrWhiteSpace(linkTo) ? null : linkTo);
                if (part == null)
                    return string.Create(CultureInfo.InvariantCulture, $"Stored find {id} ({f.Kind}) as a symbol box without a part.");
                SymbolBox stored = e.Root.SymbolBoxes![^1];
                return string.Create(CultureInfo.InvariantCulture,
                    $"Find {id} ({f.Kind}) is now the box of {part}{(stored.Gate != null ? " gate " + stored.Gate.Value.ToString(CultureInfo.InvariantCulture) : "")} ({e.FindPart(part)!.Type}).");
            });

        /// <summary>
        /// Sets a symbol box by hand.
        /// </summary>
        /// <param name="source">The source page.</param>
        /// <param name="x">Left, page pixels.</param>
        /// <param name="y">Top, page pixels.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="kind">What it holds.</param>
        /// <param name="reference">The part, or empty.</param>
        /// <param name="gate">The gate of the part, 0 for none.</param>
        /// <param name="text">Label text or circled pin, or empty.</param>
        /// <param name="index">0 to add, else the box to replace (1 first).</param>
        /// <returns>What was done.</returns>
        [McpServerTool(Name = "set_symbol_box"), Description("Set a symbol box drawn by hand on an original page (edit mode; undoable): where a part, a gate of a part, a passive part, a circled pin or a label is drawn, in the page's pixels (a PDF page: at 300 dpi). index 0 adds a box, n replaces box n. A card without pages is converted to format version 3 first.")]
        public string SetSymbolBox([Description("The source page: its number in list_sources")] int source,
            [Description("Left edge, page pixels")] double x, [Description("Top edge, page pixels")] double y,
            [Description("Width, page pixels")] double width, [Description("Height, page pixels")] double height,
            [Description("chip, gate, resistor, capacitor, switch, strap, ground, supply, circle, offpage, dot or label")] string kind,
            [Description("The part it is (13D), empty for none")] string? reference = null,
            [Description("Which gate of the part, 1 = first; 0 for none")] int gate = 0,
            [Description("A label's text, or the pin a circle names (P.81)")] string? text = null,
            [Description("0 to add a box; n to replace box n")] int index = 0)
            => Run(e =>
            {
                int? dpi = null;
                if (e.Root.Sources != null && source >= 1 && source <= e.Root.Sources.Count && SheetAttachments.IsPdf(e.Root.Sources[source - 1].File))
                    dpi = ReadDpi;
                int at = e.SetSymbolBox(new SymbolBox
                {
                    Source = source, X = x, Y = y, Width = width, Height = height, Dpi = dpi, Kind = kind,
                    Ref = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
                    Gate = gate > 0 ? gate : null,
                    Text = string.IsNullOrWhiteSpace(text) ? null : text,
                    By = "hand",
                }, index - 1);
                return string.Create(CultureInfo.InvariantCulture, $"Symbol box {at + 1} set.");
            });

        /// <summary>
        /// Connects a pin of the part drawn at a place on a page to a signal.
        /// </summary>
        /// <param name="source">The source page.</param>
        /// <param name="x">Wire end x, page pixels.</param>
        /// <param name="y">Wire end y, page pixels.</param>
        /// <param name="pin">The pin number.</param>
        /// <param name="to">A pin or a signal name.</param>
        /// <returns>What was done, with the part's pinout.</returns>
        [McpServerTool(Name = "connect_pin_on_page"), Description("Connect a wire end on an original page to a pin (edit mode; undoable): the part is the one whose symbol box holds the point (x, y in the page's pixels) or is within 40 px of it; pin is its pin number (the reply lists the part's pinout from the catalogue, pin number = name); to is another pin (13D.5), making or joining that pin's signal, or a signal's name. Symbol boxes come from accept_find or set_symbol_box.")]
        public string ConnectPinOnPage([Description("The source page: its number in list_sources")] int source,
            [Description("Wire end x, page pixels")] double x, [Description("Wire end y, page pixels")] double y,
            [Description("The pin number on the part")] int pin,
            [Description("Another pin (13D.5) or a signal name")] string to)
            => Run(e =>
            {
                (string p, string net) = e.ConnectPinOnPage(source, x, y, pin, to);
                string part = p.Substring(0, p.LastIndexOf('.'));
                return "Pin " + p + " is on signal " + net + ". Pinout of " + part + ": " + Pinout(e, part);
            });

        private static string Pinout(SheetEditor e, string part)
        {
            List<ChipPin> pins = new(e.PinoutOf(part));
            pins.Sort((a, b) => a.Number.CompareTo(b.Number));
            if (pins.Count == 0)
                return "(no pin table in the catalogue)";
            StringBuilder sb = new();
            for (int i = 0; i < pins.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(pins[i].Number.ToString(CultureInfo.InvariantCulture)).Append('=').Append(pins[i].Inverted ? "/" : "").Append(pins[i].Name);
            }
            return sb.ToString();
        }

        private PageFind FindOf(int source, int id)
        {
            if (!_analyses.TryGetValue(source, out (PageAnalysis Analysis, int? Dpi) a))
                throw new SheetEditException($"Source page {source} has not been analysed; run analyse_page first.");
            List<PageFind> finds = a.Analysis.Finds;
            for (int i = 0; i < finds.Count; i++)
            {
                if (finds[i].Id == id)
                    return finds[i];
            }
            throw new SheetEditException($"The analysis of source page {source} has no find {id}.");
        }

        private SymbolBox BoxOf(int source, PageFind f) => new()
        {
            Source = source, X = f.X, Y = f.Y, Width = f.Width, Height = f.Height, Dpi = _analyses[source].Dpi,
            Kind = f.Kind, GateKind = f.GateKind, Score = f.Score, By = "tool",
        };

        private static BoardGrid GridOf(Netlist n, string? board)
        {
            if (board == "nd-100")
                return BoardGrid.Nd100;
            if (board == "nord-10")
                return BoardGrid.Nord10;
            if (!string.IsNullOrEmpty(board))
                throw new McpException("board is nord-10, nd-100 or empty.");
            return n.Sheet.Bus == MachineBus.Nd100 ? BoardGrid.Nd100 : BoardGrid.Nord10;
        }

        private static string Report(Netlist n, int source, PageAnalysis a)
        {
            StringBuilder sb = new();
            int chips = 0, gates = 0, coded = 0;
            for (int i = 0; i < a.Finds.Count; i++)
            {
                string k = a.Finds[i].Kind;
                chips += k == "chip" ? 1 : 0;
                gates += k == "gate" ? 1 : 0;
                coded += (k == "chip" || k == "gate") && a.Finds[i].Code != null ? 1 : 0;
            }
            sb.Append(CultureInfo.InvariantCulture,
                $"Source page {source}: {a.Finds.Count} finds ({chips} chips, {gates} gates, {coded} with a code), style {(a.Style == PageStyle.Cad ? "cad" : "hand")} ({a.Detector}), turned {a.TurnCw} degrees, scale {a.Scale:0.###}, {a.Milliseconds} ms. All inferred.\n");
            for (int i = 0; i < a.Finds.Count; i++)
            {
                PageFind f = a.Finds[i];
                sb.Append(CultureInfo.InvariantCulture, $"{f.Id}: {f.Kind}");
                if (f.GateKind != null)
                    sb.Append(' ').Append(f.GateKind);
                sb.Append(CultureInfo.InvariantCulture, $" at {f.X},{f.Y} {f.Width}x{f.Height} score {f.Score:0.00}");
                if (f.Code != null)
                    sb.Append(" code ").Append(f.Code).Append(f.CodeFromColumn ? " (from the column)" : "");
                if (f.TypeId != null)
                    sb.Append(" type ").Append(f.TypeId).Append(" (read \"").Append(f.TypeText).Append("\")");
                sb.Append(f.IsSure ? " sure" : " unsure");
                string? boxed = BoxedAs(n, source, f);
                if (boxed != null)
                    sb.Append(" - boxed as ").Append(boxed);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        // the part of a symbol box on the page that covers the find (overlap over half the smaller)
        internal static string? BoxedAs(Netlist n, int source, PageFind f)
        {
            if (n.SymbolBoxes == null)
                return null;
            for (int i = 0; i < n.SymbolBoxes.Count; i++)
            {
                SymbolBox b = n.SymbolBoxes[i];
                if (b.Source != source)
                    continue;
                double ix = Math.Min(b.X + b.Width, f.X + f.Width) - Math.Max(b.X, f.X);
                double iy = Math.Min(b.Y + b.Height, f.Y + f.Height) - Math.Max(b.Y, f.Y);
                if (ix > 0 && iy > 0 && ix * iy > 0.5 * Math.Min(b.Width * b.Height, (double)f.Width * f.Height))
                    return b.Ref ?? b.Kind;
            }
            return null;
        }
    }
}
