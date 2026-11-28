//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using RetroSchematic.Model;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The Bus use window: every card's backplane signals, the filter by bus, and a click on a
    /// chip that opens its card, goes into its block and picks it.
    /// </summary>
    [TestFixture]
    public class BusUseWindowTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [AvaloniaTest]
        public void TheWindow_ListsEveryCard_FiltersByBus_AndAClickShowsTheChipInsideItsBlock()
        {
            MainWindow w = new();
            w.Show();
            w.OpenSheet(Sheet2A50);
            BusUseWindow b = w.OpenBusUseWindow();
            bool nd100 = false, nord10 = false;
            for (int i = 0; i < b.All.Count; i++)
            {
                nd100 |= b.All[i].Card == "3207";
                nord10 |= b.All[i].Card == "2A50";
            }
            Assert.That(nd100 && nord10, Is.True, "both cards are read: " + b.Status);
            Assert.That(b.Status, Does.Contain("Card 2A50 is in two files"), "the 2A50 block demo is the same card");

            b.PickBus(1);
            int breq = -1;
            for (int i = 0; i < b.Shown.Count; i++)
            {
                Assert.That(b.Shown[i].Bus, Is.EqualTo(MachineBus.Nd100));
                if (b.Shown[i].Signal == "BREQ~")
                    breq = i;
            }
            Assert.That(breq, Is.GreaterThanOrEqualTo(0));
            int chip = -1;
            for (int k = 0; k < b.Shown[breq].Chips.Count; k++)
            {
                if (b.Shown[breq].Chips[k].Part == "9C")
                    chip = k;
            }
            Assert.That(chip, Is.GreaterThanOrEqualTo(0));

            // the click opens card 3207 in place of 2A50, goes down three block levels and picks 9C
            b.Pick(breq, chip);
            b.GoToChip();
            Assert.That(w.Session!.Editor.Root.Sheet.Id, Is.EqualTo("3207"));
            Assert.That(w.Session.LevelNames().Count, Is.EqualTo(4));
            Assert.That(w.Session.Selected, Is.EqualTo("part:9C"));
            b.Close();
            w.Close();
        }
    }
}
