# The netlist file format, version 1

One JSON file describes one sheet of an ND card drawing: the chips on it, the gates inside
the chips, and the signal lines (nets) between their pins. Every converter (KiCad, Logisim,
EDIF), the renderer and the viewer read this file and nothing else.

The schema is `schema/nd-netlist-1.schema.json`. The first sheet written in it is
`sheets/nord-10/2A50-sync-modem-buffer-1050.rsc`.

## File endings

A sheet is stored in one of two endings; the content is the same JSON either way.

| Ending | What is in the file |
|---|---|
| `.rsc` | the JSON as plain UTF-8 text (no byte-order mark). Saving writes this unless `.rsx` is chosen. |
| `.rsx` | the same JSON zipped: a normal zip archive holding exactly one entry, named `sheet.json`, compressed with deflate, with the JSON as UTF-8 text inside. No folders, no other entries. |
| `.json` | the old ending, plain text like `.rsc`. Still read; renamed sheets in `sheets/` use `.rsc`. |

A reader goes by the content, not the name: a file starting with the zip header bytes `PK 03 04`
is opened as a zip and its `sheet.json` entry is read; anything else is read as text (a UTF-8
byte-order mark is skipped). The writer picks by the name: a path ending in `.rsx` is zipped,
every other path is written as text. The chip, signal and connector catalogues under `catalog/`
are not sheets and stay `.json`.

On Windows, File > "Open .rsc/.rsx files with RetroSchematic" makes both endings open in the
program on a double-click, for the current user only (keys under
`HKEY_CURRENT_USER\Software\Classes`: `.rsc` and `.rsx` name `RetroSchematic.Sheet`, whose
`shell\open\command` runs the program with the file). The menu item below it takes that away
again.

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
| `sheet` | the drawing: id (`2A50`), title, machine (free text), `bus` (`nord-10`, `nd-100` or `unknown`: which machine bus the card is for; it picks the bus-signal and connector catalogues, a NORD-10 sheet never uses the ND-100 ones and the other way round; missing reads as `unknown`, shown as a warning), drawing dates, and `source` (document file in `docs/sources/`, PDF page) |
| `parts[]` | one chip: `ref` = its position on the board as printed on the drawing (`13D`, `7-8C-D`), `type` (`7442`), `function`, `confidence`, `note`, optional `position` |
| `gates[]` | one gate inside a part: `id` (`11D/1`), `part`, `kind` (`nand`, `nor`, `not`, `and`, `or`, `buffer`, `and-or-invert`, `other`; `and-or-invert` is the 7450/7451 gate: the inputs are taken in pairs in the order listed, each pair is ANDed and the output is the NOR of the pairs), `inputs` and `output` as pin numbers, optional `position` |
| `nets[]` | one signal: `name` as on the drawing without subscript, `active` (`high` = subscript 1, `low` = subscript 0, `none`), `pins[]`, `note` |
| `nets[].drive` | what drives the line: `totem-pole` (normal TTL output), `open-collector` (outputs that can only pull low; several may share the line, which then needs a pull-up and is the AND of them), `open-collector-high-voltage` (open collector whose output may be pulled up above 5 V - 7406/7407 up to 30 V, 7416/7417 up to 15 V; the pull-up voltage goes in `pullUp`), `three-state` (bus driver that can switch off), or `none`. Absent means not read yet |
| `nets[].driveConfidence` | how sure the `drive` value is |
| `nets[].pullUp` | for an open-collector line: the pull-up as written on the drawing, or `open` |
| `nets[].pins[]` | `at` = `<part ref>.<pin>`; `P.<n>` = backplane pin and `F.<n>` = front connector pin, the connector not known yet; `P.<letter>.<n>` / `F.<letter>.<n>` (such as `P.A.32`) when the connector is known - a card can have several edge connectors, and the drawing shows the pin as `A.32`; pin 0 is allowed on a connector; plus `confidence` and `note` |
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

