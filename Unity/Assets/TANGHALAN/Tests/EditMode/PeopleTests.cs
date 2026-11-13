// Tests for the people the code moves: the walking grid of the museum, the museum's other visitors, and the
// neighbors on the street (Tanghalan.Core: NavGrid, MuseumVisitors, StreetLife, Crowd).
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tanghalan.Tests
{
    public class NavGridTests
    {
        static readonly P3 Spawn = new P3(MuseumData.SpawnX, 0, MuseumData.SpawnZ);

        static IEnumerable<PlanBox> Obstacles()
        {
            foreach (var b in MuseumPlan.Solids) if (b.Y1 > 0.1 && b.Y0 < 2.0) yield return b;
            foreach (var w in MuseumData.Works) if (w.Stand) yield return MuseumPlan.HotBox(w.Hot);
        }

        // a path stays clear of every wall and piece of furniture, by a little less than a body's width
        static void AssertClear(List<P3> path, P3 from, string what)
        {
            var obstacles = Obstacles().ToList();
            var a = from;
            foreach (var b in path)
            {
                double d = P3.Flat(a, b);
                int n = Math.Max(1, (int)(d / 0.05));
                for (int k = 0; k <= n; k++)
                {
                    double t = (double)k / n, x = a.X + (b.X - a.X) * t, z = a.Z + (b.Z - a.Z) * t;
                    foreach (var o in obstacles)
                        Assert.IsFalse(o.Covers(x, z, 0.15), what + ": the way passes through " + o.Id + " at (" + x.ToString("0.00") + ", " + z.ToString("0.00") + ")");
                }
                a = b;
            }
        }

        [Test]
        public void EveryWorkCanBeReachedAndLookedAtFromTheLobby()
        {
            var grid = NavGrid.Museum;
            Assert.IsTrue(grid.Open(Spawn.X, Spawn.Z), "the visitor's starting place is open");
            foreach (var w in MuseumData.Works)
            {
                double nx, nz;
                var spot = MuseumVisitors.ViewSpot(w, out nx, out nz);
                Assert.IsTrue(grid.Open(spot.X, spot.Z), w.Id + ": the place to look at it from is open");
                Assert.AreEqual(w.Room, MuseumData.RoomAt(spot.X, spot.Z).Id, w.Id + ": looked at from its own room");
                var path = grid.Path(Spawn, spot);
                Assert.IsNotNull(path, w.Id + " can be reached");
                AssertClear(path, Spawn, w.Id);
                Assert.Less(P3.Flat(path[path.Count - 1], spot), 1e-6, w.Id + ": the way ends there");
            }
        }

        [Test]
        public void EverySturdyTableAndTheStreetDoorCanBeReached()
        {
            var grid = NavGrid.Museum;
            foreach (var t in MuseumPlan.Tables)
                for (int side = -1; side <= 1; side += 2)
                {
                    var o = MuseumVisitors.Beside(t, side);
                    Assert.IsTrue(grid.Open(o.X, o.Z), t.Id + side + ": beside the table is open");
                    Assert.Less(P3.Flat(o, t.Slot(side)), 1.6, t.Id + side + ": and near it");
                    Assert.IsNotNull(grid.Path(Spawn, o), t.Id + " can be reached");
                    // the place under the table is under it: inside its box, and clear of the visitor's place in the middle
                    var s = t.Slot(side);
                    var box = MuseumPlan.HotBox(t.Id);
                    Assert.IsTrue(box.Covers(s.X, s.Z, 0), t.Id + side + ": the place is under the table");
                    Assert.Greater(P3.Flat(s, new P3(t.X, 0, t.Z)), 0.35, t.Id + side + ": leaves the middle to the visitor");
                }
            var door = grid.Path(Spawn, MuseumVisitors.StreetDoorInside);
            Assert.IsNotNull(door, "the street door can be reached");
            AssertClear(door, Spawn, "to the street door");
        }

        [Test]
        public void WaysBetweenAnyTwoPlacesGoThroughTheDoorways()
        {
            var grid = NavGrid.Museum;
            var rnd = Rng.Seeded(7);
            int tried = 0;
            while (tried < 60)
            {
                var a = new P3(-13 + 26 * rnd(), 0, -20 + 29 * rnd());
                var b = new P3(-13 + 26 * rnd(), 0, -20 + 29 * rnd());
                if (!grid.Open(a.X, a.Z) || !grid.Open(b.X, b.Z)) continue;
                tried++;
                var path = grid.Path(a, b);
                Assert.IsNotNull(path, "from " + a + " to " + b);
                AssertClear(path, a, "from " + a + " to " + b);
            }
        }

        [Test]
        public void AWayCanGoRoundAPersonInTheWay()
        {
            var grid = NavGrid.Museum;
            var a = new P3(0, 0, 6); var b = new P3(0, 0, 2);
            var person = new P3(0, 0, 4);
            var path = grid.Path(a, b, (x, z) => Math.Sqrt((x - person.X) * (x - person.X) + (z - person.Z) * (z - person.Z)) < 1.0);
            Assert.IsNotNull(path);
            var from = a;
            foreach (var p in path)
            {
                double along;
                Assert.Greater(Floor.ToSegment(person, from, p, out along), 0.9, "the way keeps clear of the person");
                from = p;
            }
        }
    }

    public class VisitorTests
    {
        const double Dt = 0.1;

        static void Run(MuseumVisitors v, double seconds, P3 player, string reading, DrillPhase phase, double intensity, Action<double> each = null)
        {
            for (double t = 0; t < seconds; t += Dt)
            {
                v.Update(Dt, player, reading, phase, intensity);
                if (each != null) each(t);
            }
        }

        static void AssertNotInWallsOrFurniture(MuseumVisitors v, string when)
        {
            foreach (var p in v.People)
            {
                if (p.Pose == Pose.Cover) continue; // under a table, on purpose
                foreach (var b in MuseumPlan.Solids)
                    if (b.Y1 > 0.1 && b.Y0 < 2.0)
                        Assert.IsFalse(b.Covers(p.Position.X, p.Position.Z, 0.05), when + ": " + p.Id + " stands in " + b.Id + " at " + p.Position);
            }
        }

        [Test]
        public void TheClassWalksFromWorkToWorkAndLooks()
        {
            var v = new MuseumVisitors(Rng.Seeded(11));
            Assert.AreEqual(7, v.People.Count);
            Assert.AreEqual(1, v.People.Count(p => p.Adult), "one teacher");
            var player = new P3(0, 0, 7.5);
            var seen = new HashSet<string>();
            var poses = new HashSet<Pose>();
            Run(v, 600, player, null, DrillPhase.Ready, 0, t =>
            {
                foreach (var p in v.People)
                {
                    poses.Add(p.Pose);
                    var room = MuseumData.RoomAt(p.Position.X, p.Position.Z);
                    Assert.IsNotNull(room, p.Id + " stays in the museum");
                    if (p.Pose == Pose.Look)
                        foreach (var w in MuseumData.Works)
                        {
                            double nx, nz;
                            if (P3.Flat(MuseumVisitors.ViewSpot(w, out nx, out nz), p.Position) < 1.2) seen.Add(w.Id);
                        }
                }
                if (Math.Abs(t % 10) < Dt / 2) AssertNotInWallsOrFurniture(v, "at " + t.ToString("0") + " s");
            });
            Assert.GreaterOrEqual(seen.Count, 10, "in ten minutes they look at many works: " + string.Join(", ", seen));
            CollectionAssert.IsSubsetOf(new[] { Pose.Walk, Pose.Look, Pose.Point, Pose.Talk }, poses, "they walk, look, point and explain");
        }

        [Test]
        public void TheyKeepOutOfTheVisitorsWay()
        {
            // the visitor stands right in front of a work a group starts at, and reads it
            var v = new MuseumVisitors(Rng.Seeded(5));
            var work = MuseumData.WorkById("bayanihan");
            double nx, nz;
            var spot = MuseumVisitors.ViewSpot(work, out nx, out nz);
            var player = new P3(spot.X + nx * 0.3, 0, spot.Z + nz * 0.3);
            var b = MuseumPlan.HotBox(work.Hot);
            var center = new P3(b.CX, 0, b.CZ);
            int tooClose = 0, inTheWay = 0;
            Run(v, 120, player, work.Id, DrillPhase.Ready, 0, t =>
            {
                if (t < 4) return; // a moment to step aside
                foreach (var p in v.People)
                {
                    if (p.Pose == Pose.Walk) continue;
                    if (P3.Flat(p.Position, player) < 0.6) tooClose++;
                    double along;
                    if (Floor.ToSegment(p.Position, player, center, out along) < 0.4 && along > 0.1 && along < 0.9) inTheWay++;
                }
            });
            Assert.AreEqual(0, tooClose, "nobody stands right against the visitor");
            Assert.AreEqual(0, inTheWay, "nobody stands between the visitor and the work they read");
        }

        [Test]
        public void InTheDrillTheyTakeCoverThenWalkToTheEvacuationCenter()
        {
            var v = new MuseumVisitors(Rng.Seeded(3));
            var player = new P3(0, 0, 7.5);
            Run(v, 40, player, null, DrillPhase.Ready, 0);
            // the alarm, and the shaking 3 s later
            Run(v, 3, player, null, DrillPhase.Warning, 0);
            Run(v, 4, player, null, DrillPhase.Shaking, 1);
            foreach (var p in v.People)
            {
                Assert.That(p.Pose, Is.EqualTo(Pose.Cover).Or.EqualTo(Pose.Duck), p.Id + " is down by now");
                if (p.Pose == Pose.Cover)
                    Assert.IsTrue(MuseumPlan.Tables.Any(t => P3.Flat(t.Slot(-1), p.Position) < 0.05 || P3.Flat(t.Slot(1), p.Position) < 0.05), p.Id + " is under a table");
            }
            Assert.IsTrue(v.People.Any(p => p.Pose == Pose.Cover), "some of them reach a table");
            var under = v.People.Where(p => p.Pose == Pose.Cover).Select(p => Math.Round(p.Position.X, 2) + "," + Math.Round(p.Position.Z, 2)).ToList();
            Assert.AreEqual(under.Count, under.Distinct().Count(), "one person in each place under a table");
            Run(v, 20, player, null, DrillPhase.Shaking, 1);
            var still = v.People.Select(p => p.Position).ToList();
            Run(v, 5, player, null, DrillPhase.Waiting, 0);
            for (int k = 0; k < still.Count; k++) Assert.Less(P3.Flat(still[k], v.People[k].Position), 1e-9, "they stay down until it is safe");
            // walking out; an aftershock on the way
            double t0 = 0; bool sawDuck = false;
            Run(v, 30, player, null, DrillPhase.Evacuating, 0);
            var before = v.People.Select(p => p.Position).ToList();
            Run(v, 4, player, null, DrillPhase.Aftershock, 0.45, t =>
            {
                foreach (var p in v.People) if (p.Pose == Pose.Duck) sawDuck = true;
            });
            for (int k = 0; k < before.Count; k++)
                if (v.People[k].Pose == Pose.Duck) Assert.Less(P3.Flat(before[k], v.People[k].Position), 1e-9, v.People[k].Id + " stops in the aftershock");
            Assert.IsTrue(sawDuck, "they duck in the aftershock");
            while (!v.AllArrived && t0 < 200) { v.Update(Dt, player, null, DrillPhase.Evacuating, 0); t0 += Dt; }
            Assert.IsTrue(v.AllArrived, "everyone reaches the evacuation center");
            Assert.AreEqual(7, v.ArrivedCount);
            foreach (var p in v.People) Assert.IsTrue(Evacuation.AtCenter(p.Position.X, p.Position.Z), p.Id + " is at the court");
            Assert.AreEqual(7, v.People.Select(p => Math.Round(p.Position.X, 1) + "," + Math.Round(p.Position.Z, 1)).Distinct().Count(), "each in a place of their own");
            // clear of the board: nobody between the visitor's place and the board on the court
            var board = new P3(-24, 0, 7.55);
            foreach (var p in v.People)
            {
                double along;
                Assert.Greater(Floor.ToSegment(p.Position, Evacuation.Assembly, board, out along), 0.6, p.Id + " does not stand in front of the board");
            }
        }
    }

    public class StreetLifeTests
    {
        const double Dt = 0.1;
        static readonly P3 Away = new P3(0, 0, 7.5); // the visitor, in the museum

        [Test]
        public void TheNeighborsAreOutsideDoingThings()
        {
            var crowd = new Crowd();
            var s = new StreetLife(crowd, Rng.Seeded(2));
            Assert.AreEqual(8, s.People.Count);
            double minZ = 0, maxZ = -100; int throws = 0; bool wasFlying = false; double lowest = 10;
            var poses = new HashSet<Pose>();
            for (double t = 0; t < 120; t += Dt)
            {
                s.Update(Dt, Away, false, DrillPhase.Ready, 0);
                var sweeper = s.People[0];
                minZ = Math.Min(minZ, sweeper.Position.Z); maxZ = Math.Max(maxZ, sweeper.Position.Z);
                foreach (var p in s.People)
                {
                    poses.Add(p.Pose);
                    Assert.IsTrue(p.Visible);
                    Assert.That(p.Position.X, Is.InRange(-23.3, -21.3), p.Id + " stays on the sidewalk or the doorstep");
                }
                bool flying = s.Ball.Y > -0.45 + 0.75 + 0.05;
                if (flying && !wasFlying) throws++;
                wasFlying = flying;
                lowest = Math.Min(lowest, s.Ball.Y);
            }
            Assert.That(minZ, Is.GreaterThanOrEqualTo(StreetLife.SweepZ0 - 1e-9)); Assert.That(maxZ, Is.LessThanOrEqualTo(StreetLife.SweepZ1 + 1e-9));
            Assert.Greater(maxZ - minZ, 1.5, "the sweeper sweeps up and down her stretch");
            Assert.Greater(throws, 20, "the ball goes back and forth");
            Assert.Greater(lowest, -0.45, "the ball never goes through the ground");
            CollectionAssert.IsSubsetOf(new[] { Pose.Sweep, Pose.Sit, Pose.Talk, Pose.Listen, Pose.Throw, Pose.Catch, Pose.Run }, poses);
        }

        [Test]
        public void TheyStopForTheVisitor()
        {
            var crowd = new Crowd();
            var s = new StreetLife(crowd, Rng.Seeded(4));
            var a = s.People[3].Position; var b = s.People[5].Position;
            var between = new P3((a.X + b.X) / 2, 0, (a.Z + b.Z) / 2);
            for (double t = 0; t < 5; t += Dt) s.Update(Dt, between, false, DrillPhase.Ready, 0);
            for (double t = 0; t < 20; t += Dt)
            {
                s.Update(Dt, between, false, DrillPhase.Ready, 0);
                Assert.AreNotEqual(Pose.Throw, s.People[3].Pose, "no throwing across the visitor");
                Assert.AreNotEqual(Pose.Throw, s.People[5].Pose);
            }
            for (double t = 0; t < 3; t += Dt) s.Update(Dt, StreetLife.TagMiddle, false, DrillPhase.Ready, 0);
            Assert.AreEqual(Pose.Stand, s.People[6].Pose, "the children stop their game of tag");
        }

        [Test]
        public void TheyGoInsideWhenItRainsAndComeOutAfter()
        {
            var crowd = new Crowd();
            var s = new StreetLife(crowd, Rng.Seeded(6));
            for (double t = 0; t < 5; t += Dt) s.Update(Dt, Away, false, DrillPhase.Ready, 0);
            for (double t = 0; t < 20; t += Dt) s.Update(Dt, Away, true, DrillPhase.Ready, 0);
            foreach (var p in s.People) Assert.IsFalse(p.Visible, p.Id + " is inside");
            Assert.IsFalse(s.BallOut, "the ball went in with the children");
            for (double t = 0; t < 20; t += Dt) s.Update(Dt, Away, false, DrillPhase.Ready, 0);
            foreach (var p in s.People) { Assert.IsTrue(p.Visible, p.Id + " is out again"); Assert.AreNotEqual(Pose.Walk, p.Pose, p.Id + " is back at it"); }
            Assert.IsTrue(s.BallOut);
        }

        [Test]
        public void InTheEarthquakeTheyDuckThenWalkToTheCourt()
        {
            var crowd = new Crowd();
            var s = new StreetLife(crowd, Rng.Seeded(8));
            for (double t = 0; t < 30; t += Dt) s.Update(Dt, Away, false, DrillPhase.Ready, 0);
            for (double t = 0; t < 10; t += Dt) s.Update(Dt, Away, false, DrillPhase.Shaking, 1);
            foreach (var p in s.People) Assert.AreEqual(Pose.Duck, p.Pose, p.Id + " drops and covers their head");
            crowd.Start();
            double time = 0;
            while (s.ArrivedCount < 8 && time < 90)
            {
                crowd.Step(Dt, 0);
                s.Update(Dt, Away, false, DrillPhase.Evacuating, 0);
                time += Dt;
            }
            Assert.AreEqual(8, s.ArrivedCount, "everyone reaches the court");
            foreach (var p in s.People) Assert.IsTrue(Evacuation.AtCenter(p.Position.X, p.Position.Z), p.Id);
        }

        [Test]
        public void TheCrowdStopsInTheAftershockAndStartsFromTheNeighborsPlaces()
        {
            var crowd = new Crowd();
            for (int k = 0; k < crowd.People.Count; k++)
            {
                Assert.Less(P3.Flat(crowd.People[k].Path[0], Crowd.Homes[k]), 1e-9, "starts where the neighbor is");
                var end = crowd.People[k].Path[crowd.People[k].Path.Length - 1];
                Assert.IsTrue(Evacuation.AtCenter(end.X, end.Z));
                double along;
                Assert.Greater(Floor.ToSegment(end, Evacuation.Assembly, new P3(-24, 0, 7.55), out along), 0.6, "clear of the board");
            }
            crowd.Start();
            for (int k = 0; k < 100; k++) crowd.Step(0.1, 0);
            var before = crowd.People.Select(p => p.Position).ToList();
            for (int k = 0; k < 30; k++) crowd.Step(0.1, 0.45);
            Assert.IsTrue(crowd.Ducking);
            for (int k = 0; k < before.Count; k++) Assert.Less(P3.Flat(before[k], crowd.People[k].Position), 1e-9);
        }
    }
}
