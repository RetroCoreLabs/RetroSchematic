//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// The bus tools: create, set the path, add and remove members, delete, list. Drawing only.
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>
        /// Creates a bus.
        /// </summary>
        /// <param name="name">
        /// The bus name.
        /// </param>
        /// <param name="members">
        /// Signal keys or patterns.
        /// </param>
        /// <param name="path">
        /// The path as x,y points in mm, or empty.
        /// </param>
        /// <returns>
        /// What was done, and how the bus is drawn.
        /// </returns>
        [McpServerTool(Name = "create_bus"), Description("Create a virtual bus (drawing only, never changes connections). members: signal keys (BD15_0) and/or patterns, separated by commas or spaces - BD*_0 (any characters for *) or IDATA0-7 / BD0-15_1 (a numbered range). path: x,y points in whole mm separated by spaces, every piece level or upright; empty to set later with set_bus_path.")]
        public string CreateBus([Description("Bus name, e.g. BD_0")] string name, [Description("Members or patterns")] string members,
            [Description("Path points, e.g. \"120,50 120,220\"; empty for none yet")] string? path = null)
            => Run(e =>
            {
                List<Point>? points = string.IsNullOrWhiteSpace(path) ? null : ParseBranches(path)[0];
                e.CreateBus(name, new List<string> { members }, points);
                return "Created bus " + name + " with " + e.FindBus(name)!.Members.Count + " signals. " + BusReport(e, name);
            });

        /// <summary>
        /// Sets or moves a bus's path.
        /// </summary>
        /// <param name="name">
        /// The bus.
        /// </param>
        /// <param name="path">
        /// The path points.
        /// </param>
        /// <returns>
        /// How the bus is drawn now.
        /// </returns>
        [McpServerTool(Name = "set_bus_path"), Description("Set or move a bus's path: x,y points in whole mm separated by spaces, every piece level or upright. A path through a chip is routed afresh between its ends, and the reason is given.")]
        public string SetBusPath([Description("Bus name")] string name, [Description("Path points, e.g. \"120,50 120,220\"")] string path)
            => Run(e =>
            {
                e.SetBusPath(name, ParseBranches(path)[0]);
                return "Set the path of bus " + name + ". " + BusReport(e, name);
            });

        /// <summary>
        /// Adds a signal to a bus.
        /// </summary>
        /// <param name="name">
        /// The bus.
        /// </param>
        /// <param name="signal">
        /// The signal key.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "add_bus_member"), Description("Add a signal (key such as BD15_0) to a bus. A signal joins one bus only.")]
        public string AddBusMember([Description("Bus name")] string name, [Description("Signal key, e.g. BD15_0")] string signal)
            => Run(e =>
            {
                e.AddBusMember(name, signal);
                return "Added " + signal + " to bus " + name + ". " + BusReport(e, name);
            });

        /// <summary>
        /// Takes a signal off a bus.
        /// </summary>
        /// <param name="name">
        /// The bus.
        /// </param>
        /// <param name="signal">
        /// The signal key.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "remove_bus_member"), Description("Take a signal off a bus; it is drawn as usual again.")]
        public string RemoveBusMember([Description("Bus name")] string name, [Description("Signal key, e.g. BD15_0")] string signal)
            => Run(e =>
            {
                e.RemoveBusMember(name, signal);
                return "Took " + signal + " off bus " + name + ".";
            });

        /// <summary>
        /// Deletes a bus.
        /// </summary>
        /// <param name="name">
        /// The bus.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "delete_bus"), Description("Delete a bus; its signals are drawn as usual again. Connections never change.")]
        public string DeleteBus([Description("Bus name")] string name)
            => Run(e =>
            {
                e.DeleteBus(name);
                return "Deleted bus " + name + ".";
            });

        /// <summary>
        /// Lists the buses.
        /// </summary>
        /// <returns>
        /// One line per bus: name, path, members.
        /// </returns>
        [McpServerTool(Name = "list_buses"), Description("List the buses on the open sheet: name, path in mm and member signals.")]
        public string ListBuses()
        {
            SheetEditor e = Open();
            List<BusLine>? buses = e.Netlist.Layout?.Buses;
            StringBuilder sb = new();
            for (int i = 0; buses != null && i < buses.Count; i++)
            {
                BusLine b = buses[i];
                sb.Append(b.Name).Append(" path:");
                for (int j = 0; j < b.Path.Count; j++)
                    sb.Append(' ').Append(b.Path[j].X.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(b.Path[j].Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.Append(" members:");
                for (int j = 0; j < b.Members.Count; j++)
                {
                    Net? n = SheetLayout.FindMember(e.Netlist, b.Members[j]);
                    sb.Append(' ').Append(n == null ? b.Members[j].Net : SheetLayout.NetKey(n));
                }
                sb.Append('\n');
            }
            return sb.Length == 0 ? "No buses." : sb.ToString();
        }

        /// <summary>
        /// How a bus is drawn now: fine, or each problem in plain words.
        /// </summary>
        /// <param name="e">
        /// The editor.
        /// </param>
        /// <param name="name">
        /// The bus.
        /// </param>
        /// <returns>
        /// The report.
        /// </returns>
        private static string BusReport(SheetEditor e, string name)
        {
            SchematicDrawing d = SheetLayout.Build(e.Netlist, SymbolStyle.NorskData, new LayoutOptions { Catalog = e.Catalog });
            StringBuilder sb = new();
            for (int i = 0; i < d.BusProblems.Count; i++)
            {
                if (d.BusProblems[i].StartsWith(name + ":", StringComparison.Ordinal))
                    sb.Append(d.BusProblems[i]).Append(". ");
            }
            return sb.Length == 0 ? "Drawn with every member pin joining it." : sb.ToString();
        }
    }
}
