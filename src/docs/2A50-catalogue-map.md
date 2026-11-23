# Sheet 2A50: every drawn symbol mapped to the symbol catalogue (2026-11-23)

Sheet: "SYNCHRONOUS MODEM BUFFER 1050", NORD-10, A/S Norsk Data-Elektronikk, drawn 6.11.73,
updated 28/29.11.73. Scan: `docs/sources/nord-10/NORD-10-drawings.pdf`, PDF page 64, read at
600 dpi in overlapping tiles. Catalogue: `docs/SYMBOL-CATALOG.md`, `catalog/symbols/nd1976/`,
`catalog/chips/`. Netlist: `sheets/nord-10/2A50-sync-modem-buffer-1050.rsc`.

Status words: **READ** = read on the scan; **INFERRED** = follows from the scan plus the stated
reason; **UNKNOWN** = not legible or not on the sheet.

Pin notation: inputs `>` output. "Fits" means the printed pins are exactly one gate (or one
section) of the chip file's pinout; the gate number is the chip file's (1A/1B/1Y = gate 1 ...).

## 1. Every drawn element

### Small gates

| Pos | Pins printed | Drawn as | Catalogue symbol | Chip | Fits chip file? | Status |
|---|---|---|---|---|---|---|
| 11E | 2,1 > 3 (BA7, BA8) | plain narrow box, empty | 7400 / nand2 (empty box) | 7400 | yes, gate 1 | READ |
| 11E | 4,5 > 6 (IERR) | plain box | 7400 / nand2 | 7400 | yes, gate 2 | READ |
| 11E | 9,10 > 8 (OSR, IDR) | plain box | 7400 / nand2 | 7400 | yes, gate 3 | READ |
| 11E | 12,13 > 11 (ISR, IDR) | plain box | 7400 / nand2 | 7400 | yes, gate 4 | READ |
| 13B | 2,1 > 3 (RTM, IACTIVE) | plain box | 7400 / nand2 | 7400 | yes, gate 1 | READ |
| 13B | 5,4 > 6 (OACTIVE, TTM) | plain box | 7400 / nand2 | 7400 | yes, gate 2 | READ |
| 13B | 9,10 > 8 (PRQTS > TRQTS) | plain box | 7400 / nand2 | 7400 | yes, gate 3 | READ |
| 13B | 13,12 > 11 (CARR, HALF) | plain box | 7400 / nand2 | 7400 | yes, gate 4 | READ |
| 17B | 1,2 > 3 (RFS > OACTIVE) | plain box | 7400 / nand2 | 7400 | yes, gate 1 | READ |
| 17B | 5,4 > 6 (ICW, BD4) | plain box | 7400 / nand2 | 7400 | yes, gate 2 | READ |
| 17B | 10,9 > 8 (MC, 17B.6 > CL) | plain box | 7400 / nand2 | 7400 | yes, gate 3 | READ |
| 17B | gate 4 (pins 11,12,13) | not found on the sheet | - | 7400 | - | UNKNOWN (spare or not drawn) |
| 17C | 1,2 > 3 (17C.6 > 15C clear) | plain box | 7400 / nand2 | 7400 | yes, gate 1 | READ |
| 17C | 4,5 > 6 (CL, TIMEOUT) | plain box | 7400 / nand2 | 7400 | yes, gate 2 | READ |
| 17C | 9,10 > 8 (CL > CL0) | plain box | 7400 / nand2 | 7400 | yes, gate 3 | READ |
| 17C | 12,13 > 11 (CDSTL, CARR > 19C.13) | plain box | 7400 / nand2 | 7400 | yes, gate 4 | READ |
| 1B | 9,10 > 8 (CLINT, IRFT) | plain box | 7400 / nand2 | 7400 | yes, gate 3 | READ |
| 1B | 12,13 > 11 (CLINT, IERR) | plain box | 7400 / nand2 | 7400 | yes, gate 4 | READ |
| 1B | 2,1 > 3 (CLINT, ORFT) | plain box | 7400 / nand2 | 7400 | yes, gate 1 | READ |
| 1B | 5,4 > 6 (CLINT, OERR) | plain box | 7400 / nand2 | 7400 | yes, gate 2 | READ |
| 11D | 3,2 > 1 (SCW, BD"5" > ISCW) | box with X | 7402 / nor2 (X corner to corner) | 7402 | yes, gate 1 | READ |
| 11D | 5,6 > 4 (BD"5", BD9 > OSCW) | box with X | 7402 / nor2 | 7402 | yes, gate 2 | READ |
| 11D | 8,9 > 10 (IDR > IDR1) | box with X | 7402 / nor2 | 7402 | yes, gate 3 | READ |
| 11D | 11,12 > 13 (ODW > COM 2601 pin 20) | box with X | 7402 / nor2 | 7402 | yes, gate 4 | READ |
| 9E | 2,3 > 1 (ICW, BD10 > 17D.1) | box with X | 7402 / nor2 | 7402 | yes, gate 1 | READ |
| 9E | 6,5 > 4 (11A.6, IOR) | box with X | 7402 / nor2 | 7402 | yes, gate 2 | READ |
| 9E | 8,9 > 10 (11A.3, IPE) | box with X | 7402 / nor2 | 7402 | yes, gate 3 | READ |
| 9E | 12,11 > 13 (BA6, 11E.3 > 13E.2) | box with X | 7402 / nor2 | 7402 | yes, gate 4 | READ |
| 3A | 3,2 > 1 (TINT, INIDENT) | box with X | 7402 / nor2 | 7402 | yes, gate 1 | READ |
| 3A | 5,6 > 4 (> OUTIDENT) | box with X | 7402 / nor2 | 7402 | yes, gate 2 | READ |
| 3A | 8,9 > 10 (both GND > H1) | box with X | 7402 / nor2 | 7402 | yes, gate 3 | READ |
| 3A | 11,12 > 13 (3B.6 > 5A, LEV10) | box with X | 7402 / nor2 | 7402 | yes, gate 4 | READ |
| 11C | 1 > 2 (BA1) | box with a right-pointing triangle | 7404 / not1 | 7404 | yes, gate 1 | READ |
| 11C | 3 > 4 (BA0) | box with triangle | 7404 / not1 | 7404 | yes, gate 2 | READ |
| 11C | 5 > 6 (CL > CL1) | box with triangle | 7404 / not1 | 7404 | yes, gate 3 | READ |
| 11C | 9 > 8 (DEQL > 7442 pin 12) | box with triangle | 7404 / not1 | 7404 | yes, gate 4 | READ |
| 11C | 13 > 12 (ICW0 > ICW1) | box with triangle | 7404 / not1 | 7404 | yes, gate 6 | READ |
| 11C | gate 5 (pins 11 > 10) | not found | - | 7404 | - | UNKNOWN |
| 7B | 5 > 6 (BD3), 3 > 4 (BD4), 1 > 2 (BD5), 13 > 12 (BD6), 9 > 8 (BD7), 11 > 10 (3B.8 > 5A, LEV12) | box with triangle | 7404 / not1 | 7404 | yes, gates 3, 2, 1, 6, 4, 5 (all six) | READ |
| 9B | 9 > 8 (BD2), 11 > 10 (BD8), 13 > 12 (BD9), 5 > 6 (BD15), 3 > 4 (BD13 > COM 2601 pin 40), 1 > 2 (TINPUT > 8098 enables) | box with triangle | 7404 / not1 | 7404 | yes, gates 4, 5, 6, 3, 2, 1 (all six) | READ |
| 11A | 1,2 > 3 (CARREN, CARR > CARRERR) | box split down the middle, triangle in the right half | 7408 / and2 | 7408 | yes, gate 1 | READ |
| 11A | 5,4 > 6 (SCEE, SCR > ISYNERR) | split box with triangle | 7408 / and2 | 7408 | yes, gate 2 | READ |
| 11A | 12,13 > 11 (ISR, OSR > SR) | split box with triangle | 7408 / and2 | 7408 | yes, gate 4 | READ |
| 11A | gate 3 (pins 9,10 > 8) | not found | - | 7408 | - | UNKNOWN |
| 5A | 9,10 > 8 (> pin 35 LEV12) | box with a pointed right end and a vertical bar | 7438 / nand-buf-oc2 (bar before the point) | 7438 | yes, gate 3 | READ |
| 5A | 1,2 > 3 (> pin 27 LEV10) | pointed box with bar | 7438 / nand-buf-oc2 | 7438 | yes, gate 1 | READ |
| 5A | 4,5 > 6 (> pin 15 INPUT) | pointed box with bar | 7438 / nand-buf-oc2 | 7438 | yes, gate 2 | READ |
| 5A | 12,13 > 11 (> pin 23 CONNECT) | pointed box with bar | 7438 / nand-buf-oc2 | 7438 | yes, gate 4 | READ |
| 3B | 9,10 / 13,1 > 8 (IRFTIE, IRFT / IERR, IERRIE) | two stacked boxes feeding an X box | 7450 entry / aoi-2x2 ("7451 is drawn the same way") | 7451 | pins fit gate 1 of the 7450 chip file (1C,1D / 1B,1A > 1Y); no 7451 chip file | READ (pins), INFERRED (7451 pin map = 7450 gate pins) |
| 3B | 3,2 / 5,4 > 6 (ORFTIE, ORFT / OERR, OERRIE) | stacked boxes feeding an X box | 7450 entry / aoi-2x2 | 7451 | pins fit gate 2 of the 7450 chip file (2B,2A / 2D,2C > 2Y) | READ (pins), INFERRED |

