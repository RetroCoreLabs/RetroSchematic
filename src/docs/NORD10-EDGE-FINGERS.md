# NORD-10 card edge: which gold finger is which pin

A NORD-10 card has one edge connector along its backplane side, numbered 0-99 in the
backwiring list (`Backwiring til NORD-10`, connector specification list: "Pin Range 0-99").
No NORD-10 document found says how those numbers sit on the card. This page records how it was
worked out from the photos of board 322049 (card 1050, sheet 2A50) and checked against the 2A50
drawing. The data is in `catalog/boards/nord10-card.json` (`edgeFingers`, the geometry) and
`catalog/connectors/nord10-io-card-backplane.json` (`fingers`, the proof, and `pinMap`, every
pin 0-99 with its signal).

## The rule

Seen from the component side, fingers at the top:

| | finger 0 (left) | finger 1 | ... | finger k | ... | finger 49 (right) |
|---|---|---|---|---|---|---|
| component side | pin 0 | pin 2 | | pin 2k | | pin 98 |
| solder side (behind it) | pin 1 | pin 3 | | pin 2k+1 | | pin 99 |

So the solder side, looked at directly (board turned over), has pin 1 at its right end and pin
99 at its left end. Confidence: INFERRED for the whole rule (checked on one board), with each
step below READ on the photos.

Geometry: 50 fingers on each side, each opposite one on the other side (READ, counted on both
new photos of 322049); 2.54 mm (0.1 inch) apart (INFERRED: 2.546 mm measured on the solder photo,
2.58 mm on the component photo); the row centred on the 156 mm edge, finger 0 centred 15.77 mm
from the left edge (INFERRED: measured 14.5 and 15.6 mm); fingers about 1.5 mm wide and 8 mm
long (measured on one photo).

## The proof on 2A50

1. **Ground.** On the component side the vias under fingers 40, 41, 42 and 43 are joined to
   each other, and a wide trace joins finger 43 to finger 47. The drawing ties exactly five
   circled pins to ground: 80, 82, 84, 86 and 94, which are 2 x 40, 41, 42, 43 and 47. READ.
2. **BA0, pin 32.** Component finger 16 goes to its via, a solder-side trace takes it to a via
   beside chip 11C, and a component-side trace takes it to 11C pin 3. The chip is marked 7404;
   the drawing has BA0 = P.32 = 11C.3 and 11C = 7404. READ.
3. **BA1, pin 33.** The solder-side finger behind component finger 16 runs on the solder side to
   the via above it, and a component-side trace takes it to 11C pin 1. The drawing has BA1 =
   P.33 = 11C.1. READ.
4. **Which solder fingers go anywhere.** Each solder finger has a short hook of copper; on 24 of
   them the copper goes on. Compared with the pins 2A50 uses, "solder finger k is pin 2k+1"
   agrees on 46 of 50 fingers; "pin 2k" on 39; every split into 0-49 on one side and 50-99 on
   the other on 27 or fewer. The four that disagree: pins 1 and 99 (wide copper at the board's
   ends: +5V by ND-06.012.01 figure 3.1, not on the drawing), 9 (INGRANT, joined straight to pin 8 on the other side)
   and 11 (IOXE, trace not followed to its end).
5. **Pins joined on the drawing sit behind each other**: 8/9 (INGRANT), 62/63 (-12V) and
   66/67 (+12V) are each one finger position, both sides.

The traces were followed by marking the green copper in the photos and taking each connected
piece of copper from the finger to the pad it ends on; the pad positions were turned into mm on
the board and matched against the chip grid (`nord10-card` columns, the 2A50 rows).

## Supply pins (READ, ND-06.012.01 figure 3.1 "I/O slot/plug layout", page 3-3)

+5V on pins 0, 1, 98, 99 and ground on 2, 3, 96, 97. On 2A50 that is component fingers 0 and 49
and the solder fingers behind them (+5V), and component fingers 1 and 48 and the solder fingers
behind them (ground). The wide copper and tantalum capacitors at 0, 1, 98, 99 on the photos
match +5V, which also explains two of the four solder fingers that broke the use pattern above.
The Fingers overlay on the photos labels these eight fingers +5V and GND.

## Still OPEN

- What the wide trace joining 60 and 64 on the 2A50 component side is (cable side, not on the
  drawing).
- Whether every NORD-10 card follows the same rule (only 2A50 checked).
- The maker and part number of the 100-terminal backplane connector (figure 3.1 gives the type
  only).
