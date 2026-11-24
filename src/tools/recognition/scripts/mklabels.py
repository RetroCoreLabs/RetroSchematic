"""Write recog/labels-nearby/pNN.json from the pipeline output + the by-eye corrections, and count
the corrections per kind. Letters: the 160 sampled pieces per page (nearby/pNN-sample.json), each
checked on nearby/pNN-letters0/1.png. Boxes: every chip/gate box, checked on nearby/pNN-boxes.png,
labelled by the SHAPE drawn (chip, X, tri = '>' inside, ref+tri / x+tri = two-part box, plain,
line-mark = vertical line with '=' mark, none = not a symbol: title block, wire loop).
Usage: python -I mklabels.py <nearbydir> <outdir>
"""
import json, sys, os
nd, od = sys.argv[1:3]; os.makedirs(od, exist_ok=True)
SHAPE = {
 61: ["chip"] * 9 + ["none"] + ["chip"] * 6 + ["none"] * 3 + ["X", "ref+tri", "tri", "tri", "ref+tri", "ref+tri", "plain", "ref+tri", "tri", "plain",
      "line-mark", "line-mark", "line-mark", "line-mark", "x+tri", "tri", "x+tri", "plain", "plain", "X", "ref+tri", "plain", "ref+tri"],
 62: ["none", "chip", "chip", "none", "chip", "chip", "tri", "X", "tri", "none", "tri", "tri", "none", "plain", "none", "none", "none", "none", "none"],
 63: ["chip"] * 10 + ["none", "none", "chip", "chip", "none", "none", "plain", "tri", "plain", "tri", "X", "tri"] + ["plain"] * 7 + ["none", "plain", "plain", "plain"],
 65: [],
 66: ["chip", "chip", "none", "none"] + ["chip"] * 9 + ["none", "none", "plain", "none", "plain", "plain"] + ["none"] * 6,
}
PIPE = {"chip": "chip", "nor": "X", "not": "tri", "nand": "plain", "and-or-invert": "wide"}
tot = {"letters_checked": 0, "letters_changed": 0, "letters_not_a_letter": 0, "letters_other_char": 0, "boxes_checked": 0, "boxes_changed": 0}
per = {}
for pg in (61, 62, 63, 65, 66):
    U = json.load(open(os.path.join(nd, f"p{pg}-dump.json")))
    idx = json.load(open(os.path.join(nd, f"p{pg}-sample.json")))
    fix = json.load(open(os.path.join(nd, f"fix{pg}.json")))
    letters = []
    for j, k in enumerate(idx):
        pred = U["pcls"][k]; lab = fix.get(str(j), pred)
        letters.append({"box": U["pieces"][k][:4], "small": bool(U["pieces"][k][4]), "pipeline": pred, "label": lab, "checked": True})
    bx = [("chip", c["box"]) for c in U["chips"]] + [(q["kind"], q["box"]) for q in U["gates"]]
    assert len(bx) == len(SHAPE[pg]), (pg, len(bx), len(SHAPE[pg]))
    boxes = []
    for (kd, b), s in zip(bx, SHAPE[pg]):
        p = PIPE[kd]
        ok = p == s or (p == "wide" and s in ("ref+tri", "x+tri"))
        boxes.append({"box": b, "pipeline": kd, "shape": s, "pipeline_right": ok, "checked": True})
    c = {"letters_checked": len(letters), "letters_changed": sum(l["label"] != l["pipeline"] for l in letters),
         "letters_not_a_letter": sum(l["label"] == "x" for l in letters),
         "letters_other_char": sum(l["label"] not in ("x", l["pipeline"]) for l in letters),
         "boxes_checked": len(boxes), "boxes_changed": sum(not b["pipeline_right"] for b in boxes)}
    for k in tot: tot[k] += c[k]
    per[pg] = c
    unchecked = {"circles": U["circles"], "diamonds": U["diamonds"], "junction_dots": U["dots_kept"],
                 "note": "pipeline output, NOT checked by eye"}
    json.dump({"page": pg, "pdf": "docs/sources/nord-10/NORD-10-drawings.pdf", "dpi": 300, "image_size": [3507, 2496],
               "letters": letters, "boxes": boxes, "unchecked": unchecked, "corrections": c},
              open(os.path.join(od, f"p{pg}.json"), "w"), indent=0)
print(json.dumps({"per_page": per, "total": tot}, indent=0))
