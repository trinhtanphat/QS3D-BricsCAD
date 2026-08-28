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
            HonestMultiInterfaceCountRemainsAccepted();
            PureStreamingInputRemainsAccepted();
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

        private static void PureStreamingInputRemainsAccepted()
        {
            var result = new TradeCostAnalysisService().Analyze(Stream(TradeItem(0), TradeItem(1)), 1m);
            Equal(2, result.Count, "Pure streaming sources without deterministic Count evidence must remain accepted.");
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
