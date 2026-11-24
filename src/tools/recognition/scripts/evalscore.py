"""Step 6.2: per-class score of each finder from the eye judgement of evaltiles.py objects.
judge.json: {"wrong": [object ids that are not a real object of their class],
             "missed": {"chip": n, ...}  real objects that no finder marked, counted by eye}
truth = objects judged right + missed. found = right objects a finder marked; extra = its wrong ones.
Usage: python -I evalscore.py <objects.json> <judge.json>
"""
import sys, json
O = json.load(open(sys.argv[1])); J = json.load(open(sys.argv[2]))
wrong = set(J["wrong"])
res = {}
SRC = sorted({x for o in O for x in o["src"]})
for c in sorted({o["cls"] for o in O}):
    oc = [o for o in O if o["cls"] == c]
    truth = sum(o["id"] not in wrong for o in oc) + J["missed"].get(c, 0)
    r = {"truth": truth}
    for s in SRC:
        f = sum(s in o["src"] and o["id"] not in wrong for o in oc)
        e = sum(s in o["src"] and o["id"] in wrong for o in oc)
        r[s] = {"found": f, "extra": e}
    res[c] = r
print(json.dumps(res))
