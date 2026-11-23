//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Importing a board from the ND PCB catalogue (<see cref="PcbCatalogue"/>): the board
    /// facts, the photos and the catalogue's switch text. One undo step.
    /// </summary>
    public sealed partial class SheetEditor
    {
        /// <summary>
        /// Fills the board section from a catalogue entry, copies its component- and
        /// solder-side photos into the sheet, and adds the catalogue's switch text as a further
        /// source on the switch blocks it names.
        /// <list type="bullet">
        /// <item><description>Board: the catalogue's values replace the section; the old note
        /// is kept.</description></item>
        /// <item><description>Photos: a photo the sheet already has (the same bytes) keeps its
        /// file and gets the catalogue's version, variant, caption and source; a new one is
        /// copied. A .rsc gets the file on disk in the folder the sheet's photos are in already,
        /// else in SHEETNAME-files/photos/; a .rsx (or a sheet not saved yet) holds it inside.
        /// </description></item>
        /// <item><description>Switches: "(position E1)" in the catalogue is our 1E (row letter
        /// first there); a block of the sheet at that position gets the text as a source with
        /// kind catalogue, replacing an older catalogue source. Nothing else of the block
        /// changes.</description></item>
        /// </list>
        /// </summary>
        /// <param name="entry">
        /// The catalogue entry.
        /// </param>
        /// <param name="sheetPath">
        /// The sheet file (.rsc links, .rsx holds), or null for a sheet not saved yet (holds).
        /// </param>
        /// <returns>
        /// What was done, one line per change.
        /// </returns>
        public string ImportFromCatalogue(CatalogueEntry entry, string? sheetPath)
        {
            CheckWritable();
            bool hold = sheetPath == null || SheetFile.IsZipPath(sheetPath);
            StringBuilder said = new();

            // read every photo before anything changes, so a missing file changes nothing
            List<byte[]> bytes = new();
            for (int i = 0; i < entry.Photos.Count; i++)
            {
                if (!File.Exists(entry.Photos[i].Path))
                    throw new SheetEditException("The catalogue photo " + entry.Photos[i].Path + " is not there.");
                bytes.Add(File.ReadAllBytes(entry.Photos[i].Path));
            }

            DescriptionSource src = new() { Kind = SourceKind.Catalogue, Document = PcbCatalogue.DocumentName, Place = entry.Board.SourceUrl, Confidence = Confidence.Read };
            List<BoardPhoto> photos = Root.Photos == null ? new List<BoardPhoto>() : NetlistReader.Clone(Root.Photos);
            string? diskFolder = hold ? null : PhotoFolder(photos, sheetPath!);
            List<(string Path, byte[] Bytes)> toWrite = new();
            List<(string Name, byte[] Bytes)> toHold = new();
            for (int i = 0; i < entry.Photos.Count; i++)
            {
                CataloguePhoto cp = entry.Photos[i];
                int at = SamePhoto(photos, bytes[i]);
                BoardPhoto p;
                if (at >= 0)
                {
                    p = photos[at];
                    said.Append("Photo ").Append(p.File).Append(" is the catalogue's ").Append(Path.GetFileName(cp.Path)).Append("; kept.\n");
                }
                else
                {
                    string name = PhotoName(entry, cp);
                    string link;
                    if (hold)
                    {
                        link = "photos/" + name;
                        toHold.Add((link, bytes[i]));
                    }
                    else
                    {
                        link = diskFolder + "/" + name;
                        toWrite.Add((Path.Combine(Path.GetDirectoryName(Path.GetFullPath(sheetPath!))!, link.Replace('/', Path.DirectorySeparatorChar)), bytes[i]));
                    }
                    p = new BoardPhoto { File = link };
                    photos.Add(p);
                    said.Append("Photo ").Append(link).Append(" copied from ").Append(Path.GetFileName(cp.Path)).Append(".\n");
                }
                p.Side = cp.Side;
                p.Version = cp.Version;
                p.Variant = cp.Variant;
                p.Caption = cp.Caption;
                p.Source = new DescriptionSource { Kind = SourceKind.Catalogue, Document = PcbCatalogue.DocumentName, Place = entry.Board.SourceUrl, Confidence = Confidence.Read, Quote = cp.Original == null ? null : "picture " + cp.Original + " on the board's page" };
            }

            // the switch text, as a further source on the blocks it names
            List<SwitchBlock>? switches = Root.Switches == null ? null : NetlistReader.Clone(Root.Switches);
            for (int i = 0; entry.Board.Switches != null && i < entry.Board.Switches.Count; i++)
            {
                string? text = entry.Board.Switches[i].Text;
                string? at = PcbCatalogue.SwitchPosition(text);
                SwitchBlock? block = null;
                for (int j = 0; at != null && switches != null && j < switches.Count; j++)
                {
                    if (string.Equals(switches[j].Ref, at, StringComparison.OrdinalIgnoreCase))
                        block = switches[j];
                }
                if (block == null)
                {
                    said.Append("Catalogue switch text \"").Append(text).Append("\" names no switch block of the sheet; kept in the board section only.\n");
                    continue;
                }
                block.MoreSources ??= new List<DescriptionSource>();
                for (int j = block.MoreSources.Count - 1; j >= 0; j--)
                {
                    if (block.MoreSources[j].Kind == SourceKind.Catalogue)
                        block.MoreSources.RemoveAt(j);
                }
                string written = text!.Substring(text.IndexOf("(position ", StringComparison.Ordinal) + 10);
                written = written.Substring(0, written.IndexOf(')'));
                block.MoreSources.Add(new DescriptionSource
                {
                    Kind = SourceKind.Catalogue,
                    Document = PcbCatalogue.DocumentName,
                    Place = entry.Board.SourceUrl,
                    Confidence = Confidence.Read,
                    Quote = text,
                    Note = "The catalogue writes the position row letter first: its " + written + " is our " + block.Ref + ".",
                });
                said.Append("Switch ").Append(block.Ref).Append(": the catalogue's text added as a source.\n");
            }

            // now change: files first (a failed write changes nothing in the sheet)
            for (int i = 0; i < toWrite.Count; i++)
            {
                if (File.Exists(toWrite[i].Path) && Same(File.ReadAllBytes(toWrite[i].Path), toWrite[i].Bytes))
                    continue;
                if (File.Exists(toWrite[i].Path))
                    throw new SheetEditException("A different file is already at " + toWrite[i].Path + "; nothing was imported.");
                Directory.CreateDirectory(Path.GetDirectoryName(toWrite[i].Path)!);
                File.WriteAllBytes(toWrite[i].Path, toWrite[i].Bytes);
            }
            Snapshot();
            for (int i = 0; i < toHold.Count; i++)
                Files.Put(toHold[i].Name, toHold[i].Bytes);
            BoardSection b = NetlistReader.Clone(entry.Board);
            b.Note = Root.Board?.Note;
            Root.Board = b;
            Root.Photos = photos.Count == 0 ? null : photos;
            Root.Switches = switches;
            Done();
            said.Insert(0, "Board facts imported from the catalogue entry " + (b.PartNumber ?? "?") + " (card " + (b.Number ?? "?") + ").\n");
            if (b.NdSystem == null && entry.CatalogueSystem != null)
                said.Append("The catalogue's machine \"").Append(entry.CatalogueSystem).Append("\" is not NORD-10 or ND-100, so ndSystem is left empty.\n");
            return said.ToString();
        }

        // the index of a photo with these bytes, or -1
        private int SamePhoto(List<BoardPhoto> photos, byte[] bytes)
        {
            for (int i = 0; i < photos.Count; i++)
            {
                byte[]? b = Files.Read(photos[i].File);
                if (b != null && Same(b, bytes))
                    return i;
            }
            return -1;
        }

        // where a .rsc's new photos go: beside the photos it has, else SHEETNAME-files/photos
        private static string PhotoFolder(List<BoardPhoto> photos, string sheetPath)
        {
            for (int i = 0; i < photos.Count; i++)
            {
                int slash = photos[i].File.LastIndexOf('/');
                if (slash > 0 && !photos[i].File.StartsWith("../", StringComparison.Ordinal))
                    return photos[i].File.Substring(0, slash);
            }
            return Path.GetFileNameWithoutExtension(sheetPath) + "-files/photos";
        }

        /// <summary>
        /// The file name a catalogue photo gets in the sheet: part number, side, print version
        /// and variant, such as "322049-component-side-I-new.jpg".
        /// </summary>
        /// <param name="entry">
        /// The board.
        /// </param>
        /// <param name="photo">
        /// The photo.
        /// </param>
        /// <returns>
        /// The name.
        /// </returns>
        public static string PhotoName(CatalogueEntry entry, CataloguePhoto photo)
        {
            StringBuilder sb = new();
            sb.Append(entry.PartNumber ?? entry.Number ?? "board");
            sb.Append(photo.Side == BoardSide.Component ? "-component-side" : "-solder-side");
            if (photo.Version != null)
                sb.Append('-').Append(Safe(photo.Version));
            if (photo.Variant != null)
                sb.Append('-').Append(Safe(photo.Variant));
            sb.Append(Path.GetExtension(photo.Path).ToLowerInvariant());
            return sb.ToString();
        }

        // letters and digits kept, every run of anything else becomes one '-'
        private static string Safe(string text)
        {
            StringBuilder sb = new();
            bool dash = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsAsciiLetterOrDigit(c))
                {
                    sb.Append(c);
                    dash = false;
                }
                else if (!dash && sb.Length > 0)
                {
                    sb.Append('-');
                    dash = true;
                }
            }
            return sb.ToString().TrimEnd('-');
        }
    }
}
