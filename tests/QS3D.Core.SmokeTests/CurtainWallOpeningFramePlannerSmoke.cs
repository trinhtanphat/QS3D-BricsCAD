using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallOpeningFramePlannerSmoke
    {
        public static void Run()
        {
            FiniteOverflowingClearanceFailsClosed();
            NonFiniteClearanceStillFailsAtInputBoundary();
            OrdinaryClearanceProducesFinitePieces();
        }

        private static void FiniteOverflowingClearanceFailsClosed()
        {
            var frames = new[] { new CurtainWallRect(0d, 0d, 1d, 1d) };
            var openings = new[]
            {
                new CurtainWallOpeningRect { X_M = 0.25d, Z_M = 0.25d, WidthM = 0.5d, HeightM = 0.5d }
            };

            ExpectArgumentOutOfRange(
                () => CurtainWallOpeningFramePlanner.Plan(frames, openings, double.MaxValue),
                "Finite clearance that overflows expanded-opening geometry must fail closed.",
                "expandedOpening[0]");
        }

        private static void NonFiniteClearanceStillFailsAtInputBoundary()
        {
            var frames = new[] { new CurtainWallRect(0d, 0d, 1d, 1d) };
            var openings = new[]
            {
                new CurtainWallOpeningRect { X_M = 0.25d, Z_M = 0.25d, WidthM = 0.5d, HeightM = 0.5d }
            };

            ExpectArgumentOutOfRange(
                () => CurtainWallOpeningFramePlanner.Plan(frames, openings, double.NaN),
                "NaN clearance must fail at the input boundary.",
                "clearanceM");
            ExpectArgumentOutOfRange(
                () => CurtainWallOpeningFramePlanner.Plan(frames, openings, double.PositiveInfinity),
                "Positive-infinity clearance must fail at the input boundary.",
                "clearanceM");
            ExpectArgumentOutOfRange(
                () => CurtainWallOpeningFramePlanner.Plan(frames, openings, double.NegativeInfinity),
                "Negative-infinity clearance must fail at the input boundary.",
                "clearanceM");
        }

        private static void OrdinaryClearanceProducesFinitePieces()
        {
            var plan = CurtainWallOpeningFramePlanner.Plan(
                new[] { new CurtainWallRect(0d, 0d, 1d, 1d) },
                new[]
                {
                    new CurtainWallOpeningRect { X_M = 0.25d, Z_M = 0.25d, WidthM = 0.5d, HeightM = 0.5d }
                },
                0.1d);

            if (plan.InterruptedFrameCount != 1)
                throw new Exception("Ordinary opening clearance should interrupt the source frame exactly once.");
            if (plan.Pieces.Count != 4)
                throw new Exception("Ordinary centered opening should leave four deterministic frame pieces.");

            foreach (var piece in plan.Pieces)
            {
                RequireFinite(piece.X_M, "piece.X_M");
                RequireFinite(piece.Z_M, "piece.Z_M");
                RequireFinite(piece.WidthM, "piece.WidthM");
                RequireFinite(piece.HeightM, "piece.HeightM");
                if (piece.WidthM <= 0d || piece.HeightM <= 0d)
                    throw new Exception("Opening-frame smoke produced a non-positive frame piece.");
            }

            RequireFinite(plan.OriginalFrameAreaM2, nameof(plan.OriginalFrameAreaM2));
            RequireFinite(plan.RemainingFrameAreaM2, nameof(plan.RemainingFrameAreaM2));
            RequireFinite(plan.RemovedFrameAreaM2, nameof(plan.RemovedFrameAreaM2));
        }

        private static void ExpectArgumentOutOfRange(Action action, string message, string expectedParamPrefix)
        {
            try
            {
                action();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if (ex.ParamName == null || !ex.ParamName.StartsWith(expectedParamPrefix, StringComparison.Ordinal))
                    throw new Exception(message + " Unexpected parameter: " + (ex.ParamName ?? "<null>"));
                return;
            }

            throw new Exception(message);
        }

        private static void RequireFinite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new Exception(label + " must be finite.");
        }
    }
}
