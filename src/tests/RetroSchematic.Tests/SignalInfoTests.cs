//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// Signal facts: what follows from the chip catalogue (never stored), what the sheet says,
    /// and what the bus catalogue adds without ever overriding the sheet.
    /// </summary>
    [TestFixture]
    public class SignalInfoTests
    {
        private static readonly string Catalog = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog");
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [Test]
        public void TwoOpenCollectorOutputs_MakeAnOpenCollectorLine()
        {
            SheetEditor e = Sheet();
            SignalInfo s = SignalInfo.Of(e, e.FindNet("OC")!, SignalNameStyle.Slash);
            Assert.That(s.DerivedDrive, Is.EqualTo(LineDrive.OpenCollector));
            Assert.That(s.MixedDrive, Is.False);
            Assert.That(s.Drivers.Count, Is.EqualTo(2));
            Assert.That(s.Loads, Is.EqualTo(1));
            Assert.That(s.Pins[0].ToString(), Is.EqualTo("1A.3  7403 1Y  output (open-collector)"));
            Assert.That(s.Conflict, Is.Null);
        }

        [Test]
        public void AThreeStateOutput_MakesAThreeStateLine()
        {
            SheetEditor e = Sheet();
            SignalInfo s = SignalInfo.Of(e, e.FindNet("TS")!, SignalNameStyle.Slash);
            Assert.That(s.DerivedDrive, Is.EqualTo(LineDrive.ThreeState));
            Assert.That(s.DriveSummary(), Is.EqualTo("three-state"));
        }

        [Test]
        public void DifferentOutputTypes_OnOneLine_AreAWarning()
        {
            SheetEditor e = Sheet();
            SignalInfo s = SignalInfo.Of(e, e.FindNet("MIX")!, SignalNameStyle.Slash);
            Assert.That(s.MixedDrive, Is.True);
            Assert.That(s.DerivedDrive, Is.Null);
            Assert.That(s.DriveSummary(), Does.Contain("warning"));
        }

        [Test]
        public void AStoredDriveThatDisagrees_IsShownNextToTheCatalogue_AndNeverChanged()
        {
            SheetEditor e = Sheet();
            Net n = e.FindNet("OC")!;
            n.Drive = LineDrive.TotemPole;
            SignalInfo s = SignalInfo.Of(e, n, SignalNameStyle.Slash);
            Assert.That(s.Conflict, Is.EqualTo("The file says totem-pole, the chip catalogue says open collector."));
            Assert.That(s.Details(), Does.Contain("CONFLICT"));
            Assert.That(n.Drive, Is.EqualTo(LineDrive.TotemPole), "the stored value is kept");
        }

        [Test]
        public void TheTooltip_HasTheName_TheFirstDescriptionLine_TheDrive_AndTheDriver()
        {
            SheetEditor e = Sheet();
            e.ReadOnly = false;
            e.SetNetInfo("TS_0", "Data out.\nSecond line.", null, null);
            SignalInfo s = SignalInfo.Of(e, e.FindNet("TS")!, SignalNameStyle.Slash);
            Assert.That(s.Tooltip(), Is.EqualTo("/TS\nData out.\nthree-state, from 4A.3 (8093 Y1)"));
        }

        [Test]
        public void TheDescription_RoundTrips_AndUndoes_AndOnlyInEditMode()
        {
            SheetEditor e = Sheet();
            Assert.Throws<SheetEditException>(() => e.SetNetInfo("OC_0", "x", null, null));
            e.ReadOnly = false;
            DescriptionSource src = new() { Kind = SourceKind.Reading, Document = "docs/2A50-reading.md", Place = "section 3", Confidence = Confidence.Inferred };
            e.SetNetInfo("OC_0", "Shared request line.", src, "checked twice");
            Assert.That(e.Modified, Is.True);
            Netlist back = NetlistReader.Parse(NetlistReader.Write(e.Root));
            Net n = back.Nets[0];
            Assert.That(n.Description, Is.EqualTo("Shared request line."));
            Assert.That(n.DescriptionSource!.Kind, Is.EqualTo(SourceKind.Reading));
            Assert.That(n.DescriptionSource.Place, Is.EqualTo("section 3"));
            Assert.That(n.DescriptionSource.Confidence, Is.EqualTo(Confidence.Inferred));
            Assert.That(n.Note, Is.EqualTo("checked twice"));
            Assert.That(NetlistReader.Write(e.Root), Does.Not.Contain("\"derived"), "derived facts are never written");
            e.Undo();
            Assert.That(e.FindNet("OC")!.Description, Is.Null);
        }

        [Test]
        public void TheBusCatalogue_DescribesABackplaneSignal_ButNeverOverTheSheet()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Catalog);
            SignalInfo i = s.SignalInfoOf("IOXE_0")!;
            Assert.That(i.CatalogueSignal!.Name, Is.EqualTo("BIOXE"));
            Assert.That(i.DescriptionFromCatalogue, Is.True);
            Assert.That(i.Description, Does.StartWith("IOX enable"));
            Assert.That(i.DescriptionSourceText, Does.StartWith("bus catalogue, BIOXE: "));
            Assert.That(s.TooltipAt("pin:P.11"), Does.StartWith("/IOXE\nIOX enable"));
            s.Editor.ReadOnly = false;
            s.SetSignalInfo("IOXE_0", "Typed on the sheet.", new DescriptionSource { Kind = SourceKind.Manual, Confidence = Confidence.Read }, null);
            i = s.SignalInfoOf("IOXE_0")!;
            Assert.That(i.Description, Is.EqualTo("Typed on the sheet."));
            Assert.That(i.DescriptionFromCatalogue, Is.False);
        }

        [Test]
        public void Every2A50Description_SaysItComesFromTheReadingDocument()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            int described = 0;
            for (int i = 0; i < n.Nets.Count; i++)
            {
                Net net = n.Nets[i];
                if (net.Description == null)
                    continue;
                described++;
                Assert.That(net.DescriptionSource, Is.Not.Null, net.Name);
                Assert.That(net.DescriptionSource!.Kind, Is.EqualTo(SourceKind.Reading), net.Name);
                Assert.That(net.DescriptionSource.Document, Is.EqualTo("docs/2A50-reading.md"), net.Name);
                Assert.That(net.DescriptionSource.Place, Is.Not.Empty, net.Name);
                Assert.That(net.DescriptionSource.Confidence, Is.Not.EqualTo(Confidence.Open), net.Name);
            }
            Assert.That(described, Is.EqualTo(60));
        }

        private static SheetEditor Sheet()
        {
            Netlist n = SheetEditor.NewSheet("T", "t");
            n.Parts.Add(new Part { Ref = "1A", Type = "7403" });
            n.Parts.Add(new Part { Ref = "2A", Type = "7403" });
            n.Parts.Add(new Part { Ref = "3A", Type = "7400" });
            n.Parts.Add(new Part { Ref = "4A", Type = "8093" });
            n.Nets.Add(Net("OC", "1A.3", "2A.3", "3A.1"));
            n.Nets.Add(Net("TS", "4A.3", "3A.2"));
            n.Nets.Add(Net("MIX", "1A.6", "3A.3"));
            return new SheetEditor(n, new ChipCatalog(Catalog));
        }

        private static Net Net(string name, params string[] pins)
        {
            Net n = new() { Name = name, Active = ActiveLevel.Low };
            for (int i = 0; i < pins.Length; i++)
                n.Pins.Add(new PinReference { At = pins[i] });
            return n;
        }

        [Test]
        public void ThePanelFacts_OfASignal_AreStructured()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Catalog);
            SignalInfo info = s.SignalInfoOf("LEV12_0")!;
            Assert.That(info.ActiveText, Is.EqualTo("active low"));
            Assert.That(info.DriveBadge, Is.EqualTo("OPEN-COLLECTOR"));
            Assert.That(info.DescriptionConfidence, Is.EqualTo(Confidence.Read));
            Assert.That(info.DescriptionSourcePlace, Does.Not.EndWith("(read)"));
            Assert.That(info.Places, Does.Contain("backplane ?.35 BINT12"));
            SignalPin conn = info.Pins.Find(p => p.At == "P.35")!;
            Assert.That(conn.Direction, Is.EqualTo("connector"));
            Assert.That(conn.ChipType, Is.EqualTo("backplane"));
            Assert.That(SignalPin.DirectionOf("output"), Is.EqualTo("out"));
            Assert.That(SignalPin.DirectionOf("bidirectional"), Is.EqualTo("in/out"));
        }

        [Test]
        public void APart_GivesEveryChipPin_WithItsSignal()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Catalog);
            PartInfo p = s.PartInfoOf("13D")!;
            Assert.That(p.Type, Is.EqualTo("7442"));
            Assert.That(p.Pins.Count, Is.EqualTo(16));
            Assert.That(p.Pins[0].At, Is.EqualTo("13D.1"));
            Assert.That(p.Pins[15].Direction, Is.EqualTo("power"));
            Assert.That(p.Source, Does.Contain("page 64"));
            Assert.That(s.PartInfoOf("no-such"), Is.Null);
        }

        [Test]
        public void TheBackplaneConnector_ListsItsSignals_InPinOrder()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Catalog);
            SignalList l = SignalList.Of(s, "conn:P")!;
            Assert.That(l.Kind, Is.EqualTo("Connector"));
            SignalLink lev12 = l.Rows.Find(r => r.NetKey == "LEV12_0")!;
            Assert.That(lev12.Label, Is.EqualTo("35"));
            Assert.That(lev12.Extra, Is.EqualTo("BINT12"));
            for (int i = 1; i < l.Rows.Count; i++)
                Assert.That(int.Parse(l.Rows[i].Label) >= int.Parse(l.Rows[i - 1].Label), Is.True);
            Assert.That(SignalList.Of(s, "part:13D"), Is.Null);
        }
    }
}
