//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RetroSchematic.Model
{
    /// <summary>
    /// What <see cref="SheetCheck.Run"/> found.
    /// </summary>
    public sealed class SheetCheckResult
    {
        /// <summary>Null when the reader accepts the sheet, else why it refuses it.</summary>
        public string? Invalid { get; set; }

        /// <summary>
        /// Real faults: a pin the chip does not have, a power or ground pin on a signal that is not
        /// a supply. Each line names the level, the pin and the signal.
        /// </summary>
        public List<string> Problems { get; } = new();

        /// <summary>Part types with no chip file in the catalogue (their pins are not checked), sorted.</summary>
        public List<string> Missing { get; } = new();

        /// <summary>
        /// For each type in <see cref="Missing"/>, the parts that use it: the part on the top
        /// level (13D), BLOCK/13D inside a block.
        /// </summary>
        public SortedDictionary<string, List<string>> MissingParts { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gate pins that are on no signal (level, gate and pin). Not a fault by itself: a pin
        /// can be left open on purpose (an unused input, or a pin not settled from the drawing).
        /// </summary>
        public List<string> Unconnected { get; } = new();

        /// <summary>How many levels were checked: the top level and every block.</summary>
        public int Levels { get; set; }

        /// <summary>How many parts, on every level.</summary>
        public int Parts { get; set; }

        /// <summary>How many signals, on every level.</summary>
        public int Nets { get; set; }

        /// <summary>How many pins on signals, on every level.</summary>
        public int Pins { get; set; }

        /// <summary>True when the sheet is valid and has no problems.</summary>
        public bool Ok => Invalid == null && Problems.Count == 0;
    }

    /// <summary>
    /// Checks a whole sheet (top level and every block) against the reader and the chip
    /// catalogue. The same check for the command-line tool (<c>check</c>) and the MCP server
    /// (<c>check_sheet</c>).
    /// </summary>
    public static class SheetCheck
    {
        /// <summary>
        /// The names a power or ground pin may be on: the reading supply names, and 5V.
        /// </summary>
        public static readonly HashSet<string> SupplyNets = new(StringComparer.Ordinal) { "+5V", "5V", "GND", "12V", "+12V", "-12V", "5VSTBY" };

        /// <summary>
        /// Checks the sheet:
        /// <para>
        /// - the reader accepts it (written out and read back, as a save does);
        /// - every chip pin on a signal exists on the chip, when the catalogue has its file (with
        ///   the 74 family rule of <see cref="ChipCatalog.Get"/>);
        /// - a power or ground pin is only on a supply signal (<see cref="SupplyNets"/>);
        /// - the types with no chip file, and the gate pins on no signal, are listed.
        /// </para>
        /// </summary>
        /// <param name="root">
        /// The whole sheet.
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue, or null (then only the reader's check is made).
        /// </param>
        /// <returns>
        /// What was found.
        /// </returns>
        public static SheetCheckResult Run(Netlist root, ChipCatalog? catalog)
        {
            SheetCheckResult r = new();
            try
            {
                NetlistReader.Parse(NetlistReader.Write(root));
            }
            catch (NetlistException e)
            {
                r.Invalid = e.Message;
            }
            Level(r, "top", root.Parts, root.Gates, root.Nets, catalog);
            for (int i = 0; root.Blocks != null && i < root.Blocks.Count; i++)
            {
                BlockDefinition b = root.Blocks[i];
                Level(r, "block " + b.Name, b.Parts, b.Gates, b.Nets, catalog);
            }
            r.Missing.AddRange(r.MissingParts.Keys);
            return r;
        }

        /// <summary>
        /// The result in words: counts, every problem, the missing types and the open gate pins.
        /// </summary>
        /// <param name="r">
        /// The result.
        /// </param>
        /// <returns>
        /// The report, one fact per line.
        /// </returns>
        public static string Report(SheetCheckResult r)
        {
            StringBuilder sb = new();
            sb.Append(CultureInfo.InvariantCulture, $"{r.Levels} level(s), {r.Parts} parts, {r.Nets} signals, {r.Pins} pins.\n");
            if (r.Invalid != null)
                sb.Append("NOT VALID: ").Append(r.Invalid).Append('\n');
            for (int i = 0; i < r.Problems.Count; i++)
                sb.Append("PROBLEM ").Append(r.Problems[i]).Append('\n');
            if (r.Missing.Count > 0)
                sb.Append("No chip file (pins not checked): ").Append(string.Join(" ", r.Missing)).Append('\n');
            if (r.Unconnected.Count > 0)
                sb.Append(r.Unconnected.Count.ToString(CultureInfo.InvariantCulture)).Append(" gate pin(s) on no signal: ").Append(string.Join(" ", r.Unconnected)).Append('\n');
            sb.Append(r.Problems.Count.ToString(CultureInfo.InvariantCulture)).Append(" problem(s)").Append(r.Invalid == null ? "" : ", and the sheet is not valid").Append('\n');
            return sb.ToString();
        }

        private static void Level(SheetCheckResult r, string level, List<Part> parts, List<Gate> gates, List<Net> nets, ChipCatalog? catalog)
        {
            r.Levels++;
            r.Parts += parts.Count;
            r.Nets += nets.Count;
            // the part's name as listed: 13D on the top level, BLOCK/13D inside a block
            string prefix = level == "top" ? "" : level.Substring(6) + "/";
            Dictionary<string, Part> byRef = new(StringComparer.Ordinal);
            for (int i = 0; i < parts.Count; i++)
            {
                byRef[parts[i].Ref] = parts[i];
                if (catalog == null || catalog.Get(parts[i].Type) != null)
                    continue;
                if (!r.MissingParts.TryGetValue(parts[i].Type, out List<string>? users))
                {
                    users = new List<string>();
                    r.MissingParts.Add(parts[i].Type, users);
                }
                users.Add(prefix + parts[i].Ref);
            }
            HashSet<string> onNet = new(StringComparer.Ordinal);
            for (int i = 0; i < nets.Count; i++)
            {
                Net n = nets[i];
                for (int j = 0; j < n.Pins.Count; j++)
                {
                    string at = n.Pins[j].At;
                    r.Pins++;
                    onNet.Add(at);
                    int dot = at.LastIndexOf('.');
                    if (dot <= 0)
                        continue;
                    string reference = at.Substring(0, dot);
                    if (ConnectorPins.IsConnector(reference) || !byRef.TryGetValue(reference, out Part? part) || catalog == null)
                        continue;
                    ChipInfo? chip = catalog.Get(part.Type);
                    if (chip == null)
                        continue;
                    if (!int.TryParse(at.AsSpan(dot + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                        continue;
                    string? role = null;
                    for (int k = 0; k < chip.Pins.Count && role == null; k++)
                    {
                        if (chip.Pins[k].Number == number)
                            role = chip.Pins[k].Role;
                    }
                    if (role == null)
                        r.Problems.Add(level + ": " + at + " on " + n.Name + ": a " + part.Type + (chip.ReadFrom != null ? " (pins from " + chip.ReadFrom + ")" : "") + " has no pin " + number.ToString(CultureInfo.InvariantCulture));
                    else if ((role == "power" || role == "ground") && !SupplyNets.Contains(n.Name))
                        r.Problems.Add(level + ": " + at + " is a " + role + " pin of " + part.Type + " but is on signal " + n.Name);
                }
            }
            for (int i = 0; i < gates.Count; i++)
            {
                Gate g = gates[i];
                for (int k = 0; k <= g.Inputs.Count; k++)
                {
                    int pin = k < g.Inputs.Count ? g.Inputs[k] : g.Output;
                    string at = g.Part + "." + pin.ToString(CultureInfo.InvariantCulture);
                    if (!onNet.Contains(at))
                        r.Unconnected.Add(prefix + at);
                }
            }
        }
    }
}
