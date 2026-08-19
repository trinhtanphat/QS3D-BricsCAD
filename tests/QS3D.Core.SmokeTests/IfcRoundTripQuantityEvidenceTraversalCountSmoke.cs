using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripQuantityEvidenceTraversalCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AdvertisedCountGreaterThanTraversalFails();
            AdvertisedCountLessThanTraversalFails();
            EnumerableWithoutKnownCountRemainsAccepted();
        }

        private static void AdvertisedCountGreaterThanTraversalFails()
        {
            var evidence = new KnownCountCollection<IfcRoundTripQuantityEvidence>(
                2,
                new[] { CreateEvidence(1) });

            ThrowsCountMismatch(() => IfcRoundTripQuantityEvidenceSet.Create(evidence));
        }

        private static void AdvertisedCountLessThanTraversalFails()
        {
            var evidence = new KnownCountCollection<IfcRoundTripQuantityEvidence>(
                1,
                new[] { CreateEvidence(1), CreateEvidence(2) });

            ThrowsCountMismatch(() => IfcRoundTripQuantityEvidenceSet.Create(evidence));
        }

        private static void EnumerableWithoutKnownCountRemainsAccepted()
        {
            var evidence = new EnumerableOnly<IfcRoundTripQuantityEvidence>(
                new[] { CreateEvidence(2), CreateEvidence(1) });

            var set = IfcRoundTripQuantityEvidenceSet.Create(evidence);

            Require(set.CandidateCount == 2,
                "Enumerable-only IFC quantity evidence source changed materialized candidate count.");
            Require(set.Groups.Count == 2,
                "Enumerable-only IFC quantity evidence source changed grouping cardinality.");
            Require(set.Groups[0].ExternalSourceIdentity == "source-1" &&
                    set.Groups[1].ExternalSourceIdentity == "source-2",
                "Enumerable-only IFC quantity evidence source changed canonical group ordering.");
        }

        private static void ThrowsCountMismatch(Action action)
        {
            try
            {
                action();
                throw new Exception("Expected IFC quantity evidence Count/traversal mismatch rejection.");
            }
            catch (InvalidOperationException exception)
            {
                Require(exception.Message.StartsWith(
                        "IFC round-trip quantity evidence source Count does not match enumerated candidate count.",
                        StringComparison.Ordinal),
                    "Unexpected IFC quantity evidence Count/traversal mismatch diagnostic: " + exception.Message);
            }
        }

        private static IfcRoundTripQuantityEvidence CreateEvidence(int index)
        {
            return new IfcRoundTripQuantityEvidence(
                "NetVolume",
                index,
                "m3",
                "source-" + index,
                "trace-" + index);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private sealed class KnownCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly List<T> _items;
            private readonly int _count;

            internal KnownCountCollection(int count, IEnumerable<T> items)
            {
                _count = count;
                _items = new List<T>(items ?? throw new ArgumentNullException(nameof(items)));
            }

            int ICollection<T>.Count => _count;
            int IReadOnlyCollection<T>.Count => _count;
            int ICollection.Count => _count;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => _items.Contains(item);
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }

        private sealed class EnumerableOnly<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _items;

            internal EnumerableOnly(IEnumerable<T> items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
