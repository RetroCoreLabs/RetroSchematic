import sys, json, glob, os
for f in sorted(glob.glob(sys.argv[1] + "/*.json")):
    K = json.load(open(f, encoding="utf-8")); P = K["parts"]; G = K["gates"]
    ok = lambda b: isinstance(b, list) and len(b) == 4 and b[2] > 0
    print(os.path.basename(f), "parts", len(P), "boxed", sum(ok(p.get("box")) for p in P),
          "chips", sum(p.get("kind") == "chip" for p in P), "gates", len(G), "boxed", sum(ok(g.get("box")) for g in G),
          "pins", sum(len(n["pins"]) for n in K.get("nets", [])), "corr", len(K.get("corrections", [])))
