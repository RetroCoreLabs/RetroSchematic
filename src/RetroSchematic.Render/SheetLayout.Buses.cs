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
    /// Virtual buses (layout.buses): a named thick line that a group of signals join. Drawing
    /// only - the nets list is never read differently because of a bus.
    /// </summary>
    public static partial class SheetLayout
    {
        /// <summary>
        /// The signal a bus member names, or null when the sheet has no such signal.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The signal, or null.
        /// </returns>
        public static Net? FindMember(Netlist netlist, BusMember member)
        {
            for (int i = 0; i < netlist.Nets.Count; i++)
            {
                Net n = netlist.Nets[i];
                if (n.Name == member.Net && (member.Active == null || member.Active == n.Active))
                    return n;
            }
            return null;
        }

        /// <summary>
        /// The signals that are members of a bus with a path (a bus without one is not drawn, so
        /// its signals are drawn as usual).
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <returns>
        /// The member signals.
        /// </returns>
        private static HashSet<Net> BusMembers(Netlist netlist)
        {
            HashSet<Net> set = new(ReferenceEqualityComparer.Instance);
            List<BusLine>? buses = netlist.Layout?.Buses;
            for (int b = 0; buses != null && b < buses.Count; b++)
            {
                if (buses[b].Path.Count < 2)
                    continue;
                for (int m = 0; m < buses[b].Members.Count; m++)
                {
                    Net? n = FindMember(netlist, buses[b].Members[m]);
                    if (n != null)
                        set.Add(n);
                }
            }
            return set;
        }

        /// <summary>
        /// Draws every bus that has a path: the thick line, its name at the start, and for each
        /// drawn pin of each member signal a wire from the pin to a point one grid step beside
        /// the bus, a 45-degree piece onto the bus, and the signal's name with its subscript at
        /// that entry. A stored path through a chip is routed afresh between its two ends. A pin
        /// that cannot reach the bus keeps its ordinary label, and the reason is kept in
        /// <see cref="SchematicDrawing.BusProblems"/>.
        /// </summary>
        /// <param name="d">
        /// The drawing.
        /// </param>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="router">
        /// The router, already told about every frame, text and label room.
        /// </param>
        /// <param name="netNumber">
        /// Each signal's number on the router.
        /// </param>
        /// <param name="pinPlans">
        /// Every drawn pin.
        /// </param>
        /// <param name="busPins">
        /// Filled with the pins that joined a bus.
        /// </param>
        private static void DrawBuses(SchematicDrawing d, Netlist netlist, WireRouter router, Dictionary<Net, int> netNumber,
            Dictionary<string, PinPlan> pinPlans, HashSet<string> busPins)
        {
            List<BusLine>? buses = netlist.Layout?.Buses;
            List<double[]> segments = new();
            for (int b = 0; buses != null && b < buses.Count; b++)
            {
                BusLine bus = buses[b];
                // a number on the router different from every signal's
                int busNumber = netlist.Nets.Count + 1 + b;
                if (bus.Path.Count < 2)
                {
                    d.BusProblems.Add(bus.Name + ": no path is set, so the bus is not drawn");
                    continue;
                }
                List<DrawPoint> points = new();
                for (int i = 0; i < bus.Path.Count; i++)
                    points.Add(new DrawPoint(FromMm(bus.Path[i].X), FromMm(bus.Path[i].Y)));
                if (!router.LayPath(busNumber, points))
                {
                    string why = router.Refusal;
                    List<DrawPoint> fresh = new();
                    if (!router.FindPath(busNumber, points[0], points[^1], fresh) || !router.LayPath(busNumber, fresh))
                    {
                        // its members were left out of the wire list; their pins keep labels
                        d.BusProblems.Add(bus.Name + ": the stored path is not used (" + why + ") and no fresh path was found, so the bus is not drawn");
                        continue;
                    }
                    d.BusProblems.Add(bus.Name + ": the stored path is not used (" + why + "); it was routed afresh between its two ends");
                    points = fresh;
                }
                d.BusPaths[bus.Name] = points;
                string owner = "bus:" + bus.Name;
                for (int i = 1; i < points.Count; i++)
                {
                    d.Items.Add(new DrawItem
                    {
                        Kind = DrawKind.Line, Role = DrawRole.Bus, Confidence = Confidence.Read, Owner = owner,
                        X1 = points[i - 1].X, Y1 = points[i - 1].Y, X2 = points[i].X, Y2 = points[i].Y,
                    });
                }
                DrawItem title = Text(d, DrawRole.Bus, Confidence.Read, owner, points[0].X + 3, points[0].Y - 4, bus.Name, SignalFont, TextAnchor.Start);
                title.Bold = true;
                router.Block(points[0].X + 3, points[0].Y - 4 - SignalFont, points[0].X + 3 + TextWidth(bus.Name, "", SignalFont), points[0].Y - 2);

                double widest = 0;
                List<PinPlan> pins = new();
                List<Net> pinNet = new();
                for (int m = 0; m < bus.Members.Count; m++)
                {
                    Net? n = FindMember(netlist, bus.Members[m]);
                    if (n == null)
                    {
                        d.BusProblems.Add(bus.Name + ": member " + bus.Members[m].Net + " is not a signal on this sheet");
                        continue;
                    }
                    widest = Math.Max(widest, TextWidth(n.Name, Subscript(n), SignalFont));
                    for (int j = 0; j < n.Pins.Count; j++)
                    {
                        if (pinPlans.TryGetValue(n.Pins[j].At, out PinPlan? p))
                        {
                            pins.Add(p);
                            pinNet.Add(n);
                        }
                    }
                }
                List<BusSlot> slots = Slots(points, widest);
                for (int i = 0; i < pins.Count; i++)
                {
                    if (!JoinBus(d, router, netNumber[pinNet[i]], pinNet[i], pins[i], slots, segments))
                    {
                        d.BusProblems.Add(bus.Name + ": pin " + pins[i].At + " of " + NetKey(pinNet[i]) + " could not reach the bus; it keeps its label");
                        continue;
                    }
                    busPins.Add(pins[i].At);
                }
            }
        }

        /// <summary>
        /// The places along a bus where a signal can join it: every two grid steps on an upright
        /// piece, and on a level piece far enough apart for the longest name to fit between.
        /// The first three steps (room for the bus name) and the last two of each piece are left free.
        /// </summary>
        /// <param name="points">
        /// The bus's bend points.
        /// </param>
        /// <param name="widest">
        /// The widest member name with subscript.
        /// </param>
        /// <returns>
        /// The places, each usable once on each side.
        /// </returns>
        private static List<BusSlot> Slots(List<DrawPoint> points, double widest)
        {
            List<BusSlot> slots = new();
            double s = WireRouter.Step;
            for (int i = 1; i < points.Count; i++)
            {
                DrawPoint a = points[i - 1], b = points[i];
                bool upright = a.X == b.X;
                double length = upright ? Math.Abs(b.Y - a.Y) : Math.Abs(b.X - a.X);
                double pitch = upright ? 2 * s : Snap(widest + (3 * s));
                double dir = upright ? Math.Sign(b.Y - a.Y) : Math.Sign(b.X - a.X);
                for (double t = 3 * s; t <= length - (2 * s); t += pitch)
                {
                    slots.Add(new BusSlot
                    {
                        X = upright ? a.X : a.X + (dir * t),
                        Y = upright ? a.Y + (dir * t) : a.Y,
                        Upright = upright,
                    });
                }
            }
            return slots;
        }

        /// <summary>
        /// Routes one pin to the nearest free entry of a bus and draws the wire, the 45-degree
        /// piece and the name. Tries the three nearest entries.
        /// </summary>
        /// <param name="d">
        /// The drawing.
        /// </param>
        /// <param name="router">
        /// The router.
        /// </param>
        /// <param name="number">
        /// The signal's number on the router.
        /// </param>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <param name="p">
        /// The pin.
        /// </param>
        /// <param name="slots">
        /// The bus's entries; each one tried is marked taken on that side.
        /// </param>
        /// <param name="segments">
        /// Scratch list for the route.
        /// </param>
        /// <returns>
        /// True when the pin joined the bus.
        /// </returns>
        private static bool JoinBus(SchematicDrawing d, WireRouter router, int number, Net net, PinPlan p, List<BusSlot> slots, List<double[]> segments)
        {
            double s = WireRouter.Step;
            double w = TextWidth(net.Name, Subscript(net), SignalFont);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                int best = -1, bestSide = 0;
                double bestCost = double.MaxValue;
                for (int i = 0; i < slots.Count; i++)
                {
                    BusSlot slot = slots[i];
                    int side = slot.Upright ? (p.X < slot.X ? -1 : 1) : (p.Y < slot.Y ? -1 : 1);
                    if (side < 0 ? slot.TakenBefore : slot.TakenAfter)
                        continue;
                    double ax = slot.Upright ? slot.X + (side * s) : slot.X - s;
                    double ay = slot.Upright ? slot.Y - s : slot.Y + (side * s);
                    // the entry point and the room for the name must both still be free
                    NameAt(slot.Upright, side, ax, ay, w, out double nx, out double ny);
                    if (!router.IsFree(ax, ay, ax, ay) || !router.IsFree(nx - 3, ny - SignalFont - 2, nx + w + 3, ny + (0.3 * SignalFont) + 2))
                    {
                        if (side < 0)
                            slot.TakenBefore = true;
                        else
                            slot.TakenAfter = true;
                        continue;
                    }
                    double cost = Math.Abs(p.X - ax) + Math.Abs(p.Y - ay);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        best = i;
                        bestSide = side;
                    }
                }
                if (best < 0)
                    return false;
                BusSlot e = slots[best];
                // taken whether or not the route works: a failed entry is not tried again
                if (bestSide < 0)
                    e.TakenBefore = true;
                else
                    e.TakenAfter = true;
                DrawPoint a = e.Upright ? new DrawPoint(e.X + (bestSide * s), e.Y - s) : new DrawPoint(e.X - s, e.Y + (bestSide * s));
                // the name's room (with a little margin) is closed to every wire
                NameAt(e.Upright, bestSide, a.X, a.Y, w, out double tx, out double ty);
                router.Block(tx - 3, ty - SignalFont - 2, tx + w + 3, ty + (0.3 * SignalFont) + 2);
                router.Block(e.X, e.Y, e.X, e.Y);
                if (!router.RouteTo(number, new DrawPoint(p.X, p.Y), p.Side, a, 420, segments))
                    continue;
                string owner = "net:" + NetKey(net);
                Confidence c = PinConfidence(net, p.At);
                for (int i = 0; i < segments.Count; i++)
                {
                    double[] g = segments[i];
                    d.Items.Add(new DrawItem
                    {
                        Kind = DrawKind.Line, Role = DrawRole.Wire, Confidence = c, Owner = owner,
                        X1 = g[0], Y1 = g[1], X2 = g[2], Y2 = g[3], Dashed = c == Confidence.Open,
                    });
                }
                // the 45-degree entry onto the bus
                d.Items.Add(new DrawItem
                {
                    Kind = DrawKind.Line, Role = DrawRole.Wire, Confidence = c, Owner = owner,
                    X1 = a.X, Y1 = a.Y, X2 = e.X, Y2 = e.Y, Dashed = c == Confidence.Open,
                });
                DrawItem t = Text(d, DrawRole.SignalName, c, owner, tx, ty, net.Name, SignalFont, TextAnchor.Start);
                t.Subscript = Subscript(net);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Where the name at a bus entry is written: beside the entry on the pin's side, clear of
        /// the row (upright bus) or column (level bus) the wire arrives on.
        /// </summary>
        /// <param name="upright">
        /// True for an upright piece of bus.
        /// </param>
        /// <param name="side">
        /// -1 left of / above the bus, +1 right of / below it.
        /// </param>
        /// <param name="ax">
        /// Entry point X (where the wire ends and the 45-degree piece starts).
        /// </param>
        /// <param name="ay">
        /// Entry point Y.
        /// </param>
        /// <param name="w">
        /// The name's width with its subscript.
        /// </param>
        /// <param name="tx">
        /// The text's left edge.
        /// </param>
        /// <param name="ty">
        /// The text's baseline.
        /// </param>
        private static void NameAt(bool upright, int side, double ax, double ay, double w, out double tx, out double ty)
        {
            if (upright)
            {
                tx = side < 0 ? ax - 4 - w : ax + 4;
                ty = ay - 4;
            }
            else
            {
                tx = ax - 4 - w;
                ty = side < 0 ? ay - 4 : ay + 4 + SignalFont;
            }
        }

        /// <summary>
        /// One place along a bus where a signal can join it, from either side.
        /// </summary>
        private sealed class BusSlot
        {
            public double X, Y;
            public bool Upright;
            public bool TakenBefore, TakenAfter;
        }
    }
}
