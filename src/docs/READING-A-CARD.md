# Reading a new card: the checklist

How to turn one Norsk Data card drawing into a complete sheet file in this repository. Written
for an LLM agent doing the work, and for the person checking it. Follow it in order and tick
every line; a step left out is the kind of gap that was found by hand on the first card.

The worked example for every step is NORD-10 sheet 2A50, the synchronous modem buffer card
1050: `sheets/nord-10/2A50-sync-modem-buffer-1050.rsc`, with `docs/2A50-reading.md` and
`docs/2A50-interrupts.md`.

The reading method itself (symbols, subscripts, how to prove a chip type) is in the
`nd-schematics` skill. This document is the order of work and what must end up in the files.

## Ground rules

- **Every fact carries how sure it is:** `read` (seen on the drawing), `inferred` (worked out;
  the note says from what), `open` (not known). An open pin is kept, never dropped, never
  guessed.
- **Never use the OCR text of a drawing page.** It is invented. Render the page and look.
- **A chip's behaviour comes only from its own datasheet**, with the page. Never from a
  similar chip.
- **Agreeing sources are not proof** if one was copied from the other.
- **Plain words** in every note and document.
- **No drive letters or machine paths** in any file in this repository. Paths are relative to
  the repository.
- **Commit messages never mention an AI.**

## 1. Find the card and its material

- [ ] The card number (e.g. 1050), sheet number (e.g. 2A50), machine (NORD-1, NORD-10, ND-100,
      ND-500) and which drawing book and PDF page(s) it is on. `docs/LIBRARY-INVENTORY.md`
      lists the cards we have; confirm the page by reading the title block (bottom right, or
      top right on the sideways ND-100 pages), never from the OCR text.
- [ ] Every page of the card. Several pages of one card can be different revisions: say
      which one you read.
- [ ] The board in the ND PCB catalogue (`pcb.json`, matched by card number or part number):
      part number, print and ECO version, size, layers, switch and strap text, chip list with
      datasheet links, and the board photos (component side, solder side, old/new variants).
- [ ] The driver that uses the card (SINTRAN source in NDInsight), for the cross-check in
      step 7.

## 2. Make the sheet file and its files

- [ ] Create `sheets/<machine>/<sheet>-<card-name>-<card-number>.rsc`
      (e.g. `sheets/nord-10/2A50-sync-modem-buffer-1050.rsc`), with the window (File > New
      sheet) or the MCP tool `new_sheet`. Set `bus` to `nord-10` or `nd-100`: it picks the bus
      signal and connector catalogues.
- [ ] Render each drawing page to PNG at 300 dpi and put it in
      `sheets/<machine>/<sheet>-files/sources/` (e.g. `2A50-sync-modem-buffer-1050-p64.png`).
      Add it as a source (`add_source`, or File > Board info): file, book, PDF page, title.
- [ ] Import the board facts and photos from the PCB catalogue (File > "Import board info from
      the ND PCB catalogue", MCP `import_board_from_catalogue`, or the command line
      `import-board`). This fills `board` and copies the photos to
      `sheets/<machine>/<sheet>-files/photos/`. Every imported fact is marked with its source.
- [ ] Check the board type's position grid fits the photo (PCB front view, F9 Chips). The
      columns come from `catalog/boards/`; the rows are set per card (`pcb` section).

The format of every section is in `docs/FORMAT.md`. Do not invent fields.

## 3. The title block and the parts list

- [ ] Card name, number, sheet number, dates, revision - into `sheet`.
- [ ] The printed parts list ("5 stk 7400, 3 - 7402 ... 40 pk"): write it down. Every chip type
      and every count you find later must add up to this list. It is the main proof tool.

## 4. Every part on the sheet - chips AND everything else

Go over the whole page at 400 dpi in a grid of tiles, then zoom to 1200 dpi round every part.

**Chips**

- [ ] A symbol you do not recognise: stop there and follow `ADDING-A-SYMBOL.md` before going on.

- [ ] Each chip's position code (`13D`, `7-8C-D`) and type. If the type is written in the box,
      still check the pins against the datasheet pinout. If it is not written, prove it: the
      pin numbers on the drawing against the datasheet pinout, then the count against the parts
      list (skill, section 4).
- [ ] Each chip type has a file in `catalog/chips/`. If not, write it from the datasheet
      first (pins, inverted pins, pin function text, behaviour, delays, datasheet link and
      page). A chip whose datasheet cannot be found gets behaviour "none" with the reason.
- [ ] Each gate: which pins form it, and its kind (nand, nor, not, and-or-invert ...).
- [ ] Check against the board photo: the chip printed at that grid position on the component
      side should be the same type.

**Passive parts and switches** - the first reading of 2A50 missed these; do not skip them

- [ ] Every resistor and resistor pack: value as written (`1k`, `47K`), both pins. A pull-up
      goes from a pin to +5V / H.
- [ ] One resistor drawn above several lines with a dot on each line = a pull-up on every one
      of those lines. Decide from the photo and the parts list whether it is a pack or one
      resistor per line; if you cannot, say so.
