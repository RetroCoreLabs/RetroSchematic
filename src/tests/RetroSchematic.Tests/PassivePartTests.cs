//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// Passive parts (docs/FORMAT.md, "Passive parts"): the types and their pins, the catalogue
    /// files, the reader's checks, the switches section tied to its switch block part, the pull-up
    /// facts of a signal, and how they are drawn.
    /// </summary>
    [TestFixture]
    public class PassivePartTests
    {
        private static readonly string Catalog = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog");

        [Test]
        public void TheTypes_SayTheirKind_Count_AndPins()
        {
            Assert.That(PassiveParts.KindOf("R"), Is.EqualTo(PassiveKind.Resistor));
            Assert.That(PassiveParts.KindOf("C-POL"), Is.EqualTo(PassiveKind.CapacitorPolarised));
            Assert.That(PassiveParts.KindOf("7400"), Is.EqualTo(PassiveKind.None));
            Assert.That(PassiveParts.KindOf("SW-DIP-0"), Is.EqualTo(PassiveKind.None), "no levers is not a switch block");
            Assert.That(PassiveParts.KindOf("SW-DIP-x"), Is.EqualTo(PassiveKind.None));
            Assert.That(PassiveParts.CountOf("RN-SIP-8"), Is.EqualTo(8));
            List<ChipPin> sip = PassiveParts.Pins("RN-SIP-8");
            Assert.That(sip.Count, Is.EqualTo(9));
            Assert.That(sip[0].Name, Is.EqualTo("COM"));
            Assert.That(sip[8].Name, Is.EqualTo("R8"));
            List<ChipPin> sw = PassiveParts.Pins("SW-DIP-4");
            Assert.That(sw.Count, Is.EqualTo(8));
            PassiveParts.LeverPins(4, 1, out int a, out int b);
            Assert.That((a, b), Is.EqualTo((1, 8)));
            Assert.That(sw[7].Name, Is.EqualTo("S1B"), "pin 8 is the far end of lever 1");
            Assert.That(PassiveParts.LeverOfPin(4, 5), Is.EqualTo(4));
            Assert.That(PassiveParts.Pins("D")[1].Name, Is.EqualTo("K"));
        }

        [Test]
        public void EveryKind_HasACatalogueFile_ThatAgreesWithThePins()
        {
            PassiveKind[] kinds =
            {
                PassiveKind.Resistor, PassiveKind.ResistorNetwork, PassiveKind.Capacitor, PassiveKind.CapacitorPolarised,
                PassiveKind.Diode, PassiveKind.Strap, PassiveKind.SwitchBlock,
            };
            for (int i = 0; i < kinds.Length; i++)
            {
                string path = Path.Combine(Catalog, "passives", PassiveParts.CatalogueId(kinds[i]) + ".json");
                Assert.That(File.Exists(path), Is.True, path);
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
                JsonElement root = doc.RootElement;
                Assert.That(root.GetProperty("catalogFormat").GetString(), Is.EqualTo("nd-passive"));
                string type = root.GetProperty("type").GetString()!;
                Assert.That(root.TryGetProperty("pinNumberingConfidence", out _), Is.True, path);
                if (!root.TryGetProperty("pins", out JsonElement pins))
                {
                    Assert.That(root.TryGetProperty("pinRule", out _), Is.True, "a counted kind states its pin rule: " + path);
                    continue;
                }
                List<ChipPin> code = PassiveParts.Pins(type);
                Assert.That(pins.GetArrayLength(), Is.EqualTo(code.Count), path);
                for (int k = 0; k < code.Count; k++)
                {
                    Assert.That(pins[k].GetProperty("pin").GetInt32(), Is.EqualTo(code[k].Number), path);
                    Assert.That(pins[k].GetProperty("name").GetString(), Is.EqualTo(code[k].Name), path);
                }
            }
            ChipCatalog cat = new(Catalog);
            Assert.That(cat.Get("R")!.Title, Is.EqualTo("Resistor"));
            Assert.That(cat.Get("SW-DIP-4")!.Pins.Count, Is.EqualTo(8));
        }

        [Test]
        public void AResistor_RoundTrips_WithItsValue()
        {
            Netlist n = PullUpSheet(true);
            Netlist back = NetlistReader.Parse(NetlistReader.Write(n));
            Part r = back.Parts.Find(p => p.Ref == "R1")!;
            Assert.That(r.Value, Is.EqualTo("1k"));
            Assert.That(r.Tolerance, Is.EqualTo("5%"));
        }

        [Test]
        public void TheReader_RefusesWrongPassiveFacts()
        {
            Netlist chipValue = PullUpSheet(true);
            chipValue.Parts[0].Value = "1k";
            Assert.Throws<NetlistException>(() => NetlistReader.Parse(NetlistReader.Write(chipValue)), "a chip has no value");

            Netlist badPin = PullUpSheet(true);
            badPin.Nets[0].Pins.Add(new PinReference { At = "R1.3" });
            Assert.Throws<NetlistException>(() => NetlistReader.Parse(NetlistReader.Write(badPin)), "a resistor has two pins");

            Netlist badServes = PullUpSheet(true);
            badServes.Parts.Find(p => p.Ref == "R1")!.Serves = "9Z";
            Assert.Throws<NetlistException>(() => NetlistReader.Parse(NetlistReader.Write(badServes)));
        }

        [Test]
        public void TheSwitchesSection_AndTheSwitchBlockPart_MustAgree()
        {
            Netlist ok = SwitchSheet(4, "SMN3");
            Assert.DoesNotThrow(() => NetlistReader.Parse(NetlistReader.Write(ok)));

            Netlist count = SwitchSheet(3, "SMN3");
            NetlistException e1 = Assert.Throws<NetlistException>(() => NetlistReader.Parse(NetlistReader.Write(count)))!;
            Assert.That(e1.Message, Does.Contain("3 levers"));

            Netlist signal = SwitchSheet(4, "OTHER");
            NetlistException e2 = Assert.Throws<NetlistException>(() => NetlistReader.Parse(NetlistReader.Write(signal)))!;
            Assert.That(e2.Message, Does.Contain("lever 1"));

            Netlist notSwitch = SwitchSheet(4, "SMN3");
            notSwitch.Parts.Find(p => p.Ref == "15E")!.Type = "R";
            notSwitch.Nets.Clear();
            Assert.Throws<NetlistException>(() => NetlistReader.Parse(NetlistReader.Write(notSwitch)));
        }

        [Test]
        public void APullUpResistor_IsShownOnTheSignal_AndAnOpenCollectorLineWithoutOne_IsAWarning()
        {
            SheetEditor with = new(PullUpSheet(true), new ChipCatalog(Catalog));
            SignalInfo s = SignalInfo.Of(with, with.FindNet("OC")!, SignalNameStyle.Slash);
            Assert.That(s.PullResistors, Is.EqualTo(new[] { "pulled up by R1 (1k) to +5V" }));
            Assert.That(s.PullUpWarning, Is.Null);
            Assert.That(s.Details(), Does.Contain("Resistor: pulled up by R1 (1k) to +5V"));

            SheetEditor without = new(PullUpSheet(false), new ChipCatalog(Catalog));
            SignalInfo w = SignalInfo.Of(without, without.FindNet("OC")!, SignalNameStyle.Slash);
            Assert.That(w.PullResistors, Is.Empty);
            Assert.That(w.PullUpWarning, Does.Contain("no pull-up resistor"));
        }

        [Test]
        public void APassivePart_ShowsItsKind_Value_AndBothSignals()
        {
            SheetEditor e = new(PullUpSheet(true), new ChipCatalog(Catalog));
            PartInfo p = PartInfo.Of(e, "R1", SignalNameStyle.Slash)!;
            Assert.That(p.Kind, Is.EqualTo("resistor"));
            Assert.That(p.Value, Is.EqualTo("1k"));
            Assert.That(p.Pins.Count, Is.EqualTo(2));
            Assert.That(p.Pins[0].NetName, Is.EqualTo("/OC"));
            Assert.That(p.Pins[1].NetName, Is.EqualTo("+5V"));
        }

        [Test]
        public void APartWithNoNameOnTheDrawing_GetsANameFromTheChipItServes()
        {
            SheetEditor e = new(PullUpSheet(false), new ChipCatalog(Catalog)) { ReadOnly = false };
            string name = e.AddPassive("R", null, "1A", null, null);
            Assert.That(name, Is.EqualTo("R-1A-1"));
            Part p = e.FindPart(name)!;
            Assert.That(p.Confidence, Is.EqualTo(Confidence.Inferred));
            Assert.That(p.Serves, Is.EqualTo("1A"));
            Assert.That(e.AddPassive("R", null, "1A", null, null), Is.EqualTo("R-1A-2"));
            Assert.That(e.AddPassive("C", "C3", null, "100n", null), Is.EqualTo("C3"));
            Assert.That(e.FindPart("C3")!.Confidence, Is.EqualTo(Confidence.Read));
            Assert.Throws<SheetEditException>(() => e.AddPassive("R", null, null, null, null), "an unnamed part needs the chip it serves");
            Assert.Throws<SheetEditException>(() => e.AddPassive("7400", "X1", null, null, null));
        }

        [Test]
        public void APullUp_IsDrawnAsASymbol_WithASupplySymbolAtItsFarEnd()
        {
            Netlist n = PullUpSheet(true);
            for (int style = 0; style < 2; style++)
            {
                SchematicDrawing d = SheetLayout.Build(n, style == 0 ? SymbolStyle.NorskData : SymbolStyle.Modern, new LayoutOptions { Catalog = new ChipCatalog(Catalog) });
                Assert.That(d.Pins.ContainsKey("R1.1"), Is.True);
                Assert.That(d.Pins.ContainsKey("R1.2"), Is.True);
                bool supplyName = false, supplyWire = false, chipFrame = false;
                for (int i = 0; i < d.Items.Count; i++)
                {
                    DrawItem it = d.Items[i];
                    if (it.Owner == "net:+5V" && it.Kind == DrawKind.Text && it.Text == "+5V")
                        supplyName = true;
                    if (it.Owner == "net:+5V" && it.Role == DrawRole.Wire)
                        supplyWire = true;
                    if (it.Owner == "part:R1" && it.Role == DrawRole.PartOutline)
                        chipFrame = true;
                }
                Assert.That(supplyName, Is.True, "the far end carries the +5V supply symbol");
                Assert.That(supplyWire, Is.False, "a supply line at a resistor gets no wire");
                Assert.That(chipFrame, Is.False, "a resistor is not drawn as a chip box");
                Assert.That(d.Frames["R1"].Y, Is.GreaterThan(d.Frames["1A"].Y), "it is drawn below the chip it serves");
            }
        }

        [Test]
        public void ASwitchBlock_IsDrawnWithOneLeverPerRow()
        {
            Netlist n = SwitchSheet(4, "SMN3");
            SchematicDrawing d = SheetLayout.Build(n, SymbolStyle.Modern, new LayoutOptions { Catalog = new ChipCatalog(Catalog) });
            for (int pin = 1; pin <= 8; pin++)
                Assert.That(d.Pins.ContainsKey("15E." + pin.ToString(System.Globalization.CultureInfo.InvariantCulture)), Is.True, "pin " + pin);
            Assert.That(d.Pins["15E.1"].Y, Is.EqualTo(d.Pins["15E.8"].Y), "lever 1 runs from pin 1 to pin 8");
            Assert.That(d.Pins["15E.1"].X, Is.LessThan(d.Pins["15E.8"].X));
        }

        // an open-collector line from two 7403 outputs into a 7400, with (or without) R1 from
        // the line to +5V
        private static Netlist PullUpSheet(bool withResistor)
        {
            Netlist n = SheetEditor.NewSheet("T", "t");
            n.Parts.Add(new Part { Ref = "1A", Type = "7403" });
            n.Parts.Add(new Part { Ref = "2A", Type = "7400" });
            Net oc = Net("OC", ActiveLevel.Low, "1A.3", "1A.6", "2A.1");
            n.Nets.Add(oc);
            if (withResistor)
            {
                n.Parts.Add(new Part { Ref = "R1", Type = "R", Value = "1k", Tolerance = "5%" });
                oc.Pins.Add(new PinReference { At = "R1.1" });
                n.Nets.Add(Net("+5V", ActiveLevel.None, "R1.2", "1A.14", "2A.14"));
            }
            return n;
        }

        private static Netlist SwitchSheet(int leverCount, string leverSignal)
        {
            Netlist n = SheetEditor.NewSheet("T", "t");
            n.Parts.Add(new Part { Ref = "1A", Type = "7400" });
            n.Parts.Add(new Part { Ref = "15E", Type = "SW-DIP-4" });
            n.Nets.Add(Net("SMN3", ActiveLevel.High, "15E.1", "1A.1"));
            n.Nets.Add(Net("GND", ActiveLevel.None, "15E.8", "1A.7"));
            n.Switches = new List<SwitchBlock>
            {
                new()
                {
                    Ref = "15E",
                    Function = "device address",
                    LeverCount = leverCount,
                    Levers = new List<SwitchLever> { new() { Number = 1, Signal = leverSignal, Confidence = Confidence.Read, Setting = "open" } },
                },
            };
            return n;
        }

        private static Net Net(string name, ActiveLevel active, params string[] pins)
        {
            Net n = new() { Name = name, Active = active };
            for (int i = 0; i < pins.Length; i++)
                n.Pins.Add(new PinReference { At = pins[i] });
            return n;
        }
        [Test]
        public void On2A50_TheOpenCollectorLinesStillWithoutAPullUp_AreTheListedReadingQuestions()
        {
            // the 2A50 pull-ups are parts now (docs/2A50-passives-and-pins.md); every open-collector
            // line still warned about is a reading question listed in that document, not a resistor
            // to invent. A line leaving this list must also leave the document.
            string sheet = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
            SheetEditor e = new(NetlistReader.ReadFile(sheet), new ChipCatalog(Catalog));
            List<string> warned = new();
            for (int i = 0; i < e.Netlist.Nets.Count; i++)
            {
                SignalInfo s = SignalInfo.Of(e, e.Netlist.Nets[i], SignalNameStyle.Slash);
                if (s.PullUpWarning != null)
                    warned.Add(e.Netlist.Nets[i].Name);
            }
            warned.Sort(System.StringComparer.Ordinal);
            TestContext.Out.WriteLine("still warned: " + string.Join(", ", warned));
            Assert.That(warned, Is.EqualTo(new[] { "CONNECT", "INPUT", "LEV10", "LEV12" }));
        }
    }
}
