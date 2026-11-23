"""Render the gate shape SVGs to PNG and write docs/SYMBOL-CATALOG.md, a page that shows every
part in ND-13.003.01 with its cut-out, its description and (for gates) both drawings.

Run from the src folder after build-symbol-catalog.py and build-gate-shapes.py:
    python -I tools/build-catalog-page.py
"""
import glob, json, os
import fitz

import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from shape_map import shape_for, shapes_needed

def render(svg, png, zoom=4):
    doc = fitz.open(svg)
    page = doc[0]
    pix = page.get_pixmap(matrix=fitz.Matrix(zoom, zoom), alpha=False)
    pix.save(png)

for style in ("nd1976", "modern"):
    for k, _, _, _ in shapes_needed():
        base = f"catalog/symbols/{style}/shapes/{k}"
        render(base + ".svg", base + ".png")
    # the box parts, drawn by build-block-shapes.py from the cut-out readings
    for svg in sorted(glob.glob(f"catalog/symbols/{style}/shapes/block-*.svg")):
        render(svg, svg[:-4] + ".png", zoom=3)

entries = [json.load(open(f, encoding="utf-8")) for f in glob.glob("catalog/symbols/nd1976/*.json")]
entries.sort(key=lambda e: (e["source"]["printedPage"], e["id"]))
manual_formulas = json.load(open("tools/manual-formulas.json", encoding="utf-8"))

def formula(e):
    if e.get("formula"):
        return e["formula"]
    m = manual_formulas.get(e["partNumber"]) or manual_formulas.get(e["id"])
    if m:
        return m
    # not printed in the manual: the formulas from the chip's datasheet (catalog/chips)
    path = f"catalog/chips/{e['id']}.json"
    if os.path.exists(path):
        fs = json.load(open(path, encoding="utf-8")).get("functions") or []
        texts = [f.get("formula") for f in fs if f.get("formula")]
        if texts:
            return "; ".join(texts[:3]).replace("|", "\|") + (" ..." if len(texts) > 3 else "") + " (datasheet)"
    return "not printed in the manual; chip data not written yet"

def cell(v):
    """A value for a table cell; a missing figure shows as a dash, never as a guess."""
    if v is None or v == "":
        return "-"
    return str(v).replace("|", "\\|").replace("\n", " ")


EDGE = {"rising": "rising edge (L->H) of", "falling": "falling edge (H->L) of",
        "level-high": "while high:", "level-low": "while low:"}


def behaviour_lines(b):
    """The structured behaviour (FORMAT.md, Behaviour), one line per output and stored bit."""
    if not b:
        return ["- **Behaviour:** not written yet (see BUGS.md B3)."]
    if b.get("kind") == "none":
        return [f"- **Behaviour:** none - {cell(b.get('reason'))}"]
    out = ["- **Behaviour** (as the simulator reads it):"]
    stored = b.get("stored") or []
    if len(stored) > 16:
        out.append(f"  - {len(stored)} stored bits; the first two and the last:")
        stored = stored[:2] + stored[-1:]
    for s in stored:
        c = s.get("clock") or {}
        when = f"{EDGE.get(c.get('edge'), c.get('edge'))} {c.get('pin') or c.get('when')}" if c else "no clock"
        line = f"  - stored `{s['name']}` ({s['kind']}): " + (f"`{cell(s['next'])}` on {cell(when)}" if s.get("next") else f"pulse on {cell(when)}, {cell(s.get('pulseWidth'))}")
        for k in ("set", "clear"):
            if s.get(k):
                line += f"; {k} by {s[k]['pin']} {s[k]['active']}"
        for a in s.get("async") or []:
            line += f"; at once `{cell(a['value'])}` while `{cell(a['when'])}`"
        if s.get("whenSetAndClear"):
            line += f"; both: {cell(s['whenSetAndClear'])}"
        out.append(line)
    for i in b.get("internal") or []:
        out.append(f"  - inside: `{cell(i['formula'])}`")
    for c in b.get("conditions") or []:
        out.append(f"  - from analog levels: `{c['name']}` = {cell(c['meaning'])}")
    m = b.get("memory")
    if m:
        out.append(f"  - memory `{m['name']}`: {m['words']} words x {m['width']} bits, address {', '.join(m['address'])}")
    for o in b.get("outputs") or []:
        en = o.get("outputEnable")
        e = ""
        if en:
            e = " - drives when " + " and ".join(f"{p['pin']} {p['active']}" for p in en["pins"]) + f", else {en['disabled']}"
        out.append(f"  - `{cell(o['formula'])}` ({o['type']}){e}")
    return out


