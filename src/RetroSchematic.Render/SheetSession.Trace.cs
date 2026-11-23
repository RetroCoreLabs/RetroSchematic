//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// One place on an instance box where the traced signal goes inside the block.
    /// </summary>
    public readonly struct TraceMark
    {
        /// <summary>
        /// Creates a mark.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="port">The port the signal goes through.</param>
        /// <param name="at">Where the port meets the box.</param>
        public TraceMark(string instance, string port, DrawPoint at)
        {
            Instance = instance;
            Port = port;
            At = at;
        }

        /// <summary>The instance name.</summary>
        public string Instance { get; }

        /// <summary>The port name.</summary>
        public string Port { get; }

        /// <summary>Where the port meets the box.</summary>
        public DrawPoint At { get; }
    }

    /// <summary>
    /// Tracing, the main way to read a sheet: a click on anything that belongs to a signal (a
    /// wire, a name tag, a pin, a bus entry, an instance's port) shows every place the signal
    /// goes. The window makes the traced items pulse with <see cref="PulseLevel"/>; which items
    /// they are is <see cref="SheetSession.TraceOf"/>.
    /// </summary>
    public sealed partial class SheetSession
    {
        private int _place = -1;

        /// <summary>
        /// How bright the traced items are at a moment: a smooth rise and fall once a second,
        /// from 0 (plain trace colour) to 1 (brightest, widest glow).
        /// </summary>
        /// <param name="seconds">
        /// Time in seconds, from any fixed start.
        /// </param>
        /// <returns>
        /// The level, 0 to 1.
        /// </returns>
        public static double PulseLevel(double seconds) => 0.5 - (0.5 * Math.Cos(2 * Math.PI * seconds));

        /// <summary>
        /// The signal a clicked item belongs to, as its key (BD3_0), or empty when the item is not
        /// part of a signal (a chip, a gate, the paper, a bus line).
        /// </summary>
        /// <param name="owner">
        /// The owner name of what was clicked.
        /// </param>
        /// <returns>
        /// The signal key, or empty.
        /// </returns>
        public string SignalAt(string owner)
        {
            if (owner.StartsWith("net:", StringComparison.Ordinal))
                return owner.Substring(4);
            if (owner.StartsWith("pin:", StringComparison.Ordinal))
            {
                string at = owner.Substring(4);
                int colon = at.IndexOf(':');
                if (colon < 0)
                {
                    Net? n = Editor.NetOf(at);
                    return n == null ? "" : SheetLayout.NetKey(n);
                }
                // an instance's port end: U1:PORT.0
                int dot = at.LastIndexOf('.');
                return PortSignal(at.Substring(0, colon), at.Substring(colon + 1, (dot < 0 ? at.Length : dot) - colon - 1));
            }
            if (owner.StartsWith("port:", StringComparison.Ordinal))
            {
                int colon = owner.IndexOf(':', 5);
                return colon < 0 ? "" : PortSignal(owner.Substring(5, colon - 5), owner.Substring(colon + 1));
            }
            return "";
        }

        /// <summary>
        /// Traces the signal a clicked item belongs to; anything else clears the trace.
        /// </summary>
        /// <param name="owner">
        /// The owner name of what was clicked.
        /// </param>
        public void TraceOwner(string owner) => Trace(SignalAt(owner));

        /// <summary>
        /// Every place on an instance box where the traced signal goes inside a block.
        /// </summary>
        /// <returns>
        /// The marks, empty when nothing is traced.
        /// </returns>
        public List<TraceMark> TracedPortMarks()
        {
            List<TraceMark> marks = new();
            foreach (string owner in _traceOwners)
            {
                if (!owner.StartsWith("port:", StringComparison.Ordinal))
                    continue;
                int colon = owner.IndexOf(':', 5);
                string key = owner.Substring(5);
                if (colon > 0 && Drawing.PortEdges.TryGetValue(key, out DrawPoint at))
                    marks.Add(new TraceMark(owner.Substring(5, colon - 5), owner.Substring(colon + 1), at));
            }
            marks.Sort((a, b) => string.CompareOrdinal(a.Instance + ":" + a.Port, b.Instance + ":" + b.Port));
            return marks;
        }

        /// <summary>
        /// Follows the traced signal into a block: when the clicked item is a port (or port end)
        /// of an instance that the traced signal goes through, the instance is opened and the
        /// signal inside it is traced. Otherwise nothing happens.
        /// </summary>
        /// <param name="owner">
        /// The owner name of what was double-clicked.
        /// </param>
        /// <returns>
        /// True when the trace was followed inside.
        /// </returns>
        public bool FollowAt(string owner)
        {
            if (Traced.Length == 0)
                return false;
            string instance, port;
            if (owner.StartsWith("port:", StringComparison.Ordinal))
            {
                int colon = owner.IndexOf(':', 5);
                if (colon < 0)
                    return false;
                instance = owner.Substring(5, colon - 5);
                port = owner.Substring(colon + 1);
            }
            else if (owner.StartsWith("pin:", StringComparison.Ordinal) && owner.IndexOf(':', 4) > 0)
            {
                int colon = owner.IndexOf(':', 4);
                int dot = owner.LastIndexOf('.');
                instance = owner.Substring(4, colon - 4);
                port = owner.Substring(colon + 1, (dot < colon ? owner.Length : dot) - colon - 1);
            }
            else
                return false;
            Net? outer = Editor.FindNetByKey(Traced);
            BlockInstance? inst = Hierarchy.FindInstance(Netlist.Instances, instance);
            BlockDefinition? b = inst == null ? null : Hierarchy.FindBlock(Editor.Root, inst.Block);
            if (outer == null || inst == null || b == null || inst.Connections == null)
                return false;
            for (int c = 0; c < inst.Connections.Count; c++)
            {
                PortConnection pc = inst.Connections[c];
                if (pc.Port != port)
                    continue;
                for (int k = 0; k < pc.Nets.Count; k++)
                {
                    if (NetlistReader.FindNet(Netlist.Nets, pc.Nets[k]) != outer)
                        continue;
                    for (int q = 0; q < b.Ports.Count; q++)
                    {
                        if (b.Ports[q].Name != port || k >= b.Ports[q].Signals.Count)
                            continue;
                        Net? inner = NetlistReader.FindNet(b.Nets, b.Ports[q].Signals[k]);
                        if (inner == null)
                            return false;
                        string key = SheetLayout.NetKey(inner);
                        OpenInstance(instance);
                        Trace(key);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// The next place the traced signal goes (its pins, then where it enters blocks), in
        /// turn, for "jump to the next place". Wraps round at either end.
        /// </summary>
        /// <param name="step">
        /// 1 for the next place, -1 for the one before.
        /// </param>
        /// <param name="at">
        /// The place, in drawing units.
        /// </param>
        /// <param name="what">
        /// What is there, such as 11D.3 or U2:BD_0, for the status bar.
        /// </param>
        /// <returns>
        /// False when nothing is traced or the signal has no drawn place.
        /// </returns>
        public bool NextTracePlace(int step, out DrawPoint at, out string what)
        {
            List<string> names = new();
            List<DrawPoint> points = new();
            List<string> pins = TracedPins();
            for (int i = 0; i < pins.Count; i++)
            {
                if (Drawing.Pins.TryGetValue(pins[i], out DrawPoint p))
                {
                    names.Add(pins[i]);
                    points.Add(p);
                }
            }
            List<TraceMark> marks = TracedPortMarks();
            for (int i = 0; i < marks.Count; i++)
            {
                names.Add(marks[i].Instance + ":" + marks[i].Port);
                points.Add(marks[i].At);
            }
            at = default;
            what = "";
            if (points.Count == 0)
                return false;
            _place = ((_place + step) % points.Count + points.Count) % points.Count;
            at = points[_place];
            what = names[_place];
            return true;
        }

        /// <summary>
        /// The signal an instance's single port is connected to on this level, as its key, or
        /// empty (also for a bus port: it carries many signals).
        /// </summary>
        private string PortSignal(string instance, string port)
        {
            BlockInstance? inst = Hierarchy.FindInstance(Netlist.Instances, instance);
            for (int c = 0; inst?.Connections != null && c < inst.Connections.Count; c++)
            {
                PortConnection pc = inst.Connections[c];
                if (pc.Port != port || pc.Nets.Count != 1)
                    continue;
                Net? n = NetlistReader.FindNet(Netlist.Nets, pc.Nets[0]);
                return n == null ? "" : SheetLayout.NetKey(n);
            }
            return "";
        }
    }
}
