"""Loop: render the 10 work pages and 3 SCSI test pages upright at draft scale (READ-ONLY on PDFs).
NORD-10 pages: 300 dpi like out/p64-300.png. ND-100 pages: render_norm (letters ~25 px), then
turned so text reads left to right (all five are drawn turned 90 degrees, text bottom-to-top).
Usage: python -I loopprep.py <outdir>"""
import site, sys, os
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(__file__))
import cv2, fitz, numpy as np
from books import BOOKS
from pageio import render_norm
od = sys.argv[1]; os.makedirs(od, exist_ok=True)
N10 = os.path.join(os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..")), "docs/sources/nord-10/NORD-10-drawings.pdf")
d = fitz.open(N10)
for p in (61, 62, 63, 65, 66):
    pm = d[p - 1].get_pixmap(dpi=300, colorspace=fitz.csGRAY)
    g = np.frombuffer(pm.samples, np.uint8).reshape(pm.height, pm.stride)[:, :pm.width]
    cv2.imwrite(f"{od}/n10-{p}.png", g)
for book, p, rot in (("binder", 8, 1), ("binder", 96, 1), ("iosys", 228, 1), ("iosys", 237, 1), ("nd120", 41, 1),
                     ("scsi", 6, 0), ("scsi", 13, 0), ("scsi", 21, 0)):
    g = render_norm(BOOKS[book][0], p)
    g = g[0] if isinstance(g, tuple) else g
    if rot: g = cv2.rotate(g, cv2.ROTATE_90_CLOCKWISE)
    cv2.imwrite(f"{od}/{book}{p}.png", g)
    print(book, p, g.shape)
