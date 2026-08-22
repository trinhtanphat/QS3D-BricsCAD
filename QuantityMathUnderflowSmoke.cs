using System;
using System.Reflection;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityMathUnderflowSmoke
    {
        private static readonly Type QuantityMathType = typeof(GeometryTolerancePolicy).Assembly.GetType("QS3D.Core.Services.QuantityMath", throwOnError: true)
            ?? throw new InvalidOperationException("QuantityMath type was not found.");

        internal static void Run()
        {
            Equal(0d, Invoke("Multiply", 0d, double.Epsilon, "zero multiplication"));
            CanonicalPositiveZero(Invoke("Multiply", -0d, 2d, "negative-zero left multiplication"));
            CanonicalPositiveZero(Invoke("Multiply", 2d, -0d, "negative-zero right multiplication"));
            Equal(double.Epsilon, Invoke("Multiply", double.Epsilon, 1d, "subnormal multiplication"));
            var multiplyUnderflow = Capture<InvalidOperationException>(() => Invoke("Multiply", 1e-200d, 1e-200d, "multiply regression"));
            Equal("Quantity multiplication underflow: multiply regression", multiplyUnderflow.Message);

            CanonicalPositiveZero(Invoke("Add", -0d, -0d, "negative-zero addition"));
            Equal(3d, Invoke("Add", 1d, 2d, "ordinary addition"));
            Equal(double.Epsilon, Invoke("Add", double.Epsilon, 0d, "subnormal addition"));

            CanonicalPositiveZero(Invoke("SubtractFloorZero", -0d, 0d, "negative-zero floor subtraction"));
            CanonicalPositiveZero(Invoke("SubtractFloorZero", 2d, 5d, "negative floor subtraction"));
            Equal(3d, Invoke("SubtractFloorZero", 5d, 2d, "ordinary subtraction"));

            Equal(0d, Invoke("Divide", 0d, 2d, "zero division"));
            CanonicalPositiveZero(Invoke("Divide", -0d, 2d, "negative-zero division"));
            Equal(double.Epsilon, Invoke("Divide", double.Epsilon, 1d, "subnormal division"));
            var divideUnderflow = Capture<InvalidOperationException>(() => Invoke("Divide", double.Epsilon, 2d, "divide regression"));
            Equal("Quantity division underflow: divide regression", divideUnderflow.Message);

            CanonicalPositiveZero(InvokeClamp(-0d, 0d, 1d, "negative-zero clamp"));
            Equal(0.5d, InvokeClamp(0.5d, 0d, 1d, "ordinary clamp"));
            Equal(1d, InvokeClamp(2d, 0d, 1d, "upper clamp"));
        }

        private static double Invoke(string methodName, double first, double second, string label)
        {
            var method = QuantityMathType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null) throw new Exception("QuantityMath method not found: " + methodName);

            try
            {
                var result = method.Invoke(null, new object[] { first, second, label });
                if (result is double value) return value;
                throw new Exception("QuantityMath method did not return a double: " + methodName);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static double InvokeClamp(double value, double minimum, double maximum, string label)
        {
            var method = QuantityMathType.GetMethod("Clamp", BindingFlags.Public | BindingFlags.Static);
            if (method == null) throw new Exception("QuantityMath method not found: Clamp");

            try
            {
                var result = method.Invoke(null, new object[] { value, minimum, maximum, label });
                if (result is double quantity) return quantity;
                throw new Exception("QuantityMath method did not return a double: Clamp");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static T Capture<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void CanonicalPositiveZero(double actual)
        {
            if (actual != 0d) throw new Exception("Expected zero but got " + actual + ".");
            if (BitConverter.DoubleToInt64Bits(actual) != BitConverter.DoubleToInt64Bits(0d))
                throw new Exception("Expected canonical positive zero.");
        }

        private static void Equal(double expected, double actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + " but got " + actual + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
