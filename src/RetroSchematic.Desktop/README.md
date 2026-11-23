# RetroSchematic viewer and editor

A window that shows a sheet file the way `retroschematic render` writes it, and lets you click
on it: a part, a gate, a signal or a pin shows what it is in the side panel. In edit mode it
also places chips, draws wires and moves chips, and saves the sheet back to its JSON file. The drawing on screen
and the SVG file come from the same layout code (`RetroSchematic.Render/SheetLayout.cs`), so they
always match.

## The Details panel

The right-hand panel shows what is picked as cards with a header bar; click a header to fold a
card, and the fold stays while you pick other things.

- **A signal:** its name and active level, what it does (with where that is written and how sure:
  read, inferred or open), the electrical facts (drive type, driver, loads, pull-up, and any
  conflict in a red box), where else it goes (bus, connector, block levels), and a table of its
  pins. Click a pin row to centre the drawing on that pin.
- **A part:** board position, type, function, catalogue title and where it was read, then every
  pin with the signal on it.
- **A connector, a bus or a block:** its facts and the list of signals on it.

In the part, connector, bus and block lists, clicking a row traces that row's signal, as a click
on its wire does. The panel only lays out facts that the library works out (`SignalInfo`,
`PartInfo`, `SignalList` in RetroSchematic.Render), and every colour comes from the theme.

## Back and Forward

Every pick, every trace started from the drawing, a list or the Details panel, and every
change of block level is a step in a history, as in a web browser. Back returns to the step
before, with its view; Forward goes again. Use the arrow buttons at the left of the toolbar or
at the top of the Details panel, Alt+Left / Alt+Right, or the mouse's side buttons. A new pick
after going back drops the steps that were ahead. Tab jumps between the places of a traced
signal are not steps. The tooltips name where the buttons go ("Back to 11D 7402").

## Start it

```powershell
# from the src folder
dotnet run --project RetroSchematic.Desktop -- sheets\nord-10\2A50-sync-modem-buffer-1050.rsc
```

The program is `RetroSchematic.exe`. With no file named it opens the sheet that was open last time.
File > Open the demo sheet opens `sheets/demo/demo-gates.rsc`, a small made-up sheet (not a
reading of a real drawing) with every kind of connection in it.

## Using it

| Do this | To get |
|---|---|
| File > New sheet | an empty design, in edit mode, for a NORD-10 card, an ND-100 card, or a bus not known yet (a warning until it is set in Settings, This sheet) |
| File > Open sheet | open another sheet file (`*.json`), read-only |
| File > Save sheet / Save sheet as | write the sheet as nd-netlist JSON, with every position |
| File > Save as SVG | the drawing as shown, as an SVG file |
| File > Export PNG | the whole sheet as a picture, twice the drawing size |
| Edit > Edit mode | switch between read-only (every change refused) and edit mode |
| Edit > Undo / Redo | step back and forward through changes |
| View > Show pin numbers / Show board positions / Show port arrows | hide or show them (the toolbar buttons too) |
| View > Show buses, or its toolbar button | off: buses hidden and every member drawn with its own wires and name tags. Display only - the sheet keeps its buses |
| View > Settings (Ctrl+,), or the gear button | every display option, as a default for all sheets and for this sheet (see below) |
| Search box | lists matching parts, signals and pins (write a pin as 11D.3) |
| View > ND 1976 symbols / Modern symbols | switch the symbol set; what is picked stays picked |
| View > Fit to window | the whole sheet in view |
| View > Theme | Light, Dark, Solarized Dark, Nord, Retro Green, Amber CRT, Synthwave '84, High Contrast |
| Mouse wheel | zoom round the pointer |
| Right or middle button drag | move the sheet |
| Left click | pick a part, gate, signal or pin; its details show on the right |
| Parts / Signals lists | pick by name: the same as a click on the drawing (a signal is traced), and the view moves to show it |
| Rest the pointer on a signal | after about 400 ms: its name, what it does, its drive and the pin that drives it |

Colours: what was READ off the drawing is in the theme's ink colour, anything INFERRED is in
its own colour, anything OPEN (not known) in another, and the picked item is in the theme's
selection colour. A wire is as sure as the less sure of the two pins it joins.

