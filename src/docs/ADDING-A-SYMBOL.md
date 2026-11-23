# Adding a symbol that is not recognised

What to do when, while reading a card (see `READING-A-CARD.md`), something on the drawing does
not match any symbol we know. Written for an LLM agent doing the reading, and for the person
checking it.

**The rule: stop at that symbol and deal with it there.** Never draw it as the nearest known
symbol, never skip it, never leave it as an unnamed box "to be fixed later". A wrong symbol
gives a wrong part, a wrong part gives a wrong circuit, and nothing downstream will notice.

## Where symbols live today

| Folder / file | What it holds |
|---|---|
| `catalog/symbols/nd1976/<id>.json` + `.png` | how ND draws a part: a cut-out of the page in ND-13.003.01 "Circuit Diagram Symbols" (1976), with the page, what the shape looks like, and how to tell it apart |
| `catalog/symbols/pins/<shape>.json` | the pin layout of a gate shape (`and2`, `aoi-2x28` ...) used by the drawing code |
| `catalog/symbols/modern/` | the modern (IEEE/IEC style) version of each shape |
| `catalog/chips/<id>.json` | what the part DOES: pins, behaviour, delays, from its datasheet (`catalog/chips/FORMAT.md`) |
| `catalog/passives/` | resistors, capacitors, switch blocks, straps, diodes |
| `docs/SYMBOL-CATALOG.md` | every symbol in both styles, with its chip data |

## 1. Stop and record the unknown

- [ ] Cut the symbol out of the drawing at 1200 dpi (the `nd-schematics` skill, section 2) and
      keep the picture.
- [ ] Write down everything visible: the shape, marks inside it (X, diagonal, triangle, bar,
      double line, circle at a pin), the pin numbers on each line, labels beside it, the
      position code, any type written in or near it, and what its pins connect to.
- [ ] Put it in the sheet as a part with type `unknown`, confidence `open`, and a note with the
      above, so the reading can carry on round it without losing it.

## 2. Find out what it is - in this order

1. **ND's symbol manual** (ND-13.003.01, in `docs/sources/manuals/`). Render the page and
   look; never trust its OCR text. Contents on PDF page 5. If it is in there, the symbol is
   simply missing from our catalogue: go to step 3.
2. **The type written on the drawing**, if any, and the sheet's **parts list**: which part type
   is on the list but not yet found on the sheet? The count tells you what is left over.
3. **The pin numbers against datasheet pinouts.** A pin pattern often fits one part only (the
   7402 is proved by its 2,3->1 pattern). If it only narrows it to a family, say so.
4. **The board photo:** the chip at that grid position has its type printed on it.
5. **Other ND drawings** of the same period that use the same shape.
6. **The logic around it:** what the part must do for the circuit (and the driver software's
   values) to work. This can choose between candidates; it never proves a part alone.

If after all this the part is still not known: leave it `unknown` / `open`, write it in
`docs/PLAN.md` with the picture and what was tried, and tell the owner. Never guess.

## 3. Add the symbol

- [ ] `catalog/symbols/nd1976/<id>.json` with a `.png` cut-out: source (document and page - or,
      if the shape is not in the manual, the drawing and page it was seen on), `symbolType`,
      a plain description of the shape, `tells` (how to tell it from look-alikes), confidence.
      A shape seen only on drawings, not in the manual, is marked so.
- [ ] If it is a new gate shape: `catalog/symbols/pins/<shape>.json` (pin positions) and its
      modern version under `catalog/symbols/modern/`. Say where the modern mark comes from
      (IEEE 91 / IEC 60617), as `SYMBOL-CATALOG.md` does for the others.
- [ ] The chip file `catalog/chips/<id>.json` from the part's OWN datasheet with page numbers:
      pins, inverted pins, pin functions, behaviour (or "none" with the reason), delays,
      datasheet link. The datasheet pages go in as OCR text plus redrawn SVG figures, not the
      whole book (owner's decision, 2026-11-23).
- [ ] The entry in `docs/SYMBOL-CATALOG.md`.
- [ ] If the drawing code cannot draw the shape yet, that is a code change: write it, with a
      test, and render the symbol in both styles, light and dark, and look at it.

## 4. Prove and go on

- [ ] Change the part in the sheet from `unknown` to its type; confidence `read` if the type
      is written or proved by pins + count, else `inferred` with the reason.
- [ ] The tests pass, including the catalogue tests (every symbol has a chip file, every pin
      named), and `retroschematic check` passes on the sheet.
- [ ] Add a line to the `nd-schematics` skill's symbol table (section 3 or 4), with the sheet
      it was proved on, so the next reader knows it at once.
- [ ] Commit the symbol on its own, before the rest of the card, with a plain message.
