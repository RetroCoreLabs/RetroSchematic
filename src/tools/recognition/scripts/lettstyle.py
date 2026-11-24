"""Step 4 (letters): style experiment, same three setups as gatestyle.py.
  A: one shared model.  B: hand model + CAD model, the page-style detector picks.  C: one model, style
  as a second input channel.
Same network (train.py Net, 39 classes incl. x), same data, epochs, seed and sampling for all.
Training data, hand style (0): generated pages synth2/ (as nd-letters-c3), named NORD-10 piles
(harvest/), checked letters of NORD-10 pages 61,62,63 (x10), named Binder + I/O-system piles
(harvest100/hand). CAD style (1): named ND-120 piles (harvest100/cad).
Held-out (choosing the epoch, never trained on): every 3rd checked letter of NORD-10 pages 61-66
(as c3) + pieces of every 5th ND-100 page (page % 5 == 0) of both styles' named piles.
Test, scored once at the end: page 64 (148 letters, hand) and SCSI pages 6/13/21 (173 pieces, CAD),
each with the style the detector gives its page (style/pagestyle.json).
Usage: python -I lettstyle.py <out.json>"""
import site, sys, os, json, glob, random
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np, cv2, torch, torch.nn.functional as F
R = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
outj = sys.argv[1]
N10PDF = os.path.join(os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..")), "docs/sources/nord-10/NORD-10-drawings.pdf")     # read-only
sys.argv = ["train.py", R + "/synth2", R + "/real", R + "/out/p64-300.png", R + "/model"]
src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "train.py")).read().split("t0 = time.time()")[0]
ns = {"__file__": "train.py"}; exec(compile(src, "train.py", "exec"), ns)
CLASSES = ns["CLASSES"] + ["x"]; CI = {c: i for i, c in enumerate(CLASSES)}; ns["CI"].update(CI)
crop, load_synth, Net = ns["crop"], ns["load_synth"], ns["Net"]
from books import BOOKS
from pageio import render_norm
torch.manual_seed(0); np.random.seed(0); random.seed(0)
CAP = 3000
rng = np.random.default_rng(0)
Xs, Ys = load_synth(sorted(glob.glob(R + "/synth2/page*.png")), 2); Xs = (Xs * 255).astype(np.uint8)
parts_tr, parts_va = [(Xs, Ys, 0)], []


def piles(hd, held_fn, style, cap=CAP):
    z = np.load(hd + "/pieces.npz"); X, meta = z["X"], z["meta"]; a = np.load(hd + "/clusters.npz")["assign"]
    nm = {int(l.split()[0]): l.split()[1] for l in open(hd + "/names.txt") if l[0] != "#" and l.strip()}
    lab = np.array([CI[nm[int(g)]] if int(g) in nm else -1 for g in a]); held = held_fn(meta)
    tr, va = [], []
    for c in range(len(CLASSES)):
        i1 = np.nonzero((lab == c) & ~held)[0]; i2 = np.nonzero((lab == c) & held)[0]
        tr += list(rng.choice(i1, min(cap, len(i1)), replace=False)); va += list(rng.choice(i2, min(300, len(i2)), replace=False))
    parts_tr.append((X[tr, None], lab[tr], style))
    if va: parts_va.append((X[va, None], lab[va], style))


piles(R + "/harvest", lambda m: np.zeros(len(m), bool), 0)           # NORD-10 named piles (all train)
piles(R + "/harvest100/hand", lambda m: m[:, 1] % 5 == 0, 0)
piles(R + "/harvest100/cad", lambda m: m[:, 1] % 5 == 0, 1)
import fitz
pdfd = fitz.open(N10PDF)
Xn, Yn, Xv, Yv = [], [], [], []
for f in sorted(glob.glob(R + "/labels-nearby/p*.json")):
    L = json.load(open(f)); pp = pdfd[L["page"] - 1].get_pixmap(dpi=300, colorspace=fitz.csGRAY)
    gp = np.frombuffer(pp.samples, np.uint8).reshape(pp.height, pp.width)
    for j, l in enumerate(L["letters"]):
        x, y, w, h = l["box"]; c = (crop(gp, x + w / 2, y + h / 2) * 255).astype(np.uint8)
        if j % 3 == 2: Xv.append(c); Yv.append(CI[l["label"]])
        elif L["page"] in (61, 62, 63):
            for _ in range(10): Xn.append(c); Yn.append(CI[l["label"]])
parts_tr.append((np.array(Xn)[:, None], np.array(Yn), 0)); parts_va.append((np.array(Xv)[:, None], np.array(Yv), 0))
Xtr = np.concatenate([p[0] for p in parts_tr]); Ytr = np.concatenate([p[1] for p in parts_tr])
Str = np.concatenate([np.full(len(p[1]), p[2]) for p in parts_tr])
Xva = np.concatenate([p[0] for p in parts_va]); Yva = np.concatenate([p[1] for p in parts_va])
Sva = np.concatenate([np.full(len(p[1]), p[2]) for p in parts_va])
print("train", len(Ytr), "hand", int((Str == 0).sum()), "cad", int((Str == 1).sum()), "held-out", len(Yva),
      "hand", int((Sva == 0).sum()), "cad", int((Sva == 1).sum()), flush=True)
