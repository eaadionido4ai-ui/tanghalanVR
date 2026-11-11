#!/usr/bin/env python3
"""Runs the reference flood model (flood.py) and compares it with the recorded results (flood_golden.json),
which the C# tests also use. Run it after changing flood.py on purpose, with --write to record new results.

    python3 Tools/flood-reference/check_reference.py [--write]
"""
import json, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import flood

def record():
    gold = {'dt': flood.DT, 'params': flood.P, 'rain': flood.RAIN, 'door': list(flood.DOOR), 'runs': []}
    for pieces in (14, 10, 7, 0):
        f = flood.Flood(pieces); rows = []
        for s in range(1, 5401):
            f.step()
            if s % 300 == 0: rows.append([s, f.door(), f.road_mean(), f.depth(flood.CANAL_I, 0), f.depth(flood.CANAL_I, 24), f.volume(), f.out_total])
        gold['runs'].append({'pieces': pieces, 'rows': rows})
    clear = [400, 800, 1000, 1200, 1500, 1700, 1900, 2100]
    f = flood.Flood(14); rows = []
    for s in range(1, 5401):
        if s in clear: f.pieces -= 1
        f.step()
        if s % 300 == 0: rows.append([s, f.door(), f.road_mean(), f.depth(flood.CANAL_I, 0), f.depth(flood.CANAL_I, 24), f.volume(), f.out_total])
    gold['runs'].append({'pieces': 14, 'clearAt': clear, 'rows': rows})
    return gold

new = record()
path = os.path.join(HERE, 'flood_golden.json')
if '--write' in sys.argv:
    json.dump(new, open(path, 'w'))
    print('recorded; now regenerate Unity/Assets/TANGHALAN/Tests/EditMode/FloodGolden.g.cs')
    sys.exit(0)
old = json.load(open(path))
worst = 0.0
for a, b in zip(old['runs'], new['runs']):
    for ra, rb in zip(a['rows'], b['rows']):
        worst = max(worst, max(abs(x - y) for x, y in zip(ra, rb)))
print('largest difference from the recorded results: %.3g' % worst)
sys.exit(0 if worst < 1e-9 else 1)
