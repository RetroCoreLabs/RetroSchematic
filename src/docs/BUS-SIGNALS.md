# Bus signals and connectors

These files hold what Norsk Data's own documents say about the signals on the I/O bus of a
NORD-10 and of a NORD-100 (ND-100), and about the connectors a card uses. Purpose: any card-edge
signal on a netlist sheet can be looked up and given a plain-words meaning.

| File | What it holds |
|---|---|
| `catalog/signals/nd-bus-nord10.json` | NORD-10 I/O bus, as seen at an I/O device card position (56 entries) |
| `catalog/signals/nd-bus-nd100.json` | NORD-100 / ND-100 bus on plug C (68 entries, every pin of the table) |
| `catalog/connectors/nord10-io-card-backplane.json` | NORD-10 I/O card connector to the backplane: bus side 6-55, cable side 56-99 (the 2A50 modem lines, +-12V out, grounds) |
| `catalog/connectors/nd100-c-connector.json` | NORD-100 module plugs C (bus, 96 pins) and A, B (64 lines each) |
| `catalog/connectors/nd120-cpu-a-plug.json` | ND-120 CPU card 3202D, plug A (console and panel), from the owner's spreadsheet |
| `catalog/connectors/nd120-cpu-b-plug.json` | ND-120 CPU card 3202D, plug B (trace bus), from the owner's spreadsheet |
| `catalog/connectors/nd100-3207-a-plug.json` | Card 3207 SCSI/floppy controller, plug A (floppy drive lines), read from its drawing ND-350007-A1 sheets 1-2 |
| `catalog/connectors/nd100-3207-b-plug.json` | Card 3207 SCSI/floppy controller, plug B (SCSI bus), read from its drawing ND-350007-A1 sheet 3 |

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

A card's own plug file (`nd-connector`, such as `nd100-3207-a-plug`) has a flat `pins` list of
`{ row, pin, signal, source, page, confidence, note }`, where `signal` is the label exactly as
printed on the card's drawing. Rows are lower case (a, b, c), and a12 and b12 are two pins.
`usedBy` lists the cards wired this way (card number, part number, print), so another edition
with the same plug wiring points to the same file. `unusedPins` names the pins the drawing
does not show, and `disagreements` records a conflict with another source, left open.

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
| F612 | ND-06.012.01: figure 3.1 "I/O slot/plug layout" (page 3-3) and figure 7.4 "Signal distribution I/O bus B and C rack" (section 7.4), read from the owner's screenshots of the two figures | NORD-10 I/O slot supply pins, the cable plug, the bus pin names of the B and C rack, tri-state or open collector per line, the logic levels. Only those two figures have been read. |
| NDI | NDInsight repository, SINTRAN/Devices/BusSignals/ND-100-BUS-C-CONNECTOR.md | secondary: DIN 41612 connector type, driver chips and pull-ups on the ND-120 3202D CPU card |

The NORD-10/S manual says signal levels, definitions and timing are in the "NORD-10 Input/Output
manual (ND-06.004)". That manual was not found on this machine. Figures 3.1 and 7.4 of
ND-06.012.01 (source F612) now give the supply pins, the electrical kind of each line and the
logic levels; ND-06.004 would still close most of the other open NORD-10 items below.

## NORD-10 I/O bus (I/O device positions)

Pin numbers are those of the backwiring list and of the circled numbers on the card drawings;
they agree for every 2A50 bus pin. All bus names carry suffix 0. Directions are inferred from
the backwiring (a `G` before a signal marks the pin that drives it, worked out from the IDENT
chain) and from how 2A50 drives each line.

ND-06.012.01 figure 7.4 (read): the lines are **tri-state TTL, except the interrupt lines and DMA
request, which are open collector**; logical 1 = 0-0.4 V, logical 0 = 2.4-5 V, so the bus is
**active low** (this was inferred from the suffix 0 before; now read).

**Where figure 7.4 and the backwiring list disagree** (both kept, the backwiring name is the one
in the table; figure 7.4 is drawn for the "I/O bus B and C rack", and whether that rack is wired
like A25-A32 of the CPU crate is open):

