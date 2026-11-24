"""Code reading split per key symbol. usage: codesplit.py <key> <dump.json> [verbose]  -> JSON line
For every key chip/gate with a box that a draft symbol overlaps (30 % of the smaller box):
  right        - an overlapping draft symbol carries the key code
  wrong-symbol - a code box near the symbol (inside, or up to 60 px above/below) was read as the key code,
                 but the symbol did not get it
  misread      - a code box is near, none read as the key code
  no box       - no code box near the symbol at all
  elsewhere    - (sub-count of 'no box') the key code was read by some code box elsewhere on the page"""
import sys, json
from collections import Counter
K = json.load(open(sys.argv[1], encoding="utf-8")); U = json.load(open(sys.argv[2])); V = len(sys.argv) > 3
def ov(a, b):
    ix = min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0]); iy = min(a[1] + a[3], b[1] + b[3]) - max(a[1], b[1])
    return 0.0 if ix <= 0 or iy <= 0 else ix * iy / max(1, min(a[2] * a[3], b[2] * b[3]))
syms = U["chips"] + U["gates"]
cb = [d["box"] for d in U["dets"] if d["cls"] == "poscode"]
reads = {}
for c in U.get("detcodes", []): reads.setdefault(tuple(c["box"]), set()).add(c["code"])
allcodes = {c["code"] for c in U.get("detcodes", [])}
def near(b, sb):
    x, y, w, h = sb; cx, cy = b[0] + b[2] / 2, b[1] + b[3] / 2
    return x - 25 <= cx <= x + w + 25 and y - 60 <= cy <= y + h + 60
st = Counter(); ex = []
items = [(q["ref"], q["box"], "chip") for q in K["parts"] if q.get("kind") == "chip" and q.get("box")]
items += [(g["part"], g["box"], "gate") for g in K["gates"] if g.get("box")]
for ref, box, kd in items:
    hit = [s for s in syms if ov(s["box"], box) >= 0.3]
    if not hit: st["part not found"] += 1; continue
    st["symbols"] += 1
    if any((s.get("ref") or "").upper() == ref.upper() for s in hit): st["right"] += 1; continue
    nb = [b for b in cb if near(b, box)]
    rd = set().union(*[reads.get(tuple(b), set()) for b in nb]) if nb else set()
    if ref in rd: k = "wrong-symbol"
    elif nb: k = "misread"
    else: k = "no box"; st["no box, read elsewhere"] += ref in allcodes
    st[k] += 1; st[kd + " " + k] += 1
    ex.append((ref, kd, box, k, sorted(str(x) for x in rd), [s.get("ref") for s in hit]))
if V:
    for e in ex: print(e)
print(json.dumps(dict(st)))
