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
using System.Text.RegularExpressions;

namespace RetroSchematic.Model
{
    /// <summary>
    /// A reading file that cannot be read: an unknown line, a line with too few fields, or no
    /// block or page line. The message names the file and line.
    /// </summary>
    public sealed class ReadingException : Exception
    {
        /// <summary>
        /// Creates the exception.
        /// </summary>
        /// <param name="message">
        /// What is wrong and where.
        /// </param>
        public ReadingException(string message) : base(message) { }
    }

    /// <summary>
    /// One pin named on a net line, with the file and line it was read from.
    /// </summary>
    public sealed class ReadingPin
    {
        /// <summary>The pin as written: REF.PIN (13D.7, or 18A.E1 on a connector).</summary>
        public string At { get; init; } = "";

        /// <summary>Where it was read: file and line, such as p06.txt:12.</summary>
        public string Where { get; init; } = "";
    }

    /// <summary>
    /// One gate line: a gate of a chip named after its output pin (13D/6).
    /// </summary>
    public sealed class ReadingGate
    {
        /// <summary>The gate's name: REF/OUTPUT.</summary>
        public string Id { get; init; } = "";

        /// <summary>The kind as written: nand, nor, not, and, or, buffer, and-or-invert or other.</summary>
        public string Kind { get; init; } = "";

        /// <summary>The input pin numbers.</summary>
        public List<int> Inputs { get; } = new();

        /// <summary>The output pin number.</summary>
        public int Output { get; init; }

        /// <summary>Where it was read: file and line.</summary>
        public string Where { get; init; } = "";

        /// <summary>The sheet number of the file it is on (set when the sheets are merged).</summary>
        public string Page { get; set; } = "";
    }

    /// <summary>
    /// A connector line: a part that is a socket or header on the board, not a chip.
    /// </summary>
    public sealed class ReadingConnector
    {
        /// <summary>The connector catalogue file id.</summary>
        public string Catalog { get; init; } = "";

        /// <summary>What it is used for, as written.</summary>
        public string Use { get; init; } = "";

        /// <summary>The sheet number it was named on.</summary>
        public string Page { get; set; } = "";
    }

    /// <summary>
    /// One reading file (docs/CARD-readings/pNN.txt): what one drawing sheet shows, line by line
    /// as described in docs/3207-readings/README.md.
    /// </summary>
    public sealed class ReadingFile
    {
        /// <summary>The file name (p06.txt).</summary>
        public string File { get; init; } = "";

        /// <summary>The block this sheet draws the inside of.</summary>
        public string Block { get; set; } = "";

        /// <summary>The sheet number.</summary>
        public string Page { get; set; } = "";

        /// <summary>The parts: board position (or printed name) to type, in file order.</summary>
        public OrderedDictionary<string, string> Parts { get; } = new(StringComparer.Ordinal);

        /// <summary>The optional boxes of the parts, in 300-dpi pixels (x, y, width, height).</summary>
        public Dictionary<string, int[]> Boxes { get; } = new(StringComparer.Ordinal);

        /// <summary>The gate lines, in file order.</summary>
        public List<ReadingGate> Gates { get; } = new();

        /// <summary>The net lines: name to pins, in file order (several lines of one name are one net).</summary>
        public OrderedDictionary<string, List<ReadingPin>> Nets { get; } = new(StringComparer.Ordinal);

        /// <summary>The note lines.</summary>
        public List<string> Notes { get; } = new();

        /// <summary>Pins that could not be settled from the drawing (left out on purpose).</summary>
        public HashSet<string> Open { get; } = new(StringComparer.Ordinal);

        /// <summary>Pins drawn as unconnected stubs.</summary>
        public HashSet<string> Nc { get; } = new(StringComparer.Ordinal);

        /// <summary>The port lines: block name to the signals drawn at that block box's edge.</summary>
        public OrderedDictionary<string, List<string>> Ports { get; } = new(StringComparer.Ordinal);

