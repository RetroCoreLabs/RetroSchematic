# NORD-10 backplane

What the sources say about the NORD-10 CPU crate's backplane (the "backwiring"). The data, with
a source and READ / INFERRED / OPEN on every fact, is in `catalog/boards/nord10-backplane.json`.
Per-pin signals of an I/O position are in `catalog/connectors/nord10-io-card-backplane.json`
(`pinMap`) and `catalog/signals/nd-bus-nord10.json`; which gold finger is which pin is in
[NORD10-EDGE-FINGERS.md](NORD10-EDGE-FINGERS.md).

## Positions (READ, backwiring list, connector specification list)

32 positions, A01-A32, each one connector with pins 0-99:

| Positions | Cards |
|---|---|
| A01-A17 | CPU: status, arithmetic (2), registers (4), decoding, OR logic, 1K PROM, micro address, timing, memory control, paging, interrupt control, interrupt register, panel driver |
| A18-A21 | memory address and memory data (2 of each) |
| A22, A23, A24 | I/O control, I/O address, I/O data |
| A25-A32 | eight I/O device positions (cards such as 1050, sheet 2A50) |

## How the I/O positions are wired (READ, connector lists and signal code list)

- **Bussed to all eight I/O positions, same pin everywhere:** BA0-BA17 (from A23), BD0-BD15
  (from A24), BIOXE pin 11 and BMCL pin 55 (driven from A22), the interrupt lines BINT10-15,
  BINPUT, BCON, BSKIP, IOTE, RESTART.
- **Chains, position to position:** IDENT (out pin 6, in pin 7) and GRANT (out pin 8, in pin 9).
  The chain starts at A22 (BIDENT, A22 pin 10, into A25 pin 7) and ends at A32. An empty
  position therefore breaks the chain unless its pins are joined (INFERRED from the wiring). The
  2A50 card joins its GRANT pins 8 and 9 straight through.
- **Not wired:** pins 0-5 and 56-99 are empty at every I/O position. Pin 4 is BFQ1 on the 2A50
  drawing, so where BFQ1 comes from is OPEN.
- **Cable side, 56-99:** the card's own pins, reached by a device cable plugged onto the
  backplane posts at the back of the position (INFERRED from the NORD-10/S manual, section
  4.1.1, "Cable connectors are plugged directly into the backplane", and the photos, which show
  long wire-wrap posts behind every position).

## Supply - OPEN

No supply pin is named anywhere in the backwiring list. The I/O backplane board 501062 has heavy
screw lugs that look like supply feeds (READ on the photo), but not which pins they reach. On the
2A50 card the fingers of pins 0, 1, 98, 99 and 60, 64 run into wide copper: candidates only.

## Pull-ups on the open-collector lines - OPEN

`2A50-passives-and-pins.md` asks where LEV10 (BINT10, pin 27), LEV12 (BINT12, pin 35),
CONNECT (BCON, pin 23) and INPUT (BINPUT, pin 15) are pulled up, since 2A50 drives them
open-collector with no resistor of its own. The backwiring list has no resistors, and the photos
of the I/O backplanes 501062, xx6022 and 501048 show none. The only other end of those lines is
A22, I/O CONTROL, so the pull-ups are most likely on that card (INFERRED from the wiring only).
Reading the A22 drawing would settle it.

## Mechanics and photos

The backplane sockets have two rows of contacts, 50 per card side (READ on the photos). The crate
in the ND PCB catalogue photo is an "EMI RACK SYSTEM 19 x 1/2 INCH", Art. M-1001, by Elektro
Mek. Industri A.S., Horten, with positions 1-24 on the rail. Photos in the ND PCB catalogue:
501062 (I/O bus, 8 positions, also shown in a crate), xx6022 (a shorter I/O bus board), 501048
(bus receiver backplane, card 6009, 16 positions). How A01-A32 sit on rails of 24 is OPEN.

The NORD-10 Input/Output manual ND-06.004 (named by the NORD-10/S manual for levels and timing)
was not found; it is the most likely place for the supply pins and the terminations.
