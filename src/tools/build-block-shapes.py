"""Draw every box part (the parts that are not gates) from its reading of the manual cut-out.

Input: tools/block-drawings/<id>.json, read off catalog/symbols/nd1976/<id>.png (format in
tools/block-drawings/FORMAT.md). Nothing here comes from a datasheet: the drawing shows what the
manual prints - which side each pin is on, in what order, its number and its text.

Output, for each part:
    catalog/symbols/nd1976/shapes/block-<id>.svg   ND 1976 style: plain box, numbers on the lines
    catalog/symbols/modern/shapes/block-<id>.svg   modern style: the same box with the signal
                                                   names written inside, as a modern data book does
    catalog/symbols/pins/block-<id>.json           where every pin line ends; both drawings end
                                                   their lines exactly there (GateShapeTests)

Run from the src folder:  python -I tools/build-block-shapes.py
"""
import glob, json, os

GRID = 10        # space between two pins on one side
STUB = 20        # length of a pin line outside the box
GAP = 6          # extra space where the drawing leaves a group gap
MARGIN = 60      # room for the text outside the box
SECTION_GAP = 30 # space between two boxes of one part


def esc(t):
    return (t or "").replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


GRID_TB = 9      # the manual keeps top and bottom pins close; their names are staggered instead


def side_length(pins, grid=GRID):
    n = 0
    for i, p in enumerate(pins):
        if i and p.get("gapBefore"):
            n += GAP
        n += grid
    return n


def offsets(pins, grid=GRID):
    """Distance of each pin from the start of its side."""
    out, at = [], grid / 2
    for i, p in enumerate(pins):
        if i and p.get("gapBefore"):
            at += GAP
        out.append(at)
        at += grid
    return out


def key(p, side, i, used):
    k = p.get("number") or f"{side}:{i}"
    while k in used:          # some parts print the same number twice (7494 pin 11)
        k += "'"
    used.add(k)
    return k


def section_layout(s):
    left, right = s.get("left") or [], s.get("right") or []
    top, bottom = s.get("top") or [], s.get("bottom") or []
    h = max(side_length(left), side_length(right), 30)
    w = max(side_length(top, GRID_TB), side_length(bottom, GRID_TB), 28)
    # a reading may set the box size to match the cut-out's proportions
    return s.get("width") or w, s.get("height") or h


