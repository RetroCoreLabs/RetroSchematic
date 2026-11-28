//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// The validate tools, and the bulk import of a card's reading files: check a sheet against
    /// the reader and the chip catalogue (the command line's <c>check</c>), check a reading set
    /// and write it into the open sheet (<c>card-readings.py check / sheet</c>), and list the
    /// chips the catalogue is missing.
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>
        /// Checks a sheet.
        /// </summary>
        /// <param name="path">
        /// A sheet file to check, or empty for the open sheet as it is in memory.
        /// </param>
        /// <returns>
        /// The report.
        /// </returns>
        [McpServerTool(Name = "check_sheet"), Description("Check a whole sheet (top level and every block), the same check as the command line's check: the reader accepts it (as a save would), every chip pin on a signal is on the chip (catalogue file, with the 74 family rule), a power or ground pin is only on a supply signal (+5V, 5V, GND, 12V, +12V, -12V, 5VSTBY); then the types with no chip file and the gate pins on no signal (not a fault by itself: a pin may be left open on purpose). path: a sheet file; empty checks the open sheet as it is in memory. Allowed read-only.")]
        public string CheckSheet([Description("Full path of a sheet file; empty for the open sheet")] string? path = null)
        {
            Netlist root;
            ChipCatalog? catalog;
            if (string.IsNullOrWhiteSpace(path))
            {
                SheetEditor e = Open();
                root = e.Root;
                catalog = e.Catalog;
            }
            else
            {
                try
                {
                    root = NetlistReader.ReadFile(path);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NetlistException || ex is System.Text.Json.JsonException)
                {
                    return "NOT VALID: " + path + ": " + ex.Message;
                }
                string? folder = CatalogFolder(path);
                catalog = folder == null ? null : new ChipCatalog(folder);
            }
            return SheetCheck.Report(SheetCheck.Run(root, catalog));
        }

        /// <summary>
        /// Lists the chip types on the sheet that the catalogue has no file for.
        /// </summary>
        /// <returns>
        /// The types and the parts of each.
        /// </returns>
        [McpServerTool(Name = "missing_chips"), Description("The part types on the open sheet (every level) that the chip catalogue has no file for, even through the 74 family rule, each with the parts that use it (block/part). Their pins cannot be checked until the chip file is added from its datasheet. Allowed read-only.")]
        public string MissingChips()
        {
            SheetEditor e = Open();
            if (e.Catalog == null)
                throw new McpException("No chip catalogue was found, so every type is missing.");
            SheetCheckResult r = SheetCheck.Run(e.Root, e.Catalog);
            if (r.Missing.Count == 0)
                return "Every part type on the sheet has a chip file.";
            StringBuilder sb = new();
            sb.Append(r.Missing.Count.ToString(CultureInfo.InvariantCulture)).Append(" type(s) with no chip file:\n");
            for (int i = 0; i < r.Missing.Count; i++)
                sb.Append(r.Missing[i]).Append(": ").Append(string.Join(" ", r.MissingParts[r.Missing[i]])).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Checks a card's reading files.
        /// </summary>
        /// <param name="folder">
        /// The folder of reading files.
        /// </param>
        /// <returns>
        /// The report.
        /// </returns>
        [McpServerTool(Name = "check_readings"), Description("Read and check a card's reading files (pNN.txt in a folder, format in docs/3207-readings/README.md), the same check as tools/card-readings.py check: per block the sheets, parts, nets, pins and gates; the types with no catalogue file; and every PROBLEM (a pin of no part, a pin in two nets or two blocks, a part with two types, a pin the chip does not have, a supply pin off the supply, a port that is not a net, a gate pin in no net and not open/nc, a gate not named after its output). A good reading set has 0 problems. Uses the open sheet's catalogue, else the one found from the folder. Allowed read-only.")]
        public string CheckReadings([Description("Full path of the folder of reading files, e.g. .../docs/3207-readings")] string folder)
            => CardReadings.Report(LoadReadings(folder));

        /// <summary>
        /// Writes a card's reading files into the open sheet.
        /// </summary>
        /// <param name="folder">
        /// The folder of reading files.
        /// </param>
        /// <returns>
        /// The check report and the blocks written.
        /// </returns>
        [McpServerTool(Name = "import_readings"), Description("Check a card's reading files and write them into the open sheet, as tools/card-readings.py sheet does (edit mode; ONE undo step): every block that has a reading and is drawn as a box on another reading's sheet (a port line) gets its parts, gates, nets, pages and ports (directions inferred from the catalogue pin roles, never read), and an instance in the block whose sheet draws its box; connector parts become connectors J.REF. The top level, the board facts and blocks with no reading stay. Written even with problems (they are listed, as in the script). Then save_sheet writes the file and raises its file stamp.")]
        public string ImportReadings([Description("Full path of the folder of reading files")] string folder)
        {
            ReadingSet set = LoadReadings(folder);
            return Run(e =>
            {
                List<string> blocks = e.ApplyReadings(set);
                return CardReadings.Report(set) + "Wrote " + blocks.Count.ToString(CultureInfo.InvariantCulture) + " block(s) into the sheet: " + string.Join(" ", blocks)
                    + ". Not saved yet: use save_sheet.";
            });
        }

        private ReadingSet LoadReadings(string folder)
        {
            ChipCatalog? cat = _editor?.Catalog;
            if (cat == null)
            {
                string? found = CatalogFolder(Path.Combine(folder, "x"));
                cat = found == null ? null : new ChipCatalog(found);
            }
            try
            {
                return CardReadings.Load(folder, cat);
            }
            catch (Exception ex) when (ex is ReadingException || ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new McpException("Could not read the readings in " + folder + ": " + ex.Message, ex);
            }
        }
    }
}
