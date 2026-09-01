using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedCostTransientCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateBuildUpRejectsTransientCountBeforeCurrent();
            HistoricalCatalogRejectsTransientCountBeforeCurrent();
            TenderBidRejectsTransientCountBeforeCurrent();
            StableCountedAndStreamingControlsSucceed();
            Console.WriteLine("PASS advanced cost transient Count stability");
        }

        private static void RateBuildUpRejectsTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<CostResourceComponent>(
                new CostResourceComponent("R-TRANSIENT", "Transient resource", "m", 1m, 10m),
                2);

            ExpectInvalidOperation(
                () => new CostRateBuildUp(
                    "BUILDUP-TRANSIENT",
                    new CostCode("CONC"),
                    "m3",
                    "VND",
                    source),
                "rate build-up transient Count growth");
            Require(source.CurrentReads == 0,
                "rate build-up must reject transient Count growth before reading Current");
        }

        private static void HistoricalCatalogRejectsTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<HistoricalCostRecord>(
                new HistoricalCostRecord(
                    "HIST-TRANSIENT",
                    "BUILDING",
                    "OFFICE",
                    1m,
                    10m,
                    "VND",
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                0);

            ExpectInvalidOperation(
                () => new HistoricalCostCatalog(source),
                "historical catalog transient Count shrink");
            Require(source.CurrentReads == 0,
                "historical catalog must reject transient Count shrink before reading Current");
        }

        private static void TenderBidRejectsTransientCountBeforeCurrent()
        {
            var source = new TransientCountCollection<TenderQuoteLine>(
                new TenderQuoteLine("ITEM-TRANSIENT", 5m),
                -1);

            ExpectInvalidOperation(
                () => new TenderBid("BID-TRANSIENT", "Transient bidder", "VND", source),
                "tender bid transient negative Count");
            Require(source.CurrentReads == 0,
                "tender bid must reject transient negative Count before reading Current");
        }

        private static void StableCountedAndStreamingControlsSucceed()
        {
            var stableBuildUp = new CostRateBuildUp(
                "BUILDUP-STABLE",
                new CostCode("STEEL"),
                "kg",
                "VND",
                new[] { new CostResourceComponent("R-STABLE", "Stable resource", "kg", 2m, 3m) });
            Require(stableBuildUp.Components.Count == 1,
                "stable counted rate build-up control must succeed");

            var streamingCatalog = new HistoricalCostCatalog(StreamHistoricalRecords());
            Require(streamingCatalog.Records.Count == 1,
                "pure streaming historical catalog control must succeed");

            var stableBid = new TenderBid(
                "BID-STABLE",
                "Stable bidder",
                "VND",
                new[] { new TenderQuoteLine("ITEM-STABLE", 7m) });
            Require(stableBid.Lines.Count == 1,
                "stable counted tender bid control must succeed");
        }

        private static IEnumerable<HistoricalCostRecord> StreamHistoricalRecords()
        {
            yield return new HistoricalCostRecord(
                "HIST-STREAM",
                "BUILDING",
                "OFFICE",
                2m,
                30m,
                "VND",
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        private static void ExpectInvalidOperation(Action action, string label)
        {
            try
            {
                action();
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
