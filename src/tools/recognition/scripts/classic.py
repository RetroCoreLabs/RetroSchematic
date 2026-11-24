"""Step B: plain image processing on one scanned ND sheet (no network).
Finds wire lines, junction dots, circles (connector pins), closed boxes, diamonds.
Usage: python -I classic.py <page.png> <outdir>
"""
import site, sys, json, time
sys.path.append(site.getusersitepackages())
import numpy as np, cv2

t0 = time.time()
src, outdir = sys.argv[1], sys.argv[2]
g = cv2.imread(src, cv2.IMREAD_GRAYSCALE)
H, W = g.shape
# ink = 1. Otsu threshold on the whole page.
_, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
res = {"size": [W, H]}

# --- stroke width: most common black run length across vertical scans
runs = []
for x in range(0, W, 7):
    col = ink[:, x] > 0
    d = np.diff(np.concatenate(([0], col.astype(np.int8), [0])))
    s = np.where(d == 1)[0]; e = np.where(d == -1)[0]
    runs.append(e - s)
runs = np.concatenate(runs)
hist = np.bincount(runs[runs < 30])
pen = int(np.argmax(hist[1:]) + 1)
res["pen_width_px"] = pen

# --- wire lines: long thin straight runs. Opening with a long bar keeps only
# straight strokes at least L pixels long (L > letter height so text is dropped).
L = 60
hl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (L, 1)))
vl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, L)))
def segs(mask, horiz):
    n, lab, st, _ = cv2.connectedComponentsWithStats(mask, 8)
    out = []
    for i in range(1, n):
        x, y, w, h, a = st[i]
        out.append([int(x), int(y), int(w), int(h)])
    return out
hs, vs = segs(hl, True), segs(vl, False)
res["h_segments"], res["v_segments"] = len(hs), len(vs)
res["h_total_len_px"] = int(sum(s[2] for s in hs)); res["v_total_len_px"] = int(sum(s[3] for s in vs))

# --- junction dots: filled blobs clearly thicker than the pen.
# Distance transform: a dot of diameter D has an inner max around D/2, a line only pen/2.
dist = cv2.distanceTransform(ink, cv2.DIST_L2, 5)
core = (dist >= pen * 0.5 + 2.0).astype(np.uint8)
n, lab, st, cen = cv2.connectedComponentsWithStats(core, 8)
# solid parts only: opening with a small disc keeps dots and the thick page frame,
# drops every pen-width stroke. A dot is a SMALL solid part; the frame is a long one.
solid = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7)))
ns, labs, sts, _ = cv2.connectedComponentsWithStats(solid, 8)
dots = []
for i in range(1, n):
    x, y, w, h, a = st[i]
    if w > 14 or h > 14:      # big filled areas (triangle heads, title fill) are not dots
        continue
    cx, cy = cen[i]
    # check the blob in the full ink is round and small: radius of filled disc
    r = dist[int(cy), int(cx)]
    if r < 4.0 or r > 12:
        continue
    k = labs[int(cy), int(cx)]
    if k == 0 or sts[k][2] > 22 or sts[k][3] > 22:
        continue
    # a junction dot sits on a wire: some long-line ink within 4 px of the dot edge
    rr = int(r) + 4
    win = (hl | vl)[max(0,int(cy)-rr):int(cy)+rr+1, max(0,int(cx)-rr):int(cx)+rr+1]
    if not win.any():
        continue
    dots.append([float(cx), float(cy), float(r)])
res["junction_dots"] = len(dots)

# --- circles: Hough on a lightly blurred page, radius range from the pin circles.
blur = cv2.GaussianBlur(g, (5, 5), 1.5)
cir = cv2.HoughCircles(blur, cv2.HOUGH_GRADIENT, dp=1, minDist=40, param1=120, param2=38,
                       minRadius=22, maxRadius=36)
circles = [] if cir is None else [[float(a), float(b), float(c)] for a, b, c in cir[0]]
# verify: ink coverage along the circle path must be high (a real drawn ring)
good = []
for cx, cy, r in circles:
    ang = np.linspace(0, 2 * np.pi, 90, endpoint=False)
    hit = 0
    for a in ang:
        px, py = int(cx + r * np.cos(a)), int(cy + r * np.sin(a))
        if 0 <= px < W and 0 <= py < H and ink[max(0,py-2):py+3, max(0,px-2):px+3].any():
            hit += 1
    if hit / len(ang) >= 0.85:
        good.append([cx, cy, r])
