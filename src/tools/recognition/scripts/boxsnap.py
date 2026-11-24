# boxsnap.py <page> [--apply]: move each gate box to the nearby rectangle whose 4 edges carry the most ink.
# Search: shift +-35 px, size 0.7..1.4. Edge score = mean ink on each edge, take the weakest edge (all 4 must be drawn).
import sys, json, numpy as np
from PIL import Image
pg = sys.argv[1]; K = json.load(open(f"loop/key/{pg}.json"))
a = (np.asarray(Image.open(f"loop/pages/{pg}.png").convert("L")) < 128).astype(np.float32)
H, W = a.shape
def edge(y0, y1, x0, x1):   # max over 3-px band of mean ink along a line
    return a[y0:y1, x0:x1].mean()
def score(x, y, w, h):
    if x < 2 or y < 2 or x + w + 3 > W or y + h + 3 > H: return 0
    t = max(a[y + d, x:x + w].mean() for d in (-1, 0, 1)); b = max(a[y + h + d, x:x + w].mean() for d in (-1, 0, 1))
    l = max(a[y:y + h, x + d].mean() for d in (-1, 0, 1)); r = max(a[y:y + h, x + w + d].mean() for d in (-1, 0, 1))
    return min(t, b, l, r)
moved = 0; out = []
for g in K["gates"]:
    x, y, w, h = g["box"]; s0 = score(x, y, w, h); best = (s0, x, y, w, h)
    for sw in (0.7, 0.8, 0.9, 1.0, 1.1, 1.25, 1.4):
        for sh in (0.7, 0.8, 0.9, 1.0, 1.1, 1.25, 1.4):
            ww, hh = int(w * sw), int(h * sh)
            cx, cy = x + w / 2, y + h / 2
            for dx in range(-35, 36, 3):
                for dy in range(-35, 36, 3):
                    xx, yy = int(cx - ww / 2 + dx), int(cy - hh / 2 + dy)
                    s = score(xx, yy, ww, hh)
                    if s > best[0]: best = (s, xx, yy, ww, hh)
    # refine by 1 px
    s, bx, by, bw, bh = best
    for dx in (-2, -1, 0, 1, 2):
        for dy in (-2, -1, 0, 1, 2):
            for dw in (-2, 0, 2):
                for dh in (-2, 0, 2):
                    v = score(bx + dx, by + dy, bw + dw, bh + dh)
                    if v > best[0]: best = (v, bx + dx, by + dy, bw + dw, bh + dh)
    s, bx, by, bw, bh = best
    ok = s >= 0.6 and s > s0 + 0.1
    out.append((g["id"], round(s0, 2), round(s, 2), ok))
    if ok: g["box0"] = g["box"]; g["box"] = [bx, by, bw, bh]; moved += 1
print(pg, "gates", len(K["gates"]), "snapped", moved)
if "--apply" in sys.argv: json.dump(K, open(f"loop/key/{pg}.json", "w"), indent=1)
else:
    for o in out: print(" ", *o)
