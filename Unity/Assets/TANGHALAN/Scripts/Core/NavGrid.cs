// Where people can walk in the museum, and the way from one place to another.
//
// The floor is cut into squares of 25 cm. A square is open if a person standing in its middle is clear of
// every wall and piece of furniture (MuseumPlan) by a body's width. The way between two points is the
// shortest chain of open squares (A*, through the doorways), straightened wherever a straight line stays on
// open squares. The game rooms cost more to walk through, so visitors only cross them when it saves a lot.
// Positions are in the web app's coordinates: metres, x to the east, z to the south.
using System;
using System.Collections.Generic;

namespace Tanghalan
{
    public sealed class NavGrid
    {
        public const double Cell = 0.25;
        /// <summary>How far a person keeps from walls and furniture.</summary>
        public const double BodyRadius = 0.3;

        public readonly double X0, Z0;
        public readonly int NX, NZ;
        readonly bool[] open;
        readonly float[] cost;

        public NavGrid(double x0, double z0, double x1, double z1, IList<PlanBox> solids, double radius, Func<double, double, double> costAt)
        {
            X0 = x0; Z0 = z0;
            NX = (int)Math.Ceiling((x1 - x0) / Cell); NZ = (int)Math.Ceiling((z1 - z0) / Cell);
            open = new bool[NX * NZ]; cost = new float[NX * NZ];
            for (int j = 0; j < NZ; j++)
                for (int i = 0; i < NX; i++)
                {
                    double x = X0 + (i + 0.5) * Cell, z = Z0 + (j + 0.5) * Cell;
                    bool ok = true;
                    foreach (var b in solids) if (b.Covers(x, z, radius)) { ok = false; break; }
                    open[j * NX + i] = ok;
                    cost[j * NX + i] = (float)(costAt != null ? costAt(x, z) : 1);
                }
        }

        static NavGrid museum;

        /// <summary>The museum's floor: its walls and furniture, and the story stands, which stand in the rooms.</summary>
        public static NavGrid Museum { get { return museum ?? (museum = BuildMuseum()); } }

        static NavGrid BuildMuseum()
        {
            var boxes = new List<PlanBox>();
            foreach (var b in MuseumPlan.Solids) if (b.Y1 > 0.1 && b.Y0 < 2.0) boxes.Add(b); // not the ceiling, not flat bases
            foreach (var w in MuseumData.Works)
            {
                if (!w.Stand) continue;
                var b = MuseumPlan.HotBox(w.Hot);
                if (b != null) boxes.Add(b);
            }
            return new NavGrid(-13, -20, 13, 9, boxes, BodyRadius, (x, z) =>
            {
                var r = MuseumData.RoomAt(x, z);
                return r != null && !string.IsNullOrEmpty(r.Game) ? 3.0 : 1.0;
            });
        }

        // ---------- squares ----------

        int Index(double x, double z)
        {
            int i = (int)Math.Floor((x - X0) / Cell), j = (int)Math.Floor((z - Z0) / Cell);
            if (i < 0 || j < 0 || i >= NX || j >= NZ) return -1;
            return j * NX + i;
        }

        P3 Middle(int c) { return new P3(X0 + (c % NX + 0.5) * Cell, 0, Z0 + (c / NX + 0.5) * Cell); }

        /// <summary>Whether a person can stand at this point.</summary>
        public bool Open(double x, double z)
        {
            int c = Index(x, z);
            return c >= 0 && open[c];
        }

        /// <summary>The nearest point to p where a person can stand (and that avoid does not rule out), within maxR; null if none.</summary>
        public P3? NearestOpen(P3 p, double maxR = 2.0, Func<double, double, bool> avoid = null)
        {
            int c = NearestCell(p, maxR, avoid);
            if (c < 0) return null;
            if (c == Index(p.X, p.Z)) return new P3(p.X, 0, p.Z);
            return Middle(c);
        }

        int NearestCell(P3 p, double maxR, Func<double, double, bool> avoid)
        {
            int c0 = Index(p.X, p.Z);
            if (c0 >= 0 && open[c0] && (avoid == null || !avoid(p.X, p.Z))) return c0;
            int i0 = (int)Math.Floor((p.X - X0) / Cell), j0 = (int)Math.Floor((p.Z - Z0) / Cell);
            int rings = (int)Math.Ceiling(maxR / Cell);
            int best = -1; double bestD = double.MaxValue;
            for (int r = 1; r <= rings && best < 0; r++)
                for (int dj = -r; dj <= r; dj++)
                    for (int di = -r; di <= r; di++)
                    {
                        if (Math.Max(Math.Abs(di), Math.Abs(dj)) != r) continue;
                        int i = i0 + di, j = j0 + dj;
                        if (i < 0 || j < 0 || i >= NX || j >= NZ) continue;
                        int c = j * NX + i;
                        if (!open[c]) continue;
                        var m = Middle(c);
                        if (avoid != null && avoid(m.X, m.Z)) continue;
                        double d = P3.Flat(m, p);
                        if (d < bestD && d <= maxR) { bestD = d; best = c; }
                    }
            return best;
        }

