using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class LinearRebarClearancePrecisionSmoke
    {
        internal static void Run()
        {
            LostPositiveClearanceFailsClosed();
            RepresentableLargeClearanceStillPlans();
            OrdinaryLayoutRemainsStable();
        }

        private static void LostPositiveClearanceFailsClosed()
        {
            var error = Capture<OverflowException>(() => LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 1e16d,
                CoverM = 0d,
                DiameterMm = 1000d,
                Count = 2
            }));

            Assert(
                error.Message == "Linear rebar usable span lost positive edge clearance at the current numeric scale.",
                "Linear rebar precision-collapse diagnostic changed unexpectedly.");
        }

        private static void RepresentableLargeClearanceStillPlans()
        {
            var layout = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 1e16d,
                CoverM = 0d,
                DiameterMm = 2000d,
                Count = 2
            });

            Assert(layout.Count == 2, "Representable large-scale linear layout count changed unexpectedly.");
            Assert(layout.UsableSpanM == 9999999999999998d, "Representable two-metre clearance must remain accepted at large coordinate scale.");
            Assert(layout.OffsetsM[0] == -4999999999999999d, "Large-scale first linear rebar offset changed unexpectedly.");
            Assert(layout.OffsetsM[1] == 4999999999999999d, "Large-scale last linear rebar offset changed unexpectedly.");
        }

        private static void OrdinaryLayoutRemainsStable()
        {
            var layout = LinearRebarLayoutPlanner.Plan(new LinearRebarLayoutInput
            {
                SpanM = 10d,
                CoverM = 0.05d,
                DiameterMm = 20d,
                Count = 3
            });

            Assert(layout.Count == 3, "Ordinary linear rebar count changed unexpectedly.");
            Assert(Math.Abs(layout.UsableSpanM - 9.88d) <= 1e-12d, "Ordinary usable span changed unexpectedly.");
            Assert(Math.Abs(layout.ActualSpacingM - 4.94d) <= 1e-12d, "Ordinary actual spacing changed unexpectedly.");
            Assert(Math.Abs(layout.OffsetsM[0] + 4.94d) <= 1e-12d, "Ordinary first offset changed unexpectedly.");
            Assert(Math.Abs(layout.OffsetsM[1]) <= 1e-12d, "Ordinary center offset changed unexpectedly.");
            Assert(Math.Abs(layout.OffsetsM[2] - 4.94d) <= 1e-12d, "Ordinary last offset changed unexpectedly.");
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
