#
# SPDX-License-Identifier: MIT
# Copyright (c) 1985-2026 Ronny Hansen
# RetroCore Labs - https://github.com/RetroCoreLabs
#
"""Reads a card's per-sheet reading files (docs/<card>-readings/pNN.txt), checks them and
merges them per block.

    python -I tools/card-readings.py check docs/3207-readings
    python -I tools/card-readings.py json docs/3207-readings out.json
    python -I tools/card-readings.py sheet docs/3207-readings sheets/nd-100/3207-scsi-floppy.rsc

The reading format is described in docs/3207-readings/README.md. The checks:
  - every pin names a part listed in the same block;
  - a pin is in only one net in its block (after the sheets of the block are merged by net name);
  - a part listed on two sheets has the same type on both, also across blocks, and a pin is used in one
    block only (one board position is one chip);
  - the pin number exists on the chip (from catalog/chips/<type>.json, or the plain 74xx type for
    the pin numbers only when the printed family such as 74LS00 has no file: same pins, other
    timing), and a supply pin is only on +5V / GND;
  - every name on a "port BLOCK NAME..." line (the signals drawn leaving a block box) is a net in that
    block;
  - a "connector REF CATALOG use..." line marks a part as a socket or header on the board: its pins
    may be named by a letter and a number (A1) and are not checked against a chip file; the sheet
    file stores it as connector J.REF with catalogue CATALOG, its pins as J.REF.a1;
  - a gate line names pins that are in nets, or listed on an "open" line (a pin that could not be
    settled from the drawing: left out on purpose, never guessed) or an "nc" line (drawn as a stub,
    not connected); such a pin must not be in a net.
Types with no catalogue file at all are listed, so they can be added from their datasheets.
"""
import json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
CHIPS = os.path.join(HERE, '..', 'catalog', 'chips')
SUPPLY = {'+5V', 'GND', '12V', '+12V', '-12V', '5VSTBY'}
FAMILY = re.compile(r'^74(ALS|AS|LS|HCT|HC|F|S|L|H|PCT)?(\d+)$')


def parse_file(path):
    """One reading file -> dict with block, page, parts, gates, nets, notes."""
    r = {'file': os.path.basename(path), 'block': None, 'page': None, 'parts': {}, 'boxes': {}, 'gates': [], 'nets': {}, 'notes': [], 'open': set(), 'nc': set(), 'ports': {}, 'connectors': {}}
    for n, raw in enumerate(open(path, encoding='utf-8'), 1):
        line = raw.split('#', 1)[0].strip() if not raw.lstrip().startswith('note ') else raw.strip()
        if not line:
            continue
        word, _, rest = line.partition(' ')
        rest = rest.strip()
        where = '%s:%d' % (r['file'], n)
        if word == 'block':
            r['block'] = rest
        elif word == 'page':
            r['page'] = rest
        elif word == 'part':
            f = rest.split()
            r['parts'][f[0]] = f[1]
            if len(f) >= 6:
                r['boxes'][f[0]] = [int(v) for v in f[2:6]]
        elif word == 'gate':
            f = rest.split()
            ins, out = f[2].split('>')
            r['gates'].append({'id': f[0], 'kind': f[1], 'inputs': [int(v) for v in ins.split(',')], 'output': int(out),
                               'box': [int(v) for v in f[3:7]] if len(f) >= 7 else None, 'where': where})
        elif word == 'net':
            name, _, pins = rest.partition(':')
            name = name.strip()
            lst = r['nets'].setdefault(name, [])
            for p in pins.split():
                lst.append((p, where))
        elif word == 'port':
            f = rest.split()
            r['ports'].setdefault(f[0], []).extend(f[1:])
        elif word == 'connector':
            f = rest.split(None, 2)
            r['connectors'][f[0]] = (f[1], f[2] if len(f) > 2 else '')
        elif word in ('open', 'nc'):
            r[word].update(rest.split())
        elif word == 'note':
            r['notes'].append(rest)
        else:
            raise SystemExit('%s: unknown line "%s"' % (where, word))
    if not r['block'] or not r['page']:
        raise SystemExit('%s: needs a block and a page line' % r['file'])
    return r