res["circles_hough_raw"] = len(circles); res["circles"] = len(good)

# --- closed regions (holes): white areas fully surrounded by ink.
# Boxes = holes whose outline is close to an axis-aligned rectangle.
inv = cv2.bitwise_not(ink)
contours, hier = cv2.findContours(ink, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_SIMPLE)
boxes, diamonds = [], []
for i, c in enumerate(contours):
    if hier[0][i][3] < 0:     # only inner contours (holes)
        continue
    x, y, w, h = cv2.boundingRect(c)
    a = cv2.contourArea(c)
    if w < 12 or h < 12:
        continue
    rect_fill = a / float(w * h)
    ap = cv2.approxPolyDP(c, 0.04 * cv2.arcLength(c, True), True)
    if rect_fill > 0.85 and w >= 25 and h >= 25:
        boxes.append([x, y, w, h])
    elif len(ap) == 4 and 0.4 < rect_fill < 0.65 and 12 <= w <= 40 and 8 <= h <= 30:
        diamonds.append([x, y, w, h])
# second pass: holes split by inner marks. Grow every small/medium hole by the pen
# width so pieces separated by one thin stroke join, then test the union for a rectangle.
holes = np.zeros_like(ink)
for i, c in enumerate(contours):
    if hier[0][i][3] >= 0:
        x, y, w, h = cv2.boundingRect(c)
        if w < 400 and h < 400:
            cv2.drawContours(holes, [c], -1, 255, -1)
grown = cv2.dilate(holes, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (pen + 3, pen + 3)))
n2, lab2, st2, _ = cv2.connectedComponentsWithStats(grown, 8)
single = boxes
boxes = []
for i in range(1, n2):
    x, y, w, h, a = st2[i]
    if w < 22 or h < 22:
        continue
    if a / float(w * h) > 0.85:
        boxes.append([int(x), int(y), int(w), int(h)])
def inside(a, b, m=6):
    return a[0] >= b[0]-m and a[1] >= b[1]-m and a[0]+a[2] <= b[0]+b[2]+m and a[1]+a[3] <= b[1]+b[3]+m
for b in single:
    if not any(inside(b, o) and inside(o, b) for o in boxes):
        boxes.append(b)
# drop a box that lies inside a bigger box (pin-number boxes on a chip edge stay: they are outside)
boxes = [b for b in boxes if not any(o is not b and inside(b, o, 0) and o[2]*o[3] > b[2]*b[3] for o in boxes)]
diamonds = [d for d in diamonds if not any(inside(d, b, 2) for b in boxes)]
res["boxes_all"] = len(boxes)
big = [b for b in boxes if b[2] >= 60 and b[3] >= 100]
res["boxes_big_chip_bodies"] = len(big)
res["boxes_small"] = len(boxes) - len(big)
res["diamonds"] = len(diamonds)
res["seconds"] = round(time.time() - t0, 2)

# overlay for checking by eye
ov = cv2.cvtColor(g, cv2.COLOR_GRAY2BGR)
ov[hl > 0] = (255, 160, 0); ov[vl > 0] = (255, 160, 0)
for x, y, w, h in boxes: cv2.rectangle(ov, (x, y), (x + w, y + h), (0, 160, 0), 3)
for x, y, w, h in diamonds: cv2.rectangle(ov, (x - 3, y - 3), (x + w + 3, y + h + 3), (200, 0, 200), 3)
for cx, cy, r in good: cv2.circle(ov, (int(cx), int(cy)), int(r) + 4, (0, 0, 255), 4)
for cx, cy, r in dots: cv2.circle(ov, (int(cx), int(cy)), 14, (0, 200, 255), 4)
cv2.imwrite(outdir + "/overlay.png", ov)
json.dump({"result": res, "circles": good, "dots": dots, "boxes": boxes, "diamonds": diamonds,
           "h": hs, "v": vs}, open(outdir + "/classic.json", "w"))
print(json.dumps(res, indent=1))
