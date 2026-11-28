# 2A50: how the modem card raises interrupt levels 10 and 12

Sheet 2A50 "SYNCHRONOUS MODEM BUFFER 1050" (NORD-10 drawings, PDF page 64), netlist
`sheets/nord-10/2A50-sync-modem-buffer-1050.rsc`. Every wire and pin named below was read
again on 2026-11-23 from the scan rendered at 1200 dpi and compared with the netlist. The
register-level reading of the whole card is in `docs/2A50-reading.md`.

How sure each statement is:

- **READ**: seen on the drawing (pin numbers, labels, wires, junction dots).
- **INFERRED**: worked out from the drawing plus something else (a chip data sheet, the
  parts list, the SINTRAN driver). The reason is given.
- **OPEN**: not settled.

Names: a signal written `X1` is true when its wire is high, `X0` is true when it is low
(ND writes the 1 or 0 as a small subscript). `P.35` is backplane pin 35. A chip pin is
written `3B.8` = chip at board position 3B, pin 8.

## The short version

- **Level 12 is the receive side.** It is raised when (a character has arrived AND the
  "character interrupt" is enabled) OR (an input error is present AND the "error interrupt"
  is enabled).
- **Level 10 is the transmit side.** It is raised when (the transmitter can take the next
  character AND its "character interrupt" is enabled) OR (an output error is present AND its
  "error interrupt" is enabled).
- The four enables are four flip-flops. Bits 0 and 1 of the input control word (ICW, register
  offset 3) set the two receive enables; bits 0 and 1 of the output control word (OCW, offset 7)
  set the two transmit enables.
- When the CPU answers the interrupt with IDENT, the card puts its ident code on the bus and
  at the same moment **switches off the enable(s) that caused the interrupt**. The level then
  drops. The driver must write the control word again to get the next interrupt. That is why
  SINTRAN writes 205 (ICW) or 5 (OCW) after every character.
- Master clear, or an ICW write with bit 4 set, switches all four enables off.

## Picture

```
 RECEIVE SIDE (level 12)
                                    ICW0 (end of write) clocks, BD0 / BD1 = data
                                         |                     |
 COM2601 26 RDA --7E 12>11-- IRFT1 --+   v                     v
                                     |  3C 9 IRFTIE1    1D 9 IERRIE1
                                     |   |  (cleared by CLDI0)  |  (cleared by CLEI0)
                                     v   v                      v
                                  3B pins 10,9 ----AND--+   pins 1,13 --AND--+
 IPE  (COM 16 >7E 2>3) --+                              |                    |
 CARRERR (11A 3)  -------+--9E 8,9>10--+                +------ OR ----------+
                                        +--11E 4,5>6 = IERR1 ---> 3B pin 13
 IOR  (COM 27 >7E 14>13) +--9E 5,6>4 --+                         |
 ISYNERR (11A 6) --------+                              3B pin 8 (low = request)
                                                                 |
                                                       7B 11>10 (inverter)
                                                                 |
                                               LEV12_1 ----------+---> 13C pin 4 (for IDENT)
                                                                 |
                                                       5A 9,10>8 (7438, open collector)
                                                                 |
                                                         P.35  LEV12_0 (bus line, low = request)

 TRANSMIT SIDE (level 10)
 COM2601 2 TBMT --7E 10>9-- ORFT1 --> 3B pins 2,3 (with ORFTIE1 from 3C pin 5) --+
 COM2601 5 SCT  --7E 6>7 -- OERR1 --> 3B pins 5,4 (with OERRIE1 from 1D pin 5) --+-- 3B pin 6
                                                                                  |
                                               3A 11,12>13 (NOR used as inverter) = LEV10_1
                                                                                  |---> 13C pin 6
                                                       5A 1,2>3 --> P.27  LEV10_0
```

## Level 12 step by step (receive side)

### The two conditions

