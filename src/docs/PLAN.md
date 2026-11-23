# Plan

Outstanding work only. Finished items are deleted.

**Next:** add the parts the catalogue cannot name yet (see `docs/2A50-catalogue-map.md`), then
follow the four 74153 select lines on 2A50 (3E, 5E, 5D pins 2 and 14 are `inferred`).

## 0. Sheet 2A50 - what the wire reading left open

- 74153 select pins 2 and 14 on 3E, 5E and 5D: taken to match 3D, not followed on the scan.
- Front connector pins (circled 81-95 with V.24 names) are written `F.` as `inferred`; the
  supply pins circled 62/63 and 66/67 are taken as backplane pins, also `inferred`.
- Switches 15E and 1E, resistors and capacitors have no place in format version 1; they are
  described in net notes. Nine nets end at a switch or an RC part and so have one pin.

## 1. Symbol catalogue - what is still missing

Done: all 128 parts of ND-13.003.01 have a cut-out and a file in `catalog/symbols/nd1976/`; the 31
gate symbols have a description, the marks that tell them apart, and the manual's formula; NAND,
NOR, NOT, AND, OR and XOR are drawn in both styles from one pin table, checked by `GateShapeTests`.
`docs/SYMBOL-CATALOG.md` shows it all. Still missing:

- a datasheet for 74118 (its chip file has the title only), VP5, VP12 and DC1035T, and a real
  74H datasheet for 74H11 and 74H21 (they carry LS figures);
- parts on sheet 2A50 with no catalogue entry: 7451 (only inside the 7450 entry), 74S157,
  COM 2601, and VP12/12's pins;
- the candidates folder and process for symbols found on drawings but not in the manual.

### Layout

```
src/catalog/
  chips/7402.json          what the chip is: pins (in/out/power, open collector, three-state),
                           gates with a formula per output, stored state, delays, datasheet source
  symbols/nd1976/NOR.json  how ND draws it: text description for an AI, the marks that tell it
                           from look-alikes, manual page, chips it is used for, the pin check rule
  symbols/nd1976/NOR.svg
  symbols/nd1976/NOR.png   cut-out from the manual page
  symbols/modern/NOR.json, NOR.svg
  symbols/pins/NOR.json    the one shared pin table both SVGs are tested against
  candidates/              symbols found on drawings but not in the manual, waiting for review
src/docs/SYMBOL-CATALOG.md the process document
```

Chip and symbol are separate files because one chip can be drawn with several symbols (a 7400
as NAND, or as OR with inverted inputs). Every fact carries where it came from.

A symbol not in the manual: the reader writes a candidate (cut-out, description, position code,
pin numbers, chip type, its guess marked as a guess), proves it by pin numbers against the
datasheet and the chip count against the parts list, and it moves into `symbols/` only when
approved. Until then every use of it in a netlist is `inferred`, never `read`.

Work split for building it: one worker per group of manual pages (symbol files and cut-outs),
one per chip family (chip files from datasheets), one for the SVGs and the pin-table test, and a
final check that compares the symbol files against the manual pages without seeing the others'
reasoning - about 6-8 workers.

## 1b. Signal names and layout

- Every net has a name: the printed one, or an automatic one from the driving output
  (`N_11D_1`), with a field saying which. Automatic names do not change when a chip is moved.
- The driving output and the line type (normal, open-collector shared, three-state bus) are marked.
- Layout: chip positions and fixed wire paths are saved (`parts[].position`, `layout.wires`).
  Still missing: label positions, several layouts for one netlist (the original sheet, a tidied
  one), and keeping the layout as on the scan so the scan can be shown behind the drawing.
- Drawing a wire path by hand with the mouse (today a path is fixed from the current route, or
  given as points through the MCP server).

## 2. Logic description of each part

In the symbol/part file, so the viewer and the MCP tool can work out signal values:

- simple gates: a formula per output (7402 pin 1 = NOT(pin 2 OR pin 3));
- open-collector outputs (7401, 7403, 7405, 7438): only pull low; a line with several is the
  AND of them and needs a pull-up. That is a property of the WIRE, so the netlist marks such
  lines;
- three-state outputs (8097/8098): a line has a fourth value, "not driven", and two drivers on
  at once is an error;
- parts with memory (7474, counters, COM2601): stored state plus a rule for when it changes
  (rising clock edge). The COM2601 needs its own model; one exists in C# in RetroCore
  (`Emulated.HW/SMC/COM/COM2601/COM2601.cs`);
