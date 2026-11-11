// The rules of the Fire Safety Room, taken as they are from the TANGHALAN web app (src/rooms/logic.js), which
// took them from Fire Extinguisher 3D in the BAYANIHanda Game Library. The room itself is not yet playable in
// TANGHALAN VR; its rules are here, with their tests, so that it can be built on them.
using System;

namespace Tanghalan
{
    /// <summary>What the player is doing with the extinguisher. A null number means "not given".</summary>
    public sealed class FireState
    {
        public bool PinPulled, AimAtBase, Sweeping, Spraying;
        public double? AgentLeft, Distance;
    }

    public sealed class FireResult
    {
        public bool Easy;
        public double TimeSec;
        public double? AimAccuracy;
    }

    public static class FireRules
    {
        /// <summary>The fire drops by rate x Drop a second.</summary>
        public const double Drop = 8;
        /// <summary>On Difficult the extinguisher empties at this much a second.</summary>
        public const double AgentUse = 6;

        /// <summary>How fast the fire drops, given the technique.</summary>
        public static double ExtinguishRate(FireState s)
        {
            if (s == null || !s.PinPulled) return 0;
            if (s.AgentLeft != null && s.AgentLeft <= 0) return 0;
            double rate = s.AimAtBase ? 1.0 : 0.25;
            rate *= s.Sweeping ? 1.5 : 0.7;
            double d = s.Distance ?? 3;
            double df = 1.0;
            if (d < 1.0) df = 0.4;       // too close
            else if (d > 5) df = 0.25;   // too far to reach
            else if (d > 4) df = 0.7;
            return rate * df;
        }

        /// <summary>The fire grows back when it is not being fought well (teaches: do not stop early).</summary>
        public static double Growth(FireState s)
        {
            if (s == null) return 0;
            bool fighting = s.PinPulled && s.Spraying && s.AimAtBase && (s.AgentLeft == null || s.AgentLeft > 0);
            return fighting ? 0 : 2.2; // intensity per second
        }

        public static string NextHint(FireState s)
        {
            if (s == null) s = new FireState();
            if (!s.PinPulled) return "Pull the safety pin first.";
            if (s.AgentLeft != null && s.AgentLeft <= 0) return "The extinguisher is empty.";
            if (s.Distance != null && s.Distance > 4.2) return "Step a little closer.";
            if (s.Distance != null && s.Distance < 1.0) return "Not too close. Step back a step.";
            if (!s.Spraying) return "Hold the Squeeze button.";
            if (!s.AimAtBase) return "Aim low, at the base of the fire.";
            if (!s.Sweeping) return "Sweep from side to side.";
            return "Great, keep sweeping the base!";
        }

        public static int RateStars(FireResult r)
        {
            if (r == null) r = new FireResult();
            int s = 3;
            if (!r.Easy && r.TimeSec > 22) s -= 1;
            if ((r.AimAccuracy ?? 1) < 0.7) s -= 1;
            return Math.Max(1, s);
        }
    }
}
