"""Which drawing (catalog/symbols/*/shapes/<shape>.svg) belongs to which manual entry.

Used by build-gate-shapes.py, build-catalog-page.py and the side-by-side check, so all three
always agree. A shape is named <base><number of inputs>, e.g. nand3, and-oc2.
"""
import glob, json, os

# Base drawing for parts whose look is not just "kind + output type" (see the cut-outs).
BASE_BY_PART = {
    "7406": "not-buf-ochv",        # pointed box with one diagonal
    "7416": "not-buf-ochv-bar",    # box, bar, point
    "7407": "buf-ochv",            # split box, diagonal in the right half
    "7413": "nand-schmitt",
    "7414": "not-schmitt",
    "7417": "hex-buf-ochv",        # whole package drawn
    "7425": "nor-strobe",
    "7428": "nor-buf",
    "7433": "nor-buf-oc",
    "7437": "nand-buf",
    "7438": "nand-buf-oc",
    "7440": "nand-buf",
    "7450": "aoi-2x2",
    "75150": "line-driver",
    "75154": "line-driver",
}

# Input counts the manual draws, where gate-symbols.json does not give one.
INPUTS_BY_PART = {"75150": 2, "75154": 1, "7417": 6, "7450": 8, "7425": 4}
SECOND_INPUT_NAME = {"75150": "S"}


def shape_for(e):
    """(shape name, base, inputs, second input name) for one manual entry. A gate symbol
    without a drawing is an error, never a blank."""
    part = e["partNumber"]
    n = INPUTS_BY_PART.get(part) or e.get("inputs") or 1
    if part in BASE_BY_PART:
        base = BASE_BY_PART[part]
    else:
        k = e.get("kind")
        out = e.get("outputType")
        if out == "totem-pole" and k in ("nand", "nor", "not", "and", "or", "xor"):
            base = k
        elif out == "open-collector" and k in ("nand", "not", "and"):
            base = k + "-oc"
        else:
            raise SystemExit(f"{part}: gate symbol without a drawing - add it to shape_map.py and build-gate-shapes.py")
    return base + str(n), base, n, SECOND_INPUT_NAME.get(part)


def gate_entries(root="catalog/symbols/nd1976"):
    out = []
    for f in sorted(glob.glob(os.path.join(root, "*.json"))):
        e = json.load(open(f, encoding="utf-8"))
        if e["symbolType"] == "gate":
            out.append(e)
    return out


def shapes_needed():
    seen = {}
    for e in gate_entries():
        kind, base, n, second = shape_for(e)
        seen[kind] = (kind, base, n, second)
    return list(seen.values())
