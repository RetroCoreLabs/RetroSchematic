"""Shared helpers for the ND-100 books: render a PDF page (read-only), scale it so letters are about
the NORD-10 training size, cut letter pieces, run the letter network."""
import site, sys
sys.path.append(site.getusersitepackages())
import numpy as np, cv2, fitz

TARGET_H = 25.0          # typical letter height on the NORD-10 300 dpi pages the network learned from


def render(pdf, page, dpi=200):
    d = fitz.open(pdf)
    pm = d[page - 1].get_pixmap(dpi=dpi, colorspace=fitz.csGRAY)
    g = np.frombuffer(pm.samples, np.uint8).reshape(pm.height, pm.stride)[:, :pm.width].copy()
    d.close()
    # Contrast stretch: some scans (the ND-120 book) are light grey, darkest ink about 130 of 255,
    # while the others are black. A network then learns "faint = that book" instead of the shapes
    # (the first page-style detector called every SCSI page hand-drawn for exactly this reason).
    # The darkest 0.5 % of the page is taken as full black; black pages are unchanged.
    lo = float(np.percentile(g, 0.5))
    if lo > 5:
        g = np.clip((g.astype(np.float32) - lo) * 255.0 / (255.0 - lo), 0, 255).astype(np.uint8)
    return g


def ink_of(g):
    _, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
    return ink


def letter_size(ink):
    """Most common size (long side) of letter-like ink pieces, rotation independent. The mode,
    not the median: on sparse CAD pages the median was pulled up by symbol parts and specks.
    Page 64 (NORD-10, the training scale) measures 20 at 300 dpi with this rule."""
    n, lab, st, _ = cv2.connectedComponentsWithStats(ink, 8)
    w, h, a = st[1:, 2], st[1:, 3], st[1:, 4]
    big, small = np.maximum(w, h), np.minimum(w, h)
    fill = a / np.maximum(1, w * h)
    sel = (big >= 6) & (big <= 80) & (small >= 0.25 * big) & (fill > 0.12) & (fill < 0.75)
    if sel.sum() < 30:
        return None
    hb = np.convolve(np.bincount(big[sel], minlength=81), [1, 2, 1], "same")
    return float(np.argmax(hb))


def normalised(g):
    """Page scaled so its letters are about TARGET_H high. Returns (image, scale)."""
    s = letter_size(ink_of(g))
    if s is None:
        return g, 1.0
    f = TARGET_H / s
    f = min(3.0, max(0.33, f))
    return cv2.resize(g, None, fx=f, fy=f, interpolation=cv2.INTER_AREA if f < 1 else cv2.INTER_CUBIC), f


def pieces_of(g, ink=None):
    """Letter-sized pieces of ink that are not long lines (same size rule as draft.py)."""
    if ink is None:
        ink = ink_of(g)
    L = 60
    hl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (L, 1)))
    vl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, L)))
    txt = cv2.bitwise_and(ink, cv2.bitwise_not(cv2.dilate(hl | vl, np.ones((3, 3), np.uint8))))
    n, lab, st, _ = cv2.connectedComponentsWithStats(txt, 8)
    out = []
    for i in range(1, n):
        x, y, w, h, a = st[i]
        if 18 <= h <= 38 and 4 <= w <= 40 and a >= 40:
            out.append((int(x), int(y), int(w), int(h)))
    return out


WIN, SZ = 48, 32


def crops(g, pcs):
    pad = cv2.copyMakeBorder(g, WIN, WIN, WIN, WIN, cv2.BORDER_CONSTANT, value=255)
    X = np.zeros((len(pcs), 1, SZ, SZ), np.float32)
    for k, (x, y, w, h) in enumerate(pcs):
        cx, cy = int(x + w / 2) + WIN, int(y + h / 2) + WIN
        c = pad[cy - WIN // 2:cy + WIN // 2, cx - WIN // 2:cx + WIN // 2]
        X[k, 0] = (255 - cv2.resize(c, (SZ, SZ), interpolation=cv2.INTER_AREA)).astype(np.float32) / 255
    return X


def softmax(z):
    e = np.exp(z - z.max(1, keepdims=True))
    return e / e.sum(1, keepdims=True)


def render_norm(pdf, page):
    """Render at 150 dpi, measure the letters, render again at the dpi that makes them TARGET_H."""
    g = render(pdf, page, 150)
    s = letter_size(ink_of(g))
    if s is None:
        return g, 150
    dpi = int(min(600, max(100, 300 * (10.0 / s))))      # page 64 measures 10 at 150 dpi
    return render(pdf, page, dpi), dpi


def gatecands(ink):
    """Gate-symbol candidates: closed white areas (holes in the ink) of gate size, any outline
    (rectangle, D shape, OR shape, triangle). Sizes are for a page scaled to TARGET_H letters."""
    cs, hh = cv2.findContours(ink, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_SIMPLE)
    out = []
    for i, c in enumerate(cs):
        if hh[0][i][3] < 0:
            continue
        x, y, w, h = cv2.boundingRect(c)
        if not (22 <= w <= 110 and 22 <= h <= 130):
            continue
        fill = cv2.contourArea(c) / float(w * h)
        if 0.4 <= fill <= 0.99:
            out.append((int(x), int(y), int(w), int(h)))
    return out


def gatecrop(g, x, y, w, h, S=48):
    """Square crop around a symbol body with 40 % margin (bubble, pin stubs), ink = 255."""
    s = int(max(w, h) * 1.4) + 4
    cx, cy = x + w // 2, y + h // 2
    p = cv2.copyMakeBorder(g, s, s, s, s, cv2.BORDER_CONSTANT, value=255)
    c = p[cy + s - s // 2:cy + s + s // 2, cx + s - s // 2:cx + s + s // 2]
    return 255 - cv2.resize(c, (S, S), interpolation=cv2.INTER_AREA)
