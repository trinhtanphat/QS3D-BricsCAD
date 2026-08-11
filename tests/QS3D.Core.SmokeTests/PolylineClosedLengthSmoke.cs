using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineClosedLengthSmoke
    {
        public static void Run()
        {
            TwoVertexOpenAndClosedLengthsDifferByClosingSegment();
            MultiVertexClosedLengthStillAddsOneClosingSegment();
        }

        private static void TwoVertexOpenAndClosedLengthsDifferByClosingSegment()
        {
            var points = new[] { new Point2(0d, 0d), new Point2(3d, 4d) };
            Near(5d, PolylineMetrics.Length(points, false));
            Near(10d, PolylineMetrics.Length(points, true));
        }

        private static void MultiVertexClosedLengthStillAddsOneClosingSegment()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(3d, 0d),
                new Point2(3d, 4d)
            };
            Near(7d, PolylineMetrics.Length(points, false));
            Near(12d, PolylineMetrics.Length(points, true));
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }

    internal static class PolylineClosedLengthSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PolylineClosedLengthSmoke.Run();
        }
    }
}