def draw(part):
    secs = part["sections"]
    if not secs:
        return None
    sizes = [section_layout(s) for s in secs]
    width = max(w for w, _ in sizes) + 2 * (STUB + MARGIN)
    height = sum(h + 2 * STUB + SECTION_GAP for _, h in sizes) + 30
    nd, mo, pins, used = [], [], [], set()
    y0 = 20 + STUB
    for s, (w, h) in zip(secs, sizes):
        x0 = (width - w) / 2
        outline = s.get("outline") or "box"
        if outline == "box":
            box = f'<rect x="{x0}" y="{y0}" width="{w}" height="{h}"/>'
        elif outline == "dashed":
            box = f'<rect x="{x0}" y="{y0}" width="{w}" height="{h}" stroke-dasharray="2,1.5"/>'
        else:                       # "none": the marks are the whole drawing (opto-couplers)
            box = ""
        nd.append(box)
        mo.append(box)
        # marks drawn inside (or around) the outline, read off the cut-out. Coordinates are
        # fractions of the box: 0,0 = top left corner, 1,1 = bottom right corner.
        for m in s.get("marks") or []:
            X = lambda f: x0 + f * w
            Y = lambda f: y0 + f * h
            t = m.get("type")
            dash = ' stroke-dasharray="2,1.5"' if m.get("dashed") else ""
            if t == "line":
                e = f'<line x1="{X(m["x1"])}" y1="{Y(m["y1"])}" x2="{X(m["x2"])}" y2="{Y(m["y2"])}"{dash}/>'
            elif t in ("polygon", "polyline"):
                pts = " ".join(f"{X(a)},{Y(b)}" for a, b in m["points"])
                fill = ' fill="#000000"' if m.get("filled") else ""
                e = f'<{t} points="{pts}"{fill}{dash}/>'
            elif t == "circle":
                e = f'<circle cx="{X(m["x"])}" cy="{Y(m["y"])}" r="{m["r"] * w}"/>'
            elif t == "text":
                e = f'<text x="{X(m["x"])}" y="{Y(m["y"])}" text-anchor="middle" font-size="{m.get("size", 4.5)}">{esc(m["text"])}</text>'
            else:
                raise SystemExit(f'{part["id"]}: unknown mark type {t!r}')
            nd.append(e)
            mo.append(e)
        label = s.get("insideLabel") or ""
        if label:
            t = f'<text x="{x0 + w / 2}" y="{y0 + h / 2 + 2}" text-anchor="middle" font-size="5">{esc(label)}</text>'
            nd.append(t)
            mo.append(t)
        for side in ("left", "right", "top", "bottom"):
            ps = s.get(side) or []
            grid = GRID_TB if side in ("top", "bottom") else GRID
            offs = offsets(ps, grid)
            full = h if side in ("left", "right") else w
            used_len = side_length(ps, grid)
            if ps and used_len < full:
                # the manual spreads a short side over the whole box (7442 inputs, 74181 outputs)
                offs = [o * full / used_len for o in offs]
            for i, (p, o) in enumerate(zip(ps, offs)):
                if side == "left":
                    bx, by, ex, ey = x0, y0 + o, x0 - STUB, y0 + o
                elif side == "right":
                    bx, by, ex, ey = x0 + w, y0 + o, x0 + w + STUB, y0 + o
                elif side == "top":
                    bx, by, ex, ey = x0 + o, y0, x0 + o, y0 - STUB
                else:
                    bx, by, ex, ey = x0 + o, y0 + h, x0 + o, y0 + h + STUB
                k = key(p, side, i, used)
                pins.append({"pin": k, "x": ex, "y": ey, "side": side,
                             "name": p.get("outside") or p.get("inside") or ""})
                line = f'<line data-pin="{esc(k)}" x1="{bx}" y1="{by}" x2="{ex}" y2="{ey}"/>'
                bubble = ""
                if p.get("bubble"):
                    cx, cy = (bx + (ex - bx) * 0.12, by + (ey - by) * 0.12)
                    bubble = f'<circle cx="{cx}" cy="{cy}" r="1.5" fill="#ffffff"/>'
                nd.append(line + bubble)
                mo.append(line + bubble)
                num, out, ins = p.get("number"), p.get("outside") or "", p.get("inside") or ""
                # ND style: number on the line, text outside, inside text in the box
                if side in ("left", "right"):
                    nx = (bx + ex) / 2
                    nd.append(f'<text x="{nx}" y="{by - 1}" text-anchor="middle" font-size="4">{esc(num)}</text>' if num else "")
                    anc = "end" if side == "left" else "start"
                    tx = ex - 2 if side == "left" else ex + 2
                    nd.append(f'<text x="{tx}" y="{ey + 1.5}" text-anchor="{anc}" font-size="4.5">{esc(out)}</text>' if out else "")
                    ix = bx + 2 if side == "left" else bx - 2
                    ia = "start" if side == "left" else "end"
                    nd.append(f'<text x="{ix}" y="{by + 1.5}" text-anchor="{ia}" font-size="4.5">{esc(ins)}</text>' if ins else "")
                    # modern: number outside on the line, the name inside the box
                    mo.append(f'<text x="{nx}" y="{by - 1}" text-anchor="middle" font-size="4">{esc(num)}</text>' if num else "")
                    name = out or ins
                    mo.append(f'<text x="{ix}" y="{by + 1.5}" text-anchor="{ia}" font-size="4.5">{esc(name)}</text>' if name else "")
                else:
                    ny = (by + ey) / 2
                    nd.append(f'<text x="{bx + 1}" y="{ny}" font-size="4">{esc(num)}</text>' if num else "")
                    # names on neighbouring top/bottom lines are staggered, as in the manual (74193)
                    ty = (ey - 2 - (i % 2) * 6) if side == "top" else (ey + 5 + (i % 2) * 6)
                    nd.append(f'<text x="{ex}" y="{ty}" text-anchor="middle" font-size="4.5">{esc(out)}</text>' if out else "")
                    mo.append(f'<text x="{bx + 1}" y="{ny}" font-size="4">{esc(num)}</text>' if num else "")
                    name = out or ins
                    # modern: the name runs along the line inside the box, turned on its side
                    iy = by + 2 if side == "top" else by - 2
                    anc = "end" if side == "top" else "start"
                    # (turned names inside a short box ran into the side names, 74193) - so the
                    # name of a top/bottom line goes outside, at the line's end, as in ND style
                    mo.append(f'<text x="{ex}" y="{ty}" text-anchor="middle" font-size="4.5">{esc(name)}</text>' if name else "")
        y0 += h + 2 * STUB + SECTION_GAP
    # group labels ("Select input", "Enable", "Input"): placed beyond the pins they name
    at = {p["pin"]: p for p in pins}
    for g in part.get("groupLabels") or []:
        hits = [at[k] for k in g.get("pins") or [] if k in at]
        if not hits:
            continue
        side = hits[0]["side"]
        xs = [h["x"] for h in hits]; ys = [h["y"] for h in hits]
        if side == "left":
            x, y, a = min(xs) - 14, sum(ys) / len(ys) + 1.5, "end"
        elif side == "right":
            x, y, a = max(xs) + 14, sum(ys) / len(ys) + 1.5, "start"
        elif side == "top":
            x, y, a = sum(xs) / len(xs), min(ys) - 15, "middle"
        else:
            x, y, a = sum(xs) / len(xs), max(ys) + 18, "middle"
        t = f'<text x="{x}" y="{y}" text-anchor="{a}" font-size="4.5">{esc(g["text"])}</text>'
        nd.append(t)
        mo.append(t)
    head = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{width * 4}" height="{height * 4}" '
            f'viewBox="0 0 {width} {height}"><title>{esc(part["id"])}</title>'
            f'<rect x="0" y="0" width="{width}" height="{height}" fill="#ffffff" stroke="none"/>'
            '<g fill="none" stroke="#000000" stroke-width="0.8" font-family="serif">')
    def doc(body):
        # text is drawn filled, not outlined
        body = "".join(body).replace("<text ", '<text fill="#000000" stroke="none" ')
        return head + body + "</g></svg>\n"
    return doc(nd), doc(mo), pins


def main():
    n = 0
    import sys
    only = set(sys.argv[1:])      # optional: part ids to draw, e.g. 7474 74193
    for f in sorted(glob.glob("tools/block-drawings/*.json")):
        part = json.load(open(f, encoding="utf-8"))
        if only and part["id"] not in only:
            continue
        r = draw(part)
        if r is None:
            continue          # 8551: the manual prints only "See 74173!"
        nd, mo, pins = r
        k = "block-" + part["id"]
        open(f"catalog/symbols/nd1976/shapes/{k}.svg", "w", encoding="utf-8", newline="\n").write(nd)
        open(f"catalog/symbols/modern/shapes/{k}.svg", "w", encoding="utf-8", newline="\n").write(mo)
        json.dump({"shape": k, "pins": pins}, open(f"catalog/symbols/pins/{k}.json", "w", encoding="utf-8", newline="\n"), indent=1)
        n += 1
    print(n, "box parts drawn")


if __name__ == "__main__":
    main()
