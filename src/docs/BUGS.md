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