_pincache = {}


def chip_pins(ctype):
    """{pin number: role} from the catalogue, the file used, or (None, None)."""
    if ctype in _pincache:
        return _pincache[ctype]
    names = [ctype]
    m = FAMILY.match(ctype)
    if m:
        names.append('74' + m.group(2))
        names.append('74S' + m.group(2))
    found = (None, None)
    for nm in names:
        p = os.path.join(CHIPS, nm + '.json')
        if os.path.exists(p):
            d = json.load(open(p, encoding='utf-8'))
            found = ({int(x['pin']): x.get('role', '') for x in d['pins'] if str(x['pin']).isdigit()}, nm)
            break
    _pincache[ctype] = found
    return found


def is_passive(ctype):
    return bool(re.match(r'^\d+(\.\d+)?(PF|NF|UF|KOHM|OHM|MOHM)$', ctype)) or ctype.startswith('4610X')


def merge(files):
    """Merge the sheets per block; returns {block: {...}} and a list of problems."""
    blocks, problems = {}, []
    for r in files:
        b = blocks.setdefault(r['block'], {'parts': {}, 'partpage': {}, 'boxes': {}, 'nets': {}, 'gates': [], 'pages': [], 'notes': [], 'open': set(), 'nc': set(), 'connectors': {}})
        b['pages'].append(r['page'])
        b['open'] |= r['open']
        for ref, c in r['connectors'].items():
            b['connectors'][ref] = (c, r['page'])
        b['nc'] |= r['nc']
        b['notes'] += ['sheet %s: %s' % (r['page'], t) for t in r['notes']]
        for ref, t in r['parts'].items():
            if ref in b['parts'] and b['parts'][ref] != t:
                problems.append('%s: part %s is %s here but %s on sheet %s' % (r['file'], ref, t, b['parts'][ref], b['partpage'][ref]))
            b['parts'].setdefault(ref, t)
            b['partpage'].setdefault(ref, r['page'])
            if ref in r['boxes']:
                b['boxes'].setdefault(ref, (r['page'], r['boxes'][ref]))
        for name, pins in r['nets'].items():
            b['nets'].setdefault(name, []).extend(pins)
        for g in r['gates']:
            g['page'] = r['page']
            b['gates'].append(g)
    missing = set()
    for bn, b in blocks.items():
        owner = {}
        for name, pins in b['nets'].items():
            seen = set()
            for p, where in pins:
                if p in seen:
                    continue
                seen.add(p)
                ref, _, num = p.rpartition('.')
                if ref not in b['parts']:
                    problems.append('%s: %s in net %s: no part %s in block %s' % (where, p, name, ref, bn))
                    continue
                if p in owner and owner[p][0] != name:
                    problems.append('%s: %s is in net %s and in net %s (%s)' % (where, p, name, owner[p][0], owner[p][1]))
                owner.setdefault(p, (name, where))
                ctype = b['parts'][ref]
                if is_passive(ctype) or ref in b['connectors']:
                    continue
                pins_of, used = chip_pins(ctype)
                if pins_of is None:
                    missing.add(ctype)
                    continue
                k = int(num)
                if k not in pins_of:
                    problems.append('%s: %s: %s (%s) has no pin %d' % (where, p, ctype, used, k))
                elif pins_of[k] in ('power', 'ground') and name not in SUPPLY:
                    problems.append('%s: %s is a %s pin of %s but is in net %s' % (where, p, pins_of[k], ctype, name))
        for g in b['gates']:
            ref = g['id'].split('/')[0]
            if ref not in b['parts']:
                problems.append('%s: gate %s: no part %s' % (g['where'], g['id'], ref))
                continue
            for k in g['inputs'] + [g['output']]:
                if '%s.%d' % (ref, k) not in owner and '%s.%d' % (ref, k) not in b['open'] | b['nc']:
                    problems.append('%s: gate %s pin %d is in no net' % (g['where'], g['id'], k))
        for p in b['open'] | b['nc']:
            if p in owner:
                problems.append('%s is marked open/nc but is in net %s (%s)' % (p, owner[p][0], owner[p][1]))
        b['owner'] = owner
    # across blocks: a board position is one chip, so the same type everywhere, and each pin used once
    gtype, gpin = {}, {}
    for bn, b in blocks.items():
        for ref, t in b['parts'].items():
            if ref in gtype and gtype[ref][0] != t:
                problems.append('part %s is %s in block %s but %s in block %s' % (ref, t, bn, gtype[ref][0], gtype[ref][1]))
            gtype.setdefault(ref, (t, bn))
        for p, (name, where) in b['owner'].items():
            if name in SUPPLY:
                continue
            if p in gpin and gpin[p][0] != bn:
                problems.append('%s: pin %s is used in block %s (net %s) and in block %s (net %s)' % (where, p, bn, name, gpin[p][0], gpin[p][1]))
            gpin.setdefault(p, (bn, name))
    # a port line on the sheet that holds the block boxes names a signal that leaves the block: it must be a net there
    for r in files:
        for bn, names in r['ports'].items():
            if bn not in blocks:
                problems.append('%s: port list for block %s, which has no reading' % (r['file'], bn))
                continue
            for nm in names:
                if nm not in blocks[bn]['nets']:
                    problems.append('%s: port %s of block %s is not a net in that block' % (r['file'], nm, bn))
    # a gate is named REF/<output pin>, so one name is one gate everywhere
    gseen = {}
    for bn, b in blocks.items():
        for g in b['gates']:
            ref, _, num = g['id'].partition('/')
            if num != str(g['output']):
                problems.append('%s: gate %s must be named %s/%d (after its output pin)' % (g['where'], g['id'], ref, g['output']))
            sig = (tuple(g['inputs']), g['output'], g['kind'])
            if g['id'] in gseen and gseen[g['id']][0] != sig:
                problems.append('%s: gate %s differs from %s' % (g['where'], g['id'], gseen[g['id']][1]))
            gseen.setdefault(g['id'], (sig, g['where']))
    return blocks, problems, sorted(missing)


