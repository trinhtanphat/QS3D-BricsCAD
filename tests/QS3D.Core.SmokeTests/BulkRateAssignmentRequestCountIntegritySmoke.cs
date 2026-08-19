using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkRateAssignmentRequestCountIntegritySmoke
    {
        private const int MaximumSelectedLines = 10000;
        private const int MaximumUnitRates = 256;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            LineIdMalformedKnownCountsFailBeforeEnumeration();
            UnitRateMalformedKnownCountsFailBeforeEnumeration();
            LineIdKnownCountMismatchesFailClosed();
            UnitRateKnownCountMismatchesFailClosed();
            HonestKnownCountsRemainAccepted();
            PureStreamsPreserveIndependentBounds();
        }

        private static void LineIdMalformedKnownCountsFailBeforeEnumeration()
        {
            AssertPreEnumerationFailure(
                new MultiCountCollection<string>(new[] { "L1" }, -1, -1, -1, true),
                source => Request(source, Array.Empty<UnitRateAssignment>()),
                "negative selected-line count");

            AssertPreEnumerationFailure(
                new MultiCountCollection<string>(new[] { "L1" }, MaximumSelectedLines + 1, MaximumSelectedLines + 1, MaximumSelectedLines + 1, true),
                source => Request(source, Array.Empty<UnitRateAssignment>()),
                "exceeds the supported 10000 selected-line limit");

            AssertPreEnumerationFailure(
                new MultiCountCollection<string>(new[] { "L1" }, 1, 2, 1, true),
                source => Request(source, Array.Empty<UnitRateAssignment>()),
                "conflicting known selected-line counts");
        }

        private static void UnitRateMalformedKnownCountsFailBeforeEnumeration()
        {
            AssertPreEnumerationFailure(
                new MultiCountCollection<UnitRateAssignment>(new[] { Rate("m", 1m) }, -1, -1, -1, true),
                source => Request(new[] { "L1" }, source),
                "negative unit-rate count");

            AssertPreEnumerationFailure(
                new MultiCountCollection<UnitRateAssignment>(new[] { Rate("m", 1m) }, MaximumUnitRates + 1, MaximumUnitRates + 1, MaximumUnitRates + 1, true),
                source => Request(new[] { "L1" }, source),
                "exceeds the supported 256 unit-rate limit");

            AssertPreEnumerationFailure(
                new MultiCountCollection<UnitRateAssignment>(new[] { Rate("m", 1m) }, 1, 2, 1, true),
                source => Request(new[] { "L1" }, source),
                "conflicting known unit-rate counts");
        }

        private static void LineIdKnownCountMismatchesFailClosed()
        {
            var under = new MultiCountCollection<string>(new[] { "L1" }, 2, 2, 2, false);
            ExpectInvalidOperation(() => Request(under, Array.Empty<UnitRateAssignment>()), "selected-line count changed during enumeration");
            Equal(1, under.EnumerationRequestCount, "Known selected-line under-enumeration must enumerate exactly once.");

            var over = new MultiCountCollection<string>(new[] { "L1", "L2" }, 1, 1, 1, false);
            ExpectInvalidOperation(() => Request(over, Array.Empty<UnitRateAssignment>()), "selected-line count changed during enumeration");
            Equal(1, over.EnumerationRequestCount, "Known selected-line over-enumeration must enumerate exactly once.");
        }

        private static void UnitRateKnownCountMismatchesFailClosed()
        {
            var under = new MultiCountCollection<UnitRateAssignment>(new[] { Rate("m", 1m) }, 2, 2, 2, false);
            ExpectInvalidOperation(() => Request(new[] { "L1" }, under), "unit-rate count changed during enumeration");
            Equal(1, under.EnumerationRequestCount, "Known unit-rate under-enumeration must enumerate exactly once.");

            var over = new MultiCountCollection<UnitRateAssignment>(new[] { Rate("m", 1m), Rate("m2", 2m) }, 1, 1, 1, false);
            ExpectInvalidOperation(() => Request(new[] { "L1" }, over), "unit-rate count changed during enumeration");
            Equal(1, over.EnumerationRequestCount, "Known unit-rate over-enumeration must enumerate exactly once.");
        }

        private static void HonestKnownCountsRemainAccepted()
        {
            var lines = new MultiCountCollection<string>(new[] { "L2", "L1" }, 2, 2, 2, false);
            var rates = new MultiCountCollection<UnitRateAssignment>(new[] { Rate("m", 1m), Rate("m2", 2m) }, 2, 2, 2, false);
            var request = Request(lines, rates);

            Equal(2, request.LineIds.Count, "Honest selected-line Count must remain accepted.");
            Equal("L2", request.LineIds[0], "Bulk request must preserve selected-line order.");
            Equal(2, request.UnitRates.Count, "Honest unit-rate Count must remain accepted.");
            Equal("m", request.UnitRates[0].Unit, "Bulk request must preserve unit-rate order.");
        }

        private static void PureStreamsPreserveIndependentBounds()
        {
            var exactLines = new StreamingEnumerable<string>(MaximumSelectedLines, i => "L" + i);
            var exactLineRequest = Request(exactLines, Array.Empty<UnitRateAssignment>());
            Equal(MaximumSelectedLines, exactLineRequest.LineIds.Count, "Pure selected-line stream must accept the exact 10000 boundary.");

            var oversizedLines = new StreamingEnumerable<string>(MaximumSelectedLines + 1, i => "L" + i);
            ExpectInvalidOperation(() => Request(oversizedLines, Array.Empty<UnitRateAssignment>()), "supports at most 10000 selected lines");
            Equal(MaximumSelectedLines + 1, oversizedLines.MoveNextCalls, "Selected-line stream must stop immediately after observing item 10001.");

            var exactRates = new StreamingEnumerable<UnitRateAssignment>(MaximumUnitRates, i => Rate("u" + i, i));
            var exactRateRequest = Request(new[] { "L1" }, exactRates);
            Equal(MaximumUnitRates, exactRateRequest.UnitRates.Count, "Pure unit-rate stream must accept the exact 256 boundary.");

            var oversizedRates = new StreamingEnumerable<UnitRateAssignment>(MaximumUnitRates + 1, i => Rate("u" + i, i));
            ExpectInvalidOperation(() => Request(new[] { "L1" }, oversizedRates), "supports at most 256 unit rates");
            Equal(MaximumUnitRates + 1, oversizedRates.MoveNextCalls, "Unit-rate stream must stop immediately after observing item 257.");
        }

        private static BulkRateAssignmentRequest Request(IEnumerable<string> lineIds, IEnumerable<UnitRateAssignment> unitRates)
        {
            return new BulkRateAssignmentRequest(lineIds, "COST", "RATE-SOURCE", "REV-1", unitRates);
        }

        private static UnitRateAssignment Rate(string unit, decimal rate) => new UnitRateAssignment(unit, rate);

        private static void AssertPreEnumerationFailure<T>(
            MultiCountCollection<T> source,
            Action<MultiCountCollection<T>> action,
            string expectedMessageFragment)
        {
            ExpectInvalidOperation(() => action(source), expectedMessageFragment);
            if (source.EnumerationRequested)
                throw new Exception("Malformed known Count requested the caller enumerator before failing closed.");
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
                throw new Exception("Unexpected bulk rate Count-integrity diagnostic. Actual: " + ex.Message);
            }

            throw new Exception("Expected bulk rate Count-integrity validation to fail closed: " + expectedMessageFragment);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private sealed class MultiCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            internal MultiCountCollection(T[] items, int genericCount, int readOnlyCount, int nonGenericCount, bool throwOnEnumeration)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            internal bool EnumerationRequested { get; private set; }
            internal int EnumerationRequestCount { get; private set; }
            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationRequested = true;
                EnumerationRequestCount++;
                if (_throwOnEnumeration)
                    throw new Exception("Enumerator must not be requested for malformed known Count input.");
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }

        private sealed class StreamingEnumerable<T> : IEnumerable<T>
        {
            private readonly int _count;
            private readonly Func<int, T> _factory;

            internal StreamingEnumerable(int count, Func<int, T> factory)
            {
                _count = count;
                _factory = factory;
            }

            internal int MoveNextCalls { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly StreamingEnumerable<T> _owner;
                private int _index = -1;

                internal Enumerator(StreamingEnumerable<T> owner)
                {
                    _owner = owner;
                }

                public T Current { get; private set; } = default!;
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index >= _owner._count)
                        return false;
                    Current = _owner._factory(_index);
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
