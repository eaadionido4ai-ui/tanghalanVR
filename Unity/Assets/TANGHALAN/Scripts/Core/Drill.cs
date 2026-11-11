// The earthquake drill that ends the visit: the ground shakes, the visitor does Duck, Cover and Hold under a
// sturdy table, waits until it is safe, then walks to the evacuation center on the street. On the way there
// an aftershock may come, and then the visitor stops and ducks, protecting the head.
//
// QuakeSchedule says how strongly the ground shakes over time. GAMA runs the same schedule and sends it to
// Unity (GAMA/models/TANGHALAN.gaml); without GAMA, Unity runs it itself.
// DrillJudge follows what the visitor does and gives the result. It only needs to know how strong the
// shaking is from moment to moment, so it works the same with either source.
using System;

namespace Tanghalan
{
    public static class QuakeSchedule
    {
        /// <summary>Seconds of the main shaking: it builds up, holds, then dies down.</summary>
        public const double RampUp = 3, Strong = 13, RampDown = 8;
        public const double Duration = RampUp + Strong + RampDown;
        /// <summary>The aftershock: when it comes after the visitor starts to walk out, and how long it lasts.</summary>
        public const double AftershockAfter = 25, AftershockDuration = 6, AftershockPeak = 0.45;
        /// <summary>PHIVOLCS Earthquake Intensity Scale: what the strongest shaking of the drill is called.</summary>
        public const string IntensityLabelEn = "PEIS Intensity VI: Very strong";
        public const string IntensityLabelFil = "PEIS Intensity VI: Napakalakas";

        /// <summary>How strongly the ground shakes (0 to 1), t seconds after the main shaking began.</summary>
        public static double Main(double t)
        {
            if (t < 0) return 0;
            if (t < RampUp) return 0.25 + 0.75 * t / RampUp;
            if (t < RampUp + Strong) return 1.0;
            if (t < Duration) return 1.0 - (t - RampUp - Strong) / RampDown;
            return 0;
        }

        /// <summary>How strongly the aftershock shakes, t seconds after it began.</summary>
        public static double Aftershock(double t)
        {
            if (t < 0 || t >= AftershockDuration) return 0;
            double k = t < 1 ? t : t > AftershockDuration - 2 ? (AftershockDuration - t) / 2 : 1;
            return AftershockPeak * k;
        }
    }

    public enum DrillPhase
    {
        /// <summary>The drill has not started.</summary>
        Ready,
        /// <summary>The drill was announced; the shaking starts when the countdown ends.</summary>
        Countdown,
        /// <summary>The ground is shaking: Duck, Cover, Hold.</summary>
        Shaking,
        /// <summary>The shaking stopped: stay under cover a few seconds more and look around.</summary>
        Waiting,
        /// <summary>Walk out calmly to the evacuation center.</summary>
        Evacuating,
        /// <summary>An aftershock while walking out: stop, duck and protect your head.</summary>
        Aftershock,
        /// <summary>At the evacuation center.</summary>
        Arrived,
    }

    /// <summary>What Bayani tells the visitor to do now. Strings.Hint gives the words.</summary>
    public enum DrillHint
    {
        None, Countdown, Duck, Cover, Hold, GoodStay, StayUnderCover, Wait, WalkOut, KeepWalking,
        AftershockDuck, AftershockGood, Arrived,
    }

    public struct DrillResult
    {
        /// <summary>Seconds from the start of the shaking until the visitor ducked, and until they were under cover (null: never).</summary>
        public double? DuckAfter, CoverAfter;
        /// <summary>Of the shaking that came after they took cover, how much of it they held on (0 to 1).</summary>
        public double HoldShare;
        /// <summary>True if they came out from under cover while it still shook, or before it was safe.</summary>
        public bool LeftDuringShaking, LeftBeforeSafe;
        public bool Arrived;
        /// <summary>Seconds from "walk out" to the evacuation center.</summary>
        public double WalkSeconds;
        public bool HadAftershock;
        /// <summary>Seconds from the start of the aftershock until they ducked (null: they did not).</summary>
        public double? AftershockDuckAfter;
        public int Stars;
    }

    public sealed class DrillJudge
    {
        /// <summary>Seconds of countdown after the drill is announced.</summary>
        public double CountdownSeconds = 10;
        /// <summary>Seconds to stay under cover after the shaking stops, before walking out.</summary>
        public double WaitSeconds = 5;
        /// <summary>Shaking weaker than this counts as no shaking.</summary>
        public const double Threshold = 0.05;
        /// <summary>The shaking must stay below the threshold this long to count as stopped.</summary>
        public const double StillFor = 1.0;

        public DrillPhase Phase { get; private set; }
        public DrillHint Hint { get; private set; }
        /// <summary>Seconds since the phase began.</summary>
        public double PhaseTime { get; private set; }

        double shakeStart, shakeTime, still, holdTime, coveredShaking, walkStart, walkEnd, aftershockStart;
        double? duckAt, coverAt, aftershockDuckAt;
        bool wasCovered, leftDuringShaking, leftBeforeSafe, hadAftershock, arrived;
        double clock;

        public void Announce()
        {
            if (Phase != DrillPhase.Ready) return;
            Set(DrillPhase.Countdown, DrillHint.Countdown);
        }

