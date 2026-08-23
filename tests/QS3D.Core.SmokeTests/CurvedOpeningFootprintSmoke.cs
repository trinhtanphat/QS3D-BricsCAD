using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurvedOpeningFootprintSmoke
    {
        public static void Run()
        {
            StraightPathMatchesExpectedSpan();
            CornerSpanIncludesIntermediateVertex();
            RejectsFarAndAmbiguousBranches();
            RejectsOpeningPastHostEnd();
            RejectsCumulativeStationPrecisionCollapse();
            RejectsInteriorProjectionStationPrecisionCollapse();
            RejectsOpeningSpanStationPrecisionCollapse();
        }

        private static void StraightPathMatchesExpectedSpan()
        {
            var plan = CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = new[] { new Point2(0, 0), new Point2(5, 0) },
                OpeningPoint = new Point2(2.5, 0.1),
                OpeningWidthM = 1d,
                HostThicknessM = 0.2d,
                ClearanceM = 0.01d,
                MaximumCenterlineOffsetM = 0.35d
            });
            Near(2d, plan.StartStationM, 1e-12d);
            Near(3d, plan.EndStationM, 1e-12d);
            Near(0.1d, plan.CenterlineOffsetM, 1e-12d);
            Near(0.22d, plan.CutterFootprintAreaM2, 1e-10d);
            Equal(2, plan.CutterCenterline.Count);
        }

        private static void CornerSpanIncludesIntermediateVertex()
        {
            var plan = CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = new[] { new Point2(0, 0), new Point2(2, 0), new Point2(2, 2) },
                OpeningPoint = new Point2(2.02, 0.02),
                OpeningWidthM = 1d,
                HostThicknessM = 0.2d,
                ClearanceM = 0.01d,
                MaximumCenterlineOffsetM = 0.2d,
                AmbiguityMarginM = 0d
            });
            if (plan.CutterCenterline.Count < 3) throw new Exception("Expected corner cutter centerline to keep the intermediate wall vertex.");
            if (plan.CutterPolygon.Count < 4) throw new Exception("Expected a valid cutter footprint polygon.");
        }

        private static void RejectsFarAndAmbiguousBranches()
        {
            Throws<InvalidOperationException>(() => CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = new[] { new Point2(0, 0), new Point2(5, 0) },
                OpeningPoint = new Point2(2.5, 1d),
                OpeningWidthM = 1d,
                HostThicknessM = 0.2d,
                MaximumCenterlineOffsetM = 0.2d
            }));

            Throws<InvalidOperationException>(() => CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = new[]
                {
                    new Point2(0, 0), new Point2(5, 0), new Point2(5, 2),
                    new Point2(0, 2), new Point2(0, 0.2), new Point2(5, 0.2)
                },
                OpeningPoint = new Point2(2.5, 0.1),
                OpeningWidthM = 0.5d,
                HostThicknessM = 0.2d,
                MaximumCenterlineOffsetM = 0.2d,
                AmbiguityMarginM = 0.001d
            }));
        }

        private static void RejectsOpeningPastHostEnd()
        {
            Throws<InvalidOperationException>(() => CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = new[] { new Point2(0, 0), new Point2(2, 0) },
                OpeningPoint = new Point2(0.1, 0),
                OpeningWidthM = 1d,
                HostThicknessM = 0.2d
            }));
        }

        private static void RejectsCumulativeStationPrecisionCollapse()
        {
            Throws<OverflowException>(() => CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = new[]
                {
                    new Point2(0d, 0d),
                    new Point2(1e16d, 0d),
                    new Point2(1e16d, 1d)
                },
                OpeningPoint = new Point2(1e16d, 0.5d),
                OpeningWidthM = 0.5d,
                HostThicknessM = 0.2d,
                MaximumCenterlineOffsetM = 1d
            }));
        }

        private static void RejectsInteriorProjectionStationPrecisionCollapse()
        {
            Throws<OverflowException>(() => CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = new[]
                {
                    new Point2(0d, 0d),
                    new Point2(1e16d, 0d),
                    new Point2(1e16d, 2d)
                },
                OpeningPoint = new Point2(1e16d, 0.5d),
                OpeningWidthM = 1d,
                HostThicknessM = 0.2d,
                MaximumCenterlineOffsetM = 1d
            }));
        }

        private static void RejectsOpeningSpanStationPrecisionCollapse()
        {
            Throws<OverflowException>(() => CurvedOpeningFootprintPlanner.Plan(new CurvedOpeningFootprintInput
            {
                Centerline = new[]
                {
                    new Point2(0d, 0d),
                    new Point2(1e16d, 0d),
                    new Point2(1e16d, 4d)
                },
                OpeningPoint = new Point2(1e16d, 2d),
                OpeningWidthM = 1d,
                HostThicknessM = 0.2d,
                MaximumCenterlineOffsetM = 1d
            }));
        }

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
