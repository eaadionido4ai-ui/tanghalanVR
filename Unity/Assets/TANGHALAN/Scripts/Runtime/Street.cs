// The street from Rico's story, outside the Stop Hazards Early hall. The canal is full of trash; the visitor
// points at a piece to pick it up, and at the right bin to throw it in (nabubulok, nare-recycle,
// di-nabubulok). Then the board by the steps replays two hours of heavy rain in a minute or so: the water
// rises in the street, and the gauge at Rico's door shows how deep it gets with the trash that is left.
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Tanghalan.Vr
{
    public sealed class Street
    {
        readonly TanghalanApp app;
        readonly World world;
        readonly GameObject[] pieces = new GameObject[Waste.Count];
        readonly Vector3[] homes = new Vector3[Waste.Count];
        Panel board, gaugePanel;
        TextMeshPro gaugeText;
        Transform held;
        Mesh waterMesh; GameObject water;
        ParticleSystem rain;
        double? peakIfDirty;
        bool told, stormShown;
        float nextBoard;

        Visit V { get { return app.Visit; } }
        Lang L { get { return app.Lang; } }

        public Street(TanghalanApp app)
        {
            this.app = app; world = app.World;
            var root = new GameObject("Canal trash").transform;
            for (int k = 0; k < Waste.Count; k++)
            {
                string id = "waste_" + k.ToString("00");
                homes[k] = world.Point(id);
                var go = world.Spawn("waste_" + Waste.Pieces[k], root);
                if (go == null) continue;
                go.transform.position = homes[k];
                go.transform.rotation = Quaternion.Euler(0, k * 67 % 360, 0);
                go.transform.localScale = Vector3.one * 1.6f;
                var col = go.AddComponent<SphereCollider>(); col.radius = 0.14f; col.center = new Vector3(0, 0.06f, 0);
                var p = go.AddComponent<Pointable>();
                int piece = k;
                p.Reach = 9f;
                p.OnPress = () => PickUp(piece);
                p.CanPress = () => V.Canal.StateOf(piece) == CanalCleanup.State.InCanal && !app.Drill.Busy;
                pieces[k] = go;
            }
            foreach (Bin b in System.Enum.GetValues(typeof(Bin)))
            {
                Collider zone;
                if (!world.Zones.TryGetValue(Waste.BinId(b), out zone)) continue;
                var bin = b;
                var p = zone.gameObject.AddComponent<Pointable>();
                p.Reach = 9f;
                p.OnPress = () => DropIn(bin, zone.bounds.center + Vector3.up * 0.3f);
                p.CanPress = () => V.Canal.Held >= 0;
            }
            board = new Panel(world, null, "Street board", 1.2f, 0.95f, Palette.Paper);
            board.PlaceOnWall(world.Point("street_board"), world.Facing("street_board"));
            gaugePanel = new Panel(world, null, "Gauge", 0.62f, 0.2f, Palette.Navy);
            gaugePanel.PlaceOnWall(world.Point("gauge"), world.Facing("gauge"));
            gaugeText = gaugePanel.Text("", 0, 0, 0.58f, 0.17f, 0.32f, Palette.Gold, TextAlignmentOptions.Center, true);
            gaugePanel.Visible = false;
            BuildWater();
            BuildRain();
            Refresh();
        }

        // ---------- the trash ----------

        void PickUp(int piece)
        {
            if (V.Canal.Held >= 0 && held != null) ReturnHeld();
            if (!V.Canal.PickUp(piece)) return;
            V.CanalTried = true;
            held = pieces[piece].transform;
            Flights.Fly(held, () => HandPoint, 1.6f, 1.0f);
            Sfx.Play("pop");
            var kind = Waste.Piece(piece);
            app.Hud.Show(Strings.Format("street.holding", L, kind.Name(L)), 6);
            app.Scenario.Report("waste_picked", kind.Model);
            TanghalanSimulationManager.Ask("tg_canal", "left", V.Canal.Left.ToString());
            Refresh();
        }

        void ReturnHeld()
        {
            int k = V.Canal.Held;
            V.Canal.PutBack();
            if (k >= 0) { held.position = homes[k]; held.localScale = Vector3.one * 1.6f; }
            held = null;
        }

        void DropIn(Bin bin, Vector3 binTop)
        {
            int k = V.Canal.Held;
            if (k < 0) return;
            var kind = Waste.Piece(k);
            if (V.Canal.DropIn(bin))
            {
                var piece = held;
                Flights.Fly(piece, () => binTop, 1.0f, 0.2f, () => piece.gameObject.SetActive(false));
                Sfx.Play("drop");
                app.Bayani.Say(Strings.Format("street.right", L, kind.Why(L)));
                app.Hud.Hide();
                app.Scenario.Report("waste_binned", kind.Model + ":" + Waste.BinId(bin));
                held = null;
                if (V.Canal.Left == 0 && V.Canal.Held < 0) { app.Bayani.Say(Strings.Get("street.done", L)); Sfx.Play("cheer"); }
            }
            else
            {
                Sfx.Play("sad");
                app.Bayani.Say(Strings.Format("street.wrong", L, kind.Name(L), Waste.BinName(kind.Bin, L)));
                app.Scenario.Report("waste_wrong", kind.Model + ":" + Waste.BinId(bin));
            }
            Refresh();
        }

        Vector3 HandPoint
        {
            get
            {
                var r = app.Rig;
                if (r.Desktop || r.RightHand == null) return r.Head.transform.TransformPoint(new Vector3(0.22f, -0.22f, 0.55f));
                return r.RightHand.TransformPoint(new Vector3(0, 0, 0.12f));
            }
        }

        // ---------- the board ----------

        public void Refresh()
        {
            board.Clear();
            int level = FloodModel.WarningLevel(app.Scenario.StormRunning ? app.Scenario.Rain : 20);
            var color = level >= 3 ? Palette.Red : level == 2 ? Palette.Orange : Palette.Gold;
            board.Box(0, 0.4f, 1.2f, 0.15f, color);
            board.Text(Strings.Get("street.warning", L) + (level >= 3 ? " · RED" : level == 2 ? " · ORANGE" : " · YELLOW"), 0, 0.4f, 1.12f, 0.13f, 0.44f, Palette.White, TextAlignmentOptions.Center, true);
            board.Text(Strings.Format("street.left", L, V.Canal.Left, Waste.Count) + "\n" + Strings.Format("street.binned", L, V.Canal.Binned), 0, 0.2f, 1.1f, 0.2f, 0.34f, Palette.Navy, TextAlignmentOptions.Center);
            var s = app.Scenario;
            if (s.StormRunning)
            {
                board.Text(Strings.Get("street.storm.run", L), 0, -0.02f, 1.1f, 0.18f, 0.3f, Palette.Grey, TextAlignmentOptions.Center);
                board.Text(Strings.Format("street.storm.time", L, Mathf.RoundToInt((float)s.Rain), Mathf.RoundToInt((float)(s.StormTime / 60))) + "\n" +
                    Strings.Format("street.storm.door", L, Mathf.RoundToInt((float)(s.Flood.DoorDepth * 100))), 0, -0.25f, 1.1f, 0.22f, 0.38f, Palette.Navy, TextAlignmentOptions.Center, true);
            }
            else if (V.StormPeakDoor != null)
            {
                string line = V.StormPeakDoor.Value < 0.02
                    ? Strings.Format("street.storm.dry", L, Strings.Depth(V.StormPeakDoorIfDirty ?? 0, L))
                    : Strings.Format("street.storm.end", L, V.Canal.Left, Mathf.RoundToInt((float)(V.StormPeakDoor.Value * 100)), Mathf.RoundToInt((float)((V.StormPeakDoorIfDirty ?? 0) * 100)));
                board.Text(line, 0, -0.08f, 1.1f, 0.3f, 0.32f, Palette.Navy, TextAlignmentOptions.Center);
                var b = board.Button(Strings.Get("street.storm.again", L), 0, -0.36f, 0.7f, 0.13f, Palette.Teal, StartStorm);
                b.Pointable.CanPress = () => !app.Drill.Busy;
            }
            else
            {
                board.Text(Strings.Get("street.howto", L), 0, -0.04f, 1.1f, 0.22f, 0.3f, Palette.Grey, TextAlignmentOptions.Center);
                var b = board.Button(Strings.Get("street.storm", L), 0, -0.33f, 0.9f, 0.15f, Palette.Teal, StartStorm);
                b.Pointable.CanPress = () => !app.Drill.Busy;
            }
        }

        // ---------- the storm ----------

        void StartStorm()
        {
            if (V.Canal.Held >= 0) ReturnHeld();
            if (peakIfDirty == null) { double d, r; FloodModel.Peak(Waste.Count, out d, out r); peakIfDirty = d; }
            app.Scenario.StartStorm(V.Canal.Left);
            stormShown = true;
            gaugePanel.Visible = true;
            app.Bayani.Say(Strings.Get("street.storm.run", L));
            Refresh();
        }

        /// <summary>Takes the water away: the replay is a "what if", and the drill needs a dry street.</summary>
        public void ClearWater()
        {
            app.Scenario.ClearStorm();
            stormShown = false;
            if (water != null) water.SetActive(false);
            gaugePanel.Visible = false;
            if (rain != null) rain.Stop();
            Refresh();
        }

        void BuildWater()
        {
            water = new GameObject("Flood water");
            waterMesh = new Mesh { name = "Flood water" };
            waterMesh.MarkDynamic();
            water.AddComponent<MeshFilter>().sharedMesh = waterMesh;
            var mr = water.AddComponent<MeshRenderer>();
            mr.sharedMaterial = world.Water;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            water.SetActive(false);
        }

        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<int> tris = new List<int>();

        void UpdateWater(FloodModel f)
        {
            verts.Clear(); tris.Clear();
            for (int j = 0; j < StreetPlan.NZ; j++)
                for (int i = 0; i < StreetPlan.NX; i++)
                {
                    double d = f.Depth(i, j);
                    if (d < 0.004) continue;
                    float y = (float)f.Surface(i, j) + 0.01f;
                    double x0 = StreetPlan.X0 + i, z0 = StreetPlan.Z0 + j;
                    int b = verts.Count;
                    verts.Add(WebSpace.ToUnity(x0, y, z0)); verts.Add(WebSpace.ToUnity(x0 + 1, y, z0));
                    verts.Add(WebSpace.ToUnity(x0 + 1, y, z0 + 1)); verts.Add(WebSpace.ToUnity(x0, y, z0 + 1));
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2); tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
                }
            waterMesh.Clear();
            waterMesh.SetVertices(verts);
            waterMesh.SetTriangles(tris, 0);
            waterMesh.RecalculateNormals();
            waterMesh.RecalculateBounds();
            water.SetActive(verts.Count > 0);
        }

        void BuildRain()
        {
            var go = new GameObject("Rain");
            go.transform.position = world.Point("rain_center");
            rain = go.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rain.main;
            main.playOnAwake = false; main.loop = true;
            main.startLifetime = 0.9f; main.startSpeed = 9f; main.startSize = 0.02f;
            main.startColor = new Color(0.75f, 0.85f, 0.95f, 0.6f);
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(15, 0.1f, 26);
            shape.rotation = new Vector3(90, 0, 0); // the box's "forward" points down: rain falls
            var em = rain.emission; em.rateOverTime = 0;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 8f;
            r.sharedMaterial = new Material(world.Water);
            r.sharedMaterial.SetColor("_BaseColor", new Color(0.8f, 0.88f, 0.96f, 0.55f));
        }

        // ---------- each frame ----------

        public void OnArrive()
        {
            if (told) return;
            told = true;
            app.Bayani.Say(Strings.Get("street.arrive", L) + " " + Strings.Get("street.howto", L), 12);
        }

        public void OnLeave()
        {
            if (V.Canal.Held >= 0 && held != null) ReturnHeld();
            if (stormShown && !app.Scenario.StormRunning) ClearWater();
        }

        public void Tick()
        {
            // the piece in hand follows the hand
            if (held != null && !Flights.IsFlying(held)) held.position = HandPoint;

            var s = app.Scenario;
            if (stormShown && s.Flood != null)
            {
                UpdateWater(s.Flood);
                gaugeText.text = Strings.Depth(s.Flood.DoorDepth, L);
                float rainK = s.StormRunning ? (float)(s.Rain / 85.0) : 0;
                var em = rain.emission; em.rateOverTime = 2500 * rainK;
                if (rainK > 0 && !rain.isPlaying) rain.Play();
                if (rainK <= 0 && rain.isPlaying) rain.Stop();
                Sfx.Rain(app.OnStreet ? rainK : rainK * 0.3f);
                if (s.StormDone && V.StormPeakDoor == null || s.StormDone && Time.time > nextBoard && boardStale)
                {
                    V.StormPeakDoor = s.PeakDoor; V.StormPeakDoorIfDirty = peakIfDirty;
                    app.Scenario.Report("storm_peak_door_cm", Mathf.RoundToInt((float)(s.PeakDoor * 100)).ToString());
                    app.Tour.Refresh();
                    boardStale = false;
                    Refresh();
                }
                if (s.StormRunning) { boardStale = true; if (Time.time > nextBoard) { nextBoard = Time.time + 0.5f; Refresh(); } }
            }
            else Sfx.Rain(0);
        }

        bool boardStale;
    }
}
