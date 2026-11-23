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
    /// One connector type from catalog/connectors (see docs/BUS-SIGNALS.md): its title and the
    /// names the catalogue gives single pins.
    /// </summary>
    public sealed class ConnectorInfo
    {
        /// <summary>
        /// The file's id, such as nord10-io-card-backplane.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// The title, such as "NORD-10 I/O device card: connector to the backplane".
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// The name the catalogue gives a pin, by pin number (only pins listed one by one).
        /// </summary>
        public Dictionary<int, string> PinNames { get; } = new();
    }

    /// <summary>
    /// The connector catalogue for one machine bus: only the files written for that bus.
    /// </summary>
    public sealed class ConnectorCatalog
    {
        private readonly Dictionary<string, ConnectorInfo> _byId = new(StringComparer.Ordinal);

        private ConnectorCatalog(MachineBus bus)
        {
            Bus = bus;
        }

        /// <summary>
        /// The bus this catalogue is for.
        /// </summary>
        public MachineBus Bus { get; }

        /// <summary>
        /// Reads every file in <c>connectors/</c> of a catalogue folder that is for the bus.
        /// </summary>
        /// <param name="catalogFolder">
        /// The catalogue folder, or null.
        /// </param>
        /// <param name="bus">
        /// The sheet's bus. Unknown reads nothing.
        /// </param>
        /// <returns>
        /// The catalogue.
        /// </returns>
        public static ConnectorCatalog Load(string? catalogFolder, MachineBus bus)
        {
            ConnectorCatalog c = new(bus);
            if (catalogFolder == null || bus == MachineBus.Unknown)
                return c;
            string dir = Path.Combine(catalogFolder, "connectors");
            if (!Directory.Exists(dir))
                return c;
            string[] files = Directory.GetFiles(dir, "*.json");
            for (int i = 0; i < files.Length; i++)
                c.Read(File.ReadAllText(files[i]));
            return c;
        }

        /// <summary>
        /// Reads one connector file's text, if it is for this catalogue's bus.
        /// </summary>
        /// <param name="json">
        /// The file's text.
        /// </param>
        public void Read(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            string? machine = root.TryGetProperty("machine", out JsonElement m) ? m.GetString() : null;
            if (!BusCatalog.IsFor(machine, Bus))
                return;
            ConnectorInfo info = new()
            {
                Id = root.TryGetProperty("id", out JsonElement id) ? id.GetString() ?? "" : "",
                Title = root.TryGetProperty("title", out JsonElement t) ? t.GetString() ?? "" : "",
            };
            if (root.TryGetProperty("pins", out JsonElement pins) && pins.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < pins.GetArrayLength(); i++)
                {
                    JsonElement p = pins[i];
                    // only pins listed one by one; "6, 7" style groups name a use, not a pin
                    if (!p.TryGetProperty("pin", out JsonElement n) || n.ValueKind != JsonValueKind.Number)
                        continue;
                    string? name = p.TryGetProperty("signal", out JsonElement s) ? s.GetString() : null;
                    if (name == null)
                        continue;
                    if (p.TryGetProperty("v24", out JsonElement v) && v.ValueKind == JsonValueKind.Number)
                        name += " (V.24 " + v.GetInt32().ToString(CultureInfo.InvariantCulture) + ")";
                    info.PinNames[n.GetInt32()] = name;
                }
            }
            if (info.Id.Length > 0)
                _byId[info.Id] = info;
        }

        /// <summary>
        /// The connector with this id, or null (also null when it is for another bus).
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The connector, or null.
        /// </returns>
        public ConnectorInfo? Get(string id) => _byId.TryGetValue(id, out ConnectorInfo? c) ? c : null;
    }
}
