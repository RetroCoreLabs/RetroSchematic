//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// One row of a list of signals in the side panel: what the row is (a connector pin, a bus
    /// member, a block port), the signal on it, and any extra word. A row with a signal key is
    /// clickable: it traces that signal, as a click on its wire does.
    /// </summary>
    public sealed class SignalLink
    {
        /// <summary>
        /// The row's own name: the connector pin (A.32, 11), the port name, or the bus member.
        /// </summary>
        public string Label { get; init; } = "";

        /// <summary>
        /// The signal's key (for tracing it), or empty when the row has no signal.
        /// </summary>
        public string NetKey { get; init; } = "";

        /// <summary>
        /// The signal's name in the chosen style, or empty.
        /// </summary>
        public string NetName { get; init; } = "";

        /// <summary>
        /// Extra words: the backplane name, the port's direction, or empty.
        /// </summary>
        public string Extra { get; init; } = "";
    }

    /// <summary>
    /// What the side panel shows for a connector, a bus or a block instance: a title, a few
    /// label and value facts, and the list of signals on it, each one clickable.
    /// </summary>
    public sealed class SignalList
    {
        /// <summary>
        /// The kind: Connector, Bus or Block.
        /// </summary>
        public string Kind { get; init; } = "";

        /// <summary>
        /// The big title, such as "Backplane (connector not known)" or "U1  SYNC".
        /// </summary>
        public string Title { get; init; } = "";

        /// <summary>
        /// Label and value facts.
        /// </summary>
        public List<KeyValuePair<string, string>> Facts { get; } = new();

        /// <summary>
        /// The column titles of the list: row name, signal, extra.
        /// </summary>
        public string[] Columns { get; init; } = { "", "Signal", "" };

        /// <summary>
        /// The rows.
        /// </summary>
        public List<SignalLink> Rows { get; } = new();

        /// <summary>
        /// The list for a connector, a bus or an instance on the level shown, or null for any
        /// other pick.
        /// </summary>
        /// <param name="session">
        /// The open sheet.
        /// </param>
        /// <param name="owner">
        /// What is picked: conn:P, bus:BD_0, inst:U1.
        /// </param>
        /// <returns>
        /// The list, or null.
        /// </returns>
        public static SignalList? Of(SheetSession session, string owner)
        {
            if (owner.StartsWith("conn:", System.StringComparison.Ordinal))
                return Connector(session, owner.Substring(5));
            if (owner.StartsWith("bus:", System.StringComparison.Ordinal))
                return Bus(session, owner.Substring(4));
            if (owner.StartsWith("inst:", System.StringComparison.Ordinal))
                return Instance(session, owner.Substring(5));
            return null;
        }

        private static SignalList Connector(SheetSession session, string reference)
        {
            SignalList l = new() { Kind = "Connector", Title = ConnectorPins.Name(reference, session.Bus), Columns = new[] { "Pin", "Signal", "Backplane name" } };
            List<ConnectorUse>? uses = session.Editor.Root.Connectors;
            for (int i = 0; uses != null && i < uses.Count; i++)
            {
                if (uses[i].Ref != reference)
                    continue;
                if (uses[i].Catalog != null)
                    l.Facts.Add(new KeyValuePair<string, string>("Catalogue", uses[i].Catalog!));
                if (!string.IsNullOrEmpty(uses[i].Note))
                    l.Facts.Add(new KeyValuePair<string, string>("Notes", uses[i].Note!));
            }
            l.Facts.Add(new KeyValuePair<string, string>("Bus", SheetSession.BusText(session.Bus)));
            // every pin the sheet uses, in pin order
            List<KeyValuePair<int, Net>> pins = new();
            Netlist n = session.Netlist;
            for (int i = 0; i < n.Nets.Count; i++)
            {
                for (int j = 0; j < n.Nets[i].Pins.Count; j++)
                {
                    if (n.Nets[i].Pins[j].PartRef == reference)
                        pins.Add(new KeyValuePair<int, Net>(n.Nets[i].Pins[j].Pin, n.Nets[i]));
                }
            }
            pins.Sort((a, b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < pins.Count; i++)
            {
                BusSignal? bs = ConnectorPins.Kind(reference) == 'P' ? session.BusSignals.ByPin(ConnectorPins.Letter(reference), pins[i].Key) : null;
                l.Rows.Add(new SignalLink
                {
                    Label = ConnectorPins.Letter(reference) == null ? pins[i].Key.ToString(System.Globalization.CultureInfo.InvariantCulture) : ConnectorPins.Show(reference, pins[i].Key, session.Bus),
                    NetKey = SheetLayout.NetKey(pins[i].Value),
                    NetName = session.NameOf(pins[i].Value),
                    Extra = bs == null || string.Equals(bs.Name, pins[i].Value.Name, System.StringComparison.OrdinalIgnoreCase) ? "" : bs.Name,
                });
            }
            return l;
        }

        private static SignalList Bus(SheetSession session, string name)
        {
            SignalList l = new() { Kind = "Bus", Title = "Bus " + name, Columns = new[] { "#", "Signal", "" } };
            BusLine? bus = session.Editor.FindBus(name);
            if (bus == null)
                return l;
            l.Facts.Add(new KeyValuePair<string, string>("Members", bus.Members.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            for (int i = 0; i < bus.Members.Count; i++)
            {
                Net? n = SheetLayout.FindMember(session.Netlist, bus.Members[i]);
                l.Rows.Add(new SignalLink
                {
                    Label = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    NetKey = n == null ? "" : SheetLayout.NetKey(n),
                    NetName = n == null ? bus.Members[i].Net + " (not on this sheet)" : session.NameOf(n),
                });
            }
            return l;
        }

        private static SignalList? Instance(SheetSession session, string name)
        {
            BlockInstance? inst = Hierarchy.FindInstance(session.Netlist.Instances, name);
            BlockDefinition? b = inst == null ? null : Hierarchy.FindBlock(session.Editor.Root, inst.Block);
            if (inst == null || b == null)
                return null;
            SignalList l = new() { Kind = "Block", Title = inst.Name + "  " + b.Name, Columns = new[] { "Port", "Signal", "Dir" } };
            l.Facts.Add(new KeyValuePair<string, string>("Inside", b.Parts.Count + " parts, " + (b.Instances?.Count ?? 0) + " instances (double-click the box to open it)"));
            for (int i = 0; i < b.Ports.Count; i++)
            {
                BlockPort p = b.Ports[i];
                string dir = p.Direction.ToString().ToLowerInvariant();
                PortConnection? pc = null;
                for (int c = 0; inst.Connections != null && c < inst.Connections.Count; c++)
                {
                    if (inst.Connections[c].Port == p.Name)
                        pc = inst.Connections[c];
                }
                if (pc == null || pc.Nets.Count == 0)
                {
                    l.Rows.Add(new SignalLink { Label = p.Name, Extra = dir });
                    continue;
                }
                // a bus port gives one row per signal
                for (int k = 0; k < pc.Nets.Count; k++)
                {
                    Net? n = NetlistReader.FindNet(session.Netlist.Nets, pc.Nets[k]);
                    l.Rows.Add(new SignalLink
                    {
                        Label = pc.Nets.Count == 1 ? p.Name : p.Name + "[" + k.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]",
                        NetKey = n == null ? "" : SheetLayout.NetKey(n),
                        NetName = n == null ? "" : session.NameOf(n),
                        Extra = dir,
                    });
                }
            }
            return l;
        }
    }
}
