// The way to the evacuation center, and the neighbors who walk there too.
//
// The evacuation center is the barangay's covered court at the south-west of the street. From anywhere in
// the museum the safe route goes through the doorways to the Stop Hazards Early hall, out of its street door,
// down the steps, straight over the bridge, and south along the sidewalk to the court.
//
// The neighbors (the Crowd) are outside on the street (StreetLife: sweeping, talking, playing). When the
// visitor walks out after the shaking, they walk to the court too, along the west sidewalk; in the
// aftershock they stop and duck until it passes. GAMA moves them when it is connected
// (GAMA/models/TANGHALAN.gaml has the same people, routes and speeds); otherwise this code does.
// All positions are in the web app's coordinates: metres, x east, z south, y up.
using System;
using System.Collections.Generic;

namespace Tanghalan
{
    public struct P3
    {
        public double X, Y, Z;
        public P3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static double Flat(P3 a, P3 b) { double dx = a.X - b.X, dz = a.Z - b.Z; return Math.Sqrt(dx * dx + dz * dz); }
        public override string ToString() { return string.Format("({0:0.##}, {1:0.##}, {2:0.##})", X, Y, Z); }
    }

    public static class Evacuation
    {
        /// <summary>The covered court: x from -27 to -21, z from 3 to 8.</summary>
        public const double CourtX0 = -27, CourtX1 = -21, CourtZ0 = 3, CourtZ1 = 8;
        /// <summary>Where the visitor gathers with the others.</summary>
        public static readonly P3 Assembly = new P3(-23.0, 0, 4.4);

        /// <summary>The street door, in the west wall of the Stop Hazards Early hall.</summary>
        public const string StreetDoorRoom = "mit";
        public const double StreetDoorX = -13, StreetDoorZ = -2.7;

        /// <summary>From the street door to the court, outside.</summary>
        public static readonly P3[] Outside =
        {
            new P3(-13.6, 0, -2.7),      // on the terrace, out of the door
            new P3(-14.7, 0, -2.7),      // the top of the steps
            new P3(-16.3, -0.6, -2.7),   // the foot of the steps, on the road
            new P3(-19.8, -0.6, -2.7),   // the bridge
            new P3(-21.2, -0.6, -2.7),
            new P3(-22.0, -0.45, -2.2),  // the sidewalk
            new P3(-22.0, -0.45, 2.1),
            new P3(-22.0, -0.15, 2.85),  // the steps up to the court
            new P3(-22.4, 0, 3.6),
            Assembly,
        };

        public static bool AtCenter(double x, double z)
        {
            return x >= CourtX0 && x <= CourtX1 && z >= CourtZ0 && z <= CourtZ1;
        }

        /// <summary>
        /// The route from a point to the evacuation center: through the museum's doorways to the street door
        /// (the fewest doorways), then outside. A point outside the museum joins the outside route where it is
        /// nearest.
        /// </summary>
        public static List<P3> RouteFrom(double x, double z)
        {
            var route = new List<P3>();
            var room = MuseumData.RoomAt(x, z);
            if (room != null)
            {
                foreach (var d in DoorsTo(room.Id, StreetDoorRoom)) AddDoorway(route, d, x, z);
                // the street door itself, crossed from east to west
                route.Add(new P3(StreetDoorX + 0.8, 0, StreetDoorZ));
                route.Add(new P3(StreetDoorX, 0, StreetDoorZ));
                route.AddRange(Outside);
                return route;
            }
            int nearest = 0; double best = double.MaxValue;
            var here = new P3(x, 0, z);
            for (int k = 0; k < Outside.Length; k++)
            {
                double d = P3.Flat(here, Outside[k]);
                if (d < best) { best = d; nearest = k; }
            }
            // do not walk back towards the museum if the next point is closer to the court
            if (nearest + 1 < Outside.Length && P3.Flat(here, Outside[nearest + 1]) < P3.Flat(Outside[nearest], Outside[nearest + 1])) nearest++;
            for (int k = nearest; k < Outside.Length; k++) route.Add(Outside[k]);
            return route;
        }

