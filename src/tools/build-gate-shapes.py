"""Write every gate-level symbol of ND-13.003.01 in two styles, plus the one pin table both share.

Run from the src folder:  python -I tools/build-gate-shapes.py

A shape is named <base><inputs>, e.g. nand2, nand8, and-oc2, line-driver1. tools/shape_map.py
says which shape each manual entry uses. The pin table (catalog/symbols/pins/<shape>.json) gives
where each pin's line ends; both SVGs draw every pin as <line data-pin="..."> ending exactly there,
so switching from ND symbols to modern ones never moves a signal line. GateShapeTests checks it.

The ND drawings follow the manual's cut-outs (catalog/symbols/nd1976/<part>.png). Checked side by
side against every cut-out on 2026-11-23. What the manual draws:
  NAND      a narrow upright rectangle, empty
  NAND o.c. the same with a vertical line down the centre (7401, 7403)
  NOR       rectangle with an X corner to corner
  NOT       rectangle with a triangle filling it (7404)
  NOT o.c.  rectangle with ONE diagonal, bottom left to top right (7405)
  AND       ONE rectangle split down the middle: left half empty, right half holds the triangle
  AND o.c.  split rectangle, right half holds one diagonal from bottom left to the middle of the
            right edge (7409; 7407 is the same with one input)
  OR        split rectangle: left half X, right half triangle (7432)
  XOR       rectangle with a centre line and a short "=" across it (7486)
  Schmitt   top edge runs on to the right, bottom edge runs on to the left (7413, 7414)
  buffer    the right end is a point (7437, 7440, 7428); a bar before the point = open collector
            (7433, 7438, 7416); a diagonal in the pointed box = inverting open collector (7406)
  strobe    the G input comes up into the bottom of the box (7425)
  line drv. a large open triangle (75150, 75154)
"""
import glob, json, os

T = 4  # top of a body


def height_for(n):
    return max(40, 10 * n + 10)


def input_ys(n, h):
    b = h - T
    return [T + (b - T) * (i + 0.5) / n for i in range(n)]


def names(n, second=None):
    out = [chr(ord("A") + i) for i in range(n)]
    if second and n == 2:
        out[1] = second
    return out


def r(v):
    v = round(v, 2)
    return int(v) if v == int(v) else v


# ---- marks shared by the modern drawings ------------------------------------------------------
def oc_mark(x, y):
    return (f'<path d="M{r(x)},{r(y-3)} L{r(x+3)},{r(y)} L{r(x)},{r(y+3)} L{r(x-3)},{r(y)} Z"/>'
            f'<line x1="{r(x-3)}" y1="{r(y+5)}" x2="{r(x+3)}" y2="{r(y+5)}"/>')


def hv_mark(x, y):
    """Our own label for a high-voltage open-collector output (7406/7407 up to 30 V, 7416/7417 up
    to 15 V, per the TI datasheets). The common modern standard has no separate mark for it."""
    return f'<text x="{r(x)}" y="{r(y)}" text-anchor="middle" font-size="5" fill="#000000" stroke="none">HV</text>'


def buf_mark(x, y):
    return f'<path d="M{r(x-3)},{r(y-4)} L{r(x+4)},{r(y)} L{r(x-3)},{r(y+4)} Z"/>'


def hyst_mark(x, y):
    return (f'<polyline points="{r(x-6)},{r(y+3)} {r(x+2)},{r(y+3)} {r(x+2)},{r(y-3)} {r(x+6)},{r(y-3)}"/>'
            f'<polyline points="{r(x-6)},{r(y+3)} {r(x-2)},{r(y+3)} {r(x-2)},{r(y-3)} {r(x+6)},{r(y-3)}"/>')


