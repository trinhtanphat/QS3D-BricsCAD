using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class DeepCostCollectionBoundSmoke
    {
        private const int MaximumEntries = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            KnownCountInputsRejectBeforeEnumeration();
            StreamingInputsStopAtFirstDisallowedEntry();
            ExactBoundariesRemainAccepted();
            OrdinarySemanticsRemainStable();
        }

        private static void KnownCountInputsRejectBeforeEnumeration()
        {
            var edges = new KnownCountCollection<RateReferenceEdge>(MaximumEntries + 1);
            Throws<InvalidOperationException>(() => new RateReferenceGraph(edges));
            Equal(false, edges.EnumerationStarted, "Known-count oversized rate-reference input must fail before enumeration.");

            var rates = new KnownCountCollection<BuildUpRateSnapshot>(MaximumEntries + 1);
            Throws<InvalidOperationException>(() =>
                new BuildUpAnalysisService().Analyze(rates, new RateReferenceGraph(Array.Empty<RateReferenceEdge>())));
            Equal(false, rates.EnumerationStarted, "Known-count oversized build-up analysis input must fail before enumeration.");

            var tradeItems = new KnownCountCollection<TradeCostItem>(MaximumEntries + 1);
            Throws<InvalidOperationException>(() => new TradeCostAnalysisService().Analyze(tradeItems, 1m));
            Equal(false, tradeItems.EnumerationStarted, "Known-count oversized trade-analysis input must fail before enumeration.");

            var libraryEntries = new KnownCountCollection<BqLibraryEntry>(MaximumEntries + 1);
            Throws<InvalidOperationException>(() => new BqLibraryCatalog("LIB", libraryEntries));
            Equal(false, libraryEntries.EnumerationStarted, "Known-count oversized BQ library input must fail before enumeration.");

            var projectEntries = new KnownCountCollection<BqLibraryEntry>(MaximumEntries + 1);
            var catalog = new BqLibraryCatalog("LIB", Array.Empty<BqLibraryEntry>());
            Throws<InvalidOperationException>(() => catalog.ImportFromProject(projectEntries, replaceExisting: true));
            Equal(false, projectEntries.EnumerationStarted, "Known-count oversized BQ import input must fail before enumeration.");
        }

        private static void StreamingInputsStopAtFirstDisallowedEntry()
        {
            var edgeCounter = new ProductionCounter();
            Throws<InvalidOperationException>(() =>
                new RateReferenceGraph(EdgeSequence(MaximumEntries + 2, edgeCounter)));
            Equal(MaximumEntries + 1, edgeCounter.Produced, "Rate-reference streaming bound requested entry 10,002.");

            var rateCounter = new ProductionCounter();
            Throws<InvalidOperationException>(() =>
                new BuildUpAnalysisService().Analyze(
                    RateSequence(MaximumEntries + 2, rateCounter),
                    new RateReferenceGraph(Array.Empty<RateReferenceEdge>())));
            Equal(MaximumEntries + 1, rateCounter.Produced, "Build-up analysis streaming bound requested entry 10,002.");

            var tradeCounter = new ProductionCounter();
            Throws<InvalidOperationException>(() =>
                new TradeCostAnalysisService().Analyze(TradeSequence(MaximumEntries + 2, tradeCounter), 1m));
            Equal(MaximumEntries + 1, tradeCounter.Produced, "Trade-analysis streaming bound requested entry 10,002.");

            var libraryCounter = new ProductionCounter();
            Throws<InvalidOperationException>(() =>
                new BqLibraryCatalog("LIB", LibrarySequence(MaximumEntries + 2, libraryCounter)));
            Equal(MaximumEntries + 1, libraryCounter.Produced, "BQ library streaming bound requested entry 10,002.");

            var importCounter = new ProductionCounter();
            var catalog = new BqLibraryCatalog("LIB", Array.Empty<BqLibraryEntry>());
            Throws<InvalidOperationException>(() =>
                catalog.ImportFromProject(LibrarySequence(MaximumEntries + 2, importCounter), replaceExisting: true));
            Equal(MaximumEntries + 1, importCounter.Produced, "BQ import streaming bound requested entry 10,002.");
        }

        private static void ExactBoundariesRemainAccepted()
        {
            var edges = new List<RateReferenceEdge>(MaximumEntries);
            for (var i = 0; i < MaximumEntries; i++)
                edges.Add(new RateReferenceEdge("R", RateReferenceTargetKind.BillItem, TargetId(i)));
            var graph = new RateReferenceGraph(edges);
            Equal(MaximumEntries, graph.Edges.Count, "Rate-reference exact boundary changed.");
            Equal("T00000", graph.Edges[0].TargetId, "Rate-reference boundary ordering changed.");
            Equal("T09999", graph.Edges[MaximumEntries - 1].TargetId, "Rate-reference terminal ordering changed.");

            var rates = new List<BuildUpRateSnapshot>(MaximumEntries);
            for (var i = 0; i < MaximumEntries; i++)
                rates.Add(new BuildUpRateSnapshot(RateCode(i), 1m));
            var analysis = new BuildUpAnalysisService().Analyze(
                rates,
                new RateReferenceGraph(Array.Empty<RateReferenceEdge>()),
                adoptedOnly: false);
            Equal(MaximumEntries, analysis.Count, "Build-up analysis exact boundary changed.");
            Equal("R00000", analysis[0].Rate.RateCode, "Build-up analysis boundary ordering changed.");
            Equal("R09999", analysis[MaximumEntries - 1].Rate.RateCode, "Build-up analysis terminal ordering changed.");

            var tradeItems = new List<TradeCostItem>(MaximumEntries);
            for (var i = 0; i < MaximumEntries; i++)
                tradeItems.Add(new TradeCostItem(ItemCode(i), "Trade", 1m));
            var tradeRows = new TradeCostAnalysisService().Analyze(tradeItems, 100m);
            Equal(1, tradeRows.Count, "Trade-analysis exact boundary row count changed.");
            Equal(MaximumEntries, tradeRows[0].ItemCount, "Trade-analysis exact boundary item count changed.");
            Equal((decimal)MaximumEntries, tradeRows[0].TotalCost, "Trade-analysis exact boundary total changed.");
            Equal(100m, tradeRows[0].CostPerCfaM2, "Trade-analysis exact boundary cost/CFA changed.");

            var entries = new List<BqLibraryEntry>(MaximumEntries);
            for (var i = 0; i < MaximumEntries; i++)
                entries.Add(new BqLibraryEntry(ItemCode(i), "Item " + i, "ea", "Category"));
            var catalog = new BqLibraryCatalog("LIB", entries);
            Equal(MaximumEntries, catalog.Entries.Count, "BQ library exact boundary changed.");

            var imported = new BqLibraryCatalog("IMPORT", Array.Empty<BqLibraryEntry>())
                .ImportFromProject(entries, replaceExisting: true);
            Equal(MaximumEntries, imported.Entries.Count, "BQ import exact boundary changed.");
        }

        private static void OrdinarySemanticsRemainStable()
        {
            Throws<ArgumentException>(() =>
                new RateReferenceGraph(new[]
                {
                    new RateReferenceEdge("R", RateReferenceTargetKind.BillItem, "A"),
                    new RateReferenceEdge("r", RateReferenceTargetKind.BillItem, "a")
                }));

            var graph = new RateReferenceGraph(new[]
            {
                new RateReferenceEdge("USED", RateReferenceTargetKind.UnitRate, "Z"),
                new RateReferenceEdge("used", RateReferenceTargetKind.BillItem, "A")
            });
            var mark = graph.GetMark("UsEd");
            Equal(true, mark.UsedInBillItems, "Rate-reference bill-item mark changed.");
            Equal(true, mark.UsedInUnitRates, "Rate-reference unit-rate mark changed.");

            var analysis = new BuildUpAnalysisService().Analyze(
                new[]
                {
                    new BuildUpRateSnapshot("UNUSED", 3m),
                    new BuildUpRateSnapshot("USED", 2m)
                },
                graph);
            Equal(1, analysis.Count, "Adopted-only build-up analysis changed.");
            Equal("USED", analysis[0].Rate.RateCode, "Adopted build-up identity changed.");
            Equal("A", analysis[0].BillItems[0], "Build-up reverse bill-item reference changed.");
            Equal("Z", analysis[0].UnitRates[0], "Build-up reverse unit-rate reference changed.");

            Throws<ArgumentException>(() =>
                new TradeCostAnalysisService().Analyze(
                    new[]
                    {
                        new TradeCostItem("A", "Concrete", 1m),
                        new TradeCostItem("a", "Steel", 1m)
                    },
                    1m));

            var tradeRows = new TradeCostAnalysisService().Analyze(
                new[]
                {
                    new TradeCostItem("A", "Steel", 4m),
                    new TradeCostItem("B", "Concrete", 6m),
                    new TradeCostItem("C", "steel", 2m)
                },
                2m);
            Equal(2, tradeRows.Count, "Trade aggregation row count changed.");
            Equal("Concrete", tradeRows[0].TradeCode, "Trade deterministic ordering changed.");
            Equal(6m, tradeRows[0].TotalCost, "Concrete trade total changed.");
            Equal("Steel", tradeRows[1].TradeCode, "Case-insensitive trade grouping changed.");
            Equal(2, tradeRows[1].ItemCount, "Steel trade item count changed.");
            Equal(6m, tradeRows[1].TotalCost, "Steel trade total changed.");
            Equal(3m, tradeRows[1].CostPerCfaM2, "Steel trade cost/CFA changed.");

            var catalog = new BqLibraryCatalog("LIB", new[]
            {
                new BqLibraryEntry("B", "Second", "ea", "Z", 2m),
                new BqLibraryEntry("A", "First", "ea", "A", 1m)
            });
            Equal("A", catalog.Entries[0].ItemCode, "BQ category/item ordering changed.");
            Throws<InvalidOperationException>(() =>
                catalog.ImportFromProject(
                    new[] { new BqLibraryEntry("a", "Replacement", "ea", "A", 5m) },
                    replaceExisting: false));

            var replaced = catalog.ImportFromProject(
                new[] { new BqLibraryEntry("a", "Replacement", "ea", "A", 5m) },
                replaceExisting: true);
            Equal(2, replaced.Entries.Count, "BQ replacement changed catalog size.");
            Equal("Replacement", replaced.Entries[0].Description, "BQ replacement semantics changed.");
            Equal(5m, replaced.Entries[0].ReferenceUnitRate, "BQ replacement unit rate changed.");
        }

        private static IEnumerable<RateReferenceEdge> EdgeSequence(int count, ProductionCounter counter)
        {
            for (var i = 0; i < count; i++)
            {
                counter.Produced++;
                yield return new RateReferenceEdge("R", RateReferenceTargetKind.BillItem, TargetId(i));
            }
        }

        private static IEnumerable<BuildUpRateSnapshot> RateSequence(int count, ProductionCounter counter)
        {
            for (var i = 0; i < count; i++)
            {
                counter.Produced++;
                yield return new BuildUpRateSnapshot(RateCode(i), 1m);
            }
        }

        private static IEnumerable<TradeCostItem> TradeSequence(int count, ProductionCounter counter)
        {
            for (var i = 0; i < count; i++)
            {
                counter.Produced++;
                yield return new TradeCostItem(ItemCode(i), "Trade", 1m);
            }
        }

        private static IEnumerable<BqLibraryEntry> LibrarySequence(int count, ProductionCounter counter)
        {
            for (var i = 0; i < count; i++)
            {
                counter.Produced++;
                yield return new BqLibraryEntry(ItemCode(i), "Item " + i, "ea", "Category", 1m);
            }
        }

        private static string RateCode(int index) => "R" + index.ToString("D5");
        private static string TargetId(int index) => "T" + index.ToString("D5");
        private static string ItemCode(int index) => "I" + index.ToString("D5");

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private sealed class ProductionCounter
        {
            internal int Produced { get; set; }
        }

        private sealed class KnownCountCollection<T> : ICollection<T>
        {
            private readonly int _count;

            internal KnownCountCollection(int count)
            {
                _count = count;
            }

            internal bool EnumerationStarted { get; private set; }
            public int Count => _count;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationStarted = true;
                throw new InvalidOperationException("Enumeration should not start for a rejected known-count collection.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
