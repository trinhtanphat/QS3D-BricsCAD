using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateReferenceGraphKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            OverrunFailsBeforeUnexpectedEdgeValidation();
            PostTraversalCountDriftFailsClosed();
            PostTraversalNegativeCountFailsClosed();
            PostTraversalConflictingCountsFailClosed();
            UnderYieldStillFailsClosed();
            HonestMultiInterfaceCountRemainsAccepted();
            PureStreamingInputRemainsAccepted();
        }

        private static void OverrunFailsBeforeUnexpectedEdgeValidation()
        {
            var source = new FixedCountCollection(
                1,
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A"),
                null!);

            ThrowsWithMessage<ArgumentException>(
                () => new RateReferenceGraph(source),
                "more entries than its known count",
                "known-count overrun must outrank unexpected null-edge validation");
        }

        private static void PostTraversalCountDriftFailsClosed()
        {
            var source = new PostTraversalCountCollection(
                1,
                2,
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A"));

            ThrowsWithMessage<ArgumentException>(
                () => new RateReferenceGraph(source),
                "known count changed during traversal",
                "post-traversal Count drift");
        }

        private static void PostTraversalNegativeCountFailsClosed()
        {
            var source = new PostTraversalCountCollection(
                1,
                -1,
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A"));

            ThrowsWithMessage<ArgumentException>(
                () => new RateReferenceGraph(source),
                "invalid negative known count",
                "negative post-traversal Count");
        }

        private static void PostTraversalConflictingCountsFailClosed()
        {
            var source = new PostTraversalMultiCountCollection(
                1,
                1,
                1,
                1,
                2,
                1,
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A"));

            ThrowsWithMessage<ArgumentException>(
                () => new RateReferenceGraph(source),
                "conflicting known counts",
                "conflicting post-traversal Count views");
        }

        private static void UnderYieldStillFailsClosed()
        {
            var source = new FixedCountCollection(
                2,
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A"));

            ThrowsWithMessage<ArgumentException>(
                () => new RateReferenceGraph(source),
                "known count does not match the observed traversal",
                "under-yield counted source");
        }

        private static void HonestMultiInterfaceCountRemainsAccepted()
        {
            var source = new PostTraversalMultiCountCollection(
                2,
                2,
                2,
                2,
                2,
                2,
                Edge("RATE-B", RateReferenceTargetKind.UnitRate, "RATE-C"),
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A"));

            var graph = new RateReferenceGraph(source);
            Equal(2, graph.Edges.Count, "honest counted edge count changed");
            Equal("RATE-A", graph.Edges[0].SourceRateCode, "deterministic sorting changed");
            Equal("RATE-B", graph.Edges[1].SourceRateCode, "deterministic sorting changed");
        }

        private static void PureStreamingInputRemainsAccepted()
        {
            var graph = new RateReferenceGraph(Stream(
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A")));
            Equal(1, graph.Edges.Count, "pure streaming input changed");
        }

        private static RateReferenceEdge Edge(
            string sourceRateCode,
            RateReferenceTargetKind targetKind,
            string targetId) =>
            new RateReferenceEdge(sourceRateCode, targetKind, targetId);

        private static IEnumerable<RateReferenceEdge> Stream(params RateReferenceEdge[] items)
        {
            for (var index = 0; index < items.Length; index++)
                yield return items[index];
        }

        private static void ThrowsWithMessage<TException>(Action action, string expectedMessage, string label)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                if (exception.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new Exception(
                    "RateReferenceGraphKnownCountStabilitySmoke " + label +
                    ": unexpected message: " + exception.Message);
            }

            throw new Exception(
                "RateReferenceGraphKnownCountStabilitySmoke " + label +
                ": expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class FixedCountCollection : ICollection<RateReferenceEdge>
        {
            private readonly RateReferenceEdge[] _items;

            internal FixedCountCollection(int count, params RateReferenceEdge[] items)
            {
                Count = count;
                _items = items;
            }

            public int Count { get; }
            public bool IsReadOnly => true;
            public IEnumerator<RateReferenceEdge> GetEnumerator() =>
                ((IEnumerable<RateReferenceEdge>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(RateReferenceEdge item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(RateReferenceEdge[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(RateReferenceEdge item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(RateReferenceEdge item) => throw new NotSupportedException();
        }

        private sealed class PostTraversalCountCollection : ICollection<RateReferenceEdge>
        {
            private readonly RateReferenceEdge[] _items;
            private readonly int _initialCount;
            private readonly int _postTraversalCount;
            private bool _traversed;

            internal PostTraversalCountCollection(
                int initialCount,
                int postTraversalCount,
                params RateReferenceEdge[] items)
            {
                _initialCount = initialCount;
                _postTraversalCount = postTraversalCount;
                _items = items;
            }

            public int Count => _traversed ? _postTraversalCount : _initialCount;
            public bool IsReadOnly => true;

            public IEnumerator<RateReferenceEdge> GetEnumerator()
            {
                try
                {
                    for (var index = 0; index < _items.Length; index++)
                        yield return _items[index];
                }
                finally
                {
                    _traversed = true;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(RateReferenceEdge item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(RateReferenceEdge[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(RateReferenceEdge item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(RateReferenceEdge item) => throw new NotSupportedException();
        }

        private sealed class PostTraversalMultiCountCollection :
            ICollection<RateReferenceEdge>,
            IReadOnlyCollection<RateReferenceEdge>,
            ICollection
        {
            private readonly RateReferenceEdge[] _items;
            private readonly int _initialGenericCount;
            private readonly int _initialReadOnlyCount;
            private readonly int _initialNonGenericCount;
            private readonly int _postGenericCount;
            private readonly int _postReadOnlyCount;
            private readonly int _postNonGenericCount;
            private bool _traversed;

            internal PostTraversalMultiCountCollection(
                int initialGenericCount,
                int initialReadOnlyCount,
                int initialNonGenericCount,
                int postGenericCount,
                int postReadOnlyCount,
                int postNonGenericCount,
                params RateReferenceEdge[] items)
            {
                _initialGenericCount = initialGenericCount;
                _initialReadOnlyCount = initialReadOnlyCount;
                _initialNonGenericCount = initialNonGenericCount;
                _postGenericCount = postGenericCount;
                _postReadOnlyCount = postReadOnlyCount;
                _postNonGenericCount = postNonGenericCount;
                _items = items;
            }

            int ICollection<RateReferenceEdge>.Count => _traversed ? _postGenericCount : _initialGenericCount;
            int IReadOnlyCollection<RateReferenceEdge>.Count => _traversed ? _postReadOnlyCount : _initialReadOnlyCount;
            int ICollection.Count => _traversed ? _postNonGenericCount : _initialNonGenericCount;
            bool ICollection<RateReferenceEdge>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<RateReferenceEdge> GetEnumerator()
            {
                try
                {
                    for (var index = 0; index < _items.Length; index++)
                        yield return _items[index];
                }
                finally
                {
                    _traversed = true;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            bool ICollection<RateReferenceEdge>.Contains(RateReferenceEdge item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<RateReferenceEdge>.CopyTo(RateReferenceEdge[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
            void ICollection<RateReferenceEdge>.Add(RateReferenceEdge item) => throw new NotSupportedException();
            void ICollection<RateReferenceEdge>.Clear() => throw new NotSupportedException();
            bool ICollection<RateReferenceEdge>.Remove(RateReferenceEdge item) => throw new NotSupportedException();
        }
    }

    internal static class RateReferenceGraphKnownCountStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RateReferenceGraphKnownCountStabilitySmoke.Run();
    }
}
