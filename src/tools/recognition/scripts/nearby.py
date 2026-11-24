"""Render the 5 drawing pages nearest page 64, run the draft pipeline on each, and make crop sheets
to check by eye: a random sample of letters (sorted by the network's answer) and every gate/chip box.
Usage: python -I nearby.py <pdf> <outdir> <model> <catalog> pages...
"""
import site, sys, json, subprocess, random, os
sys.path.append(site.getusersitepackages())
import numpy as np, cv2, fitz
pdf, od, model, cat = sys.argv[1:5]; pages = [int(p) for p in sys.argv[5:]]
here = os.path.dirname(os.path.abspath(__file__))
d = fitz.open(pdf)
for pg in pages:
    png = os.path.join(od, f"p{pg}.png")
    if not os.path.exists(png):
        p = d[pg - 1].get_pixmap(dpi=300, colorspace=fitz.csGRAY)
        cv2.imwrite(png, np.frombuffer(p.samples, np.uint8).reshape(p.height, p.width))
    subprocess.run([sys.executable, "-I", os.path.join(here, "draft.py"), png, model, cat, od, f"p{pg}"], check=True, stdout=subprocess.DEVNULL)
    U = json.load(open(os.path.join(od, f"p{pg}-dump.json")))
    g = cv2.imread(png, 0); pad = cv2.copyMakeBorder(g, 40, 40, 40, 40, cv2.BORDER_CONSTANT, value=255)
    idx = list(range(len(U["pieces"]))); random.Random(pg).shuffle(idx); idx = idx[:160]
    idx.sort(key=lambda k: (U["pcls"][k], k))
    T = 64
    for s in range(2):
        img = np.full((8 * (T + 16), 10 * (T + 6), 3), 255, np.uint8)
        for j, k in enumerate(idx[s * 80:(s + 1) * 80]):
            x, y, w, h, sm = U["pieces"][k]; cx, cy = x + w // 2 + 40, y + h // 2 + 40
            t = cv2.cvtColor(pad[cy - 32:cy + 32, cx - 32:cx + 32], cv2.COLOR_GRAY2BGR)
            cv2.rectangle(t, (x + 40 - (cx - 32) - 1, y + 40 - (cy - 32) - 1), (x + 40 - (cx - 32) + w, y + 40 - (cy - 32) + h), (0, 0, 255), 1)
            r, c = divmod(j, 10)
            img[r * (T + 16):r * (T + 16) + T, c * (T + 6):c * (T + 6) + T] = t
            cv2.putText(img, f"{j + s * 80}:{U['pcls'][k]}", (c * (T + 6), r * (T + 16) + T + 12), 0, 0.42, (200, 0, 0), 1)
        cv2.imwrite(os.path.join(od, f"p{pg}-letters{s}.png"), img)
    json.dump(idx, open(os.path.join(od, f"p{pg}-sample.json"), "w"))
    bx = [("chip", c["box"]) for c in U["chips"]] + [(q["kind"], q["box"]) for q in U["gates"]]
    T = 110; cols = 8
    img = np.full(((len(bx) + cols - 1) // cols * (T + 16), cols * (T + 6), 3), 255, np.uint8)
    for j, (kd, (x, y, w, h)) in enumerate(bx):
        cx, cy = x + w // 2 + 40, y + h // 2 + 40
        sc = min(1.0, (T - 6) / max(w + 30, h + 30))
        crop = pad[max(0, cy - int(h / 2) - 15):cy + int(h / 2) + 15, max(0, cx - int(w / 2) - 15):cx + int(w / 2) + 15]
        crop = cv2.resize(crop, None, fx=sc, fy=sc)
        r, c = divmod(j, cols)
        img[r * (T + 16):r * (T + 16) + crop.shape[0], c * (T + 6):c * (T + 6) + crop.shape[1]] = cv2.cvtColor(crop, cv2.COLOR_GRAY2BGR)
        cv2.putText(img, f"{j}:{kd}", (c * (T + 6), r * (T + 16) + T + 12), 0, 0.42, (200, 0, 0), 1)
    if bx: cv2.imwrite(os.path.join(od, f"p{pg}-boxes.png"), img)
    print(pg, U["stats"])
