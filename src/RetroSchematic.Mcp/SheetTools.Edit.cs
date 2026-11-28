//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// The edit tools for the facts of parts, gates, signals and the sheet itself: everything
    /// the reading files and the window's Details panel set, one change at a time. Each needs
    /// edit mode, is one undo step, and goes through the <see cref="SheetEditor"/> the window
    /// uses; save_sheet writes the file (and raises its file stamp).
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>
        /// Renames a part.
        /// </summary>
        /// <param name="reference">
        /// The part's name now.
        /// </param>
        /// <param name="newReference">
        /// The new name.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "rename_part"), Description("Give a part on the current level a new board position or name (edit mode; undoable). Its gates (13D/6 becomes 12D/6), its pins on every signal, a passive part serving it, and on the top level its symbol boxes and hand-set board box follow.")]
        public string RenamePart([Description("The part now, e.g. 13D")] string reference, [Description("The new name, e.g. 12D")] string newReference)
            => Run(e =>
            {
                e.RenamePart(reference, newReference);
                return "Renamed " + reference + " to " + newReference.Trim() + ".";
            });

        /// <summary>
        /// Sets a part's facts.
        /// </summary>
        /// <param name="reference">
        /// The part.
        /// </param>
        /// <param name="type">
        /// The new type, or empty to keep.
        /// </param>
        /// <param name="function">
        /// What it does; empty keeps, "-" removes.
        /// </param>
        /// <param name="confidence">
        /// read, inferred or open; empty keeps.
        /// </param>
        /// <param name="note">
        /// Free text; empty keeps, "-" removes.
        /// </param>
        /// <param name="value">
        /// A passive part's value as written; empty keeps, "-" removes.
        /// </param>
        /// <param name="tolerance">
        /// A passive part's tolerance as written; empty keeps, "-" removes.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_part"), Description("Set a part's facts on the current level (edit mode; undoable): type (as printed: 74LS240), function (what it does), confidence (read, inferred or open), note, and for a passive part value and tolerance (only as written on the drawing, never guessed). Empty keeps a fact; a single - removes it.")]
        public string SetPart([Description("Board position or name, e.g. 13D")] string reference,
            [Description("New type; empty keeps")] string? type = null,
            [Description("What it does; empty keeps, - removes")] string? function = null,
            [Description("read, inferred or open; empty keeps")] string? confidence = null,
            [Description("Note; empty keeps, - removes")] string? note = null,
            [Description("Passive value as written; empty keeps, - removes")] string? value = null,
            [Description("Passive tolerance as written; empty keeps, - removes")] string? tolerance = null)
            => Run(e =>
            {
                e.SetPartInfo(reference, Keep(type), KeepOrRemove(function), string.IsNullOrWhiteSpace(confidence) ? null : ParseConfidence(confidence),
                    KeepOrRemove(note), KeepOrRemove(value), KeepOrRemove(tolerance));
                return "Set the facts of " + reference + ".";
            });

        /// <summary>
        /// Adds a gate to a part.
        /// </summary>
        /// <param name="reference">
        /// The part.
        /// </param>
        /// <param name="kind">
        /// The gate kind.
        /// </param>
        /// <param name="inputs">
        /// The input pins, separated by commas.
        /// </param>
        /// <param name="output">
        /// The output pin.
        /// </param>
        /// <param name="confidence">
        /// read, inferred or open.
        /// </param>
        /// <param name="note">
        /// Free text, or empty.
        /// </param>
        /// <returns>
        /// The gate's name.
        /// </returns>
        [McpServerTool(Name = "add_gate"), Description("Add a gate to a part on the current level (edit mode; undoable), named after its output pin (13D/6). kind: nand, nor, not, and, or, buffer, and-or-invert or other. When the catalogue has the chip's file every pin must be on the chip. Wire its pins with connect_pins.")]
        public string AddGate([Description("Board position, e.g. 13D")] string reference, [Description("nand, nor, not, and, or, buffer, and-or-invert or other")] string kind,
            [Description("Input pins separated by commas, e.g. 4,5")] string inputs, [Description("Output pin, e.g. 6")] int output,
            [Description("read, inferred or open")] string confidence = "read", [Description("Note, or empty")] string? note = null)
            => Run(e => "Added gate " + e.AddGate(reference, ParseGateKind(kind), ParsePins(inputs), output, ParseConfidence(confidence), note) + ".");

        /// <summary>
        /// Removes a gate.
        /// </summary>
        /// <param name="id">
        /// The gate's name.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "remove_gate"), Description("Remove a gate (by its name, 13D/6) from the current level (edit mode; undoable). The part and its pins on the signals stay.")]
        public string RemoveGate([Description("Gate name, e.g. 13D/6")] string id)
            => Run(e =>
            {
                e.RemoveGate(id);
                return "Removed gate " + id + ".";
            });

        /// <summary>
        /// Adds a signal with no pins.
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        /// <param name="active">
        /// 0 (low), 1 (high) or none.
        /// </param>
        /// <returns>
        /// The signal's key.
        /// </returns>
        [McpServerTool(Name = "add_net"), Description("Add a signal with no pins yet to the current level (edit mode; undoable): one that leaves a block through a port (enter_block first), or one connect_pin_on_page or connect_pins fills later. active: 0 (active low), 1 (active high) or none (a supply, or an unnamed REF_PIN signal).")]
        public string AddNet([Description("Signal name as stored, without the level mark, e.g. IOXE")] string name, [Description("0, 1 or none")] string active)
            => Run(e => "Added signal " + e.AddNet(name, ParseLevel(active)) + ".");

        /// <summary>
        /// Renames a signal or sets its facts.
        /// </summary>
        /// <param name="net">
        /// The signal's key.
        /// </param>
        /// <param name="newName">
        /// The new name, or empty to keep.
        /// </param>
        /// <param name="active">
        /// 0, 1 or none; empty keeps.
        /// </param>
        /// <param name="printed">
        /// As printed; empty keeps, "-" removes.
        /// </param>
        /// <param name="note">
        /// Note; empty keeps, "-" removes.
        /// </param>
        /// <param name="pullUp">
        /// Pull-up; empty keeps, "-" removes.
        /// </param>
        /// <returns>
        /// The signal's key afterwards.
        /// </returns>
        [McpServerTool(Name = "set_net"), Description("Rename a signal on the current level or set its facts (edit mode; undoable): new name, active level (0, 1 or none), the name as printed on the drawing (BD~00), note, pull-up resistor. A rename is followed by its fixed wire path, its bus membership, the instance ports it is connected to and, inside a block, the block's own ports. A name and level another signal has is refused: connect the pins to join them. Empty keeps a fact; a single - removes it. The description is set with set_signal_description.")]
        public string SetNet([Description("Signal key, e.g. IOXE_0")] string net,
            [Description("New name; empty keeps")] string? newName = null,
            [Description("0, 1 or none; empty keeps")] string? active = null,
            [Description("As printed; empty keeps, - removes")] string? printed = null,
            [Description("Note; empty keeps, - removes")] string? note = null,
            [Description("Pull-up resistor, e.g. R12; empty keeps, - removes")] string? pullUp = null)
            => Run(e => "The signal is now " + e.SetNet(net, Keep(newName), string.IsNullOrWhiteSpace(active) ? null : ParseLevel(active),
                KeepOrRemove(printed), KeepOrRemove(note), KeepOrRemove(pullUp)) + ".");

        /// <summary>
        /// Sets the sheet's title block.
        /// </summary>
        /// <param name="id">
        /// The sheet number; empty keeps.
        /// </param>
        /// <param name="title">
        /// The title; empty keeps.
        /// </param>
        /// <param name="machine">
        /// The machine; empty keeps, "-" removes.
        /// </param>
        /// <param name="drawn">
        /// Drawn by and when; empty keeps, "-" removes.
        /// </param>
        /// <param name="updated">
        /// Last change; empty keeps, "-" removes.
        /// </param>
        /// <param name="company">
        /// Company; empty keeps, "-" removes.
        /// </param>
        /// <param name="note">
        /// Note; empty keeps, "-" removes.
        /// </param>
        /// <param name="document">
        /// Source document; empty keeps.
        /// </param>
        /// <param name="pdfPage">
        /// Page in it; -1 keeps, 0 removes.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_sheet_info"), Description("Set the sheet's title block (edit mode; undoable): number, title, machine (the family the card library lists it under: NORD-10, ND-100), drawn, updated, company, note, and the source document and page it was read from. Empty keeps a fact; a single - removes it (not the number or title). The machine bus is set with set_bus.")]
        public string SetSheetInfo([Description("Sheet number; empty keeps")] string? id = null, [Description("Title; empty keeps")] string? title = null,
            [Description("Machine family; empty keeps, - removes")] string? machine = null, [Description("Drawn by/when; empty keeps, - removes")] string? drawn = null,
            [Description("Last change written on it; empty keeps, - removes")] string? updated = null, [Description("Company; empty keeps, - removes")] string? company = null,
            [Description("Note; empty keeps, - removes")] string? note = null, [Description("Source document; empty keeps")] string? document = null,
            [Description("Page in the source document; -1 keeps, 0 removes")] int pdfPage = -1)
            => Run(e =>
            {
                e.SetSheetInfo(Keep(id), Keep(title), KeepOrRemove(machine), KeepOrRemove(drawn), KeepOrRemove(updated), KeepOrRemove(company), KeepOrRemove(note),
                    Keep(document), pdfPage < 0 ? null : pdfPage);
                return "Set the title block of " + e.Root.Sheet.Id + ".";
            });

        /// <summary>
        /// Sets which machine bus the card is for.
        /// </summary>
        /// <param name="bus">
        /// nord-10, nd-100 or unknown.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_bus"), Description("Set which machine bus the card is for: nord-10, nd-100 or unknown. It picks the bus-signal and connector catalogues. Like the window, allowed read-only (it changes no connection); undoable and saved with the sheet.")]
        public string SetBus([Description("nord-10, nd-100 or unknown")] string bus)
        {
            SheetEditor e = Open();
            MachineBus b = ParseBus(bus);
            e.SetBus(b);
            return "The card is for the " + SheetSession.BusText(b) + (b == MachineBus.Unknown ? "." : " bus.");
        }

        /// <summary>
        /// Sets the sheet's own display values.
        /// </summary>
        /// <param name="display">
        /// The display section as JSON, or empty to remove it.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_display"), Description("Set the sheet's own display values (the window's View toggles and Settings, saved with the sheet), as JSON in the file's names: nameStyle (slash, tilde, subscript), symbols (nd, modern), theme, pinNumbers, boardPositions, portArrows, buses, tracePulse (true/false), lineWidth. A value left out uses the viewer's default; empty text removes the section. Display only: allowed read-only, undoable.")]
        public string SetDisplay([Description("The display section as JSON, e.g. {\"pinNumbers\":true}; empty removes it")] string display)
        {
            SheetEditor e = Open();
            SheetDisplay? d = string.IsNullOrWhiteSpace(display) ? null : Parse<SheetDisplay>(display);
            e.SetDisplay(d);
            return d == null ? "Removed the display section." : "Set the display section.";
        }

        /// <summary>
        /// Converts a card without pages to format version 3.
        /// </summary>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "convert_to_pages"), Description("Turn a card without pages (format version 1 or 2) into format version 3, as File > Convert to a card with pages does: the sheet becomes page 1 holding every part; nothing that is connected changes (edit mode; undoable).")]
        public string ConvertToPages()
            => Run(e => e.ConvertToPages() ? "The card now has pages (format version 3): everything is on page 1." : "The card has pages already.");

        /// <summary>
        /// Sets a card page's facts.
        /// </summary>
        /// <param name="id">
        /// The page's id.
        /// </param>
        /// <param name="title">
        /// The title, or empty for none.
        /// </param>
        /// <param name="source">
        /// The source page number, 0 for none.
        /// </param>
        /// <param name="sheetNumber">
        /// The sheet number in its title block, or empty.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_page_info"), Description("Set a card page's title, the original page it is read from (its number in list_sources, 0 for none) and the sheet number in its title block (edit mode; undoable). Every value is replaced: empty means none.")]
        public string SetPageInfo([Description("Page id, e.g. 2")] string id, [Description("Title, as in its title block")] string? title = null,
            [Description("Source page number in list_sources, 0 for none")] int source = 0, [Description("Sheet number in the title block, e.g. 2A50/2")] string? sheetNumber = null)
            => Run(e =>
            {
                e.SetPageInfo(id, string.IsNullOrWhiteSpace(title) ? null : title, source == 0 ? null : source, string.IsNullOrWhiteSpace(sheetNumber) ? null : sheetNumber);
                return "Set page " + id + ".";
            });

        // empty text keeps a fact (null to the editor)
        private static string? Keep(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

        // empty text keeps a fact (null), a single - removes it (empty to the editor)
        private static string? KeepOrRemove(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim() == "-" ? "" : text;

        private static Confidence ParseConfidence(string text) => text.Trim().ToLowerInvariant() switch
        {
            "read" => Confidence.Read,
            "inferred" => Confidence.Inferred,
            "open" => Confidence.Open,
            _ => throw new McpException("confidence must be read, inferred or open, not '" + text + "'."),
        };

        private static ActiveLevel ParseLevel(string text) => text.Trim().ToLowerInvariant() switch
        {
            "0" or "low" => ActiveLevel.Low,
            "1" or "high" => ActiveLevel.High,
            "none" or "" => ActiveLevel.None,
            _ => throw new McpException("active must be 0, 1 or none, not '" + text + "'."),
        };

        private static MachineBus ParseBus(string text) => text.Trim().ToLowerInvariant() switch
        {
            "nord-10" => MachineBus.Nord10,
            "nd-100" => MachineBus.Nd100,
            "unknown" => MachineBus.Unknown,
            _ => throw new McpException("bus must be nord-10, nd-100 or unknown, not '" + text + "'."),
        };

        private static GateKind ParseGateKind(string text) => text.Trim().ToLowerInvariant() switch
        {
            "nand" => GateKind.Nand,
            "nor" => GateKind.Nor,
            "not" => GateKind.Not,
            "and" => GateKind.And,
            "or" => GateKind.Or,
            "buffer" => GateKind.Buffer,
            "and-or-invert" => GateKind.AndOrInvert,
            "other" => GateKind.Other,
            _ => throw new McpException("kind must be nand, nor, not, and, or, buffer, and-or-invert or other, not '" + text + "'."),
        };

        private static List<int> ParsePins(string text)
        {
            List<int> pins = new();
            string[] parts = text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out int pin) || pin < 1)
                    throw new McpException("\"" + parts[i] + "\" is not a pin number.");
                pins.Add(pin);
            }
            return pins;
        }
    }
}
