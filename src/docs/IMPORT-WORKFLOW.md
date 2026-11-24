# Importing a new controller card: the workflow

How a new card goes from a scanned PDF to a finished, checked sheet file. Decided by the owner
on 2026-11-24. Parts of it are not built yet - see "Built or not" at the end. The reading
method behind each step is in `READING-A-CARD.md` and the `nd-schematics` skill.

## Two ways to start, one workflow

- **AI mode:** an LLM agent runs every step through the MCP tools, all the way to "finished".
- **Manual mode:** a person works in the app and asks the LLM for help on any step or item
  when needed (for example "read this unsure pin" or "run review round 2").

Both use the same steps and the same tools, and can be mixed on one card: a person can take
over an AI card at any point, and an agent can take over a manual one.

## The steps

The app has an "Import card" panel that shows these steps in order and which are done. Each
step is also an MCP tool.

1. **Pick the pages.** Choose the PDF and the pages of the card (title block, all sheets,
   every revision noted).
2. **Turn and render.** Each page is turned the right way up (found automatically, can be set
   by hand) and rendered to PNG at 300 dpi into `<card>-files/sources/`.
3. **Make the card.** A new card file (format version 3) with one page per source page;
   machine and bus set (NORD-10 / ND-100).
4. **Board and photos.** Import board facts and photos from the ND PCB catalogue (`pcb.json`);
   if the board is not there, say so to the owner.
5. **Pre-analyse.** The offline networks mark chips, gates, passives, circles, diamonds, dots
   and position codes on each page, each with how sure it is.
6. **Read.** Accept or correct the finds as parts, then connect wire ends to pins (the chip's
   pinout is offered). Done by a person, an LLM agent, or both.
7. **Review rounds** (below).
8. **Finish.** Pack the `.rsx`, run the checks, and mark the card finished.

## Review rounds - a fixed ladder, each stricter

| Round | What it checks |
|---|---|
| 1. Rules | every pin number exists in the chip's pinout; one pin, one signal; gate pins form one gate; chip count matches the printed parts list; open-collector lines have a pull-up; every `H` input names what pulls it high |
| 2. Second reader | a fresh reader (person, or an LLM that has not seen the first reading) re-reads everything still unsure, at 1200 dpi |
| 3. Outside proof | the board photo (chip types at each position), the bus and connector catalogues, and the driver software's values must decode |
| 4 and on | re-read only what failed in the round before, until a round passes |

Each round's result is stored with the card: what was checked, what failed, what was fixed.

## Who made it and how sure

Every part, pin and wire records:

- **made by:** network, LLM or person;
- **how sure:** the confidence, plus read / inferred / open;
- **checked in:** which review round(s) checked it.

The app colours by this and can list "everything not yet checked by a person". Every accepted
box, pin and correction is saved as training data, with its source, for the offline networks.

## Finished

- **AI-checked:** an LLM may mark a card finished when every round passes.
- **Human-checked:** a person reviews and signs it off.
- A person can reopen any item or card at any time.

## Built or not (2026-11-24)

- Built: new card (format v3 pages), sources and photos, PCB catalogue import, rule checks in
  `retroschematic check` (part of round 1).
- Being built: pre-analyse and assisted reading in the app (steps 5-6).
- Not built: the "Import card" panel, page picking/turning/rendering in the app (steps 1-2),
  the review-round records, the "made by / checked in" fields, the finished states.
