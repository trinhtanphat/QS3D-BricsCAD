using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfIssueExchangeKnownCountStabilitySmoke
    {
        private static readonly DateTime CreatedUtc = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            TopicCountDriftFailsAfterExactTraversal();
            ViewpointCountDriftFailsAfterExactTraversal();
            CommentCountDriftFailsAfterExactTraversal();
            ComponentCountDriftFailsAfterExactTraversal();
            PostTraversalNegativeCountFailsClosed();
            PostTraversalConflictingCountsFailClosed();
            StableCountedAndStreamingInputsRemainAccepted();
        }

        private static void TopicCountDriftFailsAfterExactTraversal()
        {
            var topics = Drift(new[] { CreateTopic(1) }, 1, 2);
            Throws(() => BcfIssueExchange.Create(topics), "topics", "BCF collection Count changed during enumeration.");
        }

        private static void ViewpointCountDriftFailsAfterExactTraversal()
        {
            var viewpoints = Drift(new[] { CreateViewpoint(10, Array.Empty<BcfComponentReference>()) }, 1, 2);
            Throws(() => new BcfTopic(GuidFor(2), "Topic 2", "Open", "Issue", string.Empty, "tester", CreatedUtc,
                Array.Empty<BcfComment>(), viewpoints), "viewpoints", "BCF collection Count changed during enumeration.");
        }

        private static void CommentCountDriftFailsAfterExactTraversal()
        {
            var comments = Drift(new[] { new BcfComment(GuidFor(20), "tester", CreatedUtc, "comment", null) }, 1, 2);
            Throws(() => new BcfTopic(GuidFor(3), "Topic 3", "Open", "Issue", string.Empty, "tester", CreatedUtc,
                comments, Array.Empty<BcfViewpoint>()), "comments", "BCF collection Count changed during enumeration.");
        }

        private static void ComponentCountDriftFailsAfterExactTraversal()
        {
            var components = Drift(new[] { new BcfComponentReference("E-1", "0000000000000000000001") }, 1, 2);
            Throws(() => CreateViewpoint(30, components), "components", "BCF collection Count changed during enumeration.");
        }

        private static void PostTraversalNegativeCountFailsClosed()
        {
            var topics = Drift(new[] { CreateTopic(4) }, 1, -1);
            Throws(() => BcfIssueExchange.Create(topics), "topics", "BCF collection reports a negative known Count.");
        }

        private static void PostTraversalConflictingCountsFailClosed()
        {
            var topics = new DriftingKnownCountCollection<BcfTopic>(
                new[] { CreateTopic(5) },
                beforeGeneric: 1,
                beforeReadOnly: 1,
                beforeNonGeneric: 1,
                afterGeneric: 1,
                afterReadOnly: 2,
                afterNonGeneric: 1);
            Throws(() => BcfIssueExchange.Create(topics), "topics", "BCF collection reports conflicting known Count values.");
        }

        private static void StableCountedAndStreamingInputsRemainAccepted()
        {
            var topic = CreateTopic(6);
            var stable = Drift(new[] { topic }, 1, 1);
            var countedExchange = BcfIssueExchange.Create(stable);
            Require(countedExchange.Topics.Count == 1 && countedExchange.Topics[0].Id == topic.Id,
                "Stable BCF Count evidence changed canonical materialization.");

            var streamingExchange = BcfIssueExchange.Create(Stream(topic));
            Require(streamingExchange.Topics.Count == 1 && streamingExchange.Topics[0].Id == topic.Id,
                "Pure streaming BCF input changed canonical materialization.");
        }

        private static DriftingKnownCountCollection<T> Drift<T>(IEnumerable<T> items, int before, int after)
        {
            return new DriftingKnownCountCollection<T>(items, before, before, before, after, after, after);
        }

        private static IEnumerable<T> Stream<T>(T item)
        {
            yield return item;
        }

        private static BcfTopic CreateTopic(int index)
        {
            return new BcfTopic(GuidFor(index), "Topic " + index, "Open", "Issue", string.Empty, "tester", CreatedUtc,
                Array.Empty<BcfComment>(), Array.Empty<BcfViewpoint>());
        }

        private static BcfViewpoint CreateViewpoint(int index, IEnumerable<BcfComponentReference> components)
        {
            return new BcfViewpoint(
                GuidFor(index),
                new BcfOrthogonalCamera(
                    new BcfPoint3(0d, 0d, 0d),
                    new BcfPoint3(0d, 0d, -1d),
                    new BcfPoint3(0d, 1d, 0d),
                    1d,
                    1d),
                components);
        }

        private static string GuidFor(int index) => index.ToString("x8") + "-0000-0000-0000-000000000000";

        private static void Throws(Action action, string parameterName, string expectedMessage)
        {
            try
            {
                action();
                throw new Exception("Expected BCF Count stability rejection.");
            }
            catch (ArgumentException exception)
            {
                Require(exception.ParamName == parameterName,
                    "Unexpected BCF Count stability parameter: " + (exception.ParamName ?? "<null>"));
                Require(exception.Message.StartsWith(expectedMessage, StringComparison.Ordinal),
                    "Unexpected BCF Count stability diagnostic: " + exception.Message);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class DriftingKnownCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly List<T> _items;
            private readonly int _beforeGeneric;
            private readonly int _beforeReadOnly;
            private readonly int _beforeNonGeneric;
            private readonly int _afterGeneric;
            private readonly int _afterReadOnly;
            private readonly int _afterNonGeneric;
            private bool _completed;

            internal DriftingKnownCountCollection(
                IEnumerable<T> items,
                int beforeGeneric,
                int beforeReadOnly,
                int beforeNonGeneric,
                int afterGeneric,
                int afterReadOnly,
                int afterNonGeneric)
            {
                _items = new List<T>(items ?? throw new ArgumentNullException(nameof(items)));
                _beforeGeneric = beforeGeneric;
                _beforeReadOnly = beforeReadOnly;
                _beforeNonGeneric = beforeNonGeneric;
                _afterGeneric = afterGeneric;
                _afterReadOnly = afterReadOnly;
                _afterNonGeneric = afterNonGeneric;
            }

            int ICollection<T>.Count => _completed ? _afterGeneric : _beforeGeneric;
            int IReadOnlyCollection<T>.Count => _completed ? _afterReadOnly : _beforeReadOnly;
            int ICollection.Count => _completed ? _afterNonGeneric : _beforeNonGeneric;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    foreach (var item in _items)
                        yield return item;
                }
                finally
                {
                    _completed = true;
                }
            }

            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => _items.Contains(item);
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }
    }
}