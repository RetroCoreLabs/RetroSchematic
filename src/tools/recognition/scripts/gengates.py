"""Generated gate symbols for the style experiment, both drawing styles, cut with the same
pageio.gatecrop as the real candidates. Nothing from page 64 or the SCSI card is used.
  old ND style (ND-13.003.01, as in the nd-schematics skill): a tall box; empty = NAND, X = NOR,
    one diagonal = inverter, box with a triangle after it = AND, X box + triangle = AND-OR-INVERT
    (other), a line-driver triangle (other); position codes and pin numbers drawn around it.
  modern (ANSI, as on the CAD pages): drawn from the catalog's own renders in
    src/catalog/symbols/modern/shapes (read-only), scaled, thickened and noised.
  none: circles with numbers, pin-number boxes, halves of boxes, letters.
Writes <out>/gates-synth.npz: G (n,48,48) ink=255, Y (label), S (0 old, 1 modern)
Usage: python -I gengates.py <catalog dir> <out> [per class]
"""
import site, sys, os, random
sys.path.append(site.getusersitepackages())
sys.path.insert(0, os.path.dirname(__file__))
import numpy as np, cv2
from pageio import gatecrop

cat, out = sys.argv[1], sys.argv[2]
N = int(sys.argv[3]) if len(sys.argv) > 3 else 600
random.seed(0); np.random.seed(0)
FONT = [cv2.FONT_HERSHEY_SIMPLEX, cv2.FONT_HERSHEY_PLAIN, cv2.FONT_HERSHEY_DUPLEX, cv2.FONT_ITALIC | cv2.FONT_HERSHEY_SIMPLEX]


def canvas():
    return np.full((260, 260), 255, np.uint8)


def txt(im, s, x, y, sc=0.6):
    cv2.putText(im, s, (int(x), int(y)), random.choice(FONT), sc * random.uniform(0.85, 1.15), 0, random.choice([1, 2]))


def finish(im, body):
    t = random.choice([0, 0, 1, 2])
    if t: im = cv2.erode(im, np.ones((t + 1, t + 1), np.uint8))          # thicker ink
    im = cv2.GaussianBlur(im, (3, 3), random.uniform(0.3, 1.0))
    noise = np.random.normal(0, random.uniform(0, 18), im.shape)
    im = np.clip(im + noise, 0, 255).astype(np.uint8)
    _, im = cv2.threshold(im, 140, 255, cv2.THRESH_BINARY)
    x, y, w, h = body
    j = lambda v: int(v + random.uniform(-3, 3))
    return gatecrop(im, j(x), j(y), w, h)


