using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Formulas;

namespace QS3D.Core.SmokeTests
{
    internal static class ExpressionEvaluatorKnownCountTraversalSmoke
    {
        internal static void Run()
        {
            UnderEnumerationIsRejected();
            OverEnumerationIsRejected();
            ExactKnownCountRemainsAccepted();
            OrdinaryDictionaryRemainsAccepted();
        }

        private static void UnderEnumerationIsRejected()
        {
            var variables = new MisreportedReadOnlyDictionary(
                2,
                Pair("x", 2d));

            var error = Capture<InvalidOperationException>(() =>
                new ExpressionEvaluator().Evaluate("x + 1", variables));

            Contains(
                "known count reported 2, but traversal produced 1",
                error.Message,
                "Expression variables must reject under-enumeration relative to the preflight Count.");
        }

        private static void OverEnumerationIsRejected()
        {
            var variables = new MisreportedReadOnlyDictionary(
                1,
                Pair("x", 2d),
                Pair("y", 3d));

            var error = Capture<InvalidOperationException>(() =>
                new ExpressionEvaluator().Evaluate("x + y", variables));

            Contains(
                "known count reported 1, but traversal produced 2",
                error.Message,
                "Expression variables must reject over-enumeration relative to the preflight Count.");
        }

        private static void ExactKnownCountRemainsAccepted()
        {
            var variables = new MisreportedReadOnlyDictionary(
                2,
                Pair("x", 2d),
                Pair("y", 3d));

            Equal(
                5d,
                new ExpressionEvaluator().Evaluate("x + y", variables),
                "An honest IReadOnlyDictionary Count must retain normal expression evaluation.");
        }

        private static void OrdinaryDictionaryRemainsAccepted()
        {
            var variables = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["x"] = 2d,
                ["y"] = 3d
            };

            Equal(
                7d,
                new ExpressionEvaluator().Evaluate("x * y + 1", variables),
                "Ordinary Dictionary variable evaluation must remain unchanged.");
        }

        private static KeyValuePair<string, double> Pair(string key, double value)
        {
            return new KeyValuePair<string, double>(key, value);
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal(double expected, double actual, string message)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class MisreportedReadOnlyDictionary : IReadOnlyDictionary<string, double>
        {
            private readonly KeyValuePair<string, double>[] _items;

            internal MisreportedReadOnlyDictionary(
                int advertisedCount,
                params KeyValuePair<string, double>[] items)
            {
                Count = advertisedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count { get; }

            public IEnumerable<string> Keys
            {
                get
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i].Key;
                }
            }

            public IEnumerable<double> Values
            {
                get
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i].Value;
                }
            }

            public double this[string key]
            {
                get
                {
                    if (TryGetValue(key, out var value)) return value;
                    throw new KeyNotFoundException(key);
                }
            }

            public bool ContainsKey(string key)
            {
                return TryGetValue(key, out _);
            }

            public bool TryGetValue(string key, out double value)
            {
                for (var i = 0; i < _items.Length; i++)
                {
                    if (string.Equals(_items[i].Key, key, StringComparison.Ordinal))
                    {
                        value = _items[i].Value;
                        return true;
                    }
                }

                value = default;
                return false;
            }

            public IEnumerator<KeyValuePair<string, double>> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                    yield return _items[i];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class ExpressionEvaluatorKnownCountTraversalRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExpressionEvaluatorKnownCountTraversalSmoke.Run();
        }
    }
}
