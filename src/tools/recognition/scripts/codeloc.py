"""Find where each known code (from the answer key) is written on the page, by reading near its symbol.
Candidates: draft words, code-detector boxes and runs of 2-4 read letter pieces on one line, within
R px of the symbol box. A candidate fits when its text is the code with at most one letter wrong
(same length). Each candidate box is used for one code text only once; a code written once over a
column of gates is found once. Usage: codeloc.py <key.json> <dump.json> [R=140] -> JSON {found, codes, boxes}"""
import sys, json
K = json.load(open(sys.argv[1], encoding="utf-8")); U = json.load(open(sys.argv[2])); R = float(sys.argv[3]) if len(sys.argv) > 3 else 140
P = U["pieces"]; C = U.get("pcls") or []
cand = [(w["snap"], w["box"]) for w in U["words"]] + [(d["code"], d["box"]) for d in U.get("detcodes", [])]
idx = sorted(range(len(P)), key=lambda k: (P[k][1], P[k][0]))
for a in range(len(P)):
    run = [a]
    for _ in range(3):
        x, y, w, h = P[run[-1]][:4]
        nx = [k for k in range(len(P)) if k not in run and 0 <= P[k][0] - (x + w) <= 10 and abs(P[k][1] + P[k][3] / 2 - y - h / 2) < 8]
        if not nx: break
        run.append(min(nx, key=lambda k: P[k][0]))
        if len(run) >= 2 and all(k < len(C) and len(C[k]) == 1 for k in run):
            xs = [P[k][0] for k in run]; ys = [P[k][1] for k in run]
            x0, y0 = min(xs), min(ys); x1 = max(P[k][0] + P[k][2] for k in run); y1 = max(P[k][1] + P[k][3] for k in run)
            cand.append(("".join(C[k] for k in run), [x0, y0, x1 - x0, y1 - y0]))
def near(b, s):
    dx = max(s[0] - (b[0] + b[2]), b[0] - (s[0] + s[2]), 0); dy = max(s[1] - (b[1] + b[3]), b[1] - (s[1] + s[3]), 0)
    return (dx * dx + dy * dy) ** 0.5
fit = lambda t, c: bool(t) and len(t) == len(c) and sum(a != b for a, b in zip(t.upper(), c.upper())) <= 1
syms = [(q["ref"], q["box"]) for q in K["parts"] if q.get("kind") == "chip" and q.get("box")] + [(g["part"], g["box"]) for g in K["gates"] if g.get("box")]
codes = {c for c, b in syms if c and "?" not in c}
found = {}
for c, b in syms:
    if not c or "?" in c: continue
    best = None
    for t, cb in cand:
        if fit(t, c):
            d = near(cb, b) - 5 * (t.upper() == c.upper())
            if d < R and (best is None or d < best[0]): best = (d, cb)
    if best: found.setdefault(c, []).append(best[1])
boxes = []
for c, bs in found.items():
    for b in bs:
        if not any(abs(b[0] - o[0]) < 10 and abs(b[1] - o[1]) < 10 for o in boxes): boxes.append(b)
print(json.dumps({"codes": len(codes), "found": len(found), "boxes": boxes}))
