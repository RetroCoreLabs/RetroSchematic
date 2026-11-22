"""Build catalog/symbols/nd1976 from the ND-13.003.01 symbol manual.

Run from the src folder:  python -I tools/build-symbol-catalog.py
Needs PyMuPDF (pip install pymupdf). Writes one <id>.png cut-out and one <id>.json per part.

The cut-outs are found from the position of each part number in the PDF's OCR text layer. Where
the OCR misread the number, ALIAS gives the word it actually holds; where the number is not in the
text layer at all, FRACTION gives the heading's height on the page, measured by eye on the page
images (2026-11-23). Every cut-out was checked by eye afterwards.
"""
import json, os, re, sys
import fitz

MANUAL = "docs/sources/manuals/ND-13.003.01-Circuit-Diagram-Symbols-1976.pdf"
OUT = "catalog/symbols/nd1976"
PAGE_OFFSET = 6   # printed page n is PDF page n + 6

ALIAS = {"74H54": "TLHSA4", "74S64": "74564", "74L95": "74195", "74S133": "748133",
         "74157": "7415774158", "MC4007": "MC", "MCT2": "MCT", "TIL113": "TIL", "VP5": "VP",
         "TMS4030": "TMS", "TMS6010": "TMS", "MK4096P": "MK", "2107B": "2107"}
FRACTION = {"74S112": .39, "74S114": .65, "74S258": .50, "74S280": .63, "75109": .68,
            "75452": .73, "VP12": .53, "DC1035T": .11}


def norm(s):
    return re.sub(r"[^0-9A-Z]", "", s.upper())


def main():
    doc = fitz.open(MANUAL)
    entries = [l.rstrip("\n").split("|") for l in open("tools/symbol-manual-entries.txt", encoding="utf-8") if l.strip()]
    gates = json.load(open("tools/gate-symbols.json", encoding="utf-8"))
    os.makedirs(OUT, exist_ok=True)
    by_page = {}
    for e in entries:
        by_page.setdefault(int(e[0]), []).append(e)
    count = 0
    for printed, es in by_page.items():
        page = doc[printed + PAGE_OFFSET - 1]
        width, height = page.rect.width, page.rect.height
        words = page.get_text("words")
        ys = []
        for e in es:
            key = norm(e[1].split("/")[0])
            if key in FRACTION:
                ys.append(FRACTION[key] * height)
                continue
            want = ALIAS.get(key, key)
            found = [w[1] for w in words if norm(w[4]) == want]
            if not found:
                sys.exit(f"heading {e[1]} not found on printed page {printed}")
            ys.append(found[0])
        order = sorted(range(len(es)), key=lambda i: ys[i])
        for j, i in enumerate(order):
            printed_s, ident, title = es[i]
            key = norm(ident.split("/")[0])
            y0 = max(0, ys[i] - 12)
            y1 = ys[order[j + 1]] - 12 if j + 1 < len(es) else height * 0.92
            page.get_pixmap(dpi=150, clip=fitz.Rect(width * 0.08, y0, width * 0.95, y1),
                            colorspace=fitz.csGRAY).save(f"{OUT}/{key}.png")
            entry = {
                "catalogFormat": "nd-symbol", "catalogFormatVersion": 1,
                "id": key, "partNumber": ident, "title": title,
                "source": {"document": MANUAL, "printedPage": printed, "pdfPage": printed + PAGE_OFFSET},
                "image": f"{key}.png",
            }
            g = gates.get(ident) or gates.get(key)
            if g:
                entry["symbolType"] = "gate"
                entry.update({k: v for k, v in g.items()})
                entry["confidence"] = "read"
            else:
                entry["symbolType"] = "block"
                entry["description"] = ("A rectangle for the whole function (or one section of it). Pin numbers are "
                                        "written on the lines where they meet the box; signal names are written beside them.")
                entry["tells"] = "Read the part type from the parts list and compare the pin numbers against this cut-out."
                entry["pinsAsDrawn"] = "open"
                entry["confidence"] = "read"
            json.dump(entry, open(f"{OUT}/{key}.json", "w", encoding="utf-8", newline="\n"), indent=2)
            count += 1
    print(f"{count} parts written to {OUT}")


if __name__ == "__main__":
    main()
