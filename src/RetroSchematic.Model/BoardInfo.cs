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
    /// which ND machine it is for, its card and part numbers, print and ECO versions, size and
    /// layers, and the lists the ND PCB catalogue gives (LEDs, straps, switches, connectors,
    /// chips). The field names are the catalogue's own (pcb.json, scraped from norsk-data.com)
    /// wherever a field means the same, so an import is a straight copy. Every value is
    /// optional: a missing one is not known.
    /// </summary>
    public sealed class BoardSection
    {
        /// <summary>
        /// The card number ND gave the board, such as "1050" (the catalogue's "number").
        /// </summary>
        public string? Number { get; set; }

        /// <summary>
        /// The part number printed on the board, such as "322049" (was "boardNumber" before
        /// 2026-11-23).
        /// </summary>
        public string? PartNumber { get; set; }

        /// <summary>
        /// The board's name, such as "Synchronous Modem Buffer".
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// The ND machine the card plugs into: "NORD-10" or "ND-100". (The catalogue writes
        /// "10" and "100"; an import turns them into these names.)
        /// </summary>
        public string? NdSystem { get; set; }

        /// <summary>
        /// The catalogue's group code for the board, such as "1C". What the codes mean is not
        /// written in the catalogue.
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// What the board is, in words.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The print (PCB) version, such as "I".
        /// </summary>
        public string? PrintVersion { get; set; }

        /// <summary>
        /// The ECO (engineering change) version, such as "3".
        /// </summary>
        public string? EcoVersion { get; set; }

        /// <summary>
        /// Every print version known, with its ECO version.
        /// </summary>
        public List<BoardPrintVersion>? PrintVersions { get; set; }

        /// <summary>
        /// Size and layers.
        /// </summary>
        public BoardSpecifications? Specifications { get; set; }

        /// <summary>
        /// The release date as the catalogue writes it, such as "01. Jan. 1981".
        /// </summary>
        public string? ReleaseDate { get; set; }

        /// <summary>
        /// The LEDs on the board, in the catalogue's words.
        /// </summary>
        public List<BoardItem>? Leds { get; set; }

        /// <summary>
        /// The straps (and test points) on the board, in the catalogue's words.
        /// </summary>
        public List<BoardItem>? Straps { get; set; }

        /// <summary>
        /// The switches, in the catalogue's words. The sheet's own "switches" list holds the
        /// worked-out switch blocks; this is the catalogue text as it is.
        /// </summary>
        public List<BoardItem>? Switches { get; set; }

        /// <summary>
        /// The connectors, in the catalogue's words. The sheet's own "connectors" list holds the
        /// connectors as used by the signals.
        /// </summary>
        public List<BoardItem>? Connectors { get; set; }

        /// <summary>
        /// The special chips the catalogue names, with a link to a datasheet where it has one.
        /// </summary>
        public List<BoardChip>? Chipset { get; set; }

        /// <summary>
        /// The web page the catalogue entry was scraped from.
        /// </summary>
        public string? SourceUrl { get; set; }

        /// <summary>
        /// Where each fact came from: the field name ("title", "specifications" ...) and its
        /// source. A field not named here was typed in by hand (see <see cref="Note"/>).
        /// </summary>
        public Dictionary<string, DescriptionSource>? FactSources { get; set; }

        /// <summary>
        /// Free text: where the values were read.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// One print version of a board and its ECO version.
    /// </summary>
    public sealed class BoardPrintVersion
    {
        /// <summary>
        /// The print version, such as "I".
        /// </summary>
        public string Print { get; set; } = "";

        /// <summary>
        /// The ECO version, such as "3".
        /// </summary>
        public string? Eco { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// A board's size and copper layers (the catalogue's "specifications").
    /// </summary>
    public sealed class BoardSpecifications
    {
        /// <summary>
        /// The size in millimetres. (The catalogue writes "156 x 149 mm"; an import reads the
        /// two numbers.)
        /// </summary>
        public BoardSize? Size { get; set; }

        /// <summary>
        /// How many copper layers the board has.
        /// </summary>
        public int? Layers { get; set; }
    }

    /// <summary>
    /// One LED, strap, switch or connector as the catalogue lists it.
    /// </summary>
    public sealed class BoardItem
    {
        /// <summary>
        /// Its name on the board, such as "OCTO" or "MACK"; often empty.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// For an LED: its colour.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// The catalogue's text, as it is.
        /// </summary>
        public string? Text { get; set; }
    }

    /// <summary>
    /// One chip the catalogue names for a board.
    /// </summary>
    public sealed class BoardChip
    {
        /// <summary>
        /// The chip, such as "COM2601".
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// What it is, such as "USRT".
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Where its datasheet was on the web, as the catalogue gives it (not checked).
        /// </summary>
        public string? Url { get; set; }
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
        /// The print version of the board in the photo, such as "I".
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Which variant of the board the photo shows when there are several, such as "old"
        /// or "new" (the catalogue's words). The photo views let the reader choose.
        /// </summary>
        public string? Variant { get; set; }

        /// <summary>
        /// A short caption, such as "Component side, PCB version I, old".
        /// </summary>
        public string? Caption { get; set; }

        /// <summary>
        /// Where the photo came from, or null when not recorded.
        /// </summary>
        public DescriptionSource? Source { get; set; }

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
        /// Further sources that say something about the block, such as the ND PCB catalogue's
        /// switch text (kept word for word in <see cref="DescriptionSource.Quote"/>).
        /// </summary>
        public List<DescriptionSource>? MoreSources { get; set; }

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