- [ ] Every capacitor: value as written (`10uF`, `220pF`), both pins, polarity if drawn.
- [ ] Every one-shot (74221 ...) has its timing resistor and capacitor recorded on the right
      pins.
- [ ] Every switch block: position, number of levers, which line each lever switches and to
      what (usually ground), and what each lever means (address bit, ident bit) - in the part
      and in `switches`.
- [ ] Every strap or wire link, diode, test point.
- [ ] Every `H` / `H1` input: tied to +5V directly or through which resistor.
- [ ] Find each capacitor, resistor row and switch block on the photo as well.

## 5. Every wire, pin by pin

- [ ] For every pin of every part: which net it is on. Net names keep ND's name and subscript
      (`CDSTL_1`, `BA2_0`); an unnamed net gets the automatic name from its driver.
- [ ] **Read the subscript at each pin**, especially clock and enable pins. Do not assume a pin
      takes the inverted copy because an inverter for that signal stands nearby. (2A50: the
      receive interrupt enables clock on ICW₀, not ICW₁ - the first reading had it wrong.)
- [ ] A pin number sits above its wire on stacked-box pins (74153, 74157).
- [ ] Every circled number: it marks a connector pin where the signal leaves the card. Prove
      which connector and which pin from the bus catalogue (`catalog/signals/`) and the
      connector files (`catalog/connectors/`): backplane `P.<n>`, front `F.<n>`, or `P.A.32`
      when the connector letter is known. A circle with no name or tied to ground or a supply
      is still a pin: find out what it is, or mark it open.
- [ ] Every net with only one pin: either it leaves the card, ends at a switch or passive
      part, or it is a reading gap - follow it again.

## 6. Signal facts

- [ ] Each net's active level (`_0` = true when low, `_1` = true when high).
- [ ] A description for each signal, with its source: the drawing, a manual, the bus
      catalogue, or the reading document. Never a guess.
- [ ] Open-collector lines have their pull-up; three-state lines have their enables. The tool
      warns about an open-collector line without one.

## 7. Prove the reading

- [ ] `retroschematic check <sheet>` passes (from `src`: `dotnet run --project
      RetroSchematic.Cli -- check sheets\...`). It refuses a pin on two nets, a part type not
      in the catalogue, and unknown fields.
- [ ] Every count in the parts list matches the parts in the file.
- [ ] Decode the registers: address decode (comparator and switches), register decode (the
      1-of-N decoder; check every input for a missing inverter), write registers (which latch
      each strobe clocks, which BD bit feeds it), read registers (multiplexers and bus drivers
      back to the sources), interrupts (back to the AND-OR terms; what sets and clears each
      enable). Write this in `docs/<sheet>-reading.md`.
- [ ] Decode every value the driver software writes. If one does not decode, a reading is
      wrong (on 2A50 this proved a label read as "BD5" was BD8).
- [ ] **A second reader** (a fresh agent that has not seen the first reading) re-reads the
      paths that matter - interrupts, register decode, every passive part - pin by pin at 1200
      dpi and compares with the file. Fix every proven error, with a note on the pin.

## 8. Finish

- [ ] Mark where each part, gate, circle, passive and label sits on the scanned page (a box on
      the source page, linked to the part). These boxes are the training data for a symbol
      finder that will read new cards first (`docs/PLAN.md`); they cost little now and cannot
      be recovered later. Until the window can draw them, note that the card still needs them.

- [ ] Open the sheet in the app: Drawing (F5), Original (F6), PCB front (F7), PCB back (F8).
      Trace a few signals end to end. Look at the drawing: nothing on top of anything else.
- [ ] Pack it: File > Save As `.rsx` (or `retroschematic pack`), so one file carries the sheet,
      the drawing pages and the photos.
- [ ] Run the tests (`dotnet test RetroSchematic.slnx -nodeReuse:false` from `src`) and read
      the counts. `python eng\check-docs.py` must say "no problems".
- [ ] Add the card to `docs/LIBRARY-INVENTORY.md` as read, and every open question to
      `docs/PLAN.md`.
- [ ] Commit only your own files, with a plain message. Do not push unless asked.

## What the files hold, in short

| File | What goes in it |
|---|---|
| `sheets/<machine>/<sheet>-<name>.rsc` | the sheet: parts (chips and passives), gates, nets with every pin and its confidence, signal descriptions, board facts, connectors, switches, sources, photos, PCB grid rows |
| `sheets/<machine>/<sheet>-files/sources/*.png` | the drawing page(s) at 300 dpi |
| `sheets/<machine>/<sheet>-files/photos/*.jpg` | the board photos, copied from the PCB catalogue |
| `sheets/<machine>/<sheet>-<name>.rsx` | all of the above packed in one zip |
| `docs/<sheet>-reading.md` | the register-level reading, every row read / inferred / open |
| `catalog/chips/<type>.json` | a chip type not yet in the catalogue, from its datasheet |
| `datasheets/<maker>/*.pdf` | the datasheet PDF, with its web link in the chip file |
