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
using System.Text.RegularExpressions;

namespace RetroSchematic.Model
{
    /// <summary>
    /// The ND PCB catalogue: a pcb.json file scraped from norsk-data.com (one entry per ND
    /// board, under "pcbs"), with one folder per part number beside it holding the board's
    /// photos. It lives outside the repository and is only read, never changed. An entry is
    /// found by its card number ("1050") or its part number ("322049") and turned into a
    /// sheet's board section (<see cref="BoardSection"/>), with the catalogue recorded as the
    /// source of every fact it fills.
    /// </summary>
    public sealed class PcbCatalogue
    {
        /// <summary>
        /// The document name written into every source taken from the catalogue.
        /// </summary>
        public const string DocumentName = "ND PCB catalogue (pcb.json, scraped from norsk-data.com)";

        // "IDENT (position E1), ..." - the catalogue writes the row letter first
        private static readonly Regex s_position = new(@"\(position\s+([A-Z]{1,2})(\d{1,2})\)", RegexOptions.CultureInvariant);

        // "156 x 149 mm"
        private static readonly Regex s_size = new(@"^\s*(\d+(?:\.\d+)?)\s*x\s*(\d+(?:\.\d+)?)\s*mm\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private readonly List<CatalogueEntry> _entries = new();

        private PcbCatalogue(string folder)
        {
            Folder = folder;
        }

        /// <summary>
        /// The folder pcb.json is in; the photo folders are inside it.
        /// </summary>
        public string Folder { get; }

        /// <summary>
        /// The number of boards in the catalogue.
        /// </summary>
        public int Count => _entries.Count;

        /// <summary>
        /// Reads a catalogue file.
        /// </summary>
        /// <param name="path">
        /// pcb.json.
        /// </param>
        /// <returns>
        /// The catalogue.
        /// </returns>
        /// <exception cref="IOException">
        /// The file cannot be read or is not a catalogue (no "pcbs" list).
        /// </exception>
        public static PcbCatalogue Load(string path)
        {
            string full = Path.GetFullPath(path);
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(full);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new IOException("The PCB catalogue " + path + " cannot be read: " + ex.Message, ex);
            }
            return Parse(bytes, Path.GetDirectoryName(full)!);
        }

