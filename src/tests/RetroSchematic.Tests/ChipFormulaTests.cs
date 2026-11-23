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

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The chip formula reader: a ' is part of a pin name when the chip has that pin, and NOT
    /// otherwise; names side by side are ANDed; free text is refused with a reason.
    /// </summary>
    [TestFixture]
    public class ChipFormulaTests
    {
        private static readonly string Chips = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog", "chips");

        private static bool Eval(ChipFormula f, params (string Pin, bool High)[] levels)
        {
            Dictionary<string, bool> d = new();
            for (int i = 0; i < levels.Length; i++)
                d[levels[i].Pin] = levels[i].High;
            return f.Expression.Evaluate(d);
        }

        private static ChipFormula Read(string text, params string[] pins)
        {
            Assert.That(ChipFormula.TryParse(text, pins, out ChipFormula? f, out string? error), Is.True, error);
            return f!;
        }

        [Test]
        public void APrimeAfterANameThatIsNotAPin_IsNot_AndNamesSideBySide_AreAnded()
        {
            // 74155 (TI SN74155 function table): 2Y1 = NOT(2G' AND 2C' AND B'A); the chip has pins
            // 2G and 2C written without the prime in this test, so each ' is a NOT
            ChipFormula f = Read("2Y1 = NOT(2G' AND 2C' AND B'A)", "2Y1", "2G", "2C", "A", "B");
            Assert.That(f.ToString(), Is.EqualTo("2Y1 = NOT(NOT(2G) AND NOT(2C) AND NOT(B) AND A)"));
            Assert.That(Eval(f, ("2G", false), ("2C", false), ("B", false), ("A", true)), Is.False);
            Assert.That(Eval(f, ("2G", false), ("2C", false), ("B", true), ("A", true)), Is.True);
        }

        [Test]
        public void APrimeThatIsPartOfAPinName_IsThatPin()
        {
            // a chip with both 1Q and 1Q' pins: "1Q'" is the pin, never NOT 1Q
            ChipFormula f = Read("1Y = 1Q' AND A", "1Y", "1Q", "1Q'", "A");
            Assert.That(f.ToString(), Is.EqualTo("1Y = 1Q' AND A"));
            List<string> pins = new();
            f.Expression.CollectPins(pins);
            Assert.That(pins, Is.EqualTo(new[] { "1Q'", "A" }));
            // the same text on a chip that has only 1Q is NOT 1Q
            Assert.That(Read("1Y = 1Q' AND A", "1Y", "1Q", "A").ToString(), Is.EqualTo("1Y = NOT(1Q) AND A"));
            // and a pin name with a prime can be the output
            Assert.That(Read("1Q' = NOT A", "1Q'", "A").Target, Is.EqualTo("1Q'"));
        }

        [Test]
        public void RunTogetherSelectNames_SplitOnThePinList()
        {
            // 74170: RB'RA' is NOT RB AND NOT RA
            ChipFormula f = Read("Q1 = RB'RA' AND W0B1", "Q1", "RA", "RB", "W0B1");
            Assert.That(f.ToString(), Is.EqualTo("Q1 = NOT(RB) AND NOT(RA) AND W0B1"));
        }

        [Test]
        public void OrBindsLooserThanAnd_AndBracketsWork()
        {
            ChipFormula f = Read("Y = A AND B OR C", "Y", "A", "B", "C");
            Assert.That(f.ToString(), Is.EqualTo("Y = (A AND B) OR C"));
            Assert.That(Eval(f, ("A", false), ("B", true), ("C", true)), Is.True);
            ChipFormula g = Read("Y = A AND (B OR C)", "Y", "A", "B", "C");
            Assert.That(Eval(g, ("A", false), ("B", true), ("C", true)), Is.False);
            ChipFormula x = Read("Y = A XOR B", "Y", "A", "B");
            Assert.That(Eval(x, ("A", true), ("B", true)), Is.False);
            Assert.That(Eval(x, ("A", true), ("B", false)), Is.True);
        }

        [Test]
        public void KeywordsAreWholeWords_SoPinsStartingWithOrAreNames()
        {
            ChipFormula f = Read("Y = ORFT OR ANDY", "Y", "ORFT", "ANDY");
            Assert.That(f.ToString(), Is.EqualTo("Y = ORFT OR ANDY"));
        }

        [TestCase("1Q = FF1", "not a pin name")]
        [TestCase("Y = A when G' = L", "not a pin name")]
        [TestCase("Y = NOT(A", "not closed")]
        [TestCase("Z = A", "not a pin name of the chip")]
        [TestCase("Y equals A", "no \" = \"")]
        public void FreeText_IsRefused_WithAReason(string text, string why)
        {
            Assert.That(ChipFormula.TryParse(text, new[] { "Y", "A", "G", "1Q" }, out ChipFormula? f, out string? error), Is.False);
            Assert.That(f, Is.Null);
            Assert.That(error, Does.Contain(why));
        }

        [Test]
        public void AMissingPinLevel_IsAnError_NotAQuietZero()
        {
            ChipFormula f = Read("Y = A AND B", "Y", "A", "B");
            Assert.Throws<KeyNotFoundException>(() => Eval(f, ("A", true)));
        }

        /// <summary>
        /// Every formula in the catalogue is either read in full or refused; the plain gates
        /// (7400, 7402, 7404, 7410, 7420, 7430, 7486) are all read. The refused ones are the
        /// free-text formulas that item 6 of the plan turns into structured behaviour.
        /// </summary>
        [Test]
        public void TheCatalogue_PlainGatesAreAllRead_AndTheRestIsReadOrRefused()
        {
            int read = 0, refused = 0;
            string[] files = Directory.GetFiles(Chips, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(files[i]));
                JsonElement root = doc.RootElement;
                List<string> pins = new();
                foreach (JsonElement p in root.GetProperty("pins").EnumerateArray())
                    pins.Add(p.GetProperty("name").GetString()!);
                if (!root.TryGetProperty("functions", out JsonElement fns) || fns.ValueKind != JsonValueKind.Array)
                    continue;
                string id = Path.GetFileNameWithoutExtension(files[i]);
                bool plain = id is "7400" or "7402" or "7404" or "7410" or "7420" or "7430" or "7486";
                foreach (JsonElement fn in fns.EnumerateArray())
                {
                    if (!fn.TryGetProperty("formula", out JsonElement t) || t.ValueKind != JsonValueKind.String)
                        continue;
                    bool ok = ChipFormula.TryParse(t.GetString()!, pins, out _, out string? error);
                    if (plain)
                        Assert.That(ok, Is.True, id + ": " + t.GetString() + ": " + error);
                    if (ok)
                        read++;
                    else
                    {
                        refused++;
                        TestContext.Out.WriteLine("refused " + id + ": " + t.GetString() + " -> " + error);
                    }
                }
            }
            TestContext.Out.WriteLine("formulas read: " + read + ", refused: " + refused);
            Assert.That(read, Is.GreaterThan(0));
        }
    }
}
