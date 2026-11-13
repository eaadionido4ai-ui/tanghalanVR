// The museum's other visitors: a class on a field trip, with their teacher, in three small groups.
//
// Each group walks from work to work through the halls (NavGrid), stops in front of one for a while, and
// looks: the teacher explains, a child points. They keep out of the visitor's way: someone in their path
// makes them wait and then go round; standing too close, or between the visitor and the work the visitor is
// reading, makes them step aside; and they leave a work the visitor wants to read.
//
// In the earthquake drill they do what the visitor should do: at the alarm, the children and the teacher go
// to the nearest sturdy table, if it is close, and duck, cover and hold under it (or drop where they are
// and cover their heads); when the visitor walks out, they walk out too, by the same way, to the evacuation
// center; in the aftershock they stop and duck. Bayani counts them at the court.
using System;
using System.Collections.Generic;

namespace Tanghalan
{
    public sealed class MuseumVisitors
    {
        /// <summary>Where the visitors stand at the evacuation center: the east side of the covered court, clear of the board.</summary>
        public static readonly P3[] CourtSpots =
        {
            new P3(-22.3, 0, 5.0), new P3(-21.6, 0, 5.2), new P3(-22.3, 0, 5.8), new P3(-21.6, 0, 6.0),
            new P3(-22.3, 0, 6.6), new P3(-21.6, 0, 6.8), new P3(-22.0, 0, 7.3),
        };
        static readonly P3 CourtMiddle = new P3(-24, 0, 5.6);
        /// <summary>Just inside the street door, the way out of the museum.</summary>
        public static readonly P3 StreetDoorInside = new P3(Evacuation.StreetDoorX + 0.8, 0, Evacuation.StreetDoorZ);

        /// <summary>How near the visitor may come before they step aside, and how much room they want on their way.</summary>
        public const double PersonalSpace = 0.75, WayClearance = 0.85;
        /// <summary>How far a table may be for them to go to it at the alarm.</summary>
        public const double TableReach = 5.5;

        // the class: the teacher with two children, and two pairs of children
        static readonly string[][] Members =
        {
            new[] { "teacher", "visitor_1", "visitor_2" },
            new[] { "visitor_3", "visitor_4" },
            new[] { "visitor_5", "visitor_6" },
        };
        // where each group starts: at a work in the halls beside the lobby
        static readonly string[] Starts = { "bayanihan", "baha", "bahaghari" };

        public readonly List<Person> People = new List<Person>();
        public int ArrivedCount { get; private set; }

        enum State { Going, Blocked, Looking, StepAside, ToTable, Down, WaitOut, Out, Arrived }

        sealed class Member
        {
            public Person P; public Group G; public int K, N;
            public readonly Walker W = new Walker();
            public double Speed;
            public State S = State.Looking;
            public P3 Spot; public double SpotHeading;
            public double Delay; public bool PathPending;
            public double T;
            public Pose Gesture; public double GestureLeft, WaveAgain, AsideAgain;
            public PlanTable Table; public int Side; public bool UnderTable;
        }

        sealed class Group
        {
            public readonly List<Member> M = new List<Member>();
            public Work Target;
            public bool Looking, Explained, Pointed;
            public double LookLeft, LookTime;
            public readonly List<string> Seen = new List<string>();
        }

        readonly List<Member> members = new List<Member>();
        readonly List<Group> groups = new List<Group>();
        readonly Rand rnd;
        readonly NavGrid grid;
        bool drill, walkingOut;

        public MuseumVisitors(Rand rnd)
        {
            this.rnd = rnd;
            grid = NavGrid.Museum;
            int n = 0;
            for (int g = 0; g < Members.Length; g++)
            {
                var group = new Group();
                groups.Add(group);
                for (int k = 0; k < Members[g].Length; k++)
                {
                    string model = Members[g][k];
                    bool adult = model == "teacher";
                    var p = new Person(model, model, adult, new P3(0, 0, 0), 0);
                    var m = new Member { P = p, G = group, K = k, N = n++, Speed = adult ? 0.9 : 0.95 + 0.2 * rnd() };
                    group.M.Add(m); members.Add(m); People.Add(p);
                }
                // they start already looking at a work
                Aim(group, MuseumData.WorkById(Starts[g]));
                foreach (var m in group.M) { m.P.Position = m.Spot; m.P.Heading = m.SpotHeading; m.S = State.Looking; m.PathPending = false; m.Delay = 0; }
                group.Looking = true; group.LookLeft = 3 + 7 * rnd();
            }
        }

