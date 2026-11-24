"""Print the page-style decision (0 hand, 1 CAD) of the trained style rule (style/pagestyle.json)
for a loop page name like n10-61, binder8, nd12041, scsi13, p64."""
import sys, json, re, os
R = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
p = sys.argv[1]
m = re.match(r"n10-(\d+)", p) or None
k = f"nord10:{m.group(1)}" if m else "nord10:64" if p == "p64" else re.sub(r"^([a-z]+?)(\d+)$", lambda x: {"nd": "nd120"}.get(x.group(1), x.group(1)) + ":" + x.group(2), p)
if p.startswith("nd120"): k = "nd120:" + p[5:]
print(json.load(open(R + "/style/pagestyle.json"))[k])
