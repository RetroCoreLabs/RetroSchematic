{
  "format": "nd-netlist",
  "formatVersion": 3,
  "sheet": {
    "id": "3207",
    "title": "SCSI/FLOPPY",
    "machine": "ND-100",
    "bus": "nd-100",
    "company": "Norsk Data A.S.",
    "drawn": "1990-07-09",
    "source": {
      "document": "ND-350007-A1-EN Schematic Circuit Diagrams.pdf (24 sheets; kept outside this repository)"
    },
    "note": "Top level and the first block level read from sheets 1-4 on 2026-11-27 (docs/3207-reading.md). No chips yet. Each net keeps its label as printed in 'printed'. A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
  },
  "parts": [],
  "gates": [],
  "nets": [
    {
      "name": "CURRSW",
      "active": "low",
      "printed": "CURRSW~",
      "pins": [
        {
          "at": "P.A.a1",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "FTWOS",
      "active": "low",
      "printed": "FTWOS~",
      "pins": [
        {
          "at": "P.A.a5",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "FDISKCH",
      "active": "low",
      "printed": "FDISKCH~",
      "pins": [
        {
          "at": "P.A.a6",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "F8SSEL",
      "active": "low",
      "printed": "F8SSEL~",
      "pins": [
        {
          "at": "P.A.a7",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "INUSE",
      "active": "low",
      "printed": "INUSE~",
      "pins": [
        {
          "at": "P.A.a8",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "8HLD5OPT",
      "active": "low",
      "printed": "8HLD5OPT~",
      "pins": [
        {
          "at": "P.A.a9",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b9",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "8IND5INU",
      "active": "low",
      "printed": "8IND5INU~",
      "pins": [
        {
          "at": "P.A.a10",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b10",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "8REA5DS3",
      "active": "low",
      "printed": "8REA5DS3~",
      "pins": [
        {
          "at": "P.A.a11",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b11",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "F5INDEX",
      "active": "low",
      "printed": "F5INDEX~",
      "pins": [
        {
          "at": "P.A.a12",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b12",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "DS00",
      "active": "low",
      "printed": "DS~00",
      "pins": [
        {
          "at": "P.A.a13",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b13",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "DS01",
      "active": "low",
      "printed": "DS~01",
      "pins": [
        {
          "at": "P.A.a14",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b14",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "DS02",
      "active": "low",
      "printed": "DS~02",
      "pins": [
        {
          "at": "P.A.a15",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b15",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "8DS35MOT",
      "active": "low",
      "printed": "8DS35MOT~",
      "pins": [
        {
          "at": "P.A.a16",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b16",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "DIR",
      "active": "low",
      "printed": "DIR~",
      "pins": [
        {
          "at": "P.A.a17",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b17",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "STEP",
      "active": "low",
      "printed": "STEP~",
      "pins": [
        {
          "at": "P.A.a18",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b18",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "WDAT",
      "active": "low",
      "printed": "WDAT~",
      "pins": [
        {
          "at": "P.A.a19",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b19",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "WG",
      "active": "low",
      "printed": "WG~",
      "pins": [
        {
          "at": "P.A.a20",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b20",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "FTROO",
      "active": "low",
      "printed": "FTROO~",
      "pins": [
        {
          "at": "P.A.a21",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b21",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "FWPRT",
      "active": "low",
      "printed": "FWPRT~",
      "pins": [
        {
          "at": "P.A.a22",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b22",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "XRD",
      "active": "low",
      "printed": "XRD~",
      "pins": [
        {
          "at": "P.A.a23",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b23",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "F5SSEL",
      "active": "low",
      "printed": "F5SSEL~",
      "pins": [
        {
          "at": "P.A.a24",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b24",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "F5READY",
      "active": "low",
      "printed": "F5READY~",
      "pins": [
        {
          "at": "P.A.a25",
          "confidence": "read",
          "note": "drawn on sheet 1"
        },
        {
          "at": "P.A.b25",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "CKDIS",
      "active": "low",
      "printed": "CKDIS~",
      "pins": [
        {
          "at": "P.A.a26",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ]
    },
    {
      "name": "TFRQ",
      "active": "high",
      "pins": [
        {
          "at": "P.A.a27",
          "confidence": "read",
          "note": "drawn on sheet 1"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "GND",
      "active": "none",
      "pins": [
        {
          "at": "P.A.c1",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c2",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c3",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c4",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c5",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c6",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c7",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c8",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c9",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c10",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c11",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c12",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c13",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c14",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c15",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c16",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c17",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c18",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c19",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c20",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c21",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c22",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c23",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c24",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.A.c25",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.B.a11",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.a12",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.a14",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b11",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b12",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b14",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.c12",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.c14",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.c16",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.c25",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.C.a1",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.b1",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.c1",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.a32",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.b32",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.c32",
          "confidence": "read",
          "note": "drawn on sheet 2"
        }
      ],
      "note": "Every pin drawn to the open-triangle supply symbol, proved to be GND by ND-06.016.01 F-2."
    },
    {
      "name": "OGROUND2",
      "active": "high",
      "pins": [
        {
          "at": "P.B.a1",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b1",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODB0",
      "active": "low",
      "printed": "ODB~0",
      "pins": [
        {
          "at": "P.B.a2",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b2",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "ODB1",
      "active": "low",
      "printed": "ODB~1",
      "pins": [
        {
          "at": "P.B.a3",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b3",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "ODB2",
      "active": "low",
      "printed": "ODB~2",
      "pins": [
        {
          "at": "P.B.a4",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b4",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "ODB3",
      "active": "low",
      "printed": "ODB~3",
      "pins": [
        {
          "at": "P.B.a5",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b5",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "ODB4",
      "active": "low",
      "printed": "ODB~4",
      "pins": [
        {
          "at": "P.B.a6",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b6",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "ODB5",
      "active": "low",
      "printed": "ODB~5",
      "pins": [
        {
          "at": "P.B.a7",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b7",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "ODB6",
      "active": "low",
      "printed": "ODB~6",
      "pins": [
        {
          "at": "P.B.a8",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b8",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "ODB7",
      "active": "low",
      "printed": "ODB~7",
      "pins": [
        {
          "at": "P.B.a9",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b9",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "ODBP",
      "active": "low",
      "printed": "ODBP~",
      "pins": [
        {
          "at": "P.B.a10",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b10",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "OTERMPWR2",
      "active": "high",
      "pins": [
        {
          "at": "P.B.a13",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b13",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OATN",
      "active": "low",
      "printed": "OATN~",
      "pins": [
        {
          "at": "P.B.a15",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b15",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "OGROUND32",
      "active": "high",
      "pins": [
        {
          "at": "P.B.a16",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b16",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OBSY",
      "active": "low",
      "printed": "OBSY~",
      "pins": [
        {
          "at": "P.B.a17",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b17",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "OACK",
      "active": "low",
      "printed": "OACK~",
      "pins": [
        {
          "at": "P.B.a18",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b18",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "ORST",
      "active": "low",
      "printed": "ORST~",
      "pins": [
        {
          "at": "P.B.a19",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b19",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "OMSG",
      "active": "low",
      "printed": "OMSG~",
      "pins": [
        {
          "at": "P.B.a20",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b20",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "OSEL",
      "active": "low",
      "printed": "OSEL~",
      "pins": [
        {
          "at": "P.B.a21",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b21",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "OCD",
      "active": "low",
      "printed": "OCD~",
      "pins": [
        {
          "at": "P.B.a22",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b22",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "OREQ",
      "active": "low",
      "printed": "OREQ~",
      "pins": [
        {
          "at": "P.B.a23",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b23",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "OIO",
      "active": "low",
      "printed": "OIO~",
      "pins": [
        {
          "at": "P.B.a24",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b24",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ]
    },
    {
      "name": "OGROUND50",
      "active": "high",
      "pins": [
        {
          "at": "P.B.a25",
          "confidence": "read",
          "note": "drawn on sheet 3"
        },
        {
          "at": "P.B.b25",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OSHGND",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c1",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODB0",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c2",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODB1",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c3",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODB2",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c4",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODB3",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c5",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODB4",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c6",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODB5",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c7",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODB6",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c8",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODB7",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c9",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODBP",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c10",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ODIFFS",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c11",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OTERMPWR1",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c13",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OATN",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c15",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OBSY",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c17",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OACK",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c18",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "ORST",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c19",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OMSG",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c20",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OSEL",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c21",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OCD",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c22",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OREQ",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c23",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "OIO",
      "active": "high",
      "pins": [
        {
          "at": "P.B.c24",
          "confidence": "read",
          "note": "drawn on sheet 3"
        }
      ],
      "note": "A name without a tilde is taken as active high: inferred from the drawing's naming, where plug B carries pairs like ODB0 / ODB~0 and the tilde marks the complement."
    },
    {
      "name": "BD00",
      "active": "low",
      "printed": "BD~00",
      "pins": [
        {
          "at": "P.C.c3",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD01",
      "active": "low",
      "printed": "BD~01",
      "pins": [
        {
          "at": "P.C.a3",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD02",
      "active": "low",
      "printed": "BD~02",
      "pins": [
        {
          "at": "P.C.c4",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD03",
      "active": "low",
      "printed": "BD~03",
      "pins": [
        {
          "at": "P.C.a4",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD04",
      "active": "low",
      "printed": "BD~04",
      "pins": [
        {
          "at": "P.C.c5",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD05",
      "active": "low",
      "printed": "BD~05",
      "pins": [
        {
          "at": "P.C.a5",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD06",
      "active": "low",
      "printed": "BD~06",
      "pins": [
        {
          "at": "P.C.c6",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD07",
      "active": "low",
      "printed": "BD~07",
      "pins": [
        {
          "at": "P.C.a6",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD08",
      "active": "low",
      "printed": "BD~08",
      "pins": [
        {
          "at": "P.C.c7",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD09",
      "active": "low",
      "printed": "BD~09",
      "pins": [
        {
          "at": "P.C.a7",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD10",
      "active": "low",
      "printed": "BD~10",
      "pins": [
        {
          "at": "P.C.c8",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD11",
      "active": "low",
      "printed": "BD~11",
      "pins": [
        {
          "at": "P.C.a8",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD12",
      "active": "low",
      "printed": "BD~12",
      "pins": [
        {
          "at": "P.C.c9",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD13",
      "active": "low",
      "printed": "BD~13",
      "pins": [
        {
          "at": "P.C.a9",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD14",
      "active": "low",
      "printed": "BD~14",
      "pins": [
        {
          "at": "P.C.c10",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD15",
      "active": "low",
      "printed": "BD~15",
      "pins": [
        {
          "at": "P.C.a10",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD16",
      "active": "low",
      "printed": "BD~16",
      "pins": [
        {
          "at": "P.C.b3",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD17",
      "active": "low",
      "printed": "BD~17",
      "pins": [
        {
          "at": "P.C.b4",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD18",
      "active": "low",
      "printed": "BD~18",
      "pins": [
        {
          "at": "P.C.b5",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD19",
      "active": "low",
      "printed": "BD~19",
      "pins": [
        {
          "at": "P.C.b6",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD20",
      "active": "low",
      "printed": "BD~20",
      "pins": [
        {
          "at": "P.C.b7",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD21",
      "active": "low",
      "printed": "BD~21",
      "pins": [
        {
          "at": "P.C.b8",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD22",
      "active": "low",
      "printed": "BD~22",
      "pins": [
        {
          "at": "P.C.b9",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BD23",
      "active": "low",
      "printed": "BD~23",
      "pins": [
        {
          "at": "P.C.b10",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BDRY",
      "active": "low",
      "printed": "BDRY~",
      "pins": [
        {
          "at": "P.C.a19",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BINPUT",
      "active": "low",
      "printed": "BINPUT~",
      "pins": [
        {
          "at": "P.C.a18",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BAPR",
      "active": "low",
      "printed": "BAPR~",
      "pins": [
        {
          "at": "P.C.a20",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BMEM",
      "active": "low",
      "printed": "BMEM~",
      "pins": [
        {
          "at": "P.C.c20",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BMCL",
      "active": "low",
      "printed": "BMCL~",
      "pins": [
        {
          "at": "P.C.b20",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BINACK",
      "active": "low",
      "printed": "BINACK~",
      "pins": [
        {
          "at": "P.C.b19",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BIOXE",
      "active": "low",
      "printed": "BIOXE~",
      "pins": [
        {
          "at": "P.C.c19",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BDAP",
      "active": "low",
      "printed": "BDAP~",
      "pins": [
        {
          "at": "P.C.c18",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "INIDENT",
      "active": "low",
      "printed": "INIDENT~",
      "pins": [
        {
          "at": "P.C.a22",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "INGRANT",
      "active": "low",
      "printed": "INGRANT~",
      "pins": [
        {
          "at": "P.C.a23",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BINT11",
      "active": "low",
      "printed": "BINT11~",
      "pins": [
        {
          "at": "P.C.c15",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "OUTIDENT",
      "active": "low",
      "printed": "OUTIDENT~",
      "pins": [
        {
          "at": "P.C.c22",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "OUTGRANT",
      "active": "low",
      "printed": "OUTGRANT~",
      "pins": [
        {
          "at": "P.C.c23",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BERROR",
      "active": "low",
      "printed": "BERROR~",
      "pins": [
        {
          "at": "P.C.b21",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "BREQ",
      "active": "low",
      "printed": "BREQ~",
      "pins": [
        {
          "at": "P.C.c12",
          "confidence": "read",
          "note": "drawn on sheet 2; agrees with ND-06.016.01 F-2"
        }
      ]
    },
    {
      "name": "12V",
      "active": "none",
      "pins": [
        {
          "at": "P.C.a28",
          "confidence": "read",
          "note": "drawn on sheet 2, joined to the flag labelled 12V"
        },
        {
          "at": "P.C.b28",
          "confidence": "read",
          "note": "drawn on sheet 2, joined to the flag labelled 12V"
        },
        {
          "at": "P.C.c28",
          "confidence": "read",
          "note": "drawn on sheet 2, joined to the flag labelled 12V"
        }
      ],
      "note": "Plug C a28/b28/c28 go to a flag labelled 12V on sheet 2; box BLOCKLEV takes 12V from a flag of the same name on sheet 1. Joined by name, as both flags are on the top level (sheets 1 and 2): inferred."
    },
    {
      "name": "+5V",
      "active": "none",
      "pins": [
        {
          "at": "P.C.a2",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.b2",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.c2",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.a31",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.b31",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.c31",
          "confidence": "read",
          "note": "drawn on sheet 2"
        }
      ],
      "note": "Name given here: the drawing shows only the T-bar supply symbol, proved to be +5 V by ND-06.016.01 F-2 (C a2, b2, c2, a31, b31, c31)."
    },
    {
      "name": "5VSTBY",
      "active": "none",
      "pins": [
        {
          "at": "P.C.a30",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.b30",
          "confidence": "read",
          "note": "drawn on sheet 2"
        },
        {
          "at": "P.C.c30",
          "confidence": "read",
          "note": "drawn on sheet 2"
        }
      ],
      "note": "Name given here: the drawing shows only the filled-square supply symbol, proved to be 5 V standby by ND-06.016.01 F-2 (C a30, b30, c30)."
    },
    {
      "name": "AVC",
      "active": "high",
      "pins": [],
      "note": "Enters box BLOCKLEV from a flag on sheet 1; nothing on sheets 1-3 drives it. Where it comes from: open."
    },
    {
      "name": "AVC",
      "active": "low",
      "printed": "AVC~",
      "pins": [],
      "note": "Enters box BLOCKLEV from a flag on sheet 1; nothing on sheets 1-3 drives it. Where it comes from: open."
    }
  ],
  "connectors": [
    {
      "ref": "P.A",
      "catalog": "nd100-3207-a-plug",
      "use": "floppy drive lines",
      "rows": [
        "a",
        "b",
        "c"
      ],
      "pinsPerRow": 32,
      "page": "1"
    },
    {
      "ref": "P.B",
      "catalog": "nd100-3207-b-plug",
      "use": "SCSI bus",
      "rows": [
        "a",
        "b",
        "c"
      ],
      "pinsPerRow": 32,
      "page": "3"
    },
    {
      "ref": "P.C",
      "catalog": "nd100-c-connector",
      "use": "ND-100 bus",
      "rows": [
        "a",
        "b",
        "c"
      ],
      "pinsPerRow": 32,
      "page": "2"
    }
  ],
  "board": {
    "number": "3207",
    "partNumber": "350007",
    "title": "N-100 SCSI/Floppy Ctrl IV",
    "ndSystem": "ND-100",
    "group": "1E",
    "description": "SCSI (DL or SE) and floppy interface. Using 1 piece of SRAM chip, 8192 x 8 bits x 1 piece (total 8 KB).",
    "printVersion": "A",
    "ecoVersion": "A",
    "printVersions": [
      {
        "print": "A",
        "eco": "A"
      }
    ],
    "specifications": {
      "size": {
        "widthMm": 367,
        "heightMm": 280
      },
      "layers": 8
    },
    "releaseDate": "05. Nov. 1990",
    "leds": [
      {
        "id": "Z80 DMA",
        "color": "yellow",
        "text": "Z80 DMA (yellow) -"
      },
      {
        "id": "BUSY",
        "color": "yellow",
        "text": "BUSY (yellow) -"
      },
      {
        "id": "REQ",
        "color": "yelloq",
        "text": "REQ (yelloq) -"
      },
      {
        "text": "Floppy error code display"
      }
    ],
    "straps": [
      {
        "text": "1887A2"
      }
    ],
    "switches": [
      {
        "id": "TH1",
        "text": "TH1 - SCSI device no."
      },
      {
        "id": "TH2",
        "text": "TH2 - SCSI ident"
      },
      {
        "id": "TH3",
        "text": "TH3 - Floppy device no."
      }
    ],
    "connectors": [
      {
        "id": "A",
        "text": "(A) Floppy bus"
      },
      {
        "id": "B",
        "text": "(B) SCSI bus"
      },
      {
        "id": "C",
        "text": "(C) Standard ND-100 system bus"
      }
    ],
    "chipset": [
      {
        "name": "00900B"
      },
      {
        "name": "05800B"
      },
      {
        "name": "34500B"
      },
      {
        "name": "35700A"
      },
      {
        "name": "42200A"
      },
      {
        "name": "42201A"
      },
      {
        "name": "42500A"
      },
      {
        "name": "45800A"
      },
      {
        "name": "45900E"
      },
      {
        "name": "78100A"
      },
      {
        "name": "78200A"
      },
      {
        "name": "78300B"
      },
      {
        "name": "78400B"
      },
      {
        "name": "AM9517",
        "description": "DMA controller",
        "url": "http://sintran.com/library/libother/extern/AM9517.pdf"
      },
      {
        "name": "FD1797",
        "description": "Floppy Disc controller",
        "url": "http://sintran.com/library/libother/extern/FD1797.pdf"
      },
      {
        "name": "NCR5386",
        "description": "SCSI protocol controller",
        "url": "http://sintran.com/library/libother/extern/NCR5386.pdf"
      },
      {
        "name": "TC5563 or",
        "url": "http://sintran.com/library/libother/extern/TC5563.pdf"
      },
      {
        "name": "TMM2063",
        "description": "SRAM",
        "url": "http://sintran.com/library/libother/extern/TMM2063.pdf"
      },
      {
        "name": "Z80 CPU",
        "description": "CPU",
        "url": "http://sintran.com/library/libother/extern/Z80CPU-2016.pdf"
      },
      {
        "name": "Z80 CTC",
        "description": "CTC x 2 pieces",
        "url": "http://sintran.com/library/libother/extern/Z80CTC.pdf"
      }
    ],
    "sourceUrl": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
    "factSources": {
      "number": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "partNumber": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "title": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "ndSystem": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "group": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "description": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "printVersion": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "ecoVersion": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "releaseDate": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "printVersions": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "specifications": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "leds": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "straps": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "switches": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "connectors": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      },
      "chipset": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read"
      }
    }
  },
  "photos": [
    {
      "file": "3207-scsi-floppy-files/photos/350007-component-side-A.jpg",
      "side": "component",
      "version": "A",
      "caption": "Component side, PCB version A",
      "source": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read",
        "quote": "picture nd-350007-A1.jpg on the board's page"
      }
    },
    {
      "file": "3207-scsi-floppy-files/photos/350007-solder-side-A.jpg",
      "side": "solder",
      "version": "A",
      "caption": "Solder side, PCB version A",
      "source": {
        "kind": "catalogue",
        "document": "ND PCB catalogue (pcb.json, scraped from norsk-data.com)",
        "place": "http://norsk-data.com/hardware/nd-100/nd-350007.html",
        "confidence": "read",
        "quote": "picture nd-350007-A2.jpg on the board's page"
      }
    }
  ],
  "blocks": [
    {
      "name": "BLOCKLEV",
      "ports": [
        {
          "name": "ND100BUS",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "bus port of the 39 plug C signals (sheet 2); its members go both ways or one way, see the SCSI and FLOPPY ports",
          "signals": [
            {
              "net": "BD00",
              "active": "low"
            },
            {
              "net": "BD01",
              "active": "low"
            },
            {
              "net": "BD02",
              "active": "low"
            },
            {
              "net": "BD03",
              "active": "low"
            },
            {
              "net": "BD04",
              "active": "low"
            },
            {
              "net": "BD05",
              "active": "low"
            },
            {
              "net": "BD06",
              "active": "low"
            },
            {
              "net": "BD07",
              "active": "low"
            },
            {
              "net": "BD08",
              "active": "low"
            },
            {
              "net": "BD09",
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
            },
            {
              "net": "BD16",
              "active": "low"
            },
            {
              "net": "BD17",
              "active": "low"
            },
            {
              "net": "BD18",
              "active": "low"
            },
            {
              "net": "BD19",
              "active": "low"
            },
            {
              "net": "BD20",
              "active": "low"
            },
            {
              "net": "BD21",
              "active": "low"
            },
            {
              "net": "BD22",
              "active": "low"
            },
            {
              "net": "BD23",
              "active": "low"
            },
            {
              "net": "BDRY",
              "active": "low"
            },
            {
              "net": "BINPUT",
              "active": "low"
            },
            {
              "net": "BAPR",
              "active": "low"
            },
            {
              "net": "BMEM",
              "active": "low"
            },
            {
              "net": "BMCL",
              "active": "low"
            },
            {
              "net": "BINACK",
              "active": "low"
            },
            {
              "net": "BIOXE",
              "active": "low"
            },
            {
              "net": "BDAP",
              "active": "low"
            },
            {
              "net": "INIDENT",
              "active": "low"
            },
            {
              "net": "INGRANT",
              "active": "low"
            },
            {
              "net": "BINT11",
              "active": "low"
            },
            {
              "net": "OUTIDENT",
              "active": "low"
            },
            {
              "net": "OUTGRANT",
              "active": "low"
            },
            {
              "net": "BERROR",
              "active": "low"
            },
            {
              "net": "BREQ",
              "active": "low"
            }
          ]
        },
        {
          "name": "OSCSI",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 3: six-sided (both ways); every plug B pin is six-sided",
          "signals": [
            {
              "net": "OGROUND2",
              "active": "high"
            },
            {
              "net": "ODB0",
              "active": "low"
            },
            {
              "net": "ODB1",
              "active": "low"
            },
            {
              "net": "ODB2",
              "active": "low"
            },
            {
              "net": "ODB3",
              "active": "low"
            },
            {
              "net": "ODB4",
              "active": "low"
            },
            {
              "net": "ODB5",
              "active": "low"
            },
            {
              "net": "ODB6",
              "active": "low"
            },
            {
              "net": "ODB7",
              "active": "low"
            },
            {
              "net": "ODBP",
              "active": "low"
            },
            {
              "net": "OTERMPWR2",
              "active": "high"
            },
            {
              "net": "OATN",
              "active": "low"
            },
            {
              "net": "OGROUND32",
              "active": "high"
            },
            {
              "net": "OBSY",
              "active": "low"
            },
            {
              "net": "OACK",
              "active": "low"
            },
            {
              "net": "ORST",
              "active": "low"
            },
            {
              "net": "OMSG",
              "active": "low"
            },
            {
              "net": "OSEL",
              "active": "low"
            },
            {
              "net": "OCD",
              "active": "low"
            },
            {
              "net": "OREQ",
              "active": "low"
            },
            {
              "net": "OIO",
              "active": "low"
            },
            {
              "net": "OGROUND50",
              "active": "high"
            },
            {
              "net": "OSHGND",
              "active": "high"
            },
            {
              "net": "ODB0",
              "active": "high"
            },
            {
              "net": "ODB1",
              "active": "high"
            },
            {
              "net": "ODB2",
              "active": "high"
            },
            {
              "net": "ODB3",
              "active": "high"
            },
            {
              "net": "ODB4",
              "active": "high"
            },
            {
              "net": "ODB5",
              "active": "high"
            },
            {
              "net": "ODB6",
              "active": "high"
            },
            {
              "net": "ODB7",
              "active": "high"
            },
            {
              "net": "ODBP",
              "active": "high"
            },
            {
              "net": "ODIFFS",
              "active": "high"
            },
            {
              "net": "OTERMPWR1",
              "active": "high"
            },
            {
              "net": "OATN",
              "active": "high"
            },
            {
              "net": "OBSY",
              "active": "high"
            },
            {
              "net": "OACK",
              "active": "high"
            },
            {
              "net": "ORST",
              "active": "high"
            },
            {
              "net": "OMSG",
              "active": "high"
            },
            {
              "net": "OSEL",
              "active": "high"
            },
            {
              "net": "OCD",
              "active": "high"
            },
            {
              "net": "OREQ",
              "active": "high"
            },
            {
              "net": "OIO",
              "active": "high"
            }
          ]
        },
        {
          "name": "CKDIS_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "CKDIS",
              "active": "low"
            }
          ]
        },
        {
          "name": "TFRQ",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "TFRQ",
              "active": "high"
            }
          ]
        },
        {
          "name": "F5READY_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "F5READY",
              "active": "low"
            }
          ]
        },
        {
          "name": "F5INDEX_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "F5INDEX",
              "active": "low"
            }
          ]
        },
        {
          "name": "FTROO_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "FTROO",
              "active": "low"
            }
          ]
        },
        {
          "name": "FDISKCH_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "FDISKCH",
              "active": "low"
            }
          ]
        },
        {
          "name": "FTWOS_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "FTWOS",
              "active": "low"
            }
          ]
        },
        {
          "name": "FWPRT_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "FWPRT",
              "active": "low"
            }
          ]
        },
        {
          "name": "XRD_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "XRD",
              "active": "low"
            }
          ]
        },
        {
          "name": "DS_000",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "DS00",
              "active": "low"
            }
          ]
        },
        {
          "name": "DS_001",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "DS01",
              "active": "low"
            }
          ]
        },
        {
          "name": "DS_002",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "DS02",
              "active": "low"
            }
          ]
        },
        {
          "name": "CURRSW_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "CURRSW",
              "active": "low"
            }
          ]
        },
        {
          "name": "DIR_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "DIR",
              "active": "low"
            }
          ]
        },
        {
          "name": "INUSE_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "INUSE",
              "active": "low"
            }
          ]
        },
        {
          "name": "STEP_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "STEP",
              "active": "low"
            }
          ]
        },
        {
          "name": "WDAT_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "WDAT",
              "active": "low"
            }
          ]
        },
        {
          "name": "WG_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "WG",
              "active": "low"
            }
          ]
        },
        {
          "name": "F5SSEL_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "F5SSEL",
              "active": "low"
            }
          ]
        },
        {
          "name": "F8SSEL_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "F8SSEL",
              "active": "low"
            }
          ]
        },
        {
          "name": "8HLD5OPT_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "8HLD5OPT",
              "active": "low"
            }
          ]
        },
        {
          "name": "8DS35MOT_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "8DS35MOT",
              "active": "low"
            }
          ]
        },
        {
          "name": "8REA5DS3_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: six-sided (both ways)",
          "signals": [
            {
              "net": "8REA5DS3",
              "active": "low"
            }
          ]
        },
        {
          "name": "8IND5INU_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: six-sided (both ways)",
          "signals": [
            {
              "net": "8IND5INU",
              "active": "low"
            }
          ]
        },
        {
          "name": "AVC",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the flag shape on sheets 1 and 4: an arrow into the box",
          "signals": [
            {
              "net": "AVC",
              "active": "high"
            }
          ]
        },
        {
          "name": "AVC_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the flag shape on sheets 1 and 4: an arrow into the box",
          "signals": [
            {
              "net": "AVC",
              "active": "low"
            }
          ]
        },
        {
          "name": "12V",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the flag shape on sheets 1 and 4: an arrow into the box",
          "signals": [
            {
              "net": "12V",
              "active": "none"
            }
          ]
        }
      ],
      "parts": [],
      "gates": [],
      "nets": [
        {
          "name": "BD00",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD01",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD02",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD03",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD04",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD05",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD06",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD07",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD08",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD09",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD10",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD11",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD12",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD13",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD14",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD15",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD16",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD17",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD18",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD19",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD20",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD21",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD22",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD23",
          "active": "low",
          "pins": []
        },
        {
          "name": "BDRY",
          "active": "low",
          "pins": []
        },
        {
          "name": "BINPUT",
          "active": "low",
          "pins": []
        },
        {
          "name": "BAPR",
          "active": "low",
          "pins": []
        },
        {
          "name": "BMEM",
          "active": "low",
          "pins": []
        },
        {
          "name": "BMCL",
          "active": "low",
          "pins": []
        },
        {
          "name": "BINACK",
          "active": "low",
          "pins": []
        },
        {
          "name": "BIOXE",
          "active": "low",
          "pins": []
        },
        {
          "name": "BDAP",
          "active": "low",
          "pins": []
        },
        {
          "name": "INIDENT",
          "active": "low",
          "pins": []
        },
        {
          "name": "INGRANT",
          "active": "low",
          "pins": []
        },
        {
          "name": "BINT11",
          "active": "low",
          "pins": []
        },
        {
          "name": "OUTIDENT",
          "active": "low",
          "pins": []
        },
        {
          "name": "OUTGRANT",
          "active": "low",
          "pins": []
        },
        {
          "name": "BERROR",
          "active": "low",
          "pins": []
        },
        {
          "name": "BREQ",
          "active": "low",
          "pins": []
        },
        {
          "name": "OGROUND2",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB0",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB1",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB2",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB3",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB4",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB5",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB6",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB7",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODBP",
          "active": "low",
          "pins": []
        },
        {
          "name": "OTERMPWR2",
          "active": "high",
          "pins": []
        },
        {
          "name": "OATN",
          "active": "low",
          "pins": []
        },
        {
          "name": "OGROUND32",
          "active": "high",
          "pins": []
        },
        {
          "name": "OBSY",
          "active": "low",
          "pins": []
        },
        {
          "name": "OACK",
          "active": "low",
          "pins": []
        },
        {
          "name": "ORST",
          "active": "low",
          "pins": []
        },
        {
          "name": "OMSG",
          "active": "low",
          "pins": []
        },
        {
          "name": "OSEL",
          "active": "low",
          "pins": []
        },
        {
          "name": "OCD",
          "active": "low",
          "pins": []
        },
        {
          "name": "OREQ",
          "active": "low",
          "pins": []
        },
        {
          "name": "OIO",
          "active": "low",
          "pins": []
        },
        {
          "name": "OGROUND50",
          "active": "high",
          "pins": []
        },
        {
          "name": "OSHGND",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB0",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB1",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB2",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB3",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB4",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB5",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB6",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB7",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODBP",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODIFFS",
          "active": "high",
          "pins": []
        },
        {
          "name": "OTERMPWR1",
          "active": "high",
          "pins": []
        },
        {
          "name": "OATN",
          "active": "high",
          "pins": []
        },
        {
          "name": "OBSY",
          "active": "high",
          "pins": []
        },
        {
          "name": "OACK",
          "active": "high",
          "pins": []
        },
        {
          "name": "ORST",
          "active": "high",
          "pins": []
        },
        {
          "name": "OMSG",
          "active": "high",
          "pins": []
        },
        {
          "name": "OSEL",
          "active": "high",
          "pins": []
        },
        {
          "name": "OCD",
          "active": "high",
          "pins": []
        },
        {
          "name": "OREQ",
          "active": "high",
          "pins": []
        },
        {
          "name": "OIO",
          "active": "high",
          "pins": []
        },
        {
          "name": "CKDIS",
          "active": "low",
          "pins": []
        },
        {
          "name": "TFRQ",
          "active": "high",
          "pins": []
        },
        {
          "name": "F5READY",
          "active": "low",
          "pins": []
        },
        {
          "name": "F5INDEX",
          "active": "low",
          "pins": []
        },
        {
          "name": "FTROO",
          "active": "low",
          "pins": []
        },
        {
          "name": "FDISKCH",
          "active": "low",
          "pins": []
        },
        {
          "name": "FTWOS",
          "active": "low",
          "pins": []
        },
        {
          "name": "FWPRT",
          "active": "low",
          "pins": []
        },
        {
          "name": "XRD",
          "active": "low",
          "pins": []
        },
        {
          "name": "DS00",
          "active": "low",
          "pins": []
        },
        {
          "name": "DS01",
          "active": "low",
          "pins": []
        },
        {
          "name": "DS02",
          "active": "low",
          "pins": []
        },
        {
          "name": "CURRSW",
          "active": "low",
          "pins": []
        },
        {
          "name": "DIR",
          "active": "low",
          "pins": []
        },
        {
          "name": "INUSE",
          "active": "low",
          "pins": []
        },
        {
          "name": "STEP",
          "active": "low",
          "pins": []
        },
        {
          "name": "WDAT",
          "active": "low",
          "pins": []
        },
        {
          "name": "WG",
          "active": "low",
          "pins": []
        },
        {
          "name": "F5SSEL",
          "active": "low",
          "pins": []
        },
        {
          "name": "F8SSEL",
          "active": "low",
          "pins": []
        },
        {
          "name": "8HLD5OPT",
          "active": "low",
          "pins": []
        },
        {
          "name": "8DS35MOT",
          "active": "low",
          "pins": []
        },
        {
          "name": "8REA5DS3",
          "active": "low",
          "pins": []
        },
        {
          "name": "8IND5INU",
          "active": "low",
          "pins": []
        },
        {
          "name": "AVC",
          "active": "high",
          "pins": []
        },
        {
          "name": "AVC",
          "active": "low",
          "pins": []
        },
        {
          "name": "12V",
          "active": "none",
          "pins": []
        },
        {
          "name": "LADR1",
          "active": "high",
          "pins": []
        },
        {
          "name": "LADR2",
          "active": "high",
          "pins": []
        },
        {
          "name": "SOTGRANT",
          "active": "low",
          "pins": []
        },
        {
          "name": "SOTIDENT",
          "active": "low",
          "pins": []
        }
      ],
      "instances": [
        {
          "name": "SCSI",
          "block": "SCSI",
          "connections": [
            {
              "port": "BINACK_0",
              "nets": [
                {
                  "net": "BINACK",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BMEM_0",
              "nets": [
                {
                  "net": "BMEM",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BMCL_0",
              "nets": [
                {
                  "net": "BMCL",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BIOXE_0",
              "nets": [
                {
                  "net": "BIOXE",
                  "active": "low"
                }
              ]
            },
            {
              "port": "INGRANT_0",
              "nets": [
                {
                  "net": "INGRANT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "INIDENT_0",
              "nets": [
                {
                  "net": "INIDENT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BAPR_0",
              "nets": [
                {
                  "net": "BAPR",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BDAP_0",
              "nets": [
                {
                  "net": "BDAP",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BDRY_0",
              "nets": [
                {
                  "net": "BDRY",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BREQ_0",
              "nets": [
                {
                  "net": "BREQ",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BINPUT_0",
              "nets": [
                {
                  "net": "BINPUT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BERROR_0",
              "nets": [
                {
                  "net": "BERROR",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BD_0",
              "nets": [
                {
                  "net": "BD00",
                  "active": "low"
                },
                {
                  "net": "BD01",
                  "active": "low"
                },
                {
                  "net": "BD02",
                  "active": "low"
                },
                {
                  "net": "BD03",
                  "active": "low"
                },
                {
                  "net": "BD04",
                  "active": "low"
                },
                {
                  "net": "BD05",
                  "active": "low"
                },
                {
                  "net": "BD06",
                  "active": "low"
                },
                {
                  "net": "BD07",
                  "active": "low"
                },
                {
                  "net": "BD08",
                  "active": "low"
                },
                {
                  "net": "BD09",
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
                },
                {
                  "net": "BD16",
                  "active": "low"
                },
                {
                  "net": "BD17",
                  "active": "low"
                },
                {
                  "net": "BD18",
                  "active": "low"
                },
                {
                  "net": "BD19",
                  "active": "low"
                },
                {
                  "net": "BD20",
                  "active": "low"
                },
                {
                  "net": "BD21",
                  "active": "low"
                },
                {
                  "net": "BD22",
                  "active": "low"
                },
                {
                  "net": "BD23",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BINT11_0",
              "nets": [
                {
                  "net": "BINT11",
                  "active": "low"
                }
              ]
            },
            {
              "port": "OSCSI",
              "nets": [
                {
                  "net": "OGROUND2",
                  "active": "high"
                },
                {
                  "net": "ODB0",
                  "active": "low"
                },
                {
                  "net": "ODB1",
                  "active": "low"
                },
                {
                  "net": "ODB2",
                  "active": "low"
                },
                {
                  "net": "ODB3",
                  "active": "low"
                },
                {
                  "net": "ODB4",
                  "active": "low"
                },
                {
                  "net": "ODB5",
                  "active": "low"
                },
                {
                  "net": "ODB6",
                  "active": "low"
                },
                {
                  "net": "ODB7",
                  "active": "low"
                },
                {
                  "net": "ODBP",
                  "active": "low"
                },
                {
                  "net": "OTERMPWR2",
                  "active": "high"
                },
                {
                  "net": "OATN",
                  "active": "low"
                },
                {
                  "net": "OGROUND32",
                  "active": "high"
                },
                {
                  "net": "OBSY",
                  "active": "low"
                },
                {
                  "net": "OACK",
                  "active": "low"
                },
                {
                  "net": "ORST",
                  "active": "low"
                },
                {
                  "net": "OMSG",
                  "active": "low"
                },
                {
                  "net": "OSEL",
                  "active": "low"
                },
                {
                  "net": "OCD",
                  "active": "low"
                },
                {
                  "net": "OREQ",
                  "active": "low"
                },
                {
                  "net": "OIO",
                  "active": "low"
                },
                {
                  "net": "OGROUND50",
                  "active": "high"
                },
                {
                  "net": "OSHGND",
                  "active": "high"
                },
                {
                  "net": "ODB0",
                  "active": "high"
                },
                {
                  "net": "ODB1",
                  "active": "high"
                },
                {
                  "net": "ODB2",
                  "active": "high"
                },
                {
                  "net": "ODB3",
                  "active": "high"
                },
                {
                  "net": "ODB4",
                  "active": "high"
                },
                {
                  "net": "ODB5",
                  "active": "high"
                },
                {
                  "net": "ODB6",
                  "active": "high"
                },
                {
                  "net": "ODB7",
                  "active": "high"
                },
                {
                  "net": "ODBP",
                  "active": "high"
                },
                {
                  "net": "ODIFFS",
                  "active": "high"
                },
                {
                  "net": "OTERMPWR1",
                  "active": "high"
                },
                {
                  "net": "OATN",
                  "active": "high"
                },
                {
                  "net": "OBSY",
                  "active": "high"
                },
                {
                  "net": "OACK",
                  "active": "high"
                },
                {
                  "net": "ORST",
                  "active": "high"
                },
                {
                  "net": "OMSG",
                  "active": "high"
                },
                {
                  "net": "OSEL",
                  "active": "high"
                },
                {
                  "net": "OCD",
                  "active": "high"
                },
                {
                  "net": "OREQ",
                  "active": "high"
                },
                {
                  "net": "OIO",
                  "active": "high"
                }
              ]
            },
            {
              "port": "LADR1",
              "nets": [
                {
                  "net": "LADR1",
                  "active": "high"
                }
              ]
            },
            {
              "port": "LADR2",
              "nets": [
                {
                  "net": "LADR2",
                  "active": "high"
                }
              ]
            },
            {
              "port": "SOTGRANT_0",
              "nets": [
                {
                  "net": "SOTGRANT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "SOTIDENT_0",
              "nets": [
                {
                  "net": "SOTIDENT",
                  "active": "low"
                }
              ]
            }
          ]
        },
        {
          "name": "FLOPPY",
          "block": "FLOPPY",
          "connections": [
            {
              "port": "BDAP_0",
              "nets": [
                {
                  "net": "BDAP",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BDRY_0",
              "nets": [
                {
                  "net": "BDRY",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BREQ_0",
              "nets": [
                {
                  "net": "BREQ",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BINPUT_0",
              "nets": [
                {
                  "net": "BINPUT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BERROR_0",
              "nets": [
                {
                  "net": "BERROR",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BD_0",
              "nets": [
                {
                  "net": "BD00",
                  "active": "low"
                },
                {
                  "net": "BD01",
                  "active": "low"
                },
                {
                  "net": "BD02",
                  "active": "low"
                },
                {
                  "net": "BD03",
                  "active": "low"
                },
                {
                  "net": "BD04",
                  "active": "low"
                },
                {
                  "net": "BD05",
                  "active": "low"
                },
                {
                  "net": "BD06",
                  "active": "low"
                },
                {
                  "net": "BD07",
                  "active": "low"
                },
                {
                  "net": "BD08",
                  "active": "low"
                },
                {
                  "net": "BD09",
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
                },
                {
                  "net": "BD16",
                  "active": "low"
                },
                {
                  "net": "BD17",
                  "active": "low"
                },
                {
                  "net": "BD18",
                  "active": "low"
                },
                {
                  "net": "BD19",
                  "active": "low"
                },
                {
                  "net": "BD20",
                  "active": "low"
                },
                {
                  "net": "BD21",
                  "active": "low"
                },
                {
                  "net": "BD22",
                  "active": "low"
                },
                {
                  "net": "BD23",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BINACK_0",
              "nets": [
                {
                  "net": "BINACK",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BMEM_0",
              "nets": [
                {
                  "net": "BMEM",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BMCL_0",
              "nets": [
                {
                  "net": "BMCL",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BIOXE_0",
              "nets": [
                {
                  "net": "BIOXE",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BAPR_0",
              "nets": [
                {
                  "net": "BAPR",
                  "active": "low"
                }
              ]
            },
            {
              "port": "BINT11_0",
              "nets": [
                {
                  "net": "BINT11",
                  "active": "low"
                }
              ]
            },
            {
              "port": "OUTGRANT_0",
              "nets": [
                {
                  "net": "OUTGRANT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "OUTIDENT_0",
              "nets": [
                {
                  "net": "OUTIDENT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "CKDIS_0",
              "nets": [
                {
                  "net": "CKDIS",
                  "active": "low"
                }
              ]
            },
            {
              "port": "TFRQ",
              "nets": [
                {
                  "net": "TFRQ",
                  "active": "high"
                }
              ]
            },
            {
              "port": "F5READY_0",
              "nets": [
                {
                  "net": "F5READY",
                  "active": "low"
                }
              ]
            },
            {
              "port": "F5INDEX_0",
              "nets": [
                {
                  "net": "F5INDEX",
                  "active": "low"
                }
              ]
            },
            {
              "port": "FTROO_0",
              "nets": [
                {
                  "net": "FTROO",
                  "active": "low"
                }
              ]
            },
            {
              "port": "FDISKCH_0",
              "nets": [
                {
                  "net": "FDISKCH",
                  "active": "low"
                }
              ]
            },
            {
              "port": "FTWOS_0",
              "nets": [
                {
                  "net": "FTWOS",
                  "active": "low"
                }
              ]
            },
            {
              "port": "FWPRT_0",
              "nets": [
                {
                  "net": "FWPRT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "XRD_0",
              "nets": [
                {
                  "net": "XRD",
                  "active": "low"
                }
              ]
            },
            {
              "port": "DS_000",
              "nets": [
                {
                  "net": "DS00",
                  "active": "low"
                }
              ]
            },
            {
              "port": "DS_001",
              "nets": [
                {
                  "net": "DS01",
                  "active": "low"
                }
              ]
            },
            {
              "port": "DS_002",
              "nets": [
                {
                  "net": "DS02",
                  "active": "low"
                }
              ]
            },
            {
              "port": "CURRSW_0",
              "nets": [
                {
                  "net": "CURRSW",
                  "active": "low"
                }
              ]
            },
            {
              "port": "DIR_0",
              "nets": [
                {
                  "net": "DIR",
                  "active": "low"
                }
              ]
            },
            {
              "port": "INUSE_0",
              "nets": [
                {
                  "net": "INUSE",
                  "active": "low"
                }
              ]
            },
            {
              "port": "STEP_0",
              "nets": [
                {
                  "net": "STEP",
                  "active": "low"
                }
              ]
            },
            {
              "port": "WDAT_0",
              "nets": [
                {
                  "net": "WDAT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "WG_0",
              "nets": [
                {
                  "net": "WG",
                  "active": "low"
                }
              ]
            },
            {
              "port": "F5SSEL_0",
              "nets": [
                {
                  "net": "F5SSEL",
                  "active": "low"
                }
              ]
            },
            {
              "port": "F8SSEL_0",
              "nets": [
                {
                  "net": "F8SSEL",
                  "active": "low"
                }
              ]
            },
            {
              "port": "8HLD5OPT_0",
              "nets": [
                {
                  "net": "8HLD5OPT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "8DS35MOT_0",
              "nets": [
                {
                  "net": "8DS35MOT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "8REA5DS3_0",
              "nets": [
                {
                  "net": "8REA5DS3",
                  "active": "low"
                }
              ]
            },
            {
              "port": "8IND5INU_0",
              "nets": [
                {
                  "net": "8IND5INU",
                  "active": "low"
                }
              ]
            },
            {
              "port": "LADR1",
              "nets": [
                {
                  "net": "LADR1",
                  "active": "high"
                }
              ]
            },
            {
              "port": "LADR2",
              "nets": [
                {
                  "net": "LADR2",
                  "active": "high"
                }
              ]
            },
            {
              "port": "SOTGRANT_0",
              "nets": [
                {
                  "net": "SOTGRANT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "SOTIDENT_0",
              "nets": [
                {
                  "net": "SOTIDENT",
                  "active": "low"
                }
              ]
            },
            {
              "port": "AVC",
              "nets": [
                {
                  "net": "AVC",
                  "active": "high"
                }
              ]
            },
            {
              "port": "AVC_0",
              "nets": [
                {
                  "net": "AVC",
                  "active": "low"
                }
              ]
            },
            {
              "port": "12V",
              "nets": [
                {
                  "net": "12V",
                  "active": "none"
                }
              ]
            }
          ]
        }
      ],
      "pages": [
        {
          "id": "4",
          "title": "BLOCKLEV",
          "sheetNumber": "4 of 24",
          "note": "margin path BLOCKLEV 1.DRAW"
        }
      ],
      "note": "The whole card as one box (sheet 1). Its inside is sheet 4: boxes SCSI and FLOPPY."
    },
    {
      "name": "SCSI",
      "ports": [
        {
          "name": "BINACK_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow into the card",
          "signals": [
            {
              "net": "BINACK",
              "active": "low"
            }
          ]
        },
        {
          "name": "BMEM_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow into the card",
          "signals": [
            {
              "net": "BMEM",
              "active": "low"
            }
          ]
        },
        {
          "name": "BMCL_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow into the card",
          "signals": [
            {
              "net": "BMCL",
              "active": "low"
            }
          ]
        },
        {
          "name": "BIOXE_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow into the card",
          "signals": [
            {
              "net": "BIOXE",
              "active": "low"
            }
          ]
        },
        {
          "name": "INGRANT_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow into the card",
          "signals": [
            {
              "net": "INGRANT",
              "active": "low"
            }
          ]
        },
        {
          "name": "INIDENT_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow into the card",
          "signals": [
            {
              "net": "INIDENT",
              "active": "low"
            }
          ]
        },
        {
          "name": "BAPR_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BAPR",
              "active": "low"
            }
          ]
        },
        {
          "name": "BDAP_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BDAP",
              "active": "low"
            }
          ]
        },
        {
          "name": "BDRY_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BDRY",
              "active": "low"
            }
          ]
        },
        {
          "name": "BREQ_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BREQ",
              "active": "low"
            }
          ]
        },
        {
          "name": "BINPUT_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BINPUT",
              "active": "low"
            }
          ]
        },
        {
          "name": "BERROR_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BERROR",
              "active": "low"
            }
          ]
        },
        {
          "name": "BD_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "bus BD~(23:00) on sheet 4; inferred from the plug port shape on sheet 2: six-sided (both ways)",
          "signals": [
            {
              "net": "BD00",
              "active": "low"
            },
            {
              "net": "BD01",
              "active": "low"
            },
            {
              "net": "BD02",
              "active": "low"
            },
            {
              "net": "BD03",
              "active": "low"
            },
            {
              "net": "BD04",
              "active": "low"
            },
            {
              "net": "BD05",
              "active": "low"
            },
            {
              "net": "BD06",
              "active": "low"
            },
            {
              "net": "BD07",
              "active": "low"
            },
            {
              "net": "BD08",
              "active": "low"
            },
            {
              "net": "BD09",
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
            },
            {
              "net": "BD16",
              "active": "low"
            },
            {
              "net": "BD17",
              "active": "low"
            },
            {
              "net": "BD18",
              "active": "low"
            },
            {
              "net": "BD19",
              "active": "low"
            },
            {
              "net": "BD20",
              "active": "low"
            },
            {
              "net": "BD21",
              "active": "low"
            },
            {
              "net": "BD22",
              "active": "low"
            },
            {
              "net": "BD23",
              "active": "low"
            }
          ]
        },
        {
          "name": "BINT11_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow out of the card",
          "signals": [
            {
              "net": "BINT11",
              "active": "low"
            }
          ]
        },
        {
          "name": "OSCSI",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 3: six-sided (both ways)",
          "signals": [
            {
              "net": "OGROUND2",
              "active": "high"
            },
            {
              "net": "ODB0",
              "active": "low"
            },
            {
              "net": "ODB1",
              "active": "low"
            },
            {
              "net": "ODB2",
              "active": "low"
            },
            {
              "net": "ODB3",
              "active": "low"
            },
            {
              "net": "ODB4",
              "active": "low"
            },
            {
              "net": "ODB5",
              "active": "low"
            },
            {
              "net": "ODB6",
              "active": "low"
            },
            {
              "net": "ODB7",
              "active": "low"
            },
            {
              "net": "ODBP",
              "active": "low"
            },
            {
              "net": "OTERMPWR2",
              "active": "high"
            },
            {
              "net": "OATN",
              "active": "low"
            },
            {
              "net": "OGROUND32",
              "active": "high"
            },
            {
              "net": "OBSY",
              "active": "low"
            },
            {
              "net": "OACK",
              "active": "low"
            },
            {
              "net": "ORST",
              "active": "low"
            },
            {
              "net": "OMSG",
              "active": "low"
            },
            {
              "net": "OSEL",
              "active": "low"
            },
            {
              "net": "OCD",
              "active": "low"
            },
            {
              "net": "OREQ",
              "active": "low"
            },
            {
              "net": "OIO",
              "active": "low"
            },
            {
              "net": "OGROUND50",
              "active": "high"
            },
            {
              "net": "OSHGND",
              "active": "high"
            },
            {
              "net": "ODB0",
              "active": "high"
            },
            {
              "net": "ODB1",
              "active": "high"
            },
            {
              "net": "ODB2",
              "active": "high"
            },
            {
              "net": "ODB3",
              "active": "high"
            },
            {
              "net": "ODB4",
              "active": "high"
            },
            {
              "net": "ODB5",
              "active": "high"
            },
            {
              "net": "ODB6",
              "active": "high"
            },
            {
              "net": "ODB7",
              "active": "high"
            },
            {
              "net": "ODBP",
              "active": "high"
            },
            {
              "net": "ODIFFS",
              "active": "high"
            },
            {
              "net": "OTERMPWR1",
              "active": "high"
            },
            {
              "net": "OATN",
              "active": "high"
            },
            {
              "net": "OBSY",
              "active": "high"
            },
            {
              "net": "OACK",
              "active": "high"
            },
            {
              "net": "ORST",
              "active": "high"
            },
            {
              "net": "OMSG",
              "active": "high"
            },
            {
              "net": "OSEL",
              "active": "high"
            },
            {
              "net": "OCD",
              "active": "high"
            },
            {
              "net": "OREQ",
              "active": "high"
            },
            {
              "net": "OIO",
              "active": "high"
            }
          ]
        },
        {
          "name": "LADR1",
          "direction": "inout",
          "directionConfidence": "open",
          "note": "runs between boxes SCSI and FLOPPY on sheet 4 with no direction mark; to be read from the logic sheets",
          "signals": [
            {
              "net": "LADR1",
              "active": "high"
            }
          ]
        },
        {
          "name": "LADR2",
          "direction": "inout",
          "directionConfidence": "open",
          "note": "runs between boxes SCSI and FLOPPY on sheet 4 with no direction mark; to be read from the logic sheets",
          "signals": [
            {
              "net": "LADR2",
              "active": "high"
            }
          ]
        },
        {
          "name": "SOTGRANT_0",
          "direction": "inout",
          "directionConfidence": "open",
          "note": "runs between boxes SCSI and FLOPPY on sheet 4 with no direction mark; to be read from the logic sheets",
          "signals": [
            {
              "net": "SOTGRANT",
              "active": "low"
            }
          ]
        },
        {
          "name": "SOTIDENT_0",
          "direction": "inout",
          "directionConfidence": "open",
          "note": "runs between boxes SCSI and FLOPPY on sheet 4 with no direction mark; to be read from the logic sheets",
          "signals": [
            {
              "net": "SOTIDENT",
              "active": "low"
            }
          ]
        }
      ],
      "parts": [],
      "gates": [],
      "nets": [
        {
          "name": "BINACK",
          "active": "low",
          "pins": []
        },
        {
          "name": "BMEM",
          "active": "low",
          "pins": []
        },
        {
          "name": "BMCL",
          "active": "low",
          "pins": []
        },
        {
          "name": "BIOXE",
          "active": "low",
          "pins": []
        },
        {
          "name": "INGRANT",
          "active": "low",
          "pins": []
        },
        {
          "name": "INIDENT",
          "active": "low",
          "pins": []
        },
        {
          "name": "BAPR",
          "active": "low",
          "pins": []
        },
        {
          "name": "BDAP",
          "active": "low",
          "pins": []
        },
        {
          "name": "BDRY",
          "active": "low",
          "pins": []
        },
        {
          "name": "BREQ",
          "active": "low",
          "pins": []
        },
        {
          "name": "BINPUT",
          "active": "low",
          "pins": []
        },
        {
          "name": "BERROR",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD00",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD01",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD02",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD03",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD04",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD05",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD06",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD07",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD08",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD09",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD10",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD11",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD12",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD13",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD14",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD15",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD16",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD17",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD18",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD19",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD20",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD21",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD22",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD23",
          "active": "low",
          "pins": []
        },
        {
          "name": "BINT11",
          "active": "low",
          "pins": []
        },
        {
          "name": "OGROUND2",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB0",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB1",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB2",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB3",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB4",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB5",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB6",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODB7",
          "active": "low",
          "pins": []
        },
        {
          "name": "ODBP",
          "active": "low",
          "pins": []
        },
        {
          "name": "OTERMPWR2",
          "active": "high",
          "pins": []
        },
        {
          "name": "OATN",
          "active": "low",
          "pins": []
        },
        {
          "name": "OGROUND32",
          "active": "high",
          "pins": []
        },
        {
          "name": "OBSY",
          "active": "low",
          "pins": []
        },
        {
          "name": "OACK",
          "active": "low",
          "pins": []
        },
        {
          "name": "ORST",
          "active": "low",
          "pins": []
        },
        {
          "name": "OMSG",
          "active": "low",
          "pins": []
        },
        {
          "name": "OSEL",
          "active": "low",
          "pins": []
        },
        {
          "name": "OCD",
          "active": "low",
          "pins": []
        },
        {
          "name": "OREQ",
          "active": "low",
          "pins": []
        },
        {
          "name": "OIO",
          "active": "low",
          "pins": []
        },
        {
          "name": "OGROUND50",
          "active": "high",
          "pins": []
        },
        {
          "name": "OSHGND",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB0",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB1",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB2",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB3",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB4",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB5",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB6",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODB7",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODBP",
          "active": "high",
          "pins": []
        },
        {
          "name": "ODIFFS",
          "active": "high",
          "pins": []
        },
        {
          "name": "OTERMPWR1",
          "active": "high",
          "pins": []
        },
        {
          "name": "OATN",
          "active": "high",
          "pins": []
        },
        {
          "name": "OBSY",
          "active": "high",
          "pins": []
        },
        {
          "name": "OACK",
          "active": "high",
          "pins": []
        },
        {
          "name": "ORST",
          "active": "high",
          "pins": []
        },
        {
          "name": "OMSG",
          "active": "high",
          "pins": []
        },
        {
          "name": "OSEL",
          "active": "high",
          "pins": []
        },
        {
          "name": "OCD",
          "active": "high",
          "pins": []
        },
        {
          "name": "OREQ",
          "active": "high",
          "pins": []
        },
        {
          "name": "OIO",
          "active": "high",
          "pins": []
        },
        {
          "name": "LADR1",
          "active": "high",
          "pins": []
        },
        {
          "name": "LADR2",
          "active": "high",
          "pins": []
        },
        {
          "name": "SOTGRANT",
          "active": "low",
          "pins": []
        },
        {
          "name": "SOTIDENT",
          "active": "low",
          "pins": []
        }
      ],
      "pages": [
        {
          "id": "15",
          "sheetNumber": "15 of 24",
          "note": "margin path BLOCKLEV/SCSI 1.DRAW"
        }
      ],
      "note": "Box SCSI on sheet 4. Its inside (sheet 15, with DIFF_SCSI, SING_DIFF_SW, S_INTERF and S_ND_INT on sheets 16-24) is not read yet."
    },
    {
      "name": "FLOPPY",
      "ports": [
        {
          "name": "BDAP_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BDAP",
              "active": "low"
            }
          ]
        },
        {
          "name": "BDRY_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BDRY",
              "active": "low"
            }
          ]
        },
        {
          "name": "BREQ_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BREQ",
              "active": "low"
            }
          ]
        },
        {
          "name": "BINPUT_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BINPUT",
              "active": "low"
            }
          ]
        },
        {
          "name": "BERROR_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BERROR",
              "active": "low"
            }
          ]
        },
        {
          "name": "BD_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "bus BD~(23:00) on sheet 4; inferred from the plug port shape on sheet 2: six-sided (both ways)",
          "signals": [
            {
              "net": "BD00",
              "active": "low"
            },
            {
              "net": "BD01",
              "active": "low"
            },
            {
              "net": "BD02",
              "active": "low"
            },
            {
              "net": "BD03",
              "active": "low"
            },
            {
              "net": "BD04",
              "active": "low"
            },
            {
              "net": "BD05",
              "active": "low"
            },
            {
              "net": "BD06",
              "active": "low"
            },
            {
              "net": "BD07",
              "active": "low"
            },
            {
              "net": "BD08",
              "active": "low"
            },
            {
              "net": "BD09",
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
            },
            {
              "net": "BD16",
              "active": "low"
            },
            {
              "net": "BD17",
              "active": "low"
            },
            {
              "net": "BD18",
              "active": "low"
            },
            {
              "net": "BD19",
              "active": "low"
            },
            {
              "net": "BD20",
              "active": "low"
            },
            {
              "net": "BD21",
              "active": "low"
            },
            {
              "net": "BD22",
              "active": "low"
            },
            {
              "net": "BD23",
              "active": "low"
            }
          ]
        },
        {
          "name": "BINACK_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow into the card",
          "signals": [
            {
              "net": "BINACK",
              "active": "low"
            }
          ]
        },
        {
          "name": "BMEM_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow into the card",
          "signals": [
            {
              "net": "BMEM",
              "active": "low"
            }
          ]
        },
        {
          "name": "BMCL_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow into the card",
          "signals": [
            {
              "net": "BMCL",
              "active": "low"
            }
          ]
        },
        {
          "name": "BIOXE_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow into the card",
          "signals": [
            {
              "net": "BIOXE",
              "active": "low"
            }
          ]
        },
        {
          "name": "BAPR_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: six-sided (both ways); the box pin carries a small open circle on sheet 4, seen only where the plug port is six-sided",
          "signals": [
            {
              "net": "BAPR",
              "active": "low"
            }
          ]
        },
        {
          "name": "BINT11_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow out of the card",
          "signals": [
            {
              "net": "BINT11",
              "active": "low"
            }
          ]
        },
        {
          "name": "OUTGRANT_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow out of the card",
          "signals": [
            {
              "net": "OUTGRANT",
              "active": "low"
            }
          ]
        },
        {
          "name": "OUTIDENT_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 2: an arrow out of the card",
          "signals": [
            {
              "net": "OUTIDENT",
              "active": "low"
            }
          ]
        },
        {
          "name": "CKDIS_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "CKDIS",
              "active": "low"
            }
          ]
        },
        {
          "name": "TFRQ",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "TFRQ",
              "active": "high"
            }
          ]
        },
        {
          "name": "F5READY_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "F5READY",
              "active": "low"
            }
          ]
        },
        {
          "name": "F5INDEX_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "F5INDEX",
              "active": "low"
            }
          ]
        },
        {
          "name": "FTROO_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "FTROO",
              "active": "low"
            }
          ]
        },
        {
          "name": "FDISKCH_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "FDISKCH",
              "active": "low"
            }
          ]
        },
        {
          "name": "FTWOS_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "FTWOS",
              "active": "low"
            }
          ]
        },
        {
          "name": "FWPRT_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "FWPRT",
              "active": "low"
            }
          ]
        },
        {
          "name": "XRD_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow into the card",
          "signals": [
            {
              "net": "XRD",
              "active": "low"
            }
          ]
        },
        {
          "name": "DS_000",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "DS00",
              "active": "low"
            }
          ]
        },
        {
          "name": "DS_001",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "DS01",
              "active": "low"
            }
          ]
        },
        {
          "name": "DS_002",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "DS02",
              "active": "low"
            }
          ]
        },
        {
          "name": "CURRSW_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "CURRSW",
              "active": "low"
            }
          ]
        },
        {
          "name": "DIR_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "DIR",
              "active": "low"
            }
          ]
        },
        {
          "name": "INUSE_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "INUSE",
              "active": "low"
            }
          ]
        },
        {
          "name": "STEP_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "STEP",
              "active": "low"
            }
          ]
        },
        {
          "name": "WDAT_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "WDAT",
              "active": "low"
            }
          ]
        },
        {
          "name": "WG_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "WG",
              "active": "low"
            }
          ]
        },
        {
          "name": "F5SSEL_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "F5SSEL",
              "active": "low"
            }
          ]
        },
        {
          "name": "F8SSEL_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "F8SSEL",
              "active": "low"
            }
          ]
        },
        {
          "name": "8HLD5OPT_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "8HLD5OPT",
              "active": "low"
            }
          ]
        },
        {
          "name": "8DS35MOT_0",
          "direction": "output",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: an arrow out of the card",
          "signals": [
            {
              "net": "8DS35MOT",
              "active": "low"
            }
          ]
        },
        {
          "name": "8REA5DS3_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: six-sided (both ways); open circle on the FLOPPY box pin on sheet 4",
          "signals": [
            {
              "net": "8REA5DS3",
              "active": "low"
            }
          ]
        },
        {
          "name": "8IND5INU_0",
          "direction": "inout",
          "directionConfidence": "inferred",
          "note": "inferred from the plug port shape on sheet 1: six-sided (both ways); open circle on the FLOPPY box pin on sheet 4",
          "signals": [
            {
              "net": "8IND5INU",
              "active": "low"
            }
          ]
        },
        {
          "name": "LADR1",
          "direction": "inout",
          "directionConfidence": "open",
          "note": "runs between boxes SCSI and FLOPPY on sheet 4 with no direction mark; to be read from the logic sheets",
          "signals": [
            {
              "net": "LADR1",
              "active": "high"
            }
          ]
        },
        {
          "name": "LADR2",
          "direction": "inout",
          "directionConfidence": "open",
          "note": "runs between boxes SCSI and FLOPPY on sheet 4 with no direction mark; to be read from the logic sheets",
          "signals": [
            {
              "net": "LADR2",
              "active": "high"
            }
          ]
        },
        {
          "name": "SOTGRANT_0",
          "direction": "inout",
          "directionConfidence": "open",
          "note": "runs between boxes SCSI and FLOPPY on sheet 4 with no direction mark; to be read from the logic sheets",
          "signals": [
            {
              "net": "SOTGRANT",
              "active": "low"
            }
          ]
        },
        {
          "name": "SOTIDENT_0",
          "direction": "inout",
          "directionConfidence": "open",
          "note": "runs between boxes SCSI and FLOPPY on sheet 4 with no direction mark; to be read from the logic sheets",
          "signals": [
            {
              "net": "SOTIDENT",
              "active": "low"
            }
          ]
        },
        {
          "name": "AVC",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the flag shape on sheet 4: an arrow into the box",
          "signals": [
            {
              "net": "AVC",
              "active": "high"
            }
          ]
        },
        {
          "name": "AVC_0",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the flag shape on sheet 4: an arrow into the box",
          "signals": [
            {
              "net": "AVC",
              "active": "low"
            }
          ]
        },
        {
          "name": "12V",
          "direction": "input",
          "directionConfidence": "inferred",
          "note": "inferred from the flag shape on sheet 4: an arrow into the box",
          "signals": [
            {
              "net": "12V",
              "active": "none"
            }
          ]
        }
      ],
      "parts": [],
      "gates": [],
      "nets": [
        {
          "name": "BDAP",
          "active": "low",
          "pins": []
        },
        {
          "name": "BDRY",
          "active": "low",
          "pins": []
        },
        {
          "name": "BREQ",
          "active": "low",
          "pins": []
        },
        {
          "name": "BINPUT",
          "active": "low",
          "pins": []
        },
        {
          "name": "BERROR",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD00",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD01",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD02",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD03",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD04",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD05",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD06",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD07",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD08",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD09",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD10",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD11",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD12",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD13",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD14",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD15",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD16",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD17",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD18",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD19",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD20",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD21",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD22",
          "active": "low",
          "pins": []
        },
        {
          "name": "BD23",
          "active": "low",
          "pins": []
        },
        {
          "name": "BINACK",
          "active": "low",
          "pins": []
        },
        {
          "name": "BMEM",
          "active": "low",
          "pins": []
        },
        {
          "name": "BMCL",
          "active": "low",
          "pins": []
        },
        {
          "name": "BIOXE",
          "active": "low",
          "pins": []
        },
        {
          "name": "BAPR",
          "active": "low",
          "pins": []
        },
        {
          "name": "BINT11",
          "active": "low",
          "pins": []
        },
        {
          "name": "OUTGRANT",
          "active": "low",
          "pins": []
        },
        {
          "name": "OUTIDENT",
          "active": "low",
          "pins": []
        },
        {
          "name": "CKDIS",
          "active": "low",
          "pins": []
        },
        {
          "name": "TFRQ",
          "active": "high",
          "pins": []
        },
        {
          "name": "F5READY",
          "active": "low",
          "pins": []
        },
        {
          "name": "F5INDEX",
          "active": "low",
          "pins": []
        },
        {
          "name": "FTROO",
          "active": "low",
          "pins": []
        },
        {
          "name": "FDISKCH",
          "active": "low",
          "pins": []
        },
        {
          "name": "FTWOS",
          "active": "low",
          "pins": []
        },
        {
          "name": "FWPRT",
          "active": "low",
          "pins": []
        },
        {
          "name": "XRD",
          "active": "low",
          "pins": []
        },
        {
          "name": "DS00",
          "active": "low",
          "pins": []
        },
        {
          "name": "DS01",
          "active": "low",
          "pins": []
        },
        {
          "name": "DS02",
          "active": "low",
          "pins": []
        },
        {
          "name": "CURRSW",
          "active": "low",
          "pins": []
        },
        {
          "name": "DIR",
          "active": "low",
          "pins": []
        },
        {
          "name": "INUSE",
          "active": "low",
          "pins": []
        },
        {
          "name": "STEP",
          "active": "low",
          "pins": []
        },
        {
          "name": "WDAT",
          "active": "low",
          "pins": []
        },
        {
          "name": "WG",
          "active": "low",
          "pins": []
        },
        {
          "name": "F5SSEL",
          "active": "low",
          "pins": []
        },
        {
          "name": "F8SSEL",
          "active": "low",
          "pins": []
        },
        {
          "name": "8HLD5OPT",
          "active": "low",
          "pins": []
        },
        {
          "name": "8DS35MOT",
          "active": "low",
          "pins": []
        },
        {
          "name": "8REA5DS3",
          "active": "low",
          "pins": []
        },
        {
          "name": "8IND5INU",
          "active": "low",
          "pins": []
        },
        {
          "name": "LADR1",
          "active": "high",
          "pins": []
        },
        {
          "name": "LADR2",
          "active": "high",
          "pins": []
        },
        {
          "name": "SOTGRANT",
          "active": "low",
          "pins": []
        },
        {
          "name": "SOTIDENT",
          "active": "low",
          "pins": []
        },
        {
          "name": "AVC",
          "active": "high",
          "pins": []
        },
        {
          "name": "AVC",
          "active": "low",
          "pins": []
        },
        {
          "name": "12V",
          "active": "none",
          "pins": []
        }
      ],
      "pages": [
        {
          "id": "5",
          "title": "FLOPPY",
          "sheetNumber": "5 of 24",
          "note": "margin path BLOCKLEV/FLOPPY 1.DRAW"
        }
      ],
      "note": "Box FLOPPY on sheet 4. Its inside (sheet 5, with Z80_COMP, LO_FLOPPY and F_ND_INT on sheets 6-14) is not read yet."
    }
  ],
  "instances": [
    {
      "name": "BLOCKLEV",
      "block": "BLOCKLEV",
      "connections": [
        {
          "port": "ND100BUS",
          "nets": [
            {
              "net": "BD00",
              "active": "low"
            },
            {
              "net": "BD01",
              "active": "low"
            },
            {
              "net": "BD02",
              "active": "low"
            },
            {
              "net": "BD03",
              "active": "low"
            },
            {
              "net": "BD04",
              "active": "low"
            },
            {
              "net": "BD05",
              "active": "low"
            },
            {
              "net": "BD06",
              "active": "low"
            },
            {
              "net": "BD07",
              "active": "low"
            },
            {
              "net": "BD08",
              "active": "low"
            },
            {
              "net": "BD09",
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
            },
            {
              "net": "BD16",
              "active": "low"
            },
            {
              "net": "BD17",
              "active": "low"
            },
            {
              "net": "BD18",
              "active": "low"
            },
            {
              "net": "BD19",
              "active": "low"
            },
            {
              "net": "BD20",
              "active": "low"
            },
            {
              "net": "BD21",
              "active": "low"
            },
            {
              "net": "BD22",
              "active": "low"
            },
            {
              "net": "BD23",
              "active": "low"
            },
            {
              "net": "BDRY",
              "active": "low"
            },
            {
              "net": "BINPUT",
              "active": "low"
            },
            {
              "net": "BAPR",
              "active": "low"
            },
            {
              "net": "BMEM",
              "active": "low"
            },
            {
              "net": "BMCL",
              "active": "low"
            },
            {
              "net": "BINACK",
              "active": "low"
            },
            {
              "net": "BIOXE",
              "active": "low"
            },
            {
              "net": "BDAP",
              "active": "low"
            },
            {
              "net": "INIDENT",
              "active": "low"
            },
            {
              "net": "INGRANT",
              "active": "low"
            },
            {
              "net": "BINT11",
              "active": "low"
            },
            {
              "net": "OUTIDENT",
              "active": "low"
            },
            {
              "net": "OUTGRANT",
              "active": "low"
            },
            {
              "net": "BERROR",
              "active": "low"
            },
            {
              "net": "BREQ",
              "active": "low"
            }
          ]
        },
        {
          "port": "OSCSI",
          "nets": [
            {
              "net": "OGROUND2",
              "active": "high"
            },
            {
              "net": "ODB0",
              "active": "low"
            },
            {
              "net": "ODB1",
              "active": "low"
            },
            {
              "net": "ODB2",
              "active": "low"
            },
            {
              "net": "ODB3",
              "active": "low"
            },
            {
              "net": "ODB4",
              "active": "low"
            },
            {
              "net": "ODB5",
              "active": "low"
            },
            {
              "net": "ODB6",
              "active": "low"
            },
            {
              "net": "ODB7",
              "active": "low"
            },
            {
              "net": "ODBP",
              "active": "low"
            },
            {
              "net": "OTERMPWR2",
              "active": "high"
            },
            {
              "net": "OATN",
              "active": "low"
            },
            {
              "net": "OGROUND32",
              "active": "high"
            },
            {
              "net": "OBSY",
              "active": "low"
            },
            {
              "net": "OACK",
              "active": "low"
            },
            {
              "net": "ORST",
              "active": "low"
            },
            {
              "net": "OMSG",
              "active": "low"
            },
            {
              "net": "OSEL",
              "active": "low"
            },
            {
              "net": "OCD",
              "active": "low"
            },
            {
              "net": "OREQ",
              "active": "low"
            },
            {
              "net": "OIO",
              "active": "low"
            },
            {
              "net": "OGROUND50",
              "active": "high"
            },
            {
              "net": "OSHGND",
              "active": "high"
            },
            {
              "net": "ODB0",
              "active": "high"
            },
            {
              "net": "ODB1",
              "active": "high"
            },
            {
              "net": "ODB2",
              "active": "high"
            },
            {
              "net": "ODB3",
              "active": "high"
            },
            {
              "net": "ODB4",
              "active": "high"
            },
            {
              "net": "ODB5",
              "active": "high"
            },
            {
              "net": "ODB6",
              "active": "high"
            },
            {
              "net": "ODB7",
              "active": "high"
            },
            {
              "net": "ODBP",
              "active": "high"
            },
            {
              "net": "ODIFFS",
              "active": "high"
            },
            {
              "net": "OTERMPWR1",
              "active": "high"
            },
            {
              "net": "OATN",
              "active": "high"
            },
            {
              "net": "OBSY",
              "active": "high"
            },
            {
              "net": "OACK",
              "active": "high"
            },
            {
              "net": "ORST",
              "active": "high"
            },
            {
              "net": "OMSG",
              "active": "high"
            },
            {
              "net": "OSEL",
              "active": "high"
            },
            {
              "net": "OCD",
              "active": "high"
            },
            {
              "net": "OREQ",
              "active": "high"
            },
            {
              "net": "OIO",
              "active": "high"
            }
          ]
        },
        {
          "port": "CKDIS_0",
          "nets": [
            {
              "net": "CKDIS",
              "active": "low"
            }
          ]
        },
        {
          "port": "TFRQ",
          "nets": [
            {
              "net": "TFRQ",
              "active": "high"
            }
          ]
        },
        {
          "port": "F5READY_0",
          "nets": [
            {
              "net": "F5READY",
              "active": "low"
            }
          ]
        },
        {
          "port": "F5INDEX_0",
          "nets": [
            {
              "net": "F5INDEX",
              "active": "low"
            }
          ]
        },
        {
          "port": "FTROO_0",
          "nets": [
            {
              "net": "FTROO",
              "active": "low"
            }
          ]
        },
        {
          "port": "FDISKCH_0",
          "nets": [
            {
              "net": "FDISKCH",
              "active": "low"
            }
          ]
        },
        {
          "port": "FTWOS_0",
          "nets": [
            {
              "net": "FTWOS",
              "active": "low"
            }
          ]
        },
        {
          "port": "FWPRT_0",
          "nets": [
            {
              "net": "FWPRT",
              "active": "low"
            }
          ]
        },
        {
          "port": "XRD_0",
          "nets": [
            {
              "net": "XRD",
              "active": "low"
            }
          ]
        },
        {
          "port": "DS_000",
          "nets": [
            {
              "net": "DS00",
              "active": "low"
            }
          ]
        },
        {
          "port": "DS_001",
          "nets": [
            {
              "net": "DS01",
              "active": "low"
            }
          ]
        },
        {
          "port": "DS_002",
          "nets": [
            {
              "net": "DS02",
              "active": "low"
            }
          ]
        },
        {
          "port": "CURRSW_0",
          "nets": [
            {
              "net": "CURRSW",
              "active": "low"
            }
          ]
        },
        {
          "port": "DIR_0",
          "nets": [
            {
              "net": "DIR",
              "active": "low"
            }
          ]
        },
        {
          "port": "INUSE_0",
          "nets": [
            {
              "net": "INUSE",
              "active": "low"
            }
          ]
        },
        {
          "port": "STEP_0",
          "nets": [
            {
              "net": "STEP",
              "active": "low"
            }
          ]
        },
        {
          "port": "WDAT_0",
          "nets": [
            {
              "net": "WDAT",
              "active": "low"
            }
          ]
        },
        {
          "port": "WG_0",
          "nets": [
            {
              "net": "WG",
              "active": "low"
            }
          ]
        },
        {
          "port": "F5SSEL_0",
          "nets": [
            {
              "net": "F5SSEL",
              "active": "low"
            }
          ]
        },
        {
          "port": "F8SSEL_0",
          "nets": [
            {
              "net": "F8SSEL",
              "active": "low"
            }
          ]
        },
        {
          "port": "8HLD5OPT_0",
          "nets": [
            {
              "net": "8HLD5OPT",
              "active": "low"
            }
          ]
        },
        {
          "port": "8DS35MOT_0",
          "nets": [
            {
              "net": "8DS35MOT",
              "active": "low"
            }
          ]
        },
        {
          "port": "8REA5DS3_0",
          "nets": [
            {
              "net": "8REA5DS3",
              "active": "low"
            }
          ]
        },
        {
          "port": "8IND5INU_0",
          "nets": [
            {
              "net": "8IND5INU",
              "active": "low"
            }
          ]
        },
        {
          "port": "AVC",
          "nets": [
            {
              "net": "AVC",
              "active": "high"
            }
          ]
        },
        {
          "port": "AVC_0",
          "nets": [
            {
              "net": "AVC",
              "active": "low"
            }
          ]
        },
        {
          "port": "12V",
          "nets": [
            {
              "net": "12V",
              "active": "none"
            }
          ]
        }
      ],
      "page": "1"
    }
  ],
  "pages": [
    {
      "id": "1",
      "title": "EXTERNAL CONNECTIONS",
      "sheetNumber": "1 of 24"
    },
    {
      "id": "2",
      "title": "ND100 BUS CONNECTORS, POWER & GND CONNECTORS",
      "sheetNumber": "2 of 24"
    },
    {
      "id": "3",
      "sheetNumber": "3 of 24",
      "note": "plug B; the scan cuts off this sheet's margin"
    }
  ]
}