        /// <summary>The connector lines: part to connector catalogue and use.</summary>
        public OrderedDictionary<string, ReadingConnector> Connectors { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// The supply lines: signals that are a supply on this card (AVC on 3207, a local 5.6 V
        /// made on the card), card wide, in addition to <see cref="CardReadings.Supply"/>.
        /// </summary>
        public HashSet<string> Supplies { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// The sheets of one block merged: every part, net and gate the readings give for it.
    /// </summary>
    public sealed class ReadingBlock
    {
        /// <summary>The block's name.</summary>
        public string Name { get; init; } = "";

        /// <summary>The parts: reference to type (the first sheet that names it wins).</summary>
        public OrderedDictionary<string, string> Parts { get; } = new(StringComparer.Ordinal);

        /// <summary>The sheet each part was first named on.</summary>
        public Dictionary<string, string> PartPage { get; } = new(StringComparer.Ordinal);

        /// <summary>The nets: name to every pin line of that name, from every sheet of the block.</summary>
        public OrderedDictionary<string, List<ReadingPin>> Nets { get; } = new(StringComparer.Ordinal);

        /// <summary>The gates, from every sheet of the block.</summary>
        public List<ReadingGate> Gates { get; } = new();

        /// <summary>The sheet numbers of the block, in file order.</summary>
        public List<string> Pages { get; } = new();

        /// <summary>The notes, each starting "sheet N: ".</summary>
        public List<string> Notes { get; } = new();

        /// <summary>Pins left open on purpose.</summary>
        public HashSet<string> Open { get; } = new(StringComparer.Ordinal);

        /// <summary>Pins drawn as unconnected stubs.</summary>
        public HashSet<string> Nc { get; } = new(StringComparer.Ordinal);

        /// <summary>The connector parts.</summary>
        public OrderedDictionary<string, ReadingConnector> Connectors { get; } = new(StringComparer.Ordinal);

        /// <summary>Each pin's net (the first net that names it), filled by the check.</summary>
        public Dictionary<string, string> Owner { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// The pins of a net once each, in the order first named.
        /// </summary>
        /// <param name="net">
        /// The net's name.
        /// </param>
        /// <returns>
        /// The pins, or an empty list for no such net.
        /// </returns>
        public List<string> PinsOf(string net)
        {
            List<string> list = new();
            if (!Nets.TryGetValue(net, out List<ReadingPin>? pins))
                return list;
            HashSet<string> seen = new(StringComparer.Ordinal);
            for (int i = 0; i < pins.Count; i++)
            {
                if (seen.Add(pins[i].At))
                    list.Add(pins[i].At);
            }
            return list;
        }
    }

    /// <summary>
    /// A card's reading files, merged per block and checked.
    /// </summary>
    public sealed class ReadingSet
    {
        /// <summary>The files, in name order.</summary>
        public List<ReadingFile> Files { get; } = new();

        /// <summary>The blocks, in the order first named.</summary>
        public OrderedDictionary<string, ReadingBlock> Blocks { get; } = new(StringComparer.Ordinal);

        /// <summary>Every problem the check found; a good reading set has none.</summary>
        public List<string> Problems { get; } = new();

        /// <summary>Chip types with no catalogue file (their pins were not checked), sorted.</summary>
        public List<string> Missing { get; } = new();

        /// <summary>
        /// Every supply name on this card: <see cref="CardReadings.Supply"/> and the supply lines
        /// of every file (a supply is card wide, so a line on one sheet counts for all blocks).
        /// </summary>
        public HashSet<string> Supplies { get; } = new(CardReadings.Supply, StringComparer.Ordinal);
    }

    /// <summary>
    /// The per-sheet reading files of a card (docs/3207-readings/README.md): read, merged per
    /// block, checked, and written into a sheet. The same rules as tools/card-readings.py, so the
    /// MCP server and the command-line tool give the same answers as the script.
    /// </summary>
    public static class CardReadings
    {
        /// <summary>The supply net names: a power or ground pin may only be on one of these.</summary>
        public static readonly HashSet<string> Supply = new(StringComparer.Ordinal) { "+5V", "GND", "12V", "+12V", "-12V", "5VSTBY" };

        // the reading names that are a supply and so have no active level (5V as well)
        private static readonly HashSet<string> s_supplyNames = new(StringComparer.Ordinal) { "+5V", "GND", "12V", "+12V", "-12V", "5VSTBY", "5V" };
        private static readonly Regex s_fileName = new(@"^p\d+\.txt$", RegexOptions.CultureInvariant);
        private static readonly Regex s_passiveR = new(@"^\d*\.?\d+[KM]?OHM$", RegexOptions.CultureInvariant);
        private static readonly Regex s_passiveC = new(@"^\d*\.?\d+(PF|NF|UF)$", RegexOptions.CultureInvariant);
        private static readonly Regex s_busPort = new(@"^BD~\d\d$", RegexOptions.CultureInvariant);

        /// <summary>
        /// Reads every reading file (p05.txt, p06.txt ...) in a folder, in name order, merges them
        /// per block and checks them.
        /// </summary>
        /// <param name="folder">
        /// The folder of reading files.
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue for the pin checks, or null (then every chip type is listed as missing).
        /// </param>
        /// <returns>
        /// The merged and checked readings.
        /// </returns>
        /// <exception cref="ReadingException">
        /// A file cannot be read.
        /// </exception>
        /// <exception cref="IOException">
        /// The folder cannot be read.
        /// </exception>
        public static ReadingSet Load(string folder, ChipCatalog? catalog)
        {
            if (!Directory.Exists(folder))
                throw new IOException("There is no folder " + folder + ".");
            string[] paths = Directory.GetFiles(folder);
            List<string> names = new();
            for (int i = 0; i < paths.Length; i++)
            {
                string name = Path.GetFileName(paths[i]);
                if (s_fileName.IsMatch(name))
                    names.Add(name);
            }
            names.Sort(StringComparer.Ordinal);
            List<ReadingFile> files = new();
            for (int i = 0; i < names.Count; i++)
                files.Add(Parse(names[i], System.IO.File.ReadAllText(Path.Combine(folder, names[i]))));
            return Check(files, catalog);
        }

        /// <summary>
        /// Reads one reading file's text.
        /// </summary>
        /// <param name="file">
        /// The file name, used in the messages (p06.txt).
        /// </param>
        /// <param name="text">
        /// The file's text.
        /// </param>
        /// <returns>
        /// What the file says.
        /// </returns>
        /// <exception cref="ReadingException">
        /// An unknown line, a line with too few fields, or no block or page line.
        /// </exception>
        public static ReadingFile Parse(string file, string text)
        {
            ReadingFile r = new() { File = file };
            string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            // a text ending in a newline has an empty last piece that is not a line
            int count = lines.Length > 0 && lines[^1].Length == 0 ? lines.Length - 1 : lines.Length;
            for (int n = 0; n < count; n++)
            {
                string raw = lines[n];
                // a # starts a comment, except on a note line
                string line;
                if (raw.TrimStart().StartsWith("note ", StringComparison.Ordinal))
                    line = raw.Trim();
                else
                {
                    int hash = raw.IndexOf('#');
                    line = (hash < 0 ? raw : raw.Substring(0, hash)).Trim();
                }
                if (line.Length == 0)
                    continue;
                int space = line.IndexOf(' ');
                string word = space < 0 ? line : line.Substring(0, space);
                string rest = space < 0 ? "" : line.Substring(space + 1).Trim();
                string where = file + ":" + (n + 1).ToString(CultureInfo.InvariantCulture);
                string[] f = rest.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                switch (word)
                {
                    case "block":
                        r.Block = rest;
                        break;
                    case "page":
                        r.Page = rest;
                        break;
                    case "part":
                        Need(f, 2, where, word);
                        r.Parts[f[0]] = f[1];
                        if (f.Length >= 6)
                            r.Boxes[f[0]] = new[] { Number(f[2], where), Number(f[3], where), Number(f[4], where), Number(f[5], where) };
                        break;
                    case "gate":
                    {
                        Need(f, 3, where, word);
                        int arrow = f[2].IndexOf('>');
                        if (arrow < 0)
                            throw new ReadingException(where + ": a gate is written REF/OUT KIND IN,IN>OUT");
                        ReadingGate g = new() { Id = f[0], Kind = f[1], Output = Number(f[2].Substring(arrow + 1), where), Where = where };
                        string[] ins = f[2].Substring(0, arrow).Split(',');
                        for (int i = 0; i < ins.Length; i++)
                            g.Inputs.Add(Number(ins[i], where));
                        r.Gates.Add(g);
                        break;
                    }
                    case "net":
                    {
                        int colon = rest.IndexOf(':');
                        string name = (colon < 0 ? rest : rest.Substring(0, colon)).Trim();
                        string pins = colon < 0 ? "" : rest.Substring(colon + 1);
                        if (!r.Nets.TryGetValue(name, out List<ReadingPin>? list))
                        {
                            list = new List<ReadingPin>();
                            r.Nets.Add(name, list);
                        }
                        string[] p = pins.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < p.Length; i++)
                            list.Add(new ReadingPin { At = p[i], Where = where });
                        break;
                    }
                    case "port":
                    {
                        Need(f, 1, where, word);
                        if (!r.Ports.TryGetValue(f[0], out List<string>? names))
                        {
                            names = new List<string>();
                            r.Ports.Add(f[0], names);
                        }
                        for (int i = 1; i < f.Length; i++)
                            names.Add(f[i]);
                        break;
                    }
                    case "connector":
                    {
                        string[] c = rest.Split((char[]?)null, 3, StringSplitOptions.RemoveEmptyEntries);
                        Need(c, 2, where, word);
                        r.Connectors[c[0]] = new ReadingConnector { Catalog = c[1], Use = c.Length > 2 ? c[2].Trim() : "" };
                        break;
                    }
                    case "open":
                        for (int i = 0; i < f.Length; i++)
                            r.Open.Add(f[i]);
                        break;
                    case "nc":
                        for (int i = 0; i < f.Length; i++)
                            r.Nc.Add(f[i]);
                        break;
                    case "supply":
                        // supply NAME [what it is]: the signal is a supply on this card
                        Need(f, 1, where, word);
                        r.Supplies.Add(f[0]);
                        break;
                    case "note":
                        r.Notes.Add(rest);
                        break;
                    default:
                        throw new ReadingException(where + ": unknown line \"" + word + "\"");
                }
            }
            if (r.Block.Length == 0 || r.Page.Length == 0)
                throw new ReadingException(file + ": needs a block and a page line");
            return r;
        }

        /// <summary>
        /// Merges the files per block and checks them. The checks:
        /// <para>
        /// - every pin names a part of its block, and is in one net only;
        /// - a part has one type on every sheet and in every block, and a pin is used in one block only;
        /// - the pin exists on the chip (the catalogue's file, with the 74 family rule of
        ///   <see cref="ChipCatalog.Get"/>), and a power or ground pin is only on a supply net;
        /// - every port name is a net in its block; every gate pin is in a net, open or nc;
        /// - a gate is named after its output pin, and one gate name is one gate everywhere.
        /// </para>
        /// <para>
        /// Types with no catalogue file are listed in <see cref="ReadingSet.Missing"/>.
        /// </para>
        /// </summary>
        /// <param name="files">
        /// The files, in name order.
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue, or null.
        /// </param>
        /// <returns>
        /// The merged blocks and every problem found.
        /// </returns>
        public static ReadingSet Check(List<ReadingFile> files, ChipCatalog? catalog)
        {
            ReadingSet set = new();
            set.Files.AddRange(files);
            for (int fi = 0; fi < files.Count; fi++)
                set.Supplies.UnionWith(files[fi].Supplies);
            List<string> problems = set.Problems;
            for (int fi = 0; fi < files.Count; fi++)
            {
                ReadingFile r = files[fi];
                if (!set.Blocks.TryGetValue(r.Block, out ReadingBlock? b))
                {
                    b = new ReadingBlock { Name = r.Block };
                    set.Blocks.Add(r.Block, b);
                }
                b.Pages.Add(r.Page);
                b.Open.UnionWith(r.Open);
                for (int i = 0; i < r.Connectors.Count; i++)
                {
                    KeyValuePair<string, ReadingConnector> c = r.Connectors.GetAt(i);
                    b.Connectors[c.Key] = new ReadingConnector { Catalog = c.Value.Catalog, Use = c.Value.Use, Page = r.Page };
                }
                b.Nc.UnionWith(r.Nc);
                for (int i = 0; i < r.Notes.Count; i++)
                    b.Notes.Add("sheet " + r.Page + ": " + r.Notes[i]);
                for (int i = 0; i < r.Parts.Count; i++)
                {
                    KeyValuePair<string, string> p = r.Parts.GetAt(i);
                    if (b.Parts.TryGetValue(p.Key, out string? had) && had != p.Value)
                        problems.Add(r.File + ": part " + p.Key + " is " + p.Value + " here but " + had + " on sheet " + b.PartPage[p.Key]);
                    b.Parts.TryAdd(p.Key, p.Value);
                    b.PartPage.TryAdd(p.Key, r.Page);
                }
                for (int i = 0; i < r.Nets.Count; i++)
                {
                    KeyValuePair<string, List<ReadingPin>> n = r.Nets.GetAt(i);
                    if (!b.Nets.TryGetValue(n.Key, out List<ReadingPin>? list))
                    {
                        list = new List<ReadingPin>();
                        b.Nets.Add(n.Key, list);
                    }
                    list.AddRange(n.Value);
                }
                for (int i = 0; i < r.Gates.Count; i++)
                {
                    r.Gates[i].Page = r.Page;
                    b.Gates.Add(r.Gates[i]);
                }
            }

            SortedSet<string> missing = new(StringComparer.Ordinal);
            // where each pin's net was read, per block: for the messages of the cross-block check
            Dictionary<ReadingBlock, Dictionary<string, string>> wheres = new();
            for (int bi = 0; bi < set.Blocks.Count; bi++)
            {
                ReadingBlock b = set.Blocks.GetAt(bi).Value;
                Dictionary<string, string> ownerWhere = new(StringComparer.Ordinal);
                b.Owner.Clear();
                for (int ni = 0; ni < b.Nets.Count; ni++)
                {
                    KeyValuePair<string, List<ReadingPin>> net = b.Nets.GetAt(ni);
                    HashSet<string> seen = new(StringComparer.Ordinal);
                    for (int pi = 0; pi < net.Value.Count; pi++)
                    {
                        string p = net.Value[pi].At;
                        string where = net.Value[pi].Where;
                        if (!seen.Add(p))
                            continue;
                        int dot = p.LastIndexOf('.');
                        string reference = dot < 0 ? "" : p.Substring(0, dot);
                        string num = dot < 0 ? p : p.Substring(dot + 1);
                        if (!b.Parts.TryGetValue(reference, out string? ctype))
                        {
                            problems.Add(where + ": " + p + " in net " + net.Key + ": no part " + reference + " in block " + b.Name);
                            continue;
                        }
                        if (b.Owner.TryGetValue(p, out string? other) && other != net.Key)
                            problems.Add(where + ": " + p + " is in net " + net.Key + " and in net " + other + " (" + ownerWhere[p] + ")");
                        if (b.Owner.TryAdd(p, net.Key))
                            ownerWhere[p] = where;
                        if (b.Connectors.ContainsKey(reference))
                            continue;
                        if (PassiveParts.ReadingPart(ctype, out string passiveId, out int passivePins))
                        {
                            // a passive part: its file in catalog/passives/ must exist, and when
                            // the file knows the pin count the pin must be one of them
                            if (catalog != null && !PassiveParts.HasPassiveFile(ctype, catalog.Folder))
                                missing.Add(ctype);
                            else if (passivePins > 0 && (!int.TryParse(num, NumberStyles.None, CultureInfo.InvariantCulture, out int pk) || pk < 1 || pk > passivePins))
                                problems.Add(where + ": " + p + ": " + ctype + " (" + passiveId + ") has no pin " + num);
                            continue;
                        }
                        ChipInfo? chip = catalog?.Get(ctype);
                        if (chip == null)
                        {
                            missing.Add(ctype);
                            continue;
                        }
                        if (!int.TryParse(num, NumberStyles.None, CultureInfo.InvariantCulture, out int k))
                        {
                            problems.Add(where + ": " + p + ": \"" + num + "\" is not a pin number of " + ctype);
                            continue;
                        }
                        string? role = RoleOf(chip, k);
                        if (role == null)
                            problems.Add(where + ": " + p + ": " + ctype + " (" + (chip.ReadFrom ?? ctype) + ") has no pin " + k.ToString(CultureInfo.InvariantCulture));
                        else if ((role == "power" || role == "ground") && !set.Supplies.Contains(net.Key))
                            problems.Add(where + ": " + p + " is a " + role + " pin of " + ctype + " but is in net " + net.Key);
                    }
                }
                for (int gi = 0; gi < b.Gates.Count; gi++)
                {
                    ReadingGate g = b.Gates[gi];
                    string reference = GatePart(g.Id);
                    if (!b.Parts.ContainsKey(reference))
                    {
                        problems.Add(g.Where + ": gate " + g.Id + ": no part " + reference);
                        continue;
                    }
                    for (int k = 0; k <= g.Inputs.Count; k++)
                    {
                        int pin = k < g.Inputs.Count ? g.Inputs[k] : g.Output;
                        string at = reference + "." + pin.ToString(CultureInfo.InvariantCulture);
                        if (!b.Owner.ContainsKey(at) && !b.Open.Contains(at) && !b.Nc.Contains(at))
                            problems.Add(g.Where + ": gate " + g.Id + " pin " + pin.ToString(CultureInfo.InvariantCulture) + " is in no net");
                    }
                }
                List<string> marked = new(b.Open);
                marked.AddRange(b.Nc);
                marked.Sort(StringComparer.Ordinal);
                for (int i = 0; i < marked.Count; i++)
                {
                    if ((i == 0 || marked[i] != marked[i - 1]) && b.Owner.TryGetValue(marked[i], out string? net))
                        problems.Add(marked[i] + " is marked open/nc but is in net " + net + " (" + ownerWhere[marked[i]] + ")");
                }
                wheres[b] = ownerWhere;
            }

            // across blocks: a board position is one chip, so the same type everywhere, and each pin used once
            Dictionary<string, (string Type, string Block)> types = new(StringComparer.Ordinal);
            Dictionary<string, (string Block, string Net)> pinsUsed = new(StringComparer.Ordinal);
            for (int bi = 0; bi < set.Blocks.Count; bi++)
            {
                ReadingBlock b = set.Blocks.GetAt(bi).Value;
                for (int i = 0; i < b.Parts.Count; i++)
                {
                    KeyValuePair<string, string> p = b.Parts.GetAt(i);
                    if (types.TryGetValue(p.Key, out (string Type, string Block) had) && had.Type != p.Value)
                        problems.Add("part " + p.Key + " is " + p.Value + " in block " + b.Name + " but " + had.Type + " in block " + had.Block);
                    types.TryAdd(p.Key, (p.Value, b.Name));
                }
                Dictionary<string, string> ownerWhere = wheres[b];
                List<string> pins = new(b.Owner.Keys);
                for (int i = 0; i < pins.Count; i++)
                {
                    string net = b.Owner[pins[i]];
                    if (set.Supplies.Contains(net))
                        continue;
                    if (pinsUsed.TryGetValue(pins[i], out (string Block, string Net) used) && used.Block != b.Name)
                        problems.Add(ownerWhere[pins[i]] + ": pin " + pins[i] + " is used in block " + b.Name + " (net " + net + ") and in block " + used.Block + " (net " + used.Net + ")");
                    pinsUsed.TryAdd(pins[i], (b.Name, net));
                }
            }

            // a port line on the sheet that holds the block boxes names a signal that leaves the
            // block: it must be a net there
            for (int fi = 0; fi < files.Count; fi++)
            {
                ReadingFile r = files[fi];
                for (int i = 0; i < r.Ports.Count; i++)
                {
                    KeyValuePair<string, List<string>> port = r.Ports.GetAt(i);
                    if (!set.Blocks.TryGetValue(port.Key, out ReadingBlock? target))
                    {
                        problems.Add(r.File + ": port list for block " + port.Key + ", which has no reading");
                        continue;
                    }
                    for (int k = 0; k < port.Value.Count; k++)
                    {
                        if (!target.Nets.ContainsKey(port.Value[k]))
                            problems.Add(r.File + ": port " + port.Value[k] + " of block " + port.Key + " is not a net in that block");
                    }
                }
            }

            // a gate is named REF/<output pin>, so one name is one gate everywhere
            Dictionary<string, (string Shape, string Where)> gates = new(StringComparer.Ordinal);
            for (int bi = 0; bi < set.Blocks.Count; bi++)
            {
                ReadingBlock b = set.Blocks.GetAt(bi).Value;
                for (int gi = 0; gi < b.Gates.Count; gi++)
                {
                    ReadingGate g = b.Gates[gi];
                    int slash = g.Id.IndexOf('/');
                    string reference = slash < 0 ? g.Id : g.Id.Substring(0, slash);
                    string num = slash < 0 ? "" : g.Id.Substring(slash + 1);
                    string output = g.Output.ToString(CultureInfo.InvariantCulture);
                    if (num != output)
                        problems.Add(g.Where + ": gate " + g.Id + " must be named " + reference + "/" + output + " (after its output pin)");
                    string shape = string.Join(",", g.Inputs) + ">" + output + " " + g.Kind;
                    if (gates.TryGetValue(g.Id, out (string Shape, string Where) had) && had.Shape != shape)
                        problems.Add(g.Where + ": gate " + g.Id + " differs from " + had.Where);
                    gates.TryAdd(g.Id, (shape, g.Where));
                }
            }
            set.Missing.AddRange(missing);
            return set;
        }

        /// <summary>
        /// The report the script prints: one line per block (sheets, parts, nets, pins, gates), the
        /// types with no catalogue file, every problem, and the count of problems.
        /// </summary>
        /// <param name="set">
        /// The checked readings.
        /// </param>
        /// <returns>
        /// The report, one fact per line.
        /// </returns>
        public static string Report(ReadingSet set)
        {
            StringBuilder sb = new();
            for (int bi = 0; bi < set.Blocks.Count; bi++)
            {
                ReadingBlock b = set.Blocks.GetAt(bi).Value;
                int pins = 0;
                for (int i = 0; i < b.Nets.Count; i++)
                    pins += b.PinsOf(b.Nets.GetAt(i).Key).Count;
                sb.Append(CultureInfo.InvariantCulture, $"block {b.Name}: sheets {string.Join(",", b.Pages)}, {b.Parts.Count} parts, {b.Nets.Count} nets, {pins} pins, {b.Gates.Count} gates\n");
            }
            if (set.Missing.Count > 0)
                sb.Append("no catalogue entry (pins not checked): ").Append(string.Join(" ", set.Missing)).Append('\n');
            for (int i = 0; i < set.Problems.Count; i++)
                sb.Append("PROBLEM ").Append(set.Problems[i]).Append('\n');
            sb.Append(set.Problems.Count.ToString(CultureInfo.InvariantCulture)).Append(" problem(s)\n");
            return sb.ToString();
        }

        /// <summary>
        /// Puts every block that has a reading and is drawn as a box on another reading's sheet
        /// (a port line) into the sheet: its parts, gates, nets, pages and ports, and an instance
        /// of it in the block whose sheet draws its box. A signal the parent block does not have
        /// yet is added there with no pins. A connector part becomes connector J.REF on the card.
        /// The top level, the board facts and the blocks with no reading are left as they are. The
        /// same result as <c>card-readings.py sheet</c>; saving (and the file stamp) is the
        /// caller's, through <c>SheetEditor.Save</c>.
        /// </summary>
        /// <param name="root">
        /// The whole sheet, changed in place.
        /// </param>
        /// <param name="set">
        /// The checked readings.
        /// </param>
        /// <param name="catalog">
        /// The chip catalogue: port directions are worked out from its pin roles; null gives every
        /// port direction "not known".
        /// </param>
        /// <returns>
        /// The names of the blocks written, in order.
        /// </returns>
        /// <exception cref="ReadingException">
        /// A block's box is drawn on a sheet whose block is neither read nor in the sheet.
        /// </exception>
        public static List<string> WriteInto(Netlist root, ReadingSet set, ChipCatalog? catalog)
        {
            // the source page of each PDF page number, and how many sources there are
            Dictionary<string, int> sourceByPage = new(StringComparer.Ordinal);
            int total = root.Sources?.Count ?? 0;
            for (int i = 0; i < total; i++)
            {
                int? pg = root.Sources![i].Original?.PdfPage;
                if (pg is int page && page != 0)
                    sourceByPage[page.ToString(CultureInfo.InvariantCulture)] = i + 1;
            }
            // the block whose sheet draws each block's box, with the names on its port lines
            Dictionary<string, (string Parent, List<string> Names)> parentOf = new(StringComparer.Ordinal);
            for (int fi = 0; fi < set.Files.Count; fi++)
            {
                ReadingFile r = set.Files[fi];
                for (int i = 0; i < r.Ports.Count; i++)
                    parentOf[r.Ports.GetAt(i).Key] = (r.Block, r.Ports.GetAt(i).Value);
            }

            List<BlockDefinition> written = new();
            for (int bi = 0; bi < set.Blocks.Count; bi++)
            {
                ReadingBlock b = set.Blocks.GetAt(bi).Value;
                if (!parentOf.TryGetValue(b.Name, out (string Parent, List<string> Names) parent))
                    continue;
                BlockDefinition blk = new() { Name = b.Name, Pages = new List<CardPage>() };
                for (int i = 0; i < b.Pages.Count; i++)
                {
                    CardPage page = new() { Id = b.Pages[i], SheetNumber = b.Pages[i] + " of " + total.ToString(CultureInfo.InvariantCulture) };
                    if (sourceByPage.TryGetValue(b.Pages[i], out int source))
                        page.Source = source;
                    blk.Pages.Add(page);
                }
                List<string> refs = new();
                for (int i = 0; i < b.Parts.Count; i++)
                    refs.Add(b.Parts.GetAt(i).Key);
                refs.Sort(StringComparer.Ordinal);
                for (int i = 0; i < refs.Count; i++)
                {
                    if (!b.Connectors.ContainsKey(refs[i]))
                        blk.Parts.Add(PartEntry(refs[i], b.Parts[refs[i]], b.PartPage[refs[i]]));
                }
                for (int i = 0; i < b.Connectors.Count; i++)
                    WriteConnector(root, b, b.Connectors.GetAt(i).Key, b.Connectors.GetAt(i).Value);
                HashSet<string> seenGates = new(StringComparer.Ordinal);
                for (int i = 0; i < b.Gates.Count; i++)
                {
                    ReadingGate g = b.Gates[i];
                    if (!seenGates.Add(g.Id))
                        continue;
                    GateKind kind = KindOf(g.Kind);
                    Gate gate = new() { Id = g.Id, Part = GatePart(g.Id), Kind = kind, Output = g.Output, Confidence = Confidence.Read };
                    gate.Inputs.AddRange(g.Inputs);
                    if (kind == GateKind.Other && g.Kind != "other")
                        gate.Note = "read as " + g.Kind;
                    blk.Gates.Add(gate);
                }
                Dictionary<string, List<string>> netPins = new(StringComparer.Ordinal);
                for (int i = 0; i < b.Nets.Count; i++)
                {
                    string name = b.Nets.GetAt(i).Key;
                    List<string> unique = b.PinsOf(name);
                    netPins[name] = unique;
                    (string stored, ActiveLevel active, string? printed) = Signal(name, b.Parts, set.Supplies);
                    Net net = new() { Name = stored, Active = active, Printed = printed };
                    // a supply named by a supply line (not a fixed one) is marked in the file, so
                    // the sheet check knows it too
                    if (set.Supplies.Contains(name) && !Supply.Contains(name))
                        net.Supply = true;
                    // a pin named by a letter and a number (socket 18A: E1) is stored with the
                    // letter in lowercase, the form the file format allows (e1)
                    for (int k = 0; k < unique.Count; k++)
                        net.Pins.Add(new PinReference { At = PinAt(unique[k], b.Connectors), Confidence = Confidence.Read });
                    blk.Nets.Add(net);
                }
                // the pins drawn as stubs (nc) and the ones not settled (open) are kept as a block note
                List<string> notes = new(b.Notes);
                if (b.Nc.Count > 0)
                    notes.Add("Pins drawn as unconnected stubs: " + string.Join(" ", Sorted(b.Nc)));
                if (b.Open.Count > 0)
                    notes.Add("OPEN, pins not settled from the drawing (left out of every net): " + string.Join(" ", Sorted(b.Open)));
                blk.Note = string.Join("\n", notes);
                // ports: the names on the parent sheet's port lines, in order; BD~00..BD~23 as one bus port
                List<string> names = new();
                for (int i = 0; i < parent.Names.Count; i++)
                {
                    if (!names.Contains(parent.Names[i]))
                        names.Add(parent.Names[i]);
                }
                List<string> bus = new();
                for (int i = 0; i < names.Count; i++)
                {
                    if (s_busPort.IsMatch(names[i]))
                        bus.Add(names[i]);
                }
                for (int i = 0; i < names.Count; i++)
                {
                    string nm = names[i];
                    bool inBus = bus.Contains(nm);
                    if (inBus && nm != bus[0])
                        continue;
                    List<string> group = inBus ? bus : new List<string> { nm };
                    List<string> pins = new();
                    for (int g = 0; g < group.Count; g++)
                    {
                        if (netPins.TryGetValue(group[g], out List<string>? gp))
                            pins.AddRange(gp);
                    }
                    (PortDirection dir, Confidence conf, string why) = Direction(pins, b.Parts, catalog);
                    BlockPort port = new() { Name = inBus ? "BD_0" : PortName(nm), Direction = dir, DirectionConfidence = conf, Note = why };
                    for (int g = 0; g < group.Count; g++)
                    {
                        (string stored, ActiveLevel active, string? _) = Signal(group[g], b.Parts, set.Supplies);
                        port.Signals.Add(new SignalRef { Net = stored, Active = active });
                    }
                    blk.Ports.Add(port);
                }
                written.Add(blk);
            }

            // instances in the parent blocks; a signal the parent level does not have yet is added
            // there with no pins
            Dictionary<string, BlockDefinition> byName = new(StringComparer.Ordinal);
            for (int i = 0; root.Blocks != null && i < root.Blocks.Count; i++)
                byName[root.Blocks[i].Name] = root.Blocks[i];
            for (int i = 0; i < written.Count; i++)
                byName[written[i].Name] = written[i];
            for (int i = 0; i < written.Count; i++)
            {
                BlockDefinition blk = written[i];
                string parentName = parentOf[blk.Name].Parent;
                if (!byName.TryGetValue(parentName, out BlockDefinition? parent))
                    throw new ReadingException("Block " + blk.Name + " is drawn as a box in block " + parentName + ", which is neither read nor in the sheet.");
                List<PortConnection> conns = new();
                for (int p = 0; p < blk.Ports.Count; p++)
                {
                    PortConnection pc = new() { Port = blk.Ports[p].Name };
                    for (int s = 0; s < blk.Ports[p].Signals.Count; s++)
                    {
                        SignalRef sr = blk.Ports[p].Signals[s];
                        if (!HasNet(parent.Nets, sr))
                            parent.Nets.Add(new Net { Name = sr.Net, Active = sr.Active ?? ActiveLevel.None });
                        pc.Nets.Add(new SignalRef { Net = sr.Net, Active = sr.Active });
                    }
                    conns.Add(pc);
                }
                parent.Instances ??= new List<BlockInstance>();
                for (int k = parent.Instances.Count - 1; k >= 0; k--)
                {
                    if (parent.Instances[k].Name == blk.Name)
                        parent.Instances.RemoveAt(k);
                }
                parent.Instances.Add(new BlockInstance { Name = blk.Name, Block = blk.Name, Connections = conns });
            }
            // the sheet's block order, with new blocks at the end
            List<string> order = new();
            for (int i = 0; root.Blocks != null && i < root.Blocks.Count; i++)
                order.Add(root.Blocks[i].Name);
            for (int i = 0; i < written.Count; i++)
            {
                if (!order.Contains(written[i].Name))
                    order.Add(written[i].Name);
            }
            List<BlockDefinition> blocks = new();
            for (int i = 0; i < order.Count; i++)
                blocks.Add(byName[order[i]]);
            root.Blocks = blocks;
            List<string> result = new();
            for (int i = 0; i < written.Count; i++)
                result.Add(written[i].Name);
            return result;
        }

        /// <summary>
        /// True for a passive part type as a reading writes it: a printed value (100PF, 4.7KOHM),
        /// a Bourns resistor network, a diode, LED, strap and the rest of
        /// <see cref="PassiveParts.ReadingPart"/>. Such a part's pins are not checked against a chip
        /// file; the check uses the passive pin count instead.
        /// </summary>
        /// <param name="type">
        /// The type as written in the reading.
        /// </param>
        /// <returns>
        /// True for a passive part.
        /// </returns>
        public static bool IsPassive(string type) => PassiveParts.ReadingPart(type, out _, out _);

        /// <summary>
        /// A reading name as the sheet stores it: a tilde marks active low (BD~00 is BD00, low,
        /// printed BD~00); an unnamed net REF_PIN and a supply have no level; any other name is
        /// active high.
        /// </summary>
        /// <param name="name">
        /// The name in the reading.
        /// </param>
        /// <param name="parts">
        /// The block's parts, to tell an unnamed net (13D_7) from a name.
        /// </param>
        /// <param name="cardSupplies">
        /// The card's supply names (<see cref="ReadingSet.Supplies"/>), or null for the fixed ones only.
        /// </param>
        /// <returns>
        /// The stored name, its level, and the printed name (null when the same).
        /// </returns>
        public static (string Name, ActiveLevel Active, string? Printed) Signal(string name, OrderedDictionary<string, string> parts, IReadOnlySet<string>? cardSupplies = null)
        {
            // a supply (a fixed one, or one named by a supply line of this card) has no level
            if (s_supplyNames.Contains(name) || (cardSupplies != null && cardSupplies.Contains(name)))
                return (name, ActiveLevel.None, null);
            int us = name.LastIndexOf('_');
            if (us >= 0 && parts.ContainsKey(name.Substring(0, us)) && IsDigits(name.AsSpan(us + 1)))
                return (name, ActiveLevel.None, null);
            if (name.Contains('~', StringComparison.Ordinal))
                return (name.Replace("~", "", StringComparison.Ordinal), ActiveLevel.Low, name);
            return (name, ActiveLevel.High, null);
        }

        // the port name for a signal: the tilde written as _0, the way the sheet's other ports are named
        private static string PortName(string name) => name.Replace("~", "_0", StringComparison.Ordinal);

        private static string GatePart(string id)
        {
            int slash = id.IndexOf('/');
            return slash < 0 ? id : id.Substring(0, slash);
        }

        private static GateKind KindOf(string kind) => kind switch
        {
            "nand" => GateKind.Nand,
            "nor" => GateKind.Nor,
            "not" => GateKind.Not,
            "and" => GateKind.And,
            "or" => GateKind.Or,
            "buffer" => GateKind.Buffer,
            "and-or-invert" => GateKind.AndOrInvert,
            _ => GateKind.Other,
        };

        private static Part PartEntry(string reference, string type, string page)
        {
            Part p = new() { Ref = reference, Confidence = Confidence.Read, Page = page };
            if (s_passiveR.IsMatch(type))
            {
                p.Type = "R";
                p.Value = type;
            }
            else if (s_passiveC.IsMatch(type))
            {
                p.Type = "C";
                p.Value = type;
            }
            else
                p.Type = type;
            return p;
        }

        // a connector part on the card: connector J.REF with its catalogue file, the rows and the
        // longest row its pins use; an entry of the same ref is replaced
        private static void WriteConnector(Netlist root, ReadingBlock b, string reference, ReadingConnector c)
        {
            SortedSet<string> rows = new(StringComparer.Ordinal);
            int most = 0;
            Regex pin = new("^" + Regex.Escape(reference) + @"\.([A-Z])(\d+)$", RegexOptions.CultureInvariant);
            for (int i = 0; i < b.Nets.Count; i++)
            {
                List<ReadingPin> pins = b.Nets.GetAt(i).Value;
                for (int k = 0; k < pins.Count; k++)
                {
                    Match m = pin.Match(pins[k].At);
                    if (!m.Success)
                        continue;
                    rows.Add(m.Groups[1].Value.ToLowerInvariant());
                    most = Math.Max(most, int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
                }
            }
            root.Connectors ??= new List<ConnectorUse>();
            for (int i = root.Connectors.Count - 1; i >= 0; i--)
            {
                if (root.Connectors[i].Ref == "J." + reference)
                    root.Connectors.RemoveAt(i);
            }
            root.Connectors.Add(new ConnectorUse { Ref = "J." + reference, Catalog = c.Catalog, Use = c.Use, Rows = new List<string>(rows), PinsPerRow = most });
        }

        // a reading pin as the sheet file writes it: a connector pin as J.REF.a1 (row letter in
        // lower case, as the format wants), any other pin as it is
        private static string PinAt(string p, OrderedDictionary<string, ReadingConnector> conns)
        {
            int dot = p.LastIndexOf('.');
            string reference = dot < 0 ? "" : p.Substring(0, dot);
            if (!conns.ContainsKey(reference))
                return p;
            string num = p.Substring(dot + 1);
            return "J." + reference + "." + (num.Length > 0 && char.IsLetter(num[0]) ? char.ToLowerInvariant(num[0]) + num.Substring(1) : num);
        }

        // the direction of a port, from the catalogue roles of the chip pins on its net inside
        // the block. Drivers outside the block are not seen, so this is INFERRED, never read
        private static (PortDirection Direction, Confidence Confidence, string Note) Direction(List<string> pins, OrderedDictionary<string, string> parts, ChipCatalog? catalog)
        {
            bool output = false, input = false;
            for (int i = 0; i < pins.Count; i++)
            {
                int dot = pins[i].LastIndexOf('.');
                if (dot < 0 || !parts.TryGetValue(pins[i].Substring(0, dot), out string? type) || IsPassive(type))
                    continue;
                ChipInfo? chip = catalog?.Get(type);
                if (chip == null || !int.TryParse(pins[i].AsSpan(dot + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                    continue;
                string? role = RoleOf(chip, number);
                if (role == "output")
                    output = true;
                else if (role == "input" || role == "clock" || role == "enable")
                    input = true;
            }
            const string why = "from the catalogue roles of the chip pins on this signal inside the block; drivers outside the block are not counted";
            if (output)
                return (PortDirection.Output, Confidence.Inferred, "inferred " + why + " (a chip output drives it)");
            if (input)
                return (PortDirection.Input, Confidence.Inferred, "inferred " + why + " (only chip inputs on it)");
            return (PortDirection.Inout, Confidence.Open, "direction not known: no catalogued chip pin on this signal inside the block");
        }

        private static string? RoleOf(ChipInfo chip, int number)
        {
            for (int i = 0; i < chip.Pins.Count; i++)
            {
                if (chip.Pins[i].Number == number)
                    return chip.Pins[i].Role;
            }
            return null;
        }

        private static bool HasNet(List<Net> nets, SignalRef r)
        {
            ActiveLevel level = r.Active ?? ActiveLevel.None;
            for (int i = 0; i < nets.Count; i++)
            {
                if (nets[i].Name == r.Net && nets[i].Active == level)
                    return true;
            }
            return false;
        }

        private static List<string> Sorted(HashSet<string> set)
        {
            List<string> list = new(set);
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        private static bool IsDigits(ReadOnlySpan<char> s)
        {
            if (s.Length == 0)
                return false;
            for (int i = 0; i < s.Length; i++)
            {
                if (!char.IsAsciiDigit(s[i]))
                    return false;
            }
            return true;
        }

        private static int Number(string text, string where)
        {
            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int v))
                throw new ReadingException(where + ": \"" + text + "\" is not a number");
            return v;
        }

        private static void Need(string[] fields, int count, string where, string word)
        {
            if (fields.Length < count)
                throw new ReadingException(where + ": a " + word + " line needs " + count.ToString(CultureInfo.InvariantCulture) + " fields or more");
        }
    }
}
