//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Model;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// File, Card library (every card file listed by machine family, read from the files) and
    /// File, Recent sheets.
    /// </summary>
    [TestFixture]
    public class CardLibraryWindowTests
    {
        private static readonly string Demo = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "demo", "demo-gates.rsc");
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [AvaloniaTest]
        public void TheLibrary_ListsTheCardsByFamily_FromTheFiles_AndOpensOne()
        {
            MainWindow w = new();
            w.Show();
            CardLibraryWindow lib = w.OpenCardLibrary();
            Assert.That(lib.Families, Does.Contain("NORD-10").And.Contain("ND-100"), lib.Status);

            // the packed .rsx file is listed too
            bool zip = false;
            for (int i = 0; i < lib.All.Count; i++)
                zip |= SheetFile.IsZipPath(lib.All[i].File);
            Assert.That(zip, Is.True);

            lib.PickFamily("ND-100");
            Assert.That(lib.Shown.Count, Is.EqualTo(1));
            Assert.That(lib.Shown[0].Id, Is.EqualTo("3207"));
            Assert.That(lib.Shown[0].Pages, Is.GreaterThan(1));

            lib.PickFamily("NORD-10");
            for (int i = 0; i < lib.Shown.Count; i++)
                Assert.That(lib.Shown[i].Id, Is.EqualTo("2A50"));

            lib.PickFamily("ND-100");
            lib.Pick(0);
            lib.OpenPicked();
            Assert.That(w.Session!.Editor.Root.Sheet.Id, Is.EqualTo("3207"));
            lib.Close();
            w.Close();
        }

        [Test]
        public void ANewFamily_IsFoundFromTheFiles_WithNothingInCode()
        {
            string work = Path.Combine(TestContext.CurrentContext.WorkDirectory, "library-nd500");
            if (Directory.Exists(work))
                Directory.Delete(work, true);
            Directory.CreateDirectory(Path.Combine(work, "nd-500"));
            string text = File.ReadAllText(Demo).Replace("\"machine\": \"none\"", "\"machine\": \"ND-500\"");
            Assert.That(text, Does.Contain("ND-500"), "the demo sheet's machine field was replaced");
            File.WriteAllText(Path.Combine(work, "nd-500", "card.rsc"), text);
            File.WriteAllText(Path.Combine(work, "broken.rsc"), "{ not json");

            List<string> problems = new();
            List<CardEntry> cards = CardLibrary.Scan(work, problems);
            Assert.That(cards.Count, Is.EqualTo(1));
            Assert.That(CardLibrary.Families(cards), Is.EqualTo(new[] { "ND-500" }));
            Assert.That(problems.Count, Is.EqualTo(1));
            Assert.That(problems[0], Does.StartWith("broken.rsc"));
        }

        [AvaloniaTest]
        public void RecentSheets_NewestFirst_NoDoubles_AndTheMenuOpensOne()
        {
            UiSettings.Instance.RecentSheets.Clear();
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Demo);
            w.OpenSheet(Sheet2A50);
            w.OpenSheet(Demo);
            List<string> recent = UiSettings.Instance.RecentSheets;
            Assert.That(recent.Count, Is.EqualTo(2));
            Assert.That(recent[0], Is.EqualTo(Path.GetFullPath(Demo)));
            Assert.That(recent[1], Is.EqualTo(Path.GetFullPath(Sheet2A50)));
            Assert.That(w.RecentMenu.Items.Count, Is.EqualTo(2));

            ((MenuItem)w.RecentMenu.Items[1]!).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
            Assert.That(w.Session!.Editor.Root.Sheet.Id, Is.EqualTo("2A50"));
            Assert.That(UiSettings.Instance.RecentSheets[0], Is.EqualTo(Path.GetFullPath(Sheet2A50)));

            for (int i = 0; i < UiSettings.RecentMax + 3; i++)
                UiSettings.Instance.AddRecent(Path.Combine(TestContext.CurrentContext.WorkDirectory, "x" + i + ".rsc"));
            Assert.That(UiSettings.Instance.RecentSheets.Count, Is.EqualTo(UiSettings.RecentMax));
            w.FillRecent();
            Assert.That(((MenuItem)w.RecentMenu.Items[0]!).IsEnabled, Is.False, "a file that is gone is greyed out");
            UiSettings.Instance.RecentSheets.Clear();
            w.Close();
        }
    }
}
