"""Write the gate shapes in both styles and the one pin table they share.

Run from the src folder:  python -I tools/build-gate-shapes.py

Each shape is 60 x 40 units. The pin table (catalog/symbols/pins/<kind>.json) gives where each
pin's line ends. Both SVGs draw each pin as <line data-pin="..."> ending exactly there, so switching
from ND symbols to modern ones never moves a signal line. The unit test GateShapeTests checks it.

ND shapes follow ND-13.003.01 (see catalog/symbols/nd1976/<part>.json for the description):
NAND = empty box, NOR = box with an X, NOT = box with an inner triangle, AND = NAND box +
inverter box, OR = NOR box + inverter box, XOR = box with the exclusive-OR mark.
"""
import json, os

PINS2 = [{"pin": "A", "x": 0, "y": 12, "dir": "in"}, {"pin": "B", "x": 0, "y": 28, "dir": "in"},
         {"pin": "Y", "x": 60, "y": 20, "dir": "out"}]
PINS1 = [{"pin": "A", "x": 0, "y": 20, "dir": "in"}, {"pin": "Y", "x": 60, "y": 20, "dir": "out"}]

def stub(p, edge):
    # line from the pin end to the shape edge
    return f'<line data-pin="{p["pin"]}" x1="{edge}" y1="{p["y"]}" x2="{p["x"]}" y2="{p["y"]}"/>'

ND_BODY = {
    "nand": '<rect x="12" y="4" width="18" height="32"/>',
    "nor":  '<rect x="12" y="4" width="18" height="32"/><line x1="12" y1="4" x2="30" y2="36"/><line x1="12" y1="36" x2="30" y2="4"/>',
    # 7404 in the manual: a tall narrow rectangle (taller than wide, like the NAND box) with the
    # triangle filling it.
    "not":  '<rect x="20" y="5" width="16" height="30"/><polyline points="20,5 36,20 20,35"/>',
    "and":  '<rect x="10" y="4" width="18" height="32"/><rect x="28" y="10" width="16" height="20"/><polyline points="28,10 44,20 28,30"/>',
    "or":   '<rect x="10" y="4" width="18" height="32"/><line x1="10" y1="4" x2="28" y2="36"/><line x1="10" y1="36" x2="28" y2="4"/><rect x="28" y="10" width="16" height="20"/><polyline points="28,10 44,20 28,30"/>',
    "xor":  '<rect x="12" y="4" width="18" height="32"/><line x1="16" y1="18" x2="26" y2="18"/><line x1="16" y1="22" x2="26" y2="22"/><line x1="21" y1="12" x2="21" y2="28"/>',
}
# Open-collector variants. ND (manual page 4-6): NAND gets a vertical bar down the centre (7401,
# 7403); the inverter gets ONE diagonal instead of the triangle (7405); AND = NAND box + that
# diagonal box (7409). Modern: the plain shape with the open-collector mark (a small diamond with a
# bar under it) inside, near the output.
ND_BODY["nand-oc"] = '<rect x="12" y="4" width="18" height="32"/><line x1="21" y1="4" x2="21" y2="36"/>'
ND_BODY["not-oc"] = '<rect x="20" y="5" width="16" height="30"/><line x1="20" y1="35" x2="36" y2="5"/>'
ND_BODY["and-oc"] = '<rect x="10" y="4" width="18" height="32"/><rect x="28" y="10" width="16" height="20"/><line x1="28" y1="30" x2="44" y2="10"/>'
ND_EDGE = {"nand-oc": (12, 30), "not-oc": (20, 36), "and-oc": (10, 44), "nand": (12, 30), "nor": (12, 30), "not": (20, 36), "and": (10, 44), "or": (10, 44), "xor": (12, 30)}

MOD_BODY = {
    "and":  '<path d="M12,4 H28 A16,16 0 0 1 28,36 H12 Z"/>',
    "nand": '<path d="M12,4 H26 A16,16 0 0 1 26,36 H12 Z"/><circle cx="45" cy="20" r="3"/>',
    "or":   '<path d="M10,4 Q34,4 46,20 Q34,36 10,36 Q18,20 10,4 Z"/>',
    "nor":  '<path d="M8,4 Q32,4 42,20 Q32,36 8,36 Q16,20 8,4 Z"/><circle cx="45" cy="20" r="3"/>',
    "xor":  '<path d="M14,4 Q36,4 46,20 Q36,36 14,36 Q22,20 14,4 Z"/><path d="M9,4 Q17,20 9,36"/>',
    "not":  '<path d="M16,8 L40,20 L16,32 Z"/><circle cx="43" cy="20" r="3"/>',
}
OC_MARK = '<path d="M20,17 L23,20 L20,23 L17,20 Z"/><line x1="17" y1="25" x2="23" y2="25"/>'
MOD_BODY["nand-oc"] = MOD_BODY["nand"] + OC_MARK
MOD_BODY["not-oc"] = MOD_BODY["not"].replace('<path d="M16,8', '<path d="M16,8') + '<path d="M24,17 L27,20 L24,23 L21,20 Z"/><line x1="21" y1="25" x2="27" y2="25"/>'
MOD_BODY["and-oc"] = MOD_BODY["and"] + OC_MARK
# where each pin line meets the modern body: inputs, output
MOD_EDGE = {"nand-oc": (12, 48), "not-oc": (16, 46), "and-oc": (12, 44), "and": (12, 44), "nand": (12, 48), "or": (13, 46), "nor": (11, 48), "xor": (16, 46), "not": (16, 46)}

def svg(body, pins, edge_in, edge_out, title):
    lines = [stub(p, edge_in if p["dir"] == "in" else edge_out) for p in pins]
    return ('<svg xmlns="http://www.w3.org/2000/svg" width="60" height="40" viewBox="0 0 60 40">\n'
            f'<title>{title}</title>\n<g fill="none" stroke="#000000" stroke-width="1.2">\n'
            + body + "\n" + "\n".join(lines) + "\n</g>\n</svg>\n")

for style in ("nd1976", "modern"):
    os.makedirs(f"catalog/symbols/{style}/shapes", exist_ok=True)
os.makedirs("catalog/symbols/pins", exist_ok=True)
for kind in ND_BODY:
    pins = PINS1 if kind.startswith("not") else PINS2
    json.dump({"catalogFormat": "nd-symbol-pins", "catalogFormatVersion": 1, "kind": kind,
               "width": 60, "height": 40, "pins": pins},
              open(f"catalog/symbols/pins/{kind}.json", "w", newline="\n"), indent=2)
    a, b = ND_EDGE[kind]
    open(f"catalog/symbols/nd1976/shapes/{kind}.svg", "w", newline="\n").write(svg(ND_BODY[kind], pins, a, b, f"{kind}, ND-13.003.01 style"))
    a, b = MOD_EDGE[kind]
    open(f"catalog/symbols/modern/shapes/{kind}.svg", "w", newline="\n").write(svg(MOD_BODY[kind], pins, a, b, f"{kind}, modern style"))
print("ok")