| Pin | Backwiring til NORD-10 | Figure 7.4 |
|---|---|---|
| 6 | IDENT out (A25:G06 drives A26:07) | INIDENT |
| 7 | IDENT in (BIDENT from A22 into A25:07) | OUTIDENT |
| 8 | GRANT out (A25:G08 drives A26:09) | INGRANT (2A50 also calls its joined 8/9 INGRANT) |
| 9 | GRANT in | OUTGRANT |
| 19 | RESTART | MPX ADDRESS |
| 47 | IOTE (driven from A22) | DMA REQUEST (open collector) |
| 51 | BSKIP | DMA DATA READY |
| 15, 23 | BINPUT, BCON: 2A50 drives them with 7438 open-collector gates | tri-state lines |
| 4 | BFQ1: 2A50 draws it with suffix 1 (true when high) | the bus is active low in general |

Where they agree: BD0-15 on 10, 12, 13, 14, 16, 17, 18, 20, 21, 22, 24, 25, 26, 28, 29, 30;
BA0-17 on 32, 33, 34, 36, 37, 38, 40, 41, 42, 44, 45, 46, 48, 49, 50, 52, 53, 54 (same pins, same
order); IOXE 11, BINPUT 15, BCONNECT 23, INT10/11/12/13/15 on 27/31/35/39/43, MASTER CLEAR 55;
that 6, 7 and 8, 9 do not run one-to-one to the next I/O position. Pin 5 is BFQ2 (figure 7.4 only).

**DMA cable-side signals, not bus pins.** Figure 7.4 also lists signals between card 1022 and the
cards 1014/1092: DMA data ready, DINPUT, device request, WRITE, CF (clear flag), DR0-2 (bus
address), DEQL, BERROR, BCOMPLETE, BRBUSY, BSTART, RRQ and the data lines MDB0-15. They belong to
those cards' cables only and are not in the bus catalogue; their pins are open.

