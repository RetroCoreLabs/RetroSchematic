//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;

namespace RetroSchematic.Model
{
    /// <summary>
    /// Where a read symbol is drawn on an original page (format version 3, <c>symbolBoxes[]</c>):
    /// a chip, one gate of a chip, a passive part, a circled backplane pin or a label. The boxes
    /// say nothing about what is connected; they tie the reading to the drawing, and they are the
    /// training data for the symbol finder (docs/PLAN.md, "Symbol boxes on the scanned page").
    /// </summary>
    public sealed class SymbolBox
    {
        /// <summary>
        /// The original page the box is on: its place in the card's <c>sources</c>, 1 = first.
        /// </summary>
        public int Source { get; set; }

        /// <summary>
        /// Left edge in pixels of the source picture (for a PDF source: of the page rendered at
        /// <see cref="Dpi"/>).
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Top edge in pixels.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Width in pixels.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Height in pixels.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// For a PDF source: the dots per inch the pixels are counted at. Null for a picture
        /// source (its own pixels).
        /// </summary>
        public int? Dpi { get; set; }

        /// <summary>
        /// What is in the box: one of <see cref="SymbolBoxes.Kinds"/> (<c>chip</c>, <c>gate</c>,
        /// <c>resistor</c>, <c>circle</c>, <c>label</c>, ...).
        /// </summary>
        public string Kind { get; set; } = "";

        /// <summary>
        /// The part it is (<c>13D</c>, <c>R5</c>), or null for a circle, label, dot or diamond.
        /// </summary>
        public string? Ref { get; set; }

        /// <summary>
        /// For a gate: which gate of the part, 1 = the part's first gate; null when not known.
        /// </summary>
        public int? Gate { get; set; }

        /// <summary>
        /// The gate kind the box shows (<c>nand</c>, <c>nor</c>, ...), as the finder or the reader
        /// saw it; for training. Null when not given.
        /// </summary>
        public string? GateKind { get; set; }

        /// <summary>
        /// The text of a label, or the pin a circle names (<c>P.81</c>); null otherwise.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Who drew the box: <c>hand</c> (drawn by a person) or <c>tool</c> (found by the page
        /// analyser and accepted by a person or an agent).
        /// </summary>
        public string By { get; set; } = "hand";

        /// <summary>
        /// For a box by the tool: how sure the finder was, 0..1. Null otherwise.
        /// </summary>
        public double? Score { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// The checks a reader makes on <c>symbolBoxes[]</c>, and small helpers.
    /// </summary>
    public static class SymbolBoxes
    {
        /// <summary>
        /// The things a box may hold: the page analyser's classes plus <c>label</c>.
        /// </summary>
        public static readonly string[] Kinds =
        {
            "chip", "gate", "resistor", "capacitor", "switch", "strap", "ground", "supply",
            "circle", "offpage", "dot", "label",
        };

        /// <summary>
        /// True when <paramref name="kind"/> is one of <see cref="Kinds"/>.
        /// </summary>
        /// <param name="kind">
        /// The kind.
        /// </param>
        /// <returns>
        /// Whether it is allowed.
        /// </returns>
        public static bool IsKind(string kind)
        {
            for (int i = 0; i < Kinds.Length; i++)
            {
                if (Kinds[i] == kind)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks the boxes: version 3 only; each names a source page the card has, has a size,
        /// a known kind, a part that exists (when it names one) and <c>by</c> hand or tool.
        /// </summary>
        /// <param name="netlist">
        /// The file as read.
        /// </param>
        /// <exception cref="NetlistException">
        /// A check fails.
        /// </exception>
        public static void Check(Netlist netlist)
        {
            List<SymbolBox>? boxes = netlist.SymbolBoxes;
            if (boxes == null)
                return;
            if (netlist.FormatVersion != NetlistReader.PagesFormatVersion)
                throw new NetlistException("Symbol boxes need format version 3.");
            int sources = netlist.Sources?.Count ?? 0;
            HashSet<string> parts = new(StringComparer.Ordinal);
            for (int i = 0; i < netlist.Parts.Count; i++)
                parts.Add(netlist.Parts[i].Ref);
            for (int i = 0; i < boxes.Count; i++)
            {
                SymbolBox b = boxes[i];
                string what = $"Symbol box {i + 1}";
                if (b.Source < 1 || b.Source > sources)
                    throw new NetlistException($"{what} is on source {b.Source}, but the card has {sources} source page(s).");
                if (!(b.Width > 0) || !(b.Height > 0))
                    throw new NetlistException($"{what} has no size.");
                if (!IsKind(b.Kind))
                    throw new NetlistException($"{what} holds \"{b.Kind}\", which is not a kind a symbol box can hold.");
                if (b.Ref != null && !parts.Contains(b.Ref))
                    throw new NetlistException($"{what} names part {b.Ref}, which is not on the card.");
                if (b.Gate != null && (b.Gate < 1 || b.Ref == null))
                    throw new NetlistException($"{what} names gate {b.Gate} without a part, or a gate below 1.");
                if (b.By != "hand" && b.By != "tool")
                    throw new NetlistException($"{what} was drawn by \"{b.By}\"; it must be hand or tool.");
                if (b.Dpi != null && b.Dpi <= 0)
                    throw new NetlistException($"{what} has a dpi of {b.Dpi}.");
            }
        }

        /// <summary>
        /// The part whose box on a source page holds a point (the smallest such box), or null.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <param name="source">
        /// The source page, 1 = first.
        /// </param>
        /// <param name="x">
        /// Point x in the page's pixels.
        /// </param>
        /// <param name="y">
        /// Point y.
        /// </param>
        /// <param name="margin">
        /// How far outside a box still counts (a wire end sits just outside the symbol).
        /// </param>
        /// <returns>
        /// The box, or null.
        /// </returns>
        public static SymbolBox? PartBoxAt(Netlist netlist, int source, double x, double y, double margin)
        {
            List<SymbolBox>? boxes = netlist.SymbolBoxes;
            if (boxes == null)
                return null;
            SymbolBox? best = null;
            double bestDist = double.MaxValue;
            for (int i = 0; i < boxes.Count; i++)
            {
                SymbolBox b = boxes[i];
                if (b.Source != source || b.Ref == null)
                    continue;
                double dx = Math.Max(0, Math.Max(b.X - x, x - (b.X + b.Width)));
                double dy = Math.Max(0, Math.Max(b.Y - y, y - (b.Y + b.Height)));
                double d = Math.Max(dx, dy);
                if (d > margin)
                    continue;

                // inside beats near; among the boxes holding the point, the smallest
                double key = d > 0 ? 1e12 + d : b.Width * b.Height;
                if (key < bestDist)
                {
                    bestDist = key;
                    best = b;
                }
            }
            return best;
        }
    }
}
