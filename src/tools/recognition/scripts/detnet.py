"""Step 6.1: one small object detector for both drawing styles (CenterNet type, CPU, ONNX).

Input: grey page at the normalised scale (letters about 20 px), ink dark. Output at 1/4 scale:
  hm   (14 classes, sigmoid)  - centre heat per class (gendet.CLASSES)
  wh   (2)                    - log(box width/4), log(box height/4) at the centre
  off  (2)                    - sub-cell centre offset
  kind (8)                    - gate kind logits (gendet.KINDS), read at gate centres
Usage: python -I detnet.py train <train dir> <val dir> <out.pt> <iters>
       python -I detnet.py export <in.pt> <out.onnx>
"""
import site, sys, os, json, glob, math, random, time
sys.path.append(site.getusersitepackages())
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np, cv2, torch, torch.nn as nn, torch.nn.functional as F

NC, NK, S, CROP = 14, 8, 4, 512
SCALE_LO = float(os.environ.get("SCALELO", "0.8"))
CLASSES = ["chip", "gate", "resistor", "capacitor", "switch", "strap", "ground", "supply", "circle",
           "offpage", "dot", "poscode", "pinnum", "label"]
KINDS = ["nand", "nor", "not", "and", "or", "aoi", "buffer", "other"]


def cbr(i, o, s=1, d=1):
    return nn.Sequential(nn.Conv2d(i, o, 3, s, d, dilation=d, bias=False), nn.BatchNorm2d(o), nn.ReLU(inplace=True))


class Det(nn.Module):
    def __init__(self, c=(24, 48, 64, 96, 128)):
        super().__init__()
        self.s1 = nn.Sequential(cbr(1, c[0], 2), cbr(c[0], c[0]))            # /2
        self.s2 = nn.Sequential(cbr(c[0], c[1], 2), cbr(c[1], c[1]))         # /4
        self.s3 = nn.Sequential(cbr(c[1], c[2], 2), cbr(c[2], c[2]), cbr(c[2], c[2]))   # /8
        self.s4 = nn.Sequential(cbr(c[2], c[3], 2), cbr(c[3], c[3]), cbr(c[3], c[3]))   # /16
        self.s5 = nn.Sequential(cbr(c[3], c[4], 2), cbr(c[4], c[4], d=2), cbr(c[4], c[4], d=4), cbr(c[4], c[4], d=8))  # /32, wide view for chip boxes
        self.u4 = nn.Conv2d(c[4], c[3], 1); self.u3 = nn.Conv2d(c[3], c[2], 1); self.u2 = nn.Conv2d(c[2], c[1], 1)
        self.m4 = cbr(c[3], c[3]); self.m3 = cbr(c[2], c[2]); self.m2 = cbr(c[1], 64)
        self.hm = nn.Sequential(cbr(64, 64), nn.Conv2d(64, NC, 1))
        self.reg = nn.Sequential(cbr(64, 64), nn.Conv2d(64, 4, 1))
        self.kind = nn.Sequential(cbr(64, 64), nn.Conv2d(64, NK, 1))
        self.hm[-1].bias.data.fill_(-4.0)

    def forward(self, x):
        x = 1.0 - x                                  # ink = 1
        a2 = self.s2(self.s1(x)); a3 = self.s3(a2); a4 = self.s4(a3); a5 = self.s5(a4)
        up = lambda t, r: F.interpolate(t, size=r.shape[2:], mode="nearest")
        b4 = self.m4(a4 + up(self.u4(a5), a4))
        b3 = self.m3(a3 + up(self.u3(b4), a3))
        b2 = self.m2(a2 + up(self.u2(b3), a2))
        return torch.sigmoid(self.hm(b2)), self.reg(b2), self.kind(b2)


# ---------------------------------------------------------------- data
def load_dir(d, limit=None):
    out = []
    for f in sorted(glob.glob(os.path.join(d, "*.json")))[:limit]:
        L = json.load(open(f))
        # "labelled": the classes this page's labels are complete for (answer-key pages label only
        # symbols, not every pin number or word); other classes get no loss on that page.
        cm = np.array([c in L.get("labelled", CLASSES) for c in CLASSES], np.float32)
        img = cv2.imread(f[:-5] + ".jpg", cv2.IMREAD_GRAYSCALE)
        if img is None: img = cv2.imread(f[:-5] + ".png", cv2.IMREAD_GRAYSCALE)
        for _ in range(int(L.get("repeat", 1))):
            out.append((img, L["objects"], cm))
    return out


