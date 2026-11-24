import json, glob, collections, os, sys
C = collections.Counter(); W = collections.Counter()
for f in sorted(glob.glob(sys.argv[1] + "/*.json")):
    cs = json.load(open(f, encoding="utf-8")).get("corrections", [])
    for c in cs:
        C[c.get("cause", "?").split(":")[0].strip()] += 1; W[c.get("what", "?")] += 1
print("causes", C.most_common()); print("what", W.most_common()); print("total", sum(C.values()))
