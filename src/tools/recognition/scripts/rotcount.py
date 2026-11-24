"""Turn finder helper: number of letter-sized pieces (pageio.pieces_of, no cap) on the page as it is
and turned a quarter. Upright letters are taller than wide, so the turn whose text runs across gives
more pieces in the letter-height band. Usage: python -I rotcount.py <rot2.json>  (adds 'n_all')"""
import site, sys, os, json
sys.path.append(site.getusersitepackages()); sys.path.insert(0, os.path.dirname(__file__))
import cv2
from books import BOOKS
from pageio import render_norm, pieces_of
j = sys.argv[1]; r = json.load(open(j))
for k in r:
    b, p = k.split(":")
    g, _ = render_norm(BOOKS[b][0], int(p))
    r[k]["n_all"] = {"0": len(pieces_of(g)), "90": len(pieces_of(cv2.rotate(g, cv2.ROTATE_90_CLOCKWISE)))}
    print(k, r[k]["n_all"], flush=True)
json.dump(r, open(j, "w"))
