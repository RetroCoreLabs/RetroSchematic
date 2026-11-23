//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using NUnit.Framework;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The viewer's logic, without a window: picking by position, what is lit, the details text,
    /// and that the SVG output and the screen come from one drawing.
    /// </summary>
    [TestFixture]
    public class SheetSessionTests
    {
        private static readonly string Sheet = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [Test]
        public void ClickingAPartLabel_PicksThePart_AndDescribesIt()
        {
            SheetSession s = new(Sheet, SymbolStyle.NorskData, null);
            DrawItem label = FindLabel(s.Drawing, "part:13D");
            s.PickAt(label.X1 + 2, label.Y1 - 2);
            Assert.That(s.Selected, Is.EqualTo("part:13D"));
            Assert.That(s.Describe(), Does.StartWith("Part 13D: 7442"));
        }

        [Test]
        public void ClickingASignalName_PicksTheSignal_AndListsItsPins()
        {
            SheetSession s = new(Sheet, SymbolStyle.NorskData, null);
            DrawItem signal = FindFirst(s.Drawing, "net:");
            s.PickAt(signal.X1 + (signal.Anchor == TextAnchor.End ? -2 : 2), signal.Y1 - 2);
            Assert.That(s.Selected, Is.EqualTo(signal.Owner));
            Assert.That(s.Describe(), Does.StartWith("Signal ").And.Contain("Pins:"));
        }

        [Test]
        public void ClickingEmptyPaper_PicksNothing()
        {
            SheetSession s = new(Sheet, SymbolStyle.NorskData, null);
            s.Select("part:13D");
            s.PickAt(-100, -100);
            Assert.That(s.Selected, Is.Empty);
            Assert.That(s.Describe(), Is.Empty);
        }

        [Test]
        public void PickingAPart_LightsItsGates_ButNotAnotherPartsGates()
        {
            SheetSession s = new(Sheet, SymbolStyle.NorskData, null);
            s.Select("part:11D");
            Assert.That(s.IsLit(new DrawItem { Owner = "gate:11D/1" }), Is.True);
            Assert.That(s.IsLit(new DrawItem { Owner = "gate:11C/1" }), Is.False);
            // "11" must not match "11D": only a whole part reference followed by '/'
            s.Select("part:11");
            Assert.That(s.IsLit(new DrawItem { Owner = "gate:11D/1" }), Is.False);
        }

        [Test]
        public void ChangingStyle_LaysOutAgain_AndTellsTheWindow()
        {
            SheetSession s = new(Sheet, SymbolStyle.NorskData, null);
            SchematicDrawing before = s.Drawing;
            int calls = 0;
            s.DrawingChanged += _ => calls++;
            s.SetStyle(SymbolStyle.Modern);
            Assert.That(s.Drawing, Is.Not.SameAs(before));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(SvgRenderer.Write(s.Drawing), Does.Contain("modern symbols"));
        }

        [Test]
        public void WithTheCatalogue_APartShowsItsChipTitle()
        {
            string catalog = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog");
            SheetSession s = new(Sheet, SymbolStyle.NorskData, catalog);
            s.Select("part:13D");
            Assert.That(s.Describe(), Does.Contain("Catalogue: "));
        }

        private static DrawItem FindLabel(SchematicDrawing d, string owner)
        {
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Owner == owner && d.Items[i].Role == DrawRole.PartLabel)
                    return d.Items[i];
            }
            Assert.Fail("no label for " + owner);
            return null!;
        }

        private static DrawItem FindFirst(SchematicDrawing d, string ownerPrefix)
        {
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Owner.StartsWith(ownerPrefix, System.StringComparison.Ordinal) && d.Items[i].Kind == DrawKind.Text)
                    return d.Items[i];
            }
            Assert.Fail("no text owned by " + ownerPrefix);
            return null!;
        }
    }
}
