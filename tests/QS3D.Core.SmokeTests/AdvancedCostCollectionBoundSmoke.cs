using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class AdvancedCostCollectionBoundSmoke
    {
        private const int MaximumEntries = 10000;
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            CountedBuildUpOversizeFailsBeforeEnumeration();
            StreamingBuildUpOversizeStopsAtFirstDisallowedEntry();
            ExactBuildUpBoundaryPreservesArithmetic();
            CountedHistoricalOversizeFailsBeforeEnumeration();
            StreamingHistoricalOversizeStopsAtFirstDisallowedEntry();
            ExactHistoricalBoundaryPreservesQueryBehavior();
        }

        private static void CountedBuildUpOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<CostResourceComponent>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() => BuildUp("BUILDUP-COUNTED-OVERSIZE", source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted rate build-up input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted build-up oversize failure must report the component bound.");
        }

        private static void StreamingBuildUpOversizeStopsAtFirstDisallowedEntry()
        {
            var source = new StreamingComponents(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() => BuildUp("BUILDUP-STREAMING-OVERSIZE", source));

            Equal(
                MaximumEntries + 1,
                source.YieldedCount,
                "Streaming rate build-up ingestion must stop immediately after observing component 10,001.");
            Contains("at most 10000", error.Message, "Streaming build-up oversize failure must report the component bound.");
        }

        private static void ExactBuildUpBoundaryPreservesArithmetic()
        {
            var components = new CostResourceComponent[MaximumEntries];
            for (var i = 0; i < components.Length; i++)
                components[i] = Component(i);

            var buildUp = new CostRateBuildUp(
                "BUILDUP-BOUNDARY",
                new CostCode("CONC"),
                "m3",
                "VND",
                components,
                overheadPercent: 10m,
                profitPercent: 5m);

            Equal(MaximumEntries, buildUp.Components.Count, "Rate build-up must accept exactly 10,000 valid components.");
            Equal(10000m, buildUp.DirectUnitCost, "Boundary-sized rate build-up direct cost changed.");
            Equal(1000m, buildUp.OverheadUnitCost, "Boundary-sized rate build-up overhead cost changed.");
            Equal(550m, buildUp.ProfitUnitCost, "Boundary-sized rate build-up profit cost changed.");
            Equal(11550m, buildUp.UnitRate, "Boundary-sized rate build-up unit rate changed.");
        }

        private static void CountedHistoricalOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<HistoricalCostRecord>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() => new HistoricalCostCatalog(source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted historical catalog input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted historical oversize failure must report the record bound.");
        }

        private static void StreamingHistoricalOversizeStopsAtFirstDisallowedEntry()
        {
            var source = new StreamingHistoricalRecords(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() => new HistoricalCostCatalog(source));

            Equal(
                MaximumEntries + 1,
                source.YieldedCount,
                "Streaming historical catalog ingestion must stop immediately after observing record 10,001.");
            Contains("at most 10000", error.Message, "Streaming historical oversize failure must report the record bound.");
        }

        private static void ExactHistoricalBoundaryPreservesQueryBehavior()
        {
            var records = new HistoricalCostRecord[MaximumEntries];
            for (var i = 0; i < records.Length; i++)
                records[i] = HistoricalRecord(i);

            var catalog = new HistoricalCostCatalog(records);
            Equal(MaximumEntries, catalog.Records.Count, "Historical catalog must accept exactly 10,000 valid records.");

            var matches = catalog.Query("BUILDING", "OFFICE", "VND");
            Equal(MaximumEntries, matches.Count, "Boundary-sized historical catalog query count changed.");
            Equal("HIST-00000", matches[0].RecordId, "Historical query ordering changed at the first record.");
            Equal("HIST-09999", matches[matches.Count - 1].RecordId, "Historical query ordering changed at the final record.");
        }

        private static CostRateBuildUp BuildUp(string id, IEnumerable<CostResourceComponent> components)
        {
            return new CostRateBuildUp(id, new CostCode("CONC"), "m3", "VND", components);
        }

        private static CostResourceComponent Component(int index)
        {
            return new CostResourceComponent(
                "RES-" + index.ToString("D5", CultureInfo.InvariantCulture),
                "Resource " + index.ToString(CultureInfo.InvariantCulture),
                "kg",
                1m,
                1m);
        }

        private static HistoricalCostRecord HistoricalRecord(int index)
        {
            return new HistoricalCostRecord(
                "HIST-" + index.ToString("D5", CultureInfo.InvariantCulture),
                "BUILDING",
                "OFFICE",
                1m,
                index + 1m,
                "VND",
                StartUtc.AddTicks(index));
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

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedNeverEnumerated<T> : IReadOnlyCollection<T>
        {
            internal CountedNeverEnumerated(int count)
            {
                Count = count;
            }

            public int Count { get; }
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Oversized counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingComponents : IEnumerable<CostResourceComponent>
        {
            private readonly int _count;

            internal StreamingComponents(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<CostResourceComponent> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Component(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingHistoricalRecords : IEnumerable<HistoricalCostRecord>
        {
            private readonly int _count;

            internal StreamingHistoricalRecords(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<HistoricalCostRecord> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return HistoricalRecord(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class AdvancedCostCollectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AdvancedCostCollectionBoundSmoke.Run();
        }
    }
}
