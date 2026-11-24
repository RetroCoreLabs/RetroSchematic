"""Step 6.1: generated pages with exact object boxes for the object detector, BOTH drawing styles.

Scale: letters 17-24 px high, the scale of page 64 at 300 dpi and of pageio.render_norm pages.
  hand ND style  - gen.py pen (wobble, slant), half the letters stamped from real NORD-10 cut-outs
                   (harvest/glyphbank.npz, never page 64), ND gate boxes (empty/X/diagonal/triangle),
                   diamonds, circled pin numbers, codes inside or above the symbol.
  CAD style      - straight thin lines, plotter letters (Hershey), ANSI gates from the catalogue's
                   own renders (src/catalog/symbols/modern/shapes, read-only), pentagon off-page
                   arrows, code under the chip, type above.
Pin numbers: above the wire outside the box, or inside the box next to the edge (both placements).
Text is labelled per WORD with its role: poscode / pinnum / label.
Label file: list of {"cls", "box":[x,y,w,h], "kind"(gates only)}.
Usage: python -I gendet.py <outdir> <pages> <seed> <catalog dir> <glyphbank.npz>
"""
import site, sys, os, json, math, random, time
sys.path.append(site.getusersitepackages())
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np, cv2
import gen

CLASSES = ["chip", "gate", "resistor", "capacitor", "switch", "strap", "ground", "supply", "circle",
           "offpage", "dot", "poscode", "pinnum", "label"]
KINDS = ["nand", "nor", "not", "and", "or", "aoi", "buffer", "other"]

# ---------------------------------------------------------------- real letter stamps (as gen2.py)
BANK = {}


def load_bank(f):
    z = np.load(f)
    o = 0
    for (h, w), L in zip(z["shapes"], z["labels"]):
        BANK.setdefault(str(L), []).append(z["flat"][o:o + h * w].reshape(h, w).astype(np.uint8)); o += h * w


def _stamp(self, m, x, y, h, cls):
    r = self.r
    gh, gw = m.shape
    s = h / gh * r.uniform(0.92, 1.08); sh = r.uniform(-0.08, 0.08)
    ow, oh = int(gw * s + abs(sh) * gh * s) + 8, int(gh * s) + 8
    M = np.array([[s, -sh * s, 4 + max(0, sh * gh * s)], [0, s, 4]], np.float32)
    mm = (cv2.warpAffine(m.astype(np.float32), M, (ow, oh)) > 0.45).astype(np.uint8)
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


_orig_draw = gen.Page._draw_glyph


def _draw_glyph(self, ch, x, y, h, slant, pw, cls):
    if cls in BANK and self.r.random() < 0.5:
        return _stamp(self, self.r.choice(BANK[cls]), x, y, h, cls)
    return _orig_draw(self, ch, x, y, h, slant, self.r.choice([2, 3, 3]) - 1, cls)


gen.Page._draw_glyph = _draw_glyph

# ---------------------------------------------------------------- CAD gate shapes
SHAPES = {"nand": ["nand2", "nand3", "nand4", "nand-oc2", "nand-buf2", "nand-schmitt4", "nand8"],
          "and": ["and2", "and3", "and4", "and-oc2"], "nor": ["nor2", "nor3", "nor-buf2", "nor-buf-oc2"],
          "not": ["not1", "not-oc1", "not-schmitt1", "not-buf-ochv1"], "or": ["or2"], "aoi": ["aoi-2x28"],
          "buffer": ["buf-ochv1", "line-driver1", "line-driver2"], "other": ["xor2"]}
SH = {}


def load_shapes(cat):
    for k, v in SHAPES.items():
        for f in v:
            im = cv2.imread(os.path.join(cat, "symbols", "modern", "shapes", f + ".png"), cv2.IMREAD_GRAYSCALE)
            if im is None:
                continue
            _, b = cv2.threshold(im, 160, 255, cv2.THRESH_BINARY)
            ys, xs = np.nonzero(b < 128)
            SH.setdefault(k, []).append(b[ys.min():ys.max() + 1, xs.min():xs.max() + 1])


HERSHEY = [cv2.FONT_HERSHEY_SIMPLEX, cv2.FONT_HERSHEY_PLAIN, cv2.FONT_HERSHEY_DUPLEX]


