"""Step 3: whole-page DRAFT of one scanned ND sheet, no network at run time except the small
letter classifier (ONNX). Reads the page, writes a draft nd-netlist sheet (.rsc JSON) and a
words/boxes/wires dump for scoring. Nothing from the hand reading is used here.
Usage: python -I draft.py <page.png> <model.onnx> <catalog dir> <outdir> [sheet id]
"""
import site, sys, json, time, os, re, glob
sys.path.append(site.getusersitepackages())
import numpy as np, cv2, onnxruntime as ort

T0 = time.time()
page, onnx_path, catdir, od = sys.argv[1:5]
SID = sys.argv[5] if len(sys.argv) > 5 else "draft"
os.makedirs(od, exist_ok=True)
CLASSES = list("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ") + ["sub0", "sub1"]
g = cv2.imread(page, 0); H, W = g.shape
_, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
times = {}

# ---------------- 1. the same plain image processing as classic.py / dots.py
sys.argv = ["classic.py", page, od]
exec(open(os.path.join(os.path.dirname(__file__), "classic.py")).read().split("# overlay for checking by eye")[0])
times["classic"] = round(time.time() - T0, 2)
wire = cv2.dilate(ink, np.ones((3, 3), np.uint8)) > 0
# FIX1b: more symbol boxes. A gate's inner mark (X, diagonal) or a pin wire cut its hole in
# pieces, so the hole test misses it. Keep only straight strokes of 25 px or more (marks and
# letters go), then look for rectangular holes again.
if os.environ.get("BOX2") == "1":
    _m = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (25, 1))) |          cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, 25)))
    _m = cv2.dilate(_m, np.ones((3, 3), np.uint8))
    _cs, _h = cv2.findContours(_m, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_SIMPLE)
    def _ov(a, b):
        return min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0]) > 0.5 * min(a[2], b[2]) and                min(a[1] + a[3], b[1] + b[3]) - max(a[1], b[1]) > 0.5 * min(a[3], b[3])
    _new = []
    for _i, _c in enumerate(_cs):
        if _h[0][_i][3] < 0: continue
        bx, by, bw, bh = cv2.boundingRect(_c)
        if bw >= 25 and bh >= 40 and bw < 600 and cv2.contourArea(_c) / (bw * bh) > 0.85 and                 not any(_ov([bx, by, bw, bh], o) for o in boxes):
            _new.append([bx, by, bw, bh])
    # a narrow box glued to the side of a big box is that chip's pin-number column, not a symbol
    _big = [b for b in boxes + _new if b[2] >= 60 and b[3] >= 100]
    for b in _new:
        if b[2] < 45 and any(abs(b[0] + b[2] - o[0]) < 8 or abs(b[0] - (o[0] + o[2])) < 8 for o in _big
                             if min(b[1] + b[3], o[1] + o[3]) - max(b[1], o[1]) > 0.5 * b[3]):
            continue
        boxes.append(b)
kdots = []
for cx, cy, r in dots:                      # dots.py rule: 3 or more arms leave the dot
    arms = 0
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        pts = [(int(round(cx + dx * t)), int(round(cy + dy * t))) for t in np.arange(r + 3, r + 19)]
        arms += sum(1 for x, y in pts if 0 <= x < W and 0 <= y < H and wire[y, x]) >= 0.9 * len(pts)
    if arms >= 3:
        kdots.append((cx, cy))

# ---------------- 2. letters: pieces of ink that are not long lines, split joined ones, classify
txt = cv2.bitwise_and(ink, cv2.bitwise_not(cv2.dilate(hl | vl, np.ones((3, 3), np.uint8))))
n, lab, st, _ = cv2.connectedComponentsWithStats(txt, 8)
SH = 0.2
def cut(m, depth=0):                        # split.py's cut, returning (x offset, mask)
    h, w = m.shape
    if w <= 0.8 * h or depth > 3:
        return [(0, m)]
    ys, xs = np.nonzero(m)
    xs2 = np.round(xs + SH * (ys - h / 2)).astype(int); xs2 -= xs2.min()
    prof = np.bincount(xs2, minlength=xs2.max() + 1)
    lo, hi = int(len(prof) * 0.25), int(len(prof) * 0.75)
    if hi <= lo:
        return [(0, m)]
    c = lo + int(np.argmin(prof[lo:hi]))
    if prof[c] > 2:
        return [(0, m)]
    out = []
    for sel in (xs2 < c, xs2 > c):
        if sel.sum() < 15:
            return [(0, m)]
        yy, xx = ys[sel], xs[sel]
        p = np.zeros((h, xx.max() - xx.min() + 1), np.uint8); p[yy, xx - xx.min()] = 1
        out += [(xx.min() + o, q) for o, q in cut(p, depth + 1)]
    return out
pieces = []   # x, y, w, h (page coords) of each letter
for i in range(1, n):
    x, y, w, h, a = st[i]
    if 18 <= h <= 38 and 4 <= w <= 90 and a >= 40:
        m = (lab[y:y + h, x:x + w] == i).astype(np.uint8)
        for ox, p in (cut(m) if w > 30 else [(0, m)]):
            ys, xs = np.nonzero(p)
            pieces.append([int(x + ox + xs.min()), int(y + ys.min()), int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1), 0])
    elif 7 <= h <= 16 and 6 <= w <= 16 and a >= 25:
        pieces.append([int(x), int(y), int(w), int(h), 1])   # small: a subscript (or a dot / dash)
