# 2A50: connector pins, passive parts and other gaps

A second reading of NORD-10 sheet 2A50 (PDF page 64 of the NORD-10 drawing book), done
2026-11-23 at 400 and 1200 dpi, compared with the sheet file, the bus and connector
catalogues, the chip files and the old component-side photo. **READ** = seen on the drawing or
photo, **INFERRED** = worked out (reason given), **OPEN** = not known.

The changes listed in "Changes to apply" are not in the sheet file yet: the file format gets
passive parts first.

## Circled numbers

Every circled number is a pin of the card's one 0-99 connector.

- **Bus circles** (41 of them) match the NORD-10 backwiring list exactly: BA0-BA9 = 32-44,
  BD0-BD15 = 10-30, IOXE = 11, LEV10 = 27, LEV12 = 35, MC = 55, INPUT = 15, CONNECT = 23,
  IDENT 6/7, GRANT 8/9. READ. The sheet file has every one right.
- **(10), (12), (13)** are BD0, BD1, BD2, driven by 5C (8098). The BD numbers have gaps because
  pins 11, 15, 19, 23, 27, 31 and 35 carry other signals. READ.
- **Pins 56-99** are not bus pins: the backwiring list puts bus lines only on 6-55 and lists
  0-5 and 56-99 as empty on every I/O slot. They are the card's own pins, reached by a cable
  plugged onto the backplane (NORD-10/S manual 4.1.1: "Cable connectors are plugged directly
  into the backplane"). List READ, cable INFERRED.
- **(80) (82) (84) (86) (94) to ground:** grounds among the modem pins 81-95, about one between
  each pair of signal pins. READ that they go to ground; INFERRED that they are the cable's
  signal returns. V.24 pins 1 and 7 (the grounds) appear nowhere on the sheet.
- **Modem circles with a bracket number**, e.g. `(3) RECEIVED DATA (87)`: the bracket is the
  V.24 / RS-232 pin on the modem plug. All fit RS-232: 2 TD, 3 RD, 4 RTS, 5 CTS, 6 DSR, 8 DCD,
  15 TxC, 17 RxC, 20 DTR, 22 RI, 23 rate select. INFERRED.
- **Split circles (66/67) and (62/63):** two pins joined. They connect to the OUTPUTS of the
  VP12/12 converter at 18-19E: pins 3 and 22 give +12V to 66/67, pins 10 and 15 give -12V to
  62/63. The card makes its own +-12V from +5V and puts it out on these pins (the ND PCB
  catalogue: "Uses the VP12-12 to generate the required +12V"). Wiring READ, direction
  INFERRED. The connector catalogues list them wrongly as supply pins taken from the bus.
- **(4) BFQ1:** empty in the backwiring list on every I/O slot.
- **+5V:** no circle carries +5V, so where +5V enters the card is OPEN.

## Supply symbols

- A bar at a wire end (a `T`, or `-|` on a horizontal wire) is +5V. It is labelled "+5V" at
  the 47k resistor; elsewhere it is bare. INFERRED for the bare ones.
- The three-line symbol is ground (also drawn sideways at the 74153 pins 15). READ.
- **H** = pulled high through ONE resistor: the "15A9" label, which is chip 15A pin 9. 15A is
  a 75154 line receiver; its pin 9 (R1) is a resistor inside the chip to +5V (about 5 kohm, TI
  1974 data book page 4-81). Every H input is on this net: 13E.1, 11D.11, 19C.4, 19C.12,
  17D.3, 17D.11, 15C.4, 15B.10, 7-8C-D.25, 17A.2, 19A.2. **15A.9 is missing from the H net in
  the sheet file**, so today nothing pulls H high.
- **H1** is not a pull-up: it is 3A pin 10, a 7402 NOR with both inputs grounded, so always 1.
  READ.

## Passive parts

None of these is in the sheet file yet.

| Part | Value as written | Connections | Status |
|---|---|---|---|
| 4 pull-ups at switch block 15E | none written | +5V to SMN3 (13E.4), SMN2 (13E.13), SMN1 (13E.10), SMN0 (13E.6) | READ; four separate resistors |
| 4 pull-ups at switch block 1E | none written | +5V to IDENT2 (3E.6), IDENT3 (3E.10), IDENT4 (5E.6), IDENT5 (5E.10) | READ; four separate |
| Switch blocks 15E, 1E | 4 levers each | lever n connects its line to a common ground rail | READ |
| Pull-up on DEQL | 1k | +5V to DEQL_1 (13E.9, 11C.9, 13C.10, 13C.12, 1A.3) | READ |
| 17D second half | 47k | +5V (labelled) to 17D.7 | READ |
| 17D second half | 10uF | 17D.6 to 17D.7 | READ |
| 17D first half | 10k | 17D.15 to a bare +5V bar | READ (supply INFERRED) |
| 17D first half | 220pF | 17D.14 to 17D.15 | READ |
| On 17D pin 12 | 220 ohm | OSC_1 (17D.12, 19C.9) to OSCD_1 (17D.10) | READ |
| OSCD | 10uF | OSCD_1 to ground | READ |
| 5C/5B enable | 1000 pF | 9B.2 = 5C.1 = 5B.1 to ground | READ |
| SR | 1000 pF | 11A.11 = 7A.1 = 7A.15 to ground | READ |
| Line 95 | 2200 pF | 19A.12 = F.95 to ground | READ |
| Straps at 3D pins 10 and 6 | (STRAP)(GND) | wire links to ground | READ |

- **17D (74221):** pin 6 = 2Cext, 7 = 2Rext/Cext, 14 = 1Cext, 15 = 1Rext/Cext (datasheet), so
  the capacitors and resistors sit exactly where a 74221 needs them. The second half feeds its
  inverted output (pin 12) back through the 220 ohm and 10uF into its B input (pin 10): a slow
  oscillator, OSC_1, that clocks the 7497 timeout counter 19C. INFERRED.
- **Switch pull-ups are four resistors, not a pack.** One resistor is drawn with a solid line
  to line 1; lines 2-4 have dashed stubs ending in a dot. One shared resistor would join
  SMN0-3 into a single signal and the card could not answer its 16 addresses, so there is one
  per line (INFERRED from the logic). The photo shows four separate resistors beside each
  block (READ). Their value cannot be read from the photo: OPEN.

## Photo against drawing

- Red "220pF +-5%" beside the 74221 = the 220pF on 17D pins 14-15. INFERRED.
- One tubular 10uF near it = one of the two 10uF on the drawing; which one is OPEN.
- A second tubular capacitor near the right switch block: print unreadable. OPEN.
- **Two red "2200pF +-5%" on the old board, but the drawing has one 2200 pF and two 1000 pF.**
  Mismatch, OPEN. The drawing's last update (28.11.73) may be older than this board (ECO 3).
  Neither side is "corrected".
- Two 10uF tantalum capacitors at the top corners and the small capacitors beside the chips
  are not drawn: supply smoothing. INFERRED.

## Other things on the drawing

- **Title block:** drawn 6.11.73 by Emil, checked by TU, approval empty, "Ajourført 28.11.73".
  The sheet file says 1973-11-29; the drawing reads **28.11.73**.
- A diagonal stroke across the INIDENT_0 wire at 1A pin 1: a cut or strap mark, or a pen slip.
  OPEN.
- Not drawn (normal for ND sheets): the 75154 threshold pins (1, 2, 3, 14) and the supply pins
  of the line drivers and logic chips. OPEN.

## Changes to apply

1. Add 15A.9 to the H net (inferred; note the "15A9" label and the 75154's built-in resistor).
2. Add every passive part in the table above, with new nets N_17D_7 and N_17D_15, and a ground
   pin on each switch block's common rail. OSCD gets its second pin.
3. List 17D.4 and 17D.5 as unused.
4. Pins 80/82/84/86/94, 62/63 and 66/67 are in the same cable range as F.81-F.95: write them
   with one connector letter (owner's decision).
5. Catalogues (`catalog/connectors/nord10-io-card-backplane.json`,
   `catalog/signals/nd-bus-nord10.json`): 62/63 and 66/67 are outputs of the card's own
   converter, and 80-94 are cable-side grounds (inferred), not bus pins. The modem front
   connector's ground entry can name 80/82/84/86/94.
6. Set the sheet's updated date to 1973-11-28.
7. The 2200 pF / 1000 pF mismatch is recorded here as OPEN.
