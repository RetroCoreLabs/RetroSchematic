"""Run the exported object detector (detnet.py) with onnxruntime only, and decode its output.
detect(g) -> list of dicts {"cls", "score", "box":[x,y,w,h], "kind"} in page pixels.
The page must be at the normalised scale (letters about 20 px: page 64 at 300 dpi, or
pageio.render_norm)."""
import site, sys, math
sys.path.append(site.getusersitepackages())
import numpy as np, cv2, onnxruntime as ort

CLASSES = ["chip", "gate", "resistor", "capacitor", "switch", "strap", "ground", "supply", "circle",
           "offpage", "dot", "poscode", "pinnum", "label"]
KINDS = ["nand", "nor", "not", "and", "or", "aoi", "buffer", "other"]
S = 4
_sess = {}


def detect(g, onnx_path, thr=0.3):
    if onnx_path not in _sess:
        _sess[onnx_path] = ort.InferenceSession(onnx_path, providers=["CPUExecutionProvider"])
    s = _sess[onnx_path]
    H, W = g.shape
    Hp, Wp = (H + 31) // 32 * 32, (W + 31) // 32 * 32
    x = np.ones((1, 1, Hp, Wp), np.float32); x[0, 0, :H, :W] = g / 255.0
    hm, reg, kind = [o[0] for o in s.run(None, {"page": x})]
    out = []
    for c in range(hm.shape[0]):
        m = hm[c]
        peak = (m == cv2.dilate(m, np.ones((3, 3), np.float32))) & (m >= thr)
        for y, xx in zip(*np.nonzero(peak)):
            w, h = math.exp(reg[0, y, xx]) * S, math.exp(reg[1, y, xx]) * S
            cx, cy = (xx + reg[2, y, xx]) * S, (y + reg[3, y, xx]) * S
            d = {"cls": CLASSES[c], "score": round(float(m[y, xx]), 3),
                 "box": [int(round(cx - w / 2)), int(round(cy - h / 2)), int(round(w)), int(round(h))]}
            if CLASSES[c] == "gate":
                d["kind"] = KINDS[int(np.argmax(kind[:, y, xx]))]
            out.append(d)
    return out
