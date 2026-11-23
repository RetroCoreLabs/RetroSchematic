//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// The board tools: board facts and connectors, source pages, photos and switches
    /// (docs/FORMAT.md, "Board, sources, photos, switches"). Values go in and out as JSON in
    /// the file's own field names, so what a tool returns can be changed and sent back.
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>
        /// The board section and the connectors, as JSON.
        /// </summary>
        /// <returns>
        /// {"board": ..., "connectors": [...]}.
        /// </returns>
        [McpServerTool(Name = "get_board_info"), Description("The sheet's board facts (ND machine, board number, print and ECO versions, size in mm, layers) and its connectors (ref, catalog, use, used, standard, pins, rows, pinsPerRow, note), as JSON in the file's own field names. Allowed read-only.")]
        public string GetBoardInfo()
        {
            Netlist n = Open().Root;
            return "{\"board\": " + (n.Board == null ? "null" : NetlistReader.ToJson(n.Board))
                + ",\n\"connectors\": " + (n.Connectors == null ? "[]" : NetlistReader.ToJson(n.Connectors)) + "}";
        }

        /// <summary>
        /// Sets the board section.
        /// </summary>
        /// <param name="board">
        /// The board section as JSON, or empty to remove it.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_board_info"), Description("Set the board facts (edit mode; undoable). JSON with any of (the ND PCB catalogue's own names): number (card number, 1050), partNumber (322049), title, ndSystem (NORD-10 or ND-100), group, description, printVersion, ecoVersion, printVersions [{print, eco, note}], specifications {size {widthMm, heightMm}, layers}, releaseDate, leds/straps/switches/connectors [{id, color, text}], chipset [{name, description, url}], sourceUrl, factSources {field: {kind, document, place, confidence, quote, note}}, note. The whole section is replaced; empty text removes it.")]
        public string SetBoardInfo([Description("The board section as JSON, e.g. {\"ndSystem\":\"NORD-10\",\"partNumber\":\"322049\"}")] string board)
            => Run(e =>
            {
                BoardSection? b = string.IsNullOrWhiteSpace(board) ? null : Parse<BoardSection>(board);
                if (b?.NdSystem != null && b.NdSystem != "NORD-10" && b.NdSystem != "ND-100")
                    throw new McpException("ndSystem is NORD-10 or ND-100.");
                e.SetBoard(b);
                return b == null ? "Removed the board section." : "Set the board section.";
            });

        /// <summary>
        /// Imports a board from the ND PCB catalogue.
        /// </summary>
        /// <param name="catalogue">
        /// The catalogue file, pcb.json.
        /// </param>
        /// <param name="key">
        /// A part number or card number, or empty for the sheet's own.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "import_board_from_catalogue"), Description("Fill the board facts from the ND PCB catalogue (pcb.json, scraped from norsk-data.com; read only) (edit mode; one undo step). The board is found by part number (322049) or card number (1050); empty key uses the sheet's own partNumber, else number. Every fact filled records the catalogue and the board's web page as its source. The component- and solder-side photos are copied into the sheet (a .rsc: beside its photos, else SHEETNAME-files/photos/; a .rsx holds them) with version, variant (old/new) and caption; a photo the sheet already has is kept. The catalogue's switch text is added as a further source on the switch block it names (its 'position E1' is our 1E).")]
        public string ImportBoardFromCatalogue(
            [Description("Full path of pcb.json")] string catalogue,
            [Description("Part number or card number; empty = the sheet's own")] string? key = null)
            => Run(e =>
            {
                PcbCatalogue c;
                try
                {
                    c = PcbCatalogue.Load(catalogue);
                }
                catch (System.IO.IOException ex)
                {
                    throw new McpException(ex.Message, ex);
                }
                string k = string.IsNullOrWhiteSpace(key) ? e.Root.Board?.PartNumber ?? e.Root.Board?.Number ?? "" : key;
                if (k.Length == 0)
                    throw new McpException("The sheet has no part or card number; give one as key.");
                List<CatalogueEntry> found = c.Find(k);
                if (found.Count == 0)
                    throw new McpException("No board " + k + " in the catalogue (" + c.Count.ToString(CultureInfo.InvariantCulture) + " boards).");
                if (found.Count > 1)
                {
                    StringBuilder sb = new();
                    for (int i = 0; i < found.Count; i++)
                        sb.Append(i == 0 ? "" : ", ").Append(found[i].PartNumber).Append(" (").Append(found[i].Board.Title).Append(')');
                    throw new McpException("Card number " + k + " is used by " + found.Count.ToString(CultureInfo.InvariantCulture) + " boards: " + sb + ". Give the part number.");
                }
                return e.ImportFromCatalogue(found[0], _path);
            });

        /// <summary>
        /// Adds or replaces one connector entry.
        /// </summary>
        /// <param name="connector">
        /// The connector as JSON.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_connector"), Description("Add a connector entry, or replace the one with the same ref (edit mode; undoable). JSON: ref (P, F, or P.A, P.B, P.C ...), catalog (a file id in catalog/connectors for the sheet's bus), use (what it carries on this card, or \"not used\"), used (false = not used), standard (DIN 41612), pins (\"0-99\"), rows ([\"a\",\"b\",\"c\"]), pinsPerRow (32), note. A replaced entry keeps its hand-placed box position.")]
        public string SetConnector([Description("The connector as JSON")] string connector)
            => Run(e =>
            {
                ConnectorUse c = Parse<ConnectorUse>(connector);
                e.SetConnector(c);
                return "Set connector " + c.Ref + ".";
            });

        /// <summary>
        /// Removes one connector entry.
        /// </summary>
        /// <param name="reference">
        /// The connector.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "remove_connector"), Description("Remove a connector entry (edit mode; undoable). Its pins are still drawn; only the description goes.")]
        public string RemoveConnector([Description("The connector, e.g. P.C")] string reference)
            => Run(e =>
            {
                e.RemoveConnector(reference);
                return "Removed connector " + reference + ".";
            });

        /// <summary>
        /// The source pages and photos, numbered.
        /// </summary>
        /// <returns>
        /// One line per entry.
        /// </returns>
        [McpServerTool(Name = "list_sources"), Description("List the sheet's original schematic pages and board photos, numbered from 1, with file, PDF page, title or side, whether the file is held inside the sheet (.rsx or imported) or linked on disk, and whether it can be found. Allowed read-only.")]
        public string ListSources()
        {
            SheetEditor e = Open();
            Netlist n = e.Root;
            StringBuilder sb = new();
            sb.Append("Source pages:\n");
            for (int i = 0; n.Sources != null && i < n.Sources.Count; i++)
            {
                SourcePage s = n.Sources[i];
                sb.Append(string.Create(CultureInfo.InvariantCulture, $"  {i + 1}. {s.File}"));
                if (s.PdfPage != null)
                    sb.Append(string.Create(CultureInfo.InvariantCulture, $" page {s.PdfPage}"));
                if (s.Title != null)
                    sb.Append(" - ").Append(s.Title);
                if (s.Original != null)
                    sb.Append(string.Create(CultureInfo.InvariantCulture, $" (from {s.Original.File} page {s.Original.PdfPage} at {s.Original.Dpi} dpi)"));
                sb.Append(Where(e, s.File)).Append('\n');
            }
            if (n.Sources == null)
                sb.Append("  none\n");
            sb.Append("Photos:\n");
            for (int i = 0; n.Photos != null && i < n.Photos.Count; i++)
            {
                BoardPhoto p = n.Photos[i];
                sb.Append(string.Create(CultureInfo.InvariantCulture, $"  {i + 1}. {p.File} - {(p.Side == BoardSide.Component ? "component side" : "solder side")}"));
                if (p.Version != null || p.Variant != null)
                    sb.Append(" (version ").Append(p.Version ?? "?").Append(p.Variant == null ? "" : ", " + p.Variant).Append(')');
                sb.Append(Where(e, p.File)).Append('\n');
            }
            if (n.Photos == null)
                sb.Append("  none\n");
            return sb.ToString();
        }

        /// <summary>
        /// Adds a source page from a file.
        /// </summary>
        /// <param name="path">
        /// The file.
        /// </param>
        /// <param name="pdfPage">
        /// The PDF page, or 0 for a picture.
        /// </param>
        /// <param name="title">
        /// A title, or empty.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "add_source"), Description("Add an original schematic page from a file (edit mode; undoable): a picture, or a PDF with the page number. A .rsc links the file by a path relative to the sheet; a .rsx (or a sheet not saved yet) keeps the file inside and writes it into the zip on save (a PDF page as PNG at 300 dpi).")]
        public string AddSource(
            [Description("Full path of the picture or PDF")] string path,
            [Description("For a PDF: the page, 1 = first; 0 for a picture")] int pdfPage = 0,
            [Description("A title for the page picker")] string? title = null)
            => Run(e =>
            {
                string link = e.ImportFile(path, "sources", Hold());
                e.SetSource(new SourcePage { File = link, PdfPage = pdfPage > 0 ? pdfPage : null, Title = string.IsNullOrWhiteSpace(title) ? null : title });
                return "Added source page " + link + ".";
            });

        /// <summary>
        /// Removes a source page.
        /// </summary>
        /// <param name="number">
        /// Its number in list_sources.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "remove_source"), Description("Remove a source page by its number in list_sources (edit mode; undoable). The file itself is not deleted.")]
        public string RemoveSource([Description("Its number, 1 first")] int number)
            => Run(e =>
            {
                e.RemoveSource(number - 1);
                return "Removed source page " + number.ToString(CultureInfo.InvariantCulture) + ".";
            });

        /// <summary>
        /// Adds a board photo from a file.
        /// </summary>
        /// <param name="path">
        /// The picture.
        /// </param>
        /// <param name="side">
        /// component or solder.
        /// </param>
        /// <param name="note">
        /// A note, or empty.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "add_photo"), Description("Add a photo of the board from a picture file (edit mode; undoable): the component side or the solder side. Linked from a .rsc, kept inside a .rsx.")]
        public string AddPhoto(
            [Description("Full path of the picture")] string path,
            [Description("component or solder")] string side,
            [Description("What it shows")] string? note = null)
            => Run(e =>
            {
                BoardSide s = side.Trim().ToLowerInvariant() switch
                {
                    "component" => BoardSide.Component,
                    "solder" => BoardSide.Solder,
                    _ => throw new McpException("side is component or solder."),
                };
                string link = e.ImportFile(path, "photos", Hold());
                e.SetPhoto(new BoardPhoto { File = link, Side = s, Note = string.IsNullOrWhiteSpace(note) ? null : note });
                return "Added photo " + link + ".";
            });

        /// <summary>
        /// Removes a board photo.
        /// </summary>
        /// <param name="number">
        /// Its number in list_sources.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "remove_photo"), Description("Remove a board photo by its number in list_sources (edit mode; undoable). The file itself is not deleted.")]
        public string RemovePhoto([Description("Its number, 1 first")] int number)
            => Run(e =>
            {
                e.RemovePhoto(number - 1);
                return "Removed photo " + number.ToString(CultureInfo.InvariantCulture) + ".";
            });

        /// <summary>
        /// The switch blocks, as JSON.
        /// </summary>
        /// <returns>
        /// The switches list.
        /// </returns>
        [McpServerTool(Name = "get_switches"), Description("The card's switch blocks as JSON: ref (board position), function, leverCount, onMeans (0, 1 or open: the bit a lever that is on gives) and how sure, typical setting, and per lever its number, signal, bit, confidence, setting (on, off, open) and note. Allowed read-only.")]
        public string GetSwitches()
        {
            List<SwitchBlock>? s = Open().Root.Switches;
            return s == null ? "[]" : NetlistReader.ToJson(s);
        }

        /// <summary>
        /// Adds or replaces one switch block.
        /// </summary>
        /// <param name="block">
        /// The switch block as JSON.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_switch"), Description("Add a switch block, or replace the one at the same board position (edit mode; undoable). JSON as get_switches returns one entry. Mark every fact: confidence read, inferred or open; never guess a lever setting - use open.")]
        public string SetSwitch([Description("The switch block as JSON")] string block)
            => Run(e =>
            {
                SwitchBlock b = Parse<SwitchBlock>(block);
                e.SetSwitch(b);
                return "Set switch block " + b.Ref + ".";
            });

        /// <summary>
        /// Removes a switch block.
        /// </summary>
        /// <param name="reference">
        /// Its board position.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "remove_switch"), Description("Remove a switch block by its board position (edit mode; undoable).")]
        public string RemoveSwitch([Description("Board position, e.g. 15E")] string reference)
            => Run(e =>
            {
                e.RemoveSwitch(reference);
                return "Removed switch block " + reference + ".";
            });

        // a sheet not saved yet, or a .rsx, keeps imported files inside; a .rsc links them
        private bool Hold() => _path == null || SheetFile.IsZipPath(_path);

        private static string Where(SheetEditor e, string file)
            => e.Files.Holds(file) ? " [inside the sheet]" : e.Files.Read(file) != null ? " [linked]" : " [NOT FOUND]";

        /// <summary>
        /// The chip grid: the board type, the sheet's pcb section, every chip's area in mm and
        /// each photo's saved corners.
        /// </summary>
        /// <returns>
        /// The grid as text.
        /// </returns>
        [McpServerTool(Name = "get_pcb_grid"), Description("The chip grid on the board photos: the board type (catalog/boards, size and columns, how they were measured), the sheet's pcb section (template, rows in mm from the finger edge, hand-set boxes), each chip's and switch block's area in mm on the component side (from its position code, or its hand-set box, which wins), and each photo's saved board corners (pixels). Allowed read-only.")]
        public string GetPcbGrid()
        {
            SheetEditor e = Open();
            Netlist n = e.Root;
            string? id = n.Pcb?.Template ?? (n.Board?.NdSystem == "NORD-10" ? "nord10-card" : n.Board?.NdSystem == "ND-100" ? "nd100-card" : null);
            BoardTemplate? t;
            try
            {
                t = BoardTemplate.Find(e.Catalog?.Folder, id);
            }
            catch (NetlistException ex)
            {
                throw new McpException(ex.Message, ex);
            }
            StringBuilder sb = new();
            sb.Append("Board type: ").Append(t == null ? "none (" + (id ?? "no template and no ND machine") + ")" : t.Id + ", " + t.Title + ", confidence " + t.Confidence).Append('\n');
            if (t?.Columns != null)
                sb.Append(CultureInfo.InvariantCulture, $"Columns {t.Columns.First}-{t.Columns.Last}: centre of column {t.Columns.First} at {t.Columns.FirstCentreMm} mm, {t.Columns.PitchMm} mm apart\n");
            sb.Append("pcb section: ").Append(n.Pcb == null ? "none" : NetlistReader.ToJson(n.Pcb)).Append('\n');
            if (t != null)
            {
                sb.Append("Areas (mm, component side; x from the left edge, y from the finger edge):\n");
                for (int i = 0; i < n.Parts.Count; i++)
                    AppendArea(sb, t, n.Pcb, n.Parts[i].Ref, n.Parts[i].Type);
                for (int i = 0; n.Switches != null && i < n.Switches.Count; i++)
                    AppendArea(sb, t, n.Pcb, n.Switches[i].Ref, "switch");
            }
            for (int i = 0; n.Photos != null && i < n.Photos.Count; i++)
            {
                List<PhotoPoint>? c = n.Photos[i].Corners;
                sb.Append("Photo ").Append(i + 1).Append(' ').Append(n.Photos[i].File).Append(": corners ")
                  .Append(c == null ? "not saved (found in the photo, else the whole picture)" : string.Create(CultureInfo.InvariantCulture, $"{c[0].X},{c[0].Y} {c[1].X},{c[1].Y} {c[2].X},{c[2].Y} {c[3].X},{c[3].Y}")).Append('\n');
            }
            return sb.ToString();
        }

        private static void AppendArea(StringBuilder sb, BoardTemplate t, PcbLayout? pcb, string reference, string? type)
        {
            if (!PcbGrid.Area(t, pcb, reference, out GridPoint[] c, out bool byHand))
            {
                sb.Append("  ").Append(reference).Append(' ').Append(type).Append(": no cell (the code names no column or row of the grid)\n");
                return;
            }
            sb.Append("  ").Append(reference).Append(' ').Append(type).Append(byHand ? " (by hand)" : "").Append(':');
            for (int i = 0; i < c.Length; i++)
                sb.Append(CultureInfo.InvariantCulture, $" {c[i].X:0.##},{c[i].Y:0.##}");
            sb.Append('\n');
        }

        /// <summary>
        /// Sets or removes a chip's hand-set box.
        /// </summary>
        /// <param name="reference">
        /// The part's position.
        /// </param>
        /// <param name="box">
        /// The box as JSON, or empty to remove it.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_chip_box"), Description("Set a chip's box on the board by hand (edit mode; undoable); it then wins over the grid cell of its position code. JSON {xMm, yMm (the centre, mm on the component side from the left and finger edges), widthMm, heightMm, rotationDeg}. Empty box removes the hand-set box so the cell is used again.")]
        public string SetChipBox(
            [Description("The part's position, such as 13D")] string reference,
            [Description("The box as JSON, e.g. {\"xMm\":101.4,\"yMm\":106,\"widthMm\":7.6,\"heightMm\":20}; empty to remove")] string? box = null)
            => Run(e =>
            {
                if (string.IsNullOrWhiteSpace(box))
                {
                    e.RemoveChipBox(reference);
                    return "Removed the hand-set box of " + reference + "; its grid cell is used.";
                }
                ChipBox b = Parse<ChipBox>(box);
                b.Ref = reference;
                e.SetChipBox(b);
                return "Set the box of " + reference + " by hand.";
            });

        /// <summary>
        /// Sets or removes a photo's board corners.
        /// </summary>
        /// <param name="photo">
        /// The photo, 1 first.
        /// </param>
        /// <param name="corners">
        /// Eight numbers, or empty.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_photo_corners"), Description("Set where the board's four corners are in a photo (edit mode; undoable), in the photo's pixels: top left, top right, bottom right, bottom left of the board as the grid sees it (finger edge at the top; on a solder photo the grid is mirrored for you). Eight numbers 'x1 y1 x2 y2 x3 y3 x4 y4'. Empty removes them (the viewer looks for the board again).")]
        public string SetPhotoCorners(
            [Description("The photo number, 1 first (list_sources shows them)")] int photo,
            [Description("x1 y1 x2 y2 x3 y3 x4 y4, or empty")] string? corners = null)
            => Run(e =>
            {
                if (string.IsNullOrWhiteSpace(corners))
                {
                    e.SetPhotoCorners(photo - 1, null);
                    return "Removed the corners of photo " + photo + ".";
                }
                string[] parts = corners.Split(new[] { ' ', ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 8)
                    throw new McpException("Give eight numbers: x and y of four corners.");
                GridPoint[] c = new GridPoint[4];
                for (int i = 0; i < 4; i++)
                {
                    if (!double.TryParse(parts[2 * i], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                        || !double.TryParse(parts[(2 * i) + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                        throw new McpException("'" + corners + "' is not eight numbers.");
                    c[i] = new GridPoint(x, y);
                }
                e.SetPhotoCorners(photo - 1, c);
                return "Set the board corners of photo " + photo + ".";
            });

        private static T Parse<T>(string json)
        {
            try
            {
                return NetlistReader.FromJson<T>(json);
            }
            catch (NetlistException ex)
            {
                throw new McpException(ex.Message, ex);
            }
        }
    }
}