        /// <summary>Whether the straight line from a to b stays on open squares (and away from what avoid rules out).</summary>
        public bool Clear(P3 a, P3 b, Func<double, double, bool> avoid = null)
        {
            double d = P3.Flat(a, b);
            int n = Math.Max(1, (int)Math.Ceiling(d / (Cell * 0.4)));
            for (int k = 0; k <= n; k++)
            {
                double t = (double)k / n, x = a.X + (b.X - a.X) * t, z = a.Z + (b.Z - a.Z) * t;
                int c = Index(x, z);
                if (c < 0 || !open[c]) return false;
                if (avoid != null && avoid(x, z)) return false;
            }
            return true;
        }

        // ---------- the way ----------

        /// <summary>
        /// The way from one point to another: the points to walk to, in order, ending at the goal (or, if the goal
        /// is not open, at the nearest open point). Null if there is no way.
        /// </summary>
        public List<P3> Path(P3 from, P3 to, Func<double, double, bool> avoid = null)
        {
            int s = NearestCell(from, 2.0, null), g = NearestCell(to, 2.0, avoid);
            if (s < 0 || g < 0) return null;
            var cells = AStar(s, g, avoid);
            if (cells == null) return null;
            var raw = new List<P3>();
            raw.Add(from);
            for (int k = 1; k < cells.Count - 1; k++) raw.Add(Middle(cells[k]));
            int gc = Index(to.X, to.Z);
            raw.Add(gc == g ? to : Middle(g));
            // straighten: from each point, go on to the farthest one in plain view
            var path = new List<P3>();
            int at = 0;
            while (at < raw.Count - 1)
            {
                int far = at + 1;
                for (int m = at + 2; m < raw.Count; m++)
                {
                    if (Clear(raw[at], raw[m], avoid)) far = m;
                    else if (m > far + 6) break;
                }
                path.Add(raw[far]);
                at = far;
            }
            return path;
        }

        // the search's working memory, kept from one search to the next (a headset should not collect garbage
        // every time someone sets off): a square's values count only if it was touched in this search
        float[] gScore;
        int[] came, touched, closedIn;
        int search;
        readonly Heap heap = new Heap();

        List<int> AStar(int s, int g, Func<double, double, bool> avoid)
        {
            if (s == g) return new List<int> { s, g };
            int n = NX * NZ;
            if (gScore == null) { gScore = new float[n]; came = new int[n]; touched = new int[n]; closedIn = new int[n]; }
            search++;
            heap.Clear();
            Touch(s); gScore[s] = 0;
            heap.Push(s, H(s, g));
            while (heap.Count > 0)
            {
                int c = heap.Pop();
                if (c == g) break;
                if (closedIn[c] == search) continue;
                closedIn[c] = search;
                int ci = c % NX, cj = c / NX;
                for (int dj = -1; dj <= 1; dj++)
                    for (int di = -1; di <= 1; di++)
                    {
                        if (di == 0 && dj == 0) continue;
                        int i = ci + di, j = cj + dj;
                        if (i < 0 || j < 0 || i >= NX || j >= NZ) continue;
                        int e = j * NX + i;
                        if (!open[e] || closedIn[e] == search) continue;
                        // no cutting a corner past a wall
                        if (di != 0 && dj != 0 && (!open[cj * NX + i] || !open[j * NX + ci])) continue;
                        if (avoid != null && e != g)
                        {
                            var m = Middle(e);
                            if (avoid(m.X, m.Z)) continue;
                        }
                        float step = (di != 0 && dj != 0 ? 1.41421356f : 1f) * cost[e];
                        float t = gScore[c] + step;
                        Touch(e);
                        if (t < gScore[e]) { gScore[e] = t; came[e] = c; heap.Push(e, t + H(e, g)); }
                    }
            }
            if (touched[g] != search || came[g] < 0) return null;
            var cells = new List<int>();
            for (int c = g; c >= 0; c = came[c]) { cells.Add(c); if (c == s) break; }
            cells.Reverse();
            return cells;
        }

        void Touch(int c)
        {
            if (touched[c] == search) return;
            touched[c] = search; gScore[c] = float.MaxValue; came[c] = -1;
        }

        float H(int a, int b)
        {
            int dx = Math.Abs(a % NX - b % NX), dz = Math.Abs(a / NX - b / NX);
            return Math.Max(dx, dz) + 0.41421356f * Math.Min(dx, dz);
        }

        // a small binary heap of squares by their estimated cost
        sealed class Heap
        {
            readonly List<int> items = new List<int>();
            readonly List<float> keys = new List<float>();
            public int Count { get { return items.Count; } }
            public void Clear() { items.Clear(); keys.Clear(); }

            public void Push(int item, float key)
            {
                items.Add(item); keys.Add(key);
                int k = items.Count - 1;
                while (k > 0)
                {
                    int p = (k - 1) / 2;
                    if (keys[p] <= keys[k]) break;
                    Swap(k, p); k = p;
                }
            }

            public int Pop()
            {
                int top = items[0];
                int last = items.Count - 1;
                items[0] = items[last]; keys[0] = keys[last];
                items.RemoveAt(last); keys.RemoveAt(last);
                int k = 0;
                while (true)
                {
                    int l = 2 * k + 1, r = l + 1, m = k;
                    if (l < items.Count && keys[l] < keys[m]) m = l;
                    if (r < items.Count && keys[r] < keys[m]) m = r;
                    if (m == k) break;
                    Swap(k, m); k = m;
                }
                return top;
            }

            void Swap(int a, int b)
            {
                int t = items[a]; items[a] = items[b]; items[b] = t;
                float u = keys[a]; keys[a] = keys[b]; keys[b] = u;
            }
        }
    }
}
