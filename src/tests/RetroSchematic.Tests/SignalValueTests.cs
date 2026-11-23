//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using NUnit.Framework;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// Simulated signal values on the session: which drawing items take a value's colour.
    /// </summary>
    [TestFixture]
    public class SignalValueTests
    {
        [Test]
        public void WithoutValues_NothingIsColouredByValue()
        {
            SheetSession s = new(DemoSheetLayoutTests.Demo, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);
            for (int i = 0; i < s.Drawing.Items.Count; i++)
                Assert.That(s.ValueOf(s.Drawing.Items[i]), Is.EqualTo(SignalValue.NotSimulated));
        }

        [Test]
        public void AValue_ColoursTheSignalsWiresAndLabels_NotTheChips()
        {
            SheetSession s = new(DemoSheetLayoutTests.Demo, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);
            int calls = 0;
            s.DrawingChanged += _ => calls++;
            s.SetValue("IRQ", SignalValue.Conflict);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(s.ValueOf("IRQ"), Is.EqualTo(SignalValue.Conflict));
            Assert.That(s.ValueOf(new DrawItem { Owner = "net:IRQ", Role = DrawRole.NetLabel }), Is.EqualTo(SignalValue.Conflict));
            Assert.That(s.ValueOf(new DrawItem { Owner = "net:IRQ", Role = DrawRole.Wire }), Is.EqualTo(SignalValue.Conflict));
            Assert.That(s.ValueOf(new DrawItem { Owner = "net:Q", Role = DrawRole.Wire }), Is.EqualTo(SignalValue.NotSimulated));
            Assert.That(s.ValueOf(new DrawItem { Owner = "part:5A", Role = DrawRole.PartOutline }), Is.EqualTo(SignalValue.NotSimulated));
            s.SetValue("IRQ", SignalValue.NotSimulated);
            Assert.That(s.ValueOf("IRQ"), Is.EqualTo(SignalValue.NotSimulated));
        }
    }
}
