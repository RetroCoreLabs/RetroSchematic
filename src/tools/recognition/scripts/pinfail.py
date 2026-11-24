"""How pin numbers fail. For every draft wire end on a chip/gate whose code is a key part:
  not read      - number '?'
  no such pin   - number is not a key pin of that part
  right         - the other ends on the same wire group belong to the key net of this pin
  wrong end     - the wire group belongs to the key net of ANOTHER pin of the same part (number given to the wrong end / swapped)
  wrong number  - the group belongs to some other key net, not one of this part's pins
  no evidence   - no other end in the group says which key net it is
usage: pinfail.py <key> <dump.json> [<key> <dump> ...]  -> one JSON line (summed)"""
import sys, json
from collections import Counter, defaultdict
T = Counter()
a = sys.argv[1:]
for kf, uf in zip(a[0::2], a[1::2]):
    K = json.load(open(kf, encoding="utf-8")); U = json.load(open(uf))
    kpin = {(p if isinstance(p, str) else p["at"]): i for i, n in enumerate(K["nets"]) for p in n["pins"]}
    parts = {p["ref"] for p in K["parts"]} | {g["part"] for g in K["gates"]}
    phys = dict((x, y) for x, y in U.get("phys", []))
    grp = defaultdict(list)
    for i, e in U["ends"]:
        if e[0] in ("pin", "conn"): grp[phys.get(i)].append((i, e[1]))
    for i, e in U["ends"]:
        if e[0] != "pin" or (len(e) > 4 and e[4] == "passive"): continue
        r, pn = e[1].rsplit(".", 1)
        if r not in parts: continue
        if pn == "?": T["not read"] += 1; continue
        if e[1] not in kpin: T["no such pin"] += 1; continue
        g = phys.get(i)
        ev = Counter(kpin[at] for j, at in grp.get(g, []) if j != i and at in kpin and at != e[1]) if g is not None else Counter()
        if not ev: T["no evidence"] += 1; continue
        net = ev.most_common(1)[0][0]
        if net == kpin[e[1]]: T["right"] += 1; continue
        mine = {kpin[at] for at in kpin if at.rsplit(".", 1)[0] == r}
        T["wrong end" if net in mine else "wrong number"] += 1
print(json.dumps(dict(T)))
