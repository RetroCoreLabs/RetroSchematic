//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;

namespace RetroSchematic.Model
{
    /// <summary>
    /// What the printed circuit board is (the sheet's optional "board" section, docs/FORMAT.md):
    /// which ND machine it is for, its board number, print and ECO versions, size and layers.
    /// Every value is optional: a missing one is not known.
    /// </summary>
    public sealed class BoardSection
    {
        /// <summary>
        /// The ND machine the card plugs into: "NORD-10" or "ND-100".
        /// </summary>
        public string? NdSystem { get; set; }

        /// <summary>
        /// The board (part) number printed on the card, such as 322049.
        /// </summary>
        public string? BoardNumber { get; set; }

        /// <summary>
        /// The print (PCB) version, such as "I".
        /// </summary>
        public string? PrintVersion { get; set; }

        /// <summary>
        /// The ECO (engineering change) version, such as "3".
        /// </summary>
        public string? EcoVersion { get; set; }

        /// <summary>
        /// The board's size in millimetres.
        /// </summary>
        public BoardSize? Size { get; set; }

        /// <summary>
        /// How many copper layers the board has.
        /// </summary>
        public int? Layers { get; set; }

        /// <summary>
        /// Free text: where the values were read.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// A board's width and height in millimetres.
    /// </summary>
    public sealed class BoardSize
    {
        /// <summary>
        /// Width in mm (along the backplane edge).
        /// </summary>
        public double WidthMm { get; set; }

        /// <summary>
        /// Height in mm (away from the backplane edge).
        /// </summary>
        public double HeightMm { get; set; }
    }

    /// <summary>
    /// One original schematic page the sheet was read from (the "sources" list). The file is
    /// relative to the sheet file; in a .rsx it is an entry inside the zip ("sources/...").
    /// </summary>
    public sealed class SourcePage
    {
        /// <summary>
        /// The file: a picture (PNG, JPEG) or a PDF, relative to the sheet, with forward slashes.
        /// </summary>
        public string File { get; set; } = "";

        /// <summary>
        /// For a PDF: which page (1 = first). Null for a picture.
        /// </summary>
        public int? PdfPage { get; set; }

        /// <summary>
        /// A title for the page picker, such as "2A50 page 1".
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Where a stored picture came from when it was made from a PDF page (packing a .rsx
        /// renders a PDF page as PNG at 300 dpi and keeps the PDF's name and page here).
        /// </summary>
        public SourceOrigin? Original { get; set; }
    }

    /// <summary>
    /// The PDF file and page a stored picture was rendered from.
    /// </summary>
    public sealed class SourceOrigin
    {
        /// <summary>
        /// The PDF file as it was named before packing.
        /// </summary>
        public string File { get; set; } = "";

        /// <summary>
        /// The PDF page (1 = first).
        /// </summary>
        public int? PdfPage { get; set; }

        /// <summary>
        /// The resolution it was rendered at, dots per inch.
        /// </summary>
        public int? Dpi { get; set; }
    }

    /// <summary>
    /// Which side of a board a photo shows.
    /// </summary>
    public enum BoardSide
    {
        /// <summary>
        /// The component side (the chips).
        /// </summary>
        Component,

        /// <summary>
        /// The solder side (the copper and solder joints).
        /// </summary>
        Solder,
    }

    /// <summary>
    /// One photo of the printed circuit board (the "photos" list).
    /// </summary>
    public sealed class BoardPhoto
    {
        /// <summary>
        /// The picture file, relative to the sheet; in a .rsx an entry "photos/...".
        /// </summary>
        public string File { get; set; } = "";

        /// <summary>
        /// Which side it shows.
        /// </summary>
        public BoardSide Side { get; set; }

        /// <summary>
        /// Free text: which board, which print version, anything seen on it.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// One switch block on the card (the "switches" list): where it is, how many levers, what
    /// each lever sets and what "on" means.
    /// </summary>
    public sealed class SwitchBlock
    {
        /// <summary>
        /// The board position, as for a chip: 1E, 15E.
        /// </summary>
        public string Ref { get; set; } = "";

        /// <summary>
        /// What the whole block sets, such as "device address".
        /// </summary>
        public string? Function { get; set; }

        /// <summary>
        /// The number of levers.
        /// </summary>
        public int LeverCount { get; set; }

        /// <summary>
        /// The bit value a lever in the "on" (closed) position gives: "0", "1" or "open".
        /// </summary>
        public string? OnMeans { get; set; }

        /// <summary>
        /// How sure <see cref="OnMeans"/> is.
        /// </summary>
        public Confidence? OnMeansConfidence { get; set; }

        /// <summary>
        /// The usual setting, in words.
        /// </summary>
        public string? Typical { get; set; }

        /// <summary>
        /// One entry per lever, lever 1 first.
        /// </summary>
        public List<SwitchLever> Levers { get; set; } = new();

        /// <summary>
        /// Where the facts come from.
        /// </summary>
        public DescriptionSource? Source { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// One lever of a switch block.
    /// </summary>
    public sealed class SwitchLever
    {
        /// <summary>
        /// The lever number as on the switch (1 first).
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// The signal the lever sets, as on the drawing (SMN3, IDENT2).
        /// </summary>
        public string? Signal { get; set; }

        /// <summary>
        /// What it sets: "BA9", "ident bit 2".
        /// </summary>
        public string? Bit { get; set; }

        /// <summary>
        /// How sure the lever-to-bit reading is.
        /// </summary>
        public Confidence Confidence { get; set; }

        /// <summary>
        /// Where the lever stands on a known card ("on", "off") or "open" when it cannot be read.
        /// </summary>
        public string? Setting { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }
    }
}
