// The other people in TANGHALAN VR, on screen: the class visiting the museum with their teacher
// (Tanghalan.MuseumVisitors) and the neighbors on the street (Tanghalan.StreetLife). The rules in
// Tanghalan.Core say where each person is and what they are doing; this shows them with Figures, moves them
// smoothly, and puts the ball in the children's hands and the broom in Aling Rosa's.
using System.Collections.Generic;
using Tanghalan;
using UnityEngine;

namespace Tanghalan.Vr
{
    public sealed class Crowds
    {
        public readonly MuseumVisitors Visitors;
        public readonly StreetLife Street;

        readonly TanghalanApp app;
        readonly List<Figure> visitorFigures = new List<Figure>();
        readonly List<Figure> streetFigures = new List<Figure>();
        readonly Transform ball, broom;

        public Crowds(TanghalanApp app)
        {
            this.app = app;
            var world = app.World;
            var root = new GameObject("People").transform;
            Visitors = new MuseumVisitors(Rng.Next);
            Street = new StreetLife(app.Scenario.Crowd, Rng.Next);
            foreach (var p in Visitors.People) visitorFigures.Add(Make(world, p, root));
            foreach (var p in Street.People) streetFigures.Add(Make(world, p, root));
            var b = world.Spawn("prop_ball", root); ball = b != null ? b.transform : null;
            var w = world.Spawn("prop_broom", root); broom = w != null ? w.transform : null;
        }

        static Figure Make(World world, Person p, Transform root)
        {
            var f = Figure.Make(world, p.Model, root);
            if (f != null) f.Snap(WebSpace.ToUnity(p.Position), WebSpace.Heading(p.Heading));
            return f;
        }

        /// <summary>How many of them are at the evacuation center, and how many there are.</summary>
        public int ArrivedCount { get { return Visitors.ArrivedCount + Street.ArrivedCount; } }
        public int Count { get { return Visitors.People.Count + Street.People.Count; } }

        public void Tick()
        {
            float dt = Time.deltaTime;
            var feet = app.Rig.Feet;
            var player = new P3(WebSpace.WebX(feet), 0, WebSpace.WebZ(feet));
            var phase = app.Visit.Drill.Phase;
            double shaking = app.Scenario.Intensity;
            Visitors.Update(dt, player, app.Tour.Reading, phase, shaking);
            Street.Update(dt, player, app.Scenario.StormRunning, phase, shaking);
            Show(Visitors.People, visitorFigures, dt);
            Show(Street.People, streetFigures, dt);
            ShowBall();
            ShowBroom();
        }

        static void Show(List<Person> people, List<Figure> figures, float dt)
        {
            for (int k = 0; k < people.Count && k < figures.Count; k++)
            {
                var f = figures[k];
                if (f == null) continue;
                var p = people[k];
                if (f.gameObject.activeSelf != p.Visible) f.gameObject.SetActive(p.Visible);
                if (!p.Visible) continue;
                var t = f.transform;
                t.position = WebSpace.ToUnity(p.Position);
                t.rotation = Quaternion.Slerp(t.rotation, WebSpace.Heading(p.Heading), 1 - Mathf.Exp(-dt * 8));
                f.Pose = p.Pose;
            }
        }

        void ShowBall()
        {
            if (ball == null) return;
            bool on = Street.BallOut;
            if (ball.gameObject.activeSelf != on) ball.gameObject.SetActive(on);
            // the model stands on its lowest point; the rules give the ball's middle
            ball.position = WebSpace.ToUnity(Street.Ball) + Vector3.down * 0.09f;
        }

        // the walis tingting: its handle in Aling Rosa's hands, its ribs on the ground in front of her
        void ShowBroom()
        {
            if (broom == null || streetFigures.Count == 0 || streetFigures[0] == null) return;
            var f = streetFigures[0];
            bool on = f.gameObject.activeSelf && Street.People[0].Broom;
            if (broom.gameObject.activeSelf != on) broom.gameObject.SetActive(on);
            if (!on) return;
            var hands = (f.HandR.position + f.HandL.position) / 2;
            var t = f.transform;
            float ground = t.position.y;
            var foot = t.position + t.forward * 0.55f + t.right * 0.1f;
            // lying down or ducking: the broom lies on the ground beside her
            if (Street.People[0].Pose == Pose.Duck) { hands = t.position + t.right * 0.45f + t.forward * 0.6f + Vector3.up * 0.04f; foot = t.position + t.right * 0.45f - t.forward * 0.4f; }
            foot.y = ground;
            var up = hands - foot;
            if (up.sqrMagnitude < 1e-4f) return;
            broom.position = foot;
            broom.rotation = Quaternion.FromToRotation(Vector3.up, up.normalized);
            // the broom is 1.05 m long; stretch it a little, or not, to reach her hands
            broom.localScale = new Vector3(1, Mathf.Clamp(up.magnitude / 0.95f, 0.8f, 1.15f), 1);
        }
    }
}
