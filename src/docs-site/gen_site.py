"""Writes the generated part of the documentation site, from the files git tracks.

    python -I docs-site/gen_site.py            # run from the src folder: writes the files
    python -I docs-site/gen_site.py --check    # writes nothing; fails when a file is out of date

What it writes:

  docs/reference/index.md          the overview of the reference pages
  docs/reference/chips/index.md    a table of every chip in catalog/chips
  docs/reference/chips/<id>.md     one page per chip file: symbol drawings, pins, functions,
                                   state, timing, sources, the sheets that use it, and the JSON
  docs/reference/signals/<id>.md   one page per bus signal file in catalog/signals
  docs-site/mkdocs.yml             INHERIT: base.yml plus the page tree (nav)

Everything comes from the catalogue JSON files and the sheet files. Nothing here is typed by
hand except the policy below: which guide goes in which section of the page tree, and which
files stay off the site.

The page tree lists only files git tracks (git ls-files), so an untracked note in the working
tree can never reach the strict CI build. A tracked Markdown file the policy does not name goes
into the "More" section, and the script says so on stderr, so a new guide is never silently left
out.
"""
import json
import os
import subprocess
import sys

SRC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROOT = os.path.dirname(SRC)
REPO_URL = "https://github.com/RetroCoreLabs/RetroSchematic"
BLOB = REPO_URL + "/blob/master/"

sys.path.insert(0, os.path.join(SRC, "tools"))
from shape_map import shape_for  # noqa: E402  (the same shape names the symbol catalogue uses)

# ---------------------------------------------------------------------------------------------
# Policy: the page tree. Paths are relative to the repository root. The generated reference
# pages are added by the script (section "Reference").
# ---------------------------------------------------------------------------------------------
SECTIONS = [
    ("Using RetroSchematic", [
        "src/RetroSchematic.Desktop/README.md",
        "src/docs/READING-A-CARD.md",
        "src/docs/IMPORT-WORKFLOW.md",
        "src/docs/ADDING-A-SYMBOL.md",
    ]),
    ("File formats", [
        "src/docs/FORMAT.md",
        "src/catalog/chips/FORMAT.md",
        "src/docs/BUS-SIGNALS.md",
        "src/tools/block-drawings/FORMAT.md",
    ]),
    ("NORD-10", [
        "src/docs/NORD10-BACKPLANE.md",
        "src/docs/NORD10-EDGE-FINGERS.md",
        "src/docs/2A50-reading.md",
        "src/docs/2A50-catalogue-map.md",
        "src/docs/2A50-interrupts.md",
        "src/docs/2A50-passives-and-pins.md",
    ]),
    ("Recognition", [
        "src/tools/recognition/README.md",
        "src/docs/RECOGNITION-LEARNINGS.md",
        "src/models/README.md",
    ]),
]
# Shown inside the Reference section, after the generated pages.
REFERENCE_GUIDES = [
    ("Symbol catalogue (ND-13.003.01)", "src/docs/SYMBOL-CATALOG.md"),
    ("Card drawings we have", "src/docs/LIBRARY-INVENTORY.md"),
]
# Kept off the site: the plan and the bug list are working lists, the rename note is a dated
# one-off record. Links to them from site pages go to the file on GitHub (docs-site/hooks.py).
# The .github files are not in the site either (MkDocs skips folders that start with a dot);
# links to them also go to GitHub.
OFF_SITE = {
    "src/docs/PLAN.md",
    "src/docs/BUGS.md",
    "src/docs/pin-rename-2026-11-23.md",
    ".github/CONTRIBUTING.md",
    ".github/SECURITY.md",
}

# ---------------------------------------------------------------------------------------------


def tracked(pattern=None):
    args = ["git", "-C", ROOT, "ls-files", "-z"]
    if pattern:
        args.append(pattern)
    out = subprocess.run(args, check=True, capture_output=True).stdout.decode("utf-8")
    return sorted(p for p in out.split("\0") if p)


def load(path):
    with open(os.path.join(ROOT, path), encoding="utf-8") as f:
        return json.load(f)


def cell(v):
    """A value for a table cell. Empty stays empty - never a placeholder."""
    if v is None:
        return ""
    if isinstance(v, bool):
        return "yes" if v else "no"
    if isinstance(v, (list, tuple)):
        v = ", ".join(str(x) for x in v)
    return (str(v).replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
            .replace("|", "\\|").replace("\n", " "))


