using System;
using System.Collections.Generic;
using QS3D.Core.Formulas;

namespace QS3D.Core.SmokeTests
{
    internal static class FormulaVariableNameNormalizationSmoke
    {
        internal static void Run()
        {
            var evaluator = new ExpressionEvaluator();
            var padded = new Dictionary<string, double>
            {
                ["  Width "] = 0.4,
                ["HEIGHT\t"] = 0.6
            };

            Near(1.0, evaluator.Evaluate("width + height", padded), 1e-12, "trimmed variable-name binding");
            Near(0.24, evaluator.Evaluate("WIDTH * Height", padded), 1e-12, "trimmed variable-name multiplication");

            var ambiguous = new Dictionary<string, double>
            {
                [" Width"] = 0.4,
                ["width "] = 0.5
            };
            Throws<InvalidOperationException>(() => evaluator.Evaluate("width", ambiguous), "trimmed duplicate variable names");

            var blank = new Dictionary<string, double> { [" \t "] = 1.0 };
            Throws<InvalidOperationException>(() => evaluator.Evaluate("1", blank), "blank variable name");
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