def chip_section(part_id):
    """What the chip is and does, from catalog/chips/<id>.json (datasheet facts with sources)."""
    path = f"catalog/chips/{part_id}.json"
    if not os.path.exists(path):
        return ["- **Chip data:** not written yet.\n"]
    c = json.load(open(path, encoding="utf-8"))
    out = []
    src = c.get("sources") or []
    if src:
        out.append("- **Datasheet:** " + "; ".join(
            f"[{s.get('document', '')}]({s.get('url', '')}) p. {s.get('pages', '')}"
            + (f" (figures for {s['familyUsed']})" if s.get("familyUsed") else "") for s in src))
    funcs = c.get("functions") or []
    if funcs:
        out.append("- **Formulas:**")
        for f in funcs:
            out.append(f"  - `{cell(f.get('formula'))}` - {cell(f.get('text'))}")
    st = c.get("state")
    if st:
        out.append(f"- **Stored state:** {cell(st.get('bits'))} bits, changes on {cell(st.get('changesOn'))}")
        for r in st.get("rules") or []:
            out.append(f"  - {cell(r)}")
    out += behaviour_lines(c.get("behaviour"))
    pins = c.get("pins") or []
    if pins:
        out.append("\n| Pin | Name | Role | Details |\n|---|---|---|---|")
        for p in sorted(pins, key=lambda p: (p.get("pin") if isinstance(p.get("pin"), int) else 999)):
            extra = []
            for key in ("outputType", "active", "edge", "voltage", "whenInactive", "enabledBy", "text"):
                if p.get(key):
                    extra.append(f"{key}: {p[key]}")
            if p.get("controls"):
                extra.append("controls: " + ", ".join(str(x) for x in p["controls"]))
            if p.get("schmitt"):
                extra.append("Schmitt input")
            if p.get("async"):
                extra.append("acts at once (not on a clock edge)")
            out.append(f"| {cell(p.get('pin'))} | {cell(p.get('name'))} | {cell(p.get('role'))} | {cell('; '.join(extra))} |")
        out.append("")
    tm = c.get("timing") or []
    if tm:
        out.append("| From | To | Output edge | Min ns | Typ ns | Max ns | Conditions |\n|---|---|---|---|---|---|---|")
        for t in tm:
            edge = {"PLH": "low to high", "PHL": "high to low"}.get(t.get("edge"), t.get("edge"))
            out.append(f"| {cell(t.get('from'))} | {cell(t.get('to'))} | {cell(edge)} | {cell(t.get('minNs'))} "
                       f"| {cell(t.get('typNs'))} | {cell(t.get('maxNs'))} | {cell(t.get('conditions'))} |")
        out.append("")
    lim = c.get("timingLimits") or []
    if lim:
        out.append("- **Timing limits:** " + "; ".join(
            f"{cell(l.get('what'))} {cell(l.get('pin'))}"
            + (f" to {l['ref']}" if l.get("ref") else "")
            + "".join(f" {k} {l[k]}" for k in ("minNs", "typNs", "maxNs", "minMHz", "typMHz") if l.get(k) is not None)
            for l in lim))
    el = c.get("electrical") or {}
    if el:
        out.append(f"- **Output current:** low {cell(el.get('outputLowMaxCurrentMa'))} mA, high {cell(el.get('outputHighMaxCurrentMa'))} mA"
                   + (f"; output may be pulled up to {el['outputMaxVoltageV']} V" if el.get("outputMaxVoltageV") else ""))
        if el.get("notes"):
            out.append(f"- **Electrical notes:** {cell(el['notes'])}")
    if c.get("unknown"):
        out.append("- **Not settled by the datasheet:** " + "; ".join(cell(u) for u in c["unknown"]))
    if c.get("notes"):
        out.append(f"- **Notes:** {cell(c['notes'])}")
    out.append("")
    return out


def anchor(e):
    return (e["partNumber"] + " - " + e["title"]).lower().replace(" ", "-")

out = []
out.append("# ND circuit diagram symbol catalogue\n")
out.append("Every part in ND-13.003.01 \"Circuit Diagram Symbols\" (A/S Norsk Data-Elektronikk, July 1976), "
           "in the manual's order. Each entry shows the cut-out from the manual. Gate symbols also show the "
           "description used to recognise them on a drawing, the manual's formula, and the same gate drawn "
           "again in ND style and in modern style. Both drawings end their pin lines on the same points "
           "(checked by the unit test `GateShapeTests`).\n")
out.append("This page is generated by `tools/build-catalog-page.py`. Do not edit it by hand.\n")
gates = [e for e in entries if e["symbolType"] == "gate"]
out.append(f"{len(entries)} parts, {len(gates)} of them gate symbols.\n")

out.append("## All parts\n")
out.append("| Part | Description (as in the manual) | Formula | Page |\n|---|---|---|---|")
for e in entries:
    out.append(f"| {e['partNumber']} | {e['title']} | {formula(e)} | {e['source']['printedPage']} |")