- `layout.buses` (2026-11-23): virtual buses. Drawing only - a bus never changes what is
  connected; the nets list reads the same with or without it. One entry per bus: `name`, `path`
  (points in whole mm, every piece level or upright; empty = not drawn yet) and `members`
  (`net` and, when two signals share a name, `active`). A signal is on one bus at most.

  ```json
  "layout": { "wires": [], "buses": [ { "name": "BD_0",
    "path": [ {"x": 122, "y": 55}, {"x": 122, "y": 220} ],
    "members": [ {"net": "BD0", "active": "low"}, {"net": "BD1", "active": "low"} ] } ] }
  ```

  A bus is drawn as a line thicker than a wire and thinner than a chip frame, with its name at
  its start. Every drawn pin of a member leaves with a wire that ends one grid step beside the
  bus and joins it with a 45-degree piece; the signal's name and its 0/1 subscript are written
  at that entry. A stored bus path that crosses a chip, a text or a label is not used: the bus
  is routed afresh between its two ends and the program says why. A pin that cannot reach its
  bus keeps its ordinary net label, and that is reported too.

- `nets[].description`, `nets[].descriptionSource`, `nets[].function` (2026-11-23): what the
  signal does, in plain words, and where that came from. A description is only ever what a
  source says; a signal no source explains has none. `descriptionSource` holds `kind`
  (`drawing`, `manual`, `reading` for a reading document such as `docs/2A50-reading.md`, or
  `inferred`), `document`, `place` (a section or page) and `confidence` (`read`, `inferred`,
  `open`). `function` is a list of short tags such as `interrupt` or `status`. Notes go in the
  existing `nets[].note`.

  ```json
  { "name": "IRFTIE", "active": "high", "description": "ICW bit 0: enable the level-12 interrupt on character received",
    "descriptionSource": { "kind": "reading", "document": "docs/2A50-reading.md",
                           "place": "ICW, offset 3: input control", "confidence": "read" }, "pins": [ ... ] }
  ```

  Facts that follow from the chips are NOT stored: the drive type (from each output pin's
  `outputType` in the chip catalogue), the driving pins, the number of loads, the bus a signal
  is on and the block levels it reaches are worked out when the sheet is read. When a stored
  `drive` disagrees with what the catalogue gives, both are shown and the difference is
  reported; the stored value is never overwritten.

