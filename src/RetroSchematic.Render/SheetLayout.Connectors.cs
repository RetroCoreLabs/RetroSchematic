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
    /// connector F, or P.A, P.B ... once the letter is known), right of everything else. The box
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
        /// Measures and places a box for every connector the sheet's signals use.
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
        /// <param name="left">
        /// Where the first box's pin ends may start.
        /// </param>
        /// <param name="top">
        /// Top of the boxes.
        /// </param>
        /// <returns>
        /// The boxes, left to right.
        /// </returns>
        private static List<ConnectorPlan> PlanConnectors(Netlist netlist, Dictionary<string, Net> netByPin, LayoutOptions options, double left, double top)
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
            double x = Snap(left + ConnectorGap);
            for (int r = 0; r < refs.Count; r++)
            {
                string reference = refs[r];
                List<int> pins = used[reference];
                pins.Sort();
                ConnectorInfo? type = TypeOf(netlist, reference, options);
                ConnectorPlan plan = new() { Ref = reference, Title = ConnectorPins.Name(reference), TypeTitle = type?.Title };
                double numberWidth = 0, nameWidth = 0, otherWidth = 0;
                for (int i = 0; i < pins.Count; i++)
                {
                    string at = reference + "." + pins[i].ToString(CultureInfo.InvariantCulture);
                    Net net = netByPin[at];
                    ConnectorRow row = new()
                    {
                        Number = ConnectorPins.Letter(reference) == null ? pins[i].ToString(CultureInfo.InvariantCulture) : ConnectorPins.Show(reference, pins[i]),
                        Net = net,
                        Other = CatalogueName(reference, pins[i], net, type, options),
                    };
                    plan.Rows.Add(row);
                    numberWidth = Math.Max(numberWidth, TextWidth(row.Number, "", PinFont));
                    nameWidth = Math.Max(nameWidth, NameWidth(net, options.NameStyle) * PinFont / SignalFont);
                    if (row.Other != null)
                        otherWidth = Math.Max(otherWidth, TextWidth(OtherText(row.Other), "", PinFont));
                }
                plan.NameX = Snap(numberWidth + 10);
                plan.OtherX = plan.NameX + nameWidth + 8;
                double width = plan.OtherX + otherWidth + 6;
                width = Math.Max(width, TextWidth(plan.Title, "", LabelFont) + 10);
                plan.X = x;
                plan.Y = Snap(top);
                plan.Width = Snap(width + 6);
                plan.Height = Snap(HeaderHeight + 8 + (pins.Count * PinPitch) + 6);
                for (int i = 0; i < pins.Count; i++)
                {
                    string at = reference + "." + pins[i].ToString(CultureInfo.InvariantCulture);
                    Net net = netByPin[at];
                    plan.Pins.Add(new PinPlan
                    {
                        At = at,
                        Number = pins[i],
                        Side = -1,
                        X = plan.X - PinOut,
                        // header 16 + 8 + half a pitch = 30, five grid steps: every pin end is on the
                        // router's grid, so a wire ends exactly at the pin
                        Y = plan.Y + HeaderHeight + 8 + ((i + 0.5) * PinPitch),
                        Owner = "pin:" + at,
                        Name = "",
                        Net = net,
                        Confidence = Confidence.Read,
                    });
                }
                plans.Add(plan);
                // the next box leaves room for this one's labels on its left
                x = Snap(plan.X + plan.Width + ConnectorGap);
            }
            return plans;
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
            for (int i = 0; i < plan.Pins.Count; i++)
            {
                PinPlan p = plan.Pins[i];
                ConnectorRow row = plan.Rows[i];
                double baseLine = p.Y + 2.5;
                Line(d, DrawRole.PinLine, Confidence.Read, p.Owner, plan.X, p.Y, p.X, p.Y);
                // the number is the pin's name on a connector, so it stays when pin numbers are hidden
                Text(d, DrawRole.PinName, Confidence.Read, p.Owner, plan.X + 4, baseLine, row.Number, PinFont, TextAnchor.Start);
                DrawItem name = Text(d, DrawRole.PinName, Confidence.Read, "pin:" + p.At, plan.X + plan.NameX, baseLine, row.Net.Name, PinFont, TextAnchor.Start);
                SetName(name, row.Net, options.NameStyle);
                if (row.Other != null)
                    Text(d, DrawRole.Note, Confidence.Read, "pin:" + p.At, plan.X + plan.OtherX, baseLine, OtherText(row.Other), PinFont, TextAnchor.Start);
                PinDot(d, p.At, p.X, p.Y);
            }
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
