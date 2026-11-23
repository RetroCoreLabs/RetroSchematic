//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The three ways to write a signal's active level (slash, tilde, ND subscript), that search
    /// finds a signal typed in any of them, and that the style never changes the sheet.
    /// </summary>
    [TestFixture]
    public class SignalNameTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [TestCase(SignalNameStyle.Slash, ActiveLevel.Low, "/RESET", "")]
        [TestCase(SignalNameStyle.Slash, ActiveLevel.High, "RESET", "")]
        [TestCase(SignalNameStyle.Slash, ActiveLevel.None, "RESET", "")]
        [TestCase(SignalNameStyle.Tilde, ActiveLevel.Low, "~RESET", "")]
        [TestCase(SignalNameStyle.Tilde, ActiveLevel.High, "RESET", "")]
        [TestCase(SignalNameStyle.Tilde, ActiveLevel.None, "RESET", "")]
        [TestCase(SignalNameStyle.Subscript, ActiveLevel.Low, "RESET", "0")]
        [TestCase(SignalNameStyle.Subscript, ActiveLevel.High, "RESET", "1")]
        [TestCase(SignalNameStyle.Subscript, ActiveLevel.None, "RESET", "")]
        public void EachStyle_WritesLowHighAndNone(SignalNameStyle style, ActiveLevel level, string text, string sub)
        {
            Assert.That(SignalNames.Format("RESET", level, style, out string s), Is.EqualTo(text));
            Assert.That(s, Is.EqualTo(sub));
        }

        [Test]
        public void TheDefault_IsSlash()
        {
            Assert.That(SignalNames.Default, Is.EqualTo(SignalNameStyle.Slash));
            Assert.That(new LayoutOptions().NameStyle, Is.EqualTo(SignalNameStyle.Slash));
            Assert.That(SignalNames.TryParseStyle("TILDE", out SignalNameStyle t), Is.True);
            Assert.That(t, Is.EqualTo(SignalNameStyle.Tilde));
            Assert.That(SignalNames.TryParseStyle("dots", out _), Is.False);
        }

        [TestCase("/BD3", "BD3_0")]
        [TestCase("~BD3", "BD3_0")]
        [TestCase("BD3_0", "BD3_0")]
        [TestCase("BD3_1", "BD3_1")]
        public void Search_FindsASignalTypedInAnyStyle(string typed, string key)
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, ChipCatalog.Find(Sheet2A50));
            List<string> found = s.Search(typed);
            Assert.That(found, Does.Contain("net:" + key));
            // the level asked for is respected: /BD3 is not BD3 active high
            string other = key.EndsWith("_0") ? "net:BD3_1" : "net:BD3_0";
            Assert.That(found, Does.Not.Contain(other));
            // plain text finds both levels
            Assert.That(s.Search("BD3"), Does.Contain("net:BD3_0").And.Contain("net:BD3_1"));
        }

        [Test]
        public void TheStyle_ChangesTheDrawingAndTexts_NeverTheSheet()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, ChipCatalog.Find(Sheet2A50));
            string saved = NetlistReader.Write(s.Netlist);
            Assert.That(Texts(s), Does.Contain("/BD3").And.Not.Contain("~BD3"));
            s.SetNameStyle(SignalNameStyle.Tilde);
            Assert.That(Texts(s), Does.Contain("~BD3").And.Not.Contain("/BD3"));
            s.SetNameStyle(SignalNameStyle.Subscript);
            Assert.That(Texts(s), Does.Not.Contain("~BD3").And.Not.Contain("/BD3"));
            s.Select("net:BD3_0");
            Assert.That(s.Describe(), Does.StartWith("Signal BD3_0"));
            s.SetNameStyle(SignalNameStyle.Slash);
            Assert.That(s.Describe(), Does.StartWith("Signal /BD3"));
            Assert.That(NetlistReader.Write(s.Netlist), Is.EqualTo(saved), "names in the file never change");
        }

        private static List<string> Texts(SheetSession s)
        {
            List<string> t = new();
            for (int i = 0; i < s.Drawing.Items.Count; i++)
            {
                if (s.Drawing.Items[i].Kind == DrawKind.Text)
                    t.Add(s.Drawing.Items[i].Text);
            }
            return t;
        }
    }
}