        // ---------- the tables ----------

        /// <summary>
        /// Where to stand beside a table to get under it (and to come out): the nearest place with room to
        /// stand, out from the place under it through a long side (towards the middle of the room first),
        /// along the way out, or out of the end.
        /// </summary>
        public static P3 Beside(PlanTable t, int side)
        {
            var grid = NavGrid.Museum;
            var s = t.Slot(side);
            double sz = t.NZ >= 0 ? 1 : -1;
            // a direction, and how much a step that way counts (the room side of a long side is best)
            double[][] ways = { new[] { 0, sz, 1.0 }, new[] { 0, -sz, 1.3 }, new[] { t.NX, t.NZ, 1.1 }, new[] { (double)side, 0, 1.25 } };
            P3? best = null; double bestCost = double.MaxValue;
            foreach (var w in ways)
                for (double r = 0.3; r <= 2.0; r += 0.05)
                {
                    var p = new P3(s.X + w[0] * r, 0, s.Z + w[1] * r);
                    if (!grid.Open(p.X, p.Z)) continue;
                    if (r * w[2] < bestCost) { bestCost = r * w[2]; best = p; }
                    break;
                }
            return best ?? grid.NearestOpen(s, 2.5) ?? s;
        }

        // ---------- the works ----------

        static P3 WorkCenter(Work w)
        {
            var b = MuseumPlan.HotBox(w.Hot);
            return b != null ? new P3(b.CX, 0, b.CZ) : new P3(0, 0, 0);
        }

        /// <summary>Where to stand to look at a work, and the direction from the work to that place.</summary>
        public static P3 ViewSpot(Work w, out double nx, out double nz)
        {
            var b = MuseumPlan.HotBox(w.Hot);
            var r = MuseumData.RoomById(w.Room);
            double rx = (r.X0 + r.X1) / 2, rz = (r.Z0 + r.Z1) / 2;
            if (w.Stand)
            {
                // a story stand stands in the room: read it from the side towards the middle of the room
                double dx = rx - b.CX, dz = rz - b.CZ, l = Math.Sqrt(dx * dx + dz * dz);
                if (l < 1e-6) { dx = 0; dz = 1; l = 1; }
                nx = dx / l; nz = dz / l;
                double half = Math.Abs(nx) * (b.X1 - b.X0) / 2 + Math.Abs(nz) * (b.Z1 - b.Z0) / 2;
                return new P3(b.CX + nx * (half + 0.85), 0, b.CZ + nz * (half + 0.85));
            }
            // a work on a wall: its box is thin across the wall; stand 1.6 m out from it
            if (b.X1 - b.X0 < b.Z1 - b.Z0) { nx = rx > b.CX ? 1 : -1; nz = 0; }
            else { nx = 0; nz = rz > b.CZ ? 1 : -1; }
            return new P3(b.CX + nx * 1.6, 0, b.CZ + nz * 1.6);
        }

        // the group's places in front of a work: the first in the middle, the others beside and a little behind
        void Aim(Group g, Work w)
        {
            g.Target = w; g.Looking = false; g.Explained = g.Pointed = false;
            g.Seen.Add(w.Id); if (g.Seen.Count > 6) g.Seen.RemoveAt(0);
            double nx, nz;
            var c = ViewSpot(w, out nx, out nz);
            double tx = -nz, tz = nx;
            var center = WorkCenter(w);
            for (int k = 0; k < g.M.Count; k++)
            {
                var m = g.M[k];
                double side = k == 0 ? 0 : (k % 2 == 1 ? 0.75 : -0.75), back = k == 0 ? 0 : 0.25;
                var want = new P3(c.X + tx * side + nx * back, 0, c.Z + tz * side + nz * back);
                m.Spot = grid.NearestOpen(want, 1.2) ?? want;
                m.SpotHeading = Floor.HeadingTo(m.Spot, center);
                m.Delay = k * 0.45 + 0.3 * rnd();
                m.PathPending = true;
                m.S = State.Going; m.T = 0;
            }
        }

