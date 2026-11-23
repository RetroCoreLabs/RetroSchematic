# Card drawings we have

What card drawings exist for the card library, where they are, and the order to read them in.
Taken from the title block of each drawing page (rendered from the PDF; the OCR text was not
used). Listed 2026-11-23. Cards already read are marked **read**.

The source files are kept outside this repository, except the NORD-10 book, which is in
`docs/sources/nord-10/NORD-10-drawings.pdf`. Each section names the book; the owner's archive
holds it.

## Sources

| Book | Machine | Pages | Drawn sheets | Scan |
|---|---|---|---|---|
| NORD-10 drawings (Gandalf scan) | NORD-10 | 252 | 249 | good; a few pages faint |
| Binder 1x ND-100 diagrams (Gandalf scan); the same drawings as "Tab 03 Logic Diagrams", page number minus 2 | ND-100 | 337 | about 154 (158-337 are wiring lists) | good; drawn sideways, title block top right |
| ND-500 binder with drawings (Gandalf scan) | ND-500 | 227 | about 100 (1-80 course text, 186-227 wiring lists) | good |
| ND-01.004.01 Hardware manual NORD-1 | NORD-1 | 57 | 50 | photos, grey, low contrast - check at 300 dpi |
| NORD-2B hardware manual vol. I, logic diagrams, December 1970 | NORD-2B | 24 | 19 | clean |
| NORD-20 vol. I, logic diagrams and lists | NORD-20 | 53 | about 33 cards | very faint |

Other drawings: Ethernet II 324534 and Ethernet III 324232 (clean CAD), SCSI 350007 (CAD),
NORD-50 CPU manual vol. II (about 75 logic pages, not listed by card), PIOC drawings (4 pages),
ND-120 schematics and DELILAH circuit diagrams (CPU boards, not listed by sheet), and logic
figures without title blocks in the NORD-10 operator's panel, HP 7970 magtape, HDLC and ND-110
bus switch manuals.

PCB photos: the owner's archive holds component-side and solder-side photos with a metadata
file per board for about 80 boards, among them 3002, 3006, 3009, 3010, 3013, 3015, 3018, 3019,
3020, 3022-3024, 3026, 3027, 3029, 3030 and many 3226xx-3501xx print numbers.

## NORD-10 (about 160 cards)

Card number / sheet / PDF page(s) in the NORD-10 book. Several pages of one card are usually
different revisions.

