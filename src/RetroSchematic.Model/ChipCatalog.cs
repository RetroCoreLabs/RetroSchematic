//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace RetroSchematic.Model
{
    /// <summary>
    /// One pin of a chip as its catalogue file lists it.
    /// </summary>
    public sealed class ChipPin
    {
        /// <summary>
        /// The pin number on the package.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// The datasheet's name for the pin without its bar (1A, GND, 1Q). Whether the datasheet
        /// draws a bar over it is <see cref="Inverted"/>.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// True when the datasheet draws the name with a bar (an active-low or inverted pin: the
        /// 7474's Q-bar is <c>Name</c> "1Q" with <c>Inverted</c> true). Shown in the chosen name
        /// style (/1Q, ~1Q or 1Q with a small 0).
        /// </summary>
        public bool Inverted { get; set; }

        /// <summary>
        /// The name as formulas and the other fields of a chip file write it: <c>/NAME</c> for an
        /// inverted pin, else the name (FORMAT.md, Pins).
        /// </summary>
        public string Key => KeyOf(Name, Inverted);

        /// <summary>
        /// The key of a pin name: <c>/NAME</c> when inverted, else the name.
        /// </summary>
        /// <param name="name">
        /// The name without its bar.
        /// </param>
        /// <param name="inverted">
        /// True when the datasheet draws a bar over it.
        /// </param>
        /// <returns>
        /// The key.
        /// </returns>
        public static string KeyOf(string name, bool inverted) => inverted ? "/" + name : name;

        /// <summary>
        /// The pin's role from catalog/chips/FORMAT.md (input, output, power, ground, clock, ...).
        /// </summary>
        public string Role { get; set; } = "";

        /// <summary>
        /// For an output: "totem-pole", "open-collector", "open-collector-high-voltage" or
        /// "three-state"; empty when the file does not say or the pin is not an output.
        /// </summary>
        public string OutputType { get; set; } = "";
    }

    /// <summary>
    /// What the editor needs from one chip file: its pins, and the simple gates its formulas
    /// describe.
    /// </summary>
    public sealed class ChipInfo
    {
        /// <summary>
        /// The chip type (7400).
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// The datasheet title.
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// Every pin, in the order the file lists them.
        /// </summary>
        public List<ChipPin> Pins { get; } = new();

        /// <summary>
        /// The gates made from the file's formulas (see <see cref="ChipCatalog.GateFromFormula"/>).
        /// Id and Part are left empty; the editor fills them when the chip is placed.
        /// </summary>
        public List<Gate> Gates { get; } = new();

        /// <summary>
        /// The formulas as written in the file, one per output.
        /// </summary>
        public List<string> Formulas { get; } = new();

        /// <summary>
        /// The file the chip was read from when it is not the type's own: a 74LS240 with no file
        /// of its own is read from 74240 (same pins and logic, other timing). Null for its own file.
        /// </summary>
        public string? ReadFrom { get; set; }
    }

    /// <summary>
    /// Reads chip files from the catalogue folder (the one holding <c>chips/</c>).
    /// </summary>
    public sealed class ChipCatalog
    {
        private readonly Dictionary<string, ChipInfo?> _cache = new(StringComparer.Ordinal);

        /// <summary>
        /// Opens a catalogue folder.
        /// </summary>
        /// <param name="folder">
        /// The catalogue folder, the one holding <c>chips/</c>.
        /// </param>
        public ChipCatalog(string folder)
        {
            Folder = folder;
        }

        /// <summary>
        /// The catalogue folder.
        /// </summary>
        public string Folder { get; }

        /// <summary>
        /// Walks up from a file or folder to the first folder that holds <c>catalog/chips</c>, and
        /// returns that <c>catalog</c> folder, or null when there is none.
        /// </summary>
        /// <param name="start">
        /// A file or folder to start from.
        /// </param>
        /// <returns>
        /// The catalogue folder, or null.
        /// </returns>
        public static string? Find(string start)
        {
            string? dir = Directory.Exists(start) ? Path.GetFullPath(start) : Path.GetDirectoryName(Path.GetFullPath(start));
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "catalog");
                if (Directory.Exists(Path.Combine(candidate, "chips")))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        /// <summary>
        /// The catalogue to use for a sheet: the one found by walking up from the sheet, else the
        /// one found by walking up from the running program's folder.
        /// </summary>
        /// <remarks>
        /// <para><b>The lookup order, and why</b></para>
        /// 1. Up from the sheet: a sheet inside a checkout of the repository (or inside the
        /// unpacked download, under <c>sheets/</c>) uses the <c>catalog</c> folder beside it, so a
        /// newer catalogue copied there wins.
        /// 2. Up from the program's folder (<see cref="AppContext.BaseDirectory"/>): a sheet kept
        /// anywhere else - the user's own folder - still gets the catalogue that ships beside the
        /// program in the download. Before this fallback the command-line tool and the window's
        /// File &gt; Open drew such a sheet with no chip files at all (found 24 November 2026 while
        /// writing the release workflow).
        /// </remarks>
        /// <param name="sheetPath">
        /// The sheet file, or null for a sheet not saved yet.
        /// </param>
        /// <returns>
        /// The catalogue folder, or null when neither place has one.
        /// </returns>
        public static string? FindForSheet(string? sheetPath)
            => (sheetPath == null ? null : Find(sheetPath)) ?? Find(AppContext.BaseDirectory);

        /// <summary>
        /// The chip types that have a file, sorted by name.
        /// </summary>
        /// <returns>
        /// The chip types, such as 7400.
        /// </returns>
        public List<string> Types()
        {
            List<string> result = new();
            string dir = Path.Combine(Folder, "chips");
            if (!Directory.Exists(dir))
                return result;
            string[] files = Directory.GetFiles(dir, "*.json");
            for (int i = 0; i < files.Length; i++)
                result.Add(Path.GetFileNameWithoutExtension(files[i]));
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        /// <summary>
        /// The chip file for a type, or null when the catalogue has none (or the file lists no pins:
        /// a chip with no datasheet found).
        /// </summary>
        /// <param name="type">
        /// The chip type, such as 7400.
        /// </param>
        /// <returns>
        /// The chip, or null.
        /// </returns>
        public ChipInfo? Get(string type)
        {
            if (_cache.TryGetValue(type, out ChipInfo? cached))
                return cached;
            ChipInfo? info = null;
            PassiveKind kind = PassiveParts.KindOf(type);
            if (kind != PassiveKind.None)
            {
                // a passive part: its pins come from the type (PassiveParts), its title from the
                // kind's file in catalog/passives/ when there is one
                info = new ChipInfo { Id = type, Title = PassiveTitle(kind) };
                info.Pins.AddRange(PassiveParts.Pins(type));
                _cache[type] = info;
                return info;
            }
            string path = Path.Combine(Folder, "chips", type + ".json");
            // a type with a path character in it is not a chip name; never read outside the folder
            if (type.Length > 0 && type.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && File.Exists(path))
                info = Parse(File.ReadAllText(path));
            // a 74 family part with no file of its own (74LS240, 74F04) has the pins and logic of the
            // plain type (74240, 7404); its timing is not the same, so ReadFrom says which file it is
            string? plain = info == null ? PlainType(type) : null;
            if (plain != null)
            {
                string plainPath = Path.Combine(Folder, "chips", plain + ".json");
                if (File.Exists(plainPath))
                {
                    info = Parse(File.ReadAllText(plainPath));
                    if (info != null)
                        info.ReadFrom = plain;
                }
            }
            _cache[type] = info;
            return info;
        }

        /// <summary>
        /// The plain 74 type of a family part: 74LS240 -> 74240, 74F04 -> 7404, 74ALS00 -> 7400.
        /// Null for anything else (a plain type, or not a 74 part).
        /// </summary>
        /// <param name="type">
        /// The chip type.
        /// </param>
        /// <returns>
        /// The plain type, or null.
        /// </returns>
        public static string? PlainType(string type)
        {
            if (!type.StartsWith("74", StringComparison.Ordinal))
                return null;
            int i = 2;
            while (i < type.Length && char.IsAsciiLetterUpper(type[i]))
                i++;
            if (i == 2 || i == type.Length)
                return null;
            string family = type.Substring(2, i - 2);
            if (family != "LS" && family != "S" && family != "F" && family != "ALS" && family != "AS" && family != "L" && family != "H" && family != "HC" && family != "HCT")
                return null;
            for (int k = i; k < type.Length; k++)
            {
                if (!char.IsAsciiDigit(type[k]))
                    return null;
            }
            return "74" + type.Substring(i);
        }

        /// <summary>
        /// The title of a passive kind from its <c>catalog/passives/</c> file, else the kind in
        /// words.
        /// </summary>
        /// <param name="kind">
        /// The kind.
        /// </param>
        /// <returns>
        /// The title.
        /// </returns>
        public string PassiveTitle(PassiveKind kind)
        {
            string path = Path.Combine(Folder, "passives", PassiveParts.CatalogueId(kind) + ".json");
            if (File.Exists(path))
            {
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("title", out JsonElement t) && t.ValueKind == JsonValueKind.String)
                    return t.GetString()!;
            }
            return PassiveParts.KindText(kind);
        }

        /// <summary>
        /// Reads a chip file's text.
        /// </summary>
        /// <param name="json">
        /// The chip file's text.
        /// </param>
        /// <returns>
        /// The chip, or null when it lists no pins.
        /// </returns>
        public static ChipInfo? Parse(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (!root.TryGetProperty("pins", out JsonElement pins) || pins.ValueKind != JsonValueKind.Array)
                return null;
            ChipInfo info = new()
            {
                Id = root.TryGetProperty("id", out JsonElement id) ? id.GetString() ?? "" : "",
                Title = root.TryGetProperty("title", out JsonElement title) ? title.GetString() ?? "" : "",
            };
            for (int i = 0; i < pins.GetArrayLength(); i++)
            {
                JsonElement p = pins[i];
                info.Pins.Add(new ChipPin
                {
                    Number = p.GetProperty("pin").GetInt32(),
                    Name = p.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? "" : "",
                    Inverted = p.TryGetProperty("inverted", out JsonElement inv) && inv.ValueKind == JsonValueKind.True,
                    Role = p.TryGetProperty("role", out JsonElement r) ? r.GetString() ?? "" : "",
                    OutputType = p.TryGetProperty("outputType", out JsonElement ot) ? ot.GetString() ?? "" : "",
                });
            }
            if (root.TryGetProperty("functions", out JsonElement fs) && fs.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < fs.GetArrayLength(); i++)
                {
                    JsonElement f = fs[i];
                    if (!f.TryGetProperty("formula", out JsonElement formula) || formula.ValueKind != JsonValueKind.String)
                        continue;
                    string text = formula.GetString()!;
                    info.Formulas.Add(text);
                    string output = f.TryGetProperty("output", out JsonElement o) && o.ValueKind == JsonValueKind.String ? o.GetString()! : "";
                    Gate? g = GateFromFormula(text, output, info.Pins);
                    if (g != null)
                        info.Gates.Add(g);
                }
            }
            return info;
        }

        /// <summary>
        /// Makes a gate from one formula when it is a plain gate of named input pins:
        /// <c>NOT(A AND B ...)</c> is NAND, <c>NOT(A OR B ...)</c> is NOR, <c>NOT A</c> is an
        /// inverter, <c>A AND B</c> is AND, <c>A OR B</c> is OR and a lone pin name is a buffer.
        /// Anything else (XOR, a flip-flop's stored state, a counter) gives no gate: the chip is then
        /// drawn with its pins listed instead of a gate symbol, never as a made-up gate.
        /// </summary>
        /// <param name="formula">
        /// The formula, such as <c>1Y = NOT(1A AND 1B)</c>.
        /// </param>
        /// <param name="output">
        /// The file's output field: one pin number. A list of pins gives no gate.
        /// </param>
        /// <param name="pins">
        /// The chip's pins, to turn names into numbers.
        /// </param>
        /// <returns>
        /// The gate, or null when the formula is not a plain gate.
        /// </returns>
        public static Gate? GateFromFormula(string formula, string output, List<ChipPin> pins)
        {
            if (!int.TryParse(output, NumberStyles.None, CultureInfo.InvariantCulture, out int outPin))
                return null;
            int eq = formula.IndexOf('=');
            if (eq < 0)
                return null;
            string rhs = formula.Substring(eq + 1).Trim();
            bool inverted = false;
            if (rhs.StartsWith("NOT(", StringComparison.Ordinal) && rhs.EndsWith(')'))
            {
                inverted = true;
                rhs = rhs.Substring(4, rhs.Length - 5).Trim();
            }
            else if (rhs.StartsWith("NOT ", StringComparison.Ordinal))
            {
                inverted = true;
                rhs = rhs.Substring(4).Trim();
            }

            string[] words = rhs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // names at even places, one operator word between them, the same one every time
            string op = words.Length > 1 ? words[1] : "";
            if (words.Length % 2 == 0 || (op.Length > 0 && op != "AND" && op != "OR"))
                return null;
            Gate g = new() { Output = outPin, Confidence = Confidence.Read };
            for (int i = 0; i < words.Length; i++)
            {
                if ((i & 1) == 1)
                {
                    if (words[i] != op)
                        return null;
                    continue;
                }
                int number = PinNumber(pins, words[i]);
                if (number < 0)
                    return null;
                g.Inputs.Add(number);
            }
            if (words.Length == 1)
                g.Kind = inverted ? GateKind.Not : GateKind.Buffer;
            else if (op == "AND")
                g.Kind = inverted ? GateKind.Nand : GateKind.And;
            else
                g.Kind = inverted ? GateKind.Nor : GateKind.Or;
            return g;
        }

        private static int PinNumber(List<ChipPin> pins, string name)
        {
            for (int i = 0; i < pins.Count; i++)
            {
                if (pins[i].Key == name && (pins[i].Role == "input" || pins[i].Role == "clock" || pins[i].Role == "enable"))
                    return pins[i].Number;
            }
            return -1;
        }
    }
}
