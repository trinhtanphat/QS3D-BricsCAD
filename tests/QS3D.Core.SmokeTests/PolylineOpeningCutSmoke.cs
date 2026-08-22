using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineOpeningCutSmoke
    {
        public static void Run()
        {
            ProjectsOntoHorizontalSegment();
            ProjectsOntoVerticalSegment();
            RejectsCornerCrossingCut();
            RejectsFarOpening();
            RejectsDegenerateCenterline();
        }

        private static void ProjectsOntoHorizontalSegment()
        {
            var plan = PolylineOpeningCutPlanner.Plan(Input(new Point2(2d, 0.05d)));
            Equal(0, plan.SegmentIndex);
            Near(2d, plan.ProjectedCenter.X);
            Near(0d, plan.ProjectedCenter.Y);
            Near(1d, plan.Tangent.X);
            Near(0d, plan.Tangent.Y);
            Near(2d, plan.StationM);
            Near(0.05d, plan.CenterlineOffsetM);
        }

        private static void ProjectsOntoVerticalSegment()
        {
            var plan = PolylineOpeningCutPlanner.Plan(Input(new Point2(4.04d, 1.5d)));
            Equal(1, plan.SegmentIndex);
            Near(4d, plan.ProjectedCenter.X);
            Near(1.5d, plan.ProjectedCenter.Y);
            Near(0d, plan.Tangent.X);
            Near(1d, plan.Tangent.Y);
            Near(5.5d, plan.StationM);
            Near(1.5d, plan.SegmentStationM);
        }

        private static void RejectsCornerCrossingCut()
        {
            Throws<InvalidOperationException>(() => PolylineOpeningCutPlanner.Plan(Input(new Point2(3.8d, 0d))));
        }

        private static void RejectsFarOpening()
        {
            Throws<InvalidOperationException>(() => PolylineOpeningCutPlanner.Plan(Input(new Point2(2d, 1d))));
        }

        private static void RejectsDegenerateCenterline()
        {
            var input = Input(new Point2(2d, 0d));
            input.Centerline = new[] { new Point2(0d, 0d), new Point2(0d, 0d), new Point2(4d, 0d) };
            Throws<ArgumentException>(() => PolylineOpeningCutPlanner.Plan(input));
        }

        private static PolylineOpeningCutInput Input(Point2 center) => new PolylineOpeningCutInput
        {
            Centerline = new[] { new Point2(0d, 0d), new Point2(4d, 0d), new Point2(4d, 3d) },
            OpeningCenter = center,
            HostThicknessM = 0.2d,
            HostHeightM = 3d,
            OpeningWidthM = 0.9d,
            OpeningHeightM = 2d,
            SillHeightM = 0d,
            ClearanceM = 0.01d,
            MaximumCenterlineOffsetM = 0.35d
        };

        private static void Near(double expected, double actual, double tolerance = 1e-9)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
