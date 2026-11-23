//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// The block tools: make a block from parts, dissolve an instance, go into an instance and
    /// back out, list blocks and instances, place a port on the instance box. Making and
    /// dissolving never change what is connected (the flattened netlist).
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>
        /// Makes a block from parts and instances on the current level.
        /// </summary>
        /// <param name="block">
        /// The new block's name.
        /// </param>
        /// <param name="members">
        /// Part references, and instances as inst:NAME.
        /// </param>
        /// <param name="instance">
        /// The instance name, or empty for the next free U number.
        /// </param>
        /// <param name="portNames">
        /// Port renames as OLD=NEW, or empty.
        /// </param>
        /// <returns>
        /// The instance and its ports.
        /// </returns>
        [McpServerTool(Name = "make_block"), Description("Move parts (and instances, written inst:U1) on the current level into a new block, with one instance of it in their place. Signals that cross the edge become ports: numbered ones (BD0..BD15) of one level and direction become one bus port. Connections never change. port_names renames ports: OLD=NEW pairs separated by spaces.")]
        public string MakeBlock([Description("New block name")] string block, [Description("Members separated by commas or spaces, e.g. \"13E 13D 11C\"")] string members,
            [Description("Instance name; empty for U1, U2, ...")] string? instance = null, [Description("Port renames, e.g. \"BD_0=DATA\"; empty for none")] string? portNames = null)
            => Run(e =>
            {
                List<string> list = SplitWords(members);
                List<PlannedPort> ports = e.PlanBlock(list);
                List<string> renames = SplitWords(portNames ?? "");
                for (int i = 0; i < renames.Count; i++)
                {
                    int eq = renames[i].IndexOf('=');
                    if (eq <= 0)
                        throw new SheetEditException("A port rename is OLD=NEW, not " + renames[i] + ".");
                    string from = renames[i].Substring(0, eq);
                    PlannedPort? p = null;
                    for (int k = 0; k < ports.Count && p == null; k++)
                    {
                        if (ports[k].Name == from)
                            p = ports[k];
                    }
                    if (p == null)
                        throw new SheetEditException("There is no planned port " + from + ".");
                    p.Name = renames[i].Substring(eq + 1);
                }
                string name = e.MakeBlock(block, string.IsNullOrWhiteSpace(instance) ? null : instance, list, ports);
                return "Made block " + block + " as instance " + name + ".\n" + PortsReport(Hierarchy.FindBlock(e.Root, block)!);
            });

        /// <summary>
        /// Dissolves an instance into the current level.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "dissolve_block"), Description("Put the inside of an instance back on the current level and remove the instance (and its block when no other instance uses it). Connections never change; names already taken here get the instance name in front.")]
        public string DissolveBlock([Description("Instance name, e.g. U1")] string instance)
            => Run(e =>
            {
                e.DissolveInstance(instance);
                return "Dissolved " + instance + ". This level now has " + e.Netlist.Parts.Count.ToString(CultureInfo.InvariantCulture) + " parts.";
            });

        /// <summary>
        /// Shows the inside of an instance.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <returns>
        /// The level now shown.
        /// </returns>
        [McpServerTool(Name = "enter_block"), Description("Go inside an instance on the current level; every other tool then works on the block's inside. Allowed read-only.")]
        public string EnterBlock([Description("Instance name, e.g. U1")] string instance)
        {
            SheetEditor e = Open();
            try
            {
                e.EnterInstance(instance);
            }
            catch (SheetEditException x)
            {
                throw new McpException(x.Message);
            }
            return LevelReport(e);
        }

        /// <summary>
        /// Goes back up one level.
        /// </summary>
        /// <returns>
        /// The level now shown.
        /// </returns>
        [McpServerTool(Name = "leave_block"), Description("Go back up one level (nothing happens at the top).")]
        public string LeaveBlock()
        {
            SheetEditor e = Open();
            e.LeaveInstance();
            return LevelReport(e);
        }

        /// <summary>
        /// Lists the block definitions.
        /// </summary>
        /// <returns>
        /// One block per paragraph with its ports.
        /// </returns>
        [McpServerTool(Name = "list_blocks"), Description("List every block definition with its ports (name, direction, width, signals inside, edge).")]
        public string ListBlocks()
        {
            SheetEditor e = Open();
            if (e.Root.Blocks == null || e.Root.Blocks.Count == 0)
                return "No blocks.";
            StringBuilder sb = new();
            for (int i = 0; i < e.Root.Blocks.Count; i++)
            {
                BlockDefinition b = e.Root.Blocks[i];
                sb.Append("Block ").Append(b.Name).Append(": ").Append(b.Parts.Count).Append(" parts, ")
                    .Append(b.Instances?.Count ?? 0).Append(" instances inside.\n").Append(PortsReport(b)).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Lists the instances on the current level.
        /// </summary>
        /// <returns>
        /// One instance per line with its connections.
        /// </returns>
        [McpServerTool(Name = "list_instances"), Description("List the instances on the current level: block, position and what each port is connected to.")]
        public string ListInstances()
        {
            SheetEditor e = Open();
            StringBuilder sb = new();
            sb.Append(LevelReport(e)).Append('\n');
            List<BlockInstance>? list = e.Netlist.Instances;
            if (list == null || list.Count == 0)
                return sb.Append("No instances on this level.").ToString();
            for (int i = 0; i < list.Count; i++)
            {
                BlockInstance inst = list[i];
                sb.Append(inst.Name).Append(" (").Append(inst.Block).Append(')');
                if (inst.Position != null)
                    sb.Append(CultureInfo.InvariantCulture, $" at {inst.Position.X:0.#},{inst.Position.Y:0.#} mm");
                sb.Append('\n');
                for (int c = 0; inst.Connections != null && c < inst.Connections.Count; c++)
                {
                    PortConnection pc = inst.Connections[c];
                    sb.Append("  ").Append(pc.Port).Append(" -> ");
                    for (int k = 0; k < pc.Nets.Count; k++)
                        sb.Append(k == 0 ? "" : " ").Append(pc.Nets[k].Net).Append(pc.Nets[k].Active == null ? "" : pc.Nets[k].Active == ActiveLevel.High ? "_1" : "_0");
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Places a port on its instance box.
        /// </summary>
        /// <param name="block">
        /// The block.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <param name="side">
        /// left, right, top or bottom.
        /// </param>
        /// <param name="order">
        /// Place along the edge, or -1 for port order.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_port_position"), Description("Place a block's port pin on an edge of its instance boxes (left, right, top or bottom) and at a place along it (0 first, from the top or left; -1 for port order). Drawing only.")]
        public string SetPortPosition([Description("Block name")] string block, [Description("Port name")] string port,
            [Description("left, right, top or bottom")] string side, [Description("Place along the edge, -1 for port order")] int order = -1)
            => Run(e =>
            {
                if (!Enum.TryParse(side, true, out PortSide s) || int.TryParse(side, out _))
                    throw new SheetEditException("The edge is left, right, top or bottom, not " + side + ".");
                e.SetPortPosition(block, port, s, order < 0 ? null : order);
                return "Port " + port + " of " + block + " is on the " + s.ToString().ToLowerInvariant() + " edge.";
            });

        private static string LevelReport(SheetEditor e)
            => e.LevelPath.Count == 0 ? "At the top level." : "Inside " + string.Join("/", e.LevelPath) + " (block " + e.CurrentBlock()!.Name + ").";

        private static string PortsReport(BlockDefinition b)
        {
            StringBuilder sb = new();
            for (int i = 0; i < b.Ports.Count; i++)
            {
                BlockPort p = b.Ports[i];
                sb.Append("  ").Append(p.Name).Append(' ').Append(p.Direction.ToString().ToLowerInvariant())
                    .Append(p.Signals.Count > 1 ? " [" + p.Signals.Count.ToString(CultureInfo.InvariantCulture) + "]" : "")
                    .Append(" on the ").Append(SheetLayout.SideOf(p).ToString().ToLowerInvariant()).Append(": ");
                for (int k = 0; k < p.Signals.Count; k++)
                    sb.Append(k == 0 ? "" : " ").Append(p.Signals[k].Net);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private static List<string> SplitWords(string text)
        {
            List<string> list = new();
            string[] words = text.Split(new[] { ' ', ',', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
                list.Add(words[i]);
            return list;
        }
    }
}
