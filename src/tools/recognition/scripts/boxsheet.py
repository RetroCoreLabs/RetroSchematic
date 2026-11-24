# boxsheet.py <page> : contact sheet of every gate box (+20 px margin, box drawn) from loop/pages
import sys, json
from PIL import Image, ImageDraw
pg = sys.argv[1]; K = json.load(open(f"loop/key/{pg}.json")); im = Image.open(f"loop/pages/{pg}.png").convert("L")
G = K["gates"]; C = 8; S = 130; sheet = Image.new("L", (C * S, ((len(G) + C - 1) // C) * (S + 14)), 255); d = ImageDraw.Draw(sheet)
for i, g in enumerate(G):
    x, y, w, h = g["box"]; m = 25
    cr = im.crop((x - m, y - m, x + w + m, y + h + m)).convert("RGB"); dd = ImageDraw.Draw(cr); dd.rectangle((m, m, m + w, m + h), outline=(160, 160, 160))
    cr.thumbnail((S, S)); X, Y = (i % C) * S, (i // C) * (S + 14); sheet.paste(cr.convert("L"), (X, Y + 14)); d.text((X + 2, Y), f"{i}:{g['id']}", fill=0)
sheet.save(f"loop/zoom/{pg}-gates.png"); print(len(G))
