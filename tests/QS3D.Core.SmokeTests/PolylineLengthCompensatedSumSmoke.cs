using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolylineLengthCompensatedSumSmoke
    {
        internal static void Run()
        {
            RecoversRepresentableCompensation();
            RejectsSwallowedFinalCompensation();
        }

        private static void RecoversRepresentableCompensation()
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

        private static void RejectsSwallowedFinalCompensation()
        {
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(1e300, 0d),
                new Point2(1e300, 1d)
            };

            try
            {
                PolylineMetrics.Length(points, closed: false);
                throw new Exception("Expected polyline length to fail closed when final compensation is swallowed by rounding.");
            }
            catch (OverflowException ex)
            {
                if (ex.Message.IndexOf("compensated segment", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Expected swallowed-compensation failure to identify the lost compensated segment.");
            }
        }
    }
}