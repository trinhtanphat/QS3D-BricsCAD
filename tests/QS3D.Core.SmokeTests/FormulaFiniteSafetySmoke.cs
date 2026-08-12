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

            Near(5d, evaluator.Evaluate("min(10, 5)"), 1e-12);
            Near(6d, evaluator.Evaluate("2 * 3"), 1e-12);
            Near(1d, evaluator.Evaluate(new string('-', 64) + "1"), 1e-12);

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
    }
}