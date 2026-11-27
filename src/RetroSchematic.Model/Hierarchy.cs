//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Text;

namespace RetroSchematic.Model
{
    /// <summary>
    /// Working with blocks (format version 2, docs/FORMAT.md): a level of the design seen as a
    /// sheet of its own, the flattened netlist with hierarchical names, and a compare of two
    /// designs by what is connected.
    /// </summary>
    public static class Hierarchy
    {
        /// <summary>
        /// A block definition by name, or null.
        /// </summary>
        /// <param name="root">
        /// The whole design.
        /// </param>
        /// <param name="name">
        /// The block name.
        /// </param>
        /// <returns>
        /// The block, or null.
        /// </returns>
        public static BlockDefinition? FindBlock(Netlist root, string name)
        {
            for (int i = 0; root.Blocks != null && i < root.Blocks.Count; i++)
            {
                if (root.Blocks[i].Name == name)
                    return root.Blocks[i];
            }
            return null;
        }

        /// <summary>
        /// An instance on a level by name, or null.
        /// </summary>
        /// <param name="instances">
        /// The level's instances (may be null).
        /// </param>
        /// <param name="name">
        /// The instance name.
        /// </param>
        /// <returns>
        /// The instance, or null.
        /// </returns>
        public static BlockInstance? FindInstance(List<BlockInstance>? instances, string name)
        {
            for (int i = 0; instances != null && i < instances.Count; i++)
            {
                if (instances[i].Name == name)
                    return instances[i];
            }
            return null;
        }

        /// <summary>
        /// The inside of a block as a sheet of its own, so everything that works on a sheet
        /// (drawing, picking, editing) works on it. The lists are the block's own: a part added
        /// to the view is added to the block. The layout section and the instance list are
        /// copied back by <see cref="WriteBack"/>, because a view may replace them.
        /// </summary>
        /// <param name="root">
        /// The whole design (its sheet header and block list are shared).
        /// </param>
        /// <param name="block">
        /// The block.
        /// </param>
        /// <returns>
        /// The view.
        /// </returns>
        public static Netlist ViewOf(Netlist root, BlockDefinition block)
        {
            block.Instances ??= new List<BlockInstance>();
            return new Netlist
            {
                Format = root.Format,
                FormatVersion = root.FormatVersion,
                Sheet = root.Sheet,
                Parts = block.Parts,
                Gates = block.Gates,
                Nets = block.Nets,
                Layout = block.Layout,
                Instances = block.Instances,
                Blocks = root.Blocks,
            };
        }

        /// <summary>
        /// Copies a view's layout section and instance list back into its block (an empty
        /// instance list is not written).
        /// </summary>
        /// <param name="view">
        /// The view made by <see cref="ViewOf"/>.
        /// </param>
        /// <param name="block">
        /// The block it shows.
        /// </param>
        public static void WriteBack(Netlist view, BlockDefinition block)
        {
            block.Layout = view.Layout;
            block.Instances = view.Instances != null && view.Instances.Count > 0 ? view.Instances : null;
        }