WIN, SZ = 48, 32
pad = cv2.copyMakeBorder(g, WIN, WIN, WIN, WIN, cv2.BORDER_CONSTANT, value=255)
X = np.zeros((len(pieces), 1, SZ, SZ), np.float32)
for k, (x, y, w, h, s) in enumerate(pieces):
    cx, cy = int(x + w / 2) + WIN, int(y + h / 2) + WIN
    c = pad[cy - WIN // 2:cy + WIN // 2, cx - WIN // 2:cx + WIN // 2]
    X[k, 0] = (255 - cv2.resize(c, (SZ, SZ), interpolation=cv2.INTER_AREA)).astype(np.float32) / 255
sess = ort.InferenceSession(onnx_path, providers=["CPUExecutionProvider"])
if sess.get_inputs()[0].shape[1] == 2:      # style-input letter model (lettstyle.py setup C): page style as 2nd channel
    X = np.concatenate([X, np.full_like(X, float(os.environ.get("GATESTYLE", "0")))], 1)
logit = sess.run(None, {"x": X})[0] if len(X) else np.zeros((0, len(CLASSES)))
if logit.shape[1] == len(CLASSES) + 1:
    CLASSES = CLASSES + ["x"]
pcls = [CLASSES[i] for i in logit.argmax(1)]
_e = np.exp(logit - logit.max(1, keepdims=True)); PROB = _e / _e.sum(1, keepdims=True)
CI = {c: i for i, c in enumerate(CLASSES)}
times["letters"] = round(time.time() - T0, 2)

# group into words: same row, small gap; subscripts attach to the word on their left
big = [k for k in range(len(pieces)) if pieces[k][4] == 0 and pcls[k] not in ("sub0", "sub1", "x")]
big.sort(key=lambda k: pieces[k][0])
nxt = {}
taken = set()
for a in big:
    xa, ya, wa, ha, _ = pieces[a]
    best = None
    for b in big:
        xb, yb, wb, hb, _ = pieces[b]
        gap = xb - (xa + wa)
        if b == a or b in taken or gap < -4 or gap > 0.5 * max(ha, hb):
            continue
        ov = min(ya + ha, yb + hb) - max(ya, yb)
        if ov < 0.6 * min(ha, hb):
            continue
        if best is None or gap < best[0]:
            best = (gap, b)
    if best:
        nxt[a] = best[1]; taken.add(best[1])
words = []
for a in big:
    if a in taken:
        continue
    ch = [a]
    while ch[-1] in nxt:
        ch.append(nxt[ch[-1]])
    xs = [pieces[k][0] for k in ch]; ys = [pieces[k][1] for k in ch]
    x1 = max(pieces[k][0] + pieces[k][2] for k in ch); y1 = max(pieces[k][1] + pieces[k][3] for k in ch)
    words.append({"text": "".join(pcls[k] for k in ch), "box": [min(xs), min(ys), x1 - min(xs), y1 - min(ys)], "sub": None, "pieces": ch})
for k in range(len(pieces)):
    if pcls[k] != "x" and (pieces[k][4] == 1 or pcls[k] in ("sub0", "sub1")):
        x, y, w, h, _ = pieces[k]
        best = None
        for wd in words:
            bx, by, bw, bh = wd["box"]
            gap = x - (bx + bw)
            if -3 <= gap <= 18 and by + bh * 0.4 <= y + h / 2 <= by + bh + 10:
                if best is None or gap < best[0]:
                    best = (gap, wd)
        if best and best[1]["sub"] is None:
            c = pcls[k]
            best[1]["sub"] = "0" if c in ("sub0", "0", "O", "D", "Q") else "1" if c in ("sub1", "1", "I", "L", "T", "7") else c
            best[1]["pieces"].append(k)

# ---------------- dictionary: ONLY catalogues + generic ND name forms
dic = set()
for f in glob.glob(os.path.join(catdir, "chips", "*.json")):
    dic.add(json.load(open(f, encoding="utf-8"))["id"])
chip_ids = set(dic)
for f in glob.glob(os.path.join(catdir, "signals", "nd-bus-nord10.json")):
    for s in json.load(open(f, encoding="utf-8"))["signals"]:
        dic.add(s["name"]); dic.update(s.get("namesOnCards") or [])
for i in range(16):
    dic.update({f"BA{i}", f"BD{i}", f"LEV{i}", f"IDENT{i}"})
dic.update({"H", "H1", "GND", "STRAP", "INT"})
dic = {d.replace(" ", "").upper() for d in dic}
LOOK = str.maketrans({"O": "0", "I": "1", "S": "5", "Z": "2", "B": "8", "G": "6", "Q": "0", "D": "0"})
def lev(a, b):
    p = list(range(len(b) + 1))
    for i, ca in enumerate(a, 1):
        q = [i]
        for j, cb in enumerate(b, 1):
            q.append(min(p[j] + 1, q[j - 1] + 1, p[j - 1] + (0 if ca == cb or ca.translate(LOOK) == cb.translate(LOOK) else 1)))
        p = q
    return p[-1]
def snap(t, pool):
    if t in pool or len(t) < 2:
        return t
    best = sorted((lev(t, d), d) for d in pool if abs(len(d) - len(t)) <= 1)
    if best and best[0][0] <= (1 if len(t) < 6 else 2) and (len(best) == 1 or best[1][0] > best[0][0]):
        return best[0][1]
    return t
for wd in words:
    wd["snap"] = snap(wd["text"], dic)
REF = re.compile(r"^\d{1,2}[A-E]$")
# board grid: row number 1-19, column letter A-E. Look-alikes are mapped into that grid only.
DIG = {"O": "0", "Q": "0", "D": "0", "I": "1", "L": "1", "T": "7", "Z": "2", "S": "5", "H": "11", "G": "6"}
LET = {"8": "B", "P": "B", "R": "B", "0": "D", "O": "D", "Q": "D", "F": "E", "G": "C", "6": "C"}
def as_ref(t):
    if len(t) < 2:
        return None
    d = "".join(DIG.get(c, c) for c in t[:-1]); l = LET.get(t[-1], t[-1])
    r = d + l
    return r if REF.match(r) and 1 <= int(d) <= 19 else None
def as_num(t):
    t2 = t.translate(LOOK)
    return int(t2) if t2.isdigit() and len(t2) <= 2 else None
times["words"] = round(time.time() - T0, 2)

# ---------------- 2b. position codes read as one unit (FIX1)
# A code is a short run of letter pieces: N+letter (3C, 13E), N-N+letter-letter (7-8C-D), or two
# parts stacked (number above, letter below, inside small gate boxes). Every candidate is scored
# from the letter network's probabilities, but ONLY over the board grid: number 1-19, letter A-E
# (NORD-10 card, catalog boards/nord10-card.json). So a piece read as '4' can still be '1' if
# the network gave '1' some weight and the grid allows it.
FIX1 = os.environ.get("FIX1") == "1"
GRID_N, GRID_L = list(range(1, 20)), "ABCDE"
# FIX9: the board grid of THIS board type. BOARD=nd100: ND-100 cards (367 x 280 mm) - letters A-H and J
# (no I) and numbers 1-29, taken from the position codes in the five ND-100 work keys (A..H, J; 1..29)
# and matching the 9 columns about 29 mm apart and ~28 steps of 12.7 mm measured on the ND-100 card
# photos (catalog/boards/nd100-card.json). NORD-10 stays 1-19 x A-E.
if os.environ.get("BOARD") == "nd100":
    GRID_N, GRID_L = list(range(1, 30)), "ABCDEFGHJ"
def lp(k, c):
    return float(np.log(PROB[k, CI[c]] + 1e-6))
def best_code(seq_d, seq_l):
    """seq_d: pieces of the number part (1 or 2), seq_l: letter pieces (1 or 2). Returns (score, code)."""
    best = None
    if len(seq_l) == 1:
        for n_ in GRID_N:
            ds = str(n_)
            if len(ds) != len(seq_d): continue
            for L in GRID_L:
                sc = sum(lp(k, c) for k, c in zip(seq_d, ds)) + lp(seq_l[0], L)
                if best is None or sc > best[0]: best = (sc, f"{n_}{L}")
    return best
def best_span(seq):
    """N-N+L-L form: 4 pieces d d L L, digits consecutive, letters consecutive (7-8C-D)."""
    best = None
    if len(seq) == 4:
        for n_ in range(1, 9):
            for li in range(len(GRID_L) - 1):
                L1, L2 = GRID_L[li], GRID_L[li + 1]
                sc = lp(seq[0], str(n_)) + lp(seq[1], str(n_ + 1)) + lp(seq[2], L1) + lp(seq[3], L2)
                if best is None or sc > best[0]: best = (sc, f"{n_}-{n_ + 1}{L1}-{L2}")
    return best
BIGP = [k for k in range(len(pieces)) if pieces[k][4] == 0]
def code_in(region, mode="run"):
    """Best position code among the letter pieces whose centre is in region (x0,y0,x1,y1).
    mode 'run': codes written on one line (maximal runs only, so '13D' is never cut to '3D').
    mode 'stack': number above, letter below (two-part codes inside small gate boxes)."""
    x0, y0, x1, y1 = region
    ks = [k for k in BIGP if x0 <= pieces[k][0] + pieces[k][2] / 2 <= x1 and y0 <= pieces[k][1] + pieces[k][3] / 2 <= y1]
    ks.sort(key=lambda k: pieces[k][0])
    def same_line(pa, pb):
        return abs((pa[1] + pa[3] / 2) - (pb[1] + pb[3] / 2)) < 0.5 * max(pa[3], pb[3])
    def right_of(a, maxgap):
        pa = pieces[a]; best = None
        for b in ks:
            pb = pieces[b]; gap = pb[0] - (pa[0] + pa[2])
            if b != a and -4 <= gap <= maxgap * max(pa[3], pb[3]) and same_line(pa, pb) and (best is None or gap < best[0]):
                best = (gap, b)
        return best[1] if best else None
    cands = []
    if mode == "run":
        has_left = {right_of(a, 0.9) for a in ks}
        for a in ks:
            if a in has_left: continue
            run = [a]
            while len(run) < 6:
                nx = right_of(run[-1], 0.9)
                if nx is None: break
                run.append(nx)
            if len(run) in (2, 3):
                c = best_code(run[:-1], run[-1:])
                if c: cands.append((c[0] / len(run), c[1]))
            # the N-N+L-L form: dashes are too small to be pieces, so allow wider gaps
            run = [a]
            while len(run) < 4:
                nx = right_of(run[-1], 2.2)
                if nx is None: break
                run.append(nx)
            if len(run) == 4:
                c = best_span(run)
                if c: cands.append((c[0] / 4, c[1]))
            # NN-NN+letter (18-19E: a part across two board positions in one row)
            run5 = [a]
            while len(run5) < 5:
                nx = right_of(run5[-1], 2.2)
                if nx is None: break
                run5.append(nx)
            if len(run5) == 5:
                bb = None
                for n_ in range(10, GRID_N[-1]):
                    a_, b_ = str(n_), str(n_ + 1)
                    for L in GRID_L:
                        sc = lp(run5[0], a_[0]) + lp(run5[1], a_[1]) + lp(run5[2], b_[0]) + lp(run5[3], b_[1]) + lp(run5[4], L)
                        if bb is None or sc > bb[0]: bb = (sc, f"{n_}-{n_ + 1}{L}")
                cands.append((bb[0] / 5, bb[1]))
    else:
        for a in ks:
            pa = pieces[a]
            nx = right_of(a, 0.9)
            for top in ([a], [a, nx] if nx is not None else None):
                if not top: continue
                xl, xr = pa[0], pieces[top[-1]][0] + pieces[top[-1]][2]
                for b in ks:
                    pb = pieces[b]
                    if b in top: continue
                    if 0.6 * pa[3] < pb[1] - pa[1] < 2.2 * pa[3] and xl - 12 <= pb[0] + pb[2] / 2 <= xr + 12:
                        c = best_code(top, [b])
                        if c: cands.append((c[0] / (len(top) + 1), c[1]))
    if not cands:
        return None, -99
    cands.sort(key=lambda c: -c[0])
    return cands[0][1], cands[0][0]

# ---------------- 3. symbols
def cen(b): return (b[0] + b[2] / 2, b[1] + b[3] / 2)
def inbox(pt, b, m=0): return b[0] - m <= pt[0] <= b[0] + b[2] + m and b[1] - m <= pt[1] <= b[1] + b[3] + m
chips, gates = [], []
# optional gate-shape classifier (3 nearest neighbours on 14 measured features), trained on the
# checked boxes of other pages. Shape -> kind follows ND-13.003.01 as written in the nd-schematics
# skill: X = NOR, '>' / diagonal = inverter, plain box (also with the two-part position code inside)
# = NAND, vertical line mark = NAND open collector, X followed by a triangle = AND-OR-INVERT.
# 'ref+tri' (position-code box followed by a triangle) is NOT in that table: mapped to 'and'
# (the 7408 rule "plain box followed by the inverter's triangle") - an assumption, marked here.
GK = None
if os.environ.get("GATEKNN"):
    _g = json.load(open(os.environ["GATEKNN"]))
    GK = {"mu": np.array(_g["mu"]), "sd": np.array(_g["sd"]), "X": np.array(_g["X"]), "y": _g["y"]}
SHAPE2KIND = {"X": "nor", "tri": "not", "plain": "nand", "line-mark": "nand-oc", "x+tri": "and-or-invert", "ref+tri": "and"}
for b in boxes:
    x, y, w, h = b
    if w >= 60 and h >= 100 and w < 600:
        ins = [wd for wd in words if inbox(cen(wd["box"]), b, -2)]
        ref = next((as_ref(wd["text"]) for wd in ins if as_ref(wd["text"])), None)
        if FIX1:
            r2, sc2 = code_in((x + 0.15 * w, y + 4, x + 0.85 * w, y + h - 4))
            ref = r2 if r2 and sc2 > np.log(0.05) else None
        other = [wd for wd in ins if not as_ref(wd["text"]) and as_num(wd["text"]) is None]
        raw, typ = "", None
        def chip_snap(t):
            for cand in (t, t[2:] if t[:2] in ("DM", "DH", "SN", "0M") else None, "74" + t if 2 <= len(t) <= 3 and t.isdigit() else None):
                if cand and snap(cand, chip_ids) in chip_ids:
                    return snap(cand, chip_ids)
            return None
        for wd in sorted(other, key=lambda wd: -wd["box"][1]):     # the type is written low in the box
            if chip_snap(wd["text"]):
                raw, typ = wd["text"], chip_snap(wd["text"]); break
        if typ is None and other:
            low = max(o["box"][1] for o in other)
            row = sorted([o for o in other if o["box"][1] > low - 25], key=lambda o: o["box"][0])
            raw = "".join(o["text"] for o in row); typ = chip_snap(raw)
        chips.append({"box": b, "ref": ref, "type_read": raw, "type": typ, "kind": "chip"})
    elif 25 <= w <= 70 and 50 <= h <= 130:
        # gate kind from the ink inside the frame
        inner = ink[y + 4:y + h - 4, x + 4:x + w - 4] > 0
        ih, iw = inner.shape
        def cover(pts):
            hit = 0
            for px, py in pts:
                px, py = int(px), int(py)
                hit += inner[max(0, py - 2):py + 3, max(0, px - 2):px + 3].any()
            return hit / len(pts)
        t = np.linspace(0.15, 0.85, 30)
        top = min(ih, iw * 1.0)      # the diagonal / X sits in the upper square
        d1 = cover([(iw * s, top * s) for s in t]); d2 = cover([(iw * (1 - s), top * s) for s in t])
        dfull1 = cover([(iw * s, ih * s) for s in t]); dfull2 = cover([(iw * (1 - s), ih * s) for s in t])
        vcol = (inner[int(ih * .2):int(ih * .8)].mean(0) > 0.8) if ih > 10 else np.zeros(1, bool)
        nin = sum(1 for wd in words if inbox(cen(wd["box"]), b, -1))
        hh, ww2 = max(1, ih // 2), max(1, iw // 2)
        feats = [d1, d2, dfull1, dfull2, float(vcol.sum()) / max(1, iw), float(inner.mean()), w / h, w / 40, h / 60, min(nin, 3) / 3,
                 float(inner[:hh].mean()), float(inner[hh:].mean()), float(inner[:, :ww2].mean()), float(inner[:, ww2:].mean())]
        if GK is not None:
            z = (np.array(feats) - GK["mu"]) / GK["sd"]
            dd = ((GK["X"] - z) ** 2).sum(1); nn_ = np.argsort(dd)[:3]
            votes = {}
            for t_ in nn_: votes[GK["y"][t_]] = votes.get(GK["y"][t_], 0) + 1 / (1e-3 + dd[t_])
            shape = max(votes, key=votes.get)
            if shape == "none":
                continue
            kind = SHAPE2KIND[shape]
        elif w > 50:
            kind = "and-or-invert"
        elif min(dfull1, dfull2) > 0.6 or min(d1, d2) > 0.6:
            kind = "nor"
        elif max(d1, d2, dfull1, dfull2) > 0.6:
            kind = "not"
        elif vcol.sum() >= 2:
            kind = "nand-oc"
        else:
            kind = "nand"
        # position code: above the box, or written inside as two stacked parts (11 / E)
        cand = []
        near = [wd for wd in words if x - 30 <= cen(wd["box"])[0] <= x + w + 30 and y - 60 <= cen(wd["box"])[1] <= y + h]
        for wd in near:
            cx, cy = cen(wd["box"])
            if cy <= y + 5 and as_ref(wd["text"]):
                cand.append((abs(cy - y), as_ref(wd["text"])))
            for w2 in near:
                if w2 is wd: continue
                bx, by, bw, bh = wd["box"]; b2 = w2["box"]
                side = abs(b2[1] - by) < 12 and 0 <= b2[0] - (bx + bw) < 30 and cy <= y + 5
                stack = inbox((cx, cy), b, 0) and inbox(cen(b2), b, 0) and 10 < b2[1] - by < 45 and abs(b2[0] - bx) < 15
                if (side or stack) and as_ref(wd["text"] + w2["text"]):
                    cand.append((abs(cy - y) + 1, as_ref(wd["text"] + w2["text"])))
        ref = min(cand)[1] if cand else None
        if FIX1:
            ra = code_in((x - 20, y - 50, x + w + 20, y + 4))          # written above the symbol
            rb = code_in((x + 2, y + 2, x + w - 2, y + h - 2), "stack")  # two parts inside the symbol
            r2, sc2 = max(ra, rb, key=lambda t: t[1])
            ref = r2 if r2 and sc2 > np.log(0.05) else None
        gates.append({"box": b, "ref": ref, "kind": kind, "feats": [round(float(f), 4) for f in feats]})
# GATECNN: gate kind from the style-experiment gate network (gatestyle.py) on the pageio.gatecrop of
# the symbol, instead of the 14-feature nearest-neighbour table. GATESTYLE = page style (0 hand,
# 1 CAD) from the page-style detector, fed as the second input channel when the model has one.
#  - every gate box found above: the network's answer replaces the kind; 'none' drops the box.
#    The network's 'other' and 'nand' are coarser than the draft's kinds: a box the rules called
#    and-or-invert or nand-oc keeps that name when the network says other / nand.
#  - every gatecands() hole the network calls a gate and that no chip or gate box already covers
#    becomes a new gate (CAD gates are D / OR / triangle shapes, not rectangles, so the box finder
#    above never sees them).
if os.environ.get("GATECNN"):
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from pageio import gatecands, gatecrop
    _gs = ort.InferenceSession(os.environ["GATECNN"], providers=["CPUExecutionProvider"])
    _two = _gs.get_inputs()[0].shape[1] == 2
    _st = float(os.environ.get("GATESTYLE", "0"))
    GK6 = ["nand", "nor", "not", "and", "other", "none"]
    def gate_kind(bx):
        c = gatecrop(g, *bx).astype(np.float32)[None, None] / 255.0
        if _two:
            c = np.concatenate([c, np.full_like(c, _st)], 1)
        return GK6[int(_gs.run(None, {"x": c})[0].argmax())]
    kept = []
    for q in gates:
        k = gate_kind(q["box"])
        if k == "none":
            continue
        if not ((k == "other" and q["kind"] == "and-or-invert") or (k == "nand" and q["kind"] == "nand-oc")):
            q["kind"] = k
        kept.append(q)
    gates = kept
    def _ov(a, b):
        return min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0]) > 0 and min(a[1] + a[3], b[1] + b[3]) - max(a[1], b[1]) > 0
    for bx in gatecands(ink):
        if any(_ov(bx, q["box"]) for q in gates + chips):
            continue
        k = gate_kind(bx)
        if k == "none":
            continue
        ref = None
        if FIX1:
            x, y, w, h = bx
            r2, sc2 = code_in((x - 20, y - 50, x + w + 20, y + 4))
            ref = r2 if r2 and sc2 > np.log(0.05) else None
        gates.append({"box": list(bx), "ref": ref, "kind": k, "feats": []})
