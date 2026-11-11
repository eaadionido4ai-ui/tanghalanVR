// The flood on the street, cell by cell. This is what Unity runs when GAMA is not connected; the GAMA model
// (GAMA/models/TANGHALAN Flood.gaml) does the same steps with the same numbers, and
// Tools/flood-reference/flood.py is the reference both are checked against.
//
// Each step of Dt seconds:
//   1. rain falls on every cell, and the canal's south end receives the runoff of the land upstream;
//   2. the canal carries water north (CanalPasses passes between neighbouring canal cells);
//   3. water moves between all other neighbouring cells, from the higher surface to the lower;
//   4. the culvert at the canal's north end lets water out, at a rate that falls with the trash left.
using System;

namespace Tanghalan
{
    public sealed class FloodModel
    {
        /// <summary>Seconds of the storm in one step.</summary>
        public const double Dt = 2.0;
        /// <summary>How long it rains, in seconds of the storm (two hours).</summary>
        public const double RainSeconds = 7200;
        public const double KLand = 0.20, KCanal = 0.90;
        public const int CanalPasses = 12;
        /// <summary>The land upstream whose runoff reaches the canal, in square metres.</summary>
        public const double UpstreamArea = 1300.0;
        /// <summary>What the clear culvert lets out, in cubic metres a second.</summary>
        public const double OutletFlow = 0.046;
        /// <summary>How much of the culvert is open at least, and how much each piece of trash closes.</summary>
        public const double OutletMin = 0.03, OutletPerPiece = 0.0693;

        // rain, in millimetres an hour, at minutes after the rain starts (straight lines in between)
        static readonly double[] RainMinute = { 0, 10, 30, 70, 100, 120 };
        static readonly double[] RainRate = { 10, 20, 85, 85, 20, 0 };

        const int NX = StreetPlan.NX, NZ = StreetPlan.NZ, N = NX * NZ, CI = StreetPlan.CanalI;
        static readonly double[] ground = new double[N];
        static readonly int[] canalA, canalB, landA, landB;

        static FloodModel()
        {
            for (int j = 0; j < NZ; j++) for (int i = 0; i < NX; i++) ground[j * NX + i] = StreetPlan.Ground(i, j);
            canalA = new int[NZ - 1]; canalB = new int[NZ - 1];
            for (int j = 0; j < NZ - 1; j++) { canalA[j] = j * NX + CI; canalB[j] = (j + 1) * NX + CI; }
            int n = 0;
            for (int pass = 0; pass < 2; pass++)
            {
                var a = pass == 1 ? new int[n] : null; var b = pass == 1 ? new int[n] : null;
                int k = 0;
                for (int j = 0; j < NZ; j++)
                    for (int i = 0; i < NX; i++)
                    {
                        int c = j * NX + i;
                        if (i + 1 < NX) { if (pass == 1) { a[k] = c; b[k] = c + 1; } k++; }
                        if (j + 1 < NZ && i != CI) { if (pass == 1) { a[k] = c; b[k] = c + NX; } k++; }
                    }
                n = k;
                if (pass == 1) { landA = a; landB = b; }
            }
        }

        readonly double[] w = new double[N];   // water depth in each cell, in metres
        readonly double[] d = new double[N];
        readonly double[] outSum = new double[N];
        readonly double[] q;
        readonly int[] qSrc, qDst;

        /// <summary>Seconds of the storm so far.</summary>
        public double Time { get; private set; }
        /// <summary>Pieces of trash still in the canal.</summary>
        public int Pieces { get; set; }
        /// <summary>Cubic metres that came in (rain and runoff) and that left through the culvert.</summary>
        public double TotalIn { get; private set; }
        public double TotalOut { get; private set; }

        public FloodModel(int pieces)
        {
            Pieces = pieces;
            int m = Math.Max(landA.Length, canalA.Length);
            q = new double[m]; qSrc = new int[m]; qDst = new int[m];
        }

        /// <summary>Rain in millimetres an hour, t seconds after it started.</summary>
        public static double RainAt(double t)
        {
            double m = t / 60.0;
            if (m <= RainMinute[0]) return RainRate[0];
            for (int k = 1; k < RainMinute.Length; k++)
                if (m <= RainMinute[k]) return RainRate[k - 1] + (RainRate[k] - RainRate[k - 1]) * (m - RainMinute[k - 1]) / (RainMinute[k] - RainMinute[k - 1]);
            return 0.0;
        }

        /// <summary>PAGASA's rainfall warning for this much rain an hour: 0 none, 1 yellow, 2 orange, 3 red.</summary>
        public static int WarningLevel(double mmPerHour)
        {
            return mmPerHour > 30 ? 3 : mmPerHour > 15 ? 2 : mmPerHour >= 7.5 ? 1 : 0;
        }

