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
    /// Pins on the card's connectors. A pin reference names the connector and the pin:
    /// <list type="bullet">
    ///   <item><description><c>P.32</c> - backplane pin 32, which connector it is on is not known yet;</description></item>
    ///   <item><description><c>P.A.32</c> - backplane connector A, pin 32;</description></item>
    ///   <item><description><c>F.81</c> and <c>F.A.81</c> - the same for a front connector.</description></item>
    /// </list>
    /// The part of the reference before the last dot (<c>P</c>, <c>P.A</c>) is the connector, so
    /// every place that splits a pin at its last dot keeps working; old files with only
    /// <c>P.n</c> and <c>F.n</c> read unchanged and mean "connector not known yet".
    /// </summary>
    public static class ConnectorPins
    {
        /// <summary>
        /// True when the part of a pin reference before its last dot is a connector: P, F, or
        /// P.x / F.x with a connector letter.
        /// </summary>
        /// <param name="partRef">
        /// The part reference, such as P, P.A or 13C.
        /// </param>
        /// <returns>
        /// True for a connector.
        /// </returns>
        public static bool IsConnector(string partRef)
        {
            if (partRef.Length == 0 || (partRef[0] != 'P' && partRef[0] != 'F'))
                return false;
            if (partRef.Length == 1)
                return true;
            return partRef[1] == '.' && partRef.Length > 2 && partRef.IndexOf('.', 2) < 0;
        }

        /// <summary>
        /// The kind of a connector: 'P' for the backplane, 'F' for a front connector.
        /// </summary>
        /// <param name="partRef">
        /// The connector reference.
        /// </param>
        /// <returns>
        /// P or F.
        /// </returns>
        public static char Kind(string partRef) => partRef[0];

        /// <summary>
        /// The connector letter (A, B, C ...), or null when the reference does not say (P, F).
        /// </summary>
        /// <param name="partRef">
        /// The connector reference.
        /// </param>
        /// <returns>
        /// The letter, or null.
        /// </returns>
        public static string? Letter(string partRef) => partRef.Length > 2 ? partRef.Substring(2) : null;

        /// <summary>
        /// How a connector pin is shown on the drawing: "A.32", or "?.32" when the connector is
        /// not known yet.
        /// </summary>
        /// <param name="partRef">
        /// The connector reference.
        /// </param>
        /// <param name="pin">
        /// The pin number.
        /// </param>
        /// <returns>
        /// The text.
        /// </returns>
        public static string Show(string partRef, int pin)
            => (Letter(partRef) ?? "?") + "." + pin.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// The connector's name in plain words: "Backplane A", "Backplane (connector not known)",
        /// "Front connector A", "Front connector (not known)".
        /// </summary>
        /// <param name="partRef">
        /// The connector reference.
        /// </param>
        /// <returns>
        /// The name.
        /// </returns>
        public static string Name(string partRef)
        {
            string? letter = Letter(partRef);
            string kind = Kind(partRef) == 'P' ? "Backplane" : "Front connector";
            return letter == null ? kind + " (connector not known)" : kind + " " + letter;
        }

        /// <summary>
        /// Compares connector references so P comes before P.A, P.B, then F, F.A.
        /// </summary>
        /// <param name="a">
        /// One reference.
        /// </param>
        /// <param name="b">
        /// The other.
        /// </param>
        /// <returns>
        /// The order.
        /// </returns>
        public static int Compare(string a, string b)
        {
            if (Kind(a) != Kind(b))
                return Kind(a) == 'P' ? -1 : 1;
            return string.CompareOrdinal(a, b);
        }
    }

    /// <summary>
    /// Which catalogue entry describes one of the card's connectors (the sheet's
    /// <c>connectors</c> section). The catalogue must be for the sheet's bus.
    /// </summary>
    public sealed class ConnectorUse
    {
        /// <summary>
        /// The connector as pin references name it: P, F, P.A ...
        /// </summary>
        public string Ref { get; set; } = "";

        /// <summary>
        /// The id of a file in catalog/connectors, such as nord10-io-card-backplane.
        /// </summary>
        public string? Catalog { get; set; }

        /// <summary>
        /// Free text.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Where the connector's box was put by hand: its top-left corner in millimetres, like a
        /// chip's position. Null puts the box where its wires are shortest (left or right of the
        /// chips, level with the chips its signals go to).
        /// </summary>
        public Point? Position { get; set; }

        /// <summary>
        /// Which side of the box the pins are on: "left" or "right". Null picks the side that
        /// faces the chips (pins on the right for a box left of the chips).
        /// </summary>
        public string? PinSide { get; set; }

        /// <summary>
        /// What this connector carries on this card, in words: "ND-100 bus", "modem (V.24)
        /// lines", "not used".
        /// </summary>
        public string? Use { get; set; }

        /// <summary>
        /// False when the card has the connector position but uses no pin of it. Null or true:
        /// used.
        /// </summary>
        public bool? Used { get; set; }

        /// <summary>
        /// The connector standard, such as "DIN 41612".
        /// </summary>
        public string? Standard { get; set; }

        /// <summary>
        /// The pin numbers in words, such as "0-99" for a NORD-10 card edge.
        /// </summary>
        public string? Pins { get; set; }

        /// <summary>
        /// The row letters of a plug with rows, such as a, b, c on a DIN 41612 plug. Null for a
        /// connector with one row of numbered pins.
        /// </summary>
        public List<string>? Rows { get; set; }

        /// <summary>
        /// Pins in each row of a plug with rows (32 on a DIN 41612 plug).
        /// </summary>
        public int? PinsPerRow { get; set; }

        /// <summary>
        /// The card page the connector's box is drawn on (format version 3), or null for the
        /// first page. A signal from another page reaches the box through an off-page marker.
        /// </summary>
        public string? Page { get; set; }
    }
}