1. **Character received (IRFT1).** The COM 2601 raises RDA ("received data available") on its
   pin 26 when a whole character is in its receive buffer. It goes through one buffer of the
   8097 at 7E (pin 12 in, pin 11 out) and becomes IRFT1. READ (pins); what RDA means is from the
   COM 2601 data sheet (see `docs/2A50-reading.md`), INFERRED.
2. **Input error (IERR1).** Four error sources are combined:
   - IPE (parity error): COM 2601 pin 16 → 7E pin 2 → 7E pin 3. READ.
   - IOR (overrun, a character was lost): COM 2601 pin 27 → 7E pin 14 → 7E pin 13. READ.
   - ISYNERR (a sync character arrived, counted as an error only when enabled): AND gate 11A
     pins 5 (SCEE1 = ICW bit 8, latch 11B pin 5) and 4 (SCR1, COM 2601 pin 17) → 11A pin 6. READ.
   - CARRERR (carrier lost, counted only when enabled): AND gate 11A pins 1 (CARREN1 = ICW bit 9,
     latch 11B pin 2) and 2 (CARR0, from line receiver 15A pin 12, modem "signal detector") →
     11A pin 3. READ.
   - NOR 9E pins 8 (CARRERR) and 9 (IPE) → 9E pin 10. NOR 9E pins 6 (ISYNERR) and 5 (IOR) →
     9E pin 4. NAND 11E pins 4 and 5 → 11E pin 6 = **IERR1 = IPE or IOR or ISYNERR or CARRERR**.
     READ. (A NOR followed by a NAND is an OR of all four inputs.)

### The two enables

- **IRFTIE1** = flip-flop 3C, upper half (7474): data pin 12 = BD0_1 (bus data bit 0, inverted
  at 5C pins 12>11), clock pin 11 = **ICW0**, preset pin 10 = H1 (tied high), clear pin 13 = CLDI0,
  output Q pin 9. READ.
- **IERRIE1** = flip-flop 1D, upper half: data pin 12 = BD1_1 (5C pins 14>13), clock pin 11 =
  ICW0, preset pin 10 = H1, clear pin 13 = CLEI0, output pin 9. READ.
- ICW0 is the decoder output 13D pin 9 (register offset 3). A 7474 loads on the rising edge of
  its clock, and ICW0 rises when the write ends, so **the enables take the new value at the end
  of the ICW write**. READ (label and wire); the edge rule is the 7474 data sheet.

### The gate

- 7451 at 3B, gate 1 (AND-OR-INVERT): pins 9 (IRFTIE1) and 10 (IRFT1) are one AND pair, pins 1
  (IERRIE1) and 13 (IERR1) the other; output pin 8 is LOW when either pair is all high. READ for
  the pins. The pairing (1 with 13, 9 with 10) is the 7450 pinout in `catalog/chips/7450.json`;
  there is no 7451 chip file, and the 7451 is the same gate without the expander pins. INFERRED.
- **3B pin 8 goes to one place only: inverter 7B pin 11** (net N_3B_8). This short two-pin net
  is correct and is the link the owner's trace stopped on: to go further back from 7B pin 11
  you step through the 7451 gate to its four inputs above. READ.
- 7B (7404) pin 11 → pin 10 = **LEV12_1** (high = this card requests level 12). READ.

### Onto the bus

- 5A (7438, open-collector NAND) pins 9 and 10 both take LEV12_1; output pin 8 goes to
  **backplane pin 35, LEV12_0**. A NAND with both inputs joined is an inverter, so pin 35 is
  pulled LOW while the card requests the level. Open collector lets every card on the bus pull
  the same line. READ (pins, circled 35). That 5A is a 7438: all five 7400s on the parts list
  are placed elsewhere (11E, 13B, 17B, 17C, 1B), the 7408 is 11A, and the only two-input quad
  left on the list is "1 - 7438". INFERRED from the count.
- LEV12_1 also goes to 13C pin 4, so the card knows during IDENT that it is the one asking
  (see below). READ.

### Truth table, level 12

