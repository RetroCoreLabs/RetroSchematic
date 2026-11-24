"""Score a machine draft against the hand reading (the answer key; used here ONLY).
Usage: python -I score.py <draft.rsc> <dump.json> <hand .rsc> <page.png> <errors.png> <result.json>
"""
import site, sys, json, re
sys.path.append(site.getusersitepackages())
import numpy as np, cv2
dr, dump, hand, page, epng, outj = sys.argv[1:7]
D = json.load(open(dr, encoding="utf-8")); U = json.load(open(dump)); K = json.load(open(hand, encoding="utf-8"))
norm = lambda t: re.sub(r"^(DM|SN)", "", (t or "").replace(" ", "").upper())
res = {}
# ---- words: net names of the hand reading found as a word on the page, exactly
names = sorted({n["name"] for n in K["nets"] if not n["name"].startswith("N_")})
raw = {w["text"] for w in U["words"]}; sn = {w["snap"] for w in U["words"]}
nosp = [x for x in names if " " not in x]
res["words"] = {"net_names": len(names), "one_word_names": len(nosp),
                "read_exactly_before_snap": sum(x in raw for x in nosp), "after_snap": sum(x in sn for x in nosp),
                "missed_after_snap": [x for x in nosp if x not in sn]}
# subscripts: label words carrying a subscript that matches a hand net name+level
hk = {(n["name"], n["active"]) for n in K["nets"]}
lw = [(w["snap"], {"0": "low", "1": "high"}.get(w["sub"])) for w in U["words"] if w["sub"] is not None]
res["words"]["words_with_subscript"] = len(lw)
res["words"]["name_and_subscript_in_hand_reading"] = sum((a, b) in hk for a, b in lw)
# ---- parts
kp = {p["ref"]: p for p in K["parts"]}; dp = {p["ref"]: p for p in D["parts"]}
chips_k = {r for r, p in kp.items() if not re.match(r"^[RCW]-", r)}
res["parts"] = {"hand_parts": len(kp), "hand_chips_no_passives": len(chips_k), "draft_parts": len(dp),
                "ref_found": sum(r in dp for r in chips_k), "ref_found_type_right": sum(r in dp and norm(dp[r]["type"]) == norm(kp[r]["type"]) for r in chips_k),
                "draft_refs_not_in_hand": sorted(r for r in dp if r not in kp)}
bigk = {r for r in chips_k if kp[r]["type"] not in ("7400", "7402", "7404", "7408", "7438", "7451", "7401")}
res["parts"]["typed_chip_boxes_hand"] = len(bigk)
res["parts"]["typed_found_type_right"] = sum(r in dp and norm(dp[r]["type"]) == norm(kp[r]["type"]) for r in bigk)
res["parts"]["type_errors"] = [(r, dp[r]["type"], kp[r]["type"]) for r in chips_k if r in dp and norm(dp[r]["type"]) != norm(kp[r]["type"])]
# ---- gates: draft gate matches a hand gate of the same part sharing the output pin (or, without
# an output, sharing an input)
kg = K["gates"]; gm = {"found": 0, "kind_right": 0, "hand": len(kg), "draft": len(D["gates"]), "draft_with_ref": len(D["gates"])}
used = set(); kinds = []
for g in D["gates"]:
    best = None
    for j, h in enumerate(kg):
        if j in used or h["part"] != g["part"]:
            continue
        sc = 2 * (g["output"] is not None and g["output"] == h["output"]) + len(set(g["inputs"]) & set(h["inputs"]))
        if sc > 0 and (best is None or sc > best[0]):
            best = (sc, j)
    if best:
        used.add(best[1]); gm["found"] += 1; h = kg[best[1]]
        gm["kind_right"] += g["kind"] == h["kind"]; kinds.append((h["kind"], g["kind"]))
from collections import Counter
gm["kind_confusion_hand_vs_draft"] = Counter(f"{a}->{b}" for a, b in kinds).most_common()
gm["gate_symbols_seen"] = len(U["gates"]); gm["gate_symbols_without_ref"] = sum(q["ref"] is None for q in U["gates"])
gm["draft_kind_counts"] = Counter(q["kind"] for q in U["gates"]).most_common()
gm["hand_kind_counts"] = Counter(h["kind"] for h in kg).most_common()
res["gates"] = gm
# ---- nets
kpin = {p["at"]: i for i, n in enumerate(K["nets"]) for p in n["pins"]}
dpin = {p["at"]: i for i, n in enumerate(D["nets"]) for p in n["pins"]}
# each draft net maps to the hand net holding most of its pins
dmap = {}
for i, n in enumerate(D["nets"]):
    c = Counter(kpin[p["at"]] for p in n["pins"] if p["at"] in kpin)
    dmap[i] = c.most_common(1)[0][0] if c else None
