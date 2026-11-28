//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    [TestFixture]
    public class NetlistTests
    {
        private static readonly string SyncModemSheet =
            Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private const string Minimal =
            "{\"format\":\"nd-netlist\",\"formatVersion\":1,\"sheet\":{\"id\":\"X\",\"title\":\"T\",\"source\":{\"document\":\"d.pdf\"}}," +
            "\"parts\":[{\"ref\":\"1A\",\"type\":\"7402\",\"confidence\":\"read\"}]," +
            "\"gates\":[{\"id\":\"1A/1\",\"part\":\"1A\",\"kind\":\"nor\",\"inputs\":[2,3],\"output\":1,\"confidence\":\"read\"}]," +
            "\"nets\":[{\"name\":\"S\",\"active\":\"low\",\"pins\":[{\"at\":\"1A.1\",\"confidence\":\"read\"},{\"at\":\"P.34\",\"confidence\":\"inferred\"}]}]}";

        [Test]
        public void SyncModemSheet_IsValid()
        {
            Netlist netlist = NetlistReader.ReadFile(SyncModemSheet);
            Assert.That(netlist.Sheet.Id, Is.EqualTo("2A50"));
            Assert.That(netlist.Parts.Count, Is.GreaterThan(20));
        }

        [Test]
        public void SyncModemSheet_RoundTrips()
        {
            Netlist first = NetlistReader.ReadFile(SyncModemSheet);
            Netlist second = NetlistReader.Parse(NetlistReader.Write(first));
            Assert.That(NetlistReader.Write(second), Is.EqualTo(NetlistReader.Write(first)));
        }

        [Test]
        public void Minimal_Parses()
        {
            Netlist netlist = NetlistReader.Parse(Minimal);
            Assert.That(netlist.Gates[0].Kind, Is.EqualTo(GateKind.Nor));
            Assert.That(netlist.Nets[0].Pins[1].PartRef, Is.EqualTo("P"));
            Assert.That(netlist.Nets[0].Pins[1].Pin, Is.EqualTo(34));
        }

        [Test]
        public void UnknownVersion_IsRefused()
        {
            // version 2 (blocks) is known since the blocks work; 3 is the first unknown one
            NetlistException e = Assert.Throws<NetlistException>(() => NetlistReader.Parse(Minimal.Replace("\"formatVersion\":1", "\"formatVersion\":3", StringComparison.Ordinal)))!;
            Assert.That(e.Message, Does.Contain("version 3"));
        }

        [Test]
        public void UnknownField_IsRefused()
        {
            Assert.Throws<NetlistException>(() => NetlistReader.Parse(Minimal.Replace("\"title\":\"T\"", "\"title\":\"T\",\"colour\":\"red\"", StringComparison.Ordinal)));
        }

        [Test]
        public void PinInTwoNets_IsRefused()
        {
            string twice = Minimal.Replace("]}]}", "]},{\"name\":\"T\",\"active\":\"none\",\"pins\":[{\"at\":\"1A.1\",\"confidence\":\"read\"}]}]}", StringComparison.Ordinal);
            Assert.Throws<NetlistException>(() => NetlistReader.Parse(twice));
        }

        [Test]
        public void GateInUnknownPart_IsRefused()
        {
            Assert.Throws<NetlistException>(() => NetlistReader.Parse(Minimal.Replace("\"part\":\"1A\"", "\"part\":\"9Z\"", StringComparison.Ordinal)));
        }

        [Test]
        public void Drive_OpenCollectorWithPullUp_Parses()
        {
            string json = Minimal.Replace("\"active\":\"low\",", "\"active\":\"low\",\"drive\":\"open-collector\",\"driveConfidence\":\"read\",\"pullUp\":\"1k to +5V\",", StringComparison.Ordinal);
            Netlist netlist = NetlistReader.Parse(json);
            Assert.That(netlist.Nets[0].Drive, Is.EqualTo(LineDrive.OpenCollector));
            Assert.That(netlist.Nets[0].PullUp, Is.EqualTo("1k to +5V"));
            Assert.That(NetlistReader.Write(netlist), Does.Contain("\"open-collector\""));
        }

        [TestCase("totem-pole", LineDrive.TotemPole)]
        [TestCase("three-state", LineDrive.ThreeState)]
        [TestCase("open-collector-high-voltage", LineDrive.OpenCollectorHighVoltage)]
        [TestCase("none", LineDrive.None)]
        public void Drive_EachValue_Parses(string text, LineDrive expected)
        {
            string json = Minimal.Replace("\"active\":\"low\",", "\"active\":\"low\",\"drive\":\"" + text + "\",", StringComparison.Ordinal);
            Assert.That(NetlistReader.Parse(json).Nets[0].Drive, Is.EqualTo(expected));
        }

        [Test]
        public void Drive_Absent_IsNotReadYet()
        {
            Assert.That(NetlistReader.Parse(Minimal).Nets[0].Drive, Is.Null);
        }

        [Test]
        public void PullUp_OnTotemPoleLine_IsRefused()
        {
            string json = Minimal.Replace("\"active\":\"low\",", "\"active\":\"low\",\"drive\":\"totem-pole\",\"pullUp\":\"1k\",", StringComparison.Ordinal);
            Assert.Throws<NetlistException>(() => NetlistReader.Parse(json));
        }

        [TestCase("13D", 13, 13, 3, 3)]
        [TestCase("7-8C-D", 7, 8, 2, 3)]
        [TestCase("1A", 1, 1, 0, 0)]
        public void BoardPosition_Parses(string text, int c1, int c2, int r1, int r2)
        {
            BoardPosition p = BoardPosition.Parse(text);
            Assert.That(new[] { p.FirstColumn, p.LastColumn, p.FirstRow, p.LastRow }, Is.EqualTo(new[] { c1, c2, r1, r2 }));
        }

        [TestCase("D13")]
        [TestCase("13")]
        [TestCase("13d")]
        public void BoardPosition_RejectsOtherText(string text)
        {
            Assert.Throws<FormatException>(() => BoardPosition.Parse(text));
            Assert.That(BoardPosition.TryParse(text, out _), Is.False);
        }

        // card 3207 names some parts with no grid position in the name: they are not positions
        [TestCase("LD3", false)]
        [TestCase("OSC1", false)]
        [TestCase("16G2", false)]
        [TestCase("13A", true)]
        [TestCase("7-8C-D", true)]
        public void BoardPosition_TryParse_TellsANameFromAPosition(string text, bool position)
        {
            Assert.That(BoardPosition.TryParse(text, out _), Is.EqualTo(position));
        }

        [Test]
        public void Render_NorskDataNor_IsBoxWithX()
        {
            string svg = SvgRenderer.Render(NetlistReader.Parse(Minimal), SymbolStyle.NorskData);
            // ND-13.003.01: the 7402 NOR is a box with both diagonals.
            // only the gate's own group: the net-label diamonds elsewhere are paths
            int start = svg.IndexOf("<g id=\"gate-", System.StringComparison.Ordinal);
            string gate = svg.Substring(start, svg.IndexOf("</g>", start, System.StringComparison.Ordinal) - start);
            Assert.That(gate, Does.Contain("<rect x=\"").And.Contain("<line x1=\"").And.Not.Contain("<path"));
            // active low in the default name style: /S
            Assert.That(svg, Does.Contain(">/S</text>"));
        }

        [Test]
        public void Render_ModernNor_IsCurvedWithBubble()
        {
            string svg = SvgRenderer.Render(NetlistReader.Parse(Minimal), SymbolStyle.Modern);
            Assert.That(svg, Does.Contain("<path d=\"").And.Contain("<circle"));
        }

        [Test]
        public void Render_SyncModemSheet_HasEveryPart()
        {
            Netlist netlist = NetlistReader.ReadFile(SyncModemSheet);
            string svg = SvgRenderer.Render(netlist, SymbolStyle.NorskData);
            for (int i = 0; i < netlist.Parts.Count; i++)
                Assert.That(svg, Does.Contain("id=\"part-" + netlist.Parts[i].Ref + "\""));
        }
    }
}
