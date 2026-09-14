using System;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

internal static class QsQuantBimScreenRaySmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var projector = new QuantBimStandaloneScreenRayProjector();
        var camera = new IfcCameraNavigationState(
            0.0, -10.0, 0.0,
            0.0, 0.0, 0.0,
            0.0, 0.0, 1.0,
            5.0, 0.1, 100.0);

        var center = projector.ProjectPerspective(camera, 800.0, 600.0, 400.0, 300.0, 60.0);
        Require(Near(center.DirectionX, 0.0), "center x");
        Require(center.DirectionY > 0.999999, "center forward");
        Require(Near(center.DirectionZ, 0.0), "center z");
        Require(Near(center.OriginX, camera.PositionX) && Near(center.OriginY, camera.PositionY) && Near(center.OriginZ, camera.PositionZ), "camera origin");

        var topLeft = projector.ProjectPerspective(camera, 800.0, 600.0, 0.0, 0.0, 60.0);
        Require(topLeft.DirectionX < 0.0, "left points left");
        Require(topLeft.DirectionZ > 0.0, "top points up");

        var bottomRight = projector.ProjectPerspective(camera, 800.0, 600.0, 800.0, 600.0, 60.0);
        Require(bottomRight.DirectionX > 0.0, "right points right");
        Require(bottomRight.DirectionZ < 0.0, "bottom points down");

        var narrow = projector.ProjectPerspective(camera, 800.0, 600.0, 800.0, 300.0, 30.0);
        var wide = projector.ProjectPerspective(camera, 800.0, 600.0, 800.0, 300.0, 90.0);
        Require(Math.Abs(wide.DirectionX) > Math.Abs(narrow.DirectionX), "fov widens ray");

        var portrait = projector.ProjectPerspective(camera, 300.0, 600.0, 300.0, 300.0, 60.0);
        Require(Math.Abs(portrait.DirectionX) < Math.Abs(bottomRight.DirectionX), "aspect affects horizontal ray");

        ExpectFailure(() => projector.ProjectPerspective(camera, 0.0, 600.0, 0.0, 0.0, 60.0), "zero viewport");
        ExpectFailure(() => projector.ProjectPerspective(camera, 800.0, 600.0, -0.01, 0.0, 60.0), "outside viewport");
        ExpectFailure(() => projector.ProjectPerspective(camera, 800.0, 600.0, double.NaN, 0.0, 60.0), "non-finite pointer");
        ExpectFailure(() => projector.ProjectPerspective(camera, 800.0, 600.0, 400.0, 300.0, 180.0), "invalid fov");
    }

    private static bool Near(double left, double right)
    {
        return Math.Abs(left - right) <= 1e-8 * Math.Max(1.0, Math.Max(Math.Abs(left), Math.Abs(right)));
    }

    private static void Require(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("QuantBIM screen-ray smoke failed: " + name + ".");
    }

    private static void ExpectFailure(Action action, string name)
    {
        try
        {
            action();
        }
        catch (Exception)
        {
            return;
        }
        throw new InvalidOperationException("QuantBIM screen-ray smoke expected failure: " + name + ".");
    }
}
