"""Step 6.2: eye-check sheets for scoring the detector against the step-5 rule finder on a real page.
Candidates of both finders are pooled per class; candidates that overlap become ONE object (so an
object is judged once). Each object gets an id, drawn on zoomed tiles; the judge (by eye) writes
the ids that are NOT a real object of that class, and counts real objects that no finder marked.
Classes: chip, gate, circle, offpage (rule: diamonds), poscode (rule: none - the rules read codes
at fixed places around a symbol, they have no code boxes).
Usage: python -I evaltiles.py <page.png> <rule dump.json> <out dir> <cols> <rows> <classes,comma> name=<dets.json> [name=<dets.json> ...]
"""
import site, sys, os, json
sys.path.append(site.getusersitepackages())
import numpy as np, cv2

page, rdump, od = sys.argv[1:4]
cols, rows, CLS = int(sys.argv[4]), int(sys.argv[5]), sys.argv[6].split(",")
DS = [a.split("=", 1) for a in sys.argv[7:]]
os.makedirs(od, exist_ok=True)
g = cv2.imread(page, 0); H, W = g.shape
R = json.load(open(rdump)) if rdump != "-" else {"chips": [], "gates": [], "circles": [], "diamonds": []}   # "-": detector only
cand = []
for c in R["chips"]: cand.append(("chip", "rule", c["box"]))
for q in R["gates"]: cand.append(("gate", "rule", q["box"]))
for cx, cy, r in R["circles"]: cand.append(("circle", "rule", [cx - r, cy - r, 2 * r, 2 * r]))
for b in R["diamonds"]: cand.append(("offpage", "rule", b))
for name, f in DS:
    for d in json.load(open(f))["dets"]:
        cand.append((d["cls"], name, d["box"]))
cand = [c for c in cand if c[0] in CLS]


def same(a, b):
    ca = (a[0] + a[2] / 2, a[1] + a[3] / 2); cb = (b[0] + b[2] / 2, b[1] + b[3] / 2)
    return a[0] <= cb[0] <= a[0] + a[2] and a[1] <= cb[1] <= a[1] + a[3] and b[0] <= ca[0] <= b[0] + b[2] and b[1] <= ca[1] <= b[1] + b[3]


objs = []
for cls, src, b in cand:
    for o in objs:
        if o["cls"] == cls and same(o["box"], b):
            o["src"] = sorted(set(o["src"]) | {src}); break
    else:
        objs.append({"id": len(objs), "cls": cls, "src": [src], "box": [int(v) for v in b]})
json.dump(objs, open(os.path.join(od, "objects.json"), "w"))
COL = {"chip": (0, 0, 230), "gate": (230, 0, 0), "circle": (0, 150, 0), "offpage": (0, 120, 255), "poscode": (200, 0, 200)}
im = cv2.cvtColor(g, cv2.COLOR_GRAY2BGR)
for o in objs:
    x, y, w, h = o["box"]
    cv2.rectangle(im, (x, y), (x + w, y + h), COL[o["cls"]], 2)
    cv2.putText(im, str(o["id"]), (x, max(12, y - 3)), cv2.FONT_HERSHEY_SIMPLEX, 0.55, COL[o["cls"]], 2)
tw, th = W // cols + 1, H // rows + 1
for r in range(rows):
    for c in range(cols):
        cv2.imwrite(os.path.join(od, f"t{r}{c}.png"), im[r * th:(r + 1) * th + 40, c * tw:(c + 1) * tw + 40])
print(len(objs), {k: sum(o["cls"] == k for o in objs) for k in COL})