| IRFTIE | IRFT | IERRIE | IERR | 3B pin 8 | LEV12_1 (7B pin 10) | P.35 |
|---|---|---|---|---|---|---|
| 0 | x | 0 | x | 1 | 0 | released (high) |
| 1 | 0 | 0 | x | 1 | 0 | released |
| 1 | 1 | x | x | 0 | 1 | **pulled low = request** |
| x | x | 1 | 1 | 0 | 1 | **pulled low = request** |
| 0 | x | 1 | 0 | 1 | 0 | released |

`x` = does not matter.

## Level 10 step by step (transmit side)

- **ORFT1** (transmitter can take a character): COM 2601 pin 2 TBMT ("transmit buffer empty")
  → 7E pin 10 → 7E pin 9. READ.
- **OERR1** (output error): COM 2601 pin 5 SCT → 7E pin 6 → 7E pin 7. SCT goes high when the
  transmitter had nothing new to send and sent its fill character instead (an underrun).
  READ (pins); the meaning is from the data sheet, INFERRED. OERR1 is also status bits 4 and 5
  of the output status register (5E pins 5 and 11).
- **ORFTIE1** = 3C lower half: data pin 2 = BD0_1, clock pin 3 = **OCW0** (13D pin 4), preset
  pin 4 = H1, clear pin 1 = CLDO0, output pin 5. READ.
- **OERRIE1** = 1D lower half: data pin 2 = BD1_1, clock pin 3 = OCW0, preset pin 4 = H1, clear
  pin 1 = CLEO0, output pin 5. READ.
- 7451 at 3B, gate 2: pins 3 (ORFTIE1) and 2 (ORFT1) one pair, pins 4 (OERRIE1) and 5 (OERR1)
  the other; output pin 6 LOW = request. READ (pairing INFERRED as for gate 1).
- 3B pin 6 → 7402 NOR 3A pins 11 and 12 (joined, so it is an inverter) → 3A pin 13 =
  **LEV10_1**. READ. (Level 12 uses a 7404 inverter for the same job, level 10 a spare NOR.)
- 5A pins 1 and 2 → 5A pin 3 → **backplane pin 27, LEV10_0**, pulled low on request. READ.
- LEV10_1 also goes to 13C pin 6. READ.

### Truth table, level 10

| ORFTIE | ORFT | OERRIE | OERR | 3B pin 6 | LEV10_1 (3A pin 13) | P.27 |
|---|---|---|---|---|---|---|
| 0 | x | 0 | x | 1 | 0 | released |
| 1 | 1 | x | x | 0 | 1 | **pulled low = request** |
| x | x | 1 | 1 | 0 | 1 | **pulled low = request** |
| 1 | 0 | 1 | 0 | 1 | 0 | released |

## What switches the enables on

| Write | Bit 0 | Bit 1 | READ |
|---|---|---|---|
| ICW (offset 3) | IRFTIE (3C pin 12) | IERRIE (1D pin 12) | yes |
| OCW (offset 7) | ORFTIE (3C pin 2) | OERRIE (1D pin 2) | yes |

Each write sets both enables of its side from bits 0 and 1 (a 0 bit switches it off).
SINTRAN's values (from `docs/2A50-reading.md`): ICW 205 and 2205 set IRFTIE and clear IERRIE;
ICW 2204 and 0 clear both; OCW 5 sets ORFTIE and clears OERRIE. So in the values seen so far
the driver uses only the character interrupts, never the error interrupts. INFERRED (driver
values from NDInsight, decoded against this wiring).

## What switches the enables off

All four flip-flops have their clear pin driven by the 74157 at 1C (outputs 12 CLDI0, 9 CLEI0,
4 CLDO0, 7 CLEO0). A 74157 output is low (= clear) in two cases.

### 1. Master clear, or ICW with bit 4