        // the doorway as two points, 0.8 m before and after it, so the route never cuts a corner through a wall
        static void AddDoorway(List<P3> route, Door d, double x, double z)
        {
            P3 from = route.Count > 0 ? route[route.Count - 1] : new P3(x, 0, z);
            if (d.Axis == 'x')
            {
                double s = from.Z < d.At ? -1 : 1; // the side we come from
                route.Add(new P3(d.MidX, 0, d.At + s * 0.8));
                route.Add(new P3(d.MidX, 0, d.At - s * 0.8));
            }
            else
            {
                double s = from.X < d.At ? -1 : 1;
                route.Add(new P3(d.At + s * 0.8, 0, d.MidZ));
                route.Add(new P3(d.At - s * 0.8, 0, d.MidZ));
            }
        }

        /// <summary>The doorways to walk through from one room to another (breadth-first: the fewest doorways).</summary>
        public static List<Door> DoorsTo(string from, string to)
        {
            var path = new List<Door>();
            if (from == to) return path;
            var prev = new Dictionary<string, Door>();
            var queue = new Queue<string>();
            queue.Enqueue(from); prev[from] = null;
            while (queue.Count > 0)
            {
                string r = queue.Dequeue();
                if (r == to) break;
                foreach (var d in MuseumData.Doors)
                {
                    string o = d.Other(r);
                    if (o == null || prev.ContainsKey(o) || MuseumData.RoomById(o) == null) continue;
                    prev[o] = d; queue.Enqueue(o);
                }
            }
            if (!prev.ContainsKey(to)) return path;
            for (string r = to; prev[r] != null; r = prev[r].Other(r)) path.Insert(0, prev[r]);
            return path;
        }

        public static double Length(IList<P3> route)
        {
            double s = 0;
            for (int k = 1; k < route.Count; k++) s += P3.Flat(route[k - 1], route[k]);
            return s;
        }
    }

    /// <summary>One neighbor walking to the evacuation center.</summary>
    public sealed class Evacuee
    {
        public readonly string Id;
        public readonly bool Adult;
        /// <summary>The shirt's color (rrggbb), so Unity and GAMA show the same person.</summary>
        public readonly string Shirt;
        public readonly double Delay, Speed;
        public readonly P3[] Path;
        public P3 Position;
        public double Heading; // degrees; 0 faces north (-z), 90 faces east
        public bool Walking, Arrived;
        int next = 1;

        public Evacuee(string id, bool adult, string shirt, double delay, double speed, P3[] path)
        {
            Id = id; Adult = adult; Shirt = shirt; Delay = delay; Speed = speed; Path = path; Position = path[0];
            Heading = 180; // along the sidewalk, towards the court
        }

        public void Step(double t, double dt)
        {
            if (Arrived || t < Delay) { Walking = false; return; }
            Walking = true;
            double go = Speed * dt;
            while (go > 0 && next < Path.Length)
            {
                P3 a = Position, b = Path[next];
                double d = P3.Flat(a, b);
                if (d < 1e-6) { Position = b; next++; continue; }
                Heading = Math.Atan2(b.X - a.X, -(b.Z - a.Z)) * 180 / Math.PI;
                if (go >= d) { Position = b; go -= d; next++; }
                else
                {
                    double k = go / d;
                    Position = new P3(a.X + (b.X - a.X) * k, a.Y + (b.Y - a.Y) * k, a.Z + (b.Z - a.Z) * k);
                    go = 0;
                }
            }
            if (next >= Path.Length) { Arrived = true; Walking = false; Heading = Floor.HeadingTo(Position, Crowd.CourtMiddle); }
        }
    }

