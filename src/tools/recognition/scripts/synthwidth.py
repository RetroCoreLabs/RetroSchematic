"""Median letter stroke width (px) on generated pages, same measure as realglyphs.py.
Usage: python -I synthwidth.py <synthdir> [pages]"""
import site, sys, os, json, glob
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np, cv2
from gen import thin
sw = []
for f in sorted(glob.glob(sys.argv[1] + "/page*.png"))[:int(sys.argv[2]) if len(sys.argv) > 2 else 20]:
    g = cv2.imread(f, 0); _, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
    for l in json.load(open(f[:-4] + ".json")):
        if len(l["cls"]) == 1:
            x, y, w, h = l["box"]; m = (ink[max(0, y):y + h, max(0, x):x + w] > 0).astype(np.uint8)
            if m.sum() > 30:
                v = cv2.distanceTransform(m, cv2.DIST_L2, 3)[thin(m) > 0]
                if len(v): sw.append(2 * float(np.median(v)))
print(len(sw), "median", np.median(sw), "p10", np.percentile(sw, 10), "p90", np.percentile(sw, 90))
