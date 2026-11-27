# The netlist file format, version 1

(Versions 2 and 3 are further down: "Format version 2: blocks" and "Format version 3: pages".
Every version is still read.)

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
| `.rsx` | the same JSON zipped: a normal zip archive whose first entry is `sheet.json`, compressed with deflate, with the JSON as UTF-8 text inside. The sheet's source pages and board photos are stored beside it as `sources/NAME` and `photos/NAME` (see "Board, sources, photos, switches"), so one file carries everything. No other entries. |
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

- `board`, `sources`, `photos`, `switches`, and more fields on `connectors` (2026-11-23): see
  "Board, sources, photos, switches" below. Both format versions accept them. Later the same
  day the `board` fields were renamed to the ND PCB catalogue's names (`boardNumber` became
  `partNumber`, `size` and `layers` moved into `specifications`) and the catalogue's other
  fields were added; only 2A50 used the old names, and it was changed with them.

- `pcb` and `photos[].corners` (2026-11-23): the chip grid on the board photos, see "Board,
  sources, photos, switches" below. Both format versions accept them.

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

- Passive parts (2026-11-23): resistors, resistor networks, capacitors, diodes, straps and
  switch blocks are ordinary `parts[]` entries with pins in nets; see "Passive parts" below.
  Both format versions accept them. Older files have none and stay valid.

## Passive parts

A pull-up resistor, a capacitor, a switch block or a strap is a normal part: it has a `ref`, a
`type`, `confidence` and pins that nets name (`R1.1`, `15E.8`), so it is traced, picked and
checked like a chip. The type says the kind and carries the count, so a reader needs no
catalogue to check the pins:

| Type | Kind | Pins |
|---|---|---|
| `R` | resistor | 1, 2 |
| `RN-SIP-n` | resistor network in one row of pins | 1 = common, resistor k to pin k+1 (n+1 pins) |
| `C` | capacitor | 1, 2 |
| `C-POL` | polarised capacitor | 1 = plus, 2 = minus |
| `D` | diode | 1 = anode, 2 = cathode |
| `STRAP` | strap (wire link) | 1, 2 |
| `SW-DIP-n` | switch block of n levers | lever k joins pin k and pin 2n+1-k |

The pin rules for `RN-SIP-n` and `SW-DIP-n` are the usual layouts, not read from a datasheet
(`catalog/passives/` says so in `pinNumberingConfidence`); a part laid out differently needs
its own kind.

Extra part fields, for passive parts only (a reader refuses them on a chip):

| Field | What it is |
|---|---|
| `value` | the value as written on the drawing (`1k`, `470p`); absent when the drawing gives none. Never filled in from anything else |
| `tolerance` | the tolerance as written on the drawing (`5%`) |
| `valueConfidence` | how sure the value is, when that differs from the part's `confidence` |
| `serves` | for a part the drawing gives no name: the board position of the chip it serves (a part anywhere in the file, at any level) |

**Names.** A part keeps the name ND wrote on the drawing (`R1`, `C3`), with `confidence`
`read`. A part with no name gets an automatic one made from the chip it serves: the kind's
letter, the chip and a running number (`R-13D-1`; letters R, C, D, S for a switch block, W for
a strap), with `confidence` `inferred` and `serves` set. The dash tells it from ND's names.

**Supply lines.** A resistor's far end on `+5V`, `H`, `H1`, `VCC` (or any `+n` name) is a
pull-up; on `GND` or `0V` a pull-down. The signal's facts list it ("pulled up by R1 (1k) to
+5V"), and an open-collector line with no pull-up resistor on the sheet (and no `pullUp` text
naming one) is shown with a warning.

**Switch blocks and the `switches` section.** A `switches[]` entry and a `SW-DIP-n` part with
the same `ref` are one thing: the part carries the pins, the section what each lever means. A
reader refuses a sheet where they disagree: the part is not a switch block, `leverCount` is not
n, a lever number is outside 1..n, or a lever's `signal` is not on either of that lever's pins
(when those pins are wired). A `switches[]` entry with no part is still allowed (the pins are
not read yet).