### Line drivers and receivers (large open triangles)

| Pos | Pins printed | Drawn as | Catalogue symbol | Chip | Fits chip file? | Status |
|---|---|---|---|---|---|---|
| 13A | 7 > 10 (pin 85, CALLING INDICATOR > CAL) | big triangle, one input | 75154 / line-driver1 | 75154 (printed under the group) | yes, section 4 (4A > 4Y) | READ |
| 13A | 6 > 11 (pin 83, TRANSMITTER TIMING > TTM) | big triangle | 75154 / line-driver1 | 75154 | yes, section 3 | READ |
| 13A | 5 > 12 (pin 81, RECEIVER TIMING > RTM) | big triangle | 75154 / line-driver1 | 75154 | yes, section 2 | READ |
| 13A | 4 > 13 (CARR0 > CARR1) | big triangle | 75154 / line-driver1 | 75154 | yes, section 1 | READ |
| 15A | 4 > 13 (pin 87, RECEIVED DATA > RDA) | big triangle | 75154 / line-driver1 | 75154 | yes, section 1 | READ |
| 15A | 6 > 11 (pin 89, READY FOR SENDING > RFS) | big triangle | 75154 / line-driver1 | 75154 | yes, section 3 | READ |
| 15A | 7 > 10 (pin 90, DATA SET READY > DSR) | big triangle | 75154 / line-driver1 | 75154 | yes, section 4 | READ |
| 15A | 5 > 12 (pin 88, SIGNAL DETECTOR > CARR) | big triangle | 75154 / line-driver1 | 75154 | yes, section 2 | READ |
| 17A | 2,4 > 11 (H, CDSTL > pin 91, CONNECT TO LINE, modem (20)) | big triangle, two inputs | 75150 / line-driver2 | 75150 | yes, driver 2 (S=2, 2A=4 > 2Y=11) | READ |
| 17A | 2,3 > 12 (RQTS > pin 92, modem (4)) | big triangle, two inputs | 75150 / line-driver2 | 75150 | yes, driver 1 (S=2, 1A=3 > 1Y=12) | READ |
| 19A | 2,4 > 11 (H, GND > pin 93, modem (23)) | big triangle, two inputs | 75150 / line-driver2 | 75150 (printed under the group) | yes, driver 2 | pins READ; position INFERRED (see Disagreements) |
| 19A | 2,3 > 12 (TDA > pin 95, TRANSM DATA, modem (2)) | big triangle, two inputs | 75150 / line-driver2 | 75150 | yes, driver 1 | pins READ; position INFERRED |

