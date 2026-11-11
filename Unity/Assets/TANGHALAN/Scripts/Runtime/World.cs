// Loads the museum and the street (Resources/TANGHALAN/Models, made by Tools/blender from the web app) and
// gets them ready: the right materials, colliders for the invisible boxes, places to teleport to, and a list
// of the named boxes and points the rest of the code looks up. The names are explained in
// Tools/web-export/src/tag.js.
using System.Collections.Generic;
using UnityEngine;

namespace Tanghalan.Vr
{
    public sealed class World
    {
        public const string ModelPath = "TANGHALAN/Models/", TexturePath = "TANGHALAN/Textures/", MaterialPath = "TANGHALAN/Materials/";

        public readonly Transform Root;
        public Material Palette, Picture, Flat, Water;
        /// <summary>The boxes you can point at (HOT__) and drop things into (ZONE__), by id.</summary>
        public readonly Dictionary<string, Collider> Hot = new Dictionary<string, Collider>();
        public readonly Dictionary<string, Collider> Zones = new Dictionary<string, Collider>();
        /// <summary>The anchors (A__), by id: where they are, and the way they face.</summary>
        public readonly Dictionary<string, Vector3> Points = new Dictionary<string, Vector3>();
        public readonly Dictionary<string, Vector3> Facings = new Dictionary<string, Vector3>();
        /// <summary>The switchable sets (SW__), and the boxes that go on and off with each.</summary>
        readonly Dictionary<string, List<GameObject>> sets = new Dictionary<string, List<GameObject>>();
        /// <summary>The pictures and signs (T__), which rattle in an earthquake.</summary>
        public readonly List<Transform> Pictures = new List<Transform>();
        public readonly List<Collider> Floors = new List<Collider>();
        /// <summary>The boxes you cannot walk through (COL__), by id.</summary>
        public readonly Dictionary<string, Collider> Solids = new Dictionary<string, Collider>();
        readonly Dictionary<string, Material> pictureMats = new Dictionary<string, Material>();
        readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        public World(Transform root)
        {
            Root = root;
            Palette = Resources.Load<Material>(MaterialPath + "TanghalanPalette");
            Picture = Resources.Load<Material>(MaterialPath + "TanghalanPicture");
            Flat = Resources.Load<Material>(MaterialPath + "TanghalanFlat");
            Water = Resources.Load<Material>(MaterialPath + "TanghalanWater");
            if (Palette == null) Debug.LogError("TANGHALAN: the materials are missing from Resources/TANGHALAN/Materials (run Tools/make_materials.py).");
        }

