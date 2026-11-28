# Known bugs

Problems that are not fixed are written down here by name. Fixed ones say how, and stay until
the fix has been watched over several runs.

## B1 - window tests hang while saving pictures of a traced signal

**What happens.** A run of the window tests (`tests/RetroSchematic.Desktop.Tests`) stops
moving. No test fails; the test host just stops until `dotnet test` is killed, or until
`--blame-hang-timeout` takes a dump and ends it. This shows up in two ways:

- the run stops after some tests have passed, and the blame step reports it as "Test host
  process crashed";
- without a hang timeout, the run never ends at all.

**How often.** Only with `RETROSCHEMATIC_PANEL_PNG` set, which saves panel pictures. Without
it, 13 runs in a row were clean. With it, 8 of 9 runs hung.

**What the dumps show.** The test thread was inside `DetailsPanelTests.Save`, in
`CaptureRenderedFrame` (or the `RunJobs` call before it), while a signal was traced. It was
running render jobs one after another: once inside the compositor and once inside
`SchematicCanvas.Render`. It was not blocked on a lock.

**Cause.** The trace pulse was a `DispatcherTimer` at Render priority. It asked for a redraw
every 40 ms whenever a signal was traced. With real Skia drawing, one frame of the whole 2A50
sheet takes longer than that, so there was always a new render job waiting.
`Dispatcher.RunJobs()`, which `CaptureRenderedFrame` calls, runs jobs until none are left, so
it never returned. In the app itself the same timer ran ahead of input handling on a slow frame
(Render priority comes before Input).

**What was tried, measured with picture saving on.**

| Change | Runs | Hung |
|---|---|---|
| none | 1 | 1 |
| no `RunJobs` before the capture | 8 | 7 |
| the pulse asks for one frame at a time (`RequestAnimationFrame`) | 8 | 8 |
| the pulse switched off while the pictures are taken | 4 | 0 |

So the trigger is a capture made while the pulse is running. A still picture of something that
keeps moving never settles, so `CaptureRenderedFrame` never returns.

**Fix.**

- The picture helper (`DetailsPanelTests.Save`) switches the pulse off for its capture and back
  on after it. It no longer calls `RunJobs` itself.
- In the app, the pulse is now driven by frames (`SchematicCanvas.NextPulseFrame`, using
  `TopLevel.RequestAnimationFrame`) instead of a fixed 40 ms timer at Render priority. The
  timer could ask for frames faster than a big sheet draws them, and Render priority runs before
  input. The measurement above shows this change alone does not fix the test hang.

**Still open.** A window test that fails while its window is open leaves that window drawing.
Every later window test in the same run then fails in about 1 ms with "The given key
'fonts:SystemFonts' was not present in the dictionary", raised from `SchematicCanvas.DrawText`.
So one real failure shows up as 15 or more failures. Only the first failure in the list is
the real one.

## B2 - tag diamonds on the front connector rows had pieces missing (fixed)

**What happened.** The diamonds (tags) beside the front connector rows (F.81 .. F.95) were drawn
with gaps in their outline, in the window and in the SVG export.

**Cause.** Those pins are `inferred`, and an inferred line is drawn dashed: 6.8 units of line,
4.2 units of gap. The style rule (`SchematicStyle.Of`) gave a tag the same dashed line as a
wire. A diamond with 8-unit sides is only about 23 units round, so two or three gaps of 4.2
units cut whole pieces out of it. It had nothing to do with the step that cuts lines at circles
(that one never touches a path) and nothing to do with clipping or pixel snapping.

**Fix.** A tag's diamond is always drawn with a solid line. How sure the reading is still shows
in the diamond's colour, the same colour as the name beside it. The test
`StyleTests.EveryTagDiamond_IsWhole_AndSolid` checks every diamond on 2A50, the blocks demo and
the gate demo, in both symbol styles: four equal 45-degree edges, closed, drawn solid. It fails
on the old rule.

## B3 - TMS6010 had no structured behaviour (closed 2026-11-23)

**What failed.** `ChipBehaviourTests.EveryChip_HasABehaviour_ThatTheReaderAccepts("TMS6010")`.

**Cause.** No TMS6010 datasheet has been found (searched 2026-11-23: the TI 1975 Semiconductor
Memory Data Book lists only the TMS 6011, and a web search found nothing), and the rule was
"behaviour only from the chip's own datasheet".

**The owner's ruling (2026-11-23).** The TMS6010 is described from the TMS 6011 datasheet. Every
fact in `catalog/chips/TMS6010.json` now carries the mark "from the TMS 6011 sheet; the ND manual
calls the 6010 equivalent", with the 6011 pages (TI 1975 Semiconductor Memory Data Book, printed
152-157, PDF 153-158) and the ND manual statement (ND-13.003.01 printed page 59, PDF page 65:
"TMS6010 Universal asynchronous receiver/transmitter (TMS 6011, AY-5-1012, AY-5-1013)"). The file
has an `equivalence` section that records the ruling. The page range is the whole 6011 sheet;
which page holds each single fact has not been checked.

**How it was closed.** The behaviour is `"kind": "none"` with a reason, the same as the COM 2601:
the format has no serial shift engine (receiver sampling at 16 times the bit rate, transmitter
shifting start, data, parity and stop bits), and the 6011 sheet does not say which edge of TC
and RC shifts the data. The words the sheet gives are in `pins[].text`, `functions` and `state`.
A runnable UART model needs a format addition and that clock edge; it is a plan item
(`docs/PLAN.md`, section 2).

