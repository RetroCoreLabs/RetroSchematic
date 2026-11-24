"""Step 2.5: split joined letters (two letters touching = one piece) on page 64.
Pieces = the same 3056 letter-size pieces as realcomp.py, plus the wider text-height pieces
(height 18-38, width 31-90) that realcomp.py left out because they are too wide for one letter.
Method: a piece is "maybe joined" when width > 0.8 x height. Its own ink is projected onto a
line slanted like the ND lettering (shear 0.2), and it is cut at the column with the least ink
in the middle 25-75 % of its width, if that column has at most 2 ink pixels; recursively, so
three joined letters give three parts. A part counts as a single letter when height 18-38 and
width <= 0.8 x height.
Writes out/split-sheet.png (cuts drawn in red) to check by eye.
Usage: python -I split.py <page.png> <outdir>
"""
import site, sys, json
sys.path.append(site.getusersitepackages())
import numpy as np, cv2

g = cv2.imread(sys.argv[1], 0); od = sys.argv[2]
_, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
hl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (60, 1)))
vl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, 60)))
txt = cv2.bitwise_and(ink, cv2.bitwise_not(cv2.dilate(hl | vl, np.ones((3, 3), np.uint8))))
n, lab, st, _ = cv2.connectedComponentsWithStats(txt, 8)
a = st[1:]
small = ((a[:, 3] >= 18) & (a[:, 3] <= 38) & (a[:, 2] >= 4) & (a[:, 2] <= 30) & (a[:, 4] >= 40)) | \
        ((a[:, 3] >= 7) & (a[:, 3] <= 16) & (a[:, 2] >= 6) & (a[:, 2] <= 16) & (a[:, 4] >= 25))
wide = (a[:, 3] >= 18) & (a[:, 3] <= 38) & (a[:, 2] > 30) & (a[:, 2] <= 90) & (a[:, 4] >= 40)
SH = 0.2


def cut(m, depth=0):
    """m: piece mask. Returns list of (x0, x1) column ranges in sheared space plus the sheared mask."""
    h, w = m.shape
    if w <= 0.8 * h or depth > 3:
        return [m]
    ys, xs = np.nonzero(m)
    xs2 = np.round(xs + SH * (ys - h / 2)).astype(int); xs2 -= xs2.min()
    prof = np.bincount(xs2, minlength=xs2.max() + 1)
    W = len(prof); lo, hi = int(W * 0.25), int(W * 0.75)
    if hi <= lo:
        return [m]
    c = lo + int(np.argmin(prof[lo:hi]))
    if prof[c] > 2:
        return [m]
    out = []
    for sel in (xs2 < c, xs2 > c):
        if sel.sum() < 15:
            return [m]
        yy, xx = ys[sel], xs[sel]
        p = np.zeros((yy.max() - yy.min() + 1, xx.max() - xx.min() + 1), np.uint8); p[yy - yy.min(), xx - xx.min()] = 1
        out += cut(p, depth + 1)
    return out


def single(p):
    h, w = p.shape
    return 18 <= h <= 38 and w <= 0.8 * h


res = {"pieces_3056": int(small.sum()), "wide_pieces": int(wide.sum())}
stat = {}
shown = []
for name, sel in (("3056", small), ("wide", wide)):
    s = {"single_as_is": 0, "maybe_joined": 0, "split_all_parts_single": 0, "parts_from_split": 0, "not_split": 0, "split_some_not_single": 0}
    for i in np.nonzero(sel)[0]:
        x, y, w, h, _ = a[i]
        m = (lab[y:y + h, x:x + w] == i + 1).astype(np.uint8)
        if w <= 0.8 * h or h < 18:     # h < 18 = the small subscript pieces, one mark each
            s["single_as_is"] += 1; continue
        s["maybe_joined"] += 1
        parts = cut(m)
        if len(parts) == 1:
            s["not_split"] += 1
        elif all(single(p) for p in parts):
            s["split_all_parts_single"] += 1; s["parts_from_split"] += len(parts)
        else:
            s["split_some_not_single"] += 1
        if len(parts) > 1 and len(shown) < 120:
            shown.append((i, parts))
    stat[name] = s
res.update(stat)
# sheet: original piece on the left, parts to the right
T = 56; img = np.full((20 * (T + 4), 6 * (3 * T + 8)), 255, np.uint8)
for j, (i, parts) in enumerate(shown):
    r, c = divmod(j, 6); x, y, w, h, _ = a[i]
    tile = np.full((T, 3 * T), 255, np.uint8)
    m = (lab[y:y + h, x:x + w] == i + 1)
    tile[:min(T, h), :min(T + 30, w)] = np.where(m[:T, :T + 30], 0, 255)
    ox = T + 34
    for p in parts:
        ph, pw = p.shape
        if ox + pw >= 3 * T: break
        tile[:min(T, ph), ox:ox + pw] = np.where(p[:T] > 0, 0, 255); ox += pw + 6
    img[r * (T + 4):r * (T + 4) + T, c * (3 * T + 8):c * (3 * T + 8) + 3 * T] = tile
cv2.imwrite(od + "/split-sheet.png", img)
print(json.dumps(res, indent=1))
