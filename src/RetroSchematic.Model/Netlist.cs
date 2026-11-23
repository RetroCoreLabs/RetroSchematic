//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;

namespace RetroSchematic.Model
{
    /// <summary>
    /// How sure a reading of the drawing is. See docs/FORMAT.md.
    /// </summary>
    public enum Confidence
    {
        /// <summary>
        /// Read off the drawing.
        /// </summary>
        Read,

        /// <summary>
        /// Worked out from the drawing plus something else; the note says what.
        /// </summary>
        Inferred,

        /// <summary>
        /// Not known.
        /// </summary>
        Open,
    }

    /// <summary>
    /// Which level of a signal is its active state (ND subscript 1 = high, 0 = low).
    /// </summary>
    public enum ActiveLevel
    {
        /// <summary>
        /// Subscript 1: active when high.
        /// </summary>
        High,

        /// <summary>
        /// Subscript 0: active when low.
        /// </summary>
        Low,

        /// <summary>
        /// No subscript (supply, serial data).
        /// </summary>
        None,
    }

    /// <summary>
    /// What kind of output drives a signal line.
    /// </summary>
    public enum LineDrive
    {
        /// <summary>
        /// Normal TTL output: drives both high and low. Two on one line is a fault.
        /// </summary>
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

        /// <summary>
        /// Three-state bus driver: high, low or switched off.
        /// </summary>
        ThreeState,

        /// <summary>
        /// No output on the sheet drives it (supply, or an input from outside the sheet).
        /// </summary>
        None,
    }

    /// <summary>
    /// The logic function of one gate.
    /// </summary>
    public enum GateKind
    {
        /// <summary>
        /// NAND.
        /// </summary>
        Nand,

        /// <summary>
        /// NOR.
        /// </summary>
        Nor,

        /// <summary>
        /// Inverter.
        /// </summary>
        Not,

        /// <summary>
        /// AND.
        /// </summary>
        And,

        /// <summary>
        /// OR.
        /// </summary>
        Or,

        /// <summary>
        /// Non-inverting buffer.
        /// </summary>
        Buffer,

        /// <summary>
        /// Anything else; drawn as a labelled box.
        /// </summary>
        Other,
    }

    /// <summary>
    /// One sheet of an ND card drawing: format version 1 (docs/FORMAT.md).
    /// </summary>
    public sealed class Netlist
    {
        /// <summary>
        /// Always "nd-netlist".
        /// </summary>
        public string Format { get; set; } = "";

        /// <summary>
        /// The format version; a reader refuses one it does not know.
        /// </summary>
        public int FormatVersion { get; set; }

        /// <summary>
        /// The drawing this file describes.
        /// </summary>
        public Sheet Sheet { get; set; } = new();

        /// <summary>
        /// The chips on the sheet.
        /// </summary>
        public List<Part> Parts { get; set; } = new();

        /// <summary>
        /// Gates inside the chips.
        /// </summary>
        public List<Gate> Gates { get; set; } = new();

        /// <summary>
        /// Signal lines and the pins they join.
        /// </summary>
        public List<Net> Nets { get; set; } = new();

        /// <summary>
        /// How the sheet is laid out beyond the chip positions, or null: wire paths fixed by hand.
        /// Drawing only - it never changes what is connected to what.
        /// </summary>
        public SheetLayoutSection? Layout { get; set; }

        /// <summary>
        /// Block definitions (format version 2), or null for none. Each is a Verilog module.
        /// </summary>
        public List<BlockDefinition>? Blocks { get; set; }

        /// <summary>
        /// Instances of blocks on this level (format version 2), or null for none.
        /// </summary>
        public List<BlockInstance>? Instances { get; set; }
    }

    /// <summary>
    /// Which way a block port carries its signal, seen from inside the block.
    /// </summary>
    public enum PortDirection
    {
        /// <summary>
        /// Into the block.
        /// </summary>
        Input,

        /// <summary>
        /// Out of the block.
        /// </summary>
        Output,

        /// <summary>
        /// Both ways (a bus line, or a line driven both inside and outside).
        /// </summary>
        Inout,
    }

    /// <summary>
    /// The edge of an instance box a port pin is drawn on.
    /// </summary>
    public enum PortSide
    {
        /// <summary>
        /// The left edge.
        /// </summary>
        Left,

        /// <summary>
        /// The right edge.
        /// </summary>
        Right,

        /// <summary>
        /// The top edge.
        /// </summary>
        Top,

        /// <summary>
        /// The bottom edge.
        /// </summary>
        Bottom,
    }

    /// <summary>
    /// A signal named on one level: its name and, when two signals share it, its level.
    /// </summary>
    public sealed class SignalRef
    {
        /// <summary>
        /// The signal's name.
        /// </summary>
        public string Net { get; set; } = "";

        /// <summary>
        /// Its level; null matches the first signal of that name.
        /// </summary>
        public ActiveLevel? Active { get; set; }
    }

