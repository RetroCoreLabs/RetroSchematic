# The netlist file format, version 1

One JSON file describes one sheet of an ND card drawing: the chips on it, the gates inside
the chips, and the signal lines (nets) between their pins. Every converter (KiCad, Logisim,
EDIF), the renderer and the viewer read this file and nothing else.

The schema is `schema/nd-netlist-1.schema.json`. The first sheet written in it is
`sheets/nord-10/2A50-sync-modem-buffer-1050.json`.

## The version rule

The file starts with:

```json
{ "format": "nd-netlist", "formatVersion": 1, ... }
```

- A change that an old reader can safely ignore (a new optional field) keeps the version.
- A change that an old reader would get wrong (a field renamed, removed, or with a new
  meaning) raises the version, gets a new schema file, and every converter is updated.
- A reader refuses a file whose `formatVersion` it does not know. It never guesses.

## How sure a reading is

Every part, gate and pin carries `confidence`:

| Value | Meaning |
|---|---|
| `read` | read off the drawing |
| `inferred` | worked out from the drawing plus something else (a pinout, a datasheet, the driver's values); `note` says what |
| `open` | not known. Kept in the file, never dropped |

The mark sits on each pin, not on the whole net, because one end of a wire can be clear on
the scan while the other is smudged.

## Fields

| Field | What it is |
|---|---|
| `sheet` | the drawing: id (`2A50`), title, machine, drawing dates, and `source` (document file in `docs/sources/`, PDF page) |
| `parts[]` | one chip: `ref` = its position on the board as printed on the drawing (`13D`, `7-8C-D`), `type` (`7442`), `function`, `confidence`, `note`, optional `position` |
| `gates[]` | one gate inside a part: `id` (`11D/1`), `part`, `kind` (`nand`, `nor`, `not`, `and`, `or`, `buffer`, `other`), `inputs` and `output` as pin numbers, optional `position` |
| `nets[]` | one signal: `name` as on the drawing without subscript, `active` (`high` = subscript 1, `low` = subscript 0, `none`), `pins[]`, `note` |
| `nets[].drive` | what drives the line: `totem-pole` (normal TTL output), `open-collector` (outputs that can only pull low; several may share the line, which then needs a pull-up and is the AND of them), `open-collector-high-voltage` (open collector whose output may be pulled up above 5 V - 7406/7407 up to 30 V, 7416/7417 up to 15 V; the pull-up voltage goes in `pullUp`), `three-state` (bus driver that can switch off), or `none`. Absent means not read yet |
| `nets[].driveConfidence` | how sure the `drive` value is |
| `nets[].pullUp` | for an open-collector line: the pull-up as written on the drawing, or `open` |
| `nets[].pins[]` | `at` = `<part ref>.<pin>`; `P.<n>` = backplane pin; `F.<n>` = front connector pin; plus `confidence` and `note` |
| `position` | on a gate: where the gate is drawn on the sheet, in millimetres from the top left of the scan. On a part: where the top left corner of the chip's frame is drawn, same units; the editor writes it when a chip is placed or moved. Optional on both: absent means not measured, and the renderer lays it out itself (a part with no position but measured gates gets a frame round its gates; otherwise it sits at its board position) |

A pin reference splits at its LAST dot, so a part ref such as `7-8C-D` is fine.

## Checks a reader makes

- `format` is `nd-netlist` and `formatVersion` is known.
- Every gate names an existing part, and every pin reference names an existing part or `P`/`F`.
- No pin is in two nets.
- Part refs and gate ids are unique.

## Optional fields added within version 1

- `nets[].drive`, `nets[].driveConfidence`, `nets[].pullUp` (2026-11-23). Older files without them
  are still valid: the drive of their lines is simply not read yet.
- `layout.wires` (2026-11-23): wire paths fixed by hand. Drawing only - it never changes what is
  connected. One entry per signal: `net` (its name), `active` (its level, needed when two signals
  share a name, as BD15 with subscript 0 and with 1 do on 2A50), and `branches`. Each branch is a
  list of points in whole mm from the top left, every piece level or upright. The first branch
  joins two of the signal's pins; each later one runs from one more pin to the wire before it.

  ```json
  "layout": { "wires": [ { "net": "CLK", "active": "high",
    "branches": [ [ {"x": 60, "y": 40}, {"x": 80, "y": 40}, {"x": 80, "y": 70} ] ] } ] }
  ```

  A program reading the file keeps a stored path exactly when it still obeys every routing rule
  (see "How a sheet is drawn"). When it does not - most often because a chip was moved onto it -
  the path is not used, the signal is routed afresh, and the program says why; the stored path
  stays in the file until it is changed or cleared.

## How a sheet is drawn

The file holds connections only; the drawing is worked out from them every time. The rules, so
a reader knows what the picture means:

- **Wire or label.** A signal is drawn as a wire only when every one of its pins is drawn on this
  sheet, it has at most 6 pins, and the pins lie within 420 drawing units (70 mm) of each other.
  Every other signal - long ones, busy ones, and any that also go to the backplane, the front
  connector or another sheet - gets the ND net-label diamond at each of its pins, with the
  signal's name and its 0/1 subscript, the way ND's own drawings end a line in a named diamond.
- **Routing.** Wires run on a 6-unit grid (one unit is 1/6 mm), round every chip frame, gate
  symbol and text, and round the room kept for every label. Two wires may cross at a right angle
  but never run along the same line. A branch point is marked with a dot.
- **Reading colours.** A wire branch is drawn as sure as its own pin's reading: `read` in the
  ink colour, `inferred` in the inferred colour, `open` red and dashed.
- **Line widths.** Chip frames and gate symbols are 2.25 times as thick as a wire.

These are drawing rules, not part of the format: a later program may draw the same file another
way and the file stays valid.
