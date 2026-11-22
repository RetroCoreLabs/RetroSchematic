# ND synchronous modem card: what the schematic says (2026-11-22)

The emulator card built from this reading is `Emulated.HW/ND/CPU/NDBUS/NDBusSyncModem.cs` in the
RetroCore repository. The netlist of the sheet is `sheets/nord-10/2A50-sync-modem-buffer-1050.json`;
the scan is `docs/sources/nord-10/NORD-10-drawings.pdf`, page 64.

## Sources

| Source | What it is |
|---|---|
| NORD-10 drawings, scanned PDF, page 64 | "SYNCHRONOUS MODEM BUFFER 1050", sheet **2A50**, A/S Norsk Data-Elektronikk, drawn 6.11.73, updated 29.11.73. The NDInsight repository holds the OCR text as `Reference-Manuals/10/NORD-10 drawings.md`, but **the OCR text of this page is useless** (it describes RS485, Ethernet and an HVAC block). Everything below was read from the scanned drawing itself, rendered at 400 and 1200 dpi. |
| NDInsight `Installation/Communication/IBM-Mainframe/HOW-SINTRAN-TALKED-TO-IBM.md`, sections 2.1-2.1b | The SINTRAN driver's use of the card (IOX values, sequences) and the address/ident table. |

**This is the NORD-10 card.** Whether the ND-100 card (ND 107010 / "ND 701") has the same
registers is NOT proved by any document. What supports it: every IOX address and every value
the SINTRAN driver writes decodes cleanly against this drawing (see below).

Status words used below:
- **READ**: read off the drawing.
- **INFERRED**: follows from the drawing plus a stated assumption.
- **UNKNOWN**: not on the drawing; needs the COM 2601 data sheet or a real card.

## Parts

USRT chip **COM 2601** (SMC), position 7-8C-D. Bus drivers 8097/8098, line drivers
75150/75154, address comparator DM8160 (13E), register decoder 7442 (13D), status
multiplexers 74153 (3D, 3E, 5E, 5D), control latches 7474 (3C, 1D, 15C) and 74174 (11B).
READ (parts list on the sheet).

## Address and ident

- Comparator 13E matches BA3, BA4, BA5, BA9 against four switches (15E, SWN0-3); BA6 must be
  1 and BA7, BA8 must be 0. That gives 16 positions: **100-177 and 1100-1177 (octal)**, eight
  registers each. READ. This agrees with the 16-modem table in the NDInsight document.
- Ident code: when the CPU asks for an ident, the status multiplexers put switches 1E
  (IDENT2-IDENT5) on **bits 2-5** and wire bits 0-1 to ground (marked "STRAP (GND)").
  So **every ident code on this card is a multiple of 4**. READ.
  - Modems 1-8 in the table (4, 14, 20, 24, 30, 34, 40, 10) all fit.
  - Modem 16's ident **75** in the pasted table **cannot be set on this card** (bit 0 is
    strapped to 0). Either the paste is wrong or the ND-100 card differs.
- The same ident answers on both levels (one switch set). READ.
- Interrupt chain: INIDENT/OUTIDENT and INGRANT/OUTGRANT pass through (1A, 3A). READ.

## Register decode (7442 at 13D)

The 7442 gets BA0 and BA1 through inverters (11C) but **BA2 straight**. The ND bus is
active-low (the `0` subscripts on the drawing), so BA2 is decoded inverted.
INFERRED from that assumption. The result matches the driver exactly:

| Offset | IOX (modem 1) | Name on drawing | Direction | Driver use |
|---|---|---|---|---|
| 0 | 100 | IDR | read | received character |
| 1 | 101 | SCW | write | sync register (values 777, 426, ...) |
| 2 | 102 | ISR | read | (driver read not found by NDInsight) |
| 3 | 103 | ICW | write | 2204, 205, 2205, 0 |
| 4 | 104 | (no decoder output) | - | - |
| 5 | 105 | ODW | write | character to send |
| 6 | 106 | OSR | read | - |
| 7 | 107 | OCW | write | 5, 0 |

What offset 4 does (read or write) is UNKNOWN: the card's bus answer logic (13C, DEQL)
was not fully traced.

## SCW, offset 1: sync character write

Data bits 0-7 go straight to the COM 2601 data inputs (READ). Two 7402 NOR gates (11D)
make the chip's two strobes:

- `ISCW` (chip pin 21, receiver sync character) = SCW selected **and bit 8 = 0**.
- `OSCW` (chip pin 18, transmitter fill character) = SCW selected **and bit 9 = 0**.

The bit-8 label is printed so that it looks like "BD5" in the scan. It is read as **BD8**
because (a) bit 5 is a data bit of the character and cannot also be a select, and (b) only
bit 8 decodes every driver value. INFERRED. Check against the driver values:

