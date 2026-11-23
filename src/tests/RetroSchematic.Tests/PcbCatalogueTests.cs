//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using System.Text;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The ND PCB catalogue reader (<see cref="PcbCatalogue"/>) and the import into a sheet
    /// (<see cref="SheetEditor.ImportFromCatalogue"/>). The real catalogue is outside the
    /// repository, so these tests write a small one in the shape of pcb.json (two boards, one
    /// card number shared) with tiny photo files.
    /// </summary>
    [TestFixture]
    public class PcbCatalogueTests
    {
        // the 322049 entry as pcb.json has it (shortened description), plus a second board
        // that shares card number 1050 to test the match by card number
        private const string Json = """
        { "pcbs": [
          { "number": "1050", "partNumber": "322049", "title": "Synchronous Modem Buffer", "ndSystem": "10", "group": "1C",
            "description": "Synchronous modem buffer.", "printVersion": "I", "ecoVersion": "3",
            "printVersions": [ { "print": "I", "eco": "3", "note": "" } ],
            "specifications": { "size": "156 x 149 mm", "layers": 2 }, "releaseDate": "",
            "chipset": [ { "name": "COM2601", "description": "USRT", "text": "COM2601 - USRT", "url": "http://sintran.com/library/libother/extern/COM2601.pdf" } ],
            "leds": [ { "id": "OCTO", "color": "yellow", "description": "Octobus activity.", "text": "OCTO (yellow): Octobus activity." } ],
            "straps": [],
            "switches": [
              { "id": "", "description": "x", "text": "IDENT (position E1), binary inverted value * 4" },
              { "id": "", "description": "x", "text": "First device number (position E15), binary value * 8" },
              { "id": "", "description": "x", "text": "Test (position C9), not on the sheet" } ],
            "connectors": [ { "id": "", "type": "Standard NORD-10 bus", "text": "Standard NORD-10 bus" } ],
            "images": [
              { "type": "component_side", "filename": "322049-Component Side - PCB Version I old.jpg", "version": "I", "variant": "old", "caption": "Component side, PCB version I, old", "source": "nd-322049-3.jpg" },
              { "type": "solder_side", "filename": "322049-Solder Side - PCB Version I old.jpg", "version": "I", "variant": "old", "caption": "Solder side, PCB version I, old", "source": "nd-322049-4.jpg" },
              { "type": "component_side", "filename": "322049-Component Side - PCB Version I new.jpg", "version": "I", "variant": "new", "caption": "Component side, PCB version I, new", "source": "nd-322049-1.jpg" },
              { "type": "other", "filename": "box.jpg" } ],
            "sourceUrl": "http://norsk-data.com/hardware/nd-10/nd-322049.html", "folder": "322049" },
          { "number": "1050", "partNumber": "322999", "title": "Another board", "ndSystem": "500", "specifications": { "size": "", "layers": 4 }, "images": [], "folder": "322999" }
        ] }
        """;

        private string _dir = "";

        [SetUp]
        public void MakeCatalogue()
        {
            _dir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "pcb-" + TestContext.CurrentContext.Test.Name);
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
            Directory.CreateDirectory(Path.Combine(_dir, "catalogue", "322049"));
            File.WriteAllText(Path.Combine(_dir, "catalogue", "pcb.json"), Json, new UTF8Encoding(false));
            Photo("322049-Component Side - PCB Version I old.jpg", 1);
            Photo("322049-Solder Side - PCB Version I old.jpg", 2);
            Photo("322049-Component Side - PCB Version I new.jpg", 3);
        }

        [TearDown]
        public void RemoveCatalogue()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        private string CatalogueFile => Path.Combine(_dir, "catalogue", "pcb.json");

        // a "photo": a few bytes that differ per photo
        private void Photo(string name, byte mark)
            => File.WriteAllBytes(Path.Combine(_dir, "catalogue", "322049", name), new byte[] { 0xFF, 0xD8, mark, 0xFF, 0xD9 });

        [Test]
        public void Positions_AreRowLetterFirst_InTheCatalogue()
        {
            Assert.That(PcbCatalogue.SwitchPosition("IDENT (position E1), binary inverted value * 4"), Is.EqualTo("1E"));
            Assert.That(PcbCatalogue.SwitchPosition("First device number (position E15), binary value * 8"), Is.EqualTo("15E"));
            Assert.That(PcbCatalogue.SwitchPosition("no position here"), Is.Null);
            Assert.That(PcbCatalogue.MachineName("10"), Is.EqualTo("NORD-10"));
            Assert.That(PcbCatalogue.MachineName("100"), Is.EqualTo("ND-100"));
            Assert.That(PcbCatalogue.MachineName("500"), Is.Null);
            BoardSize s = PcbCatalogue.ParseSize("156 x 149 mm")!;
            Assert.That(s.WidthMm, Is.EqualTo(156));
            Assert.That(s.HeightMm, Is.EqualTo(149));
            Assert.That(PcbCatalogue.ParseSize(""), Is.Null);
        }

        [Test]
        public void Find_ByPartNumber_ThenByCardNumber()
        {
            PcbCatalogue c = PcbCatalogue.Load(CatalogueFile);
            Assert.That(c.Count, Is.EqualTo(2));
            Assert.That(c.Find("322049"), Has.Count.EqualTo(1));
            Assert.That(c.Find("ND-322049"), Has.Count.EqualTo(1));
            Assert.That(c.Find("1050"), Has.Count.EqualTo(2), "two boards share the card number");
            Assert.That(c.Find("12345"), Is.Empty);
            CatalogueEntry e = c.Find("322049")[0];
            BoardSection b = e.Board;
            Assert.That(b.Number, Is.EqualTo("1050"));
            Assert.That(b.Title, Is.EqualTo("Synchronous Modem Buffer"));
            Assert.That(b.NdSystem, Is.EqualTo("NORD-10"));
            Assert.That(b.Specifications!.Size!.WidthMm, Is.EqualTo(156));
            Assert.That(b.Specifications.Layers, Is.EqualTo(2));
            Assert.That(b.Chipset![0].Url, Is.EqualTo("http://sintran.com/library/libother/extern/COM2601.pdf"));
            Assert.That(b.Leds![0].Color, Is.EqualTo("yellow"));
            Assert.That(b.Straps, Is.Null, "an empty list is not written");
            Assert.That(b.ReleaseDate, Is.Null, "an empty value is not a fact");
            Assert.That(b.FactSources!.ContainsKey("releaseDate"), Is.False);
            // every fact filled names the catalogue and the board's page
            string[] filled = { "number", "partNumber", "title", "ndSystem", "group", "description", "printVersion", "ecoVersion", "printVersions", "specifications", "chipset", "leds", "switches", "connectors" };
            Assert.That(b.FactSources.Count, Is.EqualTo(filled.Length));
            for (int i = 0; i < filled.Length; i++)
            {
                DescriptionSource f = b.FactSources[filled[i]];
                Assert.That(f.Kind, Is.EqualTo(SourceKind.Catalogue), filled[i]);
                Assert.That(f.Place, Is.EqualTo("http://norsk-data.com/hardware/nd-10/nd-322049.html"), filled[i]);
            }
            Assert.That(e.Photos, Has.Count.EqualTo(3), "only component and solder side pictures");

            CatalogueEntry other = c.Find("322999")[0];
            Assert.That(other.Board.NdSystem, Is.Null, "ND-500 is not a sheet machine");
            Assert.That(other.CatalogueSystem, Is.EqualTo("500"));
            Assert.That(other.Board.Specifications!.Size, Is.Null);
        }

        [Test]
        public void NotACatalogue_IsRefused()
        {
            string bad = Path.Combine(_dir, "bad.json");
            File.WriteAllText(bad, "{ \"boards\": [] }");
            Assert.Throws<IOException>(() => PcbCatalogue.Load(bad));
        }

        /// <summary>
        /// A sheet not saved yet holds the photos inside; the board facts, the photo variants
        /// and the switch text all arrive; one undo takes it all back; a second import changes
        /// nothing more.
        /// </summary>
        [Test]
        public void Import_FillsTheBoard_Photos_AndSwitchSources_InOneUndoStep()
        {
            Netlist n = SheetEditor.NewSheet("2A50", "test", MachineBus.Nord10);
            n.Switches = new()
            {
                new SwitchBlock { Ref = "15E", LeverCount = 4 },
                new SwitchBlock { Ref = "1E", LeverCount = 4, Function = "ident" },
            };
            n.Board = new BoardSection { Note = "typed by hand" };
            SheetEditor e = new(n, null) { ReadOnly = false };
            CatalogueEntry entry = PcbCatalogue.Load(CatalogueFile).Find("322049")[0];

            string said = e.ImportFromCatalogue(entry, null);
            Assert.That(said, Does.Contain("names no switch block"), "the C9 text has no block");
            Netlist r = e.Root;
            Assert.That(r.Board!.PartNumber, Is.EqualTo("322049"));
            Assert.That(r.Board.Note, Is.EqualTo("typed by hand"), "the old note is kept");
            Assert.That(r.Photos, Has.Count.EqualTo(3));
            Assert.That(r.Photos![0].File, Is.EqualTo("photos/322049-component-side-I-old.jpg"));
            Assert.That(r.Photos[2].File, Is.EqualTo("photos/322049-component-side-I-new.jpg"));
            Assert.That(r.Photos[2].Variant, Is.EqualTo("new"));
            Assert.That(r.Photos[1].Side, Is.EqualTo(BoardSide.Solder));
            Assert.That(r.Photos[0].Source!.Quote, Does.Contain("nd-322049-3.jpg"));
            Assert.That(e.Files.Holds("photos/322049-component-side-I-new.jpg"), Is.True);
            Assert.That(e.Files.Read("photos/322049-component-side-I-new.jpg")![2], Is.EqualTo(3));
            Assert.That(r.Switches![1].MoreSources![0].Quote, Is.EqualTo("IDENT (position E1), binary inverted value * 4"));
            Assert.That(r.Switches[1].MoreSources![0].Note, Does.Contain("E1 is our 1E"));
            Assert.That(r.Switches[1].Function, Is.EqualTo("ident"), "nothing else of the block changes");
            Assert.That(r.Switches[0].MoreSources![0].Quote, Does.Contain("(position E15)"));

            // again: the same photos are found by their bytes, the catalogue source replaced
            e.ImportFromCatalogue(entry, null);
            Assert.That(e.Root.Photos, Has.Count.EqualTo(3));
            Assert.That(e.Root.Switches![1].MoreSources, Has.Count.EqualTo(1));

            e.Undo();
            e.Undo();
            Assert.That(e.Root.Board!.PartNumber, Is.Null);
            Assert.That(e.Root.Photos, Is.Null);
            Assert.That(e.Root.Switches![1].MoreSources, Is.Null);
        }

        [Test]
        public void Import_NeedsEditMode()
        {
            SheetEditor e = new(SheetEditor.NewSheet("T", "t", MachineBus.Nord10), null);
            CatalogueEntry entry = PcbCatalogue.Load(CatalogueFile).Find("322049")[0];
            Assert.Throws<SheetEditException>(() => e.ImportFromCatalogue(entry, null));
        }

        /// <summary>
        /// A .rsc gets the new photos on disk beside the photos it has, and a photo it has
        /// already (the same bytes) keeps its own file.
        /// </summary>
        [Test]
        public void Import_IntoRsc_CopiesBesideItsPhotos_AndKeepsTheSameOnes()
        {
            string sheetDir = Path.Combine(_dir, "sheet");
            Directory.CreateDirectory(Path.Combine(sheetDir, "my-files", "photos"));
            File.Copy(Path.Combine(_dir, "catalogue", "322049", "322049-Component Side - PCB Version I old.jpg"), Path.Combine(sheetDir, "my-files", "photos", "front.jpg"));
            string rsc = Path.Combine(sheetDir, "card.rsc");
            Netlist n = SheetEditor.NewSheet("T", "t", MachineBus.Nord10);
            n.Photos = new() { new BoardPhoto { File = "my-files/photos/front.jpg", Side = BoardSide.Component } };
            SheetEditor e = new(n, null) { ReadOnly = false, Files = new SheetAttachments { Folder = sheetDir } };
            e.ImportFromCatalogue(PcbCatalogue.Load(CatalogueFile).Find("322049")[0], rsc);
            Assert.That(e.Root.Photos, Has.Count.EqualTo(3));
            Assert.That(e.Root.Photos![0].File, Is.EqualTo("my-files/photos/front.jpg"));
            Assert.That(e.Root.Photos[0].Variant, Is.EqualTo("old"));
            Assert.That(e.Root.Photos[1].File, Is.EqualTo("my-files/photos/322049-solder-side-I-old.jpg"));
            Assert.That(File.Exists(Path.Combine(sheetDir, "my-files", "photos", "322049-component-side-I-new.jpg")), Is.True);
            Assert.That(e.Files.Holds(e.Root.Photos[1].File), Is.False, "a .rsc links");
        }
    }
}
