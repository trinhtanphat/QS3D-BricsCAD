using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests;

internal static class CurtainWallOpeningFrameCoordinateCollapseSmoke
{
    internal static void Run()
    {
        OpeningWidthCollapseFailsClosed();
        OpeningHeightCollapseFailsClosed();
        PositiveClearanceCollapseFailsClosed();
        FrameWidthCollapseFailsClosed();
        FrameHeightCollapseFailsClosed();
        OrdinaryInterruptionRemainsStable();
    }

    private static void OpeningWidthCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallOpeningFramePlanner.Plan(
            new[] { new CurtainWallRect(0d, 0d, 10d, 10d) },
            new[] { Opening(1e16d, 0d, 1d, 1d) }));
        Equal("opening[0] width is below the representable coordinate resolution.", error.Message);
    }

    private static void OpeningHeightCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallOpeningFramePlanner.Plan(
            new[] { new CurtainWallRect(0d, 0d, 10d, 10d) },
            new[] { Opening(0d, 1e16d, 1d, 1d) }));
        Equal("opening[0] height is below the representable coordinate resolution.", error.Message);
    }

    private static void PositiveClearanceCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallOpeningFramePlanner.Plan(
            new[] { new CurtainWallRect(1e16d, 0d, 8d, 8d) },
            new[] { Opening(1e16d, 1d, 4d, 4d) },
            1d));
        Equal("opening[0] horizontal clearance is below the representable coordinate resolution.", error.Message);
    }

    private static void FrameWidthCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallOpeningFramePlanner.Plan(
            new[] { new CurtainWallRect(1e16d, 0d, 1d, 1d) },
            Array.Empty<CurtainWallOpeningRect>()));
        Equal("frame[0] width is below the representable coordinate resolution.", error.Message);
    }

    private static void FrameHeightCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallOpeningFramePlanner.Plan(
            new[] { new CurtainWallRect(0d, 1e16d, 1d, 1d) },
            Array.Empty<CurtainWallOpeningRect>()));
        Equal("frame[0] height is below the representable coordinate resolution.", error.Message);
    }

    private static void OrdinaryInterruptionRemainsStable()
    {
        var plan = CurtainWallOpeningFramePlanner.Plan(
            new[] { new CurtainWallRect(0d, 0d, 10d, 10d) },
            new[] { Opening(4d, 4d, 2d, 2d) });

        Equal(4, plan.Pieces.Count);
        Equal(1, plan.InterruptedFrameCount);
        Near(100d, plan.OriginalFrameAreaM2);
        Near(96d, plan.RemainingFrameAreaM2);
        Near(4d, plan.RemovedFrameAreaM2);
    }

    private static CurtainWallOpeningRect Opening(double x, double z, double width, double height) => new()
    {
        X_M = x,
        Z_M = z,
        WidthM = width,
        HeightM = height
    };

    private static T Capture<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T error)
        {
            return error;
        }

        throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
    }

    private static void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'.");
    }

    private static void Equal(int expected, int actual)
    {
        if (expected != actual)
            throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
    }

    private static void Near(double expected, double actual)
    {
        var tolerance = Math.Max(1e-12d, Math.Abs(expected) * 1e-12d);
        if (Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
    }
}
