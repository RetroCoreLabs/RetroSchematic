"""Step 6: cut letter pieces and gate-symbol candidates from the ND-100 drawing pages.
The SCSI/floppy card 3207 (the planned ND-100 TEST card) is never read here: the whole SCSI book
is left out, and any page listed in TESTPAGES (other books) too.
Each page is rendered so its letters are about the NORD-10 training size (pageio.render_norm),
then turned by the automatic turn from step 4 (rot.json), never by a known answer.
A page counts as a drawing page when it has 20 or more long lines (same rule as harvest.py).
Writes <out>/pieces.npz: X (window with neighbours), M (piece alone), meta = book,page,x,y,w,h
       <out>/gatecand.npz: G (48x48 symbol crop), meta = book,page,x,y,w,h
       <out>/pages.json
Usage: python -I harvest100.py <rot.json> <out> [max pieces per page]
"""
import site, sys, os, json, time
sys.path.append(site.getusersitepackages())
sys.path.insert(0, os.path.dirname(__file__))
import numpy as np, cv2
from books import BOOKS
from pageio import render_norm, ink_of, gatecands, gatecrop

rotj, out = sys.argv[1], sys.argv[2]
CAP = int(sys.argv[3]) if len(sys.argv) > 3 else 700
TESTPAGES = set()            # (book, page) of the SCSI card found in other books: none found (step 5)
BOOKID = {"binder": 0, "nd120": 1, "scsi": 2, "iosys": 3}
ROT = {0: None, 90: cv2.ROTATE_90_CLOCKWISE, 180: cv2.ROTATE_180, 270: cv2.ROTATE_90_COUNTERCLOCKWISE}
rot = json.load(open(rotj))
WIN, SZ, GS = 48, 32, 48
rng = np.random.default_rng(0)
X, M, meta, G, gmeta, pages = [], [], [], [], [], []
t0 = time.time()
for book in ("binder", "nd120", "iosys"):
    pdf, pgs = BOOKS[book]
    for p in [q for q in pgs if book != "iosys" or 222 <= q <= 241]:
        info = {"book": book, "page": p}
        if (book, p) in TESTPAGES:
            info["skip"] = "test card"; pages.append(info); continue
        g, dpi = render_norm(pdf, p)
        turn = rot[f"{book}:{p}"]["turn_conf_only"]     # confidence-only rule (step 4 result)
        if ROT[turn] is not None:
            g = cv2.rotate(g, ROT[turn])
        ink = ink_of(g)
        L = 60
        hl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (L, 1)))
        vl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, L)))
        nh = cv2.connectedComponents(hl, connectivity=8)[0] - 1
        nv = cv2.connectedComponents(vl, connectivity=8)[0] - 1
        info.update({"dpi": dpi, "turn_cw": turn, "h_lines": int(nh), "v_lines": int(nv)})
        if nh + nv < 20:
            info["skip"] = "no long lines (text/list page)"; pages.append(info); continue
        txt = cv2.bitwise_and(ink, cv2.bitwise_not(cv2.dilate(hl | vl, np.ones((3, 3), np.uint8))))
        n, lab, st, _ = cv2.connectedComponentsWithStats(txt, 8)
        a = st[1:]
        k = ((a[:, 3] >= 18) & (a[:, 3] <= 38) & (a[:, 2] >= 4) & (a[:, 2] <= 30) & (a[:, 4] >= 40)) | \
            ((a[:, 3] >= 7) & (a[:, 3] <= 16) & (a[:, 2] >= 6) & (a[:, 2] <= 16) & (a[:, 4] >= 25))
        idx = np.nonzero(k)[0]
        info["pieces_all"] = int(len(idx))
        if len(idx) > CAP:
            idx = rng.choice(idx, CAP, replace=False)
        pd_ = cv2.copyMakeBorder(g, 24, 24, 24, 24, cv2.BORDER_CONSTANT, value=255)
        lp = np.pad(lab, 24)
        for i in idx:
            x, y, w, h, _ = a[i]
            cx, cy = x + w // 2, y + h // 2
            c = cv2.resize(pd_[cy:cy + WIN, cx:cx + WIN], (SZ, SZ), interpolation=cv2.INTER_AREA)
            X.append(255 - c); meta.append([BOOKID[book], p, x, y, w, h])
            lw = lp[cy:cy + WIN, cx:cx + WIN] == i + 1
            M.append(cv2.resize(np.where(lw, 255 - pd_[cy:cy + WIN, cx:cx + WIN], 0).astype(np.uint8), (SZ, SZ), interpolation=cv2.INTER_AREA))
        # gate-symbol candidates: closed white areas (holes) of gate size, see gatecands()
        for (x, y, w, h) in gatecands(ink):
            G.append(gatecrop(g, x, y, w, h)); gmeta.append([BOOKID[book], p, x, y, w, h])
        info["pieces_kept"] = int(len(idx))
        pages.append(info)
        print(info, round(time.time() - t0), flush=True)
np.savez_compressed(out + "/pieces.npz", X=np.array(X, np.uint8), M=np.array(M, np.uint8), meta=np.array(meta, np.int32))
np.savez_compressed(out + "/gatecand.npz", G=np.array(G, np.uint8), meta=np.array(gmeta, np.int32))
json.dump(pages, open(out + "/pages.json", "w"), indent=0)
print("pieces", len(X), "gate candidates", len(G), "pages used", sum(1 for p in pages if "skip" not in p), "s", round(time.time() - t0))