### Boxes (one rectangle per function, pin numbers on the lines)

| Pos | Pins printed | Printed type in box | Catalogue symbol | Fits chip file? | Status |
|---|---|---|---|---|---|
| 13E | 4,3 / 13,12 / 10,11 / 6,5 / 1,2 / 15,14 compare pairs; 7 strobe (GND); 9 out (DEQL) | DM 8160 | 8160 (block) | yes: pairs A1/B1 .. A6/B6, 7 = STROBE, 9 = Y | READ |
| 13D | 15,14,13,12 in; 2,3,4,5,6,7,9 out (ODW, OSR, OCW, IDR, SCW, ISR, ICW) | 7442 | 7442 (block) | yes: A,B,C,D in, outputs 1-7 | READ |
| 3C | upper: 13 clear, 11 clock, 12 D, 10 preset, 9 Q (IRFTIE); lower: 1, 3, 2, 4, 5 (ORFTIE) | 7474 | 7474 (block) | yes, flip-flop 2 and flip-flop 1 | READ |
| 1D | upper: 13, 11, 12, 10, 9 (IERRIE); lower: 1, 3, 2, 4, 5 (OERRIE) | 7474 | 7474 (block) | yes, both flip-flops | READ |
| 15C | upper: 13, 11, 12, 10 (CAL), 8 and 9 out (CDSTL); lower: 1, 3, 2, 4, 5 (PRQTS) | 7474 | 7474 (block) | yes, both flip-flops | READ |
| 19C | 1,14,15,2,3 to GND; 4 and 12 to H; 11 PRQTS; 10 TIMEOUTEN; 9 OSC; 13 from 17C.11; 5 out TIMEOUT | 7497 | 7497 (block) | yes: B,C,D,E,F rate inputs = 0, A = 1, 11 enable in, 10 strobe, 9 clock, 13 clear, 5 Z out | READ |
| 3D | 2,14 select; 10,11,12,13 > 9 (DAT0); 6,5,4,3 > 7 (DAT1); 1,15 GND | 74153 | 74153 (block) | yes | READ |
| 3E | same pin set; 7 = DAT2, 9 = DAT3 | 74153 | 74153 (block) | yes | READ |
| 5E | same pin set; 7 = DAT4, 9 = DAT5 | 74153 | 74153 (block) | yes | READ |
| 5D | same pin set; 7 = DAT6, 9 = DAT7 | 74153 | 74153 (block) | yes | READ |
| 13C | 2 (BA1), 14 (BA0) select; 6,5,4,3 > 7 (INT); 10,11,12,13 > 9 (DINPUT); 1,15 GND | 74153 | 74153 (block) | yes | READ |
| 1C | 14,13 > 12 (CLDI); 11,10 > 9 (CLEI); 2,3 > 4 (CLDO); 5,6 > 7 (CLEO); 1 = BA1, 15 = CL | 74157 | 74157 (block) | yes: four 2-to-1 sections, 1 SELECT, 15 STROBE | READ |
| 15B | 5,6 > 7 (TCLOCK); 2,3 > 4 (RCLOCK); 14,13 > 12 (RDATA); 11,10 > 9 (RQTS); 1 = TEST; 15 = GND | 74157 | 74157 (block) | yes | READ |
| 1A | 5,6 > 7 (CLINT); 14,13 > 12 (TINPUT); 2,3 > 4 (TCONNECT); 11,10 > 9 (TINT); 1 = INIDENT; 15 = GND | 74S157 | none of its own; the 74157 entry's datasheet covers the 74S157 | pins fit the 74157 chip file | READ |
| 11B | 1 clear (CL1); 9 clock (ICW1); 13>12 TEST, 11>10 TIMEOUTEN, 4>5 SCEE, 3>2 CARREN, 14>15 IACTIVE, 6>7 HALF | 74174 | 74174 (block) | yes, all six flip-flops | READ |
| 17D | half 1: 1 (from 9E.1), 2 (CL), 3 (H), 13 out (RR), 14/15 timing (220 pF, 10k); half 2: 10 (OSCD), 9 (TTM), 11 (H), 12 out (OSC), 6/7 timing (10 uF, 47k) | 74 221 | 74221 (block) | yes, both one-shots | READ |
| 7E | 2>3 (PE>IPE), 4>5 (TDATA>TDA), 6>7 (SCT>OERR), 10>9 (TBMT>ORFT), 12>11 (DA>IRFT), 14>13 (OR>IOR); 1 and 15 to GND | 8097 | 8097 (block) | yes, all six buffers, both enables tied on | READ |
| 5C | 6,4,2,10 > 7,5,3,9 (DAT0,1,2,6 > BD0,1,2,6, enable 1 = TINPUT inverted); 12,14 > 11,13 (BD0, BD1 in, enable 15 = GND) | 8098 | 8098 (block) | yes, all six | READ |
| 5B | 6,4,2,10 > 7,5,3,9 (DAT3,4,5,7 > BD3,4,5,7, enable 1); 12,14 > 11,13 (CARRERR, CARREN > BD8, BD9, enable 15 = ISR) | 8098 | 8098 (block) | yes, all six | READ |
| 7A | 6>7, 4>5, 2>3, 12>11, 14>13, 10>9 (RQTS, CARR, CDSTL, DSR, RFS, HALF > BD10-BD15); 15 = SR, 1 = SR node | 8098 | 8098 (block) | yes, all six | READ |
| 7-8C-D | as in the netlist (7-14, 18-25, 28-35, 2, 3, 5, 15, 16, 17, 26, 27, 36-40; +5V:1, -12V:6, GND:4) | COM 2601 | none (USRT, not in the catalogue) | no chip file | READ |
| 18-19E | 1,24 +5V; 2,11,12,13,14,23 GND; 3,22 +12V (pins 66/67); 10,15 -12V (pins 62/63) | VP12/12 | VP12/12 entry (voltage converter) | the VP12 chip file has no pins (no datasheet found) | READ (pins on the scan) |

