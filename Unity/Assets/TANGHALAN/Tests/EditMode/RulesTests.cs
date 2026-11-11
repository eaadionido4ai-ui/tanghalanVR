// The web app's checks of the game rules (tests/logic.test.mjs), in C#, plus a check that the C# rules build
// the very same rounds as the web app for the same seeds.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tanghalan.Tests
{
    public class GoBagRulesTests
    {
        [Test]
        public void TheItemsAreUniqueEachWithNamesNotesAndAWeight()
        {
            var ids = new HashSet<string>(GoBagRules.Items.Select(x => x.Id));
            Assert.AreEqual(GoBagRules.Items.Count, ids.Count);
            Assert.AreEqual(24, GoBagRules.Items.Count);
            foreach (var it in GoBagRules.Items)
            {
                Assert.IsNotEmpty(it.Name, it.Id); Assert.IsNotEmpty(it.Info, it.Id);
                Assert.IsNotEmpty(it.NameFil, it.Id); Assert.IsNotEmpty(it.InfoFil, it.Id);
                Assert.Greater(it.Weight, 0, it.Id);
            }
            Assert.GreaterOrEqual(GoBagRules.Items.Count(x => x.Good), 9, "enough useful things for a round");
            Assert.GreaterOrEqual(GoBagRules.Items.Count(x => !x.Good), 5, "enough things that could stay home");
        }

        [Test]
        public void ARoundHasNineUsefulThingsAndFiveThatCouldStayHome()
        {
            for (int k = 1; k <= 50; k++)
            {
                var round = GoBagRules.BuildRound(Rng.Seeded(k));
                Assert.AreEqual(14, round.Count);
                Assert.AreEqual(14, round.Select(x => x.Id).Distinct().Count());
                Assert.AreEqual(9, round.Count(x => x.Good));
                Assert.AreEqual(5, round.Count(x => !x.Good));
            }
        }

        [Test]
        public void TheRoundsAreTheSameAsTheWebAppsForTheSameSeed()
        {
            // from the web app: gobag.buildRound(undefined, undefined, seeded(k)) in src/rooms/logic.js
            var web = new Dictionary<int, string>
            {
                { 1, "batteries firstaid tv documents mask soda toy clothes hairdryer radio powerbank icecream books contacts" },
                { 2, "soda documents bricks whistle flashlight toy books powerbank tv meds clothes mask hairdryer food" },
                { 7, "powerbank cash food mask beachball firstaid balloon hairdryer toy soda radio icecream meds contacts" },
                { 42, "books clothes flashlight bricks whistle console mask radio contacts documents tv cash toy beachball" },
            };
            foreach (var kv in web)
                Assert.AreEqual(kv.Value, string.Join(" ", GoBagRules.BuildRound(Rng.Seeded(kv.Key)).Select(x => x.Id)), "seed " + kv.Key);
        }

        [Test]
        public void TheFourteenThingsFitTheSlotsOfBothLayouts()
        {
            Assert.AreEqual(14, new[] { 5, 5, 4 }.Sum(), "three tables on Easy");
            Assert.AreEqual(14, new[] { 4, 4, 3, 3 }.Sum(), "cabinet, drawers, crate and cooler on Difficult");
        }

        [Test]
        public void TheBagsWeightAddsUpAndMoreThanTheLimitIsTooHeavy()
        {
            var water = GoBagRules.ById("water"); var food = GoBagRules.ById("food");
            Assert.AreEqual(water.Weight + food.Weight, GoBagRules.BagWeight(new[] { water, food }));
            Assert.IsFalse(GoBagRules.IsOverCapacity(new[] { water, food }));
            var heavy = GoBagRules.Items.OrderByDescending(x => x.Weight).ToList();
            var pile = new List<GoBagItem>();
            while (GoBagRules.BagWeight(pile) <= GoBagRules.Capacity) pile.Add(heavy[pile.Count]);
            Assert.IsTrue(GoBagRules.IsOverCapacity(pile));
        }

        [Test]
        public void PackingEveryUsefulThingAndNothingElseEarnsThreeStars()
        {
            var round = GoBagRules.BuildRound(Rng.Seeded(7));
            var r = GoBagRules.ScoreRound(round, round.Where(x => x.Good).Select(x => x.Id).ToList());
            Assert.AreEqual(9, r.TotalGood); Assert.AreEqual(9, r.PackedGood); Assert.AreEqual(0, r.MissedGood); Assert.AreEqual(0, r.WrongPacks);
            Assert.AreEqual(3, GoBagRules.RateStars(r));
        }

        [Test]
        public void PackingEverythingCostsAStarAndPackingTooLittleEarnsFewer()
        {
            var round = GoBagRules.BuildRound(Rng.Seeded(11));
            var all = GoBagRules.ScoreRound(round, round.Select(x => x.Id).ToList());
            Assert.AreEqual(5, all.WrongPacks);
            Assert.AreEqual(2, GoBagRules.RateStars(all));
            var good = round.Where(x => x.Good).Select(x => x.Id).ToList();
            Assert.AreEqual(2, GoBagRules.RateStars(GoBagRules.ScoreRound(round, good.Take(6).ToList())));
            Assert.AreEqual(1, GoBagRules.RateStars(GoBagRules.ScoreRound(round, good.Take(3).ToList())));
            Assert.AreEqual(1, GoBagRules.RateStars(GoBagRules.ScoreRound(round, new List<string>())));
        }
    }

    public class FireRulesTests
    {
        static FireState S(bool pin, bool aim, bool sweep, double? dist, double? agent = null, bool spray = false)
        {
            return new FireState { PinPulled = pin, AimAtBase = aim, Sweeping = sweep, Distance = dist, AgentLeft = agent, Spraying = spray };
        }

        [Test]
        public void TheExtinguisherDoesNothingUntilThePinIsPulledOrOnceItIsEmpty()
        {
            Assert.AreEqual(0, FireRules.ExtinguishRate(S(false, true, true, 2.5)));
            Assert.AreEqual(0, FireRules.ExtinguishRate(S(true, true, true, 2.5, 0)));
        }

        [Test]
        public void AimingLowAndSweepingFromASafeDistanceWorksBest()
        {
            double best = FireRules.ExtinguishRate(S(true, true, true, 2.5));
            Assert.AreEqual(1.5, best, 1e-12);
            Assert.Less(FireRules.ExtinguishRate(S(true, false, true, 2.5)), best);
            Assert.Less(FireRules.ExtinguishRate(S(true, true, false, 2.5)), best);
            Assert.Less(FireRules.ExtinguishRate(S(true, true, true, 0.6)), best, "too close");
            Assert.Less(FireRules.ExtinguishRate(S(true, true, true, 5.5)), best, "too far");
        }

        [Test]
        public void TheFireGrowsBackUnlessItIsFoughtAtTheBase()
        {
            Assert.AreEqual(0, FireRules.Growth(S(true, true, false, null, 50, true)));
            Assert.AreEqual(2.2, FireRules.Growth(S(true, false, false, null, 50, true)));
            Assert.AreEqual(2.2, FireRules.Growth(S(true, true, false, null, 50, false)));
            Assert.AreEqual(2.2, FireRules.Growth(S(true, true, false, null, 0, true)));
        }

        [Test]
        public void TheHintsFollowPASSInOrder()
        {
            Assert.AreEqual("Pull the safety pin first.", FireRules.NextHint(new FireState()));
            Assert.AreEqual("Step a little closer.", FireRules.NextHint(S(true, false, false, 6)));
            Assert.AreEqual("Not too close. Step back a step.", FireRules.NextHint(S(true, false, false, 0.5)));
            Assert.AreEqual("Hold the Squeeze button.", FireRules.NextHint(S(true, false, false, 2.5)));
            Assert.AreEqual("Aim low, at the base of the fire.", FireRules.NextHint(S(true, false, false, 2.5, null, true)));
            Assert.AreEqual("Sweep from side to side.", FireRules.NextHint(S(true, true, false, 2.5, null, true)));
            Assert.AreEqual("Great, keep sweeping the base!", FireRules.NextHint(S(true, true, true, 2.5, null, true)));
            Assert.AreEqual("The extinguisher is empty.", FireRules.NextHint(S(true, false, false, null, 0)));
        }

        [Test]
        public void SlowOnDifficultOrPoorAimCostAStarEach()
        {
            Assert.AreEqual(3, FireRules.RateStars(new FireResult { TimeSec = 12, AimAccuracy = 0.95 }));
            Assert.AreEqual(2, FireRules.RateStars(new FireResult { TimeSec = 30, AimAccuracy = 0.95 }));
            Assert.AreEqual(3, FireRules.RateStars(new FireResult { TimeSec = 30, AimAccuracy = 0.95, Easy = true }), "no clock on Easy");
            Assert.AreEqual(1, FireRules.RateStars(new FireResult { TimeSec = 30, AimAccuracy = 0.5 }));
        }

        [Test]
        public void AGoodTechniquePutsOutAFullFireBeforeADifficultExtinguisherRunsOut()
        {
            double secsToOut = 100 / (1.5 * FireRules.Drop), secsOfAgent = 100 / FireRules.AgentUse;
            Assert.Less(secsToOut, secsOfAgent);
            double offTarget = FireRules.ExtinguishRate(S(true, false, false, 2.5)) * FireRules.Drop - 2.2;
            Assert.Less(offTarget, 0, "the fire wins when you aim too high");
        }
    }
}
