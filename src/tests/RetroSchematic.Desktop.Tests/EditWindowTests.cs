//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using NUnit.Framework;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The window with the demo sheet: it opens read-only, the edit-mode switch reaches the
    /// library, a new sheet starts in edit mode, the OPEN colour reaches the screen, and PNG
    /// export writes the whole sheet.
    /// </summary>
    [TestFixture]
    public class EditWindowTests
    {
        private static readonly string Demo = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "demo", "demo-gates.json");

        [AvaloniaTest]
        public void Demo_OpensReadOnly_AndTheSwitchTurnsEditModeOnAndOff()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Demo);
            Assert.That(w.Session!.Editor.ReadOnly, Is.True, "a sheet must open read-only");
            Assert.That(w.FindControl<TextBlock>("ModeText")!.Text, Is.EqualTo("Read-only"));
            Assert.That(w.FindControl<ComboBox>("ChipBox")!.IsEnabled, Is.False);
            Assert.Throws<SheetEditException>(() => w.Session.Editor.DeletePart("1A"));

            w.SetEditMode(true);
            Assert.That(w.Session.Editor.ReadOnly, Is.False);
            Assert.That(w.FindControl<ComboBox>("ChipBox")!.IsEnabled, Is.True, "the chip list needs the catalogue above the test folder");
            w.Session.Editor.DeletePart("1A");
            ListBox parts = w.FindControl<ListBox>("PartsList")!;
            Assert.That(parts.ItemCount, Is.EqualTo(4), "the parts list follows an edit");
            Assert.That(w.Title, Does.Contain("(changed)"));

            w.SetEditMode(false);
            Assert.That(w.Session.Editor.ReadOnly, Is.True);
            w.Close();
        }

        [AvaloniaTest]
        public void NewSheet_StartsEmpty_InEditMode_AndAChipCanBePlaced()
        {
            MainWindow w = new();
            w.Show();
            w.NewSheet();
            Assert.That(w.Session!.Netlist.Parts.Count, Is.EqualTo(0));
            Assert.That(w.Session.Editor.ReadOnly, Is.False);
            Assert.That(w.Session.SheetPath, Is.Null);
            ComboBox chips = w.FindControl<ComboBox>("ChipBox")!;
            chips.SelectedItem = "7400";
            Assert.That(w.FindControl<Controls.SchematicCanvas>("Canvas")!.Tool!.PlaceType, Is.EqualTo("7400"));
            w.FindControl<Controls.SchematicCanvas>("Canvas")!.Tool!.PointerDown(SheetLayout.FromMm(10), SheetLayout.FromMm(10));
            Assert.That(w.Session.Netlist.Parts.Count, Is.EqualTo(1));
            w.Close();
        }

        [AvaloniaTest]
        public void Search_ListsWhatItFinds_AndPickingOneSelectsIt()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Demo);
            w.FindControl<TextBox>("SearchBox")!.Text = "IRQ";
            // TextChanged is raised from the dispatcher queue, not inside the setter
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ListBox found = w.FindControl<ListBox>("FoundList")!;
            Assert.That(found.ItemCount, Is.EqualTo(1));
            found.SelectedIndex = 0;
            Assert.That(w.Session!.Selected, Is.EqualTo("net:IRQ_0"));
            Assert.That(w.FindControl<SelectableTextBlock>("DetailsText")!.Text, Does.Contain("Pull-up: 1k to +5V"));
            w.Close();
        }

        [AvaloniaTest]
        public void ExportPng_WritesTheWholeSheet()
        {
            ThemeManager.Instance.Apply("Light", persist: false);
            MainWindow w = new() { Width = 1200, Height = 800 };
            w.Show();
            w.OpenSheet(Demo);
            string png = Path.Combine(TestAppBuilder.SettingsFolder, "demo.png");
            Directory.CreateDirectory(TestAppBuilder.SettingsFolder);
            w.FindControl<Controls.SchematicCanvas>("Canvas")!.ExportPng(png, 2);
            using (Bitmap b = new(png))
            {
                Assert.That(b.PixelSize.Width, Is.EqualTo((int)System.Math.Ceiling(w.Session!.Drawing.Width * 2)));
                Assert.That(b.PixelSize.Height, Is.EqualTo((int)System.Math.Ceiling(w.Session.Drawing.Height * 2)));
                // the OPEN wire on CLK (to 5A.4) must be in the OPEN colour (#D00000 in Light):
                // one strong channel and two near zero. Red sits in byte 0 or byte 2 depending on
                // the pixel order, so both are taken; no other Light colour has that shape.
                int stride = b.PixelSize.Width * 4;
                byte[] pixels = new byte[stride * b.PixelSize.Height];
                System.Runtime.InteropServices.GCHandle h = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
                try
                {
                    b.CopyPixels(new Avalonia.PixelRect(b.PixelSize), h.AddrOfPinnedObject(), pixels.Length, stride);
                }
                finally
                {
                    h.Free();
                }
                int red = 0;
                for (int i = 0; i + 3 < pixels.Length; i += 4)
                {
                    byte c0 = pixels[i], c1 = pixels[i + 1], c2 = pixels[i + 2];
                    if (c1 < 60 && ((c0 > 150 && c2 < 60) || (c2 > 150 && c0 < 60)))
                        red++;
                }
                Assert.That(red, Is.GreaterThan(20), "no OPEN-coloured pixels in the exported sheet");
            }
            File.Delete(png);
            w.Close();
        }
    }
}