# tests
ps = json.load(open(R + "/style/pagestyle.json"))
g = cv2.imread(R + "/out/p64-300.png", 0); comps = json.load(open(R + "/real/comps.json")); T = {}
X64, Y64 = [], []
for line in open(R + "/real/labels.txt"):
    if line.startswith("#") or not line.strip(): continue
    k, l = line.split(); x, y, w, h = comps[int(k)]; X64.append((crop(g, x + w / 2, y + h / 2) * 255).astype(np.uint8)); Y64.append(CI[l])
T["p64"] = (np.array(X64)[:, None], np.array(Y64), ps["nord10:64"])
sl = json.load(open(R + "/style/scsi-sample.json"))["letters"]
for p in sorted({l["page"] for l in sl}):
    gs, _ = render_norm(BOOKS["scsi"][0], p)
    L = [l for l in sl if l["page"] == p]
    T[f"scsi{p}"] = (np.array([(crop(gs, l["box"][0] + l["box"][2] / 2, l["box"][1] + l["box"][3] / 2) * 255).astype(np.uint8) for l in L])[:, None],
                     np.array([CI[l["label"]] for l in L]), ps[f"scsi:{p}"])


class Net2(Net):
    def __init__(s, n):
        super().__init__(n); s.c1 = torch.nn.Conv2d(2, 16, 3, padding=1)


def inp(X, S, ws):
    x = torch.tensor(X.astype(np.float32) / 255)
    if ws: x = torch.cat([x, torch.tensor(np.asarray(S, np.float32))[:, None, None, None].expand_as(x)], 1)
    return x


def pred(net, X, S, ws):
    with torch.no_grad():
        return np.concatenate([net(inp(X[i:i + 4096], None if S is None else S[i:i + 4096], ws)).argmax(1).numpy() for i in range(0, len(X), 4096)])


def train(m, ws, tag, EP=6):
    tr = np.ones(len(Ytr), bool) if m is None else Str == m; va = np.ones(len(Yva), bool) if m is None else Sva == m
    torch.manual_seed(0); net = Net2(len(CLASSES)) if ws else Net(len(CLASSES)); opt = torch.optim.Adam(net.parameters(), 2e-3)
    Xt, Yt, St = Xtr[tr], torch.tensor(Ytr[tr]), Str[tr]; n = len(Yt)
    sched = torch.optim.lr_scheduler.OneCycleLR(opt, 3e-3, total_steps=EP * ((n + 127) // 128))
    best, log = -1, []
    for ep in range(EP):
        net.train(); perm = torch.randperm(n).numpy()
        for i in range(0, n, 128):
            b = perm[i:i + 128]; xb = inp(Xt[b], St[b], ws)
            xb[:, :1] = (xb[:, :1] * torch.empty(len(b), 1, 1, 1).uniform_(0.7, 1.2) + torch.randn_like(xb[:, :1]) * 0.05).clamp(0, 1)
            loss = F.cross_entropy(net(xb), Yt[b]); opt.zero_grad(); loss.backward(); opt.step(); sched.step()
        net.eval(); a = float((pred(net, Xva[va], Sva[va], ws) == Yva[va]).mean()); log.append(round(a, 4))
        if a > best: best, st, bep = a, {k: v.clone() for k, v in net.state_dict().items()}, ep
    net.load_state_dict(st); net.eval(); print(tag, "held-out", log, "chosen", bep, flush=True)
    return net, {"heldout": best, "epoch": bep, "log": log}


mA, iA = train(None, False, "A"); mH, iH = train(0, False, "B-hand"); mCd, iCd = train(1, False, "B-cad"); mC, iC = train(None, True, "C")
res = {"train_info": {"A": iA, "B_hand": iH, "B_cad": iCd, "C": iC}}
for name, (X, y, st) in T.items():
    S = np.full(len(y), st)
    r = {"style_used": st, "n": int(len(y))}
    for k, (net, ws) in {"A": (mA, False), "B": (mH if st == 0 else mCd, False), "C": (mC, True)}.items():
        p = pred(net, X, S, ws); r[k] = int((p == y).sum())
        r[k + "_wrong"] = [(CLASSES[a], CLASSES[b]) for a, b in zip(y, p) if a != b]
    res[name] = r; print(name, {k: r[k] for k in ("style_used", "n", "A", "B", "C")}, flush=True)
for n, net, c in (("A", mA, 1), ("Bh", mH, 1), ("Bc", mCd, 1), ("C", mC, 2)):
    torch.onnx.export(net, torch.zeros(1, c, 32, 32), f"{R}/model/letters-{n}.onnx", input_names=["x"], output_names=["logits"],
                      dynamic_axes={"x": {0: "n"}, "logits": {0: "n"}}, opset_version=17, dynamo=False)
json.dump(res, open(outj, "w"), indent=1)