        // the next work for a group: one they have not seen lately, that no other group is at, that the
        // visitor is not reading or standing at; nearer ones are likelier
        Work Choose(Group g, P3 player, string reading)
        {
            var from = g.M[0].P.Position;
            var options = new List<Work>();
            var weights = new List<double>();
            double total = 0;
            foreach (var w in MuseumData.Works)
            {
                if (w.Id == reading || g.Seen.Contains(w.Id)) continue;
                bool taken = false;
                foreach (var o in groups) if (o != g && o.Target == w) taken = true;
                if (taken) continue;
                double nx, nz;
                var c = ViewSpot(w, out nx, out nz);
                if (P3.Flat(c, player) < 2.5) continue;
                double wt = 1 / (1 + 0.15 * P3.Flat(c, from));
                options.Add(w); weights.Add(wt); total += wt;
            }
            if (options.Count == 0) return g.Target;
            double r = rnd() * total;
            for (int k = 0; k < options.Count; k++) { r -= weights[k]; if (r <= 0) return options[k]; }
            return options[options.Count - 1];
        }

        // ---------- each moment ----------

        /// <param name="player">Where the visitor (the player) stands.</param>
        /// <param name="reading">The work the visitor is reading, or null.</param>
        /// <param name="phase">The visitor's earthquake drill.</param>
        /// <param name="intensity">How strongly the ground shakes (0 to 1).</param>
        public void Update(double dt, P3 player, string reading, DrillPhase phase, double intensity)
        {
            if (!drill && phase != DrillPhase.Ready) StartDrill();
            if (drill) { UpdateDrill(dt, phase, intensity); return; }
            foreach (var g in groups) UpdateGroup(g, dt, player, reading);
            foreach (var m in members) UpdateMember(m, dt, player, reading);
        }

        void UpdateGroup(Group g, double dt, P3 player, string reading)
        {
            if (!g.Looking)
            {
                // the look begins when the first of them stands at the work; the others join as they come
                if (g.M[0].S == State.Looking) { g.Looking = true; g.LookTime = 0; g.LookLeft = 7 + 7 * rnd(); }
                return;
            }
            g.LookTime += dt; g.LookLeft -= dt;
            // the visitor reads this work: let them, and move on
            if (reading == g.Target.Id && g.LookLeft > 1.5) g.LookLeft = 1.5;
            if (!g.Explained && g.LookTime > 0.8)
            {
                g.Explained = true;
                foreach (var m in g.M) if (m.P.Adult && m.S == State.Looking) { m.Gesture = Pose.Talk; m.GestureLeft = 2.8; }
            }
            if (!g.Pointed && g.LookTime > 3.5)
            {
                g.Pointed = true;
                var children = g.M.FindAll((x) => !x.P.Adult && x.S == State.Looking);
                if (children.Count > 0) { var m = children[(int)(rnd() * children.Count) % children.Count]; m.Gesture = Pose.Point; m.GestureLeft = 1.8; }
            }
            if (g.LookLeft <= 0) Aim(g, Choose(g, player, reading));
        }

