# MCP coverage

What the window, the command-line tool and `tools/card-readings.py` can do, and the MCP tool that
does the same through the server (`RetroSchematic.Mcp`). Written 2026-11-28.

## How it is built

- The MCP tool classes (`RetroSchematic.Mcp/SheetTools*.cs`) only read their arguments, call one
  shared helper, and turn the answer into text. A refused change comes back as a tool error that
  says why; nothing is swallowed.
- Every change to a sheet goes through `SheetEditor` (the window's editor): one undo step each,
  refused in read-only mode. The file is only ever written by `SheetEditor.Save`, which raises the
  file stamp (`file.revision` + 1, `file.modified` in UTC). No tool writes sheet JSON any other way.
- The lookups that the window's side panel, search box and trace tool give (`describe`, `search`,
  `trace_signal`, `get_part_info`) run the window's own `SheetSession` over a copy of the open
  sheet, on the level shown, so the answer is the same text the window shows and the session's
  page and trace state never touch the sheet being edited.

### Shared helpers, and who calls them

| Helper | What it does | Called by |
|---|---|---|
| `Model/CardReadings.cs` | reads, merges per block, checks and writes a card's reading files (the C# port of `card-readings.py`) | MCP `check_readings`, `import_readings`; CLI `readings check`, `readings sheet` |
| `Render/SheetEditor.Readings.cs` (`ApplyReadings`) | a checked reading set written into the sheet as one undo step | MCP `import_readings`; CLI `readings sheet` |
| `Model/SheetCheck.cs` | checks a whole sheet: the reader, chip pins against the catalogue, supply pins, missing chip files, gate pins on no signal | MCP `check_sheet`, `missing_chips`; CLI `check` |
| `Render/SheetRender.cs` | draws a level as SVG in the card's page layout | MCP `export_svg`; CLI `render` |
| `Render/PageStats.cs` (`ReportBoth`) | measures both page layouts, a card without pages on a copy | MCP `page_stats`; CLI `pages` |
| `Model/CardLibrary.cs` (`FolderBeside`) | the library folder beside the chip catalogue | Card library window, Bus use window, MCP `list_cards`, `list_families`, `bus_use` |
| `Model/BusUse.cs` (`OfLibrary`) | the backplane signals of every card, the open sheet standing in for its file | Bus use window, MCP `bus_use`, `bus_use_report` |
| `Model/ChipCatalog.cs` (`Get`, `PlainType`, `SchottkyType`) | the 74 family rule: own file, else the plain 74 file, else the 74S file of the same number | the window, the reading check, every MCP tool that reads a chip |
| `Render/SheetEditor.Edits.cs` | rename a part, set a part's facts, add and remove a gate, add a signal, rename a signal, set the title block | MCP edit tools |
| `Render/SheetEditor.Structure.cs` | a new empty block, its ports, instances, port connections | MCP structure tools |
| `Render/SheetSession.Trace.cs` (`TracedOwners`) | what a trace lights up, as a list | MCP `trace_signal` |

The 74 family rule gained one step on 2026-11-28: when neither the type nor its plain 74 type has
a file, the Schottky 74S file of the same number is used (74LS112 and 74F112 read `74S112.json`).
That is the rule `card-readings.py` already checked the readings with; the window now finds the
same pins, and `ReadFrom` still says which file they come from.

## The window

