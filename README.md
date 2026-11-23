# RetroSchematic

[![build](https://github.com/RetroCoreLabs/RetroSchematic/actions/workflows/build.yml/badge.svg)](https://github.com/RetroCoreLabs/RetroSchematic/actions/workflows/build.yml)
[![licence](https://img.shields.io/github/license/RetroCoreLabs/RetroSchematic)](LICENSE)

**RetroSchematic turns hand-drawn Norsk Data circuit drawings from the 1970s into a machine-readable list of chips and wires, and draws them again in either ND's own gate symbols or modern ones.**

[What it does](#what-you-can-use-it-for) · [Quick start](#quick-start) · [Format](src/docs/FORMAT.md) · [About the drawings](#about-norsk-data-circuit-drawings)

## Why it exists

The only record of how many Norsk Data cards worked is a scanned drawing. The scans are hard
to read, the symbols are ND's own rather than the usual ones, and the OCR text of the drawing
pages is nonsense. Emulating a card, rebuilding it in KiCad or Logisim, or moving it onto an
FPGA all need the same thing first: every chip and every wire, written down once, with how sure
each reading is.

This repository holds that written-down form (one JSON file per drawing sheet), the scans it
was read from, and the tools that check and draw it.

## What you can use it for

- **Look at a card in a window** - the viewer draws a sheet in ND's 1976 symbols or modern
  ones, in a choice of colour themes. Wires go round chips, never through them, and signal
  values are coloured the way Logisim does it (1, 0, Z, unknown).
- **Trace a signal** - click a wire, pin, tag or list entry and every place the signal reaches
  pulses. The Details panel lists its pins, the chip that drives it, its drive type
  (open-collector, three-state), its description and where it leaves the card. Back and
  Forward work like a web browser (Alt+Left / Alt+Right, mouse side buttons).
- **See how a chip works** - each chip's pins, functions and behaviour come from its
  datasheet, with the page: truth table, output enable, which clock edge it latches on, and
  set/clear pins.
- **Edit a card** - read-only by default; in edit mode place chips, connect wires, make
  virtual buses and blocks (sub-circuits, like Verilog modules or KiCad sheets), and type
  signal descriptions. Everything can also be done through the MCP server.
- **See the connectors** - backplane and front connectors are drawn as parts and wired to the
  chips, with ND's bus signal names from the bus catalogues.
- **Choose how names look** - an active-low signal as `/NAME` (default), `~NAME`, or ND's own
  0/1 subscript. Settings can follow the file.
- **Check a sheet file** - `retroschematic check` refuses a file with an unknown format
  version, an unknown field, a pin on two wires, or a chip type not in the catalogue.
- **Draw a sheet to SVG** - `retroschematic render`.
- **Look up a symbol, chip or bus signal** - the symbol catalogue (`src/docs/SYMBOL-CATALOG.md`),
  the chip catalogue (`src/catalog/chips/`), and the NORD-10 and ND-100 bus signal catalogues
  (`src/docs/BUS-SIGNALS.md`).

## Sheet files

A sheet is one drawing page of one card. It is stored as JSON (`src/docs/FORMAT.md`):

| Ending | What it is |
|---|---|
| `.rsc` | the JSON as text |
| `.rsx` | the same JSON zipped, together with its original drawing pages and PCB photos |

Old `.json` sheets still open. Sheets live in one folder per machine under `src/sheets/`
(`nord-10/` today). The app can register itself to open `.rsc` and `.rsx` files on
double-click (File menu, current Windows user only).

## Programs

| Program | What it is |
|---|---|
| `RetroSchematic.Cli` (`retroschematic`) | command line: check a sheet, write it as SVG, wire statistics |
| [`RetroSchematic.Desktop`](src/RetroSchematic.Desktop/README.md) (`RetroSchematic`) | the viewer and editor window |
| `RetroSchematic.Mcp` (`retroschematic-mcp`) | MCP server over standard input and output: read, trace and change sheets with the same rules as the window (see [the viewer](src/RetroSchematic.Desktop/README.md#mcp-server)) |

## Status

Version 0.1.0, no tagged release yet. Nothing is pushed to GitHub yet.

- **One card is read:** NORD-10 sheet 2A50, the synchronous modem buffer card 1050. Every
  chip, gate and wire is read pin by pin, then re-read by a second reader, which corrected
  the clock of the receive interrupt enables. How its interrupts work is explained in
  `src/docs/2A50-interrupts.md`.
- **Symbol catalogue:** all 128 parts of ND-13.003.01, drawn in ND and modern style.
- **Chip catalogue:** 131 chip files from datasheets, with behaviour as data. A few have no
  behaviour, and say why: no datasheet was found, or the datasheet gives no logic function.
- **Bus catalogues:** NORD-10 (55 signals) and ND-100 (68 signals, with the ND-120 names),
  plus connector files for the NORD-10 backplane and modem front, the ND-100 C plug and the
  ND-120 CPU's A and B plugs.
- **Tests:** library and window tests, run with the command in Quick start. Known test
  problems are in `src/docs/BUGS.md`.
- **Outstanding work** (simulator, KiCad/Logisim converters, Verilog export, more cards) is in
  `src/docs/PLAN.md`.

## Prerequisites

| # | Requirement | Needed for | How to check |
|---|---|---|---|
| P1 | .NET SDK as pinned in `src/global.json` | building | `dotnet --info` |
| P2 | Git LFS | the scans under `src/docs/sources/` | `git lfs version` |

## Quick start

```powershell
# from the src folder
dotnet test RetroSchematic.slnx -nodeReuse:false
dotnet run --project RetroSchematic.Cli -- check sheets\nord-10\2A50-sync-modem-buffer-1050.rsc
dotnet run --project RetroSchematic.Cli -- render sheets\nord-10\2A50-sync-modem-buffer-1050.rsc 2A50.svg nd
dotnet run --project RetroSchematic.Desktop -- sheets\nord-10\2A50-sync-modem-buffer-1050.rsc
```

Use `modern` instead of `nd` for modern gate symbols.

## Current limitations

- **Only one card is read so far.** A library of NORD-1, NORD-10 and ND-100 cards is planned.
- **No simulator yet** - the run/step buttons and value colours exist, but nothing simulates.
- **No KiCad, Logisim or Verilog export yet.**
- **Busy sheets still have long wires** - a large card like 2A50 draws correctly but is
  crowded; connector boxes and chips can be moved by hand.

## About Norsk Data circuit drawings

Norsk Data (Oslo, 1967-1992) built the NORD-10, ND-100 and ND-500 minicomputers. Their circuit
drawings use a house symbol set built from NAND and NOR boxes with straight lines only, a 0/1
subscript on every signal name for its active level, and a board position (column number, row
letter, such as 13D) for every chip. The symbols are defined in ND-13.003.01 "Circuit Diagram
Symbols" (July 1976), which is in `src/docs/sources/manuals/`.

## Documentation

- [The file format](src/docs/FORMAT.md) - fields, the version rule, how sure a reading is
- [Sheet 2A50 reading](src/docs/2A50-reading.md) - register-level reading of the sync modem card
- [Sheet 2A50 interrupts](src/docs/2A50-interrupts.md) - how LEV10 and LEV12 are raised and cleared
- [Sheet 2A50 against the catalogue](src/docs/2A50-catalogue-map.md) - every drawn element matched to its symbol and chip
- [Symbol catalogue](src/docs/SYMBOL-CATALOG.md) - every ND symbol, both styles, with chip data
- [Chip file format](src/catalog/chips/FORMAT.md) - pins and behaviour from datasheets
- [Bus signals](src/docs/BUS-SIGNALS.md) - NORD-10 and ND-100 bus, sources and where they disagree
- [The viewer](src/RetroSchematic.Desktop/README.md)
- [Plan](src/docs/PLAN.md) - outstanding work
- [Known bugs](src/docs/BUGS.md)

## Contributing

See [CONTRIBUTING.md](.github/CONTRIBUTING.md). Security problems go through
[SECURITY.md](.github/SECURITY.md), never a public issue.

## Licence

The code is MIT licensed - see [LICENSE](LICENSE). The scanned drawings and manuals under
`src/docs/sources/` are Norsk Data documents, the chip datasheets under `src/datasheets/` belong
to their makers (Texas Instruments, National Semiconductor, SMC and others), and the PCB photos
under `src/sheets/` belong to their owner. None of them are covered by that licence.
