// Life on the street before anything happens: the neighbors are outside, in front of their houses.
//   Aling Rosa sweeps the sidewalk with a walis tingting; her daughter sits on the doorstep.
//   Mang Ben (Rico's father) talks with Lola Ising in front of his gate.
//   Rico and Lola Ising's granddaughter throw a ball to each other.
//   Two children play tag at the north end of the street.
// They mind the visitor: they stop sweeping, throwing or running when the visitor is in the way.
//
// When the rain replay starts, they go inside their houses, and come out again when it is over. In the
// earthquake they drop where they are and cover their heads; when the visitor walks out after the shaking,
// they walk to the evacuation center (Crowd, or GAMA when it is connected: they follow it).
// Positions are in the web app's coordinates: metres, x to the east, z to the south, y up.
using System;
using System.Collections.Generic;

namespace Tanghalan
{
    public sealed class StreetLife
    {
        public readonly List<Person> People = new List<Person>();
        /// <summary>The children's ball: where it is, and whether it is out (not while they are inside).</summary>
        public P3 Ball;
        public bool BallOut = true;

        /// <summary>The sweeper's stretch of sidewalk, and the middle of the tag game.</summary>
        public const double SweepZ0 = -13.6, SweepZ1 = -11.6, SweepX = -22.4;
        public static readonly P3 TagMiddle = new P3(-22.0, -0.45, -15.0);
        public const double TagRX = 0.5, TagRZ = 1.25, TagSpeed = 1.5;
        /// <summary>How fast the neighbors catch up with where the evacuation (Crowd or GAMA) says they are.</summary>
        public const double FollowSpeed = 1.8;

        enum Doing { Activity, GoingIn, Inside, ComingOut, Down, Following }

        sealed class N
        {
            public Person P; public int K;
            public Doing D = Doing.Activity;
            public readonly Walker W = new Walker();
            public double DoorZ;
        }

        static readonly string[] Ids = { "Aling Rosa", "Rosa's daughter", "Mang Ben", "Rico", "Lola Ising", "Lola Ising's granddaughter", "Tag 1", "Tag 2" };
        static readonly string[] Models = { "person_c8782a", "person_e2588a", "person_2f74d0", "person_4caf50", "person_7a53c6", "person_f2b300", "person_2fa37a", "person_e2382b" };
        // the houses they go into when it rains (the two at the north end go into the first house)
        static readonly int[] HouseOf = { 0, 0, 1, 1, 2, 2, 0, 0 };

        readonly List<N> ns = new List<N>();
        readonly Crowd crowd;
        readonly Rand rnd;

        // the sweeper
        int sweepDir = 1; double sweepPause;
        // the talk: who speaks, and for how long
        int speaker = 2; double talkLeft = 3;
        // the ball: who holds it, and its flight
        int holder = 3; double holdLeft = 1.5, flight = -1;
        P3 flyFrom, flyTo;
        public const double FlightTime = 0.85, HandsUp = 0.45;
        // tag: where the runner is on the loop, which way they run, and when they turn
        double tagAngle; int tagDir = 1; double tagTurn = 8;

        public StreetLife(Crowd crowd, Rand rnd)
        {
            this.crowd = crowd; this.rnd = rnd;
            for (int k = 0; k < Models.Length; k++)
            {
                var home = Crowd.Homes[k];
                var p = new Person(Ids[k], Models[k], crowd.People[k].Adult, home, 180);
                People.Add(p);
                ns.Add(new N { P = p, K = k, DoorZ = Crowd.Houses[HouseOf[k]] });
            }
            People[0].Broom = true;
            People[1].Heading = 90;  // on the doorstep, looking out at the street
            People[2].Heading = 180; // Mang Ben and Lola Ising face each other
            People[4].Heading = 0;
            People[3].Heading = 180; // the two with the ball face each other
            People[5].Heading = 0;
            Ball = HandsOf(People[holder]);
        }

        // where a child holds the ball: in front, at the height of the hands
        static P3 HandsOf(Person p)
        {
            double dx, dz;
            Floor.Direction(p.Heading, out dx, out dz);
            return new P3(p.Position.X + dx * 0.28, p.Position.Y + 0.72, p.Position.Z + dz * 0.28);
        }

        // ---------- each moment ----------

