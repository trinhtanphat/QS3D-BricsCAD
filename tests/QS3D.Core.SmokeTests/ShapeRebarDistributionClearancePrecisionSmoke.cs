using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ShapeRebarDistributionClearancePrecisionSmoke
    {
        internal static void Run()
        {
            LostPositiveClearanceFailsClosed();
            RepresentableLargeClearanceRemainsValid();
            OrdinaryDistributionRemainsStable();
        }

        private static void LostPositiveClearanceFailsClosed()
        {
            Capture<OverflowException>(() => ShapeRebarDistributionPlanner.Plan(new ShapeRebarDistributionInput
            {
                Span = 1e16d,
                Cover = 0d,
                Radius = 0.5d,
                Count = 1,
                Centered = true
            }));
        }

        private static void RepresentableLargeClearanceRemainsValid()
        {
            var result = ShapeRebarDistributionPlanner.Plan(new ShapeRebarDistributionInput
            {
                Span = 1e16d,
                Cover = 0d,
                Radius = 1d,
                Count = 1,
                Centered = true
            });

            Assert(result.CenterClearance == 1d, "Representable large-scale center clearance changed unexpectedly.");
            Assert(result.Offsets.Count == 1 && result.Offsets[0] == 0d, "Representable large-scale centered single-bar layout changed unexpectedly.");
        }

        private static void OrdinaryDistributionRemainsStable()
        {
            var result = ShapeRebarDistributionPlanner.Plan(new ShapeRebarDistributionInput
            {
                Span = 10d,
                Cover = 1d,
                Radius = 0.5d,
                Count = 3,
                Centered = false
            });

            Assert(result.CenterClearance == 1.5d, "Ordinary shape-rebar center clearance changed unexpectedly.");
            Assert(result.Offsets.Count == 3, "Ordinary shape-rebar count changed unexpectedly.");
            Assert(result.Offsets[0] == 1.5d, "Ordinary first shape-rebar offset changed unexpectedly.");
            Assert(result.Offsets[1] == 5d, "Ordinary middle shape-rebar offset changed unexpectedly.");
            Assert(result.Offsets[2] == 8.5d, "Ordinary last shape-rebar offset changed unexpectedly.");
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