- an OPEN (unknown) connection gives "unknown", never a quiet 0;
- a field for the datasheet delay of each part, even if the first simulator ignores it.

The file layout must cover all of this from the start; adding it later rewrites every converter.

## 3. Avalonia viewer

The window has no logic of its own: every action calls the shared libraries, and the
command-line tool and an MCP tool can do the same.

- File: export to KiCad, Logisim.
- View: show the original scan behind the drawing to compare. Wires start at the pin ends,
  which are the same in both symbol styles except the ND AND gate's output (10 units further
  right), so switching style still moves those wires a little.
- A picked signal lights its pins on this sheet only; across sheets is not done.
- Editing, still missing: rename a signal and set its subscript, drive and pull-up; change a
  part's or pin's confidence and note; move a single gate; hand-placed wire bend points (wires
  are routed again from the pin ends after every change). A chip whose formulas are not plain
  gates (7474, counters) is placed without gate symbols and shown as a pin list.
- Simulation: the toolbar buttons (run, pause, step, reset) and the value colours (0, 1, not
  driven, unknown, driven twice) exist, but nothing simulates yet; click an input to set 0 or 1.
- Signal plot window (logic analyser view).

## 4. Converters

- KiCad netlist and schematic. Which import paths KiCad 9 offers is not checked yet.
- Logisim-evolution `.circ` (XML). Whether its TTL library has every part used is not checked.
- EDIF if a tool needs it.

## 5. Verilog export

- One module per chip type, built from the same formulas as item 2, so the simulator and the
  Verilog cannot drift apart.
- One top module per sheet: one instance per chip, one wire per signal, the card edge pins
  (P., F.) as ports.
- Open-collector lines become an AND of the outputs with the pull-up giving the 1, written out
  explicitly.
- Three-state bus lines become a multiplexer inside the FPGA.
- 7474 becomes a clocked register; every clock from a gate output and every async set/reset is
  listed as a warning.
- A chip with no model (COM2601 until one is written) becomes a module with the right pins and
  an empty body: a comment at the top says no model exists, its outputs are left undriven
  (never tied to 0 or 1), and the export log lists it as a warning. The export still finishes.
- An OPEN connection fails the export and is listed.
- A test bench from the same input patterns the simulator runs.
- Checking, in order: (1) build and run with Verilator under WSL and compare against the
  tool's own simulation; (2) `verilator --lint-only` with all warnings, failing on double
  drivers, undriven signals and timing loops; (3) synthesis with Yosys. A real FPGA board only
  after that. Installed in WSL (checked 2026-11-23): Verilator 5.025, Yosys 0.9 (2019 - old;
  stay with plain Verilog or update it), Icarus Verilog.

## 6. Renderer

- Wires are routed again from scratch after every change; hand-placed bend points are not kept
  yet (see section 1b). A wire can still take a long way round a busy corner of a sheet.
- Backplane and front pins have no place on the sheet, so their signals are always labelled.
- OR and buffer symbols in ND style are not transcribed from the manual yet; they are drawn as
  a labelled box.

## 7. Blocks (hierarchy) - the design is in FORMAT.md, "Format version 2"

Being built, in this order:

- Model and reader: `blocks[]`, `instances[]`, ports with `signals[]`; version 1 still read as
  the top block; the checks listed in FORMAT.md; schema `schema/nd-netlist-2.schema.json`.
- Flattening with hierarchical names (`U1/3C.9`), and a check that compares two designs by
  their flattened connections.
- Editor: make a block from a set of parts and instances (ports made for every signal that
  crosses the edge, signals numbered alike grouped into bus ports), dissolve an instance, enter
  and leave a level, set a port's side and place. Undo for all of it.
- Drawing: an instance is a box with its name, its block's name, and a pin per port on its
  edges with the port name and a direction arrow (in, out, both ways); a bus port is drawn
  bus-thick.
- Port direction arrows: a View menu switch and the toolbar button, on by default, saved with
  the other view settings; SVG export draws what the screen shows.
- Window: Ctrl-click to select several, right-click "Make block...", "Open", "Dissolve into this
  level", double-click opens, a bar showing the path back up, Make block and Up one level
  buttons.
- MCP: make block, dissolve, enter, leave, list blocks and instances, set port position.
- A demo file: 2A50 grouped into address decode, read multiplexers, interrupt logic and modem
  interface. The 2A50 file itself stays the reading of one sheet.
