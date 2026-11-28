//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

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

        // a printed value used as a part type in a reading: 100PF, 4.7KOHM, .2KOHM (a leading dot
        // is how ND writes 0.2 kohm)
        private static readonly Regex s_readingValue = new(@"^\d*\.?\d+(PF|NF|UF|KOHM|OHM|MOHM)$", RegexOptions.CultureInvariant);

        // a Bourns 4600X conformal SIP network: 46, the pin count, X, the circuit (101 bussed,
        // 102 isolated, 104 dual terminator), then the value code; the dash may be read as _ or
        // left out (4608X104_221/331)
        private static readonly Regex s_bourns4600 = new(@"^46(\d\d)X_?(101|102|104)_", RegexOptions.CultureInvariant);

        // a Bourns 4100R molded DIP network: 41, the pin count, R, the circuit (written 001 on the
        // ND drawing for circuit 1, isolated), then the value code
        private static readonly Regex s_bourns4100 = new(@"^41(\d\d)R_?0*(1)_", RegexOptions.CultureInvariant);

        // a clock oscillator printed by its frequency: 10MHZ, 32MHZ
        private static readonly Regex s_oscillator = new(@"^\d+(\.\d+)?MHZ$", RegexOptions.CultureInvariant);

        // a zener diode printed by its voltage: 5.6V_ZENER
        private static readonly Regex s_zener = new(@"^\d+(\.\d+)?V_ZENER$", RegexOptions.CultureInvariant);

        /// <summary>
        /// The passive catalogue file that describes a part type as a card reading writes it
        /// (<c>docs/&lt;card&gt;-readings/</c>, the type printed on the drawing), and how many pins it
        /// has. This is the ONE rule in C# for reading types; <c>tools/card-readings.py</c>
        /// (<c>passive_part</c>) has the same rule and the two must be changed together.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>A printed resistor or capacitor value (<c>4.7KOHM</c>, <c>.2KOHM</c>,
        /// <c>100PF</c>): <c>resistor</c> or <c>capacitor</c>, 2 pins.</description></item>
        /// <item><description>A Bourns 4600X SIP (<c>4606X_101_151</c>): <c>bourns-4600x-101</c>,
        /// <c>-102</c> or <c>-104</c> by circuit, the pin count from the name.</description></item>
        /// <item><description>A Bourns 4100R DIP of circuit 1 (<c>4116R_001_102</c>):
        /// <c>bourns-4100r-1</c>, the pin count from the name.</description></item>
        /// <item><description><c>1N4148</c> and <c>2N3906</c>: their own files, 2 and 3 pins.</description></item>
        /// <item><description><c>STRAP</c> or <c>STR2</c>: <c>strap</c>, 2 pins.</description></item>
        /// <item><description><c>TESTPOINT</c>: <c>test-point</c>, 1 pin.</description></item>
        /// <item><description>An LED printed by its colour (<c>YELLOW</c>, <c>RED</c>,
        /// <c>GREEN</c>): <c>led</c>, 2 pins.</description></item>
        /// <item><description>A zener printed by its voltage (<c>5.6V_ZENER</c>): <c>zener</c>, 2 pins.</description></item>
        /// <item><description>An oscillator printed by its frequency (<c>32MHZ</c>):
        /// <c>oscillator</c>, pin count 0 because the module's pin numbering is not known (the
        /// file says so), so its pin numbers are not checked.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="type">
        /// The part type as the reading writes it.
        /// </param>
        /// <param name="catalogueId">
        /// The file name in <c>catalog/passives/</c> without <c>.json</c>; empty when the type is
        /// not a passive part.
        /// </param>
        /// <param name="pinCount">
        /// The number of pins (numbered 1 to this); 0 when not known.
        /// </param>
        /// <returns>
        /// True when the type is a passive part.
        /// </returns>
        public static bool ReadingPart(string type, out string catalogueId, out int pinCount)
            => ReadingPartCore(type, out catalogueId, out pinCount);

        /// <summary>
        /// Whether a reading's part type is a passive part whose file exists in the catalogue's
        /// <c>passives</c> folder. The reading check and the sheet check both ask this, so a
        /// type counts as catalogued in the same way everywhere.
        /// </summary>
        /// <param name="type">
        /// The part type as the reading or the sheet writes it.
        /// </param>
        /// <param name="catalogFolder">
        /// The catalogue folder (the one holding <c>chips</c> and <c>passives</c>).
        /// </param>
        /// <returns>
        /// True when the type is a passive part and its file is there.
        /// </returns>
        public static bool HasPassiveFile(string type, string catalogFolder)
            => ReadingPart(type, out string id, out _) && System.IO.File.Exists(System.IO.Path.Combine(catalogFolder, "passives", id + ".json"));

        private static bool ReadingPartCore(string type, out string catalogueId, out int pinCount)
        {
            catalogueId = "";
            pinCount = 0;
            if (s_readingValue.IsMatch(type))
            {
                catalogueId = type.EndsWith("OHM", StringComparison.Ordinal) ? "resistor" : "capacitor";
                pinCount = 2;
                return true;
            }
            Match m = s_bourns4600.Match(type);
            if (m.Success)
            {
                catalogueId = "bourns-4600x-" + m.Groups[2].Value;
                pinCount = int.Parse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture);
                return true;
            }
            m = s_bourns4100.Match(type);
            if (m.Success)
            {
                catalogueId = "bourns-4100r-" + m.Groups[2].Value;
                pinCount = int.Parse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture);
                return true;
            }
            switch (type)
            {
                case "1N4148":
                    catalogueId = "diode-1n4148";
                    pinCount = 2;
                    return true;
                case "2N3906":
                    catalogueId = "transistor-2n3906";
                    pinCount = 3;
                    return true;
                case "STRAP":
                case "STR2":
                    catalogueId = "strap";
                    pinCount = 2;
                    return true;
                case "TESTPOINT":
                    catalogueId = "test-point";
                    pinCount = 1;
                    return true;
                case "YELLOW":
                case "RED":
                case "GREEN":
                    catalogueId = "led";
                    pinCount = 2;
                    return true;
            }
            if (s_zener.IsMatch(type))
            {
                catalogueId = "zener";
                pinCount = 2;
                return true;
            }
            if (s_oscillator.IsMatch(type))
            {
                catalogueId = "oscillator";
                return true;
            }
            return false;
        }

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
