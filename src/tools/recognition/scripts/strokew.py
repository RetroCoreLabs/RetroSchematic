"""Stroke width measure used for step 2.3 (replaces the coarse one first used in realglyphs.py):
mask padded by one blank pixel, exact Euclidean distance, width = 2 x mean distance on the
thinned centre line, minus 1 (a one-pixel line measures 1).
Usage: python -I strokew.py bank <glyphbank.npz> | synth <dir> [pages]"""
import site, sys, os, json, glob
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np, cv2
from gen import thin


def width(m):
    m = np.pad(m.astype(np.uint8), 1)
    v = cv2.distanceTransform(m, cv2.DIST_L2, cv2.DIST_MASK_PRECISE)[thin(m) > 0]
    return 2 * float(v.mean()) - 1 if len(v) else None


def run(masks):
    w = np.array([x for x in (width(m) for m in masks) if x is not None])
    return f"n {len(w)} median {np.median(w):.2f} p10 {np.percentile(w, 10):.2f} p90 {np.percentile(w, 90):.2f}"


if sys.argv[1] == "bank":
    z = np.load(sys.argv[2]); o = 0; ms = []
    for h, w in z["shapes"]:
        ms.append(z["flat"][o:o + h * w].reshape(h, w)); o += h * w
    print("real", run(ms[::3]))
else:
    ms = []
    for f in sorted(glob.glob(sys.argv[2] + "/page*.png"))[:int(sys.argv[3]) if len(sys.argv) > 3 else 20]:
        g = cv2.imread(f, 0); _, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
        for l in json.load(open(f[:-4] + ".json")):
            if len(l["cls"]) == 1:
                x, y, w, h = l["box"]; m = ink[max(0, y):y + h, max(0, x):x + w] > 0
                if m.sum() > 30: ms.append(m)
    print("synth", run(ms))