# ---- ND bodies ---------------------------------------------------------------------------------
def nd_body(base, h):
    """Return (svg, x where input lines meet the body, x where the output line starts)."""
    b, m = h - T, h / 2
    box = lambda x0, x1: f'<rect x="{x0}" y="{T}" width="{x1 - x0}" height="{r(b - T)}"/>'
    xline = lambda x0, x1: f'<line x1="{x0}" y1="{T}" x2="{x1}" y2="{r(b)}"/><line x1="{x0}" y1="{r(b)}" x2="{x1}" y2="{T}"/>'
    tri = lambda x0, x1: f'<polyline points="{x0},{T} {x1},{r(m)} {x0},{r(b)}"/>'
    vline = lambda x: f'<line x1="{x}" y1="{T}" x2="{x}" y2="{r(b)}"/>'
    point = lambda x0, x1, tip: f'<path d="M{x0},{T} H{x1} L{tip},{r(m)} L{x1},{r(b)} H{x0} Z"/>'
    # the pointed end alone, open on its left: the box edge it sits against is the only line there
    tip_open = lambda x0, tip: f'<polyline points="{x0},{T} {tip},{r(m)} {x0},{r(b)}"/>'
    schmitt = f'<line x1="36" y1="{T}" x2="46" y2="{T}"/><line x1="10" y1="{r(b)}" x2="20" y2="{r(b)}"/>'
    if base == "nand":
        return box(20, 36), 20, 36
    if base == "nand-oc":
        return box(20, 36) + vline(28), 20, 36
    if base == "nor":
        return box(20, 36) + xline(20, 36), 20, 36
    if base == "not":
        return box(20, 36) + tri(20, 36), 20, 36
    if base == "not-oc":
        # 7405 cut-out: the diagonal runs from the bottom left corner to the middle of the right edge
        return box(20, 36) + f'<line x1="20" y1="{r(b)}" x2="36" y2="{r(m)}"/>', 20, 36
    if base == "and":
        return box(16, 40) + vline(28) + tri(28, 40), 16, 40
    if base in ("and-oc", "buf-ochv"):
        return box(16, 40) + vline(28) + f'<line x1="28" y1="{r(b)}" x2="40" y2="{r(m)}"/>', 16, 40
    if base == "or":
        return box(16, 40) + vline(28) + xline(16, 28) + tri(28, 40), 16, 40
    if base == "xor":
        return (box(20, 36) + vline(28) + f'<line x1="25" y1="{r(m - 2)}" x2="31" y2="{r(m - 2)}"/>'
                f'<line x1="25" y1="{r(m + 2)}" x2="31" y2="{r(m + 2)}"/>'), 20, 36
    if base == "nand-schmitt":
        return box(20, 36) + schmitt, 20, 36
    if base == "not-schmitt":
        return box(20, 36) + tri(20, 36) + schmitt, 20, 36
    if base == "nand-buf":
        return point(16, 30, 38), 16, 38
    if base == "nor-buf":
        # 7428 cut-out: the X runs into all four corners of the box; the box's right edge is the
        # bar; the point follows.
        return point(16, 30, 38) + xline(16, 30) + vline(30), 16, 38
    if base == "nor-buf-oc":
        # 7433 cut-out: the 7428 symbol (X into all four corners, box edge, point) plus a vertical
        # line down the centre of the box - the same centre-line open-collector mark as 7401.
        return point(16, 30, 38) + xline(16, 30) + vline(30) + vline(23), 16, 38
    if base == "nand-buf-oc":
        # 7438 cut-out: the bar sits about a third of the way in from the left; no line where the
        # point starts
        return point(16, 30, 38) + vline(23), 16, 38
    if base == "not-buf-ochv":
        # 7406 cut-out: the diagonal runs from the bottom left corner to the tip of the point
        return point(20, 32, 40) + f'<line x1="20" y1="{r(b)}" x2="40" y2="{r(m)}"/>', 20, 40
    if base == "not-buf-ochv-bar":
        # 7416 cut-out: the bar sits about a third of the way in from the left; no line where the
        # point starts
        return point(20, 31, 38) + vline(26), 20, 38
    if base == "line-driver":
        return f'<path d="M16,{T} L44,{r(m)} L16,{r(b)} Z"/>', 16, 44
    raise SystemExit("no ND body for " + base)


