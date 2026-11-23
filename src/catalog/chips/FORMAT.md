# Chip files - format version 1

One file per chip type, `catalog/chips/<id>.json`, where `<id>` is the same id as the symbol in
`catalog/symbols/nd1976/`. The symbol file says how ND **draws** the part; the chip file says what
the part **is and does**: every pin with its role, the logic formula of every output, stored state,
and the electrical figures. Every fact comes from a datasheet that was actually read, named in
`sources`. Nothing is filled in from memory.

```json
{
  "catalogFormat": "nd-chip",
  "catalogFormatVersion": 1,
  "id": "74153",
  "family": "74",
  "title": "Dual 4-line to 1-line data selector/multiplexer",
  "package": "DIP-16",
  "sources": [
    {"document": "TI SN54153/SN74153 datasheet SDLS055A", "url": "https://www.ti.com/lit/ds/symlink/sn74153.pdf",
     "pages": "1-3", "familyUsed": "SN74153"}
  ],
  "pins": [
    {"pin": 16, "name": "VCC", "role": "power", "voltage": "+5 V"},
    {"pin": 8,  "name": "GND", "role": "ground"},
    {"pin": 2,  "name": "A",   "role": "input"},
    {"pin": 1,  "name": "1G",  "role": "enable", "active": "low", "controls": ["7"],
     "whenInactive": "low"},
    {"pin": 7,  "name": "1Y",  "role": "output", "outputType": "totem-pole"}
  ],
  "functions": [
    {"output": "7", "formula": "1Y = 1G' AND ((B'A' AND 1C0) OR (B'A AND 1C1) OR (BA' AND 1C2) OR (BA AND 1C3))",
     "text": "When 1G is low, 1Y follows the 1C input picked by B,A; when 1G is high, 1Y is low."}
  ],
  "state": null,
  "electrical": {
    "outputLowMaxCurrentMa": 16, "outputHighMaxCurrentMa": -0.8, "outputMaxVoltageV": null,
    "inputSchmitt": false, "notes": ""
  },
  "unknown": [],
  "notes": ""
}
```

## Pins

`role` is one of:

| role | meaning | extra fields |
|---|---|---|
| `power` | supply pin | `voltage` |
| `ground` | ground | |
| `input` | ordinary logic input | `schmitt: true` if it is a Schmitt trigger input |
| `clock` | input whose EDGE makes something happen | `edge`: `rising` or `falling` |
| `enable` | input that switches outputs on or off | `active`: `high`/`low`; `controls`: the output pin numbers; `whenInactive`: what those outputs do when it is not active: `float` (three-state, Z), `low`, `high`, or `hold` |
| `output` | logic output | `outputType`: `totem-pole`, `open-collector`, `open-collector-high-voltage`, `three-state`, `open-emitter` |
| `analog` | not a logic pin (timing resistor/capacitor, reference) | `text` |
| `nc` | not connected | |

A clear, preset, load or reset input that acts at once (not on a clock edge) is `input` with
`"async": true` and `active`. An output that is three-state names its enable pin in `enabledBy`.

**A name the datasheet draws with a bar** (Q-bar, CLR-bar, G0-bar) is stored as its base name
and `"inverted": true`: `{"pin": 6, "name": "1Q", "inverted": true, "role": "output"}`. The
name itself never holds a ' or a /. Everywhere else in the file - formulas, `behaviour` pin
fields, `timing` `from`/`to` - that pin is written `/NAME` (`/1Q = NOT FF1`), so the 7474's
1Q and Q-bar stay two different names. A ' after a name in a formula still means NOT. The
viewer shows an inverted pin in the chosen name style: `/1Q` (the default), `~1Q`, or `1Q` with
a small 0. The change from the old `1Q'` names was made by script on 2026-11-23
(`docs/pin-rename-2026-11-23.md` lists every change); `ChipPinNameTests` checks that every
formula still gives the truth table it gave before.

## Functions

One entry per output pin (or per group of identical outputs). `formula` uses `AND`, `OR`, `XOR`,
`NOT`, and `X'` for NOT X, with the datasheet's pin names (`/NAME` for a pin drawn with a bar, see Pins). `text` says the same in plain words. A
part with stored state (flip-flop, counter, register, memory, one-shot) says in `formula` what the
output is FROM the state, and describes in `state` how the state changes:

```json
"state": {"bits": 4, "changesOn": "rising edge of CLK",
          "rules": ["CLR low: Q = 0 at once", "LOAD low: Q = D,C,B,A at the clock edge", "..."]}
```

Analog or mixed parts (one-shots, line receivers, opto-couplers, regulators, memories) describe
their behaviour in `text` and `state` as far as the datasheet states it.

## Electrical

