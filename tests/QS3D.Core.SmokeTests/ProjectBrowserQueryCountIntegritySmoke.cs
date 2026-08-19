using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Navigation;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectBrowserQueryCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            HonestCountPasses();
            CountTraversalMismatchFailsClosed();
            InvalidKnownCountsFailBeforeEnumeration();
            StreamingEnumerableRemainsSupported();
        }

        private static void HonestCountPasses()
        {
            var source = new CountedSource<string>(2, "F1", "F2");
            var options = new ProjectBrowserQueryOptions(floorIds: source);
            Equal(2, options.FloorIds.Count, "honest counted source length");
            Equal("F1", options.FloorIds[0], "honest counted source first value");
            Equal(1, source.EnumerationCount, "honest counted source enumeration count");
        }

        private static void CountTraversalMismatchFailsClosed()
        {
            ExpectInvalid(
                () => new ProjectBrowserQueryOptions(floorIds: new CountedSource<string>(1, "F1", "F2")),
                "does not match traversed value count",
                "under-reported Count");
            ExpectInvalid(
                () => new ProjectBrowserQueryOptions(floorIds: new CountedSource<string>(2, "F1")),
                "does not match traversed value count",
                "over-reported Count");
        }

        private static void InvalidKnownCountsFailBeforeEnumeration()
        {
            var negative = new CountedSource<string>(-1, "F1");
            ExpectInvalid(
                () => new ProjectBrowserQueryOptions(floorIds: negative),
                "negative Count",
                "negative Count");
            Equal(0, negative.EnumerationCount, "negative Count must fail before enumeration");

            var oversized = new CountedSource<string>(10001, "F1");
            ExpectInvalid(
                () => new ProjectBrowserQueryOptions(floorIds: oversized),
                "at most 10000 values",
                "oversized Count");
            Equal(0, oversized.EnumerationCount, "oversized Count must fail before enumeration");

            var conflicting = new ConflictingCountSource<string>(1, 2, "F1");
            ExpectInvalid(
                () => new ProjectBrowserQueryOptions(floorIds: conflicting),
                "conflicting Count contracts",
                "conflicting Count interfaces");
            Equal(0, conflicting.EnumerationCount, "conflicting Count must fail before enumeration");
        }

        private static void StreamingEnumerableRemainsSupported()
        {
            var options = new ProjectBrowserQueryOptions(floorIds: Stream());
            Equal(2, options.FloorIds.Count, "streaming source length");
            Equal("F2", options.FloorIds[1], "streaming source second value");
        }

        private static IEnumerable<string> Stream()
        {
            yield return "F1";
            yield return "F2";
        }

        private static void ExpectInvalid(Action action, string expectedText, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(label + ": unexpected error message: " + ex.Message);
                return;
            }

            throw new InvalidOperationException(label + ": expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private sealed class CountedSource<T> : ICollection<T>
        {
            private readonly int _count;
            private readonly List<T> _values;

            internal CountedSource(int count, params T[] values)
            {
                _count = count;
                _values = new List<T>(values);
            }

            public int Count => _count;
            public bool IsReadOnly => true;
            internal int EnumerationCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationCount++;
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => _values.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _values.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class ConflictingCountSource<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly int _collectionCount;
            private readonly int _readOnlyCount;
            private readonly List<T> _values;

            internal ConflictingCountSource(int collectionCount, int readOnlyCount, params T[] values)
            {
                _collectionCount = collectionCount;
                _readOnlyCount = readOnlyCount;
                _values = new List<T>(values);
            }

            int ICollection<T>.Count => _collectionCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            public bool IsReadOnly => true;
            internal int EnumerationCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationCount++;
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => _values.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _values.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