# DET: the trained object detector (detnet.py, run by detio.py) for both drawing styles.
#  DET=<onnx>  DETMODE=replace (default): chips and gates come from the detector only.
#              DETMODE=add: detector chips/gates that no rule box overlaps are added.
#  Position codes are read from the detector's position-code boxes (letter network over the
#  board grid, code_in), each code box given to the symbol it sits in / above / below.
#  DETGRID=<letters> widens the board-grid letters (CAD cards use more rows than A-E), DETGRIDN
#  the highest position number.
DETS, DETCODES = [], []
if os.environ.get("DET"):
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import detio
    if os.environ.get("DETGRID"):
        GRID_L = os.environ["DETGRID"]
    if os.environ.get("DETGRIDN"):
        GRID_N = list(range(1, int(os.environ["DETGRIDN"]) + 1))
    DETS = detio.detect(g, os.environ["DET"], float(os.environ.get("DETTHR", "0.3")))
    _cb = [d["box"] for d in DETS if d["cls"] == "poscode"]
    # FIX6: a code written on a box edge loses the touching strokes with the long lines, falls under
    # the letter size limit and never becomes a letter piece. Inside each code box with no full-size
    # piece, take the ink components again with a lower size limit (12 px) and classify them.
    if os.environ.get("FIX6") == "1":
        _add = []
        for b in _cb:
            x0, y0, x1, y1 = b[0] - 4, b[1] - 4, b[0] + b[2] + 4, b[1] + b[3] + 4
            if any(x0 <= pieces[k][0] + pieces[k][2] / 2 <= x1 and y0 <= pieces[k][1] + pieces[k][3] / 2 <= y1 for k in BIGP): continue
            xa, ya = max(0, int(x0)), max(0, int(y0))
            sub = txt[ya:int(y1), xa:int(x1)]
            n2, l2, s2, _ = cv2.connectedComponentsWithStats(sub, 8)
            for j in range(1, n2):
                x, y, w, h, a = s2[j]
                if 12 <= h <= 38 and 4 <= w <= 60 and a >= 25:
                    m_ = (l2[y:y + h, x:x + w] == j).astype(np.uint8)
                    for ox, p_ in (cut(m_) if w > 0.8 * h else [(0, m_)]):
                        ys_, xs_ = np.nonzero(p_)
                        _add.append([int(xa + x + ox + xs_.min()), int(ya + y + ys_.min()), int(xs_.max() - xs_.min() + 1), int(ys_.max() - ys_.min() + 1), 0])
        if _add:
            X2 = np.zeros((len(_add), 1, SZ, SZ), np.float32)
            for k, (x, y, w, h, s_) in enumerate(_add):
                cx, cy = int(x + w / 2) + WIN, int(y + h / 2) + WIN
                c = pad[cy - WIN // 2:cy + WIN // 2, cx - WIN // 2:cx + WIN // 2]
                X2[k, 0] = (255 - cv2.resize(c, (SZ, SZ), interpolation=cv2.INTER_AREA)).astype(np.float32) / 255
            if sess.get_inputs()[0].shape[1] == 2:
                X2 = np.concatenate([X2, np.full_like(X2, float(os.environ.get("GATESTYLE", "0")))], 1)
            l2_ = sess.run(None, {"x": X2})[0]; e2 = np.exp(l2_ - l2_.max(1, keepdims=True)); P2 = e2 / e2.sum(1, keepdims=True)
            k0 = len(pieces); pieces.extend(_add); PROB = np.concatenate([PROB, P2], 0)
            pcls.extend(CLASSES[i] for i in l2_.argmax(1)); BIGP.extend(range(k0, len(pieces)))
    # FIX10: codes whose characters touch ('11A', '10C' written in one stroke run) are one ink piece, or
    # none at all, so code_in finds no run. Read the code box with a sliding window instead: the letter
    # network is run on windows centred every 2 px along the box's middle line, and the code is the
    # best split into 2 or 3 characters (each 0.25-1.1 x the box height wide) scored over the board
    # grid only. The score is the mean log probability per character, the same scale as code_in.
    def _slide(b):
        x, y, w, h = [int(v) for v in b]
        if w < 12 or h < 10: return None, -99
        cy = y + h // 2 + WIN; xs_ = list(range(x, x + w + 1, 2))
        Xs = np.zeros((len(xs_), 1, SZ, SZ), np.float32)
        for k, cx in enumerate(xs_):
            c = pad[cy - WIN // 2:cy + WIN // 2, cx + WIN - WIN // 2:cx + WIN + WIN // 2]
            Xs[k, 0] = (255 - cv2.resize(c, (SZ, SZ), interpolation=cv2.INTER_AREA)).astype(np.float32) / 255
        if sess.get_inputs()[0].shape[1] == 2:
            Xs = np.concatenate([Xs, np.full_like(Xs, float(os.environ.get("GATESTYLE", "0")))], 1)
        lo_ = sess.run(None, {"x": Xs})[0]; e_ = np.exp(lo_ - lo_.max(1, keepdims=True)); P_ = np.log(e_ / e_.sum(1, keepdims=True) + 1e-6)
        ch = float(h); mn, mx = 0.25 * ch, 1.1 * ch
        def at(xc): return P_[min(len(xs_) - 1, max(0, int(round((xc - x) / 2))))]
        best = (-99, None)
        cuts = list(range(x, x + w + 1, 2))
        for n_ in (2, 3):
            def segs(start, left):
                if left == 1:
                    if mn <= x + w - start <= mx: yield [(start, x + w)]
                    return
                for c in cuts:
                    if mn <= c - start <= mx:
                        for rest in segs(c, left - 1): yield [(start, c)] + rest
            for sg in segs(x, n_):
                ps = [at((a + b_) / 2) for a, b_ in sg]
                lt = ps[-1]; Lb = max(GRID_L, key=lambda L: lt[CI[L]])
                ds = ps[:-1]
                for nn in GRID_N:
                    st_ = str(nn)
                    if len(st_) != len(ds): continue
                    sc = (sum(d[CI[c_]] for d, c_ in zip(ds, st_)) + lt[CI[Lb]]) / n_
                    if sc > best[0]: best = (sc, f"{nn}{Lb}")
        return best[1], best[0]
    def _read(b, pad=4):
        x, y, w, h = b
        reg = (x - pad, y - pad, x + w + pad, y + h + pad)
        r = max(code_in(reg), code_in(reg, "stack"), key=lambda t: t[1])
        if os.environ.get("FIX10") == "1":
            r2 = _slide(b)
            if r2[0] and (r[0] is None or r2[1] > r[1] + float(os.environ.get("FIX10M", "0"))): r = r2
        DETCODES.append({"box": [int(v) for v in b], "code": r[0], "sc": round(float(r[1]), 2),
                         "pieces": [k for k in BIGP if reg[0] <= pieces[k][0] + pieces[k][2] / 2 <= reg[2] and reg[1] <= pieces[k][1] + pieces[k][3] / 2 <= reg[3]]})
        return r
    def det_code(sb, below=60, above=60):
        """Code for symbol box sb from the code boxes inside it, just above it or just below it.
        Each code box alone, and the union of the code boxes inside (number above letter)."""
        x, y, w, h = sb
        ins = [b for b in _cb if inbox(cen(b), sb, 2)]
        out = [b for b in _cb if x - 25 <= cen(b)[0] <= x + w + 25 and (y - above <= cen(b)[1] < y or y + h < cen(b)[1] <= y + h + below)]
        cands = [_read(b) for b in ins + out]
        if len(ins) >= 2:
            ux0 = min(b[0] for b in ins); uy0 = min(b[1] for b in ins)
            ux1 = max(b[0] + b[2] for b in ins); uy1 = max(b[1] + b[3] for b in ins)
            cands.append(_read((ux0, uy0, ux1 - ux0, uy1 - uy0)))
        cands = [c for c in cands if c[0]]
        if not cands:
            return None
        r, sc = max(cands, key=lambda t: t[1])
        return r if sc > np.log(0.05) else None
    def det_type(sb):
        """Chip type: words inside the box (written low) or the word just above (CAD)."""
        x, y, w, h = sb
        ws_ = [wd for wd in words if inbox(cen(wd["box"]), sb, -2) or (x - 10 <= wd["box"][0] <= x + w and y - 45 <= cen(wd["box"])[1] < y)]
        # words of 3+ characters first: a pin number is never more than 2 digits (BUGS.md B6)
        for wd in sorted(ws_, key=lambda wd: (len(wd["text"]) < 3, -wd["box"][1])):
            t = wd["text"]
            for cand in (t, t[2:] if t[:2] in ("DM", "DH", "SN", "0M") else None, "74" + t if 2 <= len(t) <= 3 and t.isdigit() else None):
                if cand and snap(cand, chip_ids) in chip_ids:
                    return wd["text"], snap(cand, chip_ids)
        return "", None
    DK = {"aoi": "and-or-invert"}
    dchips, dgates = [], []
    for d in DETS:
        b = d["box"]
        if d["cls"] == "chip":
            raw, typ = det_type(b)
            dchips.append({"box": b, "ref": det_code(b), "type_read": raw, "type": typ, "kind": "chip", "det": d["score"]})
        elif d["cls"] == "gate":
            dgates.append({"box": b, "ref": det_code(b), "kind": DK.get(d["kind"], d["kind"]), "feats": [], "det": d["score"]})
    # one box per symbol: biggest first, a box whose centre lies inside a kept box of the same class
    # is a second answer for the same symbol (the detector often gives the whole chip and its code area)
    def _one(L):
        kept = []
        for q in sorted(L, key=lambda q: -q["box"][2] * q["box"][3]):
            if not any(inbox(cen(q["box"]), k["box"]) for k in kept):
                kept.append(q)
        return kept
    dchips, dgates = _one(dchips), _one(dgates)
    if os.environ.get("DETMODE", "replace") == "replace":
        chips, gates = dchips, dgates
    else:
        def _ovl(a, b):
            return min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0]) > 0 and min(a[1] + a[3], b[1] + b[3]) - max(a[1], b[1]) > 0
        chips += [c for c in dchips if not any(_ovl(c["box"], o["box"]) for o in chips + gates)]
        gates += [q for q in dgates if not any(_ovl(q["box"], o["box"]) for o in chips + gates)]
    # FIX7: (a) rule boxes kept in DETMODE=add never asked the code boxes for their code: ask them.
    # (b) a "gate" glued to the side of a chip box, or inside it, is that chip's pin-number column
    # (ND draws pin numbers in small cells): it is not a symbol, and it steals the chip's wire ends.
    if os.environ.get("FIX7") == "1":
        for q in chips + gates:
            if not q.get("ref"):
                q["ref"] = det_code(q["box"])
        _bigc = [c["box"] for c in chips if c["box"][2] >= 50 and c["box"][3] >= 90]
        def _cell(b):
            for o in _bigc:
                vo = min(b[1] + b[3], o[1] + o[3]) - max(b[1], o[1])
                if vo < 0.5 * b[3]: continue
                if abs(b[0] + b[2] - o[0]) < 10 or abs(b[0] - (o[0] + o[2])) < 10: return True
                if o[0] - 3 <= b[0] and b[0] + b[2] <= o[0] + o[2] + 3: return True
            return False
        gates = [q for q in gates if q.get("ref") or not _cell(q["box"])]
