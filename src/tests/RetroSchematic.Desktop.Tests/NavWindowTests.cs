//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using NUnit.Framework;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// Back / Forward in the window: the buttons, Alt+Left / Alt+Right, and the mouse's side
    /// buttons, each returning to the earlier pick and its view.
    /// </summary>
    [TestFixture]
    public class NavWindowTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        [AvaloniaTest]
        public void BackAndForward_ByButton_ReturnToThePickAndItsView()
        {
            MainWindow w = Open();
            Button back = w.FindControl<Button>("TbBack")!;
            Button forward = w.FindControl<Button>("TbForward")!;
            Assert.That(back.IsEnabled, Is.False);
            w.PickFromList("part:11D");
            Controls.SchematicCanvas canvas = w.FindControl<Controls.SchematicCanvas>("Canvas")!;
            Point chipView = canvas.ViewCentre;
            w.PickFromList("net:LEV12_0");
            Assert.That(back.IsEnabled, Is.True);
            Assert.That(ToolTip.GetTip(back), Is.EqualTo("Back to 11D 7402 (Alt+Left)"));
            Assert.That(forward.IsEnabled, Is.False);

            back.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.That(w.Session!.Selected, Is.EqualTo("part:11D"));
            Assert.That(w.Session.Traced, Is.Empty);
            Assert.That(canvas.ViewCentre.X, Is.EqualTo(chipView.X).Within(0.5));
            Assert.That(canvas.ViewCentre.Y, Is.EqualTo(chipView.Y).Within(0.5));
            Assert.That(forward.IsEnabled, Is.True);
            Assert.That(ToolTip.GetTip(forward), Is.EqualTo("Forward to /LEV12 (Alt+Right)"));

            w.FindControl<Button>("DetailsForward")!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.That(w.Session.Selected, Is.EqualTo("net:LEV12_0"));
            Assert.That(w.Session.Traced, Is.EqualTo("LEV12_0"));
            w.Close();
        }

        [AvaloniaTest]
        public void AltLeft_AltRight_AndTheMouseSideButtons_GoBackAndForward()
        {
            MainWindow w = Open();
            w.PickFromList("part:11D");
            w.PickFromList("part:13D");
            w.KeyPress(Key.Left, RawInputModifiers.Alt, PhysicalKey.ArrowLeft, null);
            Assert.That(w.Session!.Selected, Is.EqualTo("part:11D"));
            w.KeyPress(Key.Right, RawInputModifiers.Alt, PhysicalKey.ArrowRight, null);
            Assert.That(w.Session.Selected, Is.EqualTo("part:13D"));
            Point inside = new(700, 500);
            w.MouseDown(inside, MouseButton.XButton1);
            w.MouseUp(inside, MouseButton.XButton1);
            Assert.That(w.Session.Selected, Is.EqualTo("part:11D"));
            w.MouseDown(inside, MouseButton.XButton2);
            w.MouseUp(inside, MouseButton.XButton2);
            Assert.That(w.Session.Selected, Is.EqualTo("part:13D"));
            w.Close();
        }

        [AvaloniaTest]
        public void APickAfterGoingBack_DropsTheForwardPlaces()
        {
            MainWindow w = Open();
            w.PickFromList("part:11D");
            w.PickFromList("part:13D");
            w.GoBack();
            w.PickFromList("part:3C");
            Assert.That(w.FindControl<Button>("TbForward")!.IsEnabled, Is.False);
            w.GoBack();
            Assert.That(w.Session!.Selected, Is.EqualTo("part:11D"));
            w.Close();
        }

        private static MainWindow Open()
        {
            MainWindow w = new() { Width = 1500, Height = 1000 };
            w.Show();
            w.OpenSheet(Sheet2A50);
            return w;
        }
    }
}
