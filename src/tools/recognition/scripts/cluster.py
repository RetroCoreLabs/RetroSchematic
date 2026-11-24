"""Step 2.2: group look-alike pieces. Input = the piece alone (M). Features = the 128-number inner layer of the step-1 letter
network (model/nd-letters.onnx, layer after fc + ReLU), length-normalised; k-means (cosine) in torch.
Writes harvest/clusters.npz (assign, centres) and harvest/sheets/sheetNN.png: one row per group,
16 random members, row label = group number, size, and the step-1 net's most common answer.
Usage: python -I cluster.py <harvestdir> <onnx> <K>
"""
import site, sys, os, json
sys.path.append(site.getusersitepackages())
import numpy as np, cv2, torch, onnx, onnxruntime as ort

hd, mpath, K = sys.argv[1], sys.argv[2], int(sys.argv[3])
CLASSES = list("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ") + ["sub0", "sub1", "x"]   # x = not one letter (c3 model)
z = np.load(hd + "/pieces.npz"); X = z["M"]   # the piece alone, neighbours blanked
m = onnx.load(mpath)
m.graph.output.append(onnx.helper.make_tensor_value_info("/Relu_3_output_0", onnx.TensorProto.FLOAT, None))
s = ort.InferenceSession(m.SerializeToString(), providers=["CPUExecutionProvider"])
F, P = [], []
for i in range(0, len(X), 4096):
    lo, f = s.run(None, {"x": X[i:i + 4096, None].astype(np.float32) / 255.0})
    F.append(f); P.append(lo.argmax(1))
F = np.concatenate(F); P = np.concatenate(P)
F = F / (np.linalg.norm(F, axis=1, keepdims=True) + 1e-6)
Ft = torch.tensor(F)
g = torch.Generator().manual_seed(0)
C = Ft[torch.randperm(len(Ft), generator=g)[:K]].clone()
for it in range(25):
    a = torch.cat([(Ft[i:i + 65536] @ C.T).argmax(1) for i in range(0, len(Ft), 65536)])
    for k in range(K):
        mk = a == k
        if mk.any():
            c = Ft[mk].mean(0); C[k] = c / (c.norm() + 1e-6)
        else:
            C[k] = Ft[torch.randint(len(Ft), (1,), generator=g)]
a = a.numpy()
np.savez_compressed(hd + "/clusters.npz", assign=a, centres=C.numpy(), net=P)
os.makedirs(hd + "/sheets", exist_ok=True)
rng = np.random.default_rng(0)
T, ROWS = 48, 20
info = []
for sh in range(0, K, ROWS):
    img = np.full((ROWS * (T + 4), 150 + 16 * (T + 2)), 255, np.uint8)
    for r, k in enumerate(range(sh, min(K, sh + ROWS))):
        idx = np.nonzero(a == k)[0]
        top = np.bincount(P[idx], minlength=len(CLASSES)).argmax() if len(idx) else 0
        info.append({"group": k, "size": int(len(idx)), "net_top": CLASSES[top]})
        cv2.putText(img, f"{k} n{len(idx)} {CLASSES[top]}", (2, r * (T + 4) + 30), 0, 0.5, 0, 1)
        for j, q in enumerate(rng.choice(idx, min(16, len(idx)), replace=False) if len(idx) else []):
            t = cv2.resize(255 - X[q], (T, T), interpolation=cv2.INTER_NEAREST)
            img[r * (T + 4):r * (T + 4) + T, 150 + j * (T + 2):150 + j * (T + 2) + T] = t
    cv2.imwrite(hd + f"/sheets/sheet{sh // ROWS:02d}.png", img)
json.dump(info, open(hd + "/groups.json", "w"), indent=0)
print("groups", K, "sizes min/median/max", min(i["size"] for i in info), int(np.median([i["size"] for i in info])), max(i["size"] for i in info))