def gauss(hm, cx, cy, sx, sy):
    H, W = hm.shape
    rx, ry = int(3 * sx) + 1, int(3 * sy) + 1
    x0, x1, y0, y1 = max(0, int(cx) - rx), min(W, int(cx) + rx + 1), max(0, int(cy) - ry), min(H, int(cy) + ry + 1)
    if x0 >= x1 or y0 >= y1:
        return
    X, Y = np.meshgrid(np.arange(x0, x1), np.arange(y0, y1))
    g = np.exp(-((X - cx) ** 2 / (2 * sx * sx) + (Y - cy) ** 2 / (2 * sy * sy)))
    hm[y0:y1, x0:x1] = np.maximum(hm[y0:y1, x0:x1], g)


def sample(pages, rng):
    img, objs, cm = pages[rng.randrange(len(pages))]
    f = rng.uniform(SCALE_LO, 1.25)                  # letter size spread of the real pages; low end < 0.8 so a page
                                                     # run at half size shows its 900 px chips at trained sizes
    H, W = img.shape
    cw = int(CROP / f) + 1
    x0, y0 = rng.randint(0, max(0, W - cw)), rng.randint(0, max(0, H - cw))
    c = img[y0:y0 + cw, x0:x0 + cw]
    c = cv2.resize(c, (CROP, CROP), interpolation=cv2.INTER_AREA if f < 1 else cv2.INTER_LINEAR)
    O = CROP // S
    hm = np.zeros((NC, O, O), np.float32); reg = np.zeros((4, O, O), np.float32)
    rm = np.zeros((O, O), np.float32); kd = np.full((O, O), -100, np.int64)
    for o in objs:
        bx, by, bw, bh = o["box"]
        X0, Y0 = max(bx, x0), max(by, y0); X1, Y1 = min(bx + bw, x0 + cw), min(by + bh, y0 + cw)
        if X1 <= X0 or Y1 <= Y0 or (X1 - X0) * (Y1 - Y0) < 0.6 * bw * bh:
            continue                                  # mostly outside the crop: not a target
        w, h = bw * f / S, bh * f / S
        cx, cy = (bx + bw / 2 - x0) * f / S, (by + bh / 2 - y0) * f / S
        k = CLASSES.index(o["cls"])
        ix, iy = int(cx), int(cy)
        gauss(hm[k], ix, iy, max(0.6, w / 6), max(0.6, h / 6))   # peak exactly 1 on the centre cell (the focal loss needs it)
        if 0 <= ix < O and 0 <= iy < O:
            reg[:, iy, ix] = (math.log(max(w, 0.25)), math.log(max(h, 0.25)), cx - ix, cy - iy)
            rm[iy, ix] = 1
            if o["cls"] == "gate":
                kd[iy, ix] = KINDS.index(o["kind"])
    x = c.astype(np.float32)[None] / 255.0
    if rng.random() < 0.3:                            # lighter / darker scan
        x = np.clip(x * rng.uniform(0.9, 1.1) + rng.uniform(-0.05, 0.1), 0, 1)
    return x, hm, reg, rm, kd, cm


def batch(pages, rng, n):
    b = [sample(pages, rng) for _ in range(n)]
    return [torch.from_numpy(np.stack(t)) for t in zip(*b)]


def loss_fn(out, hm, reg, rm, kd, cm=None):
    p, r, k = out
    if cm is not None:                      # classes not labelled on a page: no loss either way
        w = cm[:, :, None, None]
        hm = hm * w + (1 - w) * 0.999       # neither 1 (pos) nor counted as negative
        p = p * w + (1 - w) * 0.5
    p = p.clamp(1e-4, 1 - 1e-4)
    pos = hm.eq(1).float()
    neg = 1 - pos
    lp = -(torch.log(p) * (1 - p) ** 2 * pos).sum()
    ln = -(torch.log(1 - p) * p ** 2 * (1 - hm) ** 4 * neg * (cm[:, :, None, None] if cm is not None else 1)).sum()
    npos = pos.sum().clamp(min=1)
    lh = (lp + ln) / npos
    m = rm.unsqueeze(1)
    lr = (F.l1_loss(r * m, reg * m, reduction="sum")) / rm.sum().clamp(min=1)
    lk = F.cross_entropy(k, kd, ignore_index=-100) if (kd >= 0).any() else torch.zeros(())
    return lh + lr + 0.5 * lk, (lh.item(), lr.item(), lk.item())


# ---------------------------------------------------------------- decode + match (also used by detect.py)
def decode(hm, reg, kind, thr=0.3, scale=1.0):
    """numpy outputs of one image -> list of (cls, score, x, y, w, h, kind) in input pixels / scale."""
    C, H, W = hm.shape
    t = torch.from_numpy(hm)[None]
    mx = F.max_pool2d(t, 3, 1, 1)[0].numpy()
    keep = (hm == mx) & (hm >= thr)
    out = []
    for c, y, x in zip(*np.nonzero(keep)):
        w, h = math.exp(reg[0, y, x]) * S, math.exp(reg[1, y, x]) * S
        cx, cy = (x + reg[2, y, x]) * S, (y + reg[3, y, x]) * S
        kk = KINDS[int(np.argmax(kind[:, y, x]))] if CLASSES[c] == "gate" else None
        out.append((CLASSES[c], float(hm[c, y, x]), (cx - w / 2) / scale, (cy - h / 2) / scale, w / scale, h / scale, kk))
    return out


