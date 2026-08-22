using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurvedOpeningProjectionOverflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var plan = CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = new[]
                {
                    new Point2(0d, 0d),
                    new Point2(1e200, 0d)
                },
                OpeningPoint = new Point2(5e199, 0d),
                OpeningWidthM = 1e190,
                HostThicknessM = 1e100,
                ClearanceM = 0d,
                MaximumCenterlineOffsetM = 1d,
                AmbiguityMarginM = 0d,
                ToleranceM = 1e-9d
            });

            if (!Finite(plan.CenterStationM) || Math.Abs(plan.CenterStationM / 5e199 - 1d) > 1e-12d)
                throw new Exception("Expected a finite midpoint curved-opening station for a long representable host segment.");
            if (!Finite(plan.StartStationM) || !Finite(plan.EndStationM) || !(plan.EndStationM > plan.StartStationM))
                throw new Exception("Expected a finite non-degenerate curved-opening station range.");
            if (!Finite(plan.CenterlineOffsetM) || plan.CenterlineOffsetM != 0d)
                throw new Exception("Expected zero centerline offset for an opening point on the host segment.");
            if (plan.CutterCenterline.Count < 2)
                throw new Exception("Expected a non-degenerate curved-opening cutter centerline.");
            if (plan.CutterPolygon.Count < 4)
                throw new Exception("Expected a non-degenerate curved-opening cutter footprint.");
            if (!Finite(plan.CutterFootprintAreaM2) || !(plan.CutterFootprintAreaM2 > 0d))
                throw new Exception("Expected a finite positive curved-opening cutter footprint area.");

            foreach (var point in plan.CutterCenterline) EnsureFinite(point, "cutter centerline");
            foreach (var point in plan.CutterPolygon) EnsureFinite(point, "cutter polygon");
        }

        private static void EnsureFinite(Point2 point, string label)
        {
            if (!Finite(point.X) || !Finite(point.Y)) throw new Exception("Expected finite " + label + " coordinates.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