exact = partial = 0; wrong_join = 0; pin_right = 0
for j, n in enumerate(K["nets"]):
    ps = [p["at"] for p in n["pins"]]
    c = Counter(dpin[p] for p in ps if p in dpin)
    if not c:
        continue
    bi = c.most_common(1)[0][0]
    dset = {p["at"] for p in D["nets"][bi]["pins"]}
    if dset == set(ps):
        exact += 1
    else:
        partial += 1
for i, n in enumerate(D["nets"]):
    for p in n["pins"]:
        if p["at"] not in kpin:
            continue
        if kpin[p["at"]] == dmap[i]:
            pin_right += 1
        else:
            wrong_join += 1
allk = len(kpin)
res["nets"] = {"hand_nets": len(K["nets"]), "hand_pins": allk, "draft_nets": len(D["nets"]),
               "draft_pins": len(dpin), "draft_pins_not_in_hand": sum(p not in kpin for p in dpin),
               "hand_nets_exact": exact, "hand_nets_partial": partial, "hand_nets_none": len(K["nets"]) - exact - partial,
               "pins_in_right_net": pin_right, "pins_in_wrong_net": wrong_join,
               "share_pin_assignments_right": round(pin_right / allk, 4),
               "pin_ends_unreadable": sum(1 for e in U["ends"] if e[1][0] in ("pin", "conn") and "?" in e[1][1])}
st = Counter()
for e in U["ends"]:
    a = e[1]
    if a[0] == "pin":
        r, pn = a[1].rsplit(".", 1)
        st["pin end: no part ref" if r == "?" else "pin end: ref not a hand part" if r not in kp else "pin end: no pin number" if pn == "?" else
           "pin end: pin not in hand nets" if a[1] not in kpin else "pin end: ok ref+pin"] += 1
    elif a[0] == "conn":
        st["connector end: number unread" if a[1] == "P.?" else "connector end: pin not in hand" if a[1] not in kpin else "connector end: ok"] += 1
    else:
        st["label end"] += 1
res["ends"] = dict(st)
why = Counter()
dref = {a[1].rsplit(".", 1)[0] for e in U["ends"] for a in [e[1]] if a[0] == "pin"}
for at in kpin:
    if at in dpin:
        continue
    r = at.rsplit(".", 1)[0]
    why["passive part (resistor/capacitor/strap/switch): not detected" if re.match(r"^[RCW]-", r) or kp.get(r, {}).get("type", "").startswith(("VP", "SW")) or r in ("15E", "1E")
        else "connector pin: no wire end reached a read circle" if r == "P"
        else "part ref never read on the page" if r not in dref
        else "part ref read, this pin not attached / number misread"] += 1
res["hand_pins_missing_why"] = why.most_common()
res["times"] = U["times"]; res["stats"] = U["stats"]
# ---- picture
g = cv2.imread(page); ov = (g * 0.55 + 110).astype(np.uint8)
for s in U["segs"]:
    if s[0] == "h": cv2.line(ov, (int(s[2]), int(s[1])), (int(s[3]), int(s[1])), (200, 150, 0), 2)
    else: cv2.line(ov, (int(s[1]), int(s[2])), (int(s[1]), int(s[3])), (200, 150, 0), 2)
for c in U["chips"] + U["gates"]:
    x, y, w, h = c["box"]; ref = c["ref"]
    col = (0, 0, 255) if ref is None or ref not in kp else (0, 170, 0)
    cv2.rectangle(ov, (x, y), (x + w, y + h), col, 3)
    cv2.putText(ov, str(ref) + ("/" + c.get("type", "") if c.get("type") else ""), (x, y - 4), 0, 0.7, col, 2)
for e in U["ends"]:
    a = e[1]; px, py = map(int, a[2])
    if a[0] in ("pin", "conn"):
        at = a[1]
        if at in dpin and at in kpin:
            col = (0, 170, 0) if kpin[at] == dmap[dpin[at]] else (0, 0, 255)
        else:
            col = (0, 140, 255)
        cv2.circle(ov, (px, py), 10, col, 3); cv2.putText(ov, at, (px + 8, py + 22), 0, 0.55, col, 2)
    else:
        cv2.circle(ov, (px, py), 8, (200, 0, 200), 3)
cv2.putText(ov, "green = right, red = wrong net / unknown ref, orange = pin not read or not in the hand reading, magenta = label end",
            (40, ov.shape[0] - 30), 0, 1.0, (0, 0, 0), 2)
cv2.imwrite(epng, ov)
json.dump(res, open(outj, "w"), indent=1, default=str)
print(json.dumps(res, indent=1, default=str))