- 17B (7400) pins 5 (ICW1) and 4 (BD4_1) → 17B pin 6. 17B pins 10 (MC0, backplane pin 55,
  master clear) and 9 (17B pin 6) → 17B pin 8 = **CL1**. CL1 is high during master clear, or
  while an ICW with bit 4 set is being written. READ.
- CL1 → 1C pin 15 (strobe). While the strobe is high every 74157 output is forced low, so **all
  four enables are cleared**. READ (pins); the strobe rule is the 74157 data sheet.
- CL1 also clears the input control latch 11B (through 11C pins 5>6, CL1_0, to 11B pin 1) and,
  through 17C pins 9,10>8 (CL0), restarts the receiver one-shot 17D and the timeout logic. READ.

### 2. The IDENT that answers the interrupt

- 1B (7400) makes four NANDs, each of CLINT1 with one condition: pins 9,10 (CLINT, IRFT) → 8;
  pins 12,13 (CLINT, IERR) → 11; pins 2,1 (CLINT, ORFT) → 3; pins 5,4 (CLINT, OERR) → 6. READ.
- 1C (74157) select pin 1 = BA1_1 (bus address bit 1). The inputs, read with the rule "the pin
  number sits above its wire": pin 13 = 1B pin 8, pin 10 = 1B pin 11, pin 2 = 1B pin 3,
  pin 5 = 1B pin 6; pins 14, 11, 3, 6 = H1. READ.
  - BA1 = 1 (IDENT of level 12): CLDI0 = NAND(CLINT, IRFT), CLEI0 = NAND(CLINT, IERR);
    the transmit clears stay high.
  - BA1 = 0 (IDENT of level 10): CLDO0 = NAND(CLINT, ORFT), CLEO0 = NAND(CLINT, OERR);
    the receive clears stay high.
- CLINT1 is high only while this card holds an IDENT (next section). So during its IDENT, **each
  enable of the answered level is cleared if its own condition is present** - the character
  enable if a character is waiting, the error enable if an error is present, both if both.
  READ (wiring). The older reading said "the data enable, else the error enable"; the wiring
  has no "else".

### 3. The condition goes away

The enable stays on, but the request drops when the condition ends:

- Reading the data register (IDR, offset 0): IDR1 → COM 2601 pin 24 (RDAR), which resets RDA,
  so IRFT falls. READ (pins); chip behaviour from the data sheet, INFERRED.
- Writing the data register (ODW, offset 5): 11D pin 13 → COM 2601 pin 20 (TDS) loads the
  transmit buffer, so TBMT and ORFT fall. Same status.
- ICW bit 10 fires the one-shot 17D (9E pins 2 ICW0, 3 BD10_0 → 9E pin 1 → 17D pin 1;
  17D pin 13 = RR → COM 2601 pin 15), which resets the receiver's RDA, SCR, ROR and RPE.
  READ (pins); reset list from the data sheet, INFERRED.
- CARRERR ends when the carrier comes back or CARREN (ICW bit 9) is written 0; ISYNERR ends when
  SCR falls or SCEE (ICW bit 8) is written 0. READ (gates).

## How the card answers IDENT

The CPU answers an interrupt with IDENT. The bus passes an "ident" signal from card to card
(INIDENT in, OUTIDENT out); the first card that is requesting the level keeps it and puts its
ident code on the bus. On this card:

1. **Which level is asked.** 13C (74153) half 1: select A pin 14 = BA0_1, select B pin 2 =
   BA1_1; inputs pin 6 = LEV10_1, pin 5 = GND, pin 4 = LEV12_1, pin 3 = GND; output pin 7 = INT1.
   So INT = LEV10 when BA1,BA0 = 0,0 and INT = LEV12 when BA1,BA0 = 1,0; any other address gives
   0. READ. That the CPU puts the level on BA0/BA1 during IDENT in this way is INFERRED from this
   wiring.
