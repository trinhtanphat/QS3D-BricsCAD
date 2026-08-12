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
            Equal(double.Epsilon, Invoke("Multiply", double.Epsilon, 1d, "subnormal multiplication"));
            var multiplyUnderflow = Capture<InvalidOperationException>(() => Invoke("Multiply", 1e-200d, 1e-200d, "multiply regression"));
            Equal("Quantity multiplication underflow: multiply regression", multiplyUnderflow.Message);

            Equal(0d, Invoke("Divide", 0d, 2d, "zero division"));
            Equal(double.Epsilon, Invoke("Divide", double.Epsilon, 1d, "subnormal division"));
            var divideUnderflow = Capture<InvalidOperationException>(() => Invoke("Divide", double.Epsilon, 2d, "divide regression"));
            Equal("Quantity division underflow: divide regression", divideUnderflow.Message);
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
