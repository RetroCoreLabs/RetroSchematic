#!/bin/sh
# usage: [DETH=hand model] [DETC=CAD model] [THR=0.3] [LET=letters.onnx] draftall.sh <round dir> [pages...]
# The detector is picked per page by the trained page-style rule (style/pagestyle.json).
R="${RECOG_WORK:-.}"   # the work folder (models, pages, runs); not in the repository
cd "$R"; o=loop/$1; shift
P=${*:-"n10-61 n10-62 n10-63 n10-65 n10-66 binder8 binder96 iosys228 iosys237 nd12041 scsi6 scsi13 scsi21"}
dm() { if [ "$(python -I scripts/stylekey.py $1)" = "1" ]; then echo ${DETC:-model/det-A.onnx}; else echo ${DETH:-model/det-A.onnx}; fi; }
for p in $P; do mkdir -p $o/$p
 B=nord10; case $p in n10-*) ;; *) B=nd100;; esac
 BOARD=${GRIDFIX:+$B} BOX2=1 FIX1=1 FIX2=1 FIX3=1 DETTHR=${THR:-0.3} DET=$(dm $p) DETMODE=add python -I scripts/draft.py loop/pages/$p.png ${LET:-model/letters-A.onnx} /e/Dev/RCL/ND-Schematics/src/catalog $o/$p $p > $o/$p/log.txt 2>&1 || echo FAIL $p
done
mkdir -p $o/p64
BOX2=1 FIX1=1 FIX2=1 FIX3=1 DETTHR=${THR:-0.3} DET=$(dm p64) DETMODE=add python -I scripts/draft.py out/p64-300.png ${LET:-model/letters-A.onnx} /e/Dev/RCL/ND-Schematics/src/catalog $o/p64 2A50 >/dev/null 2>&1
python -I scripts/score.py $o/p64/2A50-draft.rsc $o/p64/2A50-dump.json /e/Dev/RCL/ND-Schematics/src/sheets/nord-10/2A50-sync-modem-buffer-1050.rsc out/p64-300.png $o/p64/errors.png $o/p64/score.json > /dev/null