| Name | Name on 2A50 | Name in ND-06.012.01 fig. 7.4 | Group | Active | Level (fig. 7.4) | Direction (from the card) | Pin(s) | What it does | Meaning confidence |
|---|---|---|---|---|---|---|---|---|---|
| BA0 | BA0 | BA0 | address | low | tri-state TTL | in | 32 | Bus address bit 0. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA1 | BA1 | BA1 | address | low | tri-state TTL | in | 33 | Bus address bit 1. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA2 | BA2 | BA2 | address | low | tri-state TTL | in | 34 | Bus address bit 2. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA3 | BA3 | BA3 | address | low | tri-state TTL | in | 36 | Bus address bit 3. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA4 | BA4 | BA4 | address | low | tri-state TTL | in | 37 | Bus address bit 4. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA5 | BA5 | BA5 | address | low | tri-state TTL | in | 38 | Bus address bit 5. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA6 | BA6 | BA6 | address | low | tri-state TTL | in | 40 | Bus address bit 6. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA7 | BA7 | BA7 | address | low | tri-state TTL | in | 41 | Bus address bit 7. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA8 | BA8 | BA8 | address | low | tri-state TTL | in | 42 | Bus address bit 8. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA9 | BA9 | BA9 | address | low | tri-state TTL | in | 44 | Bus address bit 9. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA10 |  | BA10 | address | low | tri-state TTL | in | 45 | Bus address bit 10. With IOX the device register address is 11 bits (bits 0-10). | inferred |
| BA11 |  | BA11 | address | low | tri-state TTL | in | 46 | Bus address bit 11. Use on I/O cards not documented in the sources found. | inferred |
| BA12 |  | BA12 | address | low | tri-state TTL | in | 48 | Bus address bit 12. Use on I/O cards not documented in the sources found. | inferred |
| BA13 |  | BA13 | address | low | tri-state TTL | in | 49 | Bus address bit 13. Use on I/O cards not documented in the sources found. | inferred |
| BA14 |  | BA14 | address | low | tri-state TTL | in | 50 | Bus address bit 14. Use on I/O cards not documented in the sources found. | inferred |
| BA15 |  | BA15 | address | low | tri-state TTL | in | 52 | Bus address bit 15. Use on I/O cards not documented in the sources found. | inferred |
| BA16 |  | BA16 | address | low | tri-state TTL | in | 53 | Bus address bit 16. Use on I/O cards not documented in the sources found. | inferred |
| BA17 |  | BA17 | address | low | tri-state TTL | in | 54 | Bus address bit 17. Use on I/O cards not documented in the sources found. | inferred |
| BD0 | BD0 | BD0 | data | low | tri-state TTL | bidir | 10 | Bus data bit 0 (16-bit data word between the CPU A register and the card). | inferred |
| BD1 | BD1 | BD1 | data | low | tri-state TTL | bidir | 12 | Bus data bit 1 (16-bit data word between the CPU A register and the card). | inferred |
| BD2 | BD2 | BD2 | data | low | tri-state TTL | bidir | 13 | Bus data bit 2 (16-bit data word between the CPU A register and the card). | inferred |
| BD3 | BD3 | BD3 | data | low | tri-state TTL | bidir | 14 | Bus data bit 3 (16-bit data word between the CPU A register and the card). | inferred |
| BD4 | BD4 | BD4 | data | low | tri-state TTL | bidir | 16 | Bus data bit 4 (16-bit data word between the CPU A register and the card). | inferred |
| BD5 | BD5 | BD5 | data | low | tri-state TTL | bidir | 17 | Bus data bit 5 (16-bit data word between the CPU A register and the card). | inferred |
| BD6 | BD6 | BD6 | data | low | tri-state TTL | bidir | 18 | Bus data bit 6 (16-bit data word between the CPU A register and the card). | inferred |
| BD7 | BD7 | BD7 | data | low | tri-state TTL | bidir | 20 | Bus data bit 7 (16-bit data word between the CPU A register and the card). | inferred |
| BD8 | BD8 | BD8 | data | low | tri-state TTL | bidir | 21 | Bus data bit 8 (16-bit data word between the CPU A register and the card). | inferred |
| BD9 | BD9 | BD9 | data | low | tri-state TTL | bidir | 22 | Bus data bit 9 (16-bit data word between the CPU A register and the card). | inferred |
| BD10 | BD10 | BD10 | data | low | tri-state TTL | bidir | 24 | Bus data bit 10 (16-bit data word between the CPU A register and the card). | inferred |
| BD11 | BD11 | BD11 | data | low | tri-state TTL | bidir | 25 | Bus data bit 11 (16-bit data word between the CPU A register and the card). | inferred |
| BD12 | BD12 | BD12 | data | low | tri-state TTL | bidir | 26 | Bus data bit 12 (16-bit data word between the CPU A register and the card). | inferred |
| BD13 | BD13 | BD13 | data | low | tri-state TTL | bidir | 28 | Bus data bit 13 (16-bit data word between the CPU A register and the card). | inferred |
| BD14 | BD14 | BD14 | data | low | tri-state TTL | bidir | 29 | Bus data bit 14 (16-bit data word between the CPU A register and the card). | inferred |
| BD15 | BD15 | BD15 | data | low | tri-state TTL | bidir | 30 | Bus data bit 15 (16-bit data word between the CPU A register and the card). | inferred |
| BIOXE | IOXE | IOXE | control | low | tri-state TTL | in | 11 | IOX enable: the strobe that tells every card that an IOX transfer is under way, so the card compares BA against its own address. | inferred |
| BINPUT | INPUT | BINPUT | control | low | tri-state TTL | out, open collector (shared) | 15 | The addressed card says the transfer is an input (card to CPU). | inferred |
| BCON | CONNECT | BCONNECT | control | low | tri-state TTL | out, open collector (shared) | 23 | The card answers that it recognised its address (connected). | inferred |
| BINT10 | LEV10 | INT10 | interrupt | low | open collector | out, open collector (shared) | 27 | Interrupt request on program level 10. Levels 15, 13, 12, 11 and 10 are the ones available to external devices. | inferred |
| BINT11 |  | INT11 | interrupt | low | open collector | out, open collector (shared) | 31 | Interrupt request on program level 11. Levels 15, 13, 12, 11 and 10 are the ones available to external devices. | inferred |
| BINT12 | LEV12 | INT12 | interrupt | low | open collector | out, open collector (shared) | 35 | Interrupt request on program level 12. Levels 15, 13, 12, 11 and 10 are the ones available to external devices. | inferred |
| BINT13 |  | INT13 | interrupt | low | open collector | out, open collector (shared) | 39 | Interrupt request on program level 13. Levels 15, 13, 12, 11 and 10 are the ones available to external devices. | inferred |
| BINT15 |  | INT15 | interrupt | low | open collector | out, open collector (shared) | 43 | Interrupt request on program level 15. Levels 15, 13, 12, 11 and 10 are the ones available to external devices. | inferred |
| BIDENT / IDENTn (chain in) | INIDENT | OUTIDENT | ident | low | tri-state TTL | in | 7 | IDENT search signal coming in from the card nearer the CPU. When several cards interrupt on the same level, the slot position decides priority; a card that answers IDENT turns off its interrupt. | inferred |
| IDENTn (chain out) | OUTIDENT | INIDENT | ident | low | tri-state TTL | out | 6 | IDENT search passed on to the next card further from the CPU (when this card does not answer). | inferred |
| BGRANT / GRANTn (chain in) | INGRANT | OUTGRANT | control | low | tri-state TTL | in | 9 | open | open |
| GRANTn (chain out) | OUTGRANT | INGRANT | control | low | tri-state TTL | out | 8 | open | open |
| BMCL | MC | MASTER CLEAR | control | low | tri-state TTL | in | 55 | Master clear: resets the card. | inferred |
| RESTART |  | MPX ADDRESS | control | low | tri-state TTL | open | 19 | open | open |
| IOTE |  | DMA REQUEST | control | low | open collector | in | 47 | open | open |
| BSKIP |  | DMA DATA READY | control | low | tri-state TTL | out | 51 | open | open |
| BFQ1 | BFQ1 | BFQ1 | control | high | tri-state TTL | in | 4 | A clock the card uses in test mode in place of the modem clocks. | inferred |
| BFQ2 |  | BFQ2 | control | low | tri-state TTL | open | 5 | open | open |
| -12V |  |  | power | none |  | out | 62, 63 | -12 V made on the 2A50 card by its own VP12/12 converter and put out on the cable side. | inferred |
| +12V |  |  | power | none |  | out | 66, 67 | +12 V made on the 2A50 card by its own VP12/12 converter and put out on the cable side. | inferred |
| GND |  |  | power | none |  | in | 2, 3, 96, 97, 80, 82, 84, 86, 94 | Ground: 2, 3, 96, 97 from the backplane; 80, 82, 84, 86, 94 are 2A50's cable-side grounds. | read |
| +5V |  |  | power | none |  | in | 0, 1, 98, 99 | +5 V logic supply. | read |

