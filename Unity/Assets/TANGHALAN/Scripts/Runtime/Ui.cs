// Panels in the world: a card with text, pictures and buttons, facing the visitor. Everything the visitor can
// press (buttons, works, tables, trash) carries a Pointable, which the Pointer finds with a ray from the
// controller (or the mouse) and presses with the trigger.
using System;
using TMPro;
using UnityEngine;

namespace Tanghalan.Vr
{
    public sealed class Pointable : MonoBehaviour
    {
        public Action OnPress;
        public Func<bool> CanPress;
        /// <summary>How far away it can be pressed from, in metres.</summary>
        public float Reach = 12f;
        public Renderer Highlight;
        public Color Normal = Color.white, Hover = new Color(1f, 0.85f, 0.4f);
        bool hovered;

        public bool Active { get { return isActiveAndEnabled && (CanPress == null || CanPress()); } }

        public void SetHover(bool on)
        {
            if (hovered == on) return;
            hovered = on;
            if (Highlight != null) Ui.SetColor(Highlight, on ? Hover : Normal);
        }

        public void Press() { if (Active && OnPress != null) OnPress(); }
    }

    public static class Palette
    {
        public static readonly Color Navy = Hex(0x16303f), Paper = Hex(0xfffaf0), Cream = Hex(0xfff5df), Gold = Hex(0xffc53d),
            Orange = Hex(0xd9650a), Green = Hex(0x128253), Teal = Hex(0x2a6b8f), Red = Hex(0xc62f22), Grey = Hex(0x4f6572), White = Color.white,
            Yellow = Hex(0xfff3a0), Amber = Hex(0xf2a33a);

        public static Color Hex(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f); }
    }

    public sealed class Panel
    {
        public readonly GameObject Root;
        public readonly float Width, Height;
        readonly World world;
        readonly Renderer back;

        public Panel(World world, Transform parent, string name, float width, float height, Color background)
        {
            this.world = world;
            Width = width; Height = height;
            Root = new GameObject("Panel " + name);
            Root.transform.SetParent(parent, false);
            back = Ui.Quad(Root.transform, world.FlatMaterial(background), 0, 0, width, height, 0, false);
        }

        public void SetBackground(Color c) { Ui.SetColor(back, c); }

        /// <summary>Some text in a box: x and y are the box's center from the panel's center, in metres.</summary>
        public TextMeshPro Text(string text, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft, bool bold = false)
        {
            return Ui.Text(Root.transform, text, x, y, w, h, size, color, align, bold);
        }

        public Renderer Picture(string texture, float x, float y, float maxW, float maxH)
        {
            var mat = world.PictureMaterial(texture);
            var tex = mat.mainTexture;
            float w = maxW, h = maxH;
            if (tex != null)
            {
                float aspect = (float)tex.width / tex.height;
                if (maxW / maxH > aspect) w = maxH * aspect; else h = maxW / aspect;
            }
            return Ui.Quad(Root.transform, mat, x, y, w, h, -0.003f, false);
        }

        public Renderer Box(float x, float y, float w, float h, Color c)
        {
            return Ui.Quad(Root.transform, world.FlatMaterial(c), x, y, w, h, -0.002f, false);
        }

        public Button Button(string label, float x, float y, float w, float h, Color color, Action onPress)
        {
            var bg = Ui.Quad(Root.transform, world.FlatMaterial(color), x, y, w, h, -0.004f, true);
            var text = Ui.Text(bg.transform.parent, label, x, y, w - 0.03f, h - 0.02f, 0.42f, Palette.White, TextAlignmentOptions.Center, true);
            text.transform.localPosition += new Vector3(0, 0, -0.003f);
            var p = bg.gameObject.AddComponent<Pointable>();
            p.Highlight = bg; p.Normal = color; p.Hover = Color.Lerp(color, Color.white, 0.35f); p.OnPress = onPress;
            return new Button { Pointable = p, Label = text, Background = bg };
        }

        /// <summary>Puts the panel at a point, its face turned towards a viewer standing at another point.</summary>
        public void Place(Vector3 at, Vector3 viewer)
        {
            Root.transform.position = at;
            var d = at - viewer; d.y = 0;
            Root.transform.rotation = d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d.normalized, Vector3.up) : Quaternion.identity;
        }

        /// <summary>Puts the panel flat against a wall, its face out along the wall's facing.</summary>
        public void PlaceOnWall(Vector3 at, Vector3 facing)
        {
            Root.transform.position = at;
            facing.y = 0;
            Root.transform.rotation = Quaternion.LookRotation(-facing.normalized, Vector3.up);
        }

        public bool Visible { get { return Root.activeSelf; } set { Root.SetActive(value); } }

        public void Clear()
        {
            for (int i = Root.transform.childCount - 1; i >= 0; i--)
            {
                var c = Root.transform.GetChild(i);
                if (c.GetComponent<Renderer>() != back) UnityEngine.Object.Destroy(c.gameObject);
            }
        }

        public void Destroy() { UnityEngine.Object.Destroy(Root); }
    }

    public sealed class Button
    {
        public Pointable Pointable;
        public TextMeshPro Label;
        public Renderer Background;
        public bool Visible { set { Background.gameObject.SetActive(value); Label.gameObject.SetActive(value); } }
        public string Text { set { Label.text = value; } }
    }

    public static class Ui
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public static void SetColor(Renderer r, Color c)
        {
            var m = r.material;
            m.SetColor(BaseColor, c); m.color = c;
        }

        /// <summary>A flat rectangle facing the panel's front. Its collider is kept only when it can be pressed.</summary>
        public static Renderer Quad(Transform parent, Material mat, float x, float y, float w, float h, float z, bool pressable)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var col = q.GetComponent<Collider>();
            UnityEngine.Object.Destroy(col);
            if (pressable) { var b = q.AddComponent<BoxCollider>(); b.size = new Vector3(1, 1, 0.05f); }
            q.transform.SetParent(parent, false);
            q.transform.localPosition = new Vector3(x, y, z);
            q.transform.localScale = new Vector3(w, h, 1);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        public static TextMeshPro Text(Transform parent, string text, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions align, bool bold)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, -0.006f);
            var t = go.AddComponent<TextMeshPro>();
            t.rectTransform.sizeDelta = new Vector2(w, h);
            t.text = text;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.enableAutoSizing = true;
            t.fontSizeMax = size; t.fontSizeMin = size * 0.4f;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }
    }

    /// <summary>Finds what the visitor points at, lights it up, and presses it when they pull the trigger.</summary>
    public sealed class Pointer : MonoBehaviour
    {
        public Rig Rig;
        Pointable hovered;
        Transform dot;
        int lastHand = 1;

        void Start()
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(d.GetComponent<Collider>());
            d.name = "Pointer dot";
            d.transform.localScale = Vector3.one * 0.025f;
            var r = d.GetComponent<Renderer>();
            r.sharedMaterial = new Material(Resources.Load<Material>(World.MaterialPath + "TanghalanFlat"));
            r.sharedMaterial.SetColor("_BaseColor", Palette.Gold);
            dot = d.transform;
        }

        void Update()
        {
            if (Rig == null || Rig.Head == null) return;
            int hand;
            bool pressed = Rig.Pressed(out hand);
            if (pressed && hand != 0) lastHand = hand;
            var ray = Rig.PointingRay(Rig.Desktop ? 0 : lastHand);
            Pointable found = null;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 15f, ~0, QueryTriggerInteraction.Collide))
            {
                var p = hit.collider.GetComponentInParent<Pointable>();
                if (p != null && p.Active && hit.distance <= p.Reach) found = p;
                dot.gameObject.SetActive(true);
                dot.position = hit.point;
                dot.localScale = Vector3.one * (found != null ? 0.04f : 0.02f);
            }
            else dot.gameObject.SetActive(false);
            if (found != hovered)
            {
                if (hovered != null) hovered.SetHover(false);
                hovered = found;
                if (hovered != null) { hovered.SetHover(true); Rig.Haptic(0.15f, 0.03f); }
            }
            if (pressed && hovered != null) { Sfx.Play("click"); hovered.Press(); }
        }
    }
}

