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
            RecoversRepresentableAreaCompensation();
            RejectsSwallowedFinalAreaCompensation();
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

        private static void RecoversRepresentableAreaCompensation()
        {
            const double largeCross = 9007199254740992d;
            const double expectedArea = 4503599627370497d;
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(largeCross, 0d),
                new Point2(0d, 1d),
                new Point2(-1d, 0d),
                new Point2(0d, -1d)
            };

            var actualArea = PolylineMetrics.SignedArea(points);
            if (actualArea != expectedArea)
                throw new Exception("Expected compensated polyline area " + expectedArea + " but got " + actualArea + ".");
        }

        private static void RejectsSwallowedFinalAreaCompensation()
        {
            var large = Math.Pow(2d, 500d);
            var tiny = Math.Pow(2d, -500d);
            var points = new[]
            {
                new Point2(0d, 0d),
                new Point2(large, 0d),
                new Point2(0d, large),
                new Point2(-tiny, 0d)
            };

            try
            {
                PolylineMetrics.SignedArea(points);
                throw new Exception("Expected polyline area to fail closed when final compensation is swallowed by rounding.");
            }
            catch (OverflowException ex)
            {
                if (ex.Message.IndexOf("compensated contribution", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Expected swallowed-area-compensation failure to identify the lost compensated contribution.");
            }
        }
    }
}