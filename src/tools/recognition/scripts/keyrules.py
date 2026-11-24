# Rule checks on an answer key: pinout, one net per pin, gate pattern, parts count.
# usage: keyrules.py <key.json> [--list]
import sys, json, re, os
CAT = os.path.join(os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..")), "catalog/chips")
def norm(t):
    t = (t or "").upper().replace(" ", "")
    m = re.match(r"^(?:SN|DM|MC|N)?(74|75|80|81|82|85|88)(?:LS|S|H|L|ALS|F|HC|AS)?(\d+)", t)
    if m: return m.group(1) + m.group(2)
    return t
_cat = {}
NOT_SAME = {"74LS51"}   # LS51 has a 3-3/2-2 pinout, the catalog 7451 is 2-2/2-2
def chip(t):
    if (t or "").upper().replace(" ", "") in NOT_SAME: return None
    n = norm(t)
    for cand in (n, (t or "").upper().replace(" ", "")):
        if cand in _cat: return _cat[cand]
        p = os.path.join(CAT, cand + ".json")
        if os.path.exists(p):
            _cat[cand] = json.load(open(p, encoding="utf-8")); return _cat[cand]
    # 74S/74H variants stored with letter
    m = re.match(r"^(74)(S|H|L)(\d+)", (t or "").upper().replace(" ", ""))
    if m:
        p = os.path.join(CAT, "74" + m.group(2) + m.group(3) + ".json")
        if os.path.exists(p): return json.load(open(p, encoding="utf-8"))
    return None
KIND = {"nand": ("NOT", "AND"), "nor": ("NOT", "OR"), "and": ("AND",), "or": ("OR",), "not": ("NOT",), "xor": ("XOR",), "buffer": ()}
def check(K):
    V = []; parts = {p["ref"]: p for p in K["parts"]}
    seen = {}
    for n in K["nets"]:
        for p in n["pins"]:
            a = p["at"]
            if a in seen and seen[a] != n["name"]: V.append(("two-nets", a, seen[a] + " / " + n["name"]))
            seen[a] = n["name"]
            if "." not in a: continue
            ref, pin = a.rsplit(".", 1)
            pr = parts.get(ref)
            if pr is None:
                if ref != "P": V.append(("no-part", a, ""))
                continue
            if pr.get("kind") not in ("chip", "gatepack"): continue
            c = chip(pr.get("type"))
            if c is None: continue
            pins = {str(x["pin"]) for x in c["pins"]}
            if not pins: V.append(("no-pinout-in-catalog", a, pr.get("type"))); continue
            if pin not in pins: V.append(("pin-not-in-pinout", a, pr.get("type") + " has " + str(len(pins)) + " pins"))
    for g in K["gates"]:
        pr = parts.get(g.get("part"))
        if pr is None: V.append(("gate-no-part", g["id"], "")); continue
        if g.get("kind") == "other": continue          # flip-flops etc: no gate pattern
        c = chip(pr.get("type"))
        if c is None: continue
        name2pin = {x["name"]: str(x["pin"]) for x in c["pins"]}
        fn = None
        for f in c.get("functions") or []:
            outs = [o.strip() for o in str(f.get("output")).split(",")]
            if str(g.get("output")) in outs: fn = f
        if fn is None:
            V.append(("gate-output-not-output", g["id"], f"{pr.get('type')} pin {g.get('output')}")); continue
        if "," in str(fn.get("output")) or "=" not in fn["formula"]: continue   # bus buffers: no per-gate pattern
        rhs = fn["formula"].split("=", 1)[1].upper()
        strobe = re.match(r"^\s*NOT\s*\(\s*(\d?G)\s+AND\s*\((.*)\)\s*\)\s*$", rhs)
        if strobe: rhs = "NOT(" + strobe.group(2) + ")"   # 7425-style strobe input: gate drawn without it is still a NOR
        ins = sorted(name2pin[t] for t in re.findall(r"[A-Za-z0-9]+", fn["formula"].split("=", 1)[1]) if t in name2pin)
        gi = sorted(str(x) for x in g.get("inputs") or [])
        if strobe and gi:   # strobe pin may be left undrawn
            sp = name2pin.get(strobe.group(1)) if strobe.group(1) in name2pin else None
            if sp and sp not in gi: ins = [i for i in ins if i != sp]
        if gi and gi != ins: V.append(("gate-inputs", g["id"], f"key {gi} pinout {ins}"))
        neg = rhs.strip().startswith("NOT")
        ops = set(re.findall(r"\b(AND|OR|XOR)\b", rhs))
        if neg and ops == {"AND", "OR"}: got = "and-or-invert"
        elif ops == {"XOR"}: got = "xnor" if neg else "xor"
        elif ops == {"AND"}: got = "nand" if neg else "and"
        elif ops == {"OR"}: got = "nor" if neg else "or"
        elif not ops: got = "not" if neg else "buffer"
        else: got = "complex"
        k = g.get("kind")
        dual = {"nand": "or", "or": "nand", "nor": "and", "and": "nor"}
        if k not in ("other",) and got != "complex" and k != got and dual.get(k) != got:
            V.append(("gate-kind", g["id"], f"key {k} pinout {fn['formula']}"))
    # gates on one package must not share pins
    used = {}
    for g in K["gates"]:
        for x in list(g.get("inputs") or []) + [g.get("output")]:
            a = f"{g.get('part')}.{x}"
            if a in used and used[a] != g["id"]: V.append(("pin-in-two-gates", a, used[a] + " / " + g["id"]))
            used[a] = g["id"]
    # two driving totem-pole outputs on one net
    for n in K["nets"]:
        drv = []
        for p in n["pins"]:
            if "." not in p["at"]: continue
            ref, pin = p["at"].rsplit(".", 1); pr = parts.get(ref)
            if not pr or pr.get("kind") not in ("chip", "gatepack"): continue
            c = chip(pr.get("type"))
            if not c: continue
            for x in c["pins"]:
                if str(x["pin"]) == pin and x.get("role") == "output" and x.get("outputType", "totem-pole") == "totem-pole": drv.append(p["at"])
        if len(drv) > 1: V.append(("two-outputs-on-net", n["name"], " ".join(drv)))
    return V
if __name__ == "__main__":
    K = json.load(open(sys.argv[1], encoding="utf-8")); V = check(K)
    from collections import Counter
    print(json.dumps({"key": os.path.basename(sys.argv[1]), "violations": len(V), "by": Counter(v[0] for v in V)}))
    if "--list" in sys.argv:
        for v in V: print(" ", *v)
