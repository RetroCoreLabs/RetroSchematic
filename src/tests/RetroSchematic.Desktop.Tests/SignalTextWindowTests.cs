//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// Signal text in the window: the hover text, the side panel, and editing the description.
    /// </summary>
    [TestFixture]
    public class SignalTextWindowTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");

        [AvaloniaTest]
        public void HoveringASignal_ShowsItsNameDescriptionAndDriver()
        {
            MainWindow w = Open();
            Controls.SchematicCanvas canvas = w.FindControl<Controls.SchematicCanvas>("Canvas")!;
            canvas.ShowHover("pin:P.11");
            Assert.That(canvas.HoverText, Does.StartWith("/IOXE\nIOX enable"));
            Assert.That(ToolTip.GetIsOpen(canvas), Is.True);
            canvas.ShowHover("");
            Assert.That(canvas.HoverText, Is.Null);
            Assert.That(ToolTip.GetIsOpen(canvas), Is.False);
            w.Close();
        }

        [AvaloniaTest]
        public void PickingASignal_ShowsEverythingInTheSidePanel()
        {
            MainWindow w = Open();
            w.PickFromList("net:LEV12_0");
            string text = w.FindControl<SelectableTextBlock>("DetailsText")!.Text!;
            Assert.That(text, Does.Contain("What it does: Level-12 interrupt"));
            Assert.That(text, Does.Contain("Source: reading document docs/2A50-reading.md, Interrupts (read)"));
            Assert.That(text, Does.Contain("Pins:"));
            Assert.That(w.FindControl<StackPanel>("SignalEditPanel")!.IsVisible, Is.False, "read-only: nothing to edit");
            w.Close();
        }

        [AvaloniaTest]
        public void InEditMode_TheDescriptionCanBeTyped_SavedAndUndone()
        {
            MainWindow w = Open();
            w.SetEditMode(true);
            w.PickFromList("net:IOXE_0");
            Assert.That(w.FindControl<StackPanel>("SignalEditPanel")!.IsVisible, Is.True);
            w.FindControl<TextBox>("DescriptionBox")!.Text = "Starts an IOX transfer.";
            w.FindControl<TextBox>("SourceDocumentBox")!.Text = "manual X";
            w.SaveSignalText();
            Assert.That(w.Session!.Editor.FindNetByKey("IOXE_0")!.Description, Is.EqualTo("Starts an IOX transfer."));
            Assert.That(w.Session.Editor.Modified, Is.True);
            Assert.That(w.Title, Does.Contain("(changed)"));
            w.Session.Editor.Undo();
            Assert.That(w.Session.Editor.FindNetByKey("IOXE_0")!.Description, Is.Null);
            w.Close();
        }

        private static MainWindow Open()
        {
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(Sheet2A50);
            return w;
        }
    }
}
