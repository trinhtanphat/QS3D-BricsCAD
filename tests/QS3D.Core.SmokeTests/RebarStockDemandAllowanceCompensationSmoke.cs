using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockDemandAllowanceCompensationSmoke
    {
        internal static void Run()
        {
            DecimalAllowanceAcrossRequirementsIsCompensated();
            RequiredLengthCompensationRemainsStable();
            ZeroAllowanceRemainsStable();
            AllowanceOverflowFailsClosed();
        }

        private static void DecimalAllowanceAcrossRequirementsIsCompensated()
        {
            var cuts = new RebarCutRequirement[10];
            for (var index = 0; index < cuts.Length; index++)
                cuts[index] = new RebarCutRequirement("CUT-" + index, 1d, 1);

            var demand = Demand(cuts, 0.1d);

            Assert(demand.RequiredCutCount == 10L, "Allowance compensation must not change cut count.");
            Assert(demand.RequiredCutLengthM == 10d, "Allowance compensation must not change required cut length.");
            Assert(demand.AllowanceLengthM == 1d, "Ten 0.1m per-cut allowances must aggregate to the canonical 1m total.");
            Assert(demand.DemandLengthBeforeKerfM == 11d, "Demand before kerf must include the compensated allowance total.");
        }

        private static void RequiredLengthCompensationRemainsStable()
        {
            var demand = Demand(new[]
            {
                new RebarCutRequirement("BIG", 1e16d, 1),
                new RebarCutRequirement("SMALL-1", 1d, 1),
                new RebarCutRequirement("SMALL-2", 1d, 1)
            }, 0d);

            Assert(demand.RequiredCutLengthM == 10000000000000002d, "Required cut-length compensation regressed while sharing the accumulator helper.");
            Assert(demand.AllowanceLengthM == 0d, "Zero allowance must remain exactly zero.");
        }

        private static void ZeroAllowanceRemainsStable()
        {
            var demand = Demand(new[]
            {
                new RebarCutRequirement("A", 2.5d, 2),
                new RebarCutRequirement("B", 1.25d, 4)
            }, 0d);

            Assert(demand.RequiredCutCount == 6L, "Zero-allowance control cut count changed unexpectedly.");
            Assert(demand.RequiredCutLengthM == 10d, "Zero-allowance control required length changed unexpectedly.");
            Assert(demand.AllowanceLengthM == 0d, "Zero allowance must remain exactly zero.");
            Assert(demand.DemandLengthBeforeKerfM == 10d, "Zero allowance must not change demand before kerf.");
        }

        private static void AllowanceOverflowFailsClosed()
        {
            var error = Capture<OverflowException>(() => Demand(new[]
            {
                new RebarCutRequirement("A", 1d, 1),
                new RebarCutRequirement("B", 1d, 1)
            }, double.MaxValue));

            Assert(error.Message == "Rebar addition overflow: total required rebar cut allowance", "Allowance overflow must retain a deterministic allowance-specific error contract.");
        }

        private static RebarStockDemand Demand(RebarCutRequirement[] cuts, double allowancePerCutM)
        {
            return new RebarStockDemand(
                "G-ALLOWANCE",
                "B500",
                16d,
                double.MaxValue,
                cuts,
                new RebarCutAllowancePolicy(0d, allowancePerCutM));
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
