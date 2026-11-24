"""Step 2.1: cut letter-size pieces from every NORD-10 drawing page except page 64 (index 63).
Same cut rule as realcomp.py. A page counts as a drawing page when it has long wire lines;
index and text pages (few long lines) are skipped.
Writes harvest/pieces.npz: X (n,32,32) uint8 ink=255 (window with neighbours), M (same, piece only), meta (n,5) = page index,x,y,w,h;
and harvest/pages.json with per-page counts.
Usage: python -I harvest.py <pdf> <outdir>
"""
import site, sys, json, time
sys.path.append(site.getusersitepackages())
import numpy as np, cv2, fitz

WIN, SZ = 48, 32
pdf, out = sys.argv[1], sys.argv[2]
d = fitz.open(pdf)
X, M, meta, pages = [], [], [], []
t0 = time.time()
for pi in range(len(d)):
    if pi == 63:          # page 64 = the test page, never used for training
        pages.append({"index": pi, "page": pi + 1, "skip": "test page"}); continue
    p = d[pi].get_pixmap(dpi=300, colorspace=fitz.csGRAY)
    g = np.frombuffer(p.samples, np.uint8).reshape(p.height, p.width)
    _, ink = cv2.threshold(g, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
    L = 60
    hl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (L, 1)))
    vl = cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, (1, L)))
    nh = cv2.connectedComponents(hl, connectivity=8)[0] - 1
    nv = cv2.connectedComponents(vl, connectivity=8)[0] - 1
    txt = cv2.bitwise_and(ink, cv2.bitwise_not(cv2.dilate(hl | vl, np.ones((3, 3), np.uint8))))
    n, lab, st, cen = cv2.connectedComponentsWithStats(txt, 8)
    a = st[1:]
    k = ((a[:, 3] >= 18) & (a[:, 3] <= 38) & (a[:, 2] >= 4) & (a[:, 2] <= 30) & (a[:, 4] >= 40)) | \
        ((a[:, 3] >= 7) & (a[:, 3] <= 16) & (a[:, 2] >= 6) & (a[:, 2] <= 16) & (a[:, 4] >= 25))
    info = {"index": pi, "page": pi + 1, "size": [int(g.shape[1]), int(g.shape[0])], "h_lines": int(nh),
            "v_lines": int(nv), "pieces": int(k.sum())}
    # drawing page rule: at least 20 long lines in all (the 7 index/text pages have 0-4;
    # the smallest drawing page measured has 44). Checked by eye on thumbnails.

    if nh + nv < 20:
        info["skip"] = "no long lines (text/index page)"; pages.append(info); continue
    pd_ = cv2.copyMakeBorder(g, 24, 24, 24, 24, cv2.BORDER_CONSTANT, value=255)
    lp = np.pad(lab, 24)
    for i in np.nonzero(k)[0]:
        x, y, w, h, _ = a[i]
        cx, cy = x + w // 2, y + h // 2
        c = cv2.resize(pd_[cy:cy + WIN, cx:cx + WIN], (SZ, SZ), interpolation=cv2.INTER_AREA)
        X.append(255 - c); meta.append([pi, x, y, w, h])
        # the piece alone (its own pixels only, neighbours blanked) for grouping and naming
        lw = lp[cy:cy + WIN, cx:cx + WIN] == i + 1
        M.append(cv2.resize(np.where(lw, 255 - pd_[cy:cy + WIN, cx:cx + WIN], 0).astype(np.uint8), (SZ, SZ), interpolation=cv2.INTER_AREA))
    pages.append(info)
    print(pi, info, round(time.time() - t0), flush=True)
np.savez_compressed(out + "/pieces.npz", X=np.array(X, np.uint8), M=np.array(M, np.uint8), meta=np.array(meta, np.int32))
json.dump(pages, open(out + "/pages.json", "w"), indent=0)
print("pieces", len(X), "pages used", sum(1 for p in pages if "skip" not in p), "s", round(time.time() - t0))
