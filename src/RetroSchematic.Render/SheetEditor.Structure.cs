//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.Globalization;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Building the block structure by hand, piece by piece, as a reading gives it: a new empty
    /// block, its ports, an instance of it on the level shown, and what each instance port is
    /// connected to. (<see cref="MakeBlock"/> instead moves parts that are already drawn into a
    /// new block.) Each needs edit mode and is one undo step.
    /// </summary>
    public sealed partial class SheetEditor
    {
        /// <summary>
        /// Adds an empty block definition (no ports, parts or signals yet).
        /// </summary>
        /// <param name="name">
        /// The block's name.
        /// </param>
        /// <exception cref="SheetEditException">
        /// Read-only, an empty name, or the name is taken.
        /// </exception>
        public void CreateBlock(string name)
        {
            CheckWritable();
            string n = name.Trim();
            if (n.Length == 0 || n.IndexOf('/') >= 0 || n.IndexOf(' ') >= 0)
                throw new SheetEditException("\"" + name + "\" cannot name a block: no slash or space, and not empty.");
            if (Hierarchy.FindBlock(Root, n) != null)
                throw new SheetEditException("There is a block " + n + " already.");
            Snapshot();
            Root.Blocks ??= new List<BlockDefinition>();
            Root.Blocks.Add(new BlockDefinition { Name = n });
            Done();
        }

        /// <summary>
        /// Adds a port to a block, or replaces the port of the same name (its drawing place is
        /// kept). Every signal must be a signal inside the block (add one with no pins with
        /// <see cref="AddNet"/> while inside it). A replaced port with a different number of
        /// signals drops the instance connections made for the old one.
        /// </summary>
        /// <param name="block">
        /// The block.
        /// </param>
        /// <param name="port">
        /// The port's name.
        /// </param>
        /// <param name="direction">
        /// Which way it carries its signals, seen from inside the block.
        /// </param>
        /// <param name="directionConfidence">
        /// How sure the direction is.
        /// </param>
        /// <param name="signals">
        /// The signals inside the block it carries, in order (one, or several for a bus port).
        /// </param>
        /// <param name="note">
        /// Free text, or null.
        /// </param>
        /// <exception cref="SheetEditException">
        /// Read-only, no such block, no signals, or a signal that is not in the block.
        /// </exception>
        public void SetBlockPort(string block, string port, PortDirection direction, Confidence directionConfidence, List<SignalRef> signals, string? note = null)
        {
            CheckWritable();
            BlockDefinition b = Hierarchy.FindBlock(Root, block) ?? throw new SheetEditException("There is no block " + block + ".");
            string name = port.Trim();
            if (name.Length == 0)
                throw new SheetEditException("A port needs a name.");
            if (signals.Count == 0)
                throw new SheetEditException("A port carries at least one signal.");
            SyncBack();
            for (int i = 0; i < signals.Count; i++)
            {
                if (NetlistReader.FindNet(b.Nets, signals[i]) == null)
                    throw new SheetEditException("Port " + name + ": there is no signal " + signals[i].Net + " inside block " + block + "; add it there first.");
            }
            Snapshot();
            b = Hierarchy.FindBlock(Root, block)!;
            BlockPort p = new() { Name = name, Direction = direction, DirectionConfidence = directionConfidence, Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim() };
            p.Signals.AddRange(signals);
            int at = PortIndex(b, name);
            if (at < 0)
                b.Ports.Add(p);
            else
            {
                p.Side = b.Ports[at].Side;
                p.Order = b.Ports[at].Order;
                if (b.Ports[at].Signals.Count != p.Signals.Count)
                    DropConnections(block, name);
                b.Ports[at] = p;
            }
            ShowLevel();
            Done();
        }

        /// <summary>
        /// Removes a port from a block, and every instance connection made to it.
        /// </summary>
        /// <param name="block">
        /// The block.
        /// </param>
        /// <param name="port">
        /// The port's name.
        /// </param>
        /// <exception cref="SheetEditException">
        /// Read-only, or no such block or port.
        /// </exception>
        public void RemoveBlockPort(string block, string port)
        {
            CheckWritable();
            BlockDefinition b = Hierarchy.FindBlock(Root, block) ?? throw new SheetEditException("There is no block " + block + ".");
            if (PortIndex(b, port) < 0)
                throw new SheetEditException("Block " + block + " has no port " + port + ".");
            Snapshot();
            b = Hierarchy.FindBlock(Root, block)!;
            b.Ports.RemoveAt(PortIndex(b, port));
            DropConnections(block, port);
            ShowLevel();
            Done();
        }

        /// <summary>
        /// Puts an instance of a block on the level shown, with no port connected yet.
        /// </summary>
        /// <param name="name">
        /// The instance's name.
        /// </param>
        /// <param name="block">
        /// The block it is an instance of.
        /// </param>
        /// <param name="xMm">
        /// Its box's left edge in mm, or null to let the layout place it.
        /// </param>
        /// <param name="yMm">
        /// Its box's top edge in mm, or null to let the layout place it.
        /// </param>
        /// <exception cref="SheetEditException">
        /// Read-only, no such block, the block shown itself, or the name is taken.
        /// </exception>
        public void AddInstance(string name, string block, double? xMm, double? yMm)
        {
            CheckWritable();
            string n = name.Trim();
            if (n.Length == 0 || n.IndexOf('/') >= 0 || n.IndexOf('.') >= 0 || n.IndexOf(' ') >= 0)
                throw new SheetEditException("\"" + name + "\" cannot name an instance: no slash, dot or space, and not empty.");
            if (Hierarchy.FindBlock(Root, block) == null)
                throw new SheetEditException("There is no block " + block + ".");
            if (CurrentBlock()?.Name == block)
                throw new SheetEditException("A block cannot hold an instance of itself.");
            if (Hierarchy.FindInstance(Netlist.Instances, n) != null || FindPart(n) != null)
                throw new SheetEditException("The name " + n + " is taken on this level.");
            Snapshot();
            BlockInstance inst = new() { Name = n, Block = block, Connections = new List<PortConnection>() };
            if (xMm != null && yMm != null)
                inst.Position = new Point { X = xMm.Value, Y = yMm.Value };
            Netlist.Instances ??= new List<BlockInstance>();
            Netlist.Instances.Add(inst);
            if (_path.Count == 0)
                Root.Instances = Netlist.Instances;
            Done();
        }

        /// <summary>
        /// Removes an instance from the level shown. Its block stays (other instances, or a later
        /// one, may use it); the signals it was connected to stay.
        /// </summary>
        /// <param name="name">
        /// The instance's name.
        /// </param>
        /// <exception cref="SheetEditException">
        /// Read-only, or no such instance on this level.
        /// </exception>
        public void RemoveInstance(string name)
        {
            CheckWritable();
            if (Hierarchy.FindInstance(Netlist.Instances, name) == null)
                throw new SheetEditException("There is no instance " + name + " on this level.");
            Snapshot();
            for (int i = Netlist.Instances!.Count - 1; i >= 0; i--)
            {
                if (Netlist.Instances[i].Name == name)
                    Netlist.Instances.RemoveAt(i);
            }
            Done();
        }

        /// <summary>
        /// Connects an instance's port to signals on the level shown, one for each signal the port
        /// carries, in the port's order; an empty list takes the connection away.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="port">
        /// The port of its block.
        /// </param>
        /// <param name="nets">
        /// The signals on this level, or an empty list.
        /// </param>
        /// <exception cref="SheetEditException">
        /// Read-only, no such instance or port, the wrong number of signals, or a signal that is
        /// not on this level.
        /// </exception>
        public void ConnectPort(string instance, string port, List<SignalRef> nets)
        {
            CheckWritable();
            BlockInstance inst = Hierarchy.FindInstance(Netlist.Instances, instance) ?? throw new SheetEditException("There is no instance " + instance + " on this level.");
            BlockDefinition b = Hierarchy.FindBlock(Root, inst.Block) ?? throw new SheetEditException("Instance " + instance + " names block " + inst.Block + ", which does not exist.");
            int at = PortIndex(b, port);
            if (at < 0)
                throw new SheetEditException("Block " + b.Name + " has no port " + port + ".");
            int want = b.Ports[at].Signals.Count;
            if (nets.Count != 0 && nets.Count != want)
                throw new SheetEditException("Port " + port + " carries " + want.ToString(CultureInfo.InvariantCulture) + " signal(s); give that many, not " + nets.Count.ToString(CultureInfo.InvariantCulture) + ".");
            for (int i = 0; i < nets.Count; i++)
            {
                if (NetlistReader.FindNet(Netlist.Nets, nets[i]) == null)
                    throw new SheetEditException("There is no signal " + nets[i].Net + " on this level.");
            }
            Snapshot();
            inst = Hierarchy.FindInstance(Netlist.Instances, instance)!;
            inst.Connections ??= new List<PortConnection>();
            for (int i = inst.Connections.Count - 1; i >= 0; i--)
            {
                if (inst.Connections[i].Port == port)
                    inst.Connections.RemoveAt(i);
            }
            if (nets.Count > 0)
            {
                PortConnection pc = new() { Port = port };
                pc.Nets.AddRange(nets);
                inst.Connections.Add(pc);
            }
            Done();
        }

        private static int PortIndex(BlockDefinition b, string port)
        {
            for (int i = 0; i < b.Ports.Count; i++)
            {
                if (b.Ports[i].Name == port)
                    return i;
            }
            return -1;
        }

        // every instance of the block, on every level, loses its connection to the port
        private void DropConnections(string block, string port)
        {
            DropConnections(Root.Instances, block, port);
            for (int i = 0; Root.Blocks != null && i < Root.Blocks.Count; i++)
                DropConnections(Root.Blocks[i].Instances, block, port);
        }

        private static void DropConnections(List<BlockInstance>? instances, string block, string port)
        {
            for (int i = 0; instances != null && i < instances.Count; i++)
            {
                List<PortConnection>? conns = instances[i].Block == block ? instances[i].Connections : null;
                for (int c = conns == null ? -1 : conns.Count - 1; c >= 0; c--)
                {
                    if (conns![c].Port == port)
                        conns.RemoveAt(c);
                }
            }
        }
    }
}