PASSIVE_R = re.compile(r'^\d*\.?\d+[KM]?OHM$')
PASSIVE_C = re.compile(r'^\d*\.?\d+(PF|NF|UF)$')
SUPPLY_NAMES = SUPPLY | {'5V'}


def sig(name, parts):
    """A reading name -> (stored name, active, printed). A tilde marks active low (BD~00 -> BD00,
    low, printed BD~00); an unnamed net REF_pin and a supply have no level; any other name is high."""
    if name in SUPPLY_NAMES:
        return name, 'none', None
    ref, _, num = name.rpartition('_')
    if ref in parts and num.isdigit():
        return name, 'none', None
    if '~' in name:
        return name.replace('~', ''), 'low', name
    return name, 'high', None


def port_name(name):
    """The port name for a signal: the tilde written as _0, the way the sheet's other ports are named."""
    return name.replace('~', '_0')


def direction(pins, parts):
    """(direction, confidence, note) of a port, from the catalogue roles of the chip pins on its net
    inside the block. Drivers outside the block are not seen, so this is INFERRED, never read."""
    roles = set()
    for p in pins:
        ref, _, num = p.rpartition('.')
        t = parts.get(ref)
        if t is None or is_passive(t):
            continue
        pins_of, _ = chip_pins(t)
        if pins_of and num.isdigit() and int(num) in pins_of:
            roles.add(pins_of[int(num)])
    why = 'from the catalogue roles of the chip pins on this signal inside the block; drivers outside the block are not counted'
    if 'output' in roles:
        return 'output', 'inferred', 'inferred ' + why + ' (a chip output drives it)'
    if roles & {'input', 'clock', 'enable'}:
        return 'input', 'inferred', 'inferred ' + why + ' (only chip inputs on it)'
    return 'inout', 'open', 'direction not known: no catalogued chip pin on this signal inside the block'


