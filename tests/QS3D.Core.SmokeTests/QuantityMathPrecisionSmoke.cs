using System;
using System.Reflection;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityMathPrecisionSmoke
    {
        private static readonly Type QuantityMathType =
            typeof(ProjectState).Assembly.GetType("QS3D.Core.Services.QuantityMath", throwOnError: true);

        internal static void Run()
        {
            LostAddContributionFailsClosed();
            LostSubtractDeductionFailsClosed();
            LostHypotComponentFailsClosed();
            ZeroAndOrdinaryCasesRemainStable();
        }

        private static void LostAddContributionFailsClosed()
        {
            ThrowsInvalidOperation(() => Invoke("Add", 1e16, 1d), "right add contribution rounded away");
            ThrowsInvalidOperation(() => Invoke("Add", 1d, 1e16), "left add contribution rounded away");
        }

        private static void LostSubtractDeductionFailsClosed()
        {
            ThrowsInvalidOperation(() => Invoke("SubtractFloorZero", 1e16, 1d), "positive subtraction rounded away");
        }

        private static void LostHypotComponentFailsClosed()
        {
            ThrowsInvalidOperation(() => Invoke("Hypot", 1e16, 1d), "positive hypotenuse component rounded away");
        }

        private static void ZeroAndOrdinaryCasesRemainStable()
        {
            Equal(5d, Invoke("Add", 2d, 3d), "ordinary addition");
            Equal(1e16, Invoke("Add", 1e16, 0d), "zero addition");

            Equal(3d, Invoke("SubtractFloorZero", 5d, 2d), "ordinary subtraction");
            Equal(5d, Invoke("SubtractFloorZero", 5d, 0d), "zero deduction");
            Equal(0d, Invoke("SubtractFloorZero", 2d, 5d), "floor-to-zero subtraction");

            Equal(5d, Invoke("Hypot", 3d, 4d), "ordinary hypotenuse");
            Equal(1e16, Invoke("Hypot", 1e16, 0d), "zero hypotenuse component");
        }

        private static double Invoke(string methodName, double first, double second)
        {
            var method = QuantityMathType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(double), typeof(double), typeof(string) },
                modifiers: null);
            if (method == null)
                throw new InvalidOperationException("Missing QuantityMath method: " + methodName + ".");

            return (double)method.Invoke(null, new object[] { first, second, "quantity-math-precision-smoke" });
        }

        private static void ThrowsInvalidOperation(Action action, string scenario)
        {
            try
            {
                action();
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Expected InvalidOperationException for " + scenario + ".");
        }

        private static void Equal(double expected, double actual, string scenario)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException("Unexpected " + scenario + ": expected " + expected + ", got " + actual + ".");
        }
    }
}