def table(head, rows):
    """A Markdown table; columns that are empty on every row are left out."""
    keep = [i for i in range(len(head)) if any(str(r[i]).strip() for r in rows)]
    out = ["| " + " | ".join(head[i] for i in keep) + " |",
           "|" + "|".join("---" for _ in keep) + "|"]
    for r in rows:
        out.append("| " + " | ".join(str(r[i]) for i in keep) + " |")
    return out


def pin_name(p):
    return ("/" if p.get("inverted") else "") + p["name"]


def json_fold(title, path, data):
    """The source file in a fold-out, with a link to it on GitHub."""
    text = json.dumps(data, indent=2, ensure_ascii=False)
    lines = [f'??? abstract "{title}"', "", f"    [{path}]({BLOB}{path}) on GitHub", "",
             "    ```json"]
    lines += ["    " + t for t in text.split("\n")]
    lines += ["    ```", ""]
    return lines


# ---------------------------------------------------------------------------------------------
# Sheets: which chip types each sheet uses. The viewer finds a chip's catalogue file by the
# part type exactly (catalog/chips/<type>.json, RetroSchematic.Model/ChipCatalog.cs), so the
# same exact match is used here.
# ---------------------------------------------------------------------------------------------
def sheet_uses():
    uses = {}
    for path in tracked("src/sheets/*.rsc"):
        d = load(path)
        sheet = d.get("sheet", {})
        label = sheet.get("id") or os.path.basename(path)
        title = sheet.get("title") or ""
        for part in d.get("parts", []):
            t = part.get("type")
            if not t:
                continue
            uses.setdefault(t, {}).setdefault(path, {"label": label, "title": title, "refs": []})
            uses[t][path]["refs"].append(part.get("ref", ""))
    return uses


# ---------------------------------------------------------------------------------------------
# Chip pages
# ---------------------------------------------------------------------------------------------
def symbol_pictures(chip_id, files):
    """The cut-out from the 1976 symbol manual and the two drawn shapes, as far as they exist."""
    sym_path = f"src/catalog/symbols/nd1976/{chip_id}.json"
    if sym_path not in files:
        return []
    e = load(sym_path)
    if e.get("symbolType") == "block":
        shape = f"block-{chip_id}"
    else:
        shape = shape_for(e)[0]
    up = "../../../"
    cols = []
    missing = []
    cut = f"src/catalog/symbols/nd1976/{e.get('image', chip_id + '.png')}"
    src = e.get("source", {})
    if cut in files:
        cols.append((f"As printed in ND-13.003.01, page {src.get('printedPage', '')}",
                     f"![{chip_id} as printed in the 1976 symbol manual]({up}{cut[4:]})"))
    for style, label in (("nd1976", "Drawn in ND style"), ("modern", "Drawn in modern style")):
        png = f"src/catalog/symbols/{style}/shapes/{shape}.png"
        if png in files:
            cols.append((label, f"![{chip_id} {label.lower()}]({up}{png[4:]})"))
        else:
            print(f"gen_site: {chip_id}: no drawing {png}", file=sys.stderr)
            missing.append(png[4:])
    if not cols:
        return []
    out = ["## Symbol", ""]
    out += ["| " + " | ".join(c[0] for c in cols) + " |", "|" + "|".join("---" for _ in cols) + "|",
            "| " + " | ".join(c[1] for c in cols) + " |", ""]
    if missing:
        out += ["No drawn shape for this chip has been made yet (no file " +
                ", ".join(f"`{m}`" for m in missing) + ").", ""]
    if e.get("description"):
        out += [cell(e["description"]), ""]
    if e.get("tells"):
        out += [f"How to tell it on a drawing: {cell(e['tells'])}", ""]
    out += ["All symbols: [symbol catalogue](../../SYMBOL-CATALOG.md).", ""]
    return out


def pin_details(p):
    bits = []
    if p.get("edge"):
        bits.append(f"{p['edge']} edge")
    if p.get("async"):
        bits.append("acts at once (not clocked)")
    if p.get("controls"):
        bits.append("controls pins " + ", ".join(str(x) for x in p["controls"]))
    if p.get("enabledBy"):
        bits.append("enabled by " + cell(p["enabledBy"]))
    if p.get("whenInactive"):
        bits.append("when not enabled: " + cell(p["whenInactive"]))
    if p.get("schmitt"):
        bits.append("Schmitt trigger input")
    if p.get("voltage"):
        bits.append(cell(p["voltage"]))
    if p.get("text"):
        bits.append(cell(p["text"]))
    if p.get("source"):
        bits.append("source: " + cell(p["source"]))
    return "; ".join(bits)