### Connectors of a NORD-10 I/O card

- The backwiring list has **one connector per card position, pins numbered 0-99** (column "Pin
  Range 0-99"; read). It gives no rows or sides and no connector letter. The column "Pin Sequen."
  reads 6 on every position; its meaning is open.
- **Connector type** (ND-06.012.01 figure 3.1, read): one 100-terminal connector per I/O slot.
  Its maker and part number are open. No NORD-10 document found names it A/B/C; those letters are
  documented for NORD-100 modules only, so the connector of each NORD-10 pin stays `open`.
- In the backwiring list, I/O position pins 6-55 are bussed; pins 0-5 and 56-99 are empty (the
  list names signal wires only).
- **Supply** (figure 3.1, read): **+5 V on 0, 1, 98, 99; ground on 2, 3, 96, 97** ("4 terminals
  for power, 4 for ground"). On the 2A50 photos the fingers of 0, 1, 98, 99 run into wide copper,
  which matches.
- **Cable side** (figure 3.1, read): pins 56-95 go to the external device through a Berg plug,
  cabled to a Burndy MS42RM plug in the plug panel (Berg 56 -> v, 57 -> t, ... 94/95 at A-D; the
  full letter table is open, the figure was shown only in part). 2A50's -12 V out on 62, 63, +12 V
  out on 66, 67 (its own VP12/12 converter) and grounds on 80, 82, 84, 86, 94 are that card's
  choice of free cable pins, not a conflict with the figure.
- **Modem lines on 2A50**: circled pins 81-95 carry the V.24 lines, with the modem plug pin in
  brackets on the drawing (81 = V.24 17 receiver timing, 83 = 15 transmitter timing, 85 = 22
  calling indicator, 87 = 3 received data, 88 = 8 signal detector, 89 = 5 ready for sending, 90 =
  6 data set ready, 91 = 20 connect to line, 92 = 4 request to send, 93 = 23, 95 = 2 transmitted
  data). The NORD-10/S manual says "cable connectors are plugged directly into the backplane";
  that 81-95 are on the same 0-99 connector as the bus pins is inferred from the shared numbering,
  not proven. Which connector they are on is open.

### Still open for NORD-10

1. The maker and part number of the 100-terminal card connector; whether the Berg cable plug
   sits on the backplane posts or on the card; the full Berg-to-Burndy letter table.
2. The meaning of RESTART / MPX ADDRESS (pin 19), IOTE / DMA REQUEST (pin 47), BSKIP / DMA DATA
   READY (pin 51) - which name is right for which rack - and of BA11-BA17 on I/O cards and the
   GRANT chain (pins 8, 9). Which pin of 6/7 and 8/9 is the input (backwiring list and figure 7.4
   disagree). Where BGRANT comes from (no source in the list).
3. Where BFQ1 (2A50 pin 4) and BFQ2 (pin 5) come from: the backwiring list has pins 4 and 5
   empty on every I/O position, and FQ1 only at A22 pin 6 with one connection. Their frequency.
4. Whether BINPUT and BCON are tri-state (figure 7.4) or open collector (the 7438 on 2A50), and
   where the open-collector lines are pulled up.
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

### Names in the ND-120 Verilog and in the KiCad card design

Two more, non-ND sources are recorded per NORD-100 signal in `names`:

- `names.nd120Verilog`: the port name in the nd-120 repository's ND-120 CPU board model
  (`Verilog/CPU-BOARD-3202/circuit/ND3202D.v`, module ND3202D, port list "BACKPLANE C-PLUG",
  lines 28-80), with the line number. Bus lines are `<NAME>_n`; lines the CPU both drives and
  reads are split into `_n_IN` and `_n_OUT`; BD0-BD23 are `BD_23_0_n_IN` / `BD_23_0_n_OUT`.
- `names.kicadNet`: the net name on connector J1 "BUS-C" (DIN 41612 3x32) of the KiCad project
  NDModulE-v3-reduced, sheet SystemBusC, with each pin and whether the card leaves it unconnected.

The ND manual table stays the authority. Field `disagreementsOtherSources` lists every
difference found:

| Source | Where | Says | ND table says |
|---|---|---|---|
| Verilog | line 72 comment | BREF at B12 | BREF at a12 (b12 is LOAD) |
| Verilog | line 76 comment | BMEM at C28 | BMEM at c20 (c28 is +12V) |
| Verilog | line 79 | port `MCL` | BMCL |
| Verilog | lines 53-54 | `SEMRQ_n` (SEMREQ) | PANREQ |
| Verilog | line 73 | drives BERROR | BERROR is for future extensions only (E/E) |
| Verilog | ports | no port for BPERR, BMINH, BLANK, PA0-3, INIDENT, INGRANT, BCRQ, INCONTR, OUTCONTR; RESTART (b13) is noted as not connected | - |
| KiCad | a15, a16, c15, c16, c17 | nets INT10, INT12, INT11, INT13, INT15 | BINT10, BINT12, BINT11, BINT13, BINT15 |
| KiCad | a17 | net SEMREQ | PANREQ |

Not disagreements, but worth knowing: every KiCad pin position agrees with the ND table; that
card leaves pins 25-31 of all rows unconnected (so it takes +5 V on pin 2 only); pin 1 of each
row is on a net NDGND_EURO5V, separate from net GND on pins 11, 24, 32; KiCad gives no directions
(all pins are "passive").

### The owner's spreadsheet "ND-120 Commands"

A second-hand source, kept outside this repository (sheets A-PLUG, B-PLUG, C-PLUG (ND-BUS), C-BUS,
Signal Names). Everything taken from it has confidence `inferred` and never replaces an ND manual
fact. **The NDInsight C-connector note appears to be made from this spreadsheet**: it names the
same file and sheets as its source, and its Source/Used cells match the spreadsheet's.

- Plugs A and B of the ND-120 CPU card: `nd120-cpu-a-plug.json` (console and panel) and
  `nd120-cpu-b-plug.json` (trace bus), rows A, B, C x pins 1-32, each pin's signal as written.
  The owner's question marks and alternatives ("O2N (TXD?)", "DSR_n (DTR_n)", "(TXD)") are kept
  and marked uncertain; empty cells are `open`. Cell colours give a direction for only a few A-plug
  pins (RXD marked INPUT; three cells highlighted yellow, meaning not given).
- In `nd-bus-nd100.json`: per signal `descriptionSpreadsheet` (the sheet's Name and Description),
  `names.spreadsheetCPlug` (sheet C-BUS) and `names.spreadsheetCPlugSheet` (sheet C-PLUG, e.g.
  "INT 10", "SEMRQ", "INGRANT (DMA)"), and `spreadsheetNotes` (BD pull-ups 4.7 kohm 4610X-101-472;
  "BREQ ... From CPU?"; "RFRQ ... same as BREQ but given priority"; one bus cycle at most 8 us).
- Its Source/Used codes were compared cell by cell with the ND table, for both sheets C-BUS and
  Signal Names. The two sheets also disagree with each other on b14 RUN: O/C in one, I/C in the
  other. All 57 differences are in `disagreementsOtherSources`. The ND table says, against the
  sheets (sheet value in brackets): a13/a14/c13/c14 PA source blank (I); a18 BINPUT used CM (C);
  a19 BDRY used CI (C); a20 BAPR used MI (CMI); a22 INIDENT and c22 OUTIDENT used I (CI); b13
  RESTART used C (I); b14 RUN C/I (O/C or I/C); b19 BINACK used I (C); b23 BMINH used CM (CMI;
  C-BUS also writes "3MINH"); c18 BDAP used MI (CI); c20 BMEM C/MI (CMI/CMI); c1/c2 blank (P/CMI);
  pin 32 P/CMI (C-BUS blank); and the power pins 26-30 in several rows. Signal Names b29 POW. SENSE
  carries the +12V description by mistake.

## ND-120 Verilog check

The ND-120 CPU board model (nd-120 repository, `Verilog/CPU-BOARD-3202/circuit/`, read only)
checked against ND-06.016.01 Appendix F. The ND-120 CPU card carries its own memory, so it acts
as C (CPU) **and** M (memory) in the table's Source/Used codes; directions are judged with both.
File names below are relative to that folder.

| # | Where | The Verilog says | The ND manual says | Verdict |
|---|---|---|---|---|
| 1 | ND3202D.v:72 | BREF_n comment: C-plug B12 | BREF is a12 (b12 is LOAD) | **wrong** (comment) |
| 2 | ND3202D.v:76 | BMEM_n comment: C-plug C28 | BMEM is c20 (c28 is +12V) | **wrong** (comment) |
| 3 | ND3202D.v:79, 768 | port `MCL` = ~s_mcl, "MCL~ (after negation)" | BMCL, active low | name lacks BMCL and `_n`; the port is low when s_mcl is high, which is right only if s_mcl is active high (IO_37.v:86, 317 passes it through) - cannot tell without the IO_37 logic |
| 4 | ND3202D.v:79 | MCL is an output only | BMCL source CI, used CMI: other cards may also pull it (wired OR) and the CPU uses it | **wrong / missing**: the model cannot be master-cleared from the bus |
| 5 | ND3202D.v:53-54 | SEMRQ_n_IN / SEMRQ_n_OUT at A17 | PANREQ at a17, source I, used C: the CPU only reads it | name differs; the CPU driving it (7438 per NDInsight) is not in the manual - cannot tell |
| 6 | ND3202D.v:73; BIF_BCTL_BDRV_7.v:253 | BERROR_n driven low on bus timeout (TOUT) | BERROR source E / used E (future extensions only) | **differs from the manual**; the Verilog follows the 3202D schematic per its comments - cannot tell which is right for ND-120 |
| 7 | ND3202D.v:36 | RESTART (b13) not connected to any logic | RESTART source I, used C | **differs**: the manual says the CPU uses it |
| 8 | ND3202D.v:956 | BPERR not a port; the internal input is tied to 1, "DISABLE BUS PARITY ERROR" | BPERR source M, used C and X | **missing** (switched off on purpose) |
| 9 | ND3202D.v:28-80 | no BMINH port | BMINH source P, used CM | **missing** (NDInsight says the 3202D drives it with a 7407; not in the model) |
| 10 | ND3202D.v:76, 1178 | BMEM_n output only | BMEM source C, used MI: as memory the card must also see BMEM from a DMA master | cannot tell (no BMEM input to the on-board memory at the port) |
| 11 | ND3202D.v:34-44 | LOAD, BREQ, CONTINUE, STOP, BINT10-13/15 inputs at B12, C12, B15, B16, A15, C15, A16, C16, C17 | same pins, source I, used C | correct |
| 12 | ND3202D.v:550-554 | BINT10/12/13 ANDed with on-board I/O interrupts | wired OR, active low (AND of active-low lines = OR) | correct |
| 13 | ND3202D.v:45 | POWSENSE_n input, A29/B29/C29 | POW. SENSE source P, used CI | correct |
| 14 | ND3202D.v:49-50 | BD_23_0_n_IN / _OUT | BD0-23 CMI/CMI, active low | correct (direction, level) |
| 15 | ND3202D.v:56-66 | BINPUT, BDAP, BDRY, BAPR both in and out at A18, C18, A19, A20 | BINPUT CI/CM, BDAP CI/MI, BDRY MI/CI, BAPR CI/MI | correct (C or M drives and reads each) |
| 16 | ND3202D.v:71, 74, 75, 77, 78 | RUN, BINACK, BIOXE, OUTGRANT, OUTIDENT outputs at B14, B19, C19, C23, C22 | RUN C/I, BINACK C/I, BIOXE C/I, OUTGRANT CI/I, OUTIDENT CI/I | correct |
| 17 | ND3202D.v:28-80 | no INIDENT, INGRANT, PA0-3, BLANK, BCRQ, INCONTR, OUTCONTR ports | none of these is used by C or M (I or E only) | correct |
| 18 | all C-plug ports | every bus port name ends in `_n` except MCL | all logic signals active low | correct except #3 |
| 19 | BIF_BCTL_BDRV_7.v:236-246 | the 74F241 is replaced by logic: OUTIDENT, BIOXE, BINACK driven HIGH when disabled ("Changed from the original schematic ... instead of three-state") | 3202D: 74F241 tri-state (NDInsight) | **differs from the chip** - drives high where the real part lets go; harmless only while nothing else drives those lines |
| 20 | BIF_BCTL_BDRV_7.v:253-254 | BERROR and BDRY driven HIGH when TOUT is low (same change) | BDRY is wired OR (memory and I/O pull it low too) | **differs from the chip**: a real 74F241 lets go; the outside BDRY must be combined outside this module |
| 21 | ND3202D.v:28-80 | open-collector outputs (7407, 7438, 7406) are plain outputs; the wired-OR bus is split into separate _IN and _OUT ports | 3202D chips are open collector (NDInsight) | not modelled at the port - the wired OR has to be made by whatever connects the ports; cannot tell from this file alone |
| 22 | BIF_DPATH_BDLBD_10.v:60-75 | 74AS648 BD driver: released value all ones (fixed, with comment) | active-low BD, released = high | correct |
| 23 | ND3202D.v:1066-1080 | the 74F244 receiver (CHIP_33C) is commented out; the inputs go straight in | 3202D: 74F244 receivers with pull-ups (NDInsight) | not modelled; pull-ups not modelled; cannot tell if it matters |

**A and B plugs** (against the owner's spreadsheet, second-hand - no ND document for these plugs
was found):

| # | Where | The Verilog says | Spreadsheet says | Verdict |
|---|---|---|---|---|
| 24 | ND3202D.v:113 | LOCK_n at A-plug B12 | LOCK_n at C20; B12 empty | **wrong** (against the sheet) |
| 25 | ND3202D.v:93 | XPIL3 at B-plug C8 | XPIL3 at B11; C8 is XLUA0 | **wrong** (against the sheet) |
| 26 | ND3202D.v:88 | port EBUS (no `_n`), B-plug B3, "pulled high" | EBUS_n at B3 | pin right; name and level differ - cannot tell |
| 27 | ND3202D.v:110-117, 134-135 | OSCCL_n B3, OC0 C6, OC1 A6, XTR B4/C23, CONSOLE_n C21/C30, SWMCL_n C16, EAUTO_n C19, RXD C8, TXD A10 and C7, DP_5_1_n C25-C29 | same positions | correct |
| 28 | ND3202D.v:93, 97 | PIL0 B9, PIL1 B10, PIL2 B12; MIS0 A14, MIS1 C14 | same | correct |
| 29 | ND3202D.v:120 | LOAD_n from the A plug "to consider" (SWLD_n); not a port | A15 SWLD, C15 LOAD_n; also STOP_n C17 and RUN_n C10 on the A plug | not modelled on the A plug (only on the C plug) |
| 30 | ND3202D.v:87, 89 | INR_7_0 and SEL5MS_n without pin numbers | INR0-7 on row B; SELM5MS_n at B14 | cannot tell (no pins in the Verilog); the spelling differs (SEL5MS / SELM5MS) |
