"""Where each answer-key pin is lost, by stage. usage: stageloss.py <key> <dump.json> <draft.rsc>  -> one JSON line
Stages for a key pin that the draft does not put in the right net:
  part not found     - no draft symbol overlaps the key box of the chip/gate (30 % of the smaller box)
  code wrong         - a draft symbol is there but its code is not the key's
  pin number wrong   - code right; a wire end reached the symbol but its pin number is '?' or not a key pin of it
  wire not traced    - code right, no wire end gave this pin
  wire joined wrongly- pin is in the draft, but its own wire group (before label joins) holds pins of 2+ key nets
  label merge        - pin is in the draft, its wire group is clean, the net went wrong through a label name join
Connector (P.n) and passive pins are counted on their own rows. Keys without boxes (page 64): the first two
stages are one row 'part or code' (code read on the page by the code detector -> part not found)."""
import sys, json, re
from collections import Counter, defaultdict
K = json.load(open(sys.argv[1], encoding="utf-8")); U = json.load(open(sys.argv[2])); D = json.load(open(sys.argv[3], encoding="utf-8"))
def ov(a, b):
    ix = min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0]); iy = min(a[1] + a[3], b[1] + b[3]) - max(a[1], b[1])
    return 0.0 if ix <= 0 or iy <= 0 else ix * iy / max(1, min(a[2] * a[3], b[2] * b[3]))
parts = {p["ref"]: p for p in K["parts"]}
kpin = {p["at"]: i for i, n in enumerate(K["nets"]) for p in n["pins"]}
dpin = {p["at"]: i for i, n in enumerate(D["nets"]) for p in n["pins"]}
dmap = {}
for i, n in enumerate(D["nets"]):
    c = Counter(kpin[p["at"]] for p in n["pins"] if p["at"] in kpin); dmap[i] = c.most_common(1)[0][0] if c else None
gbox = {}
for g in K["gates"]:
    for x in list(g.get("inputs") or []) + [g.get("output")]:
        if g.get("box"): gbox[f"{g['part']}.{x}"] = g["box"]
kgate = {}
for g in K["gates"]:
    ps = {str(x) for x in list(g.get("inputs") or []) + [g.get("output")]}
    for x in ps: kgate[f"{g['part']}.{x}"] = ps
syms = U["chips"] + U["gates"]
codes = {c["code"] for c in U.get("detcodes", [])} | {w["snap"] for w in U["words"]}
phys = dict((a, b) for a, b in U.get("phys", []))
ends_at = defaultdict(list); grp_of = {}
for i, a in U["ends"]:
    if a[0] in ("pin", "conn"):
        ends_at[a[1].rsplit(".", 1)[0]].append(a[1]); grp_of.setdefault(a[1], phys.get(i))
grp_keynets = defaultdict(set)
for at, gq in grp_of.items():
    if at in kpin and gq is not None: grp_keynets[gq].add(kpin[at])
dupg = defaultdict(set)
for i, a in U["ends"]:
    if a[0] == "pin": dupg[a[1]].add(phys.get(i))
dup = {k: len(v) for k, v in dupg.items()}
st = Counter(); used_bad = Counter()
def passive(r): return parts.get(r, {}).get("kind") in ("passive", "other") or bool(re.match(r"^[RCWL]-", r))
for at in kpin:
    r, pn = at.rsplit(".", 1)
    if at in dpin:
        if kpin[at] == dmap[dpin[at]]: st["right"] += 1; continue
        gq = grp_of.get(at)
        if len({phys.get(i) for i, a in U["ends"] if a[0] in ("pin", "conn") and a[1] == at}) > 1: st["pin number wrong"] += 1; continue
        st["wire joined wrongly" if gq is not None and len(grp_keynets[gq]) > 1 else "label merge"] += 1; continue
    if r == "P": st["connector pin missing"] += 1; continue
    if passive(r): st["passive pin missing"] += 1; continue
    box = gbox.get(at) or parts.get(r, {}).get("box")
    hit = [s for s in syms if box and ov(s["box"], box) >= 0.3]
    if box:
        if not hit: st["part not found"] += 1; continue
        if not any((s.get("ref") or "").upper() == r.upper() for s in hit): st["code wrong"] += 1; continue
    elif not any((s.get("ref") or "").upper() == r.upper() for s in syms):
        st["part not found" if r in codes else "code wrong"] += 1; continue
    elif at in kgate and not any(e.rsplit(".", 1)[1] in kgate[at] for e in ends_at.get(r, [])):
        st["code wrong"] += 1; continue      # no box in the key: no wire end reached this gate under its code
    bad = [e for e in ends_at.get(r, []) if e.endswith(".?") or e not in kpin or dup.get(e, 0) > 1]
    if used_bad[r] < len(bad): used_bad[r] += 1; st["pin number wrong"] += 1
    else: st["wire not traced"] += 1
st["key pins"] = len(kpin)
print(json.dumps(dict(st)))
