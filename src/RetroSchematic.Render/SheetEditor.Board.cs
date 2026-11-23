//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Board facts: the board section, connectors, source pages, photos and switches
    /// (docs/FORMAT.md, "Board, sources, photos, switches"). They belong to the whole sheet, so
    /// they are kept on the top level (<see cref="Root"/>) whatever level is shown. Every change
    /// needs edit mode and can be undone, like any other change.
    /// </summary>
    public sealed partial class SheetEditor
    {
        /// <summary>
        /// Sets the board section, or null to remove it.
        /// </summary>
        /// <param name="board">
        /// The values.
        /// </param>
        public void SetBoard(BoardSection? board)
        {
            CheckWritable();
            Snapshot();
            Root.Board = board;
            Done();
        }

        /// <summary>
        /// Adds a connector entry, or replaces the one with the same <see cref="ConnectorUse.Ref"/>.
        /// A replaced entry keeps its hand-placed box position and pin side when the new one has
        /// none.
        /// </summary>
        /// <param name="use">
        /// The connector.
        /// </param>
        public void SetConnector(ConnectorUse use)
        {
            CheckWritable();
            if (!ConnectorPins.IsConnector(use.Ref))
                throw new SheetEditException("A connector is named P or F, or P.A, F.B ... with its letter; \"" + use.Ref + "\" is not.");
            Snapshot();
            Root.Connectors ??= new List<ConnectorUse>();
            int at = IndexOfConnector(use.Ref);
            if (at < 0)
            {
                Root.Connectors.Add(use);
            }
            else
            {
                use.Position ??= Root.Connectors[at].Position;
                use.PinSide ??= Root.Connectors[at].PinSide;
                Root.Connectors[at] = use;
            }
            Done();
        }

        /// <summary>
        /// Removes a connector entry. The pins the signals use are still drawn (with no catalogue
        /// text); only the description goes.
        /// </summary>
        /// <param name="reference">
        /// The connector, such as P.C.
        /// </param>
        public void RemoveConnector(string reference)
        {
            CheckWritable();
            if (IndexOfConnector(reference) < 0)
                throw new SheetEditException("There is no connector entry " + reference + ".");
            Snapshot();
            Root.Connectors!.RemoveAt(IndexOfConnector(reference));
            if (Root.Connectors.Count == 0)
                Root.Connectors = null;
            Done();
        }

        /// <summary>
        /// Adds a source page, or replaces the one at an index.
        /// </summary>
        /// <param name="page">
        /// The page; its file must be readable (held, or on disk next to the sheet).
        /// </param>
        /// <param name="index">
        /// The entry to replace, or -1 to add at the end.
        /// </param>
        public void SetSource(SourcePage page, int index = -1)
        {
            CheckWritable();
            CheckFile(page.File);
            if (SheetAttachments.IsPdf(page.File) && page.PdfPage == null)
                throw new SheetEditException("A PDF source needs its page number.");
            Snapshot();
            Root.Sources ??= new List<SourcePage>();
            Put(Root.Sources, page, index);
            Done();
        }

        /// <summary>
        /// Removes a source page.
        /// </summary>
        /// <param name="index">
        /// Its place in the list, 0 first.
        /// </param>
        public void RemoveSource(int index)
        {
            CheckWritable();
            if (Root.Sources == null || index < 0 || index >= Root.Sources.Count)
                throw new SheetEditException("There is no source page " + (index + 1) + ".");
            Snapshot();
            Root.Sources.RemoveAt(index);
            if (Root.Sources.Count == 0)
                Root.Sources = null;
            Done();
        }

        /// <summary>
        /// Adds a board photo, or replaces the one at an index.
        /// </summary>
        /// <param name="photo">
        /// The photo; its file must be readable.
        /// </param>
        /// <param name="index">
        /// The entry to replace, or -1 to add at the end.
        /// </param>
        public void SetPhoto(BoardPhoto photo, int index = -1)
        {
            CheckWritable();
            CheckFile(photo.File);
            if (SheetAttachments.IsPdf(photo.File))
                throw new SheetEditException("A photo must be a picture (PNG or JPEG), not a PDF.");
            Snapshot();
            Root.Photos ??= new List<BoardPhoto>();
            Put(Root.Photos, photo, index);
            Done();
        }

        /// <summary>
        /// Removes a board photo.
        /// </summary>
        /// <param name="index">
        /// Its place in the list, 0 first.
        /// </param>
        public void RemovePhoto(int index)
        {
            CheckWritable();
            if (Root.Photos == null || index < 0 || index >= Root.Photos.Count)
                throw new SheetEditException("There is no photo " + (index + 1) + ".");
            Snapshot();
            Root.Photos.RemoveAt(index);
            if (Root.Photos.Count == 0)
                Root.Photos = null;
            Done();
        }

        /// <summary>
        /// Adds a switch block, or replaces the one at the same board position.
        /// </summary>
        /// <param name="block">
        /// The switch block.
        /// </param>
        public void SetSwitch(SwitchBlock block)
        {
            CheckWritable();
            if (string.IsNullOrWhiteSpace(block.Ref))
                throw new SheetEditException("A switch block needs its board position, such as 15E.");
            if (block.LeverCount < 1)
                throw new SheetEditException("A switch block has at least one lever.");
            if (block.OnMeans != null && block.OnMeans != "0" && block.OnMeans != "1" && block.OnMeans != "open")
                throw new SheetEditException("\"On means\" is 0, 1 or open, not \"" + block.OnMeans + "\".");
            for (int i = 0; i < block.Levers.Count; i++)
            {
                SwitchLever l = block.Levers[i];
                if (l.Number < 1 || l.Number > block.LeverCount)
                    throw new SheetEditException("Lever " + l.Number + " is not on a switch with " + block.LeverCount + " levers.");
                if (l.Setting != null && l.Setting != "on" && l.Setting != "off" && l.Setting != "open")
                    throw new SheetEditException("A lever is on, off or open, not \"" + l.Setting + "\".");
            }
            Snapshot();
            Root.Switches ??= new List<SwitchBlock>();
            int at = IndexOfSwitch(block.Ref);
            if (at < 0)
                Root.Switches.Add(block);
            else
                Root.Switches[at] = block;
            Done();
        }

        /// <summary>
        /// Removes a switch block.
        /// </summary>
        /// <param name="reference">
        /// Its board position.
        /// </param>
        public void RemoveSwitch(string reference)
        {
            CheckWritable();
            if (IndexOfSwitch(reference) < 0)
                throw new SheetEditException("There is no switch block " + reference + ".");
            Snapshot();
            Root.Switches!.RemoveAt(IndexOfSwitch(reference));
            if (Root.Switches.Count == 0)
                Root.Switches = null;
            Done();
        }

        /// <summary>
        /// The switch block at a board position, or null.
        /// </summary>
        /// <param name="reference">
        /// The board position, such as 15E.
        /// </param>
        /// <returns>
        /// The block, or null.
        /// </returns>
        public SwitchBlock? FindSwitch(string reference)
        {
            int at = IndexOfSwitch(reference);
            return at < 0 ? null : Root.Switches![at];
        }

        /// <summary>
        /// Brings a file into the sheet and gives the link to name it by. With
        /// <paramref name="hold"/> (a .rsx, or a sheet not saved yet) the bytes are kept in
        /// <see cref="Files"/> as "sources/NAME" or "photos/NAME", and they go into the zip on
        /// the next save. Without it (a .rsc) the file stays where it is and the link is its path
        /// relative to the sheet. This does not change the sheet: use the link in
        /// <see cref="SetSource"/> or <see cref="SetPhoto"/>.
        /// </summary>
        /// <param name="path">
        /// The file on disk.
        /// </param>
        /// <param name="folder">
        /// "sources" or "photos".
        /// </param>
        /// <param name="hold">
        /// True to keep the bytes in the sheet.
        /// </param>
        /// <returns>
        /// The link.
        /// </returns>
        public string ImportFile(string path, string folder, bool hold)
        {
            if (!File.Exists(path))
                throw new SheetEditException("There is no file " + path + ".");
            if (!hold && Files.Folder != null)
                return Path.GetRelativePath(Files.Folder, Path.GetFullPath(path)).Replace('\\', '/');
            string name = folder + "/" + Path.GetFileName(path);
            byte[] bytes = File.ReadAllBytes(path);
            // the same name with other bytes gets -2, -3 ...
            string stem = name.Substring(0, name.Length - Path.GetExtension(name).Length);
            for (int n = 2; Files.Holds(name) && !Same(Files.Read(name)!, bytes); n++)
                name = stem + "-" + n.ToString(System.Globalization.CultureInfo.InvariantCulture) + Path.GetExtension(path);
            Files.Put(name, bytes);
            return name;
        }

        private static bool Same(byte[] a, byte[] b) => a.AsSpan().SequenceEqual(b);

        private void CheckFile(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new SheetEditException("No file is named.");
            if (Files.Read(file) == null)
                throw new SheetEditException("The file " + file + " cannot be found" + (Files.PathOf(file) is string p ? " (looked at " + p + ")" : "") + ".");
        }

        private static void Put<T>(List<T> list, T item, int index)
        {
            if (index < 0)
            {
                list.Add(item);
                return;
            }
            if (index >= list.Count)
                throw new SheetEditException("There is no entry " + (index + 1) + " to replace.");
            list[index] = item;
        }

        private int IndexOfConnector(string reference)
        {
            for (int i = 0; Root.Connectors != null && i < Root.Connectors.Count; i++)
            {
                if (Root.Connectors[i].Ref == reference)
                    return i;
            }
            return -1;
        }

        private int IndexOfSwitch(string reference)
        {
            for (int i = 0; Root.Switches != null && i < Root.Switches.Count; i++)
            {
                if (string.Equals(Root.Switches[i].Ref, reference, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }
    }
}
