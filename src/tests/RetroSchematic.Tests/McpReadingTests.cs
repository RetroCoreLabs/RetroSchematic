//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Mcp;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The MCP tools for assisted reading: analyse_page, accept_find, set_symbol_box and
    /// connect_pin_on_page on sheet 2A50 and its source page, as an agent would drive them.
    /// </summary>
    [TestFixture]
    public class McpReadingTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        // the find number on the report line of the chip read as code
        private static int FindNumber(string report, string kind, string code)
        {
            string[] lines = report.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Contains(": " + kind + " ", StringComparison.Ordinal) && lines[i].Contains(" code " + code + " ", StringComparison.Ordinal))
                    return int.Parse(lines[i].Substring(0, lines[i].IndexOf(':')), System.Globalization.CultureInfo.InvariantCulture);
            }
            return -1;
        }

        [Test]
        public void AnAgentFindsAcceptsAndConnects()
        {
            SheetTools t = new(TestContext.CurrentContext.TestDirectory);
            t.OpenSheet(Sheet2A50, true);
            string report = t.AnalysePage(1);
            Assert.That(report, Does.StartWith("Source page 1: "));
            Assert.That(report, Does.Contain("(19 chips, 74 gates, 81 with a code), style hand (det-L3)"));
            int id = FindNumber(report, "chip", "15B");
            Assert.That(id, Is.GreaterThan(0), report);

            Assert.That(t.AcceptFind(id), Does.Contain("is now the box of 15B (74157)"));
            Assert.That(FindNumber(t.AnalysePage(1), "chip", "15B"), Is.EqualTo(id));
            Assert.That(t.AnalysePage(1), Does.Contain("code 15B type 74157 (read \"157\") sure - boxed as 15B"));

            // a wire end just above the 74157 box (2219,1081 92x418): pin 1 onto signal of 13D.1's net, or a new one
            string r = t.ConnectPinOnPage(1, 2260, 1070, 16, "13D.16");
            Assert.That(r, Does.StartWith("Pin 15B.16 is on signal "));
            Assert.That(r, Does.Contain("Pinout of 15B: 1="));

            t.Undo();
            t.Undo();
            Assert.That(t.AnalysePage(1), Does.Not.Contain("boxed as"));

            Assert.That(t.SetSymbolBox(1, 10, 10, 30, 30, "circle", text: "P.81"), Is.EqualTo("Symbol box 1 set."));
            Assert.Throws<ModelContextProtocol.McpException>(() => t.AcceptFind(9999));
            Assert.Throws<ModelContextProtocol.McpException>(() => t.ConnectPinOnPage(1, 5, 3000, 1, "13D.1"));
        }

        [Test]
        public void ReadOnlyRefusesAcceptingButAllowsAnalysing()
        {
            SheetTools t = new(TestContext.CurrentContext.TestDirectory);
            t.OpenSheet(Sheet2A50);
            Assert.That(t.AnalysePage(1), Does.StartWith("Source page 1"));
            Assert.That(Assert.Throws<ModelContextProtocol.McpException>(() => t.AcceptFind(1))!.Message, Does.Contain("read-only"));
        }
    }
}