**How they are drawn.** The ND symbol manual (ND-13.003.01) has no symbol for any of these, so
the ND style here is a choice, not read from an ND drawing: a resistor is an oblong and a switch
block has a frame round its levers. The modern style draws the zigzag resistor and the levers
without a frame. A capacitor is two plates (a polarised one marked +, its minus plate curved in
the modern style), a diode the triangle and bar, a strap two posts with an arch. The reference
is written muted above, the value bold. The reference is hidden with the board positions, like a chip's. A passive pin on a supply line ends in a supply symbol
(a bar with the name for a high supply, the three shortening bars for ground, turned to point away from the part so it fits one pin row) instead of a net label or a
wire. A part with no `position` is drawn just below the chip it serves (or the first chip it
shares a signal with).

```json
{ "ref": "R1", "type": "R", "value": "1k", "confidence": "read" },
{ "ref": "R-13D-1", "type": "R", "serves": "13D", "confidence": "inferred",
  "note": "No name on the drawing; the name is made from the chip it serves." },
{ "ref": "15E", "type": "SW-DIP-4", "function": "device address", "confidence": "read" }
```

## Board, sources, photos, switches

Facts about the printed circuit board itself, the pages the sheet was read from, and photos of
the board. None of them changes what is connected. All are optional and belong to the whole
sheet (the top level), whatever block is shown.

**`board`** - what the board is. Every value is optional; a missing one is not known. The field
names are the ones the ND PCB catalogue uses (`pcb.json`, scraped from norsk-data.com, kept
outside the repository), wherever a field means the same, so an import is a straight copy.

