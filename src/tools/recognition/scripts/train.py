"""Step D: tiny CNN letter classifier trained ONLY on synthetic pages, exported to ONNX,
scored on hand-labelled real cut-outs from 2A50 (page 64).

Input: a 48x48 pixel window of the 300 dpi page centred on the letter, scaled to 32x32,
ink = 1.0. Absolute size is kept (no per-letter scaling), so a subscript stays small.
Usage: python -I train.py <synthdir> <realdir> <page64.png> <outdir>
"""
import site, sys, os, json, glob, time, random
sys.path.append(site.getusersitepackages())
import numpy as np, cv2, torch, torch.nn as nn, torch.nn.functional as F

CLASSES = list("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ") + ["sub0", "sub1"]
CI = {c: i for i, c in enumerate(CLASSES)}
WIN, SZ = 48, 32
synth, realdir, page, outdir = sys.argv[1:5]
torch.manual_seed(0); np.random.seed(0); random.seed(0)
torch.set_num_threads(max(1, os.cpu_count() // 2))


def crop(img, cx, cy):
    h = WIN // 2
    p = cv2.copyMakeBorder(img, h, h, h, h, cv2.BORDER_CONSTANT, value=255)
    cx = min(max(int(cx), 0), img.shape[1] - 1); cy = min(max(int(cy), 0), img.shape[0] - 1)
    c = p[cy:cy + WIN, cx:cx + WIN]
    c = cv2.resize(c, (SZ, SZ), interpolation=cv2.INTER_AREA)
    return (255 - c).astype(np.float32) / 255.0


def load_synth(files, jitter):
    X, Y = [], []
    for f in files:
        img = cv2.imread(f, cv2.IMREAD_GRAYSCALE)
        for l in json.load(open(f[:-4] + ".json")):
            if l["cls"] not in CI:
                continue
            x, y, w, h = l["box"]
            for _ in range(jitter):
                X.append(crop(img, x + w / 2 + random.uniform(-2, 2), y + h / 2 + random.uniform(-2, 2)))
                Y.append(CI[l["cls"]])
    return np.array(X)[:, None], np.array(Y)


class Net(nn.Module):
    def __init__(self, n=len(CLASSES)):
        super().__init__()
        self.c1 = nn.Conv2d(1, 16, 3, padding=1); self.b1 = nn.BatchNorm2d(16)
        self.c2 = nn.Conv2d(16, 32, 3, padding=1); self.b2 = nn.BatchNorm2d(32)
        self.c3 = nn.Conv2d(32, 64, 3, padding=1); self.b3 = nn.BatchNorm2d(64)
        self.fc = nn.Linear(64 * 4 * 4, 128); self.out = nn.Linear(128, n); self.dp = nn.Dropout(0.3)

    def forward(self, x):
        x = F.max_pool2d(F.relu(self.b1(self.c1(x))), 2)
        x = F.max_pool2d(F.relu(self.b2(self.c2(x))), 2)
        x = F.max_pool2d(F.relu(self.b3(self.c3(x))), 2)
        return self.out(self.dp(F.relu(self.fc(x.flatten(1)))))


t0 = time.time()
files = sorted(glob.glob(os.path.join(synth, "page*.png")))
Xtr, Ytr = load_synth(files[:180], 2)
Xva, Yva = load_synth(files[180:], 1)
print("synthetic train", len(Ytr), "val", len(Yva), "load s", round(time.time() - t0, 1))

# real test set
g = cv2.imread(page, cv2.IMREAD_GRAYSCALE)
comps = json.load(open(os.path.join(realdir, "comps.json")))
Xre, Yre = [], []
for line in open(os.path.join(realdir, "labels.txt")):
    if line.startswith("#") or not line.strip():
        continue
    k, lab = line.split()
    x, y, w, h = comps[int(k)]
    Xre.append(crop(g, x + w / 2, y + h / 2)); Yre.append(CI[lab])
Xre, Yre = np.array(Xre)[:, None], np.array(Yre)
print("real labelled", len(Yre))

net = Net()
print("parameters", sum(p.numel() for p in net.parameters()))
opt = torch.optim.Adam(net.parameters(), 2e-3)
Xt, Yt = torch.tensor(Xtr), torch.tensor(Ytr)


def acc(X, Y):
    net.eval()
    with torch.no_grad():
        p = net(torch.tensor(X)).argmax(1).numpy()
    net.train()
    return (p == Y).mean(), p


t1 = time.time()
EP = 8
sched = torch.optim.lr_scheduler.OneCycleLR(opt, 3e-3, total_steps=EP * ((len(Yt) + 127) // 128))
for ep in range(EP):
    perm = torch.randperm(len(Yt))
    for i in range(0, len(Yt), 128):
        idx = perm[i:i + 128]
        xb = Xt[idx]
        # augmentation: random ink strength and small noise, like differing scans
        xb = (xb * torch.empty(len(idx), 1, 1, 1).uniform_(0.7, 1.2) + torch.randn_like(xb) * 0.05).clamp(0, 1)
        loss = F.cross_entropy(net(xb), Yt[idx])
        opt.zero_grad(); loss.backward(); opt.step(); sched.step()
    va, _ = acc(Xva, Yva); ra, _ = acc(Xre, Yre)
    print(f"epoch {ep} loss {loss.item():.3f} synthetic-val {va:.3f} real {ra:.3f}")
train_s = time.time() - t1
va, _ = acc(Xva, Yva); ra, pr = acc(Xre, Yre)

os.makedirs(outdir, exist_ok=True)
net.eval()
onnx_path = os.path.join(outdir, "nd-letters.onnx")
torch.onnx.export(net, torch.zeros(1, 1, SZ, SZ), onnx_path, input_names=["x"], output_names=["logits"],
                  dynamic_axes={"x": {0: "n"}, "logits": {0: "n"}}, opset_version=17, dynamo=False)
import onnxruntime as ort
s = ort.InferenceSession(onnx_path, providers=["CPUExecutionProvider"])
t2 = time.time()
po = s.run(None, {"x": Xre})[0].argmax(1)
ort_ms = (time.time() - t2) * 1000
onnx_acc = (po == Yre).mean()

wrong = [(CLASSES[a], CLASSES[b]) for a, b in zip(Yre, po) if a != b]
per = {}
for a, b in zip(Yre, po):
    c = CLASSES[a]; per.setdefault(c, [0, 0]); per[c][1] += 1; per[c][0] += int(a == b)
res = {"synthetic_train": int(len(Ytr)), "synthetic_val": int(len(Yva)), "real": int(len(Yre)),
       "synthetic_val_acc": round(float(va), 4), "real_acc_torch": round(float(ra), 4),
       "real_acc_onnx": round(float(onnx_acc), 4), "train_seconds": round(train_s, 1),
       "onnx_bytes": os.path.getsize(onnx_path), "onnx_ms_for_all_real": round(ort_ms, 1),
       "wrong_true_pred": wrong, "per_class_right_of": per}
json.dump(res, open(os.path.join(outdir, "result.json"), "w"), indent=1)
print(json.dumps({k: v for k, v in res.items() if k != "per_class_right_of"}, indent=1))
