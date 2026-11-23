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
    /// Passive parts on the sheet: resistors, resistor networks, capacitors, diodes, straps and
    /// switch blocks (docs/FORMAT.md, "Passive parts"), and the supply symbol a pull-up's far end
    /// is drawn with.
    /// </summary>
    /// <remarks>
    /// ND-13.003.01 (the ND symbol manual) has no symbol for any of these parts. The "ND" style
    /// here draws them in the box form (a resistor is an oblong, a switch block has a frame round
    /// its levers); that is a choice made for this program, not read from an ND drawing. The modern
    /// style draws the zigzag resistor and the levers without a frame.
    /// </remarks>
    public static partial class SheetLayout
    {
        // the body of a two-pin part between its pin lines
        private const double PassiveWidth = 36;

        // a switch lever is wider: two contacts and the lever between them
        private const double LeverWidth = 48;

        /// <summary>
        /// Size and pins of a passive part. Two-pin parts lie level, pin 1 on the left. A resistor
        /// network has its common pin top left and one resistor per row to its pin on the right. A
        /// switch block has one lever per row, pin k on the left and pin 2n+1-k on the right.
        /// </summary>
        /// <param name="plan">
        /// The plan to fill.
        /// </param>
        /// <param name="part">
        /// The part.
        /// </param>
        /// <param name="netByPin">
        /// The signal on each pin.
        /// </param>
        private static void MeasurePassive(PartPlan plan, Part part, Dictionary<string, Net> netByPin)
        {
            PassiveKind kind = PassiveParts.KindOf(part.Type);
            int n = PassiveParts.CountOf(part.Type);
            string owner = "part:" + part.Ref;
            switch (kind)
            {
                case PassiveKind.ResistorNetwork:
                    plan.Width = Snap(PassiveWidth + 12);
                    plan.Height = Snap((n + 1) * PinPitch);
                    PassivePin(plan, part, 1, -1, -PinOut, PinPitch, owner, netByPin);
                    for (int k = 1; k <= n; k++)
                        PassivePin(plan, part, k + 1, 1, plan.Width + PinOut, k * PinPitch, owner, netByPin);
                    break;
                case PassiveKind.SwitchBlock:
                    plan.Width = Snap(LeverWidth);
                    plan.Height = Snap((n + 1) * PinPitch);
                    for (int k = 1; k <= n; k++)
                    {
                        PassiveParts.LeverPins(n, k, out int first, out int second);
                        PassivePin(plan, part, first, -1, -PinOut, k * PinPitch, owner, netByPin);
                        PassivePin(plan, part, second, 1, plan.Width + PinOut, k * PinPitch, owner, netByPin);
                    }
                    break;
                default:
                    plan.Width = Snap(PassiveWidth);
                    plan.Height = Snap(2 * PinPitch);
                    PassivePin(plan, part, 1, -1, -PinOut, PinPitch, owner, netByPin);
                    PassivePin(plan, part, 2, 1, plan.Width + PinOut, PinPitch, owner, netByPin);
                    break;
            }
        }

        private static void PassivePin(PartPlan plan, Part part, int pin, int side, double x, double y, string owner, Dictionary<string, Net> netByPin)
        {
            PinPlan p = AddPin(plan, part, pin, side, x, y, owner, netByPin);
            p.Passive = true;
        }

        /// <summary>
        /// True when one of a signal's drawn pins belongs to a passive part.
        /// </summary>
        private static bool HasPassivePin(Net net, Dictionary<string, PinPlan> pinPlans)
        {
            for (int j = 0; j < net.Pins.Count; j++)
            {
                if (pinPlans.TryGetValue(net.Pins[j].At, out PinPlan? p) && p.Passive)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Places the passive parts that have no position of their own: just below the chip they
        /// serve (<see cref="Part.Serves"/>), else below the first chip that shares a signal with
        /// them that is not a supply line, else below everything; each moved down past anything it
        /// would cover.
        /// </summary>
        private static void PlaceNearChips(Netlist netlist, PartPlan[] plans, bool[] nearChip, List<PartPlan> placed, double left)
        {
            double bottom = Margin + 42;
            for (int i = 0; i < placed.Count; i++)
                bottom = Math.Max(bottom, placed[i].Y + placed[i].Height + PartGap + 18);
            for (int i = 0; i < plans.Length; i++)
            {
                if (!nearChip[i])
                    continue;
                PartPlan? anchor = AnchorOf(netlist, plans, nearChip, plans[i].Part);
                if (anchor != null)
                {
                    plans[i].X = Snap(anchor.X);
                    plans[i].Y = Snap(anchor.Y + anchor.Height + PartGap);
                }
                else
                {
                    plans[i].X = Snap(Margin + left + plans[i].LeftReach);
                    plans[i].Y = Snap(bottom);
                }
                for (bool moved = true; moved;)
                {
                    moved = false;
                    for (int j = 0; j < placed.Count; j++)
                    {
                        if (Covers(plans[i], placed[j]))
                        {
                            plans[i].Y = Snap(placed[j].Y + placed[j].Height + PartGap + 18);
                            moved = true;
                        }
                    }
                }
                placed.Add(plans[i]);
            }
        }

        private static PartPlan? AnchorOf(Netlist netlist, PartPlan[] plans, bool[] nearChip, Part part)
        {
            string? target = part.Serves;
            if (target == null)
            {
                // the first chip on a signal of this part that is not a supply line
                string prefix = part.Ref + ".";
                for (int i = 0; target == null && i < netlist.Nets.Count; i++)
                {
                    Net net = netlist.Nets[i];
                    if (SupplyNets.IsSupply(net.Name))
                        continue;
                    bool mine = false;
                    for (int j = 0; j < net.Pins.Count && !mine; j++)
                        mine = net.Pins[j].At.StartsWith(prefix, StringComparison.Ordinal) && net.Pins[j].PartRef == part.Ref;
                    for (int j = 0; mine && target == null && j < net.Pins.Count; j++)
                    {
                        string other = net.Pins[j].PartRef;
                        if (other != part.Ref && !ConnectorPins.IsConnector(other))
                            target = other;
                    }
                }
            }
            for (int i = 0; target != null && i < plans.Length; i++)
            {
                if (!nearChip[i] && plans[i].Part.Ref == target)
                    return plans[i];
            }
            return null;
        }

        /// <summary>
        /// Draws a passive part: its reference (muted) and value (bold) above it, its symbol in
        /// the chosen style, and its pin lines, numbers and dots.
        /// </summary>
        private static void DrawPassive(SchematicDrawing d, PartPlan plan, SymbolStyle style, LayoutOptions options)
        {
            Part part = plan.Part;
            string owner = "part:" + part.Ref;
            Confidence c = part.Confidence;
            PassiveKind kind = PassiveParts.KindOf(part.Type);
            bool nd = style == SymbolStyle.NorskData;
            double x0 = plan.X, x1 = plan.X + plan.Width, y0 = plan.Y;

            // no chip frame: the symbol is the part (picking uses the part's frame all the same)
            Text(d, DrawRole.BoardPosition, c, owner, x0, y0 - 5, part.Ref, LabelFont, TextAnchor.Start);
            string value = part.Value ?? (kind == PassiveKind.SwitchBlock || kind == PassiveKind.ResistorNetwork ? part.Type : "");
            if (value.Length > 0)
            {
                DrawItem v = Text(d, DrawRole.PartLabel, part.ValueConfidence ?? c, owner, x0 + TextWidth(part.Ref + " ", "", LabelFont), y0 - 5, value, LabelFont, TextAnchor.Start);
                v.Bold = true;
            }

            switch (kind)
            {
                case PassiveKind.Resistor:
                    ResistorBody(d, c, owner, x0, x1, y0 + PinPitch, nd);
                    break;
                case PassiveKind.ResistorNetwork:
                    {
                        int n = PassiveParts.CountOf(part.Type);
                        double common = x0 + 6;
                        // the frame first: it is filled, so it goes under the resistors
                        if (nd)
                            d.Items.Add(new DrawItem { Kind = DrawKind.Rect, Role = DrawRole.PartOutline, Confidence = c, Owner = owner, X1 = x0, Y1 = y0, X2 = plan.Width, Y2 = plan.Height, Dashed = c != Confidence.Read });
                        // the common pin joins a line down the left; each resistor leaves it
                        Line(d, DrawRole.Symbol, c, owner, x0, y0 + PinPitch, common, y0 + PinPitch);
                        Line(d, DrawRole.Symbol, c, owner, common, y0 + PinPitch, common, y0 + (n * PinPitch));
                        for (int k = 1; k <= n; k++)
                            ResistorBody(d, c, owner, common, x1, y0 + (k * PinPitch), nd);
                        break;
                    }
                case PassiveKind.Capacitor:
                case PassiveKind.CapacitorPolarised:
                    {
                        double m = (x0 + x1) / 2, y = y0 + PinPitch;
                        Line(d, DrawRole.Symbol, c, owner, x0, y, m - 3, y);
                        Line(d, DrawRole.Symbol, c, owner, m + 3, y, x1, y);
                        Line(d, DrawRole.Symbol, c, owner, m - 3, y - 7, m - 3, y + 7);
                        if (kind == PassiveKind.CapacitorPolarised && !nd)
                        {
                            // the minus plate curved, as modern drawings show it
                            Path(d, c, owner, new[]
                            {
                                new PathSegment(PathOp.MoveTo, m + 5, y - 7),
                                new PathSegment(PathOp.QuadTo, m + 1, y, m + 5, y + 7),
                            });
                        }
                        else
                        {
                            Line(d, DrawRole.Symbol, c, owner, m + 3, y - 7, m + 3, y + 7);
                        }
                        if (kind == PassiveKind.CapacitorPolarised)
                            Text(d, DrawRole.Symbol, c, owner, m - 10, y - 4, "+", 8, TextAnchor.Middle);
                        break;
                    }
                case PassiveKind.Diode:
                    {
                        double m = (x0 + x1) / 2, y = y0 + PinPitch;
                        Line(d, DrawRole.Symbol, c, owner, x0, y, m - 5, y);
                        Line(d, DrawRole.Symbol, c, owner, m + 5, y, x1, y);
                        d.Items.Add(new DrawItem
                        {
                            Kind = DrawKind.Path,
                            Role = DrawRole.Symbol,
                            Confidence = c,
                            Owner = owner,
                            Path = new[]
                            {
                                new PathSegment(PathOp.MoveTo, m - 5, y - 6),
                                new PathSegment(PathOp.LineTo, m + 5, y),
                                new PathSegment(PathOp.LineTo, m - 5, y + 6),
                                new PathSegment(PathOp.Close, m - 5, y - 6),
                            },
                        });
                        Line(d, DrawRole.Symbol, c, owner, m + 5, y - 6, m + 5, y + 6);
                        break;
                    }
                case PassiveKind.Strap:
                    {
                        double y = y0 + PinPitch;
                        Line(d, DrawRole.Symbol, c, owner, x0, y, x0 + 6, y);
                        Line(d, DrawRole.Symbol, c, owner, x1 - 6, y, x1, y);
                        Bubble(d, c, owner, x0 + 9, y);
                        Bubble(d, c, owner, x1 - 9, y);
                        // the link itself, an arch from one post to the other
                        Path(d, c, owner, new[]
                        {
                            new PathSegment(PathOp.MoveTo, x0 + 9, y - 3),
                            new PathSegment(PathOp.QuadTo, (x0 + x1) / 2, y - 12, x1 - 9, y - 3),
                        });
                        break;
                    }
                case PassiveKind.SwitchBlock:
                    {
                        int n = PassiveParts.CountOf(part.Type);
                        // the frame first: it is filled, so it goes under the levers
                        if (nd)
                            d.Items.Add(new DrawItem { Kind = DrawKind.Rect, Role = DrawRole.PartOutline, Confidence = c, Owner = owner, X1 = x0 + 6, Y1 = y0 + 2, X2 = plan.Width - 12, Y2 = plan.Height - 4, Dashed = c != Confidence.Read });
                        for (int k = 1; k <= n; k++)
                        {
                            double y = y0 + (k * PinPitch);
                            double a = x0 + 14, b = x1 - 14;
                            Line(d, DrawRole.Symbol, c, owner, x0, y, a - 2, y);
                            Line(d, DrawRole.Symbol, c, owner, b + 2, y, x1, y);
                            d.Items.Add(new DrawItem { Kind = DrawKind.Circle, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = a, Y1 = y, X2 = 2 });
                            d.Items.Add(new DrawItem { Kind = DrawKind.Circle, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = b, Y1 = y, X2 = 2 });
                            // the lever, drawn open: the setting is in the switches section
                            Line(d, DrawRole.Symbol, c, owner, a + 2, y, b, y - 7);
                        }
                        break;
                    }
            }

            for (int i = 0; i < plan.Pins.Count; i++)
            {
                PinPlan p = plan.Pins[i];
                double inner = p.Side < 0 ? x0 : x1;
                Line(d, DrawRole.PinLine, c, owner, inner, p.Y, p.X, p.Y);
                if (options.ShowPinNumbers)
                {
                    double nx = p.Side < 0 ? p.X + 1 : x1 + 1;
                    Text(d, DrawRole.PinNumber, c, owner, nx, p.Y - 2, p.Number.ToString(CultureInfo.InvariantCulture), PinFont, TextAnchor.Start);
                }
                PinDot(d, p.At, p.X, p.Y);
            }
        }

        // one resistor lying level from x0 to x1 at height y: an oblong (ND style) or a zigzag
        private static void ResistorBody(SchematicDrawing d, Confidence c, string owner, double x0, double x1, double y, bool nd)
        {
            double a = x0 + 6, b = x1 - 6;
            Line(d, DrawRole.Symbol, c, owner, x0, y, a, y);
            Line(d, DrawRole.Symbol, c, owner, b, y, x1, y);
            if (nd)
            {
                d.Items.Add(new DrawItem { Kind = DrawKind.Rect, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = a, Y1 = y - 3.5, X2 = b - a, Y2 = 7 });
                return;
            }
            const int teeth = 6;
            PathSegment[] zig = new PathSegment[teeth + 2];
            zig[0] = new PathSegment(PathOp.MoveTo, a, y);
            double step = (b - a) / teeth;
            for (int t = 0; t < teeth; t++)
                zig[t + 1] = new PathSegment(PathOp.LineTo, a + ((t + 0.5) * step), y + ((t % 2 == 0) ? -4 : 4));
            zig[teeth + 1] = new PathSegment(PathOp.LineTo, b, y);
            Path(d, c, owner, zig);
        }

        /// <summary>
        /// The supply symbol at a passive part's pin on a supply line: a short stub, then for a
        /// high supply (+5V, H) an upright line to a bar with the supply's name above it; for
        /// ground, the three shortening bars, turned to point away from the part. It belongs to the signal, so it is picked and
        /// traced like a net label.
        /// </summary>
        private static void DrawSupply(SchematicDrawing d, PinPlan p, Confidence c)
        {
            Net net = p.Net!;
            string owner = "net:" + NetKey(net);
            double x = p.X + (p.Side * 6), y = p.Y;
            Line(d, DrawRole.PinLine, c, owner, p.X, y, x, y);
            if (SupplyNets.IsGround(net.Name))
            {
                // turned to point away from the part, so it fits in one pin row (rows are 12
                // apart) and a switch block's grounded levers do not run into each other
                double s = p.Side;
                Line(d, DrawRole.Symbol, c, owner, x, y - 5, x, y + 5);
                Line(d, DrawRole.Symbol, c, owner, x + (s * 2.5), y - 3.5, x + (s * 2.5), y + 3.5);
                Line(d, DrawRole.Symbol, c, owner, x + (s * 5), y - 1.5, x + (s * 5), y + 1.5);
                return;
            }
            Line(d, DrawRole.Symbol, c, owner, x, y, x, y - 8);
            Line(d, DrawRole.Symbol, c, owner, x - 6, y - 8, x + 6, y - 8);
            DrawItem t = Text(d, DrawRole.SignalName, c, owner, x, y - 11, net.Name, SignalFont, TextAnchor.Middle);
            SetName(t, net, d.NameStyle);
        }
    }
}
