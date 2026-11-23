{
  "format": "nd-netlist",
  "formatVersion": 2,
  "sheet": {
    "id": "2A50",
    "title": "SYNCHRONOUS MODEM BUFFER 1050",
    "machine": "NORD-10",
    "bus": "nord-10",
    "company": "A/S Norsk Data-Elektronikk",
    "drawn": "1973-11-06",
    "updated": "1973-11-29",
    "source": {
      "document": "docs/sources/nord-10/NORD-10-drawings.pdf",
      "pdfPage": 64,
      "image": "docs/sources/nord-10/2A50-sync-modem-buffer-1050.png"
    },
    "note": "Parts, gates and wires read pin by pin from the scan on 2026-11-23. Switches, resistors and capacitors are not parts in this format; they are described in net notes. Pins of a 74153/74157 drawn as a stack of small boxes are read with the rule that a pin number sits above its wire. Register-level reading: docs/2A50-reading.md; catalogue map: docs/2A50-catalogue-map.md."
  },
  "parts": [
    {
      "ref": "7-8C-D",
      "type": "COM2601",
      "function": "USRT, synchronous receiver/transmitter",
      "confidence": "read",
      "note": "pin use matched against the SMC COM2601 datasheet"
    },
    {
      "ref": "11D",
      "type": "7402",
      "function": "sync register strobes ISCW, OSCW",
      "confidence": "read",
      "note": "type proved by pin numbers 2,3\u003E1 5,6\u003E4 8,9\u003E10 and the parts-list count"
    },
    {
      "ref": "15C",
      "type": "7474",
      "function": "CDSTL and PRQTS",
      "confidence": "read"
    },
    {
      "ref": "11B",
      "type": "74174",
      "function": "input control latch",
      "confidence": "read"
    },
    {
      "ref": "17D",
      "type": "74221",
      "function": "one-shot for the receiver reset, and the timeout oscillator",
      "confidence": "read"
    },
    {
      "ref": "19C",
      "type": "7497",
      "function": "timeout counter",
      "confidence": "read"
    },
    {
      "ref": "9B",
      "type": "7404",
      "function": "bus receivers (inverters) for BD2, BD8, BD9, BD13, BD15 and the TINPUT enable",
      "confidence": "read",
      "note": "box-with-triangle inverter shape; all six gates read: 1\u003E2 3\u003E4 5\u003E6 9\u003E8 11\u003E10 13\u003E12"
    },
    {
      "ref": "13B",
      "type": "7400",
      "function": "NAND gates: TRQTS (half duplex), OACTIVE path, test-mode clock selects",
      "confidence": "read"
    },
    {
      "ref": "17B",
      "type": "7400",
      "function": "NAND gates: clear (ICW and BD4, master clear MC), OACTIVE from RFS",
      "confidence": "read",
      "note": "gate 4 not found on the sheet"
    },
    {
      "ref": "17C",
      "type": "7400",
      "function": "NAND gates: CL, timeout clear, timeout counter clear, CDSTL clear",
      "confidence": "read"
    },
    {
      "ref": "7B",
      "type": "7404",
      "function": "bus receivers (inverters) for BD3-BD7 and the level 12 path",
      "confidence": "read"
    },
    {
      "ref": "11A",
      "type": "7408",
      "function": "AND gates: CARRERR, ISYNERR, SR (status read)",
      "confidence": "read",
      "note": "gate 3 not found on the sheet"
    },
    {
      "ref": "18-19E",
      "type": "VP12/12",
      "function": "voltage converter: \u002B5 V in, \u002B12 V and -12 V out (for the COM 2601 and the line drivers)",
      "confidence": "read",
      "note": "pins: 1,24 = \u002B5V; 2,11,12,13,14,23 = GND; 3,22 = \u002B12V; 10,15 = -12V"
    }
  ],
  "gates": [
    {
      "id": "11D/1",
      "part": "11D",
      "kind": "nor",
      "inputs": [
        2,
        3
      ],
      "output": 1,
      "confidence": "read"
    },
    {
      "id": "11D/2",
      "part": "11D",
      "kind": "nor",
      "inputs": [
        5,
        6
      ],
      "output": 4,
      "confidence": "read"
    },
    {
      "id": "11D/3",
      "part": "11D",
      "kind": "nor",
      "inputs": [
        8,
        9
      ],
      "output": 10,
      "confidence": "read"
    },
    {
      "id": "11D/4",
      "part": "11D",
      "kind": "nor",
      "inputs": [
        11,
        12
      ],
      "output": 13,
      "confidence": "read",
      "note": "ODW strobe to COM 2601 pin 20"
    },
    {
      "id": "13B/1",
      "part": "13B",
      "kind": "nand",
      "inputs": [
        2,
        1
      ],
      "output": 3,
      "confidence": "read"
    },
    {
      "id": "13B/2",
      "part": "13B",
      "kind": "nand",
      "inputs": [
        5,
        4
      ],
      "output": 6,
      "confidence": "read"
    },
    {
      "id": "13B/3",
      "part": "13B",
      "kind": "nand",
      "inputs": [
        9,
        10
      ],
      "output": 8,
      "confidence": "read"
    },
    {
      "id": "13B/4",
      "part": "13B",
      "kind": "nand",
      "inputs": [
        13,
        12
      ],
      "output": 11,
      "confidence": "read"
    },
    {
      "id": "17B/1",
      "part": "17B",
      "kind": "nand",
      "inputs": [
        1,
        2
      ],
      "output": 3,
      "confidence": "read"
    },
    {
      "id": "17B/2",
      "part": "17B",
      "kind": "nand",
      "inputs": [
        5,
        4
      ],
      "output": 6,
      "confidence": "read"
    },
    {
      "id": "17B/3",
      "part": "17B",
      "kind": "nand",
      "inputs": [
        10,
        9
      ],
      "output": 8,
      "confidence": "read"
    },
    {
      "id": "17C/1",
      "part": "17C",
      "kind": "nand",
      "inputs": [
        1,
        2
      ],
      "output": 3,
      "confidence": "read"
    },
    {
      "id": "17C/2",
      "part": "17C",
      "kind": "nand",
      "inputs": [
        4,
        5
      ],
      "output": 6,
      "confidence": "read"
    },
    {
      "id": "17C/3",
      "part": "17C",
      "kind": "nand",
      "inputs": [
        9,
        10
      ],
      "output": 8,
      "confidence": "read"
    },
    {
      "id": "17C/4",
      "part": "17C",
      "kind": "nand",
      "inputs": [
        12,
        13
      ],
      "output": 11,
      "confidence": "read"
    },
    {
      "id": "7B/1",
      "part": "7B",
      "kind": "not",
      "inputs": [
        1
      ],
      "output": 2,
      "confidence": "read"
    },
    {
      "id": "7B/2",
      "part": "7B",
      "kind": "not",
      "inputs": [
        3
      ],
      "output": 4,
      "confidence": "read"
    },
    {
      "id": "7B/3",
      "part": "7B",
      "kind": "not",
      "inputs": [
        5
      ],
      "output": 6,
      "confidence": "read"
    },
    {
      "id": "7B/4",
      "part": "7B",
      "kind": "not",
      "inputs": [
        9
      ],
      "output": 8,
      "confidence": "read"
    },
    {
      "id": "7B/5",
      "part": "7B",
      "kind": "not",
      "inputs": [
        11
      ],
      "output": 10,
      "confidence": "read"
    },
    {
      "id": "7B/6",
      "part": "7B",
      "kind": "not",
      "inputs": [
        13
      ],
      "output": 12,
      "confidence": "read"
    },
    {
      "id": "9B/1",
      "part": "9B",
      "kind": "not",
      "inputs": [
        1
      ],
      "output": 2,
      "confidence": "read"
    },
    {
      "id": "9B/2",
      "part": "9B",
      "kind": "not",
      "inputs": [
        3
      ],
      "output": 4,
      "confidence": "read"
    },
    {
      "id": "9B/3",
      "part": "9B",
      "kind": "not",
      "inputs": [
        5
      ],
      "output": 6,
      "confidence": "read"
    },
    {
      "id": "9B/4",
      "part": "9B",
      "kind": "not",
      "inputs": [
        9
      ],
      "output": 8,
      "confidence": "read"
    },
    {
      "id": "9B/5",
      "part": "9B",
      "kind": "not",
      "inputs": [
        11
      ],
      "output": 10,
      "confidence": "read"
    },
    {
      "id": "9B/6",
      "part": "9B",
      "kind": "not",
      "inputs": [
        13
      ],
      "output": 12,
      "confidence": "read"
    },
    {
      "id": "11A/1",
      "part": "11A",
      "kind": "and",
      "inputs": [
        1,
        2
      ],
      "output": 3,
      "confidence": "read"
    },
    {
      "id": "11A/2",
      "part": "11A",
      "kind": "and",
      "inputs": [
        5,
        4
      ],
      "output": 6,
      "confidence": "read"
    },
    {
      "id": "11A/4",
      "part": "11A",
      "kind": "and",
      "inputs": [
        12,
        13
      ],
      "output": 11,
      "confidence": "read"
    }
  ],
  "nets": [
    {
      "name": "BA9",
      "active": "low",
      "pins": [
        {
          "at": "P.44",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BA5",
      "active": "low",
      "pins": [
        {
          "at": "P.38",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BA4",
      "active": "low",
      "pins": [
        {
          "at": "P.37",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BA3",
      "active": "low",
      "pins": [
        {
          "at": "P.36",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BA6",
      "active": "low",
      "pins": [
        {
          "at": "P.40",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BA7",
      "active": "low",
      "pins": [
        {
          "at": "P.41",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BA8",
      "active": "low",
      "pins": [
        {
          "at": "P.42",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IOXE",
      "active": "low",
      "pins": [
        {
          "at": "P.11",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "DEQL",
      "active": "high",
      "pins": [],
      "note": "1 kohm pull-up to \u002B5 V on the line"
    },
    {
      "name": "BA0",
      "active": "low",
      "pins": [
        {
          "at": "P.32",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BA0",
      "active": "high",
      "pins": []
    },
    {
      "name": "BA1",
      "active": "low",
      "pins": [
        {
          "at": "P.33",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BA1",
      "active": "high",
      "pins": []
    },
    {
      "name": "BA2",
      "active": "low",
      "pins": [
        {
          "at": "P.34",
          "confidence": "read"
        }
      ],
      "note": "no inverter: the decoder sees address bit 2 upside down"
    },
    {
      "name": "ODW",
      "active": "low",
      "pins": [
        {
          "at": "11D.12",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "OSR",
      "active": "low",
      "pins": [
        {
          "at": "11A.13",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "OCW",
      "active": "low",
      "pins": [
        {
          "at": "15C.3",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IDR",
      "active": "low",
      "pins": [
        {
          "at": "11D.8",
          "confidence": "read"
        },
        {
          "at": "11D.9",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "SCW",
      "active": "low",
      "pins": [
        {
          "at": "11D.3",
          "confidence": "read"
        },
        {
          "at": "11D.5",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "ISR",
      "active": "low",
      "pins": [
        {
          "at": "11A.12",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "ICW",
      "active": "low",
      "pins": [
        {
          "at": "15C.11",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "ICW",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.39",
          "confidence": "read"
        },
        {
          "at": "17B.5",
          "confidence": "read"
        },
        {
          "at": "11B.9",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IDR",
      "active": "high",
      "pins": [
        {
          "at": "11D.10",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.24",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "ISCW",
      "active": "high",
      "pins": [
        {
          "at": "11D.1",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.21",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "OSCW",
      "active": "high",
      "pins": [
        {
          "at": "11D.4",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.18",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "N_11D_13",
      "active": "none",
      "pins": [
        {
          "at": "11D.13",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.20",
          "confidence": "read"
        }
      ],
      "note": "automatic name: ODW strobe to COM 2601 pin 20"
    },
    {
      "name": "H",
      "active": "high",
      "pins": [
        {
          "at": "11D.11",
          "confidence": "read"
        },
        {
          "at": "19C.4",
          "confidence": "read"
        },
        {
          "at": "19C.12",
          "confidence": "read"
        },
        {
          "at": "17D.3",
          "confidence": "read"
        },
        {
          "at": "17D.11",
          "confidence": "read"
        },
        {
          "at": "15C.4",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.25",
          "confidence": "read"
        }
      ],
      "note": "tied high through resistor 15A9; pins marked H on the drawing"
    },
    {
      "name": "GND",
      "active": "none",
      "pins": [
        {
          "at": "19C.1",
          "confidence": "read"
        },
        {
          "at": "19C.14",
          "confidence": "read"
        },
        {
          "at": "19C.15",
          "confidence": "read"
        },
        {
          "at": "19C.2",
          "confidence": "read"
        },
        {
          "at": "19C.3",
          "confidence": "read"
        },
        {
          "at": "18-19E.23",
          "confidence": "read"
        },
        {
          "at": "18-19E.2",
          "confidence": "read"
        },
        {
          "at": "18-19E.11",
          "confidence": "read"
        },
        {
          "at": "18-19E.12",
          "confidence": "read"
        },
        {
          "at": "18-19E.13",
          "confidence": "read"
        },
        {
          "at": "18-19E.14",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.4",
          "confidence": "read"
        },
        {
          "at": "P.80",
          "confidence": "read"
        },
        {
          "at": "P.82",
          "confidence": "read"
        },
        {
          "at": "P.84",
          "confidence": "read"
        },
        {
          "at": "P.86",
          "confidence": "read"
        },
        {
          "at": "P.94",
          "confidence": "read"
        }
      ],
      "note": "3D pins 10 and 6 are marked (STRAP)(GND): ident bits 0-1. 13C pins 5, 3, 11, 13 read by the rule that a pin number sits above its wire. COM 2601 pin 4 is printed in its box as GND"
    },
    {
      "name": "BD0",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.10",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD1",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.12",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD2",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.13",
          "confidence": "read"
        },
        {
          "at": "9B.9",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD3",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.14",
          "confidence": "read"
        },
        {
          "at": "7B.5",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD4",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.16",
          "confidence": "read"
        },
        {
          "at": "7B.3",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD5",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.17",
          "confidence": "read"
        },
        {
          "at": "7B.1",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD6",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.18",
          "confidence": "read"
        },
        {
          "at": "7B.13",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD7",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.20",
          "confidence": "read"
        },
        {
          "at": "7B.9",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD8",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.21",
          "confidence": "read"
        },
        {
          "at": "9B.11",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD9",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.22",
          "confidence": "read"
        },
        {
          "at": "9B.13",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD10",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.24",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD11",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.25",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.38",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD12",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.26",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.36",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD13",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.28",
          "confidence": "read"
        },
        {
          "at": "9B.3",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD14",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.29",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.37",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD15",
      "active": "low",
      "drive": "three-state",
      "pins": [
        {
          "at": "P.30",
          "confidence": "read"
        },
        {
          "at": "9B.5",
          "confidence": "read"
        }
      ],
      "note": "bus line; driven by an 8098 when the card is read"
    },
    {
      "name": "BD0",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.7",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BD1",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.8",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BD2",
      "active": "high",
      "pins": [
        {
          "at": "9B.8",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.9",
          "confidence": "read"
        },
        {
          "at": "15C.12",
          "confidence": "read"
        },
        {
          "at": "15C.2",
          "confidence": "read"
        },
        {
          "at": "11B.14",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BD3",
      "active": "high",
      "pins": [
        {
          "at": "7B.6",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.10",
          "confidence": "read"
        },
        {
          "at": "11B.13",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BD4",
      "active": "high",
      "pins": [
        {
          "at": "7B.4",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.11",
          "confidence": "read"
        },
        {
          "at": "17B.4",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BD5",
      "active": "high",
      "pins": [
        {
          "at": "7B.2",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.12",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BD6",
      "active": "high",
      "pins": [
        {
          "at": "7B.12",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.13",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BD7",
      "active": "high",
      "pins": [
        {
          "at": "7B.8",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.14",
          "confidence": "read"
        },
        {
          "at": "11B.11",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BD8",
      "active": "high",
      "pins": [
        {
          "at": "9B.10",
          "confidence": "read"
        },
        {
          "at": "11B.4",
          "confidence": "read"
        },
        {
          "at": "11D.2",
          "confidence": "inferred",
          "note": "label looks like \u0022BD5\u0022 on the scan; only bit 8 decodes the SINTRAN sync values (docs/2A50-reading.md)"
        }
      ]
    },
    {
      "name": "BD9",
      "active": "high",
      "pins": [
        {
          "at": "9B.12",
          "confidence": "read"
        },
        {
          "at": "11B.3",
          "confidence": "read"
        },
        {
          "at": "11D.6",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BD15",
      "active": "high",
      "pins": [
        {
          "at": "9B.6",
          "confidence": "read"
        },
        {
          "at": "11B.6",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "N_9B_4",
      "active": "none",
      "pins": [
        {
          "at": "9B.4",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.40",
          "confidence": "read"
        }
      ],
      "note": "automatic name: BD13 inverted to COM 2601 pin 40"
    },
    {
      "name": "IRFTIE",
      "active": "high",
      "pins": []
    },
    {
      "name": "IERRIE",
      "active": "high",
      "pins": []
    },
    {
      "name": "ORFTIE",
      "active": "high",
      "pins": []
    },
    {
      "name": "OERRIE",
      "active": "high",
      "pins": []
    },
    {
      "name": "IRFT",
      "active": "high",
      "pins": []
    },
    {
      "name": "IERR",
      "active": "high",
      "pins": []
    },
    {
      "name": "ORFT",
      "active": "high",
      "pins": []
    },
    {
      "name": "OERR",
      "active": "high",
      "pins": []
    },
    {
      "name": "N_3B_8",
      "active": "none",
      "pins": [
        {
          "at": "7B.11",
          "confidence": "read"
        }
      ],
      "note": "automatic name: level 12 AND-OR-INVERT term"
    },
    {
      "name": "LEV12",
      "active": "high",
      "pins": [
        {
          "at": "7B.10",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "LEV12",
      "active": "low",
      "drive": "open-collector",
      "driveConfidence": "inferred",
      "pullUp": "open",
      "pins": [
        {
          "at": "P.35",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "LEV10",
      "active": "high",
      "pins": []
    },
    {
      "name": "LEV10",
      "active": "low",
      "drive": "open-collector",
      "driveConfidence": "inferred",
      "pullUp": "open",
      "pins": [
        {
          "at": "P.27",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "INT",
      "active": "high",
      "pins": []
    },
    {
      "name": "DINPUT",
      "active": "high",
      "pins": []
    },
    {
      "name": "TINPUT",
      "active": "high",
      "pins": [
        {
          "at": "9B.1",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "INPUT",
      "active": "low",
      "drive": "open-collector",
      "driveConfidence": "inferred",
      "pullUp": "open",
      "pins": [
        {
          "at": "P.15",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "CONNECT",
      "active": "low",
      "drive": "open-collector",
      "driveConfidence": "inferred",
      "pullUp": "open",
      "pins": [
        {
          "at": "P.23",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "INIDENT",
      "active": "low",
      "pins": [
        {
          "at": "P.7",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "OUTIDENT",
      "active": "low",
      "pins": [
        {
          "at": "P.6",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "INGRANT",
      "active": "low",
      "pins": [
        {
          "at": "P.9",
          "confidence": "read"
        },
        {
          "at": "P.8",
          "confidence": "read"
        }
      ],
      "note": "wired straight through to OUTGRANT (pin 8)"
    },
    {
      "name": "N_11E_8",
      "active": "none",
      "pins": [],
      "note": "automatic name: 74153 select A"
    },
    {
      "name": "N_11E_11",
      "active": "none",
      "pins": [],
      "note": "automatic name: 74153 select B"
    },
    {
      "name": "N_9B_2",
      "active": "none",
      "pins": [
        {
          "at": "9B.2",
          "confidence": "read"
        }
      ],
      "note": "automatic name: bus driver enable, 1000 pF to ground"
    },
    {
      "name": "SR",
      "active": "low",
      "pins": [
        {
          "at": "11A.11",
          "confidence": "read"
        }
      ],
      "note": "1000 pF to ground"
    },
    {
      "name": "IDATA0",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.35",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IDATA1",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.34",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IDATA2",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.33",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IDATA3",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.32",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IDATA4",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.31",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IDATA5",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.30",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IDATA6",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.29",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IDATA7",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.28",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "OACTIVE",
      "active": "high",
      "pins": [
        {
          "at": "17B.3",
          "confidence": "read"
        },
        {
          "at": "13B.5",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IACTIVE",
      "active": "high",
      "pins": [
        {
          "at": "11B.15",
          "confidence": "read"
        },
        {
          "at": "13B.1",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "IPE",
      "active": "high",
      "pins": []
    },
    {
      "name": "IOR",
      "active": "high",
      "pins": []
    },
    {
      "name": "ISYNERR",
      "active": "high",
      "pins": [
        {
          "at": "11A.6",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "CARRERR",
      "active": "high",
      "pins": [
        {
          "at": "11A.3",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "CARREN",
      "active": "high",
      "pins": [
        {
          "at": "11B.2",
          "confidence": "read"
        },
        {
          "at": "11A.1",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "SCEE",
      "active": "high",
      "pins": [
        {
          "at": "11B.5",
          "confidence": "read"
        },
        {
          "at": "11A.5",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "SCR",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.17",
          "confidence": "read"
        },
        {
          "at": "11A.4",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "TDATA",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.3",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "TBMT",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.2",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "DA",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.26",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "PE",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.16",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "OR",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.27",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "SCT",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.5",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "RR",
      "active": "high",
      "pins": [
        {
          "at": "17D.13",
          "confidence": "read"
        },
        {
          "at": "7-8C-D.15",
          "confidence": "read"
        }
      ],
      "note": "74221 one-shot pulse"
    },
    {
      "name": "TCLOCK",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.19",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "RCLOCK",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.23",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "RDATA",
      "active": "high",
      "pins": [
        {
          "at": "7-8C-D.22",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "\u002B5V",
      "active": "none",
      "pins": [
        {
          "at": "7-8C-D.1",
          "confidence": "read"
        },
        {
          "at": "18-19E.1",
          "confidence": "read"
        },
        {
          "at": "18-19E.24",
          "confidence": "read"
        }
      ],
      "note": "COM 2601 supply pins are printed in its box: \u002B5V 1, -12V 6, GND 4"
    },
    {
      "name": "-12V",
      "active": "none",
      "pins": [
        {
          "at": "7-8C-D.6",
          "confidence": "read"
        },
        {
          "at": "18-19E.10",
          "confidence": "read"
        },
        {
          "at": "18-19E.15",
          "confidence": "read"
        },
        {
          "at": "P.62",
          "confidence": "inferred",
          "note": "circled 62/63 on the drawing"
        },
        {
          "at": "P.63",
          "confidence": "inferred",
          "note": "circled 62/63 on the drawing"
        }
      ]
    },
    {
      "name": "\u002B12V",
      "active": "none",
      "pins": [
        {
          "at": "18-19E.3",
          "confidence": "read"
        },
        {
          "at": "18-19E.22",
          "confidence": "read"
        },
        {
          "at": "P.66",
          "confidence": "inferred",
          "note": "circled 66/67 on the drawing"
        },
        {
          "at": "P.67",
          "confidence": "inferred",
          "note": "circled 66/67 on the drawing"
        }
      ]
    },
    {
      "name": "MC",
      "active": "low",
      "pins": [
        {
          "at": "P.55",
          "confidence": "read"
        },
        {
          "at": "17B.10",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "N_17B_6",
      "active": "none",
      "pins": [
        {
          "at": "17B.6",
          "confidence": "read"
        },
        {
          "at": "17B.9",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "CL",
      "active": "high",
      "pins": [
        {
          "at": "17B.8",
          "confidence": "read"
        },
        {
          "at": "17C.9",
          "confidence": "read"
        },
        {
          "at": "17C.10",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "CL1",
      "active": "low",
      "pins": [
        {
          "at": "11B.1",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "CL",
      "active": "low",
      "pins": [
        {
          "at": "17C.8",
          "confidence": "read"
        },
        {
          "at": "17C.4",
          "confidence": "read"
        },
        {
          "at": "17D.2",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "TIMEOUT",
      "active": "low",
      "pins": [
        {
          "at": "19C.5",
          "confidence": "read"
        },
        {
          "at": "17C.5",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "N_17C_6",
      "active": "none",
      "pins": [
        {
          "at": "17C.6",
          "confidence": "read"
        },
        {
          "at": "17C.1",
          "confidence": "read"
        },
        {
          "at": "17C.2",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "N_17C_3",
      "active": "none",
      "pins": [
        {
          "at": "17C.3",
          "confidence": "read"
        },
        {
          "at": "15C.13",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "CDSTL",
      "active": "high",
      "pins": [
        {
          "at": "15C.9",
          "confidence": "read"
        },
        {
          "at": "15C.1",
          "confidence": "read"
        },
        {
          "at": "17C.12",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "CDSTL",
      "active": "low",
      "pins": [
        {
          "at": "15C.8",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "CARR",
      "active": "low",
      "pins": [
        {
          "at": "17C.13",
          "confidence": "read"
        },
        {
          "at": "11A.2",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "CARR",
      "active": "high",
      "pins": [
        {
          "at": "13B.13",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "N_17C_11",
      "active": "none",
      "pins": [
        {
          "at": "17C.11",
          "confidence": "read"
        },
        {
          "at": "19C.13",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "PRQTS",
      "active": "high",
      "pins": [
        {
          "at": "15C.5",
          "confidence": "read"
        },
        {
          "at": "13B.9",
          "confidence": "read"
        },
        {
          "at": "19C.11",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "TIMEOUTEN",
      "active": "low",
      "pins": [
        {
          "at": "11B.10",
          "confidence": "read"
        },
        {
          "at": "19C.10",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "OSC",
      "active": "high",
      "pins": [
        {
          "at": "17D.12",
          "confidence": "read"
        },
        {
          "at": "19C.9",
          "confidence": "read"
        }
      ],
      "note": "through 220 ohm to OSCD"
    },
    {
      "name": "OSCD",
      "active": "high",
      "pins": [
        {
          "at": "17D.10",
          "confidence": "read"
        }
      ],
      "note": "OSC through 220 ohm, 10 uF to ground"
    },
    {
      "name": "TTM",
      "active": "low",
      "pins": [
        {
          "at": "17D.9",
          "confidence": "read"
        },
        {
          "at": "13B.4",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "N_9E_1",
      "active": "none",
      "pins": [
        {
          "at": "17D.1",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "N_13B_11",
      "active": "none",
      "pins": [
        {
          "at": "13B.11",
          "confidence": "read"
        },
        {
          "at": "13B.10",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "TRQTS",
      "active": "low",
      "pins": [
        {
          "at": "13B.8",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "HALF",
      "active": "high",
      "pins": [
        {
          "at": "11B.7",
          "confidence": "read"
        },
        {
          "at": "13B.12",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "RFS",
      "active": "low",
      "pins": [
        {
          "at": "17B.1",
          "confidence": "read"
        },
        {
          "at": "17B.2",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "N_13B_6",
      "active": "none",
      "pins": [
        {
          "at": "13B.6",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "RTM",
      "active": "low",
      "pins": [
        {
          "at": "13B.2",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "N_13B_3",
      "active": "none",
      "pins": [
        {
          "at": "13B.3",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "BFQ1",
      "active": "high",
      "pins": [
        {
          "at": "P.4",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "TDA",
      "active": "high",
      "pins": []
    },
    {
      "name": "TEST",
      "active": "high",
      "pins": [
        {
          "at": "11B.12",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "RQTS",
      "active": "low",
      "pins": []
    },
    {
      "name": "DSR",
      "active": "low",
      "pins": []
    },
    {
      "name": "CAL",
      "active": "low",
      "pins": [
        {
          "at": "15C.10",
          "confidence": "read"
        }
      ]
    },
    {
      "name": "RECEIVED DATA",
      "active": "none",
      "pins": [
        {
          "at": "F.87",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 3"
    },
    {
      "name": "READY FOR SENDING",
      "active": "none",
      "pins": [
        {
          "at": "F.89",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 5"
    },
    {
      "name": "DATA SET READY",
      "active": "none",
      "pins": [
        {
          "at": "F.90",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 6"
    },
    {
      "name": "SIGNAL DETECTOR",
      "active": "none",
      "pins": [
        {
          "at": "F.88",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 8"
    },
    {
      "name": "CALLING INDICATOR",
      "active": "none",
      "pins": [
        {
          "at": "F.85",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 22"
    },
    {
      "name": "TRANSMITTER TIMING",
      "active": "none",
      "pins": [
        {
          "at": "F.83",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 15"
    },
    {
      "name": "RECEIVER TIMING",
      "active": "none",
      "pins": [
        {
          "at": "F.81",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 17"
    },
    {
      "name": "CONNECT TO LINE",
      "active": "none",
      "pins": [
        {
          "at": "F.91",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 20"
    },
    {
      "name": "REQUEST TO SEND",
      "active": "none",
      "pins": [
        {
          "at": "F.92",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 4"
    },
    {
      "name": "V24_23",
      "active": "none",
      "pins": [
        {
          "at": "F.93",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 23; no signal name printed. 19A pin 2 = H, pin 4 = GND"
    },
    {
      "name": "TRANSM DATA",
      "active": "none",
      "pins": [
        {
          "at": "F.95",
          "confidence": "inferred",
          "note": "circled number with a modem signal name: front connector"
        }
      ],
      "note": "V.24 pin 2; 2200 pF to ground"
    }
  ],
  "blocks": [
    {
      "name": "address-decode",
      "ports": [
        {
          "name": "BA_0",
          "direction": "input",
          "signals": [
            {
              "net": "BA0",
              "active": "low"
            },
            {
              "net": "BA1",
              "active": "low"
            },
            {
              "net": "BA2",
              "active": "low"
            },
            {
              "net": "BA3",
              "active": "low"
            },
            {
              "net": "BA4",
              "active": "low"
            },
            {
              "net": "BA5",
              "active": "low"
            },
            {
              "net": "BA6",
              "active": "low"
            },
            {
              "net": "BA7",
              "active": "low"
            },
            {
              "net": "BA8",
              "active": "low"
            },
            {
              "net": "BA9",
              "active": "low"
            }
          ]
        },
        {
          "name": "BA_1",
          "direction": "output",
          "signals": [
            {
              "net": "BA0",
              "active": "high"
            },
            {
              "net": "BA1",
              "active": "high"
            }
          ]
        },
        {
          "name": "IOXE",
          "direction": "input",
          "signals": [
            {
              "net": "IOXE",
              "active": "low"
            }
          ]
        },
        {
          "name": "DEQL",
          "direction": "input",
          "signals": [
            {
              "net": "DEQL",
              "active": "high"
            }
          ]
        },
        {
          "name": "ODW",
          "direction": "output",
          "signals": [
            {
              "net": "ODW",
              "active": "low"
            }
          ]
        },
        {
          "name": "OSR",
          "direction": "output",
          "signals": [
            {
              "net": "OSR",
              "active": "low"
            }
          ]
        },
        {
          "name": "OCW",
          "direction": "output",
          "signals": [
            {
              "net": "OCW",
              "active": "low"
            }
          ]
        },
        {
          "name": "IDR_0",
          "direction": "output",
          "signals": [
            {
              "net": "IDR",
              "active": "low"
            }
          ]
        },
        {
          "name": "SCW",
          "direction": "output",
          "signals": [
            {
              "net": "SCW",
              "active": "low"
            }
          ]
        },
        {
          "name": "ISR",
          "direction": "output",
          "signals": [
            {
              "net": "ISR",
              "active": "low"
            }
          ]
        },
        {
          "name": "ICW_0",
          "direction": "output",
          "signals": [
            {
              "net": "ICW",
              "active": "low"
            }
          ]
        },
        {
          "name": "ICW_1",
          "direction": "output",
          "signals": [
            {
              "net": "ICW",
              "active": "high"
            }
          ]
        },
        {
          "name": "H",
          "direction": "input",
          "signals": [
            {
              "net": "H",
              "active": "high"
            }
          ]
        },
        {
          "name": "GND",
          "direction": "input",
          "signals": [
            {
              "net": "GND"
            }
          ]
        },
        {
          "name": "BD10",
          "direction": "input",
          "signals": [
            {
              "net": "BD10",
              "active": "low"
            }
          ]
        },
        {
          "name": "IERR",
          "direction": "output",
          "signals": [
            {
              "net": "IERR",
              "active": "high"
            }
          ]
        },
        {
          "name": "N_11E_8",
          "direction": "output",
          "signals": [
            {
              "net": "N_11E_8"
            }
          ]
        },
        {
          "name": "N_11E_11",
          "direction": "output",
          "signals": [
            {
              "net": "N_11E_11"
            }
          ]
        },
        {
          "name": "IPE",
          "direction": "input",
          "signals": [
            {
              "net": "IPE",
              "active": "high"
            }
          ]
        },
        {
          "name": "IOR",
          "direction": "input",
          "signals": [
            {
              "net": "IOR",
              "active": "high"
            }
          ]
        },
        {
          "name": "ISYNERR",
          "direction": "input",
          "signals": [
            {
              "net": "ISYNERR",
              "active": "high"
            }
          ]
        },
        {
          "name": "CARRERR",
          "direction": "input",
          "signals": [
            {
              "net": "CARRERR",
              "active": "high"
            }
          ]
        },
        {
          "name": "CL_1",
          "direction": "input",
          "signals": [
            {
              "net": "CL",
              "active": "high"
            }
          ]
        },
        {
          "name": "CL1",
          "direction": "output",
          "signals": [
            {
              "net": "CL1",
              "active": "low"
            }
          ]
        },
        {
          "name": "N_9E_1",
          "direction": "output",
          "signals": [
            {
              "net": "N_9E_1"
            }
          ]
        }
      ],
      "parts": [
        {
          "ref": "13E",
          "type": "DM8160",
          "function": "address comparator against the modem number switches 15E",
          "confidence": "read"
        },
        {
          "ref": "13D",
          "type": "7442",
          "function": "register decoder",
          "confidence": "read"
        },
        {
          "ref": "11C",
          "type": "7404",
          "function": "inverters for BA0, BA1",
          "confidence": "read"
        },
        {
          "ref": "11E",
          "type": "7400",
          "function": "NAND gates: BA7/BA8 decode, OSR/IDR and ISR/IDR status enables, IERR",
          "confidence": "read"
        },
        {
          "ref": "9E",
          "type": "7402",
          "function": "NOR gates: address decode (BA6), receiver reset strobe, IERR",
          "confidence": "read"
        }
      ],
      "gates": [
        {
          "id": "11E/1",
          "part": "11E",
          "kind": "nand",
          "inputs": [
            2,
            1
          ],
          "output": 3,
          "confidence": "read"
        },
        {
          "id": "11E/2",
          "part": "11E",
          "kind": "nand",
          "inputs": [
            4,
            5
          ],
          "output": 6,
          "confidence": "read"
        },
        {
          "id": "11E/3",
          "part": "11E",
          "kind": "nand",
          "inputs": [
            9,
            10
          ],
          "output": 8,
          "confidence": "read"
        },
        {
          "id": "11E/4",
          "part": "11E",
          "kind": "nand",
          "inputs": [
            12,
            13
          ],
          "output": 11,
          "confidence": "read"
        },
        {
          "id": "9E/1",
          "part": "9E",
          "kind": "nor",
          "inputs": [
            2,
            3
          ],
          "output": 1,
          "confidence": "read"
        },
        {
          "id": "9E/2",
          "part": "9E",
          "kind": "nor",
          "inputs": [
            6,
            5
          ],
          "output": 4,
          "confidence": "read"
        },
        {
          "id": "9E/3",
          "part": "9E",
          "kind": "nor",
          "inputs": [
            8,
            9
          ],
          "output": 10,
          "confidence": "read"
        },
        {
          "id": "9E/4",
          "part": "9E",
          "kind": "nor",
          "inputs": [
            12,
            11
          ],
          "output": 13,
          "confidence": "read"
        },
        {
          "id": "11C/1",
          "part": "11C",
          "kind": "not",
          "inputs": [
            1
          ],
          "output": 2,
          "confidence": "read"
        },
        {
          "id": "11C/2",
          "part": "11C",
          "kind": "not",
          "inputs": [
            3
          ],
          "output": 4,
          "confidence": "read"
        },
        {
          "id": "11C/3",
          "part": "11C",
          "kind": "not",
          "inputs": [
            5
          ],
          "output": 6,
          "confidence": "read"
        },
        {
          "id": "11C/4",
          "part": "11C",
          "kind": "not",
          "inputs": [
            9
          ],
          "output": 8,
          "confidence": "read"
        },
        {
          "id": "11C/6",
          "part": "11C",
          "kind": "not",
          "inputs": [
            13
          ],
          "output": 12,
          "confidence": "read"
        }
      ],
      "nets": [
        {
          "name": "SMN3",
          "active": "low",
          "pins": [
            {
              "at": "13E.4",
              "confidence": "read"
            }
          ],
          "note": "modem number switch 15E position 1 with pull-up (switch and resistor are not parts in this file)"
        },
        {
          "name": "SMN2",
          "active": "low",
          "pins": [
            {
              "at": "13E.13",
              "confidence": "read"
            }
          ],
          "note": "switch 15E position 2"
        },
        {
          "name": "SMN1",
          "active": "low",
          "pins": [
            {
              "at": "13E.10",
              "confidence": "read"
            }
          ],
          "note": "switch 15E position 3"
        },
        {
          "name": "SMN0",
          "active": "low",
          "pins": [
            {
              "at": "13E.6",
              "confidence": "read"
            }
          ],
          "note": "switch 15E position 4"
        },
        {
          "name": "BA9",
          "active": "low",
          "pins": [
            {
              "at": "13E.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA5",
          "active": "low",
          "pins": [
            {
              "at": "13E.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA4",
          "active": "low",
          "pins": [
            {
              "at": "13E.11",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA3",
          "active": "low",
          "pins": [
            {
              "at": "13E.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA6",
          "active": "low",
          "pins": [
            {
              "at": "9E.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA7",
          "active": "low",
          "pins": [
            {
              "at": "11E.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA8",
          "active": "low",
          "pins": [
            {
              "at": "11E.1",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_11E_3",
          "active": "none",
          "pins": [
            {
              "at": "11E.3",
              "confidence": "read"
            },
            {
              "at": "9E.11",
              "confidence": "read"
            }
          ],
          "note": "automatic name: NAND of BA7 and BA8"
        },
        {
          "name": "N_9E_13",
          "active": "none",
          "pins": [
            {
              "at": "9E.13",
              "confidence": "read"
            },
            {
              "at": "13E.2",
              "confidence": "read"
            }
          ],
          "note": "automatic name"
        },
        {
          "name": "IOXE",
          "active": "low",
          "pins": [
            {
              "at": "13E.14",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DEQL",
          "active": "high",
          "pins": [
            {
              "at": "13E.9",
              "confidence": "read"
            },
            {
              "at": "11C.9",
              "confidence": "read"
            }
          ],
          "note": "1 kohm pull-up to \u002B5 V on the line"
        },
        {
          "name": "N_11C_8",
          "active": "none",
          "pins": [
            {
              "at": "11C.8",
              "confidence": "read"
            },
            {
              "at": "13D.12",
              "confidence": "read"
            }
          ],
          "note": "automatic name: inverted DEQL enables the 7442"
        },
        {
          "name": "BA0",
          "active": "low",
          "pins": [
            {
              "at": "11C.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA0",
          "active": "high",
          "pins": [
            {
              "at": "11C.4",
              "confidence": "read"
            },
            {
              "at": "13D.15",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA1",
          "active": "low",
          "pins": [
            {
              "at": "11C.1",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA1",
          "active": "high",
          "pins": [
            {
              "at": "11C.2",
              "confidence": "read"
            },
            {
              "at": "13D.14",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA2",
          "active": "low",
          "pins": [
            {
              "at": "13D.13",
              "confidence": "read"
            }
          ],
          "note": "no inverter: the decoder sees address bit 2 upside down"
        },
        {
          "name": "ODW",
          "active": "low",
          "pins": [
            {
              "at": "13D.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "OSR",
          "active": "low",
          "pins": [
            {
              "at": "13D.3",
              "confidence": "read"
            },
            {
              "at": "11E.9",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "OCW",
          "active": "low",
          "pins": [
            {
              "at": "13D.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IDR",
          "active": "low",
          "pins": [
            {
              "at": "13D.5",
              "confidence": "read"
            },
            {
              "at": "11E.10",
              "confidence": "read"
            },
            {
              "at": "11E.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "SCW",
          "active": "low",
          "pins": [
            {
              "at": "13D.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ISR",
          "active": "low",
          "pins": [
            {
              "at": "13D.7",
              "confidence": "read"
            },
            {
              "at": "11E.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ICW",
          "active": "low",
          "pins": [
            {
              "at": "13D.9",
              "confidence": "read"
            },
            {
              "at": "9E.2",
              "confidence": "read"
            },
            {
              "at": "11C.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ICW",
          "active": "high",
          "pins": [
            {
              "at": "11C.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "H",
          "active": "high",
          "pins": [
            {
              "at": "13E.1",
              "confidence": "read"
            }
          ],
          "note": "tied high through resistor 15A9; pins marked H on the drawing"
        },
        {
          "name": "GND",
          "active": "none",
          "pins": [
            {
              "at": "13E.15",
              "confidence": "read"
            },
            {
              "at": "13E.7",
              "confidence": "read"
            }
          ],
          "note": "3D pins 10 and 6 are marked (STRAP)(GND): ident bits 0-1. 13C pins 5, 3, 11, 13 read by the rule that a pin number sits above its wire. COM 2601 pin 4 is printed in its box as GND"
        },
        {
          "name": "BD10",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "9E.3",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "IERR",
          "active": "high",
          "pins": [
            {
              "at": "11E.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_11E_8",
          "active": "none",
          "pins": [
            {
              "at": "11E.8",
              "confidence": "read"
            }
          ],
          "note": "automatic name: 74153 select A"
        },
        {
          "name": "N_11E_11",
          "active": "none",
          "pins": [
            {
              "at": "11E.11",
              "confidence": "read"
            }
          ],
          "note": "automatic name: 74153 select B"
        },
        {
          "name": "IPE",
          "active": "high",
          "pins": [
            {
              "at": "9E.9",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IOR",
          "active": "high",
          "pins": [
            {
              "at": "9E.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ISYNERR",
          "active": "high",
          "pins": [
            {
              "at": "9E.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_9E_10",
          "active": "none",
          "pins": [
            {
              "at": "9E.10",
              "confidence": "read"
            },
            {
              "at": "11E.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_9E_4",
          "active": "none",
          "pins": [
            {
              "at": "9E.4",
              "confidence": "read"
            },
            {
              "at": "11E.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CARRERR",
          "active": "high",
          "pins": [
            {
              "at": "9E.8",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CL",
          "active": "high",
          "pins": [
            {
              "at": "11C.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CL1",
          "active": "low",
          "pins": [
            {
              "at": "11C.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_9E_1",
          "active": "none",
          "pins": [
            {
              "at": "9E.1",
              "confidence": "read"
            }
          ]
        }
      ]
    },
    {
      "name": "read-multiplexers",
      "ports": [
        {
          "name": "BA_1",
          "direction": "input",
          "signals": [
            {
              "net": "BA0",
              "active": "high"
            },
            {
              "net": "BA1",
              "active": "high"
            }
          ]
        },
        {
          "name": "BD_0",
          "direction": "inout",
          "signals": [
            {
              "net": "BD0",
              "active": "low"
            },
            {
              "net": "BD1",
              "active": "low"
            },
            {
              "net": "BD2",
              "active": "low"
            },
            {
              "net": "BD3",
              "active": "low"
            },
            {
              "net": "BD4",
              "active": "low"
            },
            {
              "net": "BD5",
              "active": "low"
            },
            {
              "net": "BD6",
              "active": "low"
            },
            {
              "net": "BD7",
              "active": "low"
            },
            {
              "net": "BD8",
              "active": "low"
            },
            {
              "net": "BD9",
              "active": "low"
            },
            {
              "net": "BD10",
              "active": "low"
            },
            {
              "net": "BD11",
              "active": "low"
            },
            {
              "net": "BD12",
              "active": "low"
            },
            {
              "net": "BD13",
              "active": "low"
            },
            {
              "net": "BD14",
              "active": "low"
            },
            {
              "net": "BD15",
              "active": "low"
            }
          ]
        },
        {
          "name": "BD_1",
          "direction": "output",
          "signals": [
            {
              "net": "BD0",
              "active": "high"
            },
            {
              "net": "BD1",
              "active": "high"
            }
          ]
        },
        {
          "name": "LEV_1",
          "direction": "input",
          "signals": [
            {
              "net": "LEV10",
              "active": "high"
            },
            {
              "net": "LEV12",
              "active": "high"
            }
          ]
        },
        {
          "name": "IDATA_1",
          "direction": "input",
          "signals": [
            {
              "net": "IDATA0",
              "active": "high"
            },
            {
              "net": "IDATA1",
              "active": "high"
            },
            {
              "net": "IDATA2",
              "active": "high"
            },
            {
              "net": "IDATA3",
              "active": "high"
            },
            {
              "net": "IDATA4",
              "active": "high"
            },
            {
              "net": "IDATA5",
              "active": "high"
            },
            {
              "net": "IDATA6",
              "active": "high"
            },
            {
              "net": "IDATA7",
              "active": "high"
            }
          ]
        },
        {
          "name": "DEQL",
          "direction": "input",
          "signals": [
            {
              "net": "DEQL",
              "active": "high"
            }
          ]
        },
        {
          "name": "ISR",
          "direction": "input",
          "signals": [
            {
              "net": "ISR",
              "active": "low"
            }
          ]
        },
        {
          "name": "GND",
          "direction": "input",
          "signals": [
            {
              "net": "GND"
            }
          ]
        },
        {
          "name": "IRFTIE",
          "direction": "input",
          "signals": [
            {
              "net": "IRFTIE",
              "active": "high"
            }
          ]
        },
        {
          "name": "IERRIE",
          "direction": "input",
          "signals": [
            {
              "net": "IERRIE",
              "active": "high"
            }
          ]
        },
        {
          "name": "ORFTIE",
          "direction": "input",
          "signals": [
            {
              "net": "ORFTIE",
              "active": "high"
            }
          ]
        },
        {
          "name": "OERRIE",
          "direction": "input",
          "signals": [
            {
              "net": "OERRIE",
              "active": "high"
            }
          ]
        },
        {
          "name": "IRFT",
          "direction": "output",
          "signals": [
            {
              "net": "IRFT",
              "active": "high"
            }
          ]
        },
        {
          "name": "IERR",
          "direction": "input",
          "signals": [
            {
              "net": "IERR",
              "active": "high"
            }
          ]
        },
        {
          "name": "ORFT",
          "direction": "output",
          "signals": [
            {
              "net": "ORFT",
              "active": "high"
            }
          ]
        },
        {
          "name": "OERR",
          "direction": "output",
          "signals": [
            {
              "net": "OERR",
              "active": "high"
            }
          ]
        },
        {
          "name": "INT",
          "direction": "output",
          "signals": [
            {
              "net": "INT",
              "active": "high"
            }
          ]
        },
        {
          "name": "DINPUT",
          "direction": "output",
          "signals": [
            {
              "net": "DINPUT",
              "active": "high"
            }
          ]
        },
        {
          "name": "N_11E_8",
          "direction": "input",
          "signals": [
            {
              "net": "N_11E_8"
            }
          ]
        },
        {
          "name": "N_11E_11",
          "direction": "input",
          "signals": [
            {
              "net": "N_11E_11"
            }
          ]
        },
        {
          "name": "N_9B_2",
          "direction": "input",
          "signals": [
            {
              "net": "N_9B_2"
            }
          ]
        },
        {
          "name": "SR",
          "direction": "input",
          "signals": [
            {
              "net": "SR",
              "active": "low"
            }
          ]
        },
        {
          "name": "OACTIVE",
          "direction": "input",
          "signals": [
            {
              "net": "OACTIVE",
              "active": "high"
            }
          ]
        },
        {
          "name": "IACTIVE",
          "direction": "input",
          "signals": [
            {
              "net": "IACTIVE",
              "active": "high"
            }
          ]
        },
        {
          "name": "IPE",
          "direction": "output",
          "signals": [
            {
              "net": "IPE",
              "active": "high"
            }
          ]
        },
        {
          "name": "IOR",
          "direction": "output",
          "signals": [
            {
              "net": "IOR",
              "active": "high"
            }
          ]
        },
        {
          "name": "ISYNERR",
          "direction": "input",
          "signals": [
            {
              "net": "ISYNERR",
              "active": "high"
            }
          ]
        },
        {
          "name": "CARRERR",
          "direction": "input",
          "signals": [
            {
              "net": "CARRERR",
              "active": "high"
            }
          ]
        },
        {
          "name": "CARREN",
          "direction": "input",
          "signals": [
            {
              "net": "CARREN",
              "active": "high"
            }
          ]
        },
        {
          "name": "TDATA",
          "direction": "input",
          "signals": [
            {
              "net": "TDATA",
              "active": "high"
            }
          ]
        },
        {
          "name": "TBMT",
          "direction": "input",
          "signals": [
            {
              "net": "TBMT",
              "active": "high"
            }
          ]
        },
        {
          "name": "DA",
          "direction": "input",
          "signals": [
            {
              "net": "DA",
              "active": "high"
            }
          ]
        },
        {
          "name": "PE",
          "direction": "input",
          "signals": [
            {
              "net": "PE",
              "active": "high"
            }
          ]
        },
        {
          "name": "OR",
          "direction": "input",
          "signals": [
            {
              "net": "OR",
              "active": "high"
            }
          ]
        },
        {
          "name": "SCT",
          "direction": "input",
          "signals": [
            {
              "net": "SCT",
              "active": "high"
            }
          ]
        },
        {
          "name": "CDSTL_0",
          "direction": "input",
          "signals": [
            {
              "net": "CDSTL",
              "active": "low"
            }
          ]
        },
        {
          "name": "CARR_0",
          "direction": "input",
          "signals": [
            {
              "net": "CARR",
              "active": "low"
            }
          ]
        },
        {
          "name": "HALF",
          "direction": "input",
          "signals": [
            {
              "net": "HALF",
              "active": "high"
            }
          ]
        },
        {
          "name": "RFS",
          "direction": "input",
          "signals": [
            {
              "net": "RFS",
              "active": "low"
            }
          ]
        },
        {
          "name": "TDA",
          "direction": "output",
          "signals": [
            {
              "net": "TDA",
              "active": "high"
            }
          ]
        },
        {
          "name": "RQTS",
          "direction": "input",
          "signals": [
            {
              "net": "RQTS",
              "active": "low"
            }
          ]
        },
        {
          "name": "DSR",
          "direction": "input",
          "signals": [
            {
              "net": "DSR",
              "active": "low"
            }
          ]
        }
      ],
      "parts": [
        {
          "ref": "3D",
          "type": "74153",
          "function": "status multiplexer",
          "confidence": "read"
        },
        {
          "ref": "3E",
          "type": "74153",
          "function": "status multiplexer",
          "confidence": "read"
        },
        {
          "ref": "5E",
          "type": "74153",
          "function": "status multiplexer",
          "confidence": "read"
        },
        {
          "ref": "5D",
          "type": "74153",
          "function": "status multiplexer",
          "confidence": "read"
        },
        {
          "ref": "13C",
          "type": "74153",
          "function": "bus answer multiplexer: INT (pins 6,5,4,3 \u003E 7, LEV10/LEV12 selected by BA0/BA1) and DINPUT (pins 10,11,12,13 \u003E 9, from DEQL)",
          "confidence": "read"
        },
        {
          "ref": "5B",
          "type": "8098",
          "function": "status buffer",
          "confidence": "read"
        },
        {
          "ref": "7A",
          "type": "8098",
          "function": "status buffer, bits 10-15",
          "confidence": "read"
        },
        {
          "ref": "7E",
          "type": "8097",
          "function": "chip status buffer",
          "confidence": "read"
        },
        {
          "ref": "5C",
          "type": "8098",
          "function": "bus drivers for status bits 0, 1, 2, 6 (pins 2,4,6,10 \u003E 3,5,7,9) and bus receivers BD0, BD1 (pins 12,14 \u003E 11,13)",
          "confidence": "read"
        }
      ],
      "gates": [],
      "nets": [
        {
          "name": "DEQL",
          "active": "high",
          "pins": [
            {
              "at": "13C.10",
              "confidence": "read"
            },
            {
              "at": "13C.12",
              "confidence": "read"
            }
          ],
          "note": "1 kohm pull-up to \u002B5 V on the line"
        },
        {
          "name": "BA0",
          "active": "high",
          "pins": [
            {
              "at": "13C.14",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BA1",
          "active": "high",
          "pins": [
            {
              "at": "13C.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ISR",
          "active": "low",
          "pins": [
            {
              "at": "5B.15",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "GND",
          "active": "none",
          "pins": [
            {
              "at": "3D.1",
              "confidence": "read"
            },
            {
              "at": "3D.15",
              "confidence": "read"
            },
            {
              "at": "3D.10",
              "confidence": "read"
            },
            {
              "at": "3D.6",
              "confidence": "read"
            },
            {
              "at": "3E.1",
              "confidence": "read"
            },
            {
              "at": "3E.15",
              "confidence": "read"
            },
            {
              "at": "5E.1",
              "confidence": "read"
            },
            {
              "at": "5E.15",
              "confidence": "read"
            },
            {
              "at": "5D.1",
              "confidence": "read"
            },
            {
              "at": "5D.15",
              "confidence": "read"
            },
            {
              "at": "5D.6",
              "confidence": "read"
            },
            {
              "at": "5D.5",
              "confidence": "read"
            },
            {
              "at": "5D.10",
              "confidence": "read"
            },
            {
              "at": "5D.11",
              "confidence": "read"
            },
            {
              "at": "13C.1",
              "confidence": "read"
            },
            {
              "at": "13C.15",
              "confidence": "read"
            },
            {
              "at": "13C.5",
              "confidence": "read"
            },
            {
              "at": "13C.3",
              "confidence": "read"
            },
            {
              "at": "13C.11",
              "confidence": "read"
            },
            {
              "at": "13C.13",
              "confidence": "read"
            },
            {
              "at": "5C.15",
              "confidence": "read"
            },
            {
              "at": "7E.15",
              "confidence": "read"
            },
            {
              "at": "7E.1",
              "confidence": "read"
            }
          ],
          "note": "3D pins 10 and 6 are marked (STRAP)(GND): ident bits 0-1. 13C pins 5, 3, 11, 13 read by the rule that a pin number sits above its wire. COM 2601 pin 4 is printed in its box as GND"
        },
        {
          "name": "BD0",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "5C.7",
              "confidence": "read"
            },
            {
              "at": "5C.12",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD1",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "5C.5",
              "confidence": "read"
            },
            {
              "at": "5C.14",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD2",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "5C.3",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD3",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "5B.7",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD4",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "5B.5",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD5",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "5B.3",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD6",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "5C.9",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD7",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "5B.9",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD8",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "5B.11",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD9",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "5B.13",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD10",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "7A.7",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD11",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "7A.5",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD12",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "7A.3",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD13",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "7A.11",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD14",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "7A.13",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD15",
          "active": "low",
          "drive": "three-state",
          "pins": [
            {
              "at": "7A.9",
              "confidence": "read"
            }
          ],
          "note": "bus line; driven by an 8098 when the card is read"
        },
        {
          "name": "BD0",
          "active": "high",
          "pins": [
            {
              "at": "5C.11",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BD1",
          "active": "high",
          "pins": [
            {
              "at": "5C.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IRFTIE",
          "active": "high",
          "pins": [
            {
              "at": "3D.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IERRIE",
          "active": "high",
          "pins": [
            {
              "at": "3D.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ORFTIE",
          "active": "high",
          "pins": [
            {
              "at": "3D.11",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "OERRIE",
          "active": "high",
          "pins": [
            {
              "at": "3D.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IRFT",
          "active": "high",
          "pins": [
            {
              "at": "7E.11",
              "confidence": "read"
            },
            {
              "at": "3E.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IERR",
          "active": "high",
          "pins": [
            {
              "at": "5E.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ORFT",
          "active": "high",
          "pins": [
            {
              "at": "7E.9",
              "confidence": "read"
            },
            {
              "at": "3E.11",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "OERR",
          "active": "high",
          "pins": [
            {
              "at": "7E.7",
              "confidence": "read"
            },
            {
              "at": "5E.5",
              "confidence": "read"
            },
            {
              "at": "5E.11",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "LEV12",
          "active": "high",
          "pins": [
            {
              "at": "13C.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "LEV10",
          "active": "high",
          "pins": [
            {
              "at": "13C.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "INT",
          "active": "high",
          "pins": [
            {
              "at": "13C.7",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DINPUT",
          "active": "high",
          "pins": [
            {
              "at": "13C.9",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_11E_8",
          "active": "none",
          "pins": [
            {
              "at": "3D.14",
              "confidence": "read"
            },
            {
              "at": "3E.14",
              "confidence": "read",
              "note": "followed on the scan 2026-11-23: the two select lines run down past 3D to pins 2 and 14 of 3E, 5E and 5D with a junction dot at each (11E pin 11 to pin 2, 11E pin 8 to pin 14)"
            },
            {
              "at": "5E.14",
              "confidence": "read",
              "note": "followed on the scan 2026-11-23: the two select lines run down past 3D to pins 2 and 14 of 3E, 5E and 5D with a junction dot at each (11E pin 11 to pin 2, 11E pin 8 to pin 14)"
            },
            {
              "at": "5D.14",
              "confidence": "read",
              "note": "followed on the scan 2026-11-23: the two select lines run down past 3D to pins 2 and 14 of 3E, 5E and 5D with a junction dot at each (11E pin 11 to pin 2, 11E pin 8 to pin 14)"
            }
          ],
          "note": "automatic name: 74153 select A"
        },
        {
          "name": "N_11E_11",
          "active": "none",
          "pins": [
            {
              "at": "3D.2",
              "confidence": "read"
            },
            {
              "at": "3E.2",
              "confidence": "read",
              "note": "followed on the scan 2026-11-23: the two select lines run down past 3D to pins 2 and 14 of 3E, 5E and 5D with a junction dot at each (11E pin 11 to pin 2, 11E pin 8 to pin 14)"
            },
            {
              "at": "5E.2",
              "confidence": "read",
              "note": "followed on the scan 2026-11-23: the two select lines run down past 3D to pins 2 and 14 of 3E, 5E and 5D with a junction dot at each (11E pin 11 to pin 2, 11E pin 8 to pin 14)"
            },
            {
              "at": "5D.2",
              "confidence": "read",
              "note": "followed on the scan 2026-11-23: the two select lines run down past 3D to pins 2 and 14 of 3E, 5E and 5D with a junction dot at each (11E pin 11 to pin 2, 11E pin 8 to pin 14)"
            }
          ],
          "note": "automatic name: 74153 select B"
        },
        {
          "name": "DAT0",
          "active": "high",
          "pins": [
            {
              "at": "3D.9",
              "confidence": "read"
            },
            {
              "at": "5C.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DAT1",
          "active": "high",
          "pins": [
            {
              "at": "3D.7",
              "confidence": "read"
            },
            {
              "at": "5C.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DAT2",
          "active": "high",
          "pins": [
            {
              "at": "3E.7",
              "confidence": "read"
            },
            {
              "at": "5C.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DAT3",
          "active": "high",
          "pins": [
            {
              "at": "3E.9",
              "confidence": "read"
            },
            {
              "at": "5B.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DAT4",
          "active": "high",
          "pins": [
            {
              "at": "5E.7",
              "confidence": "read"
            },
            {
              "at": "5B.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DAT5",
          "active": "high",
          "pins": [
            {
              "at": "5E.9",
              "confidence": "read"
            },
            {
              "at": "5B.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DAT6",
          "active": "high",
          "pins": [
            {
              "at": "5D.7",
              "confidence": "read"
            },
            {
              "at": "5C.10",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DAT7",
          "active": "high",
          "pins": [
            {
              "at": "5D.9",
              "confidence": "read"
            },
            {
              "at": "5B.10",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_9B_2",
          "active": "none",
          "pins": [
            {
              "at": "5C.1",
              "confidence": "read"
            },
            {
              "at": "5B.1",
              "confidence": "read"
            }
          ],
          "note": "automatic name: bus driver enable, 1000 pF to ground"
        },
        {
          "name": "SR",
          "active": "low",
          "pins": [
            {
              "at": "7A.15",
              "confidence": "read"
            },
            {
              "at": "7A.1",
              "confidence": "read"
            }
          ],
          "note": "1000 pF to ground"
        },
        {
          "name": "IDATA0",
          "active": "high",
          "pins": [
            {
              "at": "3D.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IDATA1",
          "active": "high",
          "pins": [
            {
              "at": "3D.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IDATA2",
          "active": "high",
          "pins": [
            {
              "at": "3E.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IDATA3",
          "active": "high",
          "pins": [
            {
              "at": "3E.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IDATA4",
          "active": "high",
          "pins": [
            {
              "at": "5E.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IDATA5",
          "active": "high",
          "pins": [
            {
              "at": "5E.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IDATA6",
          "active": "high",
          "pins": [
            {
              "at": "5D.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IDATA7",
          "active": "high",
          "pins": [
            {
              "at": "5D.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IDENT2",
          "active": "high",
          "pins": [
            {
              "at": "3E.6",
              "confidence": "read"
            }
          ],
          "note": "ident switch 1E position 1 with pull-up"
        },
        {
          "name": "IDENT3",
          "active": "high",
          "pins": [
            {
              "at": "3E.10",
              "confidence": "read"
            }
          ],
          "note": "ident switch 1E position 2 with pull-up"
        },
        {
          "name": "IDENT4",
          "active": "high",
          "pins": [
            {
              "at": "5E.6",
              "confidence": "read"
            }
          ],
          "note": "ident switch 1E position 3 with pull-up"
        },
        {
          "name": "IDENT5",
          "active": "high",
          "pins": [
            {
              "at": "5E.10",
              "confidence": "read"
            }
          ],
          "note": "ident switch 1E position 4 with pull-up"
        },
        {
          "name": "OACTIVE",
          "active": "high",
          "pins": [
            {
              "at": "3E.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IACTIVE",
          "active": "high",
          "pins": [
            {
              "at": "3E.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IPE",
          "active": "high",
          "pins": [
            {
              "at": "7E.3",
              "confidence": "read"
            },
            {
              "at": "5D.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IOR",
          "active": "high",
          "pins": [
            {
              "at": "7E.13",
              "confidence": "read"
            },
            {
              "at": "5D.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ISYNERR",
          "active": "high",
          "pins": [
            {
              "at": "5E.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CARRERR",
          "active": "high",
          "pins": [
            {
              "at": "5B.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CARREN",
          "active": "high",
          "pins": [
            {
              "at": "5B.14",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "TDATA",
          "active": "high",
          "pins": [
            {
              "at": "7E.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "TBMT",
          "active": "high",
          "pins": [
            {
              "at": "7E.10",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DA",
          "active": "high",
          "pins": [
            {
              "at": "7E.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "PE",
          "active": "high",
          "pins": [
            {
              "at": "7E.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "OR",
          "active": "high",
          "pins": [
            {
              "at": "7E.14",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "SCT",
          "active": "high",
          "pins": [
            {
              "at": "7E.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CDSTL",
          "active": "low",
          "pins": [
            {
              "at": "7A.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CARR",
          "active": "low",
          "pins": [
            {
              "at": "7A.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "HALF",
          "active": "high",
          "pins": [
            {
              "at": "7A.10",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "RFS",
          "active": "low",
          "pins": [
            {
              "at": "7A.14",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "TDA",
          "active": "high",
          "pins": [
            {
              "at": "7E.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "RQTS",
          "active": "low",
          "pins": [
            {
              "at": "7A.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DSR",
          "active": "low",
          "pins": [
            {
              "at": "7A.12",
              "confidence": "read"
            }
          ]
        }
      ]
    },
    {
      "name": "interrupt-logic",
      "ports": [
        {
          "name": "BD_1",
          "direction": "input",
          "signals": [
            {
              "net": "BD0",
              "active": "high"
            },
            {
              "net": "BD1",
              "active": "high"
            }
          ]
        },
        {
          "name": "LEV_0",
          "direction": "inout",
          "signals": [
            {
              "net": "LEV10",
              "active": "low"
            },
            {
              "net": "LEV12",
              "active": "low"
            }
          ]
        },
        {
          "name": "DEQL",
          "direction": "input",
          "signals": [
            {
              "net": "DEQL",
              "active": "high"
            }
          ]
        },
        {
          "name": "BA1_1",
          "direction": "input",
          "signals": [
            {
              "net": "BA1",
              "active": "high"
            }
          ]
        },
        {
          "name": "OCW",
          "direction": "input",
          "signals": [
            {
              "net": "OCW",
              "active": "low"
            }
          ]
        },
        {
          "name": "ICW_0",
          "direction": "input",
          "signals": [
            {
              "net": "ICW",
              "active": "low"
            }
          ]
        },
        {
          "name": "GND",
          "direction": "input",
          "signals": [
            {
              "net": "GND"
            }
          ]
        },
        {
          "name": "IRFTIE",
          "direction": "output",
          "signals": [
            {
              "net": "IRFTIE",
              "active": "high"
            }
          ]
        },
        {
          "name": "IERRIE",
          "direction": "output",
          "signals": [
            {
              "net": "IERRIE",
              "active": "high"
            }
          ]
        },
        {
          "name": "ORFTIE",
          "direction": "output",
          "signals": [
            {
              "net": "ORFTIE",
              "active": "high"
            }
          ]
        },
        {
          "name": "OERRIE",
          "direction": "output",
          "signals": [
            {
              "net": "OERRIE",
              "active": "high"
            }
          ]
        },
        {
          "name": "IRFT",
          "direction": "input",
          "signals": [
            {
              "net": "IRFT",
              "active": "high"
            }
          ]
        },
        {
          "name": "IERR",
          "direction": "input",
          "signals": [
            {
              "net": "IERR",
              "active": "high"
            }
          ]
        },
        {
          "name": "ORFT",
          "direction": "input",
          "signals": [
            {
              "net": "ORFT",
              "active": "high"
            }
          ]
        },
        {
          "name": "OERR",
          "direction": "input",
          "signals": [
            {
              "net": "OERR",
              "active": "high"
            }
          ]
        },
        {
          "name": "N_3B_8",
          "direction": "output",
          "signals": [
            {
              "net": "N_3B_8"
            }
          ]
        },
        {
          "name": "LEV12_1",
          "direction": "input",
          "signals": [
            {
              "net": "LEV12",
              "active": "high"
            }
          ]
        },
        {
          "name": "LEV10_1",
          "direction": "output",
          "signals": [
            {
              "net": "LEV10",
              "active": "high"
            }
          ]
        },
        {
          "name": "INT",
          "direction": "input",
          "signals": [
            {
              "net": "INT",
              "active": "high"
            }
          ]
        },
        {
          "name": "DINPUT",
          "direction": "input",
          "signals": [
            {
              "net": "DINPUT",
              "active": "high"
            }
          ]
        },
        {
          "name": "TINPUT",
          "direction": "input",
          "signals": [
            {
              "net": "TINPUT",
              "active": "high"
            }
          ]
        },
        {
          "name": "INPUT",
          "direction": "inout",
          "signals": [
            {
              "net": "INPUT",
              "active": "low"
            }
          ]
        },
        {
          "name": "CONNECT",
          "direction": "inout",
          "signals": [
            {
              "net": "CONNECT",
              "active": "low"
            }
          ]
        },
        {
          "name": "INIDENT",
          "direction": "input",
          "signals": [
            {
              "net": "INIDENT",
              "active": "low"
            }
          ]
        },
        {
          "name": "OUTIDENT",
          "direction": "inout",
          "signals": [
            {
              "net": "OUTIDENT",
              "active": "low"
            }
          ]
        },
        {
          "name": "CL_1",
          "direction": "input",
          "signals": [
            {
              "net": "CL",
              "active": "high"
            }
          ]
        }
      ],
      "parts": [
        {
          "ref": "1C",
          "type": "74157",
          "function": "clears the interrupt enable that answered IDENT",
          "confidence": "read",
          "note": "corrected 2026-11-23: the box is printed 74157 (earlier entry said 74153); pins 14,13\u003E12 11,10\u003E9 2,3\u003E4 5,6\u003E7, select 1 = BA1, strobe 15 = CL"
        },
        {
          "ref": "3C",
          "type": "7474",
          "function": "character interrupt enables IRFTIE, ORFTIE",
          "confidence": "read"
        },
        {
          "ref": "1D",
          "type": "7474",
          "function": "error interrupt enables IERRIE, OERRIE",
          "confidence": "read"
        },
        {
          "ref": "3B",
          "type": "7451",
          "function": "level 12 request (gate 1, pins 9,10,13,1\u003E8) and level 10 request (gate 2, pins 3,2,5,4\u003E6)",
          "confidence": "read",
          "note": "one 7451 holds both AND-OR-INVERT gates; the parts list has 1 x 7451"
        },
        {
          "ref": "5A",
          "type": "7438",
          "function": "open-collector NAND buffers driving LEV12, LEV10, INPUT and CONNECT onto the bus",
          "confidence": "read",
          "note": "corrected 2026-11-23: drawn as a pointed box with a bar (7438 shape), pins 9,10\u003E8 1,2\u003E3 4,5\u003E6 12,13\u003E11; the earlier entry said 7451. The parts list has one 7451 and one 7438."
        },
        {
          "ref": "1B",
          "type": "7400",
          "function": "NAND gates: feed the IDENT clear multiplexer 1C",
          "confidence": "read"
        },
        {
          "ref": "3A",
          "type": "7402",
          "function": "NOR gates: interrupt level combine, constant H1, OUTIDENT",
          "confidence": "read"
        },
        {
          "ref": "1A",
          "type": "74S157",
          "function": "IDENT answer multiplexer: CLINT, TINPUT, TCONNECT, TINT",
          "confidence": "read",
          "note": "printed 74S157 in the box; the parts list has 1 x 745157 = 74S157"
        }
      ],
      "gates": [
        {
          "id": "1B/1",
          "part": "1B",
          "kind": "nand",
          "inputs": [
            2,
            1
          ],
          "output": 3,
          "confidence": "read"
        },
        {
          "id": "1B/2",
          "part": "1B",
          "kind": "nand",
          "inputs": [
            5,
            4
          ],
          "output": 6,
          "confidence": "read"
        },
        {
          "id": "1B/3",
          "part": "1B",
          "kind": "nand",
          "inputs": [
            9,
            10
          ],
          "output": 8,
          "confidence": "read"
        },
        {
          "id": "1B/4",
          "part": "1B",
          "kind": "nand",
          "inputs": [
            12,
            13
          ],
          "output": 11,
          "confidence": "read"
        },
        {
          "id": "3A/1",
          "part": "3A",
          "kind": "nor",
          "inputs": [
            3,
            2
          ],
          "output": 1,
          "confidence": "read"
        },
        {
          "id": "3A/2",
          "part": "3A",
          "kind": "nor",
          "inputs": [
            5,
            6
          ],
          "output": 4,
          "confidence": "read"
        },
        {
          "id": "3A/3",
          "part": "3A",
          "kind": "nor",
          "inputs": [
            8,
            9
          ],
          "output": 10,
          "confidence": "read"
        },
        {
          "id": "3A/4",
          "part": "3A",
          "kind": "nor",
          "inputs": [
            11,
            12
          ],
          "output": 13,
          "confidence": "read"
        },
        {
          "id": "5A/1",
          "part": "5A",
          "kind": "nand",
          "inputs": [
            1,
            2
          ],
          "output": 3,
          "confidence": "read",
          "note": "open collector"
        },
        {
          "id": "5A/2",
          "part": "5A",
          "kind": "nand",
          "inputs": [
            4,
            5
          ],
          "output": 6,
          "confidence": "read",
          "note": "open collector"
        },
        {
          "id": "5A/3",
          "part": "5A",
          "kind": "nand",
          "inputs": [
            9,
            10
          ],
          "output": 8,
          "confidence": "read",
          "note": "open collector"
        },
        {
          "id": "5A/4",
          "part": "5A",
          "kind": "nand",
          "inputs": [
            12,
            13
          ],
          "output": 11,
          "confidence": "read",
          "note": "open collector"
        },
        {
          "id": "3B/1",
          "part": "3B",
          "kind": "and-or-invert",
          "inputs": [
            9,
            10,
            13,
            1
          ],
          "output": 8,
          "confidence": "read",
          "note": "AND-OR-INVERT: NOT((9 AND 10) OR (13 AND 1)), level 12"
        },
        {
          "id": "3B/2",
          "part": "3B",
          "kind": "and-or-invert",
          "inputs": [
            3,
            2,
            5,
            4
          ],
          "output": 6,
          "confidence": "read",
          "note": "AND-OR-INVERT: NOT((3 AND 2) OR (5 AND 4)), level 10"
        }
      ],
      "nets": [
        {
          "name": "DEQL",
          "active": "high",
          "pins": [
            {
              "at": "1A.3",
              "confidence": "read"
            }
          ],
          "note": "1 kohm pull-up to \u002B5 V on the line"
        },
        {
          "name": "BA1",
          "active": "high",
          "pins": [
            {
              "at": "1C.1",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "OCW",
          "active": "low",
          "pins": [
            {
              "at": "3C.3",
              "confidence": "read"
            },
            {
              "at": "1D.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ICW",
          "active": "low",
          "pins": [
            {
              "at": "3C.11",
              "confidence": "read",
              "note": "corrected 2026-11-23: the clock pins of 3C and 1D take ICW with subscript 0, as drawn at the label; see the flat sheet"
            },
            {
              "at": "1D.11",
              "confidence": "read",
              "note": "corrected 2026-11-23: the clock pins of 3C and 1D take ICW with subscript 0, as drawn at the label; see the flat sheet"
            }
          ]
        },
        {
          "name": "H1",
          "active": "high",
          "pins": [
            {
              "at": "3A.10",
              "confidence": "read"
            },
            {
              "at": "3C.10",
              "confidence": "read"
            },
            {
              "at": "1D.10",
              "confidence": "read"
            },
            {
              "at": "3C.4",
              "confidence": "read"
            },
            {
              "at": "1D.4",
              "confidence": "read"
            },
            {
              "at": "1C.14",
              "confidence": "read"
            },
            {
              "at": "1C.11",
              "confidence": "read"
            },
            {
              "at": "1C.3",
              "confidence": "read"
            },
            {
              "at": "1C.6",
              "confidence": "read"
            }
          ],
          "note": "7402 3A pins 8 and 9 grounded, so pin 10 is a constant 1"
        },
        {
          "name": "GND",
          "active": "none",
          "pins": [
            {
              "at": "3A.8",
              "confidence": "read"
            },
            {
              "at": "3A.9",
              "confidence": "read"
            },
            {
              "at": "1A.6",
              "confidence": "read"
            },
            {
              "at": "1A.15",
              "confidence": "read"
            }
          ],
          "note": "3D pins 10 and 6 are marked (STRAP)(GND): ident bits 0-1. 13C pins 5, 3, 11, 13 read by the rule that a pin number sits above its wire. COM 2601 pin 4 is printed in its box as GND"
        },
        {
          "name": "BD0",
          "active": "high",
          "pins": [
            {
              "at": "3C.12",
              "confidence": "read"
            },
            {
              "at": "3C.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BD1",
          "active": "high",
          "pins": [
            {
              "at": "1D.12",
              "confidence": "read"
            },
            {
              "at": "1D.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CLDI",
          "active": "low",
          "pins": [
            {
              "at": "1C.12",
              "confidence": "read"
            },
            {
              "at": "3C.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CLEI",
          "active": "low",
          "pins": [
            {
              "at": "1C.9",
              "confidence": "read"
            },
            {
              "at": "1D.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CLDO",
          "active": "low",
          "pins": [
            {
              "at": "1C.4",
              "confidence": "read"
            },
            {
              "at": "3C.1",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CLEO",
          "active": "low",
          "pins": [
            {
              "at": "1C.7",
              "confidence": "read"
            },
            {
              "at": "1D.1",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IRFTIE",
          "active": "high",
          "pins": [
            {
              "at": "3C.9",
              "confidence": "read"
            },
            {
              "at": "3B.9",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IERRIE",
          "active": "high",
          "pins": [
            {
              "at": "1D.9",
              "confidence": "read"
            },
            {
              "at": "3B.1",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ORFTIE",
          "active": "high",
          "pins": [
            {
              "at": "3C.5",
              "confidence": "read"
            },
            {
              "at": "3B.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "OERRIE",
          "active": "high",
          "pins": [
            {
              "at": "1D.5",
              "confidence": "read"
            },
            {
              "at": "3B.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IRFT",
          "active": "high",
          "pins": [
            {
              "at": "3B.10",
              "confidence": "read"
            },
            {
              "at": "1B.10",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "IERR",
          "active": "high",
          "pins": [
            {
              "at": "3B.13",
              "confidence": "read"
            },
            {
              "at": "1B.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "ORFT",
          "active": "high",
          "pins": [
            {
              "at": "3B.2",
              "confidence": "read"
            },
            {
              "at": "1B.1",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "OERR",
          "active": "high",
          "pins": [
            {
              "at": "3B.5",
              "confidence": "read"
            },
            {
              "at": "1B.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_3B_8",
          "active": "none",
          "pins": [
            {
              "at": "3B.8",
              "confidence": "read"
            }
          ],
          "note": "automatic name: level 12 AND-OR-INVERT term"
        },
        {
          "name": "LEV12",
          "active": "high",
          "pins": [
            {
              "at": "5A.9",
              "confidence": "read"
            },
            {
              "at": "5A.10",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "LEV12",
          "active": "low",
          "drive": "open-collector",
          "driveConfidence": "inferred",
          "pullUp": "open",
          "pins": [
            {
              "at": "5A.8",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_3B_6",
          "active": "none",
          "pins": [
            {
              "at": "3B.6",
              "confidence": "read"
            },
            {
              "at": "3A.12",
              "confidence": "read"
            },
            {
              "at": "3A.11",
              "confidence": "read"
            }
          ],
          "note": "automatic name: level 10 AND-OR-INVERT term"
        },
        {
          "name": "LEV10",
          "active": "high",
          "pins": [
            {
              "at": "3A.13",
              "confidence": "read"
            },
            {
              "at": "5A.1",
              "confidence": "read"
            },
            {
              "at": "5A.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "LEV10",
          "active": "low",
          "drive": "open-collector",
          "driveConfidence": "inferred",
          "pullUp": "open",
          "pins": [
            {
              "at": "5A.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CLINT",
          "active": "high",
          "pins": [
            {
              "at": "1A.7",
              "confidence": "read"
            },
            {
              "at": "1B.9",
              "confidence": "read"
            },
            {
              "at": "1B.12",
              "confidence": "read"
            },
            {
              "at": "1B.2",
              "confidence": "read"
            },
            {
              "at": "1B.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_1B_8",
          "active": "none",
          "pins": [
            {
              "at": "1B.8",
              "confidence": "read"
            },
            {
              "at": "1C.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_1B_11",
          "active": "none",
          "pins": [
            {
              "at": "1B.11",
              "confidence": "read"
            },
            {
              "at": "1C.10",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_1B_3",
          "active": "none",
          "pins": [
            {
              "at": "1B.3",
              "confidence": "read"
            },
            {
              "at": "1C.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_1B_6",
          "active": "none",
          "pins": [
            {
              "at": "1B.6",
              "confidence": "read"
            },
            {
              "at": "1C.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "INT",
          "active": "high",
          "pins": [
            {
              "at": "1A.10",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DINPUT",
          "active": "high",
          "pins": [
            {
              "at": "1A.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "TINT",
          "active": "high",
          "pins": [
            {
              "at": "1A.9",
              "confidence": "read"
            },
            {
              "at": "1A.5",
              "confidence": "read"
            },
            {
              "at": "1A.14",
              "confidence": "read"
            },
            {
              "at": "1A.2",
              "confidence": "read"
            },
            {
              "at": "1A.11",
              "confidence": "read"
            },
            {
              "at": "3A.3",
              "confidence": "read"
            }
          ],
          "note": "1A output 9 is fed back to its own inputs"
        },
        {
          "name": "TINPUT",
          "active": "high",
          "pins": [
            {
              "at": "1A.12",
              "confidence": "read"
            },
            {
              "at": "5A.4",
              "confidence": "read"
            },
            {
              "at": "5A.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "TCONNECT",
          "active": "high",
          "pins": [
            {
              "at": "1A.4",
              "confidence": "read"
            },
            {
              "at": "5A.12",
              "confidence": "read"
            },
            {
              "at": "5A.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "INPUT",
          "active": "low",
          "drive": "open-collector",
          "driveConfidence": "inferred",
          "pullUp": "open",
          "pins": [
            {
              "at": "5A.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CONNECT",
          "active": "low",
          "drive": "open-collector",
          "driveConfidence": "inferred",
          "pullUp": "open",
          "pins": [
            {
              "at": "5A.11",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "INIDENT",
          "active": "low",
          "pins": [
            {
              "at": "1A.1",
              "confidence": "read"
            },
            {
              "at": "3A.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_3A_1",
          "active": "none",
          "pins": [
            {
              "at": "3A.1",
              "confidence": "read"
            },
            {
              "at": "3A.6",
              "confidence": "read"
            },
            {
              "at": "3A.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "OUTIDENT",
          "active": "low",
          "pins": [
            {
              "at": "3A.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CL",
          "active": "high",
          "pins": [
            {
              "at": "1C.15",
              "confidence": "read"
            }
          ]
        }
      ]
    },
    {
      "name": "modem-interface",
      "ports": [
        {
          "name": "H",
          "direction": "input",
          "signals": [
            {
              "net": "H",
              "active": "high"
            }
          ]
        },
        {
          "name": "GND",
          "direction": "input",
          "signals": [
            {
              "net": "GND"
            }
          ]
        },
        {
          "name": "TCLOCK",
          "direction": "output",
          "signals": [
            {
              "net": "TCLOCK",
              "active": "high"
            }
          ]
        },
        {
          "name": "RCLOCK",
          "direction": "output",
          "signals": [
            {
              "net": "RCLOCK",
              "active": "high"
            }
          ]
        },
        {
          "name": "RDATA",
          "direction": "output",
          "signals": [
            {
              "net": "RDATA",
              "active": "high"
            }
          ]
        },
        {
          "name": "CDSTL_0",
          "direction": "input",
          "signals": [
            {
              "net": "CDSTL",
              "active": "low"
            }
          ]
        },
        {
          "name": "CARR_0",
          "direction": "output",
          "signals": [
            {
              "net": "CARR",
              "active": "low"
            }
          ]
        },
        {
          "name": "CARR_1",
          "direction": "output",
          "signals": [
            {
              "net": "CARR",
              "active": "high"
            }
          ]
        },
        {
          "name": "TTM",
          "direction": "output",
          "signals": [
            {
              "net": "TTM",
              "active": "low"
            }
          ]
        },
        {
          "name": "TRQTS",
          "direction": "input",
          "signals": [
            {
              "net": "TRQTS",
              "active": "low"
            }
          ]
        },
        {
          "name": "RFS",
          "direction": "output",
          "signals": [
            {
              "net": "RFS",
              "active": "low"
            }
          ]
        },
        {
          "name": "N_13B_6",
          "direction": "input",
          "signals": [
            {
              "net": "N_13B_6"
            }
          ]
        },
        {
          "name": "RTM",
          "direction": "output",
          "signals": [
            {
              "net": "RTM",
              "active": "low"
            }
          ]
        },
        {
          "name": "N_13B_3",
          "direction": "input",
          "signals": [
            {
              "net": "N_13B_3"
            }
          ]
        },
        {
          "name": "BFQ1",
          "direction": "input",
          "signals": [
            {
              "net": "BFQ1",
              "active": "high"
            }
          ]
        },
        {
          "name": "TDA",
          "direction": "input",
          "signals": [
            {
              "net": "TDA",
              "active": "high"
            }
          ]
        },
        {
          "name": "TEST",
          "direction": "input",
          "signals": [
            {
              "net": "TEST",
              "active": "high"
            }
          ]
        },
        {
          "name": "RQTS",
          "direction": "output",
          "signals": [
            {
              "net": "RQTS",
              "active": "low"
            }
          ]
        },
        {
          "name": "DSR",
          "direction": "output",
          "signals": [
            {
              "net": "DSR",
              "active": "low"
            }
          ]
        },
        {
          "name": "CAL",
          "direction": "output",
          "signals": [
            {
              "net": "CAL",
              "active": "low"
            }
          ]
        },
        {
          "name": "RECEIVED DATA",
          "direction": "input",
          "signals": [
            {
              "net": "RECEIVED DATA"
            }
          ]
        },
        {
          "name": "READY FOR SENDING",
          "direction": "input",
          "signals": [
            {
              "net": "READY FOR SENDING"
            }
          ]
        },
        {
          "name": "DATA SET READY",
          "direction": "input",
          "signals": [
            {
              "net": "DATA SET READY"
            }
          ]
        },
        {
          "name": "SIGNAL DETECTOR",
          "direction": "input",
          "signals": [
            {
              "net": "SIGNAL DETECTOR"
            }
          ]
        },
        {
          "name": "CALLING INDICATOR",
          "direction": "input",
          "signals": [
            {
              "net": "CALLING INDICATOR"
            }
          ]
        },
        {
          "name": "TRANSMITTER TIMING",
          "direction": "input",
          "signals": [
            {
              "net": "TRANSMITTER TIMING"
            }
          ]
        },
        {
          "name": "RECEIVER TIMING",
          "direction": "input",
          "signals": [
            {
              "net": "RECEIVER TIMING"
            }
          ]
        },
        {
          "name": "CONNECT TO LINE",
          "direction": "inout",
          "signals": [
            {
              "net": "CONNECT TO LINE"
            }
          ]
        },
        {
          "name": "REQUEST TO SEND",
          "direction": "inout",
          "signals": [
            {
              "net": "REQUEST TO SEND"
            }
          ]
        },
        {
          "name": "V24_23",
          "direction": "inout",
          "signals": [
            {
              "net": "V24_23"
            }
          ]
        },
        {
          "name": "TRANSM DATA",
          "direction": "inout",
          "signals": [
            {
              "net": "TRANSM DATA"
            }
          ]
        }
      ],
      "parts": [
        {
          "ref": "15B",
          "type": "74157",
          "function": "test mode: swaps modem clocks and data for internal clock and loop-back",
          "confidence": "read"
        },
        {
          "ref": "13A",
          "type": "75154",
          "function": "line receivers: CALLING INDICATOR, TRANSMITTER TIMING, RECEIVER TIMING, CARR",
          "confidence": "read"
        },
        {
          "ref": "15A",
          "type": "75154",
          "function": "line receivers: RECEIVED DATA, READY FOR SENDING, DATA SET READY, SIGNAL DETECTOR",
          "confidence": "read"
        },
        {
          "ref": "17A",
          "type": "75150",
          "function": "line drivers: CONNECT TO LINE, REQUEST TO SEND",
          "confidence": "read"
        },
        {
          "ref": "19A",
          "type": "75150",
          "function": "line drivers: modem pin 93 and TRANSMITTED DATA",
          "confidence": "inferred",
          "note": "position code read as 19A; the 9 could also be a 3, but 13A already holds four 75154 receivers, so it cannot be 13A"
        }
      ],
      "gates": [
        {
          "id": "13A/1",
          "part": "13A",
          "kind": "not",
          "inputs": [
            4
          ],
          "output": 13,
          "confidence": "read",
          "note": "line receiver"
        },
        {
          "id": "13A/2",
          "part": "13A",
          "kind": "not",
          "inputs": [
            5
          ],
          "output": 12,
          "confidence": "read",
          "note": "line receiver"
        },
        {
          "id": "13A/3",
          "part": "13A",
          "kind": "not",
          "inputs": [
            6
          ],
          "output": 11,
          "confidence": "read",
          "note": "line receiver"
        },
        {
          "id": "13A/4",
          "part": "13A",
          "kind": "not",
          "inputs": [
            7
          ],
          "output": 10,
          "confidence": "read",
          "note": "line receiver"
        },
        {
          "id": "15A/1",
          "part": "15A",
          "kind": "not",
          "inputs": [
            4
          ],
          "output": 13,
          "confidence": "read",
          "note": "line receiver"
        },
        {
          "id": "15A/2",
          "part": "15A",
          "kind": "not",
          "inputs": [
            5
          ],
          "output": 12,
          "confidence": "read",
          "note": "line receiver"
        },
        {
          "id": "15A/3",
          "part": "15A",
          "kind": "not",
          "inputs": [
            6
          ],
          "output": 11,
          "confidence": "read",
          "note": "line receiver"
        },
        {
          "id": "15A/4",
          "part": "15A",
          "kind": "not",
          "inputs": [
            7
          ],
          "output": 10,
          "confidence": "read",
          "note": "line receiver"
        },
        {
          "id": "17A/1",
          "part": "17A",
          "kind": "nand",
          "inputs": [
            2,
            3
          ],
          "output": 12,
          "confidence": "read",
          "note": "line driver, pin 2 = common strobe S"
        },
        {
          "id": "17A/2",
          "part": "17A",
          "kind": "nand",
          "inputs": [
            2,
            4
          ],
          "output": 11,
          "confidence": "read",
          "note": "line driver, pin 2 = common strobe S"
        },
        {
          "id": "19A/1",
          "part": "19A",
          "kind": "nand",
          "inputs": [
            2,
            3
          ],
          "output": 12,
          "confidence": "inferred",
          "note": "line driver, pin 2 = common strobe S"
        },
        {
          "id": "19A/2",
          "part": "19A",
          "kind": "nand",
          "inputs": [
            2,
            4
          ],
          "output": 11,
          "confidence": "inferred",
          "note": "line driver, pin 2 = common strobe S"
        }
      ],
      "nets": [
        {
          "name": "H",
          "active": "high",
          "pins": [
            {
              "at": "15B.10",
              "confidence": "read"
            },
            {
              "at": "17A.2",
              "confidence": "read"
            },
            {
              "at": "19A.2",
              "confidence": "read"
            }
          ],
          "note": "tied high through resistor 15A9; pins marked H on the drawing"
        },
        {
          "name": "GND",
          "active": "none",
          "pins": [
            {
              "at": "15B.15",
              "confidence": "read"
            },
            {
              "at": "19A.4",
              "confidence": "read"
            }
          ],
          "note": "3D pins 10 and 6 are marked (STRAP)(GND): ident bits 0-1. 13C pins 5, 3, 11, 13 read by the rule that a pin number sits above its wire. COM 2601 pin 4 is printed in its box as GND"
        },
        {
          "name": "TCLOCK",
          "active": "high",
          "pins": [
            {
              "at": "15B.7",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "RCLOCK",
          "active": "high",
          "pins": [
            {
              "at": "15B.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "RDATA",
          "active": "high",
          "pins": [
            {
              "at": "15B.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CDSTL",
          "active": "low",
          "pins": [
            {
              "at": "17A.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CARR",
          "active": "low",
          "pins": [
            {
              "at": "15A.12",
              "confidence": "read"
            },
            {
              "at": "13A.4",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CARR",
          "active": "high",
          "pins": [
            {
              "at": "13A.13",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "TTM",
          "active": "low",
          "pins": [
            {
              "at": "13A.11",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "TRQTS",
          "active": "low",
          "pins": [
            {
              "at": "15B.11",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "RFS",
          "active": "low",
          "pins": [
            {
              "at": "15A.11",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_13B_6",
          "active": "none",
          "pins": [
            {
              "at": "15B.5",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "RTM",
          "active": "low",
          "pins": [
            {
              "at": "13A.12",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "N_13B_3",
          "active": "none",
          "pins": [
            {
              "at": "15B.2",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "BFQ1",
          "active": "high",
          "pins": [
            {
              "at": "15B.3",
              "confidence": "read"
            },
            {
              "at": "15B.6",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "RDA",
          "active": "high",
          "pins": [
            {
              "at": "15A.13",
              "confidence": "read"
            },
            {
              "at": "15B.14",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "TDA",
          "active": "high",
          "pins": [
            {
              "at": "15B.13",
              "confidence": "read"
            },
            {
              "at": "19A.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "TEST",
          "active": "high",
          "pins": [
            {
              "at": "15B.1",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "RQTS",
          "active": "low",
          "pins": [
            {
              "at": "15B.9",
              "confidence": "read"
            },
            {
              "at": "17A.3",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "DSR",
          "active": "low",
          "pins": [
            {
              "at": "15A.10",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "CAL",
          "active": "low",
          "pins": [
            {
              "at": "13A.10",
              "confidence": "read"
            }
          ]
        },
        {
          "name": "RECEIVED DATA",
          "active": "none",
          "pins": [
            {
              "at": "15A.4",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 3"
        },
        {
          "name": "READY FOR SENDING",
          "active": "none",
          "pins": [
            {
              "at": "15A.6",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 5"
        },
        {
          "name": "DATA SET READY",
          "active": "none",
          "pins": [
            {
              "at": "15A.7",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 6"
        },
        {
          "name": "SIGNAL DETECTOR",
          "active": "none",
          "pins": [
            {
              "at": "15A.5",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 8"
        },
        {
          "name": "CALLING INDICATOR",
          "active": "none",
          "pins": [
            {
              "at": "13A.7",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 22"
        },
        {
          "name": "TRANSMITTER TIMING",
          "active": "none",
          "pins": [
            {
              "at": "13A.6",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 15"
        },
        {
          "name": "RECEIVER TIMING",
          "active": "none",
          "pins": [
            {
              "at": "13A.5",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 17"
        },
        {
          "name": "CONNECT TO LINE",
          "active": "none",
          "pins": [
            {
              "at": "17A.11",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 20"
        },
        {
          "name": "REQUEST TO SEND",
          "active": "none",
          "pins": [
            {
              "at": "17A.12",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 4"
        },
        {
          "name": "V24_23",
          "active": "none",
          "pins": [
            {
              "at": "19A.11",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 23; no signal name printed. 19A pin 2 = H, pin 4 = GND"
        },
        {
          "name": "TRANSM DATA",
          "active": "none",
          "pins": [
            {
              "at": "19A.12",
              "confidence": "read"
            }
          ],
          "note": "V.24 pin 2; 2200 pF to ground"
        }
      ]
    }
  ],
  "instances": [
    {
      "name": "U1",
      "block": "address-decode",
      "connections": [
        {
          "port": "BA_0",
          "nets": [
            {
              "net": "BA0",
              "active": "low"
            },
            {
              "net": "BA1",
              "active": "low"
            },
            {
              "net": "BA2",
              "active": "low"
            },
            {
              "net": "BA3",
              "active": "low"
            },
            {
              "net": "BA4",
              "active": "low"
            },
            {
              "net": "BA5",
              "active": "low"
            },
            {
              "net": "BA6",
              "active": "low"
            },
            {
              "net": "BA7",
              "active": "low"
            },
            {
              "net": "BA8",
              "active": "low"
            },
            {
              "net": "BA9",
              "active": "low"
            }
          ]
        },
        {
          "port": "BA_1",
          "nets": [
            {
              "net": "BA0",
              "active": "high"
            },
            {
              "net": "BA1",
              "active": "high"
            }
          ]
        },
        {
          "port": "IOXE",
          "nets": [
            {
              "net": "IOXE",
              "active": "low"
            }
          ]
        },
        {
          "port": "DEQL",
          "nets": [
            {
              "net": "DEQL",
              "active": "high"
            }
          ]
        },
        {
          "port": "ODW",
          "nets": [
            {
              "net": "ODW",
              "active": "low"
            }
          ]
        },
        {
          "port": "OSR",
          "nets": [
            {
              "net": "OSR",
              "active": "low"
            }
          ]
        },
        {
          "port": "OCW",
          "nets": [
            {
              "net": "OCW",
              "active": "low"
            }
          ]
        },
        {
          "port": "IDR_0",
          "nets": [
            {
              "net": "IDR",
              "active": "low"
            }
          ]
        },
        {
          "port": "SCW",
          "nets": [
            {
              "net": "SCW",
              "active": "low"
            }
          ]
        },
        {
          "port": "ISR",
          "nets": [
            {
              "net": "ISR",
              "active": "low"
            }
          ]
        },
        {
          "port": "ICW_0",
          "nets": [
            {
              "net": "ICW",
              "active": "low"
            }
          ]
        },
        {
          "port": "ICW_1",
          "nets": [
            {
              "net": "ICW",
              "active": "high"
            }
          ]
        },
        {
          "port": "H",
          "nets": [
            {
              "net": "H",
              "active": "high"
            }
          ]
        },
        {
          "port": "GND",
          "nets": [
            {
              "net": "GND"
            }
          ]
        },
        {
          "port": "BD10",
          "nets": [
            {
              "net": "BD10",
              "active": "low"
            }
          ]
        },
        {
          "port": "IERR",
          "nets": [
            {
              "net": "IERR",
              "active": "high"
            }
          ]
        },
        {
          "port": "N_11E_8",
          "nets": [
            {
              "net": "N_11E_8"
            }
          ]
        },
        {
          "port": "N_11E_11",
          "nets": [
            {
              "net": "N_11E_11"
            }
          ]
        },
        {
          "port": "IPE",
          "nets": [
            {
              "net": "IPE",
              "active": "high"
            }
          ]
        },
        {
          "port": "IOR",
          "nets": [
            {
              "net": "IOR",
              "active": "high"
            }
          ]
        },
        {
          "port": "ISYNERR",
          "nets": [
            {
              "net": "ISYNERR",
              "active": "high"
            }
          ]
        },
        {
          "port": "CARRERR",
          "nets": [
            {
              "net": "CARRERR",
              "active": "high"
            }
          ]
        },
        {
          "port": "CL_1",
          "nets": [
            {
              "net": "CL",
              "active": "high"
            }
          ]
        },
        {
          "port": "CL1",
          "nets": [
            {
              "net": "CL1",
              "active": "low"
            }
          ]
        },
        {
          "port": "N_9E_1",
          "nets": [
            {
              "net": "N_9E_1"
            }
          ]
        }
      ]
    },
    {
      "name": "U2",
      "block": "read-multiplexers",
      "connections": [
        {
          "port": "BA_1",
          "nets": [
            {
              "net": "BA0",
              "active": "high"
            },
            {
              "net": "BA1",
              "active": "high"
            }
          ]
        },
        {
          "port": "BD_0",
          "nets": [
            {
              "net": "BD0",
              "active": "low"
            },
            {
              "net": "BD1",
              "active": "low"
            },
            {
              "net": "BD2",
              "active": "low"
            },
            {
              "net": "BD3",
              "active": "low"
            },
            {
              "net": "BD4",
              "active": "low"
            },
            {
              "net": "BD5",
              "active": "low"
            },
            {
              "net": "BD6",
              "active": "low"
            },
            {
              "net": "BD7",
              "active": "low"
            },
            {
              "net": "BD8",
              "active": "low"
            },
            {
              "net": "BD9",
              "active": "low"
            },
            {
              "net": "BD10",
              "active": "low"
            },
            {
              "net": "BD11",
              "active": "low"
            },
            {
              "net": "BD12",
              "active": "low"
            },
            {
              "net": "BD13",
              "active": "low"
            },
            {
              "net": "BD14",
              "active": "low"
            },
            {
              "net": "BD15",
              "active": "low"
            }
          ]
        },
        {
          "port": "BD_1",
          "nets": [
            {
              "net": "BD0",
              "active": "high"
            },
            {
              "net": "BD1",
              "active": "high"
            }
          ]
        },
        {
          "port": "LEV_1",
          "nets": [
            {
              "net": "LEV10",
              "active": "high"
            },
            {
              "net": "LEV12",
              "active": "high"
            }
          ]
        },
        {
          "port": "IDATA_1",
          "nets": [
            {
              "net": "IDATA0",
              "active": "high"
            },
            {
              "net": "IDATA1",
              "active": "high"
            },
            {
              "net": "IDATA2",
              "active": "high"
            },
            {
              "net": "IDATA3",
              "active": "high"
            },
            {
              "net": "IDATA4",
              "active": "high"
            },
            {
              "net": "IDATA5",
              "active": "high"
            },
            {
              "net": "IDATA6",
              "active": "high"
            },
            {
              "net": "IDATA7",
              "active": "high"
            }
          ]
        },
        {
          "port": "DEQL",
          "nets": [
            {
              "net": "DEQL",
              "active": "high"
            }
          ]
        },
        {
          "port": "ISR",
          "nets": [
            {
              "net": "ISR",
              "active": "low"
            }
          ]
        },
        {
          "port": "GND",
          "nets": [
            {
              "net": "GND"
            }
          ]
        },
        {
          "port": "IRFTIE",
          "nets": [
            {
              "net": "IRFTIE",
              "active": "high"
            }
          ]
        },
        {
          "port": "IERRIE",
          "nets": [
            {
              "net": "IERRIE",
              "active": "high"
            }
          ]
        },
        {
          "port": "ORFTIE",
          "nets": [
            {
              "net": "ORFTIE",
              "active": "high"
            }
          ]
        },
        {
          "port": "OERRIE",
          "nets": [
            {
              "net": "OERRIE",
              "active": "high"
            }
          ]
        },
        {
          "port": "IRFT",
          "nets": [
            {
              "net": "IRFT",
              "active": "high"
            }
          ]
        },
        {
          "port": "IERR",
          "nets": [
            {
              "net": "IERR",
              "active": "high"
            }
          ]
        },
        {
          "port": "ORFT",
          "nets": [
            {
              "net": "ORFT",
              "active": "high"
            }
          ]
        },
        {
          "port": "OERR",
          "nets": [
            {
              "net": "OERR",
              "active": "high"
            }
          ]
        },
        {
          "port": "INT",
          "nets": [
            {
              "net": "INT",
              "active": "high"
            }
          ]
        },
        {
          "port": "DINPUT",
          "nets": [
            {
              "net": "DINPUT",
              "active": "high"
            }
          ]
        },
        {
          "port": "N_11E_8",
          "nets": [
            {
              "net": "N_11E_8"
            }
          ]
        },
        {
          "port": "N_11E_11",
          "nets": [
            {
              "net": "N_11E_11"
            }
          ]
        },
        {
          "port": "N_9B_2",
          "nets": [
            {
              "net": "N_9B_2"
            }
          ]
        },
        {
          "port": "SR",
          "nets": [
            {
              "net": "SR",
              "active": "low"
            }
          ]
        },
        {
          "port": "OACTIVE",
          "nets": [
            {
              "net": "OACTIVE",
              "active": "high"
            }
          ]
        },
        {
          "port": "IACTIVE",
          "nets": [
            {
              "net": "IACTIVE",
              "active": "high"
            }
          ]
        },
        {
          "port": "IPE",
          "nets": [
            {
              "net": "IPE",
              "active": "high"
            }
          ]
        },
        {
          "port": "IOR",
          "nets": [
            {
              "net": "IOR",
              "active": "high"
            }
          ]
        },
        {
          "port": "ISYNERR",
          "nets": [
            {
              "net": "ISYNERR",
              "active": "high"
            }
          ]
        },
        {
          "port": "CARRERR",
          "nets": [
            {
              "net": "CARRERR",
              "active": "high"
            }
          ]
        },
        {
          "port": "CARREN",
          "nets": [
            {
              "net": "CARREN",
              "active": "high"
            }
          ]
        },
        {
          "port": "TDATA",
          "nets": [
            {
              "net": "TDATA",
              "active": "high"
            }
          ]
        },
        {
          "port": "TBMT",
          "nets": [
            {
              "net": "TBMT",
              "active": "high"
            }
          ]
        },
        {
          "port": "DA",
          "nets": [
            {
              "net": "DA",
              "active": "high"
            }
          ]
        },
        {
          "port": "PE",
          "nets": [
            {
              "net": "PE",
              "active": "high"
            }
          ]
        },
        {
          "port": "OR",
          "nets": [
            {
              "net": "OR",
              "active": "high"
            }
          ]
        },
        {
          "port": "SCT",
          "nets": [
            {
              "net": "SCT",
              "active": "high"
            }
          ]
        },
        {
          "port": "CDSTL_0",
          "nets": [
            {
              "net": "CDSTL",
              "active": "low"
            }
          ]
        },
        {
          "port": "CARR_0",
          "nets": [
            {
              "net": "CARR",
              "active": "low"
            }
          ]
        },
        {
          "port": "HALF",
          "nets": [
            {
              "net": "HALF",
              "active": "high"
            }
          ]
        },
        {
          "port": "RFS",
          "nets": [
            {
              "net": "RFS",
              "active": "low"
            }
          ]
        },
        {
          "port": "TDA",
          "nets": [
            {
              "net": "TDA",
              "active": "high"
            }
          ]
        },
        {
          "port": "RQTS",
          "nets": [
            {
              "net": "RQTS",
              "active": "low"
            }
          ]
        },
        {
          "port": "DSR",
          "nets": [
            {
              "net": "DSR",
              "active": "low"
            }
          ]
        }
      ]
    },
    {
      "name": "U3",
      "block": "interrupt-logic",
      "connections": [
        {
          "port": "BD_1",
          "nets": [
            {
              "net": "BD0",
              "active": "high"
            },
            {
              "net": "BD1",
              "active": "high"
            }
          ]
        },
        {
          "port": "LEV_0",
          "nets": [
            {
              "net": "LEV10",
              "active": "low"
            },
            {
              "net": "LEV12",
              "active": "low"
            }
          ]
        },
        {
          "port": "DEQL",
          "nets": [
            {
              "net": "DEQL",
              "active": "high"
            }
          ]
        },
        {
          "port": "BA1_1",
          "nets": [
            {
              "net": "BA1",
              "active": "high"
            }
          ]
        },
        {
          "port": "OCW",
          "nets": [
            {
              "net": "OCW",
              "active": "low"
            }
          ]
        },
        {
          "port": "ICW_0",
          "nets": [
            {
              "net": "ICW",
              "active": "low"
            }
          ]
        },
        {
          "port": "GND",
          "nets": [
            {
              "net": "GND"
            }
          ]
        },
        {
          "port": "IRFTIE",
          "nets": [
            {
              "net": "IRFTIE",
              "active": "high"
            }
          ]
        },
        {
          "port": "IERRIE",
          "nets": [
            {
              "net": "IERRIE",
              "active": "high"
            }
          ]
        },
        {
          "port": "ORFTIE",
          "nets": [
            {
              "net": "ORFTIE",
              "active": "high"
            }
          ]
        },
        {
          "port": "OERRIE",
          "nets": [
            {
              "net": "OERRIE",
              "active": "high"
            }
          ]
        },
        {
          "port": "IRFT",
          "nets": [
            {
              "net": "IRFT",
              "active": "high"
            }
          ]
        },
        {
          "port": "IERR",
          "nets": [
            {
              "net": "IERR",
              "active": "high"
            }
          ]
        },
        {
          "port": "ORFT",
          "nets": [
            {
              "net": "ORFT",
              "active": "high"
            }
          ]
        },
        {
          "port": "OERR",
          "nets": [
            {
              "net": "OERR",
              "active": "high"
            }
          ]
        },
        {
          "port": "N_3B_8",
          "nets": [
            {
              "net": "N_3B_8"
            }
          ]
        },
        {
          "port": "LEV12_1",
          "nets": [
            {
              "net": "LEV12",
              "active": "high"
            }
          ]
        },
        {
          "port": "LEV10_1",
          "nets": [
            {
              "net": "LEV10",
              "active": "high"
            }
          ]
        },
        {
          "port": "INT",
          "nets": [
            {
              "net": "INT",
              "active": "high"
            }
          ]
        },
        {
          "port": "DINPUT",
          "nets": [
            {
              "net": "DINPUT",
              "active": "high"
            }
          ]
        },
        {
          "port": "TINPUT",
          "nets": [
            {
              "net": "TINPUT",
              "active": "high"
            }
          ]
        },
        {
          "port": "INPUT",
          "nets": [
            {
              "net": "INPUT",
              "active": "low"
            }
          ]
        },
        {
          "port": "CONNECT",
          "nets": [
            {
              "net": "CONNECT",
              "active": "low"
            }
          ]
        },
        {
          "port": "INIDENT",
          "nets": [
            {
              "net": "INIDENT",
              "active": "low"
            }
          ]
        },
        {
          "port": "OUTIDENT",
          "nets": [
            {
              "net": "OUTIDENT",
              "active": "low"
            }
          ]
        },
        {
          "port": "CL_1",
          "nets": [
            {
              "net": "CL",
              "active": "high"
            }
          ]
        }
      ]
    },
    {
      "name": "U4",
      "block": "modem-interface",
      "connections": [
        {
          "port": "H",
          "nets": [
            {
              "net": "H",
              "active": "high"
            }
          ]
        },
        {
          "port": "GND",
          "nets": [
            {
              "net": "GND"
            }
          ]
        },
        {
          "port": "TCLOCK",
          "nets": [
            {
              "net": "TCLOCK",
              "active": "high"
            }
          ]
        },
        {
          "port": "RCLOCK",
          "nets": [
            {
              "net": "RCLOCK",
              "active": "high"
            }
          ]
        },
        {
          "port": "RDATA",
          "nets": [
            {
              "net": "RDATA",
              "active": "high"
            }
          ]
        },
        {
          "port": "CDSTL_0",
          "nets": [
            {
              "net": "CDSTL",
              "active": "low"
            }
          ]
        },
        {
          "port": "CARR_0",
          "nets": [
            {
              "net": "CARR",
              "active": "low"
            }
          ]
        },
        {
          "port": "CARR_1",
          "nets": [
            {
              "net": "CARR",
              "active": "high"
            }
          ]
        },
        {
          "port": "TTM",
          "nets": [
            {
              "net": "TTM",
              "active": "low"
            }
          ]
        },
        {
          "port": "TRQTS",
          "nets": [
            {
              "net": "TRQTS",
              "active": "low"
            }
          ]
        },
        {
          "port": "RFS",
          "nets": [
            {
              "net": "RFS",
              "active": "low"
            }
          ]
        },
        {
          "port": "N_13B_6",
          "nets": [
            {
              "net": "N_13B_6"
            }
          ]
        },
        {
          "port": "RTM",
          "nets": [
            {
              "net": "RTM",
              "active": "low"
            }
          ]
        },
        {
          "port": "N_13B_3",
          "nets": [
            {
              "net": "N_13B_3"
            }
          ]
        },
        {
          "port": "BFQ1",
          "nets": [
            {
              "net": "BFQ1",
              "active": "high"
            }
          ]
        },
        {
          "port": "TDA",
          "nets": [
            {
              "net": "TDA",
              "active": "high"
            }
          ]
        },
        {
          "port": "TEST",
          "nets": [
            {
              "net": "TEST",
              "active": "high"
            }
          ]
        },
        {
          "port": "RQTS",
          "nets": [
            {
              "net": "RQTS",
              "active": "low"
            }
          ]
        },
        {
          "port": "DSR",
          "nets": [
            {
              "net": "DSR",
              "active": "low"
            }
          ]
        },
        {
          "port": "CAL",
          "nets": [
            {
              "net": "CAL",
              "active": "low"
            }
          ]
        },
        {
          "port": "RECEIVED DATA",
          "nets": [
            {
              "net": "RECEIVED DATA"
            }
          ]
        },
        {
          "port": "READY FOR SENDING",
          "nets": [
            {
              "net": "READY FOR SENDING"
            }
          ]
        },
        {
          "port": "DATA SET READY",
          "nets": [
            {
              "net": "DATA SET READY"
            }
          ]
        },
        {
          "port": "SIGNAL DETECTOR",
          "nets": [
            {
              "net": "SIGNAL DETECTOR"
            }
          ]
        },
        {
          "port": "CALLING INDICATOR",
          "nets": [
            {
              "net": "CALLING INDICATOR"
            }
          ]
        },
        {
          "port": "TRANSMITTER TIMING",
          "nets": [
            {
              "net": "TRANSMITTER TIMING"
            }
          ]
        },
        {
          "port": "RECEIVER TIMING",
          "nets": [
            {
              "net": "RECEIVER TIMING"
            }
          ]
        },
        {
          "port": "CONNECT TO LINE",
          "nets": [
            {
              "net": "CONNECT TO LINE"
            }
          ]
        },
        {
          "port": "REQUEST TO SEND",
          "nets": [
            {
              "net": "REQUEST TO SEND"
            }
          ]
        },
        {
          "port": "V24_23",
          "nets": [
            {
              "net": "V24_23"
            }
          ]
        },
        {
          "port": "TRANSM DATA",
          "nets": [
            {
              "net": "TRANSM DATA"
            }
          ]
        }
      ]
    }
  ]
}
