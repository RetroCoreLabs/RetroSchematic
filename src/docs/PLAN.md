# Plan

Outstanding work only. Finished items are deleted.

**Next:** read sheet 2A50 pin by pin (most chip-to-chip wires are not in the netlist yet), then
the symbol library (item 1).

## 1. Symbol library from ND-13.003.01

For every symbol in `docs/sources/manuals/ND-13.003.01-Circuit-Diagram-Symbols-1976.pdf`:

- a text description good enough for an AI to recognise it on a drawing: the shape, the marks
  inside the box and what each means (NAND, open collector, NOR ...), the 0/1 subscript rule,
  the pin numbering;
- a cut-out image of the symbol from the manual, next to the description;
- a check step: after a symbol is named on a drawing, compare it against the description and
  the image, and confirm the pin numbers and the chip count agree (as done for 7402 at 11D);
- two SVG files: ND 1976 style and modern style;
- **one shared pin table per symbol** (pin number, x, y, in/out) that both SVGs and the
  netlist read, so switching style never moves a signal line. A test checks every pin end of
  both SVGs lands on the table's coordinates.

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
