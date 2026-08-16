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
        if (!(ordinary.TotalFrameLengthM > ordinary.VerticalFrameLengthM) ||
            !(ordinary.TotalFrameLengthM > ordinary.HorizontalFrameLengthM))
            throw new InvalidOperationException("Ordinary curtain-wall total frame length must retain both positive components.");

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

        const double roundedRatioMaxPanelWidthM = 0.1000000000000001d;
        var roundedIntegerRatio = CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
        {
            LengthM = 0.5000000000000006d,
            HeightM = 1d,
            MaxPanelWidthM = roundedRatioMaxPanelWidthM,
            MaxPanelHeightM = 1d,
            PerimeterFrameWidthM = 0d,
            MullionWidthM = 0d,
            TransomWidthM = 0d
        });
        if (roundedIntegerRatio.Columns != 6)
            throw new InvalidOperationException("A rounded integer division ratio must add a curtain column when five bays exceed MaxPanelWidthM.");
        if (roundedIntegerRatio.BayWidthM > roundedRatioMaxPanelWidthM)
            throw new InvalidOperationException("A rounded integer division ratio must not produce a bay wider than MaxPanelWidthM.");

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

        AssertThrows<InvalidOperationException>(() => CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
        {
            LengthM = 1e16d,
            HeightM = 1d,
            MaxPanelWidthM = 1e16d,
            MaxPanelHeightM = 1d,
            PerimeterFrameWidthM = 0.5d,
            MullionWidthM = 0d,
            TransomWidthM = 0d
        }), "A positive perimeter-frame deduction that rounds away at large coordinate magnitude must fail closed.");

        AssertThrows<InvalidOperationException>(() => CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
        {
            LengthM = 2e16d,
            HeightM = 1d,
            MaxPanelWidthM = 1e16d,
            MaxPanelHeightM = 1d,
            PerimeterFrameWidthM = 0d,
            MullionWidthM = 0.5d,
            TransomWidthM = 0d
        }), "A positive internal-frame deduction that rounds away at large bay magnitude must fail closed.");

        var frameLengthCollapse = CurtainWallLayoutPlanner.Plan(new CurtainWallLayoutInput
        {
            LengthM = 1e16d,
            HeightM = 1d,
            MaxPanelWidthM = 1e16d,
            MaxPanelHeightM = 1d,
            PerimeterFrameWidthM = 0d,
            MullionWidthM = 0d,
            TransomWidthM = 0d
        });
        AssertThrows<OverflowException>(() => _ = frameLengthCollapse.TotalFrameLengthM,
            "TotalFrameLengthM must fail closed when floating-point addition loses a positive frame-length component.");
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
