import os
"""Confidence for every pin assignment of a draft, and the precision/recall curve.
usage: pinconf.py <round dir>    (work pages from loop/key, page 64 from the 2A50 sheet)
A pin assignment = a draft pin 'REF.n' put in a draft net. Label (strict): right when the key has that pin and
the draft net maps to the pin's key net (score.py's rule); wrong when it maps elsewhere OR the key has no such
pin (a wrong code or pin number makes a pin that does not exist). Also reported: the same for pins in nets of
2+ pins only (a net with one pin joins nothing and is right by the scoring rule).
Second draft (optional): agreement = share of the pin's net-mates that are also its net-mates there.
Model: logistic regression (numpy) on the facts below; the work pages are scored leave-one-page-out (each
page by a model trained on the other nine), page 64 by a model trained on all ten work pages."""
import sys, json, re
import numpy as np
from collections import Counter, defaultdict
R = sys.argv[1]; R2 = sys.argv[2] if len(sys.argv) > 2 else None   # R2: a second draft run (other flags), for agreement
WORK = ["n10-61", "n10-62", "n10-63", "n10-65", "n10-66", "binder8", "binder96", "iosys228", "iosys237", "nd12041"]
def load(page):
    if page == "p64":
        return (json.load(open(os.path.join(os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..")), "sheets/nord-10/2A50-sync-modem-buffer-1050.rsc"), encoding="utf-8")),
                json.load(open(f"{R}/p64/2A50-dump.json")), json.load(open(f"{R}/p64/2A50-draft.rsc", encoding="utf-8")))
    return (json.load(open(f"loop/key/{page}.json", encoding="utf-8")), json.load(open(f"{R}/{page}/{page}-dump.json")),
            json.load(open(f"{R}/{page}/{page}-draft.rsc", encoding="utf-8")))
def rows(page):
    K, U, D = load(page)
    mates = {}
    if R2:
        D2 = json.load(open(f"{R2}/p64/2A50-draft.rsc" if page == "p64" else f"{R2}/{page}/{page}-draft.rsc", encoding="utf-8"))
        for n2 in D2["nets"]:
            s2 = {q["at"] for q in n2["pins"]}
            for q in n2["pins"]: mates[q["at"]] = s2
    kpin = {(p["at"] if isinstance(p, dict) else p): i for i, n in enumerate(K["nets"]) for p in n["pins"]}
    phys = dict((a, b) for a, b in U.get("phys", []))
    grp = defaultdict(set)
    for i, a in U["ends"]:
        if a[0] in ("pin", "conn"): grp[a[1]].add(phys.get(i))
    PF = U.get("pinfeat", {})
    out = []
    for n in D["nets"]:
        c = Counter(kpin[p["at"]] for p in n["pins"] if p["at"] in kpin); m = c.most_common(1)[0][0] if c else None
        groups = set().union(*[grp.get(p["at"], set()) for p in n["pins"]]) if n["pins"] else set()
        named = 0.0 if n["name"].startswith("N_draft") else 1.0
        for p in n["pins"]:
            at = p["at"]; ref = at.rsplit(".", 1)[0]
            y = 1 if (at in kpin and kpin[at] == m) else 0
            pf = PF.get(at)
            f = np.mean(np.array(pf), 0).tolist() if pf else [-12.0, 0.0, 0.5, 0.0, 0.0, -9.0, 0.0, 0.0]
            isconn = 1.0 if ref == "P" else 0.0; ispas = 1.0 if re.match(r"^[RCW]-", ref) else 0.0
            f += [len(pf or []), isconn, ispas, np.log(len(n["pins"])), 1.0 if len(n["pins"]) == 1 else 0.0, len(groups), named,
                  len(grp.get(at, ()))]
            if R2:
                my = {q["at"] for q in n["pins"]} - {at}; o2 = mates.get(at)
                f += [0.0 if o2 is None else 1.0, (len(my & o2) / len(my)) if (o2 is not None and my) else 0.0]
            out.append((f, y, at in kpin, len(n["pins"]) > 1))
    return out
def fit(X, y, it=3000, lam=1e-2):
    mu, sd = X.mean(0), X.std(0) + 1e-6; Z = (X - mu) / sd; Z = np.hstack([Z, np.ones((len(Z), 1))])
    w = np.zeros(Z.shape[1])
    for _ in range(it):
        p = 1 / (1 + np.exp(-Z @ w)); w -= 0.1 * (Z.T @ (p - y) / len(y) + lam * w)
    return lambda X2: 1 / (1 + np.exp(-(np.hstack([(X2 - mu) / sd, np.ones((len(X2), 1))]) @ w)))
data = {p: rows(p) for p in WORK + ["p64"]}
def arr(ps): 
    L = [r for p in ps for r in data[p]]
    return np.array([r[0] for r in L], float), np.array([r[1] for r in L], float), np.array([r[3] for r in L], bool), np.array([r[2] for r in L], bool)
conf = {}
for p in WORK:
    X, y, _, _ = arr([q for q in WORK if q != p]); f = fit(X, y); Xp, _, _, _ = arr([p]); conf[p] = f(Xp)
X, y, _, _ = arr(WORK); f = fit(X, y); Xp, _, _, _ = arr(["p64"]); conf["p64"] = f(Xp)
def curve(name, ps, multi_only=False):
    c = np.concatenate([conf[p] for p in ps]); _, y, mul, ink = arr(ps)
    if multi_only: c, y, ink = c[mul], y[mul], ink[mul]
    o = np.argsort(-c); c, y, ink = c[o], y[o], ink[o]
    cum = np.cumsum(y); n = np.arange(1, len(y) + 1); prec = cum / n
    res = {"set": name + (" (nets of 2+ pins)" if multi_only else ""), "assigned": int(len(y)), "right": int(y.sum()),
           "wrong_in_key": int(((y == 0) & ink).sum()), "wrong_not_in_key": int(((y == 0) & ~ink).sum()), "precision_all": round(float(y.mean()), 3)}
    for t in (0.9, 0.95, 0.99):
        ok = np.nonzero(prec >= t)[0]
        k = int(ok.max()) if len(ok) else -1
        res[f"p{int(t*100)}"] = None if k < 0 else {"threshold": round(float(c[k]), 3), "pins_kept": k + 1, "right_kept": int(cum[k])}
    res["curve"] = [(int(k), round(float(prec[k - 1]), 3), int(cum[k - 1]), round(float(c[k - 1]), 3)) for k in
                    sorted({max(1, int(len(y) * q)) for q in (0.05, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0)})]
    print(json.dumps(res))
curve("work pages (leave-one-page-out)", WORK); curve("page 64", ["p64"])
curve("work pages (leave-one-page-out)", WORK, True); curve("page 64", ["p64"], True)
