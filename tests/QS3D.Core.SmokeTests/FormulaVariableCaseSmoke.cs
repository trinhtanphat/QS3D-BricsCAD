using System;
using System.Collections.Generic;
using QS3D.Core.Formulas;

namespace QS3D.Core.SmokeTests
{
    internal static class FormulaVariableCaseSmoke
    {
        internal static void Run()
        {
            var evaluator = new ExpressionEvaluator();
            var variables = new Dictionary<string, double>
            {
                ["Width"] = 0.4,
                ["HEIGHT"] = 0.6
            };

            Near(1.0, evaluator.Evaluate("width + height", variables), 1e-12, "mixed-case variable addition");
            Near(0.24, evaluator.Evaluate("WIDTH * Height", variables), 1e-12, "mixed-case variable multiplication");

            var ambiguous = new Dictionary<string, double>
            {
                ["Width"] = 0.4,
                ["width"] = 0.5
            };
            Throws<InvalidOperationException>(() => evaluator.Evaluate("width", ambiguous), "case-only duplicate variable names");

            var nonFinite = new Dictionary<string, double> { ["Value"] = double.PositiveInfinity };
            Throws<InvalidOperationException>(() => evaluator.Evaluate("value", nonFinite), "case-insensitive finite-safety lookup");
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new InvalidOperationException($"{label}: expected {expected}, actual {actual}.");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException($"{label}: expected {typeof(TException).Name}.");
        }
    }
}