        /// <summary>How much of the culvert is open, from 0 to 1.</summary>
        public double OutletOpen
        {
            get { double o = 1.0 - OutletPerPiece * Pieces; return o > OutletMin ? o : OutletMin; }
        }

        void Exchange(int[] pa, int[] pb, double k)
        {
            int n = 0;
            for (int p = 0; p < pa.Length; p++)
            {
                int c = pa[p], e = pb[p];
                double ha = ground[c] + w[c], hb = ground[e] + w[e];
                int src, dst; double dh;
                if (ha > hb) { src = c; dst = e; dh = ha - hb; } else { src = e; dst = c; dh = hb - ha; }
                if (dh <= 1e-9) continue;
                double sill = ground[c] > ground[e] ? ground[c] : ground[e];
                double avail = ground[src] + w[src] - sill;
                if (avail <= 0.0) continue;
                double f = dh / 2.0;
                if (avail < f) f = avail;
                if (w[src] < f) f = w[src];
                f = k * f;
                qSrc[n] = src; qDst[n] = dst; q[n] = f; outSum[src] += f; n++;
            }
            for (int p = 0; p < n; p++)
            {
                int src = qSrc[p]; double f = q[p];
                if (outSum[src] > w[src]) f = f * (w[src] / outSum[src]);
                d[src] -= f; d[qDst[p]] += f;
            }
            for (int c = 0; c < N; c++)
            {
                double v = w[c] + d[c];
                w[c] = v > 0.0 ? v : 0.0;
                d[c] = 0.0; outSum[c] = 0.0;
            }
        }

        /// <summary>Moves the storm on by Dt seconds.</summary>
        public void Step()
        {
            double add = RainAt(Time) / 3600000.0 * Dt;
            for (int c = 0; c < N; c++) w[c] += add;
            w[(NZ - 1) * NX + CI] += UpstreamArea * add;
            TotalIn += UpstreamArea * add + add * N;
            for (int p = 0; p < CanalPasses; p++) Exchange(canalA, canalB, KCanal);
            Exchange(landA, landB, KLand);
            double o = OutletFlow * OutletOpen * Dt;
            if (w[CI] < o) o = w[CI];
            w[CI] -= o; TotalOut += o;
            Time += Dt;
        }

        public void Run(int steps) { for (int s = 0; s < steps; s++) Step(); }

        /// <summary>Depth of water in a cell, in metres.</summary>
        public double Depth(int i, int j) { return w[j * NX + i]; }
        /// <summary>Height of the water's surface in a cell (the ground's height where it is dry).</summary>
        public double Surface(int i, int j) { int c = j * NX + i; return ground[c] + w[c]; }
        public double DoorDepth { get { return Depth(StreetPlan.DoorI, StreetPlan.DoorJ); } }
        public double Volume { get { double v = 0; for (int c = 0; c < N; c++) v += w[c]; return v; } }

        public double RoadMean
        {
            get
            {
                double s = 0; int n = 0;
                for (int j = 0; j < NZ; j++) for (int i = StreetPlan.RoadI0; i <= StreetPlan.RoadI1; i++) { s += w[j * NX + i]; n++; }
                return s / n;
            }
        }

        public double RoadMax
        {
            get
            {
                double m = 0;
                for (int j = 0; j < NZ; j++) for (int i = StreetPlan.RoadI0; i <= StreetPlan.RoadI1; i++) if (w[j * NX + i] > m) m = w[j * NX + i];
                return m;
            }
        }

        /// <summary>Depth at a point given in the web app's coordinates (0 off the grid).</summary>
        public double DepthAt(double x, double z)
        {
            int i, j;
            return StreetPlan.CellAt(x, z, out i, out j) ? Depth(i, j) : 0.0;
        }

        /// <summary>Copies the depths, in millimetres, row by row from the north (what GAMA sends too).</summary>
        public void CopyDepthsMm(int[] into)
        {
            for (int c = 0; c < N; c++) into[c] = (int)Math.Round(w[c] * 1000.0);
        }

        /// <summary>Takes over depths that came from GAMA, in millimetres, and the storm's time.</summary>
        public void SetDepthsMm(int[] from, double time)
        {
            int n = Math.Min(N, from.Length);
            for (int c = 0; c < n; c++) w[c] = from[c] / 1000.0;
            Time = time;
        }

        /// <summary>
        /// The deepest the water gets at Rico's door and on the road over the whole storm, with this many
        /// pieces of trash left in the canal from start to end. For the "what if" at the end of a visit.
        /// </summary>
        public static void Peak(int pieces, out double door, out double road)
        {
            var f = new FloodModel(pieces);
            door = 0; road = 0;
            int steps = (int)((RainSeconds + 1800) / Dt);
            for (int s = 0; s < steps; s++)
            {
                f.Step();
                if (s % 30 == 0) { double a = f.DoorDepth, b = f.RoadMean; if (a > door) door = a; if (b > road) road = b; }
            }
        }
    }
}