# ---- modern bodies -----------------------------------------------------------------------------
def mod_body(base, h, ys):
    """Return (svg, list of x where each input meets the body, x where the output line starts)."""
    b, m = h - T, h / 2
    rad = (b - T) / 2
    n = len(ys)

    def and_shape(x0):
        return f'<path d="M{x0},{T} H{x0 + 10} A{r(rad)},{r(rad)} 0 0 1 {x0 + 10},{r(b)} H{x0} Z"/>', x0 + 10 + rad

    def or_shape(x0):
        xr = x0 + 14 + rad
        svg = (f'<path d="M{x0},{T} Q{r(x0 + (xr - x0) * 0.7)},{T} {r(xr)},{r(m)} Q{r(x0 + (xr - x0) * 0.7)},{r(b)} {x0},{r(b)} '
               f'Q{x0 + 8},{r(m)} {x0},{T} Z"/>')
        back = [x0 + 16 * ((y - T) / (b - T)) * (1 - (y - T) / (b - T)) for y in ys]
        return svg, back, xr

    def tri(x0, x1):
        return f'<path d="M{x0},{r(m - 12)} L{x1},{r(m)} L{x0},{r(m + 12)} Z"/>'

    bubble = lambda x: f'<circle cx="{r(x + 3)}" cy="{r(m)}" r="3"/>'
    if base in ("and", "and-oc", "nand", "nand-oc", "nand-schmitt", "nand-buf", "nand-buf-oc"):
        svg, xr = and_shape(12)
        if base.startswith("nand"):
            svg += bubble(xr)
            out = xr + 6
        else:
            out = xr
        if base.endswith("-oc"):
            svg += oc_mark(20, m - 2)
        if base == "nand-schmitt":
            svg += hyst_mark(22, m)
        if base in ("nand-buf", "nand-buf-oc"):
            svg += buf_mark(28 if base == "nand-buf-oc" else 22, m)
        return svg, [12] * n, out
    if base in ("or", "nor", "nor-buf", "nor-buf-oc"):
        svg, back, xr = or_shape(10)
        out = xr
        if base != "or":
            svg += bubble(xr)
            out = xr + 6
        if base == "nor-buf":
            svg += buf_mark(24, m)
        if base == "nor-buf-oc":
            svg += buf_mark(28, m) + oc_mark(20, m - 2)
        return svg, back, out
    if base == "xor":
        svg, back, xr = or_shape(15)
        svg += f'<path d="M10,{T} Q18,{r(m)} 10,{r(b)}"/>'
        back = [10 + 16 * ((y - T) / (b - T)) * (1 - (y - T) / (b - T)) for y in ys]
        return svg, back, xr
    if base in ("not", "not-oc", "not-schmitt", "not-buf-ochv", "not-buf-ochv-bar", "buf-ochv", "line-driver"):
        svg = tri(16, 40)
        out = 40
        if base != "buf-ochv":
            svg += bubble(40)
            out = 46
        if base in ("not-oc", "not-buf-ochv", "not-buf-ochv-bar", "buf-ochv"):
            svg += oc_mark(24, m - 2)
        if base in ("not-buf-ochv", "not-buf-ochv-bar", "buf-ochv"):
            svg += hv_mark(28, m + 18)  # below the triangle, clear of its edge
        if base == "not-schmitt":
            svg += hyst_mark(25, m)
        return svg, [16] * n, out
    raise SystemExit("no modern body for " + base)


def labels(pins):
    """Pin names as the manual prints them, just above the outer end of each pin line. They come
    from the pin table, so they cannot disagree with the lines."""
    out = []
    for p in pins:
        if p["dir"] == "in":
            out.append(f'<text x="{r(p["x"] + 1)}" y="{r(p["y"] - 1.5)}" data-label="{p["pin"]}">{p["pin"]}</text>')
        else:
            out.append(f'<text x="{r(p["x"] - 1)}" y="{r(p["y"] - 1.5)}" text-anchor="end" data-label="{p["pin"]}">{p["pin"]}</text>')
    return "\n".join(out)


def svg_doc(w, h, body, stubs, title, pins):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">\n'
            f'<title>{title}</title>\n<g fill="none" stroke="#000000" stroke-width="1.2">\n'
            + body + "\n" + "\n".join(stubs) + "\n</g>\n"
            + '<g fill="#000000" font-family="sans-serif" font-size="5">\n' + labels(pins) + "\n</g>\n</svg>\n")


