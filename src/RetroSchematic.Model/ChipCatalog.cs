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
        /// The datasheet's name for the pin (1A, GND, 1Q').
        /// </summary>
        public string Name { get; set; } = "";

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
            string path = Path.Combine(Folder, "chips", type + ".json");
            // a type with a path character in it is not a chip name; never read outside the folder
            if (type.Length > 0 && type.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && File.Exists(path))
                info = Parse(File.ReadAllText(path));
            _cache[type] = info;
            return info;
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
                if (pins[i].Name == name && (pins[i].Role == "input" || pins[i].Role == "clock" || pins[i].Role == "enable"))
                    return pins[i].Number;
            }
            return -1;
        }
    }
}
