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
            ConflictingBuildUpHiddenOversizeFailsBeforeEnumeration();
            ConflictingBuildUpInBoundCountsFailBeforeEnumeration();
            StreamingBuildUpOversizeStopsAtFirstDisallowedEntry();
            ExactBuildUpBoundaryPreservesArithmetic();
            CountedHistoricalOversizeFailsBeforeEnumeration();
            StreamingHistoricalOversizeStopsAtFirstDisallowedEntry();
            ExactHistoricalBoundaryPreservesQueryBehavior();
            CountedProgressContractOversizeFailsBeforeEnumeration();
            CountedProgressClaimOversizeFailsBeforeEnumeration();
            StreamingProgressContractOversizeStopsAtFirstDisallowedEntry();
            StreamingProgressClaimOversizeStopsAtFirstDisallowedEntry();
            ExactProgressBoundaryPreservesEvaluation();
        }

        private static void CountedBuildUpOversizeFailsBeforeEnumeration()
        {
            var source = new CountedNeverEnumerated<CostResourceComponent>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() => BuildUp("BUILDUP-COUNTED-OVERSIZE", source));

            Equal(0, source.GetEnumeratorCalls, "Oversized counted rate build-up input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted build-up oversize failure must report the component bound.");
        }

        private static void ConflictingBuildUpHiddenOversizeFailsBeforeEnumeration()
        {
            var source = new MultiCountNeverEnumerated<CostResourceComponent>(1, MaximumEntries + 1, 1);
            var error = Capture<InvalidOperationException>(() => BuildUp("BUILDUP-HIDDEN-OVERSIZE", source));

            Equal(0, source.GetEnumeratorCalls, "An oversized secondary Count contract must fail before enumeration.");
            Contains("at most 10000", error.Message, "Hidden oversized Count must preserve the component capacity failure.");
        }

        private static void ConflictingBuildUpInBoundCountsFailBeforeEnumeration()
        {
            var source = new MultiCountNeverEnumerated<CostResourceComponent>(1, 2, 1);
            var error = Capture<InvalidOperationException>(() => BuildUp("BUILDUP-CONFLICTING-COUNTS", source));

            Equal(0, source.GetEnumeratorCalls, "Conflicting in-bound Count contracts must fail before enumeration.");
            Contains("conflicting known counts", error.Message, "Conflicting Count contracts must fail closed explicitly.");
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

        private static void CountedProgressContractOversizeFailsBeforeEnumeration()
        {
            var contracts = new CountedNeverEnumerated<ProgressContractItem>(MaximumEntries + 1);
            var claims = new CountedNeverEnumerated<ProgressClaimLine>(0);
            var error = Capture<InvalidOperationException>(() => new ProgressClaimService().Evaluate(contracts, claims));

            Equal(0, contracts.GetEnumeratorCalls, "Oversized counted progress contract input must fail before enumeration.");
            Equal(0, claims.GetEnumeratorCalls, "Progress claims must not be enumerated after counted contract rejection.");
            Contains("at most 10000", error.Message, "Counted progress contract oversize failure must report the item bound.");
        }

        private static void CountedProgressClaimOversizeFailsBeforeEnumeration()
        {
            var contracts = new CountedNeverEnumerated<ProgressContractItem>(1);
            var claims = new CountedNeverEnumerated<ProgressClaimLine>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() => new ProgressClaimService().Evaluate(contracts, claims));

            Equal(0, contracts.GetEnumeratorCalls, "Known oversized progress claims must fail before contract materialization starts.");
            Equal(0, claims.GetEnumeratorCalls, "Oversized counted progress claim input must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted progress claim oversize failure must report the line bound.");
        }

        private static void StreamingProgressContractOversizeStopsAtFirstDisallowedEntry()
        {
            var contracts = new StreamingProgressContracts(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() =>
                new ProgressClaimService().Evaluate(contracts, Array.Empty<ProgressClaimLine>()));

            Equal(
                MaximumEntries + 1,
                contracts.YieldedCount,
                "Streaming progress contract ingestion must stop immediately after observing item 10,001.");
            Contains("at most 10000", error.Message, "Streaming progress contract oversize failure must report the item bound.");
        }

        private static void StreamingProgressClaimOversizeStopsAtFirstDisallowedEntry()
        {
            var contracts = ProgressContracts();
            var claims = new StreamingProgressClaims(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() => new ProgressClaimService().Evaluate(contracts, claims));

            Equal(
                MaximumEntries + 1,
                claims.YieldedCount,
                "Streaming progress claim ingestion must stop immediately after observing line 10,001.");
            Contains("at most 10000", error.Message, "Streaming progress claim oversize failure must report the line bound.");
        }

        private static void ExactProgressBoundaryPreservesEvaluation()
        {
            var contracts = ProgressContracts();
            var claims = new ProgressClaimLine[MaximumEntries];
            for (var i = 0; i < claims.Length; i++)
                claims[i] = ProgressClaim(i);

            var result = new ProgressClaimService().Evaluate(contracts, claims, retentionPercent: 10m);

            Equal(MaximumEntries, result.Lines.Count, "Progress evaluation must accept exactly 10,000 contract and claim entries.");
            Equal("ITEM-00000", result.Lines[0].ItemCode, "Progress result ordering changed at the first item.");
            Equal("ITEM-09999", result.Lines[result.Lines.Count - 1].ItemCode, "Progress result ordering changed at the final item.");
            Equal(1m, result.Lines[0].CertifiedThisPeriodQuantity, "Progress certification semantics changed at the boundary.");
            Equal(0m, result.Lines[0].RejectedQuantity, "Progress rejection semantics changed at the boundary.");
            Equal(1m, result.Lines[0].RemainingQuantity, "Progress remaining-quantity semantics changed at the boundary.");
            Equal(10000m, result.GrossCertifiedThisPeriod, "Boundary-sized progress gross changed.");
            Equal(1000m, result.RetentionThisPeriod, "Boundary-sized progress retention changed.");
            Equal(9000m, result.NetCertifiedThisPeriod, "Boundary-sized progress net changed.");
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

        private static ProgressContractItem[] ProgressContracts()
        {
            var contracts = new ProgressContractItem[MaximumEntries];
            for (var i = 0; i < contracts.Length; i++)
                contracts[i] = ProgressContract(i);
            return contracts;
        }

        private static ProgressContractItem ProgressContract(int index)
        {
            return new ProgressContractItem(
                "ITEM-" + index.ToString("D5", CultureInfo.InvariantCulture),
                "m2",
                2m,
                1m);
        }

        private static ProgressClaimLine ProgressClaim(int index)
        {
            return new ProgressClaimLine(
                "ITEM-" + index.ToString("D5", CultureInfo.InvariantCulture),
                0m,
                1m);
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

        private sealed class MultiCountNeverEnumerated<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;

            internal MultiCountNeverEnumerated(int genericCount, int readOnlyCount, int nonGenericCount)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
            }

            int ICollection<T>.Count => _genericCount;
            int IReadOnlyCollection<T>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                throw new InvalidOperationException("Conflicting counted source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => false;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
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

        private sealed class StreamingProgressContracts : IEnumerable<ProgressContractItem>
        {
            private readonly int _count;

            internal StreamingProgressContracts(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<ProgressContractItem> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return ProgressContract(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingProgressClaims : IEnumerable<ProgressClaimLine>
        {
            private readonly int _count;

            internal StreamingProgressClaims(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<ProgressClaimLine> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return ProgressClaim(i);
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
