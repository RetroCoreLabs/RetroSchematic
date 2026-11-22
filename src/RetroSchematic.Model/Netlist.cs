//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;

namespace RetroSchematic.Model
{
    /// <summary>How sure a reading of the drawing is. See docs/FORMAT.md.</summary>
    public enum Confidence
    {
        /// <summary>Read off the drawing.</summary>
        Read,

        /// <summary>Worked out from the drawing plus something else; the note says what.</summary>
        Inferred,

        /// <summary>Not known.</summary>
        Open,
    }

    /// <summary>Which level of a signal is its active state (ND subscript 1 = high, 0 = low).</summary>
    public enum ActiveLevel
    {
        /// <summary>Subscript 1: active when high.</summary>
        High,

        /// <summary>Subscript 0: active when low.</summary>
        Low,

        /// <summary>No subscript (supply, serial data).</summary>
        None,
    }

    /// <summary>What kind of output drives a signal line.</summary>
    public enum LineDrive
    {
        /// <summary>Normal TTL output: drives both high and low. Two on one line is a fault.</summary>
        TotemPole,

        /// <summary>
        /// Open collector: can only pull low. Several may share the line; it needs a pull-up and
        /// reads as the AND of all of them (low if any pulls low).
        /// </summary>
        OpenCollector,

        /// <summary>
        /// Open collector rated for a pull-up above 5 V (7406/7407 up to 30 V, 7416/7417 up to
        /// 15 V). Same logic as <see cref="OpenCollector"/>; the pull-up voltage is in PullUp.
        /// </summary>
        OpenCollectorHighVoltage,

        /// <summary>Three-state bus driver: high, low or switched off.</summary>
        ThreeState,

        /// <summary>No output on the sheet drives it (supply, or an input from outside the sheet).</summary>
        None,
    }

    /// <summary>The logic function of one gate.</summary>
    public enum GateKind
    {
        /// <summary>NAND.</summary>
        Nand,

        /// <summary>NOR.</summary>
        Nor,

        /// <summary>Inverter.</summary>
        Not,

        /// <summary>AND.</summary>
        And,

        /// <summary>OR.</summary>
        Or,

        /// <summary>Non-inverting buffer.</summary>
        Buffer,

        /// <summary>Anything else; drawn as a labelled box.</summary>
        Other,
    }

    /// <summary>One sheet of an ND card drawing: format version 1 (docs/FORMAT.md).</summary>
    public sealed class Netlist
    {
        /// <summary>Always "nd-netlist".</summary>
        public string Format { get; set; } = "";

        /// <summary>The format version; a reader refuses one it does not know.</summary>
        public int FormatVersion { get; set; }

        /// <summary>The drawing this file describes.</summary>
        public Sheet Sheet { get; set; } = new();

        /// <summary>The chips on the sheet.</summary>
        public List<Part> Parts { get; set; } = new();

        /// <summary>Gates inside the chips.</summary>
        public List<Gate> Gates { get; set; } = new();

        /// <summary>Signal lines and the pins they join.</summary>
        public List<Net> Nets { get; set; } = new();
    }

    /// <summary>The drawing's title block and where the scan is.</summary>
    public sealed class Sheet
    {
        /// <summary>Sheet number, e.g. 2A50.</summary>
        public string Id { get; set; } = "";

        /// <summary>Title as printed.</summary>
        public string Title { get; set; } = "";

        /// <summary>Machine, e.g. NORD-10.</summary>
        public string? Machine { get; set; }

        /// <summary>Company in the title block.</summary>
        public string? Company { get; set; }

        /// <summary>Date drawn.</summary>
        public string? Drawn { get; set; }

        /// <summary>Date of the last update.</summary>
        public string? Updated { get; set; }

        /// <summary>Where the scan is.</summary>
        public SourceReference Source { get; set; } = new();

        /// <summary>Free text.</summary>
        public string? Note { get; set; }
    }

    /// <summary>A scanned document and a page in it.</summary>
    public sealed class SourceReference
    {
        /// <summary>Path relative to src/.</summary>
        public string Document { get; set; } = "";

        /// <summary>1-based PDF page.</summary>
        public int? PdfPage { get; set; }

        /// <summary>Rendered page image, relative to src/.</summary>
        public string? Image { get; set; }
    }

    /// <summary>One chip.</summary>
    public sealed class Part
    {
        /// <summary>Board position as printed on the drawing (13D, 7-8C-D).</summary>
        public string Ref { get; set; } = "";

        /// <summary>Part type (7442, COM2601).</summary>
        public string Type { get; set; } = "";

        /// <summary>What it does on this card.</summary>
        public string? Function { get; set; }

        /// <summary>How sure.</summary>
        public Confidence Confidence { get; set; }

        /// <summary>Free text.</summary>
        public string? Note { get; set; }
    }

    /// <summary>One gate inside a part.</summary>
    public sealed class Gate
    {
        /// <summary>Unique id, e.g. 11D/1.</summary>
        public string Id { get; set; } = "";

        /// <summary>The part ref it is in.</summary>
        public string Part { get; set; } = "";

        /// <summary>Logic function.</summary>
        public GateKind Kind { get; set; }

        /// <summary>Input pin numbers.</summary>
        public List<int> Inputs { get; set; } = new();

        /// <summary>Output pin number.</summary>
        public int Output { get; set; }

        /// <summary>Where it is drawn on the scan (mm from top left), or null when not measured.</summary>
        public Point? Position { get; set; }

        /// <summary>How sure.</summary>
        public Confidence Confidence { get; set; }

        /// <summary>Free text.</summary>
        public string? Note { get; set; }
    }

    /// <summary>A position in millimetres.</summary>
    public sealed class Point
    {
        /// <summary>From the left.</summary>
        public double X { get; set; }

        /// <summary>From the top.</summary>
        public double Y { get; set; }
    }

    /// <summary>One signal line.</summary>
    public sealed class Net
    {
        /// <summary>Name as on the drawing, without subscript.</summary>
        public string Name { get; set; } = "";

        /// <summary>The subscript.</summary>
        public ActiveLevel Active { get; set; }

        /// <summary>What drives the line, or null when not read yet.</summary>
        public LineDrive? Drive { get; set; }

        /// <summary>How sure <see cref="Drive"/> is.</summary>
        public Confidence? DriveConfidence { get; set; }

        /// <summary>For an open-collector line: the pull-up as written on the drawing, or "open".</summary>
        public string? PullUp { get; set; }

        /// <summary>The pins it joins.</summary>
        public List<PinReference> Pins { get; set; } = new();

        /// <summary>Free text.</summary>
        public string? Note { get; set; }
    }

    /// <summary>One pin on a net.</summary>
    public sealed class PinReference
    {
        /// <summary>&lt;part ref&gt;.&lt;pin&gt;, P.&lt;n&gt; backplane or F.&lt;n&gt; front connector.</summary>
        public string At { get; set; } = "";

        /// <summary>How sure.</summary>
        public Confidence Confidence { get; set; }

        /// <summary>Free text.</summary>
        public string? Note { get; set; }

        /// <summary>The part ref: everything before the last dot.</summary>
        public string PartRef => At.Substring(0, At.LastIndexOf('.'));

        /// <summary>The pin number: everything after the last dot.</summary>
        public int Pin => int.Parse(At.Substring(At.LastIndexOf('.') + 1), System.Globalization.CultureInfo.InvariantCulture);
    }
}