        void UpdateMember(Member m, double dt, P3 player, string reading)
        {
            var p = m.P;
            m.T += dt;
            if (m.GestureLeft > 0) m.GestureLeft -= dt;
            if (m.WaveAgain > 0) m.WaveAgain -= dt;
            if (m.AsideAgain > 0) m.AsideAgain -= dt;
            switch (m.S)
            {
                case State.Going:
                case State.StepAside:
                    if (m.Delay > 0) { m.Delay -= dt; p.Pose = Pose.Stand; break; }
                    if (m.PathPending)
                    {
                        m.PathPending = false;
                        var path = grid.Path(p.Position, m.Spot);
                        if (path == null) { m.S = State.Looking; break; }
                        m.W.Start(path, m.Speed);
                    }
                    // (stepping aside goes away from the visitor, so only the way to a work can be blocked)
                    if (m.S == State.Going && InTheWay(p.Position, m.W.Ahead(p.Position, 0.9), player))
                    {
                        m.S = State.Blocked; m.T = 0;
                        // a child waves at the visitor now and then
                        if (!p.Adult && m.WaveAgain <= 0 && rnd() < 0.6) { m.Gesture = Pose.Wave; m.GestureLeft = 1.6; m.WaveAgain = 25; }
                        break;
                    }
                    p.Pose = Pose.Walk;
                    m.W.Step(p, dt);
                    if (m.W.Done) { m.S = State.Looking; m.T = 0; }
                    break;

                case State.Blocked:
                    p.Pose = m.GestureLeft > 0 ? m.Gesture : Pose.Stand;
                    p.Heading = Floor.HeadingTo(p.Position, player);
                    if (!InTheWay(p.Position, m.W.Ahead(p.Position, 0.9), player)) { m.S = State.Going; break; }
                    if (m.T > 2.0)
                    {
                        // still in the way: go round
                        var around = grid.Path(p.Position, m.Spot, (x, z) => Dist(x, z, player) < WayClearance + 0.25);
                        if (around != null) { m.W.Start(around, m.Speed); m.S = State.Going; }
                        m.T = 0;
                    }
                    break;

                case State.Looking:
                    p.Pose = m.GestureLeft > 0 ? m.Gesture : Pose.Look;
                    p.Heading = m.SpotHeading;
                    if (m.AsideAgain > 0) break;
                    if (P3.Flat(p.Position, player) < PersonalSpace) { StepAside(m, player, null); break; }
                    if (reading != null)
                    {
                        var w = MuseumData.WorkById(reading);
                        if (w != null)
                        {
                            var c = WorkCenter(w);
                            double along;
                            if (Floor.ToSegment(p.Position, player, c, out along) < 0.5 && along > 0.05 && along < 0.97) StepAside(m, player, c);
                        }
                    }
                    break;
            }
        }

        static double Dist(double x, double z, P3 p) { double dx = x - p.X, dz = z - p.Z; return Math.Sqrt(dx * dx + dz * dz); }

        // the visitor stands where this person is, or right on the way ahead
        static bool InTheWay(P3 at, P3 ahead, P3 player)
        {
            if (P3.Flat(at, player) < WayClearance) return true;
            double along;
            return Floor.ToSegment(player, at, ahead, out along) < WayClearance && along > 0.05;
        }

        // a step aside: away from the visitor, or sideways out of the line from the visitor to a work
        void StepAside(Member m, P3 player, P3? work)
        {
            m.AsideAgain = 0.6; // if there is no room to step aside, try again a little later
            var p = m.P.Position;
            double dx, dz;
            if (work == null) { dx = p.X - player.X; dz = p.Z - player.Z; }
            else
            {
                double vx = work.Value.X - player.X, vz = work.Value.Z - player.Z;
                double s = (p.X - player.X) * (-vz) + (p.Z - player.Z) * vx >= 0 ? 1 : -1;
                dx = -vz * s; dz = vx * s;
            }
            double l = Math.Sqrt(dx * dx + dz * dz);
            if (l < 1e-6) { dx = 1; dz = 0; l = 1; }
            var want = new P3(p.X + dx / l * 1.0, 0, p.Z + dz / l * 1.0);
            Func<double, double, bool> keepClear = (x, z) => Dist(x, z, player) < PersonalSpace + 0.25 || (work != null && OnLine(x, z, player, work.Value));
            var to = grid.NearestOpen(want, 1.0, keepClear);
            if (to == null) return;
            var path = grid.Clear(p, to.Value) ? new List<P3> { to.Value } : grid.Path(p, to.Value);
            if (path == null) return;
            m.W.Start(path, m.Speed * 0.9);
            m.Spot = to.Value;
            m.SpotHeading = Floor.HeadingTo(to.Value, WorkCenter(m.G.Target));
            m.S = State.StepAside; m.T = 0; m.Delay = 0; m.PathPending = false;
        }

        static bool OnLine(double x, double z, P3 a, P3 b)
        {
            double along;
            return Floor.ToSegment(new P3(x, 0, z), a, b, out along) < 0.6 && along > 0.02 && along < 0.98;
        }

        // ---------- the earthquake drill ----------

