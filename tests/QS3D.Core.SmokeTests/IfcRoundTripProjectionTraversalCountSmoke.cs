using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripProjectionTraversalCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AdvertisedCountGreaterThanTraversalFails();
            AdvertisedCountLessThanTraversalFailsEarly();
            EnumerableWithoutKnownCountRemainsAccepted();
        }

        private static void AdvertisedCountGreaterThanTraversalFails()
        {
            var projections = new KnownCountCollection<IfcRoundTripProjection>(
                2,
                new[] { CreateProjection(1) });

            ThrowsFinalCountMismatch(() => IfcRoundTripProjectionSet.Create(projections));
        }

        private static void AdvertisedCountLessThanTraversalFailsEarly()
        {
            var projections = new KnownCountCollection<IfcRoundTripProjection>(
                1,
                new[] { CreateProjection(1), CreateProjection(2) });

            ThrowsEarlyCountOverrun(() => IfcRoundTripProjectionSet.Create(projections));
        }

        private static void EnumerableWithoutKnownCountRemainsAccepted()
        {
            var projections = new EnumerableOnly<IfcRoundTripProjection>(
                new[] { CreateProjection(2), CreateProjection(1) });

            var projectionSet = IfcRoundTripProjectionSet.Create(projections);

            Require(projectionSet.Items.Count == 2,
                "Enumerable-only IFC projection source changed the materialized projection count.");
            Require(projectionSet.Items[0].IfcGlobalId == "ifc-1" &&
                    projectionSet.Items[1].IfcGlobalId == "ifc-2",
                "Enumerable-only IFC projection source changed canonical projection ordering.");
        }

        private static void ThrowsFinalCountMismatch(Action action)
        {
            try
            {
                action();
                throw new Exception("Expected IFC projection Count/traversal mismatch rejection.");
            }
            catch (InvalidOperationException exception)
            {
                Require(exception.Message.StartsWith(
                        "IFC round-trip projection source Count does not match enumerated projection count.",
                        StringComparison.Ordinal),
                    "Unexpected IFC projection under-yield diagnostic: " + exception.Message);
            }
        }

        private static void ThrowsEarlyCountOverrun(Action action)
        {
            try
            {
                action();
                throw new Exception("Expected IFC projection early Count-overrun rejection.");
            }
            catch (InvalidOperationException exception)
            {
                Require(exception.Message.StartsWith(
                        "IFC round-trip projection source Count was exceeded during traversal.",
                        StringComparison.Ordinal),
                    "Unexpected IFC projection over-yield diagnostic: " + exception.Message);
            }
        }

        private static IfcRoundTripProjection CreateProjection(int index)
        {
            return new IfcRoundTripProjection(
                "qs3d-" + index,
                "ifc-" + index,
                "Wall",
                Array.Empty<IfcRoundTripNumericProperty>(),
                index,
                "m3",
                new[] { "smoke" });
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
