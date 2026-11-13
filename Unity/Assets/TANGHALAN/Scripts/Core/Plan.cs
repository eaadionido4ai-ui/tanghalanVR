// The museum's plan, for the people who walk in it: the boxes nobody walks through (the walls and the
// furniture), the boxes the visitor points at (the works, the story stands, the tables), and the sturdy
// tables to hide under in the earthquake drill. The values are in Generated/MuseumPlan.g.cs, written by
// Tools/web-export/gen-csharp.mjs from the same boxes the museum's model carries. Positions are in the web
// app's coordinates: metres, x to the east, z to the south, y up.
using System;

namespace Tanghalan
{
    /// <summary>A box in the museum, as the model's COL__ and HOT__ boxes. Set: the switchable set it belongs to, or "".</summary>
    public sealed class PlanBox
    {
        public readonly string Id, Set;
        public readonly double X0, X1, Y0, Y1, Z0, Z1;

        public PlanBox(string id, string set, double x0, double x1, double y0, double y1, double z0, double z1)
        {
            Id = id; Set = set; X0 = x0; X1 = x1; Y0 = y0; Y1 = y1; Z0 = z0; Z1 = z1;
        }

        public double CX { get { return (X0 + X1) / 2; } }
        public double CZ { get { return (Z0 + Z1) / 2; } }

        /// <summary>Whether a point on the floor is inside the box, grown by pad on every side.</summary>
        public bool Covers(double x, double z, double pad)
        {
            return x >= X0 - pad && x <= X1 + pad && z >= Z0 - pad && z <= Z1 + pad;
        }
    }

    /// <summary>A sturdy table: its middle, and the way out from under it (towards the middle of the room).</summary>
    public sealed class PlanTable
    {
        public readonly string Id;
        public readonly double X, Z, NX, NZ;

        public PlanTable(string id, double x, double z, double nx, double nz)
        {
            Id = id; X = x; Z = z;
            double n = Math.Sqrt(nx * nx + nz * nz);
            // a table in the very middle of its room has no way out of its own: north, as Unity does
            if (n < 1e-6) { NX = 0; NZ = -1; } else { NX = nx / n; NZ = nz / n; }
        }

        /// <summary>Half the table's length, along x (the tables of the Go Bag Room are longer).</summary>
        public double HalfLength { get { return Id.StartsWith("drill_gb_") ? 0.8 : 0.65; } }

        /// <summary>Where a person kneels under the table: side -1 or +1 along its length, the visitor's place (the middle) left free.</summary>
        public P3 Slot(int side) { return new P3(X + side * (HalfLength - 0.24), 0, Z); }

        public double FacingHeading { get { return Floor.HeadingOf(NX, NZ); } }
    }

    public static partial class MuseumPlan
    {
        public static PlanBox HotBox(string id)
        {
            foreach (var b in Hot) if (b.Id == id) return b;
            return null;
        }

        public static PlanTable Table(string id)
        {
            foreach (var t in Tables) if (t.Id == id) return t;
            return null;
        }
    }
}
