using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests;

internal static class WallFootprintPerimeterPrecisionSmoke
{
    [ModuleInitializer]
    internal static void Register()
    {
        Run();
    }

    private static void Run()
    {
        var ordinaryCenterline = new[]
        {
            new Point2(0d, 0d),
            new Point2(6d, 0d)
        };
        var ordinary = new WallFootprintEngine().Build(ordinaryCenterline, 2d);
        if (ordinary.Perimeter != 16d)
            throw new InvalidOperationException("Ordinary wall footprint perimeter changed unexpectedly.");

        var highDynamicRangeCenterline = new[]
        {
            new Point2(0d, 0d),
            new Point2(1e16d, 0d)
        };
        const double expectedPerimeter = 20000000000000004d;

        var footprint = new WallFootprintEngine().Build(highDynamicRangeCenterline, 2d);
        if (footprint.Polygon.Count != 4)
            throw new InvalidOperationException("Straight wall footprint must remain rectangular.");
        if (footprint.Perimeter != expectedPerimeter)
            throw new InvalidOperationException("Wall footprint perimeter lost representable short-edge contributions.");

        var profile = WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
        {
            Centerline = highDynamicRangeCenterline,
            ThicknessM = 2d,
            HeightM = 1d,
            Mode = WallPierProfileMode.Rectangular
        });
        if (profile.FootprintPerimeterM != expectedPerimeter)
            throw new InvalidOperationException("Wall-pier path perimeter lost representable short-edge contributions.");
        if (profile.LateralAreaM2 != expectedPerimeter)
            throw new InvalidOperationException("Wall-pier lateral area must preserve the compensated perimeter at unit height.");
    }
}