# FIX11: ND writes a package's code once over a column of its gates. A gate with no code that sits in a
# column with a coded gate of the same kind (left edges within 12 px, widths within 30 %, the gap to the
# next gate in the column under 2.5 gate heights) gets that code, while the package has a free gate
# slot (4 per package, 6 for inverters and buffers). Marked ref_inferred (lower confidence).
if os.environ.get("FIX11") == "1":
    _slots = lambda k: 6 if k in ("not", "buffer") else 4
    _cnt = {}
    for q in gates:
        if q.get("ref"): _cnt[q["ref"]] = _cnt.get(q["ref"], 0) + 1
    _chg = True
    while _chg:
        _chg = False
        for q in gates:
            if q.get("ref"): continue
            x, y, w, h = q["box"]; best = None
            for o in gates:
                r_ = o.get("ref")
                if not r_ or o["kind"] != q["kind"] or _cnt.get(r_, 0) >= _slots(q["kind"]): continue
                ox, oy, ow, oh = o["box"]
                if abs(ox - x) > 12 or abs(ow - w) > 0.3 * max(ow, w): continue
                gap = max(oy - (y + h), y - (oy + oh))
                if gap < 2.5 * max(h, oh) and (best is None or gap < best[0]): best = (gap, r_)
            if best:
                q["ref"] = best[1]; q["ref_inferred"] = True; _cnt[best[1]] = _cnt.get(best[1], 0) + 1; _chg = True
times["symbols"] = round(time.time() - T0, 2)

