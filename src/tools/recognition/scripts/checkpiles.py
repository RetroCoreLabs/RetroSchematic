"""300 random pieces from the NAMED piles of both styles (150 each), drawn with the pile's name, for
counting mix-ups by eye. Writes harvest100/check-*.png and harvest100/check-sample.json.
Usage: python -I checkpiles.py"""
import site, sys, os, json
sys.path.append(site.getusersitepackages())
import numpy as np, cv2
R = os.path.dirname(os.path.dirname(os.path.abspath(__file__))) + "/harvest100"
rng = np.random.default_rng(1); out = []
for st in ("hand", "cad"):
    M = np.load(f"{R}/{st}/pieces.npz")["M"]; a = np.load(f"{R}/{st}/clusters.npz")["assign"]
    nm = {int(l.split()[0]): l.split()[1] for l in open(f"{R}/{st}/names.txt") if l[0] != "#" and l.strip()}
    idx = np.nonzero(np.isin(a, list(nm)))[0]
    for i in rng.choice(idx, 150, replace=False):
        out.append((st, int(i), nm[int(a[i])], M[i]))
C, S = 15, 64
for sh in range(2):
    part = out[sh * 150:(sh + 1) * 150]
    img = np.full((10 * (S + 16), C * S), 255, np.uint8)
    for j, (st, i, n, m) in enumerate(part):
        r, q = divmod(j, C)
        img[r * (S + 16) + 16:r * (S + 16) + 16 + S, q * S:(q + 1) * S] = cv2.resize(255 - m, (S, S), interpolation=cv2.INTER_NEAREST)
        cv2.putText(img, f"{sh*150+j}:{n}", (q * S + 1, r * (S + 16) + 12), 0, 0.38, 0, 1)
    cv2.imwrite(f"{R}/check-{sh}.png", img)
json.dump([(s, i, n) for s, i, n, _ in out], open(f"{R}/check-sample.json", "w"))
