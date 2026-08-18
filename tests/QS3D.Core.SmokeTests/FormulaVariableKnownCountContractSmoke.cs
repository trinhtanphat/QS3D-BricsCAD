using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Formulas;

namespace QS3D.Core.SmokeTests
{
    internal static class FormulaVariableKnownCountContractSmoke
    {
        private const int MaxVariableCount = 4096;

        public static void Run()
        {
            ConflictingKnownCountsFailBeforeEnumeration();
            CapacityViolationPrecedesOtherMalformedCounts();
            NegativeKnownCountsFailBeforeEnumeration();
            ConsistentKnownCountsRemainAccepted();
            ExactBoundRemainsAccepted();
            DishonestKnownCountStillStopsAtStreamingBoundary();
            OrdinaryVariableSemanticsRemainCompatible();
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new MultiCountDictionary(actualCount: 1, readOnlyCount: 1, genericCount: 2, nonGenericCount: 1, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => new ExpressionEvaluator().Evaluate("1", source),
                "conflicting known counts",
                "Formula variables must reject conflicting known Count contracts before enumeration.");
            if (source.EnumerationRequested)
                throw new Exception("Formula variable ingestion requested the enumerator after conflicting known-count evidence was already available.");
        }

        private static void CapacityViolationPrecedesOtherMalformedCounts()
        {
            var source = new MultiCountDictionary(actualCount: 1, readOnlyCount: 1, genericCount: MaxVariableCount + 1, nonGenericCount: -1, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => new ExpressionEvaluator().Evaluate("1", source),
                "exceeds the supported maximum",
                "Known variable capacity violations must retain precedence over negative/conflicting Count diagnostics.");
            if (source.EnumerationRequested)
                throw new Exception("Known formula variable capacity violation must fail before caller enumeration.");
        }

        private static void NegativeKnownCountsFailBeforeEnumeration()
        {
            var source = new MultiCountDictionary(actualCount: 1, readOnlyCount: -1, genericCount: -1, nonGenericCount: -1, throwOnEnumeration: true);
            ExpectInvalidOperation(
                () => new ExpressionEvaluator().Evaluate("1", source),
                "invalid negative count",
                "Negative formula variable Count contracts must fail closed before enumeration.");
            if (source.EnumerationRequested)
                throw new Exception("Negative formula variable Count evidence must be rejected before enumeration.");
        }

        private static void ConsistentKnownCountsRemainAccepted()
        {
            var source = new MultiCountDictionary(actualCount: 1, readOnlyCount: 1, genericCount: 1, nonGenericCount: 1, throwOnEnumeration: false);
            var value = new ExpressionEvaluator().Evaluate("V0 + 2", source);
            if (Math.Abs(value - 3d) > 1e-12)
                throw new Exception("Consistent multi-interface variable Count contracts must preserve ordinary expression evaluation.");
            if (source.EnumerationRequestCount != 1)
                throw new Exception("Consistent formula variable input should be enumerated exactly once during normalization.");
        }

        private static void ExactBoundRemainsAccepted()
        {
            var source = new MultiCountDictionary(
                actualCount: MaxVariableCount,
                readOnlyCount: MaxVariableCount,
                genericCount: MaxVariableCount,
                nonGenericCount: MaxVariableCount,
                throwOnEnumeration: false);
            var value = new ExpressionEvaluator().Evaluate("V4095", source);
            if (Math.Abs(value - MaxVariableCount) > 1e-12)
                throw new Exception("The exact 4,096-variable boundary must remain accepted.");
            if (source.MoveNextCalls != MaxVariableCount + 1)
                throw new Exception("Exact-bound variable normalization must consume the complete source and its terminal MoveNext.");
        }

        private static void DishonestKnownCountStillStopsAtStreamingBoundary()
        {
            var source = new MultiCountDictionary(
                actualCount: MaxVariableCount + 1,
                readOnlyCount: 1,
                genericCount: 1,
                nonGenericCount: 1,
                throwOnEnumeration: false);
            ExpectInvalidOperation(
                () => new ExpressionEvaluator().Evaluate("1", source),
                "exceeds the supported maximum",
                "Dishonest low variable Count contracts must remain independently bounded while streaming.");
            if (source.MoveNextCalls != MaxVariableCount + 1)
                throw new Exception("Formula variable ingestion must stop immediately after observing raw variable 4,097.");
        }

        private static void OrdinaryVariableSemanticsRemainCompatible()
        {
            var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                [" Width "] = 2d,
                ["Height"] = 3d
            };
            var value = new ExpressionEvaluator().Evaluate("width * HEIGHT", variables);
            if (Math.Abs(value - 6d) > 1e-12)
                throw new Exception("Known-count hardening must not change established variable trimming/case-insensitive evaluation semantics.");
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessageFragment, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception(message + " Actual diagnostic: " + ex.Message);
                return;
            }

            throw new Exception(message);
        }

        private sealed class MultiCountDictionary : IReadOnlyDictionary<string, double>, ICollection<KeyValuePair<string, double>>, ICollection
        {
            private readonly int _actualCount;
            private readonly int _readOnlyCount;
            private readonly int _genericCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            public MultiCountDictionary(int actualCount, int readOnlyCount, int genericCount, int nonGenericCount, bool throwOnEnumeration)
            {
                _actualCount = actualCount;
                _readOnlyCount = readOnlyCount;
                _genericCount = genericCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            int IReadOnlyCollection<KeyValuePair<string, double>>.Count => _readOnlyCount;
            int ICollection<KeyValuePair<string, double>>.Count => _genericCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<KeyValuePair<string, double>>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerable<string> Keys => throw new NotSupportedException();
            public IEnumerable<double> Values => throw new NotSupportedException();
            public double this[string key] => throw new NotSupportedException();
            public bool EnumerationRequested { get; private set; }
            public int EnumerationRequestCount { get; private set; }
            public int MoveNextCalls { get; private set; }

            public bool ContainsKey(string key) => throw new NotSupportedException();
            public bool TryGetValue(string key, out double value)
            {
                value = default;
                throw new NotSupportedException();
            }

            public IEnumerator<KeyValuePair<string, double>> GetEnumerator()
            {
                EnumerationRequested = true;
                EnumerationRequestCount++;
                if (_throwOnEnumeration)
                    throw new Exception("Enumerator must not be requested for malformed known Count contracts.");
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<KeyValuePair<string, double>>.Add(KeyValuePair<string, double> item) => throw new NotSupportedException();
            void ICollection<KeyValuePair<string, double>>.Clear() => throw new NotSupportedException();
            bool ICollection<KeyValuePair<string, double>>.Contains(KeyValuePair<string, double> item) => throw new NotSupportedException();
            void ICollection<KeyValuePair<string, double>>.CopyTo(KeyValuePair<string, double>[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<KeyValuePair<string, double>>.Remove(KeyValuePair<string, double> item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<KeyValuePair<string, double>>
            {
                private readonly MultiCountDictionary _owner;
                private int _index = -1;

                public Enumerator(MultiCountDictionary owner) { _owner = owner; }
                public KeyValuePair<string, double> Current { get; private set; }
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index >= _owner._actualCount) return false;
                    Current = new KeyValuePair<string, double>("V" + _index, _index + 1d);
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
