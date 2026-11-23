# Plan

Outstanding work only. Finished items are deleted.

**Next:** decide on 2A50's BD buses (section 7), then add the parts the catalogue cannot name
yet (see `docs/2A50-catalogue-map.md`).

## Queue from the 2026-11-23 session (in this order)

- Chip catalogue as data. Steps 1 and 2 are done: the formula reader
  (`RetroSchematic.Model/ChipFormula.cs`) and the structured `behaviour` of every chip file
  (`catalog/chips/FORMAT.md`, Behaviour; `RetroSchematic.Model/ChipBehaviour.cs`;
  `ChipBehaviourTests` checks every chip file). Only TMS6010 is still without it (no datasheet of its own found; `docs/BUGS.md` B3). Still to do, in this order: (3) inverted pin names to base
  name + flag by script, truth tables the same before and after, no raw ' left, every display
  in the chosen name style; (4) function text per pin (hover + Details column); (5) the
  generated "How it works" card in Details (sentence, truth table, output-enable row, clock row
  with up L->H / down H->L, async rows; muxes 74153/74157/74S157 with the sheet's own mapping
  and the two-halves box with the "select (both halves)" band); (6) tests: generated truth
  tables equal the datasheets' function tables (test data with page citations), a 7474
  sequence test.
- Logic simulator and MCP tools (section 9), first test the ICW 2215 octal write on 2A50.

## 0. Sheet 2A50 - what the wire reading left open

- ICW with bit 4 and bits 0/1 in the same write: the clear (CL1) and the enable clock (ICW0
  rising) end together; which wins is not on the drawing (`docs/2A50-interrupts.md`).
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
- parts on sheet 2A50 with no catalogue entry: 7451 (only inside the 7450 entry) and VP12/12's
  pins; COM 2601 has a chip file but no runnable behaviour (its datasheet leaves gaps);
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
- Connector boxes go left or right of the chips. On 2A50 the front connector's wires still loop
  round the 75154 receivers, whose inputs are on their left side; the 75150/75154 chips would
  need to be turned round (a mirrored chip) to fix that.
- OR and buffer symbols in ND style are not transcribed from the manual yet; they are drawn as
  a labelled box.

## 7. Virtual buses and blocks - what is still open

Blocks follow FORMAT.md, "Format version 2". Buses follow FORMAT.md, `layout.buses`.

- The text written at a bus port of an instance ("BD0_0 .. BD15_0") still uses the subscript
  form, not the chosen name style.
- 2A50's own BD buses (BD_0, BD_1): the owner decides whether they stay (each viewer can now hide
  them with View > Show buses). With them the sheet
  draws cleanly, but the long entry wires read worse than labels, and 7 pins cannot reach their
  bus (the render lists them). The blocks demo has them deleted.
- A bus port of an instance is not yet joined to a virtual bus on the level above: it shows
  only the text of what it is connected to (BD0_0 .. BD15_0, 16).
- Only single ports on the left and right edges of an instance get wires or labels on the
  level above. A single port moved to the top or bottom edge shows only its connection text.
- An instance with no position is put in a row below the chips, not among them; a block with
  many ports (read multiplexers: 45) makes a tall box.
- Make block makes ports for the supply lines too (GND, H); a rule for leaving them out (they
  are the same signal everywhere) is not decided.
- Making a block drops the fixed wire path of a signal that crosses the new edge (it joined
  pins that are now on two levels); the wire is routed again.
- The trace lights the ports a signal goes through and the Details panel lists every level it
  reaches; the drawing of the other levels is lit only when that level is opened.
- No test checks the written files against the JSON schemas; the blocks demo and 2A50 were
  checked by hand (no problems).
- Verilog export of blocks (one module per block) belongs with section 5.

## 8. Connectors and signal facts - what is still open

- The NORD-10 card connector's letter is not known (no NORD-10 document names it), so 2A50's
  pins stay `P.n` and its box says "connector not known". The same holds for the modem pins
  81-95 (`F.n`).
- ND-100 plug rows (a, b, c) have no place in a pin reference yet: `P.C.12` names plug C pin 12
  but not the row, so ND-100 backplane names are not looked up by pin (by name they are).
- A connector box can be placed by hand anywhere and its pins put on its left or right side,
  but not on its top or bottom edge (a box above or below the chips still has its pins on a
  side).
- Inside a block, a signal that reaches a connector only on the top level shows the connector
  box there; the block's own port is the way in.
- Function tags on a signal are read, written and shown, but the window has no field for them.
- 2A50 signals the reading document does not explain (DEQL, OSC, the SMN switch lines, the
  numbered N_ nets and others) have no description; a backplane signal among them shows the bus
  catalogue's text.

## 9. Logic simulator and its MCP tools

Built after the chip behaviour data in section 2, because it runs on it.

- An event-driven engine in the library, run from the chip behaviour fields: the formulas, clock
  edges, set and clear pins, output enable, and open-collector lines pulled up as wired AND.
  It uses X for unknown values and for conflicts. The datasheet delays are optional. The
  window's run and step buttons use the same engine.
- MCP tools:
  - `sim_reset`, `sim_set_pin`: set a connector pin (`P.n`, `F.n`, later ND-100 rows) or any
    signal to 0, 1 or not driven;
  - `sim_read`: one signal, one chip pin, or every signal and chip at once, each with its value
    and what drives it;
  - `sim_run_script`: steps that set pins, wait a time or "until stable", pulse a pin (a strobe
    low for 100 ns), and read or expect values. It returns a table per step and a timing record
    of chosen signals, with a VCD file for a waveform viewer;
  - `sim_expect`: fails loudly when a value is wrong.
- A part with no model (COM2601) has unknown outputs unless the script sets them as test
  inputs, and the result says so.
- Test: an IOX write on 2A50. Set BA0-9 to the device address and BD0-15 to 2215 octal, pulse
  IOXE low, then read the IRFTIE, IERRIE and TEST flip-flops and LEV12. They must come out as
  `docs/2A50-reading.md` says.
