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
    /// The structured behaviour of every chip file (<c>behaviour</c>, FORMAT.md): every chip has
    /// one, every formula in it is read by the formula reader, every pin it names exists, every
    /// output pin is covered, and "none" always carries a reason. One test case per chip, so a
    /// failure names the chip.
    /// </summary>
    [TestFixture]
    public class ChipBehaviourTests
    {
        private static readonly string Chips = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog", "chips");

        /// <summary>
        /// Every chip file id in the catalogue.
        /// </summary>
        public static IEnumerable<string> ChipIds()
        {
            string folder = Path.Combine(Path.GetDirectoryName(typeof(ChipBehaviourTests).Assembly.Location)!, "catalog", "chips");
            string[] files = Directory.GetFiles(folder, "*.json");
            System.Array.Sort(files, System.StringComparer.Ordinal);
            for (int i = 0; i < files.Length; i++)
                yield return Path.GetFileNameWithoutExtension(files[i]);
        }

        /// <summary>
        /// Reads a chip file: its behaviour, its pin names and its output pin names.
        /// </summary>
        internal static ChipBehaviour? Load(string id, out List<string> pins, out List<string> outputs)
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Chips, id + ".json")));
            JsonElement root = doc.RootElement;
            pins = new List<string>();
            outputs = new List<string>();
            JsonElement ps = root.GetProperty("pins");
            for (int i = 0; i < ps.GetArrayLength(); i++)
            {
                // an inverted pin (a bar in the datasheet) is named /NAME in formulas
                string name = ps[i].GetProperty("name").GetString()!;
                if (ps[i].TryGetProperty("inverted", out JsonElement inv) && inv.ValueKind == JsonValueKind.True)
                    name = "/" + name;
                pins.Add(name);
                if (ps[i].TryGetProperty("role", out JsonElement r) && r.GetString() == "output")
                    outputs.Add(name);
            }
            if (!root.TryGetProperty("behaviour", out JsonElement b) || b.ValueKind != JsonValueKind.Object)
                return null;
            return ChipBehaviour.Parse(b);
        }

        [TestCaseSource(nameof(ChipIds))]
        public void EveryChip_HasABehaviour_ThatTheReaderAccepts(string id)
        {
            ChipBehaviour? b = Load(id, out List<string> pins, out List<string> outputs);
            Assert.That(b, Is.Not.Null, id + ": no behaviour object");
            List<string> errors = b!.Check(pins, outputs);
            Assert.That(errors, Is.Empty, id + ":\n  " + string.Join("\n  ", errors));
        }

        [Test]
        public void TheChecker_RefusesWhatIsWrong()
        {
            using JsonDocument doc = JsonDocument.Parse(
                "{\"kind\":\"logic\",\"source\":\"p\",\"stored\":[{\"name\":\"FF1\",\"kind\":\"flip-flop\",\"next\":\"FF1 = D\"," +
                "\"clock\":{\"pin\":\"CK\",\"edge\":\"sideways\"},\"set\":{\"pin\":\"PR\",\"active\":\"low\"},\"clear\":{\"pin\":\"CLR\",\"active\":\"low\"}}]," +
                "\"outputs\":[{\"pin\":\"Q\",\"formula\":\"Q = FF1 when G\",\"type\":\"three-state\"}]}");
            ChipBehaviour b = ChipBehaviour.Parse(doc.RootElement);
            List<string> errors = b.Check(new[] { "D", "CK", "PR", "CLR", "Q", "QN" }, new[] { "Q", "QN" });
            string all = string.Join("\n", errors);
            Assert.That(all, Does.Contain("sideways"));
            Assert.That(all, Does.Contain("not what happens when both act"));
            Assert.That(all, Does.Contain("Q = FF1 when G"));
            Assert.That(all, Does.Contain("three-state output without an output enable"));
            Assert.That(all, Does.Contain("output pin QN has no behaviour"));
        }

        /// <summary>
        /// A RAM that latches its address at a clock edge names the stored bits as its address,
        /// and an output enable may be an expression over stored bits (format additions for the
        /// 2107B, TMS4030 and MK4096). A name that is neither a pin nor a stored bit is refused.
        /// </summary>
        [Test]
        public void LatchedAddress_AndEnableExpression_AreAccepted_AndChecked()
        {
            string json =
                "{\"kind\":\"logic\",\"source\":\"p\",\"stored\":[" +
                "{\"name\":\"LA0\",\"kind\":\"flip-flop\",\"next\":\"LA0 = A0\",\"clock\":{\"pin\":\"CE\",\"edge\":\"rising\"},\"source\":\"p\"}," +
                "{\"name\":\"LCS\",\"kind\":\"flip-flop\",\"next\":\"LCS = CS\",\"clock\":{\"pin\":\"CE\",\"edge\":\"rising\"},\"source\":\"p\"}]," +
                "\"memory\":{\"name\":\"MEM\",\"words\":2,\"width\":1,\"address\":[\"ADDR\"],\"dataIn\":[\"DIN\"]," +
                "\"write\":{\"when\":\"CE AND NOT WE AND NOT LCS\",\"edge\":\"level-high\"},\"source\":\"p\"}," +
                "\"outputs\":[{\"pin\":\"DOUT\",\"formula\":\"DOUT = NOT MEM1\",\"type\":\"three-state\"," +
                "\"outputEnable\":{\"when\":\"ENABLE-EXPR\",\"disabled\":\"Z\"},\"source\":\"p\"}]}";
            string[] pins = { "A0", "CS", "CE", "WE", "DIN", "DOUT" };
            string[] outs = { "DOUT" };

            List<string> good = ChipBehaviour.Parse(JsonDocument.Parse(json.Replace("ADDR", "LA0").Replace("ENABLE-EXPR", "CE AND NOT LCS")).RootElement).Check(pins, outs);
            Assert.That(good, Is.Empty, string.Join("\n", good));

            List<string> bad = ChipBehaviour.Parse(JsonDocument.Parse(json.Replace("ADDR", "LX9").Replace("ENABLE-EXPR", "CE AND NOT LQQ")).RootElement).Check(pins, outs);
            string all = string.Join("\n", bad);
            Assert.That(all, Does.Contain("memory address: LX9 is not a pin or stored bit"));
            Assert.That(all, Does.Contain("DOUT output enable"));
        }

        /// <summary>
        /// The 74181 gate equations transcribed from the functional block diagram (TI 1976 book
        /// page 7-280) give the datasheet's Table 2 (active-high data, page 7-272) for every A and
        /// B: F = A PLUS B (S = HLLH, M = L, Cn = H, no carry), A PLUS B PLUS 1 (Cn = L), A MINUS
        /// B MINUS 1 (S = LHHL, M = L, Cn = H), and the logic rows A XOR B (S = LHHL, M = H) and
        /// AB (S = HLHH, M = H). Cn+4 is low exactly when A PLUS B carries.
        /// </summary>
        [Test]
        public void Alu74181_GateEquations_GiveTable2()
        {
            ChipBehaviour b = Load("74181", out List<string> pins, out _)!;
            List<string> names = b.Names(pins);
            List<ChipFormula> steps = new();
            for (int i = 0; i < b.Internal.Count; i++)
                steps.Add(Parse(b.Internal[i].Formula, names));
            for (int i = 0; i < b.Outputs.Count; i++)
                steps.Add(Parse(b.Outputs[i].Formula, names));
            int[,] rows = { { 0b1001, 0, 1, 0 }, { 0b1001, 0, 0, 1 }, { 0b0110, 0, 1, 2 }, { 0b0110, 1, 1, 3 }, { 0b1011, 1, 1, 4 } };
            for (int r = 0; r < rows.GetLength(0); r++)
            {
                for (int a = 0; a < 16; a++)
                {
                    for (int bb = 0; bb < 16; bb++)
                    {
                        Dictionary<string, bool> v = new();
                        for (int k = 0; k < 4; k++)
                        {
                            v["/A" + k] = ((a >> k) & 1) != 0;
                            v["/B" + k] = ((bb >> k) & 1) != 0;
                            v["S" + k] = ((rows[r, 0] >> k) & 1) != 0;
                        }
                        v["M"] = rows[r, 1] != 0;
                        v["Cn"] = rows[r, 2] != 0;
                        for (int s = 0; s < steps.Count; s++)
                            v[steps[s].Target] = steps[s].Expression.Evaluate(v);
                        int f = (v["/F0"] ? 1 : 0) | (v["/F1"] ? 2 : 0) | (v["/F2"] ? 4 : 0) | (v["/F3"] ? 8 : 0);
                        int want = rows[r, 3] switch
                        {
                            0 => (a + bb) & 15,
                            1 => (a + bb + 1) & 15,
                            2 => (a - bb - 1) & 15,
                            3 => a ^ bb,
                            _ => a & bb,
                        };
                        Assert.That(f, Is.EqualTo(want), "row " + r + " A=" + a + " B=" + bb);
                        if (rows[r, 3] == 0)
                            Assert.That(v["Cn+4"], Is.EqualTo(a + bb <= 15), "carry A=" + a + " B=" + bb);
                    }
                }
            }
        }

        private static ChipFormula Parse(string formula, List<string> names)
        {
            Assert.That(ChipFormula.TryParse(formula, names, out ChipFormula? f, out string? error), Is.True, formula + ": " + error);
            return f!;
        }

        [Test]
        public void None_NeedsAReason()
        {
            using JsonDocument doc = JsonDocument.Parse("{\"kind\":\"none\"}");
            List<string> errors = ChipBehaviour.Parse(doc.RootElement).Check(new[] { "A" }, new string[0]);
            Assert.That(errors, Has.Count.EqualTo(1));
        }
    }
}
