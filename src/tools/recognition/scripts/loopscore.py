"""Loop: score a draft against an answer key by symbol boxes (and pins, if the key has nets).
Key chips = parts of kind chip; key gates = gates (each with its own box). A draft box matches a key
symbol when their overlap is over 30 % of the smaller box (greedy, best first). Passives and
connectors in the key are used to name extras ("draft symbol that is really a resistor/arrow").
Usage: python -I loopscore.py <key.json> <dump.json> <draft.rsc> <page.png> <outdir>   -> prints one JSON line"""
import site, sys, json, re, os, subprocess
sys.path.append(site.getusersitepackages())
K = json.load(open(sys.argv[1], encoding="utf-8")); U = json.load(open(sys.argv[2]))
norm = lambda t: re.sub(r"^(DM|SN|MC|AM)", "", re.sub(r"^74(LS|S|F|ALS|AS|HC|H|L)?", "74", (t or "").replace(" ", "").upper()))
def ov(a, b):
    ix = min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0]); iy = min(a[1] + a[3], b[1] + b[3]) - max(a[1], b[1])
    if ix <= 0 or iy <= 0: return 0.0
    return ix * iy / min(a[2] * a[3], b[2] * b[3])
kp = [p for p in K["parts"] if p.get("box") and p.get("kind") in ("chip",)]
kg = [g for g in K["gates"] if g.get("box")]
ko = [p for p in K["parts"] if p.get("box") and p.get("kind") in ("passive", "connector", "other")]
D = [("chip", c) for c in U["chips"]] + [("gate", c) for c in U["gates"]]
pairs = sorted(((ov(d[1]["box"], k[1]["box"]), i, j) for i, d in enumerate(D) for j, k in enumerate([("chip", p) for p in kp] + [("gate", g) for g in kg])), reverse=True)
KS = [("chip", p) for p in kp] + [("gate", g) for g in kg]
ud, uk, match = set(), set(), []
for s, i, j in pairs:
    if s < 0.3: break
    if i in ud or j in uk: continue
    ud.add(i); uk.add(j); match.append((i, j))
r = {"key_chips": len(kp), "key_gates": len(kg), "draft_symbols": len(D)}
cf = [(D[i], KS[j]) for i, j in match]
r["chips_found"] = sum(k[0] == "chip" for d, k in cf)
r["gates_found"] = sum(k[0] == "gate" for d, k in cf)
coded = lambda k: k[1].get("ref") if k[0] == "chip" else k[1].get("part")
r["key_with_code"] = sum(1 for k in KS if coded(k) and "?" not in coded(k))
r["codes_right"] = sum(1 for d, k in cf if coded(k) and d[1]["ref"] and d[1]["ref"].upper() == coded(k).upper())
r["chip_types_right"] = sum(1 for d, k in cf if k[0] == "chip" and d[0] == "chip" and d[1].get("type") and norm(d[1]["type"]) == norm(k[1].get("type")))
r["gate_kinds_right"] = sum(1 for d, k in cf if k[0] == "gate" and d[0] == "gate" and d[1]["kind"] == k[1]["kind"])
ext = [i for i in range(len(D)) if i not in ud]
r["extras"] = len(ext)
r["extras_on_passive_or_arrow"] = sum(1 for i in ext if any(ov(D[i][1]["box"], o["box"]) > 0.3 for o in ko))
from collections import Counter
r["kind_confusion"] = Counter(f'{k[1]["kind"]}->{d[1]["kind"]}' for d, k in cf if k[0] == "gate" and d[0] == "gate" and d[1]["kind"] != k[1]["kind"]).most_common(6)
if K.get("nets"):
    od = sys.argv[5]; os.makedirs(od, exist_ok=True)
    kk = dict(K); kk["nets"] = [{"name": n["name"], "active": n.get("active", "none"), "pins": [p if isinstance(p, dict) else {"at": p} for p in n["pins"]]} for n in K["nets"]]
    for g in kk["gates"]: g.setdefault("inputs", []); g.setdefault("output", None)
    for p in kk["parts"]: p.setdefault("type", "?"); p["type"] = p["type"] or "?"
    kf = os.path.join(od, "key-as-rsc.json"); json.dump(kk, open(kf, "w"))
    subprocess.run([sys.executable, "-I", os.path.join(os.path.dirname(__file__), "score.py"), sys.argv[3], sys.argv[2], kf, sys.argv[4],
                    os.path.join(od, "errors.png"), os.path.join(od, "score.json")], stdout=subprocess.DEVNULL, check=True)
    n = json.load(open(os.path.join(od, "score.json")))["nets"]
    r.update({"key_pins": n["hand_pins"], "pins_right": n["pins_in_right_net"], "pins_wrong": n["pins_in_wrong_net"]})
print(json.dumps(r))