    /// <summary>
    /// A block definition (a Verilog module): its ports and its inside, used by name from any
    /// number of instances.
    /// </summary>
    public sealed class BlockDefinition
    {
        /// <summary>
        /// The block's name, unique in the file.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The connections to the level above, in order.
        /// </summary>
        public List<BlockPort> Ports { get; set; } = new();

        /// <summary>
        /// The chips inside.
        /// </summary>
        public List<Part> Parts { get; set; } = new();

        /// <summary>
        /// The gates of those chips.
        /// </summary>
        public List<Gate> Gates { get; set; } = new();

        /// <summary>
        /// The signals inside, including those the ports carry.
        /// </summary>
        public List<Net> Nets { get; set; } = new();

        /// <summary>
        /// Drawing of the inside, or null.
        /// </summary>
        public SheetLayoutSection? Layout { get; set; }

        /// <summary>
        /// Instances of other blocks inside this one, or null.
        /// </summary>
        public List<BlockInstance>? Instances { get; set; }
    }

    /// <summary>
    /// One port of a block: one signal (a single port, a Verilog wire) or several (a bus port,
    /// a Verilog vector, entry 0 the lowest bit).
    /// </summary>
    public sealed class BlockPort
    {
        /// <summary>
        /// The port name, unique in the block.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Into, out of, or both ways, seen from inside.
        /// </summary>
        public PortDirection Direction { get; set; }

        /// <summary>
        /// The signals inside the block this port carries.
        /// </summary>
        public List<SignalRef> Signals { get; set; } = new();

        /// <summary>
        /// The edge of the instance box it is drawn on, or null for the default.
        /// </summary>
        public PortSide? Side { get; set; }

        /// <summary>
        /// Its place along that edge (0 first, from the top or left), or null for port order.
        /// </summary>
        public int? Order { get; set; }
    }

    /// <summary>
    /// One use of a block on a level (a Verilog instantiation with named port connections).
    /// </summary>
    public sealed class BlockInstance
    {
        /// <summary>
        /// The instance name, unique on its level (U1).
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The block it is an instance of.
        /// </summary>
        public string Block { get; set; } = "";

        /// <summary>
        /// Top left of the instance box in mm, or null to place it automatically. Drawing only.
        /// </summary>
        public Point? Position { get; set; }

        /// <summary>
        /// One per connected port: the signals on this level it joins, bit for bit.
        /// </summary>
        public List<PortConnection>? Connections { get; set; }
    }

    /// <summary>
    /// How one port of an instance is connected on the instance's level.
    /// </summary>
    public sealed class PortConnection
    {
        /// <summary>
        /// The port name.
        /// </summary>
        public string Port { get; set; } = "";

        /// <summary>
        /// The signals on this level, as many as the port is wide, in the port's bit order.
        /// </summary>
        public List<SignalRef> Nets { get; set; } = new();
    }

    /// <summary>
    /// The optional layout section (added within version 1, docs/FORMAT.md).
    /// </summary>
    public sealed class SheetLayoutSection
    {
        /// <summary>
        /// Wire paths fixed by hand, one entry per signal.
        /// </summary>
        public List<WirePath> Wires { get; set; } = new();

        /// <summary>
        /// Virtual buses: named thick lines that a group of signals join, or null for none.
        /// Drawing only - a bus never changes what is connected to what.
        /// </summary>
        public List<BusLine>? Buses { get; set; }
    }

    /// <summary>
    /// A virtual bus: a named thick line on the sheet. Each member signal leaves its pins with a
    /// short wire that joins the bus at a 45-degree entry, named with the signal's name and its
    /// 0/1 subscript. Drawing only: the signals stay exactly as they are in the nets list.
    /// </summary>
    public sealed class BusLine
    {
        /// <summary>
        /// The bus name, such as BD_0.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The bend points in whole mm from the top left, every piece level or upright. Empty
        /// when no path is set yet; the bus is then not drawn and its signals are drawn as usual.
        /// </summary>
        public List<Point> Path { get; set; } = new();

        /// <summary>
        /// The signals that join the bus.
        /// </summary>
        public List<BusMember> Members { get; set; } = new();
    }

    /// <summary>
    /// One signal on a bus: its name and, when two signals share the name, its level.
    /// </summary>
    public sealed class BusMember
    {
        /// <summary>
        /// The signal's name.
        /// </summary>
        public string Net { get; set; } = "";

        /// <summary>
        /// The signal's active level; null matches the first signal of that name.
        /// </summary>
        public ActiveLevel? Active { get; set; }
    }

    /// <summary>
    /// A signal's wire as fixed by hand: its branches, each a list of bend points. The first
    /// branch joins two pins; each later one runs from a pin to the wire before it. A program
    /// that finds the path crossing a chip (because a chip moved) routes the signal afresh.
    /// </summary>
    public sealed class WirePath
    {
        /// <summary>
        /// The signal's name.
        /// </summary>
        public string Net { get; set; } = "";

