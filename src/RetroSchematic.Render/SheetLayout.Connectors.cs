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
    /// The card's connectors drawn as parts: one box per connector (the backplane P, a front
    /// connector F, or P.A, P.B ... once the letter is known), left or right of the chips, on
    /// the side and at the height where its wires are shortest, or where it was put by hand. The box
    /// has a header with the connector's name, and one row per pin the sheet uses, in pin order:
    /// the pin number, the signal's name in the chosen style and, when the catalogue names that
    /// pin differently, the catalogue's name too (IOXE = BIOXE). Each pin has a pin line and an
    /// end like a chip pin, so its signal gets a net label there and is picked and traced the same
    /// way. A signal that goes to a connector is never drawn as a wire (FORMAT.md, "Wire or label").
    /// </summary>
    public static partial class SheetLayout
    {
        /// <summary>
        /// Space left between the chips and the first connector box, and between two boxes.
        /// </summary>
        private const double ConnectorGap = 150;

        /// <summary>
        /// Fills the bus catalogues in the options for the sheet's bus, read once and kept there
        /// until the bus changes.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="options">
        /// The options; their chip catalogue says where the catalogue folder is.
        /// </param>
        private static void LoadBusCatalogues(Netlist netlist, LayoutOptions options)
        {
            MachineBus bus = netlist.Sheet.Bus ?? MachineBus.Unknown;
            string? folder = options.Catalog?.Folder;
            if (options.BusSignals == null || options.BusSignals.Bus != bus)
                options.BusSignals = BusCatalog.Load(folder, bus);
            if (options.ConnectorTypes == null || options.ConnectorTypes.Bus != bus)
                options.ConnectorTypes = ConnectorCatalog.Load(folder, bus);
        }

        /// <summary>
        /// Measures a box for every connector the sheet's signals use. The boxes are not placed
        /// yet: <see cref="ChooseConnectorSides"/> and <see cref="PlaceConnectors"/> do that.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="netByPin">
        /// The signal on each pin.
        /// </param>
        /// <param name="options">
        /// What to show, with the bus catalogues.
        /// </param>
        /// <returns>
        /// The boxes, in connector order.
        /// </returns>
        private static List<ConnectorPlan> MeasureConnectors(Netlist netlist, Dictionary<string, Net> netByPin, LayoutOptions options)
        {
            // which connectors, and which of their pins, the signals use
            Dictionary<string, List<int>> used = new(StringComparer.Ordinal);
            List<string> refs = new();
            foreach (KeyValuePair<string, Net> kv in netByPin)
            {
                int dot = kv.Key.LastIndexOf('.');
                string reference = kv.Key.Substring(0, dot);
                if (!ConnectorPins.IsConnector(reference))
                    continue;
                if (!used.TryGetValue(reference, out List<int>? pins))
                {
                    pins = new List<int>();
                    used[reference] = pins;
                    refs.Add(reference);
                }
                pins.Add(int.Parse(kv.Key.AsSpan(dot + 1), NumberStyles.None, CultureInfo.InvariantCulture));
            }
            refs.Sort(ConnectorPins.Compare);

            List<ConnectorPlan> plans = new();
            for (int r = 0; r < refs.Count; r++)
            {
                string reference = refs[r];
                List<int> pins = used[reference];
                pins.Sort();
                ConnectorInfo? type = TypeOf(netlist, reference, options);
                ConnectorPlan plan = new() { Ref = reference, Title = ConnectorPins.Name(reference, netlist.Sheet.Bus), TypeTitle = type?.Title, Use = UseOf(netlist, reference) };
                double numberWidth = 0, nameWidth = 0, otherWidth = 0;
                for (int i = 0; i < pins.Count; i++)
                {
                    string at = reference + "." + pins[i].ToString(CultureInfo.InvariantCulture);
                    Net net = netByPin[at];
                    ConnectorRow row = new()
                    {
                        Number = ConnectorPins.Letter(reference) == null ? pins[i].ToString(CultureInfo.InvariantCulture) : ConnectorPins.Show(reference, pins[i], netlist.Sheet.Bus),
                        Net = net,
                        Other = CatalogueName(reference, pins[i], net, type, options),
                    };
                    plan.Rows.Add(row);
                    plan.Numbers.Add(pins[i]);
                    numberWidth = Math.Max(numberWidth, TextWidth(row.Number, "", PinFont));
                    nameWidth = Math.Max(nameWidth, NameWidth(net, options.NameStyle) * PinFont / SignalFont);
                    if (row.Other != null)
                        otherWidth = Math.Max(otherWidth, TextWidth(OtherText(row.Other), "", PinFont));
                }
                plan.NameX = Snap(numberWidth + 10);
                plan.OtherX = plan.NameX + nameWidth + 8;
                double width = plan.OtherX + otherWidth + 6;
                width = Math.Max(width, TextWidth(plan.Title, "", LabelFont) + 10);
                plan.Width = Snap(width + 6);
                plan.Height = Snap(HeaderHeight + 8 + (pins.Count * PinPitch) + 6);
                plans.Add(plan);
            }
            return plans;
        }

        /// <summary>
        /// Picks for every connector box placed automatically whether it goes left or right of the
        /// chips: the side its chips are nearer to, by the mean X of the chip pins its signals go
        /// to. Supply lines and signals with more than <see cref="MaxWiredPins"/> chip pins keep
        /// their tags, so they do not count. The pins go on the side of the box that faces the
        /// chips. A box placed by hand keeps the pin side stored, or gets the pins on the side
        /// that faces the middle of the chips.
        /// </summary>
        /// <param name="connectors">
        /// The measured boxes.
        /// </param>
        /// <param name="plans">
        /// The chips, placed (pin positions relative to their frames).
        /// </param>
        /// <param name="shift">
        /// The room added on the left so far (0 on the first pass).
        /// </param>
        /// <returns>
        /// True when any box goes left of the chips.
        /// </returns>
        private static bool ChooseConnectorSides(List<ConnectorPlan> connectors, PartPlan[] plans, double shift)
        {
            double minX = double.MaxValue, maxX = double.MinValue;
            Dictionary<string, double> pinX = new(StringComparer.Ordinal);
            for (int i = 0; i < plans.Length; i++)
            {
                minX = Math.Min(minX, plans[i].X);
                maxX = Math.Max(maxX, plans[i].X + plans[i].Width);
                for (int j = 0; j < plans[i].Pins.Count; j++)
                    pinX[plans[i].Pins[j].At] = plans[i].X + plans[i].Pins[j].X;
            }
            double middle = plans.Length == 0 ? 0 : (minX + maxX) / 2;
            bool anyLeft = false;
            for (int c = 0; c < connectors.Count; c++)
            {
                ConnectorPlan plan = connectors[c];
                string? stored = plan.Use?.PinSide;
                // the side is chosen from the chips even for a box moved by hand, so moving the box
                // never changes the room on the left and so never moves the chips
                plan.Left = MeanOf(plan, pinX, middle) < middle;
                anyLeft |= plan.Left;
                if (plan.Use?.Position != null)
                {
                    double centre = FromMm(plan.Use.Position.X) + shift + (plan.Width / 2);
                    plan.PinSide = stored == "left" ? -1 : stored == "right" ? 1 : (centre < middle ? 1 : -1);
                    continue;
                }
                plan.PinSide = stored == "left" ? -1 : stored == "right" ? 1 : (plan.Left ? 1 : -1);
            }
            return anyLeft;
        }

        /// <summary>
        /// The mean of a coordinate over the chip pins a connector's wired signals go to.
        /// </summary>
        /// <param name="plan">
        /// The connector box.
        /// </param>
        /// <param name="at">
        /// The coordinate of every chip pin, by pin reference.
        /// </param>
        /// <param name="none">
        /// What to give when no signal of the box reaches a chip pin.
        /// </param>
        /// <returns>
        /// The mean.
        /// </returns>
        private static double MeanOf(ConnectorPlan plan, Dictionary<string, double> at, double none)
        {
            double sum = 0;
            int count = 0;
            for (int r = 0; r < plan.Rows.Count; r++)
            {
                Net net = plan.Rows[r].Net;
                // supply lines and wide signals keep their tags, so they pull nowhere
                if (ChipPinCount(net) > MaxWiredPins)
                    continue;
                for (int k = 0; k < net.Pins.Count; k++)
                {
                    if (at.TryGetValue(net.Pins[k].At, out double v))
                    {
                        sum += v;
                        count++;
                    }
                }
            }
            return count == 0 ? none : sum / count;
        }

        /// <summary>
        /// How much room the boxes left of the chips need: the widest of them, its pin
        /// lines, and the gap for the labels at its pins.
        /// </summary>
        /// <param name="connectors">
        /// The measured boxes, sides chosen.
        /// </param>
        /// <returns>
        /// The room in drawing units, 0 when no box goes left.
        /// </returns>
        private static double LeftConnectorRoom(List<ConnectorPlan> connectors)
        {
            double widest = 0;
            for (int c = 0; c < connectors.Count; c++)
            {
                if (connectors[c].Left)
                    widest = Math.Max(widest, connectors[c].Width);
            }
            return widest == 0 ? 0 : Snap(widest + PinOut + ConnectorGap);
        }

        /// <summary>
        /// Puts every connector box in its place and makes its pins. A box placed by hand goes to
        /// its stored position. An automatic box goes on its side (left of the chips at the margin,
        /// or right of everything), level with the mean height of the chip pins its signals go to,
        /// and boxes on one side are pushed down so none covers another.
        /// </summary>
        /// <param name="connectors">
        /// The measured boxes, sides chosen.
        /// </param>
        /// <param name="plans">
        /// The chips, placed (pin positions relative to their frames).
        /// </param>
        /// <param name="right">
        /// The right edge of everything drawn so far.
        /// </param>
        /// <param name="shift">
        /// The room added on the left for connector boxes; stored positions move with it.
        /// </param>
        private static void PlaceConnectors(List<ConnectorPlan> connectors, PartPlan[] plans, double right, double shift)
        {
            Dictionary<string, double> pinY = new(StringComparer.Ordinal);
            for (int i = 0; i < plans.Length; i++)
            {
                for (int j = 0; j < plans[i].Pins.Count; j++)
                    pinY[plans[i].Pins[j].At] = plans[i].Y + plans[i].Pins[j].Y;
            }
            double top = Margin + 42;
            List<ConnectorPlan> auto = new();
            for (int c = 0; c < connectors.Count; c++)
            {
                ConnectorPlan plan = connectors[c];
                if (plan.Use?.Position != null)
                {
                    plan.X = Snap(FromMm(plan.Use.Position.X) + shift);
                    plan.Y = Snap(FromMm(plan.Use.Position.Y));
                    continue;
                }
                double wanted = MeanOf(plan, pinY, top + (plan.Height / 2)) - (plan.Height / 2);
                plan.Y = Snap(Math.Max(top, wanted));
                plan.X = plan.Left ? Snap(Margin) : Snap(right + ConnectorGap);
                auto.Add(plan);
            }
            // on each side, top to bottom by wanted height, none covering the one above
            auto.Sort(static (a, b) => a.Y.CompareTo(b.Y));
            double leftBottom = 0, rightBottom = 0;
            for (int i = 0; i < auto.Count; i++)
            {
                ConnectorPlan plan = auto[i];
                double bottom = plan.Left ? leftBottom : rightBottom;
                if (plan.Y < bottom)
                    plan.Y = Snap(bottom);
                if (plan.Left)
                    leftBottom = plan.Y + plan.Height + PartGap;
                else
                    rightBottom = plan.Y + plan.Height + PartGap;
            }
            for (int c = 0; c < connectors.Count; c++)
                MakeConnectorPins(connectors[c]);
        }

        /// <summary>
        /// The pin ends of a placed box, on the side its pins face.
        /// </summary>
        /// <param name="plan">
        /// The connector box, placed.
        /// </param>
        private static void MakeConnectorPins(ConnectorPlan plan)
        {
            for (int i = 0; i < plan.Rows.Count; i++)
            {
                string at = plan.Ref + "." + plan.Numbers[i].ToString(CultureInfo.InvariantCulture);
                plan.Pins.Add(new PinPlan
                {
                    At = at,
                    Number = plan.Numbers[i],
                    Side = plan.PinSide,
                    X = plan.PinSide < 0 ? plan.X - PinOut : plan.X + plan.Width + PinOut,
                    // header 16 + 8 + half a pitch = 30, five grid steps: every pin end is on the
                    // router's grid, so a wire ends exactly at the pin
                    Y = plan.Y + HeaderHeight + 8 + ((i + 0.5) * PinPitch),
                    Owner = "pin:" + at,
                    Name = "",
                    Net = plan.Rows[i].Net,
                    Confidence = Confidence.Read,
                });
            }
        }

        /// <summary>
        /// How many chip pins (not connector pins) a signal has.
        /// </summary>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <returns>
        /// The count.
        /// </returns>
        private static int ChipPinCount(Net net)
        {
            int n = 0;
            for (int k = 0; k < net.Pins.Count; k++)
            {
                string at = net.Pins[k].At;
                int dot = at.LastIndexOf('.');
                if (dot > 0 && !ConnectorPins.IsConnector(at.Substring(0, dot)))
                    n++;
            }
            return n;
        }

        /// <summary>
        /// The sheet's <c>connectors</c> entry for a connector, or null.
        /// </summary>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="reference">
        /// The connector, such as P.
        /// </param>
        /// <returns>
        /// The entry, or null.
        /// </returns>
        private static ConnectorUse? UseOf(Netlist netlist, string reference)
        {
            List<ConnectorUse>? uses = netlist.Connectors;
            for (int i = 0; uses != null && i < uses.Count; i++)
            {
                if (uses[i].Ref == reference)
                    return uses[i];
            }
            return null;
        }

        /// <summary>
        /// The catalogue entry the sheet names for a connector (its <c>connectors</c> section),
        /// when that entry is for the sheet's bus; else null.
        /// </summary>
        private static ConnectorInfo? TypeOf(Netlist netlist, string reference, LayoutOptions options)
        {
            List<ConnectorUse>? uses = netlist.Connectors;
            for (int i = 0; uses != null && i < uses.Count; i++)
            {
                if (uses[i].Ref == reference && uses[i].Catalog != null)
                    return options.ConnectorTypes?.Get(uses[i].Catalog!);
            }
            return null;
        }

        /// <summary>
        /// The catalogue's name for a connector pin when it differs from the signal's name on the
        /// sheet: the bus-signal catalogue by pin for the backplane (pin 11 is BIOXE where the card
        /// says IOXE), the connector catalogue for any connector. Null when they agree or neither
        /// knows the pin.
        /// </summary>
        private static string? CatalogueName(string reference, int pin, Net net, ConnectorInfo? type, LayoutOptions options)
        {
            string? name = null;
            if (ConnectorPins.Kind(reference) == 'P' && options.BusSignals != null)
                name = options.BusSignals.ByPin(ConnectorPins.Letter(reference), pin)?.Name;
            if (name == null && type != null && type.PinNames.TryGetValue(pin, out string? n))
                name = n;
            if (name == null || string.Equals(name, net.Name, StringComparison.OrdinalIgnoreCase))
                return null;
            // "RECEIVER TIMING (V.24 17)" on a pin the sheet calls RECEIVER TIMING: show only
            // what the catalogue adds
            if (name.StartsWith(net.Name + " ", StringComparison.OrdinalIgnoreCase))
                return name.Substring(net.Name.Length + 1);
            return name;
        }

        /// <summary>
        /// The catalogue's text on a row: "= BIOXE" for another name, as it is for an addition
        /// in brackets such as "(V.24 17)".
        /// </summary>
        private static string OtherText(string other) => other.StartsWith('(') ? other : "= " + other;

        /// <summary>
        /// Draws a connector box: outline, header band with its name, the catalogue's title above
        /// it, and per pin the number, the signal name, the catalogue's name, the pin line and the
        /// pin dot.
        /// </summary>
        private static void DrawConnector(SchematicDrawing d, ConnectorPlan plan, LayoutOptions options)
        {
            string owner = "conn:" + plan.Ref;
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupStart, Owner = owner });
            d.Items.Add(new DrawItem
            {
                Kind = DrawKind.Rect, Role = DrawRole.PartOutline, Confidence = Confidence.Read, Owner = owner,
                X1 = plan.X, Y1 = plan.Y, X2 = plan.Width, Y2 = plan.Height,
            });
            d.Items.Add(new DrawItem
            {
                Kind = DrawKind.Rect, Role = DrawRole.BlockHeader, Confidence = Confidence.Read, Owner = owner,
                X1 = plan.X, Y1 = plan.Y, X2 = plan.Width, Y2 = HeaderHeight,
            });
            DrawItem title = Text(d, DrawRole.PartLabel, Confidence.Read, owner, plan.X + 4, plan.Y + HeaderHeight - 4, plan.Title, LabelFont, TextAnchor.Start);
            title.Bold = true;
            if (plan.TypeTitle != null)
                Text(d, DrawRole.Note, Confidence.Read, owner, plan.X, plan.Y - 5, plan.TypeTitle, PinFont, TextAnchor.Start);
            bool right = plan.PinSide > 0;
            double edge = right ? plan.X + plan.Width : plan.X;
            TextAnchor anchor = right ? TextAnchor.End : TextAnchor.Start;
            for (int i = 0; i < plan.Pins.Count; i++)
            {
                PinPlan p = plan.Pins[i];
                ConnectorRow row = plan.Rows[i];
                double baseLine = p.Y + 2.5;
                Line(d, DrawRole.PinLine, Confidence.Read, p.Owner, edge, p.Y, p.X, p.Y);
                // the columns run from the pin side inwards: number, signal name, catalogue name.
                // The number is the pin's name on a connector, so it stays when pin numbers are hidden
                Text(d, DrawRole.PinName, Confidence.Read, p.Owner, right ? edge - 4 : edge + 4, baseLine, row.Number, PinFont, anchor);
                DrawItem name = Text(d, DrawRole.PinName, Confidence.Read, "pin:" + p.At, right ? edge - plan.NameX : edge + plan.NameX, baseLine, row.Net.Name, PinFont, anchor);
                SetName(name, row.Net, options.NameStyle);
                if (row.Other != null)
                    Text(d, DrawRole.Note, Confidence.Read, "pin:" + p.At, right ? edge - plan.OtherX : edge + plan.OtherX, baseLine, OtherText(row.Other), PinFont, anchor);
                PinDot(d, p.At, p.X, p.Y);
            }
            d.ConnectorFrames[plan.Ref] = new PartFrame(plan.X, plan.Y, plan.Width, plan.Height);
            d.Items.Add(new DrawItem { Kind = DrawKind.GroupEnd, Owner = owner });
        }

        /// <summary>
        /// One connector box as measured and placed.
        /// </summary>
        private sealed class ConnectorPlan
        {
            public string Ref = "";
            public string Title = "";
            public string? TypeTitle;
            public List<PinPlan> Pins = new();
            public List<ConnectorRow> Rows = new();
            public List<int> Numbers = new();
            public ConnectorUse? Use;
            public bool Left;
            public int PinSide = -1;
            public double X, Y, Width, Height, NameX, OtherX;
        }

        /// <summary>
        /// The text of one connector row.
        /// </summary>
        private sealed class ConnectorRow
        {
            public string Number = "";
            public Net Net = null!;
            public string? Other;
        }
    }
}