        void StartDrill()
        {
            drill = true;
            var taken = new HashSet<string>();
            foreach (var m in members)
            {
                m.GestureLeft = 0; m.W.Stop(); m.Table = null; m.UnderTable = false; m.T = 0;
                var p = m.P.Position;
                var room = MuseumData.RoomAt(p.X, p.Z);
                PlanTable best = null; int bestSide = 0; double bestD = TableReach;
                foreach (var t in MuseumPlan.Tables)
                {
                    if (room == null || MuseumData.RoomAt(t.X, t.Z) != room) continue;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        if (taken.Contains(t.Id + side)) continue;
                        double d = P3.Flat(p, Beside(t, side));
                        if (d < bestD) { bestD = d; best = t; bestSide = side; }
                    }
                }
                if (best != null)
                {
                    var path = grid.Path(p, Beside(best, bestSide));
                    if (path != null)
                    {
                        taken.Add(best.Id + bestSide);
                        path.Add(best.Slot(bestSide));
                        m.Table = best; m.Side = bestSide;
                        m.W.Start(path, m.P.Adult ? 1.2 : 1.3);
                        m.S = State.ToTable;
                        continue;
                    }
                }
                // no sturdy table near: down where they are, arms over the head
                m.S = State.Down;
            }
        }

        void UpdateDrill(double dt, DrillPhase phase, double intensity)
        {
            bool goOut = phase == DrillPhase.Evacuating || phase == DrillPhase.Aftershock || phase == DrillPhase.Arrived;
            if (goOut && !walkingOut)
            {
                walkingOut = true;
                int k = 0;
                foreach (var m in members)
                {
                    if (m.S == State.Arrived) continue;
                    if (m.S == State.ToTable) { m.W.Stop(); m.Table = null; }
                    m.Delay = m.P.Adult ? 0.4 : 1.0 + 0.55 * k++;
                    m.S = State.WaitOut;
                }
            }
            ArrivedCount = 0;
            foreach (var m in members)
            {
                var p = m.P;
                m.T += dt;
                switch (m.S)
                {
                    case State.ToTable:
                        p.Pose = Pose.Walk;
                        m.W.Step(p, dt);
                        if (m.W.Done) { m.S = State.Down; m.UnderTable = true; p.Heading = m.Table.FacingHeading; }
                        // the ground already shakes and the table is still far: down here instead
                        else if (phase == DrillPhase.Shaking && m.T > 5 && m.W.Remaining(p.Position) > 1.5) { m.W.Stop(); m.Table = null; m.S = State.Down; }
                        break;
                    case State.Down:
                        p.Pose = m.UnderTable ? Pose.Cover : Pose.Duck;
                        break;
                    case State.WaitOut:
                        p.Pose = m.UnderTable ? Pose.Cover : Pose.Duck;
                        m.Delay -= dt;
                        if (m.Delay <= 0) GoOut(m);
                        break;
                    case State.Out:
                        // the aftershock: stop, duck and cover the head until it passes
                        if (intensity > DrillJudge.Threshold) { p.Pose = Pose.Duck; break; }
                        p.Pose = Pose.Walk;
                        m.W.Step(p, dt);
                        if (m.W.Done) { m.S = State.Arrived; p.Heading = Floor.HeadingTo(p.Position, CourtMiddle); }
                        break;
                    case State.Arrived:
                        p.Pose = Pose.Stand;
                        ArrivedCount++;
                        break;
                    default:
                        p.Pose = Pose.Duck;
                        break;
                }
            }
        }

        // the way out: out from under the table, through the halls to the street door, then the visitor's
        // route outside, to their own place at the court
        void GoOut(Member m)
        {
            var p = m.P;
            var route = new List<P3>();
            P3 from = p.Position;
            if (m.UnderTable) { from = Beside(m.Table, m.Side); route.Add(from); }
            route.AddRange(grid.Path(from, StreetDoorInside) ?? new List<P3> { StreetDoorInside });
            route.Add(new P3(Evacuation.StreetDoorX, 0, Evacuation.StreetDoorZ));
            for (int k = 0; k < Evacuation.Outside.Length - 1; k++) route.Add(Evacuation.Outside[k]);
            route.Add(CourtSpots[m.N % CourtSpots.Length]);
            m.W.Start(route, p.Adult ? 1.0 : 1.05 + 0.1 * rnd());
            m.S = State.Out; m.UnderTable = false;
        }

        /// <summary>Whether everyone is at the evacuation center.</summary>
        public bool AllArrived { get { return ArrivedCount == members.Count; } }
    }
}
