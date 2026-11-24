"""Confidence curve on the CONNECTION measure. Pin confidences come from pinconf.py's model (work pages
leave-one-page-out, page 64 from all work pages); a draft pair's confidence is the lower of its two pins.
Pairs are ranked by confidence; for each cut: pairs kept, right (key joins them), precision.
usage: connconf.py <round dir>"""
import sys, json, numpy as np
src = open(__file__.replace("connconf.py", "pinconf.py"), encoding="utf-8").read().split("def curve")[0]
sys.argv = [sys.argv[0], sys.argv[1]]; g = {"__file__": __file__}; exec(src, g)
conf, load, WORK = g["conf"], g["load"], g["WORK"]
def pairs(page):
    K, U, D = load(page); km = {(p["at"] if isinstance(p, dict) else p): i for i, n in enumerate(K["nets"]) for p in n["pins"]}
    c = conf[page]; k = 0; out = []
    for n in D["nets"]:
        ps = []
        for p in n["pins"]: ps.append((p["at"], c[k])); k += 1
        for a in range(len(ps)):
            for b in range(a + 1, len(ps)):
                x, y = ps[a][0], ps[b][0]
                if x == y: continue
                out.append((min(ps[a][1], ps[b][1]), 1 if x in km and y in km and km[x] == km[y] else 0))
    return out
for name, P in (("work", WORK), ("p64", ["p64"])):
    L = sorted([r for p in P for r in pairs(p)], key=lambda r: -r[0]); y = np.array([r[1] for r in L]); cum = np.cumsum(y)
    pts = [(int(len(y) * q), round(float(cum[int(len(y) * q) - 1] / int(len(y) * q)), 3), int(cum[int(len(y) * q) - 1]), round(float(L[int(len(y) * q) - 1][0]), 3)) for q in (0.05, 0.1, 0.2, 0.3, 0.5, 0.7, 1.0) if int(len(y) * q) > 0]
    print(name, "pairs", len(y), "right", int(y.sum()), "curve (kept, precision, right, threshold):", pts)
