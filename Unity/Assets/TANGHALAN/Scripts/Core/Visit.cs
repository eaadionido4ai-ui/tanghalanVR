// One visit to TANGHALAN VR, from the lobby to the evacuation center: what the visitor has done so far, when
// the earthquake drill may begin, and the summary shown at the end.
using System.Collections.Generic;

namespace Tanghalan
{
    public sealed class Visit
    {
        /// <summary>How many works to look at before the drill unlocks by itself.</summary>
        public const int WorksToSee = 3;

        public Lang Lang = Lang.Fil;
        public readonly HashSet<string> Seen = new HashSet<string>();
        public readonly CanalCleanup Canal = new CanalCleanup();
        public bool CanalTried;
        /// <summary>The storm the visitor watched: the deepest water at Rico's door, and what it would have been with no clean-up.</summary>
        public double? StormPeakDoor, StormPeakDoorIfDirty;
        public int? GoBagStars;
        public bool CarryingGoBag;
        public readonly DrillJudge Drill = new DrillJudge();
        public bool DrillStartedByTeacher;

        public bool WorksDone { get { return Seen.Count >= WorksToSee; } }
        public bool CanalDone { get { return Canal.Left == 0 || StormPeakDoor != null; } }
        public bool GoBagDone { get { return GoBagStars != null; } }

        /// <summary>The drill unlocks when the three tasks are done (a teacher can start it any time from GAMA).</summary>
        public bool ReadyForDrill { get { return WorksDone && CanalDone && GoBagDone; } }

        public bool Over { get { return Drill.Phase == DrillPhase.Arrived; } }

        /// <summary>The lines of the summary at the evacuation center.</summary>
        public List<string> Summary()
        {
            var L = Lang;
            var lines = new List<string>();
            lines.Add(Strings.Format("end.works", L, Seen.Count, MuseumData.Works.Length));
            if (CanalTried || Canal.Left < Waste.Count)
                lines.Add(Strings.Format("end.canal", L, Waste.Count - Canal.Left, Waste.Count, Canal.Binned));
            else
                lines.Add(Strings.Get("end.canal.none", L));
            if (StormPeakDoor != null)
                lines.Add(Strings.Format("end.flood", L, Strings.Depth(StormPeakDoor.Value, L), Strings.Depth(StormPeakDoorIfDirty ?? 0, L)));
            if (GoBagStars != null)
            {
                lines.Add(Strings.Format("end.gobag", L, GoBagStars.Value));
                if (CarryingGoBag) lines.Add(Strings.Get("end.gobag.carried", L));
            }
            else lines.Add(Strings.Get("end.gobag.none", L));

            var r = Drill.Result;
            lines.Add(Strings.Format("end.drill", L, r.Stars));
            if (r.CoverAfter != null)
            {
                lines.Add(Strings.Format("end.duck", L, Secs(r.DuckAfter ?? r.CoverAfter.Value), Secs(r.CoverAfter.Value)));
                lines.Add(Strings.Format("end.hold", L, (int)System.Math.Round(r.HoldShare * 100)));
            }
            else lines.Add(Strings.Get("end.nocover", L));
            if (r.LeftDuringShaking || r.LeftBeforeSafe) lines.Add(Strings.Get("end.left", L));
            if (r.Arrived) lines.Add(Strings.Format("end.walk", L, Secs(r.WalkSeconds)));
            if (r.HadAftershock) lines.Add(Strings.Get(r.AftershockDuckAfter != null ? "end.after" : "end.after.no", L));
            lines.Add(Strings.Get("end.remember", L));
            return lines;
        }

        static string Secs(double s) { return ((int)System.Math.Round(s)).ToString(); }
    }
}
