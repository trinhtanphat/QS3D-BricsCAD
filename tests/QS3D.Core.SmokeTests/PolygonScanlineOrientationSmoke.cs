using System;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class PolygonScanlineOrientationSmoke
    {
        public static void Run()
        {
            ProductCancellationPreservesOrientation();
            TrueTouchingStillFailsClosed();
            Console.WriteLine("PASS polygon scanline exact orientation cancellation");
        }

        private static void ProductCancellationPreservesOrientation()
        {
            var twoTo100 = Math.Pow(2d, 100d);
            var nextDownTwoTo100 = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(twoTo100) - 1L);
            var nextUpOne = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(1d) + 1L);

            var roundedLeft = twoTo100 * 1d;
            var roundedRight = nextDownTwoTo100 * nextUpOne;
            if (roundedLeft != roundedRight)
                throw new Exception("Polygon orientation cancellation fixture no longer rounds both products identically.");

            var polygon = new[]
            {
                new Point2(0d, 0d),
                new Point2(twoTo100, nextDownTwoTo100),
                new Point2(nextUpOne, 1d),
                new Point2(0d, -1d)
            };

            var normalized = PolygonScanlineClipper.NormalizeAndValidate(polygon);
            if (normalized.Count != polygon.Length)
                throw new Exception("Exact polygon orientation fixture was not preserved as a simple polygon.");
        }

        private static void TrueTouchingStillFailsClosed()
        {
            var touching = new[]
            {
                new Point2(0d, 0d),
                new Point2(2d, 0d),
                new Point2(1d, 0d),
                new Point2(0d, 1d)
            };

            try
            {
                PolygonScanlineClipper.NormalizeAndValidate(touching);
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new Exception("True polygon edge touching must remain rejected.");
        }
    }
}
