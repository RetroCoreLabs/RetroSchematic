//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RetroSchematic.Model
{
    /// <summary>
    /// Reads a netlist file and checks it (docs/FORMAT.md, "Checks a reader makes").
    /// A file of an unknown format version is refused, never guessed at.
    /// </summary>
    public static class NetlistReader
    {
        /// <summary>The format versions this reader understands.</summary>
        public const int SupportedFormatVersion = 1;

        private static readonly JsonSerializerOptions s_options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower, allowIntegerValues: false) },
        };

        /// <summary>Read and check a file.</summary>
        public static Netlist ReadFile(string path) => Parse(File.ReadAllText(path));

        /// <summary>Read and check JSON text.</summary>
        /// <exception cref="NetlistException">The text is not a valid version-1 netlist.</exception>
        public static Netlist Parse(string json)
        {
            // Look at the header first, so a newer file gets a version message and not a field error.
            using (JsonDocument header = JsonDocument.Parse(json))
            {
                JsonElement root = header.RootElement;
                if (!root.TryGetProperty("format", out JsonElement format) || format.GetString() != "nd-netlist")
                    throw new NetlistException("Not an ND netlist file: \"format\" must be \"nd-netlist\".");
                if (!root.TryGetProperty("formatVersion", out JsonElement version) || version.ValueKind != JsonValueKind.Number)
                    throw new NetlistException("\"formatVersion\" is missing.");
                if (version.GetInt32() != SupportedFormatVersion)
                    throw new NetlistException($"Format version {version.GetInt32()} is not supported; this reader knows version {SupportedFormatVersion}.");
            }

            Netlist? netlist;
            try
            {
                netlist = JsonSerializer.Deserialize<Netlist>(json, s_options);
            }
            catch (JsonException e)
            {
                throw new NetlistException("Invalid netlist: " + e.Message, e);
            }
            if (netlist == null)
                throw new NetlistException("Empty netlist file.");

            Check(netlist);
            return netlist;
        }

        /// <summary>Write a netlist as JSON in the file's own style.</summary>
        public static string Write(Netlist netlist) => JsonSerializer.Serialize(netlist, s_options);

        private static void Check(Netlist netlist)
        {
            HashSet<string> parts = new(StringComparer.Ordinal);
            for (int i = 0; i < netlist.Parts.Count; i++)
            {
                if (!parts.Add(netlist.Parts[i].Ref))
                    throw new NetlistException($"Part {netlist.Parts[i].Ref} is listed twice.");
            }

            HashSet<string> gateIds = new(StringComparer.Ordinal);
            for (int i = 0; i < netlist.Gates.Count; i++)
            {
                Gate gate = netlist.Gates[i];
                if (!gateIds.Add(gate.Id))
                    throw new NetlistException($"Gate {gate.Id} is listed twice.");
                if (!parts.Contains(gate.Part))
                    throw new NetlistException($"Gate {gate.Id} is in part {gate.Part}, which is not in the parts list.");
            }

            Dictionary<string, string> pinOwner = new(StringComparer.Ordinal);
            for (int i = 0; i < netlist.Nets.Count; i++)
            {
                Net net = netlist.Nets[i];
                if (net.PullUp != null && net.Drive != LineDrive.OpenCollector)
                    throw new NetlistException($"Net {net.Name} has a pull-up but its drive is not open-collector.");
                if (net.DriveConfidence != null && net.Drive == null)
                    throw new NetlistException($"Net {net.Name} says how sure its drive is, but has no drive.");
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    PinReference pin = net.Pins[j];
                    int dot = pin.At.LastIndexOf('.');
                    if (dot <= 0 || dot == pin.At.Length - 1 || !int.TryParse(pin.At.AsSpan(dot + 1), out _))
                        throw new NetlistException($"Net {net.Name}: pin \"{pin.At}\" is not <part>.<pin number>.");
                    string partRef = pin.PartRef;
                    if (partRef != "P" && partRef != "F" && !parts.Contains(partRef))
                        throw new NetlistException($"Net {net.Name}: pin {pin.At} names part {partRef}, which is not in the parts list.");
                    if (pinOwner.TryGetValue(pin.At, out string? other))
                        throw new NetlistException($"Pin {pin.At} is in two nets: {other} and {net.Name}.");
                    pinOwner.Add(pin.At, net.Name);
                }
            }
        }
    }

    /// <summary>A netlist file that is not valid.</summary>
    public sealed class NetlistException : Exception
    {
        /// <summary>Create with a message.</summary>
        public NetlistException(string message) : base(message) { }

        /// <summary>Create with a message and the cause.</summary>
        public NetlistException(string message, Exception inner) : base(message, inner) { }
    }
}