# ---------------- 4. wires
# Short wires (stubs under 60 px between a label and a gate) are lost by the 60 px opening.
# Second line mask: ink minus every letter piece and every circle ring, opened with a short bar.
SHORT = int(os.environ.get("SHORT", "22"))
if SHORT < 60:
    wm = ink.copy()
    FIX2 = os.environ.get("FIX2") == "1"
    if not FIX2:
        for x, y, w, h, s_ in pieces:
            wm[y - 1:y + h + 1, x - 1:x + w + 1] = 0
    for cx, cy, r in good:
        cv2.circle(wm, (int(cx), int(cy)), int(r) + 3, 0, 9)
    def segs2(m):
        n_, l_, st_, _ = cv2.connectedComponentsWithStats(m, 8)
        return [[int(a) for a in st_[i][:4]] for i in range(1, n_)]
    hs = segs2(cv2.morphologyEx(wm, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (SHORT, 1))))
    vs = segs2(cv2.morphologyEx(wm, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, SHORT))))
    hs = [q for q in hs if q[3] <= 12]; vs = [q for q in vs if q[2] <= 12]   # thick blobs are not wires
    if FIX2:
        # FIX2a: a pin number touching its wire made a piece whose box covered the wire; blanking
        # the whole box cut the short stub between a chip edge and its label. Now letter strokes are
        # dropped only when the stroke lies wholly inside one letter piece's box.
        def _inpiece(q):
            return any(q[0] >= x - 2 and q[1] >= y - 2 and q[0] + q[2] <= x + w + 2 and q[1] + q[3] <= y + h + 2
                       for x, y, w, h, s_ in pieces)
        hs = [q for q in hs if not _inpiece(q)]; vs = [q for q in vs if not _inpiece(q)]
segs = []
allbox = [c["box"] for c in chips] + [q["box"] for q in gates] + [b for b in boxes]
for x, y, w, h in hs:
    if w > 0.6 * W:
        continue
    yy = y + h / 2
    if any(abs(yy - by) < 6 or abs(yy - (by + bh)) < 6 for bx, by, bw, bh in allbox
           if min(x + w, bx + bw) - max(x, bx) > 0.7 * w):
        continue
    segs.append(("h", yy, x, x + w))
for x, y, w, h in vs:
    if h > 0.6 * H:
        continue
    xx = x + w / 2
    if any(abs(xx - bx) < 6 or abs(xx - (bx + bw)) < 6 for bx, by, bw, bh in allbox
           if min(y + h, by + bh) - max(y, by) > 0.7 * h):
        continue
    segs.append(("v", xx, y, y + h))
par = list(range(len(segs)))
def find(a):
    while par[a] != a:
        par[a] = par[par[a]]; a = par[a]
    return a
def join(a, b): par[find(a)] = find(b)
TOL, ENDT = 6, 12
joins_dot, crossings_left = 0, 0
for i, s in enumerate(segs):
    for j in range(i + 1, len(segs)):
        t = segs[j]
        if s[0] == t[0]:
            if abs(s[1] - t[1]) < 5 and max(s[2], t[2]) - min(s[3], t[3]) < 15:
                join(i, j)
            continue
        hsg, vsg = (s, t) if s[0] == "h" else (t, s)
        px, py = vsg[1], hsg[1]
        if not (hsg[2] - TOL <= px <= hsg[3] + TOL and vsg[2] - TOL <= py <= vsg[3] + TOL):
            continue
        at_end = min(abs(px - hsg[2]), abs(px - hsg[3])) < ENDT or min(abs(py - vsg[2]), abs(py - vsg[3])) < ENDT
        dot = any(abs(px - dx) < 14 and abs(py - dy) < 14 for dx, dy in kdots)
        if at_end or dot:
            join(i, j); joins_dot += dot and not at_end
        else:
            crossings_left += 1
diam_lab = []
for d in diamonds:
    dx, dy, dw, dh = d; cy = dy + dh / 2
    best = None
    for wd in words:
        bx, by, bw, bh = wd["box"]
        if abs(by + bh / 2 - cy) < 22:
            gap = min(abs(bx - (dx + dw)), abs(dx - (bx + bw)))
            if gap < 40 and (best is None or gap < best[0]):
                best = (gap, wd)
    diam_lab.append(best[1] if best else None)
ends = []   # (seg index, end point, attached to)
pin_words = [wd for wd in words if as_num(wd["text"]) is not None]
def pin_near(px, py, b):
    best = None
    for wd in pin_words:
        cx, cy = cen(wd["box"])
        d = abs(cx - px) + 1.5 * max(0, cy - py) + 0.5 * max(0, py - cy)  # a pin number sits above its wire
        if abs(cx - px) < 45 and -45 < cy - py < 12 and (best is None or d < best[0]):
            best = (d, as_num(wd["text"]))
    return best[1] if best else None
boxed = [(c["ref"], c["box"], "chip") for c in chips] + [(q["ref"], q["box"], "gate") for q in gates]
# ---------------- FIX3: passive parts and supply symbols, plain rules (no training, nothing from
# the hand reading). Resistor = small closed rectangle 2.5x or more longer than wide. Capacitor =
# two short parallel bars of about equal length, 5-16 px apart. Ground = a short bar with shorter
# bars stacked under it. Strap = the word (STRAP) at a wire end. Each passive gets two terminals.
FIX3 = os.environ.get("FIX3") == "1"
passives, grounds = [], []
if FIX3:
    _cs, _hh = cv2.findContours(ink, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_SIMPLE)
    for _i, _c in enumerate(_cs):
        if _hh[0][_i][3] < 0: continue
        x, y, w, h = cv2.boundingRect(_c)
        if cv2.contourArea(_c) / max(1, w * h) < 0.8: continue
        lo, hi_ = min(w, h), max(w, h)
        if 8 <= lo <= 22 and 32 <= hi_ <= 90 and hi_ >= 2.5 * lo:
            if any(inbox((x + w / 2, y + h / 2), b, 0) for b in [c["box"] for c in chips]): continue
            if w > h: side_ = np.concatenate([ink[max(0, y - 8):y - 3, x + 3:x + w - 3], ink[y + h + 3:y + h + 8, x + 3:x + w - 3]], 0)
            else: side_ = np.concatenate([ink[y + 3:y + h - 3, max(0, x - 8):x - 3], ink[y + 3:y + h - 3, x + w + 3:x + w + 8]], 1)
            if side_.size == 0 or side_.mean() / 255 > 0.06: continue
            t = [(x - 3, y + h / 2), (x + w + 3, y + h / 2)] if w > h else [(x + w / 2, y - 3), (x + w / 2, y + h + 3)]
            passives.append({"kind": "R", "box": [x, y, w, h], "t": t})
    def bars(horiz):
        k = cv2.getStructuringElement(cv2.MORPH_RECT, (12, 1) if horiz else (1, 12))
        m = cv2.morphologyEx(ink, cv2.MORPH_OPEN, k)
        n_, l_, st_, _ = cv2.connectedComponentsWithStats(m, 8)
        out = []
        for j in range(1, n_):
            x, y, w, h, a = st_[j]
            L_, T_ = (w, h) if horiz else (h, w)
            if 12 <= L_ <= 48 and T_ <= 9: out.append([int(x), int(y), int(w), int(h)])
        return out
    def thin_wire(cx_, y0_, y1_, horiz=True):
        """a single thin wire runs along the centre line: ink at the centre, none 5-8 px either side"""
        if horiz:
            if y1_ <= y0_: return False
            c_ = ink[y0_:y1_, cx_ - 2:cx_ + 3].max(1) > 0
            sd = np.concatenate([ink[y0_:y1_, cx_ - 9:cx_ - 5], ink[y0_:y1_, cx_ + 6:cx_ + 10]], 1).max(1) > 0
        else:
            if y1_ <= y0_: return False
            c_ = ink[cx_ - 2:cx_ + 3, y0_:y1_].max(0) > 0
            sd = np.concatenate([ink[cx_ - 9:cx_ - 5, y0_:y1_], ink[cx_ + 6:cx_ + 10, y0_:y1_]], 0).max(0) > 0
        return c_.mean() >= 0.9 and sd.mean() <= 0.2
    for horiz in (True, False):
        bs = bars(horiz); usedb = set()
        A = 0 if horiz else 1                     # along-bar axis index
        for a_ in range(len(bs)):
            ba = bs[a_]
            cA = ba[A] + ba[A + 2] / 2
            under = [b for b in bs if b is not ba and 2 <= (b[1 - A] - (ba[1 - A] + ba[3 - A])) <= 12 and abs((b[A] + b[A + 2] / 2) - cA) < 6]
            if horiz and 18 <= ba[2] <= 44 and any(0.35 <= b[2] / ba[2] <= 0.85 for b in under) and                     thin_wire(int(cA), max(0, ba[1] - 14), ba[1] - 1):
                grounds.append((cA, ba[1])); continue                       # ground symbol (top bar)
            for b in under:
                if not (0.75 < b[A + 2] / ba[A + 2] < 1.33) or id(b) in usedb: continue
                if horiz:
                    y0_, y1_ = ba[1] + ba[3], b[1]
                    gapink = ink[y0_ + 1:y1_ - 1, ba[0] + 3:ba[0] + ba[2] - 3].mean() / 255 if y1_ - y0_ > 2 else 0
                    ok = thin_wire(int(cA), max(0, ba[1] - 14), ba[1] - 1) and thin_wire(int(cA), b[1] + b[3] + 1, b[1] + b[3] + 14)
                else:
                    x0_, x1_ = ba[0] + ba[2], b[0]
                    gapink = ink[ba[1] + 3:ba[1] + ba[3] - 3, x0_ + 1:x1_ - 1].mean() / 255 if x1_ - x0_ > 2 else 0
                    ok = thin_wire(int(cA), max(0, ba[0] - 14), ba[0] - 1, False) and thin_wire(int(cA), b[0] + b[2] + 1, b[0] + b[2] + 14, False)
                if gapink > 0.08 or not ok:
                    continue
                usedb.add(id(b))
                if horiz:
                    t = [(cA, ba[1] - 3), (cA, b[1] + b[3] + 3)]
                else:
                    t = [(ba[0] - 3, cA), (b[0] + b[2] + 3, cA)]
                passives.append({"kind": "C", "box": [min(ba[0], b[0]), min(ba[1], b[1]), 0, 0], "t": t})