        /// <summary>Loads a whole place (the museum, the street) into the world.</summary>
        public GameObject LoadPlace(string name)
        {
            var go = Spawn(name, Root);
            if (go == null) return null;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true)) Prepare(mr);
            return go;
        }

        /// <summary>
        /// Makes a copy of a model (a prop, a person) standing at the parent's origin, the right way round.
        /// The model's own calibration points (CAL__O, CAL__X, CAL__Y, CAL__Z) say how the file was turned.
        /// </summary>
        public GameObject Spawn(string name, Transform parent)
        {
            GameObject src;
            if (!prefabs.TryGetValue(name, out src))
            {
                src = Resources.Load<GameObject>(ModelPath + name);
                prefabs[name] = src;
                if (src == null) Debug.LogError("TANGHALAN: model not found: Resources/" + ModelPath + name);
            }
            if (src == null) return null;
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            var model = Object.Instantiate(src, holder.transform, false);
            model.name = name + " (model)";
            Align(model.transform);
            if (parent != Root) foreach (var mr in model.GetComponentsInChildren<MeshRenderer>(true)) PrepareProp(mr);
            return holder;
        }

        static void Align(Transform model)
        {
            Transform o = null, x = null, y = null, z = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "CAL__O") o = t; else if (t.name == "CAL__X") x = t; else if (t.name == "CAL__Y") y = t; else if (t.name == "CAL__Z") z = t;
            }
            if (o == null || x == null || y == null || z == null) { Debug.LogWarning("TANGHALAN: no calibration points in " + model.name); return; }
            model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity; model.localScale = Vector3.one;
            Vector3 po = model.InverseTransformPoint(o.position);
            Vector3 ex = model.InverseTransformPoint(x.position) - po, ey = model.InverseTransformPoint(y.position) - po, ez = model.InverseTransformPoint(z.position) - po;
            // A maps the file's axes to web axes in Unity: x -> x, y -> y, z -> -z
            var b = Matrix4x4.identity; b.SetColumn(0, ex); b.SetColumn(1, ey); b.SetColumn(2, ez);
            var d = Matrix4x4.identity; d.SetColumn(2, new Vector4(0, 0, -1, 0));
            var a = d * b.inverse;
            Vector3 c0 = a.GetColumn(0), c1 = a.GetColumn(1), c2 = a.GetColumn(2);
            float sx = c0.magnitude, sy = c1.magnitude, sz = c2.magnitude;
            Vector3 r0 = c0 / sx, r1 = c1 / sy, r2 = c2 / sz;
            if (Vector3.Dot(Vector3.Cross(r0, r1), r2) < 0) { r0 = -r0; sx = -sx; }
            model.localRotation = Quaternion.LookRotation(r2, r1);
            model.localScale = new Vector3(sx, sy, sz);
            model.localPosition = -(a.MultiplyPoint3x4(po));
            foreach (var t in new[] { o, x, y, z }) t.gameObject.SetActive(false);
        }

        void Prepare(MeshRenderer mr)
        {
            var go = mr.gameObject;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // flat, bright rooms as in the web app; cheaper on a headset
            mr.receiveShadows = false;
            string[] parts = go.name.Split(new[] { "__" }, System.StringSplitOptions.None);
            string kind = parts[0];
            switch (kind)
            {
                case "M":
                    mr.sharedMaterial = Palette; break;
                case "SW":
                    mr.sharedMaterial = Palette; AddToSet(parts[1], go); break;
                case "T":
                    mr.sharedMaterial = PictureMaterial(parts[1]);
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    Pictures.Add(go.transform); break;
                case "COL": case "FLOOR": case "HOT": case "ZONE":
                    mr.enabled = false;
                    var box = go.AddComponent<BoxCollider>();
                    string id = parts.Length > 1 ? parts[1] : go.name;
                    if (kind == "FLOOR") Floors.Add(box);
                    if (kind == "COL") Solids[id] = box;
                    if (kind == "HOT") Hot[id] = box;
                    if (kind == "ZONE") Zones[id] = box;
                    string set = parts.Length > 2 && parts[2].StartsWith("SET_") ? parts[2].Substring(4) : null;
                    if (set != null) AddToSet(set, go);
                    break;
            }
        }

        void PrepareProp(MeshRenderer mr)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            if (mr.name.StartsWith("M__") || mr.name.StartsWith("SW__")) mr.sharedMaterial = Palette;
            else if (mr.name.StartsWith("T__")) mr.sharedMaterial = PictureMaterial(mr.name.Split(new[] { "__" }, System.StringSplitOptions.None)[1]);
            else mr.enabled = false;
        }

        /// <summary>Reads the anchors (A__) of everything loaded so far.</summary>
        public void CollectPoints()
        {
            foreach (var t in Root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("A__")) continue;
                string id = t.name.Substring(3);
                if (id.EndsWith("__N")) continue;
                Points[id] = t.position;
                var n = t.parent != null ? t.parent.Find(t.name + "__N") : null;
                if (n != null) { var f = n.position - t.position; Facings[id] = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward; }
            }
        }

        public Vector3 Point(string id)
        {
            Vector3 p;
            if (Points.TryGetValue(id, out p)) return p;
            Debug.LogWarning("TANGHALAN: no point " + id);
            return Vector3.zero;
        }

        public Vector3 Facing(string id) { Vector3 f; return Facings.TryGetValue(id, out f) ? f : Vector3.forward; }

        public Material PictureMaterial(string texture)
        {
            Material m;
            if (pictureMats.TryGetValue(texture, out m)) return m;
            m = new Material(Picture) { name = "Picture " + texture };
            var tex = Resources.Load<Texture2D>(TexturePath + texture);
            if (tex == null) Debug.LogWarning("TANGHALAN: texture not found: " + texture);
            m.SetTexture("_BaseMap", tex); m.mainTexture = tex;
            pictureMats[texture] = m;
            return m;
        }

        public Material FlatMaterial(Color c)
        {
            var m = new Material(Flat);
            m.SetColor("_BaseColor", c); m.color = c;
            return m;
        }

        void AddToSet(string set, GameObject go)
        {
            List<GameObject> list;
            if (!sets.TryGetValue(set, out list)) sets[set] = list = new List<GameObject>();
            list.Add(go);
        }

        public bool HasSet(string set) { return sets.ContainsKey(set); }

        /// <summary>Shows or hides a set (the tables, a cabinet open or closed), with its boxes.</summary>
        public void ShowSet(string set, bool on)
        {
            List<GameObject> list;
            if (!sets.TryGetValue(set, out list)) return;
            foreach (var go in list) go.SetActive(on);
        }
    }
}