### Other drawn elements

| Where | What is drawn | Catalogue | Status |
|---|---|---|---|
| 15E | 4 switches (1-4) to ground, a resistor to the supply drawn once over the group, outputs SMN3-SMN0 | not in the catalogue | READ (drawing) |
| 1E | same, outputs IDENT2-IDENT5 | not in the catalogue | READ |
| above 13D | 1k resistor, pull-up on DEQL (8160 pin 9 / 11C pin 9) | not in the catalogue | READ |
| 17D half 2 | 10 uF from pin 6 to 7, 47k from pin 7 to +5V | not in the catalogue | READ |
| 17D pin 12 | 220 ohm then 10 uF to ground, node OSCD | not in the catalogue | READ |
| 17D half 1 | 220 pF from pin 14 to 15, 10k from pin 15 to the supply | not in the catalogue | READ |
| 5C/5B enable node | 1000 pF to ground on 9B pin 2 | not in the catalogue | READ |
| SR node | 1000 pF to ground on 11A pin 11 | not in the catalogue | READ |
| pin 95 | 2200 pF to ground on the transmitted-data line | not in the catalogue | READ |
| below 5D | resistor marked "15A9" to the supply, giving H | not in the catalogue | READ (label), meaning INFERRED below |
| right edge | edge pins 80, 82, 84, 86, 94 tied to ground | not a symbol | READ |
| circles | edge-connector pins (BA, BD, LEV, IDENT/GRANT chain, modem pins 81-95); numbers in brackets = modem plug pins | not a symbol | READ |