def stub(name, x1, y1, x2, y2):
    return f'<line data-pin="{name}" x1="{r(x1)}" y1="{r(y1)}" x2="{r(x2)}" y2="{r(y2)}"/>'


def simple(base, n, second=None):
    """A gate with n inputs on the left and one output on the right."""
    h = height_for(n)
    ys = input_ys(n, h)
    m = h / 2
    nd, nd_in, nd_out = nd_body(base, h)
    mo, mo_in, mo_out = mod_body(base, h, ys)
    w = max(60, int(mo_out) + 8)
    pins = [{"pin": p, "x": 0, "y": r(y), "dir": "in"} for p, y in zip(names(n, second), ys)]
    pins.append({"pin": "Y", "x": w, "y": r(m), "dir": "out"})
    nd_stubs = [stub(p["pin"], nd_in, p["y"], 0, p["y"]) for p in pins[:-1]] + [stub("Y", nd_out, m, w, m)]
    mo_stubs = [stub(p["pin"], mo_in[i], p["y"], 0, p["y"]) for i, p in enumerate(pins[:-1])] + [stub("Y", mo_out, m, w, m)]
    return w, h, pins, nd, nd_stubs, mo, mo_stubs


def strobe4():
    """7425: four inputs plus G coming up into the bottom of the box."""
    w, h0, pins, nd, nd_stubs, mo, mo_stubs = simple("nor", 4)
    h = h0 + 14
    gy = h - 4
    b = h0 - T
    pins.insert(4, {"pin": "G", "x": 0, "y": gy, "dir": "in"})
    nd += f'<line x1="28" y1="{r(b)}" x2="28" y2="{gy}"/>'
    nd_stubs.append(stub("G", 28, gy, 0, gy))
    # modern: the NOR is drawn as OR + NAND with G, Y = NOT(G AND (A OR B OR C OR D))
    m = h0 / 2
    mo, back, xr = mod_body("or", h0, input_ys(4, h0))[0], None, None
    _, back, xr = mod_body("or", h0, input_ys(4, h0))
    xr = xr  # OR tip
    mo += (f'<polyline points="{r(xr)},{r(m)} {r(xr + 3)},{r(m)} {r(xr + 3)},{r(m - 5)} {r(xr + 6)},{r(m - 5)}"/>'
           f'<polyline points="{r(xr + 4)},{gy} {r(xr + 4)},{r(m + 5)} {r(xr + 6)},{r(m + 5)}"/>'
           f'<path d="M{r(xr + 6)},{r(m - 10)} H{r(xr + 10)} A10,10 0 0 1 {r(xr + 10)},{r(m + 10)} H{r(xr + 6)} Z"/>'
           f'<circle cx="{r(xr + 23)}" cy="{r(m)}" r="3"/>')
    w = max(w, int(xr + 26) + 6)
    for p in pins:
        if p["pin"] == "Y":
            p["x"] = w
    out_x = xr + 26
    mo_stubs = [stub(p["pin"], back[i], p["y"], 0, p["y"]) for i, p in enumerate(pins[:4])]
    mo_stubs += [stub("G", xr + 4, gy, 0, gy), stub("Y", out_x, m, w, m)]
    nd_stubs = [s.replace(f'x2="60"', f'x2="{w}"') if 'data-pin="Y"' in s else s for s in nd_stubs]
    return w, h, pins, nd, nd_stubs, mo, mo_stubs


def hex_buffer():
    """7417: the manual draws the whole package, six buffers in one tall box."""
    w, h = 60, 120
    ys = [10 + 20 * i for i in range(6)]
    pins = [{"pin": c, "x": 0, "y": y, "dir": "in"} for c, y in zip("ABCDEF", ys)]
    pins += [{"pin": "Y" + c, "x": w, "y": y, "dir": "out"} for c, y in zip("ABCDEF", ys)]
    nd = '<rect x="22" y="2" width="16" height="116"/>'
    nd_stubs = [stub(c, 22, y, 0, y) for c, y in zip("ABCDEF", ys)] + [stub("Y" + c, 38, y, w, y) for c, y in zip("ABCDEF", ys)]
    mo = "".join(f'<path d="M22,{y - 7} L38,{y} L22,{y + 7} Z"/>' + oc_mark(27, y - 1).replace("+5", "")
                 + f'<text x="44" y="{y - 2}" font-size="4" fill="#000000" stroke="none">HV</text>' for y in ys)
    mo_stubs = [stub(c, 22, y, 0, y) for c, y in zip("ABCDEF", ys)] + [stub("Y" + c, 38, y, w, y) for c, y in zip("ABCDEF", ys)]
    return w, h, pins, nd, nd_stubs, mo, mo_stubs


