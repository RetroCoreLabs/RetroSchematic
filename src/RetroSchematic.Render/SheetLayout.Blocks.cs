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
    /// Instances of blocks (format version 2), drawn the way Logisim draws a subcircuit: a box
    /// with the instance and block names, and a pin per port on its edges with the port name
    /// inside and, when switched on, an arrow for its direction. A single port on the left or
    /// right edge is a pin like a chip's: its signal gets a wire or a label. A bus port is drawn
    /// bus-thick and names the signals it is connected to.
    /// </summary>
    public static partial class SheetLayout
    {
        private const double PortSpacing = 48;

        /// <summary>
        /// The edge a port is drawn on: its own choice, else inputs left, outputs and inouts
        /// right, bus inputs on top and other bus ports at the bottom.
        /// </summary>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <returns>
        /// The edge.
        /// </returns>
        public static PortSide SideOf(BlockPort port)
        {
            if (port.Side != null)
                return port.Side.Value;
            if (port.Signals.Count > 1)
                return port.Direction == PortDirection.Input ? PortSide.Top : PortSide.Bottom;
            return port.Direction == PortDirection.Input ? PortSide.Left : PortSide.Right;
        }

        /// <summary>
        /// The name a port end has in the drawing's pin list: instance, colon, port, ".0" (so
        /// it reads as a pin reference whose part is "U1:BA0").
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="port">
        /// The port.
        /// </param>
        /// <returns>
        /// The name, such as U1:BA0.0.
        /// </returns>
        public static string PortEnd(string instance, string port) => instance + ":" + port + ".0";

        /// <summary>
        /// The direction arrow for a port pin as drawing steps: a filled-looking triangle half
        /// way along the pin line, pointing into the box for an input, out of it for an output,
        /// one each way for an inout.
        /// </summary>
        /// <param name="d">
        /// The drawing.
        /// </param>
        /// <param name="owner">
        /// Owner name for the steps.
        /// </param>
        /// <param name="direction">
        /// The port's direction.
        /// </param>
        /// <param name="edgeX">
        /// Where the pin line meets the box, X.
        /// </param>
        /// <param name="edgeY">
        /// Where the pin line meets the box, Y.
        /// </param>
        /// <param name="endX">
        /// The pin end, X.
        /// </param>
        /// <param name="endY">
        /// The pin end, Y.
        /// </param>
        public static void PortArrow(SchematicDrawing d, string owner, PortDirection direction, double edgeX, double edgeY, double endX, double endY)
        {
            double len = Math.Sqrt(((edgeX - endX) * (edgeX - endX)) + ((edgeY - endY) * (edgeY - endY)));
            if (len == 0)
                return;
            // unit step from the pin end towards the box
            double ux = (edgeX - endX) / len, uy = (edgeY - endY) / len;
            double mx = (edgeX + endX) / 2, my = (edgeY + endY) / 2;
            if (direction == PortDirection.Input || direction == PortDirection.Inout)
                Arrowhead(d, owner, mx + (ux * 3), my + (uy * 3), ux, uy);
            if (direction == PortDirection.Output || direction == PortDirection.Inout)
                Arrowhead(d, owner, mx - (ux * 3), my - (uy * 3), -ux, -uy);
        }

        private static void Arrowhead(SchematicDrawing d, string owner, double tipX, double tipY, double ux, double uy)
        {
            const double length = 4, half = 2.5;
            double bx = tipX - (ux * length), by = tipY - (uy * length);
            d.Items.Add(new DrawItem
            {
                Kind = DrawKind.Path,
                Role = DrawRole.PortArrow,
                Confidence = Confidence.Read,
                Owner = owner,
                Path = new[]
                {
                    new PathSegment(PathOp.MoveTo, tipX, tipY),
                    new PathSegment(PathOp.LineTo, bx - (uy * half), by + (ux * half)),
                    new PathSegment(PathOp.LineTo, bx + (uy * half), by - (ux * half)),
                    new PathSegment(PathOp.Close, tipX, tipY),
                },
            });
        }

        /// <summary>
        /// A copy of the sheet in which every connected single port on the left or right edge of
        /// an instance is one more pin of the signal it is connected to (named by
        /// <see cref="PortEnd"/>), so the router and the labels treat it like a chip pin. The
        /// sheet itself is returned when it has no instances.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <returns>
        /// The sheet, or the copy.
        /// </returns>
        private static Netlist WithPortEnds(Netlist netlist)
        {
            if (netlist.Instances == null || netlist.Instances.Count == 0)
                return netlist;
            Netlist copy = NetlistReader.Clone(netlist);
            for (int i = 0; i < copy.Instances!.Count; i++)
            {
                BlockInstance inst = copy.Instances[i];
                BlockDefinition? b = Hierarchy.FindBlock(copy, inst.Block);
                for (int c = 0; b != null && inst.Connections != null && c < inst.Connections.Count; c++)
                {
                    PortConnection pc = inst.Connections[c];
                    BlockPort? port = FindPort(b, pc.Port);
                    if (port == null || port.Signals.Count != 1 || pc.Nets.Count != 1)
                        continue;
                    PortSide side = SideOf(port);
                    if (side != PortSide.Left && side != PortSide.Right)
                        continue;
                    Net? n = NetlistReader.FindNet(copy.Nets, pc.Nets[0]);
                    n?.Pins.Add(new PinReference { At = PortEnd(inst.Name, port.Name), Confidence = Confidence.Read });
                }
            }
            return copy;
        }

        private static BlockPort? FindPort(BlockDefinition b, string name)
        {
            for (int i = 0; i < b.Ports.Count; i++)
            {
                if (b.Ports[i].Name == name)
                    return b.Ports[i];
            }
            return null;
        }

        /// <summary>
        /// Measures and places every instance box: at its position (pushed down past any chip it
        /// would cover), or, without one, in a row below everything else.
        /// </summary>
        private static List<InstancePlan> PlaceInstances(Netlist netlist, PartPlan[] parts, Dictionary<string, Net> netByPin, double bottom)
        {
            List<InstancePlan> list = new();
            double rowX = Margin, rowY = bottom + PartGap;
            for (int i = 0; netlist.Instances != null && i < netlist.Instances.Count; i++)
            {
                BlockInstance inst = netlist.Instances[i];
                BlockDefinition? b = Hierarchy.FindBlock(netlist, inst.Block);
                if (b == null)
                    continue;
                InstancePlan plan = Measure(inst, b, netlist, netByPin);
                if (inst.Position != null)
                {
                    plan.X = Snap(Math.Max(FromMm(inst.Position.X), Margin + plan.LeftReach));
                    plan.Y = Snap(Math.Max(FromMm(inst.Position.Y), Margin + 42 + plan.TopReach));
                    for (bool moved = true; moved;)
                    {
                        moved = false;
                        for (int j = 0; j < parts.Length; j++)
                        {
                            PartPlan p = parts[j];
                            if (plan.X - plan.LeftReach < p.X + p.Width + p.RightReach + PartGap && p.X - p.LeftReach < plan.X + plan.Width + plan.RightReach + PartGap
                                && plan.Y - plan.TopReach < p.Y + p.Height + PartGap && p.Y - 18 < plan.Y + plan.Height + plan.BottomReach + PartGap)
                            {
                                plan.Y = Snap(p.Y + p.Height + PartGap + plan.TopReach);
                                moved = true;
                            }
                        }
                    }
                }
                else
                {
                    plan.X = Snap(rowX + plan.LeftReach);
                    plan.Y = Snap(rowY + plan.TopReach);
                    rowX = plan.X + plan.Width + plan.RightReach + PartGap;
                }
                // pin ends were measured from the box's top left
                for (int k = 0; k < plan.Ports.Count; k++)
                {
                    PortPlan pp = plan.Ports[k];
                    pp.EdgeX += plan.X;
                    pp.EdgeY += plan.Y;
                    pp.EndX += plan.X;
                    pp.EndY += plan.Y;
                    if (pp.Pin != null)
                    {
                        pp.Pin.X = pp.EndX;
                        pp.Pin.Y = pp.EndY;
                        plan.Pins.Add(pp.Pin);
                    }
                }
                list.Add(plan);
            }
            return list;
        }

        private static InstancePlan Measure(BlockInstance inst, BlockDefinition b, Netlist netlist, Dictionary<string, Net> netByPin)
        {
            InstancePlan plan = new() { Instance = inst, Block = b };
            List<BlockPort>[] bySide = { new(), new(), new(), new() };
            for (int i = 0; i < b.Ports.Count; i++)
                bySide[(int)SideOf(b.Ports[i])].Add(b.Ports[i]);
            for (int s = 0; s < 4; s++)
            {
                List<BlockPort> ports = bySide[s];
                // a port's own place first, then the rest in port order
                ports.Sort((x, y) => (x.Order ?? int.MaxValue).CompareTo(y.Order ?? int.MaxValue));
            }
            double nameWidth = TextWidth(inst.Name + "  " + b.Name, "", LabelFont);
            double leftNames = 0, rightNames = 0;
            for (int i = 0; i < bySide[0].Count; i++)
                leftNames = Math.Max(leftNames, TextWidth(bySide[0][i].Name, "", PinFont));
            for (int i = 0; i < bySide[1].Count; i++)
                rightNames = Math.Max(rightNames, TextWidth(bySide[1][i].Name, "", PinFont));
            int across = Math.Max(bySide[2].Count, bySide[3].Count);
            plan.Width = Snap(Math.Max(Math.Max(72, nameWidth + 12), Math.Max(leftNames + rightNames + 24, (across + 1) * PortSpacing)));
            plan.Height = Snap(Math.Max(48, (Math.Max(bySide[0].Count, bySide[1].Count) + 1) * PinPitch + 12));
            plan.LeftReach = PinOut + 4;
            plan.RightReach = PinOut + 4;
            plan.TopReach = 18;
            plan.BottomReach = 6;
            for (int s = 0; s < 4; s++)
            {
                for (int i = 0; i < bySide[s].Count; i++)
                {
                    BlockPort port = bySide[s][i];
                    PortSide side = (PortSide)s;
                    PortPlan pp = new() { Port = port, Side = side };
                    switch (side)
                    {
                        case PortSide.Left:
                            pp.EdgeX = 0; pp.EdgeY = 12 + ((i + 1) * PinPitch); pp.EndX = -PinOut; pp.EndY = pp.EdgeY;
                            break;
                        case PortSide.Right:
                            pp.EdgeX = plan.Width; pp.EdgeY = 12 + ((i + 1) * PinPitch); pp.EndX = plan.Width + PinOut; pp.EndY = pp.EdgeY;
                            break;
                        case PortSide.Top:
                            pp.EdgeX = (i + 1) * PortSpacing; pp.EdgeY = 0; pp.EndX = pp.EdgeX; pp.EndY = -PinOut;
                            break;
                        default:
                            pp.EdgeX = (i + 1) * PortSpacing; pp.EdgeY = plan.Height; pp.EndX = pp.EdgeX; pp.EndY = plan.Height + PinOut;
                            break;
                    }
                    pp.Connected = Connected(inst, netlist, port);
                    if (port.Signals.Count == 1 && (side == PortSide.Left || side == PortSide.Right))
                    {
                        string at = PortEnd(inst.Name, port.Name);
                        netByPin.TryGetValue(at, out Net? net);
                        if (net != null)
                        {
                            pp.Pin = new PinPlan { At = at, Number = 0, Side = side == PortSide.Left ? -1 : 1, Owner = "inst:" + inst.Name, Net = net, Confidence = Confidence.Read };
                            double reach = PinOut + LabelWidth(net) + 4;
                            if (side == PortSide.Left)
                                plan.LeftReach = Math.Max(plan.LeftReach, reach);
                            else
                                plan.RightReach = Math.Max(plan.RightReach, reach);
                        }
                    }
                    else if (side == PortSide.Top)
                        plan.TopReach = Math.Max(plan.TopReach, PinOut + 18 + SignalFont);
                    else if (side == PortSide.Bottom)
                        plan.BottomReach = Math.Max(plan.BottomReach, PinOut + 6 + SignalFont);
                    plan.Ports.Add(pp);
                }
            }
            plan.LeftReach = Snap(plan.LeftReach);
            plan.RightReach = Snap(plan.RightReach);
            plan.TopReach = Snap(plan.TopReach);
            plan.BottomReach = Snap(plan.BottomReach);
            return plan;
        }

        /// <summary>
        /// What a port is connected to on this level, as short text: one signal's key, or for a
        /// bus port the first and last (BD0_0 .. BD15_0, 16); empty when not connected.
        /// </summary>
        private static string Connected(BlockInstance inst, Netlist netlist, BlockPort port)
        {
            for (int c = 0; inst.Connections != null && c < inst.Connections.Count; c++)
            {
                PortConnection pc = inst.Connections[c];
                if (pc.Port != port.Name || pc.Nets.Count == 0)
                    continue;
                Net? first = NetlistReader.FindNet(netlist.Nets, pc.Nets[0]);
                Net? last = NetlistReader.FindNet(netlist.Nets, pc.Nets[^1]);
                if (first == null || last == null)
                    return "";
                return pc.Nets.Count == 1 ? NetKey(first)
                    : NetKey(first) + " .. " + NetKey(last) + ", " + pc.Nets.Count.ToString(CultureInfo.InvariantCulture);
            }
            return "";
        }

        private static void DrawInstance(SchematicDrawing d, Netlist netlist, InstancePlan plan, LayoutOptions options)
        {
            string owner = "inst:" + plan.Instance.Name;
            d.InstanceFrames[plan.Instance.Name] = new PartFrame(plan.X, plan.Y, plan.Width, plan.Height);
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupStart, Owner = owner });
            d.Items.Add(new DrawItem
            {
                Kind = DrawKind.Rect, Role = DrawRole.PartOutline, Confidence = Confidence.Read, Owner = owner,
                X1 = plan.X, Y1 = plan.Y, X2 = plan.Width, Y2 = plan.Height,
            });
            DrawItem title = Text(d, DrawRole.PartLabel, Confidence.Read, owner, plan.X, plan.Y - 5,
                plan.Instance.Name + "  " + plan.Block.Name, LabelFont, TextAnchor.Start);
            title.Bold = true;
            for (int i = 0; i < plan.Ports.Count; i++)
            {
                PortPlan pp = plan.Ports[i];
                string portOwner = "port:" + plan.Instance.Name + ":" + pp.Port.Name;
                bool bus = pp.Port.Signals.Count > 1;
                d.Items.Add(new DrawItem
                {
                    Kind = DrawKind.Line, Role = bus ? DrawRole.Bus : DrawRole.PinLine, Confidence = Confidence.Read, Owner = portOwner,
                    X1 = pp.EdgeX, Y1 = pp.EdgeY, X2 = pp.EndX, Y2 = pp.EndY,
                });
                if (options.ShowPortArrows)
                    PortArrow(d, portOwner, pp.Port.Direction, pp.EdgeX, pp.EdgeY, pp.EndX, pp.EndY);
                // the port name inside the box, beside its pin
                string name = pp.Port.Name + (bus ? "[" + (pp.Port.Signals.Count - 1).ToString(CultureInfo.InvariantCulture) + ":0]" : "");
                switch (pp.Side)
                {
                    case PortSide.Left:
                        Text(d, DrawRole.PinName, Confidence.Read, portOwner, pp.EdgeX + 3, pp.EdgeY + 2.5, name, PinFont, TextAnchor.Start);
                        break;
                    case PortSide.Right:
                        Text(d, DrawRole.PinName, Confidence.Read, portOwner, pp.EdgeX - 3, pp.EdgeY + 2.5, name, PinFont, TextAnchor.End);
                        break;
                    case PortSide.Top:
                        Text(d, DrawRole.PinName, Confidence.Read, portOwner, pp.EdgeX, pp.EdgeY + 3 + PinFont, name, PinFont, TextAnchor.Middle);
                        break;
                    default:
                        Text(d, DrawRole.PinName, Confidence.Read, portOwner, pp.EdgeX, pp.EdgeY - 4, name, PinFont, TextAnchor.Middle);
                        break;
                }
                if (pp.Pin != null)
                {
                    PinDot(d, pp.Pin.At, pp.EndX, pp.EndY);
                    continue;
                }
                // a bus port or a top/bottom port: what it is connected to, written at its end
                if (pp.Connected.Length > 0)
                {
                    double ty = pp.Side == PortSide.Top ? pp.EndY - 4 : pp.Side == PortSide.Bottom ? pp.EndY + 4 + SignalFont : pp.EndY - 3;
                    TextAnchor a = pp.Side == PortSide.Left ? TextAnchor.End : pp.Side == PortSide.Right ? TextAnchor.Start : TextAnchor.Middle;
                    double tx = pp.Side == PortSide.Left ? pp.EndX - 3 : pp.Side == PortSide.Right ? pp.EndX + 3 : pp.EndX;
                    Text(d, DrawRole.SignalName, Confidence.Read, portOwner, tx, ty, pp.Connected, SignalFont, a);
                }
            }
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupEnd, Owner = owner });
        }

        /// <summary>
        /// One instance box as measured and placed.
        /// </summary>
        private sealed class InstancePlan
        {
            public BlockInstance Instance = null!;
            public BlockDefinition Block = null!;
            public List<PortPlan> Ports = new();
            public List<PinPlan> Pins = new();
            public double X, Y, Width, Height, LeftReach, RightReach, TopReach, BottomReach;
        }

        /// <summary>
        /// One port pin of an instance box: where it meets the box and where it ends.
        /// </summary>
        private sealed class PortPlan
        {
            public BlockPort Port = null!;
            public PortSide Side;
            public double EdgeX, EdgeY, EndX, EndY;
            public string Connected = "";
            public PinPlan? Pin;
        }
    }
}
