using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests;

internal static class CurtainWallDetailNumericCollapseSmoke
{
    internal static void Run()
    {
        RectangleAreaUnderflowFailsClosed();
        ZeroInternalFramesRemainValidWithoutDegenerateSolids();
        AllZeroFramesProducePanelOnlyDetail();
        MixedZeroMullionPreservesTransomSolids();
        OrdinaryDetailRemainsStable();
    }

    private static void RectangleAreaUnderflowFailsClosed()
    {
        var error = Capture<OverflowException>(() => _ = new CurtainWallRect(0d, 0d, 1e-200d, 1e-200d).AreaM2);
        Equal("Curtain rectangle area underflowed to zero.", error.Message);
    }

    private static void ZeroInternalFramesRemainValidWithoutDegenerateSolids()
    {
        var detail = CurtainWallDetailPlanner.Plan(Input(4d, 3d, 2d, 1.5d, .1d, 0d, 0d));
        Equal(4, detail.Panels.Count); Equal(2, detail.VerticalFrames.Count); Equal(2, detail.HorizontalFrames.Count); Equal(8, detail.DetailSolidCount);
        AllPositive(detail.VerticalFrames); AllPositive(detail.HorizontalFrames);
    }

    private static void AllZeroFramesProducePanelOnlyDetail()
    {
        var detail = CurtainWallDetailPlanner.Plan(Input(4d, 3d, 2d, 1.5d, 0d, 0d, 0d));
        Equal(4, detail.Panels.Count); Equal(0, detail.VerticalFrames.Count); Equal(0, detail.HorizontalFrames.Count); Equal(4, detail.DetailSolidCount);
    }

    private static void MixedZeroMullionPreservesTransomSolids()
    {
        var detail = CurtainWallDetailPlanner.Plan(Input(4d, 3d, 2d, 1.5d, .1d, 0d, .05d));
        Equal(4, detail.Panels.Count); Equal(2, detail.VerticalFrames.Count); Equal(3, detail.HorizontalFrames.Count); Equal(9, detail.DetailSolidCount);
        AllPositive(detail.VerticalFrames); AllPositive(detail.HorizontalFrames);
    }

    private static void OrdinaryDetailRemainsStable()
    {
        var detail = CurtainWallDetailPlanner.Plan(Input(4d, 3d, 2d, 1.5d, .1d, .05d, .05d));
        Equal(4, detail.Panels.Count); Equal(3, detail.VerticalFrames.Count); Equal(3, detail.HorizontalFrames.Count); Equal(10, detail.DetailSolidCount);
    }

    private static void AllPositive(System.Collections.Generic.IReadOnlyList<CurtainWallRect> frames)
    {
        foreach (var frame in frames) if (!(frame.WidthM > 0d) || !(frame.HeightM > 0d)) throw new InvalidOperationException("Degenerate curtain frame emitted.");
    }

    private static CurtainWallLayoutInput Input(double length, double height, double maxPanelWidth, double maxPanelHeight, double perimeter, double mullion, double transom) => new()
    {
        LengthM = length, HeightM = height, MaxPanelWidthM = maxPanelWidth, MaxPanelHeightM = maxPanelHeight,
        PerimeterFrameWidthM = perimeter, MullionWidthM = mullion, TransomWidthM = transom
    };

    private static T Capture<T>(Action action) where T : Exception { try { action(); } catch (T error) { return error; } throw new InvalidOperationException("Expected " + typeof(T).Name + "."); }
    private static void Equal(string expected, string actual) { if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'."); }
    private static void Equal(int expected, int actual) { if (expected != actual) throw new InvalidOperationException("Expected " + expected + " but got " + actual + "."); }
}
