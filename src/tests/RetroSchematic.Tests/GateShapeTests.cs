//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using NUnit.Framework;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// Both drawings of a gate (ND 1976 and modern) must end every pin line exactly where the
    /// shared pin table says, so switching symbol style never moves a signal line.
    /// </summary>
    [TestFixture]
    public class GateShapeTests
    {
        private static readonly string CatalogRoot = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog", "symbols");

        // Every shape in the catalogue, read from the pin-table folder, so a new shape is checked
        // without editing this list.
        private static readonly string[] Kinds = ReadKinds();

        [Test]
        public void EveryKind_HasPinTableAndBothShapes()
        {
            for (int i = 0; i < Kinds.Length; i++)
            {
                Assert.That(File.Exists(Path.Combine(CatalogRoot, "pins", Kinds[i] + ".json")), Kinds[i]);
                Assert.That(File.Exists(Path.Combine(CatalogRoot, "nd1976", "shapes", Kinds[i] + ".svg")), Kinds[i]);
                Assert.That(File.Exists(Path.Combine(CatalogRoot, "modern", "shapes", Kinds[i] + ".svg")), Kinds[i]);
            }
        }

        [TestCase("nd1976")]
        [TestCase("modern")]
        public void PinLines_EndOnThePinTable(string style)
        {
            for (int k = 0; k < Kinds.Length; k++)
            {
                string kind = Kinds[k];
                Dictionary<string, (double X, double Y)> table = ReadPinTable(kind);
                Dictionary<string, (double X, double Y)> drawn = ReadPinEnds(Path.Combine(CatalogRoot, style, "shapes", kind + ".svg"));

                Assert.That(drawn.Count, Is.EqualTo(table.Count), $"{style}/{kind}: pin count");
                using Dictionary<string, (double X, double Y)>.Enumerator pins = table.GetEnumerator();
                while (pins.MoveNext())
                {
                    KeyValuePair<string, (double X, double Y)> pin = pins.Current;
                    Assert.That(drawn.ContainsKey(pin.Key), $"{style}/{kind}: pin {pin.Key} not drawn");
                    Assert.That(drawn[pin.Key], Is.EqualTo(pin.Value), $"{style}/{kind}: pin {pin.Key} ends in the wrong place");
                }
            }
        }

        private static Dictionary<string, (double X, double Y)> ReadPinTable(string kind)
        {
            Dictionary<string, (double X, double Y)> result = new();
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(CatalogRoot, "pins", kind + ".json")));
            JsonElement pins = doc.RootElement.GetProperty("pins");
            for (int i = 0; i < pins.GetArrayLength(); i++)
            {
                JsonElement p = pins[i];
                result.Add(p.GetProperty("pin").GetString()!, (p.GetProperty("x").GetDouble(), p.GetProperty("y").GetDouble()));
            }
            return result;
        }

        /// <summary>
        /// Every shape name that has a pin table.
        /// </summary>
        /// <returns>
        /// The shape names.
        /// </returns>
        private static string[] ReadKinds()
        {
            string[] files = Directory.GetFiles(Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog", "symbols", "pins"), "*.json");
            string[] kinds = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                kinds[i] = Path.GetFileNameWithoutExtension(files[i]);
            return kinds;
        }

        /// <summary>
        /// The far end (x2, y2) of every line carrying data-pin.
        /// </summary>
        private static Dictionary<string, (double X, double Y)> ReadPinEnds(string path)
        {
            Dictionary<string, (double X, double Y)> result = new();
            XElement root = XElement.Load(path);
            using IEnumerator<XElement> all = root.Descendants().GetEnumerator();
            while (all.MoveNext())
            {
                XElement e = all.Current;
                XAttribute? pin = e.Attribute("data-pin");
                if (pin == null)
                    continue;
                result.Add(pin.Value, (
                    double.Parse(e.Attribute("x2")!.Value, CultureInfo.InvariantCulture),
                    double.Parse(e.Attribute("y2")!.Value, CultureInfo.InvariantCulture)));
            }
            return result;
        }
    }
}
