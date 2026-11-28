//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// Building the block structure piece by piece, as a reading gives it: a new empty block, its
    /// ports, instances of it, what each instance port is connected to, and moving an instance.
    /// (make_block instead moves parts already drawn into a new block.) Each needs edit mode and
    /// is one undo step.
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>
        /// Adds an empty block.
        /// </summary>
        /// <param name="name">
        /// The block's name.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "create_block"), Description("Add an empty block definition (no ports, parts or signals) (edit mode; undoable). Then put an instance of it on a level with add_instance, go inside with enter_block to place parts and add signals, and give it ports with set_block_port.")]
        public string CreateBlock([Description("Block name, e.g. F_ND_INT")] string name)
            => Run(e =>
            {
                e.CreateBlock(name);
                return "Made the empty block " + name.Trim() + ".";
            });

        /// <summary>
        /// Adds or replaces a block port.
        /// </summary>
        /// <param name="block">
        /// The block.
        /// </param>
        /// <param name="port">
        /// The port's name.
        /// </param>
        /// <param name="direction">
        /// input, output or inout.
        /// </param>
        /// <param name="signals">
        /// The signals inside the block, by key, separated by commas or spaces.
        /// </param>
        /// <param name="directionConfidence">
        /// read, inferred or open.
        /// </param>
        /// <param name="note">
        /// Free text, or empty.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_block_port"), Description("Add a port to a block, or replace the one of the same name (its drawing place is kept) (edit mode; undoable). signals: the signals INSIDE the block it carries, by key (BD3_0) or a name only one signal has, separated by commas or spaces; several make a bus port. Each must already be a signal in the block (add_net inside it). direction is seen from inside the block: input, output or inout; say how sure (directionConfidence) and why (note).")]
        public string SetBlockPort([Description("Block name")] string block, [Description("Port name, e.g. BD_0")] string port, [Description("input, output or inout")] string direction,
            [Description("Signals inside the block, e.g. \"BD0_0 BD1_0\"")] string signals,
            [Description("read, inferred or open")] string directionConfidence = "read", [Description("Why the direction is what it is, or empty")] string? note = null)
            => Run(e =>
            {
                BlockDefinition b = Hierarchy.FindBlock(e.Root, block) ?? throw new SheetEditException("There is no block " + block + ".");
                e.SetBlockPort(block, port, ParseDirection(direction), ParseConfidence(directionConfidence), ResolveSignals(b.Nets, signals, "inside block " + block), note);
                return "Set port " + port.Trim() + " of block " + block + ".";
            });

        /// <summary>
        /// Removes a block port.
        /// </summary>
        /// <param name="block">
        /// The block.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "remove_block_port"), Description("Remove a port from a block, and every instance connection made to it (edit mode; undoable). The signals inside the block stay.")]
        public string RemoveBlockPort([Description("Block name")] string block, [Description("Port name")] string port)
            => Run(e =>
            {
                e.RemoveBlockPort(block, port);
                return "Removed port " + port + " of block " + block + ".";
            });

        /// <summary>
        /// Puts an instance of a block on the current level.
        /// </summary>
        /// <param name="name">
        /// The instance's name.
        /// </param>
        /// <param name="block">
        /// The block.
        /// </param>
        /// <param name="xMm">
        /// Left edge in mm, or a negative number to let the layout place it.
        /// </param>
        /// <param name="yMm">
        /// Top edge in mm.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "add_instance"), Description("Put an instance of a block on the current level, with no port connected yet (edit mode; undoable). Connect its ports with connect_port. Give x and y in mm for its box, or leave x negative to let the layout place it.")]
        public string AddInstance([Description("Instance name, e.g. U1 or the block's own name")] string name, [Description("Block name")] string block,
            [Description("Left edge in mm; negative lets the layout place it")] double xMm = -1, [Description("Top edge in mm")] double yMm = -1)
            => Run(e =>
            {
                e.AddInstance(name, block, xMm < 0 ? null : xMm, xMm < 0 ? null : yMm);
                return "Put instance " + name.Trim() + " of block " + block + " on this level.";
            });

        /// <summary>
        /// Removes an instance.
        /// </summary>
        /// <param name="name">
        /// The instance.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "remove_instance"), Description("Remove an instance from the current level (edit mode; undoable). Its block stays, and so do the signals it was connected to. To bring its inside onto this level use dissolve_block instead.")]
        public string RemoveInstance([Description("Instance name")] string name)
            => Run(e =>
            {
                e.RemoveInstance(name);
                return "Removed instance " + name + ".";
            });

        /// <summary>
        /// Connects an instance port to signals on the current level.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <param name="nets">
        /// The signals on this level, by key, separated by commas or spaces; empty disconnects.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "connect_port"), Description("Connect an instance's port to signals on the current level, one for each signal the port carries, in the port's order (edit mode; undoable). nets: by key (BD3_0) or a name only one signal has, separated by commas or spaces; empty takes the connection away. A signal not on this level yet: add_net first.")]
        public string ConnectPort([Description("Instance name")] string instance, [Description("Port name")] string port,
            [Description("Signals on this level, e.g. \"BD0_0 BD1_0\"; empty disconnects")] string? nets = null)
            => Run(e =>
            {
                List<SignalRef> refs = string.IsNullOrWhiteSpace(nets) ? new List<SignalRef>() : ResolveSignals(e.Netlist.Nets, nets, "on this level");
                e.ConnectPort(instance, port, refs);
                return refs.Count == 0 ? "Disconnected " + instance + "/" + port + "." : "Connected " + instance + "/" + port + " to " + refs.Count.ToString(CultureInfo.InvariantCulture) + " signal(s).";
            });

        /// <summary>
        /// Moves an instance box.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="xMm">
        /// New left edge in mm.
        /// </param>
        /// <param name="yMm">
        /// New top edge in mm.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "move_instance"), Description("Move an instance box on the current level to a new place (mm), as dragging it in the window does. Only the drawing changes (edit mode; undoable).")]
        public string MoveInstance([Description("Instance name")] string instance, [Description("New left edge in mm")] double xMm, [Description("New top edge in mm")] double yMm)
            => Run(e =>
            {
                e.MoveInstance(instance, xMm, yMm);
                return "Moved " + instance + ".";
            });

        /// <summary>
        /// Moves parts into an existing instance's block.
        /// </summary>
        /// <param name="members">
        /// The parts (and inst:NAME instances), separated by commas or spaces.
        /// </param>
        /// <param name="instance">
        /// The instance whose block gets them.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "move_into_block"), Description("Move parts (and instances, written inst:U1) on the current level into the block of an existing instance here, as dragging them onto it in the window does. Signals that now cross the edge get ports; connections never change (edit mode; undoable).")]
        public string MoveIntoBlock([Description("Members separated by commas or spaces, e.g. \"13E 13D\"")] string members, [Description("Instance name")] string instance)
            => Run(e =>
            {
                e.MoveIntoInstance(SplitWords(members), instance);
                return "Moved " + members + " into " + instance + ".";
            });

        private static PortDirection ParseDirection(string text) => text.Trim().ToLowerInvariant() switch
        {
            "input" or "in" => PortDirection.Input,
            "output" or "out" => PortDirection.Output,
            "inout" => PortDirection.Inout,
            _ => throw new McpException("direction must be input, output or inout, not '" + text + "'."),
        };

        // signals written by key (BD3_0) or by a name only one signal has, made into references
        private static List<SignalRef> ResolveSignals(List<Net> nets, string text, string where)
        {
            List<SignalRef> refs = new();
            List<string> words = SplitWords(text);
            for (int w = 0; w < words.Count; w++)
            {
                Net? found = null;
                int byName = 0;
                for (int i = 0; i < nets.Count && found == null; i++)
                {
                    if (SheetLayout.NetKey(nets[i]) == words[w])
                        found = nets[i];
                }
                for (int i = 0; i < nets.Count && found == null; i++)
                {
                    if (nets[i].Name == words[w])
                        byName++;
                }
                if (found == null && byName == 1)
                {
                    for (int i = 0; i < nets.Count && found == null; i++)
                    {
                        if (nets[i].Name == words[w])
                            found = nets[i];
                    }
                }
                if (found == null)
                    throw new McpException(byName > 1
                        ? "Two signals " + where + " are named " + words[w] + "; give the key (" + words[w] + "_0 or " + words[w] + "_1)."
                        : "There is no signal " + words[w] + " " + where + ".");
                refs.Add(new SignalRef { Net = found.Name, Active = found.Active });
            }
            return refs;
        }
    }
}
