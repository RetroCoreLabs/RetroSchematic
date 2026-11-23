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
        /// <summary>
        /// The format version of a design with one level (no blocks): new sheets start here.
        /// </summary>
        public const int SupportedFormatVersion = 1;

        /// <summary>
        /// The newest format version this reader understands: version 2 adds blocks.
        /// </summary>
        public const int BlocksFormatVersion = 2;

        private static readonly JsonSerializerOptions s_options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true,
            // an absent optional field is left out, not written as null, so a written file
            // matches the schema (which has no null values)
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower, allowIntegerValues: false) },
        };

        /// <summary>
        /// Read and check a file: .rsc (text), .rsx (zipped) or an old .json, told apart by
        /// content (<see cref="SheetFile"/>).
        /// </summary>
        /// <param name="path">
        /// The sheet file.
        /// </param>
        /// <returns>
        /// The checked netlist.
        /// </returns>
        public static Netlist ReadFile(string path) => Parse(SheetFile.ReadText(path));

        /// <summary>
        /// Read and check JSON text.
        /// </summary>
        /// <exception cref="NetlistException">
        /// The text is not a valid version-1 netlist.
        /// </exception>
        /// <param name="json">
        /// The file's text.
        /// </param>
        /// <returns>
        /// The checked netlist.
        /// </returns>
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
                int v = version.GetInt32();
                if (v != SupportedFormatVersion && v != BlocksFormatVersion)
                    throw new NetlistException($"Format version {v} is not supported; this reader knows versions {SupportedFormatVersion} and {BlocksFormatVersion}.");
                if (v == SupportedFormatVersion && (root.TryGetProperty("blocks", out _) || root.TryGetProperty("instances", out _)))
                    throw new NetlistException("A version 1 file cannot have blocks or instances; they need format version 2.");
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

        /// <summary>
        /// Write a netlist as JSON in the file's own style.
        /// </summary>
        /// <param name="netlist">
        /// The netlist to write.
        /// </param>
        /// <returns>
        /// The JSON text.
        /// </returns>
        public static string Write(Netlist netlist)
        {
            // version 2 exactly when the design has blocks or instances (docs/FORMAT.md)
            bool blocks = (netlist.Blocks != null && netlist.Blocks.Count > 0) || (netlist.Instances != null && netlist.Instances.Count > 0);
            netlist.FormatVersion = blocks ? BlocksFormatVersion : SupportedFormatVersion;
            return JsonSerializer.Serialize(netlist, s_options);
        }

        /// <summary>
        /// A deep copy of any part of a netlist, made by writing it as JSON and reading it back.
        /// </summary>
        /// <typeparam name="T">
        /// The type copied, such as <see cref="BlockDefinition"/>.
        /// </typeparam>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The copy.
        /// </returns>
        public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, s_options), s_options)!;

        /// <summary>
        /// Any part of a netlist as JSON, in the file's own style (field names and values as in
        /// docs/FORMAT.md).
        /// </summary>
        /// <typeparam name="T">
        /// The type written, such as <see cref="BoardSection"/>.
        /// </typeparam>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The JSON text.
        /// </returns>
        public static string ToJson<T>(T value) => JsonSerializer.Serialize(value, s_options);

        /// <summary>
        /// Reads any part of a netlist from JSON in the file's own style. An unknown field is
        /// refused, as in a sheet file.
        /// </summary>
        /// <typeparam name="T">
        /// The type read.
        /// </typeparam>
        /// <param name="json">
        /// The JSON text.
        /// </param>
        /// <returns>
        /// The value.
        /// </returns>
        /// <exception cref="NetlistException">
        /// The text is not valid for the type.
        /// </exception>
        public static T FromJson<T>(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, s_options) ?? throw new NetlistException("Empty JSON.");
            }
            catch (JsonException e)
            {
                throw new NetlistException("Invalid JSON: " + e.Message, e);
            }
        }

        /// <summary>
        /// The signal a reference names on a level, or null: the first of that name when no
        /// level is given.
        /// </summary>
        /// <param name="nets">
        /// The level's signals.
        /// </param>
        /// <param name="r">
        /// The reference.
        /// </param>
        /// <returns>
        /// The signal, or null.
        /// </returns>
        public static Net? FindNet(List<Net> nets, SignalRef r)
        {
            for (int i = 0; i < nets.Count; i++)
            {
                if (nets[i].Name == r.Net && (r.Active == null || r.Active == nets[i].Active))
                    return nets[i];
            }
            return null;
        }

        private static void Check(Netlist netlist)
        {
            CheckLevel("the top level", netlist.Parts, netlist.Gates, netlist.Nets);
            List<BlockDefinition>? blocks = netlist.Blocks;
            Dictionary<string, BlockDefinition> byName = new(StringComparer.Ordinal);
            for (int i = 0; blocks != null && i < blocks.Count; i++)
            {
                if (!byName.TryAdd(blocks[i].Name, blocks[i]))
                    throw new NetlistException($"Block {blocks[i].Name} is defined twice.");
            }
            CheckInstances("the top level", netlist.Instances, netlist.Nets, byName);
            for (int i = 0; blocks != null && i < blocks.Count; i++)
            {
                BlockDefinition b = blocks[i];
                string where = "block " + b.Name;
                CheckLevel(where, b.Parts, b.Gates, b.Nets);
                CheckInstances(where, b.Instances, b.Nets, byName);
                HashSet<string> ports = new(StringComparer.Ordinal);
                HashSet<Net> carried = new(ReferenceEqualityComparer.Instance);
                for (int p = 0; p < b.Ports.Count; p++)
                {
                    BlockPort port = b.Ports[p];
                    if (!ports.Add(port.Name))
                        throw new NetlistException($"{where}: port {port.Name} is listed twice.");
                    if (port.Signals.Count == 0)
                        throw new NetlistException($"{where}: port {port.Name} carries no signal.");
                    for (int s = 0; s < port.Signals.Count; s++)
                    {
                        Net net = FindNet(b.Nets, port.Signals[s])
                            ?? throw new NetlistException($"{where}: port {port.Name} names signal {port.Signals[s].Net}, which is not in the block.");
                        if (!carried.Add(net))
                            throw new NetlistException($"{where}: signal {net.Name} is on two ports.");
                    }
                }
            }
            // no block may contain itself, directly or through others
            for (int i = 0; blocks != null && i < blocks.Count; i++)
                CheckNoLoop(blocks[i], byName, new List<string>());
        }

        private static void CheckInstances(string where, List<BlockInstance>? instances, List<Net> nets, Dictionary<string, BlockDefinition> blocks)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            for (int i = 0; instances != null && i < instances.Count; i++)
            {
                BlockInstance inst = instances[i];
                if (!names.Add(inst.Name))
                    throw new NetlistException($"{where}: instance {inst.Name} is listed twice.");
                if (!blocks.TryGetValue(inst.Block, out BlockDefinition? b))
                    throw new NetlistException($"{where}: instance {inst.Name} is of block {inst.Block}, which is not defined.");
                HashSet<string> done = new(StringComparer.Ordinal);
                for (int c = 0; inst.Connections != null && c < inst.Connections.Count; c++)
                {
                    PortConnection pc = inst.Connections[c];
                    BlockPort? port = null;
                    for (int p = 0; p < b.Ports.Count && port == null; p++)
                    {
                        if (b.Ports[p].Name == pc.Port)
                            port = b.Ports[p];
                    }
                    if (port == null)
                        throw new NetlistException($"{where}: instance {inst.Name} connects port {pc.Port}, which block {b.Name} does not have.");
                    if (!done.Add(pc.Port))
                        throw new NetlistException($"{where}: instance {inst.Name} connects port {pc.Port} twice.");
                    if (pc.Nets.Count != port.Signals.Count)
                        throw new NetlistException($"{where}: instance {inst.Name} port {pc.Port} is {port.Signals.Count} wide but is given {pc.Nets.Count} signals.");
                    for (int n = 0; n < pc.Nets.Count; n++)
                    {
                        if (FindNet(nets, pc.Nets[n]) == null)
                            throw new NetlistException($"{where}: instance {inst.Name} port {pc.Port} names signal {pc.Nets[n].Net}, which is not on that level.");
                    }
                }
            }
        }

        private static void CheckNoLoop(BlockDefinition b, Dictionary<string, BlockDefinition> blocks, List<string> path)
        {
            if (path.Contains(b.Name))
                throw new NetlistException("Block " + b.Name + " contains itself: " + string.Join(" > ", path) + " > " + b.Name + ".");
            path.Add(b.Name);
            for (int i = 0; b.Instances != null && i < b.Instances.Count; i++)
            {
                if (blocks.TryGetValue(b.Instances[i].Block, out BlockDefinition? inner))
                    CheckNoLoop(inner, blocks, path);
            }
            path.RemoveAt(path.Count - 1);
        }

        private static void CheckLevel(string where, List<Part> partList, List<Gate> gateList, List<Net> netList)
        {
            HashSet<string> parts = new(StringComparer.Ordinal);
            for (int i = 0; i < partList.Count; i++)
            {
                if (!parts.Add(partList[i].Ref))
                    throw new NetlistException($"Part {partList[i].Ref} is listed twice in {where}.");
            }

            HashSet<string> gateIds = new(StringComparer.Ordinal);
            for (int i = 0; i < gateList.Count; i++)
            {
                Gate gate = gateList[i];
                if (!gateIds.Add(gate.Id))
                    throw new NetlistException($"Gate {gate.Id} is listed twice.");
                if (!parts.Contains(gate.Part))
                    throw new NetlistException($"Gate {gate.Id} is in part {gate.Part}, which is not in the parts list.");
            }

            Dictionary<string, string> pinOwner = new(StringComparer.Ordinal);
            for (int i = 0; i < netList.Count; i++)
            {
                Net net = netList[i];
                if (net.PullUp != null && net.Drive != LineDrive.OpenCollector && net.Drive != LineDrive.OpenCollectorHighVoltage)
                    throw new NetlistException($"Net {net.Name} has a pull-up but its drive is not open-collector.");
                if (net.DriveConfidence != null && net.Drive == null)
                    throw new NetlistException($"Net {net.Name} says how sure its drive is, but has no drive.");
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    PinReference pin = net.Pins[j];
                    // P.A.32: the connector is P.A, the pin 32 (see ConnectorPins)
                    int dot = pin.At.LastIndexOf('.');
                    if (dot <= 0 || dot == pin.At.Length - 1 || !int.TryParse(pin.At.AsSpan(dot + 1), out _))
                        throw new NetlistException($"Net {net.Name}: pin \"{pin.At}\" is not <part>.<pin number>.");
                    string partRef = pin.PartRef;
                    if (!ConnectorPins.IsConnector(partRef) && !parts.Contains(partRef))
                        throw new NetlistException($"Net {net.Name}: pin {pin.At} names part {partRef}, which is not in the parts list.");
                    if (pinOwner.TryGetValue(pin.At, out string? other))
                        throw new NetlistException($"Pin {pin.At} is in two nets: {other} and {net.Name}.");
                    pinOwner.Add(pin.At, net.Name);
                }
            }
        }
    }

    /// <summary>
    /// A netlist file that is not valid.
    /// </summary>
    public sealed class NetlistException : Exception
    {
        /// <summary>
        /// Create with a message.
        /// </summary>
        /// <param name="message">
        /// What is wrong with the file.
        /// </param>
        public NetlistException(string message) : base(message) { }

        /// <summary>
        /// Create with a message and the cause.
        /// </summary>
        /// <param name="message">
        /// What is wrong with the file.
        /// </param>
        /// <param name="inner">
        /// The error that showed it.
        /// </param>
        public NetlistException(string message, Exception inner) : base(message, inner) { }
    }
}
