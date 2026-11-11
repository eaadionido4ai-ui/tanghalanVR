"""Reference version of the TANGHALAN VR street flood model (the same steps as the C# and GAML versions).

A grid of 14 x 25 one-metre cells: i runs west to east, j north to south. Each step of DT seconds:
  1. rain falls on every cell, and the canal's south end receives the runoff of the land upstream;
  2. the canal carries water north (N_CANAL passes between neighbouring canal cells);
  3. water moves between all other neighbouring cells, from the higher surface to the lower;
  4. the culvert at the canal's north end lets water out, at a rate that falls with the trash left in the canal.
"""
NX, NZ = 14, 25
CANAL_I = 6
DT = 2.0
PIECES = 14
P = dict(K_LAND=0.20, K_CANAL=0.90, N_CANAL=12, A_UP=1300.0, Q_OUT=0.046, OUT_MIN=0.03, OUT_PER_PIECE=0.0693)
# rain, in mm per hour, at minutes after the rain starts (straight lines in between)
RAIN = [(0, 10.0), (10, 20.0), (30, 85.0), (70, 85.0), (100, 20.0), (120, 0.0)]
DOOR = (3, 10)

def ground(i, j):
    if i <= 5 and j >= 20: return 0.0            # the court
    if i <= 3: return -0.40                      # yards
    if i <= 5: return -0.45                      # west sidewalk
    if i == CANAL_I: return -1.35 - 0.20 * (24.5 - j) / 25.0   # the canal's bed, lower to the north
    if i <= 11: return -0.60                     # road
    return 0.0                                   # the museum's terrace

def rain_mm_h(t):
    m = t / 60.0
    if m <= RAIN[0][0]: return RAIN[0][1]
    for (m0, r0), (m1, r1) in zip(RAIN, RAIN[1:]):
        if m <= m1: return r0 + (r1 - r0) * (m - m0) / (m1 - m0)
    return 0.0

class Flood:
    def __init__(self, pieces=PIECES, **kw):
        self.p = dict(P); self.p.update(kw)
        self.g = [ground(i, j) for j in range(NZ) for i in range(NX)]
        self.w = [0.0] * (NX * NZ)
        self.t = 0.0
        self.pieces = pieces
        self.out_total = 0.0; self.in_total = 0.0
    def outlet_open(self):
        o = 1.0 - self.p['OUT_PER_PIECE'] * self.pieces
        return o if o > self.p['OUT_MIN'] else self.p['OUT_MIN']
    def exchange(self, pairs, k):
        """Moves water between each pair of neighbours, all from the same starting state."""
        g = self.g; w = self.w
        out = [0.0] * (NX * NZ); flows = []
        for (c, d) in pairs:
            ha = g[c] + w[c]; hb = g[d] + w[d]
            if ha > hb: src, dst, dh = c, d, ha - hb
            else: src, dst, dh = d, c, hb - ha
            if dh <= 1e-9: continue
            sill = g[c] if g[c] > g[d] else g[d]
            avail = g[src] + w[src] - sill
            if avail <= 0.0: continue
            q = dh / 2.0
            if avail < q: q = avail
            if w[src] < q: q = w[src]
            q = k * q
            flows.append((src, dst, q)); out[src] += q
        for (src, dst, q) in flows:
            if out[src] > w[src]: q = q * (w[src] / out[src])
            self.d[src] -= q; self.d[dst] += q
    def apply(self):
        for c in range(NX * NZ):
            v = self.w[c] + self.d[c]
            self.w[c] = v if v > 0.0 else 0.0
            self.d[c] = 0.0
    def step(self):
        p = self.p; w = self.w
        r = rain_mm_h(self.t) / 3600000.0
        add = r * DT
        for c in range(NX * NZ): w[c] += add
        w[(NZ - 1) * NX + CANAL_I] += p['A_UP'] * add
        self.in_total += p['A_UP'] * add + add * NX * NZ
        self.d = [0.0] * (NX * NZ)
        for _ in range(p['N_CANAL']):
            self.exchange(CANAL_PAIRS, p['K_CANAL']); self.apply()
        self.exchange(LAND_PAIRS, p['K_LAND']); self.apply()
        o = p['Q_OUT'] * self.outlet_open() * DT
        if w[CANAL_I] < o: o = w[CANAL_I]
        w[CANAL_I] -= o; self.out_total += o
        self.t += DT
    def depth(self, i, j): return self.w[j * NX + i]
    def door(self): return self.depth(*DOOR)
    def road_max(self): return max(self.depth(i, j) for i in range(7, 12) for j in range(NZ))
    def road_mean(self): return sum(self.depth(i, j) for i in range(7, 12) for j in range(NZ)) / 125.0
    def volume(self): return sum(self.w)

CANAL_PAIRS = [(j * NX + CANAL_I, (j + 1) * NX + CANAL_I) for j in range(NZ - 1)]
LAND_PAIRS = []
for j in range(NZ):
    for i in range(NX):
        c = j * NX + i
        if i + 1 < NX: LAND_PAIRS.append((c, c + 1))
        if j + 1 < NZ and i != CANAL_I: LAND_PAIRS.append((c, c + NX))

def run(pieces=PIECES, minutes=180, every=10, **kw):
    f = Flood(pieces, **kw); rows = []
    steps = int(minutes * 60 / DT)
    for s in range(steps + 1):
        if s % int(every * 60 / DT) == 0:
            rows.append((f.t / 60, rain_mm_h(f.t), f.door(), f.road_mean(), f.road_max(), f.depth(CANAL_I, 0), f.depth(CANAL_I, 12), f.depth(CANAL_I, 24), f.depth(13, 12)))
        f.step()
    return f, rows

if __name__ == '__main__':
    import sys
    peaks = []
    for pieces in (14, 12, 10, 7, 4, 0):
        f, rows = run(pieces)
        if '-v' in sys.argv or pieces in (14, 7, 0):
            print('==', pieces, 'pieces left; outlet open %.2f' % f.outlet_open())
            print('  min  rain  door   road  roadmax canal0 canal12 canal24 terrace')
            for r in rows: print('  %3d  %4.0f  %5.2f  %5.2f  %5.2f   %5.2f   %5.2f   %5.2f  %5.3f' % r)
            print('  mass check: in %.1f = left %.1f + out %.1f' % (f.in_total, f.volume(), f.out_total))
        peaks.append((pieces, max(r[2] for r in rows), max(r[3] for r in rows)))
    print('pieces left -> peak depth at the door, mean on the road')
    for p in peaks: print('  %2d  %5.2f  %5.2f' % p)
