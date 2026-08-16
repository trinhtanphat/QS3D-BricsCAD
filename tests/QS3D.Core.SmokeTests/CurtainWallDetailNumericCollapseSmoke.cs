using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests;

internal static class CurtainWallDetailNumericCollapseSmoke
{
    internal static void Run()
    {
        GeneratedRightFrameCollapseFailsClosed();
        GeneratedTopFrameCollapseFailsClosed();
        InternalVerticalFrameHalfWidthPlacementCollapseFailsClosed();
        InternalHorizontalFrameHalfHeightPlacementCollapseFailsClosed();
        RectangleAreaUnderflowFailsClosed();
        PanelAreaUnderflowFailsClosed();
        OrdinaryDetailRemainsStable();
    }

    private static void GeneratedRightFrameCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallDetailPlanner.Plan(Input(1e16d, 10d, 1e16d, 10d, 1d, 0d, 0d)));
        Equal("curtain vertical frame right perimeter placement lost a positive deduction at floating-point precision.", error.Message);
    }

    private static void GeneratedTopFrameCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallDetailPlanner.Plan(Input(10d, 1e16d, 10d, 1e16d, 1d, 0d, 0d)));
        Equal("curtain horizontal frame top perimeter placement lost a positive deduction at floating-point precision.", error.Message);
    }

    private static void InternalVerticalFrameHalfWidthPlacementCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallDetailPlanner.Plan(Input(8e15d, 10d, 3e15d, 10d, 1d, .75d, 0d)));
        Equal("curtain vertical frame half-width placement lost a positive deduction at floating-point precision.", error.Message);
    }

    private static void InternalHorizontalFrameHalfHeightPlacementCollapseFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallDetailPlanner.Plan(Input(10d, 8e15d, 10d, 3e15d, 1d, 0d, .75d)));
        Equal("curtain horizontal frame half-height placement lost a positive deduction at floating-point precision.", error.Message);
    }

    private static void RectangleAreaUnderflowFailsClosed()
    {
        var error = Capture<OverflowException>(() => _ = new CurtainWallRect(0d, 0d, 1e-200d, 1e-200d).AreaM2);
        Equal("Curtain rectangle area underflowed to zero.", error.Message);
    }

    private static void PanelAreaUnderflowFailsClosed()
    {
        var error = Capture<OverflowException>(() => CurtainWallDetailPlanner.Plan(Input(1e-160d, 1e-160d, 1e-162d, 1e-162d, 1e-164d, 1e-164d, 1e-164d)));
        Equal("curtain detail panel area underflowed to zero.", error.Message);
    }

    private static void OrdinaryDetailRemainsStable()
    {
        var detail = CurtainWallDetailPlanner.Plan(Input(4d, 3d, 2d, 1.5d, 0.1d, 0.05d, 0.05d));
        Equal(4, detail.Panels.Count);
        Equal(3, detail.VerticalFrames.Count);
        Equal(3, detail.HorizontalFrames.Count);
        Equal(10, detail.DetailSolidCount);
    }

    private static CurtainWallLayoutInput Input(
        double length,
        double height,
        double maxPanelWidth,
        double maxPanelHeight,
        double perimeter,
        double mullion,
        double transom) => new()
    {
        LengthM = length,
        HeightM = height,
        MaxPanelWidthM = maxPanelWidth,
        MaxPanelHeightM = maxPanelHeight,
        PerimeterFrameWidthM = perimeter,
        MullionWidthM = mullion,
        TransomWidthM = transom
    };

    private static T Capture<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T error) { return error; }
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
}
