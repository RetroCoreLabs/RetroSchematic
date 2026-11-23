//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The board section, source pages, photos and switches (docs/FORMAT.md, "Board, sources,
    /// photos, switches"), and how a .rsx carries the files inside it while a .rsc links them.
    /// </summary>
    [TestFixture]
    public class BoardFileTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private static string Scratch(string name)
        {
            string dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "board-" + TestContext.CurrentContext.Test.Name);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, name);
        }

        [Test]
        public void TheExample_HasItsBoardFacts()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            Assert.That(n.Board, Is.Not.Null);
            Assert.That(n.Board!.NdSystem, Is.EqualTo("NORD-10"));
            Assert.That(n.Board.PartNumber, Is.EqualTo("322049"));
            Assert.That(n.Board.PrintVersion, Is.EqualTo("I"));
            Assert.That(n.Board.EcoVersion, Is.EqualTo("3"));
            Assert.That(n.Board.Specifications!.Size!.WidthMm, Is.EqualTo(156));
            Assert.That(n.Board.Specifications.Size.HeightMm, Is.EqualTo(149));
            Assert.That(n.Board.Specifications.Layers, Is.EqualTo(2));
            Assert.That(n.Connectors, Has.Count.EqualTo(2));
            Assert.That(n.Connectors![0].Pins, Is.EqualTo("0-99"));
            Assert.That(n.Sources, Has.Count.EqualTo(1));
            Assert.That(n.Sources![0].Original!.PdfPage, Is.EqualTo(64));
            // the old and the new board, both sides, from the ND PCB catalogue (322049)
            Assert.That(n.Photos, Has.Count.EqualTo(4));
            Assert.That(n.Photos![0].Side, Is.EqualTo(BoardSide.Component));
            Assert.That(n.Photos[1].Side, Is.EqualTo(BoardSide.Solder));
            Assert.That(n.Photos[0].Variant, Is.EqualTo("old"));
            Assert.That(n.Photos[2].Variant, Is.EqualTo("new"));
            Assert.That(n.Photos[2].Side, Is.EqualTo(BoardSide.Component));
            Assert.That(n.Photos[3].Side, Is.EqualTo(BoardSide.Solder));
            for (int i = 0; i < 4; i++)
                Assert.That(n.Photos[i].Source!.Kind, Is.EqualTo(SourceKind.Catalogue), n.Photos[i].File);
            Assert.That(n.Board.Number, Is.EqualTo("1050"));
            Assert.That(n.Board.Title, Is.EqualTo("Synchronous Modem Buffer"));
            Assert.That(n.Board.SourceUrl, Is.EqualTo("http://norsk-data.com/hardware/nd-10/nd-322049.html"));
            Assert.That(n.Board.FactSources!["partNumber"].Kind, Is.EqualTo(SourceKind.Catalogue));
            Assert.That(n.Board.FactSources["specifications"].Place, Is.EqualTo(n.Board.SourceUrl));

            // every linked file is there
            SheetAttachments files = SheetAttachments.Load(Sheet2A50);
            Assert.That(files.Read(n.Sources[0].File), Is.Not.Null, n.Sources[0].File);
            Assert.That(files.Read(n.Photos[0].File), Is.Not.Null, n.Photos[0].File);
            Assert.That(files.Read(n.Photos[1].File), Is.Not.Null, n.Photos[1].File);
        }

        /// <summary>
        /// 15E: lever 1 = SMN3 = BA9 ... lever 4 = SMN0 = BA3 (DM8160 pairs); 1E: lever 1 =
        /// IDENT2 = ident bit 2 ... lever 4 = IDENT5. Each lever's signal is a signal of the sheet.
        /// </summary>
        [Test]
        public void TheExample_Switches_NameRealSignals()
        {
            Netlist n = NetlistReader.ReadFile(Sheet2A50);
            Assert.That(n.Switches, Has.Count.EqualTo(2));
            SwitchBlock address = n.Switches![0], ident = n.Switches[1];
            Assert.That(address.Ref, Is.EqualTo("15E"));
            Assert.That(ident.Ref, Is.EqualTo("1E"));
            string[] bits = { "BA9", "BA5", "BA4", "BA3" };
            for (int i = 0; i < 4; i++)
            {
                Assert.That(address.Levers[i].Number, Is.EqualTo(i + 1));
                Assert.That(address.Levers[i].Signal, Is.EqualTo("SMN" + (3 - i)));
                Assert.That(address.Levers[i].Bit, Is.EqualTo(bits[i]));
                Assert.That(ident.Levers[i].Signal, Is.EqualTo("IDENT" + (i + 2)));
                Assert.That(address.Levers[i].Setting, Is.EqualTo("open"), "not readable on the photo");
                Assert.That(HasNet(n, address.Levers[i].Signal!), Is.True, address.Levers[i].Signal);
                Assert.That(HasNet(n, ident.Levers[i].Signal!), Is.True, ident.Levers[i].Signal);
            }
            Assert.That(address.OnMeansConfidence, Is.EqualTo(Confidence.Inferred));

            // the ND PCB catalogue's switch text is a second source on both blocks
            Assert.That(ident.MoreSources![0].Kind, Is.EqualTo(SourceKind.Catalogue));
            Assert.That(ident.MoreSources[0].Quote, Does.StartWith("IDENT (position E1), binary inverted value * 4"));
            Assert.That(ident.MoreSources[0].Note, Does.Contain("E1 is our 1E"));
            Assert.That(address.MoreSources![0].Quote, Does.StartWith("First device number (position E15), binary value * 8"));
        }

        /// <summary>
        /// A .rsc with links, saved as .rsx and opened again, is the same sheet, and every
        /// picture inside the zip has exactly the bytes of the linked file. Saving that .rsx as a
        /// .rsc somewhere else writes the pictures out beside it, again with the same bytes.
        /// </summary>
        [Test]
        public void LinkedSheet_ToRsx_AndBack_KeepsTheSheet_AndTheImageBytes()
        {
            string rsx = Scratch("2A50.rsx");
            string rsc = Scratch("again.rsc");
            Netlist original = NetlistReader.ReadFile(Sheet2A50);
            SheetAttachments linked = SheetAttachments.Load(Sheet2A50);
            byte[] page = linked.Read(original.Sources![0].File)!;
            byte[] front = linked.Read(original.Photos![0].File)!;
            byte[] back = linked.Read(original.Photos[1].File)!;

            SheetEditor e = new(NetlistReader.ReadFile(Sheet2A50), null) { Files = SheetAttachments.Load(Sheet2A50) };
            e.Save(rsx);

            // one file: sheet.json plus the five pictures, under sources/ and photos/
            using (ZipArchive zip = ZipFile.OpenRead(rsx))
            {
                Assert.That(zip.Entries.Count, Is.EqualTo(6));
                Assert.That(zip.GetEntry("sources/2A50-sync-modem-buffer-1050-p64.png"), Is.Not.Null);
                Assert.That(zip.GetEntry("photos/322049-component-side-I-old.jpg"), Is.Not.Null);
                Assert.That(zip.GetEntry("photos/322049-solder-side-I-old.jpg"), Is.Not.Null);
            }

            Netlist opened = NetlistReader.ReadFile(rsx);
            SheetAttachments inside = SheetAttachments.Load(rsx);
            Assert.That(SheetFileTests.WithoutLinks(NetlistReader.ReadFile(rsx)), Is.EqualTo(SheetFileTests.WithoutLinks(NetlistReader.ReadFile(Sheet2A50))));
            Assert.That(opened.Sources![0].File, Is.EqualTo("sources/2A50-sync-modem-buffer-1050-p64.png"));
            Assert.That(opened.Sources[0].Title, Is.EqualTo(original.Sources[0].Title));
            Assert.That(opened.Photos![0].Side, Is.EqualTo(BoardSide.Component));
            Assert.That(inside.Holds(opened.Sources[0].File), Is.True, "read from the zip, not from disk");
            Assert.That(inside.Read(opened.Sources[0].File), Is.EqualTo(page));
            Assert.That(inside.Read(opened.Photos[0].File), Is.EqualTo(front));
            Assert.That(inside.Read(opened.Photos[1].File), Is.EqualTo(back));

            // the .rsx saved as .rsc: the pictures are written out beside it
            SheetEditor e2 = new(opened, null) { Files = inside };
            e2.Save(rsc);
            Netlist again = NetlistReader.ReadFile(rsc);
            Assert.That(again.Photos![0].File, Is.EqualTo("again-files/photos/322049-component-side-I-old.jpg"));
            SheetAttachments disk = SheetAttachments.Load(rsc);
            Assert.That(disk.Holds(again.Photos[0].File), Is.False, "a .rsc links, it does not hold");
            Assert.That(disk.Read(again.Sources![0].File), Is.EqualTo(page));
            Assert.That(disk.Read(again.Photos[0].File), Is.EqualTo(front));
            Assert.That(disk.Read(again.Photos[1].File), Is.EqualTo(back));
            Directory.Delete(Path.GetDirectoryName(rsx)!, recursive: true);
        }

        /// <summary>
        /// A PDF source in a .rsx is stored as the PNG of its one page at 300 dpi, and the PDF's
        /// name and page are kept in "original".
        /// </summary>
        [Test]
        public void PdfSource_IsStoredAsItsPage_AsPng_At300Dpi()
        {
            string pdf = Scratch("two-pages.pdf");
            File.WriteAllBytes(pdf, TinyPdf(2));
            string rsx = Scratch("with-pdf.rsx");
            Netlist n = SheetEditor.NewSheet("T", "pdf test", MachineBus.Nord10);
            n.Sources = new() { new SourcePage { File = "two-pages.pdf", PdfPage = 2, Title = "page two" } };
            SheetEditor e = new(n, null) { Files = new SheetAttachments { Folder = Path.GetDirectoryName(pdf) } };
            e.Save(rsx);
            Netlist opened = NetlistReader.ReadFile(rsx);
            Assert.That(opened.Sources![0].File, Is.EqualTo("sources/two-pages-p2.png"));
            Assert.That(opened.Sources[0].PdfPage, Is.Null);
            Assert.That(opened.Sources[0].Original!.File, Is.EqualTo("two-pages.pdf"));
            Assert.That(opened.Sources[0].Original!.PdfPage, Is.EqualTo(2));
            Assert.That(opened.Sources[0].Original!.Dpi, Is.EqualTo(300));
            byte[] png = SheetAttachments.Load(rsx).Read(opened.Sources[0].File)!;
            Assert.That(png[0], Is.EqualTo(0x89));
            Assert.That(Encoding.ASCII.GetString(png, 1, 3), Is.EqualTo("PNG"));
            // a 2 x 1 inch page at 300 dpi is 600 x 300 pixels (PNG header: width, height at 16)
            Assert.That(BigEndian(png, 16), Is.EqualTo(600));
            Assert.That(BigEndian(png, 20), Is.EqualTo(300));
            Assert.That(PdfPages.Count(File.ReadAllBytes(pdf)), Is.EqualTo(2));
            Directory.Delete(Path.GetDirectoryName(rsx)!, recursive: true);
        }

        [Test]
        public void MissingLinkedFile_StopsTheSave_AndSaysWhich()
        {
            string rsx = Scratch("missing.rsx");
            Netlist n = SheetEditor.NewSheet("T", "missing", MachineBus.Nord10);
            n.Photos = new() { new BoardPhoto { File = "nowhere/front.jpg", Side = BoardSide.Component } };
            SheetEditor e = new(n, null) { Files = new SheetAttachments { Folder = Path.GetDirectoryName(rsx) } };
            IOException? ex = Assert.Throws<IOException>(() => e.Save(rsx));
            Assert.That(ex!.Message, Does.Contain("nowhere/front.jpg"));
            Directory.Delete(Path.GetDirectoryName(rsx)!, recursive: true);
        }

        private static bool HasNet(Netlist n, string name)
        {
            for (int i = 0; i < n.Nets.Count; i++)
            {
                if (n.Nets[i].Name == name)
                    return true;
            }
            return false;
        }

        private static int BigEndian(byte[] b, int at) => (b[at] << 24) | (b[at + 1] << 16) | (b[at + 2] << 8) | b[at + 3];

        // a small valid PDF with the given number of empty 2 x 1 inch pages (144 x 72 points)
        private static byte[] TinyPdf(int pages)
        {
            StringBuilder sb = new();
            System.Collections.Generic.List<int> offsets = new();
            sb.Append("%PDF-1.4\n");
            offsets.Add(sb.Length);
            sb.Append("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n");
            offsets.Add(sb.Length);
            sb.Append("2 0 obj << /Type /Pages /Kids [");
            for (int i = 0; i < pages; i++)
                sb.Append((3 + i).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(" 0 R ");
            sb.Append("] /Count ").Append(pages.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(" >> endobj\n");
            for (int i = 0; i < pages; i++)
            {
                offsets.Add(sb.Length);
                sb.Append((3 + i).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(" 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 144 72] >> endobj\n");
            }
            int xref = sb.Length;
            sb.Append("xref\n0 ").Append((offsets.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("0000000000 65535 f \n");
            for (int i = 0; i < offsets.Count; i++)
                sb.Append(offsets[i].ToString("D10", System.Globalization.CultureInfo.InvariantCulture)).Append(" 00000 n \n");
            sb.Append("trailer << /Size ").Append((offsets.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(" /Root 1 0 R >>\nstartxref\n")
              .Append(xref.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("\n%%EOF\n");
            return Encoding.ASCII.GetBytes(sb.ToString());
        }
    }
}