| Field | What it is |
|---|---|
| `number` | the card number ND gave the board, such as `1050` |
| `partNumber` | the part number printed on the board, such as `322049` |
| `title` | the board's name, such as `Synchronous Modem Buffer` |
| `ndSystem` | `NORD-10` or `ND-100`: the machine the card plugs into (the catalogue writes `10` and `100`; an import turns them into these names and leaves other machines empty) |
| `group` | the catalogue's group code, such as `1C` (what the codes mean is not written there) |
| `description` | what the board is, in words |
| `printVersion`, `ecoVersion` | the print (PCB) version (`I`) and the engineering change version (`3`) |
| `printVersions` | every print version known: `[{ "print": "I", "eco": "3", "note": "" }]` |
| `specifications` | `{ "size": { "widthMm": 156, "heightMm": 149 }, "layers": 2 }` (the catalogue writes the size as `156 x 149 mm`) |
| `releaseDate` | as the catalogue writes it, such as `01. Jan. 1981` |
| `leds`, `straps`, `switches`, `connectors` | the catalogue's lists as they are: `{ id, color, text }` (colour for LEDs only). The sheet's own `switches` and `connectors` lists (below) hold the worked-out facts; these are the catalogue's words |
| `chipset` | the special chips the catalogue names: `{ name, description, url }` (the URL is the catalogue's, not checked) |
| `sourceUrl` | the web page the catalogue entry came from |
| `factSources` | for each field filled from somewhere: `{ "title": { kind, document, place, confidence } }`, the same source form as a signal description's. A fact changed by hand in the Board window loses its entry |
| `note` | where the values were read |

**Importing from the ND PCB catalogue.** File > "Import board info from the ND PCB catalogue..."
(edit mode), the MCP tool `import_board_from_catalogue`, or `retroschematic import-board <sheet>
<pcb.json> [number]`. The board is found by part number first (`322049` or `ND-322049`), then by
card number (several boards can share one; then the part number is needed). One undo step:

- the board section is replaced by the catalogue's values, each with a `factSources` entry
  (`kind` `catalogue`, `document` the catalogue, `place` the board's `sourceUrl`, `confidence`
  `read`); the old `note` is kept;
- every component- and solder-side photo of the board is copied into the sheet, named
  `PART-component-side-VERSION-VARIANT.jpg` (a .rsc: on disk in the folder its photos are in
  already, else `SHEETNAME-files/photos/`; a .rsx or an unsaved sheet: held inside). A photo
  the sheet has already (the same bytes) keeps its file. Each gets `version`, `variant`,
  `caption` and `source`;
- the catalogue's switch text goes on the switch block it names as one of its `moreSources`,
  word for word in `quote`. The catalogue writes a position row letter first (`E1`); our
  drawings write the column first (`1E`), so `E1` there is `1E` here, and the note says so.

Nothing of the catalogue other than the one board is copied into the repository.

**More fields on `connectors[]`** - each connector of the card, documented:

| Field | What it is |
|---|---|
| `use` | what it carries on this card, in words; `not used` is allowed |
| `used` | `false` when the card has the plug position but uses no pin of it |
| `standard` | such as `DIN 41612` |
| `pins` | the pin numbers in words: `0-99` for the one NORD-10 card-edge connector |
| `rows`, `pinsPerRow` | for a plug with rows: `["a", "b", "c"]` and `32` (DIN 41612) |

A NORD-10 card has one backplane connector (`P`, pins 0-99, its letter not known; catalogue
`nord10-io-card-backplane`). Its pins 6-55 are the bus; 56-99 are the cable side, the card's own
pins reached by a cable plugged onto the backplane (inferred), so the 2A50 modem lines are
`P.81` .. `P.95` too (owner's decision 2026-11-23). `F` is for a real front connector. An ND-100 card has the plugs A, B and C, each its own
entry (`P.A`, `P.B`, `P.C`): C is the ND-100 bus (`nd100-c-connector`), A and B are card
specific (the ND-120 CPU's are `nd120-cpu-a-plug` and `nd120-cpu-b-plug`), and either may be
`"used": false`.

A pin on a plug with rows is written with the row letter (lower case) in front of the pin
number: `P.C.a17` = plug C, row a, pin 17. The drawing shows it as `C.a17`; ND's computer-drawn
sheets print the same pin as `C A17` (card 3207, sheet 2). Because the row sits inside the last
part, a pin reference still splits at its last dot into connector (`P.C`) and pin (`a17`). A row
letter is allowed only on a connector pin; a chip pin is always a plain number. Bus signals are
looked up by connector, row and pin in the bus catalogue (2026-11-27).

Plug C is the same in every ND-100 slot and is described once (`nd100-c-connector`, bus
signals in `nd-bus-nd100`). Plugs A and B are wired per slot and differ from card to card: each
card layout gets its own catalogue file for each (`nd120-cpu-a-plug`, `nd120-cpu-b-plug`, and
for card 3207 the files made when its sheet 1 is read). Cards that share a layout, such as
editions of one controller, point to the same file.

**`nets[].printed`** (optional, 2026-11-27): the label exactly as printed on the drawing when
it is written differently from `name` plus `active`. Card 3207 prints `BD~00` (tilde = active
low): stored as `"name": "BD00", "active": "low", "printed": "BD~00"`. How the name is shown
on screen is still chosen by `display.nameStyle`.

**`sources[]`** - the original schematic pages, one or many:

| Field | What it is |
|---|---|
| `file` | a picture (PNG, JPEG) or a PDF, relative to the sheet file, with `/` |
| `pdfPage` | for a PDF: the page, 1 = first |
| `title`, `note` | shown in the page picker and the Board window |
| `original` | `{ file, pdfPage, dpi }`: the PDF and page a stored picture was rendered from |

**`photos[]`** - photos of the board: `file` (relative to the sheet), `side` (`component` or
`solder`), `version` (print version), `variant` (which board when there are several, such as
`old` or `new`: the PCB views have a picker for it), `caption`, `source` (where the photo came
from) and `note`.
`corners` (four `{ x, y }`, the photo's pixels): where the board's top left, top right, bottom
right and bottom left corners are, as the chip grid sees the board (finger edge at the top). It
is set by hand; without it the viewer looks for the board against the light background and,
when the shape it finds is not the board's, uses the whole picture.

**`pcb`** - the chip grid on the board photos:

| Field | What it is |
|---|---|
| `template` | the board type: `catalog/boards/TEMPLATE.json` gives the board size and where the numbered columns lie (`nord10-card`: 156 x 149 mm, column n centred at 9.99 + (n-1) x 7.62 mm). Without it the board's `ndSystem` picks one. |
| `rows` | this board's rows: `[{ name, fromMm, toMm }]`, mm from the finger edge. The rows differ between boards of one type, so each sheet has its own. |
| `rowsSource` | where the rows came from, as any source |
| `boxes` | chip boxes set by hand: `[{ ref, xMm, yMm, widthMm, heightMm, rotationDeg }]`, the centre in mm on the component side. A box wins over the grid cell. |
| `fingers` | this sheet's correction of where the gold fingers are on its photos: `{ firstCentreMm, pitchMm }` (either may be left out). Set by dragging a finger with the Fingers overlay on in edit mode. Without it the board type's `edgeFingers` numbers are used. |
| `note` | how it was worked out |

The board type file can also give **`edgeFingers`**: how many gold fingers each side has, their
pitch, finger 0's centre, and which pin each finger is per side (`componentFirstPin`,
`solderFirstPin`, `pinStep`), with the connector file whose `pinMap` says what each pin carries.
For `nord10-card`: 50 per side, 2.54 mm apart, finger k from the left of the component side is
pin 2k and the one behind it on the solder side pin 2k+1 (see
[NORD10-EDGE-FINGERS.md](NORD10-EDGE-FINGERS.md)).

A chip's area is the whole cell or cells its position code names: `13D` is column 13, row D;
`7-8C-D` spans columns 7 to 8 and rows C to D. The solder side is the same grid mirrored left to
right. The board type files say how they were measured, on which boards, and how sure that is;
`nd100-card` has no columns yet (no ND-100 sheet has been read, so how its codes name the
places is open).

```json
"pcb": { "template": "nord10-card",
  "rows": [ { "name": "A", "fromMm": 12, "toMm": 41.5 }, { "name": "B", "fromMm": 41.5, "toMm": 65.5 } ],
  "boxes": [ { "ref": "13D", "xMm": 101.4, "yMm": 106, "widthMm": 7.6, "heightMm": 20 } ] }
```

**Where the files are.** In a `.rsc` the `file` fields are links, relative to the sheet file;
the files stay where they are. In a `.rsx` the files are inside the zip, as `sources/NAME` and
`photos/NAME`, and the `file` fields name those entries, so the one file carries everything.

- Saving as `.rsx` packs every linked file into the zip. A PDF source does not go in whole:
  only its one referenced page is stored, rendered as PNG at **300 dpi** (`sources/NAME-pN.png`),
  and the entry keeps the PDF's name and page in `original` (`pdfPage` is then removed, since
  the stored file is a picture). Pictures are stored without compressing them again.
- Opening a `.rsx` reads the files from the zip; nothing is unpacked to disk.
- Saving as `.rsc` writes links relative to the new sheet's folder. A file the sheet only holds
  inside (it came from a `.rsx`, or it was imported into a sheet not saved yet) is written beside
  the sheet in `SHEETNAME-files/sources/` or `SHEETNAME-files/photos/`.
- A linked file that cannot be found stops the save with its name; nothing is written half.

**`switches[]`** - the switch blocks on the card:

| Field | What it is |
|---|---|
| `ref` | the board position, as for a chip (`15E`) |
| `function` | what the block sets, in words |
| `leverCount` | the number of levers |
| `onMeans`, `onMeansConfidence` | the bit a lever that is on (closed) gives: `0`, `1` or `open`, and how sure that is |
| `typical` | the usual setting, in words, with where it comes from |
| `levers[]` | per lever: `number` (1 first), `signal` (as on the drawing), `bit` (what it sets: `BA9`, `ident bit 2`), `confidence`, `setting` (`on`, `off` or `open` = cannot be read) and `note` |
| `source` | `{ kind, document, place, confidence }`, as a signal description's source |
| `moreSources` | further sources in the same form, such as the ND PCB catalogue's switch text; `quote` holds the source's own words and `note` how they map to the sheet |
| `note` | how it was worked out |

A source (`source`, `moreSources`, `factSources`, a signal's `descriptionSource`) has `kind`
`drawing`, `manual`, `reading`, `inferred` or `catalogue` (the ND PCB catalogue), and may carry
`quote` (the words as they are) and `note`.

```json
"switches": [ { "ref": "15E", "function": "device address", "leverCount": 4,
  "onMeans": "1", "onMeansConfidence": "inferred",
  "levers": [ { "number": 1, "signal": "SMN3", "bit": "BA9", "confidence": "read", "setting": "open" } ] } ]
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
| `blocks[].ports[].directionConfidence`, `.note` | optional (2026-11-27): how sure the direction is (`read`, `inferred`, `open`) and why. A block read from a drawing whose boxes do not state each pin's direction gets `inferred` with the reason (card 3207: the shape of the plug port the line runs to) or `open` |
| `blocks[].note` | optional (2026-11-27): free text about the block |
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

# Format version 3: pages

Version 3 makes one file hold a whole card with all its schematic pages (the SCSI card has 24,
the ND-120 CPU 50). The schema is `schema/nd-netlist-3.schema.json`. A file is written as version
3 exactly when it has a `pages` list; otherwise it stays version 1 or 2 as before, and versions 1
and 2 are still read. Blocks (version 2) are allowed in a version 3 file too.

## What belongs to a page and what to the card

| Belongs to | What |
|---|---|
| a page | every top-level part (chips and passive parts) names its page in `page`; a connector box (`connectors[].page`), an instance box (`instances[].page`), a stored wire path (`layout.wires[].page`) and a bus (`layout.buses[].page`) may name one, absent meaning the first page |
| the card | the signals (`nets`), the board, the PCB grid, the photos, the switches, the source pages, the display settings and the block definitions |

A signal is card-wide: one `nets` entry with every pin it joins, on whatever pages those pins
are. The reader's checks (every pin in one net at most, unique part references, the switch
blocks) are made over the whole card, not per page.

## Fields

| Field | What it is |
|---|---|
| `pages[]` | the card's pages, in order; at least one |
| `pages[].id` | unique in the file, such as `1`; parts name their page by it |
| `pages[].title` | the page's title |
| `pages[].source` | the original page it was read from: its place in `sources`, 1 = first |
| `pages[].sheetNumber` | the sheet number in the page's title block, as written |
| `pages[].note` | free text |
| `parts[].page` | the page the part is on; required on every top-level part. A part inside a block may name one of that block's own `pages` (the sheet it is drawn on, 2026-11-28), never a card page |
| `blocks[].pages[]` | optional (2026-11-27): the original pages that draw a block's inside, with the same fields as `pages[]`; ids are unique within the block. A computer-drawn ND card draws each box in full on its own sheets (card 3207: FLOPPY on sheet 5, its F_ND_INT on sheets 6-9). Each part inside the block may name the one it is on in its `page` |
| `pageLayout` | how the redrawn drawing is cut into pages: `as-original` (the default, also when absent) or `split`; drawing only |

```json
{ "format": "nd-netlist", "formatVersion": 3,
  "pages": [ { "id": "1", "title": "SYNCHRONOUS MODEM BUFFER 1050", "source": 1, "sheetNumber": "2A50" } ],
  "parts": [ { "ref": "13D", "type": "7442", "confidence": "read", "page": "1" } ] }
```

## Checks a version 3 reader makes

Everything versions 1 and 2 check, plus:

- the `pages` list has at least one page, ids are not empty and are unique;
- every top-level part names a page, and that page is in the list;
- a connector, instance, wire path or bus that names a page names one in the list;
- a page's `source` is a place in the `sources` list;
- a part inside a block names no page, or one of that block's own `pages` (never a card page:
  the block's instance has that);
- a block's own `pages` have unique ids within the block, and each `source` is a place in the
  `sources` list; a version 1 or 2 file has no block pages;
- a version 1 or 2 file has no `pages`, no `pageLayout` and no `page` on a part or connector;
- the symbol boxes pass their checks (see Symbol boxes below).

## Symbol boxes

`symbolBoxes[]` (version 3 only, card-wide) says where a read symbol is drawn on an original
page: a chip, one gate of a chip, a passive part, a circled backplane pin, a net-label diamond,
a wire dot or a label. A box never changes what is connected. The boxes tie the reading to the
drawing (the Original view draws them, and clicking one selects its part), and they are the
training data for the symbol finder (`models/README.md`).

| Field | What it is |
|---|---|
| `source` | the original page: its place in `sources`, 1 = first |
| `x`, `y`, `width`, `height` | the box in pixels of the source picture; for a PDF source, of the page rendered at `dpi` |
| `dpi` | for a PDF source only: the dots per inch the pixels are counted at |
| `kind` | `chip`, `gate`, `resistor`, `capacitor`, `switch`, `strap`, `ground`, `supply`, `circle`, `offpage`, `dot` or `label` |
| `ref` | the part it is (`13D`, `R5`); not for a circle, diamond, dot or label |
| `gate` | for a gate: which gate of the part, 1 = the part's first |
| `gateKind` | the gate kind the box shows (`nand`, `nor`, ...), for training |
| `text` | a label's text, or the pin a circle names (`P.81`) |
| `by` | `hand` (a person drew it) or `tool` (the page analyser found it and a person or an agent accepted it) |
| `score` | for a box by the tool: how sure the finder was, 0..1 |
| `note` | free text |

```json
"symbolBoxes": [ { "source": 1, "x": 2219, "y": 1081, "width": 92, "height": 418,
  "kind": "chip", "ref": "15B", "by": "tool", "score": 0.909 } ]
```

A reader checks that each box names a source page the card has, has a width and height above
0, holds a kind from the list, names a part that is on the card (when it names one), names a
gate only together with a part, and was drawn by `hand` or `tool`. A version 1 or 2 file has no
`symbolBoxes`; adding the first box to such a card converts it to version 3 (one page) in the
same step.

## Converting an older file

`retroschematic to-pages <sheet> [out]` (the window: File > "Convert to a card with pages"; MCP:
`add_page` on a card without pages converts it first) turns a version 1 or 2 file into version 3
with one page: the page takes the sheet's title and number, is read from the first source page,
and holds every part and connector box. Nothing that is connected changes, and the file reads
back exactly as written. Sheet 2A50 is stored this way (`.rsc` and `.rsx`).

## Drawn pages

The file holds the card's own pages; what the window draws is worked out from them, by the
card's `pageLayout`:

- **as the original** (`as-original`, default): one drawn page per card page, with the parts
  read from it. Drawn page ids are the card page ids.
- **split automatically** (`split`): a card page with more than 16 chips (passive parts not
  counted; the number is a choice) is drawn as several pages, `1.1`, `1.2`, ... The steps: every
  chip is a unit; a passive part joins the chip it serves, else the first chip it shares a
  signal with; chips of one type that share two or more signals (a register group: common
  clock and clear, common selects) are one unit; the number of pages is the chip count divided
  by 16, rounded up, and a page may take a quarter more than an even share; the two units that
  share the most signals are joined over and over while the result fits; then the smallest
  groups are joined until there are no more groups than pages. Supply lines do not count as
  shared signals. A connector box goes on the drawn page that shares the most signals with it.
  The file never changes.

**Off-page markers.** On a drawn page only that page's parts, connector boxes and instances are
drawn. A signal with a pin on another page is never wired on this page; each of its pins here
gets the usual net-label diamond and name, followed by a small box naming the other drawn pages
it is on (`p. 2, 3`), the way a hand-drawn schematic names the sheet a line goes on to. Supply
lines get no marker. Clicking the box goes to the next of those pages and picks and traces the
signal there.
