"""Step C: make synthetic ND-style schematic pages with exact labels.

Every drawn thing is written to a label file with its class and box, so the labels are exact
by construction. Scale imitates a 300 dpi scan of an ND drawing: pen 3-5 px, letters 22-32 px
high, pin circles radius 25-32 px, junction dots radius 5-8 px.

Letters: Windows fonts are rendered big, thinned to a one-pixel centre line (so every font
becomes a single-stroke "stencil pen" letter like the ND lettering), then per letter given a
random slant, size, wobble and pen width.

Usage: python -I gen.py <outdir> <pages> [seed]
"""
import site, sys, os, json, math, random, time
sys.path.append(site.getusersitepackages())
import numpy as np, cv2
from PIL import Image, ImageDraw, ImageFont

CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
CLASSES = list(CHARS) + ["sub0", "sub1"]
FONTS = ["arial.ttf", "ARIALN.TTF", "bahnschrift.ttf", "consola.ttf", "GOTHIC.TTF", "verdana.ttf",
         "tahoma.ttf", "lucon.ttf", "segoepr.ttf", "Inkfree.ttf", "cour.ttf"]
FONTDIR = os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Fonts")


def thin(img):
    """Zhang-Suen thinning on a 0/1 uint8 array (numpy, vectorised per pass)."""
    a = img.copy().astype(np.uint8)
    while True:
        changed = False
        for step in (0, 1):
            p = np.pad(a, 1)
            P2, P3, P4 = p[:-2, 1:-1], p[:-2, 2:], p[1:-1, 2:]
            P5, P6, P7 = p[2:, 2:], p[2:, 1:-1], p[2:, :-2]
            P8, P9 = p[1:-1, :-2], p[:-2, :-2]
            nb = [P2, P3, P4, P5, P6, P7, P8, P9]
            B = sum(x.astype(np.int32) for x in nb)
            A = sum(((nb[i] == 0) & (nb[(i + 1) % 8] == 1)).astype(np.int32) for i in range(8))
            if step == 0:
                c = (P2 * P4 * P6 == 0) & (P4 * P6 * P8 == 0)
            else:
                c = (P2 * P4 * P8 == 0) & (P2 * P6 * P8 == 0)
            m = (a == 1) & (B >= 2) & (B <= 6) & (A == 1) & c
            if m.any():
                a[m] = 0
                changed = True
        if not changed:
            return a


def build_glyphs():
    """Centre-line bitmap per (font, char), 96 px high canvas."""
    out = {}
    for f in FONTS:
        try:
            font = ImageFont.truetype(os.path.join(FONTDIR, f), 80)
        except OSError:
            continue
        for ch in CHARS:
            im = Image.new("L", (110, 110), 0)
            ImageDraw.Draw(im).text((10, 5), ch, font=font, fill=255)
            a = (np.array(im) > 128).astype(np.uint8)
            ys, xs = np.nonzero(a)
            if len(xs) == 0:
                continue
            a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
            out.setdefault(ch, []).append(thin(a))
    return out


class Pen:
    def __init__(self, img, rng):
        self.img, self.r = img, rng

    def line(self, pts, w, wobble=1.0):
        """Polyline with slow sideways wobble, drawn as ink (0) on white."""
        out = []
        for (x0, y0), (x1, y1) in zip(pts[:-1], pts[1:]):
            L = max(1.0, math.hypot(x1 - x0, y1 - y0))
            n = max(2, int(L / 12))
            nx, ny = -(y1 - y0) / L, (x1 - x0) / L
            ph, fr = self.r.uniform(0, 6.3), self.r.uniform(0.5, 2.0)
            for i in range(n + 1):
                t = i / n
                o = wobble * math.sin(ph + fr * t * 6.3) * (L / 300 + 0.4)
                out.append((x0 + (x1 - x0) * t + nx * o, y0 + (y1 - y0) * t + ny * o))
        p = np.array(out, np.int32).reshape(-1, 1, 2)
        cv2.polylines(self.img, [p], False, 0, max(1, int(w) - 1), cv2.LINE_AA)

    def circle(self, c, r, w):
        pts = []
        ph = self.r.uniform(0, 6.3)
        for i in range(41):
            a = i / 40 * 6.283
            rr = r * (1 + 0.03 * math.sin(2 * a + ph))
            pts.append((c[0] + rr * math.cos(a), c[1] + rr * math.sin(a)))
        self.line(pts, w, 0.3)

    def dot(self, c, r):
        cv2.circle(self.img, (int(c[0]), int(c[1])), int(r), 0, -1, cv2.LINE_AA)


