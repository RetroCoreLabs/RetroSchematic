"""Step 4: decide per page how far to turn it (clockwise 0/90/180/270) so the text reads upright.
No answer is given to it. Two signals:
  - text direction: letter pieces sit side by side in words. Count neighbour pairs that sit
    left-right vs above-below; this says whether lines run across (0/180) or down (90/270).
  - letter network confidence: mean top probability of the pieces at each turn. Upright letters
    look like the training letters; upside-down or sideways ones do not.
Decision: the turn with the highest confidence among the two that agree with the text direction.
Usage: python -I rotate.py <model.onnx> <out.json> [book ...]
"""
import site, sys, json, time, os
sys.path.append(site.getusersitepackages())
sys.path.insert(0, os.path.dirname(__file__))
import numpy as np, cv2, onnxruntime as ort
from books import BOOKS
from pageio import render_norm, pieces_of, crops, softmax, ink_of

model, outj = sys.argv[1], sys.argv[2]
want = sys.argv[3:] or list(BOOKS)
sess = ort.InferenceSession(model, providers=["CPUExecutionProvider"])
ROT = {0: None, 90: cv2.ROTATE_90_CLOCKWISE, 180: cv2.ROTATE_180, 270: cv2.ROTATE_90_COUNTERCLOCKWISE}
res = json.load(open(outj)) if os.path.exists(outj) else {}
rng = np.random.default_rng(0)


def pair_dir(pcs):
    """neighbour pairs: (side by side, one above the other)"""
    if len(pcs) < 10:
        return 0, 0
    c = np.array([(x + w / 2, y + h / 2, max(w, h)) for x, y, w, h in pcs], float)
    hp = vp = 0
    for i in range(len(c)):
        d = np.abs(c[:, :2] - c[i, :2])
        s = c[i, 2]
        hp += int(((d[:, 0] > 0.3 * s) & (d[:, 0] < 1.3 * s) & (d[:, 1] < 0.25 * s)).sum())
        vp += int(((d[:, 1] > 0.3 * s) & (d[:, 1] < 1.3 * s) & (d[:, 0] < 0.25 * s)).sum())
    return hp, vp


for book in want:
    pdf, pages = BOOKS[book]
    for p in pages:
        key = f"{book}:{p}"
        if key in res:
            continue
        t0 = time.time()
        g, f = render_norm(pdf, p)
        conf = {}
        pcs0 = pieces_of(g)
        hp, vp = pair_dir(pcs0 if len(pcs0) < 3000 else [pcs0[i] for i in rng.choice(len(pcs0), 3000, replace=False)])
        for r, code in ROT.items():
            gr = g if code is None else cv2.rotate(g, code)
            pcs = pieces_of(gr)
            if len(pcs) > 800:
                pcs = [pcs[i] for i in rng.choice(len(pcs), 800, replace=False)]
            if not pcs:
                conf[r] = 0.0; continue
            pr = softmax(sess.run(None, {"x": crops(gr, pcs)})[0])
            conf[r] = float(pr.max(1).mean())
        axis = [0, 180] if hp >= vp else [90, 270]
        pick = max(axis, key=lambda r: conf[r])
        res[key] = {"turn_cw": pick, "turn_conf_only": max(conf, key=conf.get), "conf": conf, "pairs_side": hp,
                    "pairs_stacked": vp, "scale": round(f, 3), "pieces": len(pcs0), "s": round(time.time() - t0, 1)}
        print(key, res[key], flush=True)
        json.dump(res, open(outj, "w"), indent=0)
