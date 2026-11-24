#!/bin/sh
# usage: stageall.sh <round dir>  -> loop/<dir>/stage.jsonl (stage loss per page, keys from loop/key + loop/testkey)
R="${RECOG_WORK:-.}"   # the work folder (models, pages, runs); not in the repository
cd "$R"; o=loop/$1; : > $o/stage.jsonl
for p in n10-61 n10-62 n10-63 n10-65 n10-66 binder8 binder96 iosys228 iosys237 nd12041 scsi6 scsi13 scsi21; do
 k=loop/key/$p.json; [ -f $k ] || k=loop/testkey/$p.json
 echo "{\"page\":\"$p\",\"s\":$(python -I scripts/stageloss.py $k $o/$p/$p-dump.json $o/$p/$p-draft.rsc)}" >> $o/stage.jsonl; done
