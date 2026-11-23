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
import glob, json, os, re

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


def subscripts(text):
    """Q_A in a reading is Q with a small A below the line, as the manual prints it."""
    parts = re.split(r"_([A-Za-z0-9+-]+)", text)
    out = parts[0]
    for j in range(1, len(parts), 2):
        out += f'<tspan dy="1.5" font-size="3.2">{parts[j]}</tspan><tspan dy="-1.5">{parts[j + 1]}</tspan>'
    return out


def sub_text(mt):
    """Write one text element with its subscripts. The PNG renderer places tspans wrongly in
    right-aligned or centred text (the subscript lands on the letters - 74298, Vcc), so such text
    is turned into left-aligned text, moved left by its estimated width (serif: about half the
    font size per character, subscript characters smaller)."""
    head, content, tail = mt.group(1), mt.group(2), mt.group(3)
    if "_" not in content and "~" not in content:
        return head + content + tail
    size = float(re.search(r'font-size="([0-9.]+)"', head).group(1))
    anchor = re.search(r'text-anchor="(\w+)"', head)
    bars = []
    if "~" in content:
        # an overbar: ~X bars one name (letters, digits, subscript), ~{A·E} bars the whole group.
        # The PNG renderer has no overline, so the bar is a line over the estimated letter span.
        plain_so_far = 0.0
        out = ""
        k = 0
        while k < len(content):
            if content[k] == "~":
                if k + 1 < len(content) and content[k + 1] == "{":
                    end = content.index("}", k)
                    inner = content[k + 2:end]
                    k = end + 1
                else:
                    m2 = re.match(r"[A-Za-z0-9]+(_[A-Za-z0-9+-]+)?", content[k + 1:])
                    inner = m2.group(0) if m2 else ""
                    k += 1 + len(inner)
                w_inner = text_width(inner, size)
                bars.append((plain_so_far, plain_so_far + w_inner))
                plain_so_far += w_inner
                out += inner
            else:
                out += content[k]
                plain_so_far += text_width(content[k], size)
                k += 1
        content = out
    if anchor and anchor.group(1) != "start":
        parts = re.split(r"_([A-Za-z0-9+-]+)", content)
        width = text_width(content, size)
        shift = width if anchor.group(1) == "end" else width / 2
        x = float(re.search(r' x="([-0-9.]+)"', head).group(1))
        head = re.sub(r' x="[-0-9.]+"', f' x="{x - shift}"', head, count=1)
        head = head.replace(f'text-anchor="{anchor.group(1)}"', 'text-anchor="start"')
    result = head + subscripts(content) + tail
    if bars:
        x = float(re.search(r' x="([-0-9.]+)"', head).group(1))
        y = float(re.search(r' y="([-0-9.]+)"', head).group(1))
        for b0, b1 in bars:
            result += f'<line x1="{x + b0}" y1="{y - size * 0.85}" x2="{x + b1}" y2="{y - size * 0.85}" stroke-width="0.4"/>'
    return result


def split_lines(mt):
    """Text printed on two or more lines in the manual ("Serial" over "input") is written with a
    line break in the reading; each line becomes its own text element, one line height lower."""
    head, content, tail = mt.group(1), mt.group(2), mt.group(3)
    if "\n" not in content:
        return head + content + tail
    size = float(re.search(r'font-size="([0-9.]+)"', head).group(1))
    y = float(re.search(r' y="([-0-9.]+)"', head).group(1))
    out = ""
    for j, line in enumerate(content.split("\n")):
        out += re.sub(r' y="[-0-9.]+"', f' y="{y + j * size * 1.15}"', head, count=1) + line + tail
    return out


def extent(body, width, height):
    """Smallest area (with a margin) that holds the normal drawing area and everything drawn."""
    xs, ys = [0.0, width], [0.0, height]
    for m in re.finditer(r' (x|x1|x2|cx)="([-0-9.e]+)"', body):
        xs.append(float(m.group(2)))
    for m in re.finditer(r' (y|y1|y2|cy)="([-0-9.e]+)"', body):
        ys.append(float(m.group(2)))
    for m in re.finditer(r'points="([^"]+)"', body):
        for pt in m.group(1).split():
            a, b = pt.split(",")
            xs.append(float(a))
            ys.append(float(b))
    for m in re.finditer(r'<text[^>]* x="([-0-9.e]+)"[^>]*font-size="([0-9.]+)"[^>]*>(.*?)</text>', body):
        plain = re.sub(r"<[^>]+>", "", m.group(3))
        xs.append(float(m.group(1)) + text_width(plain, float(m.group(2))))
    return min(xs) - 5, min(ys) - 8, max(xs) + 5, max(ys) + 5


