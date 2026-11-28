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
    /// The library tools: the card library by machine family (File > Card library), the bus use
    /// of every card (File > Bus use, with its Markdown report) and the chip catalogue. They need
    /// no open sheet; when one is open its catalogue is used and, for bus use, the sheet as it is
    /// in memory stands in for its file, as in the window.
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>
        /// Lists the cards in the library.
        /// </summary>
        /// <param name="family">
        /// A machine family to show only, or empty for all.
        /// </param>
        /// <returns>
        /// One card per line.
        /// </returns>
        [McpServerTool(Name = "list_cards"), Description("List every card in the library (the sheets folder beside the chip catalogue), as File > Card library does: family (machine), sheet number, title (and the board's own name), pages, parts on every level, and the file to give to open_sheet. family: show only that machine family (NORD-10, ND-100 ...); empty for all. Files that could not be read are named.")]
        public string ListCards([Description("Machine family, e.g. ND-100; empty for all")] string? family = null)
        {
            string folder = LibraryFolder();
            List<string> problems = new();
            List<CardEntry> cards = CardLibrary.Scan(folder, problems);
            StringBuilder sb = new();
            int shown = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                CardEntry c = cards[i];
                if (!string.IsNullOrWhiteSpace(family) && !string.Equals(c.Family, family.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                shown++;
                string title = c.BoardTitle != null && c.BoardTitle != c.Title ? c.Title + " (" + c.BoardTitle + ")" : c.Title;
                sb.Append(CultureInfo.InvariantCulture, $"{c.Family} {c.Id}: {title}; {c.Pages} page(s), {c.Parts} parts; {c.File}\n");
            }
            sb.Insert(0, folder + ": " + shown.ToString(CultureInfo.InvariantCulture) + " card file(s)" + (string.IsNullOrWhiteSpace(family) ? "" : " in " + family) + ".\n");
            for (int i = 0; i < problems.Count; i++)
                sb.Append("Not read: ").Append(problems[i]).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Lists the machine families in the library.
        /// </summary>
        /// <returns>
        /// The family names, one per line, with how many cards each has.
        /// </returns>
        [McpServerTool(Name = "list_families"), Description("The machine families the library's cards name (NORD-10, ND-100 ...), each with how many card files it has; a card with no machine is under 'Not known'.")]
        public string ListFamilies()
        {
            List<string> problems = new();
            List<CardEntry> cards = CardLibrary.Scan(LibraryFolder(), problems);
            List<string> families = CardLibrary.Families(cards);
            StringBuilder sb = new();
            for (int f = 0; f < families.Count; f++)
            {
                int count = 0;
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i].Family == families[f])
                        count++;
                }
                sb.Append(families[f]).Append(": ").Append(count.ToString(CultureInfo.InvariantCulture)).Append(" card file(s)\n");
            }
            return sb.Length == 0 ? "The library has no cards." : sb.ToString();
        }

        /// <summary>
        /// The backplane signals of every card, with the chips on them.
        /// </summary>
        /// <param name="signal">
        /// Show only signals whose name holds this, or empty.
        /// </param>
        /// <param name="card">
        /// Show only this card (sheet number), or empty.
        /// </param>
        /// <param name="bus">
        /// nd-100, nord-10, or empty for both.
        /// </param>
        /// <returns>
        /// One signal per line, each followed by its chip pins.
        /// </returns>
        [McpServerTool(Name = "bus_use"), Description("Every backplane signal of every card (ND-100 plugs, NORD-10 edge fingers) as File > Bus use shows it: card, signal, backplane pins, the bus catalogue's name, D when a chip drives it and R when one reads it, then each chip pin on it down through the blocks (path, part, type, * when the pins come from another file of the 74 family, pin, pin name, what it does). The open sheet counts as it is in memory. Filter by signal (part of the name, any case), card (sheet number) and bus (nd-100 or nord-10).")]
        public string BusUseOf([Description("Part of a signal name, e.g. IOXE; empty for all")] string? signal = null,
            [Description("Card sheet number, e.g. 2A50; empty for all")] string? card = null,
            [Description("nd-100, nord-10, or empty for both")] string? bus = null)
        {
            List<BusSignalUse> shown = BusUseFiltered(signal, card, bus, out string status);
            StringBuilder sb = new();
            sb.Append(status).Append('\n');
            for (int i = 0; i < shown.Count; i++)
            {
                BusSignalUse u = shown[i];
                sb.Append(CultureInfo.InvariantCulture, $"{u.Card} {u.Signal} [{string.Join(" ", u.BackplanePins)}] {u.CatalogueName ?? "-"} {(u.Driven ? "D" : "-")}{(u.Received ? "R" : "-")}\n");
                for (int k = 0; k < u.Chips.Count; k++)
                {
                    BusChipPin c = u.Chips[k];
                    sb.Append(CultureInfo.InvariantCulture, $"    {(c.Path.Length == 0 ? "(top)" : c.Path)} {c.Part} {c.Type}{(c.ReadFrom != null ? "*" : "")} pin {c.Pin} {c.PinName}: {BusUse.RoleText(c)}\n");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// The bus use report as Markdown.
        /// </summary>
        /// <param name="path">
        /// A file to write it to, or empty to return the text.
        /// </param>
        /// <param name="signal">
        /// Show only signals whose name holds this, or empty.
        /// </param>
        /// <param name="card">
        /// Show only this card, or empty.
        /// </param>
        /// <param name="bus">
        /// nd-100, nord-10, or empty for both.
        /// </param>
        /// <returns>
        /// The Markdown, or where it was written.
        /// </returns>
        [McpServerTool(Name = "bus_use_report"), Description("The bus use report as Markdown, the same as the Bus use window's 'Save report': a table per signal of every card's chips on it. With a path it is written to that file (only the report file is written); without, the Markdown is returned. Same filters as bus_use.")]
        public string BusUseReport([Description("Full path of the .md file to write; empty to return the text")] string? path = null,
            [Description("Part of a signal name; empty for all")] string? signal = null,
            [Description("Card sheet number; empty for all")] string? card = null,
            [Description("nd-100, nord-10, or empty for both")] string? bus = null)
        {
            string md = BusUse.ToMarkdown(BusUseFiltered(signal, card, bus, out _));
            if (string.IsNullOrWhiteSpace(path))
                return md;
            try
            {
                File.WriteAllText(path, md);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new McpException("Could not write " + path + ": " + ex.Message, ex);
            }
            return "Wrote " + path + ".";
        }

        /// <summary>
        /// One chip from the catalogue.
        /// </summary>
        /// <param name="type">
        /// The chip type.
        /// </param>
        /// <returns>
        /// Title, pins, formulas and gates.
        /// </returns>
        [McpServerTool(Name = "get_chip"), Description("One chip type from the catalogue (catalog/chips): title, which file the pins come from (a 74 family part with no file of its own, such as 74LS240, uses the plain 74240; failing that the Schottky 74S file of the same number, such as 74S112 for 74LS112: same pins, other timing), every pin (number, name, inverted, role, output type), the formulas, and the gates that follow from them. A passive type (R, C, RN-SIP-8, SW-DIP-4 ...) gives its pins. Says so when there is no file: never guessed.")]
        public string GetChip([Description("Chip type, e.g. 74LS240")] string type)
        {
            ChipCatalog cat = CatalogForLookup();
            ChipInfo chip = cat.Get(type.Trim()) ?? throw new McpException("The catalogue (" + cat.Folder + ") has no chip file for " + type + " (nor for its plain or Schottky 74 type). It must be added from its datasheet.");
            StringBuilder sb = new();
            sb.Append(chip.Id).Append(": ").Append(chip.Title).Append('\n');
            if (chip.ReadFrom != null)
                sb.Append("Pins and logic from ").Append(chip.ReadFrom).Append(".json (same family number, timing not the same)\n");
            sb.Append("Pins:\n");
            for (int i = 0; i < chip.Pins.Count; i++)
            {
                ChipPin p = chip.Pins[i];
                sb.Append(CultureInfo.InvariantCulture, $"  {p.Number,3} {(p.Inverted ? "/" : "")}{p.Name,-10} {p.Role}{(p.OutputType.Length == 0 ? "" : " (" + p.OutputType + ")")}\n");
            }
            if (chip.Formulas.Count > 0)
            {
                sb.Append("Formulas:\n");
                for (int i = 0; i < chip.Formulas.Count; i++)
                    sb.Append("  ").Append(chip.Formulas[i]).Append('\n');
            }
            if (chip.Gates.Count > 0)
            {
                sb.Append("Gates:\n");
                for (int i = 0; i < chip.Gates.Count; i++)
                {
                    Gate g = chip.Gates[i];
                    sb.Append("  ").Append(g.Kind.ToString().ToUpperInvariant()).Append(" in ").Append(string.Join(",", g.Inputs)).Append(" out ").Append(g.Output.ToString(CultureInfo.InvariantCulture)).Append('\n');
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Lists the chip types the catalogue has.
        /// </summary>
        /// <param name="filter">
        /// Show only types holding this, or empty for all.
        /// </param>
        /// <returns>
        /// The types, separated by spaces.
        /// </returns>
        [McpServerTool(Name = "list_chips"), Description("The chip types that have a file in the catalogue, sorted. filter: only types holding the text (any case). A family part with no file of its own (74LS00) is not listed but is found by get_chip through its plain type.")]
        public string ListChips([Description("Part of a type, e.g. 74LS; empty for all")] string? filter = null)
        {
            ChipCatalog cat = CatalogForLookup();
            List<string> types = cat.Types();
            List<string> picked = new();
            for (int i = 0; i < types.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(filter) || types[i].Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase))
                    picked.Add(types[i]);
            }
            return picked.Count.ToString(CultureInfo.InvariantCulture) + " chip type(s) in " + cat.Folder + ":\n" + string.Join(" ", picked);
        }

        // the bus use of every card with the filters applied, and the status line of the window
        private List<BusSignalUse> BusUseFiltered(string? signal, string? card, string? bus, out string status)
        {
            MachineBus? only = string.IsNullOrWhiteSpace(bus) ? null : bus.Trim().ToLowerInvariant() switch
            {
                "nd-100" => MachineBus.Nd100,
                "nord-10" => MachineBus.Nord10,
                _ => throw new McpException("bus must be nd-100, nord-10 or empty, not '" + bus + "'."),
            };
            ChipCatalog? cat = CatalogOrNull();
            StringBuilder note = new();
            List<BusSignalUse> all = BusUse.OfLibrary(CardLibrary.FolderBeside(cat?.Folder ?? _catalogFolder), cat, _editor?.Root, _path, note, out int cards);
            List<BusSignalUse> shown = new();
            for (int i = 0; i < all.Count; i++)
            {
                BusSignalUse u = all[i];
                if (only != null && u.Bus != only)
                    continue;
                if (!string.IsNullOrWhiteSpace(card) && !string.Equals(u.Card, card.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrWhiteSpace(signal) && !u.Signal.Contains(signal.Trim(), StringComparison.OrdinalIgnoreCase)
                    && (u.CatalogueName == null || !u.CatalogueName.Contains(signal.Trim(), StringComparison.OrdinalIgnoreCase)))
                    continue;
                shown.Add(u);
            }
            status = cards.ToString(CultureInfo.InvariantCulture) + " card(s), " + all.Count.ToString(CultureInfo.InvariantCulture) + " backplane signals, "
                + shown.Count.ToString(CultureInfo.InvariantCulture) + " shown. " + note;
            return shown;
        }

        // the open sheet's catalogue, else the one found from the program's folder
        private ChipCatalog? CatalogOrNull()
        {
            if (_editor?.Catalog != null)
                return _editor.Catalog;
            return _catalogFolder == null ? null : new ChipCatalog(_catalogFolder);
        }

        private ChipCatalog CatalogForLookup() => CatalogOrNull() ?? throw new McpException("No chip catalogue was found (catalog/chips above the sheet or the program).");

        private string LibraryFolder()
            => CardLibrary.FolderBeside(CatalogOrNull()?.Folder) ?? throw new McpException("No library folder: no chip catalogue was found, and the sheets folder is beside it.");
    }
}
