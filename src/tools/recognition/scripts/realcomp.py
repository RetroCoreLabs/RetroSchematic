"""Cut candidate single-letter pieces out of a real page: ink minus long wire lines,
then connected pieces of letter size. Writes real/comps.json and contact sheets to label by eye."""
import site, sys, json, random
sys.path.append(site.getusersitepackages())
import numpy as np, cv2
from PIL import Image, ImageDraw
g = cv2.imread(sys.argv[1], cv2.IMREAD_GRAYSCALE); out = sys.argv[2]
_, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
L = 60
hl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (L, 1)))
vl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, L)))
txt = cv2.bitwise_and(ink, cv2.bitwise_not(cv2.dilate(hl | vl, np.ones((3, 3), np.uint8))))
n, lab, st, cen = cv2.connectedComponentsWithStats(txt, 8)
comps = []
for i in range(1, n):
    x, y, w, h, a = st[i]
    if (18 <= h <= 38 and 4 <= w <= 30 and a >= 40) or (7 <= h <= 16 and 6 <= w <= 16 and a >= 25):
        comps.append([int(x), int(y), int(w), int(h)])
print("pieces of letter size", len(comps))
random.Random(7).shuffle(comps)
comps = comps[:300]
json.dump(comps, open(out + "/comps.json", "w"))
for s in range(6):
    sh = Image.new("RGB", (8 * 104, 6 * 112), "white"); dr = ImageDraw.Draw(sh)
    for j in range(48):
        k = s * 48 + j
        if k >= len(comps): break
        x, y, w, h = comps[k]
        cx, cy = x + w // 2, y + h // 2
        c = Image.fromarray(255 - ink[max(0, cy - 32):cy + 32, max(0, cx - 32):cx + 32]).convert("RGB").resize((96, 96))
        d2 = ImageDraw.Draw(c)
        ox, oy = cx - 32, cy - 32
        d2.rectangle([(x - ox - 1) * 1.5, (y - oy - 1) * 1.5, (x - ox + w) * 1.5, (y - oy + h) * 1.5], outline="red")
        sh.paste(c, ((j % 8) * 104, (j // 8) * 112)); dr.text(((j % 8) * 104, (j // 8) * 112 + 97), str(k), fill="blue")
    sh.save(out + f"/sheet{s}.png")
