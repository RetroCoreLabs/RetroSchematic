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
    /// Assisted reading of an original page: symbol boxes (format version 3, <c>symbolBoxes[]</c>),
    /// accepting a find of the page analyser as a part or linking it to one, and connecting a pin
    /// at a place on the page to a signal. Every change is one undo step.
    /// </summary>
    public sealed partial class SheetEditor
    {
        /// <summary>
        /// How far outside a part's box a click still picks that part, in page pixels: a wire end
        /// sits just outside the symbol.
        /// </summary>
        public const double WireEndMargin = 40;

        /// <summary>
        /// Adds a symbol box, or replaces box <paramref name="index"/>. A card without pages is
        /// converted to version 3 first, in the same undo step.
        /// </summary>
        /// <param name="box">
        /// The box.
        /// </param>
        /// <param name="index">
        /// The box to replace (0 first), or -1 to add.
        /// </param>
        /// <returns>
        /// The box's place in the list, 0 first.
        /// </returns>
        public int SetSymbolBox(SymbolBox box, int index = -1)
        {
            CheckWritable();
            CheckTopLevel();
            CheckBox(box);
            int count = Root.SymbolBoxes?.Count ?? 0;
            if (index >= count)
                throw new SheetEditException($"There is no symbol box {index + 1}.");
            Snapshot();
            CardPages.ToVersion3(Root);
            int at = PutBox(NetlistReader.Clone(box), index);
            Done();
            return at;
        }

        /// <summary>
        /// Removes symbol box <paramref name="index"/> (0 first).
        /// </summary>
        /// <param name="index">
        /// The box.
        /// </param>
        public void RemoveSymbolBox(int index)
        {
            CheckWritable();
            if (Root.SymbolBoxes == null || index < 0 || index >= Root.SymbolBoxes.Count)
                throw new SheetEditException($"There is no symbol box {index + 1}.");
            Snapshot();
            Root.SymbolBoxes.RemoveAt(index);
            if (Root.SymbolBoxes.Count == 0)
                Root.SymbolBoxes = null;
            Done();
        }

        /// <summary>
        /// Accepts a find of the page analyser: the box is stored as a symbol box drawn by the tool,
        /// and, for a chip or a gate, tied to a part. With <paramref name="linkTo"/> the box goes on
        /// that existing part. Otherwise a part at <paramref name="reference"/> is used when there
        /// is one (another gate of a package already accepted), else a new part of
        /// <paramref name="type"/> is made there (confidence inferred, on the page read from this
        /// source). One undo step.
        /// </summary>
        /// <param name="box">
        /// The find's box, with its source, kind, gate kind and score; <c>ref</c>, <c>gate</c> and
        /// <c>by</c> are filled in here.
        /// </param>
        /// <param name="reference">
        /// The position code to use (the one read, or as corrected), or null.
        /// </param>
        /// <param name="type">
        /// The chip type for a new part (from the catalogue), or null.
        /// </param>
        /// <param name="linkTo">
        /// An existing part to link the box to instead, or null.
        /// </param>
        /// <returns>
        /// The part the box is tied to, or null for a box without a part (a circle, a dot).
        /// </returns>
        public string? AcceptFind(SymbolBox box, string? reference, string? type, string? linkTo)
        {
            CheckWritable();
            CheckTopLevel();
            SymbolBox b = NetlistReader.Clone(box);
            b.By = "tool";
            b.Ref = null;
            b.Gate = null;
            CheckBox(b);
            bool symbol = b.Kind == "chip" || b.Kind == "gate";
            string? part = null;
            ChipInfo? chip = null;
            bool make = false;
            if (linkTo != null)
            {
                if (FindPart(linkTo) == null)
                    throw new SheetEditException($"There is no part {linkTo} on the card.");
                part = linkTo;
            }
            else if (symbol)
            {
                if (string.IsNullOrWhiteSpace(reference))
                    throw new SheetEditException("A chip or gate needs a position code (13D), or a part to link to.");
                reference = reference.Trim();
                if (FindPart(reference) != null)
                    part = reference;
                else
                {
                    CheckBoardPosition(reference);
                    if (Catalog == null)
                        throw new SheetEditException("No chip catalogue is open, so no part can be made.");
                    if (string.IsNullOrWhiteSpace(type))
                        throw new SheetEditException($"There is no part {reference} yet; give its chip type to make it.");
                    chip = Catalog.Get(type.Trim()) ?? throw new SheetEditException($"The catalogue has no chip file with pins for {type}.");
                    part = reference;
                    make = true;
                }
            }

            Snapshot();
            CardPages.ToVersion3(Root);
            if (make)
            {
                string page = PageOfSource(b.Source);
                Netlist.Parts.Add(new Part
                {
                    Ref = part!,
                    Type = chip!.Id,
                    Confidence = Confidence.Inferred,
                    Note = "Accepted from the page analyser.",
                    Page = page,
                });
                for (int i = 0; i < chip.Gates.Count; i++)
                {
                    Gate from = chip.Gates[i];
                    Netlist.Gates.Add(new Gate
                    {
                        Id = part + "/" + (i + 1).ToString(CultureInfo.InvariantCulture),
                        Part = part!,
                        Kind = from.Kind,
                        Inputs = new List<int>(from.Inputs),
                        Output = from.Output,
                        Confidence = Confidence.Inferred,
                    });
                }
            }

            if (part != null)
            {
                b.Ref = part;
                if (b.Kind == "gate")
                    b.Gate = NextGateBox(part);
            }
            PutBox(b, -1);
            Done();
            return part;
        }

        /// <summary>
        /// Connects a pin of the part drawn at a place on an original page to a signal: the part is
        /// the one whose symbol box holds the point (or is within <see cref="WireEndMargin"/> of
        /// it), the pin is given by number. <paramref name="to"/> is another pin (<c>13D.5</c>) or a
        /// signal's name; the pin joins that signal (or the two pins make a new one).
        /// </summary>
        /// <param name="source">
        /// The original page, 1 = first.
        /// </param>
        /// <param name="x">
        /// The wire end, page pixels.
        /// </param>
        /// <param name="y">
        /// The wire end, page pixels.
        /// </param>
        /// <param name="pin">
        /// The pin number on the part.
        /// </param>
        /// <param name="to">
        /// A pin or a signal name.
        /// </param>
        /// <returns>
        /// The connected pin (<c>13D.5</c>) and the signal it is now on.
        /// </returns>
        public (string Pin, string Signal) ConnectPinOnPage(int source, double x, double y, int pin, string to)
        {
            CheckWritable();
            SymbolBox box = SymbolBoxes.PartBoxAt(Root, source, x, y, WireEndMargin)
                ?? throw new SheetEditException($"No part's box is at ({x.ToString("0", CultureInfo.InvariantCulture)}, {y.ToString("0", CultureInfo.InvariantCulture)}) on source page {source}.");
            string p = box.Ref + "." + pin.ToString(CultureInfo.InvariantCulture);
            return (p, ConnectTo(p, to));
        }

        /// <summary>
        /// Puts a pin on a signal: <paramref name="to"/> is another pin or a signal's name.
        /// </summary>
        /// <param name="pin">
        /// The pin (<c>13D.5</c>).
        /// </param>
        /// <param name="to">
        /// A pin or a signal name.
        /// </param>
        /// <returns>
        /// The signal the pin is now on.
        /// </returns>
        public string ConnectTo(string pin, string to)
        {
            CheckWritable();
            if (string.IsNullOrWhiteSpace(to))
                throw new SheetEditException("Say which pin or signal the pin goes on.");
            to = to.Trim();
            Net? net = FindNet(to);
            if (net != null)
            {
                // only the new pin is checked: the signal's own pins are in the file already (some
                // are on chips with no catalogue file, which Connect would refuse to check)
                CheckPin(pin);
                Net? on = NetOf(pin);
                if (on == net)
                    throw new SheetEditException($"Pin {pin} is on signal {net.Name} already.");
                if (on != null)
                    throw new SheetEditException($"Pin {pin} is on signal {on.Name}. A pin can be on one signal only; disconnect it first.");
                Snapshot();
                FindNet(to)!.Pins.Add(new PinReference { At = pin, Confidence = Confidence.Read });
                Done();
                return net.Name;
            }
            return Connect(to, pin);
        }

        /// <summary>
        /// The pins of a part's chip type from the catalogue (number and name), for a pin chooser.
        /// </summary>
        /// <param name="reference">
        /// The part.
        /// </param>
        /// <returns>
        /// The pins, or an empty list when the type has no pin table.
        /// </returns>
        public List<ChipPin> PinoutOf(string reference)
        {
            Part? part = FindPart(reference);
            ChipInfo? chip = part == null ? null : Catalog?.Get(part.Type);
            return chip == null ? new List<ChipPin>() : chip.Pins;
        }

        /// <summary>
        /// The picture of an original page, as stored, for reading: a picture source as it is, a
        /// PDF source rendered at <paramref name="pdfDpi"/>.
        /// </summary>
        /// <param name="source">
        /// The original page, 1 = first.
        /// </param>
        /// <param name="pdfDpi">
        /// The dpi to render a PDF page at.
        /// </param>
        /// <param name="dpi">
        /// The dpi of a rendered PDF page (the symbol boxes' <c>dpi</c>), null for a picture.
        /// </param>
        /// <returns>
        /// The PNG or JPEG bytes.
        /// </returns>
        public byte[] SourcePicture(int source, int pdfDpi, out int? dpi)
        {
            dpi = null;
            int sources = Root.Sources?.Count ?? 0;
            if (source < 1 || source > sources)
                throw new SheetEditException($"Source {source} does not exist: the card has {sources} source page(s).");
            SourcePage s = Root.Sources![source - 1];
            byte[] data = Files.Read(s.File) ?? throw new SheetEditException("The page file " + s.File + " cannot be found.");
            if (!SheetAttachments.IsPdf(s.File))
                return data;
            if (Pdf == null || s.PdfPage == null)
                throw new SheetEditException("The PDF page of " + s.File + " cannot be shown.");
            dpi = pdfDpi;
            return Pdf(data, s.PdfPage.Value, pdfDpi);
        }

        private void CheckBox(SymbolBox b)
        {
            int sources = Root.Sources?.Count ?? 0;
            if (b.Source < 1 || b.Source > sources)
                throw new SheetEditException($"Source {b.Source} does not exist: the card has {sources} source page(s).");
            if (!(b.Width > 0) || !(b.Height > 0))
                throw new SheetEditException("A symbol box needs a width and a height.");
            if (!SymbolBoxes.IsKind(b.Kind))
                throw new SheetEditException($"\"{b.Kind}\" is not a kind a symbol box can hold.");
            if (b.Ref != null && FindPart(b.Ref) == null)
                throw new SheetEditException($"There is no part {b.Ref} on the card.");
            if (b.By != "hand" && b.By != "tool")
                throw new SheetEditException("A symbol box is drawn by hand or by the tool.");
        }

        private int PutBox(SymbolBox b, int index)
        {
            Root.SymbolBoxes ??= new List<SymbolBox>();
            if (index < 0)
            {
                Root.SymbolBoxes.Add(b);
                return Root.SymbolBoxes.Count - 1;
            }
            Root.SymbolBoxes[index] = b;
            return index;
        }

        // the first gate of the part with no box yet, 1 first; null when every gate has one
        private int? NextGateBox(string part)
        {
            int gates = 0;
            for (int i = 0; i < Netlist.Gates.Count; i++)
            {
                if (Netlist.Gates[i].Part == part)
                    gates++;
            }
            List<SymbolBox>? boxes = Root.SymbolBoxes;
            for (int g = 1; g <= gates; g++)
            {
                bool used = false;
                for (int i = 0; boxes != null && i < boxes.Count && !used; i++)
                    used = boxes[i].Ref == part && boxes[i].Gate == g;
                if (!used)
                    return g;
            }
            return null;
        }

        // the card page read from a source page, else the first page
        private string PageOfSource(int source)
        {
            List<CardPage> pages = Root.Pages!;
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].Source == source)
                    return pages[i].Id;
            }
            return pages[0].Id;
        }
    }
}