def pin_at(p, conns):
    """A reading pin as the sheet file writes it: a connector pin as J.REF.a1 (row letter in lower
    case, as the format wants), any other pin as it is."""
    ref, _, num = p.rpartition('.')
    if ref in conns:
        return 'J.%s.%s' % (ref, num[0].lower() + num[1:] if num[:1].isalpha() else num)
    return p


def part_entry(ref, t, page):
    e = {'ref': ref}
    if PASSIVE_R.match(t):
        e.update(type='R', value=t)
    elif PASSIVE_C.match(t):
        e.update(type='C', value=t)
    else:
        e['type'] = t
    e['confidence'] = 'read'
    e['page'] = page
    return e


def write_sheet(files, blocks, path):
    """Put every block that has a reading into the sheet file at path: its parts, gates, nets, pages
    and ports, and an instance of it in the block whose sheet draws its box (a reading with port lines).
    The top level, the board facts and the blocks with no reading are left as they are."""
    d = json.load(open(path, encoding='utf-8'))
    src_by_page = {}
    for i, s in enumerate(d.get('sources', []), 1):
        pg = s.get('original', {}).get('pdfPage')
        if pg:
            src_by_page[str(pg)] = i
    total = len(d.get('sources', []))
    existing = {b['name']: b for b in d.get('blocks', [])}
    parent_of = {}
    for r in files:
        for bn in r['ports']:
            parent_of[bn] = (r['block'], r['ports'][bn])
    out_blocks = []
    for bn, b in blocks.items():
        if bn in parent_of:
            parts = b['parts']
            blk = {'name': bn, 'ports': [], 'parts': [], 'gates': [], 'nets': [], 'pages': []}
            for pg in b['pages']:
                e = {'id': pg, 'sheetNumber': '%s of %d' % (pg, total)}
                if pg in src_by_page:
                    e['source'] = src_by_page[pg]
                blk['pages'].append(e)
            conns = b['connectors']
            for ref in sorted(parts):
                if ref not in conns:
                    blk['parts'].append(part_entry(ref, parts[ref], b['partpage'][ref]))
            for ref, ((cat, use), pg) in conns.items():
                rows = set()
                most = 0
                for pins in b['nets'].values():
                    for p, _ in pins:
                        m = re.match(re.escape(ref) + r'\.([A-Z])(\d+)$', p)
                        if m:
                            rows.add(m.group(1).lower()); most = max(most, int(m.group(2)))
                card_conns = d.setdefault('connectors', [])
                card_conns[:] = [c for c in card_conns if c['ref'] != 'J.' + ref]
                card_conns.append({'ref': 'J.' + ref, 'catalog': cat, 'use': use, 'rows': sorted(rows), 'pinsPerRow': most})
            seen = set()
            for g in b['gates']:
                if g['id'] in seen:
                    continue
                seen.add(g['id'])
                kind = g['kind'] if g['kind'] in ('nand', 'nor', 'not', 'and', 'or', 'buffer', 'and-or-invert') else 'other'
                ge = {'id': g['id'], 'part': g['id'].split('/')[0], 'kind': kind, 'inputs': g['inputs'], 'output': g['output'], 'confidence': 'read'}
                if kind == 'other' and g['kind'] != 'other':
                    ge['note'] = 'read as ' + g['kind']
                blk['gates'].append(ge)
            netpins = {}
            for name, pins in b['nets'].items():
                uniq = []
                for p, _ in pins:
                    if p not in uniq:
                        uniq.append(p)
                netpins[name] = uniq
                n, act, printed = sig(name, parts)
                ne = {'name': n, 'active': act}
                if printed:
                    ne['printed'] = printed
                # a pin named by a letter and a number (socket 18A: E1) is stored with the letter in
                # lowercase, the form the file format allows (e1)
                ne['pins'] = [{'at': pin_at(p, conns), 'confidence': 'read'} for p in uniq]
                blk['nets'].append(ne)
            # the pins drawn as stubs (nc) and the ones not settled (open) are kept as a block note
            notes = list(b['notes'])
            if b['nc']:
                notes.append('Pins drawn as unconnected stubs: ' + ' '.join(sorted(b['nc'])))
            if b['open']:
                notes.append('OPEN, pins not settled from the drawing (left out of every net): ' + ' '.join(sorted(b['open'])))
            blk['note'] = '\n'.join(notes)
            # ports: the names on the parent sheet's port lines, in order; BD~00..BD~23 as one bus port
            names = []
            for nm in parent_of[bn][1]:
                if nm not in names:
                    names.append(nm)
            bus = [nm for nm in names if re.match(r'^BD~\d\d$', nm)]
            for nm in names:
                if nm in bus and nm != bus[0]:
                    continue
                group = bus if nm in bus else [nm]
                pins = []
                for g in group:
                    pins += netpins.get(g, [])
                dr, conf, why = direction(pins, parts)
                s = []
                for g in group:
                    n, act, _ = sig(g, parts)
                    s.append({'net': n, 'active': act})
                blk['ports'].append({'name': 'BD_0' if nm in bus else port_name(nm), 'direction': dr, 'directionConfidence': conf, 'note': why, 'signals': s})
            out_blocks.append(blk)
    # instances in the parent blocks; a signal the parent level does not have yet is added there with no pins
    byname = dict(existing)
    for blk in out_blocks:
        byname[blk['name']] = blk
    for blk in out_blocks:
        parent = byname[parent_of[blk['name']][0]]
        have = {(n['name'], n['active']) for n in parent['nets']}
        conns = []
        for p in blk['ports']:
            nets = []
            for s in p['signals']:
                key = (s['net'], s['active'])
                if key not in have:
                    ne = {'name': s['net'], 'active': s['active'], 'pins': []}
                    parent['nets'].append(ne)
                    have.add(key)
                nets.append({'net': s['net'], 'active': s['active']})
            conns.append({'port': p['name'], 'nets': nets})
        inst = parent.setdefault('instances', [])
        inst[:] = [i for i in inst if i['name'] != blk['name']]
        inst.append({'name': blk['name'], 'block': blk['name'], 'connections': conns})
    order = [b['name'] for b in d.get('blocks', [])]
    for blk in out_blocks:
        if blk['name'] not in order:
            order.append(blk['name'])
    d['blocks'] = [byname[n] for n in order]
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        json.dump(d, f, indent=2, ensure_ascii=False)
        f.write('\n')
    print('wrote %d blocks into %s' % (len(out_blocks), path))


