//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Text;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// One pin of a signal, with what the chip catalogue says about it.
    /// </summary>
    public sealed class SignalPin
    {
        /// <summary>
        /// Where the pin is, such as 13C.7.
        /// </summary>
        public string At { get; init; } = "";

        /// <summary>
        /// The chip type, such as 74153, or empty for a backplane or front pin or an unknown chip.
        /// </summary>
        public string ChipType { get; init; } = "";

        /// <summary>
        /// The pin's name in the catalogue, such as 1Y, or empty.
        /// </summary>
        public string PinName { get; init; } = "";

        /// <summary>
        /// The pin's role in the catalogue (input, output, clock, enable, ...), or empty.
        /// </summary>
        public string Role { get; init; } = "";

        /// <summary>
        /// For an output: its output type in the catalogue, or empty.
        /// </summary>
        public string OutputType { get; init; } = "";

        /// <summary>
        /// How sure the connection is.
        /// </summary>
        public Confidence Confidence { get; init; }

        /// <summary>
        /// True for an output pin: one that drives the signal.
        /// </summary>
        public bool Drives => Role == "output";

        /// <summary>
        /// True for a pin that only reads the signal (input, clock, enable).
        /// </summary>
        public bool Loads => Role == "input" || Role == "clock" || Role == "enable";

        /// <summary>
        /// The pin's direction in short words for the pin table: in, out, in/out, power, ground,
        /// connector, or empty when not known.
        /// </summary>
        public string Direction => IsConnector ? "connector" : DirectionOf(Role);

        /// <summary>
        /// True for a pin on one of the card's connectors.
        /// </summary>
        public bool IsConnector { get; init; }

        /// <summary>
        /// A chip file's pin role in short words: input, clock and enable are "in", output is
        /// "out", bidirectional is "in/out"; anything else is given as it is.
        /// </summary>
        /// <param name="role">
        /// The role from the chip file.
        /// </param>
        /// <returns>
        /// The words.
        /// </returns>
        public static string DirectionOf(string role) => role switch
        {
            "input" or "clock" or "enable" => "in",
            "output" => "out",
            "bidirectional" or "inout" => "in/out",
            _ => role,
        };

        /// <summary>
        /// The pin on one line: "13C.7  74153 1Y  output".
        /// </summary>
        /// <returns>
        /// The text.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append(At);
            if (ChipType.Length > 0)
                sb.Append("  ").Append(ChipType);
            if (PinName.Length > 0)
                sb.Append(' ').Append(PinName);
            if (Role.Length > 0)
                sb.Append("  ").Append(Role);
            if (OutputType.Length > 0 && OutputType != "totem-pole")
                sb.Append(" (").Append(OutputType).Append(')');
            return sb.ToString();
        }
    }

    /// <summary>
    /// One input of the gate that drives a signal, so a backwards trace can step through the
    /// gate body: the input pin and the signal on it.
    /// </summary>
    public sealed class DriverInput
    {
        /// <summary>
        /// The gate (11D/1).
        /// </summary>
        public string GateId { get; init; } = "";

        /// <summary>
        /// The input pin (3B.9).
        /// </summary>
        public string At { get; init; } = "";

        /// <summary>
        /// The signal's key (IRFTIE_1), or empty when nothing is wired to the pin.
        /// </summary>
        public string NetKey { get; init; } = "";

        /// <summary>
        /// The signal's name in the chosen name style, or empty when nothing is wired to the pin.
        /// </summary>
        public string Name { get; init; } = "";
    }

    /// <summary>
    /// Everything known about one signal: what the file says (its description and where that
    /// came from, its notes) and what follows from the chip catalogue, worked out here and never
    /// stored (the drive type, the driving pins, the loads, the bus, the levels it reaches). When
    /// the file's stored drive disagrees with the catalogue, both are kept and
    /// <see cref="Conflict"/> says so.
    /// </summary>
    public sealed class SignalInfo
    {
        private SignalInfo(Net net)
        {
            Net = net;
        }

        /// <summary>
        /// The signal.
        /// </summary>
        public Net Net { get; }

        /// <summary>
        /// Its name in the chosen name style.
        /// </summary>
        public string Name { get; private set; } = "";

        /// <summary>
        /// Every pin, with what the catalogue says about it.
        /// </summary>
        public List<SignalPin> Pins { get; } = new();

        /// <summary>
        /// The pins that drive the signal (outputs).
        /// </summary>
        public List<SignalPin> Drivers { get; } = new();

        /// <summary>
        /// The inputs of the gates that drive this signal, in the gates' input order, so a reader
        /// can follow the signal backwards through a gate. Empty when no gate drives it.
        /// </summary>
        public List<DriverInput> DriverInputs { get; } = new();

        /// <summary>
        /// How many pins only read it (inputs, clocks, enables).
        /// </summary>
        public int Loads { get; private set; }

        /// <summary>
        /// The drive type the catalogue gives: the output type shared by every driver. Null
        /// when there is no driver, a driver's type is not in the catalogue, or the drivers
        /// differ (then <see cref="MixedDrive"/> is true).
        /// </summary>
        public LineDrive? DerivedDrive { get; private set; }

        /// <summary>
        /// True when the drivers have different output types: a warning, two kinds of output
        /// fighting over one line.
        /// </summary>
        public bool MixedDrive { get; private set; }

        /// <summary>
        /// The bus the signal is drawn on, or null.
        /// </summary>
        public string? Bus { get; private set; }

        /// <summary>
        /// The other block levels the signal reaches through ports, as "U1: NAME".
        /// </summary>
        public List<string> Levels { get; } = new();

        /// <summary>
        /// The difference between the stored drive and the catalogue's, or null when they agree
        /// or either is missing.
        /// </summary>
        public string? Conflict { get; private set; }

        /// <summary>
        /// The bus-signal catalogue's entry for this signal (by its name, a card name such as
        /// IOXE, or one of its backplane pins), or null.
        /// </summary>
        public BusSignal? CatalogueSignal { get; private set; }

        /// <summary>
        /// The description to show: the sheet's own when it has one (it always wins), else the
        /// bus-signal catalogue's, else null.
        /// </summary>
        public string? Description => !string.IsNullOrWhiteSpace(Net.Description) ? Net.Description!.Trim() : CatalogueSignal?.Description;

        /// <summary>
        /// Where <see cref="Description"/> comes from without the confidence word (for a panel
        /// that shows the confidence as a badge of its own), or null.
        /// </summary>
        public string? DescriptionSourcePlace
        {
            get
            {
                string? s = DescriptionSourceText;
                if (s == null)
                    return null;
                int paren = s.LastIndexOf(" (", System.StringComparison.Ordinal);
                return paren < 0 ? s : s.Substring(0, paren);
            }
        }

        /// <summary>
        /// How sure <see cref="Description"/> is, or null when there is none.
        /// </summary>
        public Confidence? DescriptionConfidence => DescriptionFromCatalogue
            ? CatalogueSignal!.DescriptionConfidence
            : Description == null ? null : Net.DescriptionSource?.Confidence;

        /// <summary>
        /// The active level for a badge: "active low", "active high", or null.
        /// </summary>
        public string? ActiveText => Net.Active == ActiveLevel.Low ? "active low" : Net.Active == ActiveLevel.High ? "active high" : null;

        /// <summary>
        /// The drive type for a badge, in capitals: OPEN-COLLECTOR, THREE-STATE, TOTEM-POLE, MIXED,
        /// or null when nothing is known.
        /// </summary>
        public string? DriveBadge
        {
            get
            {
                if (MixedDrive)
                    return "MIXED";
                LineDrive? d = DerivedDrive ?? Net.Drive;
                return d switch
                {
                    LineDrive.TotemPole => "TOTEM-POLE",
                    LineDrive.OpenCollector => "OPEN-COLLECTOR",
                    LineDrive.OpenCollectorHighVoltage => "OPEN-COLLECTOR HV",
                    LineDrive.ThreeState => "THREE-STATE",
                    _ => null,
                };
            }
        }

        /// <summary>
        /// Where else the signal goes, as short tags: the bus it is drawn on, each connector pin,
        /// and each other block level.
        /// </summary>
        public List<string> Places
        {
            get
            {
                List<string> list = new();
                if (Bus != null)
                    list.Add("bus " + Bus);
                for (int i = 0; i < Net.Pins.Count; i++)
                {
                    string part = Net.Pins[i].PartRef;
                    if (ConnectorPins.IsConnector(part))
                        list.Add((ConnectorPins.Kind(part) == 'P' ? "backplane " : "front ") + ConnectorPins.Show(part, Net.Pins[i].Pin)
                            + (CatalogueSignal != null && ConnectorPins.Kind(part) == 'P' ? " " + CatalogueSignal.Name : ""));
                }
                for (int i = 0; i < Levels.Count; i++)
                    list.Add(Levels[i]);
                return list;
            }
        }

        /// <summary>
        /// True when <see cref="Description"/> comes from the bus-signal catalogue.
        /// </summary>
        public bool DescriptionFromCatalogue => string.IsNullOrWhiteSpace(Net.Description) && CatalogueSignal?.Description != null;

        /// <summary>
        /// Where <see cref="Description"/> comes from, in words, or null when nothing says.
        /// </summary>
        public string? DescriptionSourceText
        {
            get
            {
                if (DescriptionFromCatalogue)
                {
                    BusSignal c = CatalogueSignal!;
                    string place = string.IsNullOrEmpty(c.DescriptionPlace) ? "" : ", " + c.DescriptionPlace;
                    return "bus catalogue, " + c.Name + ": " + c.DescriptionDocument + place + " (" + ConfidenceWord(c.DescriptionConfidence) + ")";
                }
                return Net.DescriptionSource == null || string.IsNullOrWhiteSpace(Net.Description) ? null : SourceText(Net.DescriptionSource);
            }
        }

        /// <summary>
        /// Gathers what is known about a signal.
        /// </summary>
        /// <param name="editor">
        /// The open sheet (its catalogue, buses and levels).
        /// </param>
        /// <param name="net">
        /// The signal, on the level shown.
        /// </param>
        /// <param name="style">
        /// How names are written.
        /// </param>
        /// <param name="busSignals">
        /// The bus-signal catalogue for the sheet's bus, or null.
        /// </param>
        /// <returns>
        /// The facts.
        /// </returns>
        public static SignalInfo Of(SheetEditor editor, Net net, SignalNameStyle style, BusCatalog? busSignals = null)
        {
            SignalInfo s = new(net) { Name = SignalNames.Line(net, style) };
            string? firstType = null;
            for (int i = 0; i < net.Pins.Count; i++)
            {
                SignalPin p = PinOf(editor, net.Pins[i], style);
                s.Pins.Add(p);
                if (p.Drives)
                {
                    s.Drivers.Add(p);
                    if (firstType == null)
                        firstType = p.OutputType;
                    else if (firstType != p.OutputType)
                        s.MixedDrive = true;
                }
                else if (p.Loads)
                {
                    s.Loads++;
                }
            }
            if (s.Drivers.Count > 0 && !s.MixedDrive)
                s.DerivedDrive = DriveOf(firstType!);
            AddDriverInputs(editor.Netlist, net, style, s.DriverInputs);
            if (net.Drive.HasValue && s.DerivedDrive.HasValue && net.Drive.Value != s.DerivedDrive.Value)
                s.Conflict = "The file says " + DriveText(net.Drive.Value) + ", the chip catalogue says " + DriveText(s.DerivedDrive.Value) + ".";
            BusLine? bus = editor.BusOf(net);
            s.Bus = bus?.Name;
            List<string> levels = Hierarchy.Reach(editor.Root, editor.LevelPath, net);
            for (int i = 1; i < levels.Count; i++)
                s.Levels.Add(levels[i]);
            // the bus catalogue: by name first (BA3, or the card name IOXE), else by a backplane pin
            if (busSignals != null)
            {
                s.CatalogueSignal = busSignals.ByName(net.Name);
                for (int i = 0; s.CatalogueSignal == null && i < net.Pins.Count; i++)
                {
                    string part = net.Pins[i].PartRef;
                    if (ConnectorPins.IsConnector(part) && ConnectorPins.Kind(part) == 'P')
                        s.CatalogueSignal = busSignals.ByPin(ConnectorPins.Letter(part), net.Pins[i].Pin);
                }
            }
            return s;
        }

        // every gate whose output is on this signal: its inputs and the signals on them
        private static void AddDriverInputs(Netlist n, Net net, SignalNameStyle style, List<DriverInput> into)
        {
            for (int i = 0; i < net.Pins.Count; i++)
            {
                string part = net.Pins[i].PartRef;
                int pin = net.Pins[i].Pin;
                for (int g = 0; g < n.Gates.Count; g++)
                {
                    Gate gate = n.Gates[g];
                    if (gate.Part != part || gate.Output != pin)
                        continue;
                    for (int k = 0; k < gate.Inputs.Count; k++)
                    {
                        string at = part + "." + gate.Inputs[k].ToString(System.Globalization.CultureInfo.InvariantCulture);
                        Net? on = NetOnPin(n, at);
                        into.Add(new DriverInput
                        {
                            GateId = gate.Id,
                            At = at,
                            NetKey = on == null ? "" : SheetLayout.NetKey(on),
                            Name = on == null ? "" : SignalNames.Line(on, style),
                        });
                    }
                }
            }
        }

        private static Net? NetOnPin(Netlist n, string at)
        {
            for (int i = 0; i < n.Nets.Count; i++)
            {
                List<PinReference> pins = n.Nets[i].Pins;
                for (int j = 0; j < pins.Count; j++)
                {
                    if (pins[j].At == at)
                        return n.Nets[i];
                }
            }
            return null;
        }

        /// <summary>
        /// The drive type an output type names, or null when it names none.
        /// </summary>
        /// <param name="outputType">
        /// The catalogue's output type.
        /// </param>
        /// <returns>
        /// The drive, or null.
        /// </returns>
        public static LineDrive? DriveOf(string outputType) => outputType switch
        {
            "totem-pole" => LineDrive.TotemPole,
            "open-collector" => LineDrive.OpenCollector,
            "open-collector-high-voltage" => LineDrive.OpenCollectorHighVoltage,
            "three-state" => LineDrive.ThreeState,
            _ => null,
        };

        /// <summary>
        /// A drive type in plain words.
        /// </summary>
        /// <param name="d">
        /// The drive.
        /// </param>
        /// <returns>
        /// The words.
        /// </returns>
        public static string DriveText(LineDrive d) => d switch
        {
            LineDrive.TotemPole => "totem-pole",
            LineDrive.OpenCollector => "open collector",
            LineDrive.OpenCollectorHighVoltage => "open collector, high voltage",
            LineDrive.ThreeState => "three-state",
            _ => "not driven",
        };

        /// <summary>
        /// The drive in words, from the catalogue first, else what the file says, with a warning
        /// for mixed drivers.
        /// </summary>
        /// <returns>
        /// The text, or empty when nothing is known.
        /// </returns>
        public string DriveSummary()
        {
            if (MixedDrive)
                return "mixed drivers (warning)";
            if (DerivedDrive.HasValue)
                return DriveText(DerivedDrive.Value);
            if (Net.Drive.HasValue)
                return DriveText(Net.Drive.Value) + " (from the file)";
            return Drivers.Count == 0 ? "no driver on this sheet" : "";
        }

        /// <summary>
        /// The hover text: the name, the first line of the description, the drive and the
        /// driving pin.
        /// </summary>
        /// <returns>
        /// Two or three short lines.
        /// </returns>
        public string Tooltip()
        {
            StringBuilder sb = new();
            sb.Append(Name);
            if (Description != null)
            {
                string d = Description;
                int nl = d.IndexOf('\n');
                sb.Append('\n').Append(nl < 0 ? d : d.Substring(0, nl));
            }
            string drive = DriveSummary();
            if (drive.Length > 0 || Drivers.Count > 0)
            {
                sb.Append('\n').Append(drive.Length > 0 ? drive : "driven");
                if (Drivers.Count > 0)
                    sb.Append(", from ").Append(Drivers[0].At).Append(Drivers[0].ChipType.Length > 0 ? " (" + Drivers[0].ChipType + " " + Drivers[0].PinName + ")" : "");
                if (Drivers.Count > 1)
                    sb.Append(" and ").Append(Drivers.Count - 1).Append(" more");
            }
            return sb.ToString();
        }

        /// <summary>
        /// The full text for the side panel's Signal section.
        /// </summary>
        /// <returns>
        /// One fact per line.
        /// </returns>
        public string Details()
        {
            StringBuilder sb = new();
            sb.Append("Signal ").Append(Name);
            if (Net.Active != ActiveLevel.None)
                sb.Append(Net.Active == ActiveLevel.High ? " (active high)" : " (active low)");
            sb.Append('\n');
            if (Description != null)
            {
                sb.Append("What it does: ").Append(Description).Append('\n');
                string? source = DescriptionSourceText;
                if (source != null)
                    sb.Append("Source: ").Append(source).Append('\n');
            }
            else
            {
                sb.Append("What it does: not described yet\n");
            }
            if (Net.Function != null && Net.Function.Count > 0)
                sb.Append("Function: ").Append(string.Join(", ", Net.Function)).Append('\n');
            string drive = DriveSummary();
            if (drive.Length > 0)
                sb.Append("Drive: ").Append(drive).Append('\n');
            if (Conflict != null)
                sb.Append("CONFLICT: ").Append(Conflict).Append('\n');
            if (Drivers.Count > 0)
            {
                sb.Append("Driven by: ");
                for (int i = 0; i < Drivers.Count; i++)
                    sb.Append(i == 0 ? "" : ", ").Append(Drivers[i].At);
                sb.Append('\n');
            }
            sb.Append("Loads: ").Append(Loads).Append('\n');
            if (Net.PullUp != null)
                sb.Append("Pull-up: ").Append(Net.PullUp).Append('\n');
            if (Bus != null)
                sb.Append("On bus: ").Append(Bus).Append('\n');
            if (Levels.Count > 0)
            {
                sb.Append("Through block ports, the same signal is:\n");
                for (int i = 0; i < Levels.Count; i++)
                    sb.Append("  ").Append(Levels[i]).Append('\n');
            }
            sb.Append("Pins:\n");
            for (int i = 0; i < Pins.Count; i++)
            {
                sb.Append("  ").Append(Pins[i].ToString());
                if (Pins[i].Confidence != Confidence.Read)
                    sb.Append("  (").Append(Pins[i].Confidence == Confidence.Inferred ? "inferred" : "OPEN").Append(')');
                sb.Append('\n');
            }
            if (!string.IsNullOrEmpty(Net.Note))
                sb.Append("Notes: ").Append(Net.Note).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// A description source in words: "reading docs/2A50-reading.md, ICW..., read".
        /// </summary>
        /// <param name="s">
        /// The source.
        /// </param>
        /// <returns>
        /// The words.
        /// </returns>
        public static string SourceText(DescriptionSource s)
        {
            StringBuilder sb = new();
            sb.Append(s.Kind switch
            {
                SourceKind.Drawing => "the drawing",
                SourceKind.Manual => "manual",
                SourceKind.Reading => "reading document",
                // the document name says which catalogue
                SourceKind.Catalogue => "catalogue:",
                _ => "inferred",
            });
            if (!string.IsNullOrEmpty(s.Document))
                sb.Append(' ').Append(s.Document);
            if (!string.IsNullOrEmpty(s.Place))
                sb.Append(", ").Append(s.Place);
            sb.Append(" (").Append(ConfidenceWord(s.Confidence)).Append(')');
            return sb.ToString();
        }

        private static string ConfidenceWord(Confidence c) => c == Confidence.Read ? "read" : c == Confidence.Inferred ? "inferred" : "open";

        private static SignalPin PinOf(SheetEditor editor, PinReference r, SignalNameStyle style)
        {
            string at = r.At;
            int dot = at.LastIndexOf('.');
            if (dot > 0 && ConnectorPins.IsConnector(at.Substring(0, dot)))
            {
                string conn = at.Substring(0, dot);
                return new SignalPin
                {
                    At = at,
                    ChipType = ConnectorPins.Kind(conn) == 'P' ? "backplane" : "front",
                    PinName = ConnectorPins.Show(conn, r.Pin),
                    Confidence = r.Confidence,
                    IsConnector = true,
                };
            }
            Part? part = dot > 0 ? editor.FindPart(at.Substring(0, dot)) : null;
            ChipInfo? chip = part == null ? null : editor.Catalog?.Get(part.Type);
            if (part == null || chip == null || !int.TryParse(at.AsSpan(dot + 1), out int number))
                return new SignalPin { At = at, ChipType = part?.Type ?? "", Confidence = r.Confidence };
            for (int i = 0; i < chip.Pins.Count; i++)
            {
                ChipPin cp = chip.Pins[i];
                if (cp.Number == number)
                    return new SignalPin { At = at, ChipType = part.Type, PinName = SignalNames.PinLine(cp, style), Role = cp.Role, OutputType = cp.OutputType, Confidence = r.Confidence };
            }
            return new SignalPin { At = at, ChipType = part.Type, Confidence = r.Confidence };
        }
    }
}