def chip_page(path, d, files, uses):
    cid = d["id"]
    out = [f"<!-- generated by docs-site/gen_site.py from {path[4:]}; edit that file and regenerate -->",
           "", f"# {cid} - {cell(d.get('title'))}", ""]
    facts = [f"Package {cell(d.get('package'))}" if d.get("package") else "",
             f"family {cell(d.get('family'))}" if d.get("family") else ""]
    facts = [f for f in facts if f]
    out += [" · ".join(facts) + f" · catalogue file [{path[4:]}]({BLOB}{path})", ""]

    out += symbol_pictures(cid, files)

    pins = d.get("pins") or []
    if pins:
        out += ["## Pins", ""]
        out += table(["Pin", "Name", "Role", "Active", "Output type", "Details"],
                     [[cell(p.get("pin")), cell(pin_name(p)), cell(p.get("role")), cell(p.get("active")),
                       cell(p.get("outputType")), pin_details(p)] for p in pins])
        out += [""]

    fns = d.get("functions") or []
    if fns:
        out += ["## Functions", ""]
        out += table(["Output pin", "Formula", "What it does"],
                     [[cell(f.get("output")), cell(f.get("formula")), cell(f.get("text"))] for f in fns])
        out += [""]

    st = d.get("state")
    if st:
        out += ["## Stored state", ""]
        if st.get("bits") is not None:
            out += [f"Bits stored: {cell(st['bits'])}", ""]
        if st.get("changesOn"):
            out += [f"Changes on: {cell(st['changesOn'])}", ""]
        for r in st.get("rules") or []:
            out += [f"- {cell(r)}"]
        out += [""]

    el = d.get("electrical") or {}
    el_rows = [[cell(k), cell(v)] for k, v in el.items() if v not in (None, "")]
    if el_rows:
        out += ["## Electrical", ""] + table(["Value", "Figure"], el_rows) + [""]

    tm = d.get("timing") or []
    if tm:
        conds = {t.get("conditions") for t in tm}
        same = len(conds) == 1
        out += ["## Propagation delays", ""]
        if same and next(iter(conds)):
            out += [f"All measured at {cell(next(iter(conds)))}.", ""]
        out += table(["From", "To", "Edge", "Min ns", "Typ ns", "Max ns", "Conditions"],
                     [[cell(t.get("from")), cell(t.get("to")), cell(t.get("edge")), cell(t.get("minNs")),
                       cell(t.get("typNs")), cell(t.get("maxNs")), "" if same else cell(t.get("conditions"))]
                      for t in tm])
        out += [""]

    tl = d.get("timingLimits") or []
    if tl:
        out += ["## Timing limits", ""]
        out += table(["What", "Pin", "Against", "Min ns", "Min MHz", "Typ MHz"],
                     [[cell(t.get("what")), cell(t.get("pin")), cell(t.get("ref")), cell(t.get("minNs")),
                       cell(t.get("minMHz")), cell(t.get("typMHz"))] for t in tl])
        out += [""]

    unknown = d.get("unknown") or []
    if unknown:
        out += ["## Not known", ""] + [f"- {cell(u)}" for u in unknown] + [""]

    if d.get("notes"):
        out += ["## Notes", "", cell(d["notes"]), ""]

    srcs = d.get("sources") or []
    if srcs:
        out += ["## Sources", ""]
        for s in srcs:
            doc = cell(s.get("document"))
            if s.get("url"):
                doc = f"[{doc}]({s['url']})"
            extra = [cell(s.get(k)) for k in ("pages", "familyUsed") if s.get(k)]
            out += [f"- {doc}" + (f" - {'; '.join(extra)}" if extra else "")]
        out += [""]

    used = uses.get(cid, {})
    out += ["## Used on sheets", ""]
    if used:
        for spath, u in sorted(used.items()):
            refs = ", ".join(sorted(u["refs"]))
            out += [f"- [{cell(u['label'])}]({BLOB}{spath}) {cell(u['title'])}: {cell(refs)}"]
    else:
        out += ["No sheet in this repository uses this chip yet."]
    out += [""]

    out += json_fold(f"The catalogue file, {os.path.basename(path)}", path, d)
    return "\n".join(out)


def chip_index(chips, uses):
    out = ["<!-- generated by docs-site/gen_site.py from catalog/chips; edit those files and regenerate -->",
           "", "# Chips", "",
           f"Every chip in the chip catalogue (`catalog/chips`), {len(chips)} files. Each page holds the",
           "pins, functions and timing read from the datasheet, with the page they came from, the",
           "symbol drawings, and the sheets that use the chip. The file format is in",
           "[the chip catalogue format](../../../catalog/chips/FORMAT.md).", ""]
    rows = []
    for path, d in chips:
        cid = d["id"]
        n = sum(len(u["refs"]) for u in uses.get(cid, {}).values())
        rows.append([f"[{cid}]({cid}.md)", cell(d.get("title")), cell(d.get("package")), str(n) if n else ""])
    out += table(["Chip", "Title", "Package", "Parts on sheets"], rows)
    return "\n".join(out) + "\n"


