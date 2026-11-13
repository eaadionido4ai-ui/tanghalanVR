// The people the code moves: the museum's other visitors (MuseumVisitors) and the neighbors on the street
// (StreetLife). Each Person says where someone is, which way they face and what they are doing; Unity shows
// it (Assets/TANGHALAN/Scripts/Runtime/Figure.cs), and the tests check it.
// Positions are in the web app's coordinates: metres, x to the east, z to the south, y up. Headings are in
// degrees: 0 faces north (-z), 90 east.
using System;
using System.Collections.Generic;

namespace Tanghalan
{
    public enum Pose
    {
        /// <summary>Standing, arms down.</summary>
        Stand,
        /// <summary>Walking: the legs and arms swing with the distance walked.</summary>
        Walk,
        /// <summary>Running: a longer, faster swing.</summary>
        Run,
        /// <summary>Standing and looking at something.</summary>
        Look,
        /// <summary>Pointing at it with the right arm.</summary>
        Point,
        /// <summary>Talking: the hands move.</summary>
        Talk,
        /// <summary>Listening: a nod now and then.</summary>
        Listen,
        /// <summary>Waving with the right hand.</summary>
        Wave,
        /// <summary>The earthquake drill without a table: down on the knees, arms over the head.</summary>
        Duck,
        /// <summary>The earthquake drill under a table: down on the knees, one hand over the head, one holding the table's leg.</summary>
        Cover,
        /// <summary>Sitting on a step.</summary>
        Sit,
        /// <summary>Sweeping with a broom.</summary>
        Sweep,
        /// <summary>Throwing a ball.</summary>
        Throw,
        /// <summary>Hands up to catch a ball.</summary>
        Catch,
    }

    public sealed class Person
    {
        public readonly string Id;
        /// <summary>The model Unity shows (Resources/TANGHALAN/Models).</summary>
        public readonly string Model;
        public readonly bool Adult;
        public P3 Position;
        public double Heading;
        public Pose Pose = Pose.Stand;
        /// <summary>False while the person is inside a house.</summary>
        public bool Visible = true;
        /// <summary>Holds a broom.</summary>
        public bool Broom;

        public Person(string id, string model, bool adult, P3 at, double heading)
        {
            Id = id; Model = model; Adult = adult; Position = at; Heading = heading;
        }

        public override string ToString() { return Id + " " + Pose + " at " + Position; }
    }

    /// <summary>Directions and distances on the floor.</summary>
    public static class Floor
    {
        /// <summary>The heading of a direction on the floor: 0 north (-z), 90 east (+x).</summary>
        public static double HeadingOf(double dx, double dz)
        {
            double h = Math.Atan2(dx, -dz) * 180 / Math.PI;
            return h < 0 ? h + 360 : h;
        }

        public static double HeadingTo(P3 from, P3 to) { return HeadingOf(to.X - from.X, to.Z - from.Z); }

        /// <summary>The unit direction of a heading, as (x, z).</summary>
        public static void Direction(double heading, out double dx, out double dz)
        {
            double a = heading * Math.PI / 180;
            dx = Math.Sin(a); dz = -Math.Cos(a);
        }

        /// <summary>The distance from p to the segment a-b on the floor, and where along it (0 at a, 1 at b) p is nearest.</summary>
        public static double ToSegment(P3 p, P3 a, P3 b, out double along)
        {
            double vx = b.X - a.X, vz = b.Z - a.Z, l2 = vx * vx + vz * vz;
            along = l2 < 1e-9 ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * vx + (p.Z - a.Z) * vz) / l2));
            return P3.Flat(p, new P3(a.X + vx * along, 0, a.Z + vz * along));
        }
    }

    /// <summary>Walks a person along a list of points at a steady speed, turning to face the way.</summary>
    public sealed class Walker
    {
        List<P3> path;
        int next;
        public double Speed = 1;

        public bool Done { get { return path == null || next >= path.Count; } }

        public void Start(List<P3> points, double speed)
        {
            path = points; next = 0; Speed = speed;
        }

        public void Stop() { path = null; }

        /// <summary>Moves the person on; returns how far they went.</summary>
        public double Step(Person p, double dt)
        {
            if (Done) return 0;
            double go = Speed * dt, moved = 0;
            while (go > 1e-9 && next < path.Count)
            {
                P3 a = p.Position, b = path[next];
                double d = P3.Flat(a, b);
                if (d < 1e-6) { p.Position = b; next++; continue; }
                p.Heading = Floor.HeadingTo(a, b);
                if (go >= d) { p.Position = b; go -= d; moved += d; next++; }
                else
                {
                    double k = go / d;
                    p.Position = new P3(a.X + (b.X - a.X) * k, a.Y + (b.Y - a.Y) * k, a.Z + (b.Z - a.Z) * k);
                    moved += go; go = 0;
                }
            }
            return moved;
        }

        /// <summary>How far is left to walk from p.</summary>
        public double Remaining(P3 p)
        {
            if (Done) return 0;
            double s = P3.Flat(p, path[next]);
            for (int k = next + 1; k < path.Count; k++) s += P3.Flat(path[k - 1], path[k]);
            return s;
        }

        /// <summary>The point d metres further along the way from p (or the end).</summary>
        public P3 Ahead(P3 p, double d)
        {
            if (Done) return p;
            P3 a = p;
            for (int k = next; k < path.Count; k++)
            {
                double l = P3.Flat(a, path[k]);
                if (l >= d) { double t = d / l; return new P3(a.X + (path[k].X - a.X) * t, 0, a.Z + (path[k].Z - a.Z) * t); }
                d -= l; a = path[k];
            }
            return a;
        }

        public P3 End { get { return path != null && path.Count > 0 ? path[path.Count - 1] : default(P3); } }
    }
}
