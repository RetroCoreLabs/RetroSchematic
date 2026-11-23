//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.Globalization;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// One pin of a part, for the side panel's pin table: its number, name, direction and the
    /// signal on it.
    /// </summary>
    public sealed class PartPinRow
    {
        /// <summary>
        /// The pin reference, such as 13C.7.
        /// </summary>
        public string At { get; init; } = "";

        /// <summary>
        /// The pin number.
        /// </summary>
        public int Number { get; init; }

        /// <summary>
        /// The pin's name from the chip file, or empty.
        /// </summary>
        public string Name { get; init; } = "";

        /// <summary>
        /// in, out, in/out, power, ground ... or empty when the chip file is missing.
        /// </summary>
        public string Direction { get; init; } = "";

        /// <summary>
        /// The key of the signal on the pin (for tracing it), or empty.
        /// </summary>
        public string NetKey { get; init; } = "";

        /// <summary>
        /// The signal's name in the chosen style, or empty when the pin is free.
        /// </summary>
        public string NetName { get; init; } = "";
    }

    /// <summary>
    /// What the side panel shows for a part, as structured items: header facts, where it was
    /// read, and every pin with its signal.
    /// </summary>
    public sealed class PartInfo
    {
        /// <summary>
        /// The board position, such as 13C.
        /// </summary>
        public string Ref { get; init; } = "";

        /// <summary>
        /// The chip type, such as 74153.
        /// </summary>
        public string Type { get; init; } = "";

        /// <summary>
        /// What the part does on this sheet, or null.
        /// </summary>
        public string? Function { get; init; }

        /// <summary>
        /// The catalogue's title for the chip, or null when it has no file.
        /// </summary>
        public string? CatalogueTitle { get; init; }

        /// <summary>
        /// How sure the reading of the part is.
        /// </summary>
        public Confidence Confidence { get; init; }

        /// <summary>
        /// Notes, or null.
        /// </summary>
        public string? Note { get; init; }

        /// <summary>
        /// Where the sheet was read: "2A50, NORD-10-drawings.pdf page 64", or null.
        /// </summary>
        public string? Source { get; init; }

        /// <summary>
        /// Every pin: every pin of the chip file when there is one, else the pins on a signal.
        /// </summary>
        public List<PartPinRow> Pins { get; } = new();

        /// <summary>
        /// For a passive part: its kind in words ("resistor", "switch block"); null for a chip.
        /// </summary>
        public string? Kind { get; init; }

        /// <summary>
        /// For a passive part: the value as written on the drawing, or null.
        /// </summary>
        public string? Value { get; init; }

        /// <summary>
        /// For a passive part: the tolerance as written on the drawing, or null.
        /// </summary>
        public string? Tolerance { get; init; }

        /// <summary>
        /// How sure the value is, when the file says so apart from the part.
        /// </summary>
        public Confidence? ValueConfidence { get; init; }

        /// <summary>
        /// For a passive part with no name on the drawing: the chip it serves, or null.
        /// </summary>
        public string? Serves { get; init; }

        /// <summary>
        /// Gathers the facts of a part on the level shown.
        /// </summary>
        /// <param name="editor">
        /// The open sheet.
        /// </param>
        /// <param name="reference">
        /// The part's board position.
        /// </param>
        /// <param name="style">
        /// How signal names are written.
        /// </param>
        /// <returns>
        /// The facts, or null when there is no such part.
        /// </returns>
        public static PartInfo? Of(SheetEditor editor, string reference, SignalNameStyle style)
        {
            Part? p = editor.FindPart(reference);
            if (p == null)
                return null;
            ChipInfo? chip = editor.Catalog?.Get(p.Type);
            SourceReference? src = editor.Root.Sheet.Source;
            string? source = null;
            if (src != null && src.Document.Length > 0)
            {
                source = editor.Root.Sheet.Id + ", " + src.Document;
                if (src.PdfPage != null)
                    source += " page " + src.PdfPage.Value.ToString(CultureInfo.InvariantCulture);
            }
            PartInfo info = new()
            {
                Ref = p.Ref,
                Type = p.Type,
                Function = string.IsNullOrEmpty(p.Function) ? null : p.Function,
                CatalogueTitle = chip?.Title,
                Confidence = p.Confidence,
                Note = string.IsNullOrEmpty(p.Note) ? null : p.Note,
                Source = source,
                Kind = PassiveParts.IsPassive(p.Type) ? PassiveParts.KindText(PassiveParts.KindOf(p.Type)) : null,
                Value = p.Value,
                Tolerance = p.Tolerance,
                ValueConfidence = p.ValueConfidence,
                Serves = p.Serves,
            };
            if (chip != null)
            {
                List<ChipPin> pins = new(chip.Pins);
                pins.Sort((a, b) => a.Number.CompareTo(b.Number));
                for (int i = 0; i < pins.Count; i++)
                    info.Pins.Add(Row(editor, p.Ref, pins[i].Number, SignalNames.PinLine(pins[i], style), SignalPin.DirectionOf(pins[i].Role), style));
                return info;
            }
            // no chip file: the pins that are on a signal, in number order
            List<int> numbers = new();
            for (int i = 0; i < editor.Netlist.Nets.Count; i++)
            {
                Net n = editor.Netlist.Nets[i];
                for (int j = 0; j < n.Pins.Count; j++)
                {
                    if (n.Pins[j].PartRef == p.Ref)
                        numbers.Add(n.Pins[j].Pin);
                }
            }
            numbers.Sort();
            for (int i = 0; i < numbers.Count; i++)
                info.Pins.Add(Row(editor, p.Ref, numbers[i], "", "", style));
            return info;
        }

        private static PartPinRow Row(SheetEditor editor, string reference, int number, string name, string direction, SignalNameStyle style)
        {
            string at = reference + "." + number.ToString(CultureInfo.InvariantCulture);
            Net? n = editor.NetOf(at);
            return new PartPinRow
            {
                At = at,
                Number = number,
                Name = name,
                Direction = direction,
                NetKey = n == null ? "" : SheetLayout.NetKey(n),
                NetName = n == null ? "" : SignalNames.Line(n, style),
            };
        }
    }
}
