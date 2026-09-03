using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CostAnalysisGenerationStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            BuildUpSameCountReplacementIsRejected();
            TradeSameCountReplacementIsRejected();
            StableCountedSourcesReplayExactlyOnce();
            StreamingSourcesRemainSinglePassCompatible();
            Console.WriteLine("PASS cost analysis generation stability");
        }

        private static void BuildUpSameCountReplacementIsRejected()
        {
            var original = new BuildUpRateSnapshot("RATE-A", 10m);
            var replacement = new BuildUpRateSnapshot("RATE-B", 20m);
            var source = new SameCountGenerationCollection<BuildUpRateSnapshot>(
                new[] { original },
                new[] { replacement });
            var references = new RateReferenceGraph(Array.Empty<RateReferenceEdge>());
            var service = new BuildUpAnalysisService();
            ExpectGenerationDrift(
                () => service.Analyze(source, references, adoptedOnly: false),
                "Build-up analysis rate collection",
                "build-up same-count replacement");
        }

        private static void TradeSameCountReplacementIsRejected()
        {
            var original = new TradeCostItem("ITEM-A", "Concrete", 10m);
            var replacement = new TradeCostItem("ITEM-B", "Rebar", 20m);
            var source = new SameCountGenerationCollection<TradeCostItem>(
                new[] { original },
                new[] { replacement });
            var service = new TradeCostAnalysisService();
            ExpectGenerationDrift(
                () => service.Analyze(source, 100m),
                "Trade analysis item collection",
                "trade same-count replacement");
        }

        private static void StableCountedSourcesReplayExactlyOnce()
        {
            var rate = new BuildUpRateSnapshot("RATE-C", 30m);
            var rates = new SameCountGenerationCollection<BuildUpRateSnapshot>(
                new[] { rate },
                new[] { rate });
            var references = new RateReferenceGraph(Array.Empty<RateReferenceEdge>());
            var buildUp = new BuildUpAnalysisService().Analyze(rates, references, adoptedOnly: false);
            Require(rates.GetEnumeratorCalls == 2, "stable counted build-up source must be admitted then replayed exactly once");
            Require(buildUp.Count == 1 && buildUp[0].Rate.RateCode == "RATE-C", "stable counted build-up result changed");

            var item = new TradeCostItem("ITEM-C", "Concrete", 40m);
            var items = new SameCountGenerationCollection<TradeCostItem>(
                new[] { item },
                new[] { item });
            var trade = new TradeCostAnalysisService().Analyze(items, 100m);
            Require(items.GetEnumeratorCalls == 2, "stable counted trade source must be admitted then replayed exactly once");
            Require(trade.Count == 1 && trade[0].ItemCount == 1 && trade[0].TotalCost == 40m, "stable counted trade result changed");
        }

        private static void StreamingSourcesRemainSinglePassCompatible()
        {
            var rates = new SinglePassEnumerable<BuildUpRateSnapshot>(new BuildUpRateSnapshot("RATE-D", 50m));
            var references = new RateReferenceGraph(Array.Empty<RateReferenceEdge>());
            var buildUp = new BuildUpAnalysisService().Analyze(rates, references, adoptedOnly: false);
            Require(rates.GetEnumeratorCalls == 1 && buildUp.Count == 1, "streaming build-up source was replayed unexpectedly");

            var items = new SinglePassEnumerable<TradeCostItem>(new TradeCostItem("ITEM-D", "Concrete", 60m));
            var trade = new TradeCostAnalysisService().Analyze(items, 100m);
            Require(items.GetEnumeratorCalls == 1 && trade.Count == 1, "streaming trade source was replayed unexpectedly");
        }

        private static void ExpectGenerationDrift(Action action, string collectionLabel, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException error)
            {
                var expected = collectionLabel + " content changed during traversal.";
                if (string.Equals(error.Message, expected, StringComparison.Ordinal)) return;
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
                _generations = generations;
                Count = generations[0].Length;
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
                if (GetEnumeratorCalls > 1) throw new InvalidOperationException("Streaming source was enumerated more than once.");
                return ((IEnumerable<T>)_items).GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
