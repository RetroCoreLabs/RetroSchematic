//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;

namespace RetroSchematic.Model
{
    /// <summary>
    /// One card file in the library.
    /// </summary>
    public sealed class CardEntry
    {
        /// <summary>The sheet file (.rsc text or .rsx zip).</summary>
        public string File { get; init; } = "";

        /// <summary>The sheet number as written in the title block ("2A50", "3207").</summary>
        public string Id { get; init; } = "";

        /// <summary>The sheet title from the title block.</summary>
        public string Title { get; init; } = "";

        /// <summary>The board's own name from the board facts, or null when the file has none.</summary>
        public string? BoardTitle { get; init; }

        /// <summary>
        /// The machine family as written in the file's <c>sheet.machine</c> field ("NORD-10",
        /// "ND-100"; "ND-500" once such cards are added). A file without one is listed as
        /// "Not known".
        /// </summary>
        public string Family { get; init; } = "";

        /// <summary>The number of drawing pages in the file (1 for a single-page sheet).</summary>
        public int Pages { get; init; }

        /// <summary>The number of chips and other parts on the top level and in every block.</summary>
        public int Parts { get; init; }
    }

    /// <summary>
    /// The card library: every sheet file under a folder, read for its title block, so a window
    /// can list the cards by machine family. Nothing is fixed in code: a new family appears as
    /// soon as a file names it.
    /// </summary>
    public static class CardLibrary
    {
        /// <summary>The family name used for a file with no machine field.</summary>
        public const string UnknownFamily = "Not known";

        /// <summary>
        /// The library folder for a chip catalogue folder: the catalogue is <c>ROOT/catalog</c>
        /// and the sheets are beside it in <c>ROOT/sheets</c> (the repository's <c>src/sheets</c>,
        /// or the copy beside the program). The window, the bus use report and the MCP server all
        /// find the library this way.
        /// </summary>
        /// <param name="catalogFolder">The chip catalogue folder (the one holding <c>chips</c>), or null.</param>
        /// <returns>The library folder, or null when there is no catalogue folder.</returns>
        public static string? FolderBeside(string? catalogFolder)
            => catalogFolder == null ? null : Path.Combine(Path.GetDirectoryName(Path.GetFullPath(catalogFolder)) ?? catalogFolder, "sheets");

        /// <summary>
        /// Reads every .rsc and .rsx file under a folder (all sub-folders). A file that cannot
        /// be read is not listed; its name and the reason go into <paramref name="problems"/>.
        /// The result is sorted by family, then sheet number, then file name.
        /// </summary>
        /// <param name="folder">The library folder (the repository's <c>sheets</c> folder).</param>
        /// <param name="problems">One line per file that could not be read.</param>
        /// <returns>The cards found.</returns>
        public static List<CardEntry> Scan(string folder, List<string> problems)
        {
            List<CardEntry> cards = new();
            if (!Directory.Exists(folder))
                return cards;
            string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string f = files[i];
                if (!f.EndsWith(SheetFile.TextEnding, StringComparison.OrdinalIgnoreCase) && !SheetFile.IsZipPath(f))
                    continue;
                Netlist n;
                try
                {
                    n = NetlistReader.ReadFile(f);
                }
                catch (Exception e) when (e is NetlistException or IOException or System.Text.Json.JsonException or InvalidDataException)
                {
                    problems.Add(Path.GetFileName(f) + ": " + e.Message);
                    continue;
                }
                cards.Add(new CardEntry
                {
                    File = f,
                    Id = n.Sheet.Id,
                    Title = n.Sheet.Title ?? "",
                    BoardTitle = n.Board?.Title,
                    Family = string.IsNullOrWhiteSpace(n.Sheet.Machine) || string.Equals(n.Sheet.Machine, "none", StringComparison.OrdinalIgnoreCase)
                        ? UnknownFamily : n.Sheet.Machine!,
                    Pages = n.Pages == null || n.Pages.Count == 0 ? 1 : n.Pages.Count,
                    Parts = CountParts(n),
                });
            }
            cards.Sort(Compare);
            return cards;
        }

        /// <summary>
        /// The families found, sorted, each named once.
        /// </summary>
        /// <param name="cards">The cards from <see cref="Scan"/>.</param>
        /// <returns>The family names.</returns>
        public static List<string> Families(List<CardEntry> cards)
        {
            List<string> names = new();
            for (int i = 0; i < cards.Count; i++)
            {
                if (!names.Contains(cards[i].Family))
                    names.Add(cards[i].Family);
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        private static int Compare(CardEntry a, CardEntry b)
        {
            int c = string.Compare(a.Family, b.Family, StringComparison.OrdinalIgnoreCase);
            if (c == 0)
                c = string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
            if (c == 0)
                c = string.Compare(a.File, b.File, StringComparison.OrdinalIgnoreCase);
            return c;
        }

        private static int CountParts(Netlist n)
        {
            int count = n.Parts.Count;
            if (n.Blocks != null)
            {
                for (int i = 0; i < n.Blocks.Count; i++)
                    count += n.Blocks[i].Parts.Count;
            }
            return count;
        }
    }
}
