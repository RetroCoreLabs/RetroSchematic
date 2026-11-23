//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;

namespace RetroSchematic.Model
{
    /// <summary>
    /// The kinds of passive part a sheet can hold (docs/FORMAT.md, "Passive parts").
    /// </summary>
    public enum PassiveKind
    {
        /// <summary>
        /// Not a passive part: a chip from the chip catalogue.
        /// </summary>
        None,

        /// <summary>
        /// A resistor, type <c>R</c>: pins 1 and 2.
        /// </summary>
        Resistor,

        /// <summary>
        /// A resistor network in one row of pins (SIP), type <c>RN-SIP-n</c>: n resistors from one
        /// common pin (pin 1) to pins 2 to n+1.
        /// </summary>
        ResistorNetwork,

        /// <summary>
        /// A capacitor, type <c>C</c>: pins 1 and 2.
        /// </summary>
        Capacitor,

        /// <summary>
        /// A polarised capacitor, type <c>C-POL</c>: pin 1 is plus, pin 2 minus.
        /// </summary>
        CapacitorPolarised,

        /// <summary>
        /// A diode, type <c>D</c>: pin 1 is the anode, pin 2 the cathode.
        /// </summary>
        Diode,

        /// <summary>
        /// A strap (wire link), type <c>STRAP</c>: pins 1 and 2, joined when it is fitted.
        /// </summary>
        Strap,

        /// <summary>
        /// A block of switch levers in a DIP case, type <c>SW-DIP-n</c>: lever k joins pins k and
        /// 2n+1-k.
        /// </summary>
        SwitchBlock,
    }

    /// <summary>
    /// What a passive part type means: its kind, how many resistors or levers it has, its pins.
    /// The type names (<c>R</c>, <c>RN-SIP-8</c>, <c>C</c>, <c>C-POL</c>, <c>D</c>,
    /// <c>STRAP</c>, <c>SW-DIP-4</c>) carry the count, so a reader can check a sheet without the
    /// catalogue. The catalogue's <c>catalog/passives/</c> files describe the same kinds in words.
    /// </summary>
    public static class PassiveParts
    {
        /// <summary>
        /// The most resistors or levers a counted type may have.
        /// </summary>
        public const int MaxCount = 32;

        /// <summary>
        /// The kind a part type names, or <see cref="PassiveKind.None"/> for a chip.
        /// </summary>
        /// <param name="type">
        /// The part type, such as <c>R</c> or <c>SW-DIP-4</c>.
        /// </param>
        /// <returns>
        /// The kind.
        /// </returns>
        public static PassiveKind KindOf(string type)
        {
            switch (type)
            {
                case "R":
                    return PassiveKind.Resistor;
                case "C":
                    return PassiveKind.Capacitor;
                case "C-POL":
                    return PassiveKind.CapacitorPolarised;
                case "D":
                    return PassiveKind.Diode;
                case "STRAP":
                    return PassiveKind.Strap;
            }
            if (Counted(type, "RN-SIP-") > 0)
                return PassiveKind.ResistorNetwork;
            if (Counted(type, "SW-DIP-") > 0)
                return PassiveKind.SwitchBlock;
            return PassiveKind.None;
        }

        /// <summary>
        /// True for a passive part type.
        /// </summary>
        /// <param name="type">
        /// The part type.
        /// </param>
        /// <returns>
        /// True when it is not a chip.
        /// </returns>
        public static bool IsPassive(string type) => KindOf(type) != PassiveKind.None;

        /// <summary>
        /// The number of resistors (<c>RN-SIP-n</c>) or levers (<c>SW-DIP-n</c>); 1 for every
        /// other passive type, 0 for a chip.
        /// </summary>
        /// <param name="type">
        /// The part type.
        /// </param>
        /// <returns>
        /// The count.
        /// </returns>
        public static int CountOf(string type)
        {
            switch (KindOf(type))
            {
                case PassiveKind.None:
                    return 0;
                case PassiveKind.ResistorNetwork:
                    return Counted(type, "RN-SIP-");
                case PassiveKind.SwitchBlock:
                    return Counted(type, "SW-DIP-");
                default:
                    return 1;
            }
        }