    /// <summary>
    /// The neighbors: a grown-up and a child from each of the three houses, and two children playing at the
    /// north end of the street. Each starts from where they are outside (StreetLife), and walks along the
    /// west sidewalk to the court.
    /// </summary>
    public sealed class Crowd
    {
        public readonly List<Evacuee> People = new List<Evacuee>();
        public double Time { get; private set; }
        public bool Started { get; private set; }
        /// <summary>True while they stop and duck in the aftershock.</summary>
        public bool Ducking { get; private set; }

        // the houses' doors (z of each)
        public static readonly double[] Houses = { -12.6, -6.6, -0.6 };
        /// <summary>Where each one is outside before the drill (StreetLife): their place on the sidewalk or the doorstep.</summary>
        public static readonly P3[] Homes =
        {
            new P3(-22.4, -0.45, -12.6),   // sweeping in front of the first house
            new P3(-23.18, -0.3, -12.25),  // on its doorstep
            new P3(-22.5, -0.45, -7.25),   // talking, in front of Rico's house
            new P3(-22.05, -0.45, -4.55),  // playing ball
            new P3(-22.45, -0.45, -6.25),  // talking
            new P3(-22.05, -0.45, -2.75),  // playing ball
            new P3(-21.7, -0.45, -15.3),   // playing tag at the north end
            new P3(-22.3, -0.45, -14.7),
        };
        /// <summary>Where they stand at the court: its west side, clear of the board.</summary>
        public static readonly P3[] Spots =
        {
            new P3(-26.3, 0, 4.2), new P3(-25.5, 0, 4.4), new P3(-26.4, 0, 5.1), new P3(-25.6, 0, 5.3),
            new P3(-26.3, 0, 6.0), new P3(-25.5, 0, 6.2), new P3(-26.3, 0, 6.9), new P3(-25.5, 0, 7.1),
        };
        public static readonly P3 CourtMiddle = new P3(-24, 0, 5.6);
        static readonly string[] Shirts = { "c8782a", "e2588a", "2f74d0", "4caf50", "7a53c6", "f2b300", "2fa37a", "e2382b" };

        public Crowd()
        {
            for (int k = 0; k < Homes.Length; k++)
            {
                bool north = k >= 6;
                int h = k / 2, m = k % 2; // the house, and grown-up (0) or child (1)
                bool adult = !north && m == 0;
                // the lane along the sidewalk: the grown-ups nearer the fence, the children nearer the canal
                double lane = north ? -22.0 + (k == 6 ? 0.3 : -0.3) : -22.3 + (m == 0 ? -0.25 : 0.25);
                var home = Homes[k];
                var path = new List<P3> { home };
                if (home.X < -23.0) path.Add(new P3(-22.7, -0.45, home.Z)); // out through the gate
                path.Add(new P3(lane, -0.45, home.Z));
                path.Add(new P3(lane, -0.45, 2.1));
                path.Add(new P3(lane, -0.15, 2.85));
                path.Add(new P3(north ? -22.6 : -22.8, 0, 3.6));
                path.Add(Spots[k]);
                double delay = north ? 3.0 + (k - 6) * 0.6 : 1.0 + h * 2.5 + m * 0.8;
                double speed = north ? 1.2 : adult ? 1.05 : 1.15;
                People.Add(new Evacuee("evacuee" + k, adult, Shirts[k], delay, speed, path.ToArray()));
            }
        }

        /// <summary>They start to walk to the court (when the visitor walks out).</summary>
        public void Start() { Started = true; }

        /// <param name="shaking">How strongly the ground shakes: in an aftershock they stop and duck.</param>
        public void Step(double dt, double shaking = 0)
        {
            if (!Started) return;
            Ducking = shaking > DrillJudge.Threshold;
            if (Ducking) { foreach (var p in People) p.Walking = false; return; }
            Time += dt;
            foreach (var p in People) p.Step(Time, dt);
        }

        public int ArrivedCount { get { int n = 0; foreach (var p in People) if (p.Arrived) n++; return n; } }
    }
}
