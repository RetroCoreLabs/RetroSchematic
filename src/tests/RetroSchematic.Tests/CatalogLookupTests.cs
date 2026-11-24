//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// Where a program finds the catalogue for a sheet (<see cref="ChipCatalog.FindForSheet"/>):
    /// up from the sheet first, else up from the program's own folder. The second place is what
    /// makes a sheet kept outside the repository, or outside the unpacked download, still draw
    /// with its chip files.
    /// </summary>
    [TestFixture]
    public class CatalogLookupTests
    {
        private string _dir = "";

        [SetUp]
        public void MakeFolder()
        {
            _dir = Path.Combine(Path.GetTempPath(), "rsc-lookup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void RemoveFolder()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void SheetWithNoCatalogueAbove_UsesTheOneBesideTheProgram()
        {
            string sheet = Path.Combine(_dir, "mine.rsc");
            File.WriteAllText(sheet, "{}");
            Assert.That(ChipCatalog.Find(sheet), Is.Null, "the temp folder must have no catalog above it for this test to mean anything");

            string? found = ChipCatalog.FindForSheet(sheet);

            // the test program's folder carries a copy of catalog/ (the Desktop project copies it,
            // and the release download carries one beside the programs)
            Assert.That(found, Is.Not.Null);
            Assert.That(found, Is.EqualTo(ChipCatalog.Find(AppContext.BaseDirectory)));
        }

        [Test]
        public void CatalogueAboveTheSheet_WinsOverTheProgramsOwn()
        {
            string catalog = Path.Combine(_dir, "catalog");
            Directory.CreateDirectory(Path.Combine(catalog, "chips"));
            string sheets = Path.Combine(_dir, "sheets", "nord-10");
            Directory.CreateDirectory(sheets);
            string sheet = Path.Combine(sheets, "card.rsc");
            File.WriteAllText(sheet, "{}");

            Assert.That(ChipCatalog.FindForSheet(sheet), Is.EqualTo(catalog));
        }

        [Test]
        public void SheetNotSavedYet_UsesTheOneBesideTheProgram()
        {
            Assert.That(ChipCatalog.FindForSheet(null), Is.EqualTo(ChipCatalog.Find(AppContext.BaseDirectory)));
        }
    }
}
