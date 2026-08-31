using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleLookupResultBoundSmoke
    {
        private const int MaximumIdentityValues = 16384;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownOverBoundRejectsBeforeEnumeration();
            ConflictingKnownCountsRejectBeforeEnumeration();
            KnownOverYieldRejectsBeforeUnexpectedCurrent();
            KnownUnderYieldRejectsAtTraversalEnd();
            TransientMoveNextCountDriftRejectsBeforeCurrent();
            TransientCurrentCountDriftRejectsBeforeRetention();
            HandlesRejectFirstStreamingOverBoundObservationBeforeCurrent();
            ElementIdsRejectFirstStreamingOverBoundObservationBeforeCurrent();
            StableInputsPreserveCanonicalizationAndDeduplication();
        }

        private static void KnownOverBoundRejectsBeforeEnumeration()
        {
            var source = new HostileCountCollection(MaximumIdentityValues + 1, MaximumIdentityValues + 1, CountMutation.None);
            ExpectIntegrityFailure(() => _ = new XlsxHandleLookupResult(source, "fp", false), "handles", "bound");
            Equal(0, source.GetEnumeratorCalls, "known over-bound GetEnumerator calls");
            Equal(0, source.CurrentReads, "known over-bound Current reads");
        }

        private static void ConflictingKnownCountsRejectBeforeEnumeration()
        {
            var source = new HostileCountCollection(1, 2, CountMutation.None);
            ExpectIntegrityFailure(() => _ = new XlsxHandleLookupResult(source, "fp", false), "handles", "conflicting");
            Equal(0, source.GetEnumeratorCalls, "conflicting Count GetEnumerator calls");
        }

        private static void KnownOverYieldRejectsBeforeUnexpectedCurrent()
        {
            var source = new HostileCountCollection(1, 1, CountMutation.None, yieldedItems: 2);
            ExpectIntegrityFailure(() => _ = new XlsxHandleLookupResult(source, "fp", false), "handles", "reported Count");
            Equal(1, source.CurrentReads, "known over-yield Current reads");
        }

        private static void KnownUnderYieldRejectsAtTraversalEnd()
        {
            var source = new HostileCountCollection(2, 2, CountMutation.None, yieldedItems: 1);
            ExpectIntegrityFailure(() => _ = new XlsxHandleLookupResult(source, "fp", false), "handles", "reported Count");
            Equal(1, source.CurrentReads, "known under-yield Current reads");
        }

        private static void TransientMoveNextCountDriftRejectsBeforeCurrent()
        {
            var source = new HostileCountCollection(1, 1, CountMutation.MoveNext);
            ExpectIntegrityFailure(() => _ = new XlsxHandleLookupResult(source, "fp", false), "handles", "changed during materialization");
            Equal(0, source.CurrentReads, "MoveNext drift Current reads");
        }

        private static void TransientCurrentCountDriftRejectsBeforeRetention()
        {
            var source = new HostileCountCollection(1, 1, CountMutation.Current);
            ExpectIntegrityFailure(() => _ = new XlsxHandleLookupResult(source, "fp", false), "handles", "changed during materialization");
            Equal(1, source.CurrentReads, "Current drift Current reads");
        }

        private static void HandlesRejectFirstStreamingOverBoundObservationBeforeCurrent()
        {
            var source = new CountingIdentitySequence(MaximumIdentityValues + 1, "AA");
            ExpectBoundFailure(() => _ = new XlsxHandleLookupResult(source, "fp", false), "handles");
            Equal(MaximumIdentityValues, source.CurrentReads, "handles Current reads");
        }

        private static void ElementIdsRejectFirstStreamingOverBoundObservationBeforeCurrent()
        {
            var source = new CountingIdentitySequence(MaximumIdentityValues + 1, "element");
            ExpectBoundFailure(() => _ = new XlsxHandleLookupResult(new[] { "AA" }, source, "fp", false), "element ids");
            Equal(MaximumIdentityValues, source.CurrentReads, "element-id Current reads");
        }

        private static void StableInputsPreserveCanonicalizationAndDeduplication()
        {
            var result = new XlsxHandleLookupResult(
                new[] { "  aa  ", "AA", "bb", " ", string.Empty },
                new[] { " element-1 ", "ELEMENT-1", "element-2" },
                " fp ",
                false);

            Equal(2, result.Handles.Count, "stable handle count");
            Equal("aa", result.Handles[0], "stable first handle");
            Equal("bb", result.Handles[1], "stable second handle");
            Equal(2, result.ElementIds.Count, "stable element-id count");
            Equal("element-1", result.ElementIds[0], "stable first element id");
            Equal("element-2", result.ElementIds[1], "stable second element id");
            Equal("fp", result.DrawingFingerprint, "stable fingerprint");
        }

        private static void ExpectBoundFailure(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.Contains("identity values", StringComparison.OrdinalIgnoreCase) ||
                    !ex.Message.Contains(MaximumIdentityValues.ToString(), StringComparison.Ordinal))
                    throw new Exception(label + " wrong bound failure: " + ex.Message);
                return;
            }
            throw new Exception(label + " expected bounded identity materialization failure.");
        }

        private static void ExpectIntegrityFailure(Action action, string label, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase))
                    throw new Exception(label + " wrong integrity failure: " + ex.Message);
                return;
            }
            throw new Exception(label + " expected Count-integrity failure.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private enum CountMutation
        {
            None,
            MoveNext,
            Current
        }

        private sealed class HostileCountCollection : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly CountMutation _mutation;
            private readonly int _yieldedItems;
            private int _transientCount;

            internal HostileCountCollection(int genericCount, int readOnlyCount, CountMutation mutation, int? yieldedItems = null)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _mutation = mutation;
                _yieldedItems = yieldedItems ?? Math.Max(0, genericCount);
                _transientCount = genericCount;
            }

            public int Count => _transientCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount == _genericCount ? _transientCount : _readOnlyCount;
            public bool IsReadOnly => true;
            internal int CurrentReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => throw new NotSupportedException();
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(string item) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly HostileCountCollection _owner;
                private int _index = -1;

                internal Enumerator(HostileCountCollection owner) => _owner = owner;

                public bool MoveNext()
                {
                    _index++;
                    if (_owner._mutation == CountMutation.MoveNext && _index == 0)
                        _owner._transientCount = _owner._genericCount + 1;
                    return _index < _owner._yieldedItems;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._mutation == CountMutation.Current && _index == 0)
                            _owner._transientCount = _owner._genericCount + 1;
                        return "AA" + _index;
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class CountingIdentitySequence : IEnumerable<string>
        {
            private readonly int _count;
            private readonly string _prefix;

            internal CountingIdentitySequence(int count, string prefix)
            {
                _count = count;
                _prefix = prefix;
            }

            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                for (var index = 0; index < _count; index++)
                {
                    CurrentReads++;
                    yield return _prefix + index;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
