// Bayani, the museum's guide (from the web app): he walks beside the visitor, on their right so he never
// blocks the view, and says things in a speech bubble over his head. In the drill he crouches, and on the
// way out he walks ahead along the route to the evacuation center.
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Tanghalan.Vr
{
    public sealed class Bayani : MonoBehaviour
    {
        public Rig Rig;
        public World World;
        Transform model;
        Panel bubble;
        TextMeshPro words;
        float sayUntil;
        List<Vector3> guide;
        int guideAt;
        Vector3 velocity;
        bool crouch;

        public void Init(World world, Rig rig, Vector3 start)
        {
            World = world; Rig = rig;
            var go = world.Spawn("bayani", transform);
            model = go != null ? go.transform : transform;
            transform.position = start;
            bubble = new Panel(world, transform, "Bayani says", 1.1f, 0.42f, Palette.Cream);
            bubble.Root.transform.localPosition = new Vector3(0, 1.95f, 0);
            words = bubble.Text("", 0, 0, 1.02f, 0.36f, 0.34f, Palette.Navy, TextAlignmentOptions.Center, true);
            bubble.Visible = false;
        }

        /// <summary>Says something for a few seconds (longer for longer sentences).</summary>
        public void Say(string text, float seconds = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            words.text = text;
            bubble.Visible = true;
            sayUntil = Time.time + (seconds > 0 ? seconds : Mathf.Clamp(text.Length / 14f, 4, 12));
        }

        /// <summary>Walks ahead along a route (the way to the evacuation center); null goes back to following.</summary>
        public void Guide(List<Vector3> route) { guide = route; guideAt = 0; }

        public void Crouch(bool on) { crouch = on; }

        public void Place(Vector3 at) { transform.position = at; }

        void Update()
        {
            if (Rig == null || Rig.Head == null) return;
            var me = Rig.Feet;
            Vector3 target;
            float speed = 2.2f;
            if (guide != null && guideAt < guide.Count)
            {
                // a guide: walk the route, but wait when the visitor is more than 4 m behind
                target = guide[guideAt];
                if (Flat(transform.position, target) < 0.3f) guideAt++;
                speed = Flat(transform.position, me) > 4f ? 0f : 1.1f;
            }
            else
            {
                var right = Vector3.Cross(Vector3.up, Rig.Forward);
                target = me + right * 1.05f - Rig.Forward * 0.3f;
                target.y = me.y;
                if (Flat(transform.position, me) > 9f) transform.position = target; // left far behind: catch up
            }
            var d = target - transform.position; d.y = 0;
            float dist = d.magnitude;
            if (dist > 0.4f && speed > 0)
            {
                var step = d.normalized * Mathf.Min(dist, speed * Time.deltaTime);
                transform.position += step;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized), Time.deltaTime * 6);
            }
            else
            {
                var look = me - transform.position; look.y = 0;
                if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look.normalized), Time.deltaTime * 3);
            }
            // stand on whatever floor is under him
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 1.2f, Vector3.down, out hit, 3f, ~0, QueryTriggerInteraction.Ignore))
                transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, hit.point.y, Time.deltaTime * 10), transform.position.z);
            float bob = dist > 0.4f ? Mathf.Abs(Mathf.Sin(Time.time * 8)) * 0.03f : 0;
            model.localPosition = new Vector3(0, bob, 0);
            model.localScale = new Vector3(1, crouch ? 0.62f : 1, 1);
            if (bubble.Visible)
            {
                if (Time.time > sayUntil) bubble.Visible = false;
                bubble.Root.transform.localPosition = new Vector3(0, crouch ? 1.35f : 1.95f, 0);
                var to = bubble.Root.transform.position - Rig.Head.transform.position; to.y = 0;
                if (to.sqrMagnitude > 1e-4f) bubble.Root.transform.rotation = Quaternion.LookRotation(to.normalized);
            }
        }

        static float Flat(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }
    }
}
