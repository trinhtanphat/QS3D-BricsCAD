using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainOpeningPiecesReadonlySmoke
    {
        internal static void Run()
        {
            FramePiecesAreReadOnly();
            PanelPiecesAreReadOnly();
        }

        private static void FramePiecesAreReadOnly()
        {
            var frames = new[] { new CurtainWallRect(0d, 0d, 2d, 3d) };
            var plan = CurtainWallOpeningFramePlanner.Plan(frames, Array.Empty<CurtainWallOpeningRect>());

            Equal(1, plan.Pieces.Count, "frame count");
            Equal(0, plan.Pieces[0].SourceFrameIndex, "frame source index");
            Equal(2d, plan.Pieces[0].WidthM, "frame width");
            Equal(3d, plan.Pieces[0].HeightM, "frame height");
            AssertMutationRejected(plan.Pieces, new CurtainWallFramePiece(), "frame");
        }

        private static void PanelPiecesAreReadOnly()
        {
            var panels = new[] { new CurtainWallRect(0d, 0d, 2d, 3d) };
            var plan = CurtainWallOpeningPanelPlanner.Plan(panels, Array.Empty<CurtainWallOpeningRect>());

            Equal(1, plan.Pieces.Count, "panel count");
            Equal(1, plan.SourcePanelCount, "panel source count");
            Equal(0, plan.Pieces[0].SourcePanelIndex, "panel source index");
            Equal(2d, plan.Pieces[0].WidthM, "panel width");
            Equal(3d, plan.Pieces[0].HeightM, "panel height");
            AssertMutationRejected(plan.Pieces, new CurtainWallPanelPiece(), "panel");
        }

        private static void AssertMutationRejected<T>(IReadOnlyList<T> values, T replacement, string label)
        {
            if (values is T[])
                throw new Exception("CurtainOpeningPiecesReadonlySmoke " + label + ": result must not expose a mutable array.");
            if (!(values is IList<T> list))
                throw new Exception("CurtainOpeningPiecesReadonlySmoke " + label + ": expected IList compatibility for mutation guard verification.");

            try
            {
                list[0] = replacement;
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new Exception("CurtainOpeningPiecesReadonlySmoke " + label + ": index mutation was accepted.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception("CurtainOpeningPiecesReadonlySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class CurtainOpeningPiecesReadonlySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainOpeningPiecesReadonlySmoke.Run();
    }
}