def iou(a, b):
    ax0, ay0, aw, ah = a; bx0, by0, bw, bh = b
    ix = max(0, min(ax0 + aw, bx0 + bw) - max(ax0, bx0)); iy = max(0, min(ay0 + ah, by0 + bh) - max(ay0, by0))
    i = ix * iy
    return i / max(1e-6, aw * ah + bw * bh - i)


def match(dets, truth, thr=0.5):
    """Greedy by score; per class: found, missed, extra; gate kind right among found gates."""
    res = {}
    for c in CLASSES:
        D = sorted([d for d in dets if d[0] == c], key=lambda d: -d[1])
        T = [t for t in truth if t["cls"] == c]
        used = set(); tp = kr = 0
        for d in D:
            best, bj = thr, -1
            for j, t in enumerate(T):
                if j not in used:
                    v = iou(d[2:6], t["box"])
                    if v >= best:
                        best, bj = v, j
            if bj >= 0:
                used.add(bj); tp += 1
                if c == "gate" and d[6] == T[bj].get("kind"):
                    kr += 1
        res[c] = {"truth": len(T), "found": tp, "extra": len(D) - tp}
        if c == "gate":
            res[c]["kind_right"] = kr
    return res


def run(model, g):
    """Whole page (any size) -> numpy outputs. g uint8 grey."""
    H, W = g.shape
    Hp, Wp = (H + 31) // 32 * 32, (W + 31) // 32 * 32
    x = np.full((1, 1, Hp, Wp), 1.0, np.float32); x[0, 0, :H, :W] = g / 255.0
    with torch.no_grad():
        p, r, k = model(torch.from_numpy(x))
    return p[0].numpy(), r[0].numpy(), k[0].numpy()


def evaluate(model, pages, thr=0.3):
    model.eval()
    tot = {}
    for g, objs, *_ in pages:
        m = match(decode(*run(model, g), thr=thr), objs)
        for c, v in m.items():
            for a, b in v.items():
                tot.setdefault(c, {}).setdefault(a, 0); tot[c][a] += b
    model.train()
    return tot


def f1(tot):
    fs = []
    for c, v in tot.items():
        if v["truth"]:
            p = v["found"] / max(1, v["found"] + v["extra"]); r = v["found"] / v["truth"]
            fs.append(2 * p * r / max(1e-6, p + r))
    return sum(fs) / max(1, len(fs))


if __name__ == "__main__":
    torch.set_num_threads(16)
    if sys.argv[1] == "train":
        tr, va, outp, iters = sys.argv[2], sys.argv[3], sys.argv[4], int(sys.argv[5])
        pages = load_dir(tr); val = load_dir(va)
        if os.environ.get("TRAIN2"):
            pages += load_dir(os.environ["TRAIN2"])
        rng = random.Random(7); torch.manual_seed(7)
        model = Det()
        if os.environ.get("INIT"):
            model.load_state_dict(torch.load(os.environ["INIT"]))
        print("params", sum(p.numel() for p in model.parameters()), flush=True)
        opt = torch.optim.AdamW(model.parameters(), 2e-3, weight_decay=1e-4)
        sched = torch.optim.lr_scheduler.OneCycleLR(opt, 2e-3, total_steps=iters, pct_start=0.05)
        best = -1; t0 = time.time(); hist = []
        for it in range(1, iters + 1):
            x, hm, reg, rm, kd, cm = batch(pages, rng, 8)
            l, parts = loss_fn(model(x), hm, reg, rm, kd, cm)
            opt.zero_grad(); l.backward(); opt.step(); sched.step()
            if it % 50 == 0:
                print(it, round(l.item(), 3), [round(v, 3) for v in parts], round(time.time() - t0), "s", flush=True)
            if it % 1000 == 0 or it == iters:
                tot = evaluate(model, val[:30]); s = f1(tot)
                hist.append((it, s)); print("VAL", it, round(s, 4), json.dumps(tot), flush=True)
                if s > best:
                    best = s; torch.save(model.state_dict(), outp)
        print("best", best, hist)
    elif sys.argv[1] == "export":
        model = Det(); model.load_state_dict(torch.load(sys.argv[2])); model.eval()
        torch.onnx.export(model, torch.zeros(1, 1, 512, 512), sys.argv[3], input_names=["page"], output_names=["hm", "reg", "kind"],
                          dynamic_axes={"page": {2: "h", 3: "w"}, "hm": {2: "h4", 3: "w4"}, "reg": {2: "h4", 3: "w4"}, "kind": {2: "h4", 3: "w4"}}, opset_version=17)
        print("exported", os.path.getsize(sys.argv[3]))
