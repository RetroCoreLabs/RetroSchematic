#!/bin/sh
# usage: splitall.sh <round dir>  -> code-reading split summed over the work pages, and per page
R="${RECOG_WORK:-.}"   # the work folder (models, pages, runs); not in the repository
cd "$R"; o=loop/$1; : > $o/split.jsonl
for p in n10-61 n10-62 n10-63 n10-65 n10-66 binder8 binder96 iosys228 iosys237 nd12041 scsi6 scsi13 scsi21; do
 k=loop/key/$p.json; [ -f $k ] || k=loop/testkey/$p.json
 echo "{\"page\":\"$p\",\"s\":$(python -I scripts/codesplit.py $k $o/$p/$p-dump.json)}" >> $o/split.jsonl; done
python -I -c "
import json
T={};S={}
for l in open('$o/split.jsonl'):
  d=json.loads(l);s=d['s'];print(d['page'],{k:s.get(k,0) for k in ('symbols','right','wrong-symbol','misread','no box')})
  t=S if d['page'].startswith('scsi') else T
  for k,v in s.items(): t[k]=t.get(k,0)+v
print('WORK',T);print('SCSI',S)"
