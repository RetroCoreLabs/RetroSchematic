# Card 3207 SCSI/floppy controller - reading notes

Source: ND-350007-A1-EN "Schematic Circuit Diagrams", 24 sheets, print 3207 A1, drawn 90.07.09
(design and drawing "HS"), computer-drawn. The scan is kept outside this repository. The board
list (`pcb.json` in the ndfloppy repository) calls it "N-100 SCSI/Floppy Ctrl IV", part
350007, 367 x 280 mm, 8 layers, released 05 Nov 1990.

Every row says how sure it is: **read** (on the drawing), **inferred** (worked out, the reason
given), **open** (not known).

## How the drawing is organised

The sheets are a hierarchy: a box on one sheet is drawn in full on later sheets. The right margin of
every sheet prints the drawing's file path, which gives the tree directly (read at 300 dpi;
the common prefix `/NET/BM3/PCB/USER/ESTHER/3207A1/` is left out below).

| Sheet | Path in the margin | Title block | What it is |
|---|---|---|---|
| 1 | `1.DRAW` | EXTERNAL CONNECTIONS | plug A pins, and the whole card as one box `BLOCKLEV` |
| 2 | `2.DRAW` | ND100 BUS CONNECTORS, POWER & GND CONNECTORS | plug C pins, supplies, plug A row c |
| 3 | open: the scan cuts off the right edge of this sheet | (sheet 3 of 24) | plug B pins |
| 4 | `BLOCKLEV 1.DRAW` | BLOCKLEV | inside `BLOCKLEV`: boxes `SCSI` and `FLOPPY` |
| 5 | `BLOCKLEV/FLOPPY 1.DRAW` | FLOPPY | inside `FLOPPY`: boxes `Z80_COMP`, `LO_FLOPPY`, `F_ND_INT` |
| 6-9 | `BLOCKLEV/FLOPPY/F_ND_INT 1-4.DRAW` | | floppy side, ND-100 bus interface |
| 10-11 | `BLOCKLEV/FLOPPY/LO_FLOPPY 1-2.DRAW` | | floppy drive side |
| 12-14 | `BLOCKLEV/FLOPPY/Z80_COMP 1-3.DRAW` | | the floppy side's Z80 computer |
| 15 | `BLOCKLEV/SCSI 1.DRAW` | | inside `SCSI` |
| 16-17 | `BLOCKLEV/SCSI/DIFF_SCSI 1-2.DRAW` | | |
| 18 | `BLOCKLEV/SCSI/SING_DIFF_SW 1.DRAW` | SING_DIFF_SW | part 18A, switches the SCSI lines between single-ended and differential, drives bus `OSCSI` |
| 19 | `BLOCKLEV/SCSI/S_INTERF 1.DRAW` | | |
| 20-24 | `BLOCKLEV/SCSI/S_ND_INT 1-5.DRAW` | | SCSI side, ND-100 bus interface |

The margin also prints a date and time per sheet ("10 JUL 88 12:14" on sheet 1). Whether that
is when the file was plotted or something else is open; the title block says 90.07.09.

## The sheet file

`sheets/nd-100/3207-scsi-floppy.rsc` (format version 3), with the board facts and both board
photos imported from the ND PCB catalogue (part 350007). What it holds today:

- the top level (sheets 1-3): plugs A, B and C with every drawn pin, the supplies, and one
  instance of box `BLOCKLEV`;
- box `BLOCKLEV` (sheet 4) holding boxes `SCSI` and `FLOPPY` and the signals between them;
- `SCSI` and `FLOPPY` as boxes with their ports only; their insides are not read yet.

Choices made in it:

- Every net keeps its label as printed (`printed`). A name with no tilde is taken as active
  high. That is inferred from the drawing's naming: plug B carries pairs like ODB0 / ODB~0,
  where the tilde marks the complement.
- `+5V` and `5VSTBY` are names given here. The drawing shows only the supply symbols, proved
  against ND-06.016.01 F-2 (see Supply symbols below).
- The 12V flag on sheet 2 (plug C a28/b28/c28) and the 12V flag on sheet 1 (into `BLOCKLEV`)
  are joined by name, because both are on the top level: inferred.
- AVC and AVC~ enter `BLOCKLEV` from flags on sheet 1, but nothing on sheets 1-3 drives them:
  open.
