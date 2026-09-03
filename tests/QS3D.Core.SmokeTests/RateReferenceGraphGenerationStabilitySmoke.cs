using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateReferenceGraphGenerationStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SameCountReplacementIsRejected();
            SameCountReorderIsRejected();
            StableCountedGenerationRemainsAccepted();
            StreamingInputRemainsSinglePassCompatible();
            Console.WriteLine("PASS rate reference graph generation stability");
        }

        private static void SameCountReplacementIsRejected()
        {
            var admittedA = Edge("RATE-A", RateReferenceTargetKind.BillItem, "ITEM-1");
            var admittedB = Edge("RATE-B", RateReferenceTargetKind.UnitRate, "RATE-X");
            var replacement = Edge("RATE-C", RateReferenceTargetKind.UnitRate, "RATE-Y");
            var source = new SameCountGenerationCollection<RateReferenceEdge>(
                new[] { admittedA, admittedB },
                new[] { admittedA, replacement });

            RateReferenceGraph? published = null;
            ExpectContentDrift(() => published = new RateReferenceGraph(source), "same-count edge replacement");
            Require(published == null, "replacement drift published a partial graph");
        }

        private static void SameCountReorderIsRejected()
        {
            var a = Edge("RATE-D", RateReferenceTargetKind.BillItem, "ITEM-2");
            var b = Edge("RATE-E", RateReferenceTargetKind.UnitRate, "RATE-Z");
            var source = new SameCountGenerationCollection<RateReferenceEdge>(
                new[] { a, b },
                new[] { b, a });

            ExpectContentDrift(() => new RateReferenceGraph(source), "same-count edge reorder");
        }

        private static void StableCountedGenerationRemainsAccepted()
        {
            var a = Edge("RATE-F", RateReferenceTargetKind.BillItem, "ITEM-3");
            var b = Edge("RATE-G", RateReferenceTargetKind.UnitRate, "RATE-H");
            var source = new SameCountGenerationCollection<RateReferenceEdge>(
                new[] { b, a },
                new[] { b, a });

            var graph = new RateReferenceGraph(source);
            Require(source.GetEnumeratorCalls == 2, "stable counted edge source must be admitted then replayed exactly once");
            Require(graph.Edges.Count == 2, "stable counted edge source changed cardinality");
            Require(graph.Edges[0].SourceRateCode == "RATE-F" && graph.Edges[1].SourceRateCode == "RATE-G",
                "stable counted graph lost canonical ordering");
        }

        private static void StreamingInputRemainsSinglePassCompatible()
        {
            var source = new SinglePassEnumerable<RateReferenceEdge>(
                Edge("RATE-I", RateReferenceTargetKind.BillItem, "ITEM-4"),
                Edge("RATE-J", RateReferenceTargetKind.UnitRate, "RATE-K"));

            var graph = new RateReferenceGraph(source);
            Require(source.GetEnumeratorCalls == 1, "raw streaming edge source was replayed unexpectedly");
            Require(graph.Edges.Count == 2, "raw streaming edge source lost edges");
        }

        private static RateReferenceEdge Edge(string source, RateReferenceTargetKind kind, string target) =>
            new RateReferenceEdge(source, kind, target);

        private static void ExpectContentDrift(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException error)
            {
                if (string.Equals(error.Message, "Rate reference edge source content changed during traversal.", StringComparison.Ordinal))
                    return;
                throw new InvalidOperationException(label + " failed for the wrong reason: " + error.Message, error);
            }

            throw new InvalidOperationException(label + " was accepted unexpectedly.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class SameCountGenerationCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[][] _generations;
            private int _enumerationIndex;

            internal SameCountGenerationCollection(params T[][] generations)
            {
                if (generations == null || generations.Length == 0)
                    throw new ArgumentException("At least one generation is required.", nameof(generations));
                _generations = generations;
                Count = generations[0].Length;
                for (var i = 1; i < generations.Length; i++)
                {
                    if (generations[i].Length != Count)
                        throw new ArgumentException("All generations must preserve Count.", nameof(generations));
                }
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                var index = _enumerationIndex < _generations.Length ? _enumerationIndex++ : _generations.Length - 1;
                return ((IEnumerable<T>)_generations[index]).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class SinglePassEnumerable<T> : IEnumerable<T>
        {
            private readonly T[] _items;
            internal SinglePassEnumerable(params T[] items) { _items = items; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                if (GetEnumeratorCalls > 1)
                    throw new InvalidOperationException("Streaming source was enumerated more than once.");
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
