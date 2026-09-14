using System;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

internal static class QsQuantBimNavigationSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var navigation = new QuantBimStandaloneNavigation();
        var frame = new IfcViewportFrame(
            new IfcViewportBounds(0.0, 0.0, 0.0, 10.0, 20.0, 30.0),
            20.0,
            60.0,
            20.0,
            100.0);

        var focused = navigation.Focus(frame);
        Require(Near(focused.TargetX, 5.0) && Near(focused.TargetY, 10.0) && Near(focused.TargetZ, 15.0), "focus target");
        Require(Near(focused.Distance, 60.0), "focus distance");
        Require(focused.NearPlane > 0.0 && focused.FarPlane > focused.NearPlane, "focus clipping");

        var orbited = navigation.Orbit(focused, 90.0, 10.0);
        Require(Near(orbited.Distance, focused.Distance), "orbit preserves distance");
        Require(Near(orbited.TargetX, focused.TargetX) && Near(orbited.TargetY, focused.TargetY) && Near(orbited.TargetZ, focused.TargetZ), "orbit preserves focus");

        var panned = navigation.Pan(orbited, 4.0, -2.0);
        Require(Near(panned.Distance, orbited.Distance), "pan preserves distance");
        Require(!Near(panned.TargetX, orbited.TargetX) || !Near(panned.TargetY, orbited.TargetY) || !Near(panned.TargetZ, orbited.TargetZ), "pan moves target");

        var zoomedIn = navigation.Dolly(panned, 0.5);
        Require(zoomedIn.Distance < panned.Distance, "dolly zoom in");
        var clamped = navigation.Dolly(zoomedIn, 1e-12);
        Require(clamped.Distance >= clamped.SceneRadius * 1.05 - 1e-9, "dolly minimum clamp");

        var pole = navigation.Orbit(focused, 0.0, 10000.0);
        Require(pole.UpZ > 0.0, "orbit pole clamp");

        ExpectFailure(() => navigation.Orbit(focused, double.NaN, 0.0), "non-finite orbit");
        ExpectFailure(() => navigation.Dolly(focused, 0.0), "non-positive dolly");
        ExpectFailure(() => new IfcCameraNavigationState(0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0.1, 10), "degenerate view");
        ExpectFailure(() => new IfcCameraNavigationState(0, -10, 0, 0, 0, 0, 0, 1, 0, 1, 0.1, 10), "parallel up");
    }

    private static bool Near(double left, double right)
    {
        return Math.Abs(left - right) <= 1e-8 * Math.Max(1.0, Math.Max(Math.Abs(left), Math.Abs(right)));
    }

    private static void Require(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("QuantBIM navigation smoke failed: " + name + ".");
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
        throw new InvalidOperationException("QuantBIM navigation smoke expected failure: " + name + ".");
    }
}
