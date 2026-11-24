#!/bin/sh
# usage: env ... run2.sh <outdir>   page-64 draft with letters-A and score
R="${RECOG_WORK:-.}"   # the work folder (models, pages, runs); not in the repository
cd "$R"; d=draft/$1; mkdir -p $d
python -I scripts/draft.py out/p64-300.png model/letters-A.onnx /e/Dev/RCL/ND-Schematics/src/catalog $d 2A50 >/dev/null && \
python -I scripts/score.py $d/2A50-draft.rsc $d/2A50-dump.json /e/Dev/RCL/ND-Schematics/src/sheets/nord-10/2A50-sync-modem-buffer-1050.rsc out/p64-300.png $d/errors.png $d/score.json > /dev/null && \
python -I -c "
import json;d=json.load(open('$d/score.json'));n=d['nets'];print('$1', n['pins_in_right_net'], n['pins_in_wrong_net'], d['parts']['ref_found'], d['gates'] if 'gates' in d else '')"
