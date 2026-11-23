# Block part drawings - transcription format

One file per part, `<id>.json`, read off the manual cut-out `catalog/symbols/nd1976/<id>.png`
(ND-13.003.01, July 1976). It records the drawing **exactly as printed**: pin numbers, labels and
layout. Nothing comes from a datasheet; a datasheet may disagree, and that is kept as a note.

```json
{
  "id": "74153",
  "sections": [
    {
      "insideLabel": "",
      "left":   [ {"number": "6", "outside": "", "inside": "", "gapBefore": false} ],
      "right":  [ {"number": "7", "outside": "1Y", "inside": ""} ],
      "top":    [ {"number": "2", "outside": "(B)"}, {"number": "14", "outside": "(A)"} ],
      "bottom": [ {"number": "1", "outside": "1Y"}, {"number": "15", "outside": "2Y"} ]
    }
  ],
  "groupLabels": [
    {"text": "Select input", "pins": ["2", "14"]},
    {"text": "Enable", "pins": ["1", "15"]}
  ],
  "unreadable": [],
  "notes": ""
}
```

- `sections`: one entry per box. A part drawn as two boxes (7474) has two sections, top box first.
- `left` and `right` list pins top to bottom; `top` and `bottom` list them left to right.
- `number`: the pin number printed at the line, as text. `null` if the drawing prints none.
- `outside`: text printed beside the line outside the box (a signal name such as `1Y`, `QA`,
  `Clear`). Subscripts are written plainly: Q with subscript A is `QA`. An overbar is written as a
  leading `~` (`~Q`).
- `inside`: text printed inside the box next to that pin (`D`, `0`, `1`, `CK`).
- `gapBefore`: `true` where the drawing leaves a clearly larger space before this pin than between
  the others (a visual group break). Omit or `false` otherwise.
- `bubble`: `true` if the line meets the box with a small circle (negation). Omit otherwise.
- `insideLabel`: any text written in the middle of the box.
- `groupLabels`: text that names several pins together (a brace with "Input", "Select input"),
  with the pin numbers it covers. If the pins have no numbers, use `"side:index"` like `"left:0"`.
- `unreadable`: anything that cannot be read with certainty, described in words. Never guess a
  number; put it here instead.
- `notes`: anything odd about the drawing (for example two boxes of the same part numbered
  differently).

## Shapes and marks (added so every drawing can match its cut-out)

A section may also carry:

- `outline`: `box` (default), `dashed`, or `none` (no box: the marks are the whole drawing, as for
  an opto-coupler).
- `width`, `height`: the box size in drawing units (pins on a side are 10 apart), when the default
  does not give the cut-out's proportions.
- `marks`: everything drawn inside or around the box, as a list. Coordinates are FRACTIONS of the
  box: `0,0` is the top-left corner, `1,1` the bottom-right; values outside 0..1 reach outside it.
  - `{"type": "line", "x1": 0.5, "y1": 0, "x2": 0.5, "y2": 1}` (optional `"dashed": true`)
  - `{"type": "polygon", "points": [[0.2,0.2],[0.8,0.5],[0.2,0.8]]}` (optional `"filled": true`)
  - `{"type": "polyline", "points": [...]}` - an open line through the points
  - `{"type": "circle", "x": 0.5, "y": 0.5, "r": 0.05}` (r as a fraction of the box width)
  - `{"type": "text", "x": 0.5, "y": 0.5, "text": "+", "size": 4.5}`

The marks are read off the cut-out and checked by drawing the part and comparing it with the
cut-out at full size.

### Pin position and bent pin lines

A pin may also carry:

- `at`: where along its side the pin sits, as a fraction of the side (0 = top or left end,
  1 = bottom or right end). Without it the pins of a short side are spread evenly.
- `bend`: the line leaves the box and then turns, as the select and enable lines of 74153 do:
  `{"dir": "left", "length": 20, "stub": 12}`. `dir` is `left`, `right`, `up` or `down`;
  `length` is the turned part; `stub` (optional) is the straight part from the box before the
  turn (default 20). Two lines on one side need different `stub` values so their turned parts
  do not lie on top of each other. The `outside` name is written at the far end of the turned
  part.

Text printed beside the drawing (a truth table, a timing formula) is drawn with `text` marks.

### More pin and label fields

- `length` on a pin: the length of that one straight line (default 20), for lines the manual
  draws shorter.
- `second` on a pin: the other half's pin number printed in brackets beside this one, as text
  (`"second": "13"` prints `(13)`). Use this instead of a text mark so the number belongs to its pin.
- `endMark: "diamond"` on a pin: the line ends in a small open diamond (7483 carry out).
- `x`, `y` (and optional `anchor`: `start`, `middle`, `end`) on a group label: place it yourself,
  as fractions of the WHOLE drawing (not the box), when the automatic place is not where the
  manual prints it.

### Side-by-side drawings, braces, subscripts, printed notes

- `beside: true` on a section: draw it to the right of the previous one (8097/8098 "Alternative").
- Brace mark: `{"type": "brace", "x": -0.6, "y1": 0, "y2": 1, "dir": "right"}` (vertical; `dir`
  is the side it opens to) or `{"type": "brace", "y": 1.3, "x1": 0, "x2": 1, "dir": "up"}`.
- Subscript: write `_` before it in any printed text: `Q_A`, `Strobe_1`, `Y_A`.
- Notes printed under or beside a drawing in the manual ("Pins 7 and 9 connected to GND") are
  part of the cut-out and are drawn with `text` marks, not only kept in `notes`.
- Overbar: `~A` bars one name (`~Q`, `~Y_A`), `~{A·E}` bars a whole group, in any printed text.
- Text printed on several lines: put `\n` where the line breaks (`"Serial\ninput"`), in any
  printed text including group labels.
- The drawing area grows to hold every mark, so text placed far from the box (74107's truth
  table) is not cut off.
- Text printed on several lines: put `\n` where the line breaks (`"Serial\ninput"`), in any
  printed text including group labels.
- The drawing area grows to hold every mark, so text placed far from the box (74107's truth
  table) is not cut off.
- `anchor` on a text mark: `start` (left-aligned), `middle` (default) or `end`.
- `numberOnBend: true` on a bent pin: its number is printed on the turned part (8831).
- `numberAt` on a pin: where along the line its number stands, 0 = at the box, 1 = at the far end (default 0.5).
- `nameAnchor` on a top/bottom pin: `end` puts its name to the left of the line, `start` to the right (8599), instead of centred under it; such names are never moved down a row.
