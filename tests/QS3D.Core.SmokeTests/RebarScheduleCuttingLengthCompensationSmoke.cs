using System;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarScheduleCuttingLengthCompensationSmoke
    {
        internal static void Run()
        {
            CollectivelySignificantAllowancesArePreserved();
            InputOrderDoesNotDropAllowances();
            OrdinaryLengthAndQuantityRemainStable();
            OverflowStillFailsClosed();
        }

        private static void CollectivelySignificantAllowancesArePreserved()
        {
            var rows = RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput
                {
                    ElementId = "E-COMP",
                    BarMark = "B-COMP",
                    Notation = "1D16",
                    CuttingLengthM = 1e16d,
                    LapLengthM = 1d,
                    AnchorLengthM = 1d,
                    HookAllowanceM = 0d
                }
            });

            const double expected = 10000000000000002d;
            Assert(rows.Count == 1, "Expected one compensated rebar schedule row.");
            Assert(rows[0].Quantity == 1, "Compensated rebar schedule quantity changed unexpectedly.");
            Assert(rows[0].CuttingLengthM == expected, "Rebar schedule cutting length must preserve collectively significant lap/anchor allowances.");
            Assert(rows[0].TotalLengthM == expected, "Rebar schedule total length must inherit the compensated cutting length.");
            Assert(rows[0].NetWeightKg > 0d && rows[0].TotalWeightKg > 0d, "Compensated schedule length must continue to produce positive finite weights.");
        }

        private static void InputOrderDoesNotDropAllowances()
        {
            var rows = RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput
                {
                    ElementId = "E-ORDER",
                    BarMark = "B-ORDER",
                    Notation = "1D16",
                    CuttingLengthM = 1d,
                    LapLengthM = 1e16d,
                    AnchorLengthM = 1d,
                    HookAllowanceM = 0d
                }
            });

            const double expected = 10000000000000002d;
            Assert(rows.Count == 1, "Expected one input-order rebar schedule row.");
            Assert(rows[0].CuttingLengthM == expected, "Rebar cutting-length compensation must preserve small contributions around a huge middle allowance.");
            Assert(rows[0].TotalLengthM == expected, "Rebar total length must inherit input-order-independent compensated cutting length.");
        }

        private static void OrdinaryLengthAndQuantityRemainStable()
        {
            var rows = RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput
                {
                    ElementId = "E-ORDINARY",
                    BarMark = "B-ORDINARY",
                    Notation = "2D16",
                    CuttingLengthM = 2d,
                    LapLengthM = 0.5d,
                    AnchorLengthM = 0.25d,
                    HookAllowanceM = 0.25d
                }
            });

            Assert(rows.Count == 1, "Expected one ordinary rebar schedule row.");
            Assert(rows[0].Quantity == 2, "Ordinary rebar schedule quantity changed unexpectedly.");
            Assert(rows[0].CuttingLengthM == 3d, "Ordinary cutting/lap/anchor/hook composition changed unexpectedly.");
            Assert(rows[0].TotalLengthM == 6d, "Ordinary total rebar length changed unexpectedly.");
        }

        private static void OverflowStillFailsClosed()
        {
            var error = Capture<OverflowException>(() => RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput
                {
                    ElementId = "E-OVERFLOW",
                    BarMark = "B-OVERFLOW",
                    Notation = "1D16",
                    CuttingLengthM = double.MaxValue,
                    LapLengthM = double.MaxValue
                }
            }));

            Assert(error.Message == "Rebar addition overflow: cutting + lap length", "Cutting-length overflow contract changed unexpectedly.");
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
