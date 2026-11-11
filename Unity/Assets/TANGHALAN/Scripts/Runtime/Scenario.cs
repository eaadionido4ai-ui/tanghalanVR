// What happens around the visitor that is not up to them: the storm over the street, the earthquake, and
// the neighbors walking to the evacuation center. GAMA runs these when it is connected (the SIMPLE way: the
// simulation lives in GAMA, the headset shows it). When GAMA is not there, or stops answering, the same
// models in Tanghalan.Core run here instead, so a class without a laptop can still play.
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Tanghalan.Vr
{
    public sealed class Scenario
    {
        /// <summary>How much faster than real time the storm runs: two and a half hours in about 80 seconds.</summary>
        public const double StormSpeed = 112;
        /// <summary>If GAMA sends nothing for this long after being asked, the local model takes over.</summary>
        const float GamaPatience = 3f;

        // the storm
        public FloodModel Flood { get; private set; }
        public bool StormRunning { get; private set; }
        public bool StormDone { get; private set; }
        public double StormTime { get { return Flood != null ? Flood.Time : 0; } }
        public double Rain { get; private set; }
        public double PeakDoor { get; private set; }
        public bool StormFromGama { get; private set; }
        float stormAsked, lastFlood;
        double localAccum;

        // the earthquake
        public double Intensity { get; private set; }
        public bool QuakeFromGama { get; private set; }
        bool quakeOn, afterOn, evacuating;
        double quakeT, afterT, evacT;
        float quakeAsked, lastQuake;
        double gamaIntensity;
        bool gamaQuakeSeen;

        // the neighbors
        public readonly Crowd Crowd = new Crowd();
        public bool CrowdFromGama { get; private set; }
        float lastPeople;

        /// <summary>A teacher started the drill from GAMA.</summary>
        public event System.Action TeacherStartsDrill;

        public Scenario()
        {
            TanghalanSimulationManager.OnTanghalanMessage += OnMessage;
        }

        public void Dispose() { TanghalanSimulationManager.OnTanghalanMessage -= OnMessage; }

        public static bool Gama { get { return TanghalanSimulationManager.Linked; } }

        // ---------- the storm ----------

        public void StartStorm(int piecesLeft)
        {
            Flood = new FloodModel(piecesLeft);
            StormRunning = true; StormDone = false; PeakDoor = 0; localAccum = 0;
            StormFromGama = Gama;
            stormAsked = lastFlood = Time.time;
            if (StormFromGama) TanghalanSimulationManager.Ask("tg_storm", "left", piecesLeft.ToString());
        }

        /// <summary>Takes the water away (the replay is over, or the drill begins).</summary>
        public void ClearStorm()
        {
            if (StormRunning && StormFromGama) TanghalanSimulationManager.Ask("tg_storm_stop");
            Flood = null; StormRunning = false; Rain = 0;
        }

        // ---------- the earthquake ----------

        public void StartQuake()
        {
            quakeOn = true; quakeT = 0; afterOn = false; evacuating = false;
            QuakeFromGama = Gama; gamaQuakeSeen = false;
            quakeAsked = lastQuake = Time.time;
            if (QuakeFromGama) TanghalanSimulationManager.Ask("tg_quake");
        }

        /// <summary>The visitor starts to walk out: the neighbors leave their houses, and an aftershock will come.</summary>
        public void StartEvacuation()
        {
            evacuating = true; evacT = 0;
            Crowd.Start();
            CrowdFromGama = Gama;
            lastPeople = Time.time;
            if (CrowdFromGama) TanghalanSimulationManager.Ask("tg_evacuate");
        }

        public void Report(string kind, string detail)
        {
            TanghalanSimulationManager.Ask("tg_event", "kind", kind, "detail", detail ?? "");
        }

        // ---------- every frame ----------

        public void Tick(float dt)
        {
            float now = Time.time;
            // the storm: GAMA's numbers while they come, otherwise our own
            if (StormRunning)
            {
                if (StormFromGama && now - lastFlood > GamaPatience) { StormFromGama = false; Debug.Log("TANGHALAN: GAMA did not send the flood; running it here."); }
                if (!StormFromGama)
                {
                    localAccum += dt * StormSpeed;
                    while (localAccum >= FloodModel.Dt && StormRunning)
                    {
                        Flood.Step(); localAccum -= FloodModel.Dt;
                        if (Flood.DoorDepth > PeakDoor) PeakDoor = Flood.DoorDepth;
                        if (Flood.Time >= FloodModel.RainSeconds + 1800) { StormRunning = false; StormDone = true; }
                    }
                    Rain = FloodModel.RainAt(Flood.Time);
                }
            }
            // the earthquake
            if (quakeOn)
            {
                quakeT += dt;
                if (QuakeFromGama && now - lastQuake > GamaPatience && quakeT < QuakeSchedule.Duration) { QuakeFromGama = false; Debug.Log("TANGHALAN: GAMA did not send the shaking; running it here."); }
                double local = QuakeSchedule.Main(quakeT);
                if (evacuating)
                {
                    evacT += dt;
                    if (!afterOn && evacT >= QuakeSchedule.AftershockAfter) { afterOn = true; afterT = 0; }
                    if (afterOn) { afterT += dt; local = QuakeSchedule.Aftershock(afterT); }
                }
                // GAMA's shaking once it comes (and while it keeps coming); until then, and without GAMA, our own
                Intensity = QuakeFromGama && gamaQuakeSeen && now - lastQuake < 1f ? gamaIntensity : local;
            }
            // the neighbors
            if (Crowd.Started)
            {
                if (CrowdFromGama && now - lastPeople > GamaPatience) { CrowdFromGama = false; Debug.Log("TANGHALAN: GAMA did not send the neighbors; moving them here."); }
                if (!CrowdFromGama) Crowd.Step(dt);
            }
        }

        // ---------- what GAMA sends ----------

        void OnMessage(string kind, JObject o)
        {
            float now = Time.time;
            switch (kind)
            {
                case "flood":
                    if (Flood == null || !StormRunning) return;
                    var d = o["d"] as JArray;
                    if (d == null) return;
                    var mm = new int[d.Count];
                    for (int i = 0; i < mm.Length; i++) mm[i] = (int)d[i];
                    Flood.SetDepthsMm(mm, (double)o["t"]);
                    Rain = (double)o["rain"] / 10.0;
                    if (Flood.DoorDepth > PeakDoor) PeakDoor = Flood.DoorDepth;
                    StormFromGama = true; lastFlood = now;
                    break;
                case "storm_end":
                    if (StormRunning && StormFromGama) { StormRunning = false; StormDone = true; Rain = 0; }
                    break;
                case "quake":
                    gamaIntensity = (double)o["i"] / 1000.0;
                    QuakeFromGama = true; gamaQuakeSeen = true; lastQuake = now;
                    break;
                case "people":
                    var p = o["p"] as JArray;
                    if (p == null) return;
                    int n = Mathf.Min(Crowd.People.Count, p.Count / 5);
                    for (int k = 0; k < n; k++)
                    {
                        var e = Crowd.People[k];
                        e.Position = new P3((double)p[5 * k] / 1000.0, (double)p[5 * k + 1] / 1000.0, (double)p[5 * k + 2] / 1000.0);
                        e.Heading = (double)p[5 * k + 3];
                        e.Arrived = (int)p[5 * k + 4] == 1;
                        e.Walking = !e.Arrived;
                    }
                    CrowdFromGama = true; lastPeople = now;
                    break;
                case "drill":
                    if (TeacherStartsDrill != null) TeacherStartsDrill();
                    break;
            }
        }
    }
}