2. **Freezing the answer.** 1A (74S157), select pin 1 = INIDENT0 (backplane pin 7), strobe
   pin 15 = GND. READ.
   - INIDENT0 high (no IDENT here): outputs take the B inputs: pin 7 CLINT = pin 6 GND = 0;
     pin 12 TINPUT = pin 13 DINPUT; pin 4 TCONNECT = pin 3 DEQL; pin 9 TINT = pin 10 INT.
   - INIDENT0 low (IDENT reaches this card): outputs take the A inputs, and pins 5, 14, 2, 11
     are all wired to TINT (1A pin 9). TINT feeds itself (pin 9 → pin 11), so it **holds the
     value INT had when IDENT arrived**, and CLINT, TINPUT and TCONNECT all equal it. READ.
3. **Keep or pass on.** 3A pins 3 (TINT) and 2 (INIDENT0) → NOR → 3A pin 1 → 3A pins 5,6 (NOR as
   inverter) → pin 4 = OUTIDENT0, backplane pin 6. OUTIDENT0 goes low (passes the IDENT to the
   next card) only when INIDENT0 is low and TINT is 0. If this card is requesting (TINT = 1) the
   chain stops here. READ.
   INGRANT0 (pin 9) is a plain wire to OUTGRANT0 (pin 8); the card does not use it. READ.
4. **Putting the code on the bus** (TINT = 1):
   - TINPUT1 → 5A pins 4,5 → pin 6 → backplane pin 15, INPUT0 pulled low (tells the CPU that
     data is on the bus); TCONNECT1 → 5A pins 12,13 → pin 11 → backplane pin 23, CONNECT0. READ.
   - TINPUT1 → 9B pins 1>2 → enable pin 1 of the 8098 bus drivers 5C and 5B (bits 0-7). READ.
   - No register is selected, so 11E pin 8 (NAND of OSR0, IDR0) and 11E pin 11 (NAND of ISR0,
     IDR0) are both 0, and the four status multiplexers 3D, 3E, 5E, 5D pick input 0:
     bits 0 and 1 = GND (marked "(STRAP)(GND)", 3D pins 10 and 6), bits 2-5 = ident switches
     1E (IDENT2 3E pin 6, IDENT3 3E pin 10, IDENT4 5E pin 6, IDENT5 5E pin 10), bits 6 and 7 =
     GND (5D pins 6 and 10). READ. That the register decoder is idle during IDENT (IOXE not
     active) is INFERRED.
   - Bits 8-15 are not driven: 5B's second pair is enabled only by ISR0 (5B pin 15) and 7A only
     by SR0 (status read). READ.
   - So the **same ident code answers on both levels**, and it is always a multiple of 4.
5. **Switching the request off.** CLINT1 = TINT = 1 clears the enable(s) of the answered level
   through 1B and 1C (above). The level line drops, but TINT stays frozen until INIDENT0 goes
   high again, so the answer on the bus does not change mid-cycle. READ (wiring).

