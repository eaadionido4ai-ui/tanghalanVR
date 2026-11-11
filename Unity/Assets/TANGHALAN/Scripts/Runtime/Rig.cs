// The visitor's body: the SIMPLE template's FPS Player (an XR Origin with a camera and two controllers),
// seen from TANGHALAN VR. It moves the visitor (to a spot, or under a table), lowers the view when they take
// cover, slows them down when the go bag is too heavy, reads the buttons, and, with no headset, lets them play
// with the keyboard and the mouse.
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Tanghalan.Vr
{
    public sealed class Rig : MonoBehaviour
    {
        public XROrigin Origin { get; private set; }
        public Camera Head { get; private set; }
        public Transform RightHand { get; private set; }
        public Transform LeftHand { get; private set; }
        /// <summary>True when no headset is running: keyboard and mouse.</summary>
        public bool Desktop { get; private set; }

        CharacterController body;
        GameObject locomotion;
        UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider move;
        float baseSpeed = 2f, speedK = 1f;
        Transform floorOffset;
        float offsetBase, offsetWant, offsetNow;
        float standing = 1.2f;
        float yaw, pitch;
        Renderer fade;
        float fadeNow, fadeWant;

        InputAction trigger, triggerLeft, grip, primary, mousePress, duckKey, holdKey;
        bool duckToggle;

        public void Init()
        {
            Origin = FindFirstObjectByType<XROrigin>();
            if (Origin == null) { Debug.LogError("TANGHALAN: no XR Origin (FPS Player) in the scene."); return; }
            Head = Origin.Camera != null ? Origin.Camera : Camera.main;
            body = Origin.GetComponent<CharacterController>();
            floorOffset = Origin.CameraFloorOffsetObject != null ? Origin.CameraFloorOffsetObject.transform : Head.transform.parent;
            offsetBase = offsetNow = offsetWant = floorOffset.localPosition.y;
            foreach (var t in Origin.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Right Controller") RightHand = t;
                else if (t.name == "Left Controller") LeftHand = t;
                else if (t.name == "Locomotion") locomotion = t.gameObject;
            }
            move = Origin.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>(true);
            if (move != null) baseSpeed = move.moveSpeed;

            Desktop = !XRSettings.isDeviceActive;
            if (Desktop) StartDesktop();

            trigger = Action("trigger", "<XRController>{RightHand}/triggerPressed", "<XRController>{RightHand}/{TriggerButton}");
            triggerLeft = Action("triggerLeft", "<XRController>{LeftHand}/triggerPressed", "<XRController>{LeftHand}/{TriggerButton}");
            grip = Action("grip", "<XRController>{RightHand}/gripPressed", "<XRController>{LeftHand}/gripPressed", "<XRController>{RightHand}/{GripButton}", "<XRController>{LeftHand}/{GripButton}", "<Keyboard>/space");
            primary = Action("primary", "<XRController>{RightHand}/primaryButton", "<XRController>{LeftHand}/primaryButton");
            mousePress = Action("mouse", "<Mouse>/leftButton");
            duckKey = Action("duckKey", "<Keyboard>/c");

            // a black sphere around the head, to fade the view while the visitor is moved
            var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(f.GetComponent<Collider>());
            f.name = "Fade"; f.transform.SetParent(Head.transform, false); f.transform.localScale = Vector3.one * 0.6f;
            fade = f.GetComponent<Renderer>();
            fade.sharedMaterial = new Material(Resources.Load<Material>(World.MaterialPath + "TanghalanWater"));
            fade.sharedMaterial.SetColor("_BaseColor", new Color(0, 0, 0, 0));
            fade.sharedMaterial.SetFloat("_Cull", 0); // seen from inside
            fade.enabled = false;
        }

        static InputAction Action(string name, params string[] bindings)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var b in bindings) a.AddBinding(b);
            a.Enable();
            return a;
        }

        void OnDestroy()
        {
            foreach (var a in new[] { trigger, triggerLeft, grip, primary, mousePress, duckKey }) if (a != null) a.Dispose();
        }

        // ---------- what the visitor presses ----------

        /// <summary>The trigger (or the mouse button) went down this frame. hand: 1 right, -1 left, 0 mouse.</summary>
        public bool Pressed(out int hand)
        {
            hand = 0;
            if (trigger.WasPressedThisFrame()) { hand = 1; return true; }
            if (triggerLeft.WasPressedThisFrame()) { hand = -1; return true; }
            if (Desktop && mousePress.WasPressedThisFrame()) { hand = 0; return true; }
            return false;
        }

        /// <summary>Holding on: either grip button (Space with no headset).</summary>
        public bool Holding { get { return grip.IsPressed(); } }

        /// <summary>The A or X button (C with no headset) turns ducking on and off.</summary>
        public bool DuckPressed { get { return primary.WasPressedThisFrame() || duckKey.WasPressedThisFrame(); } }

        public void ToggleDuck() { duckToggle = !duckToggle; }
        public void SetDuck(bool on) { duckToggle = on; }

        /// <summary>The head is low (crouching for real), or the duck button is on.</summary>
        public bool Ducked
        {
            get { float h = Head.transform.localPosition.y; return duckToggle || (!Desktop && standing > 0.8f && h < standing * 0.72f); }
        }

        public Ray PointingRay(int hand)
        {
            if (Desktop || hand == 0)
            {
                var m = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
                return Head.ScreenPointToRay(m);
            }
            var t = hand < 0 ? LeftHand : RightHand;
            if (t == null) return new Ray(Head.transform.position, Head.transform.forward);
            return new Ray(t.position, t.forward);
        }

        public void Haptic(float amplitude, float seconds)
        {
            if (Desktop) return;
            foreach (var node in new[] { XRNode.LeftHand, XRNode.RightHand })
            {
                var dev = InputDevices.GetDeviceAtXRNode(node);
                if (dev.isValid) dev.SendHapticImpulse(0, Mathf.Clamp01(amplitude), seconds);
            }
        }

        // ---------- where the visitor is ----------

        /// <summary>The point on the floor under the visitor's head.</summary>
        public Vector3 Feet
        {
            get { var p = Head.transform.position; return new Vector3(p.x, Origin.transform.position.y, p.z); }
        }

        public Vector3 Forward
        {
            get { var f = Head.transform.forward; f.y = 0; return f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward; }
        }

        /// <summary>Moves the visitor so that they stand at a point, looking along a direction.</summary>
        public void Place(Vector3 feet, Vector3 look)
        {
            if (body != null) body.enabled = false;
            look.y = 0;
            if (look.sqrMagnitude > 1e-4f)
            {
                float turn = Vector3.SignedAngle(Forward, look.normalized, Vector3.up);
                Origin.transform.RotateAround(Head.transform.position, Vector3.up, turn);
                if (Desktop) yaw = Origin.transform.eulerAngles.y;
            }
            var head = Head.transform.position;
            var delta = new Vector3(feet.x - head.x, 0, feet.z - head.z);
            Origin.transform.position += delta;
            var p = Origin.transform.position; p.y = feet.y; Origin.transform.position = p;
            if (body != null) body.enabled = canWalk;
        }

        /// <summary>Lowers (or raises back) the view, so the eyes are this high above the floor; null puts it back.</summary>
        public void EyeHeight(float? metres)
        {
            if (metres == null) { offsetWant = offsetBase; return; }
            float headLocal = Head.transform.localPosition.y;
            offsetWant = metres.Value - headLocal;
        }

        /// <summary>Walking on or off (the stick, teleporting, turning). Off, the body does not collide either, so it can sit under a table.</summary>
        public void CanMove(bool on)
        {
            if (locomotion != null) locomotion.SetActive(on);
            if (body != null) body.enabled = on;
            canWalk = on;
        }
        bool canWalk = true;

        /// <summary>How fast the visitor walks, as a share of the normal speed (a heavy go bag: 0.7).</summary>
        public void SpeedFactor(float k)
        {
            speedK = k;
            if (move != null) move.moveSpeed = baseSpeed * k;
        }

        public void FadeOut() { fadeWant = 1; }
        public void FadeIn() { fadeWant = 0; }

        void Update()
        {
            if (Origin == null) return;
            if (!Desktop)
            {
                float h = Head.transform.localPosition.y;
                if (offsetWant == offsetBase) standing = Mathf.Max(standing * 0.999f, h); // the tallest the visitor stands, slowly forgotten
            }
            offsetNow = Mathf.MoveTowards(offsetNow, offsetWant + (Desktop && duckToggle ? -0.45f : 0), Time.deltaTime * 1.6f);
            var lp = floorOffset.localPosition; lp.y = offsetNow; floorOffset.localPosition = lp;
            fadeNow = Mathf.MoveTowards(fadeNow, fadeWant, Time.deltaTime * 3f);
            fade.enabled = fadeNow > 0.001f;
            if (fade.enabled) fade.sharedMaterial.SetColor("_BaseColor", new Color(0, 0, 0, fadeNow));
            if (Desktop) DesktopUpdate();
        }

        // ---------- no headset: keyboard and mouse ----------

        void StartDesktop()
        {
            // the headset's pose drivers would hold the camera still; with no headset the mouse turns it
            foreach (var c in Head.GetComponents<Behaviour>())
                if (c.GetType().Name.Contains("TrackedPoseDriver")) c.enabled = false;
            var lp = Head.transform.localPosition; lp.y = 1.25f; Head.transform.localPosition = lp;
            Head.transform.localRotation = Quaternion.identity;
            yaw = Origin.transform.eulerAngles.y;
            if (RightHand != null) RightHand.gameObject.SetActive(false);
            if (LeftHand != null) LeftHand.gameObject.SetActive(false);
            Debug.Log("TANGHALAN: no headset found; playing with the keyboard and the mouse.");
        }

        void DesktopUpdate()
        {
            var kb = Keyboard.current; var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                var d = mouse.delta.ReadValue();
                yaw += d.x * 0.15f; pitch = Mathf.Clamp(pitch - d.y * 0.15f, -70, 70);
            }
            if (kb != null)
            {
                if (kb.leftArrowKey.isPressed || kb.qKey.isPressed) yaw -= 90 * Time.deltaTime;
                if (kb.rightArrowKey.isPressed || kb.eKey.isPressed) yaw += 90 * Time.deltaTime;
            }
            Origin.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Head.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            if (!canWalk || kb == null || body == null) return;
            var v = Vector3.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += Vector3.forward;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v += Vector3.back;
            if (kb.aKey.isPressed) v += Vector3.left;
            if (kb.dKey.isPressed) v += Vector3.right;
            v = Origin.transform.rotation * Vector3.ClampMagnitude(v, 1) * 2.2f * speedK;
            body.Move((v + Vector3.down * 3f) * Time.deltaTime);
        }
    }
}
