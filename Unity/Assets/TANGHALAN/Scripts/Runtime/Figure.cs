// A person on screen: Bayani, a neighbor or a museum visitor. The model (Resources/TANGHALAN/Models, made by
// Tools/web-export/src/people.js) comes in seven parts, the body, two thighs, two shins and two arms, with
// pivots (P__) at the hips, the knees and the shoulders. There is no skeleton: this turns the parts about
// those pivots, lowers and bends the body, and swings everything in step with the distance walked, as the
// web app does.
//
// Something else moves the figure (sets its position and rotation) and says what it is doing (Pose); this
// works out the rest. In the figure's own space x is its right, y up and z forward.
using Tanghalan;
using UnityEngine;

namespace Tanghalan.Vr
{
    public sealed class Figure : MonoBehaviour
    {
        /// <summary>What the person is doing.</summary>
        public Pose Pose = Pose.Stand;
        /// <summary>A grown-up (longer legs, and a different way of standing).</summary>
        public bool Adult { get; private set; }
        /// <summary>The top of the head, above the feet.</summary>
        public float Height { get; private set; }
        /// <summary>A point at the right hand (it follows the arm).</summary>
        public Transform HandR { get; private set; }
        public Transform HandL { get; private set; }

        Transform pelvis, torso, hipR, hipL, kneeR, kneeL, shR, shL;
        float hipY, thigh, shin, crouchY, crouchLean, duckLean;
        // the joints now, in degrees (and the drop of the hips in metres); they ease towards what the pose wants
        float drop, lean, twist, legR, legL, bendR, bendL, armRx, armRz, armLx, armLz;
        // crouching (the drill): sitting back on the heels, the thighs forward, the shins folded back under them,
        // and the body bent far forward, low enough for a child or a grown-up to fit under a table 72 cm high
        const float CrouchThigh = -85, CrouchKnee = 165;
        // walking: the step's phase, how much of a full stride, and where the figure was last frame
        float phase, stride, clock, seed;
        Vector3 last;
        bool hasLast;

        // the web app's walk: the phase turns 4.2 radians a metre, the legs swing up to 0.6 radians
        const float PhasePerMetre = 4.2f, Swing = 34f;

        /// <summary>Makes a figure from a person model, standing at the parent's origin.</summary>
        public static Figure Make(World world, string model, Transform parent)
        {
            var holder = world.Spawn(model, parent);
            if (holder == null) return null;
            var f = holder.AddComponent<Figure>();
            f.Build();
            return f;
        }

