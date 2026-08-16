using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostAdjustmentRatioPrecisionSmoke
    {
        private const decimal SmallestNonZeroDecimal = 0.0000000000000000000000000001m;
        private const decimal SmallestSurvivingPercentage = 0.00000000000000000000000001m;
        private const decimal NearMinusOneHundredPercent = -99.99999999999999999999999999m;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsAdjustmentRatioPrecisionCollapse();
            RejectsMarkupRatioPrecisionCollapse();
            RejectsAdjustmentMultiplicationUnderflow();
            RejectsMarkupMultiplicationUnderflow();
            RejectsAdjustmentMultiplicationNoOp();
            RejectsMarkupMultiplicationNoOp();
            PreservesIntentionalMinusOneHundredPercent();
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

        private static void RejectsAdjustmentMultiplicationUnderflow()
        {
            var error = Capture<OverflowException>(() =>
                new CostAdjustmentService().AdjustByRatios(
                    SmallestNonZeroDecimal,
                    NearMinusOneHundredPercent,
                    0m));

            Contains(
                "after adjustment ratio",
                error.Message,
                "Positive cost erased by adjustment multiplication must fail closed.");
        }

        private static void RejectsMarkupMultiplicationUnderflow()
        {
            var error = Capture<OverflowException>(() =>
                new CostAdjustmentService().AdjustByRatios(
                    SmallestNonZeroDecimal,
                    0m,
                    NearMinusOneHundredPercent));

            Contains(
                "after markup ratio",
                error.Message,
                "Positive cost erased by markup multiplication must fail closed.");
        }

        private static void RejectsAdjustmentMultiplicationNoOp()
        {
            var error = Capture<ArgumentOutOfRangeException>(() =>
                new CostAdjustmentService().AdjustByRatios(
                    0.01m,
                    SmallestSurvivingPercentage,
                    0m));

            Equal(
                "adjustmentRatioPercent",
                error.ParamName,
                "Rounded-away adjustment ratio must identify adjustmentRatioPercent.");
            Contains(
                "too small to affect the value at decimal precision",
                error.Message,
                "A representable adjustment ratio that rounds back to the unchanged value must fail closed.");
        }

        private static void RejectsMarkupMultiplicationNoOp()
        {
            var error = Capture<ArgumentOutOfRangeException>(() =>
                new CostAdjustmentService().AdjustByRatios(
                    0.01m,
                    0m,
                    SmallestSurvivingPercentage));

            Equal(
                "markupRatioPercent",
                error.ParamName,
                "Rounded-away markup ratio must identify markupRatioPercent.");
            Contains(
                "too small to affect the value at decimal precision",
                error.Message,
                "A representable markup ratio that rounds back to the unchanged value must fail closed.");
        }

        private static void PreservesIntentionalMinusOneHundredPercent()
        {
            var result = new CostAdjustmentService().AdjustByRatios(123m, -100m, 50m);

            Equal(0m, result.AdjustedTotal, "An intentional -100% adjustment must still produce zero.");
            Equal(-100m, result.CombinedRatioPercent, "Intentional -100% combined ratio changed.");
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
