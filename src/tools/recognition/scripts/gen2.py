"""Step 2.3: synthetic pages with ND-like letters. Same page builder as gen.py, two changes:
 1. Letter pen width matched to the real scans. Measured (realglyphs.py): real letter strokes
    median 2.74 px (p10-p90 1.91-3.82), step-1 synthetic letters median 4.65 px (3.82-5.73).
    So the step-1 letters were too HEAVY, not too light; font centre lines are now given a
    pen of TEXT_PEN px instead of pw+1.
 2. Half of the letters are stamps of real named cut-outs from other NORD-10 pages
    (harvest/glyphbank.npz, never page 64), scaled to the word height with a small shear and
    elastic wobble. Classes with no named real group (0, O, J, M, Z) stay font-only.
Usage: python -I gen2.py <outdir> <pages> <glyphbank.npz> [seed] [text_pen_choices e.g. 2,3,3]
"""
import site, sys, os, json, random, time
sys.path.append(site.getusersitepackages())
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np, cv2
import gen

outdir, n, bankf = sys.argv[1], int(sys.argv[2]), sys.argv[3]
seed = int(sys.argv[4]) if len(sys.argv) > 4 else 11
TEXT_PEN = [int(v) for v in (sys.argv[5] if len(sys.argv) > 5 else "2,3,3").split(",")]
REAL_SHARE = 0.5

z = np.load(bankf)
flat, shapes, labels = z["flat"], z["shapes"], z["labels"]
BANK = {}
o = 0
for (h, w), L in zip(shapes, labels):
    BANK.setdefault(str(L), []).append(flat[o:o + h * w].reshape(h, w).astype(np.uint8)); o += h * w

_orig_draw = gen.Page._draw_glyph


def _stamp(self, m, x, y, h, cls):
    r = self.r
    gh, gw = m.shape
    s = h / gh * r.uniform(0.92, 1.08)
    sh = r.uniform(-0.08, 0.08)
    ow, oh = int(gw * s + abs(sh) * gh * s) + 8, int(gh * s) + 8
    M = np.array([[s, -sh * s, 4 + max(0, sh * gh * s)], [0, s, 4]], np.float32)
    big = cv2.warpAffine(m.astype(np.float32), M, (ow, oh), flags=cv2.INTER_LINEAR)
    dx = cv2.GaussianBlur((np.random.rand(oh, ow).astype(np.float32) - 0.5) * 6, (0, 0), 6)
    dy = cv2.GaussianBlur((np.random.rand(oh, ow).astype(np.float32) - 0.5) * 6, (0, 0), 6)
    X, Y = np.meshgrid(np.arange(ow, dtype=np.float32), np.arange(oh, dtype=np.float32))
    mm = (cv2.remap(big, X + dx, Y + dy, cv2.INTER_LINEAR) > 0.45).astype(np.uint8)
    ys, xs = np.nonzero(mm)
    if len(xs) == 0:
        return 0
    x0, y0 = int(x) - int(xs.min()), int(y) - int(ys.min())
    H, W = self.img.shape
    if x0 < 0 or y0 < 0 or x0 + ow >= W or y0 + oh >= H:
        return 0
    self.img[y0:y0 + oh, x0:x0 + ow][mm > 0] = 0
    self.lab(cls, x0 + xs.min(), y0 + ys.min(), xs.max() - xs.min() + 1, ys.max() - ys.min() + 1)
    return xs.max() - xs.min() + 1 + h * r.uniform(0.12, 0.3)


def _draw_glyph(self, ch, x, y, h, slant, pw, cls):
    if cls in BANK and self.r.random() < REAL_SHARE:
        return _stamp(self, self.r.choice(BANK[cls]), x, y, h, cls)
    # font centre line, pen matched to the measured real width (dilation kernel = TEXT_PEN)
    return _orig_draw(self, ch, x, y, h, slant, self.r.choice(TEXT_PEN) - 1, cls)


_orig_glyph = gen.Page.glyph


def glyph(self, ch, x, y, h, slant, pw):
    if ch == "sub0" and "sub0" in BANK and self.r.random() < REAL_SHARE:
        return _stamp(self, self.r.choice(BANK["sub0"]), x, y + h * 0.55, h * 0.45, "sub0")
    return _orig_glyph(self, ch, x, y, h, slant, pw)


gen.Page._draw_glyph = _draw_glyph
gen.Page.glyph = glyph

if __name__ == "__main__":
    os.makedirs(outdir, exist_ok=True)
    t0 = time.time()
    glyphs = gen.build_glyphs()
    rng = random.Random(seed); np.random.seed(seed)
    for i in range(n):
        img, lab = gen.make_page(glyphs, rng)
        cv2.imwrite(os.path.join(outdir, f"page{i:04d}.png"), img)
        json.dump(lab, open(os.path.join(outdir, f"page{i:04d}.json"), "w"))
    print("pages", n, "seconds", round(time.time() - t0, 1))