## 2. Count per chip type against the printed parts list

Parts list on the right edge, read on the scan: 5 pk 7400, 3 7402, 3 7404, 1 7408, 1 7438,
1 7442, 1 7451, 3 7474, 1 7497, 5 74153, 2 74157, 1 "745157", 1 74174, 1 74221, 2 75150,
2 75154, 1 8097, 3 8098, 1 8160, 1 VP12/12, 1 COM 2601, total "40 pk". This matches the
reading given in the task. READ.

| Type | Listed | Found on the sheet | Positions |
|---|---|---|---|
| 7400 | 5 | 5 | 11E, 13B, 17B, 17C, 1B |
| 7402 | 3 | 3 | 11D, 9E, 3A |
| 7404 | 3 | 3 | 11C, 7B, 9B |
| 7408 | 1 | 1 | 11A |
| 7438 | 1 | 1 | 5A |
| 7442 | 1 | 1 | 13D |
| 7451 | 1 | 1 | 3B |
| 7474 | 3 | 3 | 3C, 1D, 15C |
| 7497 | 1 | 1 | 19C |
| 74153 | 5 | 5 | 3D, 3E, 5E, 5D, 13C |
| 74157 | 2 | 2 | 1C, 15B |
| 74S157 ("745157") | 1 | 1 | 1A (printed "74S157" in the box) |
| 74174 | 1 | 1 | 11B |
| 74221 | 1 | 1 | 17D |
| 75150 | 2 | 2 | 17A, 19A (position INFERRED) |
| 75154 | 2 | 2 | 13A, 15A |
| 8097 | 1 | 1 | 7E |
| 8098 | 3 | 3 | 5C, 5B, 7A |
| 8160 | 1 | 1 | 13E |
| VP12/12 | 1 | 1 | 18-19E |
| COM 2601 | 1 | 1 | 7-8C-D |
| **Total** | **40** | **40** | |

