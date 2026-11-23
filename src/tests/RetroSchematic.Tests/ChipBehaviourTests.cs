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
                string name = ps[i].GetProperty("name").GetString()!;
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

        [Test]
        public void None_NeedsAReason()
        {
            using JsonDocument doc = JsonDocument.Parse("{\"kind\":\"none\"}");
            List<string> errors = ChipBehaviour.Parse(doc.RootElement).Check(new[] { "A" }, new string[0]);
            Assert.That(errors, Has.Count.EqualTo(1));
        }
    }
}
