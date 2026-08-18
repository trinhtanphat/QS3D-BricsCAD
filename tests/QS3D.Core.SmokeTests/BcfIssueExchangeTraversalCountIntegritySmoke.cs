using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfIssueExchangeTraversalCountIntegritySmoke
    {
        private static readonly DateTime CreatedUtc = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);

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
            var topics = new KnownCountCollection<BcfTopic>(2, new[] { CreateTopic(1) });
            ThrowsCountMismatch(() => BcfIssueExchange.Create(topics));
        }

        private static void AdvertisedCountLessThanTraversalFails()
        {
            var topics = new KnownCountCollection<BcfTopic>(1, new[] { CreateTopic(1), CreateTopic(2) });
            ThrowsCountMismatch(() => BcfIssueExchange.Create(topics));
        }

        private static void EnumerableWithoutKnownCountRemainsAccepted()
        {
            var topics = new EnumerableOnly<BcfTopic>(new[] { CreateTopic(2), CreateTopic(1) });
            var exchange = BcfIssueExchange.Create(topics);

            Require(exchange.Topics.Count == 2, "Enumerable-only BCF collection changed materialized topic count.");
            Require(exchange.Topics[0].Id == CreateTopic(1).Id && exchange.Topics[1].Id == CreateTopic(2).Id,
                "Enumerable-only BCF collection changed canonical topic ordering.");
        }

        private static void ThrowsCountMismatch(Action action)
        {
            try
            {
                action();
                throw new Exception("Expected BCF Count/traversal mismatch rejection.");
            }
            catch (ArgumentException exception)
            {
                Require(exception.ParamName == "topics",
                    "Unexpected BCF Count/traversal mismatch parameter: " + (exception.ParamName ?? "<null>"));
                Require(exception.Message.StartsWith(
                        "BCF collection Count does not match enumerated item count.",
                        StringComparison.Ordinal),
                    "Unexpected BCF Count/traversal mismatch diagnostic: " + exception.Message);
            }
        }

        private static BcfTopic CreateTopic(int index)
        {
            return new BcfTopic(
                GuidFor(index),
                "Topic " + index,
                "Open",
                "Issue",
                string.Empty,
                "tester",
                CreatedUtc,
                Array.Empty<BcfComment>(),
                Array.Empty<BcfViewpoint>());
        }

        private static string GuidFor(int index)
            => index.ToString("x8") + "-0000-0000-0000-000000000000";

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
