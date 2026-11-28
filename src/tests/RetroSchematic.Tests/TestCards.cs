//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// Test cards made at run time from the one stored file of a card (owner rule 2026-11-28:
    /// one card, one file). Shared by the model tests and the window tests (linked into both
    /// projects).
    /// </summary>
    public static class TestCards
    {
        /// <summary>The 2A50 card as stored: one flat sheet.</summary>
        public static readonly string Flat2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        // the four blocks of 2A50 and their members, as in the block copy that was stored until
        // 2026-11-28: the chips of each function with their resistors, capacitors and straps
        private static readonly string[] AddressDecode = { "11C", "11E", "13D", "13E", "15E", "9E", "R-13E-1", "R-15E-1", "R-15E-2", "R-15E-3", "R-15E-4" };
        private static readonly string[] ReadMultiplexers = { "13C", "1E", "3D", "3E", "5B", "5C", "5D", "5E", "7A", "7E", "R-1E-1", "R-1E-2", "R-1E-3", "R-1E-4", "W-3D-1", "W-3D-2" };
        private static readonly string[] InterruptLogic = { "1A", "1B", "1C", "1D", "3A", "3B", "3C", "5A" };
        private static readonly string[] ModemInterface = { "13A", "15A", "15B", "17A", "19A", "C-19A-1" };

        /// <summary>The word a test case uses for <see cref="Blocks2A50"/> in place of a file name.</summary>
        public const string Blocks2A50Case = "made:2A50-blocks";

        /// <summary>
        /// A test case's sheet: the made-at-run-time card for <see cref="Blocks2A50Case"/>, else
        /// the file under the test's <c>sheets</c> folder.
        /// </summary>
        /// <param name="sheet">The case's sheet name.</param>
        /// <returns>The sheet file.</returns>
        public static string Sheet(string sheet)
            => sheet == Blocks2A50Case ? Blocks2A50() : Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", sheet);

        /// <summary>
        /// Card 2A50 drawn in four blocks (U1 address-decode, U2 read-multiplexers,
        /// U3 interrupt-logic, U4 modem-interface), made from the flat sheet by the app's own
        /// make-block step and saved into the test's work folder.
        /// </summary>
        /// <returns>The saved sheet file (a new one on every call, so a test may change it).</returns>
        public static string Blocks2A50()
        {
            string folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "test-cards");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "2A50-blocks-" + System.Guid.NewGuid().ToString("N") + ".rsc");
            SheetEditor e = new(NetlistReader.ReadFile(Flat2A50), new ChipCatalog(ChipCatalog.Find(Flat2A50)!)) { ReadOnly = false, Files = SheetAttachments.Load(Flat2A50) };
            e.MakeBlock("address-decode", "U1", new List<string>(AddressDecode), null);
            e.MakeBlock("read-multiplexers", "U2", new List<string>(ReadMultiplexers), null);
            e.MakeBlock("interrupt-logic", "U3", new List<string>(InterruptLogic), null);
            e.MakeBlock("modem-interface", "U4", new List<string>(ModemInterface), null);
            e.Save(path);
            return path;
        }
    }
}
