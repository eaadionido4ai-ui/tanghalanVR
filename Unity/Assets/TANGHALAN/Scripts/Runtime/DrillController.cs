// The earthquake drill that ends the visit, and the walk to the evacuation center.
//
// 1. The earthquake comes after the go bag (DrillStart in Tanghalan.Core). Alone, that is a few seconds after
//    the bag is zipped. With a class on GAMA, GAMA starts it for everyone once every bag is zipped, or when
//    the teacher says so; until then the visitor waits, and may pack again.
//    The alarm rings, the ground trembles and Bayani calls "Duck, Cover, and Hold"; three seconds later:
// 2. The ground shakes (GAMA sends how strongly; without GAMA, QuakeSchedule). The pictures rattle, the
//    controllers rumble, a deep sound fills the room; the visitor's view itself never shakes, for comfort.
// 3. Duck (crouch for real, or press A), Cover (point at the nearest sturdy table and press the trigger: the
//    visitor is moved under it), Hold (squeeze a grip button) until the shaking stops, then wait a moment.
// 4. Walk out: arrows on the floor show the way, Bayani walks ahead, and the neighbors on the street and the
//    other visitors walk to the evacuation center too (Crowds). An aftershock comes on the way: stop and duck.
// 5. At the covered court: the head count, and the summary of the whole visit.
// DrillJudge (Tanghalan.Core) follows what the visitor does and gives the stars.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tanghalan.Vr
{
    public sealed class DrillController
    {
        readonly TanghalanApp app;
        readonly World world;
        readonly DrillJudge judge;
        readonly DrillStart start;
        readonly Dictionary<Transform, Vector3> rattleHome = new Dictionary<Transform, Vector3>();
        readonly List<GameObject> arrows = new List<GameObject>();
        Panel comeOut, summary;
        string coverId;
        bool covered, reported, shakingStarted, saidGoInside, wasWaiting;
        DrillHint lastHint = DrillHint.None;
        int zipStars, lastReady = -1, lastPlayers = -1;
        float nextHaptic, nextTell;
        Mesh arrowMesh;
        Material arrowMat;

        Lang L { get { return app.Lang; } }

        public DrillController(TanghalanApp app)
        {
            this.app = app; world = app.World;
            judge = app.Visit.Drill;
            start = app.Visit.DrillStart;
            foreach (var kv in world.Hot)
            {
                if (!kv.Key.StartsWith("drill_")) continue;
                string id = kv.Key;
                var p = kv.Value.gameObject.AddComponent<Pointable>();
                p.Reach = 4.5f;
                p.OnPress = () => TakeCover(id);
                p.CanPress = () => !covered && (judge.Phase == DrillPhase.Warning || judge.Phase == DrillPhase.Shaking);
            }
            foreach (var t in world.Pictures) rattleHome[t] = t.localPosition;
            arrowMat = world.FlatMaterial(Palette.Gold);
            arrowMesh = MakeArrow();
        }

        /// <summary>From the alarm until the visitor reaches the evacuation center.</summary>
        public bool Busy { get { return judge.Phase != DrillPhase.Ready && judge.Phase != DrillPhase.Arrived; } }

        // ---------- starting ----------

        /// <summary>The go bag was zipped: the earthquake comes after it.</summary>
        public void BagZipped(int stars)
        {
            if (judge.Phase != DrillPhase.Ready || start.Go) return;
            zipStars = stars;
            start.BagZipped();
            TellGama();
        }

        // GAMA counts the class's zipped bags. It is told again from time to time while the visitor waits, in
        // case it connected late or missed the first word.
        void TellGama()
        {
            nextTell = Time.time + 4f;
            TanghalanSimulationManager.Ask("tg_bag", "stars", zipStars.ToString());
        }

        /// <summary>GAMA's count of the class: how many go bags are zipped, of how many headsets.</summary>
        public void ClassCount(int ready, int players) { start.GamaSays(ready, players); }

        /// <summary>GAMA starts the drill: every bag of the class is zipped, or the teacher said so.</summary>
        public void CalledByGama() { if (judge.Phase == DrillPhase.Ready) start.GamaStarts(); }

        // before the alarm: is it time?
        void WaitForTheStart()
        {
            start.Update(Time.deltaTime, Scenario.Gama);
            if (start.Zipped && !start.Go && Scenario.Gama && Time.time >= nextTell) TellGama();
            // waiting for classmates who are still packing: say so once, and keep the count on the Go Bag Room's wall
            bool waiting = start.WaitingForClass;
            if (waiting != wasWaiting || waiting && (start.Ready != lastReady || start.Players != lastPlayers))
            {
                if (waiting && !wasWaiting)
                {
                    string line = Strings.Format("gobag.waiting", L, start.Ready, start.Players);
                    app.Hud.Show(line, 8); app.Bayani.Say(line);
                }
                wasWaiting = waiting; lastReady = start.Ready; lastPlayers = start.Players;
                app.GoBag.ShowWaiting();
            }
            if (!start.Go) return;
            // the tables to hide under are inside the museum: a visitor on the street is called back in first
            if (!app.OnStreet) Announce();
            else if (!saidGoInside)
            {
                saidGoInside = true;
                string line = Strings.Get("drill.inside", L);
                app.Hud.Show(line, 10); app.Bayani.Say(line, 10);
            }
        }

        // the alarm rings; the shaking follows after the short warning
        void Announce()
        {
            if (judge.Phase != DrillPhase.Ready) return;
            app.Tour.Close();
            app.Street.ClearWater();
            app.GoBag.PrepareForDrill();
            judge.Announce();
            Sfx.Play("alarm", 0.8f);
            app.Scenario.Report("drill", start.ByGama ? "started by GAMA" : "started after the go bag");
        }

        // ---------- Cover ----------

        // the table's own box: off while the visitor is under it, so nothing pushes them out
        Collider TableBox(string id)
        {
            Collider c;
            string solid = id.StartsWith("drill_gb_") ? "gb_" + id.Substring(9) : id;
            return world.Solids.TryGetValue(solid, out c) ? c : null;
        }

        void TakeCover(string id)
        {
            covered = true; coverId = id;
            var box = TableBox(id); if (box != null) box.enabled = false;
            app.Rig.FadeOut();
            var under = world.Point(id);
            var outward = world.Facing(id);
            app.Rig.Place(under, outward);
            app.Rig.EyeHeight(0.55f);
            app.Rig.CanMove(false);
            app.Rig.SetDuck(true);
            app.Rig.FadeIn();
            app.Bayani.Place(under + outward * 1.2f + Vector3.Cross(Vector3.up, outward) * 0.8f);
            app.Bayani.Crouch(true);
            Sfx.Play("pop", 0.6f);
            app.Scenario.Report("drill_cover", id);
            if (comeOut == null)
            {
                comeOut = new Panel(world, null, "Come out", 0.5f, 0.14f, Palette.Teal);
                comeOut.Button(L == Lang.Fil ? "Lumabas sa mesa" : "Come out", 0, 0, 0.48f, 0.12f, Palette.Teal, LeaveCover);
            }
            comeOut.Visible = true;
            comeOut.Place(under + outward * 0.75f + Vector3.up * 0.42f + Vector3.Cross(Vector3.up, outward) * -0.25f, under + Vector3.up * 0.55f);
        }

        void LeaveCover()
        {
            if (!covered) return;
            covered = false;
            comeOut.Visible = false;
            app.Rig.FadeOut();
            var under = world.Point(coverId);
            var outward = world.Facing(coverId);
            app.Rig.Place(under + outward * 1.0f, outward);
            var box = TableBox(coverId); if (box != null) box.enabled = true;
            app.Rig.EyeHeight(null);
            app.Rig.SetDuck(false);
            app.Rig.CanMove(true);
            app.Rig.FadeIn();
            app.Bayani.Crouch(false);
            app.Scenario.Report("drill_leave_cover", judge.Phase.ToString());
        }

        // ---------- each frame ----------

        public void Tick()
        {
            var s = app.Scenario;
            if (judge.Phase == DrillPhase.Ready)
            {
                if (!app.Started) return;
                WaitForTheStart();
                if (judge.Phase == DrillPhase.Ready) return;
            }
            var rig = app.Rig;
            if (!covered && rig.DuckPressed) rig.ToggleDuck();

            if (judge.WarningOver) BeginShaking();

            var feet = rig.Feet;
            bool atCenter = Evacuation.AtCenter(WebSpace.WebX(feet), WebSpace.WebZ(feet));
            var before = judge.Phase;
            judge.Update(Time.deltaTime, s.Intensity, rig.Ducked || covered, covered, covered && rig.Holding, atCenter);

            // the shaking you see, hear and feel; in the warning the ground only trembles
            float k = (float)s.Intensity;
            if (judge.Phase == DrillPhase.Warning) k = Mathf.Max(k, 0.12f);
            Sfx.Rumble(k);
            foreach (var kv in rattleHome)
                if (kv.Key != null) kv.Key.localPosition = kv.Value + (k > 0.01f ? Random.insideUnitSphere * 0.018f * k : Vector3.zero);
            if (k > 0.05f && Time.time > nextHaptic) { nextHaptic = Time.time + 0.12f; rig.Haptic(0.25f + 0.6f * k, 0.1f); }

            if (judge.Phase != before) OnPhase(before);
            if (judge.Hint != lastHint)
            {
                lastHint = judge.Hint;
                string line = Strings.Hint(judge.Hint, L);
                if (judge.Phase == DrillPhase.Shaking && !covered && !rig.Desktop && judge.Hint == DrillHint.Duck) line += "\n" + Strings.Get("drill.duckbtn", L);
                app.Hud.Show(line, judge.Phase == DrillPhase.Arrived ? 6 : 0);
                if (judge.Hint != DrillHint.GoodStay) app.Bayani.Say(line);
            }
            if (judge.Phase == DrillPhase.Arrived && summary != null && summary.Visible) UpdateHeadcount();
        }

        void BeginShaking()
        {
            shakingStarted = true;
            app.Scenario.StartQuake();
            judge.StartNow();
            app.Bayani.Crouch(true);
            app.Scenario.Report("drill", "shaking");
        }

        void OnPhase(DrillPhase before)
        {
            switch (judge.Phase)
            {
                case DrillPhase.Shaking:
                    if (!shakingStarted) BeginShaking();
                    break;
                case DrillPhase.Waiting:
                    app.Scenario.Report("drill", "waiting");
                    break;
                case DrillPhase.Evacuating:
                    if (before == DrillPhase.Waiting) StartWalkingOut();
                    break;
                case DrillPhase.Aftershock:
                    app.Scenario.Report("drill", "aftershock");
                    break;
                case DrillPhase.Arrived:
                    Arrive();
                    break;
            }
        }

        // ---------- walking out ----------

        void StartWalkingOut()
        {
            app.Bayani.Crouch(false);
            if (!covered) app.Rig.SetDuck(false);
            app.Scenario.StartEvacuation();
            var feet = app.Rig.Feet;
            // from the doorway side of the table, if the visitor is still under it
            if (covered) feet = world.Point(coverId) + world.Facing(coverId) * 1.0f;
            var route = Evacuation.RouteFrom(WebSpace.WebX(feet), WebSpace.WebZ(feet));
            var pts = new List<Vector3> { feet };
            foreach (var p in route) pts.Add(WebSpace.ToUnity(p));
            LayArrows(pts);
            app.Bayani.Guide(pts);
            app.Scenario.Report("drill", "evacuating");
        }

        void LayArrows(List<Vector3> pts)
        {
            foreach (var a in arrows) Object.Destroy(a);
            arrows.Clear();
            var root = new GameObject("Evacuation arrows").transform;
            float carry = 0.8f;
            for (int i = 1; i < pts.Count; i++)
            {
                Vector3 a = pts[i - 1], b = pts[i];
                var flat = b - a; flat.y = 0;
                float len = flat.magnitude;
                if (len < 0.05f) continue;
                var dir = flat / len;
                for (float d = carry; d < len; d += 1.4f)
                {
                    var p = a + (b - a) * (d / len);
                    RaycastHit hit;
                    if (Physics.Raycast(p + Vector3.up * 1.0f, Vector3.down, out hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)) p.y = hit.point.y;
                    var go = new GameObject("Arrow");
                    go.transform.SetParent(root, false);
                    go.transform.position = p + Vector3.up * 0.03f;
                    go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    go.AddComponent<MeshFilter>().sharedMesh = arrowMesh;
                    var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = arrowMat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    arrows.Add(go);
                    carry = d + 1.4f - len;
                }
                if (carry < 0) carry = 0.4f;
            }
        }

        static Mesh MakeArrow()
        {
            // a flat chevron on the floor, pointing along +z
            var m = new Mesh { name = "Arrow" };
            m.vertices = new[]
            {
                new Vector3(-0.22f, 0, -0.18f), new Vector3(0, 0, 0.12f), new Vector3(0.22f, 0, -0.18f),
                new Vector3(0.22f, 0, -0.02f), new Vector3(0, 0, 0.28f), new Vector3(-0.22f, 0, -0.02f),
            };
            m.triangles = new[] { 0, 1, 5, 1, 4, 5, 1, 2, 3, 1, 3, 4 };
            m.RecalculateNormals();
            return m;
        }

        // ---------- at the evacuation center ----------

        void Arrive()
        {
            foreach (var a in arrows) Object.Destroy(a);
            arrows.Clear();
            app.Bayani.Guide(null);
            Sfx.Play("cheer");
            var r = judge.Result;
            if (!reported)
            {
                reported = true;
                TanghalanSimulationManager.Ask("tg_arrived", "stars", r.Stars.ToString(), "walk", Mathf.RoundToInt((float)r.WalkSeconds).ToString(),
                    "duck", r.DuckAfter.HasValue ? r.DuckAfter.Value.ToString("0.0") : "-", "cover", r.CoverAfter.HasValue ? r.CoverAfter.Value.ToString("0.0") : "-",
                    "hold", Mathf.RoundToInt((float)(r.HoldShare * 100)).ToString());
            }
            app.Tour.Refresh();
            ShowSummary();
        }

        void ShowSummary()
        {
            if (summary == null) summary = new Panel(world, null, "Summary", 1.6f, 1.3f, Palette.Paper);
            summary.Clear();
            summary.Visible = true;
            summary.PlaceOnWall(world.Point("evac_board"), world.Facing("evac_board"));
            var r = judge.Result;
            summary.Box(0, 0.57f, 1.6f, 0.16f, Palette.Green);
            summary.Text(Strings.Get("end.title", L), 0, 0.57f, 1.5f, 0.14f, 0.6f, Palette.White, TextAlignmentOptions.Center, true);
            for (int i = 0; i < 3; i++) summary.Picture(i < r.Stars ? "star_on" : "star_off", -0.15f + i * 0.15f, 0.42f, 0.13f, 0.13f);
            summary.Text(string.Join("\n", app.Visit.Summary()), 0, -0.05f, 1.5f, 0.78f, 0.3f, Palette.Navy, TextAlignmentOptions.TopLeft);
            countText = summary.Text("", 0, -0.5f, 1.5f, 0.08f, 0.3f, Palette.Green, TextAlignmentOptions.Center, true);
            summary.Button(Strings.Get("end.again", L), 0.5f, -0.58f, 0.5f, 0.1f, Palette.Orange, () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
            summary.Button(Strings.Get("btn.lang", L), -0.5f, -0.58f, 0.4f, 0.1f, Palette.Teal, () => app.SetLang(L == Lang.Fil ? Lang.En : Lang.Fil));
            lastArrived = -1;
            UpdateHeadcount();
        }

        TextMeshPro countText;
        int lastArrived = -1;

        // Bayani counts the neighbors and the other visitors as they come
        void UpdateHeadcount()
        {
            var c = app.Crowds;
            int n = c.ArrivedCount, all = c.Count;
            if (n == lastArrived || countText == null) return;
            lastArrived = n;
            countText.text = n >= all ? Strings.Format("drill.headcount", L, c.Street.People.Count, c.Visitors.People.Count) : Strings.Format("drill.waiting", L, all - n);
            if (n >= all) app.Bayani.Say(countText.text);
        }

        public void Refresh()
        {
            lastHint = DrillHint.None;
            if (summary != null && summary.Visible) ShowSummary();
        }
    }
}