def old_gate(kind):
    im = canvas(); lw = random.choice([2, 2, 3])
    w, h = random.randint(28, 40), random.randint(52, 72)
    if kind == "other" and random.random() < 0.3:                       # line driver triangle
        x, y = 100, 100; w, h = random.randint(45, 60), random.randint(40, 55)
        pts = np.array([[x, y], [x, y + h], [x + w, y + h // 2]], np.int32)
        cv2.polylines(im, [pts], True, 0, lw)
        txt(im, random.choice(["13A", "15A", "17A", "19A"]), x + 4, y + h // 2 + 6, 0.5)
        cv2.line(im, (x - 25, y + 12), (x, y + 12), 0, lw); cv2.line(im, (x + w, y + h // 2), (x + w + 25, y + h // 2), 0, lw)
        return finish(im, (x, y, w, h))
    x, y = 110, 95
    cv2.rectangle(im, (x, y), (x + w, y + h), 0, lw)
    if kind == "nor" or (kind == "other"):
        cv2.line(im, (x, y), (x + w, y + h), 0, lw); cv2.line(im, (x + w, y), (x, y + h), 0, lw)
    if kind == "not":
        if random.random() < 0.5: cv2.line(im, (x, y + h), (x + w, y), 0, lw)
        else: cv2.line(im, (x, y), (x + w, y + h), 0, lw)
    if kind in ("and", "other"):
        tw = random.randint(14, 22)
        pts = np.array([[x + w, y], [x + w, y + h], [x + w + tw, y + h // 2]], np.int32)
        cv2.polylines(im, [pts], True, 0, lw)
    if kind == "nand" and random.random() < 0.25:
        cv2.line(im, (x + w // 2, y + 6), (x + w // 2, y + 22), 0, lw)       # open-collector mark
    # pins and numbers
    nin = random.choice([1, 2, 2, 3]) if kind != "not" else 1
    for k in range(nin):
        py = y + int(h * (k + 1) / (nin + 1))
        cv2.line(im, (x - random.randint(20, 40), py), (x, py), 0, lw)
        if random.random() < 0.8: txt(im, str(random.randint(1, 13)), x - 22, py - 4, 0.5)
    oy = y + h // 2
    cv2.line(im, (x + w + (20 if kind in ("and", "other") else 0), oy), (x + w + 45, oy), 0, lw)
    if random.random() < 0.8: txt(im, str(random.randint(1, 13)), x + w + 6, oy - 4, 0.5)
    code = f"{random.choice([1,3,5,7,9,11,13,15,17,19])}{random.choice('ABCDE')}"
    if random.random() < 0.5: txt(im, code, x - 2, y - 6, 0.6)
    else:
        txt(im, code[:-1], x + 4, y + 22, 0.5); txt(im, code[-1], x + 8, y + 44, 0.5)
    return finish(im, (x, y, w, h))


MOD = {"nand": ["nand2", "nand3", "nand4", "nand-oc2", "nand-buf2", "nand-schmitt4"], "and": ["and2", "and3", "and4", "and-oc2"],
       "nor": ["nor2", "nor3", "nor-buf2", "nor-buf-oc2"], "not": ["not1", "not-oc1", "not-schmitt1", "not-buf-ochv1"],
       "other": ["or2", "xor2", "line-driver1", "buf-ochv1", "aoi-2x28"]}
SH = {}
for k, v in MOD.items():
    for f in v:
        p = os.path.join(cat, "symbols", "modern", "shapes", f + ".png")
        im = cv2.imread(p, cv2.IMREAD_GRAYSCALE)
        if im is not None:
            _, b = cv2.threshold(im, 160, 255, cv2.THRESH_BINARY)
            SH.setdefault(k, []).append(b)


def body_of(b):
    """bounding box of the symbol body: the largest closed white area (hole) of the render"""
    ink = 255 - b
    cs, hh = cv2.findContours(ink, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_SIMPLE)
    best = None
    for i, c in enumerate(cs):
        if hh[0][i][3] >= 0:
            a = cv2.contourArea(c)
            if best is None or a > best[0]: best = (a, cv2.boundingRect(c))
    return best[1] if best else (0, 0, b.shape[1], b.shape[0])


def modern_gate(kind):
    b = random.choice(SH[kind])
    bx, by, bw, bh = body_of(b)
    target = random.uniform(30, 50)                    # body height on the normalised page
    f = target / max(bh, bw * 0.8)
    s = cv2.resize(b, None, fx=f, fy=f, interpolation=cv2.INTER_AREA)
    im = canvas(); ox, oy = 80, 80
    h_, w_ = min(s.shape[0], 180), min(s.shape[1], 180)
    im[oy:oy + h_, ox:ox + w_] = np.minimum(im[oy:oy + h_, ox:ox + w_], s[:h_, :w_])
    if random.random() < 0.7: txt(im, random.choice(["74LS00", "74LS04", "74F32", "26H", "12C", "74LS20"]), ox, oy - 6, 0.5)
    for _ in range(random.randint(0, 3)): txt(im, str(random.randint(1, 13)), ox + random.randint(0, 40), oy + random.randint(10, 60), 0.45)
    return finish(im, (ox + int(bx * f), oy + int(by * f), max(8, int(bw * f)), max(8, int(bh * f))))


def none_item(style):
    im = canvas(); lw = random.choice([2, 3]); t = random.random()
    if t < 0.3:
        r = random.randint(18, 30); cv2.circle(im, (130, 130), r, 0, lw); txt(im, str(random.randint(1, 99)), 115, 138, 0.6)
        cv2.line(im, (130 + r, 130), (190, 130), 0, lw); body = (130 - r, 130 - r, 2 * r, 2 * r)
    elif t < 0.6:
        w, h = random.randint(22, 30), random.randint(24, 32)
        for k in range(3):
            cv2.rectangle(im, (110, 80 + k * h), (110 + w, 80 + (k + 1) * h), 0, lw); txt(im, str(random.randint(1, 16)), 113, 80 + k * h + h - 6, 0.5)
        body = (110, 80 + h, w, h)
    elif t < 0.8:                                       # half of an X / diagonal box: a triangle
        w, h = random.randint(28, 40), random.randint(26, 36)
        pts = np.array([[100, 100], [100 + w, 100], [100 + w // 2, 100 + h]], np.int32)
        if random.random() < 0.5: pts = np.array([[100, 100], [100 + w, 100 + h], [100, 100 + h]], np.int32)
        cv2.polylines(im, [pts], True, 0, lw); body = (100, 100, w, h)
    else:
        w, h = random.randint(25, 60), random.randint(25, 45)
        cv2.rectangle(im, (100, 100), (100 + w, 100 + h), 0, lw); txt(im, random.choice(["A", "B", "8", "0", "D", "R"]), 106, 100 + h - 6, 0.7)
        body = (100, 100, w, h)
    return finish(im, body)


G, Y, S = [], [], []
for style in (0, 1):
    for kind in ("nand", "nor", "not", "and", "other"):
        for _ in range(N):
            G.append(old_gate(kind) if style == 0 else modern_gate(kind)); Y.append(kind); S.append(style)
    for _ in range(N * 2):
        G.append(none_item(style)); Y.append("none"); S.append(style)
np.savez_compressed(os.path.join(out, "gates-synth.npz"), G=np.array(G, np.uint8), Y=np.array(Y), S=np.array(S))
sheet = np.vstack([np.hstack([255 - G[i] for i in range(k * 7 * N, k * 7 * N + 7 * N, N)]) for k in (0, 1)])
cv2.imwrite(os.path.join(out, "gates-synth-sample.png"), sheet)
print("generated", len(G))
