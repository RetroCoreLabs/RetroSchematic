//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NUnit.Framework;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The window with a real sheet, drawn by Skia. These check what is ON SCREEN (pixels), not
    /// only that the window opened: a theme that never reaches the drawing would still pass a
    /// test that only looks at objects.
    /// </summary>
    [TestFixture]
    public class MainWindowTests
    {
        private static readonly string Sheet = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

        [AvaloniaTest]
        public void EveryTheme_GivesEveryBrush()
        {
            IReadOnlyList<ThemeDefinition> themes = ThemeManager.Instance.Themes;
            IReadOnlyList<string> keys = ThemeManager.BrushKeys;
            for (int t = 0; t < themes.Count; t++)
            {
                for (int k = 0; k < keys.Count; k++)
                {
                    bool found = Application.Current!.TryGetResource(keys[k], themes[t].Variant, out object? value);
                    Assert.That(found && value is ISolidColorBrush, $"theme {themes[t].Id} has no brush {keys[k]}");
                }
            }
        }

        [AvaloniaTest]
        public void SignalValues_AreReadable_OnEveryThemesPaper()
        {
            Assert.That(ThemeManager.ValueKey(SignalValue.NotSimulated), Is.Null, "no value keeps the ink colour");
            SignalValue[] values = { SignalValue.Unknown, SignalValue.Zero, SignalValue.One, SignalValue.NotDriven, SignalValue.Conflict };
            IReadOnlyList<ThemeDefinition> themes = ThemeManager.Instance.Themes;
            for (int t = 0; t < themes.Count; t++)
            {
                Color paper = Lookup(ThemeManager.SchematicPaperKey, themes[t]);
                for (int i = 0; i < values.Length; i++)
                {
                    Color c = Lookup(ThemeManager.ValueKey(values[i])!, themes[t]);
                    // WCAG: 3 to 1 is the least for lines and large marks
                    Assert.That(Contrast(c, paper), Is.GreaterThanOrEqualTo(3.0), themes[t].Id + " " + values[i] + " is too faint on the paper");
                }
                // Logisim's idea kept: 0 is the darker green, 1 the brighter, and they are clearly apart
                Color zero = Lookup(ThemeManager.ValueZeroKey, themes[t]);
                Color one = Lookup(ThemeManager.ValueOneKey, themes[t]);
                Assert.That(zero.G, Is.GreaterThan(zero.R).And.GreaterThan(zero.B), themes[t].Id + " 0 is not green");
                Assert.That(one.G, Is.GreaterThan(one.R).And.GreaterThan(one.B), themes[t].Id + " 1 is not green");
                Assert.That(Luminance(one), Is.GreaterThan(Luminance(zero)), themes[t].Id + " 1 must be the brighter green");
                Assert.That(Contrast(one, zero), Is.GreaterThanOrEqualTo(1.8), themes[t].Id + " 0 and 1 look too alike");
            }
        }

        private static Color Lookup(string key, ThemeDefinition theme)
        {
            Assert.That(Application.Current!.TryGetResource(key, theme.Variant, out object? brush), Is.True, theme.Id + " " + key);
            return ((ISolidColorBrush)brush!).Color;
        }

        private static double Luminance(Color c)
        {
            static double Lin(byte v)
            {
                double x = v / 255.0;
                return x <= 0.03928 ? x / 12.92 : System.Math.Pow((x + 0.055) / 1.055, 2.4);
            }

            return (0.2126 * Lin(c.R)) + (0.7152 * Lin(c.G)) + (0.0722 * Lin(c.B));
        }

        private static double Contrast(Color a, Color b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (System.Math.Max(la, lb) + 0.05) / (System.Math.Min(la, lb) + 0.05);
        }

        [AvaloniaTest]
        public void Sheet_IsDrawn_InkOnPaper_InTheThemesColours()
        {
            ThemeManager.Instance.Apply("Light", persist: false);
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            // the drawing's colours are the shared table's; "ink" here is the chip outline colour,
            // drawn round every chip
            SchematicTheme light = SchematicTheme.Find(ThemeManager.Instance.Themes[0].Id)!;
            SchematicTheme dark = SchematicTheme.Find(ThemeManager.Instance.Themes[1].Id)!;

            // Counted inside the drawing area only, so the window's own text (which in the Dark
            // theme is the same colour as the ink) cannot be mistaken for the sheet.
            Counts empty = Count(w, light);
            Assert.That(empty.Paper, Is.GreaterThan(10000), "the empty drawing area is not painted in the Light paper colour");
            Assert.That(empty.Ink, Is.EqualTo(0), "ink in the drawing area before any sheet was opened");

            w.OpenSheet(Sheet);
            Assert.That(w.Session!.Netlist.Parts.Count, Is.GreaterThan(20));
            Counts withSheet = Count(w, light);
            Assert.That(withSheet.Ink, Is.GreaterThan(0), "no ink drawn - the sheet did not reach the screen");
            Assert.That(withSheet.Paper, Is.GreaterThan(10000));

            // a theme change must re-colour the drawing itself, not only the window frame
            ThemeManager.Instance.Apply("Dark", persist: false);
            Counts darkCount = Count(w, dark);
            Assert.That(darkCount.Paper, Is.GreaterThan(10000), "the drawing kept its old paper after the theme change");
            Assert.That(darkCount.Ink, Is.GreaterThan(0), "no ink in the Dark theme's colour");
            Assert.That(Count(w, light).Paper, Is.EqualTo(0), "Light paper is still on screen in the Dark theme");
            w.Close();
        }

        [AvaloniaTest]
        public void PickingAPartInTheList_ShowsItsDetails_AndChangingStyle_KeepsThePick()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet);
            ListBox parts = w.FindControl<ListBox>("PartsList")!;
            parts.SelectedIndex = 0;
            string first = w.Session!.Netlist.Parts[0].Ref;
            Assert.That(w.Session.Selected, Is.EqualTo("part:" + first));
            SelectableTextBlock details = w.FindControl<SelectableTextBlock>("DetailsText")!;
            Assert.That(details.Text, Does.StartWith("Part " + first));

            w.Session.SetStyle(SymbolStyle.Modern);
            Assert.That(w.Session.Selected, Is.EqualTo("part:" + first));
            w.Close();
        }

        /// <summary>
        /// Pixels of the paper colour and of the ink colour inside the drawing area.
        /// </summary>
        /// <param name="w">
        /// The shown window.
        /// </param>
        /// <param name="p">
        /// The theme whose paper and ink colours are counted.
        /// </param>
        /// <returns>
        /// How many pixels show each colour.
        /// </returns>
        private static unsafe Counts Count(Window w, SchematicTheme p)
        {
            Color paper = Color.Parse(p.Get(SchematicColour.Paper));
            Color ink = Color.Parse(p.Get(SchematicColour.ChipOutline));
            WriteableBitmap? frame = w.CaptureRenderedFrame() as WriteableBitmap;
            Assert.That(frame, Is.Not.Null, "no frame captured");
            // only the drawing area: the canvas's rectangle in window pixels
            Control canvas = w.FindControl<Control>("Canvas")!;
            Point topLeft = canvas.TranslatePoint(new Point(0, 0), w)!.Value;
            int x0 = (int)topLeft.X + 2, y0 = (int)topLeft.Y + 2;
            int x1 = x0 + (int)canvas.Bounds.Width - 4, y1 = y0 + (int)canvas.Bounds.Height - 4;
            using ILockedFramebuffer fb = frame!.Lock();
            // Skia gives RGBA on this machine; BGRA is handled too. Any other format fails here
            // rather than counting wrong colours.
            bool rgba = fb.Format == PixelFormat.Rgba8888;
            Assert.That(rgba || fb.Format == PixelFormat.Bgra8888, "unexpected pixel format " + fb.Format);
            int paperCount = 0, inkCount = 0;
            for (int y = y0; y < y1 && y < fb.Size.Height; y++)
            {
                uint* row = (uint*)(fb.Address + (y * fb.RowBytes));
                for (int x = x0; x < x1 && x < fb.Size.Width; x++)
                {
                    uint px = row[x];
                    // the four bytes read as one little-endian uint: BGRA = 0xAARRGGBB, RGBA = 0xAABBGGRR
                    byte r = rgba ? (byte)px : (byte)(px >> 16);
                    byte g = (byte)(px >> 8);
                    byte b = rgba ? (byte)(px >> 16) : (byte)px;
                    // Paper must match exactly. Ink is counted when the pixel is nearer the ink
                    // colour than the paper colour: at fit-to-window size every line is smoothed,
                    // so hardly a pixel is pure ink, but the smoothed ones still lean to it.
                    if (r == paper.R && g == paper.G && b == paper.B)
                        paperCount++;
                    else if (Distance(r, g, b, ink) < Distance(r, g, b, paper))
                        inkCount++;
                }
            }
            return new Counts(paperCount, inkCount);
        }

        private static int Distance(byte r, byte g, byte b, Color c)
            => ((r - c.R) * (r - c.R)) + ((g - c.G) * (g - c.G)) + ((b - c.B) * (b - c.B));

        private readonly struct Counts
        {
            public readonly int Paper;
            public readonly int Ink;

            public Counts(int paper, int ink)
            {
                Paper = paper;
                Ink = ink;
            }
        }
    }
}
