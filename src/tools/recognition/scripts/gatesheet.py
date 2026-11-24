"""Contact sheets of gate candidates (pageio.gatecands) on chosen SCSI pages, for labelling by eye.
Each cell = 120x120 px crop (2.2 x the candidate's long side, so the bubble and pin stubs show),
candidate's box drawn in grey, index number above it. Writes <out>/p<page>-cands.json and sheets.
Usage: python -I gatesheet.py <out> <page> [page ...]"""
import site, sys, os, json
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(__file__))
import numpy as np, cv2
from books import BOOKS
from pageio import render_norm, ink_of, gatecands
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
C, S, PER = 10, 120, 60
for p in map(int, sys.argv[2:]):
    g, dpi = render_norm(BOOKS["scsi"][0], p)
    cs = gatecands(ink_of(g))
    cs.sort(key=lambda b: (b[1] // 150, b[0]))
    json.dump({"page": p, "dpi": dpi, "cands": cs}, open(f"{out}/p{p}-cands.json", "w"))
    pad = cv2.copyMakeBorder(cv2.cvtColor(g, cv2.COLOR_GRAY2BGR), 300, 300, 300, 300, cv2.BORDER_CONSTANT, value=(255, 255, 255))
    for s0 in range(0, len(cs), PER):
        sheet = np.full((((min(PER, len(cs) - s0) + C - 1) // C) * (S + 18), C * S, 3), 255, np.uint8)
        for j, (x, y, w, h) in enumerate(cs[s0:s0 + PER]):
            k = int(max(w, h) * 1.1) + 10
            cx, cy = x + w // 2 + 300, y + h // 2 + 300
            c = pad[cy - k:cy + k, cx - k:cx + k].copy()
            cv2.rectangle(c, (k - w // 2, k - h // 2), (k - w // 2 + w, k - h // 2 + h), (0, 0, 255), 1)
            c = cv2.resize(c, (S, S), interpolation=cv2.INTER_AREA)
            r, q = divmod(j, C)
            sheet[r * (S + 18) + 18:r * (S + 18) + 18 + S, q * S:(q + 1) * S] = c
            cv2.putText(sheet, str(s0 + j), (q * S + 2, r * (S + 18) + 14), 0, 0.5, (200, 0, 0), 1)
        cv2.imwrite(f"{out}/p{p}-s{s0 // PER}.png", sheet)
    print(p, len(cs))
