using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepQuantityInputBoundSmoke
    {
        private const int MaximumElements = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CountedOversizeFailsBeforeEnumeration();
            NegativeKnownCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            ConsistentKnownCountsRemainAccepted();
            StreamingOversizeStopsAtFirstDisallowedElement();
            ExactBoundaryRemainsAccepted();
            ExistingValidationRemainsStable();
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated(MaximumElements + 1);
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted MEP input must fail before enumeration.");
            Contains("at most 10000", error.Message, "MEP counted oversize failure must report the input bound.");
        }

        private static void NegativeKnownCountFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated(-1);
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(0, source.GetEnumeratorCalls, "Negative known MEP count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative MEP count failure must identify the invalid contract.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new MultiCountedElements(1, 2, 1, new[] { Element(0) });
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(0, source.GetEnumeratorCalls, "Conflicting known MEP counts must fail before enumeration.");
            Contains("conflicting known counts", error.Message, "Conflicting MEP count failure must identify the contract mismatch.");
        }

        private static void ConsistentKnownCountsRemainAccepted()
        {
            var source = new MultiCountedElements(
                3,
                3,
                3,
                new[] { Element(0), Element(1), Element(2) });
            var groups = new MepQuantityService().Aggregate(source);

            Equal(1, source.GetEnumeratorCalls, "Consistent MEP source must be enumerated exactly once.");
            Equal(1, groups.Count, "Consistent MEP source grouping changed unexpectedly.");
            Equal(3, groups[0].ElementCount, "Consistent MEP source element count changed unexpectedly.");
            Equal(3, groups[0].QuantityCount, "Consistent MEP source quantity count changed unexpectedly.");
            Equal(3d, groups[0].LengthM, "Consistent MEP source length changed unexpectedly.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedElement()
        {
            var source = new StreamingElements(MaximumElements + 2);
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(
                MaximumElements + 1,
                source.YieldedCount,
                "Streaming MEP ingestion must stop immediately after observing element 10,001.");
            Contains("at most 10000", error.Message, "Streaming MEP oversize failure must report the input bound.");
        }

        private static void ExactBoundaryRemainsAccepted()
        {
            var elements = new MepElement[MaximumElements];
            for (var i = 0; i < elements.Length; i++)
                elements[i] = Element(i);

            var groups = new MepQuantityService().Aggregate(elements);
            Equal(1, groups.Count, "Exact MEP input boundary must remain a single aggregate group.");
            Equal(MaximumElements, groups[0].ElementCount, "MEP input must accept exactly 10,000 elements.");
            Equal(MaximumElements, groups[0].QuantityCount, "Exact MEP input boundary quantity count changed.");
            Equal((double)MaximumElements, groups[0].LengthM, "Exact MEP input boundary length changed.");
        }

        private static void ExistingValidationRemainsStable()
        {
            var service = new MepQuantityService();
            Capture<ArgumentException>(() => service.Aggregate(new[] { Element(7), Element(7) }));
            Capture<ArgumentException>(() => service.Aggregate(new MepElement[] { null! }));
        }

        private static MepElement Element(int index)
        {
            var id = "MEP-" + index.ToString("D5", CultureInfo.InvariantCulture);
            return new MepElement(
                id,
                MepElementKind.Pipe,
                "CHW",
                "DN100",
                "L1",
                count: 1,
                lengthM: 1d,
                areaM2: 1d,
                volumeM3: 1d);
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedNeverEnumerated : IReadOnlyCollection<MepElement>
        {
            internal CountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<MepElement> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Invalid counted MEP source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class MultiCountedElements : ICollection<MepElement>, IReadOnlyCollection<MepElement>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly MepElement[] _items;

            internal MultiCountedElements(int genericCount, int readOnlyCount, int nonGenericCount, MepElement[] items)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            int ICollection<MepElement>.Count => _genericCount;
            int IReadOnlyCollection<MepElement>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<MepElement>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int GetEnumeratorCalls { get; private set; }

            void ICollection<MepElement>.Add(MepElement item) => throw new NotSupportedException();
            void ICollection<MepElement>.Clear() => throw new NotSupportedException();
            bool ICollection<MepElement>.Contains(MepElement item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<MepElement>.CopyTo(MepElement[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<MepElement>.Remove(MepElement item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);

            public IEnumerator<MepElement> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return ((IEnumerable<MepElement>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingElements : IEnumerable<MepElement>
        {
            private readonly int _count;

            internal StreamingElements(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<MepElement> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Element(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
