using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class DeepCostTransientCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateReferenceGraphRejectsTransientCountBeforeCurrent();
            BuildUpAnalysisRejectsTransientCountBeforeCurrent();
            TradeAnalysisRejectsTransientCountBeforeCurrent();
            BqLibraryRejectsTransientCountBeforeCurrent();
            BqProjectImportRejectsTransientCountBeforeCurrent();
            StableCountedAndStreamingControlsSucceed();
            Console.WriteLine("PASS deep cost transient Count stability");
        }

        private static void RateReferenceGraphRejectsTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<RateReferenceEdge>(
                new RateReferenceEdge("RATE-EDGE", RateReferenceTargetKind.BillItem, "ITEM-EDGE"),
                2);

            ExpectCountFailure(() => new RateReferenceGraph(source), "rate-reference transient Count growth");
            Require(source.CurrentReads == 0,
                "rate-reference graph must reject transient Count growth before reading Current");
        }

        private static void BuildUpAnalysisRejectsTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<BuildUpRateSnapshot>(
                new BuildUpRateSnapshot("RATE-BUILDUP", 10m),
                0);
            var references = new RateReferenceGraph(Array.Empty<RateReferenceEdge>());

            ExpectCountFailure(
                () => new BuildUpAnalysisService().Analyze(source, references, adoptedOnly: false),
                "build-up analysis transient Count shrink");
            Require(source.CurrentReads == 0,
                "build-up analysis must reject transient Count shrink before reading Current");
        }

        private static void TradeAnalysisRejectsTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<TradeCostItem>(
                new TradeCostItem("ITEM-TRADE", "Trade", 5m),
                -1);

            ExpectCountFailure(
                () => new TradeCostAnalysisService().Analyze(source, 1m),
                "trade analysis transient negative Count");
            Require(source.CurrentReads == 0,
                "trade analysis must reject transient negative Count before reading Current");
        }

        private static void BqLibraryRejectsTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<BqLibraryEntry>(
                Entry("ITEM-LIBRARY"),
                2);

            ExpectCountFailure(
                () => new BqLibraryCatalog("LIB-TRANSIENT", source),
                "BQ library transient Count growth");
            Require(source.CurrentReads == 0,
                "BQ library must reject transient Count growth before reading Current");
        }

        private static void BqProjectImportRejectsTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<BqLibraryEntry>(
                Entry("ITEM-IMPORT"),
                0);
            var catalog = new BqLibraryCatalog("LIB-IMPORT", Array.Empty<BqLibraryEntry>());

            ExpectCountFailure(
                () => catalog.ImportFromProject(source, replaceExisting: false),
                "BQ project import transient Count shrink");
            Require(source.CurrentReads == 0,
                "BQ project import must reject transient Count shrink before reading Current");
        }

        private static void StableCountedAndStreamingControlsSucceed()
        {
            var graph = new RateReferenceGraph(new[]
            {
                new RateReferenceEdge("RATE-STABLE", RateReferenceTargetKind.BillItem, "ITEM-STABLE")
            });
            Require(graph.Edges.Count == 1, "stable counted rate-reference control must succeed");

            var buildUp = new BuildUpAnalysisService().Analyze(
                new[] { new BuildUpRateSnapshot("RATE-STABLE", 2m) },
                graph,
                adoptedOnly: false);
            Require(buildUp.Count == 1, "stable counted build-up control must succeed");

            var tradeRows = new TradeCostAnalysisService().Analyze(StreamTradeItems(), 1m);
            Require(tradeRows.Count == 1, "pure streaming trade-analysis control must succeed");

            var catalog = new BqLibraryCatalog("LIB-STABLE", new[] { Entry("ITEM-STABLE") });
            Require(catalog.Entries.Count == 1, "stable counted BQ-library control must succeed");

            var imported = new BqLibraryCatalog("LIB-STREAM", Array.Empty<BqLibraryEntry>())
                .ImportFromProject(StreamBqEntries(), replaceExisting: false);
            Require(imported.Entries.Count == 1, "pure streaming BQ-import control must succeed");
        }

        private static BqLibraryEntry Entry(string itemCode) =>
            new BqLibraryEntry(itemCode, "Description", "m", "Category", 1m);

        private static IEnumerable<TradeCostItem> StreamTradeItems()
        {
            yield return new TradeCostItem("ITEM-STREAM", "Trade", 3m);
        }

        private static IEnumerable<BqLibraryEntry> StreamBqEntries()
        {
            yield return Entry("ITEM-STREAM");
        }

        private static void ExpectCountFailure(Action action, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException(label + " was accepted unexpectedly.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class TransientCountCollection<T> : ICollection<T>
        {
            private readonly T _item;
            private readonly int _transientCount;
            private bool _emitTransientCount;

            internal TransientCountCollection(T item, int transientCount)
            {
                _item = item;
                _transientCount = transientCount;
            }

            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    if (_emitTransientCount)
                    {
                        _emitTransientCount = false;
                        return _transientCount;
                    }
                    return 1;
                }
            }

            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
            public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly TransientCountCollection<T> _owner;
                private int _state;

                internal Enumerator(TransientCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    if (_state != 0)
                    {
                        _state = 2;
                        return false;
                    }

                    _state = 1;
                    _owner._emitTransientCount = true;
                    return true;
                }

                public T Current
                {
                    get
                    {
                        if (_state != 1) throw new InvalidOperationException();
                        _owner.CurrentReads++;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
