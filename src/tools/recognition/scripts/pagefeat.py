"""Page-style measures, no network. On the render_norm page at its confidence-only turn, over the
letter pieces (pageio.pieces_of): spread of letter heights, spread of pen width (distance transform),
and how straight the long lines are. Writes style/pagefeat.json {"book:page": [features]}.
Usage: python -I pagefeat.py"""
import site, sys, os, json
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(__file__))
import numpy as np, cv2
from books import BOOKS
from pageio import render_norm, pieces_of, ink_of
R = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
rot = json.load(open(R + "/nd100/rot.json")); rot3 = json.load(open(R + "/nd100/rot3.json"))
ROT = {0: None, 90: cv2.ROTATE_90_CLOCKWISE, 180: cv2.ROTATE_180, 270: cv2.ROTATE_90_COUNTERCLOCKWISE}
N10 = os.path.join(os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..")), "docs/sources/nord-10/NORD-10-drawings.pdf")
ps = json.load(open(R + "/style/pagestyle.json"))
out = {}
for k in ps:
    b, p = k.split(":"); p = int(p)
    pdf = N10 if b == "nord10" else BOOKS[b][0]
    g, _ = render_norm(pdf, p)
    if b in ("nd120",): t = int(max(rot3[k], key=lambda a: rot3[k][a]["mean"]))
    elif b != "nord10": t = rot[k]["turn_conf_only"]
    else: t = 0
    if ROT[t] is not None: g = cv2.rotate(g, ROT[t])
    ink = ink_of(g); pcs = pieces_of(g, ink)
    if len(pcs) < 30: continue
    h = np.array([q[3] for q in pcs], float)
    dt = cv2.distanceTransform(ink, cv2.DIST_L2, 3)
    pw = np.array([2 * dt[y:y + hh, x:x + w].max() for x, y, w, hh in pcs])
    # straight lines: thickness spread along long horizontal runs
    hl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (60, 1)))
    col = (hl > 0).sum(0); thick = (hl > 0)
    n, lab, st, _ = cv2.connectedComponentsWithStats(hl, 8)
    lt = [st[i, 4] / st[i, 2] for i in range(1, n) if st[i, 2] >= 100]
    lsd = [np.std((lab[st[i, 1]:st[i, 1] + st[i, 3], st[i, 0]:st[i, 0] + st[i, 2]] == i).sum(0)) for i in range(1, n) if st[i, 2] >= 100][:200]
    out[k] = [float(np.std(h) / np.mean(h)), float(np.std(pw) / np.mean(pw)), float(np.mean(lsd) if lsd else 0), float(np.median(lt) if lt else 0)]
    print(k, [round(v, 3) for v in out[k]], flush=True)
json.dump(out, open(R + "/style/pagefeat.json", "w"))
