//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace RetroSchematic.Model
{
    /// <summary>
    /// What one chip pin does with a bus signal.
    /// </summary>
    public enum BusRole
    {
        /// <summary>
        /// A chip output on the line: the card drives the bus signal.
        /// </summary>
        Drives,

        /// <summary>
        /// A chip input, clock or enable on the line: the card reads the bus signal.
        /// </summary>
        Receives,

        /// <summary>
        /// A resistor, capacitor or other passive part on the line (a pull-up, a filter).
        /// </summary>
        Passive,

        /// <summary>
        /// The chip has no catalogue file, so what its pin does is not known.
        /// </summary>
        NotCatalogued,
    }

    /// <summary>
    /// One chip pin a bus signal reaches on a card.
    /// </summary>
    public sealed class BusChipPin
    {
        /// <summary>
        /// The block instances from the top level down to the part, joined by '/': empty on the
        /// top level, "BLOCKLEV/SCSI/S_ND_INT" inside a block.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// The part's board position (5A) or name (R5).
        /// </summary>
        public string Part { get; set; } = "";

        /// <summary>
        /// The part type as written on the sheet (74S240).
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// The pin number.
        /// </summary>
        public string Pin { get; set; } = "";

        /// <summary>
        /// The pin's name from the chip catalogue (2Y1), or empty when not known.
        /// </summary>
        public string PinName { get; set; } = "";

        /// <summary>
        /// What the pin does with the signal.
        /// </summary>
        public BusRole Role { get; set; }

        /// <summary>
        /// For a driving pin: the output kind from the catalogue (three-state, open-collector,
        /// totem-pole), or empty when the catalogue does not say.
        /// </summary>
        public string OutputType { get; set; } = "";

        /// <summary>
        /// The catalogue file used when it is not the type's own (74S240 read from 74240).
        /// </summary>
        public string? ReadFrom { get; set; }
    }

    /// <summary>
    /// One bus signal of one card: the backplane pins it is on and every chip pin it reaches,
    /// through every block level of the sheet.
    /// </summary>
    public sealed class BusSignalUse
    {
        /// <summary>
        /// The card (the sheet id, 3207).
        /// </summary>
        public string Card { get; set; } = "";

        /// <summary>
        /// The card's title.
        /// </summary>
        public string CardTitle { get; set; } = "";

        /// <summary>
        /// The sheet file the card was read from, for opening it.
        /// </summary>
        public string SheetFile { get; set; } = "";

        /// <summary>
        /// The machine bus of the card.
        /// </summary>
        public MachineBus Bus { get; set; }

        /// <summary>
        /// The signal's label as printed on the card (BD~00), else its stored name.
        /// </summary>
        public string Signal { get; set; } = "";

        /// <summary>
        /// The bus catalogue's name for the signal on its pin, when the catalogue has the pin.
        /// </summary>
        public string? CatalogueName { get; set; }

        /// <summary>
        /// The backplane pins, as written in the sheet (P.C.c3, P.32).
        /// </summary>
        public List<string> BackplanePins { get; } = new();

        /// <summary>
        /// The chip pins it reaches.
        /// </summary>
        public List<BusChipPin> Chips { get; } = new();

        /// <summary>
        /// True when a chip output is on the signal.
        /// </summary>
        public bool Driven
        {
            get
            {
                for (int i = 0; i < Chips.Count; i++)
                {
                    if (Chips[i].Role == BusRole.Drives)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// True when a chip input is on the signal.
        /// </summary>
        public bool Received
        {
            get
            {
                for (int i = 0; i < Chips.Count; i++)
                {
                    if (Chips[i].Role == BusRole.Receives)
                        return true;
                }
                return false;
            }
        }
    }

    /// <summary>
    /// Which backplane signals a card uses, and which chips drive and receive each one. The
    /// signal is followed from the backplane pin on the top level through every block instance
    /// it is connected to (port by port), so a card drawn as a hierarchy (3207) is reported down
    /// to its chips. What a pin does comes from the chip catalogue; a chip with no file is
    /// reported as not catalogued, never guessed.
    /// </summary>
    public static class BusUse
    {
        /// <summary>
        /// Every backplane signal of every card in the library: the sheet files (.rsc) under the
        /// library folder, and the open sheet as it is in memory (unsaved edits included) in place
        /// of its file. A sheet with no machine bus is left out. When one card is in several
        /// files (2A50 with and without blocks), the file with the shortest name is used and the
        /// note says so; a file that cannot be read is named in the note.
        /// </summary>
        /// <param name="libraryFolder">
        /// The library folder (see <see cref="CardLibrary.FolderBeside"/>), or null.
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue, or null.
        /// </param>
        /// <param name="open">
        /// The open sheet (its whole design), or null.
        /// </param>
        /// <param name="openPath">
        /// The open sheet's file, or null for one never saved.
        /// </param>
        /// <param name="note">
        /// Gets the files not read and the cards found twice.
        /// </param>
        /// <param name="cardCount">
        /// Gets how many cards were read.
        /// </param>
        /// <returns>
        /// The signals, card by card in sheet-number order.
        /// </returns>
        public static List<BusSignalUse> OfLibrary(string? libraryFolder, ChipCatalog? catalog, Netlist? open, string? openPath, StringBuilder note, out int cardCount)
        {
            Dictionary<string, (string File, Netlist Sheet)> cards = new(StringComparer.Ordinal);
            if (libraryFolder != null && Directory.Exists(libraryFolder))
            {
                string[] files = Directory.GetFiles(libraryFolder, "*" + SheetFile.TextEnding, SearchOption.AllDirectories);
                Array.Sort(files, StringComparer.Ordinal);
                for (int i = 0; i < files.Length; i++)
                {
                    Netlist n;
                    try
                    {
                        n = NetlistReader.ReadFile(files[i]);
                    }
                    catch (Exception e) when (e is NetlistException or IOException or System.Text.Json.JsonException)
                    {
                        note.Append(Path.GetFileName(files[i])).Append(" not read: ").Append(e.Message).Append(". ");
                        continue;
                    }
                    if (n.Sheet.Bus == null || n.Sheet.Bus == MachineBus.Unknown)
                        continue;
                    if (cards.TryGetValue(n.Sheet.Id, out (string File, Netlist Sheet) had))
                    {
                        string keep = Path.GetFileName(had.File).Length <= Path.GetFileName(files[i]).Length ? had.File : files[i];
                        note.Append("Card ").Append(n.Sheet.Id).Append(" is in two files; ").Append(Path.GetFileName(keep)).Append(" is used. ");
                        if (keep == had.File)
                            continue;
                    }
                    cards[n.Sheet.Id] = (files[i], n);
                }
            }
            // the open sheet replaces its file: what is in memory is what is reported
            if (open != null && open.Sheet.Bus != null && open.Sheet.Bus != MachineBus.Unknown)
                cards[open.Sheet.Id] = (openPath ?? "", open);
            List<string> ids = new(cards.Keys);
            ids.Sort(StringComparer.Ordinal);
            List<BusSignalUse> all = new();
            for (int i = 0; i < ids.Count; i++)
                all.AddRange(Of(cards[ids[i]].Sheet, catalog, cards[ids[i]].File));
            cardCount = cards.Count;
            return all;
        }

        /// <summary>
        /// Every backplane signal of one card.
        /// </summary>
        /// <param name="sheet">
        /// The card's sheet.
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue, or null (then every chip pin is not catalogued).
        /// </param>
        /// <param name="sheetFile">
        /// The file the sheet came from (kept on each row for opening it).
        /// </param>
        /// <returns>
        /// One entry per top-level signal that has a backplane pin, in the sheet's order.
        /// </returns>
        public static List<BusSignalUse> Of(Netlist sheet, ChipCatalog? catalog, string sheetFile)
        {
            MachineBus bus = sheet.Sheet.Bus ?? MachineBus.Unknown;
            BusCatalog signals = BusCatalog.Load(catalog?.Folder, bus);
            List<BusSignalUse> list = new();
            for (int i = 0; i < sheet.Nets.Count; i++)
            {
                Net net = sheet.Nets[i];
                BusSignalUse? use = null;
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    string part = net.Pins[j].PartRef;
                    if (!ConnectorPins.IsConnector(part) || ConnectorPins.Kind(part) != 'P')
                        continue;
                    use ??= new BusSignalUse
                    {
                        Card = sheet.Sheet.Id,
                        CardTitle = sheet.Sheet.Title ?? "",
                        SheetFile = sheetFile,
                        Bus = bus,
                        Signal = net.Printed ?? net.Name,
                    };
                    use.BackplanePins.Add(net.Pins[j].At);
                    if (use.CatalogueName == null)
                    {
                        string at = net.Pins[j].At;
                        if (ConnectorPins.TryParsePin(at.AsSpan(at.LastIndexOf('.') + 1), out int key))
                            use.CatalogueName = signals.ByPin(ConnectorPins.Letter(part), key)?.Name;
                    }
                }
                if (use == null)
                    continue;
                Follow(sheet, sheet.Nets, sheet.Parts, sheet.Instances, net.Name, net.Active, "", catalog, use.Chips, 0);
                list.Add(use);
            }
            return list;
        }

        // the chip pins of one signal on one level, then the same signal inside every instance it
        // is connected to; depth stops a block that (wrongly) contains itself
        private static void Follow(Netlist root, List<Net> nets, List<Part> parts, List<BlockInstance>? instances, string name, ActiveLevel active,
            string path, ChipCatalog? catalog, List<BusChipPin> found, int depth)
        {
            if (depth > 32)
                return;
            for (int i = 0; i < nets.Count; i++)
            {
                Net net = nets[i];
                if (net.Name != name || net.Active != active)
                    continue;
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    PinReference p = net.Pins[j];
                    string partRef = p.PartRef;
                    if (ConnectorPins.IsConnector(partRef))
                        continue;
                    string type = TypeOf(parts, partRef);
                    found.Add(Describe(path, partRef, type, p.At.Substring(p.At.LastIndexOf('.') + 1), catalog));
                }
            }
            if (instances == null)
                return;
            for (int i = 0; i < instances.Count; i++)
            {
                BlockInstance inst = instances[i];
                BlockDefinition? block = Hierarchy.FindBlock(root, inst.Block);
                if (block == null || inst.Connections == null)
                    continue;
                for (int c = 0; c < inst.Connections.Count; c++)
                {
                    PortConnection conn = inst.Connections[c];
                    for (int k = 0; k < conn.Nets.Count; k++)
                    {
                        SignalRef s = conn.Nets[k];
                        if (s.Net != name || (s.Active ?? active) != active)
                            continue;
                        BlockPort? port = PortOf(block, conn.Port);
                        if (port == null || k >= port.Signals.Count)
                            continue;
                        SignalRef inner = port.Signals[k];
                        ActiveLevel innerActive = inner.Active ?? ActiveOf(block.Nets, inner.Net);
                        Follow(root, block.Nets, block.Parts, block.Instances, inner.Net, innerActive,
                            path.Length == 0 ? inst.Name : path + "/" + inst.Name, catalog, found, depth + 1);
                    }
                }
            }
        }

        private static BusChipPin Describe(string path, string partRef, string type, string pin, ChipCatalog? catalog)
        {
            BusChipPin r = new() { Path = path, Part = partRef, Type = type, Pin = pin, Role = BusRole.NotCatalogued };
            if (PassiveParts.IsPassive(type))
            {
                r.Role = BusRole.Passive;
                return r;
            }
            ChipInfo? chip = catalog?.Get(type);
            if (chip == null || !int.TryParse(pin, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                return r;
            r.ReadFrom = chip.ReadFrom;
            for (int i = 0; i < chip.Pins.Count; i++)
            {
                ChipPin cp = chip.Pins[i];
                if (cp.Number != number)
                    continue;
                r.PinName = cp.Key;
                r.OutputType = cp.OutputType;
                r.Role = cp.Role switch
                {
                    "output" => BusRole.Drives,
                    "input" or "clock" or "enable" => BusRole.Receives,
                    _ => BusRole.NotCatalogued,
                };
                break;
            }
            return r;
        }

        private static string TypeOf(List<Part> parts, string partRef)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Ref == partRef)
                    return parts[i].Type;
            }
            return "";
        }

        private static BlockPort? PortOf(BlockDefinition block, string name)
        {
            for (int i = 0; i < block.Ports.Count; i++)
            {
                if (block.Ports[i].Name == name)
                    return block.Ports[i];
            }
            return null;
        }

        private static ActiveLevel ActiveOf(List<Net> nets, string name)
        {
            for (int i = 0; i < nets.Count; i++)
            {
                if (nets[i].Name == name)
                    return nets[i].Active;
            }
            return ActiveLevel.None;
        }

        /// <summary>
        /// The role in words, as the window and the report show it.
        /// </summary>
        /// <param name="pin">
        /// The chip pin.
        /// </param>
        /// <returns>
        /// The text.
        /// </returns>
        public static string RoleText(BusChipPin pin) => pin.Role switch
        {
            BusRole.Drives => pin.OutputType.Length > 0 ? "drives (" + pin.OutputType + ")" : "drives",
            BusRole.Receives => "receives",
            BusRole.Passive => "passive part",
            _ => "not catalogued",
        };

        /// <summary>
        /// A Markdown report of the bus use of many cards: per bus, one table per signal with the
        /// card, the backplane pin, and every chip pin with what it does.
        /// </summary>
        /// <param name="uses">
        /// The signals of every card (from <see cref="Of"/>).
        /// </param>
        /// <returns>
        /// The Markdown text.
        /// </returns>
        public static string ToMarkdown(List<BusSignalUse> uses)
        {
            StringBuilder sb = new();
            sb.Append("# Backplane bus use per card\n\n");
            sb.Append("Made by RetroSchematic from the sheet files. For every backplane signal a card uses: the pin, and every chip pin the\n");
            sb.Append("signal reaches through the card's blocks, with what the pin does according to the chip catalogue. \"not catalogued\"\n");
            sb.Append("means the chip has no catalogue file yet; nothing is guessed.\n");
            MachineBus[] order = { MachineBus.Nd100, MachineBus.Nord10, MachineBus.Unknown };
            for (int b = 0; b < order.Length; b++)
            {
                bool any = false;
                for (int i = 0; i < uses.Count && !any; i++)
                    any = uses[i].Bus == order[b];
                if (!any)
                    continue;
                sb.Append("\n## ").Append(order[b] == MachineBus.Nd100 ? "ND-100 bus (backplane plugs A, B and C)" : order[b] == MachineBus.Nord10 ? "NORD-10 bus (card-edge fingers)" : "Bus not known").Append("\n\n");
                // summary: signal x card
                sb.Append("| Card | Signal | Pin | Bus catalogue | Driven by the card | Read by the card |\n|---|---|---|---|---|---|\n");
                for (int i = 0; i < uses.Count; i++)
                {
                    BusSignalUse u = uses[i];
                    if (u.Bus != order[b])
                        continue;
                    sb.Append("| ").Append(u.Card).Append(" | ").Append(Cell(u.Signal)).Append(" | ").Append(Cell(string.Join(", ", u.BackplanePins)))
                        .Append(" | ").Append(Cell(u.CatalogueName ?? "")).Append(" | ").Append(u.Driven ? "yes" : "").Append(" | ").Append(u.Received ? "yes" : "").Append(" |\n");
                }
                for (int i = 0; i < uses.Count; i++)
                {
                    BusSignalUse u = uses[i];
                    if (u.Bus != order[b])
                        continue;
                    sb.Append("\n### ").Append(u.Card).Append(' ').Append(Cell(u.Signal)).Append(" (").Append(Cell(string.Join(", ", u.BackplanePins))).Append(")\n\n");
                    if (u.Chips.Count == 0)
                    {
                        sb.Append("No chip pin on this signal (a supply, or not read yet).\n");
                        continue;
                    }
                    sb.Append("| Block | Part | Type | Pin | Pin name | What it does |\n|---|---|---|---|---|---|\n");
                    for (int k = 0; k < u.Chips.Count; k++)
                    {
                        BusChipPin c = u.Chips[k];
                        sb.Append("| ").Append(Cell(c.Path.Length == 0 ? "(top)" : c.Path.Substring(c.Path.LastIndexOf('/') + 1))).Append(" | ").Append(Cell(c.Part)).Append(" | ").Append(Cell(c.Type))
                            .Append(c.ReadFrom != null ? " (pins from " + c.ReadFrom + ")" : "").Append(" | ").Append(Cell(c.Pin)).Append(" | ").Append(Cell(c.PinName))
                            .Append(" | ").Append(RoleText(c)).Append(" |\n");
                    }
                }
            }
            return sb.ToString();
        }

        // a table cell: a | or a ~ would be read as Markdown
        private static string Cell(string text) => text.Replace("|", "\\|").Replace("~", "\\~");
    }
}
