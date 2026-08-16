using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarStockDemandCompensationSmoke
    {
        internal static void Run()
        {
            CollectivelySignificantSmallCutsArePreserved();
            InputOrderDoesNotDropSmallCuts();
            OrdinaryDemandRemainsStable();
            OverflowStillFailsClosed();
        }

        private static void CollectivelySignificantSmallCutsArePreserved()
        {
            var demand = Demand(new[]
            {
                new RebarCutRequirement("BIG", 1e16d, 1),
                new RebarCutRequirement("SMALL-1", 1d, 1),
                new RebarCutRequirement("SMALL-2", 1d, 1)
            });

            const double expected = 10000000000000002d;
            Assert(demand.RequiredCutLengthM == expected, "Required rebar cut length must preserve collectively significant small cuts after a huge cut.");
            Assert(demand.DemandLengthBeforeKerfM == expected, "Demand before kerf must inherit the compensated required cut length when allowance is zero.");
        }

        private static void InputOrderDoesNotDropSmallCuts()
        {
            var demand = Demand(new[]
            {
                new RebarCutRequirement("SMALL-1", 1d, 1),
                new RebarCutRequirement("BIG", 1e16d, 1),
                new RebarCutRequirement("SMALL-2", 1d, 1)
            });

            Assert(demand.RequiredCutLengthM == 10000000000000002d, "Required rebar cut length must preserve collectively significant small cuts across input orderings.");
        }

        private static void OrdinaryDemandRemainsStable()
        {
            var demand = new RebarStockDemand(
                "G-ORDINARY",
                "B500",
                16d,
                12d,
                new[]
                {
                    new RebarCutRequirement("A", 2.5d, 2),
                    new RebarCutRequirement("B", 1.25d, 4)
                },
                new RebarCutAllowancePolicy(0d, 0.1d));

            Assert(demand.RequiredCutCount == 6L, "Ordinary required cut count changed unexpectedly.");
            Assert(demand.RequiredCutLengthM == 10d, "Ordinary required cut length changed unexpectedly.");
            Assert(Math.Abs(demand.AllowanceLengthM - 0.6d) <= 1e-12d, "Ordinary allowance length changed unexpectedly.");
            Assert(Math.Abs(demand.DemandLengthBeforeKerfM - 10.6d) <= 1e-12d, "Ordinary demand before kerf changed unexpectedly.");
        }

        private static void OverflowStillFailsClosed()
        {
            var error = Capture<OverflowException>(() => Demand(new[]
            {
                new RebarCutRequirement("MAX-1", double.MaxValue, 1),
                new RebarCutRequirement("MAX-2", double.MaxValue, 1)
            }));

            Assert(error.Message == "Rebar addition overflow: total required rebar cut length", "Required cut-length overflow contract changed unexpectedly.");
        }

        private static RebarStockDemand Demand(RebarCutRequirement[] cuts)
        {
            return new RebarStockDemand(
                "G-COMP",
                "B500",
                16d,
                double.MaxValue,
                cuts,
                new RebarCutAllowancePolicy());
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
