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

            foreach (var invalidName in new[] { "1abc", "a-b", "a b" })
            {
                var invalid = new Dictionary<string, double> { [invalidName] = 1.0 };
                Throws<InvalidOperationException>(() => evaluator.Evaluate("1", invalid), $"invalid variable name '{invalidName}'");
            }

            var validIdentifiers = new Dictionary<string, double>
            {
                ["_x"] = 1.0,
                ["A1"] = 2.0,
                ["a.b"] = 3.0,
                ["Rate"] = 4.0
            };
            Near(10.0, evaluator.Evaluate("_x + A1 + a.b + rate", validIdentifiers), 1e-12, "valid identifier grammar and case-insensitive lookup");
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