STRAPW = [wd for wd in words if re.search(r"[S5]TRAP|STRA|TRAP", wd["text"])]
for i, s in enumerate(segs):
    pts = [(s[2], s[1]), (s[3], s[1])] if s[0] == "h" else [(s[1], s[2]), (s[1], s[3])]
    for e, (px, py) in enumerate(pts):
        att = None
        if FIX3:
            for k_, pv in enumerate(passives):
                for tn, (tx, ty) in enumerate(pv["t"]):
                    if abs(px - tx) <= 10 and abs(py - ty) <= 10:
                        att = ("pin", f"#{k_}.{tn}", (px, py), "P", "passive"); break
                if att: break
            if att is None:
                for gx, gy in grounds:
                    if abs(px - gx) <= 10 and abs(py - gy) <= 10:
                        att = ("label", ("GND", None), (px, py)); break
            if att is None:
                for wd in STRAPW:
                    bx, by, bw, bh = wd["box"]
                    if abs(by + bh / 2 - py) < 20 and -5 < bx - px < 40:
                        att = ("strap", None, (px, py)); break
        if att is not None:
            pass
        else:
          for cx, cy, r in good:
              if (px - cx) ** 2 + (py - cy) ** 2 < (r + 22) ** 2:
                  num = next((as_num(wd["text"]) for wd in words if (cen(wd["box"])[0] - cx) ** 2 + (cen(wd["box"])[1] - cy) ** 2 < r * r and as_num(wd["text"]) is not None), None)
                  att = ("conn", f"P.{num}" if num is not None else "P.?", (cx, cy)); break
        if att is None:
            for ref, b, k in boxed:
                if inbox((px, py), b, 25) and not inbox((px, py), b, -8):
                    side = "L" if px < b[0] + 8 else "R" if px > b[0] + b[2] - 8 else "TB"
                    pn = pin_near(px, py, b)
                    att = ("pin", f"{ref or '?'}.{pn if pn is not None else '?'}", (px, py), side, k); break
        if att is None:
            for d, wd in zip(diamonds, diam_lab):
                if inbox((px, py), d, 25):
                    att = ("label", (wd["snap"], wd["sub"]) if wd else None, (px, py)); break
        if att is None and s[0] == "h":
            for wd in words:
                bx, by, bw, bh = wd["box"]
                if abs(by + bh / 2 - py) < 28 and as_num(wd["text"]) is None and \
                        ((e == 0 and -5 < px - (bx + bw) < 40) or (e == 1 and -5 < bx - px < 40)):
                    att = ("label", (wd["snap"], wd["sub"]), (px, py)); break
        if att:
            ends.append((i, att))
