// The street outside the Stop Hazards Early hall, as a grid of one-metre cells: where the yards, the
// sidewalk, the canal, the road and the museum's terrace are, and how high the ground is in each cell.
//
// The same numbers are in three places, and must stay the same in all of them:
//   Tools/web-export/src/street.js      what you see (the model)
//   this file                           what Unity computes with when GAMA is not connected
//   GAMA/models/TANGHALAN Flood.gaml    what GAMA computes with
//
// Coordinates are the web app's: metres, x to the east, z to the south, y up. The museum's floor is y = 0.
namespace Tanghalan
{
    public static class StreetPlan
    {
        /// <summary>The grid's north-west corner, and its size in cells (each one metre square).</summary>
        public const double X0 = -27, Z0 = -17;
        public const int NX = 14, NZ = 25;

        public const double Yard = -0.40, WestSidewalk = -0.45, Road = -0.60, Terrace = 0.0, Court = 0.0;
        public const double CanalSouth = -1.35, CanalNorth = -1.55;
        /// <summary>The canal is the column of cells with this i.</summary>
        public const int CanalI = 6;
        /// <summary>The covered court is the cells with i up to 5 and j from this on.</summary>
        public const int CourtJ = 20;
        /// <summary>The cell in front of Rico's door, where the water gauge stands.</summary>
        public const int DoorI = 3, DoorJ = 10;
        /// <summary>The road is the cells with i from RoadI0 to RoadI1.</summary>
        public const int RoadI0 = 7, RoadI1 = 11;

        public static int Index(int i, int j) { return j * NX + i; }

        /// <summary>How high the ground is in a cell.</summary>
        public static double Ground(int i, int j)
        {
            if (i <= 5 && j >= CourtJ) return Court;
            if (i <= 3) return Yard;
            if (i <= 5) return WestSidewalk;
            if (i == CanalI) return CanalSouth - (CanalSouth - CanalNorth) * (24.5 - j) / 25.0; // the bed, lower to the north
            if (i <= RoadI1) return Road;
            return Terrace;
        }

        /// <summary>The cell that holds a point, or false when the point is off the grid.</summary>
        public static bool CellAt(double x, double z, out int i, out int j)
        {
            i = (int)System.Math.Floor(x - X0); j = (int)System.Math.Floor(z - Z0);
            return i >= 0 && i < NX && j >= 0 && j < NZ;
        }

        public static double CenterX(int i) { return X0 + i + 0.5; }
        public static double CenterZ(int j) { return Z0 + j + 0.5; }
    }
}
