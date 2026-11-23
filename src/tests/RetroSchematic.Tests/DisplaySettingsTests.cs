//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The settings window's logic: a sheet's own display values win over the defaults, a
    /// missing value means "use default", the values go through the file and back, and changing
    /// them marks the sheet as changed but never touches its connections.
    /// </summary>
    [TestFixture]
    public class DisplaySettingsTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [Test]
        public void TheSheetsValues_WinOverTheDefaults_AndMissingOnesUseTheDefault()
        {
            DisplaySettings defaults = new() { NameStyle = SignalNameStyle.Tilde, Theme = "Dark", Buses = true, PinNumbers = false };
            SheetDisplay sheet = new() { NameStyle = "subscript", Buses = false };
            DisplaySettings r = DisplaySettings.Resolve(defaults, sheet);
            Assert.That(r.NameStyle, Is.EqualTo(SignalNameStyle.Subscript), "the sheet's value");
            Assert.That(r.Buses, Is.False, "the sheet's value");
            Assert.That(r.Theme, Is.EqualTo("Dark"), "use default");
            Assert.That(r.PinNumbers, Is.False, "use default");
            Assert.That(DisplaySettings.Resolve(defaults, null).NameStyle, Is.EqualTo(SignalNameStyle.Tilde), "no section: all defaults");
            // a value that cannot be read falls back to the default, it does not fail the sheet
            Assert.That(DisplaySettings.Resolve(defaults, new SheetDisplay { NameStyle = "dots", Theme = "NoSuch" }).Theme, Is.EqualTo("Dark"));
            Assert.That(defaults.NameStyle, Is.EqualTo(SignalNameStyle.Tilde), "the defaults themselves are not changed");
        }

        [Test]
        public void LineWidth_ComesFromTheSheet_OrTheDefault_IsKeptInRange_AndThickensEveryLine()
        {
            DisplaySettings defaults = new() { LineWidth = 1.5 };
            Assert.That(DisplaySettings.Resolve(defaults, null).LineWidth, Is.EqualTo(1.5), "use default");
            Assert.That(DisplaySettings.Resolve(defaults, new SheetDisplay { LineWidth = 2 }).LineWidth, Is.EqualTo(2), "the sheet's value");
            Assert.That(DisplaySettings.Resolve(defaults, new SheetDisplay { LineWidth = 40 }).LineWidth, Is.EqualTo(DisplaySettings.MaxLineWidth));
            Assert.That(DisplaySettings.Resolve(defaults, new SheetDisplay { LineWidth = 0 }).LineWidth, Is.EqualTo(DisplaySettings.MinLineWidth));

            // through the file and back
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            n.Display = new SheetDisplay { LineWidth = 2 };
            Assert.That(NetlistReader.Parse(NetlistReader.Write(n)).Display!.LineWidth, Is.EqualTo(2));

            // the drawing carries the factor and the SVG draws every line with it, wires and
            // outlines alike; nothing moves
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog"));
            int items = s.Drawing.Items.Count;
            double width = s.Drawing.Width;
            s.SetSheetDisplay(new SheetDisplay { LineWidth = 2 });
            Assert.That(s.Drawing.LineScale, Is.EqualTo(2));
            Assert.That(s.Drawing.Items.Count, Is.EqualTo(items));
            Assert.That(s.Drawing.Width, Is.EqualTo(width));
            string svg = SvgRenderer.Write(s.Drawing, SchematicTheme.Find("Light")!);
            Assert.That(svg, Does.Contain(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"stroke-width=\"{2 * LineWidths.Wire:0.##}\"")));
            Assert.That(svg, Does.Contain(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"stroke-width=\"{2 * LineWidths.Outline:0.##}\"")));
        }

        [Test]
        public void TheDisplaySection_GoesThroughTheFileAndBack()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            Assert.That(n.Display, Is.Null);
            Assert.That(NetlistReader.Write(n), Does.Not.Contain("\"display\""), "no section when none is set");
            n.Display = new SheetDisplay { NameStyle = "tilde", Symbols = "modern", Theme = "Nord", PinNumbers = false, BoardPositions = true, PortArrows = false, Buses = false, TracePulse = false };
            string json = NetlistReader.Write(n);
            Assert.That(json, Does.Contain("\"display\""));
            SheetDisplay back = NetlistReader.Parse(json).Display!;
            Assert.That(back.NameStyle, Is.EqualTo("tilde"));
            Assert.That(back.Symbols, Is.EqualTo("modern"));
            Assert.That(back.Theme, Is.EqualTo("Nord"));
            Assert.That(back.PinNumbers, Is.False);
            Assert.That(back.BoardPositions, Is.True);
            Assert.That(back.PortArrows, Is.False);
            Assert.That(back.Buses, Is.False);
            Assert.That(back.TracePulse, Is.False);
        }

        [Test]
        public void ChangingThisSheet_MarksItChanged_DrawsAgain_AndNeverChangesConnections()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, ChipCatalog.Find(Sheet2A50));
            string connections = Hierarchy.Connections(s.Editor.Root);
            s.SetDefaults(new DisplaySettings { NameStyle = SignalNameStyle.Slash });
            Assert.That(s.Editor.Modified, Is.False, "defaults are not a change to the sheet");
            Assert.That(s.Display.NameStyle, Is.EqualTo(SignalNameStyle.Slash));

            s.SetSheetDisplay(new SheetDisplay { NameStyle = "subscript", Buses = false });
            Assert.That(s.Editor.Modified, Is.True, "needs saving");
            Assert.That(s.Display.NameStyle, Is.EqualTo(SignalNameStyle.Subscript));
            Assert.That(s.ShowBuses, Is.False);
            Assert.That(s.NameStyle, Is.EqualTo(SignalNameStyle.Subscript), "the drawing follows");
            Assert.That(s.SheetDisplay.NameStyle, Is.EqualTo("subscript"));
            Assert.That(Hierarchy.Connections(s.Editor.Root), Is.EqualTo(connections));

            // back to "use default" for every value: the section goes away
            s.SetSheetDisplay(new SheetDisplay());
            Assert.That(s.Editor.Root.Display, Is.Null);
            Assert.That(s.Display.NameStyle, Is.EqualTo(SignalNameStyle.Slash));
            Assert.That(s.ShowBuses, Is.True);
        }

        [Test]
        public void AViewSwitch_TurnsOverTheSheetsOwnValue_WhenItHasOne()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, ChipCatalog.Find(Sheet2A50));
            Assert.That(s.ToggleSheetValue(DisplayOption.Buses), Is.False, "no own value: the caller changes the default");
            Assert.That(s.Editor.Modified, Is.False);
            s.SetSheetDisplay(new SheetDisplay { Buses = false });
            Assert.That(s.ShowBuses, Is.False);
            Assert.That(s.ToggleSheetValue(DisplayOption.Buses), Is.True);
            Assert.That(s.ShowBuses, Is.True, "back on");
            Assert.That(s.SheetDisplay.Buses, Is.True);
            Assert.That(s.ToggleSheetValue(DisplayOption.Buses), Is.True);
            Assert.That(s.ShowBuses, Is.False, "off again");
        }

        [Test]
        public void ASheetsOwnValues_ApplyWhenItIsOpened()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            n.Display = new SheetDisplay { Symbols = "modern", NameStyle = "tilde" };
            SheetSession s = new(n, null, SymbolStyle.NorskData, ChipCatalog.Find(Sheet2A50));
            Assert.That(s.Style, Is.EqualTo(SymbolStyle.Modern));
            Assert.That(s.NameStyle, Is.EqualTo(SignalNameStyle.Tilde));
        }
    }
}
