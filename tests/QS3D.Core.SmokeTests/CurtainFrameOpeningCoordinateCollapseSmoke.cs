using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests;

internal static class CurtainFrameOpeningCoordinateCollapseSmoke
{
    public static void Run()
    {
        OpeningWidthCollapseFailsClosed();
        OpeningHeightCollapseFailsClosed();
        OpeningClearanceCollapseFailsClosed();
        FrameWidthCollapseFailsClosed();
        FrameHeightCollapseFailsClosed();
        OrdinaryOpeningStillInterrupts();
    }

    private static void OpeningWidthCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() =>
            new CurtainOpeningRect(1e16d, 0d, 1d, 1d));
        Equal("Curtain opening width is below the representable coordinate resolution.", error.Message);
    }

    private static void OpeningHeightCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() =>
            new CurtainOpeningRect(0d, 1e16d, 1d, 1d));
        Equal("Curtain opening height is below the representable coordinate resolution.", error.Message);
    }

    private static void OpeningClearanceCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() =>
            new CurtainOpeningRect(1e16d, 0d, 4d, 4d, 1d));
        Equal("Curtain opening horizontal clearance is below the representable coordinate resolution.", error.Message);
    }

    private static void FrameWidthCollapseFailsClosed()
    {
        var error = Capture<InvalidOperationException>(() =>
            CurtainFrameOpeningPlanner.Interrupt(
                new[] { new CurtainWallRect(1e16d, 0d, 1d, 1d) },
                Array.Empty<CurtainOpeningRect>()));
        Equal("Curtain frame rectangle width is below the representable coordinate resolution.", error.Message);
    }

    private static void FrameHeightCollapseFailsClosed()
    {
        var error = Capture<InvalidOperationException>(() =>
            CurtainFrameOpeningPlanner.Interrupt(
                new[] { new CurtainWallRect(0d, 1e16d, 1d, 1d) },
                Array.Empty<CurtainOpeningRect>()));
        Equal("Curtain frame rectangle height is below the representable coordinate resolution.", error.Message);
    }

    private static void OrdinaryOpeningStillInterrupts()
    {
        var result = CurtainFrameOpeningPlanner.Interrupt(
            new[] { new CurtainWallRect(0d, 0d, 10d, 10d) },
            new[] { new CurtainOpeningRect(4d, 4d, 2d, 2d) });

        if (result.Count != 4)
            throw new InvalidOperationException("Ordinary curtain opening must still split the frame into four fragments.");

        var area = 0d;
        foreach (var fragment in result) area += fragment.AreaM2;
        Near(96d, area);
    }

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

    private static void Near(double expected, double actual)
    {
        var tolerance = Math.Max(1e-12d, Math.Abs(expected) * 1e-12d);
        if (Math.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
    }
}