        /// <summary>Starts the drill straight away, without a countdown (when GAMA starts it).</summary>
        public void StartNow()
        {
            if (Phase == DrillPhase.Ready || Phase == DrillPhase.Countdown) BeginShaking();
        }

        public bool CountdownOver { get { return Phase == DrillPhase.Countdown && PhaseTime >= CountdownSeconds; } }

        void Set(DrillPhase p, DrillHint h) { Phase = p; Hint = h; PhaseTime = 0; }

        void BeginShaking()
        {
            Set(DrillPhase.Shaking, DrillHint.Duck);
            shakeStart = clock; shakeTime = 0; still = 0;
        }

        /// <param name="dt">Seconds since the last update.</param>
        /// <param name="intensity">How strongly the ground shakes now (0 to 1).</param>
        /// <param name="ducked">The visitor is ducking (head low, or the duck button).</param>
        /// <param name="covered">The visitor is under a sturdy table.</param>
        /// <param name="holding">The visitor is holding on (the grip button, while under the table).</param>
        /// <param name="atCenter">The visitor is at the evacuation center.</param>
        public void Update(double dt, double intensity, bool ducked, bool covered, bool holding, bool atCenter)
        {
            clock += dt; PhaseTime += dt;
            bool shaking = intensity > Threshold;
            switch (Phase)
            {
                case DrillPhase.Countdown:
                    if (shaking) BeginShaking(); // GAMA started it before the countdown ended
                    break;

                case DrillPhase.Shaking:
                    shakeTime += dt;
                    if (ducked && duckAt == null) duckAt = clock - shakeStart;
                    if (covered && coverAt == null) { coverAt = clock - shakeStart; if (duckAt == null) duckAt = coverAt; }
                    if (covered && shaking) { coveredShaking += dt; if (holding) holdTime += dt; }
                    if (wasCovered && !covered && shaking) leftDuringShaking = true;
                    if (!shaking) still += dt; else still = 0;
                    if (still >= StillFor) { Set(DrillPhase.Waiting, DrillHint.Wait); break; }
                    Hint = !shaking ? DrillHint.GoodStay
                        : wasCovered && !covered ? DrillHint.StayUnderCover
                        : !covered && !ducked ? DrillHint.Duck
                        : !covered ? DrillHint.Cover
                        : !holding ? DrillHint.Hold
                        : DrillHint.GoodStay;
                    break;

                case DrillPhase.Waiting:
                    if (wasCovered && !covered) leftBeforeSafe = true;
                    if (shaking) { Set(DrillPhase.Shaking, DrillHint.Duck); still = 0; break; }
                    if (PhaseTime >= WaitSeconds) { Set(DrillPhase.Evacuating, DrillHint.WalkOut); walkStart = clock; }
                    break;

                case DrillPhase.Evacuating:
                    if (atCenter) { Arrive(); break; }
                    if (shaking && !hadAftershock) { hadAftershock = true; aftershockStart = clock; Set(DrillPhase.Aftershock, DrillHint.AftershockDuck); break; }
                    if (PhaseTime > 8) Hint = DrillHint.KeepWalking;
                    break;

                case DrillPhase.Aftershock:
                    if (ducked && aftershockDuckAt == null) aftershockDuckAt = clock - aftershockStart;
                    Hint = aftershockDuckAt != null ? DrillHint.AftershockGood : DrillHint.AftershockDuck;
                    if (!shaking) still += dt; else still = 0;
                    if (still >= StillFor) { still = 0; Set(DrillPhase.Evacuating, DrillHint.KeepWalking); }
                    break;

                case DrillPhase.Arrived:
                    break;
            }
            wasCovered = covered;
        }

        void Arrive()
        {
            arrived = true;
            Set(DrillPhase.Arrived, DrillHint.Arrived);
            walkEnd = clock;
        }

        public DrillResult Result
        {
            get
            {
                var r = new DrillResult
                {
                    DuckAfter = duckAt, CoverAfter = coverAt,
                    HoldShare = coveredShaking > 0 ? holdTime / coveredShaking : 0,
                    LeftDuringShaking = leftDuringShaking, LeftBeforeSafe = leftBeforeSafe,
                    Arrived = arrived, WalkSeconds = arrived ? walkEnd - walkStart : 0,
                    HadAftershock = hadAftershock, AftershockDuckAfter = aftershockDuckAt,
                };
                r.Stars = Stars(r);
                return r;
            }
        }

        /// <summary>
        /// Three stars: ducked within 5 seconds, under cover within 12, held on for most of the shaking, and
        /// stayed under cover until it was safe. One star is lost for being slow or never taking cover, one for
        /// letting go or coming out too early, and one for not ducking in the aftershock. Never fewer than one.
        /// </summary>
        public static int Stars(DrillResult r)
        {
            int s = 3;
            if (r.CoverAfter == null || r.CoverAfter > 12 || r.DuckAfter == null || r.DuckAfter > 5) s--;
            if (r.CoverAfter == null || r.HoldShare < 0.6 || r.LeftDuringShaking || r.LeftBeforeSafe) s--;
            if (r.HadAftershock && (r.AftershockDuckAfter == null || r.AftershockDuckAfter > 4)) s--;
            return Math.Max(1, s);
        }
    }
}
