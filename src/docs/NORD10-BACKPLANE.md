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
  ND-06.012.01 figure 7.4 agrees that 6, 7 and 8, 9 are not wired one-to-one to the next position,
  but names them the other way round (6 INIDENT, 7 OUTIDENT, 8 INGRANT, 9 OUTGRANT). The two
  sources DISAGREE on which pin is the input; the backwiring reading is kept.
  The chain starts at A22 (BIDENT, A22 pin 10, into A25 pin 7) and ends at A32. An empty
  position therefore breaks the chain unless its pins are joined (INFERRED from the wiring). The
  2A50 card joins its GRANT pins 8 and 9 straight through.
- **Not wired in the list:** pins 0-5 and 56-99 are empty at every I/O position; the list names
  signal wires only. Pins 4 and 5 are BFQ1 and BFQ2 (figure 7.4; BFQ1 also on the 2A50 drawing),
  so where they come from in this crate is OPEN.
- **Figure 7.4 names three bus pins differently** (it is drawn for the "I/O bus B and C rack"):
  19 MPX ADDRESS (backwiring: RESTART), 47 DMA REQUEST (IOTE), 51 DMA DATA READY (BSKIP). Both
  names are kept per signal; which is right for which rack is OPEN. BA0-17, BD0-15, IOXE, BINPUT,
  BCONNECT, INT10-15 and MASTER CLEAR agree pin for pin.
- **Cable side, 56-95** (READ, ND-06.012.01 figure 3.1 "I/O slot/plug layout", page 3-3): the
  card's own pins, to the external device through a Berg plug, cabled to a Burndy MS42RM plug in
  the plug panel (Berg 56 -> v, 57 -> t, ... 94/95 at A-D; the full letter table is OPEN, the
  figure was shown only in part). Whether the Berg plug goes onto the long wire-wrap posts behind
  the position (as the NORD-10/S manual section 4.1.1, "Cable connectors are plugged directly into
  the backplane", and the photos suggest) or onto the card is OPEN. 2A50's +-12V out on 62/63,
  66/67 and its grounds on 80-94 are the card's choice of free cable pins, not a conflict.

## Supply (READ, ND-06.012.01 figure 3.1, page 3-3)

Every I/O slot has a 100-terminal connector with **+5V on pins 0, 1, 98, 99 and ground on pins 2,
3, 96, 97** ("4 terminals for power, 4 for ground"). The backwiring list names no supply pin, as
it lists signal wires only, so it does not disagree. On the 2A50 card the fingers of 0, 1, 98, 99
run into wide copper beside tantalum capacitors, which matches +5V. Still OPEN: which of the
heavy screw lugs on board 501062 feeds which pin; whether the CPU positions A01-A24 use the same
supply pins; what the wide trace joining 60 and 64 on 2A50 is.

## Signal levels (READ, ND-06.012.01 figure 7.4, section 7.4)

Tri-state TTL, except the interrupt lines and DMA request, which are open collector. Logical 1 =
0-0.4 V, logical 0 = 2.4-5 V (active low). 2A50 drives BINPUT (15) and BCON (23) with 7438
open-collector gates, which the figure counts as tri-state: DISAGREE, recorded per signal.

Figure 7.4 also lists DMA cable signals between card 1022 and cards 1014/1092 (DMA data ready,
DINPUT, device request, WRITE, CF clear flag, DR0-2 bus address, DEQL, BERROR, BCOMPLETE, BRBUSY,
BSTART, RRQ, data MDB0-15). They belong to those cards' cables, not to the bus.

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
was not found; it is the most likely place for the terminations (pull-ups).