out.append("")
out.append("## How ND draws gates\n")
out.append("| Kind | ND 1976 | Modern | How to recognise the ND symbol |\n|---|---|---|---|")
rule = [
    ("NAND", "nand2", "Narrow upright rectangle, empty"),
    ("NAND, open collector", "nand-oc2", "The same with a vertical line down the centre"),
    ("NOR", "nor2", "Rectangle with an X corner to corner"),
    ("NOT", "not1", "Rectangle with a triangle filling it"),
    ("NOT, open collector", "not-oc1", "Rectangle with one diagonal, bottom left to top right"),
    ("AND", "and2", "One rectangle split down the middle; the right half holds the triangle"),
    ("AND, open collector", "and-oc2", "Split rectangle; the right half holds one diagonal from bottom left to the middle of the right edge"),
    ("OR", "or2", "Split rectangle; X in the left half, triangle in the right half"),
    ("XOR", "xor2", "Rectangle with a centre line and a short = across it"),
    ("Schmitt trigger", "nand-schmitt4", "Top edge runs on to the right, bottom edge runs on to the left"),
    ("Buffer", "nand-buf2", "The right end is a point"),
    ("Line driver", "line-driver1", "Large open triangle"),
]
for name, k, text in rule:
    out.append(f"| {name} | ![{k} in ND style](../catalog/symbols/nd1976/shapes/{k}.png) | "
               f"![{k} in modern style](../catalog/symbols/modern/shapes/{k}.png) | {text} |")
out.append("\nMarks that change a symbol: a centre line, a bar before the point or a single diagonal = open "
           "collector (in modern drawings, a diamond with a bar under it; HV beside it = high-voltage open collector, which stands up to 30 V or 15 V on its pull-up instead of 5.5 V - HV is our own label); a pointed right end = buffer/driver; edges running past the box = Schmitt trigger: an input with two switching points (about 1.7 V going up, 0.9 V going down) so a slow or noisy signal gives one clean change instead of flicker. "
           "Signal subscripts: 1 = active high, 0 = active low (manual page 2).\n")

out.append("### Where the modern marks come from\n")
out.append("The small marks inside the modern drawings come from IEEE Std 91-1984 / IEC 617-12, as explained in "
           "Texas Instruments' *Overview of IEEE Std 91-1984* (document SDYZ001A, checked 2026-11-23):\n")
out.append("- **Triangle** (pointing in the signal direction): \"a buffer or element with more than usual output "
           "capability\" (general qualifying symbols table). Used here on 7428, 7433, 7437, 7438 and 7440.")
out.append("- **Diamond with a bar under it**: open-collector output, one of the \"symbols inside the outline\".")
out.append("- **Step shape**: \"Schmitt trigger; element with hysteresis\" (general qualifying symbols table).")
out.append("- **HV**: not in the standard. It is our own label for a high-voltage open-collector output.\n")
out.append("The standard defines these marks for its rectangle-shaped symbols. Putting them inside the curved "
           "(distinctive-shape) gates, as these drawings do, is our choice; the standard does not cover it.\n")
out.append("## Parts\n")
for e in entries:
    src = e["source"]
    out.append(f"### {e['partNumber']} - {e['title']}\n")
    out.append(f"- **Part:** {e['partNumber']}")
    out.append(f"- **Description:** {e['title']}")
    out.append(f"- **Formula:** {formula(e)}")
    out.append(f"- **Manual page:** {src['printedPage']} (PDF page {src['pdfPage']})\n")
    out.append(f"![{e['partNumber']} as drawn in ND-13.003.01](../catalog/symbols/nd1976/{e['image']})\n")
    if e["symbolType"] == "gate":
        k = shape_for(e)[0]
        out.append(f"| ND 1976 | Modern |\n|---|---|\n| ![{k} in ND style](../catalog/symbols/nd1976/shapes/{k}.png) "
                   f"| ![{k} in modern style](../catalog/symbols/modern/shapes/{k}.png) |\n")
        out.append(f"- **Output:** {e['outputType']}")
        out.append(f"- **What it looks like:** {e['description']}")
        out.append(f"- **How to tell it apart:** {e['tells']}\n")
    else:
        k = "block-" + e["id"]
        if os.path.exists(f"catalog/symbols/nd1976/shapes/{k}.svg"):
            out.append(f"| ND 1976 (redrawn from the cut-out) | Modern |\n|---|---|\n"
                       f"| ![{e['id']} in ND style](../catalog/symbols/nd1976/shapes/{k}.png) "
                       f"| ![{e['id']} in modern style](../catalog/symbols/modern/shapes/{k}.png) |\n")
        else:
            out.append("- **Drawing:** the manual prints no drawing for this part, only a reference to another part.\n")
        out.append(f"- **What it looks like:** {e['description']}\n")
    out.extend(chip_section(e["id"]))

open("docs/SYMBOL-CATALOG.md", "w", encoding="utf-8", newline="\n").write("\n".join(out))
print(len(entries), "entries")
