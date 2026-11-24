# Recognition models

Three small neural networks that help a person read a scanned ND schematic page. They were
trained offline (Python, PyTorch) and exported to ONNX; the app runs them with ONNX Runtime on
the CPU (`RetroSchematic.Recognition`). Git keeps them in LFS.

**Everything they produce is a guess.** A find is marked `inferred` until a person (or an agent
checking against the drawing) accepts it. They find chips and gates well, read about half the
position codes, and do not read the tiny pin numbers at all, so wires are connected by hand
(the wire assist in the Original view, or the MCP tool `connect_pin_on_page`).

| File | What it finds | Used for |
|---|---|---|
| `det-L3.onnx` | symbols on a hand-drawn page | NORD-10 and ND-100 hand-drawn pages |
| `det-A.onnx` | symbols on a CAD-drawn page | ND-120, SCSI and other computer-drawn pages |
| `letters-A.onnx` | one handwritten or printed character | position codes, chip type text, turning the page upright |

Which detector a page gets is decided by the page-style rule (below), not by the user.

## The two detectors (`det-L3`, `det-A`)

Same network, different training. A small "centre point" detector (CenterNet type, about 1.2
million weights).

- **Input** `page`: float `[1, 1, H, W]`, grey, 1.0 = white paper, 0.0 = black ink, H and W
  multiples of 32 (pad with 1.0). The page must be at the scale the network learned: letters
  about 20 pixels high (a NORD-10 drawing at 300 dpi).
- **Outputs**, each at a quarter of the input size (`H/4 x W/4`):
  - `hm` `[1, 14, H/4, W/4]`: how sure (0..1) that the centre of a thing of each class is in
    this cell. Classes in order: chip, gate, resistor, capacitor, switch, strap, ground, supply,
    circle (circled backplane pin), offpage (the diamond of a net label), dot (a wire joint),
    poscode (a position code such as `13D`), pinnum, label.
  - `reg` `[1, 4, H/4, W/4]`: at a centre cell, log(box width / 4), log(box height / 4), and
    the centre's x and y offset inside the cell.
  - `kind` `[1, 8, H/4, W/4]`: at a gate centre, scores for the gate kind: nand, nor, not,
    and, or, aoi (and-or-invert), buffer, other.
- **Reading the output:** a cell is a find when its `hm` value is at least 0.3 and no cell in
  the 3 x 3 around it is higher. The box is centre `((x + offx) * 4, (y + offy) * 4)`, size
  `(exp(w) * 4, exp(h) * 4)`.

How they were trained: on generated pages (140 drawn by a program in both styles, 20 kept for
checking) plus pages read by hand and checked (the work keys: NORD-10 pages 61-66, Binder pages
8 and 96, I/O-system pages 228 and 237, ND-120 page 41). `det-A` was trained first on all of it;
`det-L3` is `det-A` trained further on the hand-drawn keys (three rounds, the keys repeated 6
and then 12 times, the last round with the position-code boxes taken from the keys). SCSI pages
6, 13 and 21 and NORD-10 page 64 (sheet 2A50) were never trained on.

Measured (prototype, `rounds.json` of the offline loop):

| | det-A | det-L3 |
|---|---|---|
| generated check pages, mean F1 over the classes | 0.888 | 0.895 |
| gates found on the check pages | 93 of 96 | 59 of 59 |
| chips found on the check pages | 51 of 61 (57 extra) | 36 of 39 (37 extra) |
| page 64 (never trained): pins in the right net / wrong net, whole draft | 191 / 65 (round 0) | 252 / 74 |
| page 64: wire connections (pairs of pins joined) found | | 5.9 % (28.8 % of the drawn ones right) |
| work pages: position codes read right | | 319 of 586 |

`det-L3` was tried on the CAD pages and made them worse (SCSI gates 15 -> 12 on page 13), so
CAD pages keep `det-A`.

## The letter network (`letters-A`)

- **Input** `x`: float `[N, 1, 32, 32]`, ink = 1.0, paper = 0.0: a 48 x 48 pixel window centred
  on the character, shrunk to 32 x 32 (area average), on a page at the detector's scale.
- **Output** `logits` `[N, 39]`: one score per class: `0`-`9`, `A`-`Z`, `sub0`, `sub1` (the small
  0 and 1 written after a signal name), `x` (not a character).

How it was trained: generated letter pages plus named piles of real characters from the NORD-10,
Binder, I/O-system and ND-120 books (one model for both styles). Measured: 95.9 % right on the
held-out characters; 118 of 148 (80 %) on the checked characters of page 64, which it never saw.

The position code reader does not take the network's first answer: it scores every code the
board allows (NORD-10: numbers 1-19, letters A-E; ND-100: numbers 1-29, letters A-H and J) from
the network's probabilities and keeps the best one, so a `4` the network half thinks is a `1`
can still be read as `1` when only that fits the board.

## The page-style rule

Not a network: two measures of the page and a fitted straight-line rule (logistic). The spread
of the letter heights (standard deviation / mean) and the median thickness of the long
horizontal lines. Weights: `z = -5.835 - 6.460 * (spread - 0.1346) / 0.0401 + 3.301 * (thickness
- 2.878) / 1.092`; a page is CAD when `z > 0`. Trained on the Binder, I/O-system (hand) and
ND-120 (CAD) pages; it called every NORD-10 page hand-drawn and every SCSI page CAD, which it
was not trained on.