        /// <summary>
        /// The catalogue file name (in <c>catalog/passives/</c>) of a type's kind.
        /// </summary>
        /// <param name="kind">
        /// The kind.
        /// </param>
        /// <returns>
        /// The file name without <c>.json</c>, or empty for <see cref="PassiveKind.None"/>.
        /// </returns>
        public static string CatalogueId(PassiveKind kind) => kind switch
        {
            PassiveKind.Resistor => "resistor",
            PassiveKind.ResistorNetwork => "resistor-network-sip",
            PassiveKind.Capacitor => "capacitor",
            PassiveKind.CapacitorPolarised => "capacitor-polarised",
            PassiveKind.Diode => "diode",
            PassiveKind.Strap => "strap",
            PassiveKind.SwitchBlock => "switch-block-dip",
            _ => "",
        };

        /// <summary>
        /// The pins of a passive type, in number order, as chip pins with role <c>passive</c>:
        /// <c>1</c>/<c>2</c> for a resistor, capacitor or strap, <c>+</c>/<c>-</c> for a polarised
        /// capacitor, <c>A</c>/<c>K</c> (anode, cathode) for a diode, <c>COM</c> then <c>R1</c>..
        /// for a resistor network, <c>S1A</c>/<c>S1B</c>.. (the two ends of each lever) for a switch
        /// block.
        /// </summary>
        /// <param name="type">
        /// The part type.
        /// </param>
        /// <returns>
        /// The pins; empty for a chip.
        /// </returns>
        public static List<ChipPin> Pins(string type)
        {
            List<ChipPin> pins = new();
            PassiveKind kind = KindOf(type);
            int n = CountOf(type);
            switch (kind)
            {
                case PassiveKind.Resistor:
                case PassiveKind.Capacitor:
                case PassiveKind.Strap:
                    pins.Add(Pin(1, "1"));
                    pins.Add(Pin(2, "2"));
                    break;
                case PassiveKind.CapacitorPolarised:
                    pins.Add(Pin(1, "+"));
                    pins.Add(Pin(2, "-"));
                    break;
                case PassiveKind.Diode:
                    pins.Add(Pin(1, "A"));
                    pins.Add(Pin(2, "K"));
                    break;
                case PassiveKind.ResistorNetwork:
                    pins.Add(Pin(1, "COM"));
                    for (int i = 1; i <= n; i++)
                        pins.Add(Pin(i + 1, "R" + i.ToString(CultureInfo.InvariantCulture)));
                    break;
                case PassiveKind.SwitchBlock:
                    for (int pin = 1; pin <= 2 * n; pin++)
                    {
                        int lever = LeverOfPin(n, pin);
                        pins.Add(Pin(pin, "S" + lever.ToString(CultureInfo.InvariantCulture) + (pin <= n ? "A" : "B")));
                    }
                    break;
            }
            return pins;
        }

        /// <summary>
        /// The two pins of lever <paramref name="lever"/> (1 first) of an n-lever switch block:
        /// <c>lever</c> and <c>2n+1-lever</c>, the usual DIP layout (pins 1..n down one side, the
        /// rest back up the other). The real part's numbering is not checked by this rule; the
        /// catalogue file says so.
        /// </summary>
        /// <param name="levers">
        /// The number of levers.
        /// </param>
        /// <param name="lever">
        /// The lever, 1 first.
        /// </param>
        /// <param name="first">
        /// Its pin on the first side.
        /// </param>
        /// <param name="second">
        /// Its pin on the other side.
        /// </param>
        public static void LeverPins(int levers, int lever, out int first, out int second)
        {
            first = lever;
            second = (2 * levers) + 1 - lever;
        }

        /// <summary>
        /// The lever a switch block pin belongs to.
        /// </summary>
        /// <param name="levers">
        /// The number of levers.
        /// </param>
        /// <param name="pin">
        /// The pin, 1 to 2n.
        /// </param>
        /// <returns>
        /// The lever, 1 first.
        /// </returns>
        public static int LeverOfPin(int levers, int pin) => pin <= levers ? pin : (2 * levers) + 1 - pin;

