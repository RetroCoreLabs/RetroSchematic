//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using NUnit.Framework;
using RetroSchematic.Desktop.Controls;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The view switcher (Drawing, Original, PCB front, PCB back; F5-F8), the Board window, the
    /// Board card in Details when nothing is picked, and the switch blocks in the parts list.
    /// With RETROSCHEMATIC_VIEW_PNG set to a folder, each view is also saved as a picture in the
    /// Light and Dark themes (off in normal runs).
    /// </summary>
    [TestFixture]
    public class BoardViewTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
        // no card is stored zipped (owner rule 2026-11-28): the zip these tests need is made from
        // the text sheet at run time, by the app's own save, with the page and photos packed in
        private static string Packed2A50(string name)
        {
            string work = Path.Combine(TestContext.CurrentContext.WorkDirectory, "packed");
            Directory.CreateDirectory(work);
            string zip = Path.Combine(work, name + ".rsx");
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet2A50);
            w.Session!.SaveAs(zip);
            w.Close();
            return zip;
        }

        [AvaloniaTest]
        public void ViewSwitcher_ShowsEachView_WithItsOwnZoom_AndF5ToF8()
        {
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(Sheet2A50);
            Assert.That(w.CurrentView, Is.EqualTo(MainWindow.ViewDrawing));
            Assert.That(w.FindControl<ToggleButton>("TbViewDrawing")!.IsChecked, Is.True);
            Assert.That(w.FindControl<ToggleButton>("TbViewOriginal")!.IsEnabled, Is.True);

            // the pictures are loaded: the page is 3507 x 2496 (300 dpi), the photos 2500 x 2500
            Assert.That(w.Original.Bitmap, Is.Not.Null);
            Assert.That(w.Original.Bitmap!.PixelSize, Is.EqualTo(new PixelSize(3507, 2496)));
            Assert.That(w.Front.Bitmap!.PixelSize, Is.EqualTo(new PixelSize(2500, 2500)));
            Assert.That(w.Back.Bitmap, Is.Not.Null);
            Assert.That(w.Pages.ItemCount, Is.EqualTo(1));
            Assert.That(w.FindControl<StackPanel>("PagePickerBar")!.IsVisible, Is.False, "one page: no picker");

            w.KeyPress(Key.F6, RawInputModifiers.None, PhysicalKey.F6, null);
            Assert.That(w.CurrentView, Is.EqualTo(MainWindow.ViewOriginal));
            Assert.That(w.Original.IsVisible, Is.True);
            Assert.That(w.FindControl<SchematicCanvas>("Canvas")!.IsVisible, Is.False);
            BoardViewTests.Save(w, "view-original");
            // zoom the original; the front keeps its own zoom
            w.Original.Fit();
            double fitted = w.Original.Zoom;
            w.Original.ZoomCentre(2);
            Assert.That(w.Original.Zoom, Is.EqualTo(fitted * 2).Within(1e-9));

            w.KeyPress(Key.F7, RawInputModifiers.None, PhysicalKey.F7, null);
            Assert.That(w.CurrentView, Is.EqualTo(MainWindow.ViewFront));
            Assert.That(w.Front.IsVisible, Is.True);
            BoardViewTests.Save(w, "view-pcb-front");
            w.Front.Fit();
            Assert.That(w.Front.Zoom, Is.Not.EqualTo(w.Original.Zoom));

            w.KeyPress(Key.F8, RawInputModifiers.None, PhysicalKey.F8, null);
            Assert.That(w.CurrentView, Is.EqualTo(MainWindow.ViewBack));
            Assert.That(w.FindControl<ToggleButton>("TbViewBack")!.IsChecked, Is.True);
            BoardViewTests.Save(w, "view-pcb-back");

            w.ShowView(MainWindow.ViewOriginal);
            Assert.That(w.Original.Zoom, Is.EqualTo(fitted * 2).Within(1e-9), "the original kept its zoom");

            w.KeyPress(Key.F5, RawInputModifiers.None, PhysicalKey.F5, null);
            Assert.That(w.CurrentView, Is.EqualTo(MainWindow.ViewDrawing));
            Assert.That(w.FindControl<SchematicCanvas>("Canvas")!.IsVisible, Is.True);
            BoardViewTests.Save(w, "view-drawing");
            w.Close();
        }

        [AvaloniaTest]
        public void APackedSheet_ShowsThePicturesFromInsideTheZip()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Packed2A50("shows"));
            Assert.That(w.Session!.Editor.Files.Holds("photos/322049-component-side-I-old.jpg"), Is.True);
            Assert.That(w.Front.Bitmap!.PixelSize, Is.EqualTo(new PixelSize(2500, 2500)));
            Assert.That(w.Original.Bitmap!.PixelSize, Is.EqualTo(new PixelSize(3507, 2496)));
            w.Close();
        }

        /// <summary>
        /// 2A50 has the old and the new board on each side: the picker shows both captions on
        /// the PCB views (not on the drawing), and picking the new one shows another picture.
        /// </summary>
        [AvaloniaTest]
        public void PhotoPicker_ChoosesTheOldOrTheNewBoard()
        {
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(Sheet2A50);
            StackPanel bar = w.FindControl<StackPanel>("PhotoPickerBar")!;
            Assert.That(bar.IsVisible, Is.False, "the drawing has no photo picker");
            w.ShowView(MainWindow.ViewFront);
            Assert.That(bar.IsVisible, Is.True);
            Assert.That(w.PhotoChoice.ItemCount, Is.EqualTo(2));
            Assert.That(w.PhotoChoice.Items[0], Is.EqualTo("Component side, PCB version I, old"));
            Assert.That(w.PhotoChoice.Items[1], Is.EqualTo("Component side, PCB version I, new"));
            Avalonia.Media.Imaging.Bitmap old = w.Front.Bitmap!;
            w.PhotoChoice.SelectedIndex = 1;
            Assert.That(w.PhotoShown(BoardSide.Component), Is.EqualTo(1));
            Assert.That(w.Front.Bitmap, Is.Not.SameAs(old));
            BoardViewTests.Save(w, "view-pcb-front-new");
            w.ShowView(MainWindow.ViewBack);
            Assert.That(w.PhotoChoice.Items[1], Is.EqualTo("Solder side, PCB version I, new"));
            Assert.That(w.PhotoChoice.SelectedIndex, Is.EqualTo(0), "each side keeps its own choice");
            w.Close();
        }

        [AvaloniaTest]
        public void ChipBoxes_OnThePhotos_HoverNamesTheChip_ClickPicksIt()
        {
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(Sheet2A50);
            Assert.That(w.PcbProblem, Is.EqualTo(""));
            w.ShowView(MainWindow.ViewFront);
            Assert.That(w.FindControl<ToggleButton>("TbChipBoxes")!.IsEnabled, Is.True);
            w.SetChipBoxes(true);
            Assert.That(w.Front.ShowBoxes, Is.True);
            IReadOnlyList<PcbBox> boxes = w.ChipBoxes(BoardSide.Component);
            Assert.That(boxes.Count, Is.EqualTo(42), "40 parts and 2 switch blocks");
            Assert.That(w.CornersFrom(BoardSide.Component), Is.EqualTo("found"), "the board is found in the old component photo");
            // the middle of 13D's box: hover names it, a click picks it as on the drawing
            PcbBox d13 = Find(boxes, "13D");
            GridPoint mid = Middle(d13.Corners);
            Assert.That(w.HoverPcb(BoardSide.Component, mid), Does.StartWith("13D  7442"));
            Assert.That(w.Front.HotRef, Is.EqualTo("13D"));
            w.Front.ClickAt(mid);
            Assert.That(w.Session!.Selected, Is.EqualTo("part:13D"));
            Assert.That(w.Front.PickedRef, Is.EqualTo("13D"));
            BoardViewTests.Save(w, "chips-front-old");
            // the solder side is the mirror: 13D lies on the other half of its photo
            PcbBox back13 = Find(w.ChipBoxes(BoardSide.Solder), "13D");
            Assert.That(Middle(back13.Corners).X, Is.LessThan(w.Back.Bitmap!.PixelSize.Width / 2.0));
            Assert.That(mid.X, Is.GreaterThan(w.Front.Bitmap!.PixelSize.Width / 2.0));
            // the new photo
            w.PhotoChoice.SelectedIndex = 1;
            Assert.That(w.ChipBoxes(BoardSide.Component).Count, Is.EqualTo(42));
            BoardViewTests.Save(w, "chips-front-new");
            w.ShowView(MainWindow.ViewBack);
            BoardViewTests.Save(w, "chips-back-old");
            w.SetChipBoxes(false);
            Assert.That(w.Front.ShowBoxes, Is.False);
            w.Close();
        }

        private static PcbBox Find(IReadOnlyList<PcbBox> boxes, string reference)
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i].Ref == reference)
                    return boxes[i];
            }
            throw new AssertionException("no box " + reference);
        }

        private static GridPoint Middle(GridPoint[] c)
        {
            double x = 0, y = 0;
            for (int i = 0; i < c.Length; i++)
            {
                x += c[i].X;
                y += c[i].Y;
            }
            return new GridPoint(x / c.Length, y / c.Length);
        }

        [AvaloniaTest]
        public void NothingPicked_ShowsTheBoardCard()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet2A50);
            DetailsPanel d = w.FindControl<DetailsPanel>("Details")!;
            Assert.That(d.Sections, Does.Contain("Board"));
            string text = d.AllText();
            Assert.That(text, Does.Contain("322049").And.Contain("NORD-10").And.Contain("156 x 149 mm").And.Contain("15E"));
            Assert.That(text, Does.Contain("Synchronous Modem Buffer").And.Contain("1050").And.Contain("nd-322049.html"));
            w.Close();
        }

        [AvaloniaTest]
        public void ASwitch_IsPickedFromThePartsList_AndEditedInEditMode()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet2A50);
            w.PickSwitch("15E");
            DetailsPanel d = w.FindControl<DetailsPanel>("Details")!;
            Assert.That(d.Sections, Does.Contain("Switch"));
            Assert.That(d.AllText(), Does.Contain("SMN3").And.Contain("BA9").And.Contain("gives 1 (inferred)"));
            Assert.That(w.SwitchEditor.IsVisible, Is.False, "read-only: no editor");
            ListBox parts = w.FindControl<ListBox>("PartsList")!;
            Assert.That(parts.SelectedIndex, Is.GreaterThanOrEqualTo(0));

            w.SetEditMode(true);
            w.PickSwitch("15E");
            Assert.That(w.SwitchEditor.IsVisible, Is.True);
            List<ComboBox> settings = new();
            Collect(w.SwitchEditor, settings);
            // the first combo box is "on means", then one per lever
            Assert.That(settings, Has.Count.EqualTo(5));
            settings[1].SelectedItem = "off";
            w.SaveSwitch();
            Assert.That(w.Session!.Editor.FindSwitch("15E")!.Levers[0].Setting, Is.EqualTo("off"));
            Assert.That(w.Session.Editor.FindSwitch("15E")!.Levers[0].Confidence, Is.EqualTo(Confidence.Read), "the reading's confidence is kept");
            Assert.That(w.Session.Editor.Modified, Is.True);
            w.Close();
        }

        [AvaloniaTest]
        public void TheBoardWindow_EditsFacts_Connectors_AndImportsPagesAndPhotos()
        {
            string work = Path.Combine(TestContext.CurrentContext.WorkDirectory, "boardwindow");
            Directory.CreateDirectory(work);
            string sheet = Path.Combine(work, "copy.rsx");
            File.Copy(Packed2A50("board"), sheet, overwrite: true);
            MainWindow w = new();
            w.Show();
            w.OpenSheet(sheet);
            BoardWindow b = w.OpenBoardWindow()!;
            Assert.That(b.Thumbnails, Has.Count.EqualTo(5), "one page, four photos (old and new board, both sides)");
            Assert.That(b.Thumbnails.TrueForAll(t => t != null), Is.True, "every thumbnail could be read");
            Assert.That(b.Fields.Number.Text, Is.EqualTo("322049"));
            SaveWindow(b, "board-window");

            // read-only: refused, and the window says so
            b.Fields.Layers.Text = "4";
            Assert.That(b.SaveBoard(), Is.False);
            Assert.That(b.Message, Does.Contain("read-only").IgnoreCase.Or.Contain("edit mode").IgnoreCase);

            w.SetEditMode(true);
            b.Refresh();
            b.Fields.Layers.Text = "4";
            Assert.That(b.SaveBoard(), Is.True, b.Message);
            Assert.That(w.Session!.Editor.Root.Board!.Specifications!.Layers, Is.EqualTo(4));
            b.Fields.Width.Text = "wide";
            Assert.That(b.SaveBoard(), Is.False);

            // a connector marked not used: 2A50 has one, the 0-99 connector P (the modem lines
            // are its pins 81-95 since 2026-11-23, so there is no separate front connector)
            Assert.That(b.ConnectorRows, Has.Count.EqualTo(1));
            BoardWindow.ConnectorRow row = b.ConnectorRows[0];
            row.Used.IsChecked = false;
            Assert.That(b.SaveConnector(row), Is.True, b.Message);
            Assert.That(w.Session.Editor.Root.Connectors![0].Used, Is.False);

            // import a photo and a PDF page into the .rsx: both held inside
            string photo = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-files", "photos", "322049-solder-side-I-old.jpg");
            Assert.That(b.ImportPhoto(photo, BoardSide.Solder), Is.True, b.Message);
            Assert.That(w.Session.Editor.Root.Photos, Has.Count.EqualTo(5));
            Assert.That(b.RemovePhoto(4), Is.True);
            Assert.That(BoardWindow.ParsePages("63-65, 70", 252, out List<int> pages), Is.True);
            Assert.That(pages, Is.EqualTo(new[] { 63, 64, 65, 70 }));
            Assert.That(BoardWindow.ParsePages("300", 252, out _), Is.False);
            Assert.That(b.ImportSource(photo, new int[0]), Is.True, "a picture is one page");
            Assert.That(w.Session.Editor.Root.Sources, Has.Count.EqualTo(2));
            Assert.That(w.Pages.ItemCount, Is.EqualTo(2), "the page picker follows");
            Assert.That(b.RemoveSource(1), Is.True);
            b.Close();
            w.Close();
            Directory.Delete(work, recursive: true);
        }

        private static void Collect(Control c, List<ComboBox> into)
        {
            if (c is ComboBox cb)
                into.Add(cb);
            if (c is Panel p)
            {
                for (int i = 0; i < p.Children.Count; i++)
                    Collect(p.Children[i], into);
            }
        }

        // the window in Light and Dark, when RETROSCHEMATIC_VIEW_PNG names a folder
        internal static void Save(Window w, string name)
        {
            string? folder = System.Environment.GetEnvironmentVariable("RETROSCHEMATIC_VIEW_PNG");
            if (string.IsNullOrEmpty(folder))
                return;
            SchematicCanvas? canvas = w is MainWindow ? w.FindControl<SchematicCanvas>("Canvas") : null;
            if (canvas != null)
                canvas.TracePulse = false;
            // the view switch must be laid out before the picture is taken, or the last frame
            // (the view before) is saved; the trace pulse is off, so this ends (docs/BUGS.md B1)
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            // the first capture after a change gives the frame drawn before it (measured: the
            // Light picture of each view showed the view before); that one is thrown away
            using (w.CaptureRenderedFrame())
            {
            }
            string[] themes = { "Light", "Dark" };
            for (int i = 0; i < themes.Length; i++)
            {
                ThemeManager.Instance.Apply(themes[i], persist: false);
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                using Avalonia.Media.Imaging.WriteableBitmap? frame = w.CaptureRenderedFrame();
                frame?.Save(Path.Combine(folder, name + "-" + themes[i].ToLowerInvariant() + ".png"));
            }
            ThemeManager.Instance.Apply("Light", persist: false);
            if (canvas != null)
                canvas.TracePulse = true;
        }

        private static void SaveWindow(Window w, string name) => Save(w, name);
    }
}
