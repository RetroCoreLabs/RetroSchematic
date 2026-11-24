//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// A port that <see cref="SheetEditor.PlanBlock"/> would make: the user may change the name
    /// before <see cref="SheetEditor.MakeBlock"/>.
    /// </summary>
    public sealed class PlannedPort
    {
        /// <summary>
        /// The port name; may be changed before the block is made.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Into, out of, or both ways, seen from inside the new block.
        /// </summary>
        public PortDirection Direction { get; set; }

        /// <summary>
        /// The signals that cross the edge through this port, lowest bit first.
        /// </summary>
        public List<SignalRef> Signals { get; } = new();

        /// <summary>
        /// The port as one line for a list: name, direction, width and signals.
        /// </summary>
        /// <returns>
        /// The text.
        /// </returns>
        public override string ToString()
        {
            string what = Signals.Count == 1 ? Signals[0].Net : Signals.Count.ToString(CultureInfo.InvariantCulture) + " signals, " + Signals[0].Net + " ... " + Signals[^1].Net;
            return Name + "  " + Direction.ToString().ToLowerInvariant() + "  (" + what + ")";
        }
    }

    /// <summary>
    /// Blocks: look inside an instance and come back, make a block from a selection, dissolve an
    /// instance into its level, place a port on its instance box. Every one of these keeps the
    /// flattened netlist (<see cref="Hierarchy.Connections"/>) exactly as it was.
    /// </summary>
    public sealed partial class SheetEditor
    {
        private readonly List<string> _path = new();

        /// <summary>
        /// The instance names from the top level down to the level shown; empty at the top.
        /// </summary>
        public IReadOnlyList<string> LevelPath => _path;

        /// <summary>
        /// Shows the inside of an instance on the current level. Not a change: allowed read-only.
        /// </summary>
        /// <param name="instance">
        /// The instance name.
        /// </param>
        public void EnterInstance(string instance)
        {
            BlockInstance inst = Hierarchy.FindInstance(Netlist.Instances, instance)
                ?? throw new SheetEditException($"There is no instance {instance} on this level.");
            if (Hierarchy.FindBlock(Root, inst.Block) == null)
                throw new SheetEditException($"Instance {instance} is of block {inst.Block}, which is not defined.");
            SyncBack();
            _path.Add(instance);
            ShowLevel();
            Changed?.Invoke(this);
        }

        /// <summary>
        /// Goes back up one level (nothing at the top).
        /// </summary>
        public void LeaveInstance()
        {
            if (_path.Count == 0)
                return;
            SyncBack();
            _path.RemoveAt(_path.Count - 1);
            ShowLevel();
            Changed?.Invoke(this);
        }

        /// <summary>
        /// The block shown, or null at the top level.
        /// </summary>
        /// <returns>
        /// The block.
        /// </returns>
        public BlockDefinition? CurrentBlock()
        {
            List<BlockInstance>? instances = Root.Instances;
            BlockDefinition? block = null;
            for (int i = 0; i < _path.Count; i++)
            {
                BlockInstance? inst = Hierarchy.FindInstance(instances, _path[i]);
                block = inst == null ? null : Hierarchy.FindBlock(Root, inst.Block);
                if (block == null)
                    return null;
                instances = block.Instances;
            }
            return block;
        }

        /// <summary>
        /// The ports <see cref="MakeBlock"/> would make for a selection: one for every signal
        /// that has pins both inside and outside it (a backplane or front pin counts as
        /// outside). Signals whose names differ only in a number (BD0 ... BD15) and that share
        /// level and direction are put together in one bus port. A port's direction is output
        /// when only the inside drives the signal, input when only the outside does, and inout
        /// when both may.
        /// </summary>
        /// <param name="members">
        /// Part references and instance names on the current level.
        /// </param>
        /// <returns>
        /// The ports, in sheet order of their first signal.
        /// </returns>
        public List<PlannedPort> PlanBlock(List<string> members)
        {
            Split(members, out HashSet<string> parts, out HashSet<string> instances);
            List<Net> crossing = new();
            List<PortDirection> directions = new();
            for (int i = 0; i < Netlist.Nets.Count; i++)
            {
                Net n = Netlist.Nets[i];
                Classify(n, parts, instances, out int inside, out int outside, out bool insideDrives, out bool outsideDrives);
                if (inside == 0 || outside == 0)
                    continue;
                crossing.Add(n);
                directions.Add(insideDrives && outsideDrives ? PortDirection.Inout : insideDrives ? PortDirection.Output : PortDirection.Input);
            }

            // group NAME<number> signals of one level and direction into bus ports
            List<PlannedPort> ports = new();
            bool[] used = new bool[crossing.Count];
            for (int i = 0; i < crossing.Count; i++)
            {
                if (used[i])
                    continue;
                if (!Numbered(crossing[i].Name, out string prefix, out _))
                    continue;
                List<int> group = new();
                for (int j = i; j < crossing.Count; j++)
                {
                    if (!used[j] && Numbered(crossing[j].Name, out string p2, out _) && p2 == prefix
                        && crossing[j].Active == crossing[i].Active && directions[j] == directions[i])
                        group.Add(j);
                }
                if (group.Count < 2)
                    continue;
                group.Sort((a, b) =>
                {
                    Numbered(crossing[a].Name, out _, out int na);
                    Numbered(crossing[b].Name, out _, out int nb);
                    return na.CompareTo(nb);
                });
                string level = crossing[i].Active == ActiveLevel.None ? "" : crossing[i].Active == ActiveLevel.High ? "_1" : "_0";
                PlannedPort bus = new() { Name = prefix + level, Direction = directions[i] };
                for (int k = 0; k < group.Count; k++)
                {
                    used[group[k]] = true;
                    bus.Signals.Add(Ref(crossing[group[k]]));
                }
                ports.Add(bus);
            }
            for (int i = 0; i < crossing.Count; i++)
            {
                if (used[i])
                    continue;
                // the key (with _0/_1) only when two signals on this level share the name
                string name = crossing[i].Name;
                for (int j = 0; j < Netlist.Nets.Count; j++)
                {
                    if (Netlist.Nets[j] != crossing[i] && Netlist.Nets[j].Name == name)
                        name = SheetLayout.NetKey(crossing[i]);
                }
                PlannedPort single = new() { Name = name, Direction = directions[i] };
                single.Signals.Add(Ref(crossing[i]));
                ports.Add(single);
            }
            // port names must be unique in the block
            HashSet<string> names = new(StringComparer.Ordinal);
            for (int i = 0; i < ports.Count; i++)
            {
                string baseName = ports[i].Name;
                for (int k = 2; !names.Add(ports[i].Name); k++)
                    ports[i].Name = baseName + "_" + k.ToString(CultureInfo.InvariantCulture);
            }
            return ports;
        }

        /// <summary>
        /// Moves a selection on the current level into a new block, and puts one instance of the
        /// block where the selection was. Signals wholly inside move with it; signals that cross
        /// the edge are split: the inside part keeps the name inside the block and is carried
        /// by a port, the outside part keeps its name here and is connected to that port. The
        /// flattened netlist does not change.
        /// </summary>
        /// <param name="blockName">
        /// The new block's name.
        /// </param>
        /// <param name="instanceName">
        /// The instance's name, or null for the first free U1, U2, ...
        /// </param>
        /// <param name="members">
        /// Part references and instance names on the current level.
        /// </param>
        /// <param name="ports">
        /// The ports, as from <see cref="PlanBlock"/> with names possibly changed; null makes
        /// them as planned.
        /// </param>
        /// <returns>
        /// The instance's name.
        /// </returns>
        public string MakeBlock(string blockName, string? instanceName, List<string> members, List<PlannedPort>? ports)
        {
            CheckWritable();
            if (blockName.Trim().Length == 0)
                throw new SheetEditException("A block needs a name.");
            if (Hierarchy.FindBlock(Root, blockName) != null)
                throw new SheetEditException($"There is already a block {blockName}.");
            if (members.Count == 0)
                throw new SheetEditException("Pick the parts (and instances) to put in the block first.");
            Split(members, out HashSet<string> parts, out HashSet<string> instances);
            foreach (string r in parts)
            {
                if (FindPart(r) == null)
                    throw new SheetEditException($"There is no part {r} on this level.");
            }
            foreach (string r in instances)
            {
                if (Hierarchy.FindInstance(Netlist.Instances, r) == null)
                    throw new SheetEditException($"There is no instance {r} on this level.");
            }
            instanceName ??= FreeInstanceName();
            if (Hierarchy.FindInstance(Netlist.Instances, instanceName) != null)
                throw new SheetEditException($"There is already an instance {instanceName} on this level.");
            ports ??= PlanBlock(members);
            List<PlannedPort> planned = PlanBlock(members);
            if (ports.Count != planned.Count)
                throw new SheetEditException("The ports do not match the selection any more; plan them again.");
            HashSet<string> portNames = new(StringComparer.Ordinal);
            for (int i = 0; i < ports.Count; i++)
            {
                if (ports[i].Name.Trim().Length == 0)
                    throw new SheetEditException("Every port needs a name.");
                if (!portNames.Add(ports[i].Name))
                    throw new SheetEditException($"Two ports are called {ports[i].Name}.");
            }
            Point? anchor = Anchor(parts);

            Snapshot();
            BlockDefinition b = new() { Name = blockName };
            // parts and their gates
            for (int i = Netlist.Parts.Count - 1; i >= 0; i--)
            {
                if (parts.Contains(Netlist.Parts[i].Ref))
                {
                    b.Parts.Insert(0, Netlist.Parts[i]);
                    Netlist.Parts.RemoveAt(i);
                }
            }
            for (int i = Netlist.Gates.Count - 1; i >= 0; i--)
            {
                if (parts.Contains(Netlist.Gates[i].Part))
                {
                    b.Gates.Insert(0, Netlist.Gates[i]);
                    Netlist.Gates.RemoveAt(i);
                }
            }
            // signals: wholly inside move, crossing ones split
            Dictionary<Net, Net> outerOf = new(ReferenceEqualityComparer.Instance);
            for (int i = Netlist.Nets.Count - 1; i >= 0; i--)
            {
                Net n = Netlist.Nets[i];
                Classify(n, parts, instances, out int inside, out int outside, out _, out _);
                if (inside == 0)
                    continue;
                if (outside == 0)
                {
                    b.Nets.Insert(0, n);
                    Netlist.Nets.RemoveAt(i);
                    MoveWirePath(n, b);
                    continue;
                }
                Net inner = new() { Name = n.Name, Active = n.Active, Drive = n.Drive, DriveConfidence = n.DriveConfidence, PullUp = n.PullUp, Note = n.Note };
                for (int j = n.Pins.Count - 1; j >= 0; j--)
                {
                    if (parts.Contains(n.Pins[j].PartRef))
                    {
                        inner.Pins.Insert(0, n.Pins[j]);
                        n.Pins.RemoveAt(j);
                    }
                }
                // its old fixed path joined pins that are now on two levels
                RemoveWirePath(n.Name, n.Active);
                b.Nets.Insert(0, inner);
                outerOf[inner] = n;
            }
            // instances inside the selection move into the block
            for (int i = Netlist.Instances == null ? -1 : Netlist.Instances.Count - 1; i >= 0; i--)
            {
                if (instances.Contains(Netlist.Instances![i].Name))
                {
                    b.Instances ??= new List<BlockInstance>();
                    b.Instances.Insert(0, Netlist.Instances[i]);
                    Netlist.Instances.RemoveAt(i);
                }
            }
            // ports and the new instance's connections
            BlockInstance inst = new() { Name = instanceName, Block = blockName, Position = anchor, Connections = new List<PortConnection>() };
            for (int i = 0; i < ports.Count; i++)
            {
                BlockPort port = new() { Name = ports[i].Name, Direction = ports[i].Direction };
                PortConnection pc = new() { Port = port.Name };
                for (int k = 0; k < ports[i].Signals.Count; k++)
                {
                    Net inner = NetlistReader.FindNet(b.Nets, ports[i].Signals[k])
                        ?? throw new SheetEditException($"Port {port.Name}: signal {ports[i].Signals[k].Net} is not in the selection.");
                    port.Signals.Add(Ref(inner));
                    pc.Nets.Add(Ref(outerOf[inner]));
                }
                b.Ports.Add(port);
                inst.Connections.Add(pc);
            }
            Root.Blocks ??= new List<BlockDefinition>();
            Root.Blocks.Add(b);
            Netlist.Instances ??= new List<BlockInstance>();
            Netlist.Instances.Add(inst);
            if (_path.Count == 0)
                Root.Instances = Netlist.Instances;
            Done();
            return instanceName;
        }

        /// <summary>
        /// Puts the inside of an instance back on the current level and removes the instance (and
        /// its block, when no other instance uses it). Signals carried by a connected port join
        /// the signal they were connected to; the others come up with their own names (with the
        /// instance name in front when the name is already taken here, as are parts and
        /// instances whose names are taken). The flattened netlist does not change.
        /// </summary>
        /// <param name="instance">
        /// The instance name.
        /// </param>
        public void DissolveInstance(string instance)
        {
            CheckWritable();
            BlockInstance inst = Hierarchy.FindInstance(Netlist.Instances, instance)
                ?? throw new SheetEditException($"There is no instance {instance} on this level.");
            BlockDefinition original = Hierarchy.FindBlock(Root, inst.Block)
                ?? throw new SheetEditException($"Instance {instance} is of block {inst.Block}, which is not defined.");
            Snapshot();
            BlockDefinition b = NetlistReader.Clone(original);

            // parts keep their references unless taken here
            Dictionary<string, string> rename = new(StringComparer.Ordinal);
            Point? anchor = AnchorOf(b.Parts, b.Gates);
            double dx = 0, dy = 0;
            if (anchor != null && inst.Position != null)
            {
                dx = inst.Position.X - anchor.X;
                dy = inst.Position.Y - anchor.Y;
            }
            for (int i = 0; i < b.Parts.Count; i++)
            {
                Part p = b.Parts[i];
                if (FindPart(p.Ref) != null)
                {
                    rename[p.Ref] = instance + "-" + p.Ref;
                    p.Ref = rename[p.Ref];
                }
                if (p.Position != null)
                    p.Position = new Point { X = p.Position.X + dx, Y = p.Position.Y + dy };
                Netlist.Parts.Add(p);
            }
            for (int i = 0; i < b.Gates.Count; i++)
            {
                Gate g = b.Gates[i];
                if (rename.TryGetValue(g.Part, out string? to))
                {
                    g.Id = to + g.Id.Substring(g.Part.Length);
                    g.Part = to;
                }
                if (g.Position != null)
                    g.Position = new Point { X = g.Position.X + dx, Y = g.Position.Y + dy };
                Netlist.Gates.Add(g);
            }
            // which inside signal each connected port bit joins here
            Dictionary<Net, Net> joins = new(ReferenceEqualityComparer.Instance);
            for (int c = 0; inst.Connections != null && c < inst.Connections.Count; c++)
            {
                PortConnection pc = inst.Connections[c];
                for (int q = 0; q < b.Ports.Count; q++)
                {
                    if (b.Ports[q].Name != pc.Port)
                        continue;
                    for (int bit = 0; bit < b.Ports[q].Signals.Count; bit++)
                    {
                        Net? inside = NetlistReader.FindNet(b.Nets, b.Ports[q].Signals[bit]);
                        Net? outside = NetlistReader.FindNet(Netlist.Nets, pc.Nets[bit]);
                        if (inside != null && outside != null)
                            joins[inside] = outside;
                    }
                }
            }
            Dictionary<string, string> netRename = new(StringComparer.Ordinal);
            for (int i = 0; i < b.Nets.Count; i++)
            {
                Net n = b.Nets[i];
                for (int j = 0; j < n.Pins.Count; j++)
                {
                    string part = n.Pins[j].PartRef;
                    if (rename.TryGetValue(part, out string? to))
                        n.Pins[j].At = to + n.Pins[j].At.Substring(part.Length);
                }
                if (joins.TryGetValue(n, out Net? outside))
                {
                    outside.Pins.AddRange(n.Pins);
                    continue;
                }
                if (FindNet(n.Name, n.Active) != null)
                {
                    netRename[SheetLayout.NetKey(n)] = instance + "/" + n.Name;
                    n.Name = instance + "/" + n.Name;
                }
                Netlist.Nets.Add(n);
            }
            // inner instances come up too; their connections name inside signals
            for (int i = 0; b.Instances != null && i < b.Instances.Count; i++)
            {
                BlockInstance inner = b.Instances[i];
                if (Hierarchy.FindInstance(Netlist.Instances, inner.Name) != null)
                    inner.Name = instance + "-" + inner.Name;
                for (int c = 0; inner.Connections != null && c < inner.Connections.Count; c++)
                {
                    List<SignalRef> nets = inner.Connections[c].Nets;
                    for (int k = 0; k < nets.Count; k++)
                    {
                        Net? inside = NetlistReader.FindNet(b.Nets, nets[k]);
                        if (inside == null)
                            continue;
                        nets[k] = joins.TryGetValue(inside, out Net? outside) ? Ref(outside) : Ref(inside);
                    }
                }
                Netlist.Instances!.Add(inner);
            }
            // the inside's fixed wire paths, for signals that came up whole
            for (int i = 0; b.Layout != null && i < b.Layout.Wires.Count; i++)
            {
                WirePath w = b.Layout.Wires[i];
                Net? n = NetlistReader.FindNet(Netlist.Nets, new SignalRef { Net = w.Net, Active = w.Active });
                if (n == null || joins.ContainsValue(n))
                    continue;
                Netlist.Layout ??= new SheetLayoutSection();
                Netlist.Layout.Wires.Add(w);
            }
            Netlist.Instances!.Remove(inst);
            if (Netlist.Instances.Count == 0 && _path.Count == 0)
                Root.Instances = null;
            if (!Used(original.Name))
            {
                Root.Blocks!.Remove(original);
                if (Root.Blocks.Count == 0)
                    Root.Blocks = null;
            }
            Done();
        }

        /// <summary>
        /// Moves chips (and instances) on the current level into an instance that is there too:
        /// the instance's block takes them in and its ports are made again for the new edge. It
        /// is done as a dissolve followed by a make block, which both keep the flattened netlist,
        /// so the connections do not change. Port names, edges and places are kept for every
        /// port that carries the same signals as before, and the box stays where it was. One
        /// undo step. Refused when other instances use the same block, because they would
        /// change too.
        /// </summary>
        /// <param name="members">
        /// Part references and inst:NAME entries on the current level.
        /// </param>
        /// <param name="instance">
        /// The instance that takes them in.
        /// </param>
        public void MoveIntoInstance(List<string> members, string instance)
        {
            CheckWritable();
            BlockInstance inst = Hierarchy.FindInstance(Netlist.Instances, instance)
                ?? throw new SheetEditException($"There is no instance {instance} on this level.");
            if (members.Count == 0)
                throw new SheetEditException("Pick the parts to move into " + instance + " first.");
            if (members.Contains("inst:" + instance))
                throw new SheetEditException("An instance cannot be moved into itself.");
            BlockDefinition b = Hierarchy.FindBlock(Root, inst.Block)
                ?? throw new SheetEditException($"Instance {instance} is of block {inst.Block}, which is not defined.");
            int users = 0;
            CountUsers(Root.Instances, b.Name, ref users);
            for (int i = 0; Root.Blocks != null && i < Root.Blocks.Count; i++)
                CountUsers(Root.Blocks[i].Instances, b.Name, ref users);
            if (users > 1)
                throw new SheetEditException($"Block {b.Name} has {users} instances; moving parts into it would change all of them.");

            // what to keep: each port's look, by the signals it carries
            Dictionary<string, BlockPort> oldPorts = new(StringComparer.Ordinal);
            for (int i = 0; i < b.Ports.Count; i++)
                oldPorts[SignalsKey(b.Ports[i].Signals)] = b.Ports[i];
            string blockName = b.Name;
            Point? position = inst.Position;
            HashSet<string> partsBefore = new(StringComparer.Ordinal);
            for (int i = 0; i < Netlist.Parts.Count; i++)
                partsBefore.Add(Netlist.Parts[i].Ref);
            HashSet<string> instancesBefore = new(StringComparer.Ordinal);
            for (int i = 0; Netlist.Instances != null && i < Netlist.Instances.Count; i++)
                instancesBefore.Add(Netlist.Instances[i].Name);

            int undoBefore = _undo.Count;
            try
            {
                DissolveInstance(instance);
                // everything that came up out of the instance goes back in, with the new members
                List<string> all = new(members);
                for (int i = 0; i < Netlist.Parts.Count; i++)
                {
                    if (!partsBefore.Contains(Netlist.Parts[i].Ref))
                        all.Add(Netlist.Parts[i].Ref);
                }
                for (int i = 0; Netlist.Instances != null && i < Netlist.Instances.Count; i++)
                {
                    if (!instancesBefore.Contains(Netlist.Instances[i].Name))
                        all.Add("inst:" + Netlist.Instances[i].Name);
                }
                List<PlannedPort> ports = PlanBlock(all);
                for (int i = 0; i < ports.Count; i++)
                {
                    if (oldPorts.TryGetValue(SignalsKey(ports[i].Signals), out BlockPort? old))
                        ports[i].Name = old.Name;
                }
                // two old names may now meet on different ports: keep the names unique
                HashSet<string> names = new(StringComparer.Ordinal);
                for (int i = 0; i < ports.Count; i++)
                {
                    string baseName = ports[i].Name;
                    for (int k = 2; !names.Add(ports[i].Name); k++)
                        ports[i].Name = baseName + "_" + k.ToString(CultureInfo.InvariantCulture);
                }
                MakeBlock(blockName, instance, all, ports);
                BlockDefinition made = Hierarchy.FindBlock(Root, blockName)!;
                for (int i = 0; i < made.Ports.Count; i++)
                {
                    if (oldPorts.TryGetValue(SignalsKey(made.Ports[i].Signals), out BlockPort? old))
                    {
                        made.Ports[i].Side = old.Side;
                        made.Ports[i].Order = old.Order;
                    }
                }
                Hierarchy.FindInstance(Netlist.Instances, instance)!.Position = position;
            }
            catch (SheetEditException)
            {
                // put everything back as it was before the dissolve
                if (_undo.Count > undoBefore)
                {
                    Root = NetlistReader.Parse(_undo[undoBefore]);
                    _undo.RemoveRange(undoBefore, _undo.Count - undoBefore);
                    ShowLevel();
                    Changed?.Invoke(this);
                }
                throw;
            }
            // one undo step for the whole move
            if (_undo.Count > undoBefore + 1)
                _undo.RemoveRange(undoBefore + 1, _undo.Count - undoBefore - 1);
            Done();
        }

        /// <summary>
        /// Places a port pin on an edge of its block's instance boxes. Drawing only.
        /// </summary>
        /// <param name="block">
        /// The block.
        /// </param>
        /// <param name="port">
        /// The port name.
        /// </param>
        /// <param name="side">
        /// The edge.
        /// </param>
        /// <param name="order">
        /// Its place along the edge, 0 first; null for port order.
        /// </param>
        public void SetPortPosition(string block, string port, PortSide side, int? order)
        {
            CheckWritable();
            BlockDefinition b = Hierarchy.FindBlock(Root, block) ?? throw new SheetEditException($"There is no block {block}.");
            BlockPort? p = null;
            for (int i = 0; i < b.Ports.Count && p == null; i++)
            {
                if (b.Ports[i].Name == port)
                    p = b.Ports[i];
            }
            if (p == null)
                throw new SheetEditException($"Block {block} has no port {port}.");
            if (order < 0)
                throw new SheetEditException("A port's place along the edge is 0 or more.");
            Snapshot();
            p.Side = side;
            p.Order = order;
            Done();
        }

        /// <summary>
        /// Moves a port one or more places along its edge, swapping it with its neighbour. Every
        /// port on that edge then has a fixed place (its order), in the order they are drawn.
        /// Drawing only.
        /// </summary>
        /// <param name="block">
        /// The block.
        /// </param>
        /// <param name="port">
        /// The port name.
        /// </param>
        /// <param name="step">
        /// -1 for one place earlier (up or left), 1 for one place later.
        /// </param>
        public void MovePortAlong(string block, string port, int step)
        {
            CheckWritable();
            BlockDefinition b = Hierarchy.FindBlock(Root, block) ?? throw new SheetEditException($"There is no block {block}.");
            BlockPort? p = PortOf(b, port) ?? throw new SheetEditException($"Block {block} has no port {port}.");
            PortSide side = SheetLayout.SideOf(p);
            // the ports on that edge in drawing order: their own place first, then port order
            List<BlockPort> edge = new();
            for (int i = 0; i < b.Ports.Count; i++)
            {
                if (SheetLayout.SideOf(b.Ports[i]) == side)
                    edge.Add(b.Ports[i]);
            }
            List<BlockPort> sorted = new(edge);
            sorted.Sort((x, y) =>
            {
                int c = (x.Order ?? int.MaxValue).CompareTo(y.Order ?? int.MaxValue);
                return c != 0 ? c : edge.IndexOf(x).CompareTo(edge.IndexOf(y));
            });
            int at = sorted.IndexOf(p);
            int to = at + step;
            if (to < 0 || to >= sorted.Count)
                throw new SheetEditException($"Port {port} is already the {(step < 0 ? "first" : "last")} on its edge.");
            Snapshot();
            sorted[at] = sorted[to];
            sorted[to] = p;
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].Side = side;
                sorted[i].Order = i;
            }
            Done();
        }

        /// <summary>
        /// Moves an instance box. Drawing only.
        /// </summary>
        /// <param name="instance">
        /// The instance on the current level.
        /// </param>
        /// <param name="xMm">
        /// New left edge in mm.
        /// </param>
        /// <param name="yMm">
        /// New top edge in mm.
        /// </param>
        public void MoveInstance(string instance, double xMm, double yMm)
        {
            CheckWritable();
            BlockInstance inst = Hierarchy.FindInstance(Netlist.Instances, instance)
                ?? throw new SheetEditException($"There is no instance {instance} on this level.");
            Snapshot();
            inst.Position = new Point { X = xMm, Y = yMm };
            Done();
        }

        /// <summary>
        /// Copies the view's layout and instances back into its block, so a snapshot or a save
        /// holds them.
        /// </summary>
        private void SyncBack()
        {
            BlockDefinition? b = CurrentBlock();
            if (b != null)
                Hierarchy.WriteBack(Netlist, b);
            else if (_path.Count == 0 && Netlist.Instances != null && Netlist.Instances.Count == 0)
                Netlist.Instances = null;
            // a part placed or brought back to the top level goes on the page shown; a part
            // moved into a block loses its page (format version 3)
            CardPages.Normalize(Root, Page);
        }

        /// <summary>
        /// Makes <see cref="Netlist"/> the level <see cref="LevelPath"/> names; a path that no
        /// longer exists (after an undo) goes back up as far as it still does.
        /// </summary>
        private void ShowLevel()
        {
            Netlist = Root;
            List<BlockInstance>? instances = Root.Instances;
            for (int i = 0; i < _path.Count; i++)
            {
                BlockInstance? inst = Hierarchy.FindInstance(instances, _path[i]);
                BlockDefinition? b = inst == null ? null : Hierarchy.FindBlock(Root, inst.Block);
                if (b == null)
                {
                    _path.RemoveRange(i, _path.Count - i);
                    break;
                }
                Netlist = Hierarchy.ViewOf(Root, b);
                instances = b.Instances;
            }
        }

        private bool Used(string block)
        {
            if (Uses(Root.Instances, block))
                return true;
            for (int i = 0; Root.Blocks != null && i < Root.Blocks.Count; i++)
            {
                if (Uses(Root.Blocks[i].Instances, block))
                    return true;
            }
            return false;
        }

        private static void CountUsers(List<BlockInstance>? instances, string block, ref int users)
        {
            for (int i = 0; instances != null && i < instances.Count; i++)
            {
                if (instances[i].Block == block)
                    users++;
            }
        }

        /// <summary>
        /// The signals a port carries as one text (names and levels in bit order), to find the
        /// same port again after it is made anew.
        /// </summary>
        private static string SignalsKey(List<SignalRef> signals)
        {
            StringBuilder sb = new();
            for (int i = 0; i < signals.Count; i++)
                sb.Append(signals[i].Net).Append('|').Append(signals[i].Active?.ToString() ?? "").Append(';');
            return sb.ToString();
        }

        private static bool Uses(List<BlockInstance>? instances, string block)
        {
            for (int i = 0; instances != null && i < instances.Count; i++)
            {
                if (instances[i].Block == block)
                    return true;
            }
            return false;
        }

        private static void Split(List<string> members, out HashSet<string> parts, out HashSet<string> instances)
        {
            parts = new HashSet<string>(StringComparer.Ordinal);
            instances = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < members.Count; i++)
            {
                string m = members[i];
                if (m.StartsWith("inst:", StringComparison.Ordinal))
                    instances.Add(m.Substring(5));
                else if (m.StartsWith("part:", StringComparison.Ordinal))
                    parts.Add(m.Substring(5));
                else
                    parts.Add(m);
            }
        }

        /// <summary>
        /// Counts a signal's ends inside and outside a selection, and whether each side may
        /// drive it. A backplane or front pin is outside and may drive.
        /// </summary>
        private void Classify(Net n, HashSet<string> parts, HashSet<string> instances, out int inside, out int outside,
            out bool insideDrives, out bool outsideDrives)
        {
            inside = 0;
            outside = 0;
            insideDrives = false;
            outsideDrives = false;
            for (int j = 0; j < n.Pins.Count; j++)
            {
                string part = n.Pins[j].PartRef;
                bool drives = ConnectorPins.IsConnector(part) || IsOutput(part, n.Pins[j].Pin);
                if (parts.Contains(part))
                {
                    inside++;
                    insideDrives |= drives;
                }
                else
                {
                    outside++;
                    outsideDrives |= drives;
                }
            }
            for (int i = 0; Netlist.Instances != null && i < Netlist.Instances.Count; i++)
            {
                BlockInstance inst = Netlist.Instances[i];
                BlockDefinition? b = Hierarchy.FindBlock(Root, inst.Block);
                for (int c = 0; b != null && inst.Connections != null && c < inst.Connections.Count; c++)
                {
                    PortConnection pc = inst.Connections[c];
                    for (int k = 0; k < pc.Nets.Count; k++)
                    {
                        if (NetlistReader.FindNet(Netlist.Nets, pc.Nets[k]) != n)
                            continue;
                        bool drives = PortOf(b, pc.Port)?.Direction != PortDirection.Input;
                        if (instances.Contains(inst.Name))
                        {
                            inside++;
                            insideDrives |= drives;
                        }
                        else
                        {
                            outside++;
                            outsideDrives |= drives;
                        }
                    }
                }
            }
        }

        private static BlockPort? PortOf(BlockDefinition b, string name)
        {
            for (int i = 0; i < b.Ports.Count; i++)
            {
                if (b.Ports[i].Name == name)
                    return b.Ports[i];
            }
            return null;
        }

        /// <summary>
        /// True when a pin drives its signal: a gate output, or an output in the chip file.
        /// </summary>
        private bool IsOutput(string part, int pin)
        {
            for (int i = 0; i < Netlist.Gates.Count; i++)
            {
                if (Netlist.Gates[i].Part == part && Netlist.Gates[i].Output == pin)
                    return true;
            }
            Part? p = FindPart(part);
            ChipInfo? chip = p == null ? null : Catalog?.Get(p.Type);
            for (int i = 0; chip != null && i < chip.Pins.Count; i++)
            {
                if (chip.Pins[i].Number == pin)
                    return chip.Pins[i].Role == "output";
            }
            return false;
        }

        /// <summary>
        /// NAME followed by a number, such as BD15: the name and the number.
        /// </summary>
        private static bool Numbered(string name, out string prefix, out int number)
        {
            int k = name.Length;
            while (k > 0 && char.IsDigit(name[k - 1]))
                k--;
            prefix = name.Substring(0, k);
            number = 0;
            // a number after an underscore is not a bit number: automatic names are
            // N_<chip>_<pin> (N_11E_8), and those must never be grouped into a bus port
            if (k > 0 && name[k - 1] == '_')
                return false;
            return k > 0 && k < name.Length && int.TryParse(name.AsSpan(k), NumberStyles.None, CultureInfo.InvariantCulture, out number);
        }

        private static SignalRef Ref(Net n) => new() { Net = n.Name, Active = n.Active == ActiveLevel.None ? null : n.Active };

        private string FreeInstanceName()
        {
            for (int n = 1; ; n++)
            {
                string name = "U" + n.ToString(CultureInfo.InvariantCulture);
                if (Hierarchy.FindInstance(Netlist.Instances, name) == null)
                    return name;
            }
        }

        /// <summary>
        /// Where the selected parts start: the smallest X and Y of their hand-set positions (or
        /// measured gate positions), or null when none has one.
        /// </summary>
        private Point? Anchor(HashSet<string> parts)
        {
            List<Part> list = new();
            for (int i = 0; i < Netlist.Parts.Count; i++)
            {
                if (parts.Contains(Netlist.Parts[i].Ref))
                    list.Add(Netlist.Parts[i]);
            }
            List<Gate> gates = new();
            for (int i = 0; i < Netlist.Gates.Count; i++)
            {
                if (parts.Contains(Netlist.Gates[i].Part))
                    gates.Add(Netlist.Gates[i]);
            }
            return AnchorOf(list, gates);
        }

        private static Point? AnchorOf(List<Part> parts, List<Gate> gates)
        {
            double x = double.MaxValue, y = double.MaxValue;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Position != null)
                {
                    x = Math.Min(x, parts[i].Position!.X);
                    y = Math.Min(y, parts[i].Position!.Y);
                }
            }
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i].Position != null)
                {
                    x = Math.Min(x, gates[i].Position!.X);
                    y = Math.Min(y, gates[i].Position!.Y);
                }
            }
            return x == double.MaxValue ? null : new Point { X = x, Y = y };
        }

        private void MoveWirePath(Net n, BlockDefinition b)
        {
            WirePath? w = WirePathOf(n.Name, n.Active == ActiveLevel.None ? null : n.Active);
            if (w == null)
                return;
            RemoveWirePath(n.Name, n.Active);
            b.Layout ??= new SheetLayoutSection();
            b.Layout.Wires.Add(w);
        }
    }
}