namespace Tanghalan.Vr
{
    /// <summary>Something flying from one place to another in a short arc: a piece of trash, a go bag item.</summary>
    public sealed class Flights : MonoBehaviour
    {
        sealed class Flight
        {
            public Transform T; public Vector3 From; public System.Func<Vector3> To;
            public float K, Duration, S0, S1; public System.Action Done;
        }

        static Flights instance;
        readonly System.Collections.Generic.List<Flight> list = new System.Collections.Generic.List<Flight>();

        public static void Fly(Transform t, System.Func<Vector3> to, float scaleFrom, float scaleTo, System.Action done = null, float seconds = 0.4f)
        {
            if (instance == null) instance = new GameObject("Flights").AddComponent<Flights>();
            instance.list.RemoveAll(f => f.T == t);
            instance.list.Add(new Flight { T = t, From = t.position, To = to, Duration = seconds, S0 = scaleFrom, S1 = scaleTo, Done = done });
        }

        public static bool IsFlying(Transform t) { return instance != null && instance.list.Exists(f => f.T == t); }

        void Update()
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var f = list[i];
                if (f.T == null) { list.RemoveAt(i); continue; }
                f.K = Mathf.Min(1, f.K + Time.deltaTime / f.Duration);
                float e = f.K * f.K * (3 - 2 * f.K);
                f.T.position = Vector3.Lerp(f.From, f.To(), e) + Vector3.up * Mathf.Sin(f.K * Mathf.PI) * 0.25f;
                f.T.localScale = Vector3.one * Mathf.Lerp(f.S0, f.S1, e);
                if (f.K >= 1) { list.RemoveAt(i); if (f.Done != null) f.Done(); }
            }
        }
    }
}