- Port directions: a box pin on sheet 4 has no direction mark, so each port's direction is taken
  from the shape of the plug port its line runs to (arrow in, arrow out, six-sided = both ways),
  marked `inferred`. Some pins carry a small open circle. Across all eight pins where both can be
  seen, the circle appears exactly where the plug port is six-sided. What the circle means is
  not proved. LADR1, LADR2, SOTGRANT~ and SOTIDENT~ run between SCSI and FLOPPY with nothing to
  trace them to: direction `open`.
- No `sources` yet: see the copyright question in `PLAN.md`.

## Plugs

ND-100 cards have three DIN plugs, each with rows a, b, c of 32 pins. Plug C is the ND-100 bus
and is the same in every slot. Plugs A and B are the card's own, wired per slot. The drawing
prints a pin as `<plug> <row><pin>`, such as `C A17`. The sheet file writes it `P.C.a17`.

| Plug | Drawn on | Carries | Catalogue file | Status |
|---|---|---|---|---|
| A | sheet 1 (rows a, b), sheet 2 (row c) | floppy drive lines, row c all GND | `catalog/connectors/nd100-3207-a-plug.json` | read |
| B | sheet 3 | SCSI lines: plain signal on row c, tilde copy on rows a and b joined | `catalog/connectors/nd100-3207-b-plug.json` | read |
| C | sheet 2 | the ND-100 bus, +5 V, 5 V standby, +12 V, GND | `catalog/connectors/nd100-c-connector.json`, signals in `catalog/signals/nd-bus-nd100.json` | read; five pins cross-checked against ND-06.016.01 F-2 in the tests |

The A = floppy, B = SCSI split is printed in `pcb.json` and agrees with the signal names on
sheets 1 and 3 (read).

**Open:** ND-06.016.01 section I.2.3 (1980) gives plugs A and B 64 lines each. This card uses
all three rows of both (plug B: 75 positions). The drawing is taken as the truth for this card
(owner ruling 2026-11-27); the general rule waits for ND-06.017.01 "ND-100 Backplane/Bus
Description" (1979), which has not been found.

## Supply symbols

Proved by matching sheet 2 against the plug C table in ND-06.016.01 F-2 (read):

| Symbol on the drawing | Supply | Pins that prove it |
|---|---|---|
| open triangle pointing down | GND | C a1, b1, c1, a32, b32, c32 |
| filled square | 5 V standby | C a30, b30, c30 |
| T bar | +5 V | C a2, b2, c2, a31, b31, c31 |
| label `12V` | +12 V | C a28, b28, c28 |

## How labels were read

- Active low is a tilde inside or after the name (`BD~00`, `BINT11~`). The sheet file stores
  the name with `_0`, and keeps the label exactly as printed in `printed`.
- Look-alike characters, settled by their shape at 500-600 dpi, never by what would make
  sense: zero has a stroke inside, letter O has none (`FTROO~`, `OSHGND`); 5 has a flat top,
  S a curved one (`F5SSEL~`); 8 is closed (`8HLD5OPT~`).
- A pin label sits below its own port box (checked on sheet 1: read that way every a/b pair
  lands on the same pin number).
- Port shapes (arrow in, arrow out, six-sided) are recorded as drawn. A direction becomes a fact
  only when the logic sheet behind it shows what drives the line.

## Chips named by the board list (not yet checked on the drawing)

`pcb.json` and the sintran.com mirror page list AM9517 (DMA), FD1797 (floppy controller),
NCR5386 (SCSI controller), Z80 CPU, two Z80 CTC, an 8K x 8 SRAM (TC5563 or TMM2063), and ND
part numbers 00900B, 05800B, 34500B, 35700A, 42200A, 42201A, 42500A, 45800A, 45900E, 78100A,
78200A, 78300B, 78400B. Each one is checked against the logic sheets when they are read.

The same board list (given again by the owner, 2026-11-27) also names these. All are still to
be found on the drawing:

| Kind | Board list text | Where on the drawing |
|---|---|---|
| LED | Z80 DMA (yellow) | open |
| LED | BUSY (yellow) | open |
| LED | REQ (yellow; the list spells it "yelloq") | open |
| Display | floppy error code display | open |
| Strap | 1887A2 | open |
| Switch | TH1 - SCSI device number | open |
| Switch | TH2 - SCSI ident | open |
| Switch | TH3 - floppy device number | open |

"SCSI (DL or SE)": SE is single-ended. DL is not explained in the list. The drawing's part 18A
`SING_DIFF_SW` (sheet 18) and the `DIFF_SCSI` sheets switch between single-ended and
differential, so DL may mean differential. That is not proved: open.
