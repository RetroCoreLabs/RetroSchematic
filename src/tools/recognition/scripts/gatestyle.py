"""Step 4 (gates): style experiment for gate kinds.
  A: one shared model, no style.  B: one model per style, the page-style detector picks which.
  C: one model, style given as a second input channel (all 0 = hand, all 1 = CAD).
Same network, data, epochs and seed for all. Training: generated gates of both styles
(style/gates-synth.npz) + real NORD-10 gate candidates of pages 61,62,63,65 (style/n10-gates-train.npz).
Model chosen per epoch on HELD-OUT training-book data: NORD-10 page 66 real candidates + every 10th
generated gate (never trained on). Page 64 and SCSI are scored once at the end, never used to choose.
Labels: nand nor not and other none. SCSI 'or' is scored as 'other' (the generator puts or2 there).
Test page style comes from the style detector (style/pagestyle.json), not from the book.
Usage: python -I gatestyle.py <out.json>"""
import site, sys, os, json
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(__file__))
import numpy as np, cv2, torch, torch.nn as nn, torch.nn.functional as F
from books import BOOKS
from pageio import render_norm, gatecrop
torch.manual_seed(0); np.random.seed(0)
R = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
K = ["nand", "nor", "not", "and", "other", "none"]; KI = {k: i for i, k in enumerate(K)}
sy = np.load(R + "/style/gates-synth.npz"); n10 = np.load(R + "/style/n10-gates-train.npz")
idx = np.arange(len(sy["Y"])); hv = idx % 10 == 0
Xtr = [sy["G"][~hv], n10["G"][n10["P"] != 66]]; Ytr = [sy["Y"][~hv], n10["Y"][n10["P"] != 66]]
Str = [sy["S"][~hv], np.zeros((n10["P"] != 66).sum(), int)]
Xva = [sy["G"][hv], n10["G"][n10["P"] == 66]]; Yva = [sy["Y"][hv], n10["Y"][n10["P"] == 66]]
Sva = [sy["S"][hv], np.zeros((n10["P"] == 66).sum(), int)]
cat = lambda L: np.concatenate(L)
Xtr, Ytr, Str, Xva, Yva, Sva = map(cat, (Xtr, Ytr, Str, Xva, Yva, Sva))
# real NORD-10 is few (541 none of 578): repeat it 5x so it is not swamped by the generated set
rep = np.r_[np.zeros(int((~hv).sum()), int), np.ones(len(Ytr) - int((~hv).sum()), int)]
sel = np.r_[np.arange(len(Ytr)), np.repeat(np.nonzero(rep)[0], 4)]
Xtr, Ytr, Str = Xtr[sel], Ytr[sel], Str[sel]
Ytr = np.array([KI[y] for y in Ytr]); Yva = np.array([KI[y] for y in Yva])
# test sets
ps = json.load(open(R + "/style/pagestyle.json"))
g64 = cv2.imread(R + "/out/p64-300.png", 0)
T = {"p64": ([], [], ps["nord10:64"])}
for it in json.load(open(R + "/style/p64-gates-eye.json"))["items"]:
    T["p64"][0].append(gatecrop(g64, *it["box"])); T["p64"][1].append(KI[it["label"]])
sc = json.load(open(R + "/style/scsi-gates-eye.json"))["items"]
for p in sorted({i["page"] for i in sc}):
    g, _ = render_norm(BOOKS["scsi"][0], p)
    T[f"scsi{p}"] = ([gatecrop(g, *i["box"]) for i in sc if i["page"] == p],
                     [KI["other" if i["label"] == "or" else i["label"]] for i in sc if i["page"] == p], ps[f"scsi:{p}"])


class Net(nn.Module):
    def __init__(s, cin):
        super().__init__()
        s.c = nn.ModuleList([nn.Conv2d(cin, 16, 3, padding=1), nn.Conv2d(16, 32, 3, padding=1), nn.Conv2d(32, 64, 3, padding=1)])
        s.b = nn.ModuleList([nn.BatchNorm2d(16), nn.BatchNorm2d(32), nn.BatchNorm2d(64)])
        s.fc = nn.Linear(64 * 6 * 6, 128); s.o = nn.Linear(128, len(K)); s.dp = nn.Dropout(0.3)
    def forward(s, x):
        for c, b in zip(s.c, s.b): x = F.max_pool2d(F.relu(b(c(x))), 2)
        return s.o(s.dp(F.relu(s.fc(x.flatten(1)))))


