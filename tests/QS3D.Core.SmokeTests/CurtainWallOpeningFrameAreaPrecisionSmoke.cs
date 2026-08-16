using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests;

internal static class CurtainWallOpeningFrameAreaPrecisionSmoke
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        FrameAreaUnderflowFailsClosed();
        OriginalAreaLostPositiveContributionFailsClosed();
        RemainingAreaLostPositiveContributionFailsClosed();
        RemovedAreaCollapseFailsClosed();
        FramePieceAreaUnderflowFailsClosed();
        SubEpsilonUninterruptedFrameRemainsPresent();
        OrdinaryInterruptionRemainsStable();
    }

    private static void FrameAreaUnderflowFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallOpeningFramePlanner.Plan(
            new[] { new CurtainWallRect(0d, 0d, double.Epsilon, double.Epsilon) },
            Array.Empty<CurtainWallOpeningRect>()));
        Equal("frame area underflowed to zero.", error.Message);
    }

    private static void OriginalAreaLostPositiveContributionFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallOpeningFramePlanner.Plan(
            new[]
            {
                new CurtainWallRect(0d, 0d, 1e16d, 1d),
                new CurtainWallRect(0d, 2d, 1d, 1d)
            },
            Array.Empty<CurtainWallOpeningRect>()));
        Equal("total frame area lost a positive contribution at floating-point precision.", error.Message);
    }

    private static void RemainingAreaLostPositiveContributionFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallOpeningFramePlanner.Plan(
            new[]
            {
                new CurtainWallRect(0d, 0d, 1e16d, 1d),
                new CurtainWallRect(0d, 2d, 1e16d, 1d)
            },
            new[] { Opening(1d, 0d, 1e16d, 1d) }));
        Equal("remaining frame area lost a positive contribution at floating-point precision.", error.Message);
    }

    private static void RemovedAreaCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallOpeningFramePlanner.Plan(
            new[] { new CurtainWallRect(0d, 0d, 1e16d, 1d) },
            new[] { Opening(0d, 0d, 1d, 1d) }));
        Equal("Curtain removed frame area was lost at floating-point precision.", error.Message);
    }

    private static void FramePieceAreaUnderflowFailsClosed()
    {
        var piece = new CurtainWallFramePiece { WidthM = double.Epsilon, HeightM = double.Epsilon };
        var error = Capture<OverflowException>(() => _ = piece.AreaM2);
        Equal("Curtain frame piece area underflowed to zero.", error.Message);
    }

    private static void SubEpsilonUninterruptedFrameRemainsPresent()
    {
        const double width = 5e-10d;
        var plan = CurtainWallOpeningFramePlanner.Plan(
            new[] { new CurtainWallRect(0d, 0d, width, 1d) },
            Array.Empty<CurtainWallOpeningRect>());

        Equal(1, plan.Pieces.Count);
        Equal(0, plan.InterruptedFrameCount);
        Near(width, plan.Pieces[0].WidthM);
        Near(width, plan.OriginalFrameAreaM2);
        Near(width, plan.RemainingFrameAreaM2);
        Near(0d, plan.RemovedFrameAreaM2);
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
