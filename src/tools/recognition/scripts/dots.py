"""Step 2.6: junction dots from the wire network. A candidate dot (the step-1 blob test in
classic.py, out/classic.json) is kept only where 3 or more wire ends meet: of the four directions
up/down/left/right, at least 3 must carry a stroke leaving the dot. A wire passing straight through counts as two ends.
Arm test: along the ray from r+3 to r+18 px out, at least 90 % of points are on ink.
Writes out/dots2-sheet.png: kept candidates first (green number), dropped after (red number).
Usage: python -I dots.py <page.png> <classic.json> <outdir>
"""
import site, sys, json
sys.path.append(site.getusersitepackages())
import numpy as np, cv2

g = cv2.imread(sys.argv[1], 0); cj = json.load(open(sys.argv[2])); od = sys.argv[3]
_, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
hl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (60, 1)))
vl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, 60)))
# arms are read from the plain ink (dilated by 1 px), not from the 60 px line masks: a measured
# first try with the line masks kept only 11 of 64, because the short stubs between a dot and a
# chip pin are under 60 px and vanish from those masks.
wire = cv2.dilate(ink, np.ones((3, 3), np.uint8)) > 0
H, W = g.shape
kept, dropped = [], []
for k, (cx, cy, r) in enumerate(cj["dots"]):
    arms = 0
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        pts = [(int(round(cx + dx * t)), int(round(cy + dy * t))) for t in np.arange(r + 3, r + 19)]
        hit = sum(1 for x, y in pts if 0 <= x < W and 0 <= y < H and wire[y, x])
        arms += hit >= 0.9 * len(pts)
    (kept if arms >= 3 else dropped).append((k, cx, cy, arms))
T = 70
allc = kept + dropped
img = np.full(((len(allc) + 13) // 14 * (T + 14), 14 * (T + 4), 3), 255, np.uint8)
for j, (k, cx, cy, arms) in enumerate(allc):
    r_, c_ = divmod(j, 14)
    t = cv2.copyMakeBorder(g, T, T, T, T, cv2.BORDER_CONSTANT, value=255)[int(cy) + T // 2:int(cy) + T // 2 + T, int(cx) + T // 2:int(cx) + T // 2 + T]
    img[r_ * (T + 14):r_ * (T + 14) + T, c_ * (T + 4):c_ * (T + 4) + T] = cv2.cvtColor(t, cv2.COLOR_GRAY2BGR)
    cv2.putText(img, f"{k}:{arms}", (c_ * (T + 4), r_ * (T + 14) + T + 11), 0, 0.4, (0, 150, 0) if j < len(kept) else (0, 0, 220), 1)
cv2.imwrite(od + "/dots2-sheet.png", img)
print(json.dumps({"candidates": len(allc), "kept_3plus_ends": len(kept), "dropped": len(dropped),
                  "kept_ids": [k for k, *_ in kept]}))
