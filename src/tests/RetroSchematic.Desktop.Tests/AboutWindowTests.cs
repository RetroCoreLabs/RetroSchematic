//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using RetroSchematic.Model;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// Help > About: the menu item is there, and the box shows the build stamp - version, commit,
    /// branch and the build date and time.
    /// </summary>
    [TestFixture]
    public class AboutWindowTests
    {
        [AvaloniaTest]
        public void TheAboutBox_ShowsTheBuildStamp()
        {
            MainWindow main = new();
            main.Show();
            Assert.That(main.FindControl<MenuItem>("AboutItem"), Is.Not.Null);

            AboutWindow w = new();
            w.Show();
            Assert.That(w.VersionText.Text, Is.EqualTo(BuildIdentity.Version));
            Assert.That(w.CommitText.Text, Does.Contain(BuildIdentity.Commit));
            Assert.That(w.CommitText.Text, Does.Contain(BuildIdentity.Branch));
            Assert.That(w.CommitText.Text!.Contains("(uncommitted changes)"), Is.EqualTo(BuildIdentity.IsDirty));
            Assert.That(w.BuiltText.Text, Is.EqualTo(BuildIdentity.BuildDate + " " + BuildIdentity.BuildTime));
            Assert.That(w.CommitText.Text, Does.Not.Contain(BuildIdentity.Unknown));
            BoardViewTests.Save(w, "about");
            w.Close();
            main.Close();
        }
    }
}
