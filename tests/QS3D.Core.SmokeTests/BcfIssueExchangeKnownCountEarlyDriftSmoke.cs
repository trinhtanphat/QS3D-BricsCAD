using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class BcfIssueExchangeKnownCountEarlyDriftSmoke
    {
        private static readonly DateTime CreatedUtc = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectTopicOverrunBeforeNullValidation();
            RejectViewpointOverrunBeforeNullValidation();
            RejectCommentOverrunBeforeNullValidation();
            RejectComponentOverrunBeforeNullValidation();
            RejectTopicUnderYieldAfterTraversal();
            PreservePureStreamingTopics();
        }

        private static void RejectTopicOverrunBeforeNullValidation()
        {
            var source = new DishonestKnownCountCollection<BcfTopic>(
                reportedCount: 1,
                new BcfTopic[] { ValidTopic(1), null! });

            ExpectCountDrift(
                () => BcfIssueExchange.Create(source),
                "topics",
                "Topic Count overrun must outrank validation of the unexpected null topic.");
            RequireMoveNextCalls(source, 2, "topic Count overrun");
        }

        private static void RejectViewpointOverrunBeforeNullValidation()
        {
            var viewpoints = new DishonestKnownCountCollection<BcfViewpoint>(
                reportedCount: 1,
                new BcfViewpoint[] { ValidViewpoint(10), null! });

            ExpectCountDrift(
                () => new BcfTopic(
                    GuidFor(10),
                    "Topic 10",
                    "Open",
                    "Issue",
                    string.Empty,
                    "tester",
                    CreatedUtc,
                    Array.Empty<BcfComment>(),
                    viewpoints),
                "viewpoints",
                "Viewpoint Count overrun must outrank validation of the unexpected null viewpoint.");
            RequireMoveNextCalls(viewpoints, 2, "viewpoint Count overrun");
        }

        private static void RejectCommentOverrunBeforeNullValidation()
        {
            var comments = new DishonestKnownCountCollection<BcfComment>(
                reportedCount: 1,
                new BcfComment[] { ValidComment(20), null! });

            ExpectCountDrift(
                () => new BcfTopic(
                    GuidFor(20),
                    "Topic 20",
                    "Open",
                    "Issue",
                    string.Empty,
                    "tester",
                    CreatedUtc,
                    comments,
                    Array.Empty<BcfViewpoint>()),
                "comments",
                "Comment Count overrun must outrank validation of the unexpected null comment.");
            RequireMoveNextCalls(comments, 2, "comment Count overrun");
        }

        private static void RejectComponentOverrunBeforeNullValidation()
        {
            var components = new DishonestKnownCountCollection<BcfComponentReference>(
                reportedCount: 1,
                new BcfComponentReference[] { ValidComponent(30), null! });

            ExpectCountDrift(
                () => new BcfViewpoint(GuidFor(30), Camera(), components),
                "components",
                "Component Count overrun must outrank validation of the unexpected null component.");
            RequireMoveNextCalls(components, 2, "component Count overrun");
        }

        private static void RejectTopicUnderYieldAfterTraversal()
        {
            var source = new DishonestKnownCountCollection<BcfTopic>(
                reportedCount: 2,
                new[] { ValidTopic(40) });

            ExpectCountDrift(
                () => BcfIssueExchange.Create(source),
                "topics",
                "Topic Count under-yield must fail after completed traversal.");
            RequireMoveNextCalls(source, 2, "topic Count under-yield");
        }

        private static void PreservePureStreamingTopics()
        {
            var exchange = BcfIssueExchange.Create(PureStreamingTopics());
            if (exchange.Topics.Count != 2 ||
                exchange.Topics[0].Id != GuidFor(50) ||
                exchange.Topics[1].Id != GuidFor(51))
            {
                throw new InvalidOperationException("Pure streaming BCF topics changed after known-Count drift hardening.");
            }
        }

        private static IEnumerable<BcfTopic> PureStreamingTopics()
        {
            yield return ValidTopic(51);
            yield return ValidTopic(50);
        }

        private static BcfTopic ValidTopic(int index)
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

        private static BcfViewpoint ValidViewpoint(int index)
        {
            return new BcfViewpoint(GuidFor(index), Camera(), Array.Empty<BcfComponentReference>());
        }

        private static BcfComment ValidComment(int index)
        {
            return new BcfComment(GuidFor(index), "tester", CreatedUtc, "Comment " + index, null);
        }

        private static BcfComponentReference ValidComponent(int index)
        {
            return new BcfComponentReference("QS3D-" + index, IfcGuidFor(index));
        }

        private static BcfOrthogonalCamera Camera()
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

        private static string IfcGuidFor(int index)
        {
            return index.ToString("D2") + new string('A', 20);
        }

        private static void ExpectCountDrift(Action action, string parameterName, string failureMessage)
        {
            try
            {
                action();
            }
            catch (ArgumentException exception)
            {
                if (!string.Equals(exception.ParamName, parameterName, StringComparison.Ordinal))
                    throw new InvalidOperationException(failureMessage + " Unexpected parameter: " + (exception.ParamName ?? "<null>") + ".", exception);
                if (!exception.Message.StartsWith("BCF collection Count does not match enumerated item count.", StringComparison.Ordinal))
                    throw new InvalidOperationException(failureMessage + " Unexpected diagnostic: " + exception.Message, exception);
                return;
            }
            throw new InvalidOperationException(failureMessage);
        }

        private static void RequireMoveNextCalls<T>(DishonestKnownCountCollection<T> source, int expected, string label)
        {
            if (source.MoveNextCalls != expected)
                throw new InvalidOperationException(
                    "Unexpected MoveNext calls for " + label + ": expected " + expected + ", actual " + source.MoveNextCalls + ".");
        }

        private sealed class DishonestKnownCountCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _reportedCount;

            internal DishonestKnownCountCollection(int reportedCount, T[] items)
            {
                _reportedCount = reportedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count => _reportedCount;
            internal int MoveNextCalls { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly DishonestKnownCountCollection<T> _owner;
                private int _index = -1;

                internal Enumerator(DishonestKnownCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public T Current { get; private set; } = default!;
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index >= _owner._items.Length) return false;
                    Current = _owner._items[_index];
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
