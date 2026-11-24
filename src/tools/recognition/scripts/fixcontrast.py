"""Apply pageio.render's contrast stretch to pieces cut before it existed (harvest100), so stored
pieces match what render() now gives: ink v -> v * 255 / (255 - lo), lo = the page's 0.5 % grey level
at the dpi the page was cut at. Usage: python -I fixcontrast.py <harvestdir>"""
import site, sys, os, json
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(__file__))
import numpy as np, fitz
from books import BOOKS
hd = sys.argv[1]; BK = ["binder", "nd120", "scsi", "iosys"]
lo = {}
for p in json.load(open(hd + "/pages.json")):
    if "skip" in p: continue
    d = fitz.open(BOOKS[p["book"]][0]); pm = d[p["page"] - 1].get_pixmap(dpi=p["dpi"], colorspace=fitz.csGRAY)
    g = np.frombuffer(pm.samples, np.uint8).reshape(pm.height, pm.stride)[:, :pm.width]
    lo[(BK.index(p["book"]), p["page"])] = float(np.percentile(g, 0.5)); d.close()
print("pages with lo > 5:", sum(1 for v in lo.values() if v > 5), "of", len(lo))
for sub in ("", "/hand", "/cad"):
    f = hd + sub + "/pieces.npz"; z = dict(np.load(f))
    fac = np.array([255.0 / (255.0 - lo[(b, p)]) if lo[(b, p)] > 5 else 1.0 for b, p in z["meta"][:, :2]], np.float32)[:, None, None]
    for k in ("X", "M"):
        z[k] = np.clip(z[k].astype(np.float32) * fac, 0, 255).astype(np.uint8)
    np.savez_compressed(f, **z); print(f, "changed", int((fac[:, 0, 0] != 1).sum()))
