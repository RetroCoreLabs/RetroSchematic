"""Step 2.4: retrain the letter network on synthetic pages (gen2.py) + named real cut-outs from
other NORD-10 pages. Page 64 is never used for training or for choosing the model: the epoch kept
is the one with the best score on a HELD-OUT set = named real pieces from every 10th page
(page index % 10 == 5), never trained on. Page 64's 148 hand-labelled letters are scored once,
on the chosen model, at the end.
Same network and 48->32 window as train.py.
Usage: python -I train2.py <synthdir> <harvestdir> <realdir> <page64.png> <outdir> [per_class_cap]
"""
import site, sys, os, json, glob, time, random
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np, cv2, torch, torch.nn.functional as F

synth, hd, realdir, page, outdir = sys.argv[1:6]
CAP = int(sys.argv[6]) if len(sys.argv) > 6 else 3000
sys.argv = sys.argv[:1] + [synth, realdir, page, outdir]   # train.py reads argv at import
# reuse crop(), load_synth(), Net and CLASSES from train.py without running its training:
src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "train.py")).read()
src = src.split("t0 = time.time()")[0]
ns = {"__file__": "train.py"}; exec(compile(src, "train.py", "exec"), ns)
CLASSES, CI, crop, load_synth, Net = ns["CLASSES"], ns["CI"], ns["crop"], ns["load_synth"], ns["Net"]
torch.manual_seed(0); np.random.seed(0); random.seed(0)

t0 = time.time()
files = sorted(glob.glob(os.path.join(synth, "page*.png")))
Xs, Ys = load_synth(files, 2)
print("synthetic", len(Ys))

z = np.load(hd + "/pieces.npz"); X, meta = z["X"], z["meta"]
a = np.load(hd + "/clusters.npz")["assign"]
nm = {int(l.split()[0]): l.split()[1] for l in open(hd + "/names.txt") if l[0] != "#" and l.strip()}
lab = np.array([CI[nm[int(g)]] if int(g) in nm else -1 for g in a])
held = meta[:, 0] % 10 == 5
rng = np.random.default_rng(0)
tr_idx, va_idx = [], []
for c in range(len(CLASSES)):
    i_tr = np.nonzero((lab == c) & ~held)[0]; i_va = np.nonzero((lab == c) & held)[0]
    tr_idx += list(rng.choice(i_tr, min(CAP, len(i_tr)), replace=False))
    va_idx += list(rng.choice(i_va, min(300, len(i_va)), replace=False))
tr_idx, va_idx = np.array(tr_idx), np.array(va_idx)
Xr = X[tr_idx, None].astype(np.float32) / 255.0; Yr = lab[tr_idx]
Xv = X[va_idx, None].astype(np.float32) / 255.0; Yv = lab[va_idx]
print("real named train", len(Yr), "held-out (other pages)", len(Yv))

g = cv2.imread(page, cv2.IMREAD_GRAYSCALE)
comps = json.load(open(os.path.join(realdir, "comps.json")))
Xt64, Yt64 = [], []
for line in open(os.path.join(realdir, "labels.txt")):
    if line.startswith("#") or not line.strip(): continue
    k, l = line.split(); x, y, w, h = comps[int(k)]
    Xt64.append(crop(g, x + w / 2, y + h / 2)); Yt64.append(CI[l])
Xt64, Yt64 = np.array(Xt64)[:, None], np.array(Yt64)

Xall = torch.tensor(np.concatenate([Xs, Xr])); Yall = torch.tensor(np.concatenate([Ys, Yr]))
net = Net(); opt = torch.optim.Adam(net.parameters(), 2e-3)


def pred(X):
    net.eval()
    with torch.no_grad():
        p = torch.cat([net(torch.tensor(X[i:i + 4096])).argmax(1) for i in range(0, len(X), 4096)]).numpy()
    net.train(); return p


EP = 10
sched = torch.optim.lr_scheduler.OneCycleLR(opt, 3e-3, total_steps=EP * ((len(Yall) + 127) // 128))
best, best_state, log = -1, None, []
for ep in range(EP):
    perm = torch.randperm(len(Yall))
    for i in range(0, len(Yall), 128):
        idx = perm[i:i + 128]; xb = Xall[idx]
        xb = (xb * torch.empty(len(idx), 1, 1, 1).uniform_(0.7, 1.2) + torch.randn_like(xb) * 0.05).clamp(0, 1)
        loss = F.cross_entropy(net(xb), Yall[idx]); opt.zero_grad(); loss.backward(); opt.step(); sched.step()
    hv = float((pred(Xv) == Yv).mean())
    log.append({"epoch": ep, "loss": round(loss.item(), 3), "heldout": round(hv, 4)}); print(log[-1], flush=True)
    if hv > best:
        best = hv; best_state = {k: v.clone() for k, v in net.state_dict().items()}; best_ep = ep
net.load_state_dict(best_state); net.eval()
p64 = pred(Xt64)
os.makedirs(outdir, exist_ok=True)
onnx_path = os.path.join(outdir, "nd-letters-2.onnx")
torch.onnx.export(net, torch.zeros(1, 1, 32, 32), onnx_path, input_names=["x"], output_names=["logits"],
                  dynamic_axes={"x": {0: "n"}, "logits": {0: "n"}}, opset_version=17, dynamo=False)
import onnxruntime as ort
po = ort.InferenceSession(onnx_path, providers=["CPUExecutionProvider"]).run(None, {"x": Xt64.astype(np.float32)})[0].argmax(1)
per = {}
for t, q in zip(Yt64, po):
    c = CLASSES[t]; per.setdefault(c, [0, 0]); per[c][1] += 1; per[c][0] += int(t == q)
res = {"synthetic": int(len(Ys)), "real_named_train": int(len(Yr)), "heldout": int(len(Yv)),
       "chosen_epoch": best_ep, "heldout_acc": round(best, 4), "epochs": log,
       "page64_acc_torch": round(float((p64 == Yt64).mean()), 4), "page64_acc_onnx": round(float((po == Yt64).mean()), 4),
       "page64_right": int((po == Yt64).sum()), "page64_n": int(len(Yt64)),
       "wrong_true_pred": [(CLASSES[t], CLASSES[q]) for t, q in zip(Yt64, po) if t != q],
       "per_class_right_of": per, "seconds": round(time.time() - t0)}
json.dump(res, open(os.path.join(outdir, "result-2.json"), "w"), indent=1)
print(json.dumps({k: v for k, v in res.items() if k not in ("per_class_right_of", "epochs")}))
