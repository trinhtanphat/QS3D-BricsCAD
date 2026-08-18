using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class Point2DistancePrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var dominant = Math.Pow(2d, 500d);

            ExpectPrecisionLoss(
                () => new Point2(0d, 0d).DistanceTo(new Point2(dominant, 1d)),
                "A positive Y component must not disappear from a dominant X distance.");
            ExpectPrecisionLoss(
                () => new Point2(0d, 0d).DistanceTo(new Point2(1d, dominant)),
                "A positive X component must not disappear from a dominant Y distance.");

            Equal(dominant, new Point2(0d, 0d).DistanceTo(new Point2(dominant, 0d)),
                "A true one-axis distance must remain accepted.");

            var ordinary = new Point2(0d, 0d).DistanceTo(new Point2(3e200d, 4e200d));
            Relative(5e200d, ordinary, 1e-12d,
                "Ordinary multi-axis scaled distance must remain stable.");
        }

        private static void ExpectPrecisionLoss(Func<double> action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("orthogonal component", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception(message + " Unexpected failure message: " + ex.Message);
            }

            throw new Exception(message);
        }

        private static void Equal(double expected, double actual, string message)
        {
            if (expected != actual)
                throw new Exception(message + " Expected " + expected + ", got " + actual + ".");
        }

        private static void Relative(double expected, double actual, double tolerance, string message)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual))
                throw new Exception(message + " Distance must be finite.");
            var scale = Math.Max(Math.Abs(expected), Math.Abs(actual));
            if (scale == 0d) return;
            if (Math.Abs(expected - actual) / scale > tolerance)
                throw new Exception(message + " Expected " + expected + ", got " + actual + ".");
        }
    }
}