        /// <summary>
        /// The automatic name of a passive part the drawing does not name: the letter of its kind,
        /// the chip it serves and a running number, such as <c>R-13D-1</c>. The name says it is not
        /// ND's: ND's own names (<c>R1</c>, <c>C3</c>) have no dash.
        /// </summary>
        /// <param name="kind">
        /// The kind.
        /// </param>
        /// <param name="serves">
        /// The board position of the chip it serves.
        /// </param>
        /// <param name="taken">
        /// The part references already on the sheet.
        /// </param>
        /// <returns>
        /// A free name.
        /// </returns>
        public static string AutoName(PassiveKind kind, string serves, ICollection<string> taken)
        {
            string letter = kind switch
            {
                PassiveKind.Resistor or PassiveKind.ResistorNetwork => "R",
                PassiveKind.Capacitor or PassiveKind.CapacitorPolarised => "C",
                PassiveKind.Diode => "D",
                PassiveKind.SwitchBlock => "S",
                PassiveKind.Strap => "W",
                _ => "X",
            };
            for (int i = 1; ; i++)
            {
                string name = letter + "-" + serves + "-" + i.ToString(CultureInfo.InvariantCulture);
                if (!taken.Contains(name))
                    return name;
            }
        }

        /// <summary>
        /// The kind in plain words.
        /// </summary>
        /// <param name="kind">
        /// The kind.
        /// </param>
        /// <returns>
        /// The words, such as "resistor".
        /// </returns>
        public static string KindText(PassiveKind kind) => kind switch
        {
            PassiveKind.Resistor => "resistor",
            PassiveKind.ResistorNetwork => "resistor network (one row of pins)",
            PassiveKind.Capacitor => "capacitor",
            PassiveKind.CapacitorPolarised => "capacitor, polarised",
            PassiveKind.Diode => "diode",
            PassiveKind.Strap => "strap (wire link)",
            PassiveKind.SwitchBlock => "switch block",
            _ => "chip",
        };

        private static ChipPin Pin(int number, string name) => new() { Number = number, Name = name, Role = "passive" };

        // "RN-SIP-8" with prefix "RN-SIP-" gives 8; anything else 0
        private static int Counted(string type, string prefix)
        {
            if (!type.StartsWith(prefix, StringComparison.Ordinal))
                return 0;
            if (!int.TryParse(type.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int n))
                return 0;
            return n >= 1 && n <= MaxCount ? n : 0;
        }
    }

    /// <summary>
    /// Supply lines told apart by their names: <c>+5V</c>, <c>VCC</c>, ND's <c>H</c> and
    /// <c>H1</c> (a pin tied high), other <c>+12V</c>/<c>-12V</c> style names, and ground
    /// (<c>GND</c>, <c>0V</c>).
    /// </summary>
    public static class SupplyNets
    {
        /// <summary>
        /// True for a supply that is high: <c>+5V</c>, <c>VCC</c>, <c>H</c>, <c>H1</c>, or any
        /// name of a plus sign and a digit (<c>+12V</c>).
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        /// <returns>
        /// True for a high supply.
        /// </returns>
        public static bool IsHigh(string name)
        {
            if (name == "VCC" || name == "H" || name == "H1" || name == "+5 V")
                return true;
            return name.Length >= 2 && name[0] == '+' && char.IsAsciiDigit(name[1]);
        }

        /// <summary>
        /// True for ground: <c>GND</c>, <c>0V</c>, <c>0 V</c>.
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        /// <returns>
        /// True for ground.
        /// </returns>
        public static bool IsGround(string name) => name == "GND" || name == "0V" || name == "0 V";

        /// <summary>
        /// True for any supply line: high, ground, or a minus voltage (<c>-12V</c>).
        /// </summary>
        /// <param name="name">
        /// The signal's name.
        /// </param>
        /// <returns>
        /// True for a supply.
        /// </returns>
        public static bool IsSupply(string name)
            => IsHigh(name) || IsGround(name) || (name.Length >= 2 && name[0] == '-' && char.IsAsciiDigit(name[1]));
    }
}
