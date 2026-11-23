//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Mcp
{
    /// <summary>
    /// The signal-facts tools: read everything known about a signal, and write its description.
    /// </summary>
    public sealed partial class SheetTools
    {
        /// <summary>
        /// Everything known about a signal.
        /// </summary>
        /// <param name="net">
        /// The signal's key, such as IOXE_0.
        /// </param>
        /// <returns>
        /// The facts, one per line.
        /// </returns>
        [McpServerTool(Name = "get_signal_info"), Description("Everything known about a signal on the current level: its description and where that came from (the sheet's own, else the bus catalogue for the sheet's bus), notes, the drive type worked out from the chip catalogue (a warning for mixed drivers, and a CONFLICT line when the file's stored drive disagrees), driving pins, loads, pull-up, bus, other levels it reaches, and every pin with chip type, pin name and role. Allowed read-only.")]
        public string GetSignalInfo([Description("Signal key, e.g. IOXE_0")] string net)
        {
            SheetEditor e = Open();
            Net n = e.FindNetByKey(net) ?? throw new McpException("There is no signal " + net + " on this level.");
            BusCatalog bus = BusCatalog.Load(e.Catalog?.Folder, e.Root.Sheet.Bus ?? MachineBus.Unknown);
            return SignalInfo.Of(e, n, SignalNames.Default, bus).Details();
        }

        /// <summary>
        /// Sets a signal's description and where it comes from.
        /// </summary>
        /// <param name="net">
        /// The signal's key.
        /// </param>
        /// <param name="description">
        /// What it does; empty removes it.
        /// </param>
        /// <param name="sourceKind">
        /// drawing, manual, reading or inferred.
        /// </param>
        /// <param name="document">
        /// The document, or empty.
        /// </param>
        /// <param name="place">
        /// The place in it, or empty.
        /// </param>
        /// <param name="confidence">
        /// read, inferred or open.
        /// </param>
        /// <returns>
        /// What was done.
        /// </returns>
        [McpServerTool(Name = "set_signal_description"), Description("Set what a signal does and where that statement comes from (edit mode; undoable). Never invent one: give the document and place it was read from. A description typed here always wins over the bus catalogue's. The signal's notes are kept.")]
        public string SetSignalDescription(
            [Description("Signal key, e.g. IOXE_0")] string net,
            [Description("What the signal does; empty removes the description")] string description,
            [Description("Where it comes from: drawing, manual, reading or inferred")] string sourceKind,
            [Description("The document, e.g. docs/2A50-reading.md")] string? document = null,
            [Description("The place in it: section, page")] string? place = null,
            [Description("How sure: read, inferred or open")] string confidence = "read")
            => Run(e =>
            {
                SourceKind kind = sourceKind.Trim().ToLowerInvariant() switch
                {
                    "drawing" => SourceKind.Drawing,
                    "manual" => SourceKind.Manual,
                    "reading" => SourceKind.Reading,
                    "inferred" => SourceKind.Inferred,
                    _ => throw new McpException("sourceKind must be drawing, manual, reading or inferred."),
                };
                Confidence c = confidence.Trim().ToLowerInvariant() switch
                {
                    "read" => Confidence.Read,
                    "inferred" => Confidence.Inferred,
                    "open" => Confidence.Open,
                    _ => throw new McpException("confidence must be read, inferred or open."),
                };
                Net n = e.FindNetByKey(net) ?? throw new SheetEditException("There is no signal " + net + " on this level.");
                e.SetNetInfo(net, description, new DescriptionSource { Kind = kind, Document = document, Place = place, Confidence = c }, n.Note);
                return string.IsNullOrWhiteSpace(description) ? "Removed the description of " + net + "." : "Set the description of " + net + ".";
            });
    }
}
