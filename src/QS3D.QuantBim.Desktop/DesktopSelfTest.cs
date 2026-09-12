using System;
using System.Linq;

namespace QS3D.QuantBim.Desktop;

internal static class DesktopSelfTest
{
    public static void Run()
    {
        var resolver = new SemanticProxyGeometryResolver();
        var first = resolver.Resolve("#42");
        var second = resolver.Resolve("#42");
        if (first.Vertices.Count != 8 || first.TriangleIndices.Count != 36)
            throw new InvalidOperationException("Proxy geometry must be a closed triangulated box.");
        if (!first.Vertices.Select(v => (v.X, v.Y, v.Z)).SequenceEqual(second.Vertices.Select(v => (v.X, v.Y, v.Z))))
            throw new InvalidOperationException("Proxy geometry must be deterministic for the same IFC geometry reference.");
        if (first.TriangleIndices.Any(i => i < 0 || i >= first.Vertices.Count))
            throw new InvalidOperationException("Proxy mesh contains an invalid triangle index.");
        Console.WriteLine("QS3D QuantBIM desktop self-test PASS");
    }
}
