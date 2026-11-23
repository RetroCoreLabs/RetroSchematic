//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The Details panel: cards for a traced signal and for a picked part, folding, and pin rows
    /// that move the view. With RETROSCHEMATIC_PANEL_PNG set to a folder, the window is also
    /// saved as a picture in a light and a dark theme, to look at the panel.
    /// </summary>
    [TestFixture]
    public class DetailsPanelTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

        [AvaloniaTest]
        public void ATracedSignal_ShowsItsSections_AndAPinRowCentresTheView()
        {
            MainWindow w = Open();
            w.PickFromList("net:LEV12_0");
            Controls.DetailsPanel d = w.FindControl<Controls.DetailsPanel>("Details")!;
            int pins = w.Session!.SignalInfoOf("LEV12_0")!.Pins.Count;
            Assert.That(d.Sections, Is.EqualTo(new[] { "Signal", "Description", "Electrical", "Where it goes", "Pins (" + pins + ")" }));
            string text = d.AllText();
            Assert.That(text, Does.Contain("active low"));
            Assert.That(text, Does.Contain("OPEN-COLLECTOR"));
            Assert.That(text, Does.Contain("backplane ?.35 BINT12"));
            Save(w, "panel-signal");

            // a pin row moves the view to that pin
            Controls.SchematicCanvas canvas = w.FindControl<Controls.SchematicCanvas>("Canvas")!;
            string pin = d.PinRows[d.PinRows.Count - 1];
            canvas.CentreOn(0, 0);
            Find(d, b => b.Classes.Contains("pin-row") && (string?)b.Tag == pin)!
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            DrawPoint at = w.Session.Drawing.Pins[pin];
            Assert.That(canvas.IsOnScreen(at.X, at.Y), Is.True);
            w.Close();
        }

        [AvaloniaTest]
        public void APickedPart_ShowsItsHeaderAndPins_AndASignalLinkTracesIt()
        {
            MainWindow w = Open();
            w.PickFromList("part:13D");
            Controls.DetailsPanel d = w.FindControl<Controls.DetailsPanel>("Details")!;
            Assert.That(d.Sections[0], Is.EqualTo("Part"));
            Assert.That(d.Sections[1], Does.StartWith("Pins ("));
            Assert.That(d.AllText(), Does.Contain("13D  7442"));
            Assert.That(d.PinRows, Does.Contain("13D.1"));
            Save(w, "panel-part");
            Button? link = Find(d, b => b.Classes.Contains("signal-link"));
            Assert.That(link, Is.Not.Null);
            string key = (string)link!.Tag!;
            link.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.That(w.Session!.Traced, Is.EqualTo(key));
            w.Close();
        }

        [AvaloniaTest]
        public void ThePickedBackplaneConnector_ListsItsSignals_AndARowTracesIt()
        {
            MainWindow w = Open();
            w.Session!.Select("conn:P");
            Controls.DetailsPanel d = w.FindControl<Controls.DetailsPanel>("Details")!;
            Assert.That(d.Sections[0], Is.EqualTo("Connector"));
            Assert.That(d.AllText(), Does.Contain("/LEV12"));
            Save(w, "panel-connector");
            Find(d, b => b.Classes.Contains("pin-row") && (string?)b.Tag == "LEV12_0")!
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.That(w.Session.Traced, Is.EqualTo("LEV12_0"));
            Assert.That(w.Session.Selected, Is.EqualTo("net:LEV12_0"));
            Assert.That(d.Sections[0], Is.EqualTo("Signal"), "the panel switches to the signal");
            w.Close();
        }

        [AvaloniaTest]
        public void ACard_Folds_AndStaysFolded()
        {
            MainWindow w = Open();
            w.PickFromList("net:LEV12_0");
            Controls.DetailsPanel d = w.FindControl<Controls.DetailsPanel>("Details")!;
            Assert.That(d.ToggleFold("Pins"), Is.True);
            w.PickFromList("net:LEV10_0");
            Control? table = Find(d, b => b.Classes.Contains("pin-row"));
            Assert.That(table!.IsEffectivelyVisible, Is.False, "the folded table is hidden");
            Assert.That(d.ToggleFold("Pins"), Is.False);
            w.Close();
        }

        private static MainWindow Open()
        {
            MainWindow w = new() { Width = 1500, Height = 1000 };
            w.Show();
            w.OpenSheet(Sheet2A50);
            return w;
        }

        private static Button? Find(Control c, System.Func<Button, bool> match)
        {
            if (c is Button b && match(b))
                return b;
            if (c is ContentControl cc && cc.Content is Control inner && Find(inner, match) is Button x)
                return x;
            if (c is Decorator dec && dec.Child != null && Find(dec.Child, match) is Button y)
                return y;
            if (c is Panel p)
            {
                for (int i = 0; i < p.Children.Count; i++)
                {
                    if (Find(p.Children[i], match) is Button z)
                        return z;
                }
            }
            return null;
        }

        // saves the window as a picture in a light and a dark theme, when asked to
        private static void Save(MainWindow w, string name)
        {
            string? folder = System.Environment.GetEnvironmentVariable("RETROSCHEMATIC_PANEL_PNG");
            if (string.IsNullOrEmpty(folder))
                return;
            Controls.SchematicCanvas canvas = w.FindControl<Controls.SchematicCanvas>("Canvas")!;
            canvas.TracePulse = false;
            string[] themes = { "Light", "Dark" };
            for (int i = 0; i < themes.Length; i++)
            {
                // CaptureRenderedFrame renders the frame itself. Pumping the dispatcher here first
                // (RunJobs) ran a render tick that sometimes never came back (see docs/BUGS.md B1).
                ThemeManager.Instance.Apply(themes[i], persist: false);
                using Avalonia.Media.Imaging.WriteableBitmap? frame = w.CaptureRenderedFrame();
                frame?.Save(Path.Combine(folder, name + "-" + themes[i].ToLowerInvariant() + ".png"));
            }
            ThemeManager.Instance.Apply("Light", persist: false);
            canvas.TracePulse = true;
        }
    }
}
