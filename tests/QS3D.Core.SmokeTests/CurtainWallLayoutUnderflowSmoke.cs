using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests;

internal static class CurtainWallLayoutUnderflowSmoke
{
    public static void Run()
    {
        var ordinary = CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
        {
            LengthM = 6d,
            HeightM = 3d,
            MaxPanelWidthM = 2d,
            MaxPanelHeightM = 1.5d,
            PerimeterFrameWidthM = 0.05d,
            MullionWidthM = 0.1d,
            TransomWidthM = 0.1d
        });

        if (ordinary.Columns != 3 || ordinary.Rows != 2 || ordinary.PanelCount != 6)
            throw new InvalidOperationException("Ordinary curtain-wall layout changed unexpectedly.");
        if (!(ordinary.MinimumClearPanelWidthM > 0d) || !(ordinary.MinimumClearPanelHeightM > 0d))
            throw new InvalidOperationException("Ordinary curtain-wall clear dimensions must remain positive.");

        var zeroInternalFrames = CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
        {
            LengthM = 6d,
            HeightM = 3d,
            MaxPanelWidthM = 2d,
            MaxPanelHeightM = 1.5d,
            PerimeterFrameWidthM = 0.05d,
            MullionWidthM = 0d,
            TransomWidthM = 0d
        });
        if (zeroInternalFrames.PanelCount != 6)
            throw new InvalidOperationException("Legitimate zero internal-frame widths must remain supported.");

        AssertThrows<InvalidOperationException>(() => CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
        {
            LengthM = 6d,
            HeightM = 3d,
            MaxPanelWidthM = 2d,
            MaxPanelHeightM = 1.5d,
            PerimeterFrameWidthM = 0.05d,
            MullionWidthM = double.Epsilon,
            TransomWidthM = 0.1d
        }), "A positive mullion width that underflows during half-width division must fail closed.");

        AssertThrows<InvalidOperationException>(() => CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
        {
            LengthM = 6d,
            HeightM = 3d,
            MaxPanelWidthM = 2d,
            MaxPanelHeightM = 1.5d,
            PerimeterFrameWidthM = 0.05d,
            MullionWidthM = 0.1d,
            TransomWidthM = double.Epsilon
        }), "A positive transom width that underflows during half-width division must fail closed.");
    }

    private static void AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
