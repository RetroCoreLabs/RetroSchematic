"""Loop: cut a page into overlapping tiles for checking by eye, two versions of each tile:
plain (read the page yourself first) and with the draft's symbol boxes (blue C# chips, red G# gates).
Also writes draft-list.json: every draft symbol with its id, box, ref, type, kind.
Usage: python -I looptiles.py <page.png> <dump.json> <outdir>"""
import site, sys, os, json
sys.path.append(site.getusersitepackages())
import cv2
page, dump, od = sys.argv[1:4]; os.makedirs(od, exist_ok=True)
g = cv2.imread(page); U = json.load(open(dump)); H, W = g.shape[:2]
lst = []
ov = g.copy()
for i, c in enumerate(U["chips"]):
    lst.append({"id": f"C{i}", "box": c["box"], "ref": c["ref"], "type": c.get("type"), "type_read": c.get("type_read")})
for i, c in enumerate(U["gates"]):
    lst.append({"id": f"G{i}", "box": c["box"], "ref": c["ref"], "kind": c["kind"]})
for o in lst:
    x, y, w, h = o["box"]; col = (255, 0, 0) if o["id"][0] == "C" else (0, 0, 255)
    cv2.rectangle(ov, (x, y), (x + w, y + h), col, 2)
    cv2.putText(ov, f'{o["id"]} {o["ref"]}', (x, max(12, y - 3)), 0, 0.6, col, 2)
json.dump(lst, open(f"{od}/draft-list.json", "w"), indent=0)
TW, TH = 1300, 950; k = 0
for y0 in range(0, max(1, H - 200), TH - 150):
    for x0 in range(0, max(1, W - 200), TW - 150):
        x1, y1 = min(W, x0 + TW), min(H, y0 + TH)
        n = f"t{y0 // (TH - 150)}{x0 // (TW - 150)}_x{x0}_y{y0}"
        cv2.imwrite(f"{od}/{n}.png", g[y0:y1, x0:x1]); cv2.imwrite(f"{od}/{n}-draft.png", ov[y0:y1, x0:x1]); k += 1
s = 1000 / W
cv2.imwrite(f"{od}/whole.png", cv2.resize(ov, (1000, int(H * s)), interpolation=cv2.INTER_AREA))
print(od, k, "tiles", len(lst), "symbols")
