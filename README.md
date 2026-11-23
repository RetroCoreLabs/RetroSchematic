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

- **Check a sheet file** - `retroschematic check` refuses a file with an unknown format
  version, an unknown field, a pin on two wires, or a gate in a chip that is not listed.
- **Draw a sheet** - `retroschematic render` writes an SVG with every chip at its position on
  the board, in ND 1976 symbols or modern symbols.
- **Look at a sheet in a window** - the viewer draws the sheet, switches between ND and modern
  symbols, and shows what a clicked part, gate or signal is.
- **Look up a symbol** - `src/docs/SYMBOL-CATALOG.md` shows every part in ND's 1976 symbol
  manual, redrawn in ND style and in modern style, with its formula, pins and delays from the
  chip's datasheet.
- **Read the drawings** - the scans and the ND symbol manual are in `src/docs/sources/`.

## Programs

| Program | What it is |
|---|---|
| `RetroSchematic.Cli` (`retroschematic`) | command line: check a sheet file, write it as SVG |
| [`RetroSchematic.Desktop`](src/RetroSchematic.Desktop/README.md) (`RetroSchematic`) | the viewer and editor window |
| `RetroSchematic.Mcp` (`retroschematic-mcp`) | MCP server over standard input and output: make and change sheets with the same edit rules as the window (see [the viewer](src/RetroSchematic.Desktop/README.md#mcp-server)) |

## Status

Version 0.1.0, no tagged release yet.

- The file format, version 1, is defined (`src/docs/FORMAT.md`, `src/schema/nd-netlist-1.schema.json`).
- One sheet is written: NORD-10 sheet 2A50, the synchronous modem buffer card 1050. All 40 chips
  and 69 gates are in, checked against the parts list printed on the sheet
  (`src/docs/2A50-catalogue-map.md`); most chip-to-chip wires are not read pin by pin yet.
- The symbol catalogue covers all 128 parts of ND-13.003.01, with chip files from datasheets
  for all but one (74118: no datasheet found).
- The viewer window shows a sheet with the RetroCommander themes, a toolbar, an edit mode and a
  trace tool that lights one whole signal.
- The renderer places chips with room between them and routes short local wires round every
  part; every other signal is an ND net-label diamond at each pin (the rule is in
  `src/docs/FORMAT.md`, "How a sheet is drawn").
- 102 tests, all passing (run 2026-11-23): 91 for the libraries, 11 for the viewer window.
- The outstanding work, including the rest of the viewer, simulation, KiCad/Logisim converters
  and Verilog export, is in `src/docs/PLAN.md`.

## Prerequisites

| # | Requirement | Needed for | How to check |
|---|---|---|---|
| P1 | .NET SDK as pinned in `src/global.json` | building | `dotnet --info` |
| P2 | Git LFS | the scans under `src/docs/sources/` | `git lfs version` |

## Quick start

```powershell
# from the src folder
dotnet test RetroSchematic.slnx -nodeReuse:false
dotnet run --project RetroSchematic.Cli -- check sheets\nord-10\2A50-sync-modem-buffer-1050.json
dotnet run --project RetroSchematic.Cli -- render sheets\nord-10\2A50-sync-modem-buffer-1050.json 2A50.svg nd
dotnet run --project RetroSchematic.Desktop -- sheets\nord-10\2A50-sync-modem-buffer-1050.json
```

Use `modern` instead of `nd` for modern gate symbols.

## Current limitations

- **Only short local signals are wires** - the rest show as a named diamond at each pin, the
  way ND drawings end a line. Hand-placed wire paths are not saved yet.
- **ND OR and buffer symbols** are not transcribed from the manual yet and draw as a labelled box.

## About Norsk Data circuit drawings

Norsk Data (Oslo, 1967-1992) built the NORD-10, ND-100 and ND-500 minicomputers. Their circuit
drawings use a house symbol set built from NAND and NOR boxes with straight lines only, a 0/1
subscript on every signal name for its active level, and a board position (column number, row
letter, such as 13D) for every chip. The symbols are defined in ND-13.003.01 "Circuit Diagram
Symbols" (July 1976), which is in `src/docs/sources/manuals/`.

## Documentation

- [The file format](src/docs/FORMAT.md) - fields, the version rule, how sure a reading is
- [Sheet 2A50 reading](src/docs/2A50-reading.md) - register-level reading of the sync modem card
- [Sheet 2A50 against the catalogue](src/docs/2A50-catalogue-map.md) - every drawn element matched to its symbol and chip
- [Symbol catalogue](src/docs/SYMBOL-CATALOG.md) - every ND symbol, both styles, with chip data
- [The viewer](src/RetroSchematic.Desktop/README.md)
- [Plan](src/docs/PLAN.md) - outstanding work

## Contributing

See [CONTRIBUTING.md](.github/CONTRIBUTING.md). Security problems go through
[SECURITY.md](.github/SECURITY.md), never a public issue.

## Licence

The code is MIT licensed - see [LICENSE](LICENSE). The scanned drawings and manuals under
`src/docs/sources/` are Norsk Data documents and are not covered by that licence.