| Value (octal) | Character | Bit 8 | Bit 9 | Loads | Driver comment |
|---|---|---|---|---|---|
| 777 | 377 | 1 | 0 | fill char 377 | filler 377 |
| 426 | 026 | 1 | 0 | fill char 026 | ASCII sync |
| 462 | 062 | 1 | 0 | fill char 062 | EBCDIC sync |
| 1026 | 026 | 0 | 1 | receive sync 026 | sync on 26 received |
| 1062 | 062 | 0 | 1 | receive sync 062 | sync on 62 received |
| 1226 | 226 | 0 | 1 | receive sync 226 | sync on 226 received |
| 626 | 226 | 1 | 0 | fill char 226 | 226 between data |

## ICW, offset 3: input control (READ unless marked)

| Bit | Signal | Effect |
|---|---|---|
| 0 | IRFTIE | enable level-12 interrupt on "character received" (7474 3C) |
| 1 | IERRIE | enable level-12 interrupt on input error (7474 1D) |
| 2 | CDSTL and IACTIVE | "connect data set to line" (DTR, line driver to pin 20 of the modem plug) and input active (7474 15C, 74174 11B) |
| 3 | TEST | test mode: 74157 15B swaps the modem clocks and received data for an internal oscillator and a loop-back of the sent data |
| 4 | (CL) | with ICW: clear the card (17B, same line as master clear MC) |
| 7 | TIMEOUTEN | enable the timeout counter (7497 19C); a timeout clears CDSTL |
| 8 | SCEE | enable "sync character received" as an input error (ISYNERR) |
| 9 | CARREN | enable "carrier missing" as an input error (CARRERR) |
| 10 | (RR) | pulse the COM 2601 receiver reset (one-shot 74221 17D to chip pin 15) |
| 11-14 | to COM 2601 pins 38, 36, 40, 37 | chip control: character length and parity, see "The COM 2601" below |
| 15 | HALF | half duplex: request-to-send is blocked while carrier is present (13B) |
| 5, 6 | - | not connected |

What this means for the driver values:
- **2204** = bits 2, 7, 10: DTR on, timeout on, receiver reset (start hunting for sync), no interrupt.
- **2205** = same, plus bit 0: receiver reset and interrupt per character.
- **205** = bits 0, 2, 7: re-enable the per-character interrupt, no reset.
- **0**: everything off, DTR dropped.

## OCW, offset 7: output control (READ)

| Bit | Signal | Effect |
|---|---|---|
| 0 | ORFTIE | enable level-10 interrupt on "ready for next character" (3C) |
| 1 | OERRIE | enable level-10 interrupt on output error (1D) |
| 2 | PRQTS | request to send (15C) |

Driver value **5** = interrupt on, request to send on. **0** = both off.

## Status words (74153 multiplexers 3D, 3E, 5E, 5D and buffers 5B, 7A; READ)

| Bit | ISR (offset 2) | OSR (offset 6) | IDR (offset 0) |
|---|---|---|---|
| 0 | IRFTIE | ORFTIE | data bit 0 |
| 1 | IERRIE | OERRIE | data bit 1 |
| 2 | IACTIVE | OACTIVE (= ready for sending from modem) | data bit 2 |
| 3 | IRFT (chip DA, character ready) | ORFT (chip TBMT, transmit buffer empty) | data bit 3 |
| 4 | IERR (any input error) | OERR | data bit 4 |
| 5 | ISYNERR | OERR | data bit 5 |
| 6 | IPE (parity error) | 0 | data bit 6 |
| 7 | IOR (overrun) | 0 | data bit 7 |
| 8 | CARRERR | not driven | 0 |
| 9 | CARREN | not driven | 0 |
| 10 | RQTS | RQTS | 0 |
| 11 | CARR | CARR | 0 |
| 12 | CDSTL | CDSTL | 0 |
| 13 | DSR | DSR | 0 |
| 14 | RFS (ready for sending) | RFS | 0 |
| 15 | HALF | HALF | 0 |

- OERR comes from the chip's SCT pin ("sync/fill character transmitted", i.e. the
  transmitter ran empty and sent fill). READ: 7E pin 6 to 7. That it means underrun is INFERRED.
- IERR = IPE or IOR or ISYNERR or CARRERR (11E, 9E). READ.
- **Polarity of bits 10-14 is UNSETTLED.** They pass through an inverting 8098 from signals
  drawn with the `0` (active-low) subscript, while bit 15 comes from a `1` signal. Taken at
  face value, bits 10-14 read 1 when the modem line is OFF (the ND terminal card has the
  same "1 = carrier missing" style), but this was not proved.

## Interrupts (READ)

- Level 12 = (IRFTIE and IRFT) or (IERRIE and IERR) (7451 3B, 5A).
- Level 10 = (ORFTIE and ORFT) or (OERRIE and OERR).
- When the CPU's IDENT is answered, multiplexer 1C clears the enable that caused it
  (CLDI, CLEI, CLDO, CLEO): data enable if the data condition was set, else the error enable.
  This is why the driver writes 205 / 5 again after every character: the IDENT has
  switched the interrupt enable off. INFERRED from the wiring and the driver together.
