using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests;

internal static class WallPerimeterCompensationOrderSmoke
{
    [ModuleInitializer]
    internal static void Register() => Run();

    private static void Run()
    {
        AssertOrderIndependent(typeof(WallFootprintEngine), "wall footprint perimeter");
        AssertOrderIndependent(typeof(WallPierPathProfilePlanner), "wall-pier path perimeter");
    }

    private static void AssertOrderIndependent(Type owner, string label)
    {
        var add = owner.GetMethod("AddCompensated", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(owner.Name + " must expose its guarded compensated-add helper.");
        var finalize = owner.GetMethod("FinalizeCompensated", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(owner.Name + " must materialize the final compensation.");

        var sum = 0d;
        var compensation = 0d;
        Add(add, ref sum, ref compensation, 1d, label);
        Add(add, ref sum, ref compensation, 1e16d, label);
        Add(add, ref sum, ref compensation, 1d, label);

        var result = (double)(finalize.Invoke(null, new object[] { sum, compensation, label })
            ?? throw new InvalidOperationException(owner.Name + " final compensation returned null."));
        const double expected = 10000000000000002d;
        if (result != expected)
            throw new InvalidOperationException(owner.Name + " lost collectively significant small perimeter contributions when input order was small -> huge -> small.");
    }

    private static void Add(MethodInfo method, ref double sum, ref double compensation, double value, string label)
    {
        var args = new object[] { sum, compensation, value, label };
        method.Invoke(null, args);
        sum = (double)args[0];
        compensation = (double)args[1];
    }
}
