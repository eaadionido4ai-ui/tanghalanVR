// The Go Bag Room, as in the web app (src/rooms/gobag.js) with its rules (Tanghalan.GoBagRules): take an
// empty go bag at the packing table, look at the things on the tables (Easy) or in the cabinet, drawers,
// crate and cooler (Difficult), pack them or leave them, then zip the bag up and see the stars. In VR the
// bag hangs from the left hand (with no headset, it sits at the bottom left of the view); point at it to see
// what is inside and take things out.
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Tanghalan.Vr
{
    public sealed class GoBagRoom
    {
        enum Phase { Idle, Packing, Done }

        sealed class Piece
        {
            public GoBagItem Def; public GameObject Go; public Vector3 Home; public float Scale;
            public string Station; public bool Packed;
        }

        static readonly string[] Containers = { "cabinet", "drawers", "crate", "cooler" };
        const float ScaleEasy = 1.7f, ScaleHard = 1.45f;

        readonly TanghalanApp app;
        readonly World world;
        readonly Transform root;
        readonly List<Piece> pieces = new List<Piece>();
        readonly HashSet<string> opened = new HashSet<string>();
        Phase phase = Phase.Idle;
        bool hard, heavySaid, leftSaid, wasIn;
        List<GoBagItem> tray;
        Panel menu, card, bagPanel, best;
        GameObject bag;
        Transform bagModel;
        int bestStars;
        float startedAt;

        Lang L { get { return app.Lang; } }
        public bool Packing { get { return phase == Phase.Packing; } }

        public GoBagRoom(TanghalanApp app)
        {
            this.app = app; world = app.World;
            root = new GameObject("Go Bag Room items").transform;
            Collider counter;
            if (world.Hot.TryGetValue("gb_counter", out counter))
            {
                var p = counter.gameObject.AddComponent<Pointable>();
                p.Reach = 6f; p.OnPress = ShowMenu; p.CanPress = () => !app.Drill.Busy;
            }
            foreach (var kind in Containers)
            {
                Collider c;
                if (!world.Hot.TryGetValue("gb_open_" + kind, out c)) continue;
                var k = kind;
                var p = c.gameObject.AddComponent<Pointable>();
                p.Reach = 5f; p.OnPress = () => Open(k); p.CanPress = () => phase == Phase.Packing && hard && !opened.Contains(k);
            }
            best = new Panel(world, null, "Go bag best", 1.0f, 0.62f, Palette.Paper);
            best.PlaceOnWall(world.Point("gb_best"), world.Facing("gb_best"));
            ShowSets(false);
            Layout(GoBagRules.BuildRound(Rng.Next), false);
            DrawBest();
        }

        // ---------- the room's furniture ----------

        void ShowSets(bool hardSet)
        {
            world.ShowSet("gb_tables", !hardSet);
            foreach (var k in Containers)
            {
                world.ShowSet("gb_" + k + "_closed", hardSet && !opened.Contains(k));
                world.ShowSet("gb_" + k + "_open", hardSet && opened.Contains(k));
            }
        }

        /// <summary>The drill needs the tables to hide under.</summary>
        public void ShowTablesForDrill()
        {
            if (hard) { opened.Clear(); ShowSets(false); foreach (var p in pieces) if (!p.Packed) p.Go.SetActive(false); }
        }

        static string StationOf(int slot, bool hardSet)
        {
            if (!hardSet) return slot < 5 ? "t1" : slot < 10 ? "t2" : "t3";
            return slot < 4 ? "cabinet" : slot < 8 ? "drawers" : slot < 11 ? "crate" : "cooler";
        }

        void Layout(List<GoBagItem> round, bool hardSet)
        {
            foreach (var p in pieces) Object.Destroy(p.Go);
            pieces.Clear();
            for (int k = 0; k < round.Count; k++)
            {
                string anchor = (hardSet ? "gbslot_hard_" : "gbslot_easy_") + k;
                if (!world.Points.ContainsKey(anchor)) continue;
                var def = round[k];
                var go = world.Spawn("item_" + def.Id, root);
                if (go == null) continue;
                float scale = hardSet ? ScaleHard : ScaleEasy;
                go.transform.position = world.Point(anchor);
                go.transform.rotation = Quaternion.Euler(0, (k * 37 % 40) - 20, 0);
                go.transform.localScale = Vector3.one * scale;
                var box = go.AddComponent<BoxCollider>();
                box.center = new Vector3(0, 0.08f, 0); box.size = new Vector3(0.24f, 0.18f, 0.2f);
                var piece = new Piece { Def = def, Go = go, Home = go.transform.position, Scale = scale, Station = StationOf(k, hardSet) };
                var pt = go.AddComponent<Pointable>();
                pt.Reach = 5f;
                pt.OnPress = () => Look(piece);
                pt.CanPress = () => !piece.Packed && !app.Drill.Busy;
                if (hardSet) go.SetActive(false);
                pieces.Add(piece);
            }
        }

        // ---------- the round ----------

        void ShowMenu()
        {
            if (menu == null) menu = new Panel(world, null, "Go bag menu", 1.2f, 0.8f, Palette.Paper);
            menu.Clear();
            menu.Visible = true;
            menu.Place(world.Point("gb_menu"), app.Rig.Head.transform.position);
            menu.Box(0, 0.32f, 1.2f, 0.16f, Palette.Green);
            menu.Text("Go Bag Challenge", 0, 0.32f, 1.1f, 0.13f, 0.5f, Palette.White, TextAlignmentOptions.Center, true);
            if (phase == Phase.Packing)
            {
                menu.Text(Strings.Format("gobag.weight", L, Weight, GoBagRules.Capacity), 0, 0.1f, 1.1f, 0.12f, 0.4f, Palette.Navy, TextAlignmentOptions.Center, true);
                menu.Button(Strings.Get("gobag.zip", L), 0, -0.12f, 0.8f, 0.16f, Palette.Orange, Finish);
                menu.Button(Strings.Get("btn.close", L), 0, -0.3f, 0.5f, 0.11f, Palette.Grey, () => menu.Visible = false);
                return;
            }
            menu.Text(Strings.Get("gobag.tip", L), 0, 0.12f, 1.1f, 0.16f, 0.32f, Palette.Navy, TextAlignmentOptions.Center);
            menu.Button(Strings.Get("gobag.easy", L), -0.28f, -0.1f, 0.52f, 0.16f, Palette.Green, () => StartRound(false));
            menu.Button(Strings.Get("gobag.hard", L), 0.28f, -0.1f, 0.52f, 0.16f, Palette.Teal, () => StartRound(true));
            menu.Button(Strings.Get("btn.close", L), 0, -0.3f, 0.5f, 0.11f, Palette.Grey, () => menu.Visible = false);
        }

        void StartRound(bool difficult)
        {
            hard = difficult;
            opened.Clear();
            tray = GoBagRules.BuildRound(Rng.Next);
            ShowSets(hard);
            Layout(tray, hard);
            phase = Phase.Packing; heavySaid = false; leftSaid = false; startedAt = Time.time;
            HoldBag(true);
            app.Rig.SpeedFactor(1);
            menu.Visible = false;
            Sfx.Play("pop");
            app.Bayani.Say(Strings.Get(hard ? "gobag.start.hard" : "gobag.start.easy", L));
            app.Scenario.Report("gobag_start", hard ? "hard" : "easy");
        }

        void Open(string kind)
        {
            if (phase != Phase.Packing) { app.Bayani.Say(Strings.Get("gobag.first", L)); return; }
            opened.Add(kind);
            ShowSets(true);
            Sfx.Play("open");
            int n = 0;
            foreach (var p in pieces) if (p.Station == kind && !p.Packed) { p.Go.SetActive(true); n++; }
            string name = Strings.Get("gobag." + kind, L);
            app.Bayani.Say(n > 0 ? Strings.Format("gobag.found", L, n, name) : Strings.Get("gobag.empty", L));
        }

        void Look(Piece p)
        {
            if (phase != Phase.Packing) { app.Bayani.Say(Strings.Get("gobag.first", L)); return; }
            if (card == null) card = new Panel(world, null, "Item", 0.9f, 0.62f, Palette.Paper);
            card.Clear();
            card.Visible = true;
            var head = app.Rig.Head.transform.position;
            var to = p.Go.transform.position - head; to.y = 0;
            var at = head + to.normalized * Mathf.Min(0.9f, to.magnitude * 0.6f);
            at.y = head.y - 0.15f;
            card.Place(at, head);
            card.Picture("icon_item_" + p.Def.Id, -0.33f, 0.18f, 0.18f, 0.18f);
            card.Text(p.Def.NameIn(L), 0.1f, 0.2f, 0.62f, 0.12f, 0.46f, Palette.Navy, TextAlignmentOptions.Left, true);
            card.Text(p.Def.InfoIn(L), 0, 0.0f, 0.82f, 0.22f, 0.3f, Palette.Navy, TextAlignmentOptions.Center);
            card.Text(Strings.Format("gobag.itemweight", L, p.Def.Weight, Weight, GoBagRules.Capacity), 0, -0.14f, 0.8f, 0.06f, 0.24f, Palette.Grey, TextAlignmentOptions.Center);
            card.Button(Strings.Get("gobag.pack", L), -0.2f, -0.24f, 0.36f, 0.11f, Palette.Green, () => { card.Visible = false; Pack(p); });
            card.Button(Strings.Get("gobag.leave", L), 0.2f, -0.24f, 0.36f, 0.11f, Palette.Grey, () => card.Visible = false);
        }

        int Weight
        {
            get { int w = 0; foreach (var p in pieces) if (p.Packed) w += p.Def.Weight; return w; }
        }

        void Pack(Piece p)
        {
            if (p.Packed || phase != Phase.Packing) return;
            p.Packed = true;
            Sfx.Play("pop");
            var go = p.Go;
            Flights.Fly(go.transform, () => bagModel != null ? bagModel.position + Vector3.up * 0.1f : app.Rig.Head.transform.position, p.Scale, p.Scale * 0.25f, () => go.SetActive(false));
            if (Weight > GoBagRules.Capacity && !heavySaid) { heavySaid = true; app.Bayani.Say(Strings.Get("gobag.heavy", L)); }
            app.Scenario.Report("gobag_pack", p.Def.Id);
            if (bagPanel != null && bagPanel.Visible) ShowBag();
        }

        void Unpack(Piece p)
        {
            if (!p.Packed || phase != Phase.Packing) return;
            p.Packed = false;
            p.Go.SetActive(!hard || opened.Contains(p.Station));
            p.Go.transform.position = bagModel != null ? bagModel.position : p.Home;
            Flights.Fly(p.Go.transform, () => p.Home, p.Scale * 0.25f, p.Scale);
            Sfx.Play("drop");
            ShowBag();
        }

        void Finish()
        {
            if (phase != Phase.Packing) return;
            var packed = new List<string>();
            foreach (var p in pieces) if (p.Packed) packed.Add(p.Def.Id);
            var res = GoBagRules.ScoreRound(tray, packed);
            int stars = GoBagRules.RateStars(res);
            phase = Phase.Done;
            Sfx.Play("zip");
            if (stars >= 3) Sfx.Play("cheer");
            foreach (var p in pieces) if (!p.Packed) p.Go.SetActive(false);
            app.Rig.SpeedFactor(1);
            bestStars = Mathf.Max(bestStars, stars);
            app.Visit.GoBagStars = bestStars;
            app.Visit.CarryingGoBag = true;
            app.Tour.Refresh();
            DrawBest();
            app.Scenario.Report("gobag_result", stars + " stars, " + res.PackedGood + "/" + res.TotalGood + " useful, " + res.WrongPacks + " extra, " + Mathf.RoundToInt(Time.time - startedAt) + " s");
            // the result, on the menu card
            menu.Clear();
            menu.Visible = true;
            menu.Box(0, 0.32f, 1.2f, 0.16f, Palette.Green);
            menu.Text(Strings.Get(stars >= 3 ? "gobag.great" : stars == 2 ? "gobag.good" : "gobag.try", L), 0, 0.32f, 1.1f, 0.13f, 0.5f, Palette.White, TextAlignmentOptions.Center, true);
            for (int i = 0; i < 3; i++) menu.Picture(i < stars ? "star_on" : "star_off", -0.14f + i * 0.14f, 0.17f, 0.12f, 0.12f);
            string line = res.WrongPacks > 0 ? Strings.Format("gobag.result.wrong", L, res.PackedGood, res.TotalGood, res.WrongPacks) : Strings.Format("gobag.result", L, res.PackedGood, res.TotalGood);
            var missed = new List<string>(); var wrong = new List<string>();
            foreach (var t in tray) { bool pk = packed.Contains(t.Id); if (t.Good && !pk) missed.Add(t.NameIn(L)); if (!t.Good && pk) wrong.Add(t.NameIn(L)); }
            string more = (missed.Count > 0 ? Strings.Get("gobag.missed", L) + " " + string.Join(", ", missed) + "\n" : "") + (wrong.Count > 0 ? Strings.Get("gobag.wrongs", L) + " " + string.Join(", ", wrong) : "");
            menu.Text(line, 0, 0.04f, 1.1f, 0.1f, 0.32f, Palette.Navy, TextAlignmentOptions.Center, true);
            menu.Text(more, 0, -0.1f, 1.1f, 0.18f, 0.26f, Palette.Grey, TextAlignmentOptions.Center);
            menu.Text(Strings.Get("gobag.remind", L), 0, -0.24f, 1.1f, 0.1f, 0.24f, Palette.Green, TextAlignmentOptions.Center);
            menu.Button(Strings.Get("gobag.again", L), -0.25f, -0.34f, 0.44f, 0.1f, Palette.Teal, () => { phase = Phase.Idle; ShowMenu(); });
            menu.Button(Strings.Get("btn.close", L), 0.25f, -0.34f, 0.44f, 0.1f, Palette.Grey, () => menu.Visible = false);
            app.Bayani.Say(line);
        }

        void DrawBest()
        {
            best.Clear();
            best.Box(0, 0.22f, 1.0f, 0.16f, Palette.Green);
            best.Text(Strings.Get("gobag.best", L), 0, 0.22f, 0.9f, 0.13f, 0.45f, Palette.White, TextAlignmentOptions.Center, true);
            for (int i = 0; i < 3; i++) best.Picture(i < bestStars ? "star_on" : "star_off", -0.2f + i * 0.2f, 0.0f, 0.17f, 0.17f);
        }

        // ---------- the bag ----------

        void HoldBag(bool on)
        {
            if (bag == null && on)
            {
                bag = new GameObject("Go bag (held)");
                var m = world.Spawn("gobag", bag.transform);
                bagModel = m != null ? m.transform : bag.transform;
                var col = bag.AddComponent<BoxCollider>(); col.center = new Vector3(0, 0.2f, 0); col.size = new Vector3(0.36f, 0.42f, 0.24f);
                var p = bag.AddComponent<Pointable>(); p.Reach = 3f; p.OnPress = ShowBag;
            }
            if (bag == null) return;
            bag.SetActive(on);
            var rig = app.Rig;
            if (rig.Desktop || rig.LeftHand == null)
            {
                bag.transform.SetParent(rig.Head.transform, false);
                bag.transform.localPosition = new Vector3(-0.28f, -0.42f, 0.6f);
                bag.transform.localRotation = Quaternion.Euler(10, 160, 0);
                bag.transform.localScale = Vector3.one * 0.6f;
            }
            else
            {
                bag.transform.SetParent(rig.LeftHand, false);
                bag.transform.localPosition = new Vector3(0, -0.34f, 0.05f);
                bag.transform.localRotation = Quaternion.Euler(0, 180, 0);
                bag.transform.localScale = Vector3.one * 0.8f;
            }
        }

        void ShowBag()
        {
            if (bagPanel == null) bagPanel = new Panel(world, null, "Bag", 1.0f, 0.9f, Palette.Paper);
            bagPanel.Clear();
            bagPanel.Visible = true;
            var head = app.Rig.Head.transform;
            bagPanel.Place(head.position + app.Rig.Forward * 1.0f + Vector3.down * 0.1f, head.position);
            bagPanel.Text(Strings.Format("gobag.weight", L, Weight, GoBagRules.Capacity), 0, 0.38f, 0.9f, 0.1f, 0.4f, Weight > GoBagRules.Capacity ? Palette.Red : Palette.Navy, TextAlignmentOptions.Center, true);
            int row = 0;
            foreach (var p in pieces)
            {
                if (!p.Packed) continue;
                float y = 0.25f - row * 0.075f; row++;
                var piece = p;
                bagPanel.Text(p.Def.NameIn(L) + " (" + p.Def.Weight + ")", -0.15f, y, 0.6f, 0.065f, 0.26f, Palette.Navy, TextAlignmentOptions.Left);
                if (phase == Phase.Packing) bagPanel.Button(Strings.Get("gobag.unpack", L), 0.32f, y, 0.3f, 0.06f, Palette.Grey, () => Unpack(piece));
            }
            bagPanel.Button(Strings.Get("btn.close", L), 0, -0.4f, 0.4f, 0.09f, Palette.Orange, () => bagPanel.Visible = false);
        }

        // ---------- each frame ----------

        public void Tick()
        {
            if (phase == Phase.Packing)
            {
                int w = Weight;
                app.Rig.SpeedFactor(w > GoBagRules.Capacity ? 0.7f : 1f);
                bool inRoom = app.RoomId == "gobagroom";
                if (wasIn && !inRoom && !leftSaid) { leftSaid = true; app.Bayani.Say(Strings.Get("gobag.notzipped", L)); }
                wasIn = inRoom;
            }
            if (bagModel != null && bag.activeSelf)
            {
                float fill = Mathf.Min(1.4f, (float)Weight / GoBagRules.Capacity);
                float k = 1 + fill * 0.42f;
                bagModel.localScale = new Vector3(k, k, 1 + (k - 1) * 1.6f);
            }
        }

        public void Refresh()
        {
            DrawBest();
            if (menu != null && menu.Visible && phase != Phase.Done) ShowMenu();
            if (bagPanel != null && bagPanel.Visible) ShowBag();
        }
    }
}
