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

## Functions

One entry per output pin (or per group of identical outputs). `formula` uses `AND`, `OR`, `XOR`,
`NOT`, and `X'` for NOT X, with the datasheet's pin names. `text` says the same in plain words. A
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
`inputSchmitt`, and `notes` for anything else that matters (for example "buffer: IOL 48 mA, three
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

`from` and `to` are pin names (or `any`/`Y` for all inputs/outputs of identical gates). Setup,
hold and minimum pulse width go in `"timingLimits": [{"what": "setup", "pin": "D", "ref": "CLK",
"minNs": 20}]`. A missing figure is `null`, never guessed; a missing minimum means the datasheet
gives none - the path-delay function then uses 0 ns for that minimum and says so.

## Unknown

Anything the datasheet that was read does not settle, in words. A field that is not known is
`null` and listed here; it is never guessed.
