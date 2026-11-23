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

## B3 - TMS6010 has no structured behaviour (test red)

**What fails.** `ChipBehaviourTests.EveryChip_HasABehaviour_ThatTheReaderAccepts("TMS6010")`.

**Cause.** No TMS6010 datasheet has been found (searched 2026-11-23: the TI 1975 Semiconductor
Memory Data Book lists only the TMS 6011, and a web search found nothing). The chip file is
written from the TMS 6011 sheet, which the ND manual names as equivalent, but the behaviour
rule is "only from the chip's own datasheet", so the TMS 6011's behaviour is not copied in.
`"kind": "none"` would be wrong too: the part does have a defined behaviour, we just do not
hold its sheet.

**What fixes it.** A TMS6010 datasheet (or a TI document that says the 6010 and 6011 behave the
same), then a UART part model in the format: control register (word length, parity, stop bits)
loaded by CRL, transmitter buffer and shift register clocked by TC, receiver by RC at 16 times
the bit rate, status outputs and their output enables. The TMS 6011 sheet does not say which
clock edge shifts, so that must come from the found sheet too.

**Fixed on 2026-11-23** (were in this entry): 74181 (gate equations from the block diagram,
7-280, checked against Table 2 by `Alu74181_GateEquations_GiveTable2`), 74120 (Figures 2-5,
7-121/7-122: Y rests low and Y' high while stopped, setup times are to the rising clock edge),
8820 (STROBE low forces the output high, read from the transistor schematic on 3-22),
2107B, TMS4030 (address and CS latched at the rise of CE) and MK4096P (row at RAS, column and
CS at CAS, output latch), using two format additions: a memory address may name stored bits,
and an output enable may be an expression (`outputEnable.when`).
