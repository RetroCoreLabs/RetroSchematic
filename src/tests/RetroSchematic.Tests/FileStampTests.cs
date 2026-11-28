//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The file stamp (FORMAT.md, "File stamp"): every sheet file in the library carries a
    /// revision and the time of its last save, and every save raises the revision by one.
    /// </summary>
    [TestFixture]
    public class FileStampTests
    {
        private static readonly string Sheets = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets");
        private static readonly string Demo = Path.Combine(Sheets, "demo", "demo-gates.rsc");

        [Test]
        public void EverySheetInTheLibrary_HasAStamp_AndNoneIsStoredZipped()
        {
            string[] files = Directory.GetFiles(Sheets, "*.*", SearchOption.AllDirectories);
            int sheets = 0;
            for (int i = 0; i < files.Length; i++)
            {
                Assert.That(SheetFile.IsZipPath(files[i]), Is.False, "no card is stored zipped: " + files[i]);
                if (!files[i].EndsWith(SheetFile.TextEnding, StringComparison.OrdinalIgnoreCase))
                    continue;
                sheets++;
                Netlist n = NetlistReader.ReadFile(files[i]);
                Assert.That(n.File, Is.Not.Null, files[i]);
                Assert.That(n.File!.Revision, Is.GreaterThanOrEqualTo(1), files[i]);
                Assert.That(DateTime.TryParseExact(n.File.Modified, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _), Is.True, files[i] + ": " + n.File.Modified);
            }
            Assert.That(sheets, Is.GreaterThanOrEqualTo(3));
        }

        [TestCase(".rsc")]
        [TestCase(".rsx")]
        public void EverySave_RaisesTheRevisionByOne_AndSetsTheTime(string ending)
        {
            string work = Path.Combine(TestContext.CurrentContext.WorkDirectory, "stamp" + ending);
            int before = NetlistReader.ReadFile(Demo).File!.Revision;
            DateTime start = DateTime.UtcNow.AddSeconds(-1);
            SheetEditor editor = new(NetlistReader.ReadFile(Demo), null);
            editor.Save(work);
            Netlist once = NetlistReader.ReadFile(work);
            Assert.That(once.File!.Revision, Is.EqualTo(before + 1));
            DateTime saved = DateTime.ParseExact(once.File.Modified, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            Assert.That(saved, Is.GreaterThanOrEqualTo(start.AddSeconds(-1)));

            editor.Save(work);
            Assert.That(NetlistReader.ReadFile(work).File!.Revision, Is.EqualTo(before + 2));
        }

        [Test]
        public void AnOldFileWithoutAStamp_GetsRevisionOne_OnItsFirstSave()
        {
            Netlist old = NetlistReader.ReadFile(Demo);
            old.File = null;
            string work = Path.Combine(TestContext.CurrentContext.WorkDirectory, "unstamped.rsc");
            new SheetEditor(old, null).Save(work);
            Assert.That(NetlistReader.ReadFile(work).File!.Revision, Is.EqualTo(1));
        }
    }
}