class DetPage(gen.Page):
    def __init__(self, W, H, glyphs, rng, style):
        super().__init__(W, H, glyphs, rng)
        self.style = style                          # 0 hand, 1 CAD
        self.ch_h = rng.uniform(17, 24)
        if style == 1:
            self.pw = rng.choice([2, 2, 3])
            self.font = rng.choice(HERSHEY)
        self.out = []                               # detector labels

    def add(self, cls, x, y, w, h, kind=None):
        d = {"cls": cls, "box": [int(x), int(y), int(math.ceil(w)), int(math.ceil(h))]}
        if kind:
            d["kind"] = kind
        self.out.append(d)

    # ---- drawing primitives that follow the style
    def L(self, pts, w=None):
        w = w or self.pw
        if self.style == 0:
            self.pen.line(pts, w)
        else:
            cv2.polylines(self.img, [np.array(pts, np.int32).reshape(-1, 1, 2)], False, 0, max(2, w - 1), cv2.LINE_8)

    def C(self, c, r, w=None):
        if self.style == 0:
            self.pen.circle(c, r, w or self.pw)
        else:
            cv2.circle(self.img, (int(c[0]), int(c[1])), int(r), 0, max(2, (w or self.pw) - 1))

    def word(self, s, x, y, role, h=None, sub=None):
        """One word with its role box. (x,y) top-left. Returns (x0,y0,x1,y1) or None."""
        if not s:
            return None
        h = h or self.ch_h * self.r.uniform(0.93, 1.07)
        if self.style == 0:
            s = "".join(c if c in gen.CHARS else " " for c in s.upper()).strip()   # hand letters: the 36 classes only
            if not s:
                return None
            k = len(self.labels)
            self.text(s, x, y, h=h, sub=sub)
            new = self.labels[k:]
            del self.labels[k:]
            if not new:
                return None
            b = np.array([l["box"] for l in new])
            x0, y0 = b[:, 0].min(), b[:, 1].min(); x1, y1 = (b[:, 0] + b[:, 2]).max(), (b[:, 1] + b[:, 3]).max()
        else:
            sc = h / 22.0 * (1.0 if self.font != cv2.FONT_HERSHEY_PLAIN else 1.9)
            t = max(1, self.pw - 1)
            (tw, th), _ = cv2.getTextSize(s, self.font, sc, t)
            H, W = self.img.shape
            if x < 2 or y < 2 or x + tw + 4 >= W or y + th + 4 >= H:
                return None
            cv2.putText(self.img, s, (int(x), int(y + th)), self.font, sc, 0, t, cv2.LINE_8)
            if sub == "0":                          # CAD writes active low as a trailing ~
                cv2.putText(self.img, "~", (int(x + tw + 1), int(y + th)), self.font, sc, 0, t)
                tw += th * 0.8
            ys, xs = np.nonzero(self.img[int(y) - 2:int(y + th + 6), int(x) - 2:int(x + tw + 4)] < 128)
            if len(xs) == 0:
                return None
            x0, y0 = x - 2 + xs.min(), y - 2 + ys.min(); x1, y1 = x - 1 + xs.max(), y - 1 + ys.max()
        self.add(role, x0, y0, x1 - x0 + 1, y1 - y0 + 1)
        return x0, y0, x1, y1

    def wlen(self, s, h=None):
        return len(s) * (h or self.ch_h) * 0.8 + 6

    # ---- names
    def code(self):
        r = self.r
        if r.random() < 0.08:
            return f"{r.randint(1, 9)}-{r.randint(2, 10)}{r.choice('ABCDE')}-{r.choice('ABCDE')}"
        if self.style == 1:
            return f"{r.randint(1, 30)}{r.choice('ABCDEFGHJ')}"
        return f"{r.randint(1, 19)}{r.choice('ABCDE')}"

    def signame(self):
        w = gen.Page.word(self)
        return w[:7]

    def chiptype(self):
        r = self.r
        if self.style == 1:
            return r.choice(["74LS", "74F", "74S", "74ALS", "74HC"]) + str(r.choice([0, 4, 32, 74, 125, 138, 157, 161, 244, 245, 373, 393, 595]))
        return r.choice(["74", "75", "81", "DM8", ""]) + str(r.randint(0, 199))

    # ---- chip box
    def chip(self, x, y):
        r, st = self.r, self.style
        w, h = r.randint(70, 150), r.randint(90, 480 if not CHIPMANY else 1100)
        if CHIPMANY and r.random() < 0.3:
            h = r.randint(90, 250)
        if not self.free(x - 160, y - 50, w + 320, h + 100):
            return
        self.L([(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)], self.pw + (1 if st == 0 else 0))
        self.add("chip", x - 1, y - 1, w + 2, h + 2)
        code, typ = self.code(), self.chiptype()
        if st == 0:
            if r.random() < 0.8:
                self.word(code, x + w * 0.2, y + h * r.uniform(0.25, 0.5), "poscode", h=self.ch_h * 1.25)
            if typ and r.random() < 0.6:
                self.word(typ, x + 5, y + h - self.ch_h - 8, "label", h=self.ch_h * 0.85)
        else:
            self.word(typ, x, y - self.ch_h - 8, "label")
            self.word(code, x + w * r.uniform(0.3, 0.6), y + h + 6, "poscode")
        inside = r.random() < 0.35                   # pin numbers inside the box next to the edge
        step = r.choice([17, 20, 26, 32, 40]) if st == 1 else r.choice([36, 44, 52])   # CAD chips: pins as tight as 17 px (SCSI card)
        npin = max(1, int((h - 20) // step))
        for side in (0, 1):
            for i in range(npin):
                if r.random() < 0.2:
                    continue
                py = y + 16 + i * step
                if py > y + h - 10:
                    break
                px0 = x if side == 0 else x + w
                ln = r.randint(70, 140)
                px1 = px0 - ln if side == 0 else px0 + ln
                self.L([(px0, py), (px1, py)], self.pw if st == 1 else max(2, self.pw - 1))
                n = str(r.randint(1, 40))
                hn = self.ch_h * 0.9
                if inside:
                    tx = x + 4 if side == 0 else x + w - self.wlen(n, hn)
                    self.word(n, tx, py - hn / 2, "pinnum", h=hn)
                else:
                    tx = px0 - 6 - self.wlen(n, hn) if side == 0 else px0 + 6
                    self.word(n, tx, py - hn - 4, "pinnum", h=hn)
                if st == 1 and not inside and r.random() < 0.6:   # CAD pin names inside the box
                    nm = r.choice(["A", "B", "Q", "D", "CK", "CLR", "EN", "OE", "1A", "2Y", "QA", "D0"]) + (str(r.randint(0, 7)) if r.random() < 0.4 else "")
                    tx = x + 4 if side == 0 else x + w - self.wlen(nm, hn)
                    self.word(nm, tx, py - hn / 2, "label", h=hn)
                if r.random() < 0.6:
                    self.endpoint(px1, py, side)
                if r.random() < 0.12:
                    self.dot((px0 + px1) / 2, py)

    def endpoint(self, ex, ey, side):
        """Something at a wire end: off-page marker + name, circled pin, or just a name."""
        r = self.r
        wd = self.signame(); sub = r.choice(["0", "1"]) if self.style == 0 else r.choice([None, "0"])
        k = r.random()
        if side == 0:
            if k < 0.5:
                b = self.offpage(ex - 2, ey, left=True)
                ex = b[0] - 2 if b else ex
            self.word(wd, ex - self.wlen(wd) - 12, ey - self.ch_h * 0.5, "label", sub=sub)
        else:
            if k < 0.5:
                b = self.offpage(ex + 2, ey)
                ex = b[2] + 2 if b else ex
            elif k < 0.7 and self.style == 0:
                self.circle(ex + 30, ey)
                return
            self.word(wd, ex + 10, ey - self.ch_h * 0.5, "label", sub=sub)

    # ---- gates
    def gate(self, x, y):
        r, st = self.r, self.style
        kind = r.choice(KINDS[:7] if st == 0 else KINDS)
        if st == 0 and kind == "or":
            kind = "nand"                          # ND hand style has no separate OR outline (D-shape is NAND)
        if not self.free(x - 140, y - 50, 320, 190):
            return
        if st == 1:
            return self.cad_gate(x, y, kind)
        pw = self.pw
        w, h = r.randint(26, 42), r.randint(50, 90)
        if kind == "buffer" and r.random() < 0.5:     # line driver/receiver triangle
            w, h = r.randint(40, 60), r.randint(36, 52)
            self.L([(x, y), (x, y + h), (x + w, y + h / 2), (x, y)], pw)
            box = (x, y, w, h); oy = y + h / 2; ox = x + w
            if r.random() < 0.6:
                self.word(self.code(), x + 3, y + h / 2 - self.ch_h * 0.4, "poscode", h=self.ch_h * 0.75)
        else:
            if kind == "nand" and r.random() < 0.3:   # D shape
                self.L([(x + w * 0.4, y), (x, y), (x, y + h), (x + w * 0.4, y + h)], pw)
                pts = [(x + w * 0.4 + w * 0.6 * math.sin(t), y + h / 2 - h / 2 * math.cos(t)) for t in np.linspace(0, math.pi, 14)]
                self.L(pts, pw)
            else:
                self.L([(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)], pw)
            if kind in ("nor", "aoi"):
                self.L([(x, y), (x + w, y + h)], pw - 1); self.L([(x + w, y), (x, y + h)], pw - 1)
            if kind in ("not",) or (kind == "buffer"):
                self.L([(x, y + h), (x + w, y)] if r.random() < 0.5 else [(x, y), (x + w, y + h)], pw - 1)
            tw = 0
            if kind in ("and", "aoi"):
                tw = r.randint(14, 22)
                self.L([(x + w, y), (x + w + tw, y + h / 2), (x + w, y + h)], pw)
            box = (x, y, w + tw, h); oy = y + h / 2; ox = x + w + tw
            c = self.code()
            if r.random() < 0.5:
                self.word(c, x - 2, y - self.ch_h - 6, "poscode", h=self.ch_h * 0.9)
            elif kind in ("nand", "and") and len(c) <= 3:        # code written inside the box, digits over letter
                self.word(c[:-1], x + 3, y + 4, "poscode", h=self.ch_h * 0.75)
                self.word(c[-1], x + 6, y + h - self.ch_h * 0.75 - 5, "poscode", h=self.ch_h * 0.75)
        self.add("gate", box[0] - 2, box[1] - 2, box[2] + 4, box[3] + 4, kind)
        self.gate_wires(box, ox, oy, kind)

    def cad_gate(self, x, y, kind):
        r = self.r
        b = r.choice(SH[kind])
        f = r.uniform(42, 70) / max(b.shape[0], b.shape[1] * 0.75)
        s = cv2.resize(b, None, fx=f, fy=f, interpolation=cv2.INTER_AREA)
        _, s = cv2.threshold(s, 170, 255, cv2.THRESH_BINARY)
        if self.pw >= 3:
            s = cv2.erode(s, np.ones((2, 2), np.uint8))
        hh, ww = s.shape
        H, W = self.img.shape
        if x + ww + 200 >= W or y + hh + 60 >= H:
            return
        self.img[y:y + hh, x:x + ww] = np.minimum(self.img[y:y + hh, x:x + ww], s)
        ys, xs = np.nonzero(s < 128)
        box = (x + xs.min(), y + ys.min(), xs.max() - xs.min() + 1, ys.max() - ys.min() + 1)
        self.add("gate", box[0] - 2, box[1] - 2, box[2] + 4, box[3] + 4, kind)
        self.word(self.chiptype(), x, y - self.ch_h - 8, "label")
        self.word(self.code(), x + ww * 0.3, y + hh + 6, "poscode")
        self.gate_wires(box, x + ww, y + hh / 2, kind, drawn_stubs=True)

    def gate_wires(self, box, ox, oy, kind, drawn_stubs=False):
        r = self.r
        x, y, w, h = box
        n_in = 1 if kind in ("not", "buffer") else r.randint(2, 3)
        for i in range(n_in):
            py = y + h * (i + 1) / (n_in + 1)
            self.L([(x, py), (x - r.randint(70, 110), py)])
            self.word(str(r.randint(1, 15)), x - 34, py - self.ch_h - 4, "pinnum", h=self.ch_h * 0.9)
            if r.random() < 0.4:
                self.endpoint(x - 110, py, 0)
        self.L([(ox, oy), (ox + r.randint(80, 120), oy)])
        self.word(str(r.randint(1, 15)), ox + 6, oy - self.ch_h - 4, "pinnum", h=self.ch_h * 0.9)
        if r.random() < 0.6:
            self.endpoint(ox + 120, oy, 1)

    # ---- small symbols
    def circle(self, cx, cy):
        rad = self.r.uniform(18, 27)
        self.C((cx, cy), rad, max(2, self.pw - 1))
        self.add("circle", cx - rad - 2, cy - rad - 2, 2 * rad + 4, 2 * rad + 4)
        n = str(self.r.randint(1, 99)); h = self.ch_h * 0.95
        self.word(n, cx - len(n) * h * 0.33 - 2, cy - h / 2, "pinnum", h=h)

    def offpage(self, x, cy, left=False):
        r = self.r
        if self.style == 0:
            w, h = r.uniform(20, 30), r.uniform(12, 17)
            x0 = x - w if left else x
            self.L([(x0, cy), (x0 + w / 2, cy - h / 2), (x0 + w, cy), (x0 + w / 2, cy + h / 2), (x0, cy)], max(2, self.pw - 1))
        else:
            w, h = r.uniform(34, 50), r.uniform(14, 20)
            x0 = x - w if left else x
            if r.random() < 0.5:     # pentagon pointing right
                pts = [(x0, cy - h / 2), (x0 + w * 0.7, cy - h / 2), (x0 + w, cy), (x0 + w * 0.7, cy + h / 2), (x0, cy + h / 2), (x0, cy - h / 2)]
            else:                    # pointing left / both ends
                pts = [(x0 + w * 0.3, cy - h / 2), (x0 + w, cy - h / 2), (x0 + w, cy + h / 2), (x0 + w * 0.3, cy + h / 2), (x0, cy), (x0 + w * 0.3, cy - h / 2)]
            self.L(pts, self.pw + r.choice([0, 0, 2]))
            if r.random() < 0.1:
                w, h = h, w
        self.add("offpage", x0 - 2, cy - h / 2 - 2, w + 4, h + 4)
        return (x0, cy - h / 2, x0 + w, cy + h / 2)

    def dot(self, x, y):
        rad = self.r.uniform(3.5, 6.5)
        self.pen.dot((x, y), rad)
        self.add("dot", x - rad - 1, y - rad - 1, 2 * rad + 3, 2 * rad + 3)

    def resistor(self, x, y):
        r = self.r
        vert = r.random() < 0.6
        a, b = r.uniform(9, 14), r.uniform(30, 48)
        w, h = (a, b) if vert else (b, a)
        if not self.free(x - 60, y - 60, w + 120, h + 120):
            return
        self.L([(x, y), (x + w, y), (x + w, y + h), (x, y + h), (x, y)])
        if vert:
            self.L([(x + w / 2, y), (x + w / 2, y - 30)]); self.L([(x + w / 2, y + h), (x + w / 2, y + h + 30)])
            if r.random() < 0.3:
                self.supply(x + w / 2, y - 30)
        else:
            self.L([(x, y + h / 2), (x - 30, y + h / 2)]); self.L([(x + w, y + h / 2), (x + w + 30, y + h / 2)])
        self.add("resistor", x - 2, y - 2, w + 4, h + 4)
        val = r.choice(["1k", "47k", "220", "10k", "1KOHM", "220OHM", "4.7k"])
        if self.style == 1:
            self.word("R" + str(r.randint(1, 60)), x + w + 6, y - 4, "label")
        self.word(val, x + w + 6, y + h / 2, "label", h=self.ch_h * 0.85)

    def capacitor(self, x, y):
        r = self.r
        vert = r.random() < 0.5
        pl, gap = r.uniform(22, 34), r.uniform(6, 10)
        if not self.free(x - 60, y - 60, 160, 160):
            return
        if vert:     # plates horizontal, leads vertical
            self.L([(x - pl / 2, y), (x + pl / 2, y)], self.pw + 1); self.L([(x - pl / 2, y + gap), (x + pl / 2, y + gap)], self.pw + 1)
            self.L([(x, y), (x, y - 28)]); self.L([(x, y + gap), (x, y + gap + 28)])
            self.add("capacitor", x - pl / 2 - 2, y - 4, pl + 4, gap + 8)
        else:
            self.L([(x, y - pl / 2), (x, y + pl / 2)], self.pw + 1); self.L([(x + gap, y - pl / 2), (x + gap, y + pl / 2)], self.pw + 1)
            self.L([(x, y), (x - 28, y)]); self.L([(x + gap, y), (x + gap + 28, y)])
            self.add("capacitor", x - 4, y - pl / 2 - 2, gap + 8, pl + 4)
        self.word(r.choice(["10uF", "1000pF", "220pF", "33PF", "2200pF", "100nF"]), x + pl / 2 + 6, y - self.ch_h, "label", h=self.ch_h * 0.85)
        if r.random() < 0.3:
            self.ground(x, y + gap + 28) if vert else None

    def switch(self, x, y):
        """A switch block: n switches in a column, each a wire, a break with a slanted lever and contacts."""
        r = self.r
        n = r.randint(2, 6); step = r.uniform(24, 34)
        if not self.free(x - 60, y - 40, 260, n * step + 80):
            return
        for i in range(n):
            py = y + i * step
            self.L([(x, py), (x + 20, py)]); self.L([(x + 50, py), (x + 70, py)])
            self.L([(x + 20, py), (x + 48, py - 12)], max(2, self.pw - 1))
            self.C((x + 50, py), 2.5, 2)
        self.L([(x, y), (x, y + (n - 1) * step)])
        self.add("switch", x - 4, y - 16, 78, (n - 1) * step + 22)
        if r.random() < 0.7:
            self.word(self.code(), x - 10, y - self.ch_h - 22, "poscode")
        for i in range(n):
            if r.random() < 0.6:
                self.word(self.signame(), x + 80, y + i * step - self.ch_h / 2, "label", h=self.ch_h * 0.85)

    def strap(self, x, y):
        """Strap / jumper: two small open contacts with a link line (drawn as in the ND parts drawings).
        ASSUMPTION: the exact ND strap symbol was not checked on a page here."""
        r = self.r
        if not self.free(x - 40, y - 40, 140, 80):
            return
        rad = r.uniform(3, 5); L = r.uniform(18, 30)
        self.C((x, y), rad, 2); self.C((x + L, y), rad, 2)
        if r.random() < 0.7:
            self.L([(x + rad, y - rad), (x + L - rad, y - rad)], 2)
        self.L([(x - rad, y), (x - 30, y)]); self.L([(x + L + rad, y), (x + L + 30, y)])
        self.add("strap", x - rad - 3, y - rad - 4, L + 2 * rad + 6, 2 * rad + 8)
        if r.random() < 0.5:
            self.word("STRAP" if r.random() < 0.5 else "S" + str(r.randint(1, 9)), x, y - self.ch_h - 12, "label", h=self.ch_h * 0.8)

    def ground(self, x, y):
        r = self.r
        if self.style == 0 or r.random() < 0.5:   # three shrinking bars
            w = r.uniform(18, 28)
            for k in range(3):
                ww = w * (1 - 0.3 * k)
                self.L([(x - ww / 2, y + k * 5), (x + ww / 2, y + k * 5)], max(2, self.pw - 1))
            self.add("ground", x - w / 2 - 2, y - 3, w + 4, 16)
        else:                                     # small triangle / single bar
            w = r.uniform(10, 16)
            self.L([(x - w / 2, y), (x + w / 2, y), (x, y + w * 0.7), (x - w / 2, y)])
            self.add("ground", x - w / 2 - 2, y - 3, w + 4, w * 0.7 + 6)
        self.L([(x, y), (x, y - 25)])

    def supply(self, x, y):
        w = self.r.uniform(14, 26)
        self.L([(x - w / 2, y), (x + w / 2, y)], self.pw + 1)
        self.add("supply", x - w / 2 - 2, y - 4, w + 4, 8)
        if self.r.random() < 0.6:
            self.word(self.r.choice(["+5V", "+12V", "-12V", "H", "VCC"]), x + w / 2 + 4, y - self.ch_h - 2, "label", h=self.ch_h * 0.85)


CHIPMANY = os.environ.get("CHIPMANY") == "1"   # phase-2 pages: more and taller chip boxes (SCSI chips reach 900 px)


def make_page(glyphs, rng, style, W=1800, H=1300):
    if CHIPMANY:
        H = 1600
    p = DetPage(W, H, glyphs, rng, style)
    R = lambda a, b: rng.randint(a, b)
    for _ in range(R(2, 5) if not CHIPMANY else R(5, 10)):
        p.chip(R(170, W - 330), R(60, H - 400))
    for _ in range(R(5, 12)):
        p.gate(R(160, W - 340), R(60, H - 200))
    for f, k in ((p.resistor, (2, 6)), (p.capacitor, (1, 4)), (p.switch, (0, 2)), (p.strap, (0, 2))):
        for _ in range(R(*k)):
            f(R(100, W - 300), R(100, H - 250))
    for _ in range(R(2, 6)):
        x, y = R(120, W - 200), R(120, H - 120)
        if p.free(x - 40, y - 40, 200, 80):
            p.circle(x, y); p.L([(x + 27, y), (x + 140, y)])
    for _ in range(R(2, 6)):
        x, y = R(120, W - 200), R(120, H - 150)
        if p.free(x - 30, y - 40, 60, 90):
            p.ground(x, y)
    for _ in range(R(1, 4)):
        x, y = R(120, W - 200), R(150, H - 150)
        if p.free(x - 30, y - 40, 60, 90):
            p.supply(x, y); p.L([(x, y), (x, y + 60)])
    for _ in range(R(4, 12)):
        x, y = R(80, W - 300), R(80, H - 80)
        if p.free(x, y, 220, 40):
            p.word(p.signame(), x, y, "label", sub=rng.choice([None, "0", "1"]) if style == 0 else None)
    for _ in range(R(5, 14)):     # wires with junction dots
        x, y = R(100, W - 400), R(100, H - 300)
        if p.free(x - 20, y - 20, 300, 40, mark=False):
            Ln = R(120, 300)
            p.L([(x, y), (x + Ln, y)])
            j = x + R(30, Ln - 30)
            p.dot(j, y); p.L([(j, y), (j, y + rng.choice([-1, 1]) * R(60, 200))])
    if style == 1 and rng.random() < 0.7:          # frame and grid letters of a CAD sheet
        p.L([(20, 20), (W - 20, 20), (W - 20, H - 20), (20, H - 20), (20, 20)], 3)
    img, M = gen.degrade(p.img, rng) if style == 0 else degrade_cad(p.img, rng)
    out = []
    for l in p.out:
        x, y, w, h = l["box"]
        c = np.array([[x, y, 1], [x + w, y, 1], [x, y + h, 1], [x + w, y + h, 1]], np.float32) @ M.T
        x0, y0 = c.min(0); x1, y1 = c.max(0)
        d = dict(l); d["box"] = [int(x0), int(y0), int(round(x1 - x0)), int(round(y1 - y0))]
        out.append(d)
    return img, out


def degrade_cad(img, rng):
    """A photocopied plotter page: small tilt, little blur, a 1-bit threshold, a few specks."""
    H, W = img.shape
    M = cv2.getRotationMatrix2D((W / 2, H / 2), rng.uniform(-0.4, 0.4), 1.0)
    f = cv2.warpAffine(img.astype(np.float32), M, (W, H), borderValue=255)
    f = cv2.GaussianBlur(f, (0, 0), rng.uniform(0.4, 1.0))
    f += np.random.randn(H, W).astype(np.float32) * rng.uniform(2, 10)
    n = rng.randint(20, 300)
    f[np.random.randint(0, H, n), np.random.randint(0, W, n)] = 0
    if rng.random() < 0.7:
        f = np.where(f < rng.uniform(120, 190), 0, 255).astype(np.float32)
    return np.clip(f, 0, 255).astype(np.uint8), M


if __name__ == "__main__":
    outdir, n, seed, cat, bank = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5]
    os.makedirs(outdir, exist_ok=True)
    load_bank(bank); load_shapes(cat)
    glyphs = gen.build_glyphs()
    rng = random.Random(seed); np.random.seed(seed)
    t0 = time.time()
    for i in range(n):
        style = i % 2
        img, lab = make_page(glyphs, rng, style)
        cv2.imwrite(os.path.join(outdir, f"d{seed}_{i:04d}.jpg"), img, [cv2.IMWRITE_JPEG_QUALITY, 88])
        json.dump({"style": style, "objects": lab}, open(os.path.join(outdir, f"d{seed}_{i:04d}.json"), "w"))
    print("pages", n, "seconds", round(time.time() - t0, 1))