class Page:
    def __init__(self, W, H, glyphs, rng):
        self.W, self.H, self.g, self.r = W, H, glyphs, rng
        self.img = np.full((H, W), 255, np.uint8)
        self.pen = Pen(self.img, rng)
        self.labels = []
        self.occ = np.zeros((H // 8 + 1, W // 8 + 1), bool)   # coarse occupancy, avoids overlaps
        self.pw = rng.choice([3, 3, 3, 4, 4])
        self.ch_h = rng.uniform(22, 32)
        self.font_bias = rng.randrange(1000)

    def free(self, x, y, w, h, mark=True):
        x0, y0, x1, y1 = int(x) // 8, int(y) // 8, int(x + w) // 8 + 1, int(y + h) // 8 + 1
        if x0 < 2 or y0 < 2 or x1 >= self.occ.shape[1] - 2 or y1 >= self.occ.shape[0] - 2:
            return False
        if self.occ[y0:y1, x0:x1].any():
            return False
        if mark:
            self.occ[y0:y1, x0:x1] = True
        return True

    def lab(self, cls, x, y, w, h):
        self.labels.append({"cls": cls, "box": [int(x), int(y), int(math.ceil(w)), int(math.ceil(h))]})

    # ---------- lettering
    def glyph(self, ch, x, y, h, slant, pw):
        """Draw one letter with its top-left at (x,y), height h. Returns width used."""
        r = self.r
        if ch in ("sub0", "sub1"):
            if ch == "sub0":       # ND writes the low subscript as a small, often filled, circle
                rad = r.uniform(3.5, 6.0)
                cx, cy = x + rad + 1, y + h - rad
                if r.random() < 0.6:
                    self.pen.dot((cx, cy), rad)
                else:
                    cv2.circle(self.img, (int(cx), int(cy)), int(rad), 0, max(2, pw - 1), cv2.LINE_AA)
                self.lab("sub0", cx - rad - 1, cy - rad - 1, 2 * rad + 3, 2 * rad + 3)
                return 2 * rad + 4
            hh = h * r.uniform(0.45, 0.6)
            return self._draw_glyph("1", x, y + h - hh * 0.75, hh, slant, max(2, pw - 1), "sub1")
        return self._draw_glyph(ch, x, y, h, slant, pw, ch)

    def _draw_glyph(self, ch, x, y, h, slant, pw, cls):
        r = self.r
        g = r.choice(self.g[ch])
        gh, gw = g.shape
        s = h / gh * r.uniform(0.92, 1.08)
        sx = s * r.uniform(0.75, 1.05)
        M = np.array([[sx, -slant * s, 0], [0, s, 0]], np.float32)
        ow = int(gw * sx + abs(slant) * gh * s) + 2 * pw + 6
        oh = int(gh * s) + 2 * pw + 6
        M[0, 2] = pw + 3 + max(0, slant * gh * s)
        M[1, 2] = pw + 3
        # warp the centre line, then give it a pen
        big = cv2.warpAffine(g.astype(np.float32), M, (ow, oh), flags=cv2.INTER_LINEAR)
        # small elastic wobble
        dx = cv2.GaussianBlur((np.random.rand(oh, ow).astype(np.float32) - 0.5) * 8, (0, 0), 6)
        dy = cv2.GaussianBlur((np.random.rand(oh, ow).astype(np.float32) - 0.5) * 8, (0, 0), 6)
        X, Y = np.meshgrid(np.arange(ow, dtype=np.float32), np.arange(oh, dtype=np.float32))
        big = cv2.remap(big, X + dx, Y + dy, cv2.INTER_LINEAR)
        m = (big > 0.15).astype(np.uint8)
        k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (pw + 1, pw + 1))
        m = cv2.dilate(m, k)
        ys, xs = np.nonzero(m)
        if len(xs) == 0:
            return 0
        x0, y0 = int(x) - int(xs.min()), int(y)
        H, W = self.img.shape
        if x0 < 0:
            return 0
        if x0 + ow >= W or y0 + oh >= H:
            return 0
        reg = self.img[y0:y0 + oh, x0:x0 + ow]
        reg[m > 0] = 0
        self.lab(cls, x0 + xs.min(), y0 + ys.min(), xs.max() - xs.min() + 1, ys.max() - ys.min() + 1)
        return xs.max() - xs.min() + 1 + h * self.r.uniform(0.12, 0.3)

    def text(self, s, x, y, h=None, sub=None):
        """A run of letters. Returns end x."""
        h = h or self.ch_h * self.r.uniform(0.93, 1.07)
        slant = self.r.uniform(0.08, 0.3)
        pw = max(2, self.pw - self.r.choice([0, 0, 1]))
        cx = x
        for ch in s:
            if ch == " ":
                cx += h * 0.6
                continue
            cx += self._step(ch, cx, y, h, slant, pw)
        if sub is not None:
            cx += self.glyph("sub" + sub, cx + 1, y, h, slant, pw)
        return cx

    def _step(self, ch, x, y, h, slant, pw):
        return self.glyph(ch, x, y, h, slant, pw)

    # ---------- random words
    def word(self):
        r = self.r
        k = r.random()
        if k < 0.35:
            base = "".join(r.choice("ABCDEFGHIJKLMNOPQRSTUVWXYZ") for _ in range(r.randint(2, 5)))
            if r.random() < 0.5:
                base += str(r.randint(0, 15))
            return base
        if k < 0.6:
            return r.choice(["BD", "BA", "IDATA", "DAT", "LEV", "IDENT", "SMN", "CL", "OSC", "TBMT"]) + str(r.randint(0, 15))
        return "".join(r.choice(CHARS) for _ in range(r.randint(1, 6)))

    # ---------- parts
    def chip(self, x, y):
        r, pw = self.r, self.pw
        w, h = r.randint(80, 150), r.randint(150, 500)
        if not self.free(x - 140, y - 40, w + 280, h + 80):
            return
        self.pen.line([(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)], pw + 1, 0.6)
        self.lab("box", x, y, w, h)
        ref = str(r.randint(1, 19)) + r.choice("ABCDE")
        self.text(ref, x + w * 0.25, y + h * 0.35, h=self.ch_h * 1.2)
        if r.random() < 0.7:
            self.text(r.choice(["74", "75", "81"]) + str(r.randint(0, 199)), x + 6, y + h - self.ch_h - 10, h=self.ch_h * 0.9)
        npin = int(h // 50)
        for side in (0, 1):
            for i in range(npin):
                if r.random() < 0.25:
                    continue
                py = y + 30 + i * (h - 40) / max(1, npin - 1 if npin > 1 else 1)
                px0 = x if side == 0 else x + w
                px1 = px0 - r.randint(90, 130) if side == 0 else px0 + r.randint(90, 130)
                self.pen.line([(px0, py), (px1, py)], pw - 1 if pw > 3 else pw)
                n = str(r.randint(1, 40))
                tx = (px0 - 14 - len(n) * self.ch_h * 0.85) if side == 0 else px0 + 10
                self.text(n, tx, py - self.ch_h - 4)
                if r.random() < 0.6:
                    end = px1
                    wd = self.word()
                    if side == 0:
                        self.text(wd, end - len(wd) * self.ch_h * 0.75 - 30, py - self.ch_h * 0.5, sub=r.choice("01"))
                    else:
                        self.diamond(end + 2, py)
                        self.text(wd, end + 32, py - self.ch_h * 0.5, sub=r.choice("01"))
                if r.random() < 0.15:
                    self.junction((px0 + px1) / 2, py)

    def gate(self, x, y):
        r, pw = self.r, self.pw
        w, h = r.randint(28, 45), r.randint(55, 95)
        if not self.free(x - 140, y - 40, w + 300, h + 80):
            return
        kind = r.choice(["nand", "nor", "not", "and"])
        self.pen.line([(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)], pw, 0.4)
        if kind == "nor":
            self.pen.line([(x, y), (x + w, y + h)], pw - 1, 0.3); self.pen.line([(x + w, y), (x, y + h)], pw - 1, 0.3)
        elif kind == "not":
            self.pen.line([(x, y), (x + w, y + h / 2), (x, y + h)], pw - 1, 0.3)
        elif kind == "and":
            self.pen.line([(x + w / 2, y), (x + w / 2, y + h)], pw - 1, 0.3)
            self.pen.line([(x + w / 2, y), (x + w, y + h / 2), (x + w / 2, y + h)], pw - 1, 0.3)
        self.lab("gate_" + kind, x, y, w, h)
        self.text(str(r.randint(1, 19)) + r.choice("ABCDE"), x - 4, y - self.ch_h - 6, h=self.ch_h * 0.85)
        n_in = 1 if kind == "not" else r.randint(2, 3)
        for i in range(n_in):
            py = y + h * (i + 1) / (n_in + 1)
            self.pen.line([(x, py), (x - 100, py)], pw)
            self.text(str(r.randint(1, 15)), x - 40, py - self.ch_h - 4, h=self.ch_h * 0.9)
        py = y + h / 2
        self.pen.line([(x + w, py), (x + w + 110, py)], pw)
        self.text(str(r.randint(1, 15)), x + w + 6, py - self.ch_h - 4, h=self.ch_h * 0.9)
        if r.random() < 0.5:
            self.pin_circle(x + w + 140, py)
        else:
            self.diamond(x + w + 112, py)
            self.text(self.word(), x + w + 142, py - self.ch_h * 0.5, sub=r.choice("01"))

    def pin_circle(self, cx, cy):
        rad = self.r.uniform(25, 32)
        self.pen.circle((cx, cy), rad, self.pw - 1 if self.pw > 3 else self.pw)
        self.lab("circle", cx - rad - 2, cy - rad - 2, 2 * rad + 4, 2 * rad + 4)
        n = str(self.r.randint(1, 99))
        h = self.ch_h * self.r.uniform(0.9, 1.1)
        self.text(n, cx - len(n) * h * 0.33 - 2, cy - h / 2, h=h)

    def diamond(self, x, cy):
        w, h = self.r.uniform(24, 34), self.r.uniform(13, 18)
        self.pen.line([(x, cy), (x + w / 2, cy - h / 2), (x + w, cy), (x + w / 2, cy + h / 2), (x, cy)], max(2, self.pw - 1), 0.2)
        self.lab("diamond", x - 2, cy - h / 2 - 2, w + 4, h + 4)

    def junction(self, x, y):
        rad = self.r.uniform(5, 8)
        self.pen.dot((x, y), rad)
        self.pen.line([(x, y), (x, y + self.r.choice([-1, 1]) * self.r.randint(60, 200))], self.pw)
        self.lab("junction", x - rad - 1, y - rad - 1, 2 * rad + 3, 2 * rad + 3)

    def loose_text(self, x, y):
        wd = self.word()
        if not self.free(x, y, len(wd) * self.ch_h + 40, self.ch_h + 10):
            return
        self.text(wd, x, y, sub=self.r.choice([None, "0", "1"]))


def degrade(img, rng):
    """Imitate the scan: tilt, blur, uneven ink, speckle, threshold like a 1-bit scan."""
    H, W = img.shape
    ang = rng.uniform(-0.8, 0.8)
    M = cv2.getRotationMatrix2D((W / 2, H / 2), ang, 1.0)
    f = img.astype(np.float32)
    f = cv2.warpAffine(f, M, (W, H), borderValue=255)
    f = cv2.GaussianBlur(f, (0, 0), rng.uniform(0.6, 1.4))
    # uneven ink density
    low = cv2.resize(np.random.rand(H // 64 + 1, W // 64 + 1).astype(np.float32), (W, H))
    f = 255 - (255 - f) * (0.75 + 0.4 * low)
    f += np.random.randn(H, W).astype(np.float32) * rng.uniform(4, 18)
    # speckle
    n = rng.randint(50, 600)
    ys, xs = np.random.randint(0, H, n), np.random.randint(0, W, n)
    f[ys, xs] = 0
    if rng.random() < 0.6:     # most ND pages are near 1-bit scans
        f = np.where(f < rng.uniform(110, 170), 0, 255).astype(np.float32)
    return np.clip(f, 0, 255).astype(np.uint8), M


def make_page(glyphs, rng, W=2000, H=1400):
    p = Page(W, H, glyphs, rng)
    for _ in range(rng.randint(3, 6)):
        p.chip(rng.randint(150, W - 400), rng.randint(60, H - 550))
    for _ in range(rng.randint(6, 14)):
        p.gate(rng.randint(150, W - 400), rng.randint(60, H - 200))
    for _ in range(rng.randint(3, 8)):
        x, y = rng.randint(100, W - 200), rng.randint(100, H - 100)
        if p.free(x - 50, y - 50, 260, 100):
            p.pin_circle(x, y)
            p.pen.line([(x + 32, y), (x + 160, y)], p.pw)
    for _ in range(rng.randint(10, 25)):
        p.loose_text(rng.randint(60, W - 300), rng.randint(60, H - 60))
    for _ in range(rng.randint(5, 15)):
        x, y = rng.randint(100, W - 400), rng.randint(100, H - 300)
        if p.free(x - 20, y - 20, 300, 40, mark=False):
            L = rng.randint(120, 300)
            p.pen.line([(x, y), (x + L, y)], p.pw)
            p.junction(x + rng.randint(30, L - 30), y)
    img, M = degrade(p.img, rng)
    # move labels with the tilt (boxes grow to cover the rotated box)
    out = []
    for l in p.labels:
        x, y, w, h = l["box"]
        c = np.array([[x, y, 1], [x + w, y, 1], [x, y + h, 1], [x + w, y + h, 1]], np.float32) @ M.T
        x0, y0 = c.min(0); x1, y1 = c.max(0)
        out.append({"cls": l["cls"], "box": [int(x0), int(y0), int(round(x1 - x0)), int(round(y1 - y0))]})
    return img, out


if __name__ == "__main__":
    outdir, n = sys.argv[1], int(sys.argv[2])
    seed = int(sys.argv[3]) if len(sys.argv) > 3 else 1
    os.makedirs(outdir, exist_ok=True)
    t0 = time.time()
    glyphs = build_glyphs()
    print("glyph sets", sum(len(v) for v in glyphs.values()), "in", round(time.time() - t0, 1), "s")
    rng = random.Random(seed); np.random.seed(seed)
    t1 = time.time()
    for i in range(n):
        img, labels = make_page(glyphs, rng)
        cv2.imwrite(os.path.join(outdir, f"page{i:04d}.png"), img)
        json.dump(labels, open(os.path.join(outdir, f"page{i:04d}.json"), "w"))
    print("pages", n, "seconds", round(time.time() - t1, 1))
