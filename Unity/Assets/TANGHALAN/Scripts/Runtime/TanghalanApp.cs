// TANGHALAN VR: the whole visit, from the lobby to the evacuation center.
//
// This component sits in the main scene (Assets/TANGHALAN/Scenes/Main Scene) next to the SIMPLE template's
// Connection Manager, Game Manager (TanghalanSimulationManager) and FPS Player. When the scene starts it loads
// the museum and the street, finds the player, and sets up each part of the visit:
//   Tour            the works on the walls and the story stands, the start card, the tasks board
//   Street          the canal clean-up and the replay of the storm (Stop Hazards Early)
//   GoBagRoom       the go bag game (Get Ready)
//   DrillController the earthquake drill, which comes after the go bag, and the walk to the evacuation center
// Scenario says what GAMA (or, without GAMA, Tanghalan.Core) is doing: the storm, the shaking, the neighbors.
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace Tanghalan.Vr
{
    public sealed class TanghalanApp : MonoBehaviour
    {
        public static TanghalanApp Instance { get; private set; }

        public World World { get; private set; }
        public Rig Rig { get; private set; }
        public Bayani Bayani { get; private set; }
        public Hud Hud { get; private set; }
        public Visit Visit { get; private set; }
        public Scenario Scenario { get; private set; }
        public Tour Tour { get; private set; }
        public Street Street { get; private set; }
        public GoBagRoom GoBag { get; private set; }
        public DrillController Drill { get; private set; }
        public bool Started { get; private set; }
        public Lang Lang { get { return Visit.Lang; } }

        string roomId;
        bool placedBySpawn;

        void Awake() { Instance = this; }

        IEnumerator Start()
        {
            Visit = new Visit();
            SetLighting();
            var root = new GameObject("TANGHALAN world").transform;
            World = new World(root);
            World.LoadPlace("museum");
            World.LoadPlace("street");
            World.CollectPoints();
            yield return null;

            Rig = gameObject.AddComponent<Rig>();
            Rig.Init();
            if (Rig.Origin == null) yield break;
            Sfx.Init(Rig.Head.transform);
            var pointer = gameObject.AddComponent<Pointer>();
            pointer.Rig = Rig;
            SetUpTeleporting();
            Scenario = new Scenario();
            Scenario.DrillCalled += () => { if (Drill != null) Drill.CalledByGama(); };
            Scenario.ClassCount += (ready, players) => { if (Drill != null) Drill.ClassCount(ready, players); };

            Bayani = new GameObject("Bayani").AddComponent<Bayani>();
            Bayani.Init(World, Rig, World.Point("bayani_lobby"));
            Hud = new Hud(World, Rig);
            Tour = new Tour(this);
            Street = new Street(this);
            GoBag = new GoBagRoom(this);
            Drill = new DrillController(this);

            // the visitor starts in the lobby, facing the museum (GAMA moves them there too when it connects)
            Rig.Place(World.Point("spawn"), World.Facing("spawn"));
            placedBySpawn = true;
            Tour.ShowStart();
        }

        /// <summary>Soft, even light from the sky and the floor, as the web app's hemisphere light, and a sun with no shadows.</summary>
        static void SetLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(1f, 0.98f, 0.94f) * 0.95f;
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.82f, 0.75f) * 0.9f;
            RenderSettings.ambientGroundColor = new Color(0.6f, 0.54f, 0.45f) * 0.9f;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                l.shadows = LightShadows.None;
                l.intensity = 0.45f;
                l.transform.rotation = Quaternion.Euler(55, -30, 0);
            }
        }

        /// <summary>Every floor of the museum and the street becomes a place to teleport to, like the template's Teleport Area.</summary>
        void SetUpTeleporting()
        {
            var template = FindFirstObjectByType<TeleportationArea>();
            UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask layers;
            if (template != null) layers = template.interactionLayers;
            else layers = UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Teleport");
            // the template's own ground stands for GAMA's world, two metres down: TANGHALAN has its own floors
            if (template != null) template.gameObject.SetActive(false);
            foreach (var floor in World.Floors)
            {
                var area = floor.gameObject.AddComponent<TeleportationArea>();
                area.interactionLayers = layers;
                area.colliders.Clear();
                area.colliders.Add(floor);
                area.enabled = false; area.enabled = true; // take the collider into account
            }
        }

        public void BeginVisit()
        {
            Started = true;
            Scenario.Report("visit", Lang == Lang.Fil ? "fil" : "en");
            TanghalanSimulationManager.Ask("tg_hello", "lang", Lang == Lang.Fil ? "fil" : "en");
            Bayani.Say(Strings.Get("room.lobby", Lang));
        }

        public void SetLang(Lang l)
        {
            Visit.Lang = l;
            Tour.Refresh(); Street.Refresh(); GoBag.Refresh(); Drill.Refresh();
        }

        void Update()
        {
            if (Scenario == null) return;
            Scenario.Tick(Time.deltaTime);
            if (!Started) return;
            // the room the visitor is in: Bayani says a word about it, once per entry
            var feet = Rig.Feet;
            double wx = WebSpace.WebX(feet), wz = WebSpace.WebZ(feet);
            var room = MuseumData.RoomAt(wx, wz);
            string id = room != null ? room.Id : wx < -13.1 ? "kalye" : null;
            if (id != null && id != roomId)
            {
                roomId = id;
                if (!Drill.Busy)
                {
                    if (id == "kalye") Street.OnArrive();
                    else Bayani.Say(Strings.Get("room." + id, Lang));
                }
                if (id != "kalye") Street.OnLeave();
            }
            Tour.Tick(); Street.Tick(); GoBag.Tick(); Drill.Tick(); Hud.Tick();
        }

        public bool OnStreet { get { return roomId == "kalye"; } }
        public string RoomId { get { return roomId; } }

        void OnDestroy() { if (Scenario != null) Scenario.Dispose(); }
    }

    /// <summary>A small card that floats in front of the visitor with what to do now (the drill's Duck, Cover, Hold).</summary>
    public sealed class Hud
    {
        readonly Panel panel;
        readonly TextMeshPro text;
        readonly Rig rig;
        float until;

        public Hud(World world, Rig rig)
        {
            this.rig = rig;
            panel = new Panel(world, null, "HUD", 0.9f, 0.26f, new Color(0.09f, 0.19f, 0.25f));
            text = panel.Text("", 0, 0, 0.84f, 0.22f, 0.4f, Palette.Gold, TextAlignmentOptions.Center, true);
            panel.Visible = false;
        }

        /// <summary>Shows a line; seconds 0 keeps it until Hide.</summary>
        public void Show(string line, float seconds = 0)
        {
            if (string.IsNullOrEmpty(line)) { Hide(); return; }
            text.text = line;
            if (!panel.Visible) { panel.Visible = true; Follow(true); }
            until = seconds > 0 ? Time.time + seconds : float.MaxValue;
        }

        public void Hide() { panel.Visible = false; }

        public void Tick()
        {
            if (!panel.Visible) return;
            if (Time.time > until) { Hide(); return; }
            Follow(false);
        }

        // it drifts after the head, so it stays in view without being stuck to it
        void Follow(bool snap)
        {
            var head = rig.Head.transform;
            var want = head.position + rig.Forward * 1.2f + Vector3.down * 0.32f;
            var t = panel.Root.transform;
            t.position = snap ? want : Vector3.Lerp(t.position, want, Time.deltaTime * 2.5f);
            var d = t.position - head.position; d.y = 0;
            if (d.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(d.normalized);
        }
    }
}
