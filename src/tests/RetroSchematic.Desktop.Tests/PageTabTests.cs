//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using NUnit.Framework;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The page tabs of a card with pages: named by what each page is (card 3207: Backplane A,
    /// C, B), shown on the drawing and hidden on the PCB photos; and the toolbar label that sets
    /// the photo-mark buttons apart from the view buttons.
    /// </summary>
    [TestFixture]
    public class PageTabTests
    {
        private static readonly string Sheet3207 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nd-100", "3207-scsi-floppy.rsc");

        [AvaloniaTest]
        public void Card3207_TabsAreTheBackplanes_AndHiddenOnThePhotos()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet3207);
            w.ShowView(MainWindow.ViewDrawing);
            Assert.That(w.PageTabStrip.IsVisible, Is.True);
            string tabs = "";
            for (int i = 0; i < w.PageTabStrip.Children.Count; i++)
            {
                if (w.PageTabStrip.Children[i] is ToggleButton t)
                    tabs += t.Content + "|";
            }
            Assert.That(tabs, Does.Contain("Backplane A").And.Contain("Backplane B").And.Contain("Backplane C"));

            w.ShowView(MainWindow.ViewFront);
            Assert.That(w.PageTabStrip.IsVisible, Is.False, "no drawing pages on the component side photo");
            w.ShowView(MainWindow.ViewBack);
            Assert.That(w.PageTabStrip.IsVisible, Is.False, "no drawing pages on the solder side photo");
            w.ShowView(MainWindow.ViewDrawing);
            Assert.That(w.PageTabStrip.IsVisible, Is.True);

            Assert.That(w.FindControl<TextBlock>("PhotoMarksLabel")!.Text, Is.EqualTo("On photos:"));
            w.Close();
        }
    }
}
