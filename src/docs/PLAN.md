# Plan

Outstanding work only. Finished items are deleted.

**Next:** map sheet 2A50 with the catalogue (`docs/SYMBOL-CATALOG.md`), reading every wire pin by
pin, and note every symbol on the sheet the catalogue cannot name.

## 1. Symbol catalogue - what is still missing

Done: all 128 parts of ND-13.003.01 have a cut-out and a file in `catalog/symbols/nd1976/`; the 31
gate symbols have a description, the marks that tell them apart, and the manual's formula; NAND,
NOR, NOT, AND, OR and XOR are drawn in both styles from one pin table, checked by `GateShapeTests`.
`docs/SYMBOL-CATALOG.md` shows it all. Still missing:

- pin numbers of the block parts (7442, 74153, 7474 ...) transcribed from the cut-outs;
- drawings for buffer, open-collector, Schmitt, AND-OR-INVERT and line-driver symbols;
- chip files (`catalog/chips/`) with pinouts, formulas, stored state and delays from datasheets;
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
- Layout is kept apart from connections: chip x/y, wire bend points and label positions go in a
  layout section or file. Moving a chip never changes the netlist; one netlist can have several
  layouts (the original sheet, a tidied one). The layout as on the scan is always kept, so the
  scan can be shown behind the drawing.
- Per wire: re-route automatically when a chip moves, or keep hand-placed bend points.

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

- File: open a sheet, save, export to KiCad, Logisim, SVG/PNG.
- View: ND 1976 or modern symbols (signal lines stay put), zoom, fit, show or hide pin numbers
  and board positions, show the original scan behind the drawing to compare.
- READ, INFERRED and OPEN connections each in their own colour.
- Click a part or signal: side panel with type, formula, pins, source (sheet, page) and sure
  mark; clicking a signal lights every pin it reaches, across sheets.
- Simulation toolbar: run, pause, single step, reset; click an input to set 0 or 1; lines
  coloured by value (0, 1, not driven, unknown, driven twice).
- Signal plot window (logic analyser view).
- Search box for a part, signal or pin.

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

- Gate positions measured on the scan (`position` in the format) are not used yet.
- Signals are shown as names at the pins; routed wires are not drawn.
- OR and buffer symbols in ND style are not transcribed from the manual yet; they are drawn as
  a labelled box.
