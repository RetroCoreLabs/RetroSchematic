import os
# zoom.py <page n10-62|iosys237|binder96> x y w h [dpi] -> loop/zoom/<page>_x_y.png ; coords in loop/pages pixels
import sys, fitz
sys.path.insert(0, __file__.rsplit("\\",1)[0].rsplit("/",1)[0])
from books import BOOKS
pg = sys.argv[1]; x, y, w, h = map(float, sys.argv[2:6]); dpi = int(sys.argv[6]) if len(sys.argv) > 6 else 1200
if pg.startswith("n10-"):
    pdf, n, sc = os.path.join(os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..")), "docs/sources/nord-10/NORD-10-drawings.pdf"), int(pg[4:]), 300.0
    d = fitz.open(pdf); page = d[n - 1]; r = fitz.Rect(x, y, x + w, y + h) * (72.0 / sc)
else:
    import re, struct
    m = re.match(r"([a-z]+)(\d+)$", pg); book, n = m.group(1), int(m.group(2))
    d = fitz.open(BOOKS[book][0]); page = d[n - 1]
    png = open(f"{sys.argv[0].rsplit('scripts',1)[0]}loop/pages/{pg}.png", "rb").read(32)
    W, H = struct.unpack(">II", png[16:24])          # stored image size (turned clockwise unless scsi)
    rot = book != "scsi"
    if rot:   # new(x,y) = old(y, W-1-x); old width = H
        s = H / page.rect.width
        r = fitz.Rect(y, W - x - w, y + h, W - x) * (1.0 / s)
    else:
        s = W / page.rect.width; r = fitz.Rect(x, y, x + w, y + h) * (1.0 / s)
pm = page.get_pixmap(dpi=dpi, clip=r, colorspace=fitz.csGRAY)
if not pg.startswith("n10-") and rot:
    import numpy as np
    from PIL import Image
    a = np.frombuffer(pm.samples, np.uint8).reshape(pm.height, pm.stride)[:, :pm.width]
    im = Image.fromarray(np.rot90(a, -1).copy())
    o = f"{sys.argv[0].rsplit('scripts',1)[0]}loop/zoom/{pg}_{int(x)}_{int(y)}.png"; im.save(o); print(o, im.size); raise SystemExit
o = f"{sys.argv[0].rsplit('scripts',1)[0]}loop/zoom/{pg}_{int(x)}_{int(y)}.png"; pm.save(o); print(o, pm.width, pm.height)
