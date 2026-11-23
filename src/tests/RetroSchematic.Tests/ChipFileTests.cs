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
    /// Every chip file (catalog/chips) must be readable by the code that uses it: the timing list
    /// through <see cref="ChipTimingReader"/>, and every pin with a role from catalog/chips/FORMAT.md.
    /// </summary>
    [TestFixture]
    public class ChipFileTests
    {
        private static readonly string ChipRoot = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog", "chips");

        private static readonly HashSet<string> Roles = new() { "power", "ground", "input", "clock", "enable", "output", "analog", "nc" };

        private static readonly HashSet<string> OutputTypes = new()
        {
            "totem-pole", "open-collector", "open-collector-high-voltage", "three-state", "open-emitter",
        };

        [Test]
        public void EveryChipFile_TimingReads()
        {
            string[] files = Directory.GetFiles(ChipRoot, "*.json");
            Assert.That(files.Length, Is.GreaterThan(0), "no chip files copied to the test folder");
            for (int i = 0; i < files.Length; i++)
                Assert.DoesNotThrow(() => ChipTimingReader.Read(files[i]), Path.GetFileName(files[i]));
        }

        [Test]
        public void EveryPin_HasAKnownRole_AndEveryOutputAType()
        {
            string[] files = Directory.GetFiles(ChipRoot, "*.json");
            for (int f = 0; f < files.Length; f++)
            {
                string name = Path.GetFileName(files[f]);
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(files[f]));
                if (!doc.RootElement.TryGetProperty("pins", out JsonElement pins) || pins.ValueKind != JsonValueKind.Array)
                    continue; // a part with no datasheet found (its file says so in "unknown")
                for (int i = 0; i < pins.GetArrayLength(); i++)
                {
                    string role = pins[i].GetProperty("role").GetString()!;
                    Assert.That(Roles.Contains(role), $"{name} pin {pins[i].GetProperty("pin")}: role '{role}'");
                    if (role == "output")
                    {
                        string type = pins[i].TryGetProperty("outputType", out JsonElement t) ? t.GetString() ?? "" : "";
                        Assert.That(OutputTypes.Contains(type), $"{name} pin {pins[i].GetProperty("pin")}: output type '{type}'");
                    }
                }
            }
        }
    }
}
