using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainPathFramePlannerSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            StraightPathPreservesSingleFrame();
            BentPathSplitsFrameAtCorner();
            SmallFrameCrossingCornerSplitsDeterministically();
            ProjectionUsesNearestPathStation();
            TessellatedBulgeMapsAcrossSegments();
            InvalidPathsAndIntervalsFailClosed();
        }

        private static void StraightPathPreservesSingleFrame()
        {
            var path = new[] { new Point2(0d, 0d), new Point2(6d, 0d) };
            var frame = new CurtainWallRect(1d, 0.8d, 2d, 0.05d);
            var plan = CurtainPathFramePlanner.Plan(path, new[] { frame });
            Equal(1, plan.Pieces.Count);
            Equal(1, plan.PathSegmentCount);
            Near(6d, plan.PathLengthM);
            var piece = plan.Pieces[0];
            Near(2d, piece.WidthM);
            Near(2d, piece.CenterX_M);
            Near(0d, piece.CenterY_M);
            Near(0d, piece.AngleRadians);
            Near(0.8d, piece.Z_M);
            Near(0.05d, piece.HeightM);
        }

        private static void BentPathSplitsFrameAtCorner()
        {
            var path = new[] { new Point2(0d, 0d), new Point2(3d, 0d), new Point2(3d, 4d) };
            var frame = new CurtainWallRect(0d, 1.5d, 7d, 0.05d);
            var plan = CurtainPathFramePlanner.Plan(path, new[] { frame });
            Equal(2, plan.Pieces.Count);
            Near(3d, plan.Pieces[0].WidthM);
            Near(4d, plan.Pieces[1].WidthM);
            Near(1.5d, plan.Pieces[0].CenterX_M);
            Near(0d, plan.Pieces[0].CenterY_M);
            Near(3d, plan.Pieces[1].CenterX_M);
            Near(2d, plan.Pieces[1].CenterY_M);
            Near(Math.PI / 2d, plan.Pieces[1].AngleRadians);
        }

        private static void SmallFrameCrossingCornerSplitsDeterministically()
        {
            var path = new[] { new Point2(0d, 0d), new Point2(3d, 0d), new Point2(3d, 3d) };
            var frame = new CurtainWallRect(2.95d, 0d, 0.10d, 3.6d);
            var plan = CurtainPathFramePlanner.Plan(path, new[] { frame });
            Equal(2, plan.Pieces.Count);
            Near(0.05d, plan.Pieces[0].WidthM);
            Near(0.05d, plan.Pieces[1].WidthM);
            Equal(0, plan.Pieces[0].PathSegmentIndex);
            Equal(1, plan.Pieces[1].PathSegmentIndex);
            Equal(0, plan.Pieces[0].SourceFrameIndex);
            Equal(0, plan.Pieces[1].SourceFrameIndex);
        }

        private static void ProjectionUsesNearestPathStation()
        {
            var path = new[] { new Point2(0d, 0d), new Point2(3d, 0d), new Point2(3d, 4d) };
            var projection = CurtainPathFramePlanner.ProjectPoint(path, new Point2(3.2d, 2d));
            Equal(1, projection.PathSegmentIndex);
            Near(5d, projection.StationM);
            Near(0.2d, projection.DistanceM);
            Near(3d, projection.Point.X);
            Near(2d, projection.Point.Y);
        }

        private static void TessellatedBulgeMapsAcrossSegments()
        {
            var path = BulgeArcTessellator.Tessellate(new Point2(0d, 0d), new Point2(4d, 0d), 0.5d, 0.01d);
            True(path.Count > 2);
            var length = CurtainPathFramePlanner.Length(path);
            var frame = new CurtainWallRect(0d, 1d, length, 0.05d);
            var plan = CurtainPathFramePlanner.Plan(path, new[] { frame });
            True(plan.Pieces.Count > 1);
            Near(length, plan.Pieces.Sum(x => x.WidthM), 1e-8d);
            Near(length, plan.PathLengthM, 1e-8d);
        }

        private static void InvalidPathsAndIntervalsFailClosed()
        {
            Throws<ArgumentException>(() => CurtainPathFramePlanner.Plan(new[] { new Point2(0d, 0d) }, Array.Empty<CurtainWallRect>()));
            Throws<InvalidOperationException>(() => CurtainPathFramePlanner.Plan(
                new[] { new Point2(0d, 0d), new Point2(0d, 0d) },
                new[] { new CurtainWallRect(0d, 0d, 0.1d, 1d) }));
            Throws<InvalidOperationException>(() => CurtainPathFramePlanner.Plan(
                new[] { new Point2(0d, 0d), new Point2(1d, 0d) },
                new[] { new CurtainWallRect(0.9d, 0d, 0.2d, 1d) }));
        }

        private static void Near(double expected, double actual, double tolerance = 1e-9d)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new InvalidOperationException("Curtain path frame smoke expected " + expected + " but got " + actual + ".");
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual)
                throw new InvalidOperationException("Curtain path frame smoke expected " + expected + " but got " + actual + ".");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new InvalidOperationException("Curtain path frame smoke assertion failed.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
