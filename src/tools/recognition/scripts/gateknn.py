"""Build the gate-shape nearest-neighbour table from the checked boxes in labels-nearby/.
Features come from draft.py's dump of the same page (same box order). Leave-one-page-out score
is printed as the held-out measure. Usage: python -I gateknn.py <labelsdir> <featdir> <out.json>
"""
import site, sys, json, os
sys.path.append(site.getusersitepackages())
import numpy as np
ld, fd, out = sys.argv[1:4]
PG = [int(q) for q in os.environ.get("PAGES", "61,62,63,66").split(",")]
X, y, pg = [], [], []
for p in PG:
    L = json.load(open(os.path.join(ld, f"p{p}.json"))); U = json.load(open(os.path.join(fd, f"p{p}-dump.json")))
    gl = [b for b in L["boxes"] if b["pipeline"] != "chip"]
    assert len(gl) == len(U["gates"])
    for b, q in zip(gl, U["gates"]):
        assert b["box"] == q["box"]
        X.append(q["feats"]); y.append(b["shape"]); pg.append(p)
X = np.array(X); pg = np.array(pg)
def fit(Xt):
    mu, sd = Xt.mean(0), Xt.std(0) + 1e-6
    return mu, sd
def pred(Xt, yt, Xq):
    mu, sd = fit(Xt); Z = (Xt - mu) / sd; r = []
    for q in Xq:
        d = (((q - mu) / sd - Z) ** 2).sum(1); nn = np.argsort(d)[:3]; v = {}
        for t in nn: v[yt[t]] = v.get(yt[t], 0) + 1 / (1e-3 + d[t])
        r.append(max(v, key=v.get))
    return r
right = 0
for p in PG:
    tr = pg != p
    pr = pred(X[tr], [y[i] for i in np.nonzero(tr)[0]], X[~tr])
    right += sum(a == b for a, b in zip(pr, [y[i] for i in np.nonzero(~tr)[0]]))
mu, sd = fit(X)
json.dump({"mu": mu.tolist(), "sd": sd.tolist(), "X": ((X - mu) / sd).tolist(), "y": y}, open(out, "w"))
from collections import Counter
print(json.dumps({"boxes": len(y), "classes": Counter(y).most_common(), "leave_one_page_out_right": right}))
