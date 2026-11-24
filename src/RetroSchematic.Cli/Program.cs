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
            "retroschematic --version                                  show the build: version, commit, branch, build date and time\n" +
            "retroschematic check <sheet.rsc>                          check a netlist file\n" +
            "retroschematic pack <sheet.rsc> <sheet.rsx>                save as one zipped file with every source page and photo inside\n" +
            "retroschematic import-board <sheet> <pcb.json> [number]     fill the board info from the ND PCB catalogue (part or card number;\n" +
            "    default the sheet's own) and save the sheet; the photos are copied in\n" +
            "retroschematic render <sheet.rsc> <out.svg> [nd|modern] [--theme <name>] [--no-buses] [--names slash|tilde|subscript]\n" +
            "    [--page <id>|all] [--split] [--as-original]\n" +
            "    draw it as SVG (default: nd symbols, the Light theme, buses drawn, /NAME for active low; a card with\n" +
            "    pages: the first page in the card's own page layout; all writes out-<page>.svg for every page)\n" +
            "retroschematic to-pages <sheet> [out]                     turn a version 1 or 2 file into format version 3 with one page\n" +
            "    holding every part (written back to the same file unless out is given; .rsc or .rsx)\n" +
            "retroschematic pages <sheet> [--limit n]                  list the drawn pages and measure both page layouts: pages,\n" +
            "    wire length, crossings, labels and off-page markers\n";

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
            // the build stamp from Directory.Build.props (see RetroSchematic.Model/BuildIdentity.cs)
            if (args.Length == 1 && (args[0] == "--version" || args[0] == "version"))
            {
                Console.WriteLine(BuildIdentity.Describe("retroschematic"));
                return 0;
            }

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

                    case "stats":
                    {
                        string? cat = ChipCatalog.FindForSheet(args[1]);
                        SchematicDrawing measured = SheetLayout.Build(netlist, SymbolStyle.NorskData, new LayoutOptions { Catalog = cat == null ? null : new ChipCatalog(cat) });
                        WireStats st = WireStats.Measure(measured);
                        Console.WriteLine($"{args[1]}: wire length {st.Length / WireRouter.Step:0} grid steps in {st.Pieces} pieces, {st.Crossings} crossings");
                        return 0;
                    }

                    case "pack":
                    {
                        if (args.Length < 3)
                        {
                            Console.Error.Write(Help);
                            return 2;
                        }
                        // the same save as the window's Save As: a .rsx stores the linked files
                        // inside the zip, a PDF source as the PNG of its page at 300 dpi
                        SheetEditor packer = new(netlist, null) { Files = SheetAttachments.Load(args[1]) };
                        packer.Save(args[2]);
                        Console.WriteLine($"Wrote {args[2]}");
                        return 0;
                    }

                    case "import-board":
                    {
                        if (args.Length < 3)
                        {
                            Console.Error.Write(Help);
                            return 2;
                        }
                        // the same import as File > Import board info from the ND PCB
                        // catalogue... in the window, saved back into the same sheet file
                        PcbCatalogue catalogue = PcbCatalogue.Load(args[2]);
                        string key = args.Length > 3 ? args[3] : netlist.Board?.PartNumber ?? netlist.Board?.Number ?? "";
                        System.Collections.Generic.List<CatalogueEntry> found = catalogue.Find(key);
                        if (found.Count != 1)
                        {
                            Console.Error.WriteLine(found.Count == 0 ? $"No board \"{key}\" in the catalogue." : $"\"{key}\" matches {found.Count} boards; give the part number.");
                            return 1;
                        }
                        SheetEditor importer = new(netlist, null) { Files = SheetAttachments.Load(args[1]), ReadOnly = false };
                        Console.Write(importer.ImportFromCatalogue(found[0], args[1]));
                        importer.Save(args[1]);
                        Console.WriteLine($"Wrote {args[1]}");
                        return 0;
                    }

                    case "to-pages":
                    {
                        // the same change as the MCP tool convert_to_pages: one page with every part
                        string target = args.Length > 2 ? args[2] : args[1];
                        bool changed = CardPages.ToVersion3(netlist);
                        SheetEditor converter = new(netlist, null) { Files = SheetAttachments.Load(args[1]) };
                        converter.Save(target);
                        Console.WriteLine(changed ? $"Wrote {target}: format version 3, one page." : $"{args[1]} has pages already; wrote {target} unchanged.");
                        return 0;
                    }

                    case "pages":
                    {
                        int limit = PageSet.DefaultChipLimit;
                        for (int a = 2; a + 1 < args.Length; a++)
                        {
                            if (args[a] == "--limit")
                                limit = int.Parse(args[++a], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        // a file without pages is measured as if converted (nothing is written)
                        if (!CardPages.HasPages(netlist))
                            CardPages.ToVersion3(netlist);
                        string? cat = ChipCatalog.FindForSheet(args[1]);
                        ChipCatalog? chips = cat == null ? null : new ChipCatalog(cat);
                        Console.Write(PageStats.Report(netlist, chips, PageLayoutMode.AsOriginal, limit));
                        Console.Write(PageStats.Report(netlist, chips, PageLayoutMode.Split, limit));
                        return 0;
                    }

                    case "render":
                        if (args.Length < 3)
                        {
                            Console.Error.Write(Help);
                            return 2;
                        }
                        SymbolStyle style = SymbolStyle.NorskData;
                        SchematicTheme theme = SchematicTheme.Default;
                        bool buses = true;
                        SignalNameStyle names = SignalNames.Default;
                        string? pageId = null;
                        PageLayoutMode? mode = null;
                        for (int a = 3; a < args.Length; a++)
                        {
                            if (args[a] == "--page" && a + 1 < args.Length)
                                pageId = args[++a];
                            else if (args[a] == "--split")
                                mode = PageLayoutMode.Split;
                            else if (args[a] == "--as-original")
                                mode = PageLayoutMode.AsOriginal;
                            else if (args[a] == "--theme" && a + 1 < args.Length)
                            {
                                SchematicTheme? t = SchematicTheme.Find(args[++a]);
                                if (t == null)
                                {
                                    Console.Error.WriteLine($"Unknown theme \"{args[a]}\": use one of {ThemeNames()}.");
                                    return 2;
                                }
                                theme = t;
                            }
                            else if (args[a] == "--names" && a + 1 < args.Length)
                            {
                                if (!SignalNames.TryParseStyle(args[++a], out names))
                                {
                                    Console.Error.WriteLine($"Unknown name style \"{args[a]}\": use slash, tilde or subscript.");
                                    return 2;
                                }
                            }
                            else if (args[a] == "--no-buses")
                                buses = false;
                            else if (args[a] == "modern")
                                style = SymbolStyle.Modern;
                            else if (args[a] != "nd")
                            {
                                Console.Error.WriteLine($"Unknown option \"{args[a]}\": use nd, modern, --theme <name>, --no-buses or --names <style>.");
                                return 2;
                            }
                        }
                        // drawn with the chip catalogue found above the sheet, else beside this program, exactly as the
                        // window draws it (a chip without gates then shows every pin)
                        string? catalog = ChipCatalog.FindForSheet(args[1]);
                        LayoutOptions options = new() { Catalog = catalog == null ? null : new ChipCatalog(catalog), ShowBuses = buses, NameStyle = names };
                        if (CardPages.HasPages(netlist))
                            options.Pages = PageSet.Of(netlist, mode ?? netlist.PageLayout ?? PageLayoutMode.AsOriginal, PageSet.DefaultChipLimit);
                        int first = 0, last = 0;
                        if (options.Pages != null && pageId == "all")
                            last = options.Pages.Pages.Count - 1;
                        else if (options.Pages != null && pageId != null)
                        {
                            first = last = options.Pages.IndexOf(pageId);
                            if (first < 0)
                            {
                                Console.Error.WriteLine($"No drawn page \"{pageId}\".");
                                return 2;
                            }
                        }
                        for (int pg = first; pg <= last; pg++)
                        {
                            options.Page = pg;
                            // "all": one file per drawn page, named after the page
                            string outPath = pageId == "all"
                                ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[2]))!, Path.GetFileNameWithoutExtension(args[2]) + "-" + options.Pages!.Pages[pg].Id + ".svg")
                                : args[2];
                            SchematicDrawing drawing = SheetLayout.Build(netlist, style, options);
                            File.WriteAllText(outPath, SvgRenderer.Write(drawing, theme));
                            Console.WriteLine($"Wrote {outPath}");
                            for (int i = 0; i < drawing.RerouteReasons.Count; i++)
                                Console.WriteLine("Wire routed afresh: " + drawing.RerouteReasons[i]);
                            for (int i = 0; i < drawing.BusProblems.Count; i++)
                                Console.WriteLine("Bus: " + drawing.BusProblems[i]);
                        }
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

        /// <summary>
        /// The theme names, for the error message.
        /// </summary>
        /// <returns>
        /// The names, separated by commas.
        /// </returns>
        private static string ThemeNames()
        {
            string names = "";
            for (int i = 0; i < SchematicTheme.All.Count; i++)
                names += (i == 0 ? "" : ", ") + SchematicTheme.All[i].Id;
            return names;
        }
    }
}