# ---------------------------------------------------------------------------------------------
# Bus signal pages
# ---------------------------------------------------------------------------------------------
def pins_text(v):
    out = []
    for p in v or []:
        if not isinstance(p, dict):
            out.append(str(p))
            continue
        conn = p.get("connector")
        conn = "" if conn in (None, "open") else str(conn)
        place = f"{p.get('row', '')}{p.get('pin', '')}"
        out.append((conn + " " + place).strip())
    return ", ".join(out)


def val(field):
    if isinstance(field, dict) and "value" in field:
        return field["value"]
    return field


def signal_page(path, d):
    out = [f"<!-- generated by docs-site/gen_site.py from {path[4:]}; edit that file and regenerate -->",
           "", f"# {cell(d.get('title'))}", ""]
    out += [f"Machine {cell(d.get('machine'))} · catalogue file [{path[4:]}]({BLOB}{path}) · "
            "format and background in [Bus signals and connectors](../../BUS-SIGNALS.md)", ""]
    conv = d.get("conventions") or {}
    if conv:
        out += ["## How to read the table", ""]
        for k, v in conv.items():
            out += [f"- **{cell(k)}**: {cell(v)}"]
        out += [""]
    sigs = d.get("signals") or []
    groups = []
    for s in sigs:
        if s.get("group") not in groups:
            groups.append(s.get("group"))
    out += ["## Signals", ""]
    for g in groups:
        rows = []
        for s in sigs:
            if s.get("group") != g:
                continue
            rows.append([f"[{cell(s['name'])}](#{slug(s['name'])})", cell(pins_text(val(s.get("pins")))),
                         cell(val(s.get("active"))), cell(val(s.get("direction"))),
                         cell(val(s.get("description")))])
        out += [f"### {cell(g)}", ""]
        out += table(["Signal", "Pins", "Active", "Direction", "Description"], rows) + [""]

    out += ["## Every signal with its sources", "",
            "Each fact with the document it came from, the page, and how sure it is "
            "(read, inferred or open).", ""]
    for s in sigs:
        out += [f"### {cell(s['name'])}", ""]
        rows = []
        for k, v in s.items():
            if k in ("name",):
                continue
            rows += fact_rows(k, v)
        out += table(["Field", "Value", "Source", "Page", "Confidence", "Note"], rows) + [""]

    srcs = d.get("sources") or []
    if srcs:
        out += ["## Sources", ""]
        out += table(["Code", "Document", "Note"],
                     [[cell(s.get("id")), cell(s.get("document")), cell(s.get("note"))] for s in srcs])
        out += [""]
    out += json_fold(f"The catalogue file, {os.path.basename(path)}", path, d)
    return "\n".join(out)


def fact_rows(name, v):
    if isinstance(v, dict) and "value" in v:
        value = v["value"]
        if isinstance(value, list) and value and isinstance(value[0], dict) and name == "pins":
            value = pins_text(value)
        elif isinstance(value, (list, dict)):
            value = json.dumps(value, ensure_ascii=False)
        rows = [[cell(name), cell(value), cell(v.get("source")), cell(v.get("page")),
                 cell(v.get("confidence")), cell(v.get("note"))]]
        return rows
    if isinstance(v, dict):
        rows = []
        for k2, v2 in v.items():
            rows += fact_rows(f"{name} / {k2}", v2)
        return rows
    if isinstance(v, list):
        return [[cell(name), cell(", ".join(json.dumps(x, ensure_ascii=False) if isinstance(x, (dict, list)) else str(x) for x in v)), "", "", "", ""]]
    return [[cell(name), cell(v), "", "", "", ""]]


def slug(text):
    """The heading anchor MkDocs makes with pymdownx.slugs.slugify(case=lower), for plain names."""
    import re
    s = text.strip().lower()
    s = re.sub(r"[^\w\- ]", "", s)
    return s.replace(" ", "-")


