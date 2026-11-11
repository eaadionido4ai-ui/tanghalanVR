// The rules of the Go Bag Room, taken as they are from the TANGHALAN web app (src/rooms/logic.js), which took
// them from Go Bag Packing 3D in the BAYANIHanda Game Library: a bag that holds 14 weight units, a round of
// 9 useful things and 5 that could stay home, and the stars. The 24 items are in Generated/GoBagItems.g.cs.
using System;
using System.Collections.Generic;

namespace Tanghalan
{
    public sealed class GoBagItem
    {
        public readonly string Id, Name, NameFil, Info, InfoFil;
        public readonly bool Good;
        public readonly int Weight;

        public GoBagItem(string id, string name, string nameFil, bool good, int weight, string info, string infoFil)
        {
            Id = id; Name = name; NameFil = nameFil; Good = good; Weight = weight; Info = info; InfoFil = infoFil;
        }

        public string NameIn(Lang lang) { return lang == Lang.Fil ? NameFil : Name; }
        public string InfoIn(Lang lang) { return lang == Lang.Fil ? InfoFil : Info; }
    }

    public struct GoBagScore
    {
        public int TotalGood, PackedGood, MissedGood, WrongPacks;
    }

    public static class GoBagRules
    {
        /// <summary>How much the bag holds, in the items' weight units.</summary>
        public const int Capacity = 14;
        const int Ess = 9, Dis = 5; // a round: 9 useful things and 5 that could stay home

        public static IReadOnlyList<GoBagItem> Items { get { return GoBagItems.All; } }

        public static GoBagItem ById(string id)
        {
            for (int i = 0; i < GoBagItems.All.Length; i++) if (GoBagItems.All[i].Id == id) return GoBagItems.All[i];
            return null;
        }

        public static List<T> Shuffle<T>(IEnumerable<T> arr, Rand rnd)
        {
            var a = new List<T>(arr);
            for (int i = a.Count - 1; i > 0; i--)
            {
                int j = (int)Math.Floor(rnd() * (i + 1));
                T t = a[i]; a[i] = a[j]; a[j] = t;
            }
            return a;
        }

        /// <summary>A round's things, mixed: essCount useful ones and disCount that could stay home.</summary>
        public static List<GoBagItem> BuildRound(Rand rnd, int essCount = Ess, int disCount = Dis)
        {
            var good = new List<GoBagItem>(); var bad = new List<GoBagItem>();
            foreach (var x in GoBagItems.All) (x.Good ? good : bad).Add(x);
            int ne = Math.Min(essCount, good.Count), nd = Math.Min(disCount, bad.Count);
            var round = Shuffle(good, rnd).GetRange(0, ne);
            round.AddRange(Shuffle(bad, rnd).GetRange(0, nd));
            return Shuffle(round, rnd);
        }

        public static int BagWeight(IEnumerable<GoBagItem> items)
        {
            int w = 0;
            foreach (var it in items) w += it.Weight;
            return w;
        }

        public static bool IsOverCapacity(IEnumerable<GoBagItem> items, int cap = Capacity)
        {
            return BagWeight(items) > cap;
        }

        public static GoBagScore ScoreRound(IEnumerable<GoBagItem> tray, ICollection<string> packedIds)
        {
            var r = new GoBagScore();
            foreach (var it in tray)
            {
                bool packed = packedIds.Contains(it.Id);
                if (it.Good) { r.TotalGood++; if (packed) r.PackedGood++; else r.MissedGood++; }
                else if (packed) r.WrongPacks++;
            }
            return r;
        }

        public static int RateStars(GoBagScore r)
        {
            double ratio = r.TotalGood > 0 ? (double)r.PackedGood / r.TotalGood : 0;
            int s = ratio >= 0.9 ? 3 : ratio >= 0.6 ? 2 : 1;
            if (r.WrongPacks >= 3) s = Math.Max(1, s - 1);
            return s;
        }
    }
}
