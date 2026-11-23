# RetroSchematic viewer and editor

A window that shows a sheet file the way `retroschematic render` writes it, and lets you click
on it: a part, a gate, a signal or a pin shows what it is in the side panel. In edit mode it
also places chips, draws wires and moves chips, and saves the sheet back to its JSON file. The drawing on screen
and the SVG file come from the same layout code (`RetroSchematic.Render/SheetLayout.cs`), so they
always match.

## Start it

```powershell
# from the src folder
dotnet run --project RetroSchematic.Desktop -- sheets\nord-10\2A50-sync-modem-buffer-1050.json
```

The program is `RetroSchematic.exe`. With no file named it opens the sheet that was open last time.
File > Open the demo sheet opens `sheets/demo/demo-gates.json`, a small made-up sheet (not a
reading of a real drawing) with every kind of connection in it.

## Using it

| Do this | To get |
|---|---|
| File > New sheet | an empty design, in edit mode |
| File > Open sheet | open another sheet file (`*.json`), read-only |
| File > Save sheet / Save sheet as | write the sheet as nd-netlist JSON, with every position |
| File > Save as SVG | the drawing as shown, as an SVG file |
| File > Export PNG | the whole sheet as a picture, twice the drawing size |
| Edit > Edit mode | switch between read-only (every change refused) and edit mode |
| Edit > Undo / Redo | step back and forward through changes |
| View > Show pin numbers / Show board positions | hide or show them |
| Search box | lists matching parts, signals and pins (write a pin as 11D.3) |
| View > ND 1976 symbols / Modern symbols | switch the symbol set; what is picked stays picked |
| View > Fit to window | the whole sheet in view |
| View > Theme | Light, Dark, Solarized Dark, Nord, Retro Green, Amber CRT, Synthwave '84, High Contrast |
| Mouse wheel | zoom round the pointer |
| Right or middle button drag | move the sheet |
| Left click | pick a part, gate, signal or pin; its details show on the right |
| Parts / Signals lists | pick by name |

Colours: what was READ off the drawing is in the theme's ink colour, anything INFERRED is in
its own colour, anything OPEN (not known) in another, and the picked item is in the theme's
selection colour. A wire is as sure as the less sure of the two pins it joins.

## Editing

Switch on Edit > Edit mode first; a sheet always opens read-only.

Blocks can be looked at in read-only mode too: double-click an instance box (or right-click >
Open) to see its inside; the bar above the drawing shows where you are (Top > U2
(read-multiplexers)) and goes back up, as does Up one level. View > Show port arrows (and its
toolbar button) draws an arrow on every port pin: into the box for an input, out for an output,
both ways for an inout; the choice is remembered.

| In edit mode | Does |
|---|---|
| Pick a chip in "Chip to place", click on the sheet | places it there, at the first free board position (1A, 2A, ...); Esc stops placing |
| Drag from one pin dot to another | connects the two pins: a new signal, or the free pin joins the other's signal |
| Drag a chip | moves it; only the drawing changes, not the signals |
| Delete | deletes the picked chip or signal, or takes the picked pin off its signal |
| Edit > New bus, or the Bus button | name a bus, tick its signals (a filter box, and a pattern box such as BD*_0 or IDATA0-7), OK, then click out its path on the sheet; click the last point again or press Enter to finish. Drawing only: connections never change |
| Pick a bus | the Bus members panel: add or remove a signal, redraw the path, delete the bus |
| Ctrl-click chips, or drag a box round them on empty paper | picks several chips and instances at once |
| Edit > Make block, the Make block button, or right-click > Make block | asks a name and shows the ports it will get (every signal that crosses the edge; numbered ones become one bus port), each name editable; the picked parts become one instance box. Connections never change |
| Drag a picked chip onto an instance box | moves it (and the rest of the group picked) inside that block; one undo step |
| Drag an instance box | moves it |
| Right-click an instance > Dissolve into this level | puts its inside back on this level |

A pin is on one signal only: joining two pins that are already on different signals is refused,
and the reason shows in the status bar. A chip's gates come from the plain-gate formulas in its
catalogue file; a chip without them (7474, counters) is drawn as a list of its pins.

All of this is done by `RetroSchematic.Render/SheetEditor.cs` (the changes) and `EditTool.cs`
(what a mouse press or drag means); the window only passes the mouse and keys on.

## MCP server

`RetroSchematic.Mcp` (`retroschematic-mcp.exe`) offers the same changes to an MCP client over
standard input and output: `new_sheet`, `open_sheet` (read-only unless `edit` is true),
`set_edit_mode`, `list_parts`, `list_nets`, `place_part`, `move_part`, `connect_pins`,
`disconnect_pin`, `delete`, `create_bus`, `set_bus_path`, `add_bus_member`, `remove_bus_member`, `delete_bus`, `list_buses`, `make_block`, `dissolve_block`, `enter_block`, `leave_block`,
`list_blocks`, `list_instances`, `set_port_position`, `undo`, `redo`, `save_sheet`. It uses the same `SheetEditor`, so the
rules are the same as in the window.

## Themes

The themes and their colours are RetroCommander's, so both programs look the same under one
theme name. `Themes/ThemeManager.cs` adds six colours for the drawing itself (paper, ink, chip
frame, inferred, note, picked item). The choice is saved in `%APPDATA%\RetroSchematic\ui-settings.json`.

## Tests

`tests/RetroSchematic.Desktop.Tests` opens the real window without a screen, draws it, and
counts pixels: the drawing area must show the theme's paper and ink colours, and must change
when the theme changes. The window's logic (picking, details text) is tested without a window
in `tests/RetroSchematic.Tests` (`SheetSessionTests`, `SheetEditorTests`, `EditToolTests`,
`DemoSheetLayoutTests`, `McpToolTests`).
