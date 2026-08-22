using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfIssueExchangeKnownCountIntegritySmoke
    {
        private const int MaxTopics = 256;
        private static readonly DateTime CreatedUtc = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            NegativeKnownCountsFailBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            OversizedKnownCountKeepsBoundPrecedence();
            NonGenericKnownCountsAreObserved();
            ConsistentKnownCountsRemainAccepted();
        }

        private static void NegativeKnownCountsFailBeforeEnumeration()
        {
            var topics = new TripleKnownCountCollection<BcfTopic>(
                genericCount: -1,
                readOnlyCount: -1,
                nonGenericCount: -1,
                items: Array.Empty<BcfTopic>(),
                failIfEnumerated: true);

            ThrowsCountContract(
                () => BcfIssueExchange.Create(topics),
                "BCF collection reports a negative known Count.");
            Require(!topics.EnumerationAttempted, "Negative BCF known Count was enumerated before rejection.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var topics = new TripleKnownCountCollection<BcfTopic>(
                genericCount: 1,
                readOnlyCount: 1,
                nonGenericCount: 2,
                items: Array.Empty<BcfTopic>(),
                failIfEnumerated: true);

            ThrowsCountContract(
                () => BcfIssueExchange.Create(topics),
                "BCF collection reports conflicting known Count values.");
            Require(!topics.EnumerationAttempted, "Conflicting BCF known Counts were enumerated before rejection.");
        }

        private static void OversizedKnownCountKeepsBoundPrecedence()
        {
            var topics = new TripleKnownCountCollection<BcfTopic>(
                genericCount: -1,
                readOnlyCount: MaxTopics + 1,
                nonGenericCount: 1,
                items: Array.Empty<BcfTopic>(),
                failIfEnumerated: true);

            ThrowsCountContract(
                () => BcfIssueExchange.Create(topics),
                "BCF topic count exceeds the bounded package contract.");
            Require(!topics.EnumerationAttempted, "Oversized BCF known Count was enumerated before rejection.");
        }

        private static void NonGenericKnownCountsAreObserved()
        {
            var negative = new NonGenericKnownCountCollection<BcfTopic>(-1, Array.Empty<BcfTopic>(), true);
            ThrowsCountContract(
                () => BcfIssueExchange.Create(negative),
                "BCF collection reports a negative known Count.");
            Require(!negative.EnumerationAttempted, "Negative non-generic BCF Count was enumerated before rejection.");

            var oversized = new NonGenericKnownCountCollection<BcfTopic>(MaxTopics + 1, Array.Empty<BcfTopic>(), true);
            ThrowsCountContract(
                () => BcfIssueExchange.Create(oversized),
                "BCF topic count exceeds the bounded package contract.");
            Require(!oversized.EnumerationAttempted, "Oversized non-generic BCF Count was enumerated before rejection.");
        }

        private static void ConsistentKnownCountsRemainAccepted()
        {
            var topic = CreateTopic(1);
            var allInterfaces = new TripleKnownCountCollection<BcfTopic>(1, 1, 1, new[] { topic }, false);
            var exchange = BcfIssueExchange.Create(allInterfaces);
            Require(exchange.Topics.Count == 1 && exchange.Topics[0].Id == topic.Id,
                "Consistent BCF known Counts changed canonical topic materialization.");
            Require(allInterfaces.EnumerationAttempted, "Consistent BCF known Counts unexpectedly skipped enumeration.");

            var nonGeneric = new NonGenericKnownCountCollection<BcfTopic>(1, new[] { topic }, false);
            var nonGenericExchange = BcfIssueExchange.Create(nonGeneric);
            Require(nonGenericExchange.Topics.Count == 1 && nonGenericExchange.Topics[0].Id == topic.Id,
                "Valid non-generic BCF Count changed canonical topic materialization.");
            Require(nonGeneric.EnumerationAttempted, "Valid non-generic BCF Count unexpectedly skipped enumeration.");
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
        {
            return index.ToString("x8") + "-0000-0000-0000-000000000000";
        }

        private static void ThrowsCountContract(Action action, string expectedMessage)
        {
            try
            {
                action();
                throw new Exception("Expected BCF known Count contract rejection.");
            }
            catch (ArgumentException exception)
            {
                Require(exception.ParamName == "topics",
                    "Unexpected BCF known Count parameter: " + (exception.ParamName ?? "<null>"));
                Require(exception.Message.StartsWith(expectedMessage, StringComparison.Ordinal),
                    "Unexpected BCF known Count diagnostic: " + exception.Message);
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private sealed class TripleKnownCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly List<T> _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _failIfEnumerated;

            internal TripleKnownCountCollection(
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                IEnumerable<T> items,
                bool failIfEnumerated)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _items = new List<T>(items ?? throw new ArgumentNullException(nameof(items)));
                _failIfEnumerated = failIfEnumerated;
            }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal bool EnumerationAttempted { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationAttempted = true;
                if (_failIfEnumerated)
                    throw new InvalidOperationException("Known Count contract must fail before enumeration.");
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => _items.Contains(item);
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }

        private sealed class NonGenericKnownCountCollection<T> : IEnumerable<T>, ICollection
        {
            private readonly List<T> _items;
            private readonly int _count;
            private readonly bool _failIfEnumerated;

            internal NonGenericKnownCountCollection(int count, IEnumerable<T> items, bool failIfEnumerated)
            {
                _count = count;
                _items = new List<T>(items ?? throw new ArgumentNullException(nameof(items)));
                _failIfEnumerated = failIfEnumerated;
            }

            int ICollection.Count => _count;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal bool EnumerationAttempted { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationAttempted = true;
                if (_failIfEnumerated)
                    throw new InvalidOperationException("Known Count contract must fail before enumeration.");
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }
    }
}
