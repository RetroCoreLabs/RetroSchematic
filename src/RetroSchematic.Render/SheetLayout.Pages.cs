//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Text;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Drawing one page of a card with pages: the page's own parts, and the off-page markers.
    /// </summary>
    public static partial class SheetLayout
    {
        /// <summary>
        /// The owner prefix of an off-page marker: "offpage:" and the signal's key. Clicking one
        /// goes to the next page the signal is on.
        /// </summary>
        public const string OffPageOwner = "offpage:";

        /// <summary>
        /// A view of the card that holds only one drawn page's parts, gates, instances and the
        /// wire and bus paths stored for its card page. The signals are the card's own objects
        /// (card-wide, with every pin): a signal with a pin that is not drawn here gets labels,
        /// never a wire, and its labels get off-page markers.
        /// </summary>
        /// <param name="netlist">
        /// The card (top level).
        /// </param>
        /// <param name="page">
        /// The drawn page.
        /// </param>
        /// <returns>
        /// The view; the card itself is not changed.
        /// </returns>
        private static Netlist PageView(Netlist netlist, DrawnPage page)
        {
            List<Part> parts = new();
            for (int i = 0; i < netlist.Parts.Count; i++)
            {
                if (page.Parts.Contains(netlist.Parts[i].Ref))
                    parts.Add(netlist.Parts[i]);
            }
            List<Gate> gates = new();
            for (int i = 0; i < netlist.Gates.Count; i++)
            {
                if (page.Parts.Contains(netlist.Gates[i].Part))
                    gates.Add(netlist.Gates[i]);
            }
            List<BlockInstance>? instances = null;
            for (int i = 0; netlist.Instances != null && i < netlist.Instances.Count; i++)
            {
                if (page.Instances.Contains(netlist.Instances[i].Name))
                    (instances ??= new List<BlockInstance>()).Add(netlist.Instances[i]);
            }
            string first = netlist.Pages![0].Id;
            SheetLayoutSection? layout = null;
            if (netlist.Layout != null)
            {
                layout = new SheetLayoutSection();
                for (int i = 0; i < netlist.Layout.Wires.Count; i++)
                {
                    if ((netlist.Layout.Wires[i].Page ?? first) == page.CardPage)
                        layout.Wires.Add(netlist.Layout.Wires[i]);
                }
                for (int i = 0; netlist.Layout.Buses != null && i < netlist.Layout.Buses.Count; i++)
                {
                    if ((netlist.Layout.Buses[i].Page ?? first) == page.CardPage)
                        (layout.Buses ??= new List<BusLine>()).Add(netlist.Layout.Buses[i]);
                }
            }
            return new Netlist
            {
                Format = netlist.Format,
                FormatVersion = netlist.FormatVersion,
                Sheet = netlist.Sheet,
                Parts = parts,
                Gates = gates,
                Nets = netlist.Nets,
                Layout = layout,
                Connectors = netlist.Connectors,
                Board = netlist.Board,
                Pcb = netlist.Pcb,
                Sources = netlist.Sources,
                Photos = netlist.Photos,
                Switches = netlist.Switches,
                Display = netlist.Display,
                Blocks = netlist.Blocks,
                Instances = instances,
                Pages = netlist.Pages,
                PageLayout = netlist.PageLayout,
            };
        }

        /// <summary>
        /// The off-page marker after a signal's label: a small box naming the other drawn pages
        /// the signal is on ("p. 2, 3"), the way a hand-drawn schematic writes the sheet a line
        /// goes on to. Nothing is drawn for a signal that is on this page only.
        /// </summary>
        /// <param name="d">
        /// The drawing.
        /// </param>
        /// <param name="p">
        /// The labelled pin.
        /// </param>
        /// <param name="pages">
        /// The card's drawn pages.
        /// </param>
        /// <param name="pageNo">
        /// The page being drawn.
        /// </param>
        private static void DrawOffPage(SchematicDrawing d, PinPlan p, PageSet pages, int pageNo)
        {
            Net net = p.Net!;
            List<int> on = pages.PagesOf(net);
            StringBuilder text = new("p. ");
            List<string> ids = new();
            for (int i = 0; i < on.Count; i++)
            {
                if (on[i] == pageNo)
                    continue;
                if (ids.Count > 0)
                    text.Append(", ");
                text.Append(pages.Pages[on[i]].Id);
                ids.Add(pages.Pages[on[i]].Id);
            }
            if (ids.Count == 0)
                return;
            string key = NetKey(net);
            d.OffPage[key] = ids;
            string owner = OffPageOwner + key;
            double x = p.X + (p.Side * LabelOffset);
            double nameEnd = p.Side < 0 ? x - Diamond - 3 - NameWidth(net, d.NameStyle) : x + Diamond + 3 + NameWidth(net, d.NameStyle);
            double size = PinFont;
            double w = 0.6 * size * text.Length;
            double left = p.Side < 0 ? nameEnd - 5 - w - 4 : nameEnd + 5;
            d.Items.Add(new DrawItem
            {
                Kind = DrawKind.Rect,
                Role = DrawRole.OffPage,
                Confidence = Confidence.Read,
                Owner = owner,
                X1 = left,
                Y1 = p.Y - size + 1,
                X2 = w + 4,
                Y2 = size + 3,
            });
            Text(d, DrawRole.OffPage, Confidence.Read, owner, left + 2, p.Y + 3, text.ToString(), size, TextAnchor.Start);
        }
    }
}
