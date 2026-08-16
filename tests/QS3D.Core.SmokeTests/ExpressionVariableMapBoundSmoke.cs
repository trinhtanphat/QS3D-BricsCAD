using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Formulas;

namespace QS3D.Core.SmokeTests
{
    internal static class ExpressionVariableMapBoundSmoke
    {
        private const int MaxVariableCount = 4096;

        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactBoundaryRemainsAccepted();
            KnownOversizeRejectsBeforeEnumeration();
            DishonestCountCannotBypassStreamingBound();
            ExistingNormalizationAndValidationRemainIntact();
            OrdinaryExpressionWithoutVariablesRemainsIntact();
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var variables = new Dictionary<string, double>(StringComparer.Ordinal);
            for (var i = 0; i < MaxVariableCount; i++)
                variables.Add("v" + i, i + 1d);

            var result = new ExpressionEvaluator().Evaluate("v0 + v4095", variables);
            Equal(4097d, result, "exact variable-map boundary must remain accepted");
        }

        private static void KnownOversizeRejectsBeforeEnumeration()
        {
            var variables = new TrackingDictionary(
                reportedCount: MaxVariableCount + 1,
                yieldedEntries: 1,
                throwIfEnumerationStarts: true);

            ExpectInvalidOperation(
                () => new ExpressionEvaluator().Evaluate("1", variables),
                "cannot exceed 4096 entries");

            if (variables.EnumerationStarted)
                throw new InvalidOperationException("Known oversized variable map must fail before enumeration starts.");
            if (variables.EnumeratedCount != 0)
                throw new InvalidOperationException("Known oversized variable map must not consume any entries.");
        }

        private static void DishonestCountCannotBypassStreamingBound()
        {
            var variables = new TrackingDictionary(
                reportedCount: 1,
                yieldedEntries: MaxVariableCount + 2,
                throwIfEnumerationStarts: false);

            ExpectInvalidOperation(
                () => new ExpressionEvaluator().Evaluate("v0", variables),
                "cannot exceed 4096 entries");

            if (!variables.EnumerationStarted)
                throw new InvalidOperationException("Dishonest-count regression must exercise streaming enumeration.");
            if (variables.EnumeratedCount != MaxVariableCount + 1)
                throw new InvalidOperationException(
                    "Streaming bound must stop on first disallowed variable-map entry. Consumed " +
                    variables.EnumeratedCount + " entries.");
        }

        private static void ExistingNormalizationAndValidationRemainIntact()
        {
            var evaluator = new ExpressionEvaluator();
            var padded = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                [" x "] = 2d,
                ["Y"] = 3d
            };
            Equal(5d, evaluator.Evaluate("x + y", padded), "existing trim/case-insensitive lookup must remain intact");

            var duplicateAfterNormalization = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                [" x"] = 1d,
                ["X "] = 2d
            };
            ExpectInvalidOperation(
                () => evaluator.Evaluate("x", duplicateAfterNormalization),
                "conflicts with another variable");

            var invalidIdentifier = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["bad-name"] = 1d
            };
            ExpectInvalidOperation(
                () => evaluator.Evaluate("1", invalidIdentifier),
                "not a valid expression identifier");

            var nonFinite = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["x"] = double.PositiveInfinity
            };
            ExpectInvalidOperation(
                () => evaluator.Evaluate("x", nonFinite),
                "non-finite value");
        }

        private static void OrdinaryExpressionWithoutVariablesRemainsIntact()
        {
            Equal(7d, new ExpressionEvaluator().Evaluate("1 + 2 * 3"), "ordinary variable-free expression");
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(
                    "Unexpected InvalidOperationException message. Expected fragment '" +
                    expectedMessageFragment + "', got: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException(
                "Expected InvalidOperationException containing '" + expectedMessageFragment + "'.");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", got " + actual + ".");
        }

        private sealed class TrackingDictionary : IReadOnlyDictionary<string, double>
        {
            private readonly int _reportedCount;
            private readonly int _yieldedEntries;
            private readonly bool _throwIfEnumerationStarts;

            public TrackingDictionary(int reportedCount, int yieldedEntries, bool throwIfEnumerationStarts)
            {
                _reportedCount = reportedCount;
                _yieldedEntries = yieldedEntries;
                _throwIfEnumerationStarts = throwIfEnumerationStarts;
            }

            public bool EnumerationStarted { get; private set; }
            public int EnumeratedCount { get; private set; }
            public int Count => _reportedCount;
            public IEnumerable<string> Keys => throw new NotSupportedException();
            public IEnumerable<double> Values => throw new NotSupportedException();
            public double this[string key] => throw new KeyNotFoundException(key);

            public bool ContainsKey(string key) => false;

            public bool TryGetValue(string key, out double value)
            {
                value = 0d;
                return false;
            }

            public IEnumerator<KeyValuePair<string, double>> GetEnumerator()
            {
                EnumerationStarted = true;
                if (_throwIfEnumerationStarts)
                    throw new InvalidOperationException("Variable map enumeration should not have started.");
                return Enumerate().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<KeyValuePair<string, double>> Enumerate()
            {
                for (var i = 0; i < _yieldedEntries; i++)
                {
                    EnumeratedCount++;
                    yield return new KeyValuePair<string, double>("v" + i, i + 1d);
                }
            }
        }
    }
}
