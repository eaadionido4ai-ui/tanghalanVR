// The way to the evacuation center, and the neighbors who walk there too.
//
// The evacuation center is the barangay's covered court at the south-west of the street. From anywhere in
// the museum the safe route goes through the doorways to the Stop Hazards Early hall, out of its street door,
// down the steps, straight over the bridge, and south along the sidewalk to the court.
//
// The neighbors (the Crowd) leave their three houses when the shaking stops and walk to the court. GAMA moves
// them when it is connected (GAMA/models/TANGHALAN.gaml has the same people, routes and speeds); otherwise
// this code does. All positions are in the web app's coordinates: metres, x east, z south, y up.
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
            Heading = 90; // the houses' doors face east, towards the street
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
            if (next >= Path.Length) { Arrived = true; Walking = false; Heading = 0; }
        }
    }

    /// <summary>The neighbors: two from each of the three houses, and two from the north end of the street.</summary>
    public sealed class Crowd
    {
        public readonly List<Evacuee> People = new List<Evacuee>();
        public double Time { get; private set; }
        public bool Started { get; private set; }

        // the houses' doors (z of each), and the places in the court where the people stand
        static readonly double[] Houses = { -12.6, -6.6, -0.6 };
        static readonly P3[] Spots =
        {
            new P3(-25.9, 0, 5.2), new P3(-25.1, 0, 5.4), new P3(-26.0, 0, 6.3), new P3(-25.2, 0, 6.5),
            new P3(-24.3, 0, 6.0), new P3(-24.4, 0, 7.1), new P3(-23.4, 0, 6.6), new P3(-25.5, 0, 7.4),
        };
        static readonly string[] Shirts = { "c8782a", "e2588a", "2f74d0", "4caf50", "7a53c6", "f2b300", "2fa37a", "e2382b" };

        public Crowd()
        {
            int k = 0;
            for (int h = 0; h < Houses.Length; h++)
            {
                double z = Houses[h];
                for (int m = 0; m < 2; m++, k++)
                {
                    double side = m == 0 ? -0.25 : 0.25;
                    var path = new List<P3>
                    {
                        new P3(-23.6, -0.4, z + side), new P3(-22.8, -0.42, z + side), new P3(-22.3 + side, -0.45, z + 0.6),
                        new P3(-22.3 + side, -0.45, 2.1), new P3(-22.3 + side, -0.15, 2.85), new P3(-22.8, 0, 3.6), Spots[k],
                    };
                    People.Add(new Evacuee("evacuee" + k, m == 0, Shirts[k], 1.0 + h * 2.5 + m * 0.8, m == 0 ? 1.05 : 1.15, path.ToArray()));
                }
            }
            for (int m = 0; m < 2; m++, k++)
            {
                double side = m == 0 ? -0.3 : 0.3;
                var path = new[]
                {
                    new P3(-22.0 + side, -0.45, -16.3), new P3(-22.0 + side, -0.45, 2.1), new P3(-22.0 + side, -0.15, 2.85),
                    new P3(-22.6, 0, 3.6), Spots[k],
                };
                People.Add(new Evacuee("evacuee" + k, false, Shirts[k], 3.0 + m * 0.6, 1.2, path));
            }
        }

        /// <summary>They start to leave their houses (when the shaking stops).</summary>
        public void Start() { Started = true; }

        public void Step(double dt)
        {
            if (!Started) return;
            Time += dt;
            foreach (var p in People) p.Step(Time, dt);
        }

        public int ArrivedCount { get { int n = 0; foreach (var p in People) if (p.Arrived) n++; return n; } }
    }
}
