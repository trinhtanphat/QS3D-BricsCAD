using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarShapePathAliasingSmoke
    {
        public static void Run()
        {
            ConstructorOwnsPointSnapshot();
            BuilderPathRemainsUnchanged();
        }

        private static void ConstructorOwnsPointSnapshot()
        {
            var source = new List<RebarShapePoint>
            {
                new RebarShapePoint(0d, 0d),
                new RebarShapePoint(2d, 0d)
            };
            var path = new RebarShapePath("00", source);

            source[0] = new RebarShapePoint(99d, 99d);
            source.Clear();

            Equal(2, path.Points.Count);
            Near(0d, path.Points[0].X);
            Near(0d, path.Points[0].Y);
            Near(2d, path.Points[1].X);
            Near(0d, path.Points[1].Y);
        }

        private static void BuilderPathRemainsUnchanged()
        {
            var path = RebarShapePathBuilder.Build("L", 3d, "1;2");
            Equal(3, path.Points.Count);
            Near(0d, path.Points[0].X);
            Near(0d, path.Points[0].Y);
            Near(1d, path.Points[1].X);
            Near(0d, path.Points[1].Y);
            Near(1d, path.Points[2].X);
            Near(2d, path.Points[2].Y);
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual)
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }

    internal static class RebarShapePathAliasingSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RebarShapePathAliasingSmoke.Run();
        }
    }
}
