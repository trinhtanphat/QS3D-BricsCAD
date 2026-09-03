using System;
using System.Collections.Generic;
using System.Reflection;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CostBenchmarkMedianPrecisionSmoke
    {
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static void Run()
        {
            MedianPreservesSubCentPrecision();
            MedianEvenSamplePreservesSubCentPrecision();
            CommercialAdditionRejectsRoundedPrecisionLoss();
            CommercialSubtractionRejectsRoundedPrecisionLoss();
            CommercialArithmeticPreservesRepresentableResults();
            CommercialArithmeticPreservesOverflowSemantics();
        }

        private static void MedianPreservesSubCentPrecision()
        {
            var result = Analyze(new[] { 1.001m, 1.002m, 1.003m }, 1.002m);
            Require(result.MedianUnitCost == 1.002m, "odd median must preserve exact decimal precision");
        }

        private static void MedianEvenSamplePreservesSubCentPrecision()
        {
            var result = Analyze(new[] { 1.001m, 1.002m, 1.003m, 1.004m }, 1.0025m);
            Require(result.MedianUnitCost == 1.0025m, "even median must preserve exact decimal precision");
        }

        private static void CommercialAdditionRejectsRoundedPrecisionLoss()
        {
            var ex = CaptureCommercialOverflow(
                "Add",
                8000000000000000000000000000m,
                0.6m,
                "Commercial precision boundary");
            Require(
                ex.Message == "Commercial addition precision loss: Commercial precision boundary.",
                "commercial add must reject a representability rounding loss with the stable precision-loss diagnostic");
        }

        private static void CommercialSubtractionRejectsRoundedPrecisionLoss()
        {
            var ex = CaptureCommercialOverflow(
                "Subtract",
                -8000000000000000000000000000m,
                0.6m,
                "Commercial precision boundary");
            Require(
                ex.Message == "Commercial subtraction precision loss: Commercial precision boundary.",
                "commercial subtract must reject a representability rounding loss with the stable precision-loss diagnostic");
        }

        private static void CommercialArithmeticPreservesRepresentableResults()
        {
            Require(
                InvokeCommercialGuard("Add", 12.34m, 0.66m, "representable add") == 13.00m,
                "commercial add must preserve ordinary exact representable arithmetic");
            Require(
                InvokeCommercialGuard("Subtract", 12.34m, 0.34m, "representable subtract") == 12.00m,
                "commercial subtract must preserve ordinary exact representable arithmetic");
        }

        private static void CommercialArithmeticPreservesOverflowSemantics()
        {
            var add = CaptureCommercialOverflow("Add", decimal.MaxValue, 1m, "Commercial overflow add");
            Require(
                add.Message == "Commercial overflow add overflowed decimal arithmetic.",
                "commercial add must preserve the existing true-overflow diagnostic");

            var subtract = CaptureCommercialOverflow("Subtract", decimal.MinValue, 1m, "Commercial overflow subtract");
            Require(
                subtract.Message == "Commercial overflow subtract overflowed decimal arithmetic.",
                "commercial subtract must preserve the existing true-overflow diagnostic");
        }

        private static CostBenchmarkResult Analyze(IReadOnlyList<decimal> unitCosts, decimal currentUnitCost)
        {
            var records = new List<HistoricalCostRecord>();
            for (var index = 0; index < unitCosts.Count; index++)
            {
                records.Add(new HistoricalCostRecord(
                    "project-" + index,
                    "BUILDING",
                    "OFFICE",
                    1m,
                    unitCosts[index],
                    "VND",
                    StartUtc.AddTicks(index)));
            }

            return new CostBenchmarkService().Analyze(
                new HistoricalCostCatalog(records),
                "BUILDING",
                "OFFICE",
                "VND",
                currentUnitCost);
        }

        private static OverflowException CaptureCommercialOverflow(string methodName, decimal left, decimal right, string label)
        {
            try
            {
                InvokeCommercialGuard(methodName, left, right, label);
            }
            catch (OverflowException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exact commercial arithmetic to fail closed with OverflowException.");
        }

        private static decimal InvokeCommercialGuard(string methodName, decimal left, decimal right, string label)
        {
            var guardType = typeof(CommercialAuditLog).Assembly.GetType(
                "QS3D.Core.Commercial.CommercialGuard",
                throwOnError: true)
                ?? throw new InvalidOperationException("CommercialGuard type was not found.");
            var method = guardType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("CommercialGuard." + methodName + " was not found.");

            try
            {
                var result = method.Invoke(null, new object[] { left, right, label });
                if (result is decimal value)
                    return value;
                throw new InvalidOperationException("CommercialGuard." + methodName + " returned no decimal result.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
