"""Step 2.3a: full-resolution glyph stamps from the NAMED real pieces (never page 64), and the
stroke width of real letters vs synthetic letters.
Stroke width of one letter = 2 x median distance-transform value on its centre line (thinned).
Writes harvest/glyphbank.npz (masks as one flat array + shapes + labels).
Usage: python -I realglyphs.py <pdf> <harvestdir> <synthdir> [per_label]
"""
import site, sys, json, glob
sys.path.append(site.getusersitepackages())
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np, cv2, fitz
from gen import thin

pdf, hd, sd = sys.argv[1:4]
PER = int(sys.argv[4]) if len(sys.argv) > 4 else 300
meta = np.load(hd + "/pieces.npz")["meta"]; a = np.load(hd + "/clusters.npz")["assign"]
nm = {int(l.split()[0]): l.split()[1] for l in open(hd + "/names.txt") if l[0] != "#" and l.strip()}
lab = np.array([nm.get(int(g), "") for g in a])
rng = np.random.default_rng(2)
pick = []
for L in sorted(set(nm.values())):
    idx = np.nonzero(lab == L)[0]
    pick += list(rng.choice(idx, min(PER, len(idx)), replace=False))
pick = np.array(sorted(pick, key=lambda i: meta[i][0]))


def stroke(m):
    d = cv2.distanceTransform(m.astype(np.uint8), cv2.DIST_L2, 3)
    sk = thin(m.astype(np.uint8))
    v = d[sk > 0]
    return 2 * float(np.median(v)) if len(v) else 0.0


d = fitz.open(pdf)
masks, labels, widths = [], [], []
cur = -1
for i in pick:
    pi, x, y, w, h = meta[i]
    if pi != cur:
        p = d[int(pi)].get_pixmap(dpi=300, colorspace=fitz.csGRAY)
        g = np.frombuffer(p.samples, np.uint8).reshape(p.height, p.width)
        _, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
        hl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (60, 1)))
        vl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, 60)))
        txt = cv2.bitwise_and(ink, cv2.bitwise_not(cv2.dilate(hl | vl, np.ones((3, 3), np.uint8))))
        n, cl, st, _ = cv2.connectedComponentsWithStats(txt, 8)
        cur = pi
    sub = cl[y:y + h, x:x + w]
    ids = np.bincount(sub.ravel(), minlength=2); ids[0] = 0
    m = sub == ids.argmax()
    masks.append(m); labels.append(lab[i]); widths.append(stroke(m))
flat = np.concatenate([m.ravel() for m in masks]).astype(np.uint8)
np.savez_compressed(hd + "/glyphbank.npz", flat=flat, shapes=np.array([m.shape for m in masks]), labels=np.array(labels))
real_w = np.array(widths)
# synthetic letters from the existing synth pages (step 1 generator)
sw = []
for f in sorted(glob.glob(sd + "/page*.png"))[:20]:
    g = cv2.imread(f, 0); _, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
    for l in json.load(open(f[:-4] + ".json")):
        if len(l["cls"]) == 1:
            x, y, w, h = l["box"]
            m = ink[max(0, y):y + h, max(0, x):x + w] > 0
            if m.sum() > 30: sw.append(stroke(m))
sw = np.array(sw)
print(json.dumps({"glyphs": len(masks), "real_stroke_px_median": float(np.median(real_w)),
                  "real_p10_p90": [float(np.percentile(real_w, 10)), float(np.percentile(real_w, 90))],
                  "synth_stroke_px_median": float(np.median(sw)),
                  "synth_p10_p90": [float(np.percentile(sw, 10)), float(np.percentile(sw, 90))]}))
