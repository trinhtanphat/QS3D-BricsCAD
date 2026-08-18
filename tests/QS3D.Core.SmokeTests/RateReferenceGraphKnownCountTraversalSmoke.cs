using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateReferenceGraphKnownCountTraversalSmoke
    {
        internal static void Run()
        {
            UnderEnumerationFailsClosed();
            OverEnumerationFailsClosed();
            HonestCountedInputStillAccepted();
            PureStreamingInputStillAccepted();
        }

        private static void UnderEnumerationFailsClosed()
        {
            var source = new CountMismatchCollection(
                2,
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A"));

            Throws<ArgumentException>(
                () => new RateReferenceGraph(source),
                "under-enumerating counted RateReferenceGraph source");
        }

        private static void OverEnumerationFailsClosed()
        {
            var source = new CountMismatchCollection(
                1,
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A"),
                Edge("RATE-B", RateReferenceTargetKind.UnitRate, "RATE-C"));

            Throws<ArgumentException>(
                () => new RateReferenceGraph(source),
                "over-enumerating counted RateReferenceGraph source");
        }

        private static void HonestCountedInputStillAccepted()
        {
            var graph = new RateReferenceGraph(new List<RateReferenceEdge>
            {
                Edge("RATE-B", RateReferenceTargetKind.UnitRate, "RATE-C"),
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A")
            });

            Equal(2, graph.Edges.Count, "Honest counted RateReferenceGraph edge count changed.");
            Equal("RATE-A", graph.Edges[0].SourceRateCode, "Honest counted RateReferenceGraph sorting changed.");
        }

        private static void PureStreamingInputStillAccepted()
        {
            var graph = new RateReferenceGraph(Yield(
                Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-A")));

            Equal(1, graph.Edges.Count, "Pure streaming RateReferenceGraph edge count changed.");
        }

        private static RateReferenceEdge Edge(
            string sourceRateCode,
            RateReferenceTargetKind targetKind,
            string targetId)
        {
            return new RateReferenceEdge(sourceRateCode, targetKind, targetId);
        }

        private static IEnumerable<RateReferenceEdge> Yield(RateReferenceEdge edge)
        {
            yield return edge;
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception(
                "RateReferenceGraphKnownCountTraversalSmoke " + label +
                ": expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountMismatchCollection : ICollection<RateReferenceEdge>
        {
            private readonly RateReferenceEdge[] _items;

            internal CountMismatchCollection(int advertisedCount, params RateReferenceEdge[] items)
            {
                Count = advertisedCount;
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
    }

    internal static class RateReferenceGraphKnownCountTraversalSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RateReferenceGraphKnownCountTraversalSmoke.Run();
    }
}
