using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineLengthCompensatedSumSmoke
    {
        internal static void Run()
        {
            const double largeSegment = 9007199254740992d;
            const double expectedLength = 9007199254740994d;
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(largeSegment, 0d),
                new Point2(largeSegment, 1d),
                new Point2(largeSegment, 2d)
            };

            var actualLength = PolylineMetrics.Length(points, closed: false);
            if (actualLength != expectedLength)
                throw new Exception("Expected compensated polyline length " + expectedLength + " but got " + actualLength + ".");
        }
    }
}