| Card | No. / sheet / pages |
|---|---|
| Main arithmetic | 1001 / 2A01 / 4 |
| Registers | 1002 / 2A02 / 5 |
| Status | 1003 / 2A03 / 6 |
| Memory data | 1004 / 2A04 / 7 |
| Memory address | 1005 / 2A05 / 8 |
| OR logic | 1006 / 2A06 / 9 |
| Decoding | 1007 / 2A07 / 10 |
| Interrupt registers | 1008 / 2A08 / 11 |
| Microaddressing | 1009 / 2A09 / 12 |
| I/O address | 1010 / 2A10 / 13 |
| I/O data | 1011 / 2A11 / 14 |
| Rack driver | 1012 / 2A12 / 15 |
| Device registers | 1013 / 2A13 / 16 |
| DMA registers | 1014 / 2A14 / 17 |
| Drum sequencer, control, timing | 1015-1017 / 2A15-2A17 / 18-21 |
| Tape reader interface | 1018 / 2A18 / 22 |
| Rack control 1, 2 | 1019, 1021 / 2A19, 2A21 / 23, 25 |
| Teletype buffer | 1020 / 2A20 / 26 |
| Bus control | 1022 / 2A22 / 27-28 |
| 1K PROM | 1023 / 2A23 / 29 |
| Real-time clock | 1024 / 2A24 / 30-31 |
| Time counter | 1025 / 2A25 / 32 |
| Punch interface | 1026 / 2A26 / 33 |
| I/O control | 1027 / 2A27 / 34 |
| Interrupt control | 1028 / 2A28 / 35 |
| Panel control 1, 2; panel data 1, 2; panel driver | 1029-1033 / 2A29-2A33 / 36-41 |
| Paging surrogate | 1034 / - / 43 |
| Memory control | 1035 / 2A35 / 44 |
| Disc transceiver, control, sequence, timing | 1036-1039 / 2A36-2A39 / 45-49 |
| Paging | 1040 / 2A40 / 50-51 |
| 16-bit digital input, output | 1041, 1042 / 2A41, 2A42 / 52, 53 |
| Card reader buffer (Documation) | 1043 / 2A43 / 54-55 |
| CDC 3000 channel transmitter and receiver (faint) | 1045 / 2A45 / 56-57 |
| Async modem buffer | 1046 / 2A46 / 58-60 |
| CDC line printer interface | 1047 / 2A47 / 61 |
| Magtape control A, B | 1048, 1049 / 2A48, 2A49 / 62, 63 |
| Sync modem buffer - **read** (`sheets/nord-10/2A50-sync-modem-buffer-1050.rsc`) | 1050 / 2A50 / 64 |
| Read, write, drive control module | 1051-1053 / 2A51-2A53 / 65-67 |
| Panel control 2 | 1054 / 2A54 / 68 |
| Memory control | 1055 / 2A55 / 69 |
| Process input, output | 1056, 1057 / 2A56, 2A57 / 70, 71 |
| Interrupt control | 1058 / 2A58 / 72 |
| 4K RAM | 1060 / 2A60 / 73 |
| HP parity; status; HP timing A, B; HP control B | 1061-1064, 1072 / 2A61-2A64, 2A72 / 74-78, 85 |
| 4K MOS RAM (Mostek) | 1066 / 2A66 / 79 |
| 4K PROM memory | 1067 / 2A67 / 80 |
| ACM I/O | 1068 / 2A68 / 81 |
| Data and address bus selector | 1070 / 2A70 / 82-83 |
| NORD-50 data | 1071 / 2A71 / 84 |
| Control driver, receiver | 1073, 1074 / 2A73, 2A74 / 86-89 |
| Microaddressing | 1075 / 2A75 / 90 |
| SMD timing, control, receiver, transmitter, unit control | 1076-1080 / 2A76-2A80 / 91-96 |
| MPM data and address line drivers, refresh, control | 1081, 1083, 1089, 1090 / 2A81, 2A83, 2A89, 2A90 / 97-102 |
| Buffered DMA register | 1092 / 2A92 / 103 |
| Branch transceiver | 1093 / 2A93 / 104-105 |
| 8K RAM | 1094 / 2A94 / 106-107 |
| Terminal buffer | 1095 / 2A95 / 108-109 |
| DMA address | 1096 / 2A96 / 110-111 |
| Process console control | 1097 / 2A97 / 112 |
| Device switch | 1098 / 2A98 / 113 |
| Branch control | 1101 / 3A01, 4A01 / 114-116 |
| Versatec control | 1102 / 4A02 / 117 |
| Differential transceiver | 1103 / 4A03 / 118 |
| Calcomp plotter 500, 900; buffered Calcomp plotter | 1104, 1105, 1112 / 4A04, 4A05, 4A12 / 119, 120, 129-130 |
| Disc sequence | 1107 / 4A07 / 121 |
| Remote byte (pages 122-123 faint) | 1108 / 4A08 / 122-125 |
| Parallel byte output | 1109 / 4A09 / 126 |
| Floppy disc control, data | 1111, 1118 / 3A11, 4A11; 3A18, 4A18 / 127-128, 138-139 |
| Watchdog | 1113 / 4A13 / 131 |
| Panel transceiver | 1114 / 4A14 / 132-133 |
| Lamp register | 1115 / 4A15 / 134-135 |
| Transceiver address, data, control | 1119, 1125, 1127 / 4A19, 4A25, 4A27 / 140-141, 151, 153-155 |
| Time control | 1120 / 4A20 / 142 |
| Panel control | 1121 / 4A21 / 143 |
| 4 async current loop | 1122 / 4A22 / 144-145 |
| Pertec magtape control A, B | 1123, 1124 / 4A23, 4A24 / 146-150 |
| Cache | 1126 / 4A26 / 152 |
| DMA registers | 1129 / 4A29 / 156 |
| CDC line printer interface | 1130 / 4A30 / 157-158 |
| 32K RAM | 1132 / 4A32 / 159 |
| ECC polynomials, control | 1133, 1134 / 4A33, 4A34 / 160-162 |
| SMD timing | 1135 / 4A35 / 163-164 |
| MPM refresh | 1136 / 4A36 / 165-166 |
| PS data out; PS-2 data in; PS-2 control | 1137-1139 / 4A37-4A39 / 167-170 |
| Tally matrix printer | 1140 / 4A40 / 171 |
| HDLC data (4 revisions, 2 parts each) | 1141 / 4A41 / 172-179 |
| Big MPM address, data, control, X-Y log, I/O interface, Y log | 1142-1146, 1157 / 4A42-4A46, 4A57 / 180-189, 206 |
| 2 async V.24 | 1147 / 4A47 / 190-193 |
| 2K PROM | 1148 / 4A48 / 194 |
| HDLC DMA control | 1151 / 4A51 / 195-200 |
| DMA address extender | 1153 / 4A53 / 201 |
| SMD transmitter; SMD unit control | 1154, 1156 / 4A54, 4A56 / 202, 205 |
| Bus control | 1155 / 4A55 / 203-204 |
| DR11-C interface | 1158 / 4A58 / 207-208 |
| 16K RAM; 8K static RAM | 1159, 1160 / 4A59, 4A60 / 209, 210-211 |
| Channel address, data driver | 1161, 1162 / 4A61, 4A62 / 212-215 |
| Memory address, data receiver | 1163, 1164 / 4A63, 4A64 / 216-219, 235-236 |
| Memory multiplexer data | 1165 / 4A65 / 220-221 |
| Programmable clock | 1166 / 4A66 / 222 |
| 4K PROM | 1167 / 4A67 / 223 |
| Differential DMA adapter | 1168 / 4A68 / 224 |
| Dual telex control; telex transmitter | 1169, 1173 / 4A69, 4A73 / 225-226, 232 |
| External interrupt | 1170 / 4A70 / 227-228 |
| DMA channel data | 1171 / 4A71 / 229 |
| Universal DMA interface | 1172 / 4A72 / 230-231 |
| Name not readable | 1181, 1185 / 4A81, 4A85 / 233-234, 237 |
| Clock and parity | 1210 / 3A10 / 238-239 |
| MPM data, address buffer line driver | 1211, 1213 / 3A11, 3A13 / 240-243 |
| NORDCOM-74 cards (graphic buffer, address/data buffer, selector, SGB, timing, ACM DCH, -5V generator) | 1301-1306, 1314 / 3B01-3B06 / 244-251 |
| Operator's panel | 1907 / 7B16 / 252 |

