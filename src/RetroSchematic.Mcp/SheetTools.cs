//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
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
    /// The MCP tools: one open sheet at a time, changed only through the same
    /// <see cref="SheetEditor"/> the window uses. A sheet opens read-only unless asked otherwise,
    /// and every change is refused until edit mode is on. A refused change comes back as a tool
    /// error whose text says why; the sheet is left as it was.
    /// </summary>
    [McpServerToolType]
    public sealed class SheetTools
    {
        private readonly string? _catalogFolder;
        private SheetEditor? _editor;
        private string? _path;

        /// <summary>
        /// Creates the tools. The chip catalogue is found by walking up from
        /// <paramref name="catalogStart"/> and, for a sheet file, from the sheet.
        /// </summary>
        /// <param name="catalogStart">
        /// A folder to look for <c>catalog/chips</c> from, usually the program's folder.
        /// </param>
        public SheetTools(string catalogStart)
        {
            _catalogFolder = ChipCatalog.Find(catalogStart);
        }

        /// <summary>
        /// The open sheet's editor, or null when none is open.
        /// </summary>
        public SheetEditor? Editor => _editor;

        /// <summary>
        /// Starts a new, empty sheet in edit mode.
        /// </summary>
        /// <param name="id">
        /// The sheet number.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "new_sheet"), Description("Start a new, empty sheet (no parts, no signals), already in edit mode. Save it with save_sheet and a path.")]
        public string NewSheet([Description("Sheet number, e.g. 2A51")] string id, [Description("Sheet title")] string title)
        {
            _editor = new SheetEditor(SheetEditor.NewSheet(id, title), Catalog(null)) { ReadOnly = false };
            _path = null;
            return "New sheet " + id + " in edit mode." + (_editor.Catalog == null ? " No chip catalogue was found, so no chip can be placed." : "");
        }

        /// <summary>
        /// Opens a sheet file.
        /// </summary>
        /// <param name="path">
        /// The sheet file.
        /// </param>
        /// <param name="edit">
        /// True to open it in edit mode; false (the default) opens it read-only.
        /// </param>
        /// <returns>
        /// What was opened.
        /// </returns>
        [McpServerTool(Name = "open_sheet"), Description("Open an nd-netlist sheet file. It opens READ-ONLY unless edit is true; every change is refused in read-only mode.")]
        public string OpenSheet([Description("Path of the .json sheet file")] string path, [Description("True to open in edit mode")] bool edit = false)
        {
            Netlist n;
            try
            {
                n = NetlistReader.ReadFile(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is NetlistException || e is System.Text.Json.JsonException)
            {
                throw new McpException("Could not open " + path + ": " + e.Message, e);
            }
            _editor = new SheetEditor(n, Catalog(path)) { ReadOnly = !edit };
            _path = path;
            return string.Create(CultureInfo.InvariantCulture,
                $"Opened {n.Sheet.Id} {n.Sheet.Title}: {n.Parts.Count} parts, {n.Gates.Count} gates, {n.Nets.Count} signals, {(edit ? "edit mode" : "read-only")}.");
        }

        /// <summary>
        /// Switches edit mode on or off.
        /// </summary>
        /// <param name="on">
        /// True for edit mode.
        /// </param>
        /// <returns>
        /// The new mode.
        /// </returns>
        [McpServerTool(Name = "set_edit_mode"), Description("Switch the open sheet between read-only (every change refused) and edit mode.")]
        public string SetEditMode([Description("True for edit mode, false for read-only")] bool on)
        {
            Open().ReadOnly = !on;
            return on ? "Edit mode." : "Read-only.";
        }

        /// <summary>
        /// Lists the parts.
        /// </summary>
        /// <returns>
        /// One line per part: reference, type, position, gates.
        /// </returns>
        [McpServerTool(Name = "list_parts"), Description("List the chips on the open sheet: board position, type, frame position in mm if placed by hand, and gates.")]
        public string ListParts()
        {
            Netlist n = Open().Netlist;
            StringBuilder sb = new();
            for (int i = 0; i < n.Parts.Count; i++)
            {
                Part p = n.Parts[i];
                sb.Append(p.Ref).Append(' ').Append(p.Type);
                if (p.Position != null)
                    sb.Append(CultureInfo.InvariantCulture, $" at ({p.Position.X:0.#}, {p.Position.Y:0.#}) mm");
                for (int j = 0; j < n.Gates.Count; j++)
                {
                    Gate g = n.Gates[j];
                    if (g.Part != p.Ref)
                        continue;
                    sb.Append("  ").Append(g.Id).Append(' ').Append(g.Kind.ToString().ToUpperInvariant()).Append(" in ");
                    for (int k = 0; k < g.Inputs.Count; k++)
                        sb.Append(k == 0 ? "" : ",").Append(g.Inputs[k].ToString(CultureInfo.InvariantCulture));
                    sb.Append(" out ").Append(g.Output.ToString(CultureInfo.InvariantCulture));
                }
                sb.Append('\n');
            }
            return sb.Length == 0 ? "No parts." : sb.ToString();
        }

        /// <summary>
        /// Lists the signals.
        /// </summary>
        /// <returns>
        /// One line per signal with its pins.
        /// </returns>
        [McpServerTool(Name = "list_nets"), Description("List the signals on the open sheet and the pins each one joins.")]
        public string ListNets()
        {
            Netlist n = Open().Netlist;
            StringBuilder sb = new();
            for (int i = 0; i < n.Nets.Count; i++)
            {
                Net net = n.Nets[i];
                sb.Append(net.Name).Append(':');
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    sb.Append(' ').Append(net.Pins[j].At);
                    if (net.Pins[j].Confidence != Confidence.Read)
                        sb.Append('(').Append(net.Pins[j].Confidence.ToString().ToLowerInvariant()).Append(')');
                }
                sb.Append('\n');
            }
            return sb.Length == 0 ? "No signals." : sb.ToString();
        }

        /// <summary>
        /// Places a chip.
        /// </summary>
        /// <param name="type">
        /// The chip type.
        /// </param>
        /// <param name="xMm">
        /// Frame left edge in mm.
        /// </param>
        /// <param name="yMm">
        /// Frame top edge in mm.
        /// </param>
        /// <param name="reference">
        /// The board position to give it, or null for the first free one.
        /// </param>
        /// <returns>
        /// The new part's reference.
        /// </returns>
        [McpServerTool(Name = "place_part"), Description("Place a chip from the catalogue (e.g. 7400). Its gates come from the chip file. Returns the new part's board position.")]
        public string PlacePart([Description("Chip type, e.g. 7400")] string type, [Description("Frame left edge in mm")] double xMm,
            [Description("Frame top edge in mm")] double yMm, [Description("Board position to use, e.g. 3C; empty for the first free one")] string? reference = null)
            => Run(e => "Placed " + type + " as " + e.PlacePart(type, xMm, yMm, string.IsNullOrEmpty(reference) ? null : reference) + ".");

        /// <summary>
        /// Moves a chip.
        /// </summary>
        /// <param name="reference">
        /// The part.
        /// </param>
        /// <param name="xMm">
        /// New frame left edge in mm.
        /// </param>
        /// <param name="yMm">
        /// New frame top edge in mm.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "move_part"), Description("Move a chip's frame to a new place (mm). Only the drawing changes, not the signals.")]
        public string MovePart([Description("Board position, e.g. 13D")] string reference, [Description("New left edge in mm")] double xMm, [Description("New top edge in mm")] double yMm)
            => Run(e =>
            {
                e.MovePart(reference, xMm, yMm);
                return "Moved " + reference + ".";
            });

        /// <summary>
        /// Connects two pins.
        /// </summary>
        /// <param name="pinA">
        /// One pin.
        /// </param>
        /// <param name="pinB">
        /// The other pin.
        /// </param>
        /// <param name="netName">
        /// Name for a new signal, or null.
        /// </param>
        /// <returns>
        /// The signal they are now on.
        /// </returns>
        [McpServerTool(Name = "connect_pins"), Description("Join two pins (written 11D.3; P.12 backplane, F.3 front). Two free pins make a new signal; a free pin joins the other's signal. A pin can be on one signal only.")]
        public string ConnectPins([Description("First pin, e.g. 11D.3")] string pinA, [Description("Second pin")] string pinB,
            [Description("Name for a new signal; empty for N1, N2, ...")] string? netName = null)
            => Run(e => "Connected " + pinA + " and " + pinB + " on " + e.Connect(pinA, pinB, string.IsNullOrEmpty(netName) ? null : netName) + ".");

        /// <summary>
        /// Takes a pin off its signal.
        /// </summary>
        /// <param name="pin">
        /// The pin.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "disconnect_pin"), Description("Take a pin off its signal. A signal left with no pins is removed.")]
        public string DisconnectPin([Description("The pin, e.g. 11D.3")] string pin)
            => Run(e =>
            {
                e.Disconnect(pin);
                return "Disconnected " + pin + ".";
            });

        /// <summary>
        /// Deletes a chip or a signal.
        /// </summary>
        /// <param name="what">
        /// "part" or "net".
        /// </param>
        /// <param name="name">
        /// The part reference or signal name.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "delete"), Description("Delete a chip (what = part; also its gates and its pins on every signal) or a signal (what = net).")]
        public string Delete([Description("part or net")] string what, [Description("Board position or signal name")] string name)
            => Run(e =>
            {
                if (what == "part")
                    e.DeletePart(name);
                else if (what == "net")
                    e.DeleteNet(name);
                else
                    throw new SheetEditException("what must be part or net, not \"" + what + "\".");
                return "Deleted " + what + " " + name + ".";
            });

        /// <summary>
        /// Undoes the last change.
        /// </summary>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "undo"), Description("Take back the last change.")]
        public string Undo() => Run(e =>
        {
            e.Undo();
            return "Undone.";
        });

        /// <summary>
        /// Redoes the last undone change.
        /// </summary>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "redo"), Description("Do the last undone change again.")]
        public string Redo() => Run(e =>
        {
            e.Redo();
            return "Done again.";
        });

        /// <summary>
        /// Saves the sheet.
        /// </summary>
        /// <param name="path">
        /// Where to save, or null for the file it was opened from.
        /// </param>
        /// <returns>
        /// Where it was saved.
        /// </returns>
        [McpServerTool(Name = "save_sheet"), Description("Save the open sheet as nd-netlist JSON, with every position. Without a path it saves to the file it was opened from.")]
        public string SaveSheet([Description("File to write; empty for the file it was opened from")] string? path = null)
        {
            SheetEditor e = Open();
            string target = string.IsNullOrEmpty(path) ? _path ?? throw new McpException("This sheet has never been saved; give a path.") : path;
            try
            {
                e.Save(target);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SheetEditException)
            {
                throw new McpException("Could not save " + target + ": " + ex.Message, ex);
            }
            _path = target;
            return "Saved " + target + ".";
        }

        private ChipCatalog? Catalog(string? sheetPath)
        {
            string? folder = (sheetPath == null ? null : ChipCatalog.Find(sheetPath)) ?? _catalogFolder;
            return folder == null ? null : new ChipCatalog(folder);
        }

        private SheetEditor Open() => _editor ?? throw new McpException("No sheet is open; use open_sheet or new_sheet first.");

        private string Run(Func<SheetEditor, string> change)
        {
            SheetEditor e = Open();
            try
            {
                return change(e);
            }
            catch (SheetEditException ex)
            {
                throw new McpException("Refused: " + ex.Message, ex);
            }
        }
    }
}
