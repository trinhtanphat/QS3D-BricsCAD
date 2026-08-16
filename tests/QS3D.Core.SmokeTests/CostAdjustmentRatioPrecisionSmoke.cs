using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostAdjustmentRatioPrecisionSmoke
    {
        private const decimal SmallestNonZeroDecimal = 0.0000000000000000000000000001m;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsAdjustmentRatioPrecisionCollapse();
            RejectsMarkupRatioPrecisionCollapse();
            PreservesOrdinaryRatioArithmetic();
        }

        private static void RejectsAdjustmentRatioPrecisionCollapse()
        {
            var error = Capture<ArgumentOutOfRangeException>(() =>
                new CostAdjustmentService().AdjustByRatios(100m, SmallestNonZeroDecimal, 0m));

            Equal(
                "adjustmentRatioPercent",
                error.ParamName,
                "Tiny non-zero adjustment ratio must identify adjustmentRatioPercent.");
            Contains(
                "too small to preserve at decimal precision",
                error.Message,
                "Tiny non-zero adjustment ratio must report decimal precision collapse.");
        }

        private static void RejectsMarkupRatioPrecisionCollapse()
        {
            var error = Capture<ArgumentOutOfRangeException>(() =>
                new CostAdjustmentService().AdjustByRatios(100m, 0m, -SmallestNonZeroDecimal));

            Equal(
                "markupRatioPercent",
                error.ParamName,
                "Tiny non-zero markup ratio must identify markupRatioPercent.");
            Contains(
                "too small to preserve at decimal precision",
                error.Message,
                "Tiny non-zero markup ratio must report decimal precision collapse.");
        }

        private static void PreservesOrdinaryRatioArithmetic()
        {
            var result = new CostAdjustmentService().AdjustByRatios(100m, 10m, 20m);

            Equal(100m, result.BaseTotal, "Ordinary adjustment base total changed.");
            Equal(10m, result.AdjustmentRatioPercent, "Ordinary adjustment ratio changed.");
            Equal(20m, result.MarkupRatioPercent, "Ordinary markup ratio changed.");
            Equal(132m, result.AdjustedTotal, "Ordinary adjusted total changed.");
            Equal(32m, result.CombinedRatioPercent, "Ordinary combined ratio changed.");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
