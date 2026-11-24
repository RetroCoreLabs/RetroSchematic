"""Loop: answer keys -> detector training pages (real page + symbol boxes). Only chip, gate,
resistor and capacitor are labelled ("labelled" list), so the detector gets no loss for the other
classes on these pages. Keys without a box for a symbol: that page is skipped for that class.
Usage: python -I key2det.py <key dir> <pages dir> <out dir> <repeat>"""
import sys, json, glob, os, shutil
kd, pd, od, rep = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]); os.makedirs(od, exist_ok=True)
KM = {"and-or-invert": "aoi", "xor": "other", "nand": "nand", "nor": "nor", "not": "not", "and": "and", "or": "or", "buffer": "buffer"}
ok = lambda b: isinstance(b, list) and len(b) == 4 and b[2] > 3 and b[3] > 3
for f in sorted(glob.glob(kd + "/*.json")):
    K = json.load(open(f, encoding="utf-8")); p = os.path.basename(f)[:-5]
    O = []; lab = []
    ch = [q for q in K["parts"] if q.get("kind") == "chip"]
    if ch and all(ok(q.get("box")) for q in ch):
        lab.append("chip"); O += [{"cls": "chip", "box": q["box"]} for q in ch]
    G = K["gates"]
    if G and sum(ok(g.get("box")) for g in G) >= 0.9 * len(G):
        lab.append("gate"); O += [{"cls": "gate", "box": g["box"], "kind": KM.get(g["kind"], "other")} for g in G if ok(g.get("box"))]
    pa = [q for q in K["parts"] if q.get("kind") == "passive" and ok(q.get("box"))]
    for c, pre in (("resistor", ("R",)), ("capacitor", ("C",))):
        xs = [q for q in pa if str(q.get("type", "")).upper().startswith(pre) or q["ref"].upper().startswith(pre[0] + "-")]
        if xs:
            lab.append(c); O += [{"cls": c, "box": q["box"]} for q in xs]
    shutil.copy(os.path.join(pd, p + ".png"), os.path.join(od, p + ".png"))
    json.dump({"objects": O, "labelled": lab, "repeat": rep}, open(os.path.join(od, p + ".json"), "w"))
    print(p, lab, len(O))
