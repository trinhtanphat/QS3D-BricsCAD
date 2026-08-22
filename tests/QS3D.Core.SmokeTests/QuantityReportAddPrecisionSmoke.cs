using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportAddPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AddendPrecisionLossFailsClosed();
            AccumulatedValuePrecisionLossFailsClosed();
            OrdinaryAndZeroAdditionsRemainStable();
            ExistingFiniteAndOverflowGuardsRemainStable();
        }

        private static void AddendPrecisionLossFailsClosed()
        {
            var error = Capture<OverflowException>(() => QuantityReportMath.Add(1e16d, 1d, "addend-loss"));
            Contains("non-zero addend", error.Message,
                "Quantity report addition must identify a swallowed non-zero addend.");
        }

        private static void AccumulatedValuePrecisionLossFailsClosed()
        {
            var error = Capture<OverflowException>(() => QuantityReportMath.Add(1d, 1e16d, "accumulated-loss"));
            Contains("accumulated value", error.Message,
                "Quantity report addition must identify a swallowed accumulated value.");
        }

        private static void OrdinaryAndZeroAdditionsRemainStable()
        {
            Equal(5d, QuantityReportMath.Add(2d, 3d, "ordinary"),
                "Ordinary report addition changed unexpectedly.");
            Equal(1e16d, QuantityReportMath.Add(1e16d, 0d, "zero-addend"),
                "Zero addend must remain valid.");
            Equal(12d, QuantityReportMath.Add(0d, 12d, "zero-current"),
                "Zero accumulated value must remain valid.");
            Equal(0d, QuantityReportMath.Add(12d, -12d, "exact-cancel"),
                "Exact cancellation must remain valid.");
        }

        private static void ExistingFiniteAndOverflowGuardsRemainStable()
        {
            Capture<InvalidOperationException>(() => QuantityReportMath.Add(double.NaN, 1d, "nan-current"));
            Capture<InvalidOperationException>(() => QuantityReportMath.Add(1d, double.PositiveInfinity, "infinite-value"));
            Capture<OverflowException>(() => QuantityReportMath.Add(double.MaxValue, double.MaxValue, "overflow"));
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
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

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal(double expected, double actual, string message)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