        /// <param name="player">Where the visitor is.</param>
        /// <param name="storm">The rain replay is running.</param>
        /// <param name="phase">The visitor's earthquake drill.</param>
        /// <param name="intensity">How strongly the ground shakes.</param>
        public void Update(double dt, P3 player, bool storm, DrillPhase phase, double intensity)
        {
            bool quake = phase != DrillPhase.Ready && !crowd.Started;
            foreach (var n in ns)
            {
                var p = n.P;
                if (crowd.Started) { Follow(n, dt, intensity); continue; }
                if (quake)
                {
                    // down where they are, arms over the head; inside, they stay inside
                    if (n.D != Doing.Inside) { n.W.Stop(); n.D = Doing.Down; p.Pose = Pose.Duck; }
                    continue;
                }
                if (storm && (n.D == Doing.Activity || n.D == Doing.ComingOut)) GoIn(n);
                if (!storm && (n.D == Doing.Inside || n.D == Doing.GoingIn)) ComeOut(n);
                switch (n.D)
                {
                    case Doing.GoingIn:
                        p.Pose = Pose.Walk;
                        n.W.Step(p, dt);
                        if (n.W.Done) { n.D = Doing.Inside; p.Visible = false; }
                        break;
                    case Doing.ComingOut:
                        p.Pose = Pose.Walk;
                        n.W.Step(p, dt);
                        if (n.W.Done) n.D = Doing.Activity;
                        break;
                }
            }
            if (crowd.Started || quake) { if (quake) DropBall(); return; }
            Sweep(dt, player);
            Sit();
            Talk(dt);
            PlayBall(dt, player);
            PlayTag(dt, player);
            BallOut = ns[3].D == Doing.Activity || ns[5].D == Doing.Activity;
        }

        bool Busy(int k) { return ns[k].D == Doing.Activity; }

        void Sweep(double dt, P3 player)
        {
            if (!Busy(0)) return;
            var p = ns[0].P;
            p.Pose = Pose.Sweep;
            p.Heading = sweepDir > 0 ? 180 : 0;
            if (sweepPause > 0) { sweepPause -= dt; return; }
            // the visitor in the way: wait
            var ahead = new P3(p.Position.X, 0, p.Position.Z + sweepDir * 0.9);
            double along;
            if (Floor.ToSegment(player, p.Position, ahead, out along) < 0.8) return;
            double z = p.Position.Z + sweepDir * 0.22 * dt;
            if (z > SweepZ1 || z < SweepZ0) { sweepDir = -sweepDir; sweepPause = 1.2 + rnd(); z = Math.Max(SweepZ0, Math.Min(SweepZ1, z)); }
            p.Position = new P3(SweepX, -0.45, z);
        }

        void Sit()
        {
            if (!Busy(1)) return;
            var p = ns[1].P;
            p.Position = Crowd.Homes[1]; p.Heading = 90; p.Pose = Pose.Sit;
        }

        void Talk(double dt)
        {
            if (!Busy(2) || !Busy(4))
            {
                if (Busy(2)) ns[2].P.Pose = Pose.Stand;
                if (Busy(4)) ns[4].P.Pose = Pose.Stand;
                return;
            }
            talkLeft -= dt;
            if (talkLeft <= 0) { speaker = speaker == 2 ? 4 : 2; talkLeft = 3 + 2.5 * rnd(); }
            ns[2].P.Pose = speaker == 2 ? Pose.Talk : Pose.Listen;
            ns[4].P.Pose = speaker == 4 ? Pose.Talk : Pose.Listen;
            ns[2].P.Heading = Floor.HeadingTo(ns[2].P.Position, ns[4].P.Position);
            ns[4].P.Heading = Floor.HeadingTo(ns[4].P.Position, ns[2].P.Position);
        }

        void PlayBall(double dt, P3 player)
        {
            if (!Busy(3) || !Busy(5))
            {
                if (Busy(3)) ns[3].P.Pose = Pose.Stand;
                if (Busy(5)) ns[5].P.Pose = Pose.Stand;
                return;
            }
            Person a = ns[3].P, b = ns[5].P;
            a.Heading = Floor.HeadingTo(a.Position, b.Position);
            b.Heading = Floor.HeadingTo(b.Position, a.Position);
            var thrower = holder == 3 ? a : b;
            var catcher = holder == 3 ? b : a;
            if (flight >= 0)
            {
                flight += dt;
                double t = Math.Min(1, flight / FlightTime);
                // a high, soft throw
                Ball = new P3(flyFrom.X + (flyTo.X - flyFrom.X) * t, flyFrom.Y + (flyTo.Y - flyFrom.Y) * t + 0.9 * 4 * t * (1 - t), flyFrom.Z + (flyTo.Z - flyFrom.Z) * t);
                thrower.Pose = flight < 0.35 ? Pose.Throw : Pose.Stand;
                catcher.Pose = flight > FlightTime - HandsUp ? Pose.Catch : Pose.Stand;
                if (flight >= FlightTime) { flight = -1; holder = holder == 3 ? 5 : 3; holdLeft = 1.2 + 0.8 * rnd(); }
                return;
            }
            Ball = HandsOf(thrower);
            thrower.Pose = Pose.Stand; catcher.Pose = Pose.Stand;
            holdLeft -= dt;
            // nobody throws while the visitor stands between them
            double along;
            if (Floor.ToSegment(player, a.Position, b.Position, out along) < 0.8 && along > 0.05 && along < 0.95) return;
            if (holdLeft <= 0) { flight = 0; flyFrom = HandsOf(thrower); flyTo = HandsOf(catcher); }
        }

