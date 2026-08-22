using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Measurement;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementTraceReconciliationPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CollectivelySignificantSmallAdditionsReconcile();
            InputOrderDoesNotDropSmallAdjustments();
            OrdinaryAdditionAndDeductionRemainStable();
            NonFiniteReconciliationStillFailsClosed();
        }

        private static void CollectivelySignificantSmallAdditionsReconcile()
        {
            const double expected = 10000000000000002d;
            var trace = CreateTrace(
                1e16d,
                new[]
                {
                    Adjustment(MeasurementTraceAdjustmentKind.Addition, 1d, "addition-1"),
                    Adjustment(MeasurementTraceAdjustmentKind.Addition, 1d, "addition-2")
                },
                expected);

            Assert(
                trace.NetValue.Equals(expected),
                "MeasurementTrace must preserve collectively significant small finite adjustments.");
        }

        private static void InputOrderDoesNotDropSmallAdjustments()
        {
            const double expected = 10000000000000002d;
            var trace = CreateTrace(
                1d,
                new[]
                {
                    Adjustment(MeasurementTraceAdjustmentKind.Addition, 1e16d, "huge-middle"),
                    Adjustment(MeasurementTraceAdjustmentKind.Addition, 1d, "small-after")
                },
                expected);

            Assert(
                trace.NetValue.Equals(expected),
                "MeasurementTrace reconciliation must preserve small finite contributions around a huge middle adjustment.");
        }

        private static void OrdinaryAdditionAndDeductionRemainStable()
        {
            var trace = CreateTrace(
                10d,
                new[]
                {
                    Adjustment(MeasurementTraceAdjustmentKind.Addition, 2d, "addition"),
                    Adjustment(MeasurementTraceAdjustmentKind.Deduction, 1d, "deduction")
                },
                11d);

            Assert(trace.NetValue.Equals(11d), "Ordinary exact trace reconciliation changed unexpectedly.");
        }

        private static void NonFiniteReconciliationStillFailsClosed()
        {
            var error = Capture<ArgumentException>(() => CreateTrace(
                double.MaxValue,
                new[] { Adjustment(MeasurementTraceAdjustmentKind.Addition, double.MaxValue, "overflow") },
                double.MaxValue));

            Assert(
                error.ParamName == "netValue",
                "Non-finite trace reconciliation must continue to fail closed on netValue.");
        }

        private static MeasurementTrace CreateTrace(
            double grossValue,
            MeasurementTraceAdjustment[] adjustments,
            double netValue)
        {
            return new MeasurementTrace(
                "semantic-precision",
                "source-precision",
                "quantity",
                Array.Empty<MeasurementTraceFact>(),
                grossValue,
                adjustments,
                netValue,
                "m",
                "none");
        }

        private static MeasurementTraceAdjustment Adjustment(
            MeasurementTraceAdjustmentKind kind,
            double amount,
            string sourceIdentity)
        {
            return new MeasurementTraceAdjustment(
                kind,
                amount,
                "m",
                "precision regression",
                sourceIdentity);
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
