using System;
using System.Reflection;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialDecimalArithmeticExactnessSmoke
    {
        private const decimal BoundaryMagnitude = 8000000000000000000000000000m;

        internal static void Run()
        {
            RoundedHighMagnitudeAdditionFailsClosed();
            RoundedHighMagnitudeSubtractionFailsClosed();
            TrueAdditionOverflowKeepsOverflowContract();
            TrueSubtractionOverflowKeepsOverflowContract();
            RepresentableAdditionRemainsExact();
            RepresentableSubtractionRemainsExact();
        }

        private static void RoundedHighMagnitudeAdditionFailsClosed()
        {
            var error = CaptureOverflow("Add", BoundaryMagnitude, 0.6m, "boundary addition");
            Contains(
                "Commercial addition precision loss: boundary addition.",
                error.Message,
                "High-magnitude commercial addition must reject scale-reduction rounding instead of accepting a different inexact result.");
        }

        private static void RoundedHighMagnitudeSubtractionFailsClosed()
        {
            var error = CaptureOverflow("Subtract", BoundaryMagnitude, 0.6m, "boundary subtraction");
            Contains(
                "Commercial subtraction precision loss: boundary subtraction.",
                error.Message,
                "High-magnitude commercial subtraction must reject scale-reduction rounding instead of accepting a different inexact result.");
        }

        private static void TrueAdditionOverflowKeepsOverflowContract()
        {
            var error = CaptureOverflow("Add", decimal.MaxValue, 1m, "true addition overflow");
            Contains(
                "true addition overflow overflowed decimal arithmetic.",
                error.Message,
                "True commercial addition overflow must keep the established overflow contract instead of being mislabeled as precision loss.");
        }

        private static void TrueSubtractionOverflowKeepsOverflowContract()
        {
            var error = CaptureOverflow("Subtract", decimal.MinValue, 1m, "true subtraction overflow");
            Contains(
                "true subtraction overflow overflowed decimal arithmetic.",
                error.Message,
                "True commercial subtraction overflow must keep the established overflow contract instead of being mislabeled as precision loss.");
        }

        private static void RepresentableAdditionRemainsExact()
        {
            Equal(
                4.6m,
                Invoke("Add", 1.2m, 3.4m, "ordinary addition"),
                "Representable commercial addition changed.");
        }

        private static void RepresentableSubtractionRemainsExact()
        {
            Equal(
                -2.2m,
                Invoke("Subtract", 1.2m, 3.4m, "ordinary subtraction"),
                "Representable signed commercial subtraction changed.");
        }

        private static OverflowException CaptureOverflow(string methodName, decimal left, decimal right, string label)
        {
            try
            {
                Invoke(methodName, left, right, label);
            }
            catch (OverflowException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exact commercial arithmetic to fail closed with OverflowException.");
        }

        private static decimal Invoke(string methodName, decimal left, decimal right, string label)
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