        /// <summary>
        /// The signal's active level, which tells apart two signals of the same name (BD15 with
        /// subscript 0 and with 1). Null matches the first signal of that name.
        /// </summary>
        public ActiveLevel? Active { get; set; }

        /// <summary>
        /// The branches; every point in whole mm from the top left, each piece level or upright.
        /// </summary>
        public List<List<Point>> Branches { get; set; } = new();
    }

    /// <summary>
    /// The drawing's title block and where the scan is.
    /// </summary>
    public sealed class Sheet
    {
        /// <summary>
        /// Sheet number, e.g. 2A50.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// Title as printed.
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// Machine, e.g. NORD-10.
        /// </summary>
        public string? Machine { get; set; }

        /// <summary>
        /// Company in the title block.
        /// </summary>
        public string? Company { get; set; }

        /// <summary>
        /// Date drawn.
        /// </summary>
        public string? Drawn { get; set; }

        /// <summary>
        /// Date of the last update.
        /// </summary>
        public string? Updated { get; set; }

        /// <summary>
        /// Where the scan is.
        /// </summary>
        public SourceReference Source { get; set; } = new();

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// A scanned document and a page in it.
    /// </summary>
    public sealed class SourceReference
    {
        /// <summary>
        /// Path relative to src/.
        /// </summary>
        public string Document { get; set; } = "";

        /// <summary>
        /// 1-based PDF page.
        /// </summary>
        public int? PdfPage { get; set; }

        /// <summary>
        /// Rendered page image, relative to src/.
        /// </summary>
        public string? Image { get; set; }
    }

    /// <summary>
    /// One chip.
    /// </summary>
    public sealed class Part
    {
        /// <summary>
        /// Board position as printed on the drawing (13D, 7-8C-D).
        /// </summary>
        public string Ref { get; set; } = "";

        /// <summary>
        /// Part type (7442, COM2601).
        /// </summary>
        public string Type { get; set; } = "";

        /// <summary>
        /// What it does on this card.
        /// </summary>
        public string? Function { get; set; }

        /// <summary>
        /// How sure.
        /// </summary>
        public Confidence Confidence { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Where the top left corner of the chip's frame is drawn (mm from top left), or null when
        /// not placed by hand. The editor writes it when a chip is placed or moved.
        /// </summary>
        public Point? Position { get; set; }
    }

    /// <summary>
    /// One gate inside a part.
    /// </summary>
    public sealed class Gate
    {
        /// <summary>
        /// Unique id, e.g. 11D/1.
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// The part ref it is in.
        /// </summary>
        public string Part { get; set; } = "";

        /// <summary>
        /// Logic function.
        /// </summary>
        public GateKind Kind { get; set; }

        /// <summary>
        /// Input pin numbers.
        /// </summary>
        public List<int> Inputs { get; set; } = new();

        /// <summary>
        /// Output pin number.
        /// </summary>
        public int Output { get; set; }

        /// <summary>
        /// Where it is drawn on the scan (mm from top left), or null when not measured.
        /// </summary>
        public Point? Position { get; set; }

        /// <summary>
        /// How sure.
        /// </summary>
        public Confidence Confidence { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// A position in millimetres.
    /// </summary>
    public sealed class Point
    {
        /// <summary>
        /// From the left.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// From the top.
        /// </summary>
        public double Y { get; set; }
    }

    /// <summary>
    /// One signal line.
    /// </summary>
    public sealed class Net
    {
        /// <summary>
        /// Name as on the drawing, without subscript.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The subscript.
        /// </summary>
        public ActiveLevel Active { get; set; }

        /// <summary>
        /// What drives the line, or null when not read yet.
        /// </summary>
        public LineDrive? Drive { get; set; }

        /// <summary>
        /// How sure <see cref="Drive"/> is.
        /// </summary>
        public Confidence? DriveConfidence { get; set; }

        /// <summary>
        /// For an open-collector line: the pull-up as written on the drawing, or "open".
        /// </summary>
        public string? PullUp { get; set; }

        /// <summary>
        /// The pins it joins.
        /// </summary>
        public List<PinReference> Pins { get; set; } = new();

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// One pin on a net.
    /// </summary>
    public sealed class PinReference
    {
        /// <summary>
        /// Where the pin is: the part reference, a dot and the pin number (13D.7); P. and a number for a
        /// backplane pin; F. and a number for a front-connector pin.
        /// </summary>
        public string At { get; set; } = "";

        /// <summary>
        /// How sure.
        /// </summary>
        public Confidence Confidence { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// The part ref: everything before the last dot. Worked out from <see cref="At"/>, so it
        /// is not written to the file (the format has only "at").
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string PartRef => At.Substring(0, At.LastIndexOf('.'));

        /// <summary>
        /// The pin number: everything after the last dot. Worked out from <see cref="At"/>, so it
        /// is not written to the file.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public int Pin => int.Parse(At.Substring(At.LastIndexOf('.') + 1), System.Globalization.CultureInfo.InvariantCulture);
    }
}
