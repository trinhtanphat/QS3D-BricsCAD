using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfIssueExchangeCollectionBoundSmoke
    {
        private const int MaxTopics = 256;
        private const int MaxViewpointsPerTopic = 256;
        private const int MaxCommentsPerTopic = 1024;
        private const int MaxComponentsPerViewpoint = 1000;
        private static readonly DateTime CreatedUtc = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownOversizedCollectionsFailBeforeEnumeration();
            LazyCollectionsStopAtFirstDisallowedItem();
            ExactBoundsRemainAccepted();
            CanonicalValidationRemainsIntact();
        }

        private static void KnownOversizedCollectionsFailBeforeEnumeration()
        {
            var topics = new EnumerationFailCollection<BcfTopic>(MaxTopics + 1);
            ThrowsBound(
                () => BcfIssueExchange.Create(topics),
                "BCF topic count exceeds the bounded package contract.",
                "topics");
            Require(!topics.EnumerationAttempted, "Known-oversized BCF topics were enumerated before rejection.");

            var viewpoints = new EnumerationFailCollection<BcfViewpoint>(MaxViewpointsPerTopic + 1);
            ThrowsBound(
                () => CreateTopic(1, Array.Empty<BcfComment>(), viewpoints),
                "BCF viewpoint count exceeds the bounded package contract.",
                "viewpoints");
            Require(!viewpoints.EnumerationAttempted, "Known-oversized BCF viewpoints were enumerated before rejection.");

            var comments = new EnumerationFailCollection<BcfComment>(MaxCommentsPerTopic + 1);
            ThrowsBound(
                () => CreateTopic(1, comments, Array.Empty<BcfViewpoint>()),
                "BCF comment count exceeds the bounded package contract.",
                "comments");
            Require(!comments.EnumerationAttempted, "Known-oversized BCF comments were enumerated before rejection.");

            var components = new EnumerationFailCollection<BcfComponentReference>(MaxComponentsPerViewpoint + 1);
            ThrowsBound(
                () => new BcfViewpoint(GuidFor(1), CreateCamera(), components),
                "BCF viewpoint component count exceeds the bounded package contract.",
                "components");
            Require(!components.EnumerationAttempted, "Known-oversized BCF components were enumerated before rejection.");
        }

        private static void LazyCollectionsStopAtFirstDisallowedItem()
        {
            var topics = new DishonestCountCollection<BcfTopic>(MaxTopics + 20, 1, CreateTopic);
            ThrowsBound(
                () => BcfIssueExchange.Create(topics),
                "BCF topic count exceeds the bounded package contract.",
                "topics");
            Require(topics.YieldedCount == MaxTopics + 1, "BCF topic streaming bound did not stop on item 257.");

            var viewpoints = new DishonestCountCollection<BcfViewpoint>(MaxViewpointsPerTopic + 20, 1, CreateViewpoint);
            ThrowsBound(
                () => CreateTopic(1, Array.Empty<BcfComment>(), viewpoints),
                "BCF viewpoint count exceeds the bounded package contract.",
                "viewpoints");
            Require(viewpoints.YieldedCount == MaxViewpointsPerTopic + 1, "BCF viewpoint streaming bound did not stop on item 257.");

            var comments = new DishonestCountCollection<BcfComment>(MaxCommentsPerTopic + 20, 1, CreateComment);
            ThrowsBound(
                () => CreateTopic(1, comments, Array.Empty<BcfViewpoint>()),
                "BCF comment count exceeds the bounded package contract.",
                "comments");
            Require(comments.YieldedCount == MaxCommentsPerTopic + 1, "BCF comment streaming bound did not stop on item 1025.");

            var components = new DishonestCountCollection<BcfComponentReference>(MaxComponentsPerViewpoint + 20, 1, CreateComponent);
            ThrowsBound(
                () => new BcfViewpoint(GuidFor(1), CreateCamera(), components),
                "BCF viewpoint component count exceeds the bounded package contract.",
                "components");
            Require(components.YieldedCount == MaxComponentsPerViewpoint + 1, "BCF component streaming bound did not stop on item 1001.");
        }

        private static void ExactBoundsRemainAccepted()
        {
            var components = new DishonestCountCollection<BcfComponentReference>(MaxComponentsPerViewpoint, MaxComponentsPerViewpoint, CreateComponent);
            var viewpoint = new BcfViewpoint(GuidFor(5000), CreateCamera(), components);
            Require(viewpoint.Components.Count == MaxComponentsPerViewpoint, "Exact BCF component bound changed cardinality.");
            Require(components.YieldedCount == MaxComponentsPerViewpoint, "Exact BCF component bound was not fully consumed.");

            var viewpoints = new DishonestCountCollection<BcfViewpoint>(MaxViewpointsPerTopic, MaxViewpointsPerTopic, CreateViewpoint);
            var topicWithViewpoints = CreateTopic(5001, Array.Empty<BcfComment>(), viewpoints);
            Require(topicWithViewpoints.Viewpoints.Count == MaxViewpointsPerTopic, "Exact BCF viewpoint bound changed cardinality.");
            Require(viewpoints.YieldedCount == MaxViewpointsPerTopic, "Exact BCF viewpoint bound was not fully consumed.");

            var comments = new DishonestCountCollection<BcfComment>(MaxCommentsPerTopic, MaxCommentsPerTopic, CreateComment);
            var topicWithComments = CreateTopic(5002, comments, Array.Empty<BcfViewpoint>());
            Require(topicWithComments.Comments.Count == MaxCommentsPerTopic, "Exact BCF comment bound changed cardinality.");
            Require(comments.YieldedCount == MaxCommentsPerTopic, "Exact BCF comment bound was not fully consumed.");

            var topics = new DishonestCountCollection<BcfTopic>(MaxTopics, MaxTopics, CreateTopic);
            var exchange = BcfIssueExchange.Create(topics);
            Require(exchange.Topics.Count == MaxTopics, "Exact BCF topic bound changed cardinality.");
            Require(topics.YieldedCount == MaxTopics, "Exact BCF topic bound was not fully consumed.");
        }

        private static void CanonicalValidationRemainsIntact()
        {
            Throws<ArgumentNullException>(() => BcfIssueExchange.Create(null!));
            Throws<ArgumentException>(() => BcfIssueExchange.Create(Array.Empty<BcfTopic>()));
            Throws<ArgumentException>(() => BcfIssueExchange.Create(new BcfTopic[] { null! }));

            var ordered = BcfIssueExchange.Create(new[] { CreateTopic(2), CreateTopic(1) });
            Require(ordered.Topics[0].Id == GuidFor(1) && ordered.Topics[1].Id == GuidFor(2),
                "BCF topic canonical ordering changed while adding collection bounds.");

            Throws<ArgumentException>(() => BcfIssueExchange.Create(new[] { CreateTopic(1), CreateTopic(1) }));

            var unknownViewpointComment = new BcfComment(GuidFor(7000), "tester", CreatedUtc, "comment", GuidFor(7001));
            Throws<ArgumentException>(() => CreateTopic(7002, new[] { unknownViewpointComment }, Array.Empty<BcfViewpoint>()));
        }

        private static BcfTopic CreateTopic(int index)
        {
            return CreateTopic(index, Array.Empty<BcfComment>(), Array.Empty<BcfViewpoint>());
        }

        private static BcfTopic CreateTopic(
            int index,
            IEnumerable<BcfComment> comments,
            IEnumerable<BcfViewpoint> viewpoints)
        {
            return new BcfTopic(
                GuidFor(index),
                "Topic " + index,
                "Open",
                "Issue",
                string.Empty,
                "tester",
                CreatedUtc,
                comments,
                viewpoints);
        }

        private static BcfViewpoint CreateViewpoint(int index)
        {
            return new BcfViewpoint(GuidFor(index), CreateCamera(), Array.Empty<BcfComponentReference>());
        }

        private static BcfComment CreateComment(int index)
        {
            return new BcfComment(GuidFor(index), "tester", CreatedUtc, "Comment " + index, null);
        }

        private static BcfComponentReference CreateComponent(int index)
        {
            return new BcfComponentReference("E" + index, index.ToString("D22"));
        }

        private static BcfOrthogonalCamera CreateCamera()
        {
            return new BcfOrthogonalCamera(
                new BcfPoint3(0d, 0d, 0d),
                new BcfPoint3(0d, 0d, -1d),
                new BcfPoint3(0d, 1d, 0d),
                1d,
                1d);
        }

        private static string GuidFor(int index)
        {
            return index.ToString("x8") + "-0000-0000-0000-000000000000";
        }

        private static void ThrowsBound(Action action, string expectedMessage, string expectedParameter)
        {
            try
            {
                action();
                throw new Exception("Expected bounded BCF collection rejection.");
            }
            catch (ArgumentException exception)
            {
                Require(exception.ParamName == expectedParameter,
                    "Unexpected BCF bound parameter: " + (exception.ParamName ?? "<null>"));
                Require(exception.Message.StartsWith(expectedMessage, StringComparison.Ordinal),
                    "Unexpected BCF bound diagnostic: " + exception.Message);
            }
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
                throw new Exception("Expected " + typeof(TException).Name + ".");
            }
            catch (TException)
            {
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private sealed class EnumerationFailCollection<T> : IReadOnlyCollection<T>
        {
            public EnumerationFailCollection(int count) => Count = count;

            public int Count { get; }
            public bool EnumerationAttempted { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationAttempted = true;
                throw new InvalidOperationException("Known-oversized input must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class DishonestCountCollection<T> : IReadOnlyCollection<T>
        {
            private readonly int _actualCount;
            private readonly int _reportedCount;
            private readonly Func<int, T> _factory;

            public DishonestCountCollection(int actualCount, int reportedCount, Func<int, T> factory)
            {
                _actualCount = actualCount;
                _reportedCount = reportedCount;
                _factory = factory;
            }

            public int Count => _reportedCount;
            public int YieldedCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                for (var index = 0; index < _actualCount; index++)
                {
                    YieldedCount++;
                    yield return _factory(index);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
