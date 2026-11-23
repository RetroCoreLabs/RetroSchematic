//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// Sheet file endings (docs/FORMAT.md): .rsc is the JSON as text, .rsx the same JSON zipped,
    /// and an old .json still reads. Reading goes by content, not by name.
    /// </summary>
    [TestFixture]
    public class SheetFileTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private static string Scratch(string ending)
            => Path.Combine(TestContext.CurrentContext.WorkDirectory, "sheetfile-" + TestContext.CurrentContext.Test.Name + ending);

        [Test]
        public void RscAndRsx_RoundTrip_ToTheSameSheet_AndOldJsonStillLoads()
        {
            string rsc = Scratch(".rsc"), rsx = Scratch(".rsx"), json = Scratch(".json");
            try
            {
                SheetEditor e = new(NetlistReader.ReadFile(Sheet2A50), null) { Files = SheetAttachments.Load(Sheet2A50) };
                e.Save(rsc);
                e.Save(rsx);
                File.Copy(rsc, json, overwrite: true);
                // the links differ (a path beside the sheet in .rsc, a zip entry in .rsx; see
                // BoardFileTests), everything else is the same
                string a = WithoutLinks(NetlistReader.ReadFile(rsc));
                string b = WithoutLinks(NetlistReader.ReadFile(rsx));
                string c = WithoutLinks(NetlistReader.ReadFile(json));
                Assert.That(b, Is.EqualTo(a), ".rsx reads back as the same sheet as .rsc");
                Assert.That(c, Is.EqualTo(a), "an old .json still reads");
                Assert.That(File.ReadAllText(rsc).TrimStart()[0], Is.EqualTo('{'), ".rsc is plain JSON text");
                Assert.That(SheetFile.IsZip(File.ReadAllBytes(rsx)), Is.True, ".rsx is a zip");
                using ZipArchive zip = ZipFile.OpenRead(rsx);
                // sheet.json first, then the source page and the two photos
                Assert.That(zip.Entries.Count, Is.EqualTo(4));
                Assert.That(zip.Entries[0].FullName, Is.EqualTo(SheetFile.ZipEntryName));
            }
            finally
            {
                File.Delete(rsc);
                File.Delete(rsx);
                File.Delete(json);
            }
        }

        [Test]
        public void ReadingGoesByContent_NotByName()
        {
            // a zipped sheet with the text ending, and a text sheet with the zipped ending
            string zipNamedRsc = Scratch(".rsc"), textNamedRsx = Scratch(".rsx"), real = Scratch("-z.rsx");
            try
            {
                SheetEditor e = new(NetlistReader.ReadFile(Sheet2A50), null) { Files = SheetAttachments.Load(Sheet2A50) };
                e.Save(real);
                File.Copy(real, zipNamedRsc, overwrite: true);
                File.Copy(Sheet2A50, textNamedRsx, overwrite: true);
                string want = WithoutLinks(NetlistReader.ReadFile(Sheet2A50));
                Assert.That(WithoutLinks(NetlistReader.ReadFile(zipNamedRsc)), Is.EqualTo(want));
                Assert.That(WithoutLinks(NetlistReader.ReadFile(textNamedRsx)), Is.EqualTo(want));
            }
            finally
            {
                File.Delete(zipNamedRsc);
                File.Delete(textNamedRsx);
                File.Delete(real);
            }
        }

        /// <summary>
        /// The sheet as JSON with the source and photo links taken out (they are rewritten by
        /// every save to another place, by design).
        /// </summary>
        internal static string WithoutLinks(Netlist n)
        {
            n.Sources = null;
            n.Photos = null;
            return NetlistReader.Write(n);
        }
    }
}