        void PlayTag(double dt, P3 player)
        {
            if (!Busy(6) || !Busy(7))
            {
                if (Busy(6)) ns[6].P.Pose = Pose.Stand;
                if (Busy(7)) ns[7].P.Pose = Pose.Stand;
                return;
            }
            Person runner = ns[6].P, chaser = ns[7].P;
            // the visitor comes into their game: they stop and look
            if (P3.Flat(player, TagMiddle) < 2.0)
            {
                foreach (var p in new[] { runner, chaser }) { p.Pose = Pose.Stand; p.Heading = Floor.HeadingTo(p.Position, player); }
                return;
            }
            tagTurn -= dt;
            if (tagTurn <= 0) { tagDir = -tagDir; tagTurn = 6 + 4 * rnd(); }
            double w = TagSpeed / ((TagRX + TagRZ) / 2);
            tagAngle += tagDir * w * dt;
            Place(runner, tagAngle);
            Place(chaser, tagAngle - tagDir * 1.1);
            runner.Pose = Pose.Run; chaser.Pose = Pose.Run;
        }

        // a place on the tag loop, facing the way round
        void Place(Person p, double angle)
        {
            p.Position = new P3(TagMiddle.X + TagRX * Math.Cos(angle), TagMiddle.Y, TagMiddle.Z + TagRZ * Math.Sin(angle));
            double tx = -TagRX * Math.Sin(angle) * tagDir, tz = TagRZ * Math.Cos(angle) * tagDir;
            p.Heading = Floor.HeadingOf(tx, tz);
        }

        void DropBall()
        {
            flight = -1;
            if (Ball.Y > -0.45 + 0.1) Ball = new P3(Ball.X, -0.45, Ball.Z);
        }

        // ---------- rain ----------

        void GoIn(N n)
        {
            var p = n.P;
            var path = new List<P3>();
            if (p.Position.X > -23.0) path.Add(new P3(-22.6, -0.45, n.DoorZ));
            path.Add(new P3(-23.3, -0.3, n.DoorZ));
            n.W.Start(path, 1.4);
            n.D = Doing.GoingIn;
            if (n.K == 3 || n.K == 5) { flight = -1; }
        }

        void ComeOut(N n)
        {
            var p = n.P;
            if (n.D == Doing.Inside) p.Position = new P3(-23.3, -0.3, n.DoorZ);
            p.Visible = true;
            var home = Crowd.Homes[n.K];
            var path = new List<P3>();
            if (home.X > -23.0) path.Add(new P3(-22.6, -0.45, n.DoorZ));
            path.Add(home);
            n.W.Start(path, 1.1);
            n.D = Doing.ComingOut;
            if (n.K == 6) tagAngle = 0;
        }

        // ---------- the walk to the evacuation center ----------

        // they go where the evacuation says, at a walk: from wherever they were when it began
        void Follow(N n, double dt, double intensity)
        {
            var p = n.P;
            var e = crowd.People[n.K];
            if (n.D == Doing.Inside) { p.Position = new P3(-23.3, -0.3, n.DoorZ); p.Visible = true; }
            n.D = Doing.Following;
            if (intensity > DrillJudge.Threshold) { p.Pose = Pose.Duck; return; }
            double d = P3.Flat(p.Position, e.Position);
            if (d > 6) { p.Position = e.Position; d = 0; }
            double step = FollowSpeed * dt;
            if (d > 0.02)
            {
                double k = Math.Min(1, step / d);
                var to = e.Position;
                p.Heading = Floor.HeadingTo(p.Position, to);
                p.Position = new P3(p.Position.X + (to.X - p.Position.X) * k, p.Position.Y + (to.Y - p.Position.Y) * k, p.Position.Z + (to.Z - p.Position.Z) * k);
                p.Pose = Pose.Walk;
            }
            else
            {
                p.Position = e.Position;
                p.Pose = e.Arrived ? Pose.Stand : e.Walking ? Pose.Walk : Pose.Stand;
                p.Heading = e.Arrived ? Floor.HeadingTo(p.Position, Crowd.CourtMiddle) : e.Heading;
            }
        }

        /// <summary>How many have reached the evacuation center.</summary>
        public int ArrivedCount
        {
            get
            {
                int c = 0;
                for (int k = 0; k < ns.Count; k++) if (crowd.People[k].Arrived && P3.Flat(ns[k].P.Position, crowd.People[k].Position) < 0.3) c++;
                return c;
            }
        }
    }
}
