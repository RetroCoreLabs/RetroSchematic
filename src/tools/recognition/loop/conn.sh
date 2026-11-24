#!/bin/sh
# usage: conn.sh <round dir>  -> connection score (p64, work) and pin-number failure split
R="${RECOG_WORK:-.}"   # the work folder (models, pages, runs); not in the repository
cd "$R"; o=loop/$1; K=/e/Dev/RCL/ND-Schematics/src/sheets/nord-10/2A50-sync-modem-buffer-1050.rsc
echo "p64  $(python -I scripts/connscore.py $o/p64/2A50-draft.rsc $K)  $(python -I scripts/pinfail.py $K $o/p64/2A50-dump.json)"
A=""; B=""; for p in n10-61 n10-62 n10-63 n10-65 n10-66 binder8 binder96 iosys228 iosys237 nd12041; do A="$A $o/$p/$p-draft.rsc $o/$p/sc/key-as-rsc.json"; B="$B loop/key/$p.json $o/$p/$p-dump.json"; done
echo "work $(python -I scripts/connscore.py $A)  $(python -I scripts/pinfail.py $B)"