# FIX2b: read each pin number again with the chip's pinout in hand. Candidates are every 1-2 digit
# reading of the letter pieces just above / beside the wire end (from the network's probabilities,
# not only its top answer). A number is allowed only if the part's type is known and that pin
# exists and fits the side: an output pin cannot sit on the input (left) side of a gate or chip,
# an input cannot be the output (right) side, power/ground pins are never wire ends.
if os.environ.get("FIX2") == "1":
    PINS = {}
    for f in glob.glob(os.path.join(catdir, "chips", "*.json")):
        d_ = json.load(open(f, encoding="utf-8"))
        PINS[d_["id"]] = {int(q["pin"]): q.get("role") for q in d_.get("pins", []) if str(q.get("pin", "")).isdigit()}
    KT = {"nor": "7402", "not": "7404", "nand": "7400", "nand-oc": "7401", "and-or-invert": "7451", "and": "7408"}
    btype = {}
    for c in chips: btype[tuple(c["box"])] = c["type"]
    # FIX8: chip type checked against the pin numbers. Every catalog type is scored by how well the
    # pin numbers read at the chip's wire ends fit its pinout (pin exists, not supply, output on the
    # output side, input on the input side) plus how close the printed type text is. The best type
    # replaces the read one only when it fits clearly better.
    if os.environ.get("FIX8") == "1":
        def _fit(roles, rd):
            sc = 0.0
            for n_, side in rd:
                r_ = roles.get(n_)
                if r_ is None or r_ in ("power", "ground", "nc"): sc -= 1.0
                elif (side == "L" and r_ == "output") or (side == "R" and r_ in ("input", "enable", "clock")): sc -= 0.5
                else: sc += 1.0
            return sc
        for c in chips:
            b = c["box"]
            rd = [(int(att[1].rsplit(".", 1)[1]), att[3]) for i, att in ends if att[0] == "pin" and att[4] != "passive"
                  and inbox(att[2], b, 25) and not inbox(att[2], b, -8) and att[1].rsplit(".", 1)[1].isdigit()]
            if len(rd) < 4: continue
            tr = (c.get("type_read") or "").upper()
            def tsc(t_):
                f = _fit(PINS[t_], rd)
                if tr: f += 3.0 * max(0.0, 1 - lev(tr, t_) / max(len(t_), 1))
                if c.get("type") == t_: f += 1.0
                return f
            cur = tsc(c["type"]) if c.get("type") in PINS else -99
            if not tr: continue                       # no printed type: pin numbers alone pick too many wrong types
            best = max(((tsc(t_) - 0.15 * max(0, len(PINS[t_]) - max(n_ for n_, sd in rd)), t_) for t_ in PINS
                        if PINS[t_] and lev(tr, t_) <= 2), default=None)
            if best and best[0] > cur + 2.0:
                c["type"] = best[1]; c["type_from_pins"] = True
        btype = {tuple(c["box"]): c["type"] for c in chips}
    if os.environ.get("PINGATE") == "1":
        for q in gates: btype[tuple(q["box"])] = KT.get(q["kind"])     # "other" (GATECNN): no chip type, so no pinout assumed
    if os.environ.get("PINCHIP") == "0":
        btype = {}
    digit_pieces = [k for k in range(len(pieces)) if pieces[k][4] == 0]
    def num_cands(px, py, side):
        """(score, number) for every 1-2 digit reading near the wire end."""
        near = []
        for k in digit_pieces:
            x, y, w, h, _ = pieces[k]
            cx, cy = x + w / 2, y + h / 2
            if abs(cx - px) < 45 and -45 < cy - py < 12:
                near.append(k)
        near.sort(key=lambda k: pieces[k][0])
        out = []
        for a in near:
            d = abs(pieces[a][0] + pieces[a][2] / 2 - px) + 1.5 * max(0, pieces[a][1] + pieces[a][3] / 2 - py)
            pr = PROB[a, :10]
            for n1 in range(1, 10):
                out.append((float(np.log(pr[n1] + 1e-6)) - d / 30, n1, [a]))
            for b in near:
                if b == a: continue
                gap = pieces[b][0] - (pieces[a][0] + pieces[a][2])
                if -4 <= gap <= 12 and abs(pieces[b][1] - pieces[a][1]) < 10:
                    pb = PROB[b, :10]
                    for n2 in range(10, 25):
                        t = str(n2)
                        out.append(((float(np.log(pr[int(t[0])] + 1e-6)) + float(np.log(pb[int(t[1])] + 1e-6))) / 2 - d / 30 + 0.2, n2, [a, b]))
        return out
    newends = []
    for i, att in ends:
        if att[0] == "pin" and att[4] != "passive":
            ref = att[1].rsplit(".", 1)[0]; px, py = att[2]; side = att[3]
            box = next((b for r_, b, k_ in boxed if inbox((px, py), b, 25) and not inbox((px, py), b, -8)), None)
            typ = btype.get(tuple(box)) if box else None
            roles = PINS.get(typ) if typ else None
            def fits(n_):
                if roles is None: return n_ <= 40
                r_ = roles.get(n_)
                if r_ is None or r_ in ("power", "ground", "nc"): return False
                if side == "L" and r_ == "output": return False
                if side == "R" and r_ in ("input", "enable", "clock"): return False
                return True
            first = att[1].rsplit(".", 1)[1]
            if first != "?" and fits(int(first)):
                newends.append((i, att)); continue          # the plain reading already fits the pinout
            cands = sorted(num_cands(px, py, side), key=lambda c: -c[0])
            pick = None
            for sc, n_, ks_ in cands:
                if sc < np.log(0.02) - 1.5: break
                if roles is not None:
                    r_ = roles.get(n_)
                    if r_ is None or r_ in ("power", "ground", "nc"): continue
                    if side == "L" and r_ == "output": continue
                    if side == "R" and r_ in ("input", "enable", "clock"): continue
                elif n_ > 40:
                    continue
                pick = n_; break
            att = (att[0], f"{ref}.{pick if pick is not None else '?'}") + tuple(att[2:])
        newends.append((i, att))
    ends = newends
    # FIX4: one symbol, one wire end per pin number. The numbers of all wire ends on one symbol are
    # chosen together (assignment problem): each number is used once per symbol, it must fit the
    # pinout and side (as FIX2b), and the plain reading gets a bonus. Ends at the same point (two
    # wire pieces meeting at one pin) count as one slot.
    if os.environ.get("FIX4") == "1":
        from scipy.optimize import linear_sum_assignment
        bysym = {}
        for k, (i, att) in enumerate(ends):
            if att[0] == "pin" and att[4] != "passive":
                px, py = att[2]
                box = next((b for r_, b, k_ in boxed if inbox((px, py), b, 25) and not inbox((px, py), b, -8)), None)
                if box is not None: bysym.setdefault(tuple(box), []).append(k)
        for box, ks in bysym.items():
            typ = btype.get(box); roles = PINS.get(typ) if typ else None
            slots = []
            for k in ks:
                px, py = ends[k][1][2]
                s_ = next((sl for sl in slots if abs(ends[sl[0]][1][2][0] - px) < 12 and abs(ends[sl[0]][1][2][1] - py) < 12), None)
                if s_ is None: slots.append([k])
                else: s_.append(k)
            cand = []
            for sl in slots:
                att = ends[sl[0]][1]; px, py = att[2]; side = att[3]; first = att[1].rsplit(".", 1)[1]
                best = {}
                for sc, n_, ks_ in num_cands(px, py, side):
                    if roles is not None:
                        r_ = roles.get(n_)
                        if r_ is None or r_ in ("power", "ground", "nc"): continue
                        if side == "L" and r_ == "output": continue
                        if side == "R" and r_ in ("input", "enable", "clock"): continue
                    elif n_ > 40: continue
                    if sc < np.log(0.02) - 1.5: continue
                    best[n_] = max(best.get(n_, -99), sc)
                if first != "?" and first.isdigit(): best[int(first)] = best.get(int(first), np.log(0.5)) + 0.5
                cand.append(best)
            nums = sorted({n_ for b in cand for n_ in b})
            if not nums or len(slots) < 2: continue
            BIG = 50.0; QM = float(os.environ.get("FIX4Q", "5.9"))     # cost of leaving a slot as '?'
            C = np.full((len(slots), len(nums) + len(slots)), BIG)
            for a, b in enumerate(cand):
                for j, n_ in enumerate(nums):
                    if n_ in b: C[a, j] = -b[n_]
                C[a, len(nums) + a] = QM
            ri, ci = linear_sum_assignment(C)
            for a, j in zip(ri, ci):
                n_ = nums[j] if j < len(nums) else None
                for k in slots[a]:
                    i, att = ends[k]; ref = att[1].rsplit(".", 1)[0]
                    ends[k] = (i, (att[0], f"{ref}.{n_ if n_ is not None else '?'}") + tuple(att[2:]))
    # FIX5: solve each package's gates together. A gate symbol's wire ends must be the pins of ONE
    # gate of the package (the inputs on the input side, the output on the output side), every gate
    # of a package is used once, and all gates with the same code are one package type. Pin readings
    # only give the score; the pinout decides which numbers are possible.
    if os.environ.get("FIX5") == "1":
        from scipy.optimize import linear_sum_assignment
        FT = {"nor": ["7402", "7427", "7425"], "nand": ["7400", "7410", "7420", "7430"], "not": ["7404"],
              "and": ["7408", "7411", "7421"], "or": ["7432"], "and-or-invert": ["7451", "7453", "7454"],
              "xor": ["7486"], "nand-oc": ["7401", "7403"], "buffer": ["7407", "7417"]}
        FUN = {}
        for t_ in {t for v in FT.values() for t in v}:
            fp = os.path.join(catdir, "chips", t_ + ".json")
            if not os.path.exists(fp): continue
            d_ = json.load(open(fp, encoding="utf-8")); n2p = {q["name"]: int(q["pin"]) for q in d_["pins"] if str(q["pin"]).isdigit()}
            fl = []
            for f in d_.get("functions") or []:
                o_ = str(f.get("output"))
                if "," in o_ or not o_.isdigit() or "=" not in f["formula"]: continue
                ins = sorted({n2p[w] for w in re.findall(r"[A-Za-z0-9]+", f["formula"].split("=", 1)[1]) if w in n2p})
                fl.append((int(o_), ins))
            FUN[t_] = fl
        QM5 = float(os.environ.get("FIX5Q", "2.0"))
        def slot_scores(k_list):
            att = ends[k_list[0]][1]; px, py = att[2]; first = att[1].rsplit(".", 1)[1]
            b = {}
            for sc, n_, ks_ in num_cands(px, py, att[3]):
                b[n_] = max(b.get(n_, -99), sc)
            if first.isdigit(): b[int(first)] = b.get(int(first), np.log(0.5)) + 0.5
            return b
        gsl = []     # per gate symbol: (ref, kind, out slots, in slots)
        for q in gates:
            if not q.get("ref") or q["kind"] not in FT: continue
            b = q["box"]; ks = [k for k, (i, att) in enumerate(ends) if att[0] == "pin" and att[4] != "passive" and inbox(att[2], b, 25) and not inbox(att[2], b, -8)]
            if not ks: continue
            slots = []
            for k in ks:
                px, py = ends[k][1][2]
                s_ = next((sl for sl in slots if abs(ends[sl[0]][1][2][0] - px) < 12 and abs(ends[sl[0]][1][2][1] - py) < 12), None)
                if s_ is None: slots.append([k])
                else: s_.append(k)
            outs = [sl for sl in slots if ends[sl[0]][1][3] == "R"]; ins_ = [sl for sl in slots if ends[sl[0]][1][3] != "R"]
            gsl.append((q["ref"], q["kind"], outs, ins_, [slot_scores(sl) for sl in outs], [slot_scores(sl) for sl in ins_]))
        def fcost(g, f):
            o_, ins = f; c = 0.0
            for b in g[4]: c += -b[o_] if o_ in b else QM5
            if g[5]:
                M = np.full((len(g[5]), len(ins) + len(g[5])), 50.0)
                for a, b in enumerate(g[5]):
                    for j, n_ in enumerate(ins):
                        if n_ in b: M[a, j] = min(-b[n_], QM5 + 0.01) if True else 0
                        else: M[a, j] = QM5 + 0.01      # pin forced by the pinout, reading did not see it
                    M[a, len(ins) + a] = QM5 + 0.5
                ri, ci = linear_sum_assignment(M); c += M[ri, ci].sum()
                return c, {a: (ins[j] if j < len(ins) else None) for a, j in zip(ri, ci)}
            return c, {}
        byref = {}
        for gi, g in enumerate(gsl): byref.setdefault((g[0], g[1]), []).append(gi)
        for (ref, kind), gis in byref.items():
            best = None
            for t_ in FT[kind]:
                fl = FUN.get(t_) or []
                if not fl or len(gis) > len(fl): continue
                M = np.zeros((len(gis), len(fl))); A = {}
                for a, gi in enumerate(gis):
                    for j, f in enumerate(fl):
                        M[a, j], A[(a, j)] = fcost(gsl[gi], f)
                ri, ci = linear_sum_assignment(M); tot = M[ri, ci].sum()
                if best is None or tot < best[0]: best = (tot, t_, [(gis[a], fl[j], A[(a, j)]) for a, j in zip(ri, ci)])
            if best is None: continue
            for gi, f, amap in best[2]:
                g = gsl[gi]
                for sl in g[2]:
                    for k in sl:
                        i, att = ends[k]; ends[k] = (i, (att[0], f"{ref}.{f[0]}") + tuple(att[2:]))
                for a, sl in enumerate(g[3]):
                    n_ = amap.get(a)
                    for k in sl:
                        i, att = ends[k]; ends[k] = (i, (att[0], f"{ref}.{n_ if n_ is not None else '?'}") + tuple(att[2:]))
# FIX12: fill unread pin numbers on a chip edge from spacing + pinout. On one edge of a typed chip
# the wire ends are sorted top to bottom; a run of unread ends between two read ends a (above) and
# b (below) gets the unused pins of the pinout that fit that side and lie numerically between a and
# b - only when the count of such pins equals the count of unread ends (then they are given in order).
if os.environ.get("FIX12") == "1" and os.environ.get("FIX2") == "1":
    for r_, b, k_ in boxed:
        typ = btype.get(tuple(b)); roles = PINS.get(typ) if typ else None
        if not roles or k_ != "chip": continue
        for side in ("L", "R"):
            ok = lambda n_: roles.get(n_) not in (None, "power", "ground", "nc") and not (side == "L" and roles.get(n_) == "output") and not (side == "R" and roles.get(n_) in ("input", "enable", "clock"))
            sl = {}
            for k, (i, att) in enumerate(ends):
                if att[0] == "pin" and att[4] != "passive" and att[3] == side and inbox(att[2], b, 25) and not inbox(att[2], b, -8):
                    sl.setdefault(round(att[2][1] / 12), []).append(k)
            rows = sorted(sl.items())
            nums = [ends[ks[0]][1][1].rsplit(".", 1)[1] for y, ks in rows]
            used = {int(n) for n in nums if n.isdigit()}
            j = 0
            while j < len(rows):
                if nums[j].isdigit(): j += 1; continue
                e = j
                while e < len(rows) and not nums[e].isdigit(): e += 1
                if j > 0 and e < len(rows):
                    a, z = int(nums[j - 1]), int(nums[e]); lo, hi = min(a, z), max(a, z)
                    c = sorted(n_ for n_ in roles if isinstance(n_, int) and lo < n_ < hi and n_ not in used and ok(n_))
                    if a > z: c = c[::-1]
                    if len(c) == e - j:
                        for t, n_ in zip(range(j, e), c):
                            for k in rows[t][1]:
                                i, att = ends[k]; ends[k] = (i, (att[0], f"{att[1].rsplit('.', 1)[0]}.{n_}") + tuple(att[2:]))
                j = e
# nets: wire groups; groups with the same label (name + subscript) are one net
for i, att in ends:
    pass
phys = {i: find(i) for i, att in ends}      # wire group before label joins (for the stage-loss report)
lab_root = {}
# FIX13: a wire group that carries two or more different label names is a tracing mistake (one net has
# one name; parallel bus wires got joined). Such a group takes no part in label joins, so one bad trace
# does not chain whole buses together through their names.
_bad = set()
if os.environ.get("FIX13") == "1":
    _nm = {}
    for i, att in ends:
        if att[0] == "label" and att[1] and not (os.environ.get("FIX13G") == "1" and att[1][0] == "GND"): _nm.setdefault(phys[i], set()).add(att[1])
    _bad = {g for g, v in _nm.items() if len(v) >= int(os.environ.get("FIX13N", "2"))}
