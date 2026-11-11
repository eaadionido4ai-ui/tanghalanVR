// The museum itself: the start card in the lobby, the tasks board, the GAMA board, and the works. Pointing at
// a drawing on a wall, or at a story stand, opens a card in front of it with the work, its text in Filipino
// or English, and Bayani's tip, as the web app's Look and Read sheets do.
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Tanghalan.Vr
{
    public sealed class Tour
    {
        readonly TanghalanApp app;
        readonly World world;
        Panel start, tasks, link, reader;
        TextMeshPro linkText;
        Button drillButton;
        Work open;
        int page;
        float nextRefresh;
        string lastLink;

        Lang L { get { return app.Lang; } }

        public Tour(TanghalanApp app)
        {
            this.app = app; world = app.World;
            foreach (var w in MuseumData.Works)
            {
                Collider c;
                if (!world.Hot.TryGetValue(w.Hot, out c)) { Debug.LogWarning("TANGHALAN: no box for work " + w.Id); continue; }
                var work = w;
                var p = c.gameObject.AddComponent<Pointable>();
                p.Reach = 8f;
                p.OnPress = () => Open(work);
                p.CanPress = () => app.Started && !app.Drill.Busy;
            }
            BuildTasks();
            BuildLink();
        }

        // ---------- the start card ----------

        public void ShowStart()
        {
            if (start != null) start.Destroy();
            start = new Panel(world, null, "Start", 1.3f, 0.95f, Palette.Navy);
            start.Text(Strings.Get("start.title", L), 0, 0.36f, 1.2f, 0.16f, 0.62f, Palette.Gold, TextAlignmentOptions.Center, true);
            start.Text(MuseumData.FullName, 0, 0.24f, 1.2f, 0.08f, 0.26f, Palette.Cream, TextAlignmentOptions.Center);
            start.Text(Strings.Get("start.body", L), 0, 0.08f, 1.2f, 0.2f, 0.36f, Palette.White, TextAlignmentOptions.Center);
            start.Text(Strings.Get(app.Rig.Desktop ? "start.howto.desktop" : "start.howto", L), 0, -0.1f, 1.2f, 0.14f, 0.3f, Palette.Cream, TextAlignmentOptions.Center);
            start.Button(Strings.Get("start.lang", L), -0.32f, -0.33f, 0.56f, 0.15f, Palette.Teal, () => { app.SetLang(L == Lang.Fil ? Lang.En : Lang.Fil); ShowStart(); });
            start.Button(Strings.Get("start.go", L), 0.32f, -0.33f, 0.56f, 0.15f, Palette.Orange, () => { start.Visible = false; app.BeginVisit(); });
            start.Text(MuseumData.SampleNote, 0, -0.44f, 1.2f, 0.06f, 0.2f, Palette.Gold, TextAlignmentOptions.Center);
            start.Place(world.Point("lobby_start"), world.Point("spawn") + Vector3.up * 1.3f);
        }

        // ---------- the tasks board and the GAMA board ----------

        void BuildTasks()
        {
            tasks = new Panel(world, null, "Tasks", 1.5f, 1.1f, Palette.Paper);
            tasks.PlaceOnWall(world.Point("lobby_tasks"), world.Facing("lobby_tasks"));
            tasks.Box(0, 0.47f, 1.5f, 0.16f, Palette.Green);
            Refresh();
        }

        void BuildLink()
        {
            link = new Panel(world, null, "GAMA", 1.3f, 0.6f, Palette.Navy);
            link.PlaceOnWall(world.Point("lobby_link"), world.Facing("lobby_link"));
            link.Text("SIMPLE · GAMA", 0, 0.2f, 1.2f, 0.12f, 0.5f, Palette.Gold, TextAlignmentOptions.Center, true);
            linkText = link.Text("", 0, -0.06f, 1.2f, 0.36f, 0.34f, Palette.Cream, TextAlignmentOptions.Center);
        }

        public void Refresh()
        {
            tasks.Clear();
            tasks.Box(0, 0.47f, 1.5f, 0.16f, Palette.Green);
            tasks.Text(Strings.Get("tasks.title", L), 0, 0.47f, 1.4f, 0.14f, 0.55f, Palette.White, TextAlignmentOptions.Center, true);
            var v = app.Visit;
            // one line for each task, with a tick when it is done (the font has no tick of its own)
            var rows = new[]
            {
                new KeyValuePair<bool, string>(v.WorksDone, Strings.Format("tasks.works", L, Visit.WorksToSee, Mathf.Min(v.Seen.Count, Visit.WorksToSee))),
                new KeyValuePair<bool, string>(v.CanalDone, Strings.Get("tasks.canal", L)),
                new KeyValuePair<bool, string>(v.GoBagDone, Strings.Get("tasks.gobag", L)),
                new KeyValuePair<bool, string>(v.Over, Strings.Get("tasks.drill", L)),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                float y = 0.3f - i * 0.15f;
                tasks.Picture(rows[i].Key ? "tick" : "star_off", -0.64f, y, 0.1f, 0.1f);
                tasks.Text(rows[i].Value, 0.06f, y, 1.26f, 0.13f, 0.34f, rows[i].Key ? Palette.Green : Palette.Navy, TextAlignmentOptions.Left);
            }
            drillButton = tasks.Button(Strings.Get("tasks.startdrill", L), 0, -0.4f, 1.0f, 0.16f, Palette.Red, () => app.Drill.Announce());
            drillButton.Pointable.CanPress = () => app.Visit.ReadyForDrill && !app.Drill.Busy && !app.Visit.Over;
            if (!v.ReadyForDrill) tasks.Text(Strings.Get("tasks.notyet", L), 0, -0.51f, 1.4f, 0.06f, 0.22f, Palette.Grey, TextAlignmentOptions.Center);
            if (open != null) ShowPage();
        }

        public void Tick()
        {
            if (Time.time < nextRefresh) return;
            nextRefresh = Time.time + 1f;
            string s = Scenario.Gama ? Strings.Get("start.gama.on", L)
                : Time.timeSinceLevelLoad < 8 ? Strings.Get("start.gama.wait", L)
                : Strings.Get("start.gama.off", L);
            if (s != lastLink) { lastLink = s; linkText.text = s; }
            Ui.SetColor(drillButton.Background, drillButton.Pointable.Active ? Palette.Red : new Color(0.75f, 0.7f, 0.65f));
        }

        // ---------- a work ----------

        void Open(Work w)
        {
            open = w; page = 0;
            if (app.Visit.Seen.Add(w.Id)) { app.Scenario.Report("work_seen", w.Id); Refresh(); }
            if (reader == null) reader = new Panel(world, null, "Reader", 1.5f, 1.0f, Palette.Paper);
            reader.Visible = true;
            // the card stands between the visitor and the work, a little to the side, so both can be seen
            Collider c = world.Hot[w.Hot];
            var work = c.bounds.center;
            var head = app.Rig.Head.transform.position;
            var toWork = work - head; toWork.y = 0;
            var side = Vector3.Cross(Vector3.up, toWork.normalized);
            var at = head + toWork.normalized * Mathf.Min(1.3f, toWork.magnitude * 0.6f) + side * 0.35f;
            at.y = head.y - 0.1f;
            reader.Place(at, head);
            ShowPage();
            app.Bayani.Say(w.Note(L));
        }

        void ShowPage()
        {
            var w = open;
            reader.Clear();
            reader.Box(0, 0.44f, 1.5f, 0.12f, w.Kind == WorkKind.Art ? Palette.Teal : Palette.Amber);
            reader.Text(MuseumData.KindLabel(w.Kind, L).ToUpper() + " · " + Strings.Get("work.sample", L), 0, 0.44f, 1.4f, 0.1f, 0.3f, Palette.White, TextAlignmentOptions.Center, true);
            reader.Text(w.Title, -0.36f, 0.31f, 0.72f, 0.12f, 0.55f, Palette.Navy, TextAlignmentOptions.Left, true);
            reader.Text(Strings.Format("work.by", L, w.By, w.Age, w.Grade, w.Place), -0.36f, 0.22f, 0.72f, 0.07f, 0.26f, Palette.Grey, TextAlignmentOptions.Left);
            reader.Picture(w.Picture, 0.38f, 0.08f, 0.66f, 0.55f);
            reader.Text(w.PageText(page, L), -0.36f, -0.02f, 0.72f, 0.4f, 0.36f, Palette.Navy, TextAlignmentOptions.TopLeft);
            reader.Box(-0.36f, -0.3f, 0.72f, 0.12f, Palette.Cream);
            reader.Text("<b>" + Strings.Get("work.tip", L) + ":</b> " + w.Note(L), -0.36f, -0.3f, 0.7f, 0.11f, 0.24f, Palette.Navy, TextAlignmentOptions.Left);
            if (w.PageCount > 1)
            {
                reader.Text(Strings.Format("work.page", L, page + 1, w.PageCount), 0.38f, -0.3f, 0.6f, 0.06f, 0.24f, Palette.Grey, TextAlignmentOptions.Center);
                var back = reader.Button(Strings.Get("btn.prev", L), 0.2f, -0.43f, 0.3f, 0.1f, Palette.Teal, () => { page = Mathf.Max(0, page - 1); ShowPage(); });
                var next = reader.Button(Strings.Get("btn.next", L), 0.54f, -0.43f, 0.3f, 0.1f, Palette.Teal, () => { page = Mathf.Min(w.PageCount - 1, page + 1); ShowPage(); });
                back.Pointable.CanPress = () => page > 0;
                next.Pointable.CanPress = () => page < w.PageCount - 1;
            }
            reader.Button(Strings.Get("btn.lang", L), -0.52f, -0.43f, 0.36f, 0.1f, Palette.Green, () => app.SetLang(L == Lang.Fil ? Lang.En : Lang.Fil));
            reader.Button(Strings.Get("btn.close", L), -0.14f, -0.43f, 0.32f, 0.1f, Palette.Orange, Close);
        }

        public void Close()
        {
            open = null;
            if (reader != null) reader.Visible = false;
        }
    }
}