## B3a - chip behaviours fixed the same day

**Fixed on 2026-11-23** (were in this entry): 74181 (gate equations from the block diagram,
7-280, checked against Table 2 by `Alu74181_GateEquations_GiveTable2`), 74120 (Figures 2-5,
7-121/7-122: Y rests low and Y' high while stopped, setup times are to the rising clock edge),
8820 (STROBE low forces the output high, read from the transistor schematic on 3-22),
2107B, TMS4030 (address and CS latched at the rise of CE) and MK4096P (row at RAS, column and
CS at CAS, output latch), using two format additions: a memory address may name stored bits,
and an output enable may be an expression (`outputEnable.when`).

## B4 - card 3207: open points in the reading (opened 2026-11-28)

All 24 sheets of card 3207 (SCSI/floppy, ID 350007) are read into `docs/3207-readings/` and
written into `sheets/nd-100/3207-scsi-floppy.rsc` by `tools/card-readings.py sheet`. These
points are not settled; each one also has a note in its reading file.

- **R4 printed twice (settled 2026-11-28).** Ronny confirmed sheet 22's R4 (1 kohm, H6 pull-up)
  is right. The 220 ohm LED resistor printed "R4" on sheet 13 (grid F2) is stored as R47, the one
  free number among that sheet's R45-R50, with confidence inferred and the printed label in the note.
- **Socket 18A (settled 2026-11-28).** The photo shows the 350217 SCSI single/differential adapter
  (PCB 1887, print A2) plugged into it, moved towards SING or DIFF. It is stored as board
  connector `J.18A` (new connector kind J, FORMAT.md) with catalogue
  `nd100-3207-sing-diff-socket`. Still open: which pins the adapter joins in each position.
- **28C on sheet 21** is printed with input pin 3; read as 13 because sheet 23 uses every other
  gate of the 74F04 (proved by elimination, not by the drawing).
- **The 74F04 on sheet 24** is labelled "1662"; read as 16G2 (G and 6 look alike in the font).
- **The 74S08 on sheet 23** has no position code printed; stored as U74S08.
- **PROM and PAL contents are not read** (the drawings give only the part number): 009_00,
  058_00, 345_00, 357_00, 458_00, 459_00, 78200, 42500, 78300, 42801, 42200, 78100, 78400.
  A behaviour for these needs a dump of the real chips.
- **Port directions** of the sub-blocks are inferred from the catalogue roles of the chip pins
  inside each block; drivers outside a block are not counted, so a bus such as BD~00-23 shows
  as `output`. `directionConfidence` is `inferred` or `open` on every one.

## B5 - PageWindowTests leaves its working copies behind (opened 2026-11-28)

`tests/RetroSchematic.Desktop.Tests/PageWindowTests.cs` saves a two-page copy of 2A50 in
`SetUp` and deletes it in `TearDown`. After a full test run both copies
(`pagetest-PageTabs_PageKeys_AndTheOriginalViewFollowing.rsc` and
`pagetest-TheOverview_ListsThePages_AndThePhotos_AndAPageButtonDrawsIt.rsc`) are still on disk,
so the delete does not happen or does not work. The cause is not found yet.

Until 2026-11-28 the copies were written into the test's `sheets` folder, where the card library
and the Bus use window read them as extra 2A50 cards. They are now written to the test's own work
folder (`pagetest/`), so they no longer reach the library. The left-behind files are still a
fault, and this entry stays open until the cause is found and fixed.

## B7 - card 3207: chips with no datasheet found, and reading points the catalogue raised (opened 2026-11-28)

No catalogue file, because no datasheet was found for the exact part (a similar chip's facts are
never used). NCR5386 now has a file (2026-11-28): pins from NCR's own 5386 pinout, pin roles from
the pin-compatible 5385E data sheet.
- **HY5010_100** (delay line, 10 taps, 100 ns): probably the Hytek HY5010 family (10-tap TTL delay line, 14-pin DIP); a scanned HY5010 data sheet is listed at datasheetarchive.com (search "hy50"), but no copy with the pin table was reached, and no -100 variant is listed.
- **1N3246** (diode): no datasheet; the reading says "as read at 330 dpi", so the number may be misread.
- **2170576F** (hex switch, from the drawing notes): no datasheet, pin numbering unknown.

Checked on the drawings at full size, 2026-11-28; the readings were right in all of these:
- **Zener D1 (sheet 10):** the bar (cathode) is pin 1 on AVC, the triangle (anode) pin 2 on
  ground, so it holds AVC at 5.6 V.
- **Diodes D4 (sheet 19) and D10 (sheet 16):** both pass +5 V into their terminator rails (TERMS,
  DIFFPWR). The drawing numbers them from opposite ends: D4 pin 1 is the bar (cathode), D10 pin 1
  is the triangle (anode). Still open: a 1N3246 catalogue file cannot give one fixed pin rule
  for both, so its pins must be read per diode from the drawing.
- **RN1 / RN3 (4608X104_221/331, sheet 19):** the Bourns figure joins R2 to pin 1 and R1 to
  pin 8, so 330 ohm goes to ground (pin 1) and 220 ohm to TERMS (pin 8), as read.
