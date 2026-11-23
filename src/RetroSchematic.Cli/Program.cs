//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.IO;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Cli
{
    /// <summary>
    /// retroschematic: check and render netlist files.
    /// </summary>
    public static class Program
    {
        private const string Help =
            "retroschematic check <sheet.json>                          check a netlist file\n" +
            "retroschematic render <sheet.json> <out.svg> [nd|modern]   draw it as SVG (default: nd symbols)\n";

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">
        /// The command (check or render) and its file names.
        /// </param>
        /// <returns>
        /// 0 when the command worked, 1 when the sheet file is not valid or cannot be read, 2 for a wrong command line.
        /// </returns>
        public static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.Write(Help);
                return 2;
            }

            try
            {
                Netlist netlist = NetlistReader.ReadFile(args[1]);
                switch (args[0])
                {
                    case "check":
                        Console.WriteLine($"{args[1]}: format version {netlist.FormatVersion}, {netlist.Parts.Count} parts, {netlist.Gates.Count} gates, {netlist.Nets.Count} nets - OK");
                        return 0;

                    case "render":
                        if (args.Length < 3)
                        {
                            Console.Error.Write(Help);
                            return 2;
                        }
                        SymbolStyle style = args.Length > 3 && args[3] == "modern" ? SymbolStyle.Modern : SymbolStyle.NorskData;
                        if (args.Length > 3 && args[3] != "modern" && args[3] != "nd")
                        {
                            Console.Error.WriteLine($"Unknown symbol style \"{args[3]}\": use nd or modern.");
                            return 2;
                        }
                        // drawn with the chip catalogue when one is found above the sheet, exactly as the
                        // window draws it (a chip without gates then shows every pin)
                        string? catalog = ChipCatalog.Find(args[1]);
                        LayoutOptions options = new() { Catalog = catalog == null ? null : new ChipCatalog(catalog) };
                        SchematicDrawing drawing = SheetLayout.Build(netlist, style, options);
                        File.WriteAllText(args[2], SvgRenderer.Write(drawing));
                        Console.WriteLine($"Wrote {args[2]}");
                        for (int i = 0; i < drawing.RerouteReasons.Count; i++)
                            Console.WriteLine("Wire routed afresh: " + drawing.RerouteReasons[i]);
                        for (int i = 0; i < drawing.BusProblems.Count; i++)
                            Console.WriteLine("Bus: " + drawing.BusProblems[i]);
                        return 0;

                    default:
                        Console.Error.Write(Help);
                        return 2;
                }
            }
            catch (NetlistException e)
            {
                Console.Error.WriteLine($"{args[1]}: {e.Message}");
                return 1;
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e.Message);
                return 1;
            }
        }
    }
}
