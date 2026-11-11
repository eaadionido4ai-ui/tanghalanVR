// Checks of the street flood, the canal clean-up, the earthquake drill and the way to the evacuation center.
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tanghalan.Tests
{
    public class FloodTests
    {
        [Test]
        public void TheFloodMatchesTheReferenceModelStepForStep()
        {
            for (int r = 0; r < FloodGolden.Pieces.Length; r++)
            {
                var f = new FloodModel(FloodGolden.Pieces[r]);
                var clearAt = new HashSet<int>(FloodGolden.ClearAt[r]);
                int step = 0;
                foreach (var row in FloodGolden.Rows[r])
                {
                    while (step < (int)row[0]) { step++; if (clearAt.Contains(step)) f.Pieces--; f.Step(); }
                    string at = "run " + r + ", step " + step;
                    Assert.AreEqual(row[1], f.DoorDepth, 1e-9, at + ": door");
                    Assert.AreEqual(row[2], f.RoadMean, 1e-9, at + ": road");
                    Assert.AreEqual(row[3], f.Depth(StreetPlan.CanalI, 0), 1e-9, at + ": canal north");
                    Assert.AreEqual(row[4], f.Depth(StreetPlan.CanalI, StreetPlan.NZ - 1), 1e-9, at + ": canal south");
                    Assert.AreEqual(row[5], f.Volume, 1e-6, at + ": volume");
                    Assert.AreEqual(row[6], f.TotalOut, 1e-6, at + ": let out");
                }
            }
        }

        [Test]
        public void NoWaterIsMadeOrLost()
        {
            var f = new FloodModel(9);
            for (int s = 0; s < 4000; s++)
            {
                f.Step();
                if (s % 500 == 0) Assert.AreEqual(f.TotalIn, f.Volume + f.TotalOut, 1e-6, "step " + s);
            }
        }

        [Test]
        public void TheMoreTrashIsClearedTheLessDeepTheFlood()
        {
            double lastDoor = double.MaxValue, lastRoad = double.MaxValue;
            foreach (int left in new[] { 14, 12, 10, 7, 4, 0 })
            {
                double door, road;
                FloodModel.Peak(left, out door, out road);
                Assert.LessOrEqual(door, lastDoor + 1e-9, left + " left: door");
                Assert.LessOrEqual(road, lastRoad + 1e-9, left + " left: road");
                lastDoor = door; lastRoad = road;
            }
            double d14, r14, d0, r0;
            FloodModel.Peak(14, out d14, out r14); FloodModel.Peak(0, out d0, out r0);
            Assert.Greater(d14, 0.3, "with all the trash, the water is knee-deep at Rico's door");
            Assert.Less(d0, 0.02, "with a clean canal, Rico's door stays dry");
        }

        [Test]
        public void TheRainFollowsTheStormAndItsWarnings()
        {
            Assert.AreEqual(10, FloodModel.RainAt(0), 1e-9);
            Assert.AreEqual(85, FloodModel.RainAt(50 * 60), 1e-9);
            Assert.AreEqual(0, FloodModel.RainAt(FloodModel.RainSeconds + 60), 1e-9);
            Assert.AreEqual(1, FloodModel.WarningLevel(10));
            Assert.AreEqual(2, FloodModel.WarningLevel(20));
            Assert.AreEqual(3, FloodModel.WarningLevel(85));
        }

        [Test]
        public void TheGroundMatchesTheStreetModel()
        {
            Assert.AreEqual(-0.40, StreetPlan.Ground(0, 0), 1e-9, "a yard");
            Assert.AreEqual(-0.45, StreetPlan.Ground(5, 10), 1e-9, "the west sidewalk");
            Assert.AreEqual(-0.60, StreetPlan.Ground(9, 10), 1e-9, "the road");
            Assert.AreEqual(0.0, StreetPlan.Ground(13, 10), 1e-9, "the terrace");
            Assert.AreEqual(0.0, StreetPlan.Ground(2, 22), 1e-9, "the court");
            Assert.Less(StreetPlan.Ground(6, 0), StreetPlan.Ground(6, 24), "the canal runs north");
            int i, j;
            Assert.IsTrue(StreetPlan.CellAt(-23.5, -6.5, out i, out j));
            Assert.AreEqual(StreetPlan.DoorI, i); Assert.AreEqual(StreetPlan.DoorJ, j);
        }

        [Test]
        public void DepthsGoToGamaAndBackInMillimetres()
        {
            var a = new FloodModel(14); a.Run(2000);
            var mm = new int[StreetPlan.NX * StreetPlan.NZ];
            a.CopyDepthsMm(mm);
            var b = new FloodModel(14); b.SetDepthsMm(mm, a.Time);
            Assert.AreEqual(a.DoorDepth, b.DoorDepth, 0.0006);
            Assert.AreEqual(a.Time, b.Time);
        }
    }

    public class CanalTests
    {
        [Test]
        public void FourteenPiecesWithAKnownBinEach()
        {
            Assert.AreEqual(14, Waste.Count, "the street model has 14 places for trash (2+3+2+3+2+2)");
            foreach (var p in Waste.Pieces) Assert.IsNotNull(Waste.KindOf(p), p);
            Assert.AreEqual(4, Enumerable.Range(0, Waste.Count).Count(k => Waste.Piece(k).Bin == Bin.Bio));
            Assert.AreEqual(5, Enumerable.Range(0, Waste.Count).Count(k => Waste.Piece(k).Bin == Bin.Rec));
            Assert.AreEqual(5, Enumerable.Range(0, Waste.Count).Count(k => Waste.Piece(k).Bin == Bin.Res));
            foreach (var k in Waste.Kinds) foreach (Lang l in Enum.GetValues(typeof(Lang))) { Assert.IsNotEmpty(k.Name(l)); Assert.IsNotEmpty(k.Why(l)); }
        }

        [Test]
        public void APieceOutOfTheCanalNoLongerBlocksItAndOnlyTheRightBinTakesIt()
        {
            var c = new CanalCleanup();
            Assert.AreEqual(14, c.Left);
            Assert.IsTrue(c.PickUp(2)); // a banana peel
            Assert.AreEqual(13, c.Left);
            Assert.IsFalse(c.DropIn(Bin.Res), "not the residual bin");
            Assert.AreEqual(1, c.WrongTries);
            Assert.AreEqual(2, c.Held, "still in hand");
            Assert.IsTrue(c.DropIn(Bin.Bio));
            Assert.AreEqual(1, c.Binned);
            Assert.IsFalse(c.PickUp(2), "already in the bin");
            Assert.IsTrue(c.PickUp(0)); Assert.IsTrue(c.PickUp(1), "picking another puts the first one back");
            Assert.AreEqual(CanalCleanup.State.InCanal, c.StateOf(0));
            c.PutBack();
            Assert.AreEqual(13, c.Left);
        }

        [Test]
        public void BinIdsMatchTheStreetModel()
        {
            foreach (Bin b in Enum.GetValues(typeof(Bin)))
            {
                Bin back;
                Assert.IsTrue(Waste.TryBinFromId(Waste.BinId(b), out back));
                Assert.AreEqual(b, back);
            }
        }
    }

    public class DrillTests
    {
        // runs a drill with the local schedule; the visitor's behaviour is given as functions of time
        static DrillJudge Run(Func<double, bool> ducked, Func<double, bool> covered, Func<double, bool> holding, double arriveAfterWalk = 40, bool aftershockDuck = true)
        {
            var j = new DrillJudge();
            j.Announce();
            double dt = 0.1, t = 0, quake = -1, walk = -1, after = -1;
            for (int s = 0; s < 3000 && j.Phase != DrillPhase.Arrived; s++, t += dt)
            {
                if (j.CountdownOver) j.StartNow();
                if (j.Phase == DrillPhase.Shaking && quake < 0) quake = t;
                if (j.Phase == DrillPhase.Evacuating && walk < 0) walk = t;
                double q = quake < 0 ? 0 : t - quake;
                double intensity = QuakeSchedule.Main(q);
                if (walk >= 0)
                {
                    double w = t - walk;
                    if (w >= QuakeSchedule.AftershockAfter && after < 0) after = t;
                    if (after >= 0) intensity = QuakeSchedule.Aftershock(t - after);
                }
                bool inAfter = after >= 0 && t - after < QuakeSchedule.AftershockDuration;
                bool d = quake >= 0 && (inAfter ? aftershockDuck && t - after > 1.5 : ducked(q));
                bool c = quake >= 0 && walk < 0 && covered(q);
                bool h = c && holding(q);
                bool atCenter = walk >= 0 && t - walk >= arriveAfterWalk;
                j.Update(dt, intensity, d, c, h, atCenter);
            }
            return j;
        }

        [Test]
        public void TheShakingBuildsUpHoldsAndStops()
        {
            Assert.Greater(QuakeSchedule.Main(0.1), 0);
            Assert.AreEqual(1, QuakeSchedule.Main(QuakeSchedule.RampUp + 1));
            Assert.AreEqual(0, QuakeSchedule.Main(QuakeSchedule.Duration + 0.1));
            Assert.AreEqual(0, QuakeSchedule.Aftershock(QuakeSchedule.AftershockDuration));
            Assert.LessOrEqual(Enumerable.Range(0, 70).Max(k => QuakeSchedule.Aftershock(k * 0.1)), QuakeSchedule.AftershockPeak + 1e-9);
        }

        [Test]
        public void DuckingCoveringAndHoldingUntilItIsSafeEarnsThreeStars()
        {
            var j = Run(q => q > 1.5, q => q > 4, q => q > 5);
            var r = j.Result;
            Assert.AreEqual(DrillPhase.Arrived, j.Phase);
            Assert.AreEqual(1.6, r.DuckAfter.Value, 0.11);
            Assert.AreEqual(4.1, r.CoverAfter.Value, 0.11);
            Assert.Greater(r.HoldShare, 0.9);
            Assert.IsFalse(r.LeftDuringShaking); Assert.IsFalse(r.LeftBeforeSafe);
            Assert.IsTrue(r.HadAftershock, "the aftershock comes while walking out");
            Assert.IsNotNull(r.AftershockDuckAfter);
            Assert.AreEqual(3, r.Stars);
        }

        [Test]
        public void NeverTakingCoverEarnsOneStar()
        {
            var r = Run(q => false, q => false, q => false).Result;
            Assert.IsNull(r.CoverAfter);
            Assert.IsTrue(r.Arrived);
            Assert.AreEqual(1, r.Stars, "two stars lost for taking no cover");
            r = Run(q => false, q => false, q => false, 40, false).Result;
            Assert.AreEqual(1, r.Stars, "never fewer than one");
        }

        [Test]
        public void ComingOutWhileItStillShakesCostsAStar()
        {
            var r = Run(q => q > 1, q => q > 3 && q < 12, q => true).Result;
            Assert.IsTrue(r.LeftDuringShaking);
            Assert.AreEqual(2, r.Stars);
        }

        [Test]
        public void CoveringLateCostsAStar()
        {
            var r = Run(q => q > 7, q => q > 14, q => true).Result;
            Assert.AreEqual(2, r.Stars);
        }

        [Test]
        public void ThePhasesComeInOrderAndEveryHintHasWords()
        {
            var j = new DrillJudge();
            Assert.AreEqual(DrillPhase.Ready, j.Phase);
            j.Announce(); Assert.AreEqual(DrillPhase.Countdown, j.Phase);
            j.Update(0.1, 0.5, false, false, false, false);
            Assert.AreEqual(DrillPhase.Shaking, j.Phase, "shaking from GAMA starts the drill even during the countdown");
            for (int k = 0; k < 15; k++) j.Update(0.1, 0, false, true, true, false);
            Assert.AreEqual(DrillPhase.Waiting, j.Phase);
            for (int k = 0; k < 60; k++) j.Update(0.1, 0, false, true, true, false);
            Assert.AreEqual(DrillPhase.Evacuating, j.Phase);
            j.Update(0.1, 0, false, false, false, true);
            Assert.AreEqual(DrillPhase.Arrived, j.Phase);
            foreach (DrillHint h in Enum.GetValues(typeof(DrillHint)))
                if (h != DrillHint.None && h != DrillHint.Countdown)
                    foreach (Lang l in Enum.GetValues(typeof(Lang))) Assert.IsNotEmpty(Strings.Hint(h, l), h + " " + l);
        }
    }

    public class EvacuationTests
    {
        [Test]
        public void EveryRoomHasAWayOutThroughTheStreetDoor()
        {
            foreach (var room in MuseumData.Rooms)
            {
                double x = (room.X0 + room.X1) / 2, z = (room.Z0 + room.Z1) / 2;
                var route = Evacuation.RouteFrom(x, z);
                Assert.Greater(route.Count, 5, room.Id);
                var last = route[route.Count - 1];
                Assert.IsTrue(Evacuation.AtCenter(last.X, last.Z), room.Id + " ends at the evacuation center");
                // inside the museum, each leg stays in one room or crosses a doorway
                var here = new P3(x, 0, z);
                foreach (var p in route)
                {
                    var a = MuseumData.RoomAt(here.X, here.Z); var b = MuseumData.RoomAt(p.X, p.Z);
                    if (a != null && b != null && a != b)
                    {
                        var d = MuseumData.Doors.FirstOrDefault(o => o.Other(a.Id) == b.Id);
                        Assert.IsNotNull(d, room.Id + ": " + a.Id + " to " + b.Id + " has no doorway");
                        double t = d.Axis == 'x' ? (d.At - here.Z) / (p.Z - here.Z) : (d.At - here.X) / (p.X - here.X);
                        double cross = d.Axis == 'x' ? here.X + (p.X - here.X) * t : here.Z + (p.Z - here.Z) * t;
                        Assert.That(cross, Is.InRange(Math.Min(d.From, d.To) + 0.2, Math.Max(d.From, d.To) - 0.2), room.Id + ": through the doorway");
                    }
                    here = p;
                }
                Assert.Less(Evacuation.Length(route), 60, room.Id + " is not far");
            }
        }

        [Test]
        public void TheStreetDoorLeadsFromStopHazardsEarly()
        {
            var d = MuseumData.Doors.FirstOrDefault(o => o.B == "kalye");
            Assert.IsNotNull(d, "the street door is in the exported doorways");
            Assert.AreEqual("mit", d.A);
            Assert.AreEqual(Evacuation.StreetDoorX, d.At, 1e-9);
            Assert.AreEqual(Evacuation.StreetDoorZ, d.MidZ, 1e-9);
            Assert.AreEqual(2, Evacuation.DoorsTo("lobby", "mit").Count);
            Assert.AreEqual(0, Evacuation.DoorsTo("mit", "mit").Count);
        }

        [Test]
        public void FromTheStreetTheRouteGoesOnTowardsTheCourt()
        {
            var r = Evacuation.RouteFrom(-17.0, -2.7);
            Assert.Less(r[0].X, -17.0, "it does not lead back to the steps");
            Assert.IsTrue(Evacuation.AtCenter(r[r.Count - 1].X, r[r.Count - 1].Z));
        }

        [Test]
        public void TheNeighborsAllReachTheCourtSoon()
        {
            var c = new Crowd();
            Assert.AreEqual(8, c.People.Count);
            c.Step(10);
            Assert.AreEqual(0, c.ArrivedCount, "nobody moves before the shaking stops");
            c.Start();
            double t = 0;
            while (c.ArrivedCount < c.People.Count && t < 120) { c.Step(0.1); t += 0.1; }
            Assert.AreEqual(8, c.ArrivedCount);
            Assert.Less(t, 40, "all there within 40 s");
            foreach (var p in c.People) Assert.IsTrue(Evacuation.AtCenter(p.Position.X, p.Position.Z), p.Id);
            var spots = c.People.Select(p => Math.Round(p.Position.X, 1) + "," + Math.Round(p.Position.Z, 1)).Distinct().Count();
            Assert.AreEqual(8, spots, "each stands in a place of their own");
        }
    }

    public class ContentTests
    {
        [Test]
        public void TheMuseumHasItsNineRoomsAndNineteenWorks()
        {
            Assert.AreEqual(9, MuseumData.Rooms.Length);
            Assert.AreEqual(19, MuseumData.Works.Length);
            Assert.AreEqual(19, MuseumData.Works.Select(w => w.Hot).Distinct().Count());
            foreach (var w in MuseumData.Works)
            {
                Assert.IsNotNull(MuseumData.RoomById(w.Room), w.Id);
                foreach (Lang l in Enum.GetValues(typeof(Lang)))
                {
                    Assert.IsNotEmpty(w.Note(l), w.Id);
                    Assert.IsNotEmpty(w.PageText(0, l), w.Id);
                    Assert.AreEqual(w.PageCount, w.Pages(l) == null ? 1 : w.Pages(l).Length, w.Id + " has as many pages in both languages");
                }
            }
        }

        [Test]
        public void EveryTextIsInBothLanguagesWithTheSamePlaceholders()
        {
            foreach (var key in Strings.Keys)
            {
                string en = Strings.Get(key, Lang.En), fil = Strings.Get(key, Lang.Fil);
                Assert.IsNotEmpty(en, key); Assert.IsNotEmpty(fil, key);
                for (int k = 0; k < 4; k++)
                    Assert.AreEqual(en.Contains("{" + k + "}"), fil.Contains("{" + k + "}"), key + " {" + k + "}");
            }
            Assert.AreEqual("tuyo", Strings.Depth(0, Lang.Fil));
            StringAssert.Contains("knee", Strings.Depth(0.41, Lang.En));
        }

        [Test]
        public void TheVisitSummaryReadsInBothLanguages()
        {
            var v = new Visit();
            v.Seen.Add("kanal"); v.Seen.Add("gobag"); v.Seen.Add("dch");
            v.Canal.PickUp(0); v.Canal.DropIn(Bin.Rec);
            v.GoBagStars = 3; v.CarryingGoBag = true;
            Assert.IsFalse(v.ReadyForDrill, "13 pieces left and no storm watched yet");
            v.StormPeakDoor = 0.26; v.StormPeakDoorIfDirty = 0.41;
            Assert.IsTrue(v.ReadyForDrill);
            foreach (Lang l in Enum.GetValues(typeof(Lang)))
            {
                v.Lang = l;
                var lines = v.Summary();
                Assert.Greater(lines.Count, 5);
                foreach (var line in lines) StringAssert.DoesNotContain("{", line);
            }
        }
    }
}