Spare gates (drawn nowhere on the sheet): 17B gate 4, 11C gate 5, 11A gate 3. UNKNOWN whether
they are used on another sheet; this sheet is the whole card, so most likely they are spare.

## 3. Not in the catalogue

| Item | What it looks like | Where | What it most likely is |
|---|---|---|---|
| 7451 | stacked boxes feeding an X box (same as the 7450 picture) | 3B | Dual AND-OR-INVERT without expander pins. The catalogue names it only inside the 7450 entry; there is no 7451 chip file. Guess: the gate pins are the same as the 7450's (they fit the drawing); not checked against a 7451 data sheet. |
| 74S157 | plain box, printed "74S157" | 1A | Schottky version of the 74157, same pins (guess from the part number; the pins fit the 74157 chip file). The catalogue has no 74S157 entry; the 74157 entry's datasheet title includes it. |
| COM 2601 | large box, printed "COM 2601" | 7-8C-D | SMC synchronous USRT, 40 pins. No catalogue entry, no chip file. Pin use is in docs/2A50-reading.md. |
| VP12/12 pinout | box printed "VP12/12" | 18-19E | In the catalogue by name, but its chip file has no pins (no datasheet found). The pins on this sheet (above) are the only pin information. Guess: a 24-pin DC-DC module making +12 V and -12 V from +5 V. |
| DIP switch blocks | four switch symbols to a ground rail, one resistor over the group | 15E, 1E | 4-position DIP switches with pull-up resistors (guess: a resistor pack, since one resistor is drawn for four lines). |
| Resistors | narrow upright rectangles, value written beside | 1k at DEQL, 47k and 10k at 17D, 220 ohm at OSCD, the switch pull-ups, "15A9" | Discrete resistors. "15A9": pin 9 of the 75154 at 15A (INFERRED from the label; the value is not legible on the sheet). That pin is a 5 kohm resistor inside the chip to +5 V, connected to nothing else (FACT, TI 1974 data book page 4-81, see catalog/chips/75154.json), so it gives H through 5 kohm without a separate resistor. |
| Capacitors | two parallel bars | 10 uF x 2, 220 pF, 1000 pF x 2, 2200 pF | Timing capacitors for the 74221 (10 uF, 220 pF), a filter on OSCD (10 uF), slow-down capacitors on the 8098 enable and SR lines (1000 pF), an edge-rate capacitor on the transmitted data (2200 pF). Purposes are guesses. |
| Edge pins / ground pins | numbered circles | everywhere | Backplane and modem-connector pins, not parts. |

