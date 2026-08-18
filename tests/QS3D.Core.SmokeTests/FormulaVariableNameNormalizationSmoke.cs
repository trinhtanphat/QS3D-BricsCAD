using System;
using System.Collections;
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

            var exactBound = BuildVariables(4096);
            Near(1.0, evaluator.Evaluate("v0", exactBound), 1e-12, "exact variable-count bound");

            var overBound = BuildVariables(4097);
            Throws<InvalidOperationException>(() => evaluator.Evaluate("v0", overBound), "variable-count one over bound");

            var reportedOversize = new ReportedOversizeDictionary();
            Throws<InvalidOperationException>(() => evaluator.Evaluate("v0", reportedOversize), "reported oversized variable count rejected before enumeration");
            if (reportedOversize.EnumerationAttempted)
                throw new InvalidOperationException("reported oversized variable count: evaluator enumerated after the count guard should have rejected input.");

            var reportedNegative = new ReportedNegativeCountDictionary();
            Throws<InvalidOperationException>(() => evaluator.Evaluate("1", reportedNegative), "reported negative variable count rejected before enumeration");
            if (reportedNegative.EnumerationAttempted)
                throw new InvalidOperationException("reported negative variable count: evaluator enumerated after the count guard should have rejected input.");

            var underEnumerated = new ReportedCountDictionary(
                2,
                new KeyValuePair<string, double>("v0", 1d));
            Throws<InvalidOperationException>(
                () => evaluator.Evaluate("v0", underEnumerated),
                "advertised count larger than observed traversal");

            var overEnumerated = new ReportedCountDictionary(
                1,
                new KeyValuePair<string, double>("v0", 1d),
                new KeyValuePair<string, double>("v1", 2d));
            Throws<InvalidOperationException>(
                () => evaluator.Evaluate("v0", overEnumerated),
                "advertised count smaller than observed traversal");

            var consistentReportedCount = new ReportedCountDictionary(
                2,
                new KeyValuePair<string, double>("v0", 1d),
                new KeyValuePair<string, double>("v1", 2d));
            Near(3d, evaluator.Evaluate("v0 + v1", consistentReportedCount), 1e-12, "consistent advertised and observed variable count");
        }

        private static Dictionary<string, double> BuildVariables(int count)
        {
            var result = new Dictionary<string, double>(count, StringComparer.Ordinal);
            for (var i = 0; i < count; i++) result.Add("v" + i, i + 1d);
            return result;
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

        private sealed class ReportedCountDictionary : IReadOnlyDictionary<string, double>
        {
            private readonly int _reportedCount;
            private readonly KeyValuePair<string, double>[] _entries;

            public ReportedCountDictionary(int reportedCount, params KeyValuePair<string, double>[] entries)
            {
                _reportedCount = reportedCount;
                _entries = entries ?? throw new ArgumentNullException(nameof(entries));
            }

            public int Count => _reportedCount;

            public IEnumerable<string> Keys
            {
                get
                {
                    for (var i = 0; i < _entries.Length; i++)
                        yield return _entries[i].Key;
                }
            }

            public IEnumerable<double> Values
            {
                get
                {
                    for (var i = 0; i < _entries.Length; i++)
                        yield return _entries[i].Value;
                }
            }

            public double this[string key]
            {
                get
                {
                    if (TryGetValue(key, out var value)) return value;
                    throw new KeyNotFoundException();
                }
            }

            public bool ContainsKey(string key) => TryGetValue(key, out _);

            public bool TryGetValue(string key, out double value)
            {
                for (var i = 0; i < _entries.Length; i++)
                {
                    if (string.Equals(_entries[i].Key, key, StringComparison.Ordinal))
                    {
                        value = _entries[i].Value;
                        return true;
                    }
                }

                value = default;
                return false;
            }

            public IEnumerator<KeyValuePair<string, double>> GetEnumerator()
                => ((IEnumerable<KeyValuePair<string, double>>)_entries).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ReportedOversizeDictionary : IReadOnlyDictionary<string, double>
        {
            public bool EnumerationAttempted { get; private set; }
            public int Count => 4097;
            public IEnumerable<string> Keys => Array.Empty<string>();
            public IEnumerable<double> Values => Array.Empty<double>();
            public double this[string key] => throw new KeyNotFoundException();

            public bool ContainsKey(string key) => false;
            public bool TryGetValue(string key, out double value)
            {
                value = default;
                return false;
            }

            public IEnumerator<KeyValuePair<string, double>> GetEnumerator()
            {
                EnumerationAttempted = true;
                throw new InvalidOperationException("Enumeration should not occur for an oversized reported count.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ReportedNegativeCountDictionary : IReadOnlyDictionary<string, double>
        {
            public bool EnumerationAttempted { get; private set; }
            public int Count => -1;
            public IEnumerable<string> Keys => Array.Empty<string>();
            public IEnumerable<double> Values => Array.Empty<double>();
            public double this[string key] => throw new KeyNotFoundException();

            public bool ContainsKey(string key) => false;
            public bool TryGetValue(string key, out double value)
            {
                value = default;
                return false;
            }

            public IEnumerator<KeyValuePair<string, double>> GetEnumerator()
            {
                EnumerationAttempted = true;
                throw new InvalidOperationException("Enumeration should not occur for a negative reported count.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
