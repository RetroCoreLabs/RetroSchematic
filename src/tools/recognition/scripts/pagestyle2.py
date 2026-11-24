"""Page-style detector, second version (the letter-piece network of pagestyle.py learned the ND-120
font, not "CAD": it called all 24 SCSI pages hand-drawn). Two page measures from pagefeat.py:
spread of letter heights (std/mean) and median thickness of long horizontal lines. A logistic
rule on the two, trained on Binder + I/O-system (hand) and ND-120 (CAD) only, two-fold by page
parity for those books; NORD-10 and SCSI never trained on.
Writes style/pagestyle.json and style/pagestyle-score.json. Usage: python -I pagestyle2.py"""
import json, os, math
R = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
f = json.load(open(R + "/style/pagefeat.json"))
truth = lambda k: 1 if k.split(":")[0] in ("nd120", "scsi") else 0
X = {k: (v[0], v[3]) for k, v in f.items()}
def fit(keys):
    mu = [sum(X[k][j] for k in keys) / len(keys) for j in (0, 1)]
    sd = [math.sqrt(sum((X[k][j] - mu[j]) ** 2 for k in keys) / len(keys)) for j in (0, 1)]
    w = [0.0, 0.0, 0.0]
    n1 = sum(truth(k) for k in keys); n0 = len(keys) - n1
    for it in range(3000):                 # plain gradient descent, classes weighted equally
        g = [0.0, 0.0, 0.0]
        for k in keys:
            z = [(X[k][j] - mu[j]) / sd[j] for j in (0, 1)]
            p = 1 / (1 + math.exp(-(w[0] + w[1] * z[0] + w[2] * z[1])))
            e = (p - truth(k)) * (0.5 / (n1 if truth(k) else n0))
            g[0] += e; g[1] += e * z[0]; g[2] += e * z[1]
        w = [w[i] - 1.0 * g[i] for i in range(3)]
    return lambda k: int(w[0] + w[1] * (X[k][0] - mu[0]) / sd[0] + w[2] * (X[k][1] - mu[1]) / sd[1] > 0), (mu, sd, w)
tr = [k for k in X if k.split(":")[0] in ("binder", "iosys", "nd120")]
out = {}
for par in (0, 1):
    pr, _ = fit([k for k in tr if int(k.split(":")[1]) % 2 != par])
    for k in tr:
        if int(k.split(":")[1]) % 2 == par: out[k] = pr(k)
pr, model = fit(tr)
for k in X:
    if k not in out: out[k] = pr(k)
sc = {}
for k, v in out.items():
    d = sc.setdefault(k.split(":")[0], [0, 0, []]); d[1] += 1; d[0] += int(v == truth(k))
    if v != truth(k): d[2].append(k)
json.dump(out, open(R + "/style/pagestyle.json", "w"))
json.dump({"score_right_of": sc, "model_mu_sd_w": model}, open(R + "/style/pagestyle-score.json", "w"), indent=1)
print(json.dumps(sc))
