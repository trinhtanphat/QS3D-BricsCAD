using System;
using System.Collections.Generic;
using QS3D.Core.Formulas;

namespace QS3D.Core.SmokeTests
{
    internal static class FormulaFiniteSafetySmoke
    {
        public static void Run()
        {
            var evaluator = new ExpressionEvaluator();

            Throws<InvalidOperationException>(() => evaluator.Evaluate("min(1e308 * 1e308, 5)"));
            Throws<InvalidOperationException>(() => evaluator.Evaluate("max(1e308 + 1e308, 0)"));
            Throws<InvalidOperationException>(() => evaluator.Evaluate(
                "min(UnsafeValue, 5)",
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["UnsafeValue"] = double.PositiveInfinity
                }));
            Throws<InvalidOperationException>(() => evaluator.Evaluate(
                "max(UnsafeValue, 0)",
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["UnsafeValue"] = double.NaN
                }));

            var unusedPositiveInfinity = Capture<InvalidOperationException>(() => evaluator.Evaluate(
                "1",
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["UnusedPositiveInfinity"] = double.PositiveInfinity
                }));
            Contains("Variable 'UnusedPositiveInfinity' contains a non-finite value.", unusedPositiveInfinity.Message);

            var unusedNegativeInfinity = Capture<InvalidOperationException>(() => evaluator.Evaluate(
                "1",
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["UnusedNegativeInfinity"] = double.NegativeInfinity
                }));
            Contains("Variable 'UnusedNegativeInfinity' contains a non-finite value.", unusedNegativeInfinity.Message);

            var unusedNaN = Capture<InvalidOperationException>(() => evaluator.Evaluate(
                "1",
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["UnusedNaN"] = double.NaN
                }));
            Contains("Variable 'UnusedNaN' contains a non-finite value.", unusedNaN.Message);

            Near(1d, evaluator.Evaluate(
                "1",
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["UnusedFinite"] = 42d
                }), 0d);

            var multiplicationUnderflow = Capture<InvalidOperationException>(
                () => evaluator.Evaluate("1e-300 * 1e-300"));
            Contains("Multiplication underflowed to zero.", multiplicationUnderflow.Message);

            var divisionUnderflow = Capture<InvalidOperationException>(
                () => evaluator.Evaluate("1e-300 / 1e300"));
            Contains("Division underflowed to zero.", divisionUnderflow.Message);

            var literalUnderflow = Capture<InvalidOperationException>(
                () => evaluator.Evaluate("1e-4000"));
            Contains("Number '1e-4000' underflowed to zero.", literalUnderflow.Message);

            var roundDigitsBelowInteger = Capture<InvalidOperationException>(
                () => evaluator.Evaluate("round(1.25, 0.9999999999995)"));
            Contains("round(value, digits) requires an integer digits argument from 0 to 15.", roundDigitsBelowInteger.Message);

            var roundDigitsAboveInteger = Capture<InvalidOperationException>(
                () => evaluator.Evaluate("round(1.25, 1.0000000000005)"));
            Contains("round(value, digits) requires an integer digits argument from 0 to 15.", roundDigitsAboveInteger.Message);

            Near(5d, evaluator.Evaluate("min(10, 5)"), 1e-12);
            Near(6d, evaluator.Evaluate("2 * 3"), 1e-12);
            Near(0d, evaluator.Evaluate("0"), 0d);
            Near(0d, evaluator.Evaluate("0e-4000"), 0d);
            Near(double.Epsilon, evaluator.Evaluate("5e-324"), 0d);
            Near(0d, evaluator.Evaluate("0 * 1e-300"), 0d);
            Near(0d, evaluator.Evaluate("0 / 1e300"), 0d);
            Near(1.3d, evaluator.Evaluate("round(1.25, 1)"), 1e-12);
            Near(1.25d, evaluator.Evaluate("round(1.25, 15)"), 1e-15);
            Near(1d, evaluator.Evaluate(new string('-', 64) + "1"), 1e-12);

            PositiveZero(evaluator.Evaluate("-0"));
            PositiveZero(evaluator.Evaluate("0 * -1"));
            PositiveZero(evaluator.Evaluate("0 / -1"));
            PositiveZero(evaluator.Evaluate(
                "ZeroValue",
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ZeroValue"] = -0d
                }));

            var depthBoundary = Capture<InvalidOperationException>(
                () => evaluator.Evaluate(new string('-', 65) + "1"));
            Contains("Expression nesting is too deep. Position 65.", depthBoundary.Message);

            var longUnaryDepth = Capture<InvalidOperationException>(
                () => evaluator.Evaluate(new string('-', 4000) + "1"));
            Contains("Expression nesting is too deep. Position 65.", longUnaryDepth.Message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            Capture<T>(action);
        }

        private static T Capture<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T ex) { return ex; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private static void Contains(string expected, string actual)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception($"Expected '{actual}' to contain '{expected}'.");
        }

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new Exception($"Expected {expected} but got {actual}.");
        }

        private static void PositiveZero(double actual)
        {
            if (BitConverter.DoubleToInt64Bits(actual) != BitConverter.DoubleToInt64Bits(0d))
                throw new Exception("Expected canonical positive zero.");
        }
    }
}
