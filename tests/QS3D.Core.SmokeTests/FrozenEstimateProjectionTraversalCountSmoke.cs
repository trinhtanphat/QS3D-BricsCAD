using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class FrozenEstimateProjectionTraversalCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AdvertisedCountGreaterThanTraversalFails();
            EnumerableWithoutKnownCountRemainsAccepted();
        }

        private static void AdvertisedCountGreaterThanTraversalFails()
        {
            var lines = new KnownCountCollection<EstimateLine>(
                1,
                Array.Empty<EstimateLine>());

            ThrowsCountMismatch(() => FrozenEstimateProjection.Create(lines));
        }

        private static void EnumerableWithoutKnownCountRemainsAccepted()
        {
            var lines = new EnumerableOnly<EstimateLine>(Array.Empty<EstimateLine>());

            var projection = FrozenEstimateProjection.Create(lines);

            Require(projection.Rows.Count == 0,
                "Enumerable-only frozen estimate source changed the materialized row count.");
        }

        private static void ThrowsCountMismatch(Action action)
        {
            try
            {
                action();
                throw new Exception("Expected frozen estimate Count/traversal mismatch rejection.");
            }
            catch (InvalidOperationException exception)
            {
                Require(exception.Message.StartsWith(
                        "Frozen estimate projection source Count does not match enumerated estimate line count.",
                        StringComparison.Ordinal),
                    "Unexpected frozen estimate Count/traversal mismatch diagnostic: " + exception.Message);
            }
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
