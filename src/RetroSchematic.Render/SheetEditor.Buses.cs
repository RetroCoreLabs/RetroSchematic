//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Virtual buses: create, set the path, add and remove members, delete. Every one of these
    /// changes the layout section only, never the nets list.
    /// </summary>
    public sealed partial class SheetEditor
    {
        /// <summary>
        /// Creates a bus. Its members are given as signal keys (BD15_0, or a name only one signal
        /// has) and/or patterns (see <see cref="MatchSignals"/>).
        /// </summary>
        /// <param name="name">
        /// The bus name; must be new on the sheet.
        /// </param>
        /// <param name="members">
        /// Signal keys or patterns, such as "BD*_0" or "IDATA0-7".
        /// </param>
        /// <param name="path">
        /// The bend points in whole mm, or null/empty to set later.
        /// </param>
        public void CreateBus(string name, List<string> members, List<Point>? path)
        {
            CheckWritable();
            if (name.Trim().Length == 0)
                throw new SheetEditException("A bus needs a name.");
            if (FindBus(name) != null)
                throw new SheetEditException($"There is already a bus {name}.");
            List<Net> nets = new();
            for (int i = 0; i < members.Count; i++)
            {
                List<Net> found = MatchSignals(members[i]);
                if (found.Count == 0)
                    throw new SheetEditException($"No signal on the sheet matches \"{members[i]}\".");
                for (int j = 0; j < found.Count; j++)
                {
                    if (!nets.Contains(found[j]))
                        nets.Add(found[j]);
                }
            }
            if (nets.Count == 0)
                throw new SheetEditException("A bus needs at least one signal.");
            CheckPath(path);
            for (int i = 0; i < nets.Count; i++)
                CheckNotOnBus(nets[i], null);
            Snapshot();
            BusLine bus = new() { Name = name, Path = path == null ? new List<Point>() : new List<Point>(path) };
            for (int i = 0; i < nets.Count; i++)
                bus.Members.Add(Member(nets[i]));
            Netlist.Layout ??= new SheetLayoutSection();
            Netlist.Layout.Buses ??= new List<BusLine>();
            Netlist.Layout.Buses.Add(bus);
            Done();
        }

        /// <summary>
        /// Sets or moves a bus's path. Drawing only.
        /// </summary>
        /// <param name="name">
        /// The bus.
        /// </param>
        /// <param name="path">
        /// The bend points in whole mm, every piece level or upright.
        /// </param>
        public void SetBusPath(string name, List<Point> path)
        {
            CheckWritable();
            BusLine bus = FindBus(name) ?? throw new SheetEditException($"There is no bus {name}.");
            if (path.Count < 2)
                throw new SheetEditException("A bus path needs at least two points.");
            CheckPath(path);
            Snapshot();
            bus.Path = new List<Point>(path);
            Done();
        }

        /// <summary>
        /// Adds a signal to a bus.
        /// </summary>
        /// <param name="name">
        /// The bus.
        /// </param>
        /// <param name="signal">
        /// The signal key (BD15_0) or a name only one signal has.
        /// </param>
        public void AddBusMember(string name, string signal)
        {
            CheckWritable();
            BusLine bus = FindBus(name) ?? throw new SheetEditException($"There is no bus {name}.");
            Net net = FindNetByKey(signal) ?? throw new SheetEditException($"There is no signal {signal}.");
            CheckNotOnBus(net, null);
            Snapshot();
            bus.Members.Add(Member(net));
            Done();
        }

        /// <summary>
        /// Takes a signal off a bus; it is drawn as usual again.
        /// </summary>
        /// <param name="name">
        /// The bus.
        /// </param>
        /// <param name="signal">
        /// The signal key (BD15_0) or name.
        /// </param>
        public void RemoveBusMember(string name, string signal)
        {
            CheckWritable();
            BusLine bus = FindBus(name) ?? throw new SheetEditException($"There is no bus {name}.");
            Net net = FindNetByKey(signal) ?? throw new SheetEditException($"There is no signal {signal}.");
            int at = MemberIndex(bus, net);
            if (at < 0)
                throw new SheetEditException($"Signal {SheetLayout.NetKey(net)} is not on bus {name}.");
            if (bus.Members.Count == 1)
                throw new SheetEditException($"{SheetLayout.NetKey(net)} is the last signal on bus {name}; delete the bus instead.");
            Snapshot();
            bus.Members.RemoveAt(at);
            Done();
        }

        /// <summary>
        /// Deletes a bus; its signals are drawn as usual again.
        /// </summary>
        /// <param name="name">
        /// The bus.
        /// </param>
        public void DeleteBus(string name)
        {
            CheckWritable();
            BusLine bus = FindBus(name) ?? throw new SheetEditException($"There is no bus {name}.");
            Snapshot();
            Netlist.Layout!.Buses!.Remove(bus);
            if (Netlist.Layout.Buses.Count == 0)
                Netlist.Layout.Buses = null;
            if (Netlist.Layout.Wires.Count == 0 && Netlist.Layout.Buses == null)
                Netlist.Layout = null;
            Done();
        }

        /// <summary>
        /// A bus by name, or null.
        /// </summary>
        /// <param name="name">
        /// The bus name.
        /// </param>
        /// <returns>
        /// The bus, or null.
        /// </returns>
        public BusLine? FindBus(string name)
        {
            List<BusLine>? buses = Netlist.Layout?.Buses;
            for (int i = 0; buses != null && i < buses.Count; i++)
            {
                if (buses[i].Name == name)
                    return buses[i];
            }
            return null;
        }

        /// <summary>
        /// The bus a signal is on, or null.
        /// </summary>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <returns>
        /// The bus, or null.
        /// </returns>
        public BusLine? BusOf(Net net)
        {
            List<BusLine>? buses = Netlist.Layout?.Buses;
            for (int i = 0; buses != null && i < buses.Count; i++)
            {
                if (MemberIndex(buses[i], net) >= 0)
                    return buses[i];
            }
            return null;
        }

        /// <summary>
        /// The signals a member text names, in sheet order. The text is one of:
        /// <list type="bullet">
        /// <item><description>a pattern with * (any characters), matched against each signal's
        /// key: BD*_0 is every BD signal with subscript 0;</description></item>
        /// <item><description>a numbered range, NAME first-last, with an optional _0 or _1:
        /// IDATA0-7, BD0-15_1;</description></item>
        /// <item><description>a single key (BD15_0) or a name only one signal has.</description></item>
        /// </list>
        /// Several may be given separated by commas or spaces.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <returns>
        /// The signals; empty when none match.
        /// </returns>
        public List<Net> MatchSignals(string text)
        {
            List<Net> found = new();
            string[] parts = text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int p = 0; p < parts.Length; p++)
            {
                string t = parts[p];
                if (t.Contains('*', StringComparison.Ordinal))
                {
                    for (int i = 0; i < Netlist.Nets.Count; i++)
                    {
                        if (Glob(SheetLayout.NetKey(Netlist.Nets[i]), t))
                            AddOnce(found, Netlist.Nets[i]);
                    }
                    continue;
                }
                if (Range(t, out string prefix, out int first, out int last, out string level))
                {
                    for (int k = first; k <= last; k++)
                    {
                        Net? n = FindNetByKey(prefix + k.ToString(CultureInfo.InvariantCulture) + level);
                        if (n != null)
                            AddOnce(found, n);
                    }
                    continue;
                }
                Net? one = FindNetByKey(t);
                if (one != null)
                    AddOnce(found, one);
            }
            return found;
        }

        private static void AddOnce(List<Net> list, Net n)
        {
            if (!list.Contains(n))
                list.Add(n);
        }

        /// <summary>
        /// Reads NAME first-last with an optional _0 / _1 after it (IDATA0-7, BD0-15_1).
        /// </summary>
        /// <param name="t">
        /// The text.
        /// </param>
        /// <param name="prefix">
        /// The name before the numbers.
        /// </param>
        /// <param name="first">
        /// The first number.
        /// </param>
        /// <param name="last">
        /// The last number.
        /// </param>
        /// <param name="level">
        /// "_0", "_1" or empty.
        /// </param>
        /// <returns>
        /// True when the text is a range.
        /// </returns>
        private static bool Range(string t, out string prefix, out int first, out int last, out string level)
        {
            prefix = "";
            first = 0;
            last = 0;
            level = "";
            string body = t;
            if (body.EndsWith("_0", StringComparison.Ordinal) || body.EndsWith("_1", StringComparison.Ordinal))
            {
                level = body.Substring(body.Length - 2);
                body = body.Substring(0, body.Length - 2);
            }
            int dash = body.IndexOf('-');
            if (dash <= 0 || dash == body.Length - 1)
                return false;
            int start = dash;
            while (start > 0 && char.IsDigit(body[start - 1]))
                start--;
            if (start == dash || start == 0)
                return false;
            prefix = body.Substring(0, start);
            return int.TryParse(body.AsSpan(start, dash - start), NumberStyles.None, CultureInfo.InvariantCulture, out first)
                && int.TryParse(body.AsSpan(dash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out last)
                && last >= first;
        }

        /// <summary>
        /// Matches text against a pattern where * stands for any characters (none included).
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="pattern">
        /// The pattern.
        /// </param>
        /// <returns>
        /// True on a match.
        /// </returns>
        private static bool Glob(string text, string pattern)
        {
            int t = 0, p = 0, star = -1, mark = 0;
            while (t < text.Length)
            {
                if (p < pattern.Length && pattern[p] == text[t])
                {
                    t++;
                    p++;
                }
                else if (p < pattern.Length && pattern[p] == '*')
                {
                    star = p++;
                    mark = t;
                }
                else if (star >= 0)
                {
                    p = star + 1;
                    t = ++mark;
                }
                else
                    return false;
            }
            while (p < pattern.Length && pattern[p] == '*')
                p++;
            return p == pattern.Length;
        }

        private static BusMember Member(Net net) => new() { Net = net.Name, Active = net.Active == ActiveLevel.None ? null : net.Active };

        private int MemberIndex(BusLine bus, Net net)
        {
            for (int i = 0; i < bus.Members.Count; i++)
            {
                if (SheetLayout.FindMember(Netlist, bus.Members[i]) == net)
                    return i;
            }
            return -1;
        }

        private void CheckNotOnBus(Net net, BusLine? except)
        {
            BusLine? on = BusOf(net);
            if (on != null && on != except)
                throw new SheetEditException($"Signal {SheetLayout.NetKey(net)} is already on bus {on.Name}; a signal joins one bus only.");
        }

        private static void CheckPath(List<Point>? path)
        {
            for (int i = 1; path != null && i < path.Count; i++)
            {
                if (path[i].X != path[i - 1].X && path[i].Y != path[i - 1].Y)
                    throw new SheetEditException($"Bus path piece {i} is slanted; every piece must be level or upright.");
            }
        }
    }
}
