//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RetroSchematic.Model
{
    /// <summary>
    /// One place a bus signal sits on a connector: the connector letter ("open" when no document
    /// names it), the row (ND-100 only) and the pin number.
    /// </summary>
    public readonly struct BusPin
    {
        /// <summary>
        /// Makes a place.
        /// </summary>
        /// <param name="connector">
        /// The connector letter, or "open".
        /// </param>
        /// <param name="row">
        /// The row (a, b, c), or null.
        /// </param>
        /// <param name="pin">
        /// The pin number.
        /// </param>
        public BusPin(string connector, string? row, int pin)
        {
            Connector = connector;
            Row = row;
            Pin = pin;
        }

        /// <summary>
        /// The connector letter, or "open" when it is not known.
        /// </summary>
        public string Connector { get; }

        /// <summary>
        /// The row (a, b, c on the ND-100 plugs), or null.
        /// </summary>
        public string? Row { get; }

        /// <summary>
        /// The pin number.
        /// </summary>
        public int Pin { get; }
    }

    /// <summary>
    /// One signal of a machine bus, as the bus-signal catalogue (catalog/signals, see
    /// docs/BUS-SIGNALS.md) gives it.
    /// </summary>
    public sealed class BusSignal
    {
        /// <summary>
        /// The name ND writes on the backplane, such as BIOXE.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The names a card drawing uses for the same pin, such as IOXE.
        /// </summary>
        public List<string> NamesOnCards { get; } = new();

        /// <summary>
        /// The group: address, data, control, interrupt, ident, power.
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// The active level: low, high or none.
        /// </summary>
        public string? Active { get; set; }

        /// <summary>
        /// The direction seen from a card: in, out, bidir, or out, open collector (shared).
        /// </summary>
        public string? Direction { get; set; }

        /// <summary>
        /// The places on the connector(s).
        /// </summary>
        public List<BusPin> Pins { get; } = new();

        /// <summary>
        /// What the signal does, or null when the catalogue says "open".
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The document the description comes from (its title from the file's sources list).
        /// </summary>
        public string? DescriptionDocument { get; set; }

        /// <summary>
        /// The place in that document.
        /// </summary>
        public string? DescriptionPlace { get; set; }

        /// <summary>
        /// How sure the description is.
        /// </summary>
        public Confidence DescriptionConfidence { get; set; } = Confidence.Open;
    }

    /// <summary>
    /// The bus-signal catalogue for one machine bus. Only the files written for that bus are read:
    /// a NORD-10 sheet never sees an ND-100 signal, and the other way round.
    /// </summary>
    public sealed class BusCatalog
    {
        private readonly Dictionary<string, BusSignal> _byName = new(StringComparer.OrdinalIgnoreCase);

        private BusCatalog(MachineBus bus)
        {
            Bus = bus;
        }

        /// <summary>
        /// The bus this catalogue is for.
        /// </summary>
        public MachineBus Bus { get; }

        /// <summary>
        /// Every signal read, in file order.
        /// </summary>
        public List<BusSignal> Signals { get; } = new();

        /// <summary>
        /// An empty catalogue (for an unknown bus, or no catalogue folder).
        /// </summary>
        /// <param name="bus">
        /// The bus.
        /// </param>
        /// <returns>
        /// A catalogue with no signals.
        /// </returns>
        public static BusCatalog Empty(MachineBus bus) => new(bus);

        /// <summary>
        /// True when a catalogue file's <c>machine</c> text is the given bus. NORD-10 and NORD-100
        /// are told apart exactly: "NORD-100" starts with "NORD-10", so a plain prefix test would
        /// mix them.
        /// </summary>
        /// <param name="machine">
        /// The file's machine text, such as "NORD-10" or "NORD-100 (ND-100)".
        /// </param>
        /// <param name="bus">
        /// The bus.
        /// </param>
        /// <returns>
        /// True when it is that bus.
        /// </returns>
        public static bool IsFor(string? machine, MachineBus bus)
        {
            if (machine == null)
                return false;
            string m = machine.Trim();
            switch (bus)
            {
                case MachineBus.Nord10:
                    return string.Equals(m, "NORD-10", StringComparison.OrdinalIgnoreCase);
                case MachineBus.Nd100:
                    return m.StartsWith("NORD-100", StringComparison.OrdinalIgnoreCase) || m.StartsWith("ND-100", StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Reads every file in <c>signals/</c> of a catalogue folder that is for the bus.
        /// </summary>
        /// <param name="catalogFolder">
        /// The catalogue folder (the one holding <c>signals/</c>), or null.
        /// </param>
        /// <param name="bus">
        /// The sheet's bus. Unknown reads nothing.
        /// </param>
        /// <returns>
        /// The catalogue.
        /// </returns>
        public static BusCatalog Load(string? catalogFolder, MachineBus bus)
        {
            BusCatalog c = new(bus);
            if (catalogFolder == null || bus == MachineBus.Unknown)
                return c;
            string dir = Path.Combine(catalogFolder, "signals");
            if (!Directory.Exists(dir))
                return c;
            string[] files = Directory.GetFiles(dir, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            for (int i = 0; i < files.Length; i++)
                c.Read(File.ReadAllText(files[i]));
            return c;
        }

        /// <summary>
        /// Reads one catalogue file's text, if it is for this catalogue's bus.
        /// </summary>
        /// <param name="json">
        /// The file's text.
        /// </param>
        public void Read(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (!IsFor(Str(root, "machine"), Bus) || !root.TryGetProperty("signals", out JsonElement list))
                return;
            Dictionary<string, string> sources = new(StringComparer.Ordinal);
            if (root.TryGetProperty("sources", out JsonElement src))
            {
                for (int i = 0; i < src.GetArrayLength(); i++)
                {
                    string? id = Str(src[i], "id");
                    if (id != null)
                        sources[id] = Str(src[i], "document") ?? id;
                }
            }
            for (int i = 0; i < list.GetArrayLength(); i++)
            {
                JsonElement e = list[i];
                BusSignal s = new() { Name = Str(e, "name") ?? "" };
                if (e.TryGetProperty("namesOnCards", out JsonElement names))
                {
                    for (int j = 0; j < names.GetArrayLength(); j++)
                        s.NamesOnCards.Add(names[j].GetString() ?? "");
                }
                s.Group = Str(e, "group");
                s.Active = Fact(e, "active");
                s.Direction = Fact(e, "direction");
                if (e.TryGetProperty("pins", out JsonElement pins) && pins.TryGetProperty("value", out JsonElement pv) && pv.ValueKind == JsonValueKind.Array)
                {
                    for (int j = 0; j < pv.GetArrayLength(); j++)
                    {
                        JsonElement p = pv[j];
                        if (p.TryGetProperty("pin", out JsonElement n) && n.ValueKind == JsonValueKind.Number)
                            s.Pins.Add(new BusPin(Str(p, "connector") ?? "open", Str(p, "row"), n.GetInt32()));
                    }
                }
                if (e.TryGetProperty("description", out JsonElement d))
                {
                    string? text = Str(d, "value");
                    string conf = Str(d, "confidence") ?? "open";
                    if (text != null && text != "open")
                    {
                        s.Description = text;
                        string? sid = Str(d, "source");
                        s.DescriptionDocument = sid != null && sources.TryGetValue(sid, out string? title) ? title : sid;
                        s.DescriptionPlace = Str(d, "page");
                        s.DescriptionConfidence = conf == "read" ? Confidence.Read : conf == "inferred" ? Confidence.Inferred : Confidence.Open;
                    }
                }
                Signals.Add(s);
                Index(s.Name, s);
                for (int j = 0; j < s.NamesOnCards.Count; j++)
                    Index(s.NamesOnCards[j], s);
            }
        }

        /// <summary>
        /// The signal with this backplane name or card name (BIOXE or IOXE), or null.
        /// </summary>
        /// <param name="name">
        /// The bare name, without a level mark.
        /// </param>
        /// <returns>
        /// The signal, or null.
        /// </returns>
        public BusSignal? ByName(string name)
        {
            return _byName.TryGetValue(name, out BusSignal? s) ? s : null;
        }

        /// <summary>
        /// The signal on a connector pin, or null. A connector letter of null or "open" matches a
        /// catalogue place whose connector is not known; a known letter matches only that letter.
        /// </summary>
        /// <param name="connector">
        /// The connector letter, or null when it is not known.
        /// </param>
        /// <param name="pin">
        /// The pin number.
        /// </param>
        /// <returns>
        /// The signal, or null.
        /// </returns>
        public BusSignal? ByPin(string? connector, int pin)
        {
            string want = connector ?? "open";
            for (int i = 0; i < Signals.Count; i++)
            {
                List<BusPin> pins = Signals[i].Pins;
                for (int j = 0; j < pins.Count; j++)
                {
                    if (pins[j].Pin == pin && pins[j].Row == null && string.Equals(pins[j].Connector, want, StringComparison.OrdinalIgnoreCase))
                        return Signals[i];
                }
            }
            return null;
        }

        private void Index(string name, BusSignal s)
        {
            if (name.Length > 0 && !_byName.ContainsKey(name))
                _byName[name] = s;
        }

        private static string? Fact(JsonElement e, string name)
        {
            return e.TryGetProperty(name, out JsonElement f) ? Str(f, "value") : null;
        }

        private static string? Str(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out JsonElement v))
                return null;
            return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ValueKind == JsonValueKind.Number ? v.GetRawText() : null;
        }
    }
}
