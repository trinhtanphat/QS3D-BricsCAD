using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainFrameOpeningPositiveSpanSmoke
    {
        private const double TinySpan = 1d / 34359738368d; // 2^-35, positive and below the historical 1e-10 epsilon.

        internal static void Run()
        {
            TinyFullCoverRemovesFrame();
            TinyPositiveResidualIsPreserved();
            TouchingOpeningRemainsNonOverlapping();
        }

        private static void TinyFullCoverRemovesFrame()
        {
            var frame = new CurtainWallRect(0d, 0d, TinySpan, 1d);
            var opening = new CurtainOpeningRect(0d, 0d, TinySpan, 1d);

            var result = CurtainFrameOpeningPlanner.Interrupt(
                new[] { frame },
                new[] { opening });

            Equal(0, result.Count, "A fully covered positive representable frame must not survive solely because its width is below a fixed epsilon.");
        }

        private static void TinyPositiveResidualIsPreserved()
        {
            var frameWidth = TinySpan * 8d;
            var frame = new CurtainWallRect(0d, 0d, frameWidth, 1d);
            var opening = new CurtainOpeningRect(TinySpan, 0d, frameWidth - TinySpan, 1d);

            var result = CurtainFrameOpeningPlanner.Interrupt(
                new[] { frame },
                new[] { opening });

            Equal(1, result.Count, "A positive representable residual strip below the former epsilon must be preserved.");
            Equal(0d, result[0].X_M, "Tiny residual X changed.");
            Equal(0d, result[0].Z_M, "Tiny residual Z changed.");
            Equal(TinySpan, result[0].WidthM, "Tiny residual width was rounded or discarded.");
            Equal(1d, result[0].HeightM, "Tiny residual height changed.");
        }

        private static void TouchingOpeningRemainsNonOverlapping()
        {
            var frame = new CurtainWallRect(0d, 0d, 1d, 1d);
            var opening = new CurtainOpeningRect(1d, 0d, TinySpan, 1d);

            var result = CurtainFrameOpeningPlanner.Interrupt(
                new[] { frame },
                new[] { opening });

            Equal(1, result.Count, "A merely touching opening must remain non-overlapping.");
            Equal(1d, result[0].WidthM, "Touching-opening control changed frame width.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class CurtainFrameOpeningPositiveSpanRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CurtainFrameOpeningPositiveSpanSmoke.Run();
        }
    }
}
