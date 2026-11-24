"""Step 4: page-style detector, hand-drawn old ND style (0) vs CAD (1).
A small network looks at letter pieces (the 48->32 window from harvest100.py) and says which style
the piece comes from; a page's style = the mean over its pieces (>= 0.5 -> CAD).
Trained on the ND-100 harvest: Binder + I/O-system = hand, ND-120 = CAD. Scored two-fold by page
(even pages train / odd pages scored, then the other way), so every training-book page is scored by
a model that never saw it. NORD-10 (252 pages, hand) and SCSI (24 pages, CAD) are never trained on:
their pieces are cut the same way (render_norm, confidence-only turn, pageio.pieces_of, 300 pieces max).
Writes style/pagestyle.json {"book:page": 0/1} and style/pagestyle-score.json.
Usage: python -I pagestyle.py <letters.onnx>"""
import site, sys, os, json
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(__file__))
import numpy as np, cv2, torch, torch.nn as nn, torch.nn.functional as F, onnxruntime as ort
from books import BOOKS
from pageio import render_norm, pieces_of, crops, softmax, ink_of
R = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
torch.manual_seed(0)
z = np.load(R + "/harvest100/pieces.npz"); X, meta = z["X"], z["meta"]
y = (meta[:, 0] == 1).astype(np.int64)                  # book id 1 = nd120
key = np.array([f'{["binder", "nd120", "scsi", "iosys"][b]}:{p}' for b, p in meta[:, :2]])


class Net(nn.Module):
    def __init__(s):
        super().__init__()
        s.c1 = nn.Conv2d(1, 16, 3, padding=1); s.c2 = nn.Conv2d(16, 32, 3, padding=1); s.fc = nn.Linear(32 * 8 * 8, 64); s.o = nn.Linear(64, 2)
    def forward(s, x):
        x = F.max_pool2d(F.relu(s.c1(x)), 2); x = F.max_pool2d(F.relu(s.c2(x)), 2)
        return s.o(F.relu(s.fc(x.flatten(1))))


def fit(idx):
    # same number of pieces from each style
    i0, i1 = idx[y[idx] == 0], idx[y[idx] == 1]; n = min(len(i0), len(i1), 40000)
    rng = np.random.default_rng(0); s = np.r_[rng.choice(i0, n, False), rng.choice(i1, n, False)]
    Xt = torch.tensor(X[s, None].astype(np.float32) / 255); Yt = torch.tensor(y[s])
    torch.manual_seed(0); net = Net(); opt = torch.optim.Adam(net.parameters(), 2e-3)
    for ep in range(3):
        p = torch.randperm(len(Yt))
        for i in range(0, len(Yt), 256):
            b = p[i:i + 256]; loss = F.cross_entropy(net(Xt[b]), Yt[b]); opt.zero_grad(); loss.backward(); opt.step()
    return net.eval()


def prob(net, A):
    with torch.no_grad():
        return np.concatenate([F.softmax(net(torch.tensor(A[i:i + 4096])), 1)[:, 1].numpy() for i in range(0, len(A), 4096)])


out, sc = {}, {"two_fold": {}, "unseen": {}}
par = meta[:, 1] % 2
for f in (0, 1):
    net = fit(np.nonzero(par != f)[0]); te = np.nonzero(par == f)[0]
    pr = prob(net, X[te, None].astype(np.float32) / 255)
    for k in np.unique(key[te]):
        out[k] = int(pr[key[te] == k].mean() >= 0.5)
net = fit(np.arange(len(y)))
torch.onnx.export(net, torch.zeros(1, 1, 32, 32), R + "/model/pagestyle.onnx", input_names=["x"], output_names=["logits"],
                  dynamic_axes={"x": {0: "n"}, "logits": {0: "n"}}, opset_version=17, dynamo=False)
lett = ort.InferenceSession(sys.argv[1], providers=["CPUExecutionProvider"])
ROT = {0: None, 90: cv2.ROTATE_90_CLOCKWISE, 180: cv2.ROTATE_180, 270: cv2.ROTATE_90_COUNTERCLOCKWISE}
rng = np.random.default_rng(0)
unseen = [("scsi", BOOKS["scsi"][0], p) for p in BOOKS["scsi"][1]] + \
         [("nord10", os.path.join(os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..")), "docs/sources/nord-10/NORD-10-drawings.pdf"), p) for p in range(1, 253)]
for b, pdf, p in unseen:
    g, _ = render_norm(pdf, p)
    ink = ink_of(g); nl = 0
    for kk in ((60, 1), (1, 60)):                    # drawing page = 20+ long lines (harvest100 rule)
        nl += cv2.connectedComponents(cv2.morphologyEx(ink, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_RECT, kk)), connectivity=8)[0] - 1
    if nl < 20: continue
    best = None
    for r, code in ROT.items():                       # confidence-only turn
        gr = g if code is None else cv2.rotate(g, code); pcs = pieces_of(gr)
        if len(pcs) > 300: pcs = [pcs[i] for i in rng.choice(len(pcs), 300, replace=False)]
        if not pcs: continue
        c = float(softmax(lett.run(None, {"x": crops(gr, pcs)})[0]).max(1).mean())
        if best is None or c > best[0]: best = (c, gr, pcs)
    if best is None: continue
    pr = prob(net, crops(best[1], best[2]))
    out[f"{b}:{p}"] = int(pr.mean() >= 0.5)
    print(b, p, round(float(pr.mean()), 3), flush=True)
truth = lambda k: 1 if k.split(":")[0] in ("nd120", "scsi") else 0
for k, v in out.items():
    bk = k.split(":")[0]; d = sc["two_fold" if bk in ("binder", "iosys", "nd120") else "unseen"].setdefault(bk, [0, 0, []])
    d[1] += 1; d[0] += int(v == truth(k))
    if v != truth(k): d[2].append(k)
json.dump(out, open(R + "/style/pagestyle.json", "w")); json.dump(sc, open(R + "/style/pagestyle-score.json", "w"), indent=1)
print(json.dumps(sc))
