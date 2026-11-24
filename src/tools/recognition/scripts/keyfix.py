# keyfix.py <page>: apply loop/keyfix/<page>.json ops to loop/key-r2/<page>.json -> loop/key/<page>.json
# ops: {"op":"move","pin":"2A.11","to":"NET"} | {"op":"drop","pin":..,"net":..} | {"op":"add","pin":..,"to":..}
#      {"op":"part", ...part fields} | {"op":"gate", ...gate fields} | {"op":"setgate","id":..,fields..}
import sys, json, os
R = os.path.dirname(os.path.dirname(os.path.abspath(__file__))) + "/loop/"
pg = sys.argv[1]; K = json.load(open(R + "key-r2/" + pg + ".json", encoding="utf-8"))
F = json.load(open(R + "keyfix/" + pg + ".json", encoding="utf-8"))
nets = {n["name"]: n for n in K["nets"]}
def net(name):
    if name not in nets: nets[name] = {"name": name, "pins": []}; K["nets"].append(nets[name])
    return nets[name]
for o in F["ops"]:
    t = o["op"]
    if t in ("move", "drop"):
        for n in K["nets"]:
            if o.get("net") in (None, n["name"]): n["pins"] = [p for p in n["pins"] if p["at"] != o["pin"]]
    if t in ("move", "add"): net(o["to"])["pins"].append({"at": o["pin"]})
    if t == "part": K["parts"].append({k: v for k, v in o.items() if k != "op"})
    if t == "gate": K["gates"].append({k: v for k, v in o.items() if k != "op"})
    if t == "setgate":
        for g in K["gates"]:
            if g["id"] == o["id"]: g.update({k: v for k, v in o.items() if k not in ("op", "id")})
K["nets"] = [n for n in K["nets"] if n["pins"]]
K["check"] = F.get("check", {})
json.dump(K, open(R + "key/" + pg + ".json", "w", encoding="utf-8"), indent=1)
print(pg, "ops", len(F["ops"]))