Listed in the book's handwritten index but not drawn in it: 1044, 1059, 1065, 1069, 1082,
1084-1088, 1091, 1099, 1106, 1110, 1116, 1117.

## ND-100 (30 cards, 7 panel and adapter boards)

Page numbers are in the "Binder 1x ND-100 diagrams" book.

| Card | No. / sheets / pages |
|---|---|
| Main I/O adapter | 3001 / 5A01-02 / 4-5 |
| NORD-100 CPU (microcode store, ALU, bus data, interface control, sequencing; revisions D and E) | 3002 / 5A03-07 / 6-16 |
| MPM line driver | 3003 / 5A08 / 17 |
| 10 MB disk controller | 3004 / 5A09-12 / 18-21 |
| Dynamic RAM | 3005 / 5A13-15 / 22-24 |
| Pertec magtape control | 3006 / 5A16 / 25 |
| DMA interface | 3006, 3007 / 5A44-47 / 26-29 |
| Euro bus adapter | 3008 / 5A17-18 / 30-33 |
| Local I/O bus | 3009 / 5A19 / 34 |
| Floppy + 4-terminal interface | 3010 / 5A20-22, 5A34-35 / 40-44 |
| Memory management (3 revisions) | 3012 / 5A24-27 / 45-56 |
| 8-terminal and 4+4-terminal interface | 3013 / 5A28-31, 5A48 / 57-66 |
| HDLC + autoload (2 revisions) | 3015 / 5A39-43 / 67-77 |
| CERN autoload | - / 5A76-77 / 78-79 |
| SMD control, data | 3018, 3019 / 5A54-63 / 80-90 |
| STC magtape controller | 3020 / 5A49-52, 5B53 / 91-95 |
| Bus expander | 3021 / 5A64-69 / 96-101 |
| ND-500 interface | 3022 / 5A70-75 / 102-107 |
| Megalink interface | 3023 / 5A78-83 / 108-113 |
| N-10 bus adapter | 3024 / 5A84-86 / 114-116 |
| GPIB controller | 3026 / 5A87-92 / 117-122 |
| Floppy disk controller | 3027 / - / 123-127 |
| Bus expander | 3028 / - / 128-133 |
| Universal DMA | 3029 / - / 134-137 |
| N100 bus master | 3030 / - / 138-139 |
| Dynamic memory | 3034 / - / 140-142 |
| 32K and 64K RAM | 3036 / - / 143-145 |
| Panel control; display panel; 4K PROM adapter for 3002; cable adapters; bus expander cable | 1946, 1947, 1961, 1964-1966, 1969, 1981 / - / 146-155 |

