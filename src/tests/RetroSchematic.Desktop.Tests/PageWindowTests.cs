//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// A card with pages in the window: the page tabs, Page Up / Page Down, the card overview,
    /// and the Original view following the drawn page (or not, when picked by hand).
    /// </summary>
    [TestFixture]
    public class PageWindowTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private string _card = "";

        // 2A50 with a second page holding the address decoder, read from a second source page
        // (the same picture again, so the Original view has two pages to choose between). Saved
        // in the test's own work folder, never in the sheets folder: a copy left there by a
        // stopped run is read as a second 2A50 by the card library and Bus use. The save
        // rewrites the picture links for the new folder. Deleted after the test.
        [SetUp]
        public void SetUp()
        {
            string work = Path.Combine(TestContext.CurrentContext.WorkDirectory, "pagetest");
            Directory.CreateDirectory(work);
            _card = Path.Combine(work, "pagetest-" + TestContext.CurrentContext.Test.Name + ".rsc");
            SheetEditor e = new(NetlistReader.ReadFile(Sheet2A50), null) { ReadOnly = false, Files = SheetAttachments.Load(Sheet2A50) };
            e.Root.Sources!.Add(new SourcePage { File = e.Root.Sources[0].File, Title = "Second page (the same scan)" });
            e.AddPage("Address decoder", 2, "2A50/2");
            e.SetPartPage("13D", "2");
            e.SetPartPage("13E", "2");
            e.Save(_card);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_card))
                File.Delete(_card);
        }

        [AvaloniaTest]
        public void PageTabs_PageKeys_AndTheOriginalViewFollowing()
        {
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(_card);
            SheetSession s = w.Session!;
            Assert.That(w.PageTabStrip.IsVisible, Is.True);
            Assert.That(w.PageTabStrip.Children.Count, Is.EqualTo(3), "the card tab and two pages");
            Assert.That(w.OriginalPageShown, Is.EqualTo(0), "page 1 is read from source 1");

            // a click on the page 2 tab draws page 2, and the Original view follows it
            // the tabs are laid out before they can be clicked
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            ToggleButton tab2 = (ToggleButton)w.PageTabStrip.Children[2];
            Click(w, tab2);
            Assert.That(s.PageIndex, Is.EqualTo(1));
            Assert.That(s.Drawing.Frames.ContainsKey("13D"), Is.True);
            Assert.That(s.Drawing.Frames.ContainsKey("11D"), Is.False);
            Assert.That(w.OriginalPageShown, Is.EqualTo(1), "page 2 is read from source 2");
            Assert.That(((ToggleButton)w.PageTabStrip.Children[2]).IsChecked, Is.True);
            BoardViewTests.Save(w, "pages-drawing-page2");

            // Page Up goes back to page 1, Page Down forward again
            w.KeyPress(Key.PageUp, RawInputModifiers.None, PhysicalKey.PageUp, null);
            Assert.That(s.PageIndex, Is.EqualTo(0));
            Assert.That(w.OriginalPageShown, Is.EqualTo(0));
            w.KeyPress(Key.PageDown, RawInputModifiers.None, PhysicalKey.PageDown, null);
            Assert.That(s.PageIndex, Is.EqualTo(1));

            // Back goes to page 1 again
            w.GoBack();
            Assert.That(s.PageIndex, Is.EqualTo(0));

            // picked by hand: the Original view stays where it was put
            w.FindControl<CheckBox>("PageByHand")!.IsChecked = true;
            w.Pages.SelectedIndex = 1;
            s.SetPage(1);
            s.SetPage(0);
            Assert.That(w.OriginalPageShown, Is.EqualTo(1), "picked by hand, not followed");
            w.Close();
        }

        [AvaloniaTest]
        public void TheOverview_ListsThePages_AndThePhotos_AndAPageButtonDrawsIt()
        {
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(_card);
            w.ShowView(MainWindow.ViewOverview);
            Assert.That(w.CurrentView, Is.EqualTo(MainWindow.ViewOverview));
            int buttons = 0, images = 0;
            Button? page2 = null;
            for (int i = 0; i < w.Overview.Children.Count; i++)
            {
                if (w.Overview.Children[i] is Button b)
                {
                    buttons++;
                    if (b.Content is string t && t.StartsWith("2.", System.StringComparison.Ordinal))
                        page2 = b;
                }
                if (w.Overview.Children[i] is WrapPanel photos)
                {
                    for (int k = 0; k < photos.Children.Count; k++)
                    {
                        if (photos.Children[k] is StackPanel cell && cell.Children[0] is Image)
                            images++;
                    }
                }
            }
            Assert.That(buttons, Is.EqualTo(2), "one button per page");
            Assert.That(images, Is.EqualTo(4), "2A50's four board photos");
            Assert.That(page2, Is.Not.Null);
            Assert.That((string)page2!.Content!, Does.Contain("Address decoder").And.Contain("2 chips"));
            BoardViewTests.Save(w, "pages-overview");
            page2.Command?.Execute(null);
            page2.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.That(w.CurrentView, Is.EqualTo(MainWindow.ViewDrawing));
            Assert.That(w.Session!.PageIndex, Is.EqualTo(1));
            w.Close();
        }

        private static void Click(Window w, Control c)
        {
            Avalonia.Point centre = c.TranslatePoint(new Avalonia.Point(c.Bounds.Width / 2, c.Bounds.Height / 2), w)!.Value;
            w.MouseDown(centre, MouseButton.Left);
            w.MouseUp(centre, MouseButton.Left);
        }
    }
}
