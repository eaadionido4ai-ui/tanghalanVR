#!/usr/bin/env python3
"""Checks that the street, the flood, the earthquake, the start of the drill and the neighbors use the same
numbers in the places that describe them: Unity (Tanghalan.Core), GAMA (GAMA/models) and the reference model
(Tools/flood-reference/flood.py), and that the street model (Tools/web-export/src/street.js) agrees too.

    python3 Tools/check_constants.py
"""
import os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
read = lambda *p: open(os.path.join(ROOT, *p), encoding='utf-8').read()
cs = read('Unity', 'Assets', 'TANGHALAN', 'Scripts', 'Core', 'FloodModel.cs') + read('Unity', 'Assets', 'TANGHALAN', 'Scripts', 'Core', 'StreetPlan.cs') \
    + read('Unity', 'Assets', 'TANGHALAN', 'Scripts', 'Core', 'Drill.cs') + read('Unity', 'Assets', 'TANGHALAN', 'Scripts', 'Core', 'Evacuation.cs')
gaml = read('GAMA', 'models', 'TANGHALAN.gaml')
py = read('Tools', 'flood-reference', 'flood.py')
js = read('Tools', 'web-export', 'src', 'street.js')
sim = read('Unity', 'Assets', 'TANGHALAN', 'Scripts', 'Runtime', 'TanghalanSimulationManager.cs')
vr = read('GAMA', 'models', 'TANGHALAN VR.gaml')

def num(text, pattern):
    m = re.search(pattern, text)
    if not m:
        raise SystemExit('not found: ' + pattern)
    return float(m.group(1))

N = r'(-?[0-9.]+)'
checks = [
    ('cells across', num(cs, r'NX = ' + N), num(gaml, r'int nx <- ' + N), num(py, r'NX, NZ = ' + N)),
    ('cells along', num(cs, r'NZ = ' + N), num(gaml, r'int nz <- ' + N), num(py, r'NX, NZ = \d+, ' + N)),
    ('canal column', num(cs, r'CanalI = ' + N), num(gaml, r'int canal_i <- ' + N), num(py, r'CANAL_I = ' + N)),
    ('step (s)', num(cs, r'Dt = ' + N), num(gaml, r'float dt <- ' + N), num(py, r'DT = ' + N)),
    ('flow on land', num(cs, r'KLand = ' + N), num(gaml, r'k_land <- ' + N), num(py, r"K_LAND=" + N)),
    ('flow in the canal', num(cs, r'KCanal = ' + N), num(gaml, r'k_canal <- ' + N), num(py, r"K_CANAL=" + N)),
    ('canal passes', num(cs, r'CanalPasses = ' + N), num(gaml, r'canal_passes <- ' + N), num(py, r"N_CANAL=" + N)),
    ('upstream area', num(cs, r'UpstreamArea = ' + N), num(gaml, r'upstream_area <- ' + N), num(py, r"A_UP=" + N)),
    ('culvert flow', num(cs, r'OutletFlow = ' + N), num(gaml, r'outlet_flow <- ' + N), num(py, r"Q_OUT=" + N)),
    ('culvert minimum', num(cs, r'OutletMin = ' + N), num(gaml, r'outlet_min <- ' + N), num(py, r"OUT_MIN=" + N)),
    ('culvert per piece', num(cs, r'OutletPerPiece = ' + N), num(gaml, r'outlet_per_piece <- ' + N), num(py, r"OUT_PER_PIECE=" + N)),
    ('road height', num(cs, r'Road = ' + N), num(gaml, r'if \(i <= 11\) \{ return ' + N), num(js, r'road: ' + N)),
    ('yard height', num(cs, r'Yard = ' + N), num(gaml, r'if \(i <= 3\) \{ return ' + N), num(js, r'yard: ' + N)),
    ('ramp up (s)', num(cs, r'RampUp = ' + N), num(gaml, r'ramp_up <- ' + N), None),
    ('strong shaking (s)', num(cs, r'Strong = ' + N), num(gaml, r'strong <- ' + N), None),
    ('ramp down (s)', num(cs, r'RampDown = ' + N), num(gaml, r'ramp_down <- ' + N), None),
    ('aftershock after (s)', num(cs, r'AftershockAfter = ' + N), num(gaml, r'aftershock_after <- ' + N), None),
    ('aftershock peak', num(cs, r'AftershockPeak = ' + N), num(gaml, r'aftershock_peak <- ' + N), None),
    ('go bag to drill (s)', num(cs, r'ReadSeconds = ' + N), num(vr, r'drill_delay <- ' + N), None),
    ('GAMA x offset', num(sim, r'OffsetX = ' + N + 'f'), num(gaml, r'offset_x <- ' + N), None),
    ('GAMA y offset', num(sim, r'OffsetY = ' + N + 'f'), num(gaml, r'offset_y <- ' + N), None),
]
bad = 0
for name, a, b, c in checks:
    same = abs(a - b) < 1e-9 and (c is None or abs(a - c) < 1e-9)
    print('%-22s %-8s %-8s %-8s %s' % (name, a, b, '' if c is None else c, 'ok' if same else 'DIFFERENT'))
    bad += 0 if same else 1
# the rain over time and the neighbors' starting places
rain_cs = re.findall(r'RainRate = \{ ([^}]*) \}', cs)[0].replace(' ', '')
rain_gaml = re.findall(r'rain_rate <- \[([^\]]*)\]', gaml)[0].replace(' ', '').replace('.0', '')
print('%-22s %s' % ('rain (mm/h)', 'ok' if rain_cs == rain_gaml else 'DIFFERENT: %s / %s' % (rain_cs, rain_gaml)))
bad += 0 if rain_cs == rain_gaml else 1
houses_cs = re.findall(r'Houses = \{ ([^}]*) \}', cs)[0].replace(' ', '')
houses_gaml = re.findall(r'houses <- \[([^\]]*)\]', gaml)[0].replace(' ', '')
print('%-22s %s' % ('houses', 'ok' if houses_cs == houses_gaml else 'DIFFERENT'))
bad += 0 if houses_cs == houses_gaml else 1
# the neighbors' places outside and at the court
evac = read('Unity', 'Assets', 'TANGHALAN', 'Scripts', 'Core', 'Evacuation.cs')
def p3s(block):
    return [tuple(round(float(v), 3) for v in m) for m in re.findall(r'new P3\((-?[0-9.]+), (-?[0-9.]+), (-?[0-9.]+)\)', block)]
def points(block):
    return [tuple(round(float(v), 3) for v in m) for m in re.findall(r'\{(-?[0-9.]+), (-?[0-9.]+), (-?[0-9.]+)\}', block)]
for name, cs_name, gaml_name in [('neighbors outside', 'Homes', 'homes'), ('neighbors at the court', 'Spots', 'spots')]:
    a = p3s(re.findall(r'P3\[\] ' + cs_name + r' =\s*\{(.*?)\};', evac, re.S)[0])
    b = points(re.findall(r'list<point> ' + gaml_name + r' <- \[(.*?)\];', gaml, re.S)[0])
    same = len(a) == 8 and a == b
    print('%-22s %s' % (name, 'ok' if same else 'DIFFERENT: %s / %s' % (a, b)))
    bad += 0 if same else 1
spawn = re.findall(r'init_locations <- \[\{([0-9.]+), ([0-9.]+)', vr)[0]
ok = abs(float(spawn[0]) - 30.0) < 1e-9 and abs(float(spawn[1]) - (4.8 + 25)) < 1e-9
print('%-22s %s' % ('player start', 'ok' if ok else 'DIFFERENT'))
bad += 0 if ok else 1
sys.exit(1 if bad else 0)
