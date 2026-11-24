#!/bin/sh
# usage: scoreall.sh <round dir>  -> loop/<round>/scores.jsonl
R="${RECOG_WORK:-.}"   # the work folder (models, pages, runs); not in the repository
cd "$R"; o=loop/$1; : > $o/scores.jsonl
for p in n10-61 n10-62 n10-63 n10-65 n10-66 binder8 binder96 iosys228 iosys237 nd12041; do
 echo "{\"page\":\"$p\",\"s\":$(python -I scripts/loopscore.py loop/key/$p.json $o/$p/$p-dump.json $o/$p/$p-draft.rsc loop/pages/$p.png $o/$p/sc | tail -1)}" >> $o/scores.jsonl; done
for p in scsi6 scsi13 scsi21; do
 echo "{\"page\":\"$p\",\"s\":$(python -I scripts/loopscore.py loop/testkey/$p.json $o/$p/$p-dump.json $o/$p/$p-draft.rsc loop/pages/$p.png $o/$p/sc | tail -1)}" >> $o/scores.jsonl; done
python -I -c "
import json
n=json.load(open('$o/p64/score.json'))['nets'];print('p64 pins right/wrong',n['pins_in_right_net'],n['pins_in_wrong_net'])
T={}
for l in open('$o/scores.jsonl'):
  d=json.loads(l);s=d['s'];w=not d['page'].startswith('scsi')
  print(d['page'],'chips',s['chips_found'],'/',s['key_chips'],'gates',s['gates_found'],'/',s['key_gates'],'codes',s['codes_right'],'/',s['key_with_code'],'types',s['chip_types_right'],'kinds',s['gate_kinds_right'],'extras',s['extras'],'pins',s.get('pins_right'),'/',s.get('key_pins'),'wrong',s.get('pins_wrong'))
  if w:
    for k in ('chips_found','key_chips','gates_found','key_gates','codes_right','key_with_code','gate_kinds_right','extras','pins_right','key_pins','pins_wrong'): T[k]=T.get(k,0)+s.get(k,0)
print('WORK TOTAL',T)"