- `connectors` (2026-11-23): which catalogue entry describes each of the card's connectors, as
  `{ "ref": "P", "catalog": "nord10-io-card-backplane", "note": ..., "position": { "x": 0, "y": 120 },
  "pinSide": "right" }`. `ref` is the connector
  as pin references name it (`P`, `F`, or `P.A` once the letter is known); `catalog` is a file id
  in `catalog/connectors`, and only an entry for the sheet's `bus` is used. Optional: every
  connector the signals use is drawn whether or not it has an entry. `position` (top-left corner
  in mm, like a chip's) is set when the box is moved by hand, and `pinSide` (`left` or `right`)
  says which side of the box the pins are on; both are optional. Both format versions accept it.

  **Where a connector box goes.** Without a `position`, a box goes on the side of the chips
  that its signals' chip pins are nearer to (by the mean X of those pins; supply lines and
  signals with more than 6 chip pins keep their tags, so they do not count), level with the
  mean height of those pins, with its pins on the side facing the chips. Boxes on one side are
  pushed down so none covers another. When a box goes left of the chips, the whole sheet is
  drawn further right by the box's room: every stored position, wire path and bus path moves
  with it, and a position read off the drawing takes that room away again, so the stored
  numbers do not change. Moving a box by hand never changes that room (the side is still
  worked out from the chips), so it never moves the chips.

  **A bus at a connector.** When two or more pins of one box belong to the same drawn bus
  (BD0..BD15 on the backplane), they are not each wired to the long bus line. The box gets a
  short upright piece of the bus just outside its pins, from the first member row to the last,
  with a short wire and a 45-degree entry from each member pin and the bus name beside it. It
  is the same bus, joined by name like a net label joins a signal. The piece stands far enough
  out that the labels of other signals on rows between the members fit before it.

  **How a connector is drawn.** Each connector is a box, headed with its
  name ("Backplane (connector not known)" for `P`, "Backplane A" for `P.A`) and with the
  catalogue's title above it. It has one row per pin the sheet uses, in pin order: the pin
  number (`A.32` when the letter is known, else `32`), the signal's name in the chosen style,
  and, when the catalogue names the pin differently, that name too (`IOXE = BIOXE`: the card
  drawing's name, then the backplane name from the bus-signal catalogue). Each pin has a pin
  line and a net label like a chip pin, so it is picked and traced the same way.

- `display` (2026-11-23): how this sheet likes to be shown. Display only - it never changes what
  is connected. Every value is optional; a missing one means "use the viewer's default", and a
  value here overrides that default for this sheet only. Both format versions accept it.

  | Field | Values | Meaning |
  |---|---|---|
  | `nameStyle` | `slash`, `tilde`, `subscript` | Active-low names as /NAME, ~NAME, or NAME with ND's subscript 0/1 |
  | `symbols` | `nd`, `modern` | ND 1976 symbols or modern ones |
  | `theme` | a theme name | Colours, such as `Light` or `Dark` |
  | `pinNumbers`, `boardPositions`, `portArrows` | true / false | Show them |
  | `buses` | true / false | Draw the buses; false draws every member on its own |
  | `tracePulse` | true / false | A traced signal pulses; false shows it still |
  | `lineWidth` | 0.5 to 3 | How thick every line is drawn, as a factor of the normal widths (1 = normal) |

  ```json
  "display": { "nameStyle": "subscript", "buses": false }
  ```

## How a sheet is drawn

The file holds connections only; the drawing is worked out from them every time. The rules, so
a reader knows what the picture means:

- **Wire or label.** A signal is drawn as a wire only when every one of its pins is drawn on this
  sheet, it has at most 6 chip pins, and the chip pins lie within 420 drawing units (70 mm) of
  each other. A signal that also goes to a backplane (P.n) or front (F.n) connector pin needs
  only the first two: its connector pin is joined by a real wire however far the box is, since
  the boxes stand right of all the chips. A member of a shown bus joins the bus with a named entry
  instead, from its connector pin too. Every other signal - long ones, busy ones, those that go to
  another sheet, and any whose wire cannot be routed - gets the ND net-label diamond at each of
  its pins, with the signal's name and its 0/1 subscript, the way ND's own drawings end a line in
  a named diamond. A wired pin never also has a diamond.
- **Routing.** Wires run on a 6-unit grid (one unit is 1/6 mm), round every chip frame, gate
  symbol and text, and round the room kept for every label. Two wires may cross at a right angle
  but never run along the same line. A branch point is marked with a dot.
- **Reading colours.** A wire branch is drawn as sure as its own pin's reading: `read` in the
  ink colour, `inferred` in the inferred colour, `open` red and dashed.
- **Line widths.** Chip frames and gate symbols are 2.25 times as thick as a wire.

These are drawing rules, not part of the format: a later program may draw the same file another
way and the file stays valid.

# Format version 2: blocks

Version 2 adds hierarchy: a design can be split into **blocks**, the way Verilog splits a design
into modules, KiCad into hierarchical sheets and Logisim into subcircuits. A block is defined
once and used (instantiated) any number of times, and blocks can contain instances of other
blocks to any depth. The schema is `schema/nd-netlist-2.schema.json`.

A version 1 file is still read: it is a design with one level only, the top block. A file is
written as version 2 only when it has blocks or instances; otherwise it stays version 1.

## The parts of a version 2 file

The top level of the file is the **top block**: its `parts`, `gates`, `nets` and `layout` are
exactly as in version 1. Two lists are added:

| Field | What it is |
|---|---|
| `blocks[]` | the block definitions, each used by name |
| `blocks[].name` | the block's name, unique in the file (`ADDRESS_DECODE`) |
| `blocks[].ports[]` | the block's connections to the level above it, in order |
| `blocks[].ports[].name` | the port name, unique in the block (`BA0`, `BD_0`) |
| `blocks[].ports[].direction` | `input`, `output` or `inout`, seen from inside the block |
| `blocks[].ports[].signals[]` | the signals inside the block this port carries, as `{net, active}`. One entry is a single port; more than one is a bus port, its width the number of entries, entry 0 the lowest bit |
| `blocks[].ports[].side`, `.order` | where the port pin is drawn on an instance box: `left`, `right`, `top` or `bottom`, and its place along that side from the top or left (0 first). Drawing only. Absent: inputs left, outputs right, bus ports top (inputs) or bottom (outputs and inout), in port order |
| `blocks[].parts`, `.gates`, `.nets`, `.layout` | the inside of the block, exactly as at the top level |
| `blocks[].instances[]` | instances of other blocks inside this block |
| `instances[]` | instances of blocks at the top level |
| `instances[].name` | the instance name, unique on its level (`U1`) |
| `instances[].block` | the block it is an instance of |
| `instances[].position` | where the top left of the instance box is drawn, mm. Drawing only |
| `instances[].connections[]` | one per port: `port` (its name) and `nets[]`, the signals on this level it joins, as `{net, active}`, as many as the port is wide, in the same bit order |

A signal on a level may have no pins of its own, only instance connections.

## How it maps to Verilog

Field names were chosen so the Verilog export (PLAN section 5) is a straight walk over the file:

| This format | Verilog |
|---|---|
| the top level of the file | the top `module`, named after `sheet.id` |
| `blocks[]` | one `module` each |
| `blocks[].name` | the module name |
| a port with one signal | `input wire BA0` / `output wire IRQ` / `inout wire DQ` |
| a port with n signals (bus port) | a vector port, `input wire [15:0] BD_0`, bit i = `signals[i]` |
| `direction` | `input` / `output` / `inout` |
| `nets[]` on a level | `wire` declarations in that module |
| `parts[]` | instances of the chip modules (one module per chip type) |
| `instances[]` | module instantiations: `ADDRESS_DECODE U1 ( .BA0(BA0), .BD_0({BD15_0, ..., BD0_0}) );` |
| `connections[].nets[]` | the named port connection; several nets are joined into one vector with `{ }`, highest bit first |
| blocks inside blocks | modules instantiating modules |
| `side`, `order`, `position`, `layout` | nothing (drawing only) |

A signal name with a 0/1 subscript becomes a Verilog name with `_0` / `_1` after it (`BD15_0`),
the same key the drawing uses.

## Checks a version 2 reader makes

Everything version 1 checks, on every level, plus:

- block names are unique, and every instance names a block that exists;
- no block contains itself, directly or through other blocks;
- port names are unique in a block, every port signal is a signal inside the block, and no
  signal is on two ports;
- every instance connection names a port of its block, and has exactly as many signals as the
  port is wide, each of them a signal on the instance's level;
- every port of an instance is connected once at most (an unconnected port is allowed and is
  reported by the export as a warning).

## The flattened netlist

Expanding every instance gives the design as one level, with hierarchical names: part `3C`
inside instance `U1` is `U1/3C`, its pin 9 is `U1/3C.9`, and a signal inside `U1` that does not
leave it through a port is `U1/NAME`. A signal that leaves through a port is the same signal as
the one it is connected to on the level above, and takes that name. The flattened netlist is
what the connections mean; making a block from a selection, or dissolving an instance back
into its level, never changes it (only the instance path in front of the names).
