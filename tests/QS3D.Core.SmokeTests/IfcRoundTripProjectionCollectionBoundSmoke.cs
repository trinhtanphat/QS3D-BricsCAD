using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripProjectionCollectionBoundSmoke
    {
        private const int MaximumProjections = 10000;

        internal static void Run()
        {
            CountedOversizeFailsBeforeEnumeration();
            NegativeGenericCountFailsBeforeEnumeration();
            NegativeReadOnlyCountFailsBeforeEnumeration();
            NegativeNonGenericCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            OversizedKnownCountKeepsBoundPrecedence();
            ConsistentKnownCountsRemainAccepted();
            StreamingOversizeStopsAtFirstDisallowedProjection();
            ExactBoundaryRemainsAcceptedAndCanonical();
        }

        private static void CountedOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated(MaximumProjections + 1);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted IFC projection input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted oversize failure must report the IFC projection bound.");
        }

        private static void NegativeGenericCountFailsBeforeEnumeration()
        {
            var source = new GenericCountedNeverEnumerated(-1);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(0, source.GetEnumeratorCalls, "Negative generic IFC projection Count must fail before enumeration.");
            Contains("invalid negative known Count", error.Message, "Negative generic Count must report the malformed known-count contract.");
        }

        private static void NegativeReadOnlyCountFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated(-1);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(0, source.GetEnumeratorCalls, "Negative read-only IFC projection Count must fail before enumeration.");
            Contains("invalid negative known Count", error.Message, "Negative read-only Count must report the malformed known-count contract.");
        }

        private static void NegativeNonGenericCountFailsBeforeEnumeration()
        {
            var source = new NonGenericCountedNeverEnumerated(-1);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(0, source.GetEnumeratorCalls, "Negative non-generic IFC projection Count must fail before enumeration.");
            Contains("invalid negative known Count", error.Message, "Negative non-generic Count must report the malformed known-count contract.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new MultiCountSource(
                genericCount: 1,
                readOnlyCount: 2,
                nonGenericCount: 1,
                items: Array.Empty<IfcRoundTripProjection>(),
                throwOnEnumeration: true);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(0, source.GetEnumeratorCalls, "Conflicting IFC projection Count contracts must fail before enumeration.");
            Contains("conflicting known Count", error.Message, "Conflicting Count contracts must report the integrity failure.");
        }

        private static void OversizedKnownCountKeepsBoundPrecedence()
        {
            var source = new MultiCountSource(
                genericCount: MaximumProjections + 1,
                readOnlyCount: -1,
                nonGenericCount: 1,
                items: Array.Empty<IfcRoundTripProjection>(),
                throwOnEnumeration: true);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(0, source.GetEnumeratorCalls, "Oversized malformed IFC projection source must fail before enumeration.");
            Contains("at most 10000", error.Message, "An oversized known Count must retain deterministic collection-bound precedence.");
        }

        private static void ConsistentKnownCountsRemainAccepted()
        {
            var expected = Projection(42);
            var source = new MultiCountSource(
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 1,
                items: new[] { expected },
                throwOnEnumeration: false);

            var set = IfcRoundTripProjectionSet.Create(source);

            Equal(1, source.GetEnumeratorCalls, "Consistent known Count contracts must still enumerate the source exactly once.");
            Equal(1, set.Items.Count, "Consistent known Count contracts must remain accepted.");
            Equal(expected.IfcGlobalId, set.Items[0].IfcGlobalId, "Consistent known Count contracts changed canonical projection content.");
        }

        private static void StreamingOversizeStopsAtFirstDisallowedProjection()
        {
            var source = new StreamingProjections(MaximumProjections + 2);
            var error = Capture<InvalidOperationException>(() => IfcRoundTripProjectionSet.Create(source));

            Equal(
                MaximumProjections + 1,
                source.YieldedCount,
                "Streaming IFC projection ingestion must stop immediately after observing projection 10,001.");
            Contains("at most 10000", error.Message, "Streaming oversize failure must report the IFC projection bound.");
        }

        private static void ExactBoundaryRemainsAcceptedAndCanonical()
        {
            var projections = new IfcRoundTripProjection[MaximumProjections];
            for (var index = 0; index < projections.Length; index++)
                projections[index] = Projection(MaximumProjections - 1 - index);

            var set = IfcRoundTripProjectionSet.Create(projections);
            Equal(MaximumProjections, set.Items.Count, "IFC projection set must accept exactly 10,000 valid projections.");
            Equal("ifc-00000", set.Items[0].IfcGlobalId, "Boundary-sized IFC projection set lost canonical first-item ordering.");
            Equal("ifc-09999", set.Items[set.Items.Count - 1].IfcGlobalId, "Boundary-sized IFC projection set lost canonical last-item ordering.");
        }

        private static IfcRoundTripProjection Projection(int index)
        {
            var suffix = index.ToString("D5", CultureInfo.InvariantCulture);
            return new IfcRoundTripProjection(
                "ELEMENT-" + suffix,
                "ifc-" + suffix,
                "IfcBuildingElementProxy",
                new[] { new IfcRoundTripNumericProperty("Length", index + 1d, "m") },
                index + 1d,
                "m",
                new[] { "source:bound-smoke" });
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

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
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

        private sealed class CountedNeverEnumerated : IReadOnlyCollection<IfcRoundTripProjection>
        {
            internal CountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<IfcRoundTripProjection> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class GenericCountedNeverEnumerated : ICollection<IfcRoundTripProjection>
        {
            private readonly int _count;

            internal GenericCountedNeverEnumerated(int count)
            {
                _count = count;
            }

            public int Count => _count;
            public bool IsReadOnly => true;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<IfcRoundTripProjection> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Generic counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(IfcRoundTripProjection item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(IfcRoundTripProjection item) => false;
            public void CopyTo(IfcRoundTripProjection[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(IfcRoundTripProjection item) => throw new NotSupportedException();
        }

        private sealed class NonGenericCountedNeverEnumerated : IEnumerable<IfcRoundTripProjection>, ICollection
        {
            private readonly int _count;

            internal NonGenericCountedNeverEnumerated(int count)
            {
                _count = count;
            }

            int ICollection.Count => _count;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<IfcRoundTripProjection> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Non-generic counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class MultiCountSource : ICollection<IfcRoundTripProjection>, IReadOnlyCollection<IfcRoundTripProjection>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly IfcRoundTripProjection[] _items;
            private readonly bool _throwOnEnumeration;

            internal MultiCountSource(
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                IfcRoundTripProjection[] items,
                bool throwOnEnumeration)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _items = items;
                _throwOnEnumeration = throwOnEnumeration;
            }

            int ICollection<IfcRoundTripProjection>.Count => _genericCount;
            int IReadOnlyCollection<IfcRoundTripProjection>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<IfcRoundTripProjection>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<IfcRoundTripProjection> GetEnumerator()
            {
                GetEnumeratorCalls++;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Malformed multi-count source must not be enumerated.");
                return ((IEnumerable<IfcRoundTripProjection>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<IfcRoundTripProjection>.Add(IfcRoundTripProjection item) => throw new NotSupportedException();
            void ICollection<IfcRoundTripProjection>.Clear() => throw new NotSupportedException();
            bool ICollection<IfcRoundTripProjection>.Contains(IfcRoundTripProjection item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<IfcRoundTripProjection>.CopyTo(IfcRoundTripProjection[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<IfcRoundTripProjection>.Remove(IfcRoundTripProjection item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }

        private sealed class StreamingProjections : IEnumerable<IfcRoundTripProjection>
        {
            private readonly int _count;

            internal StreamingProjections(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<IfcRoundTripProjection> GetEnumerator()
            {
                for (var index = 0; index < _count; index++)
                {
                    YieldedCount++;
                    yield return Projection(index);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class IfcRoundTripProjectionCollectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            IfcRoundTripProjectionCollectionBoundSmoke.Run();
        }
    }
}
