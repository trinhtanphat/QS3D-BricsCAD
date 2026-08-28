using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripExchangeResultTraversalCountSmoke
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
            var results = new KnownCountCollection<IfcRoundTripExchangeResult>(
                2,
                new[] { CreateResult(1) });

            ThrowsFinalCountMismatch(() => IfcRoundTripExchangeResultSet.Create(results));
        }

        private static void AdvertisedCountLessThanTraversalFailsEarly()
        {
            var results = new KnownCountCollection<IfcRoundTripExchangeResult>(
                1,
                new[] { CreateResult(1), CreateResult(2) });

            ThrowsEarlyCountOverrun(() => IfcRoundTripExchangeResultSet.Create(results));
        }

        private static void EnumerableWithoutKnownCountRemainsAccepted()
        {
            var results = new EnumerableOnly<IfcRoundTripExchangeResult>(
                new[] { CreateResult(2), CreateResult(1) });

            var resultSet = IfcRoundTripExchangeResultSet.Create(results);

            Require(resultSet.Items.Count == 2,
                "Enumerable-only IFC result source changed the materialized result count.");
            Require(resultSet.Items[0].ExternalObjectId == "ifc-1" &&
                    resultSet.Items[1].ExternalObjectId == "ifc-2",
                "Enumerable-only IFC result source changed canonical result ordering.");
        }

        private static void ThrowsFinalCountMismatch(Action action)
        {
            try
            {
                action();
                throw new Exception("Expected IFC Count/traversal mismatch rejection.");
            }
            catch (InvalidOperationException exception)
            {
                Require(exception.Message.StartsWith(
                        "IFC exchange result source Count does not match enumerated result count.",
                        StringComparison.Ordinal),
                    "Unexpected IFC result under-yield diagnostic: " + exception.Message);
            }
        }

        private static void ThrowsEarlyCountOverrun(Action action)
        {
            try
            {
                action();
                throw new Exception("Expected IFC early Count-overrun rejection.");
            }
            catch (InvalidOperationException exception)
            {
                Require(exception.Message.StartsWith(
                        "IFC exchange result source Count was exceeded during traversal.",
                        StringComparison.Ordinal),
                    "Unexpected IFC result over-yield diagnostic: " + exception.Message);
            }
        }

        private static IfcRoundTripExchangeResult CreateResult(int index)
        {
            return new IfcRoundTripExchangeResult(
                "ifc-" + index,
                IfcRoundTripResultState.Unmapped,
                null);
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
