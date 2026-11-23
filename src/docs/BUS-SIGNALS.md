# Bus signals and connectors

These files hold what Norsk Data's own documents say about the signals on the I/O bus of a
NORD-10 and of a NORD-100 (ND-100), and about the connectors a card uses. Purpose: any card-edge
signal on a netlist sheet can be looked up and given a plain-words meaning.

| File | What it holds |
|---|---|
| `catalog/signals/nd-bus-nord10.json` | NORD-10 I/O bus, as seen at an I/O device card position (55 entries) |
| `catalog/signals/nd-bus-nd100.json` | NORD-100 / ND-100 bus on plug C (68 entries, every pin of the table) |
| `catalog/connectors/nord10-io-card-backplane.json` | NORD-10 I/O card connector to the backplane |
| `catalog/connectors/nord10-modem-front.json` | the modem (V.24) lines of the 2A50 card |
| `catalog/connectors/nd100-c-connector.json` | NORD-100 module plugs C (bus, 96 pins) and A, B (64 lines each) |

**The two buses are different and are kept apart.** The NORD-10 I/O positions have separate
address lines (BA0-BA17) and data lines (BD0-BD15) on plain pin numbers 0-99. The NORD-100 bus
shares 24 lines between address and data (BD0-BD23, with BAPR "address present" and BDAP "data
present"), uses a BDRY / BINACK handshake, and sits on plug C with rows a, b, c and pins 1-32.
Sheet 2A50 is a NORD-10 card; its pins are NOT mapped onto the NORD-100 connector.

## File format

Header: `catalogFormat` = `nd-bus-signals` (signals) or `nd-connector` (connectors),
`catalogFormatVersion` = 1, `id`, `machine`, `title`, `conventions`, `sources`.

Every fact is an object `{ value, source, page, confidence, note }`:

- `source` is an id from the file's `sources` list (document number and title);
- `confidence` is `read` (stated in the document), `inferred` (worked out; the note says from
  what) or `open` (not known; `value` is then `"open"`);
- `active`: `low` = true when low (ND suffix 0), `high` = true when high (suffix 1), `none` = power;
- `direction` is seen from a card: `in`, `out`, `bidir`, or `out, open collector (shared)`;
- `pins`: a list of `{ connector, pin }` (NORD-10; connector is `open`, see below) or
  `{ connector, row, pin }` (NORD-100).

A signal entry has `name` (as ND writes it on the backplane), `namesOnCards` (the name a card
drawing uses for the same pin, e.g. LEV10 for BINT10), `group`, `active`, `direction`, `pins`,
`description`, and on NORD-100 sometimes `cpuCardDriver` (the driver chip on the ND-120 3202D CPU
card, from a secondary source).

## Sources

| Id | Document | Used for |
|---|---|---|
| BW | Backwiring til NORD-10 (A/S Norsk Data-Elektronikk): connector specification list, a connector list for each of the 32 card positions, and a signal code list | NORD-10 pin numbers and names; which board drives a line. Positions A25-A32 are the I/O device slots, A22 is I/O control, A23 I/O address, A24 I/O data. The A22 and A25 pages were checked against the page images (PDF pages 25 and 28, printed pages 23 and 26); the OCR of A26-A32 agrees with A25 pin for pin. |
| S2A50 | NORD-10 drawings, sheet 2A50 SYNCHRONOUS MODEM BUFFER 1050, as read in this repository | card-side names, power pins, how a card drives each line, the modem pins |
| N10S | ND-06.008.01 NORD-10/S Reference Manual, Revision A | IOX address width, IDENT, interrupt levels for devices, the I/O system |
| N100 | ND-06.016.01 NORD-100 Input/Output System (1980) | Appendix F: backplane table (PDF page 214, printed F-2, read from the page image) and the definition of every signal; section I.2.3: plugs A, B, C |
| NDI | NDInsight repository, SINTRAN/Devices/BusSignals/ND-100-BUS-C-CONNECTOR.md | secondary: DIN 41612 connector type, driver chips and pull-ups on the ND-120 3202D CPU card |

The NORD-10/S manual says signal levels, definitions and timing are in the "NORD-10 Input/Output
manual (ND-06.004)". That manual was not found on this machine. It is the document that would
close most of the open NORD-10 items below.

## NORD-10 I/O bus (I/O device positions)

Pin numbers are those of the backwiring list and of the circled numbers on the card drawings;
they agree for every 2A50 bus pin. All bus names carry suffix 0. Directions are inferred from
the backwiring (a `G` before a signal marks the pin that drives it, worked out from the IDENT
chain) and from how 2A50 drives each line.

| Name | Name on 2A50 | Group | Active | Direction (from the card) | Pin(s) | What it does | Meaning confidence |
|---|---|---|---|---|---|---|---|
| BA0 | BA0 | address | low | in | 32 | Bus address bit 0. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA1 | BA1 | address | low | in | 33 | Bus address bit 1. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA2 | BA2 | address | low | in | 34 | Bus address bit 2. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA3 | BA3 | address | low | in | 36 | Bus address bit 3. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA4 | BA4 | address | low | in | 37 | Bus address bit 4. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA5 | BA5 | address | low | in | 38 | Bus address bit 5. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA6 | BA6 | address | low | in | 40 | Bus address bit 6. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA7 | BA7 | address | low | in | 41 | Bus address bit 7. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA8 | BA8 | address | low | in | 42 | Bus address bit 8. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA9 | BA9 | address | low | in | 44 | Bus address bit 9. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA10 |  | address | low | in | 45 | Bus address bit 10. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA11 |  | address | low | in | 46 | Bus address bit 11. Use on I/O cards not documented in the sources found. | inferred |
| BA12 |  | address | low | in | 48 | Bus address bit 12. Use on I/O cards not documented in the sources found. | inferred |
| BA13 |  | address | low | in | 49 | Bus address bit 13. Use on I/O cards not documented in the sources found. | inferred |
| BA14 |  | address | low | in | 50 | Bus address bit 14. Use on I/O cards not documented in the sources found. | inferred |
| BA15 |  | address | low | in | 52 | Bus address bit 15. Use on I/O cards not documented in the sources found. | inferred |
| BA16 |  | address | low | in | 53 | Bus address bit 16. Use on I/O cards not documented in the sources found. | inferred |
| BA17 |  | address | low | in | 54 | Bus address bit 17. Use on I/O cards not documented in the sources found. | inferred |
| BD0 | BD0 | data | low | bidir | 10 | Bus data bit 0 (16-bit data word between the CPU A register and the card). | inferred |
| BD1 | BD1 | data | low | bidir | 12 | Bus data bit 1 (16-bit data word between the CPU A register and the card). | inferred |
| BD2 | BD2 | data | low | bidir | 13 | Bus data bit 2 (16-bit data word between the CPU A register and the card). | inferred |
| BD3 | BD3 | data | low | bidir | 14 | Bus data bit 3 (16-bit data word between the CPU A register and the card). | inferred |
| BD4 | BD4 | data | low | bidir | 16 | Bus data bit 4 (16-bit data word between the CPU A register and the card). | inferred |
| BD5 | BD5 | data | low | bidir | 17 | Bus data bit 5 (16-bit data word between the CPU A register and the card). | inferred |
| BD6 | BD6 | data | low | bidir | 18 | Bus data bit 6 (16-bit data word between the CPU A register and the card). | inferred |
| BD7 | BD7 | data | low | bidir | 20 | Bus data bit 7 (16-bit data word between the CPU A register and the card). | inferred |
| BD8 | BD8 | data | low | bidir | 21 | Bus data bit 8 (16-bit data word between the CPU A register and the card). | inferred |
| BD9 | BD9 | data | low | bidir | 22 | Bus data bit 9 (16-bit data word between the CPU A register and the card). | inferred |
| BD10 | BD10 | data | low | bidir | 24 | Bus data bit 10 (16-bit data word between the CPU A register and the card). | inferred |
| BD11 | BD11 | data | low | bidir | 25 | Bus data bit 11 (16-bit data word between the CPU A register and the card). | inferred |
| BD12 | BD12 | data | low | bidir | 26 | Bus data bit 12 (16-bit data word between the CPU A register and the card). | inferred |
| BD13 | BD13 | data | low | bidir | 28 | Bus data bit 13 (16-bit data word between the CPU A register and the card). | inferred |
| BD14 | BD14 | data | low | bidir | 29 | Bus data bit 14 (16-bit data word between the CPU A register and the card). | inferred |
| BD15 | BD15 | data | low | bidir | 30 | Bus data bit 15 (16-bit data word between the CPU A register and the card). | inferred |
| BIOXE | IOXE | control | low | in | 11 | IOX enable: the strobe that tells every card that an IOX transfer is under way, so the card compares BA against its own address. | inferred |
| BINPUT | INPUT | control | low | out, open collector (shared) | 15 | The addressed card says the transfer is an input (card to CPU). | inferred |
| BCON | CONNECT | control | low | out, open collector (shared) | 23 | The card answers that it recognised its address (connected). | inferred |
| BINT10 | LEV10 | interrupt | low | out, open collector (shared) | 27 | Interrupt request on program level 10. Levels 15, 13, 12, 11 and 10 are the ones available to external devices. | inferred |
| BINT11 |  | interrupt | low | out, open collector (shared) | 31 | Interrupt request on program level 11. Levels 15, 13, 12, 11 and 10 are the ones available to external devices. | inferred |
| BINT12 | LEV12 | interrupt | low | out, open collector (shared) | 35 | Interrupt request on program level 12. Levels 15, 13, 12, 11 and 10 are the ones available to external devices. | inferred |
| BINT13 |  | interrupt | low | out, open collector (shared) | 39 | Interrupt request on program level 13. Levels 15, 13, 12, 11 and 10 are the ones available to external devices. | inferred |
| BINT15 |  | interrupt | low | out, open collector (shared) | 43 | Interrupt request on program level 15. Levels 15, 13, 12, 11 and 10 are the ones available to external devices. | inferred |
| BIDENT / IDENTn (chain in) | INIDENT | ident | low | in | 7 | IDENT search signal coming in from the card nearer the CPU. When several cards interrupt on the same level, the slot position decides priority; a card that answers IDENT turns off its interrupt. | inferred |
| IDENTn (chain out) | OUTIDENT | ident | low | out | 6 | IDENT search passed on to the next card further from the CPU (when this card does not answer). | inferred |
| BGRANT / GRANTn (chain in) | INGRANT | control | low | in | 9 | open | open |
| GRANTn (chain out) | OUTGRANT | control | low | out | 8 | open | open |
| BMCL | MC | control | low | in | 55 | Master clear: resets the card. | inferred |
| RESTART |  | control | low | open | 19 | open | open |
| IOTE |  | control | low | in | 47 | open | open |
| BSKIP |  | control | low | out | 51 | open | open |
| BFQ1 | BFQ1 | control | high | in | 4 | A clock the card uses in test mode in place of the modem clocks. | inferred |
| -12V |  | power | none | in | 62, 63 | -12 V supply. | read |
| +12V |  | power | none | in | 66, 67 | +12 V supply. | read |
| GND |  | power | none | in | 80, 82, 84, 86, 94 | Ground. | read |
| +5V |  | power | none | in | open | +5 V logic supply. | read |

### Connectors of a NORD-10 I/O card

- The backwiring list has **one connector per card position, pins numbered 0-99** (column "Pin
  Range 0-99"; read). It gives no rows or sides and no connector letter. The column "Pin Sequen."
  reads 6 on every position; its meaning is open.
- **No NORD-10 document found names the card connectors A/B/C.** Those letters are documented
  for NORD-100 modules only. The connector of each NORD-10 pin therefore stays `open`.
- In the backwiring list, I/O position pins 6-55 are bussed; pins 0-5 and 56-99 are empty.
- **Power is not in the backwiring list.** Known only from 2A50: -12 V on 62, 63; +12 V on 66, 67;
  ground on 80, 82, 84, 86, 94. The +5 V pin is open.
- **Modem lines on 2A50**: circled pins 81-95 carry the V.24 lines, with the modem plug pin in
  brackets on the drawing (81 = V.24 17 receiver timing, 83 = 15 transmitter timing, 85 = 22
  calling indicator, 87 = 3 received data, 88 = 8 signal detector, 89 = 5 ready for sending, 90 =
  6 data set ready, 91 = 20 connect to line, 92 = 4 request to send, 93 = 23, 95 = 2 transmitted
  data). The NORD-10/S manual says "cable connectors are plugged directly into the backplane";
  that 81-95 are on the same 0-99 connector as the bus pins is inferred from the shared numbering,
  not proven. Which connector they are on is open.

### Still open for NORD-10

1. The name of the card connector(s), how many a card has, and their rows or sides.
2. The meaning of RESTART (pin 19), IOTE (pin 47), BSKIP (pin 51), BA11-BA17 on I/O cards, and of
   the GRANT chain (pins 8, 9). Where BGRANT comes from (no source in the list).
3. Where BFQ1 (2A50 pin 4) comes from: the backwiring list has pin 4 empty on every I/O position,
   and FQ1 only at A22 pin 6 with one connection. Its frequency.
4. The +5 V pin(s) and any other power pins.
5. The exact bus handshake (timing, what BINPUT and BCON mean in the protocol).
6. What the prefix letters W, B and E in the backwiring list mean.

## NORD-100 (ND-100) bus, plug C

From the ND-06.016.01 backplane table and signal list. Every logic signal is active low
("logical 1 = 0 to 0.5 V"). Directions are worked out from the table's Source/Used codes
(C = CPU or bus receiver, M = memory, I = I/O interface, P = power, E = future extensions,
X = bus expander).

| Name | - | Group | Active | Direction (from the card) | Pin(s) | What it does | Meaning confidence |
|---|---|---|---|---|---|---|---|
| GND |  | power | none | in | C a1, C a11, C a24, C a32, C b1, C b11, C b24, C b32, C c1, C c11, C c24, C c32 | Logic ground return. | read |
| +5V |  | power | none | in | C a2, C a31, C b2, C b31, C c2, C c31 | Main logic supply. | read |
| BD1 |  | address+data | low | bidir | C a3 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD3 |  | address+data | low | bidir | C a4 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD5 |  | address+data | low | bidir | C a5 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD7 |  | address+data | low | bidir | C a6 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD9 |  | address+data | low | bidir | C a7 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD11 |  | address+data | low | bidir | C a8 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD13 |  | address+data | low | bidir | C a9 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD15 |  | address+data | low | bidir | C a10 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BREF |  | control | low | not used by I/O cards | C a12 | open | open |
| PA1 |  | position code | low | in | C a13 | Card position code; gives the device numbers of analogue and digital process interfaces. | read |
| PA3 |  | position code | low | in | C a14 | Card position code; gives the device numbers of analogue and digital process interfaces. | read |
| BINT10 |  | interrupt | low | out, wired OR (shared) | C a15 | Interrupt line. BINT10 has the lowest priority. Wired OR line. | read |
| BINT12 |  | interrupt | low | out, wired OR (shared) | C a16 | Interrupt line. BINT10 has the lowest priority. Wired OR line. | read |
| PANREQ |  | position code | low | out | C a17 | Card position code; gives the device numbers of analogue and digital process interfaces. | read |
| BINPUT |  | control | low | out, wired OR (shared) | C a18 | Bus Input, signalled by a unit that will send data. I/O interfaces must wait for BINACK before enabling data and BDRY. Wired OR line. | read |
| BDRY |  | address+data | low | bidir | C a19 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BAPR |  | control | low | bidir, wired OR (shared) | C a20 | Bus Address Present on the shared data and address bus. Wired OR line. | read |
| INCONTR |  | control | low | unused (future extensions) | C a21 | Answer to BCRQ saying control over the bus is available. A unit that does not want control passes it on as OUTCONTR to the next position further from the controlling unit. | read |
| INIDENT |  | ident | low | in | C a22 | Answer to BINT10-13, together with address bits 0-5 that give the BINT number. An interface that raised BINT on that level before the last leading edge of BAPR puts its IDENT code on the BD lines; otherwise it passes INIDENT on as OUTIDENT to the next lower-priority position. | read |
| INGRANT |  | control | low | in | C a23 | Answer to BREQ: the bus is free for a DMA cycle. An interface that sent BREQ before the last leading edge of BMEM may use the bus for one memory read or write; otherwise it passes INGRANT on as OUTGRANT to the next lower-priority position (further from the controlling unit). | read |
| +15V |  | power | none | in | C a25, C b25, C c25 | Supply for analogue interface circuits, for customer use. | read |
| An. Return |  | power | none | in | C a26, C b26, C c26 | Separate ground for analogue circuits, joined to logic ground at the power supply (+/-15 V supply). | read |
| -15V |  | power | none | in | C a27, C b27, C c27 | Supply for analogue interface circuits, for customer use. | read |
| +12V |  | power | none | in | C a28, C b28, C c28 | The table prints +12V; the voltage list on the following page has '+12V Standby: supply for memory, needs battery backup'. That the two are the same line is inferred. | inferred |
| POW. SENSE |  | power | none | in | C a29, C b29, C c29 | open | open |
| 5V St.by |  | power | none | in | C a30, C b30, C c30 | Logic supply for memory kept alive during power failure. | read |
| BD16 |  | address+data | low | bidir | C b3 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD17 |  | address+data | low | bidir | C b4 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD18 |  | address+data | low | bidir | C b5 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD19 |  | address+data | low | bidir | C b6 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD20 |  | address+data | low | bidir | C b7 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD21 |  | address+data | low | bidir | C b8 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD22 |  | address+data | low | bidir | C b9 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD23 |  | address+data | low | bidir | C b10 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| LOAD |  | control | low | out, wired OR (shared) | C b12 | Starts the load microprogram if the CPU is in STOP mode. Wired OR line (only in the CPU crate). | read |
| RESTART |  | control | low | out, wired OR (shared) | C b13 | Starts program execution at location 20 (octal) if the CPU is in STOP mode. Wired OR line (only in the CPU crate). | read |
| RUN |  | control | low | in | C b14 | The CPU is running a program, i.e. not in STOP mode. Generated by the CPU (only in the CPU crate). | read |
| CONTINUE |  | control | low | out, wired OR (shared) | C b15 | May be used to start a CPU that is in STOP mode. Wired OR line (only in the CPU crate). | read |
| STOP |  | control | low | out, wired OR (shared) | C b16 | Forces the CPU into STOP mode after the current instruction. Wired OR line (only in the CPU crate). | read |
| BLANK |  | control | low | bidir, wired OR (shared) | C b17 | Output blanking signal for process interfaces. Wired OR, generated by a monitoring device. | read |
| BPERR |  | control | low | not used by I/O cards | C b18 | Bus Parity Error: fatal or correctable error from memory, according to the ECC register. | read |
| BINACK |  | control | low | in | C b19 | Bus Input Acknowledge: an interface that asked for an input operation may now put its data on the bus. Generated by the controlling unit. | read |
| BMCL |  | control | low | bidir, wired OR (shared) | C b20 | Bus Master Clear: resets the logic at power-up and when the Master Clear button is pushed. Wired OR line. | read |
| BERROR |  | control | low | unused (future extensions) | C b21 | Bus Error: an error was found during a bus cycle, e.g. a fatal memory error. Wired OR line. | read |
| BCRQ |  | control | low | unused (future extensions) | C b22 | Bus Control Request from a unit that wants full control over the bus (for future extensions). Wired OR line. | read |
| BMINH |  | control | low | not used by I/O cards | C b23 | Bus Memory Inhibit: blocks memory accesses during power-down and power-up in systems with battery backup for memory only. Generated by the controlling unit. | read |
| BD0 |  | address+data | low | bidir | C c3 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD2 |  | address+data | low | bidir | C c4 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD4 |  | address+data | low | bidir | C c5 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD6 |  | address+data | low | bidir | C c6 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD8 |  | address+data | low | bidir | C c7 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD10 |  | address+data | low | bidir | C c8 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD12 |  | address+data | low | bidir | C c9 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BD14 |  | address+data | low | bidir | C c10 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BREQ |  | control | low | out, wired OR (shared) | C c12 | Request for a DMA cycle. Wired OR line. | read |
| PA0 |  | position code | low | in | C c13 | Card position code; gives the device numbers of analogue and digital process interfaces. | read |
| PA2 |  | position code | low | in | C c14 | Card position code; gives the device numbers of analogue and digital process interfaces. | read |
| BINT11 |  | interrupt | low | out, wired OR (shared) | C c15 | Interrupt line. BINT10 has the lowest priority. Wired OR line. | read |
| BINT13 |  | interrupt | low | out, wired OR (shared) | C c16 | Interrupt line. BINT10 has the lowest priority. Wired OR line. | read |
| BINT15 |  | interrupt | low | out, wired OR (shared) | C c17 | Interrupt line. BINT10 has the lowest priority. Wired OR line. | read |
| BDAP |  | address+data | low | bidir | C c18 | Shared (multiplexed) data and address bus, 24 bits. Bit 0 is the least significant. | read |
| BIOXE |  | control | low | in | C c19 | Input/Output Enable: a strobe to enable data transfer to or from an I/O interface. Generated by the controlling unit. | read |
| BMEM |  | control | low | in | C c20 | Bus Memory Cycle: the bus cycle accesses memory. Generated by the controlling unit. | read |
| OUTCONTR |  | control | low | unused (future extensions) | C c21 | See INCONTR: passed on to INCONTR of the next position. | read |
| OUTIDENT |  | ident | low | out | C c22 | See INIDENT: passed on to INIDENT of the next lower-priority position. | read |
| OUTGRANT |  | control | low | out | C c23 | See INGRANT: passed on to INGRANT of the next lower-priority position. | read |

### NORD-100 plugs

- **C**: the bus plug, 96 pins, rows a, b, c x pins 1-32 (read). Power: GND a/b/c 1, 11, 24, 32;
  +5 V a/b/c 2, 31; +15 V 25; analogue return 26; -15 V 27; +12 V 28; power sense 29; +5 V
  standby 30 (read).
- **A, B**: 64 lines each, use not defined by the bus; I/O controllers use them for the external
  devices (read). Pin numbering of A and B is open.
- Connector type DIN 41612 (3 x 32, 2.54 mm) comes from NDInsight only (secondary).

### Where NDInsight's C-connector note differs from the ND table

Pin positions and signal names agree everywhere. About 20 Source/Used cells differ; the JSON
field `disagreements` lists them. The one that matters: row b14 **RUN** is source C, used I in
the ND table (and the ND text says "generated by CPU"); NDInsight's table has it the other way
round. The ND table image was used here.
