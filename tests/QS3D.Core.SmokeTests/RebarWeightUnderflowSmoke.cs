using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarWeightUnderflowSmoke
    {
        internal static void Run()
        {
            var kilogramsPerMeter = 16d * 16d / 162d;
            Near(kilogramsPerMeter * 2d, RebarWeight.TotalKilograms(16d, 2d));
            Near(kilogramsPerMeter * 2d, RebarWeight.TotalKilograms(16d, 2d, 0d));
            Near(kilogramsPerMeter * 2d * 1.05d, RebarWeight.TotalKilograms(16d, 2d, 5d));

            try
            {
                RebarWeight.TotalKilograms(16d, 1d, double.Epsilon);
            }
            catch (OverflowException ex)
            {
                if (!string.Equals("Rebar division underflow: wastePercent", ex.Message, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Tiny nonzero rebar waste must fail through the guarded division path.",
                        ex);
                }

                return;
            }

            throw new InvalidOperationException(
                "Tiny nonzero rebar waste must not be silently discarded by percentage conversion.");
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
