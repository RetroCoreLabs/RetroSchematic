//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The mouse and key handling the window forwards: what a press, a drag and Delete do in
    /// read-only and in edit mode, and the side-panel, search and lighting logic of the session.
    /// </summary>
    [TestFixture]
    public class EditToolTests
    {
        [Test]
        public void ReadOnly_APressOnlyPicks_EvenWithAChipChosen()
        {
            SheetSession s = Session();
            EditTool tool = new(s) { PlaceType = "7400" };
            DrawPoint p = s.Drawing.Pins["1A.9"];
            tool.PointerDown(p.X, p.Y);
            tool.PointerUp(p.X + 100, p.Y);
            Assert.That(s.Selected, Is.EqualTo("pin:1A.9"));
            Assert.That(s.Netlist.Parts.Count, Is.EqualTo(5), "nothing placed in read-only mode");
            Assert.That(tool.Gesture, Is.EqualTo(EditGesture.None));
        }

        [Test]
        public void EditMode_DragFromPinToPin_Connects()
        {
            SheetSession s = Session();
            s.Editor.ReadOnly = false;
            EditTool tool = new(s);
            DrawPoint a = s.Drawing.Pins["1A.9"], b = s.Drawing.Pins["1A.10"];
            tool.PointerDown(a.X, a.Y);
            Assert.That(tool.Gesture, Is.EqualTo(EditGesture.Wire));
            tool.PointerMove(b.X, b.Y);
            Assert.That(tool.Current.X, Is.EqualTo(b.X));
            tool.PointerUp(b.X, b.Y);
            Net? net = s.Editor.NetOf("1A.9");
            Assert.That(net, Is.Not.Null);
            Assert.That(s.Editor.NetOf("1A.10"), Is.SameAs(net));
            Assert.That(s.Selected, Is.EqualTo("net:" + net!.Name));
            Assert.That(DemoSheetLayoutTests.Wires(s.Drawing, "net:" + net.Name).Count, Is.GreaterThan(0), "the drawing was laid out again with the new wire");
        }

        [Test]
        public void EditMode_ARefusedWire_SaysWhy_AndChangesNothing()
        {
            SheetSession s = Session();
            s.Editor.ReadOnly = false;
            EditTool tool = new(s);
            DrawPoint a = s.Drawing.Pins["1A.1"], b = s.Drawing.Pins["1A.2"];
            tool.PointerDown(a.X, a.Y);
            tool.PointerUp(b.X, b.Y);
            Assert.That(tool.Message, Does.Contain("one signal only"));
            Assert.That(s.Editor.CanUndo, Is.False);
        }

        [Test]
        public void EditMode_DragAChip_MovesIt_AndAPressWithAChipChosen_PlacesOne()
        {
            SheetSession s = Session();
            s.Editor.ReadOnly = false;
            EditTool tool = new(s);
            PartFrame f = s.Drawing.Frames["3A"];
            // on the part label, written just above the frame
            tool.PointerDown(f.X + 4, f.Y - 8);
            Assert.That(s.Selected, Is.EqualTo("part:3A"));
            Assert.That(tool.Gesture, Is.EqualTo(EditGesture.Move));
            tool.PointerUp(f.X + 46, f.Y - 8);
            Assert.That(s.Drawing.Frames["3A"].X, Is.EqualTo(f.X + 42), "moved by the drag, in whole millimetres (one grid step each)");

            tool.PlaceType = "7402";
            tool.PointerDown(SheetLayout.FromMm(10), SheetLayout.FromMm(120));
            Assert.That(s.Netlist.Parts.Count, Is.EqualTo(6));
            Assert.That(s.Selected, Is.EqualTo("part:6A"));
            Assert.That(s.Drawing.Frames["6A"].Y, Is.EqualTo(SheetLayout.FromMm(120)));
            tool.Cancel();
            Assert.That(tool.PlaceType, Is.Null);
        }

        [Test]
        public void Delete_RemovesThePick_AndUndoBringsItBack()
        {
            SheetSession s = Session();
            s.Editor.ReadOnly = false;
            EditTool tool = new(s);
            s.Select("part:4A");
            tool.DeleteSelected();
            Assert.That(s.Editor.FindPart("4A"), Is.Null);
            Assert.That(s.Selected, Is.Empty, "the deleted part is no longer picked");
            tool.Undo();
            Assert.That(s.Editor.FindPart("4A"), Is.Not.Null);
            s.Select("gate:1A/1");
            tool.DeleteSelected();
            Assert.That(tool.Message, Does.Contain("pick the chip"));
            s.Select("pin:3A.2");
            tool.DeleteSelected();
            Assert.That(s.Editor.NetOf("3A.2"), Is.Null);
        }

        [Test]
        public void PickingASignal_LightsItsPinDots_AndOnlyThose()
        {
            SheetSession s = Session();
            s.Select("net:DATA");
            Assert.That(s.IsLit(new DrawItem { Owner = "pin:3A.2" }), Is.True);
            Assert.That(s.IsLit(new DrawItem { Owner = "pin:4A.2" }), Is.True);
            Assert.That(s.IsLit(new DrawItem { Owner = "pin:1A.1" }), Is.False);
        }

        [Test]
        public void PartDetails_ListEveryPinWithItsSignal_AndTheSource()
        {
            SheetSession s = Session();
            s.Select("part:3A");
            string text = s.Describe();
            Assert.That(text, Does.Contain("Source: sheet DEMO"));
            Assert.That(text, Does.Contain("Pins:"));
            Assert.That(text, Does.Contain(" 1 1A     NAB"));
            Assert.That(text, Does.Contain(" 2 1Y     DATA"));
            Assert.That(text, Does.Contain(" 3 2A     -"), "a free pin shows no signal");
        }

        [Test]
        public void Search_FindsPartsSignalsAndPins()
        {
            SheetSession s = Session();
            Assert.That(s.Search("7474"), Is.EqualTo(new[] { "part:4A" }));
            Assert.That(s.Search("irq"), Is.EqualTo(new[] { "net:IRQ" }));
            Assert.That(s.Search("5A.3"), Is.EqualTo(new[] { "pin:5A.3" }));
            Assert.That(s.Search("  "), Is.Empty);
        }

        [Test]
        public void ShowOptions_LayOutAgain()
        {
            SheetSession s = Session();
            int calls = 0;
            s.DrawingChanged += _ => calls++;
            s.SetShowPinNumbers(false);
            s.SetShowBoardPositions(false);
            s.SetShowBoardPositions(false);
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(s.ShowPinNumbers, Is.False);
        }

        private static SheetSession Session() => new(DemoSheetLayoutTests.Demo, SymbolStyle.NorskData, DemoSheetLayoutTests.Catalog);
    }
}