def aoi_2x2():
    """7450/7451/7460: top AND box against the X box, bottom AND box feeds the X box from below."""
    w, h = 70, 90  # wider than 60 so the Y label clears the output circle
    top = [6, 14, 22, 30]
    bot = [56, 64, 72, 80]
    pins = [{"pin": c, "x": 0, "y": y, "dir": "in"} for c, y in zip("ABCDEFGH", top + bot)]
    pins.append({"pin": "Y", "x": w, "y": 18, "dir": "out"})
    nd = ('<rect x="18" y="2" width="10" height="32"/><rect x="28" y="2" width="10" height="32"/>'
          '<line x1="28" y1="2" x2="38" y2="34"/><line x1="28" y1="34" x2="38" y2="2"/>'
          '<rect x="18" y="50" width="10" height="34"/><polyline points="28,67 33,67 33,34"/>'
          # 7450 cut-out, read at full size: the top-left box is split by a horizontal line (A,B
          # above, C,D below), and a short vertical line joins its bottom to the top of the lower box.
          '<line x1="18" y1="18" x2="28" y2="18"/><line x1="23" y1="34" x2="23" y2="50"/>')
    nd_stubs = [stub(c, 18, y, 0, y) for c, y in zip("ABCDEFGH", top + bot)] + [stub("Y", 38, 18, w, 18)]
    mo = ('<path d="M10,4 H16 A14,14 0 0 1 16,32 H10 Z"/><path d="M10,54 H16 A14,14 0 0 1 16,82 H10 Z"/>'
          '<polyline points="30,18 33,18 33,13 39,13"/><polyline points="30,68 35,68 35,23 39,23"/>'
          '<path d="M36,8 Q48,8 54,18 Q48,28 36,28 Q40,18 36,8 Z"/><circle cx="56.5" cy="18" r="2.5"/>')
    mo_stubs = [stub(c, 10, y, 0, y) for c, y in zip("ABCDEFGH", top + bot)] + [stub("Y", 59, 18, w, 18)]
    return w, h, pins, nd, nd_stubs, mo, mo_stubs


def write(kind, spec):
    w, h, pins, nd, nd_stubs, mo, mo_stubs = spec
    json.dump({"catalogFormat": "nd-symbol-pins", "catalogFormatVersion": 1, "kind": kind,
               "width": w, "height": h, "pins": pins},
              open(f"catalog/symbols/pins/{kind}.json", "w", newline="\n"), indent=2)
    open(f"catalog/symbols/nd1976/shapes/{kind}.svg", "w", newline="\n").write(svg_doc(w, h, nd, nd_stubs, f"{kind}, ND-13.003.01 style", pins))
    open(f"catalog/symbols/modern/shapes/{kind}.svg", "w", newline="\n").write(svg_doc(w, h, mo, mo_stubs, f"{kind}, modern style", pins))


if __name__ == "__main__":
    import sys
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from shape_map import shapes_needed
    # This script owns these three folders completely: clear them so no stale shape survives.
    for d in ("catalog/symbols/pins", "catalog/symbols/nd1976/shapes", "catalog/symbols/modern/shapes"):
        os.makedirs(d, exist_ok=True)
        for f in glob.glob(d + "/*"):
            os.remove(f)
    done = []
    for kind, base, n, second in shapes_needed():
        if base == "nor-strobe":
            write(kind, strobe4())
        elif base == "hex-buf-ochv":
            write(kind, hex_buffer())
        elif base == "aoi-2x2":
            write(kind, aoi_2x2())
        else:
            write(kind, simple(base, n, second))
        done.append(kind)
    print(len(done), "shapes:", " ".join(done))
