using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedCostKnownCountCurrentIntegritySmoke
    {
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            RateBuildUpRejectsBeforeUnexpectedCurrent();
            HistoricalCatalogRejectsBeforeUnexpectedCurrent();
        }

        private static void RateBuildUpRejectsBeforeUnexpectedCurrent()
        {
            var source = new CountedCurrentProbe<CostResourceComponent>(
                1,
                Component(0),
                Component(1));

            var error = Capture<InvalidOperationException>(() =>
                new CostRateBuildUp("BUILDUP-CURRENT", new CostCode("CONC"), "m3", "VND", source));

            Contains("known count reported", error.Message,
                "Rate build-up must reject the Count overrun contract.");
            Equal(2, source.MoveNextCalls,
                "Rate build-up must observe the successful N+1 MoveNext that establishes the overrun.");
            Equal(1, source.CurrentReads,
                "Rate build-up must reject N+1 before observing caller-controlled Current.");
        }

        private static void HistoricalCatalogRejectsBeforeUnexpectedCurrent()
        {
            var source = new CountedCurrentProbe<HistoricalCostRecord>(
                1,
                Historical(0),
                Historical(1));

            var error = Capture<InvalidOperationException>(() => new HistoricalCostCatalog(source));

            Contains("known count reported", error.Message,
                "Historical catalog must reject the Count overrun contract.");
            Equal(2, source.MoveNextCalls,
                "Historical catalog must observe the successful N+1 MoveNext that establishes the overrun.");
            Equal(1, source.CurrentReads,
                "Historical catalog must reject N+1 before observing caller-controlled Current.");
        }

        private static CostResourceComponent Component(int index) =>
            new CostResourceComponent("CURRENT-RES-" + index, "Resource " + index, "kg", 1m, 1m);

        private static HistoricalCostRecord Historical(int index) =>
            new HistoricalCostRecord(
                "CURRENT-HIST-" + index,
                "BUILDING",
                "OFFICE",
                1m,
                index + 1m,
                "VND",
                StartUtc.AddTicks(index));

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

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedCurrentProbe<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;

            internal CountedCurrentProbe(int advertisedCount, params T[] items)
            {
                Count = advertisedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count { get; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CountedCurrentProbe<T> _owner;
                private int _index = -1;

                internal Enumerator(CountedCurrentProbe<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_index < _owner._items.Length)
                        return true;
                    throw new InvalidOperationException("Probe advanced beyond the unexpected N+1 item.");
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class AdvancedCostKnownCountCurrentIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AdvancedCostKnownCountCurrentIntegritySmoke.Run();
        }
    }
}