        /// <summary>
        /// The design with every instance expanded, as one level (format version 1). Names get
        /// the instance path in front: part 3C inside U1 is U1/3C, its pin 9 is U1/3C.9, a
        /// signal that stays inside U1 is U1/NAME. A signal that leaves an instance through a
        /// port is the signal it is connected to above, and takes that name. Backplane (P.) and
        /// front (F.) pins keep their names at every depth: they are the card's own pins.
        /// Signals that end up with no pin at all are left out.
        /// </summary>
        /// <param name="root">
        /// The whole design.
        /// </param>
        /// <returns>
        /// The flattened netlist.
        /// </returns>
        public static Netlist Flatten(Netlist root)
        {
            Netlist flat = new() { Format = root.Format, FormatVersion = 1, Sheet = root.Sheet };
            List<Net> ids = new();
            Expand(root, root.Parts, root.Gates, root.Nets, root.Instances, "", new Dictionary<Net, int>(ReferenceEqualityComparer.Instance), flat, ids, null);
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i].Pins.Count > 0)
                    flat.Nets.Add(ids[i]);
            }
            return flat;
        }

        /// <summary>
        /// What a design connects, as text that does not depend on how it is split into blocks:
        /// every part, gate and flattened signal with the instance path taken off every name,
        /// one per line, sorted. Two designs with the same text connect exactly the same pins.
        /// </summary>
        /// <param name="root">
        /// The design.
        /// </param>
        /// <returns>
        /// The text.
        /// </returns>
        public static string Connections(Netlist root)
        {
            Netlist flat = Flatten(root);
            List<string> lines = new();
            for (int i = 0; i < flat.Parts.Count; i++)
                lines.Add("part " + Leaf(flat.Parts[i].Ref) + " " + flat.Parts[i].Type);
            for (int i = 0; i < flat.Gates.Count; i++)
            {
                Gate g = flat.Gates[i];
                StringBuilder sb = new();
                sb.Append("gate ").Append(Leaf(g.Id)).Append(' ').Append(g.Kind).Append(" out ").Append(g.Output).Append(" in");
                for (int k = 0; k < g.Inputs.Count; k++)
                    sb.Append(' ').Append(g.Inputs[k]);
                lines.Add(sb.ToString());
            }
            for (int i = 0; i < flat.Nets.Count; i++)
            {
                Net n = flat.Nets[i];
                List<string> pins = new();
                for (int j = 0; j < n.Pins.Count; j++)
                    pins.Add(Leaf(n.Pins[j].At) + "(" + n.Pins[j].Confidence + ")");
                pins.Sort(StringComparer.Ordinal);
                lines.Add("net " + Leaf(n.Name) + " " + n.Active + " " + n.Drive + " " + n.PullUp + ": " + string.Join(" ", pins));
            }
            lines.Sort(StringComparer.Ordinal);
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Every level a signal reaches through block ports, as the signal's name on that level
        /// with the instance path in front (U2/BD3); the level asked about comes first. This is
        /// how a trace follows a signal up and down through the blocks.
        /// </summary>
        /// <param name="root">
        /// The whole design.
        /// </param>
        /// <param name="path">
        /// Instance names from the top down to the level the signal is on; empty for the top.
        /// </param>
        /// <param name="net">
        /// The signal on that level.
        /// </param>
        /// <returns>
        /// The names, or an empty list when the signal is not found.
        /// </returns>
        public static List<string> Reach(Netlist root, IReadOnlyList<string> path, Net net)
        {
            Netlist flat = new();
            List<Net> ids = new();
            List<Place> places = new();
            Expand(root, root.Parts, root.Gates, root.Nets, root.Instances, "", new Dictionary<Net, int>(ReferenceEqualityComparer.Instance), flat, ids, places);
            string prefix = path.Count == 0 ? "" : string.Join("/", path) + "/";
            int id = -1;
            for (int i = 0; i < places.Count && id < 0; i++)
            {
                if (places[i].Net == net && places[i].Prefix == prefix)
                    id = places[i].Id;
            }
            List<string> list = new();
            if (id < 0)
                return list;
            list.Add(prefix + net.Name);
            for (int i = 0; i < places.Count; i++)
            {
                if (places[i].Id == id && places[i].Net != net)
                    list.Add(places[i].Prefix + places[i].Net.Name);
            }
            return list;
        }

        /// <summary>
        /// A name with its instance path taken off (U1/U2/3C.9 is 3C.9).
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The last part of it.
        /// </returns>
        public static string Leaf(string name)
        {
            int slash = name.LastIndexOf('/');
            return slash < 0 ? name : name.Substring(slash + 1);
        }

        private static void Expand(Netlist root, List<Part> parts, List<Gate> gates, List<Net> nets, List<BlockInstance>? instances,
            string prefix, Dictionary<Net, int> bound, Netlist flat, List<Net> ids, List<Place>? places)
        {
            // every signal on this level: the one it is bound to above, or a new one
            Dictionary<Net, int> id = new(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < nets.Count; i++)
            {
                Net n = nets[i];
                if (!bound.TryGetValue(n, out int k))
                {
                    k = ids.Count;
                    ids.Add(new Net
                    {
                        Name = prefix + n.Name, Active = n.Active, Drive = n.Drive, DriveConfidence = n.DriveConfidence,
                        PullUp = n.PullUp, Note = n.Note, Printed = n.Printed,
                    });
                }
                id[n] = k;
                places?.Add(new Place(prefix, n, k));
                for (int j = 0; j < n.Pins.Count; j++)
                {
                    PinReference p = n.Pins[j];
                    string part = p.PartRef;
                    string at = ConnectorPins.IsConnector(part) ? p.At : prefix + p.At;
                    ids[k].Pins.Add(new PinReference { At = at, Confidence = p.Confidence, Note = p.Note });
                }
            }
            for (int i = 0; i < parts.Count; i++)
            {
                Part p = parts[i];
                flat.Parts.Add(new Part { Ref = prefix + p.Ref, Type = p.Type, Function = p.Function, Confidence = p.Confidence, Note = p.Note });
            }
            for (int i = 0; i < gates.Count; i++)
            {
                Gate g = gates[i];
                flat.Gates.Add(new Gate
                {
                    Id = prefix + g.Id, Part = prefix + g.Part, Kind = g.Kind, Inputs = new List<int>(g.Inputs), Output = g.Output,
                    Confidence = g.Confidence, Note = g.Note,
                });
            }
            for (int i = 0; instances != null && i < instances.Count; i++)
            {
                BlockInstance inst = instances[i];
                BlockDefinition b = FindBlock(root, inst.Block) ?? throw new NetlistException("Instance " + inst.Name + " is of block " + inst.Block + ", which is not defined.");
                Dictionary<Net, int> inner = new(ReferenceEqualityComparer.Instance);
                for (int c = 0; inst.Connections != null && c < inst.Connections.Count; c++)
                {
                    PortConnection pc = inst.Connections[c];
                    BlockPort? port = null;
                    for (int q = 0; q < b.Ports.Count && port == null; q++)
                    {
                        if (b.Ports[q].Name == pc.Port)
                            port = b.Ports[q];
                    }
                    for (int bit = 0; port != null && bit < port.Signals.Count && bit < pc.Nets.Count; bit++)
                    {
                        Net? inside = NetlistReader.FindNet(b.Nets, port.Signals[bit]);
                        Net? outside = NetlistReader.FindNet(nets, pc.Nets[bit]);
                        if (inside != null && outside != null)
                            inner[inside] = id[outside];
                    }
                }
                Expand(root, b.Parts, b.Gates, b.Nets, b.Instances, prefix + inst.Name + "/", inner, flat, ids, places);
            }
        }
    
        /// <summary>
        /// One signal on one level and the flattened signal it is part of.
        /// </summary>
        private sealed record Place(string Prefix, Net Net, int Id);
}
}
