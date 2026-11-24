"""Connection score. For every pair of pins the key joins: does the draft join them (recall).
For every pair the draft joins: does the key join them (precision). One-pin nets give no pairs.
Usage: python -I connscore.py <draft.rsc> <key.rsc-or-json> [more draft key pairs...] -> one JSON line
(several pairs are summed)."""
import sys, json
def nets(f):
    N = json.load(open(f, encoding="utf-8"))["nets"]
    return [[(p["at"] if isinstance(p, dict) else p) for p in n["pins"]] for n in N]
def pairs(ns):
    m = {}
    for i, ps in enumerate(ns):
        for p in ps: m[p] = i
    return m, sum(len(set(ps)) * (len(set(ps)) - 1) // 2 for ps in ns)
T = {"key_pairs": 0, "draft_pairs": 0, "pairs_right": 0}
a = sys.argv[1:]
for d, k in zip(a[0::2], a[1::2]):
    km, kn = pairs(nets(k)); dn = nets(d); dm, dpn = pairs(dn)
    right = 0
    for ps in dn:
        s = sorted(set(ps))
        for i in range(len(s)):
            for j in range(i + 1, len(s)):
                if s[i] in km and s[j] in km and km[s[i]] == km[s[j]]: right += 1
    T["key_pairs"] += kn; T["draft_pairs"] += dpn; T["pairs_right"] += right
T["recall"] = round(T["pairs_right"] / max(1, T["key_pairs"]), 4); T["precision"] = round(T["pairs_right"] / max(1, T["draft_pairs"]), 4)
print(json.dumps(T))