def main():
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)
    folder = sys.argv[2]
    files = [parse_file(os.path.join(folder, f)) for f in sorted(os.listdir(folder)) if re.match(r'^p\d+\.txt$', f)]
    blocks, problems, missing = merge(files)
    for bn, b in blocks.items():
        npins = sum(len(set(p for p, _ in v)) for v in b['nets'].values())
        print('block %s: sheets %s, %d parts, %d nets, %d pins, %d gates' % (bn, ','.join(b['pages']), len(b['parts']), len(b['nets']), npins, len(b['gates'])))
    if missing:
        print('no catalogue entry (pins not checked): ' + ' '.join(missing))
    for p in problems:
        print('PROBLEM ' + p)
    print('%d problem(s)' % len(problems))
    if sys.argv[1] == 'json':
        out = {bn: {'pages': b['pages'], 'parts': b['parts'], 'partpage': b['partpage'],
                    'boxes': b['boxes'], 'gates': b['gates'], 'notes': b['notes'],
                    'nets': {n: sorted(set(p for p, _ in v)) for n, v in b['nets'].items()}} for bn, b in blocks.items()}
        json.dump(out, open(sys.argv[3], 'w', encoding='utf-8'), indent=1)
    if sys.argv[1] == 'sheet':
        # written even with problems: they are printed above, stay in the exit code, and each one
        # must have a note in the readings and an entry in BUGS.md
        write_sheet(files, blocks, sys.argv[3])
    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main())
