using System;
using System.Reflection;
using QS3D.Core.Commercial;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostBenchmarkMedianPrecisionSmoke
    {
        private const decimal HighMiddle = 79228162514264337593543950330m;
        private const decimal CommercialBoundaryMagnitude = 8000000000000000000000000000m;
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            RepresentableOverflowedAggregateAverageRemainsExact();
            RepresentableHighMagnitudeAverageRemainsAccepted();
            UnrepresentableHighMagnitudeAverageFailsClosed();
            HighMagnitudeEvenMedianFailsClosed();
            OrdinaryEvenMedianRemainsStable();
            OddMedianRemainsStable();
            RoundedHighMagnitudeCommercialAdditionFailsClosed();
            RoundedHighMagnitudeCommercialSubtractionFailsClosed();
            TrueCommercialAdditionOverflowKeepsOverflowContract();
            TrueCommercialSubtractionOverflowKeepsOverflowContract();
            RepresentableCommercialAdditionRemainsExact();
            RepresentableCommercialSubtractionRemainsExact();
            CommercialAdditionCancellationCanonicalizesZeroScale();
            CommercialSubtractionCancellationCanonicalizesZeroScale();
        }

        private static void RepresentableOverflowedAggregateAverageRemainsExact()
        {
            const decimal expected = 36566844237352771197020284770m;
            var ascending = Analyze(
                expected,
                0m, 0m, 0m, 0m, 0m, 0m, 0m,
                decimal.MaxValue, decimal.MaxValue, decimal.MaxValue,
                decimal.MaxValue, decimal.MaxValue, decimal.MaxValue);
            var reversed = Analyze(
                expected,
                decimal.MaxValue, decimal.MaxValue, decimal.MaxValue,
                decimal.MaxValue, decimal.MaxValue, decimal.MaxValue,
                0m, 0m, 0m, 0m, 0m, 0m, 0m);

            Equal(expected, ascending.AverageUnitCost,
                "A representable benchmark mean must survive an unrepresentable aggregate without incremental-rounding drift.");
            Equal(expected, reversed.AverageUnitCost,
                "Benchmark average must remain deterministic when the same high-dynamic-range samples arrive in another order.");
            Equal<decimal?>(0m, ascending.DeviationFromAveragePercent,
                "Exact benchmark average must preserve zero deviation for the matching current cost.");
        }

        private static void RepresentableHighMagnitudeAverageRemainsAccepted()
        {
            var expected = decimal.MaxValue - 1m;
            var result = Analyze(
                expected,
                decimal.MaxValue - 2m,
                decimal.MaxValue - 1m,
                decimal.MaxValue - 1m,
                decimal.MaxValue);

            Equal(4, result.SampleCount, "High-magnitude benchmark sample count changed.");
            Equal(decimal.MaxValue - 2m, result.MinimumUnitCost, "High-magnitude benchmark minimum changed.");
            Equal(decimal.MaxValue, result.MaximumUnitCost, "High-magnitude benchmark maximum changed.");
            Equal(expected, result.AverageUnitCost, "Representable high-magnitude benchmark average must survive an unrepresentable raw sum.");
            Equal(expected, result.MedianUnitCost, "High-magnitude benchmark median changed.");
            Equal<decimal?>(0m, result.DeviationFromAveragePercent, "High-magnitude benchmark deviation changed.");
        }

        private static void UnrepresentableHighMagnitudeAverageFailsClosed()
        {
            var error = Capture<OverflowException>(() =>
                Analyze(decimal.MaxValue - 1m, decimal.MaxValue - 1m, decimal.MaxValue));

            Contains(
                "benchmark translated average",
                error.ToString(),
                "Benchmark average must fail closed when the mathematical mean cannot be represented as decimal.");
        }

        private static void HighMagnitudeEvenMedianFailsClosed()
        {
            var error = Capture<OverflowException>(() =>
                Analyze(HighMiddle, HighMiddle - 4m, HighMiddle, HighMiddle + 1m, HighMiddle + 3m));

            Contains(
                "Cost addition precision loss: benchmark median.",
                error.Message,
                "Even-sample benchmark median must fail closed when a non-zero half-difference cannot affect the lower middle value.");
        }

        private static void OrdinaryEvenMedianRemainsStable()
        {
            var result = Analyze(25m, 10m, 20m, 30m, 40m);

            Equal(4, result.SampleCount, "Ordinary even benchmark sample count changed.");
            Equal(10m, result.MinimumUnitCost, "Ordinary even benchmark minimum changed.");
            Equal(40m, result.MaximumUnitCost, "Ordinary even benchmark maximum changed.");
            Equal(25m, result.AverageUnitCost, "Ordinary even benchmark average changed.");
            Equal(25m, result.MedianUnitCost, "Ordinary even benchmark median changed.");
            Equal(25m, result.CurrentUnitCost, "Ordinary even benchmark current value changed.");
            Equal<decimal?>(0m, result.DeviationFromAveragePercent, "Ordinary even benchmark deviation changed.");
        }

        private static void OddMedianRemainsStable()
        {
            var result = Analyze(20m, 10m, 20m, 30m);

            Equal(3, result.SampleCount, "Odd benchmark sample count changed.");
            Equal(10m, result.MinimumUnitCost, "Odd benchmark minimum changed.");
            Equal(30m, result.MaximumUnitCost, "Odd benchmark maximum changed.");
            Equal(20m, result.AverageUnitCost, "Odd benchmark average changed.");
            Equal(20m, result.MedianUnitCost, "Odd benchmark median changed.");
            Equal(20m, result.CurrentUnitCost, "Odd benchmark current value changed.");
            Equal<decimal?>(0m, result.DeviationFromAveragePercent, "Odd benchmark deviation changed.");
        }

        private static void RoundedHighMagnitudeCommercialAdditionFailsClosed()
        {
            var error = CaptureCommercialOverflow("Add", CommercialBoundaryMagnitude, 0.6m, "boundary addition");
            Contains(
                "Commercial addition precision loss: boundary addition.",
                error.Message,
                "High-magnitude commercial addition must reject scale-reduction rounding instead of accepting a different inexact result.");
        }

        private static void RoundedHighMagnitudeCommercialSubtractionFailsClosed()
        {
            var error = CaptureCommercialOverflow("Subtract", CommercialBoundaryMagnitude, 0.6m, "boundary subtraction");
            Contains(
                "Commercial subtraction precision loss: boundary subtraction.",
                error.Message,
                "High-magnitude commercial subtraction must reject scale-reduction rounding instead of accepting a different inexact result.");
        }

        private static void TrueCommercialAdditionOverflowKeepsOverflowContract()
        {
            var error = CaptureCommercialOverflow("Add", decimal.MaxValue, 1m, "true addition overflow");
            Contains(
                "true addition overflow overflowed decimal arithmetic.",
                error.Message,
                "True commercial addition overflow must keep the established overflow contract instead of being mislabeled as precision loss.");
        }

        private static void TrueCommercialSubtractionOverflowKeepsOverflowContract()
        {
            var error = CaptureCommercialOverflow("Subtract", decimal.MinValue, 1m, "true subtraction overflow");
            Contains(
                "true subtraction overflow overflowed decimal arithmetic.",
                error.Message,
                "True commercial subtraction overflow must keep the established overflow contract instead of being mislabeled as precision loss.");
        }

        private static void RepresentableCommercialAdditionRemainsExact()
        {
            Equal(
                4.6m,
                InvokeCommercialGuard("Add", 1.2m, 3.4m, "ordinary addition"),
                "Representable commercial addition changed.");
        }

        private static void RepresentableCommercialSubtractionRemainsExact()
        {
            Equal(
                -2.2m,
                InvokeCommercialGuard("Subtract", 1.2m, 3.4m, "ordinary subtraction"),
                "Representable signed commercial subtraction changed.");
        }

        private static void CommercialAdditionCancellationCanonicalizesZeroScale()
        {
            var result = InvokeCommercialGuard("Add", -1.20m, 1.2m, "addition cancellation");
            Equal(0m, result, "Commercial addition cancellation must remain numerically zero.");
            Equal(0, DecimalScale(result),
                "Commercial addition cancellation must canonicalize zero scale so formatting and serialization do not depend on operand scale.");
        }

        private static void CommercialSubtractionCancellationCanonicalizesZeroScale()
        {
            var result = InvokeCommercialGuard("Subtract", 1.20m, 1.2m, "subtraction cancellation");
            Equal(0m, result, "Commercial subtraction cancellation must remain numerically zero.");
            Equal(0, DecimalScale(result),
                "Commercial subtraction cancellation must canonicalize zero scale so formatting and serialization do not depend on operand scale.");
        }

        private static int DecimalScale(decimal value)
        {
            return (decimal.GetBits(value)[3] >> 16) & 0x7F;
        }

        private static CostBenchmarkResult Analyze(decimal currentUnitCost, params decimal[] unitCosts)
        {
            var records = new HistoricalCostRecord[unitCosts.Length];
            for (var index = 0; index < unitCosts.Length; index++)
            {
                records[index] = new HistoricalCostRecord(
                    "MEDIAN-" + index,
                    "BUILDING",
                    "OFFICE",
                    1m,
                    unitCosts[index],
                    "VND",
                    StartUtc.AddTicks(index));
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
                throwOnError: true);
            var method = guardType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("CommercialGuard." + methodName + " was not found.");

            try
            {
                return (decimal)method.Invoke(null, new object[] { left, right, label });
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

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