## 4. Disagreements

1. **1C type.** The earlier netlist said 74153. The scan prints "74157" in the box and the pins
   (1 = select, 15 = strobe, 14,13>12 ...) are a 74157. Netlist corrected. READ.
2. **5A type.** The earlier netlist said 7451 for 5A. The scan draws 5A as pointed boxes with a
   bar (7438 shape) with pins 9,10>8 and 1,2>3, and the only 7451 on the list is 3B, which holds
   both AND-OR-INVERT gates (level 12 = gate 1, level 10 = gate 2). Netlist corrected. READ.
   docs/2A50-reading.md says "(7451 3B, 5A)" - 5A is the open-collector bus driver after 3B,
   not a second 7451.
3. **9B** was "inferred, type not checked": all six gates are read as 7404 inverters. Now READ.
4. **11D has four gates, not three.** The earlier netlist had 11D/1-3; gate 4 (11,12 > 13)
   makes the ODW strobe to COM 2601 pin 20. Added.
5. **"BD5" at 11D pins 2 and 5.** At 600 dpi the label still looks like "BD5". The reading notes
   take it as BD8 (INFERRED there, because bit 5 is a data bit and only bit 8 decodes the
   driver values). Note that BD8 also goes from 9B pin 10 to 11B pin 4 (SCEE). The scan alone
   does not settle it. UNKNOWN on the scan; the notes' BD8 reading stays INFERRED.
6. **19A or 13A.** The two lower 75150 drivers are labelled with a code that reads "13A" or
   "19A". 13A already holds four 75154 receivers (pins 4-7 > 10-13), so a 75150 cannot also be
   13A. Recorded as 19A, INFERRED.
7. **75154 is drawn as a receiver, the catalogue calls it a "Line driver".** On this sheet the
   four 13A and four 15A triangles take modem signals IN (received data, timing, carrier).
   The catalogue entry heads it "Line driver" with Y = NOT A. Not changed (catalogue is not
   ours to edit here); flagged.
8. **Catalogue entries for 75150 and 75154 say "Chip data: not written yet",** but chip files
   75150.json and 75154.json exist (possibly still in progress). The printed pins fit both files.
9. **Updated date.** The title block shows "Ajourført 28.11.73"; the netlist and notes say
   updated 1973-11-29. One digit of the date is hard to read; not changed. UNKNOWN which is right.
10. **Parts list count.** No disagreement: every listed type and count matches the drawing
    (40 packages).

## 5. Netlist changes made

- Parts corrected: 1C (74153 > 74157), 5A (7451 > 7438), 3B function, 9B (inferred > read).
- Parts added: 11E, 13B, 17B, 17C, 1B (7400), 9E, 3A (7402), 7B (7404), 11A (7408), 13C (74153),
  1A (74S157), 5C (8098), 13A, 15A (75154), 17A, 19A (75150, position inferred), 18-19E (VP12/12).
  Now 40 parts.
- Gates: every gate in the tables above (69 in all, including the 75150/75154 sections and the
  two 7451 AND-OR-INVERT gates as kind "other"). Nets left unchanged.