def text_width(t, size):
    """Estimated width of printed text: serif about half the font size per character, and a
    subscript (after _) at the smaller subscript size."""
    if "\n" in t:
        # text on several lines is as wide as its widest line (8599 "Memory / enable")
        return max(text_width(line, size) for line in t.split("\n"))
    parts = re.split(r"_([A-Za-z0-9+-]+)", t)
    # 0.55 per character: at 0.5 the overbars stopped short of the last letter (74H54)
    return 0.55 * size * len("".join(parts[0::2])) + 0.55 * 3.2 * len("".join(parts[1::2]))


def dashes(x1, y1, x2, y2, on=2.0, off=1.5):
    """A dashed line as real short pieces. (The PNG renderer ignores stroke-dasharray, so a
    dashed line written as a style came out solid - 7453, 7490.)"""
    import math
    L = math.hypot(x2 - x1, y2 - y1)
    if L == 0:
        return ""
    ux, uy = (x2 - x1) / L, (y2 - y1) / L
    out, t = [], 0.0
    while t < L:
        e = min(t + on, L)
        out.append(f'<line x1="{x1 + ux * t}" y1="{y1 + uy * t}" x2="{x1 + ux * e}" y2="{y1 + uy * e}"/>')
        t = e + off
    return "".join(out)


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
    # rows: a section with "beside": true sits to the right of the one before it, as the
    # "Alternative" drawings of 8097/8098 do; every other section starts a new row below
    cell = [w + 2 * (STUB + MARGIN) for w, _ in sizes]
    rows = []
    for i, sec in enumerate(secs):
        if i and sec.get("beside"):
            rows[-1].append(i)
        else:
            rows.append([i])
    width = max(sum(cell[i] for i in r) for r in rows)
    height = sum(max(sizes[i][1] for i in r) + 2 * STUB + SECTION_GAP for r in rows) + 30
    place = {}
    ry = 20 + STUB
    for r in rows:
        rx = (width - sum(cell[i] for i in r)) / 2
        for i in r:
            place[i] = (rx + (cell[i] - sizes[i][0]) / 2, ry)
            rx += cell[i]
        ry += max(sizes[i][1] for i in r) + 2 * STUB + SECTION_GAP
    nd, mo, pins, used = [], [], [], set()
    y0 = 20 + STUB
    for si, (s, (w, h)) in enumerate(zip(secs, sizes)):
        x0, y0 = place[si]
        outline = s.get("outline") or "box"
        if outline == "box":
            box = f'<rect x="{x0}" y="{y0}" width="{w}" height="{h}"/>'
        elif outline == "dashed":
            box = (dashes(x0, y0, x0 + w, y0) + dashes(x0 + w, y0, x0 + w, y0 + h)
                   + dashes(x0 + w, y0 + h, x0, y0 + h) + dashes(x0, y0 + h, x0, y0))
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
            if t == "line" and m.get("dashed"):
                e = dashes(X(m["x1"]), Y(m["y1"]), X(m["x2"]), Y(m["y2"]))
            elif t == "line":
                e = f'<line x1="{X(m["x1"])}" y1="{Y(m["y1"])}" x2="{X(m["x2"])}" y2="{Y(m["y2"])}"/>'
            elif t in ("polygon", "polyline") and m.get("dashed"):
                pts = [(X(a), Y(b)) for a, b in m["points"]]
                if t == "polygon":
                    pts.append(pts[0])
                e = "".join(dashes(*pts[j], *pts[j + 1]) for j in range(len(pts) - 1))
            elif t in ("polygon", "polyline"):
                pts = " ".join(f"{X(a)},{Y(b)}" for a, b in m["points"])
                fill = ' fill="#000000"' if m.get("filled") else ""
                e = f'<{t} points="{pts}"{fill}{dash}/>'
            elif t == "circle":
                e = f'<circle cx="{X(m["x"])}" cy="{Y(m["y"])}" r="{m["r"] * w}"/>'
            elif t == "brace":
                # a curly brace: vertical (x, y1, y2, opens "left" or "right") or horizontal
                # (y, x1, x2, opens "up" or "down"); the point faces away from the opening
                d = m.get("dir", "right")
                k = 3.0
                if d in ("left", "right"):
                    bx_, a1, a2 = X(m["x"]), Y(m["y1"]), Y(m["y2"])
                    sg = -1 if d == "right" else 1
                    mid = (a1 + a2) / 2
                    e = (f'<path d="M{bx_ - sg * k},{a1} Q{bx_},{a1} {bx_},{a1 + k} L{bx_},{mid - k} '
                         f'Q{bx_},{mid} {bx_ + sg * k},{mid} Q{bx_},{mid} {bx_},{mid + k} '
                         f'L{bx_},{a2 - k} Q{bx_},{a2} {bx_ - sg * k},{a2}"/>')
                else:
                    by_, a1, a2 = Y(m["y"]), X(m["x1"]), X(m["x2"])
                    sg = -1 if d == "down" else 1
                    mid = (a1 + a2) / 2
                    e = (f'<path d="M{a1},{by_ - sg * k} Q{a1},{by_} {a1 + k},{by_} L{mid - k},{by_} '
                         f'Q{mid},{by_} {mid},{by_ + sg * k} Q{mid},{by_} {mid + k},{by_} '
                         f'L{a2 - k},{by_} Q{a2},{by_} {a2},{by_ - sg * k}"/>')
            elif t == "text":
                # anchor: "start" (left-aligned notes, 7470), "middle" (default) or "end"
                e = f'<text x="{X(m["x"])}" y="{Y(m["y"])}" text-anchor="{m.get("anchor", "middle")}" font-size="{m.get("size", 4.5)}">{esc(m["text"])}</text>'
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
            # names on top/bottom lines go one row lower only where they would collide with the
            # neighbour's name; where there is room they stand side by side (8214 "1Y 2Y")
            low = []
            for j, q in enumerate(ps):
                nm = q.get("outside") or ""
                if side in ("top", "bottom") and j and nm and low:
                    prev = ps[j - 1].get("outside") or ""
                    room = offs[j] - offs[j - 1] if j < len(offs) else 99
                    clash = room < (text_width(prev, 4.5) + text_width(nm, 4.5)) / 2 + 2
                    low.append(clash and not low[-1] and not q.get("nameAnchor"))
                else:
                    low.append(False)
            for i, (p, o) in enumerate(zip(ps, offs)):
                if p.get("at") is not None:
                    # the reading says where along the side the pin sits (fraction 0..1 of the side)
                    o = p["at"] * full
                L = p.get("length") or STUB     # a reading may shorten one line (7475 pins 4, 13)
                if side == "left":
                    bx, by, ex, ey = x0, y0 + o, x0 - L, y0 + o
                elif side == "right":
                    bx, by, ex, ey = x0 + w, y0 + o, x0 + w + L, y0 + o
                elif side == "top":
                    bx, by, ex, ey = x0 + o, y0, x0 + o, y0 - L
                else:
                    bx, by, ex, ey = x0 + o, y0 + h, x0 + o, y0 + h + L
                k = key(p, side, i, used)
                bend = p.get("bend")
                bent_text = ""
                if bend:
                    # the line leaves the box, then turns (74153 select and enable lines); the
                    # name sits at the far end of the turned part
                    lx, ly = ex, ey
                    if "stub" in bend:      # a different first length than the default
                        f = bend["stub"] / STUB
                        lx, ly = bx + (ex - bx) * f, by + (ey - by) * f
                    d, n = bend["dir"], bend["length"]
                    fx, fy = {"left": (lx - n, ly), "right": (lx + n, ly), "up": (lx, ly - n), "down": (lx, ly + n)}[d]
                    first = f'<line x1="{bx}" y1="{by}" x2="{lx}" y2="{ly}"/>'
                    nd.append(first)
                    mo.append(first)
                    nb, ex, ey = (lx, ly), fx, fy
                    out_name = p.get("outside") or ""
                    if out_name:
                        anc = {"left": "end", "right": "start", "up": "middle", "down": "middle"}[d]
                        gap = 5 if p.get("endMark") else 2   # clear of the end diamond (7483)
                        tx = fx - gap if d == "left" else fx + gap if d == "right" else fx
                        ty = fy + 1.5 if d in ("left", "right") else fy - 2 if d == "up" else fy + 5
                        bent_text = f'<text x="{tx}" y="{ty}" text-anchor="{anc}" font-size="4.5">{esc(out_name)}</text>'
                        nd.append(bent_text)
                        mo.append(bent_text)
                    p = dict(p, outside="")   # already written
                    lbx, lby = nb
                else:
                    lbx, lby = bx, by
                pins.append({"pin": k, "x": ex, "y": ey, "side": side,
                             "name": (bend and bent_text and (p.get("inside") or "")) or p.get("outside") or p.get("inside") or ""})
                line = f'<line data-pin="{esc(k)}" x1="{lbx}" y1="{lby}" x2="{ex}" y2="{ey}"/>'
                bubble = ""
                if p.get("bubble"):
                    cx, cy = (bx + (ex - bx) * 0.12, by + (ey - by) * 0.12)
                    bubble = f'<circle cx="{cx}" cy="{cy}" r="1.5" fill="#ffffff"/>'
                extra = ""
                if p.get("second") and not p.get("number"):
                    # the other half's pin number, printed in brackets next to this one (7476)
                    if side in ("left", "right"):
                        sx = bx - L / 2 if side == "left" else bx + L / 2
                        extra += f'<text x="{sx}" y="{by + 4.5}" text-anchor="middle" font-size="3.5">({esc(p["second"])})</text>'
                    else:
                        sy = by - L / 2 if side == "top" else by + L / 2
                        extra += f'<text x="{bx - 1}" y="{sy}" text-anchor="end" font-size="3.5">({esc(p["second"])})</text>'
                if p.get("endMark") == "diamond":
                    # the line ends in a small open diamond (7483 carry out)
                    d = 2.2
                    extra += f'<polygon points="{ex - d},{ey} {ex},{ey - d} {ex + d},{ey} {ex},{ey + d}" fill="#ffffff"/>'
                nd.append(line + bubble + extra)
                mo.append(line + bubble + extra)
                num, out, ins = p.get("number"), p.get("outside") or "", p.get("inside") or ""
                # where the number sits: half way along a straight line; on a bent line, half way
                # along the straight part before the turn, so it never lands on the turn's dot (VP5)
                nd_ = L
                if p.get("numberAt") is not None:
                    # where along the line the number stands, 0 = at the box, 1 = at the end
                    # (VP5: between the box and the dot half way out)
                    nd_ = 2 * p["numberAt"] * L
                elif bend:
                    nd_ = min(bend.get("stub", STUB), STUB) * 0.8
                    if p.get("numberOnBend") and num:
                        # printed on the turned part instead (8831 "(14) 1")
                        label = f'({p["second"]}) {num}' if p.get("second") and f'({p["second"]})' not in num else num
                        mx, my = (lbx + ex) / 2, (lby + ey) / 2
                        t_ = f'<text x="{mx}" y="{my - 1}" text-anchor="middle" font-size="4">{esc(label)}</text>'
                        nd.append(t_)
                        mo.append(t_)
                        num = None
                if num and p.get("second"):
                    # the manual prints the bracketed number on the far side from the box:
                    # "(13) 1" on the left, "3 (11)" on the right (7476, 74393)
                    if side == "right":
                        num = f'{num} ({p["second"]})'
                    elif side == "left":
                        num = f'({p["second"]}) {num}'
                    else:
                        # top and bottom: the bracketed number stands under the pin number
                        # (74120 "1" over "(15)", 74123) - a line break, split by split_lines
                        # top: the bracket stands above the number, away from the box (74123
                        # "(6)" over "14"); bottom: under it (74120 "1" over "(15)")
                        num = f'({p["second"]})\n{num}' if side == "top" else f'{num}\n({p["second"]})'
                # ND style: number on the line, text outside, inside text in the box
                if side in ("left", "right"):
                    nx = bx - nd_ / 2 if side == "left" else bx + nd_ / 2
                    nd.append(f'<text x="{nx}" y="{by - 1}" text-anchor="middle" font-size="4">{esc(num)}</text>' if num else "")
                    anc = "end" if side == "left" else "start"
                    gap = 5 if p.get("endMark") else 2
                    tx = ex - gap if side == "left" else ex + gap
                    nd.append(f'<text x="{tx}" y="{ey + 1.5}" text-anchor="{anc}" font-size="4.5">{esc(out)}</text>' if out else "")
                    ix = bx + 2 if side == "left" else bx - 2
                    ia = "start" if side == "left" else "end"
                    nd.append(f'<text x="{ix}" y="{by + 1.5}" text-anchor="{ia}" font-size="4.5">{esc(ins)}</text>' if ins else "")
                    # modern: number outside on the line, the name inside the box
                    mo.append(f'<text x="{nx}" y="{by - 1}" text-anchor="middle" font-size="4">{esc(num)}</text>' if num else "")
                    name = out or ins
                    mo.append(f'<text x="{ix}" y="{by + 1.5}" text-anchor="{ia}" font-size="4.5">{esc(name)}</text>' if name else "")
                else:
                    ny = by - nd_ / 2 if side == "top" else by + nd_ / 2
                    nd.append(f'<text x="{bx + 1}" y="{ny}" font-size="4">{esc(num)}</text>' if num else "")
                    # names on neighbouring top/bottom lines are staggered, as in the manual (74193)
                    ty = (ey - 2 - low[i] * 6) if side == "top" else (ey + 5 + low[i] * 6)
                    # nameAnchor: "end" = name left of the line, "start" = right of it (8599)
                    n_anchor = p.get("nameAnchor", "middle")
                    n_x = ex - 1 if n_anchor == "end" else ex + 1 if n_anchor == "start" else ex
                    nd.append(f'<text x="{n_x}" y="{ty}" text-anchor="{n_anchor}" font-size="4.5">{esc(out)}</text>' if out else "")
                    mo.append(f'<text x="{bx + 1}" y="{ny}" font-size="4">{esc(num)}</text>' if num else "")
                    name = out or ins
                    # modern: the name runs along the line inside the box, turned on its side
                    iy = by + 2 if side == "top" else by - 2
                    anc = "end" if side == "top" else "start"
                    # (turned names inside a short box ran into the side names, 74193) - so the
                    # name of a top/bottom line goes outside, at the line's end, as in ND style
                    mo.append(f'<text x="{n_x}" y="{ty}" text-anchor="{n_anchor}" font-size="4.5">{esc(name)}</text>' if name else "")
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
        if "x" in g and "y" in g:
            # the reading places the label itself: fractions of the whole drawing (7485, 7472)
            x, y, a = g["x"] * width, g["y"] * height, g.get("anchor", "middle")
        t = f'<text x="{x}" y="{y}" text-anchor="{a}" font-size="4.5">{esc(g["text"])}</text>'
        nd.append(t)
        mo.append(t)
    def doc(body):
        # text is drawn filled, not outlined
        body = "".join(body).replace("<text ", '<text fill="#000000" stroke="none" ')
        body = re.sub(r"(<text[^>]*>)(.*?)(</text>)", split_lines, body, flags=re.S)
        body = re.sub(r"(<text[^>]*>)(.*?)(</text>)", sub_text, body)
        # the drawing area grows to hold everything drawn, also marks placed far from the box
        # (74107's truth table); pin positions do not move, only the visible area
        ax0, ay0, ax1, ay1 = extent(body, width, height)
        aw, ah = ax1 - ax0, ay1 - ay0
        head = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{aw * 4}" height="{ah * 4}" '
                f'viewBox="{ax0} {ay0} {aw} {ah}"><title>{esc(part["id"])}</title>'
                f'<rect x="{ax0}" y="{ay0}" width="{aw}" height="{ah}" fill="#ffffff" stroke="none"/>'
                '<g fill="none" stroke="#000000" stroke-width="0.8" font-family="serif">')
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