        /// <summary>
        /// Reads a catalogue from its JSON text.
        /// </summary>
        /// <param name="json">
        /// The bytes of pcb.json.
        /// </param>
        /// <param name="folder">
        /// The folder the photo folders are in.
        /// </param>
        /// <returns>
        /// The catalogue.
        /// </returns>
        /// <exception cref="IOException">
        /// The text is not a catalogue.
        /// </exception>
        public static PcbCatalogue Parse(byte[] json, string folder)
        {
            PcbCatalogue c = new(folder);
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("pcbs", out JsonElement list) || list.ValueKind != JsonValueKind.Array)
                    throw new IOException("This is not an ND PCB catalogue: there is no \"pcbs\" list.");
                int n = list.GetArrayLength();
                for (int i = 0; i < n; i++)
                {
                    if (list[i].ValueKind == JsonValueKind.Object)
                        c._entries.Add(Entry(list[i], folder));
                }
            }
            catch (JsonException ex)
            {
                throw new IOException("The PCB catalogue is not valid JSON: " + ex.Message, ex);
            }
            return c;
        }

        /// <summary>
        /// The boards whose part number or card number is the key. A part number is matched
        /// first ("ND-322049" and "322049" both match 322049); only when no part number
        /// matches are card numbers tried, which several boards can share.
        /// </summary>
        /// <param name="key">
        /// A part number or a card number.
        /// </param>
        /// <returns>
        /// The matching entries, empty when none.
        /// </returns>
        public List<CatalogueEntry> Find(string key)
        {
            List<CatalogueEntry> found = new();
            string k = key.Trim();
            if (k.StartsWith("ND-", StringComparison.OrdinalIgnoreCase))
                k = k.Substring(3);
            if (k.Length == 0)
                return found;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].PartNumber, k, StringComparison.OrdinalIgnoreCase))
                    found.Add(_entries[i]);
            }
            if (found.Count > 0)
                return found;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].Number, k, StringComparison.OrdinalIgnoreCase))
                    found.Add(_entries[i]);
            }
            return found;
        }

        /// <summary>
        /// The board position a catalogue switch text names, written the way our drawings write
        /// it: the catalogue's "(position E1)" is row E, column 1, which the drawings write
        /// column first as 1E.
        /// </summary>
        /// <param name="text">
        /// The catalogue's switch text.
        /// </param>
        /// <returns>
        /// The position, such as "1E", or null when the text names none.
        /// </returns>
        public static string? SwitchPosition(string? text)
        {
            if (text == null)
                return null;
            Match m = s_position.Match(text);
            return m.Success ? m.Groups[2].Value + m.Groups[1].Value : null;
        }

        /// <summary>
        /// The ND machine name for the catalogue's "ndSystem" code: "10" is NORD-10, "100" is
        /// ND-100. Other machines are not sheet machines yet and give null.
        /// </summary>
        /// <param name="code">
        /// The catalogue's code.
        /// </param>
        /// <returns>
        /// NORD-10, ND-100 or null.
        /// </returns>
        public static string? MachineName(string? code)
            => code switch
            {
                "10" => "NORD-10",
                "100" => "ND-100",
                _ => null,
            };

        /// <summary>
        /// Reads "156 x 149 mm".
        /// </summary>
        /// <param name="text">
        /// The catalogue's size text.
        /// </param>
        /// <returns>
        /// The size, or null when the text is not in that form.
        /// </returns>
        public static BoardSize? ParseSize(string? text)
        {
            if (text == null)
                return null;
            Match m = s_size.Match(text);
            if (!m.Success)
                return null;
            return new BoardSize
            {
                WidthMm = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                HeightMm = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
            };
        }

        private static CatalogueEntry Entry(JsonElement e, string folder)
        {
            string? url = Str(e, "sourceUrl");
            DescriptionSource src = new() { Kind = SourceKind.Catalogue, Document = DocumentName, Place = url, Confidence = Confidence.Read };
            BoardSection b = new() { FactSources = new Dictionary<string, DescriptionSource>(StringComparer.Ordinal) };
            CatalogueEntry c = new() { Board = b, Folder = Str(e, "folder") };

            b.Number = Fact(b, "number", Str(e, "number"), src);
            b.PartNumber = Fact(b, "partNumber", Str(e, "partNumber"), src);
            b.Title = Fact(b, "title", Str(e, "title"), src);
            string? system = Str(e, "ndSystem");
            b.NdSystem = Fact(b, "ndSystem", MachineName(system), src);
            c.CatalogueSystem = system;
            b.Group = Fact(b, "group", Str(e, "group"), src);
            b.Description = Fact(b, "description", Str(e, "description"), src);
            b.PrintVersion = Fact(b, "printVersion", Str(e, "printVersion"), src);
            b.EcoVersion = Fact(b, "ecoVersion", Str(e, "ecoVersion"), src);
            b.ReleaseDate = Fact(b, "releaseDate", Str(e, "releaseDate"), src);
            b.SourceUrl = url;

            if (e.TryGetProperty("printVersions", out JsonElement pv) && pv.ValueKind == JsonValueKind.Array && pv.GetArrayLength() > 0)
            {
                b.PrintVersions = new List<BoardPrintVersion>();
                for (int i = 0; i < pv.GetArrayLength(); i++)
                    b.PrintVersions.Add(new BoardPrintVersion { Print = Str(pv[i], "print") ?? "", Eco = Str(pv[i], "eco"), Note = Str(pv[i], "note") });
                b.FactSources["printVersions"] = src;
            }

            if (e.TryGetProperty("specifications", out JsonElement spec) && spec.ValueKind == JsonValueKind.Object)
            {
                BoardSize? size = ParseSize(Str(spec, "size"));
                int? layers = spec.TryGetProperty("layers", out JsonElement l) && l.ValueKind == JsonValueKind.Number && l.TryGetInt32(out int li) && li > 0 ? li : null;
                if (size != null || layers != null)
                {
                    b.Specifications = new BoardSpecifications { Size = size, Layers = layers };
                    b.FactSources["specifications"] = src;
                }
            }

            b.Leds = Items(e, "leds", "id", b, src);
            b.Straps = Items(e, "straps", "id", b, src);
            b.Switches = Items(e, "switches", "id", b, src);
            b.Connectors = Items(e, "connectors", "id", b, src);

            if (e.TryGetProperty("chipset", out JsonElement chips) && chips.ValueKind == JsonValueKind.Array && chips.GetArrayLength() > 0)
            {
                b.Chipset = new List<BoardChip>();
                for (int i = 0; i < chips.GetArrayLength(); i++)
                    b.Chipset.Add(new BoardChip { Name = Str(chips[i], "name") ?? "", Description = Str(chips[i], "description"), Url = Str(chips[i], "url") });
                b.FactSources["chipset"] = src;
            }

            if (e.TryGetProperty("images", out JsonElement images) && images.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < images.GetArrayLength(); i++)
                {
                    JsonElement im = images[i];
                    string? type = Str(im, "type");
                    string? file = Str(im, "filename");
                    if (file == null || (type != "component_side" && type != "solder_side"))
                        continue;
                    c.Photos.Add(new CataloguePhoto
                    {
                        Path = c.Folder == null ? Path.Combine(folder, file) : Path.Combine(folder, c.Folder, file),
                        Side = type == "component_side" ? BoardSide.Component : BoardSide.Solder,
                        Version = Str(im, "version"),
                        Variant = Str(im, "variant"),
                        Caption = Str(im, "caption"),
                        Original = Str(im, "source"),
                    });
                }
            }
            if (b.FactSources.Count == 0)
                b.FactSources = null;
            return c;
        }

        private static string? Fact(BoardSection b, string field, string? value, DescriptionSource src)
        {
            if (value == null)
                return null;
            b.FactSources![field] = src;
            return value;
        }

        // one of the catalogue's LED / strap / switch / connector lists; the text is the
        // catalogue's "text", else its "description" or (connectors) "type"
        private static List<BoardItem>? Items(JsonElement e, string name, string idField, BoardSection b, DescriptionSource src)
        {
            if (!e.TryGetProperty(name, out JsonElement list) || list.ValueKind != JsonValueKind.Array || list.GetArrayLength() == 0)
                return null;
            List<BoardItem> items = new();
            for (int i = 0; i < list.GetArrayLength(); i++)
            {
                JsonElement it = list[i];
                items.Add(new BoardItem
                {
                    Id = Str(it, idField),
                    Color = Str(it, "color"),
                    Text = Str(it, "text") ?? Str(it, "description") ?? Str(it, "type"),
                });
            }
            b.FactSources![name] = src;
            return items;
        }

        // a string field, or null when it is missing or empty
        private static string? Str(JsonElement e, string name)
        {
            if (!e.TryGetProperty(name, out JsonElement v))
                return null;
            string? s = v.ValueKind == JsonValueKind.String ? v.GetString() : v.ValueKind == JsonValueKind.Number ? v.GetRawText() : null;
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }
    }

    /// <summary>
    /// One board of the ND PCB catalogue, ready to import.
    /// </summary>
    public sealed class CatalogueEntry
    {
        /// <summary>
        /// The board facts, with the catalogue as the source of each.
        /// </summary>
        public BoardSection Board { get; set; } = new();

        /// <summary>
        /// The card number, such as "1050".
        /// </summary>
        public string? Number => Board.Number;

        /// <summary>
        /// The part number, such as "322049".
        /// </summary>
        public string? PartNumber => Board.PartNumber;

        /// <summary>
        /// The catalogue's own machine code ("10", "100", "500" ...), kept because only NORD-10
        /// and ND-100 have a sheet name.
        /// </summary>
        public string? CatalogueSystem { get; set; }

        /// <summary>
        /// The board's photo folder name inside the catalogue folder.
        /// </summary>
        public string? Folder { get; set; }

        /// <summary>
        /// The component- and solder-side photos (other pictures are left out).
        /// </summary>
        public List<CataloguePhoto> Photos { get; } = new();
    }

    /// <summary>
    /// One board photo in the catalogue.
    /// </summary>
    public sealed class CataloguePhoto
    {
        /// <summary>
        /// The picture file on disk.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Which side it shows.
        /// </summary>
        public BoardSide Side { get; set; }

        /// <summary>
        /// The print version, such as "I".
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// "old", "new" or other words, or null.
        /// </summary>
        public string? Variant { get; set; }

        /// <summary>
        /// The catalogue's caption.
        /// </summary>
        public string? Caption { get; set; }

        /// <summary>
        /// The picture's name on norsk-data.com, such as "nd-322049-3.jpg".
        /// </summary>
        public string? Original { get; set; }
    }
}
