// Where things are. TANGHALAN's content (the web app, the Blender files, Tanghalan.Core) uses the web app's
// coordinates: metres, x to the east, y up, z to the south. In Unity the same point is (x, y, -z), so north
// is Unity's forward (+z). GAMA uses (x + 30, z + 25) on the ground (see TanghalanSimulationManager).
using Tanghalan;
using UnityEngine;

namespace Tanghalan.Vr
{
    public static class WebSpace
    {
        public static Vector3 ToUnity(double x, double y, double z) { return new Vector3((float)x, (float)y, (float)-z); }
        public static Vector3 ToUnity(P3 p) { return ToUnity(p.X, p.Y, p.Z); }
        public static double WebX(Vector3 u) { return u.x; }
        public static double WebZ(Vector3 u) { return -u.z; }

        /// <summary>A heading in degrees (0 north, 90 east) as a Unity rotation.</summary>
        public static Quaternion Heading(double degrees) { return Quaternion.Euler(0, (float)degrees, 0); }

        /// <summary>A flat rotation that looks from one point towards another.</summary>
        public static Quaternion Facing(Vector3 from, Vector3 to)
        {
            var d = to - from; d.y = 0;
            return d.sqrMagnitude < 1e-6f ? Quaternion.identity : Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }
}