for i, att in ends:
    if att[0] == "label" and att[1] and (phys[i] not in _bad or (os.environ.get("FIX13K") == "1" and att[1][0] == "GND")):
        key = att[1]
        if key in lab_root:
            join(i, lab_root[key])
        else:
            lab_root[key] = i
nets = {}
for i, att in ends:
    r = find(i)
    nt = nets.setdefault(r, {"names": set(), "pins": []})
    if att[0] == "label" and att[1]:
        nt["names"].add(att[1])
    elif att[0] in ("pin", "conn"):
        nt["pins"].append(att)
# FIX3 naming: a passive has no name on the drawing. Named the way the sheet format does it
# (src/docs/FORMAT.md): <R|C|W>-<the chip it serves>-<n>. Served chip = the nearest part box with
# a pin on either of its nets. Pin rule: resistor pin 1 on the supply side (+5V), capacitor pin 2
# on the ground side, strap pin 1 on the chip side and pin 2 on GND; otherwise pin 1 top/left.
pv_names = []
if FIX3:
    term_net = {}
    for r, nt in nets.items():
        for att in nt["pins"]:
            if att[1].startswith("#"):
                k_, t_ = att[1][1:].split("."); term_net[(int(k_), int(t_))] = r
    strap_nets = [find(i) for i, att in ends if att[0] == "strap"]
    def served(rs, at):
        best = None
        for r in rs:
            for att in nets[r]["pins"]:
                if att[0] == "pin" and not att[1].startswith("#") and not att[1].startswith("?"):
                    ref = att[1].rsplit(".", 1)[0]
                    b = next((bb for rr, bb, kk in boxed if rr == ref), None)
                    if b is None: continue
                    d = abs(cen(b)[0] - at[0]) + abs(cen(b)[1] - at[1])
                    if best is None or d < best[0]: best = (d, ref)
        if best is None:
            for rr, bb, kk in boxed:
                if rr:
                    d = abs(cen(bb)[0] - at[0]) + abs(cen(bb)[1] - at[1])
                    if best is None or d < best[0]: best = (d, rr)
        return best[1] if best else "?"
    items = []
    for k_, pv in enumerate(passives):
        rs = [term_net.get((k_, 0)), term_net.get((k_, 1))]
        if rs[0] is None and rs[1] is None: continue
        nm = [{n_[0] for n_ in nets[r]["names"]} if r is not None else set() for r in rs]
        if pv["kind"] == "R":
            p1 = 1 if ("+5V" in nm[1] and "+5V" not in nm[0]) else 0          # terminal index that is pin 1
        else:
            p1 = 1 if ("GND" in nm[0] and "GND" not in nm[1]) else 0
        at = pv["t"][0]
        items.append((pv["kind"], served([r for r in rs if r is not None], at), at, k_, p1))
    for j, r in enumerate(strap_nets):
        at = next(att[2] for i, att in ends if att[0] == "strap" and find(i) == r)
        items.append(("W", served([r], at), at, -1 - j, 0))
    cnt = {}
    for kind, srv, at, k_, p1 in sorted(items, key=lambda t: (t[2][1], t[2][0])):
        cnt[(kind, srv)] = cnt.get((kind, srv), 0) + 1
        name = f"{kind}-{srv}-{cnt[(kind, srv)]}"
        pv_names.append(name)
        if k_ >= 0:
            for r, nt in nets.items():
                nt["pins"] = [(a[0], f"{name}.{1 if int(a[1].split('.')[1]) == p1 else 2}") + tuple(a[2:])
                              if a[1].startswith(f"#{k_}.") else a for a in nt["pins"]]
        else:
            r = strap_nets[-1 - k_]
            nets[r]["pins"].append(("pin", f"{name}.1", at))
            gr = next((rr for rr, nt in nets.items() if ("GND", None) in nt["names"]), None)
            if gr is not None: nets[gr]["pins"].append(("pin", f"{name}.2", at))
times["wires"] = round(time.time() - T0, 2)

# ---------------- CONF: facts for a confidence on every pin assignment (written to the dump as "pinfeat",
# turned into one number by pinconf.py). Per pin end: the letter-network score of the chosen pin number
# and its margin over the next-best number, whether it fits the chip's pinout and side, the detector
# score of its symbol, the score of the symbol's code reading and whether the code was inferred.
pinfeat = {}
if os.environ.get("FIX2") == "1":
    def _codesc(q):
        if not q.get("ref"): return -9.0
        x, y, w, h = q["box"]; best = -6.0
        for c in DETCODES:
            if c["code"] == q["ref"]:
                cx, cy = cen(c["box"])
                if x - 25 <= cx <= x + w + 25 and y - 60 <= cy <= y + h + 60: best = max(best, c["sc"])
        return best
    symof = [(q, "chip") for q in chips] + [(q, "gate") for q in gates]
    for i, att in ends:
        if att[0] != "pin" or att[4] == "passive" or "?" in att[1]: continue
        px, py = att[2]; side = att[3]; n_ = int(att[1].rsplit(".", 1)[1])
        q = next((q for q, k in symof if inbox((px, py), q["box"], 25) and not inbox((px, py), q["box"], -8)), None)
        cs = {}
        for sc, nn, ks_ in num_cands(px, py, side): cs[nn] = max(cs.get(nn, -99), sc)
        me = cs.get(n_, -12.0); other = max([v for k, v in cs.items() if k != n_], default=-12.0)
        typ = btype.get(tuple(q["box"])) if q else None; roles = PINS.get(typ) if typ else None
        fit = 0.5 if roles is None else (1.0 if roles.get(n_) not in (None, "power", "ground", "nc") else 0.0)
        f = [me, me - other, fit, float(q.get("det", 0.0)) if q else 0.0, 1.0 if q and "det" in q else 0.0,
             _codesc(q) if q else -9.0, 1.0 if q and q.get("ref_inferred") else 0.0, 1.0 if q and q in chips else 0.0]
        pinfeat.setdefault(att[1], []).append([round(float(v), 3) for v in f])
# ---------------- 5. draft sheet
KTYPE = {"nor": "7402", "not": "7404", "nand": "7400", "nand-oc": "7401", "and-or-invert": "7451", "and": "7408"}
parts = {}
for c in sorted(chips, key=lambda c: c["type"] is None):
    if c["ref"]:
        parts.setdefault(c["ref"], {"ref": c["ref"], "type": c["type"] or c["type_read"] or "?", "confidence": "inferred",
                                    "note": f"read type '{c['type_read']}'"})
gout, gcount = [], {}
for q in gates:
    if not q["ref"]:
        continue
    parts.setdefault(q["ref"], {"ref": q["ref"], "type": KTYPE.get(q["kind"]), "confidence": "inferred", "note": f"type from gate symbol ({q['kind']})"})
    gcount[q["ref"]] = gcount.get(q["ref"], 0) + 1
    ins, outp = [], None
    for i, att in ends:
        if att[0] == "pin" and len(att) > 4 and att[4] == "gate" and inbox(att[2], q["box"], 25) and not att[1].endswith("?"):
            pn = int(att[1].split(".")[-1])
            if att[3] == "L": ins.append(pn)
            elif att[3] == "R": outp = pn
    q["inputs"], q["output"] = sorted(set(ins)), outp
    gout.append({"id": f"{q['ref']}/{gcount[q['ref']]}", "part": q["ref"], "kind": "nand" if q["kind"] == "nand-oc" else q["kind"],
                 "inputs": q["inputs"], "output": outp, "confidence": "inferred"})
nout, used = [], set()
for k, (r, nt) in enumerate(sorted(nets.items())):
    pins = []
    for att in nt["pins"]:
        if "?" in att[1] or att[1] in used:
            continue
        used.add(att[1]); pins.append({"at": att[1], "confidence": "inferred"})
    if not pins:
        continue
    nm = sorted(nt["names"], key=lambda t: (t[0] not in dic, t[0]))
    name, sub = (nm[0] if nm else (f"N_draft_{k}", None))
    nout.append({"name": name, "active": {"0": "low", "1": "high"}.get(sub, "none"), "pins": pins,
                 "note": "draft; other names on this wire group: " + ", ".join(f"{a}_{b}" for a, b in nm[1:]) if len(nm) > 1 else "draft"})
sheet = {"format": "nd-netlist", "formatVersion": 1,
         "sheet": {"id": SID, "title": "draft read by machine", "machine": "NORD-10", "bus": "nord-10",
                   "note": "Machine draft from the scan only. Every part, gate and pin is inferred."},
         "parts": sorted(parts.values(), key=lambda p: p["ref"]), "gates": gout, "nets": nout}
json.dump(sheet, open(os.path.join(od, f"{SID}-draft.rsc"), "w", encoding="utf-8"), indent=1)
times["total"] = round(time.time() - T0, 2)
dump = {"words": [{k: v for k, v in wd.items() if k != "pieces"} for wd in words], "chips": chips, "gates": gates,
        "phys": [[i, int(phys.get(i, -1))] for i, a in ends], "segs": segs, "ends": [(i, list(a[:2]) + [list(a[2])] + list(a[3:])) for i, a in ends], "dots_kept": kdots, "dets": DETS, "detcodes": DETCODES,
        "circles": good, "diamonds": diamonds, "pieces": pieces, "pcls": pcls, "times": times,
        "passives": passives, "grounds": grounds, "passive_names": pv_names, "pinfeat": pinfeat,
        "stats": {"pieces": len(pieces), "words": len(words), "chips": len(chips), "gates": len(gates), "segs": len(segs),
                  "crossings_not_joined": crossings_left, "joined_by_dot_only": joins_dot, "ends_attached": len(ends),
                  "nets": len(nout), "dict_size": len(dic)}}
json.dump(dump, open(os.path.join(od, f"{SID}-dump.json"), "w"), default=lambda o: o if not isinstance(o, np.generic) else o.item())
print(json.dumps({"times": times, **dump["stats"]}))