Outside IDENT the same parts give the normal IOX answers: TCONNECT = DEQL (the address is this
card's), and TINPUT = DINPUT from 13C half 2 (pins 10 and 12 = DEQL, 11 and 13 = GND, output 9),
which is DEQL with BA0 = 0, so INPUT is answered for the even (read) registers. READ.

## Status bits that show the interrupt state

The status multiplexers (selects from 11E as above) show, READ:

| Bit | ISR (input status, offset 2) | OSR (output status, offset 6) |
|---|---|---|
| 0 | IRFTIE (3D pin 12) | ORFTIE (3D pin 11) |
| 1 | IERRIE (3D pin 4) | OERRIE (3D pin 5) |
| 3 | IRFT (3E pin 12) | ORFT (3E pin 11) |
| 4 | IERR (5E pin 4) | OERR (5E pin 5) |
| 5 | ISYNERR (5E pin 12) | OERR (5E pin 11) |
| 6 | IPE (5D pin 4) | 0 |
| 7 | IOR (5D pin 12) | 0 |
| 8 | CARRERR (5B pin 12) | not driven |

## What stays open

- **OPEN: ICW with bit 4 and bits 0/1 together.** CL1 (from ICW1 and BD4) clears the enables
  while the write lasts; the enables are clocked by ICW0 rising at the end of the same write,
  just as CL1 falls. Which wins depends on chip timing that the drawing does not give. SINTRAN's
  known ICW values never set bit 4.
- **OPEN: no 7451 chip file.** The 3B pin pairing is taken from the 7450 file (the 7451 is the
  7450 without expander pins). The pins drawn fit it exactly; a 7451 data sheet has not been read.
- **INFERRED: the COM 2601 flag behaviour** (RDA, TBMT, SCT, ROR, RPE, SCR, and what RDAR, TDS
  and RR reset) is from its data sheet as written in `docs/2A50-reading.md`; not re-checked here.
- **Subscript of DA at COM 2601 pin 26 is smudged** (the 8097 end at 7E pin 12 is clearly DA1).
  The netlist keeps it as one active-high signal.
- **The viewer's back-trace** stopped at N_3B_8. The netlist is complete there (3B.8 to 7B.11,
  and gate 3B/1 lists its four inputs). Whether the viewer steps back through a gate of kind
  `other` (the 7451) was not checked; that is viewer code, not the sheet.

## Differences from the earlier reading

| What | Old | New | Proof |
|---|---|---|---|
| Clock of the receive enables IRFTIE and IERRIE: 3C pin 11 and 1D pin 11 | on ICW1 (after inverter 11C pins 13>12) | on **ICW0** (decoder 13D pin 9 directly) | At 1200 dpi the label on the wire into 3C pin 11 (and on down to 1D pin 11) is ICW with the filled-dot subscript, the same mark as CLDI0, CLEI0 and ICW0 at 15C pin 11 and 9E pin 2. ICW1 is drawn only at 11C pin 12 → COM 2601 pin 39, 17B pin 5 and 11B pin 9. It also matches the transmit side, where OCW0 clocks 3C pin 3 and 1D pin 3. Effect: the receive enables load at the end of the ICW write, not the start. Changed in the flat sheet and in `2A50-sync-modem-buffer-1050-blocks.rsc` (block `interrupt-logic` port renamed ICW_1 → ICW_0); that copy was removed 2026-11-28, the flat sheet is the one 2A50 file. |
| Select pins 2 and 14 of 3E, 5E, 5D | `inferred` ("taken to match 3D") | `read` | The two select lines from 11E pins 11 and 8 are drawn running down past 3D with a junction dot at pin 2 and pin 14 of each of 3E, 5E and 5D. Same change in the blocks file. |
| `docs/2A50-reading.md`, IDENT clear | "data enable if the data condition was set, else the error enable" | each enable of the answered level is cleared if its own condition is set (both if both) | 1B makes four independent NANDs (CLINT with IRFT, IERR, ORFT, OERR) into four separate 1C inputs; there is no gate that makes one exclude the other. |
| 3B part note | pin list only | adds that pin 8 goes only to 7B pin 11, and the AND pairing | read again on the scan |

Checked and found **unchanged**: every other pin named in this document; 13C inputs (6 LEV10,
5 GND, 4 LEV12, 3 GND, 10 and 12 DEQL, 11 and 13 GND, selects 14 BA0, 2 BA1, strobes 1, 15 GND);
1C inputs (13, 10, 2, 5 from 1B; 14, 11, 3, 6 = H1); 1A all 16 pins; 7E all pins (strobes 1 and
15 = GND); 5A, 7B, 3A, 9E, 11E, 11A gates; 3D inputs. The rest of the sheet was spot-checked chip
by chip (13E, 13D, 11C, 11D, 9B, 7B, 5C, 5B, 7A, 15C, 15B, 11B, 17B, 17C, 17D, 19C, 13B, 13A,
15A, 17A, 19A, 18-19E, COM 2601 pins on the left and right edges) and matched the netlist; the
parts list (40 parts) matches the parts in the file type by type.
