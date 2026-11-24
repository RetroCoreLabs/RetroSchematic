#!/bin/sh
# usage: run.sh <outdir> [env...]
R="${RECOG_WORK:-.}"   # the work folder (models, pages, runs); not in the repository
cd "$R"; d=draft/$1; mkdir -p $d
GATEKNN=model/gate-knn-c3.json python -I scripts/draft.py out/p64-300.png model/nd-letters-c3.onnx /e/Dev/RCL/ND-Schematics/src/catalog $d 2A50 >/dev/null && \
python -I scripts/score.py $d/2A50-draft.rsc $d/2A50-dump.json /e/Dev/RCL/ND-Schematics/src/sheets/nord-10/2A50-sync-modem-buffer-1050.rsc out/p64-300.png $d/errors.png $d/score.json > /dev/null && \
python -I -c "
import json;d=json.load(open('$d/score.json'));n=d['nets'];print('$1', n['pins_in_right_net'], n['pins_in_wrong_net'], n['share_pin_assignments_right'], d['parts']['ref_found'], d['hand_pins_missing_why'])"
