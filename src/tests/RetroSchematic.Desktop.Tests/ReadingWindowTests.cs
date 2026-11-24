//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Recognition;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// Assisted reading on the Original view: Analyse page draws the finds, a picked find is
    /// accepted as the part with its code (or linked to one), the accepted box replaces the find
    /// and is saved as a symbol box, wire assist puts a pin of the part at a wire end on a signal,
    /// and undo takes each step back.
    /// </summary>
    [TestFixture]
    public class ReadingWindowTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private static int FindWithCode(PageAnalysis a, string kind, string code)
        {
            for (int i = 0; i < a.Finds.Count; i++)
            {
                if (a.Finds[i].Kind == kind && a.Finds[i].Code == code)
                    return a.Finds[i].Id;
            }
            return -1;
        }

        [AvaloniaTest]
        public void AnalyseAcceptAndWireAssist()
        {
            MainWindow w = new() { Width = 1400, Height = 900 };
            w.Show();
            w.OpenSheet(Sheet2A50);
            w.KeyPress(Key.F6, RawInputModifiers.None, PhysicalKey.F6, null);
            Assert.That(w.FindControl<StackPanel>("ReadBar")!.IsVisible, Is.True);

            w.AnalyseShownPage();
            PageAnalysis a = w.Analysis!;
            Assert.That(a.Finds.Count, Is.GreaterThan(90));
            Assert.That(w.Original.Finds!.Count, Is.EqualTo(a.Finds.Count), "every find drawn");
            Assert.That(w.Original.Boxes, Is.Empty, "no accepted box yet");
            BoardViewTests.Save(w, "reading-finds");

            int id = FindWithCode(a, "chip", "15B");
            Assert.That(id, Is.GreaterThan(0));
            w.PickFind(id);
            Assert.That(w.FindControl<StackPanel>("FindBar")!.IsVisible, Is.True);
            Assert.That(w.FindControl<TextBox>("FindCode")!.Text, Is.EqualTo("15B"));
            Assert.That(w.FindControl<TextBox>("FindType")!.Text, Is.EqualTo("74157"));

            // read-only: refused, nothing stored
            Assert.That(w.AcceptPickedFind(), Is.Null);
            Assert.That(w.Session!.Editor.Root.SymbolBoxes, Is.Null);

            w.SetEditMode(true);
            w.PickFind(id);
            Assert.That(w.AcceptPickedFind(), Is.EqualTo("15B"));
            SymbolBox b = w.Session!.Editor.Root.SymbolBoxes![0];
            Assert.That((b.Ref, b.By, b.Kind, b.Source), Is.EqualTo(("15B", "tool", "chip", 1)));
            Assert.That(w.Original.Boxes!, Has.Count.EqualTo(1));
            Assert.That(w.Original.Finds!.Count, Is.EqualTo(a.Finds.Count - 1), "the accepted find is drawn as its box");

            // a gate find with a code nobody has: change the code to a free one and make a 7400
            PageFind g = a.Finds[0];
            for (int i = a.Finds.Count - 1; i >= 0; i--)
            {
                if (a.Finds[i].Kind == "gate")
                    g = a.Finds[i];
            }
            w.PickFind(g.Id);
            w.FindControl<TextBox>("FindCode")!.Text = "19E";
            w.FindControl<TextBox>("FindType")!.Text = "7400";
            Assert.That(w.AcceptPickedFind(), Is.EqualTo("19E"));
            Assert.That(w.Session.Editor.FindPart("19E")!.Confidence, Is.EqualTo(Confidence.Inferred));

            // wire assist: a wire end just above the 74157's box, pin 16, onto 13D.16's signal
            w.FindControl<CheckBox>("WireAssist")!.IsChecked = true;
            Assert.That(w.PickWireEnd(2260, 1070), Is.EqualTo("15B"));
            Assert.That(w.FindControl<StackPanel>("PinBar")!.IsVisible, Is.True);
            ComboBox pins = w.FindControl<ComboBox>("PinPicker")!;
            Assert.That(pins.ItemCount, Is.EqualTo(16));
            pins.SelectedIndex = 15;
            Assert.That((string)pins.SelectedItem!, Does.StartWith("16  "));
            w.FindControl<TextBox>("PinTarget")!.Text = "13D.16";
            string? signal = w.ConnectPickedPin();
            Assert.That(signal, Is.Not.Null);
            Assert.That(w.Session.Editor.NetOf("15B.16")!.Name, Is.EqualTo(signal));
            Assert.That(w.FindControl<TextBox>("PinTarget")!.Text, Is.EqualTo(signal), "the next pin joins the same signal");
            Assert.That(w.PickWireEnd(5, 2400), Is.Null, "no box there");
            BoardViewTests.Save(w, "reading-accepted");

            // undo: the connection, then the new part, then the 15B box
            w.Session.Editor.Undo();
            Assert.That(w.Session.Editor.NetOf("15B.16"), Is.Null);
            w.Session.Editor.Undo();
            Assert.That(w.Session.Editor.FindPart("19E"), Is.Null);
            w.Session.Editor.Undo();
            Assert.That(w.Session.Editor.Root.SymbolBoxes, Is.Null);
        }
    }
}