- Reading IDR resets the chip's DA (pin 24). Writing ODW loads the chip's transmit buffer
  (pin 20), which clears TBMT. READ (pins wired); the chip behaviour is UNKNOWN until the
  data sheet is found.

## The COM 2601 (added later the same day, from its data sheet)

Data sheet: Standard Microsystems COM2601 "Universal Synchronous Receiver/Transmitter",
catalogue pages 91-98 (found on datasheet4u.com; SMC's 1984 data catalogue on bitsavers lists
the AMI S2350 as a functional equivalent, but the S2350's pin-out differs, so its pin numbers
must not be used here).

Pin match against the drawing (READ on both): DB1-DB8 = pins 7-14 = bus bits 0-7; TDS 20 = ODW;
TSS 18 = OSCW; RSS 21 = ISCW; RDAR 24 = IDR; RDE 25 tied high; CS 39 = ICW; RR 15 = one-shot
17D; TSO 3 = transmitted data; RSI 22, RCP 23, TCP 19 from multiplexer 15B; RD8-RD1 = pins
28-35 = IDATA7-0; TBMT 2, SCT 5, RPE 16, SCR 17, RDA 26, ROR 27 to the 8097 at 7E.

**ICW bits 11-14 = the chip's control bits** (NDB1 38 = bit 11, NDB2 36 = bit 12, NPB 37 = bit 14
straight from the active-low bus, POE 40 = bit 13 through inverter 9B):

| Bits 12, 11 | Data bits | | Bit 14 | Bit 13 | Parity |
|---|---|---|---|---|---|
| 0 0 | 8 | | 0 | - | none |
| 0 1 | 7 | | 1 | 0 | odd |
| 1 0 | 6 | | 1 | 1 | even |
| 1 1 | 5 | | | | |

Pin functions from the data sheet; the polarity is INFERRED from the active-low bus. It is the
same encoding as the ND terminal card's control bits 11-12, and SINTRAN's 0 gives 8 bits, no
parity, as bisync needs.

**Sync hunt** (data sheet, pin 15 and the receiver flow chart): RR high resets RDA, SCR, ROR and
RPE; RR going low puts the receiver in search mode. In search mode it shifts one bit at a time
and compares the shift register with the receiver sync register after every bit. On a match
SCR goes high, the sync character is loaded into the receiver buffer (so it DOES reach the CPU,
with RDA) and the receiver goes to character mode: every following character is loaded into the
buffer. SCR is set again for each later character equal to the sync register, and reset by the
next one that is not.

**Flags** (data sheet):
- RDA: high when a whole character is in the buffer, one receive-clock edge after the transfer
  (timing note 3). RDAR (reading IDR) resets it.
- ROR: set if the previous character had not been read (RDA still set) when the next one moved
  into the buffer.
- RPE: received parity disagrees; forced low when no parity is selected.
- TBMT: high when the data buffer may be loaded; low after TDS (writing ODW); high again when
  the character moves into the shift register.
- SCT: the shift register is loaded at the start of the last bit of the current character
  (timing note 1). If ODW was written in time the next character comes from the data buffer and
  SCT goes low; if not, the transmitter sync register is sent and SCT goes high (underrun fill).

Still not stated by the data sheet: the order the bits go out on the line (LSB first is
assumed), whether the parity bit takes part in the sync comparison, and the power-on state of
the transmitter.

## ND-830005.3 chapter 17 (Synchronous Modem Test Program, ND-100/110)

PDF pages 211-215, read on the page images. It calls the ND-100 card the "1050 card", with
device number switches at 15E and ident switches at 1E, the same as sheet 2A50. That is
evidence the ND-100 card is this design, not proof of identical wiring. It confirms:
- test mode loops the output back to the input at **19200 bit/s** (the drawing does not give
  the internal clock's rate);
- output status bit 5 = "synch transmitted" (matches OSR bit 5 = SCT);
- input status bit 7 = overrun;
- "STATUS: 004200 = CARRIER MISSING, OVERRUN": bit 11 = 1 means carrier MISSING. This settles
  the polarity question for bit 11, and by the same circuit for bits 10, 12, 13 and 14 (they
  read 1 when the signal is off; INFERRED for those four);
- input status bits 2, 4, 5 and 9 are "tested for correct setting" (meaning not given; on this
  drawing they are IACTIVE, IERR, ISYNERR and CARREN);
- the receiver-reset test sets the input sync register to 26 and then 62 and expects interrupts
  only on those characters (matches the sync hunt above).

## Not known

1. Offset 4 (no decoder output) - what an IOX there does. Only a real card or a fuller trace
   of the bus answer logic (13C, DEQL) can tell.
2. The timeout counter's period (7497 rate multiplier 19C clocked from the 74221 oscillator 17D;
   component values on the drawing not worked out) and exactly what starts and stops it.
3. Serial bit order on the line, and sync comparison with parity (data sheet silent).
4. Power-on state of the USRT transmitter.
5. Modem 16's ident code 75 (impossible on this card).
6. Whether the ND-100 card's wiring is identical (only the test program's "1050 card" so far).
