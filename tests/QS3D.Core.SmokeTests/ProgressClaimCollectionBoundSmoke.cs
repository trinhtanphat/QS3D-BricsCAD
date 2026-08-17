using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class ProgressClaimCollectionBoundSmoke
    {
        private const int MaximumEntries = 10000;

        internal static void Run()
        {
            CountedContractOversizeFailsBeforeEnumeration();
            CountedClaimOversizeFailsBeforeAnyEnumeration();
            StreamingContractOversizeStopsAtFirstDisallowedEntry();
            StreamingClaimOversizeStopsAtFirstDisallowedEntry();
            ExactBoundaryPreservesEvaluation();
            OrdinaryCappingRetentionAndUnknownItemSemanticsRemain();
            DuplicateSemanticsRemain();
        }

        private static void CountedContractOversizeFailsBeforeEnumeration()
        {
            var contracts = new CountedNeverEnumerated<ProgressContractItem>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() =>
                new ProgressClaimService().Evaluate(contracts, Array.Empty<ProgressClaimLine>()));

            Equal(0, contracts.GetEnumeratorCalls, "Oversized counted progress contracts must fail before enumeration.");
            Contains("at most 10000", error.Message, "Counted contract oversize failure must report the canonical Cost bound.");
        }

        private static void CountedClaimOversizeFailsBeforeAnyEnumeration()
        {
            var contracts = new CountedNeverEnumerated<ProgressContractItem>(1);
            var claims = new CountedNeverEnumerated<ProgressClaimLine>(MaximumEntries + 1);
            var error = Capture<InvalidOperationException>(() =>
                new ProgressClaimService().Evaluate(contracts, claims));

            Equal(0, contracts.GetEnumeratorCalls, "Oversized counted claims must be rejected before contract materialization.");
            Equal(0, claims.GetEnumeratorCalls, "Oversized counted claims must fail before claim enumeration.");
            Contains("at most 10000", error.Message, "Counted claim oversize failure must report the canonical Cost bound.");
        }

        private static void StreamingContractOversizeStopsAtFirstDisallowedEntry()
        {
            var contracts = new StreamingContracts(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() =>
                new ProgressClaimService().Evaluate(contracts, Array.Empty<ProgressClaimLine>()));

            Equal(MaximumEntries + 1, contracts.YieldedCount,
                "Streaming progress contracts must stop immediately after observing entry 10,001.");
            Contains("at most 10000", error.Message, "Streaming contract oversize failure must report the canonical Cost bound.");
        }

        private static void StreamingClaimOversizeStopsAtFirstDisallowedEntry()
        {
            var contracts = CreateContracts(MaximumEntries);
            var claims = new StreamingClaims(MaximumEntries + 2);
            var error = Capture<InvalidOperationException>(() =>
                new ProgressClaimService().Evaluate(contracts, claims));

            Equal(MaximumEntries + 1, claims.YieldedCount,
                "Streaming progress claims must stop immediately after observing entry 10,001.");
            Contains("at most 10000", error.Message, "Streaming claim oversize failure must report the canonical Cost bound.");
        }

        private static void ExactBoundaryPreservesEvaluation()
        {
            var contracts = CreateContracts(MaximumEntries);
            var claims = new ProgressClaimLine[MaximumEntries];
            for (var i = 0; i < claims.Length; i++)
                claims[i] = Claim(i, 0m, 1m);

            var result = new ProgressClaimService().Evaluate(contracts, claims, retentionPercent: 10m);

            Equal(MaximumEntries, result.Lines.Count, "Progress evaluation must accept exactly 10,000 contract items and claim lines.");
            Equal(10000m, result.GrossCertifiedThisPeriod, "Boundary-sized progress gross changed.");
            Equal(1000m, result.RetentionThisPeriod, "Boundary-sized progress retention changed.");
            Equal(9000m, result.NetCertifiedThisPeriod, "Boundary-sized progress net changed.");
            Equal("ITEM-00000", result.Lines[0].ItemCode, "Progress result ordering changed at the first item.");
            Equal("ITEM-09999", result.Lines[result.Lines.Count - 1].ItemCode, "Progress result ordering changed at the final item.");
        }

        private static void OrdinaryCappingRetentionAndUnknownItemSemanticsRemain()
        {
            var service = new ProgressClaimService();
            var contracts = new[] { new ProgressContractItem("A", "m", 10m, 2m) };
            var claims = new[] { new ProgressClaimLine("A", 8m, 5m) };

            var result = service.Evaluate(contracts, claims, retentionPercent: 25m);
            var line = result.Lines[0];
            Equal(2m, line.CertifiedThisPeriodQuantity, "Progress capping semantics changed.");
            Equal(3m, line.RejectedQuantity, "Progress rejected-quantity semantics changed.");
            Equal(0m, line.RemainingQuantity, "Progress remaining-quantity semantics changed.");
            Equal(4m, line.CertifiedThisPeriodValue, "Progress line-value semantics changed.");
            Equal(4m, result.GrossCertifiedThisPeriod, "Ordinary progress gross changed.");
            Equal(1m, result.RetentionThisPeriod, "Ordinary progress retention changed.");
            Equal(3m, result.NetCertifiedThisPeriod, "Ordinary progress net changed.");

            Capture<InvalidOperationException>(() => service.Evaluate(
                contracts,
                new[] { new ProgressClaimLine("UNKNOWN", 0m, 1m) }));
        }

        private static void DuplicateSemanticsRemain()
        {
            var service = new ProgressClaimService();
            var item = new ProgressContractItem("DUP", "m", 1m, 1m);
            Capture<ArgumentException>(() => service.Evaluate(
                new[] { item, item },
                Array.Empty<ProgressClaimLine>()));

            Capture<ArgumentException>(() => service.Evaluate(
                new[] { item },
                new[]
                {
                    new ProgressClaimLine("DUP", 0m, 1m),
                    new ProgressClaimLine("DUP", 0m, 1m)
                }));
        }

        private static ProgressContractItem[] CreateContracts(int count)
        {
            var result = new ProgressContractItem[count];
            for (var i = 0; i < result.Length; i++)
                result[i] = Contract(i);
            return result;
        }

        private static ProgressContractItem Contract(int index)
        {
            return new ProgressContractItem(ItemCode(index), "m", 1m, 1m);
        }

        private static ProgressClaimLine Claim(int index, decimal previous, decimal current)
        {
            return new ProgressClaimLine(ItemCode(index), previous, current);
        }

        private static string ItemCode(int index)
        {
            return "ITEM-" + index.ToString("D5", CultureInfo.InvariantCulture);
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
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
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
                throw new InvalidOperationException("Counted oversize source must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingContracts : IEnumerable<ProgressContractItem>
        {
            private readonly int _count;

            internal StreamingContracts(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<ProgressContractItem> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Contract(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingClaims : IEnumerable<ProgressClaimLine>
        {
            private readonly int _count;

            internal StreamingClaims(int count)
            {
                _count = count;
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<ProgressClaimLine> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    YieldedCount++;
                    yield return Claim(i, 0m, 1m);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class ProgressClaimCollectionBoundRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ProgressClaimCollectionBoundSmoke.Run();
        }
    }
}