# ---------------------------------------------------------------------------------------------
def reference_index(chips, signals):
    out = ["<!-- generated by docs-site/gen_site.py; edit the catalogue files and regenerate -->", "",
           "# Reference", "",
           "Pages made from the catalogue files in `catalog/`, so they always say what the files say.", "",
           f"- [Chips](chips/index.md) - {len(chips)} chips, one page each: pins, functions, timing,"
           " symbol drawings, sources and the sheets that use them.",
           ]
    for path, d in signals:
        out.append(f"- [{cell(d.get('title'))}](signals/{d['id']}.md) - {len(d.get('signals') or [])} signals"
                   " with their pins, levels and sources.")
    out += ["- [Symbol catalogue](../SYMBOL-CATALOG.md) - every part in ND-13.003.01"
            " \"Circuit Diagram Symbols\" (1976), with the cut-out and both drawings.",
            "- [Card drawings we have](../LIBRARY-INVENTORY.md) - which drawing books exist and the"
            " order to read the cards in.", ""]
    return "\n".join(out)


def nav_yaml(chips, signals, guides_more):
    def q(s):
        return json.dumps(s, ensure_ascii=False)
    lines = ["# generated by docs-site/gen_site.py - edit base.yml or the policy in gen_site.py,",
             "# then regenerate (see .github/CONTRIBUTING.md)",
             "INHERIT: base.yml", "nav:", "  - Home: README.md", "  - Reference:",
             "    - src/docs/reference/index.md",
             "    - Chips:", "      - src/docs/reference/chips/index.md"]
    for path, d in chips:
        lines.append(f"      - {q(d['id'])}: src/docs/reference/chips/{d['id']}.md")
    lines.append("    - Bus signals:")
    for path, d in signals:
        lines.append(f"      - {q(d.get('machine') or d['id'])}: src/docs/reference/signals/{d['id']}.md")
    for title, p in REFERENCE_GUIDES:
        lines.append(f"    - {q(title)}: {p}")
    for title, paths in SECTIONS:
        lines.append(f"  - {q(title)}:")
        for p in paths:
            lines.append(f"    - {p}")
    if guides_more:
        lines.append("  - More:")
        for p in guides_more:
            lines.append(f"    - {p}")
    return "\n".join(lines) + "\n"


def main():
    check = "--check" in sys.argv[1:]
    files = set(tracked())

    chips = [(p, load(p)) for p in sorted(files) if p.startswith("src/catalog/chips/") and p.endswith(".json")]
    chips.sort(key=lambda c: c[1]["id"])
    signals = [(p, load(p)) for p in sorted(files) if p.startswith("src/catalog/signals/") and p.endswith(".json")]
    uses = sheet_uses()

    # every tracked guide must be placed; a missing one is an error, an unplaced one goes to More
    named = {p for _, ps in SECTIONS for p in ps} | {p for _, p in REFERENCE_GUIDES} | {"README.md"}
    for p in sorted(named):
        if p not in files:
            sys.exit(f"gen_site: the page tree names {p}, which git does not track")
    more = []
    for p in sorted(files):
        if not p.endswith(".md") or p in named or p in OFF_SITE or p.startswith("src/docs/reference/"):
            continue
        print(f"gen_site: {p} is not placed in the page tree; listed under More", file=sys.stderr)
        more.append(p)

    want = {}
    want["src/docs/reference/index.md"] = reference_index(chips, signals)
    want["src/docs/reference/chips/index.md"] = chip_index(chips, uses)
    for p, d in chips:
        want[f"src/docs/reference/chips/{d['id']}.md"] = chip_page(p, d, files, uses)
    for p, d in signals:
        want[f"src/docs/reference/signals/{d['id']}.md"] = signal_page(p, d)
    want["src/docs-site/mkdocs.yml"] = nav_yaml(chips, signals, more)

    # files left over from a chip or signal file that is gone
    stale = [p for p in files if p.startswith("src/docs/reference/") and p not in want]

    bad = []
    for p, text in want.items():
        full = os.path.join(ROOT, p)
        old = None
        if os.path.exists(full):
            with open(full, encoding="utf-8", newline="") as f:
                old = f.read()
        if old != text:
            bad.append(p)
            if not check:
                os.makedirs(os.path.dirname(full), exist_ok=True)
                with open(full, "w", encoding="utf-8", newline="\n") as f:
                    f.write(text)
    for p in stale:
        bad.append(p)
        if not check:
            os.remove(os.path.join(ROOT, p))

    if check:
        if bad:
            for p in bad:
                print(f"gen_site: out of date: {p}", file=sys.stderr)
            sys.exit("gen_site: run python -I docs-site/gen_site.py from src and commit the result")
        print(f"gen_site: {len(want)} files up to date")
    else:
        print(f"gen_site: {len(want)} files, {len(bad)} written or removed")


if __name__ == "__main__":
    main()