| Window function | MCP tool |
|---|---|
| File > New sheet (NORD-10 card, ND-100 card, bus not known) | `new_sheet` |
| File > Open sheet | `open_sheet` (read-only unless `edit`) |
| File > Recent sheets | not a tool: it is the window user's own list of files. `list_cards` gives every card file |
| File > Card library (families, cards, open one) | `list_families`, `list_cards`, then `open_sheet` |
| File > Open the demo sheet | `open_sheet` with `sheets/demo/demo-gates.rsc` |
| File > Save sheet, Save sheet as (.rsc or .rsx) | `save_sheet` |
| File > Convert to a card with pages | `convert_to_pages` |
| File > Add a page | `add_page` |
| File > Put the picked part on page | `set_part_page` |
| File > Bus use, all cards (list, chips, Markdown report) | `bus_use`, `bus_use_report` |
| File > Board info (board facts, connectors, sources, photos) | `get_board_info`, `set_board_info`, `set_connector`, `remove_connector`, `list_sources`, `add_source`, `remove_source`, `add_photo`, `remove_photo` |
| Board info: switch blocks | `get_switches`, `set_switch`, `remove_switch` |
| File > Import board info from the ND PCB catalogue | `import_board_from_catalogue` |
| File > Save as SVG | `export_svg` |
| File > Export PNG | MISSING: the PNG is drawn by the window's own drawing engine, which the server does not have. `export_svg` gives the same drawing |
| File > Open .rsc/.rsx files with RetroSchematic (and stop) | not a tool: a Windows setting for the desktop program |
| Edit > Edit mode | `set_edit_mode` |
| Edit > Undo, Redo | `undo`, `redo` |
| Edit > Delete picked item | `delete` (part or signal) |
| Edit > Fix the picked signal's wire path | `fix_wire`, `set_wire_path` |
| Edit > Route the picked signal automatically again | `clear_wire_path` |
| Edit > New bus (name, members, path) | `create_bus`, `set_bus_path`, `add_bus_member`, `remove_bus_member`, `delete_bus`, `list_buses` |
| Edit > Make block from the picked parts (and port names) | `make_block` |
| Edit > Up one level; double-click an instance | `leave_block`; `enter_block` |
| Block menu: dissolve an instance | `dissolve_block` |
| Block menu: put a port on an edge, move a port earlier or later | `set_port_position` |
| Drag an instance; drag parts onto an instance | `move_instance`; `move_into_block` |
| Drag a chip; drag a connector box | `move_part`; `move_connector` |
| View > Back, Forward | not a tool: the window's own history of where the user looked. `enter_block`, `leave_block` and `set_page` say where the tools work |
| View > ND 1976 symbols, Modern symbols | `export_svg` (`symbols`), `set_display` (`symbols`, saved with the sheet) |
| View > Show pin numbers, board positions, port arrows, buses | `set_display` (saved with the sheet); `export_svg` (`buses`) |
| View > Settings (the sheet's own display values) | `set_display`; machine bus: `set_bus` |
| View > Fit to window, zoom | not a tool: screen only |
| View > Card overview | `list_pages`, `list_sources` |
| View > Next page, Previous page | `set_page`, `get_page` |
| View > Split crowded pages automatically | `set_page_layout` |
| View > Drawing, Original page, PCB front, PCB back | `get_page`, `list_sources`, `get_pcb_grid` (the pictures themselves are files the sheet names) |
| View > Chip boxes on the photos | `get_pcb_grid`, `set_chip_box` |
| View > Board corners on the photos | `get_pcb_grid`, `set_photo_corners` |
| View > Edge fingers on the photos | `get_edge_fingers` |
| View > Theme | `export_svg` (`theme`), `set_display` (`theme`) |
| Help > About | the server's name and build at the MCP handshake (`retroschematic-mcp`, version and commit) |
| Toolbar: Select (pick an item, side panel text) | `describe`, `get_part_info`, `get_signal_info` |
| Toolbar: Place chip (and the chip type box) | `place_part`, `add_passive`; `list_chips`, `get_chip` |
| Toolbar: Wire (pin to pin) | `connect_pins`, `disconnect_pin` |
| Toolbar: Trace | `trace_signal` |
| Toolbar: Run, Pause, Step, Reset | not built in the window yet |
| Side panel: search box, Found list | `search` |
| Side panel: Parts and Signals lists | `list_parts`, `list_nets` |
| Side panel: signal description, its source and how sure | `get_signal_info`, `set_signal_description` |
| Side panel: bus members (add, remove, redraw, delete) | `add_bus_member`, `remove_bus_member`, `set_bus_path`, `delete_bus` |
| Original page panel: Analyse page, accept a find | `analyse_page`, `accept_find`, `set_symbol_box` |
| Original page panel: wire assist (connect a wire end to a pin) | `connect_pin_on_page` |
| Original page panel: keep the page picked by hand | not a tool: screen only |

## The command-line tool

| Command | MCP tool |
|---|---|
| `--version` | the server's name and build at the MCP handshake |
| `check <sheet>` | `check_sheet` (same `SheetCheck`; the command now also fails on a problem) |
| `stats <sheet>` | `page_stats` (wire length, pieces and crossings per drawn page) |
| `pack <sheet.rsc> <sheet.rsx>` | `open_sheet`, then `save_sheet` with a `.rsx` path |
| `import-board <sheet> <pcb.json> [number]` | `import_board_from_catalogue`, then `save_sheet` |
| `render <sheet> <out.svg> ...` | `export_svg` (same `SheetRender`) |
| `to-pages <sheet> [out]` | `convert_to_pages`, then `save_sheet` |
| `pages <sheet> [--limit n]` | `page_stats` (same `PageStats.ReportBoth`) |
| `readings check <folder>` (new) | `check_readings` |
| `readings sheet <folder> <sheet>` (new) | `import_readings`, then `save_sheet` |

## tools/card-readings.py

The script is kept and still works. Its rules now also live in `Model/CardReadings.cs`.

| Script | MCP tool |
|---|---|
| `check FOLDER` (read, merge per block, check, report) | `check_readings`; CLI `readings check` |
| `sheet FOLDER SHEET` (write every read block into the sheet) | `import_readings` then `save_sheet`; CLI `readings sheet` |
| `json FOLDER OUT` (dump the merged blocks, with the part boxes, as JSON) | MISSING: an in-between dump, used to make training keys from the part boxes. Nothing else reads it, so it was not ported |
| `part`, `gate`, `net`, `port`, `connector`, `open`, `nc`, `note` lines | read by `CardReadings.Parse`; written by `CardReadings.WriteInto` |

Measured on card 3207 (2026-11-28): `readings check` and the script print the same block lines and
the same 0 problems. One difference in the list of chips with no file: the script lists `STRAP`,
the C# check does not, because the catalogue knows a strap as a passive part with pins 1 and 2.
Written into the sheet in memory, the parts, gates, signals, pages, instances, port names and port
signals are the same as the ones in `sheets/nd-100/3207-scsi-floppy.rsc`; the port directions
follow the chip catalogue as it is now (more chip files than when the file was last written).

## Tools that only the MCP server has

The window shows these by pointing; the server needs them as words or as single steps:

| Tool | What it does |
|---|---|
| `get_sheet_info` | title block, file stamp, edit mode, level and counts in short |
| `rename_part`, `set_part` | a part's name and facts (type, function, how sure, note, value) |
| `add_gate`, `remove_gate` | a gate of a part, named after its output pin |
| `add_net`, `set_net` | a signal with no pins yet; rename a signal or set its facts |
| `set_sheet_info` | the sheet's title block |
| `set_page_info` | a page's title, source page and sheet number |
| `create_block`, `set_block_port`, `remove_block_port` | a block built by hand, as a reading gives it |
| `add_instance`, `remove_instance`, `connect_port` | instances and what their ports are connected to |
| `missing_chips` | the part types with no chip file, with the parts that use them |