def inp(X, S, withstyle):
    x = torch.tensor(np.asarray(X, np.float32)[:, None] / 255.0)
    if withstyle:
        x = torch.cat([x, torch.tensor(np.asarray(S, np.float32))[:, None, None, None].expand_as(x)], 1)
    return x


def train(m, withstyle, tag):
    tr = np.ones(len(Ytr), bool) if m is None else Str == m
    va = np.ones(len(Yva), bool) if m is None else Sva == m
    X, Y = inp(Xtr[tr], Str[tr], withstyle), torch.tensor(Ytr[tr]); Xv, Yv = inp(Xva[va], Sva[va], withstyle), Yva[va]
    torch.manual_seed(0); net = Net(2 if withstyle else 1); opt = torch.optim.Adam(net.parameters(), 2e-3)
    best, st, log = -1, None, []
    for ep in range(12):
        net.train(); perm = torch.randperm(len(Y))
        for i in range(0, len(Y), 128):
            b = perm[i:i + 128]; xb = X[b].clone()
            xb[:, :1] = (xb[:, :1] * torch.empty(len(b), 1, 1, 1).uniform_(0.7, 1.2) + torch.randn_like(xb[:, :1]) * 0.05).clamp(0, 1)
            loss = F.cross_entropy(net(xb), Y[b]); opt.zero_grad(); loss.backward(); opt.step()
        net.eval()
        with torch.no_grad(): a = float((net(Xv).argmax(1).numpy() == Yv).mean())
        log.append(round(a, 4))
        if a > best: best, st, bep = a, {k: v.clone() for k, v in net.state_dict().items()}, ep
    net.load_state_dict(st); net.eval(); print(tag, "held-out", log, "chosen", bep, flush=True)
    return net, {"heldout": best, "epoch": bep}


def score(pred, y):
    y, pred = np.array(y), np.array(pred); g = y != KI["none"]
    return {"gates": int(g.sum()), "found": int((pred[g] != KI["none"]).sum()), "kind_right": int((pred[g] == y[g]).sum()),
            "none": int((~g).sum()), "false_gates": int((pred[~g] != KI["none"]).sum()),
            "wrong": [(K[a], K[b]) for a, b in zip(y[g], pred[g]) if a != b]}


res = {}
mA, iA = train(None, False, "A")
mH, iH = train(0, False, "B-hand"); mC_, iC_ = train(1, False, "B-cad")
mC, iC = train(None, True, "C")
for name, (X, y, st) in T.items():
    with torch.no_grad():
        pa = mA(inp(X, None, False)).argmax(1).numpy()
        pb = (mH if st == 0 else mC_)(inp(X, None, False)).argmax(1).numpy()
        pc = mC(inp(X, [st] * len(X), True)).argmax(1).numpy()
    res[name] = {"style_used": st, "A": score(pa, y), "B": score(pb, y), "C": score(pc, y)}
res["train_info"] = {"A": iA, "B_hand": iH, "B_cad": iC_, "C": iC}
os.makedirs(R + "/model", exist_ok=True)
for n, m, c in (("A", mA, 1), ("Bh", mH, 1), ("Bc", mC_, 1), ("C", mC, 2)):
    torch.onnx.export(m, torch.zeros(1, c, 48, 48), f"{R}/model/gates-{n}.onnx", input_names=["x"], output_names=["logits"],
                      dynamic_axes={"x": {0: "n"}, "logits": {0: "n"}}, opset_version=17, dynamo=False)
json.dump(res, open(sys.argv[1], "w"), indent=1)
for n, v in res.items():
    if n != "train_info":
        print(n, "style", v["style_used"], {s: {k: v[s][k] for k in ("gates", "found", "kind_right", "none", "false_gates")} for s in "ABC"})
