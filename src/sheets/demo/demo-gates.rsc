{
  "format": "nd-netlist",
  "formatVersion": 1,
  "file": {"revision": 1, "modified": "2026-11-28T12:32:56Z"},
  "sheet": {
    "id": "DEMO",
    "title": "Demo gates",
    "machine": "none",
    "source": {
      "document": "none - made up for the viewer"
    },
    "note": "A MADE-UP DEMO, not a reading of a real drawing. It exists so the viewer and its tests have a small sheet with every kind of thing in it: four TTL chip types, gates with measured positions, pins that are read, inferred and open, and one open-collector line with a pull-up."
  },
  "parts": [
    { "ref": "1A", "type": "7400", "function": "Two NAND gates on the inputs", "confidence": "read" },
    { "ref": "2A", "type": "7402", "function": "NOR gate making the clock", "confidence": "read" },
    { "ref": "3A", "type": "7404", "function": "Inverter in front of the flip-flop", "confidence": "read" },
    { "ref": "4A", "type": "7474", "function": "Flip-flop holding the result", "confidence": "inferred", "note": "Demo: shows a part whose type is not proved." },
    { "ref": "5A", "type": "7403", "function": "Open-collector NAND gates driving the request line", "confidence": "read" }
  ],
  "gates": [
    { "id": "1A/1", "part": "1A", "kind": "nand", "inputs": [1, 2], "output": 3, "position": { "x": 20, "y": 20 }, "confidence": "read" },
    { "id": "1A/2", "part": "1A", "kind": "nand", "inputs": [4, 5], "output": 6, "position": { "x": 20, "y": 40 }, "confidence": "read" },
    { "id": "1A/3", "part": "1A", "kind": "nand", "inputs": [9, 10], "output": 8, "position": { "x": 20, "y": 60 }, "confidence": "read", "note": "Demo: a spare gate, nothing connected, to wire up in edit mode." },
    { "id": "2A/1", "part": "2A", "kind": "nor", "inputs": [2, 3], "output": 1, "position": { "x": 70, "y": 45 }, "confidence": "read" },
    { "id": "3A/1", "part": "3A", "kind": "not", "inputs": [1], "output": 2, "position": { "x": 70, "y": 20 }, "confidence": "read" },
    { "id": "4A/1", "part": "4A", "kind": "other", "inputs": [2, 3], "output": 5, "position": { "x": 120, "y": 30 }, "confidence": "inferred", "note": "D on pin 2, clock on pin 3, Q on pin 5." },
    { "id": "5A/1", "part": "5A", "kind": "nand", "inputs": [1, 2], "output": 3, "position": { "x": 120, "y": 75 }, "confidence": "read" },
    { "id": "5A/2", "part": "5A", "kind": "nand", "inputs": [4, 5], "output": 6, "position": { "x": 70, "y": 75 }, "confidence": "read" }
  ],
  "nets": [
    {
      "name": "AIN", "active": "high",
      "pins": [
        { "at": "P.10", "confidence": "read" },
        { "at": "1A.1", "confidence": "read" },
        { "at": "1A.4", "confidence": "read" }
      ]
    },
    {
      "name": "BIN", "active": "high",
      "pins": [
        { "at": "P.11", "confidence": "read" },
        { "at": "1A.2", "confidence": "read" },
        { "at": "1A.5", "confidence": "read" }
      ]
    },
    {
      "name": "NAB", "active": "low",
      "pins": [
        { "at": "1A.3", "confidence": "read" },
        { "at": "3A.1", "confidence": "read" },
        { "at": "2A.2", "confidence": "inferred", "note": "Demo: the line is broken on the scan here." }
      ]
    },
    {
      "name": "NAB2", "active": "low",
      "pins": [
        { "at": "1A.6", "confidence": "read" },
        { "at": "2A.3", "confidence": "read" }
      ]
    },
    {
      "name": "DATA", "active": "high",
      "pins": [
        { "at": "3A.2", "confidence": "read" },
        { "at": "4A.2", "confidence": "read" }
      ]
    },
    {
      "name": "CLK", "active": "high",
      "pins": [
        { "at": "2A.1", "confidence": "read" },
        { "at": "4A.3", "confidence": "inferred" },
        { "at": "5A.4", "confidence": "open", "note": "Demo: where this pin goes is not known." }
      ]
    },
    {
      "name": "Q", "active": "high",
      "pins": [
        { "at": "4A.5", "confidence": "read" },
        { "at": "5A.1", "confidence": "read" },
        { "at": "F.3", "confidence": "read" }
      ]
    },
    {
      "name": "ENA", "active": "high",
      "pins": [
        { "at": "P.12", "confidence": "read" },
        { "at": "5A.2", "confidence": "read" },
        { "at": "5A.5", "confidence": "read" }
      ]
    },
    {
      "name": "IRQ", "active": "low",
      "drive": "open-collector",
      "driveConfidence": "read",
      "pullUp": "1k to +5V",
      "pins": [
        { "at": "5A.3", "confidence": "read" },
        { "at": "5A.6", "confidence": "read" },
        { "at": "P.20", "confidence": "read" }
      ]
    }
  ]
}
