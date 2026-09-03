using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TbqWorkspaceGenerationStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SameCountBillItemReplacementIsRejected();
            SameCountBillItemSemanticDriftIsRejected();
            SameCountBillItemReorderingIsRejected();
            SameCountBuildUpReplacementIsRejected();
            SameCountBuildUpReorderingIsRejected();
            StableCountedSourcesReplayExactlyOnce();
            StreamingSourcesRemainSinglePassCompatible();
            Console.WriteLine("PASS TBQ workspace generation stability");
        }

        private static void SameCountBillItemReplacementIsRejected()
        {
            var original = Bill("BILL-A", "Original", "m2", "Trade A", 2m, 10m, "RATE-A");
            var replacement = Bill("BILL-B", "Replacement", "m2", "Trade B", 3m, 20m, "RATE-B");
            RequireBillGenerationRejected(new[] { original }, new[] { replacement },
                "same-count TBQ bill item replacement must fail closed");
        }

        private static void SameCountBillItemSemanticDriftIsRejected()
        {
            var original = Bill("BILL-C", "Original", "m2", "Trade C", 2m, 10m, "RATE-C");
            var replacements = new[]
            {
                Bill("BILL-D", "Original", "m2", "Trade C", 2m, 10m, "RATE-C"),
                Bill("BILL-C", "Changed", "m2", "Trade C", 2m, 10m, "RATE-C"),
                Bill("BILL-C", "Original", "m3", "Trade C", 2m, 10m, "RATE-C"),
                Bill("BILL-C", "Original", "m2", "Trade D", 2m, 10m, "RATE-C"),
                Bill("BILL-C", "Original", "m2", "Trade C", 3m, 10m, "RATE-C"),
                Bill("BILL-C", "Original", "m2", "Trade C", 2m, 11m, "RATE-C"),
                Bill("BILL-C", "Original", "m2", "Trade C", 2m, 10m, "RATE-D"),
            };

            for (var i = 0; i < replacements.Length; i++)
            {
                RequireBillGenerationRejected(new[] { original }, new[] { replacements[i] },
                    "TBQ bill item semantic generation drift at field index " + i + " must fail closed");
            }
        }

        private static void SameCountBillItemReorderingIsRejected()
        {
            var first = Bill("BILL-E", "First", "m2", "Trade E", 1m, 10m, "RATE-E");
            var second = Bill("BILL-F", "Second", "m2", "Trade F", 2m, 20m, "RATE-F");
            RequireBillGenerationRejected(new[] { first, second }, new[] { second, first },
                "same-count TBQ bill item reordering must fail closed");
        }

        private static void SameCountBuildUpReplacementIsRejected()
        {
            var original = new BuildUpRateSnapshot("RATE-G", 10m);
            var replacement = new BuildUpRateSnapshot("RATE-H", 20m);
            RequireBuildUpGenerationRejected(new[] { original }, new[] { replacement },
                "same-count TBQ build-up replacement must fail closed");

            var rateDrift = new BuildUpRateSnapshot("RATE-G", 11m);
            RequireBuildUpGenerationRejected(new[] { original }, new[] { rateDrift },
                "same-count TBQ build-up unit-rate drift must fail closed");
        }

        private static void SameCountBuildUpReorderingIsRejected()
        {
            var first = new BuildUpRateSnapshot("RATE-I", 10m);
            var second = new BuildUpRateSnapshot("RATE-J", 20m);
            RequireBuildUpGenerationRejected(new[] { first, second }, new[] { second, first },
                "same-count TBQ build-up reordering must fail closed");
        }

        private static void StableCountedSourcesReplayExactlyOnce()
        {
            var bill = Bill("BILL-K", "Stable", "m2", "Trade K", 2m, 15m, "RATE-K");
            var bills = new SameCountGenerationCollection<TbqBillItem>(new[] { bill }, new[] { bill });
            var rate = new BuildUpRateSnapshot("RATE-K", 15m);
            var rates = new SameCountGenerationCollection<BuildUpRateSnapshot>(new[] { rate }, new[] { rate });

            var workspace = CreateWorkspace(bills, rates);
            Require(bills.GetEnumeratorCalls == 2, "stable counted TBQ bill items must be admitted then replayed exactly once");
            Require(rates.GetEnumeratorCalls == 2, "stable counted TBQ build-up rates must be admitted then replayed exactly once");
            Require(workspace.BillItems.Count == 1 && workspace.BillItems[0].ItemCode == "BILL-K", "stable TBQ bill snapshot changed");
            Require(workspace.BuildUpRates.Count == 1 && workspace.BuildUpRates[0].RateCode == "RATE-K", "stable TBQ build-up snapshot changed");
            Require(workspace.BaseTotal == 30m, "stable TBQ workspace commercial total changed");
        }

        private static void StreamingSourcesRemainSinglePassCompatible()
        {
            var bill = Bill("BILL-L", "Streaming", "m2", "Trade L", 3m, 12m, "RATE-L");
            var bills = new SinglePassEnumerable<TbqBillItem>(bill);
            var rate = new BuildUpRateSnapshot("RATE-L", 12m);
            var rates = new SinglePassEnumerable<BuildUpRateSnapshot>(rate);

            var workspace = CreateWorkspace(bills, rates);
            Require(bills.GetEnumeratorCalls == 1, "streaming TBQ bill items were replayed unexpectedly");
            Require(rates.GetEnumeratorCalls == 1, "streaming TBQ build-up rates were replayed unexpectedly");
            Require(workspace.BaseTotal == 36m, "streaming TBQ workspace result changed");
        }

        private static void RequireBillGenerationRejected(
            IReadOnlyList<TbqBillItem> first,
            IReadOnlyList<TbqBillItem> second,
            string message)
        {
            var source = new SameCountGenerationCollection<TbqBillItem>(first, second);
            var threw = false;
            try
            {
                _ = CreateWorkspace(source, Array.Empty<BuildUpRateSnapshot>());
            }
            catch (InvalidOperationException ex)
            {
                threw = ex.Message.Contains("TBQ workspace bill item source content changed during traversal.", StringComparison.Ordinal);
            }
            Require(threw, message);
        }

        private static void RequireBuildUpGenerationRejected(
            IReadOnlyList<BuildUpRateSnapshot> first,
            IReadOnlyList<BuildUpRateSnapshot> second,
            string message)
        {
            var source = new SameCountGenerationCollection<BuildUpRateSnapshot>(first, second);
            var threw = false;
            try
            {
                _ = CreateWorkspace(Array.Empty<TbqBillItem>(), source);
            }
            catch (InvalidOperationException ex)
            {
                threw = ex.Message.Contains("TBQ workspace build-up rate source content changed during traversal.", StringComparison.Ordinal);
            }
            Require(threw, message);
        }

        private static TbqProjectWorkspaceState CreateWorkspace(
            IEnumerable<TbqBillItem> bills,
            IEnumerable<BuildUpRateSnapshot> rates)
        {
            return new TbqProjectWorkspaceState(
                "USD",
                100m,
                bills,
                rates,
                Array.Empty<RateReferenceEdge>(),
                "LIB-1",
                Array.Empty<BqLibraryEntry>());
        }

        private static TbqBillItem Bill(
            string itemCode,
            string description,
            string unit,
            string tradeCode,
            decimal quantity,
            decimal unitRate,
            string rateCode)
        {
            return new TbqBillItem(itemCode, description, unit, tradeCode, quantity, unitRate, rateCode);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class SameCountGenerationCollection<T> : ICollection<T>
        {
            private readonly IReadOnlyList<T> _first;
            private readonly IReadOnlyList<T> _second;

            internal SameCountGenerationCollection(IReadOnlyList<T> first, IReadOnlyList<T> second)
            {
                if (first.Count != second.Count) throw new ArgumentException("Generations must have equal Count.");
                _first = first;
                _second = second;
            }

            public int GetEnumeratorCalls { get; private set; }
            public int Count => _first.Count;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return (GetEnumeratorCalls == 1 ? _first : _second).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class SinglePassEnumerable<T> : IEnumerable<T>
        {
            private readonly T _item;
            internal SinglePassEnumerable(T item) => _item = item;
            public int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                if (GetEnumeratorCalls != 1) throw new InvalidOperationException("streaming source was enumerated more than once");
                yield return _item;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