From the datasheet's recommended operating conditions, for the 74 (commercial) part:
`outputLowMaxCurrentMa` (IOL), `outputHighMaxCurrentMa` (IOH, negative = out of the pin),
`outputMaxVoltageV` (only for open-collector outputs: the highest voltage the pull-up may go to),
`inputSchmitt`, for Schmitt inputs `vtPlusV` and `vtMinusV` (the input voltages where the
input switches going up and going down) and `hysteresisV` (the difference), and `notes` for anything else that matters (for example "buffer: IOL 48 mA, three
times a standard gate").

## Timing

Propagation delay: the time from an input change to the output's response, measured between the
50% points of the two edges. Datasheets give it per direction of the OUTPUT edge:
`tPLH` (output goes low to high) and `tPHL` (output goes high to low), each with min, typ and max
(any may be missing). For flip-flops, registers and counters the clock-to-output delay is the same
thing measured from the clock edge. One entry per path the datasheet lists:

```json
"timing": [
  {"from": "A", "to": "Y", "edge": "PLH", "minNs": null, "typNs": 11, "maxNs": 22,
   "conditions": "VCC 5 V, CL 15 pF, RL 400 ohm, TA 25 C"},
  {"from": "CLK", "to": "Q", "edge": "PHL", "minNs": null, "typNs": 25, "maxNs": 40, "conditions": "..."}
]
```

Three-state outputs also have enable and disable times, with `edge` `PZH` (from floating to
high), `PZL` (floating to low), `PHZ` (high to floating) and `PLZ` (low to floating); `from` is
the enable pin.

`edge` is one of a fixed list. Besides the six above:

| edge | meaning |
|---|---|
| `PZX` / `PXZ` | output enable / disable time where the datasheet does not split it by level |
| `access` | memory: from the address or chip-select pin (`from`) until the data output is valid |
| `recovery` | memory: time the output needs after a write before a read is valid (sense recovery) |
| `rise` / `fall` | how long the output edge itself takes (opto-couplers, line drivers) |
| `turnOn` / `turnOff` | opto-coupler: from the input current change until the output reaches the stated level |
| `response` | line receiver: from the line input changing until the output responds |

`symbol` (optional) keeps the datasheet's own name for the figure (`tRAC`, `tACC`, `ta(CE)`).

`from` and `to` are pin names (or `any`/`Y` for all inputs/outputs of identical gates). Setup,
hold and minimum pulse width go in `"timingLimits": [{"what": "setup", "pin": "D", "ref": "CLK",
"minNs": 20}]`. The highest clock rate is `{"what": "clock frequency", "pin": "CLK", "minMHz": 15,
"typMHz": 25}` (the datasheet's "fmax": the guaranteed minimum of the maximum rate, and typical). A missing figure is `null`, never guessed; a missing minimum means the datasheet
gives none - the path-delay function then uses 0 ns for that minimum and says so.

## Unknown

Anything the datasheet that was read does not settle, in words. A field that is not known is
`null` and listed here; it is never guessed.

## Behaviour

`functions` and `state` say what the chip does in the datasheet's words. `behaviour` says the
same as data a simulator can run, and the test `ChipBehaviourTests` checks it for every chip
file: every formula is read by the formula reader (`RetroSchematic.Model/ChipFormula.cs`),
every pin it names exists, every output pin has an entry, every fact has a `source`.

```json
"behaviour": {
  "kind": "logic",
  "source": "6-46 - TI The TTL Data Book, 2nd ed. 1976",
  "stored": [
    {"name": "FF1", "kind": "flip-flop", "next": "FF1 = 1D",
     "clock": {"pin": "1CK", "edge": "rising"},
     "set": {"pin": "1PR", "active": "low"}, "clear": {"pin": "1CLR", "active": "low"},
     "whenSetAndClear": "neither wins: Q and Q' are both high while both act",
     "source": "6-46 function table"}
  ],
  "outputs": [
    {"pin": "1Q", "formula": "1Q = FF1", "type": "totem-pole", "source": "..."},
    {"pin": "Y1", "formula": "Y1 = A1", "type": "three-state",
     "outputEnable": {"pins": [{"pin": "G1", "active": "low"}], "disabled": "Z"}, "source": "..."}
  ]
}
```

| field | meaning |
|---|---|
| `kind` | `logic`, or `none` with a `reason` when there is no datasheet or no logic function (an optocoupler). A chip whose datasheet does describe a behaviour never gets `none`. |
| `outputs[]` | one per output pin: `formula` (its value while it drives), `type` (`totem-pole`, `open-collector`, `open-collector-high-voltage`, `three-state`, `open-emitter`), `outputEnable` |
| `outputEnable` | `pins`: every pin that must be at its `active` level (`high`/`low`) for the output to drive; `when` (optional): a formula expression that must also be 1, for an enable made from stored bits (a RAM drives only while `CE AND NOT LCS`, LCS being chip select latched at the rise of CE); at least one of the two; `disabled`: `Z` (floats), `0` or `1` while it does not |
| `stored[]` | one per stored bit. `kind`: `flip-flop`, `latch`, `counter`, `one-shot`. `next`: the value it takes, as a formula with the bit's name as target. `clock`: `pin` or `when` (an expression, for a clock made of several pins or a ripple stage) and `edge`: `rising` (L->H), `falling` (H->L), `level-high`, `level-low` (a latch follows while the level holds). `set`/`clear`: a pin that forces 1/0 at once, with `whenSetAndClear` saying which wins. `async`: a list of `{when, value}` rules for an at-once load that needs more than one pin; the first that acts wins. A one-shot has `pulseWidth` (datasheet words) and `retriggerable` instead of `next`. |
| `internal[]` | named steps (`formula` with the step's name as target), such as an adder's carries |
| `conditions[]` | logic values made from analog levels (`name`, `meaning` with the datasheet's thresholds), such as a line receiver's input threshold |
| `memory` | `name`, `words`, `width`, `address` (least significant first; pins, or stored bits when the chip latches its address: the 2107B takes A0..A11 into LA0..LA11 at the rise of CE, the MK4096 takes A0..A5 into ROW0..ROW5 at the fall of RAS and into COL0..COL5 at the fall of CAS, and `address` lists those bits), `writeAddress` when it differs, `dataIn`, `write` (`when` + `edge`); formulas read the addressed word's bit n as name + n (`M1`) |

Names of stored bits, internal steps, conditions and memory bits may be used in formulas like
pin names, so they must not be the same as a pin name.