The title bar says which machine bus the sheet is for (NORD-10, ND-100 or unknown bus); that
choice picks the bus-signal and connector catalogues.

**Connectors.** The card's connectors are boxes right of the chips: the backplane (`P`) and a
front connector (`F`), one row per pin in pin order with the pin number, the signal's name and,
when the catalogue names that pin differently, its name too (`/IOXE = BIOXE`). A pin whose
connector letter is not known shows a plain number and the box says "connector not known";
`A.32` means connector A, pin 32. Click a pin to trace its signal like any chip pin.

**Signal facts.** Picking a signal or one of its pins shows everything known about it on the
right: what it does and where that is written (the sheet's own text, else the bus catalogue's),
the drive type worked out from the chip catalogue (with a warning for mixed drivers and a
CONFLICT line when the file says something else), the driving pins, the loads, and every pin
like `13C.7  74153 1Y  output`. In edit mode the panel also has fields for the description, its
source and the notes.

## Settings

The Settings window has two columns. **Defaults for all sheets** are kept by the app.
**This sheet** values are saved in the sheet file (its optional `display` section, see
`docs/FORMAT.md`); each can be "Use default". When a sheet is opened its own values win.
Changing a value for this sheet marks it as changed, so it needs saving; it never changes what
is connected. The View switches change the defaults.

| Option | Choices |
|---|---|
| Active-low signal names | /NAME (default), ~NAME, or NAME with ND's subscript 0/1. Active-high is plain NAME; a signal with no level is plain in every style. Search finds a signal typed in any style |
| Symbol style | ND (1976 manual) or modern |
| Theme | any of the themes |
| Machine bus | this sheet only: NORD-10, ND-100 or unknown (a warning) |
| Show pin numbers, board positions, port arrows, buses | on / off |
| Trace pulse | on: a traced signal pulses; off: it is shown still |

The command line takes the same choices: `--theme <name>`, `--no-buses`,
`--names slash|tilde|subscript`.

## Tracing a signal

Click anything that belongs to a signal - a wire, a name tag, a pin, a bus entry or bus member
name, or a port of a block - and every place the signal goes lights up and pulses in the trace
colour, once a second, while everything else is dimmed. This works with the Select tool and in
read-only mode; the Trace tool does the same. A click on a chip body or the paper clears it.

- The bus a signal runs along is shown with it.
- Where the signal goes into a block, a pulsing ring sits on the block's edge; double-click
  that port to follow the signal inside (the same signal is then traced there).
- Tab centres the view on each place in turn (pins first, then the block entries); Shift-Tab
  goes back.

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
| Right-click a port pin of an instance | put the port on another edge of the box, or one place earlier or later along its edge (drawing only) |
| Right-click an instance > Dissolve into this level | puts its inside back on this level |

A pin is on one signal only: joining two pins that are already on different signals is refused,
and the reason shows in the status bar. A chip's gates come from the plain-gate formulas in its
catalogue file; a chip without them (7474, counters) is drawn as a list of its pins.

All of this is done by `RetroSchematic.Render/SheetEditor.cs` (the changes) and `EditTool.cs`
(what a mouse press or drag means); the window only passes the mouse and keys on.

## MCP server

`RetroSchematic.Mcp` (`retroschematic-mcp.exe`) offers the same changes to an MCP client over
standard input and output: `new_sheet` (with the machine bus: nord-10, nd-100 or unknown), `open_sheet` (read-only unless `edit` is true),
`set_edit_mode`, `list_parts`, `list_nets`, `place_part`, `move_part`, `connect_pins`,
`disconnect_pin`, `delete`, `create_bus`, `set_bus_path`, `add_bus_member`, `remove_bus_member`, `delete_bus`, `list_buses`, `make_block`, `dissolve_block`, `enter_block`, `leave_block`,
`list_blocks`, `list_instances`, `set_port_position`, `get_signal_info`, `set_signal_description`, `undo`, `redo`, `save_sheet`. It uses the same `SheetEditor`, so the
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
