#!/bin/sh
# usage: [FIX4=1 ...] p64.sh <round dir>   page 64 only, hand detector det-L2; prints pins + stage loss
R="${RECOG_WORK:-.}"   # the work folder (models, pages, runs); not in the repository
cd "$R"; o=loop/$1/p64; mkdir -p $o; K=/e/Dev/RCL/ND-Schematics/src/sheets/nord-10/2A50-sync-modem-buffer-1050.rsc
BOX2=1 FIX1=1 FIX2=1 FIX3=1 DETTHR=0.3 DET=model/det-L2.onnx DETMODE=add python -I scripts/draft.py out/p64-300.png model/letters-A.onnx /e/Dev/RCL/ND-Schematics/src/catalog $o 2A50 >$o/log.txt 2>&1
python -I scripts/score.py $o/2A50-draft.rsc $o/2A50-dump.json $K out/p64-300.png $o/errors.png $o/score.json > /dev/null
python -I -c "import json;n=json.load(open('$o/score.json'))['nets'];print('p64 right',n['pins_in_right_net'],'wrong',n['pins_in_wrong_net'])"
python -I scripts/stageloss.py $K $o/2A50-dump.json $o/2A50-draft.rsc
