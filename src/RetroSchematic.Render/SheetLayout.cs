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
    /// Which symbol set the sheet is drawn with.
    /// </summary>
    public enum SymbolStyle
    {
        /// <summary>
        /// Norsk Data's own symbols, from ND-13.003.01 "Circuit Diagram Symbols" (July 1976):
        /// a plain box is NAND, a box with an X is NOR, a box with a diagonal and a small
        /// triangle is an inverter, a NAND box followed by the inverter's triangle is AND.
        /// </summary>
        NorskData,

        /// <summary>
        /// The usual distinctive-shape symbols (IEEE Std 91 shapes).
        /// </summary>
        Modern,
    }

    /// <summary>
    /// What the layout shows besides the parts, gates and wires.
    /// </summary>
    public sealed class LayoutOptions
    {
        /// <summary>
        /// Write the small pin number beside each pin line.
        /// </summary>
        public bool ShowPinNumbers { get; set; } = true;

        /// <summary>
        /// Write the board position (13D) in front of the chip type on each part label.
        /// </summary>
        public bool ShowBoardPositions { get; set; } = true;

        /// <summary>
        /// The chip catalogue, used to draw every pin of a chip that has no gates; null draws only
        /// the pins that are on a signal.
        /// </summary>
        public ChipCatalog? Catalog { get; set; }

        /// <summary>
        /// Draw an arrow on every port pin of an instance box: into the box for an input, out
        /// for an output, both ways for an inout.
        /// </summary>
        public bool ShowPortArrows { get; set; } = true;
    }

    /// <summary>
    /// Lays a netlist sheet out as a <see cref="SchematicDrawing"/>, the way the ND drawings do it.
    /// <list type="bullet">
    /// <item><description>A chip is a frame round its gate symbols, stacked one under the other.
    /// Every pin is a short line out of the frame; its number is written on that line. A chip with
    /// no gates read is drawn as a box with its pins on two sides (DIP order) and the datasheet pin
    /// names inside.</description></item>
    /// <item><description>A chip sits at its board position (column number, row letter) on a grid
    /// whose columns and rows are as wide as the largest chip in them, with room between them for
    /// wires. A chip with a hand-set <c>position</c> (or whose gates all have one) sits there instead,
    /// pushed down if it would cover another.</description></item>
    /// <item><description>THE WIRE RULE. A signal is drawn as a wire only when it is short and
    /// local: every one of its pins is drawn on this sheet (none on the backplane or front
    /// connector), it has at most <see cref="MaxWiredPins"/> pins, and its pins all lie within
    /// <see cref="WireReach"/> drawing units of each other across and down. Every other signal,
    /// and a short one the router cannot lay, gets at each pin a short stub ending in the ND
    /// net-label diamond with the signal's name and its 0/1 subscript.</description></item>
    /// <item><description>Wires are laid by <see cref="WireRouter"/> on a grid, round every chip
    /// and gate frame, every text, and the room kept for every label, so a wire never crosses a
    /// chip, never runs on top of another wire, and no text overlaps another.</description></item>
    /// </list>
    /// </summary>
    public static partial class SheetLayout
    {
        /// <summary>
        /// Drawing units per millimetre of a measured position. One millimetre is one router grid step.
        /// </summary>
        public const double UnitsPerMm = WireRouter.Step;

        /// <summary>
        /// The empty border round the sheet, in drawing units.
        /// </summary>
        public const double Margin = 48;

        /// <summary>
        /// A signal with more pins than this is never drawn as a wire.
        /// </summary>
        public const int MaxWiredPins = 6;

        /// <summary>
        /// A signal whose pins spread further than this (drawing units, across or down) is never
        /// drawn as a wire.
        /// </summary>
        public const double WireReach = 420;

        /// <summary>
        /// Font size of signal names.
        /// </summary>
        public const double SignalFont = 9;

        private const double PinFont = 7;
        private const double LabelFont = 10;
        private const double GateWidth = 36;
        private const double AndTail = 12;
        private const double PinPitch = 12;
        private const double PinOut = 12;
        private const double FramePad = 6;
        private const double GateGap = 24;
        private const double Diamond = 8;
        private const double LabelOffset = 3;
        private const double PartGap = 48;
        private const double PinDotRadius = 2;
        private const double CharWidth = 0.6;

        /// <summary>
        /// Lay out a whole sheet with pin numbers and board positions shown.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="style">
        /// ND 1976 or modern symbols.
        /// </param>
        /// <returns>
        /// The sheet as drawing steps.
        /// </returns>
        public static SchematicDrawing Build(Netlist netlist, SymbolStyle style) => Build(netlist, style, new LayoutOptions());

        /// <summary>
        /// Lay out a whole sheet.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="style">
        /// ND 1976 or modern symbols.
        /// </param>
        /// <param name="options">
        /// What to show.
        /// </param>
        /// <returns>
        /// The sheet as drawing steps.
        /// </returns>
        public static SchematicDrawing Build(Netlist netlist, SymbolStyle style, LayoutOptions options)
        {
            // an instance's port ends take part in the drawing as pins of their signals; a copy
            // of the sheet carries them, so the sheet itself is never changed
            netlist = WithPortEnds(netlist);
            Dictionary<string, Net> netByPin = new(StringComparer.Ordinal);
            Dictionary<string, PinReference> pinRef = new(StringComparer.Ordinal);
            for (int i = 0; i < netlist.Nets.Count; i++)
            {
                Net net = netlist.Nets[i];
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    netByPin[net.Pins[j].At] = net;
                    pinRef[net.Pins[j].At] = net.Pins[j];
                }
            }

            // 1. measure every chip
            PartPlan[] plans = new PartPlan[netlist.Parts.Count];
            for (int i = 0; i < plans.Length; i++)
                plans[i] = Measure(netlist, netlist.Parts[i], style, options, netByPin);

            // 2. place them
            Place(netlist, plans);
            double right = 400, bottom = 200;
            for (int i = 0; i < plans.Length; i++)
            {
                right = Math.Max(right, plans[i].X + plans[i].Width + plans[i].RightReach);
                bottom = Math.Max(bottom, plans[i].Y + plans[i].Height + 12);
            }
            // instance boxes: at their position, or in a row below everything else
            List<InstancePlan> boxes = PlaceInstances(netlist, plans, netByPin, bottom);
            for (int i = 0; i < boxes.Count; i++)
            {
                right = Math.Max(right, boxes[i].X + boxes[i].Width + boxes[i].RightReach);
                bottom = Math.Max(bottom, boxes[i].Y + boxes[i].Height + boxes[i].BottomReach + 12);
            }
            SchematicDrawing d = new(Snap(right + Margin), Snap(bottom + Margin));
            Text(d, DrawRole.Title, Confidence.Read, "", Margin, 24,
                netlist.Sheet.Id + "  " + netlist.Sheet.Title + "  (" + (style == SymbolStyle.NorskData ? "ND symbols" : "modern symbols") + ")",
                16, TextAnchor.Start);

            // 3. which signals get a wire
            Dictionary<string, PinPlan> pinPlans = new(StringComparer.Ordinal);
            for (int i = 0; i < plans.Length; i++)
            {
                for (int j = 0; j < plans[i].Pins.Count; j++)
                {
                    PinPlan p = plans[i].Pins[j];
                    p.X += plans[i].X;
                    p.Y += plans[i].Y;
                    pinPlans[p.At] = p;
                }
            }
            for (int i = 0; i < boxes.Count; i++)
            {
                for (int j = 0; j < boxes[i].Pins.Count; j++)
                    pinPlans[boxes[i].Pins[j].At] = boxes[i].Pins[j];
            }
            // signals on a virtual bus join the bus instead of getting their own wire
            HashSet<Net> onBus = BusMembers(netlist);
            List<Net> candidates = WireCandidates(netlist, pinPlans);
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (onBus.Contains(candidates[i]))
                    candidates.RemoveAt(i);
            }

            // 4. draw chips (labels for every signal are drawn later, once routing has decided)
            for (int i = 0; i < plans.Length; i++)
            {
                d.Frames[plans[i].Part.Ref] = new PartFrame(plans[i].X, plans[i].Y, plans[i].Width, plans[i].Height);
                DrawPart(d, plans[i], style, options);
            }
            for (int i = 0; i < boxes.Count; i++)
                DrawInstance(d, netlist, boxes[i], options);

            // 5. route the short signals round everything drawn so far and every label's room
            HashSet<string> busPins = new(StringComparer.Ordinal);
            HashSet<string> wired = Route(d, netlist, candidates, pinPlans, busPins);

            // 6. labels for the rest
            foreach (KeyValuePair<string, PinPlan> kv in pinPlans)
            {
                PinPlan p = kv.Value;
                if (p.Net == null || wired.Contains(NetKey(p.Net)) || busPins.Contains(p.At))
                    continue;
                DrawLabel(d, p, pinRef[p.At].Confidence);
            }
            return d;
        }

        /// <summary>
        /// Turns a drawing coordinate into millimetres of a measured position (the inverse of
        /// how a position is drawn).
        /// </summary>
        /// <param name="units">
        /// X or Y in drawing units.
        /// </param>
        /// <returns>
        /// The same place in whole millimetres.
        /// </returns>
        public static double ToMm(double units) => Math.Round((units - Margin) / UnitsPerMm, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Turns millimetres of a measured position into a drawing coordinate.
        /// </summary>
        /// <param name="mm">
        /// X or Y in millimetres.
        /// </param>
        /// <returns>
        /// The same place in drawing units.
        /// </returns>
        public static double FromMm(double mm) => Margin + (mm * UnitsPerMm);

        /// <summary>
        /// The less sure of two readings: OPEN, then INFERRED, then READ.
        /// </summary>
        /// <param name="a">
        /// One reading.
        /// </param>
        /// <param name="b">
        /// The other.
        /// </param>
        /// <returns>
        /// The less sure one.
        /// </returns>
        public static Confidence Worse(Confidence a, Confidence b)
        {
            if (a == Confidence.Open || b == Confidence.Open)
                return Confidence.Open;
            if (a == Confidence.Inferred || b == Confidence.Inferred)
                return Confidence.Inferred;
            return Confidence.Read;
        }

        /// <summary>
        /// The width of a text as the drawing estimates it (0.6 x font size per character, the
        /// monospace width; a subscript counts 0.7 of a character).
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="subscript">
        /// The subscript after it, or empty.
        /// </param>
        /// <param name="size">
        /// Font size.
        /// </param>
        /// <returns>
        /// The width in drawing units.
        /// </returns>
        public static double TextWidth(string text, string subscript, double size) => CharWidth * size * (text.Length + (0.7 * subscript.Length));

        private static double Snap(double v) => Math.Ceiling(v / WireRouter.Step) * WireRouter.Step;

        /// <summary>
        /// Size of a chip, where its gates and pins go (relative to the frame), and how far its
        /// labels reach out on each side.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="part">
        /// The chip.
        /// </param>
        /// <param name="style">
        /// The symbol set (the ND AND gate is longer).
        /// </param>
        /// <param name="options">
        /// What to show, and the catalogue.
        /// </param>
        /// <param name="netByPin">
        /// The signal on each pin.
        /// </param>
        /// <returns>
        /// The plan.
        /// </returns>
        private static PartPlan Measure(Netlist netlist, Part part, SymbolStyle style, LayoutOptions options, Dictionary<string, Net> netByPin)
        {
            PartPlan plan = new() { Part = part };
            for (int i = 0; i < netlist.Gates.Count; i++)
            {
                if (netlist.Gates[i].Part == part.Ref)
                    plan.Gates.Add(netlist.Gates[i]);
            }

            if (plan.Gates.Count > 0)
            {
                double symbolWidth = 0;
                for (int i = 0; i < plan.Gates.Count; i++)
                    symbolWidth = Math.Max(symbolWidth, SymbolWidth(plan.Gates[i], style));
                plan.Width = Snap((2 * FramePad) + symbolWidth);
                double y = FramePad;
                for (int i = 0; i < plan.Gates.Count; i++)
                {
                    Gate g = plan.Gates[i];
                    double h = GateHeight(g);
                    plan.GateY.Add(y);
                    for (int k = 0; k < g.Inputs.Count; k++)
                        AddPin(plan, part, g.Inputs[k], -1, -PinOut, y + ((k + 1) * PinPitch), "gate:" + g.Id, netByPin);
                    AddPin(plan, part, g.Output, 1, plan.Width + PinOut, y + (h / 2), "gate:" + g.Id, netByPin);
                    y += h + GateGap;
                }
                plan.Height = Snap(y - GateGap + FramePad);
            }
            else
            {
                // a box of pins in DIP order: 1..n/2 down the left, the rest up the right
                ChipInfo? chip = options.Catalog?.Get(part.Type);
                List<int> numbers = new();
                List<string> names = new();
                if (chip != null)
                {
                    for (int i = 0; i < chip.Pins.Count; i++)
                    {
                        numbers.Add(chip.Pins[i].Number);
                        names.Add(chip.Pins[i].Name);
                    }
                }
                else
                {
                    for (int pin = 1; pin <= 64; pin++)
                    {
                        if (netByPin.ContainsKey(part.Ref + "." + pin.ToString(CultureInfo.InvariantCulture)))
                        {
                            numbers.Add(pin);
                            names.Add("");
                        }
                    }
                }
                int left = (numbers.Count + 1) / 2;
                double nameWidth = 0;
                for (int i = 0; i < names.Count; i++)
                    nameWidth = Math.Max(nameWidth, TextWidth(names[i], "", PinFont));
                plan.Width = Snap(Math.Max(48, (2 * nameWidth) + 24));
                plan.Height = Snap((left + 1) * PinPitch);
                for (int i = 0; i < numbers.Count; i++)
                {
                    bool onLeft = i < left;
                    int row = onLeft ? i : numbers.Count - 1 - i;
                    PinPlan p = AddPin(plan, part, numbers[i], onLeft ? -1 : 1, onLeft ? -PinOut : plan.Width + PinOut,
                        (row + 1) * PinPitch, "part:" + part.Ref, netByPin);
                    p.Name = names[i];
                }
            }

            // how far the labels reach out of the frame on each side
            plan.LeftReach = PinOut + 4;
            plan.RightReach = PinOut + 4;
            for (int i = 0; i < plan.Pins.Count; i++)
            {
                PinPlan p = plan.Pins[i];
                if (p.Net == null)
                    continue;
                double reach = PinOut + LabelWidth(p.Net) + 4;
                if (p.Side < 0)
                    plan.LeftReach = Math.Max(plan.LeftReach, reach);
                else
                    plan.RightReach = Math.Max(plan.RightReach, reach);
            }
            plan.LeftReach = Snap(plan.LeftReach);
            plan.RightReach = Snap(plan.RightReach);
            return plan;
        }

        private static PinPlan AddPin(PartPlan plan, Part part, int pin, int side, double x, double y, string owner, Dictionary<string, Net> netByPin)
        {
            string at = part.Ref + "." + pin.ToString(CultureInfo.InvariantCulture);
            netByPin.TryGetValue(at, out Net? net);
            PinPlan p = new() { At = at, Number = pin, Side = side, X = x, Y = y, Owner = owner, Net = net, Confidence = part.Confidence };
            plan.Pins.Add(p);
            return p;
        }

        private static double GateHeight(Gate g) => Math.Max(24, (g.Inputs.Count + 1) * PinPitch);

        private static double SymbolWidth(Gate g, SymbolStyle style)
            => GateWidth + (style == SymbolStyle.NorskData && g.Kind == GateKind.And ? AndTail : 0) + (style == SymbolStyle.Modern ? 6 : 0);

        /// <summary>
        /// Room a label takes beyond the pin end: the diamond, a gap, the name and its subscript.
        /// </summary>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <returns>
        /// The width in drawing units.
        /// </returns>
        private static double LabelWidth(Net net) => LabelOffset + Diamond + 3 + TextWidth(net.Name, Subscript(net), SignalFont);

        /// <summary>
        /// Puts every chip on the sheet: first the ones at their board position, on a grid whose
        /// columns and rows are as wide as their largest chip plus <see cref="PartGap"/>; then the
        /// ones placed by hand, each moved down until it covers nothing already placed.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="plans">
        /// The measured chips, given their X and Y here.
        /// </param>
        private static void Place(Netlist netlist, PartPlan[] plans)
        {
            int columns = 0, rows = 0;
            BoardPosition[] pos = new BoardPosition[plans.Length];
            bool[] byHand = new bool[plans.Length];
            for (int i = 0; i < plans.Length; i++)
            {
                byHand[i] = HandPosition(netlist, plans[i], out _, out _);
                if (byHand[i])
                    continue;
                pos[i] = BoardPosition.Parse(plans[i].Part.Ref);
                columns = Math.Max(columns, pos[i].LastColumn);
                rows = Math.Max(rows, pos[i].LastRow + 1);
            }

            double[] colWidth = new double[columns + 1];
            double[] rowHeight = new double[rows + 1];
            // single-cell chips first, then make spanning chips fit by widening their last column/row
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < plans.Length; i++)
                {
                    if (byHand[i])
                        continue;
                    BoardPosition p = pos[i];
                    double w = plans[i].LeftReach + plans[i].Width + plans[i].RightReach + PartGap;
                    double h = 18 + plans[i].Height + PartGap;
                    bool spans = p.LastColumn != p.FirstColumn || p.LastRow != p.FirstRow;
                    if (spans != (pass == 1))
                        continue;
                    double haveW = 0, haveH = 0;
                    for (int c = p.FirstColumn; c <= p.LastColumn; c++)
                        haveW += colWidth[c];
                    for (int r = p.FirstRow; r <= p.LastRow; r++)
                        haveH += rowHeight[r];
                    if (haveW < w)
                        colWidth[p.LastColumn] += w - haveW;
                    if (haveH < h)
                        rowHeight[p.LastRow] += h - haveH;
                }
            }
            double[] colX = new double[columns + 2];
            double[] rowY = new double[rows + 2];
            colX[1] = Margin;
            for (int c = 1; c <= columns; c++)
                colX[c + 1] = colX[c] + colWidth[c];
            rowY[0] = Margin + 24;
            for (int r = 0; r < rows; r++)
                rowY[r + 1] = rowY[r] + rowHeight[r];

            List<PartPlan> placed = new();
            for (int i = 0; i < plans.Length; i++)
            {
                if (byHand[i])
                    continue;
                // Columns are drawn left to right in rising order. Which edge of the real board
                // column 1 is on has NOT been checked.
                plans[i].X = Snap(colX[pos[i].FirstColumn] + plans[i].LeftReach);
                plans[i].Y = Snap(rowY[pos[i].FirstRow] + 18);
                placed.Add(plans[i]);
            }
            for (int i = 0; i < plans.Length; i++)
            {
                if (!byHand[i])
                    continue;
                HandPosition(netlist, plans[i], out double x, out double y);
                plans[i].X = Math.Max(Snap(x), Snap(Margin + plans[i].LeftReach));
                plans[i].Y = Math.Max(Snap(y), Snap(Margin + 42));
                // pushed down past anything it would cover
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

        /// <summary>
        /// Where a chip placed by hand goes: its own position, else the top left of its measured
        /// gates. False when it has neither (it goes on the board grid).
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="plan">
        /// The chip.
        /// </param>
        /// <param name="x">
        /// Frame left in drawing units.
        /// </param>
        /// <param name="y">
        /// Frame top in drawing units.
        /// </param>
        /// <returns>
        /// True when the chip is placed by hand.
        /// </returns>
        private static bool HandPosition(Netlist netlist, PartPlan plan, out double x, out double y)
        {
            x = 0;
            y = 0;
            if (plan.Part.Position != null)
            {
                x = FromMm(plan.Part.Position.X);
                y = FromMm(plan.Part.Position.Y);
                return true;
            }
            if (plan.Gates.Count == 0)
                return false;
            double minX = double.MaxValue, minY = double.MaxValue;
            for (int i = 0; i < plan.Gates.Count; i++)
            {
                Point? p = plan.Gates[i].Position;
                if (p == null)
                    return false;
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
            }
            x = FromMm(minX);
            y = FromMm(minY);
            return true;
        }

        private static bool Covers(PartPlan a, PartPlan b)
        {
            double gap = PartGap / 2;
            return a.X - a.LeftReach < b.X + b.Width + b.RightReach + gap && b.X - b.LeftReach < a.X + a.Width + a.RightReach + gap
                && a.Y - 18 < b.Y + b.Height + gap && b.Y - 18 < a.Y + a.Height + gap;
        }

        private static List<Net> WireCandidates(Netlist netlist, Dictionary<string, PinPlan> pinPlans)
        {
            List<Net> list = new();
            for (int i = 0; i < netlist.Nets.Count; i++)
            {
                Net net = netlist.Nets[i];
                if (net.Pins.Count < 2 || net.Pins.Count > MaxWiredPins)
                    continue;
                double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
                bool all = true;
                for (int j = 0; j < net.Pins.Count && all; j++)
                {
                    if (!pinPlans.TryGetValue(net.Pins[j].At, out PinPlan? p))
                    {
                        all = false;
                        break;
                    }
                    minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                    minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
                }
                if (all && maxX - minX <= WireReach && maxY - minY <= WireReach)
                    list.Add(net);
            }
            // short ones first: they get the straight routes
            list.Sort((a, b) =>
            {
                int c = Spread(a, pinPlans).CompareTo(Spread(b, pinPlans));
                return c != 0 ? c : string.CompareOrdinal(a.Name, b.Name);
            });
            return list;
        }

        private static double Spread(Net net, Dictionary<string, PinPlan> pinPlans)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            for (int j = 0; j < net.Pins.Count; j++)
            {
                PinPlan p = pinPlans[net.Pins[j].At];
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            }
            return (maxX - minX) + (maxY - minY);
        }

        /// <summary>
        /// Lays the wires. The router is told about every frame (with one grid step of margin),
        /// every text already drawn, every pin end, and the room each signal's label would take,
        /// so a signal that cannot be routed can still get its labels without overlapping a wire.
        /// </summary>
        /// <param name="d">
        /// The drawing so far (chips only).
        /// </param>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="candidates">
        /// The signals that may get a wire, shortest first.
        /// </param>
        /// <param name="pinPlans">
        /// Every drawn pin.
        /// </param>
        /// <returns>
        /// The names of the signals that got a wire.
        /// </returns>
        /// <param name="busPins">
        /// Filled with the pins that joined a bus (they get no label).
        /// </param>
        private static HashSet<string> Route(SchematicDrawing d, Netlist netlist, List<Net> candidates, Dictionary<string, PinPlan> pinPlans, HashSet<string> busPins)
        {
            HashSet<string> wired = new(StringComparer.Ordinal);
            if (candidates.Count == 0 && (netlist.Layout?.Buses == null || netlist.Layout.Buses.Count == 0))
                return wired;
            WireRouter router = new(d.Width, d.Height);
            double m = WireRouter.Step;
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem it = d.Items[i];
                if (it.Kind == DrawKind.GroupStart || it.Kind == DrawKind.GroupEnd || it.Role == DrawRole.PinDot)
                    continue;
                SchematicDrawing.Bounds(it, out double l, out double t, out double r, out double b);
                if (it.Role == DrawRole.PartOutline || (it.Role == DrawRole.Symbol && it.Kind != DrawKind.Text))
                    router.Block(l - m, t - m, r + m, b + m);
                else if (it.Kind == DrawKind.Text)
                    router.Block(l - 3, t - 2, r + 3, b + 2);
            }
            // numbered by place in the file, not by name: two signals may share a name and differ
            // only in their 0/1 level (2A50 has BD0..BD9 both ways), and they must never be
            // taken for one signal
            Dictionary<Net, int> netNumber = new(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < netlist.Nets.Count; i++)
                netNumber[netlist.Nets[i]] = i + 1;
            foreach (KeyValuePair<string, PinPlan> kv in pinPlans)
            {
                PinPlan p = kv.Value;
                if (p.Net == null)
                {
                    router.Block(p.X - 1, p.Y - 1, p.X + 1, p.Y + 1);
                    continue;
                }
                LabelBox(p, out double l, out double t, out double r, out double b);
                // a little room round each label, so a wire passing by does not touch the text
                router.Reserve(Math.Min(l, p.X) - 4, t - 2, Math.Max(r, p.X) + 4, b + 2, netNumber[p.Net]);
            }

            // buses first: their lines and entries claim their room before any wire is laid
            DrawBuses(d, netlist, router, netNumber, pinPlans, busPins);

            List<DrawPoint> ends = new();
            List<int> outward = new();
            List<double[]> segments = new();
            List<DrawPoint> junctions = new();

            // wire paths fixed by hand come first and are kept as they are while they still obey
            // every routing rule; one that now crosses a chip (or is broken) is dropped, and the
            // signal is routed afresh below like any other
            List<WirePath>? stored = netlist.Layout?.Wires;
            for (int i = 0; stored != null && i < stored.Count; i++)
            {
                WirePath w = stored[i];
                Net? net = null;
                for (int j = 0; j < netlist.Nets.Count && net == null; j++)
                {
                    if (netlist.Nets[j].Name == w.Net && (w.Active == null || w.Active == netlist.Nets[j].Active))
                        net = netlist.Nets[j];
                }
                if (net == null || wired.Contains(NetKey(net)))
                    continue;
                List<PinPlan> pins = new();
                bool all = net.Pins.Count >= 2;
                for (int j = 0; j < net.Pins.Count && all; j++)
                {
                    if (pinPlans.TryGetValue(net.Pins[j].At, out PinPlan? p))
                        pins.Add(p);
                    else
                        all = false;
                }
                ends.Clear();
                for (int j = 0; all && j < pins.Count; j++)
                    ends.Add(new DrawPoint(pins[j].X, pins[j].Y));
                List<List<DrawPoint>> branches = new();
                for (int b = 0; b < w.Branches.Count; b++)
                {
                    List<DrawPoint> points = new();
                    for (int k = 0; k < w.Branches[b].Count; k++)
                        points.Add(new DrawPoint(FromMm(w.Branches[b][k].X), FromMm(w.Branches[b][k].Y)));
                    branches.Add(points);
                }
                if (all && router.Keep(netNumber[net], ends, branches, segments, junctions))
                {
                    wired.Add(NetKey(net));
                    d.KeptWires.Add(NetKey(net));
                    AddWire(d, net, pins, segments, junctions);
                }
                else
                {
                    d.Rerouted.Add(NetKey(net));
                    d.RerouteReasons.Add(NetKey(net) + ": " + (all ? router.Refusal : "not every pin is drawn on the sheet"));
                }
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                Net net = candidates[i];
                if (wired.Contains(NetKey(net)))
                    continue;
                ends.Clear();
                outward.Clear();
                // start from the left-most pin, so a tree grows from one side
                List<PinPlan> pins = new();
                for (int j = 0; j < net.Pins.Count; j++)
                    pins.Add(pinPlans[net.Pins[j].At]);
                pins.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
                Confidence worst = Confidence.Read;
                for (int j = 0; j < pins.Count; j++)
                {
                    ends.Add(new DrawPoint(pins[j].X, pins[j].Y));
                    outward.Add(pins[j].Side);
                    worst = Worse(worst, PinConfidence(net, pins[j].At));
                }
                if (!router.Route(netNumber[net], ends, outward, 120, segments, junctions))
                    continue;
                wired.Add(NetKey(net));
                AddWire(d, net, pins, segments, junctions);
            }
            return wired;
        }

        /// <summary>
        /// Draws one signal's laid wire and keeps its branches for saving.
        /// </summary>
        /// <param name="d">
        /// The drawing.
        /// </param>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <param name="pins">
        /// Its pins, in the order the router was given them.
        /// </param>
        /// <param name="segments">
        /// The pieces, as filled by the router.
        /// </param>
        /// <param name="junctions">
        /// The branch points.
        /// </param>
        private static void AddWire(SchematicDrawing d, Net net, List<PinPlan> pins, List<double[]> segments, List<DrawPoint> junctions)
        {
            {
                Confidence worst = Confidence.Read;
                for (int j = 0; j < pins.Count; j++)
                    worst = Worse(worst, PinConfidence(net, pins[j].At));
                d.WireBranches[NetKey(net)] = WireRouter.Branches(segments);
                string owner = "net:" + NetKey(net);
                for (int s = 0; s < segments.Count; s++)
                {
                    double[] g = segments[s];
                    // a branch is as sure as its own pin; the first branch also joins the first pin,
                    // so it is as sure as the less sure of those two
                    int branch = (int)g[4];
                    Confidence c = PinConfidence(net, pins[branch].At);
                    if (branch == 1)
                        c = Worse(c, PinConfidence(net, pins[0].At));
                    d.Items.Add(new DrawItem
                    {
                        Kind = DrawKind.Line, Role = DrawRole.Wire, Confidence = c, Owner = owner,
                        X1 = g[0], Y1 = g[1], X2 = g[2], Y2 = g[3], Dashed = c == Confidence.Open,
                    });
                }
                for (int s = 0; s < junctions.Count; s++)
                    d.Items.Add(new DrawItem { Kind = DrawKind.Circle, Role = DrawRole.Wire, Confidence = worst, Owner = owner, X1 = junctions[s].X, Y1 = junctions[s].Y, X2 = 2 });
            }
        }

        private static Confidence PinConfidence(Net net, string at)
        {
            for (int i = 0; i < net.Pins.Count; i++)
            {
                if (net.Pins[i].At == at)
                    return net.Pins[i].Confidence;
            }
            return Confidence.Read;
        }

        /// <summary>
        /// The box a pin's label takes: from the pin end outwards, the diamond and the name.
        /// </summary>
        /// <param name="p">
        /// The pin, with a signal.
        /// </param>
        /// <param name="left">
        /// Left edge.
        /// </param>
        /// <param name="top">
        /// Top edge.
        /// </param>
        /// <param name="right">
        /// Right edge.
        /// </param>
        /// <param name="bottom">
        /// Bottom edge.
        /// </param>
        private static void LabelBox(PinPlan p, out double left, out double top, out double right, out double bottom)
        {
            double w = LabelWidth(p.Net!);
            left = p.Side < 0 ? p.X - w : p.X;
            right = p.Side < 0 ? p.X : p.X + w;
            top = p.Y - SignalFont + 3;
            bottom = p.Y + 3 + (0.3 * SignalFont);
        }

        private static void DrawPart(SchematicDrawing d, PartPlan plan, SymbolStyle style, LayoutOptions options)
        {
            Part part = plan.Part;
            string owner = "part:" + part.Ref;
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupStart, Owner = owner });
            d.Items.Add(new DrawItem
            {
                Kind = DrawKind.Rect,
                Role = DrawRole.PartOutline,
                Confidence = part.Confidence,
                Owner = owner,
                X1 = plan.X,
                Y1 = plan.Y,
                X2 = plan.Width,
                Y2 = plan.Height,
                Dashed = part.Confidence != Confidence.Read,
            });
            DrawItem label = Text(d, DrawRole.PartLabel, part.Confidence, owner, plan.X, plan.Y - 5,
                options.ShowBoardPositions ? part.Ref + " " + part.Type : part.Type, LabelFont, TextAnchor.Start);
            label.Bold = true;

            for (int i = 0; i < plan.Gates.Count; i++)
            {
                Gate g = plan.Gates[i];
                string gateOwner = "gate:" + g.Id;
                double gx = plan.X + FramePad, gy = plan.Y + plan.GateY[i], h = GateHeight(g);
                d.Items.Add(new DrawItem { Kind = DrawKind.GroupStart, Owner = gateOwner });
                if (style == SymbolStyle.NorskData)
                    NorskDataSymbol(d, g, gateOwner, gx, gy, h);
                else
                    ModernSymbol(d, g, gateOwner, gx, gy, h);
                d.Items.Add(new DrawItem { Kind = DrawKind.GroupEnd, Owner = gateOwner });
            }

            for (int i = 0; i < plan.Pins.Count; i++)
            {
                PinPlan p = plan.Pins[i];
                // the pin line runs from the symbol (or the box edge) out to the pin end
                double inner;
                if (p.Owner.StartsWith("gate:", StringComparison.Ordinal))
                {
                    Gate g = FindGate(plan, p.Owner.Substring(5));
                    inner = p.Side < 0 ? plan.X + FramePad : plan.X + FramePad + SymbolWidth(g, style) - (style == SymbolStyle.Modern ? 0 : 0);
                }
                else
                    inner = p.Side < 0 ? plan.X : plan.X + plan.Width;
                Line(d, DrawRole.PinLine, plan.Part.Confidence, p.Owner, inner, p.Y, p.X, p.Y);
                if (options.ShowPinNumbers)
                {
                    string number = p.Number.ToString(CultureInfo.InvariantCulture);
                    // on the part of the pin line outside the frame
                    double nx = p.Side < 0 ? p.X + 1 : plan.X + plan.Width + 1;
                    Text(d, DrawRole.PinNumber, plan.Part.Confidence, p.Owner, nx, p.Y - 2, number, PinFont, TextAnchor.Start);
                }
                if (p.Name.Length > 0)
                {
                    Text(d, DrawRole.PinName, plan.Part.Confidence, "pin:" + p.At,
                        p.Side < 0 ? plan.X + 3 : plan.X + plan.Width - 3, p.Y + 2.5, p.Name, PinFont,
                        p.Side < 0 ? TextAnchor.Start : TextAnchor.End);
                }
                PinDot(d, p.At, p.X, p.Y);
            }
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupEnd, Owner = owner });
        }

        private static Gate FindGate(PartPlan plan, string id)
        {
            for (int i = 0; i < plan.Gates.Count; i++)
            {
                if (plan.Gates[i].Id == id)
                    return plan.Gates[i];
            }
            return plan.Gates[0];
        }

        /// <summary>
        /// The ND net label at a pin end: the diamond (manual page 2) and the signal name with its
        /// subscript, outwards from the chip. It is drawn in the colour of that pin's reading, so an
        /// OPEN connection shows in the OPEN colour.
        /// </summary>
        /// <param name="d">
        /// The drawing to add to.
        /// </param>
        /// <param name="p">
        /// The pin, with a signal.
        /// </param>
        /// <param name="c">
        /// How sure the pin's connection is.
        /// </param>
        private static void DrawLabel(SchematicDrawing d, PinPlan p, Confidence c)
        {
            Net net = p.Net!;
            string owner = "net:" + NetKey(net);
            // the diamond starts a little past the pin end, so the pin dot stays clear of it
            double s = p.Side, x = p.X + (p.Side * LabelOffset), y = p.Y, half = Diamond / 2;
            d.Items.Add(new DrawItem
            {
                Kind = DrawKind.Path,
                Role = DrawRole.NetLabel,
                Confidence = c,
                Owner = owner,
                Path = new[]
                {
                    new PathSegment(PathOp.MoveTo, x, y),
                    new PathSegment(PathOp.LineTo, x + (s * half), y - half),
                    new PathSegment(PathOp.LineTo, x + (s * Diamond), y),
                    new PathSegment(PathOp.LineTo, x + (s * half), y + half),
                    new PathSegment(PathOp.Close, x, y),
                },
            });
            Line(d, DrawRole.PinLine, c, owner, p.X, y, x, y);
            // A label left of its pin is written from its left edge (the estimated width back from
            // the diamond), never right-aligned: a renderer whose font is narrower than the
            // estimate then leaves a gap, where right alignment without the subscript would run
            // the subscript into the diamond.
            double width = TextWidth(net.Name, Subscript(net), SignalFont);
            double tx = p.Side < 0 ? x - Diamond - 3 - width : x + Diamond + 3;
            DrawItem t = Text(d, DrawRole.SignalName, c, owner, tx, y + 3, net.Name, SignalFont, TextAnchor.Start);
            t.Subscript = Subscript(net);
        }

        /// <summary>
        /// The dot at a pin's end, where its wire or label starts, and the pin's place in
        /// <see cref="SchematicDrawing.Pins"/>.
        /// </summary>
        /// <param name="d">
        /// The drawing to add to.
        /// </param>
        /// <param name="at">
        /// The pin, such as 11D.3.
        /// </param>
        /// <param name="x">
        /// Pin end X.
        /// </param>
        /// <param name="y">
        /// Pin end Y.
        /// </param>
        private static void PinDot(SchematicDrawing d, string at, double x, double y)
        {
            d.Pins[at] = new DrawPoint(x, y);
            d.Items.Add(new DrawItem { Kind = DrawKind.Circle, Role = DrawRole.PinDot, Confidence = Confidence.Read, Owner = "pin:" + at, X1 = x, Y1 = y, X2 = PinDotRadius });
        }

        /// <summary>
        /// ND-13.003.01 pages 2 and 4-6: straight lines only.
        /// </summary>
        /// <param name="d">
        /// The drawing to add to.
        /// </param>
        /// <param name="gate">
        /// The gate.
        /// </param>
        /// <param name="owner">
        /// Owner name for the steps.
        /// </param>
        /// <param name="x">
        /// Left edge of the symbol.
        /// </param>
        /// <param name="y">
        /// Top edge of the symbol.
        /// </param>
        /// <param name="h">
        /// Height of the symbol.
        /// </param>
        private static void NorskDataSymbol(SchematicDrawing d, Gate gate, string owner, double x, double y, double h)
        {
            double r = x + GateWidth, b = y + h, m = y + (h / 2);
            Confidence c = gate.Confidence;
            d.Items.Add(new DrawItem { Kind = DrawKind.Rect, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = x, Y1 = y, X2 = GateWidth, Y2 = h });
            switch (gate.Kind)
            {
                case GateKind.Nand:
                    break;
                case GateKind.Nor:
                    Line(d, DrawRole.Symbol, c, owner, x, y, r, b);
                    Line(d, DrawRole.Symbol, c, owner, x, b, r, y);
                    break;
                case GateKind.Not:
                    Line(d, DrawRole.Symbol, c, owner, x, b, r, y);
                    Line(d, DrawRole.Symbol, c, owner, x + 5, y + 5, r - 5, m);
                    Line(d, DrawRole.Symbol, c, owner, r - 5, m, x + 5, b - 5);
                    break;
                case GateKind.And:
                    // The NAND box followed by the inverter's triangle (7408).
                    Line(d, DrawRole.Symbol, c, owner, r, y + 3, r + AndTail, m);
                    Line(d, DrawRole.Symbol, c, owner, r + AndTail, m, r, b - 3);
                    break;
                default:
                    // Not drawn by this layout yet: the kind is written in the box, so it shows as
                    // unfinished instead of looking like a NAND.
                    Text(d, DrawRole.Symbol, c, owner, x + 3, m + 3, gate.Kind.ToString().ToUpperInvariant(), 8, TextAnchor.Start);
                    break;
            }
        }

        private static void ModernSymbol(SchematicDrawing d, Gate gate, string owner, double x, double y, double h)
        {
            double r = x + GateWidth, b = y + h, m = y + (h / 2);
            Confidence c = gate.Confidence;
            switch (gate.Kind)
            {
                case GateKind.And:
                case GateKind.Nand:
                    Path(d, c, owner, new[]
                    {
                        new PathSegment(PathOp.MoveTo, x, y),
                        new PathSegment(PathOp.LineTo, r - (h / 2), y),
                        new PathSegment(PathOp.ArcClockwiseTo, r - (h / 2), b, h / 2),
                        new PathSegment(PathOp.LineTo, x, b),
                        new PathSegment(PathOp.Close, x, y),
                    });
                    if (gate.Kind == GateKind.Nand)
                        Bubble(d, c, owner, r + 3, m);
                    break;
                case GateKind.Or:
                case GateKind.Nor:
                    Path(d, c, owner, new[]
                    {
                        new PathSegment(PathOp.MoveTo, x, y),
                        new PathSegment(PathOp.QuadTo, r, m, x + 22, y),
                        new PathSegment(PathOp.QuadTo, x, b, x + 22, b),
                        new PathSegment(PathOp.QuadTo, x, y, x + 8, m),
                        new PathSegment(PathOp.Close, x, y),
                    });
                    if (gate.Kind == GateKind.Nor)
                        Bubble(d, c, owner, r + 3, m);
                    break;
                case GateKind.Not:
                case GateKind.Buffer:
                    Path(d, c, owner, new[]
                    {
                        new PathSegment(PathOp.MoveTo, x, y),
                        new PathSegment(PathOp.LineTo, r - 4, m),
                        new PathSegment(PathOp.LineTo, x, b),
                        new PathSegment(PathOp.Close, x, y),
                    });
                    if (gate.Kind == GateKind.Not)
                        Bubble(d, c, owner, r - 1, m);
                    break;
                default:
                    d.Items.Add(new DrawItem { Kind = DrawKind.Rect, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = x, Y1 = y, X2 = GateWidth, Y2 = h });
                    Text(d, DrawRole.Symbol, c, owner, x + 3, m + 3, "?", 8, TextAnchor.Start);
                    break;
            }
        }

        /// <summary>
        /// A signal's key on the drawing's wire lists: its name, plus "_0" or "_1" when it has an
        /// active level, because two signals may share a name and differ only in that level.
        /// </summary>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <returns>
        /// BD15_0, BD15_1, or N_9B_2 for a signal without a level.
        /// </returns>
        public static string NetKey(Net net) => net.Active == ActiveLevel.None ? net.Name : net.Name + "_" + Subscript(net);

        /// <summary>
        /// ND's subscript on a signal name: 1 = active high, 0 = active low (manual page 2).
        /// </summary>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <returns>
        /// "1", "0", or empty when the signal has no active level.
        /// </returns>
        private static string Subscript(Net net)
            => net.Active == ActiveLevel.None ? "" : net.Active == ActiveLevel.High ? "1" : "0";

        private static DrawItem Text(SchematicDrawing d, DrawRole role, Confidence c, string owner, double x, double y,
            string text, double size, TextAnchor anchor)
        {
            DrawItem t = new() { Kind = DrawKind.Text, Role = role, Confidence = c, Owner = owner, X1 = x, Y1 = y, Text = text, FontSize = size, Anchor = anchor };
            d.Items.Add(t);
            return t;
        }

        private static void Line(SchematicDrawing d, DrawRole role, Confidence c, string owner, double x1, double y1, double x2, double y2)
            => d.Items.Add(new DrawItem { Kind = DrawKind.Line, Role = role, Confidence = c, Owner = owner, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 });

        private static void Path(SchematicDrawing d, Confidence c, string owner, PathSegment[] path)
            => d.Items.Add(new DrawItem { Kind = DrawKind.Path, Role = DrawRole.Symbol, Confidence = c, Owner = owner, Path = path });

        private static void Bubble(SchematicDrawing d, Confidence c, string owner, double cx, double cy)
            => d.Items.Add(new DrawItem { Kind = DrawKind.Circle, Role = DrawRole.Symbol, Confidence = c, Owner = owner, X1 = cx, Y1 = cy, X2 = 3 });

        /// <summary>
        /// One chip as measured before it is placed.
        /// </summary>
        private sealed class PartPlan
        {
            public Part Part = null!;
            public List<Gate> Gates = new();
            public List<double> GateY = new();
            public List<PinPlan> Pins = new();
            public double X, Y, Width, Height, LeftReach, RightReach;
        }

        /// <summary>
        /// One drawn pin: where its end is (first relative to the frame, then on the sheet), which
        /// side it leaves on, and its signal.
        /// </summary>
        private sealed class PinPlan
        {
            public string At = "";
            public int Number;
            public int Side;
            public double X, Y;
            public string Owner = "";
            public string Name = "";
            public Net? Net;
            public Confidence Confidence;
        }
    }
}
