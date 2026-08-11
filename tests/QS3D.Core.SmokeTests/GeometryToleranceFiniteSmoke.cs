using System;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class GeometryToleranceFiniteSmoke
    {
        internal static void Run()
        {
            ExpectRejected(double.NaN, 0.002d, 0.001d, "NaN point tolerance");
            ExpectRejected(double.PositiveInfinity, double.PositiveInfinity, 0.001d, "infinite point tolerance");
            ExpectRejected(0.0005d, double.NaN, 0.001d, "NaN boundary-gap tolerance");
            ExpectRejected(0.0005d, double.PositiveInfinity, 0.001d, "infinite boundary-gap tolerance");
            ExpectRejected(0.0005d, 0.002d, double.NaN, "NaN tiny-segment tolerance");
            ExpectRejected(0.0005d, 0.002d, double.PositiveInfinity, "infinite tiny-segment tolerance");

            var policy = new GeometryTolerancePolicy();
            Check(policy.NearlyEqual(1d, 1.0004d), "default point tolerance");
            Check(policy.CanAutoClose(0.0015d), "default boundary-gap tolerance");
            Check(!policy.CanAutoClose(0.0021d), "boundary gap above default tolerance");
            Check(policy.IsTiny(0.0005d), "default tiny-segment tolerance");
            Check(!policy.IsTiny(0.0011d), "segment above default tiny tolerance");
        }

        private static void ExpectRejected(double pointToleranceM, double boundaryGapToleranceM, double tinySegmentM, string label)
        {
            try
            {
                _ = new GeometryTolerancePolicy(pointToleranceM, boundaryGapToleranceM, tinySegmentM);
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            throw new InvalidOperationException(label + ": invalid policy was accepted.");
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label + ": assertion failed.");
        }
    }
}