        void Build()
        {
            Transform body = null, legRm = null, legLm = null, shinRm = null, shinLm = null, armRm = null, armLm = null;
            // where the model's pivots are, in this figure's space (with the children's measurements, should one be missing)
            Vector3 pHipR = new Vector3(0.09f, 0.48f, 0), pHipL = new Vector3(-0.09f, 0.48f, 0);
            Vector3 pKneeR = new Vector3(0.09f, 0.27f, 0), pKneeL = new Vector3(-0.09f, 0.27f, 0);
            Vector3 pShR = new Vector3(0.235f, 0.98f, 0), pShL = new Vector3(-0.235f, 0.98f, 0);
            Vector3 pHandR = new Vector3(0.255f, 0.6f, 0), pHandL = new Vector3(-0.255f, 0.6f, 0);
            Height = 1.45f;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n.EndsWith("-body")) body = t;
                else if (n.EndsWith("-legR")) legRm = t;
                else if (n.EndsWith("-legL")) legLm = t;
                else if (n.EndsWith("-shinR")) shinRm = t;
                else if (n.EndsWith("-shinL")) shinLm = t;
                else if (n.EndsWith("-armR")) armRm = t;
                else if (n.EndsWith("-armL")) armLm = t;
                else if (n.StartsWith("P__"))
                {
                    var p = transform.InverseTransformPoint(t.position);
                    switch (n.Substring(3))
                    {
                        case "hipR": pHipR = p; break;
                        case "hipL": pHipL = p; break;
                        case "kneeR": pKneeR = p; break;
                        case "kneeL": pKneeL = p; break;
                        case "shoulderR": pShR = p; break;
                        case "shoulderL": pShL = p; break;
                        case "handR": pHandR = p; break;
                        case "handL": pHandL = p; break;
                        case "top": Height = p.y; break;
                    }
                }
            }
            if (body == null) Debug.LogWarning("TANGHALAN: " + name + " has no parts to move; is its model up to date?");
            hipY = (pHipR.y + pHipL.y) / 2;
            Adult = hipY > 0.62f;
            thigh = hipY - (pKneeR.y + pKneeL.y) / 2;
            shin = hipY - thigh;
            // how high the hips are when crouching: the folded legs, and their thickness
            crouchY = thigh * Mathf.Cos(CrouchThigh * Mathf.Deg2Rad) + shin * Mathf.Cos((CrouchThigh + CrouchKnee) * Mathf.Deg2Rad) + (Adult ? 0.07f : 0.06f);
            // under a table the body folds right over; in the open it can stay rounder
            crouchLean = Adult ? 72f : 70f;
            duckLean = Adult ? 58f : 55f;
            pelvis = Node("pelvis", transform, new Vector3(0, hipY, 0));
            torso = Node("torso", pelvis, Vector3.zero);
            var up = new Vector3(0, hipY, 0);
            hipR = Node("hipR", pelvis, pHipR - up);
            hipL = Node("hipL", pelvis, pHipL - up);
            kneeR = Node("kneeR", hipR, pKneeR - pHipR);
            kneeL = Node("kneeL", hipL, pKneeL - pHipL);
            shR = Node("shoulderR", torso, pShR - up);
            shL = Node("shoulderL", torso, pShL - up);
            HandR = Node("handR", shR, pHandR - pShR);
            HandL = Node("handL", shL, pHandL - pShL);
            Attach(body, torso); Attach(legRm, hipR); Attach(legLm, hipL); Attach(shinRm, kneeR); Attach(shinLm, kneeL);
            Attach(armRm, shR); Attach(armLm, shL);
            seed = Random.value * 10f;
        }

        static Transform Node(string n, Transform parent, Vector3 local)
        {
            var t = new GameObject(n).transform;
            t.SetParent(parent, false);
            t.localPosition = local;
            return t;
        }

        static void Attach(Transform part, Transform to) { if (part != null) part.SetParent(to, true); }

        void LateUpdate()
        {
            if (pelvis == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            clock += dt;
            // how fast the figure moves, from where it was
            var here = transform.position;
            float moved = 0;
            if (hasLast) { var d = here - last; d.y = 0; moved = d.magnitude; if (moved > 2f) moved = 0; }
            last = here; hasLast = true;
            float speed = moved / dt;
            bool kneeling = Pose == Pose.Duck || Pose == Pose.Cover || Pose == Pose.Sit;
            bool running = Pose == Pose.Run;
            phase += moved * (running ? 3.4f : PhasePerMetre);
            float want = kneeling ? 0 : Mathf.Clamp01(speed / (running ? 1.6f : 0.9f));
            stride = Mathf.Lerp(stride, want, 1 - Mathf.Exp(-dt * 8));

            // what the pose wants
            float tDrop = 0, tLean = 0, tTwist = 0, tLegR = 0, tLegL = 0, tBendR = 0, tBendL = 0, tArmRx = 0, tArmRz = 0, tArmLx = 0, tArmLz = 0;
            float t = clock + seed;
            switch (Pose)
            {
                case Pose.Stand:
                case Pose.Walk:
                    tLean = 1.2f * Mathf.Sin(t * 1.3f);
                    break;
                case Pose.Run:
                    tLean = 9;
                    break;
                case Pose.Look:
                    tLean = -3;
                    if (Adult) { tArmRx = tArmLx = -28; tArmRz = -14; tArmLz = 14; }   // hands together in front
                    else { tArmRx = tArmLx = 16; tArmRz = -8; tArmLz = 8; }            // hands behind the back
                    break;
                case Pose.Point:
                    tLean = -4; tArmRx = -112; tArmRz = 6;
                    tArmLx = 10; tArmLz = 6;
                    break;
                case Pose.Talk:
                    tTwist = 7 * Mathf.Sin(t * 0.9f); tLean = 2 * Mathf.Sin(t * 1.7f);
                    tArmRx = -48 + 16 * Mathf.Sin(t * 3.1f); tArmRz = 12;
                    tArmLx = -28 + 10 * Mathf.Sin(t * 2.3f + 1); tArmLz = -8;
                    break;
                case Pose.Listen:
                    tLean = 3 + 5 * Mathf.Max(0, Mathf.Sin(t * 2.2f)) * Mathf.Max(0, Mathf.Sin(t * 0.37f));  // a nod now and then
                    tArmRx = tArmLx = -42; tArmRz = -38; tArmLz = 38;                                          // arms folded
                    break;
                case Pose.Wave:
                    tArmRz = 150 + 16 * Mathf.Sin(t * 9); tArmRx = -12;
                    break;
                case Pose.Duck:
                    tDrop = hipY - crouchY; tLegR = tLegL = CrouchThigh; tBendR = tBendL = CrouchKnee; tLean = duckLean;
                    tArmRx = tArmLx = -168; tArmRz = -20; tArmLz = 20;     // the arms along the head, covering it
                    break;
                case Pose.Cover:
                    tDrop = hipY - crouchY; tLegR = tLegL = CrouchThigh; tBendR = tBendL = CrouchKnee; tLean = crouchLean;
                    tArmRx = -78; tArmRz = 30;                              // the right hand holds the table's leg
                    tArmLx = -168; tArmLz = 20;                             // the left arm over the head
                    break;
                case Pose.Sit:
                    // on the edge of a step: the knees up a little, the shins down to the ground
                    tDrop = hipY - 0.08f; tLegR = tLegL = -105; tBendR = tBendL = 95; tLean = 6;
                    tArmRx = tArmLx = -55; tArmRz = -8; tArmLz = 8;         // hands on the knees
                    break;
                case Pose.Sweep:
                    tLean = 24; tTwist = 20 * Mathf.Sin(t * 3.2f);
                    tArmRx = -52; tArmRz = -14; tArmLx = -40; tArmLz = 12;
                    tLegR = -10; tLegL = 8; tBendR = 14; tBendL = 10;
                    break;
                case Pose.Throw:
                    tLean = 6; tArmRx = -150; tArmRz = 8; tArmLx = -30;
                    break;
                case Pose.Catch:
                    tArmRx = tArmLx = -96; tArmRz = -10; tArmLz = 10;
                    break;
            }
            // the walk on top: legs and arms swing in step, the body bobs
            float amp = running ? 50 : Swing;
            float s = Mathf.Sin(phase) * amp * stride;
            tLegR += s; tLegL -= s;
            // each knee bends as its leg swings forward
            float lift = running ? 75 : 40;
            tBendR += stride * (6 + lift * Mathf.Max(0, -Mathf.Cos(phase)));
            tBendL += stride * (6 + lift * Mathf.Max(0, Mathf.Cos(phase)));
            if (Pose == Pose.Walk || Pose == Pose.Run || Pose == Pose.Stand) { tArmRx -= s * 0.9f; tArmLx += s * 0.9f; }
            float bob = Mathf.Abs(Mathf.Cos(phase)) * (running ? 0.07f : 0.04f) * stride;

            float k = 1 - Mathf.Exp(-dt * 10);
            drop = Mathf.Lerp(drop, tDrop, k); lean = Mathf.Lerp(lean, tLean, k); twist = Mathf.Lerp(twist, tTwist, k);
            legR = Mathf.Lerp(legR, tLegR, k); legL = Mathf.Lerp(legL, tLegL, k);
            bendR = Mathf.Lerp(bendR, tBendR, k); bendL = Mathf.Lerp(bendL, tBendL, k);
            armRx = Mathf.Lerp(armRx, tArmRx, k); armRz = Mathf.Lerp(armRz, tArmRz, k);
            armLx = Mathf.Lerp(armLx, tArmLx, k); armLz = Mathf.Lerp(armLz, tArmLz, k);

            pelvis.localPosition = new Vector3(0, hipY - drop + bob, 0);
            torso.localRotation = Quaternion.Euler(lean, twist, 0);
            hipR.localRotation = Quaternion.Euler(legR, 0, 0);
            hipL.localRotation = Quaternion.Euler(legL, 0, 0);
            kneeR.localRotation = Quaternion.Euler(bendR, 0, 0);
            kneeL.localRotation = Quaternion.Euler(bendL, 0, 0);
            shR.localRotation = Quaternion.Euler(armRx, 0, armRz);
            shL.localRotation = Quaternion.Euler(armLx, 0, armLz);
        }

        /// <summary>Places the figure at once (no walking in between), facing a heading.</summary>
        public void Snap(Vector3 at, Quaternion facing)
        {
            transform.SetPositionAndRotation(at, facing);
            last = at; hasLast = true;
        }
    }
}
