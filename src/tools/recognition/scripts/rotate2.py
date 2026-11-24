"""Turn finder, second version: same pieces and letter network as rotate.py, but keeps several
per-turn measures so the decision rule can be chosen afterwards (no answers are given to it):
  mean  - mean top probability (the old confidence rule)
  med   - median top probability
  hi    - share of pieces with top probability >= 0.9
  ent   - mean entropy of the class probabilities (lower = surer)
Usage: python -I rotate2.py <model.onnx> <out.json> [book ...]
"""
import site, sys, json, time, os
sys.path.append(site.getusersitepackages())
sys.path.insert(0, os.path.dirname(__file__))
import numpy as np, cv2, onnxruntime as ort
from books import BOOKS
from pageio import render_norm, pieces_of, crops, softmax

model, outj = sys.argv[1], sys.argv[2]
want = sys.argv[3:] or list(BOOKS)
sess = ort.InferenceSession(model, providers=["CPUExecutionProvider"])
ROT = {0: None, 90: cv2.ROTATE_90_CLOCKWISE, 180: cv2.ROTATE_180, 270: cv2.ROTATE_90_COUNTERCLOCKWISE}
res = json.load(open(outj)) if os.path.exists(outj) else {}
rng = np.random.default_rng(0)
for book in want:
    pdf, pages = BOOKS[book]
    for p in pages:
        key = f"{book}:{p}"
        if key in res:
            continue
        g, f = render_norm(pdf, p)
        m = {}
        for r, code in ROT.items():
            gr = g if code is None else cv2.rotate(g, code)
            pcs = pieces_of(gr)
            if len(pcs) > 800:
                pcs = [pcs[i] for i in rng.choice(len(pcs), 800, replace=False)]
            if not pcs:
                m[r] = {"mean": 0, "med": 0, "hi": 0, "ent": 9, "n": 0}; continue
            pr = softmax(sess.run(None, {"x": crops(gr, pcs)})[0])
            t = pr.max(1)
            m[r] = {"mean": float(t.mean()), "med": float(np.median(t)), "hi": float((t >= 0.9).mean()),
                    "ent": float((-pr * np.log(pr + 1e-9)).sum(1).mean()), "n": len(pcs)}
        res[key] = m
        print(key, {r: round(v["hi"], 3) for r, v in m.items()}, flush=True)
        json.dump(res, open(outj, "w"))
