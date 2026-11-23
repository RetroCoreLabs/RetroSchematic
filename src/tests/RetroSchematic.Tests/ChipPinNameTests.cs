//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using RetroSchematic.Model;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// Inverted pin names (FORMAT.md, Pins): a pin the datasheet draws with a bar is stored as its
    /// base name plus <c>"inverted": true</c>, and formulas write it as <c>/NAME</c>. These tests
    /// check that no chip file has a raw ' in a pin name, and that every formula still gives the
    /// same truth table as before the names were changed (the record in
    /// <c>Data/pin-rename-truth.json</c> was made from the old files, with the old names mapped to
    /// the new ones, by <see cref="WriteRecord"/>).
    /// </summary>
    [TestFixture]
    public class ChipPinNameTests
    {
        private static readonly string Chips = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog", "chips");

        [TestCaseSource(typeof(ChipBehaviourTests), nameof(ChipBehaviourTests.ChipIds))]
        public void NoPinName_HasARawPrime(string id)
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Chips, id + ".json")));
            JsonElement ps = doc.RootElement.GetProperty("pins");
            for (int i = 0; i < ps.GetArrayLength(); i++)
            {
                string name = ps[i].GetProperty("name").GetString()!;
                Assert.That(name, Does.Not.Contain("'"), id + " pin " + ps[i].GetProperty("pin").GetInt32());
                Assert.That(name, Does.Not.StartWith("/"), id + ": the / belongs to formulas, the name is stored without it");
            }
        }

        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        /// <summary>
        /// The one-shot 17D (74221) on 2A50: pin 4 is 1Q with a bar, pin 13 plain 1Q. The chip box,
        /// the Details pin table and the signal pin list all write pin 4 in the chosen style.
        /// </summary>
        [TestCase(Render.SignalNameStyle.Slash, "/1Q", "", "/1Q")]
        [TestCase(Render.SignalNameStyle.Tilde, "~1Q", "", "~1Q")]
        [TestCase(Render.SignalNameStyle.Subscript, "1Q", "0", "1Q_0")]
        public void InvertedPin_IsShown_InTheNameStyle(Render.SignalNameStyle style, string boxText, string boxSub, string line)
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            ChipCatalog cat = new(ChipCatalog.Find(Sheet2A50)!);
            Render.SchematicDrawing d = Render.SheetLayout.Build(n, Render.SymbolStyle.NorskData, new Render.LayoutOptions { Catalog = cat, NameStyle = style });
            Render.DrawItem? box4 = null, box13 = null;
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Role == Render.DrawRole.PinName && d.Items[i].Owner == "pin:17D.4")
                    box4 = d.Items[i];
                if (d.Items[i].Role == Render.DrawRole.PinName && d.Items[i].Owner == "pin:17D.13")
                    box13 = d.Items[i];
            }
            Assert.That(box4, Is.Not.Null);
            Assert.That(box4!.Text, Is.EqualTo(boxText));
            Assert.That(box4.Subscript, Is.EqualTo(boxSub));
            Assert.That(box13!.Text + box13.Subscript, Is.EqualTo("1Q"), "a plain pin is plain in every style");

            Render.SheetEditor e = new(n, cat);
            Render.PartInfo p = Render.PartInfo.Of(e, "17D", style)!;
            string? row4 = null;
            for (int i = 0; i < p.Pins.Count; i++)
            {
                if (p.Pins[i].Number == 4)
                    row4 = p.Pins[i].Name;
            }
            Assert.That(row4, Is.EqualTo(line));

            Net? rr = e.NetOf("17D.13");
            Assert.That(rr, Is.Not.Null);
            Render.SignalInfo s = Render.SignalInfo.Of(e, rr!, style);
            bool found = false;
            for (int i = 0; i < s.Pins.Count; i++)
                found |= s.Pins[i].At == "17D.13" && s.Pins[i].PinName == "1Q";
            Assert.That(found, Is.True, "the signal's pin list names 17D.13 as 1Q");
        }

        /// <summary>
        /// Every formula of every chip file gives the truth table recorded before the rename.
        /// </summary>
        [Test]
        public void EveryFormula_KeepsItsTruthTable()
        {
            string recordPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "pin-rename-truth.json");
            using JsonDocument rec = JsonDocument.Parse(File.ReadAllText(recordPath));
            Dictionary<string, string> now = Compute(Chips, null);
            List<string> wrong = new();
            int count = 0;
            foreach (JsonProperty p in rec.RootElement.EnumerateObject())
            {
                count++;
                if (!now.TryGetValue(p.Name, out string? hash))
                    wrong.Add(p.Name + ": the formula is gone");
                else if (hash != p.Value.GetString())
                    wrong.Add(p.Name + ": truth table changed");
            }
            Assert.That(count, Is.GreaterThan(500), "the record holds every formula");
            Assert.That(now.Count, Is.EqualTo(count), "a formula was added or lost");
            Assert.That(wrong, Is.Empty, string.Join("\n", wrong));
        }

        /// <summary>
        /// Writes the truth-table record from the chip files as they are now, with each old
        /// inverted name (X') read as the new key (/X). Run once, on the old files, before the
        /// rename script.
        /// </summary>
        [Test]
        [Explicit("writes the record; run only on the files from before the rename")]
        public void WriteRecord()
        {
            string? folder = Environment.GetEnvironmentVariable("RETROSCHEMATIC_TRUTH_OUT");
            Assert.That(folder, Is.Not.Null, "set RETROSCHEMATIC_TRUTH_OUT to the test project's Data folder");
            Dictionary<string, string> map = new();
            Dictionary<string, string> all = Compute(Chips, map);
            List<string> keys = new(all.Keys);
            keys.Sort(StringComparer.Ordinal);
            StringBuilder sb = new("{\n");
            for (int i = 0; i < keys.Count; i++)
                sb.Append("  ").Append(JsonSerializer.Serialize(keys[i])).Append(": \"").Append(all[keys[i]]).Append('"').Append(i + 1 < keys.Count ? ",\n" : "\n");
            sb.Append("}\n");
            File.WriteAllText(Path.Combine(folder!, "pin-rename-truth.json"), sb.ToString());
        }

        /// <summary>
        /// The truth-table hash of every behaviour formula of every chip file, keyed
        /// "chip|place|target". With <paramref name="oldNames"/> not null, a pin name ending in '
        /// is read as /name (the old files); with null the names are taken as they are.
        /// </summary>
        internal static Dictionary<string, string> Compute(string folder, Dictionary<string, string>? oldNames)
        {
            Dictionary<string, string> result = new();
            string[] files = Directory.GetFiles(folder, "*.json");
            Array.Sort(files, StringComparer.Ordinal);
            for (int f = 0; f < files.Length; f++)
            {
                string id = Path.GetFileNameWithoutExtension(files[f]);
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(files[f]));
                JsonElement root = doc.RootElement;
                List<string> pins = new();
                JsonElement ps = root.GetProperty("pins");
                for (int i = 0; i < ps.GetArrayLength(); i++)
                {
                    string name = ps[i].GetProperty("name").GetString()!;
                    bool inv = ps[i].TryGetProperty("inverted", out JsonElement iv) && iv.ValueKind == JsonValueKind.True;
                    pins.Add(inv ? "/" + name : name);
                }
                if (!root.TryGetProperty("behaviour", out JsonElement b) || b.ValueKind != JsonValueKind.Object)
                    continue;
                ChipBehaviour cb = ChipBehaviour.Parse(b);
                List<string> names = cb.Names(pins);
                List<(string Place, string Text, bool Expression)> all = new();
                for (int i = 0; i < cb.Outputs.Count; i++)
                {
                    all.Add(("out" + i, cb.Outputs[i].Formula, false));
                    if (cb.Outputs[i].OutputEnable != null && cb.Outputs[i].OutputEnable!.When.Length > 0)
                        all.Add(("oe" + i, cb.Outputs[i].OutputEnable!.When, true));
                }
                for (int i = 0; i < cb.Internal.Count; i++)
                    all.Add(("int" + i, cb.Internal[i].Formula, false));
                for (int i = 0; i < cb.Stored.Count; i++)
                {
                    StoredBit s = cb.Stored[i];
                    if (s.Next.Length > 0)
                        all.Add(("next" + i, s.Next, false));
                    if (s.Clock != null && s.Clock.When.Length > 0)
                        all.Add(("clock" + i, s.Clock.When, true));
                    for (int k = 0; k < s.Async.Count; k++)
                    {
                        all.Add(("async" + i + "." + k + "w", s.Async[k].When, true));
                        all.Add(("async" + i + "." + k + "v", s.Async[k].Value, true));
                    }
                }
                if (cb.Memory?.Write != null && cb.Memory.Write.When.Length > 0)
                    all.Add(("write", cb.Memory.Write.When, true));
                for (int i = 0; i < all.Count; i++)
                {
                    (string place, string text, bool expression) = all[i];
                    List<string> withTarget = new(names) { "EXPRESSION-TARGET" };
                    string formula = expression ? "EXPRESSION-TARGET = " + text : text;
                    Assert.That(ChipFormula.TryParse(formula, withTarget, out ChipFormula? cf, out string? error), Is.True, id + " " + formula + ": " + error);
                    string target = Key(cf!.Target, oldNames);
                    result[id + "|" + place + "|" + target] = Hash(cf.Expression, oldNames);
                }
            }
            return result;
        }

        /// <summary>
        /// A name as the new files write it: X' becomes /X when reading the old files.
        /// </summary>
        private static string Key(string name, Dictionary<string, string>? oldNames)
        {
            if (oldNames != null && name.Length > 1 && name[^1] == '\'')
                return "/" + name.Substring(0, name.Length - 1);
            return name;
        }

        /// <summary>
        /// Evaluates the expression for every value of its names (sorted by their new key), or for
        /// 4096 fixed pseudo-random values when it has more than 12, and hashes the results.
        /// </summary>
        private static string Hash(FormulaNode e, Dictionary<string, string>? oldNames)
        {
            List<string> used = new();
            e.CollectPins(used);
            List<(string Key, string Name)> vars = new();
            for (int i = 0; i < used.Count; i++)
            {
                bool seen = false;
                for (int k = 0; k < vars.Count; k++)
                    seen |= vars[k].Name == used[i];
                if (!seen)
                    vars.Add((Key(used[i], oldNames), used[i]));
            }
            vars.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            StringBuilder bits = new();
            for (int k = 0; k < vars.Count; k++)
                bits.Append(vars[k].Key).Append(',');
            bits.Append('|');
            bool all = vars.Count <= 12;
            int rows = all ? 1 << vars.Count : 4096;
            ulong seed = 0x9E3779B97F4A7C15UL;
            Dictionary<string, bool> v = new();
            for (int r = 0; r < rows; r++)
            {
                ulong pattern;
                if (all)
                {
                    pattern = (ulong)r;
                }
                else
                {
                    seed = seed * 6364136223846793005UL + 1442695040888963407UL;
                    pattern = seed;
                }
                for (int k = 0; k < vars.Count; k++)
                    v[vars[k].Name] = ((pattern >> (k % 64)) & 1) != 0;
                bits.Append(e.Evaluate(v) ? '1' : '0');
            }
            byte[] h = SHA256.HashData(Encoding.ASCII.GetBytes(bits.ToString()));
            return Convert.ToHexString(h, 0, 12);
        }
    }
}