The same book also holds NORD-10 sheets 1047, 1050, 1095, 1109 and 1130 (pages 35-39). Its
rack list names 3031 (ND-100 bus control) and 3032 (MPM4 port), which are not drawn; 3011,
3014, 3016, 3017 and 3025 are missing too.

## ND-500 (17 cards)

CPU slice 5001, sequencer 5004, memory management 5005, cache module 5006, cache 5007,
arithmetic 1-4 (5008, 5009, 5011, 5014), control I and II (5012, 5015), bus driver 5013, cache
control 5017, prefetch 5018, trap 5019, control store RAM 5020, 8K CS RAM 5401, ND-500
interface 3022, panel control 1981. Pages 84-182 and 220 of the ND-500 binder.

## NORD-1, NORD-2B, NORD-20

- **NORD-1** (48 cards, sheet 2Axx = card number): bus memory, registers, arithmetic,
  interrupt registers, panel buffer, I/O control, assembler, counters, shift, control
  flip-flops, protection, addressing, interrupt control, bit instruction, floating control,
  lamp register, time counter, teletype control, memory buffer, I/O cards, channel interface,
  Cambridge control, duplex buffer, punch buffer, line driver/receiver, cable cards.
- **NORD-2B** (16 cards): 201 registers and arithmetic, 7202 memory control, 8203-8208 timing,
  decoding and control, 209 load unit, 8210 IR buffer, 7211 WBT, 8212-8213 ACM, 7215 RTC and
  power fail, 8216-7217 I/O channel.
- **NORD-20** (about 33 cards, very faint scan).

## Order to read them in

Most useful for the emulators first:

1. **ND-100:** 3002 CPU, 3009 local I/O bus, 3012 memory management, 3010 floppy + terminal,
   3013 terminal interface, 3015 HDLC + autoload, 3018/3019 SMD disc, 3004 10 MB disc,
   3020/3006 magtape, 3022 ND-500 interface, 3021/3028 bus expander, 3029 universal DMA.
2. **NORD-10 bus and I/O core:** 1022/1155 bus control, 1027 I/O control, 1028/1058 interrupt
   control, 1010/1011 I/O address and data, 1013/1014 device and DMA registers, 1020/1095
   terminal buffers, 1024 real-time clock.
3. **NORD-10 discs and tape:** 1036-1039 disc, 1076-1080 SMD, 1048/1049 and 1123/1124 magtape,
   1111/1118 floppy.
4. **NORD-10 comms and panel:** 1046/1141/1151 modem and HDLC, 1029-1033 and 1907 panel; then
   memory (1035/1055, MPM 1081-1090, Big MPM 1142-1146).
5. **ND-500.**
6. **NORD-1 and NORD-2B** (complete card sets).
7. **NORD-20** (very faint scan).

## Not done in this listing

- ND-120 and DELILAH sheets are not listed by name; NORD-50 vol. II not listed by card.
- Drawings without title blocks (panel, HP 7970, HDLC, ND-110 bus switch manuals) have no
  card number yet.
- Pages marked faint or photo were not checked at high resolution.
