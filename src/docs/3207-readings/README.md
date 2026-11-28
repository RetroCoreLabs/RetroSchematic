# Card 3207 readings

One text file per drawing sheet (`p05.txt` ... `p24.txt`) of card 3207 (SCSI/floppy controller,
ID 350007, print 3207 A1). Sheets 1-4 are read in `docs/3207-reading.md`. The files are checked and
merged by `tools/card-readings.py`:

    python -I tools/card-readings.py check docs/3207-readings
    python -I tools/card-readings.py sheet docs/3207-readings sheets/nd-100/3207-scsi-floppy.rsc

`check` must report 0 problems. `sheet` writes every block that has a reading into the sheet file
(parts, gates, nets, pages, ports, and an instance in the block whose sheet draws its box).

## Lines

A `#` starts a comment, except on a `note` line.

| Line | Meaning |
|---|---|
| `block NAME` | the block this sheet draws the inside of (from the sheet's margin path) |
| `page N` | the sheet number |
| `part REF TYPE [x y w h]` | a part: board position (or printed name) and type as printed; the box is optional, in 300-dpi pixels of the full page, used to make training keys |
| `gate REF/OUT KIND IN,IN>OUT` | one gate of a chip, named after its output pin; kind `nand`, `nor`, `not`, `and`, `or`, `buffer`, `and-or-invert`, `other` |
| `net NAME: REF.PIN ...` | a signal and the pins it joins; the name as printed, a tilde for active low (`BD~00`); a net with no name on the drawing is `REF_PIN` after one of its pins. Several lines of one name, also on other sheets of the block, are one signal |
| `port BLOCK NAME ...` | on the sheet that draws block boxes: the signals drawn at that box's edge; each must be a net in that block |
| `connector REF CATALOG USE` | the part REF is a socket or header on the board, not a chip: its pins may be named by a letter and a number (`A1`); the sheet file stores it as connector `J.REF` with the catalogue file CATALOG |
| `open REF.PIN ...` | pins that could not be settled from the drawing: left out of every net on purpose, never guessed |
| `nc REF.PIN ...` | pins drawn as unconnected stubs |
| `note TEXT` | anything else: how a point was settled, what is OPEN, what is inferred |

## What the check tests

- every pin names a part of its block, and is in one net only;
- a part has one type on every sheet, and a pin is used in one block only;
- the pin exists on the chip (catalogue file, or the plain 74 type for a 74LS/74S/74F part), and a
  supply pin is only on +5V or GND;
- every port name is a net in its block; every gate pin is in a net, `open` or `nc`;
- types with no catalogue file are listed: they must be added from their datasheets.
