//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// A card's reading files written into the sheet (docs/3207-readings/README.md): the C# side
    /// of <c>card-readings.py sheet</c>, so the MCP server and the command-line tool do it through
    /// the editor, as one undoable change, and the file is written only by <see cref="Save"/>
    /// (which raises the file stamp).
    /// </summary>
    public sealed partial class SheetEditor
    {
        /// <summary>
        /// Writes every block of a checked reading set that is drawn as a box on another reading's
        /// sheet into the sheet: parts, gates, nets, pages, ports, and its instance in the parent
        /// block (see <see cref="CardReadings.WriteInto"/>). One undo step. The result is read back
        /// and checked first, so a reading set the reader would refuse changes nothing. Problems
        /// in the readings do not stop it (as in the script): they are the caller's to show.
        /// </summary>
        /// <param name="set">
        /// The readings, from <see cref="CardReadings.Load"/>.
        /// </param>
        /// <returns>
        /// The names of the blocks written.
        /// </returns>
        /// <exception cref="SheetEditException">
        /// Read-only, or the result would not be a valid sheet; the sheet is left as it was.
        /// </exception>
        public List<string> ApplyReadings(ReadingSet set)
        {
            CheckWritable();
            SyncBack();
            // on a copy first: a refusal leaves the sheet exactly as it was
            Netlist trial = NetlistReader.Parse(NetlistReader.Write(Root));
            List<string> written;
            try
            {
                written = CardReadings.WriteInto(trial, set, Catalog);
                NetlistReader.Parse(NetlistReader.Write(trial));
            }
            catch (ReadingException e)
            {
                throw new SheetEditException(e.Message, e);
            }
            catch (NetlistException e)
            {
                throw new SheetEditException("The readings would not make a valid sheet, so nothing was changed: " + e.Message, e);
            }
            Snapshot();
            Root = trial;
            ShowLevel();
            Done();
            return written;
        }
    }
}
