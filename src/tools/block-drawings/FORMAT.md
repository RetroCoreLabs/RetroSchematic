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
