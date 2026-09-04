using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedCostKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            RateBuildUpRejectsPostTraversalCountDrift();
            BuildUpAnalysisRejectsPostTraversalInterfaceConflict();
            BqLibraryRejectsPostTraversalNonGenericCountDrift();
            HistoricalCatalogRejectsStableCountGenerationDrift();
            TenderQuoteLinesRejectStableCountGenerationDrift();
            TenderRequirementsRejectStableCountGenerationDrift();
            TenderBidsRejectStableCountGenerationDrift();
            ProgressContractsRejectStableCountGenerationDrift();
            ProgressClaimsRejectStableCountGenerationDrift();
            HonestMultiInterfaceCountRemainsAccepted();
            AffectedKnownCountControlsRemainAccepted();
            PureStreamingInputRemainsAccepted();
            AffectedStreamingInputRemainsSinglePass();
        }

        private static void RateBuildUpRejectsPostTraversalCountDrift()
        {
            var source = new DriftingReadOnlyCollection<CostResourceComponent>(
                beforeCount: 2,
                afterCount: 3,
                Component(0),
                Component(1));

            AssertStabilityFailure(
                () => new CostRateBuildUp("BUILD-STABLE", new CostCode("CONC"), "m3", "VND", source),
                "Rate build-up must reject deterministic Count drift after a cardinality-matching traversal.");
        }

        private static void BuildUpAnalysisRejectsPostTraversalInterfaceConflict()
        {
            var source = new DriftingMultiCountCollection<BuildUpRateSnapshot>(
                beforeGenericCount: 2,
                beforeReadOnlyCount: 2,
                beforeNonGenericCount: 2,
                afterGenericCount: 2,
                afterReadOnlyCount: 3,
                afterNonGenericCount: 2,
                BuildUpRate(0),
                BuildUpRate(1));
            var references = new RateReferenceGraph(Array.Empty<RateReferenceEdge>());

            AssertStabilityFailure(
                () => new BuildUpAnalysisService().Analyze(source, references, adoptedOnly: false),
                "Build-up analysis must reject Count-interface conflict that appears after traversal.");
        }

        private static void BqLibraryRejectsPostTraversalNonGenericCountDrift()
        {
            var source = new DriftingMultiCountCollection<BqLibraryEntry>(
                beforeGenericCount: 2,
                beforeReadOnlyCount: 2,
                beforeNonGenericCount: 2,
                afterGenericCount: 2,
                afterReadOnlyCount: 2,
                afterNonGenericCount: 4,
                BqEntry(0),
                BqEntry(1));

            AssertStabilityFailure(
                () => new BqLibraryCatalog("LIB-STABLE", source),
                "BQ library must reject non-generic deterministic Count drift after traversal.");
        }

        private static void HistoricalCatalogRejectsStableCountGenerationDrift()
        {
            var source = new GenerationSwitchCollection<HistoricalCostRecord>(
                HistoricalRecord("REC-GEN", 10m),
                HistoricalRecord("REC-GEN", 11m));

            AssertGenerationFailure(
                () => new HistoricalCostCatalog(source),
                "Historical cost catalog must reject same-Count semantic generation drift.");
        }

        private static void TenderQuoteLinesRejectStableCountGenerationDrift()
        {
            var source = new GenerationSwitchCollection<TenderQuoteLine>(
                new TenderQuoteLine("ITEM-GEN", 10m),
                new TenderQuoteLine("ITEM-GEN", 11m));

            AssertGenerationFailure(
                () => new TenderBid("BID-LINE-GEN", "Bidder", "USD", source),
                "Tender quote lines must reject same-Count semantic generation drift.");
        }

        private static void TenderRequirementsRejectStableCountGenerationDrift()
        {
            var source = new GenerationSwitchCollection<TenderRequirement>(
                new TenderRequirement("ITEM-REQ-GEN", "Item", "ea", 1m),
                new TenderRequirement("ITEM-REQ-GEN", "Item", "ea", 2m));

            AssertGenerationFailure(
                () => new TenderEvaluationService().Evaluate(source, Array.Empty<TenderBid>()),
                "Tender requirements must reject same-Count semantic generation drift.");
        }

        private static void TenderBidsRejectStableCountGenerationDrift()
        {
            var source = new GenerationSwitchCollection<TenderBid>(
                new TenderBid("BID-GEN", "Bidder A", "USD", Array.Empty<TenderQuoteLine>()),
                new TenderBid("BID-GEN", "Bidder B", "USD", Array.Empty<TenderQuoteLine>()));

            AssertGenerationFailure(
                () => new TenderEvaluationService().Evaluate(Array.Empty<TenderRequirement>(), source),
                "Tender bids must reject same-Count semantic generation drift.");
        }

        private static void ProgressContractsRejectStableCountGenerationDrift()
        {
            var source = new GenerationSwitchCollection<ProgressContractItem>(
                new ProgressContractItem("ITEM-PROG-GEN", "ea", 10m, 2m),
                new ProgressContractItem("ITEM-PROG-GEN", "ea", 10m, 3m));

            AssertGenerationFailure(
                () => new ProgressClaimService().Evaluate(source, Array.Empty<ProgressClaimLine>()),
                "Progress contracts must reject same-Count semantic generation drift.");
        }

        private static void ProgressClaimsRejectStableCountGenerationDrift()
        {
            var contracts = new[] { new ProgressContractItem("ITEM-CLAIM-GEN", "ea", 10m, 2m) };
            var source = new GenerationSwitchCollection<ProgressClaimLine>(
                new ProgressClaimLine("ITEM-CLAIM-GEN", 0m, 1m),
                new ProgressClaimLine("ITEM-CLAIM-GEN", 0m, 2m));

            AssertGenerationFailure(
                () => new ProgressClaimService().Evaluate(contracts, source),
                "Progress claims must reject same-Count semantic generation drift.");
        }

        private static void HonestMultiInterfaceCountRemainsAccepted()
        {
            var source = new DriftingMultiCountCollection<TradeCostItem>(
                beforeGenericCount: 2,
                beforeReadOnlyCount: 2,
                beforeNonGenericCount: 2,
                afterGenericCount: 2,
                afterReadOnlyCount: 2,
                afterNonGenericCount: 2,
                TradeItem(0),
                TradeItem(1));

            var result = new TradeCostAnalysisService().Analyze(source, 1m);
            Equal(2, result.Count, "Stable multi-interface deterministic Count evidence must remain accepted.");
        }

        private static void AffectedKnownCountControlsRemainAccepted()
        {
            var historical = new HistoricalCostCatalog(new[] { HistoricalRecord("REC-OK", 10m) });
            Equal(1, historical.Records.Count, "Stable historical known-count source changed.");

            var bid = new TenderBid(
                "BID-OK",
                "Bidder",
                "USD",
                new[] { new TenderQuoteLine("ITEM-OK", 2m) });
            var tender = new TenderEvaluationService().Evaluate(
                new[] { new TenderRequirement("ITEM-OK", "Item", "ea", 3m) },
                new[] { bid });
            Equal(6m, tender[0].EvaluatedTotal, "Stable tender known-count source changed.");

            var progress = new ProgressClaimService().Evaluate(
                new[] { new ProgressContractItem("ITEM-OK", "ea", 3m, 2m) },
                new[] { new ProgressClaimLine("ITEM-OK", 0m, 3m) });
            Equal(6m, progress.GrossCertifiedThisPeriod, "Stable progress known-count source changed.");
        }

        private static void PureStreamingInputRemainsAccepted()
        {
            var result = new TradeCostAnalysisService().Analyze(Stream(TradeItem(0), TradeItem(1)), 1m);
            Equal(2, result.Count, "Pure streaming sources without deterministic Count evidence must remain accepted.");
        }

        private static void AffectedStreamingInputRemainsSinglePass()
        {
            var source = new SinglePassEnumerable<HistoricalCostRecord>(HistoricalRecord("REC-STREAM", 10m));
            var catalog = new HistoricalCostCatalog(source);
            Equal(1, catalog.Records.Count, "Streaming historical source changed.");
            Equal(1, source.EnumerationCount, "Unknown-count source must remain single-pass.");
        }

        private static void AssertStabilityFailure(Action action, string message)
        {
            var error = Capture<InvalidOperationException>(action);
            if (error.Message.IndexOf("known count", StringComparison.OrdinalIgnoreCase) < 0 &&
                error.Message.IndexOf("conflicting known counts", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException(message + " Actual: " + error.Message);
            }
        }

        private static void AssertGenerationFailure(Action action, string message)
        {
            var error = Capture<InvalidOperationException>(action);
            if (error.Message.IndexOf("semantic generation replay", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual: " + error.Message);
        }

        private static HistoricalCostRecord HistoricalRecord(string id, decimal totalCost)
        {
            return new HistoricalCostRecord(
                id,
                "BENCH-GEN",
                "DIM-GEN",
                1m,
                totalCost,
                "USD",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        private static CostResourceComponent Component(int index)
        {
            return new CostResourceComponent("RES-STABLE-" + index, "Resource " + index, "kg", 1m, index + 1m);
        }

        private static BuildUpRateSnapshot BuildUpRate(int index)
        {
            return new BuildUpRateSnapshot("RATE-STABLE-" + index, index + 1m);
        }

        private static BqLibraryEntry BqEntry(int index)
        {
            return new BqLibraryEntry("BQ-STABLE-" + index, "BQ item " + index, "m2", "CAT/" + index, index + 1m);
        }

        private static TradeCostItem TradeItem(int index)
        {
            return new TradeCostItem("TRADE-STABLE-" + index, "TRADE-" + index, index + 1m);
        }

        private static IEnumerable<T> Stream<T>(params T[] items)
        {
            for (var i = 0; i < items.Length; i++)
                yield return items[i];
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class GenerationSwitchCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _firstGeneration;
            private readonly T[] _secondGeneration;
            private int _enumerationCount;

            internal GenerationSwitchCollection(T firstGeneration, T secondGeneration)
            {
                _firstGeneration = new[] { firstGeneration };
                _secondGeneration = new[] { secondGeneration };
            }

            public int Count => _firstGeneration.Length;

            public IEnumerator<T> GetEnumerator()
            {
                var generation = _enumerationCount++ == 0 ? _firstGeneration : _secondGeneration;
                for (var i = 0; i < generation.Length; i++)
                    yield return generation[i];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class SinglePassEnumerable<T> : IEnumerable<T>
        {
            private readonly T[] _items;

            internal SinglePassEnumerable(params T[] items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            internal int EnumerationCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationCount++;
                if (EnumerationCount > 1)
                    throw new InvalidOperationException("Streaming source was enumerated more than once.");
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class DriftingReadOnlyCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _beforeCount;
            private readonly int _afterCount;
            private bool _traversed;

            internal DriftingReadOnlyCollection(int beforeCount, int afterCount, params T[] items)
            {
                _beforeCount = beforeCount;
                _afterCount = afterCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count => _traversed ? _afterCount : _beforeCount;

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                    yield return _items[i];
                _traversed = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class DriftingMultiCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private readonly int _beforeGenericCount;
            private readonly int _beforeReadOnlyCount;
            private readonly int _beforeNonGenericCount;
            private readonly int _afterGenericCount;
            private readonly int _afterReadOnlyCount;
            private readonly int _afterNonGenericCount;
            private bool _traversed;

            internal DriftingMultiCountCollection(
                int beforeGenericCount,
                int beforeReadOnlyCount,
                int beforeNonGenericCount,
                int afterGenericCount,
                int afterReadOnlyCount,
                int afterNonGenericCount,
                params T[] items)
            {
                _beforeGenericCount = beforeGenericCount;
                _beforeReadOnlyCount = beforeReadOnlyCount;
                _beforeNonGenericCount = beforeNonGenericCount;
                _afterGenericCount = afterGenericCount;
                _afterReadOnlyCount = afterReadOnlyCount;
                _afterNonGenericCount = afterNonGenericCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            int ICollection<T>.Count => _traversed ? _afterGenericCount : _beforeGenericCount;
            int IReadOnlyCollection<T>.Count => _traversed ? _afterReadOnlyCount : _beforeReadOnlyCount;
            int ICollection.Count => _traversed ? _afterNonGenericCount : _beforeNonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                    yield return _items[i];
                _traversed = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);
        }
    }

    internal static class AdvancedCostKnownCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AdvancedCostKnownCountStabilitySmoke.Run();
        }
    }
}
