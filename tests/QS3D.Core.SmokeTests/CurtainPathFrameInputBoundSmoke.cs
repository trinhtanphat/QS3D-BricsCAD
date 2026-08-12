using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainPathFrameInputBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OversizedFrameInputFailsClosed();
            OrdinaryFrameMappingRemainsStable();
        }

        private static void OversizedFrameInputFailsClosed()
        {
            var path = new[] { new Point2(0d, 0d), new Point2(10d, 0d) };
            var frame = new CurtainWallRect(1d, 0d, 1d, 2d);
            var frames = Enumerable.Repeat(frame, 20001).ToArray();
            try
            {
                CurtainPathFramePlanner.Plan(path, frames);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("cannot exceed 20000", StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("Curtain path frame input bound failed for the wrong reason.", ex);
            }
            throw new InvalidOperationException("Curtain path frame planner accepted more input frames than its native-piece budget can support.");
        }

        private static void OrdinaryFrameMappingRemainsStable()
        {
            var plan = CurtainPathFramePlanner.Plan(
                new[] { new Point2(0d, 0d), new Point2(10d, 0d) },
                new[] { new CurtainWallRect(1d, 0d, 2d, 3d) });

            if (plan.PathSegmentCount != 1 || plan.SourceFrameCount != 1 || plan.Pieces.Count != 1)
                throw new InvalidOperationException("Ordinary curtain path frame mapping count changed while adding input preflight.");
            var piece = plan.Pieces[0];
            if (piece.SourceFrameIndex != 0 || piece.PathSegmentIndex != 0 ||
                Math.Abs(piece.StationStartM - 1d) > 1e-12d || Math.Abs(piece.StationEndM - 3d) > 1e-12d ||
                Math.Abs(piece.CenterX_M - 2d) > 1e-12d || Math.Abs(piece.CenterY_M) > 1e-12d ||
                Math.Abs(piece.Z_M) > 1e-12d || Math.Abs(piece.HeightM - 3d) > 1e-12d)
                throw new InvalidOperationException("Ordinary curtain path frame geometry changed while adding input preflight.");
        }
    }
}
