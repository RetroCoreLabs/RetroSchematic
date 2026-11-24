# keycover.py <key>: signal pins of chips (not power) and of drawn gates that are in no net
import sys, json, os
sys.path.insert(0, os.path.dirname(__file__)); from keyrules import chip
K = json.load(open(sys.argv[1], encoding="utf-8")); inn = {p["at"] for n in K["nets"] for p in n["pins"]}
want = set()
for pr in K["parts"]:
    if pr.get("kind") != "chip": continue
    c = chip(pr.get("type"))
    if not c: continue
    for x in c["pins"]:
        if x.get("role") not in ("power", "ground", "nc"): want.add(f"{pr['ref']}.{x['pin']}")
for g in K["gates"]:
    for x in list(g.get("inputs") or []) + [g.get("output")]: want.add(f"{g['part']}.{x}")
miss = sorted(want - inn)
print(json.dumps({"key": os.path.basename(sys.argv[1]), "signal_pins": len(want), "in_nets": len(want & inn), "missing": len(miss)}), " ".join(miss[:60]) if "--list" in sys.argv else "")
