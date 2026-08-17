using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class ProgressClaimCollectionBoundSmoke
    {
        private const int MaximumEntries = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            KnownCountContractOverflowRejectsBeforeEnumeration();
            KnownCountClaimOverflowRejectsBeforeEnumeration();
            StreamingContractOverflowStopsAtFirstDisallowedEntry();
            StreamingClaimOverflowStopsAtFirstDisallowedEntry();
            ExactlyMaximumEntriesRemainAccepted();
            OrdinaryValidationSemanticsRemainStable();
        }

        private static void KnownCountContractOverflowRejectsBeforeEnumeration()
        {
            var contracts = new KnownCountCollection<ProgressContractItem>(MaximumEntries + 1);

            Throws<InvalidOperationException>(() =>
                new ProgressClaimService().Evaluate(contracts, Array.Empty<ProgressClaimLine>()));

            Equal(false, contracts.EnumerationStarted, "Known-count oversized progress contract must fail before enumeration.");
        }

        private static void KnownCountClaimOverflowRejectsBeforeEnumeration()
        {
            var claims = new KnownCountCollection<ProgressClaimLine>(MaximumEntries + 1);

            Throws<InvalidOperationException>(() =>
                new ProgressClaimService().Evaluate(Array.Empty<ProgressContractItem>(), claims));

            Equal(false, claims.EnumerationStarted, "Known-count oversized progress claim must fail before enumeration.");
        }

        private static void StreamingContractOverflowStopsAtFirstDisallowedEntry()
        {
            var counter = new ProductionCounter();

            Throws<InvalidOperationException>(() =>
                new ProgressClaimService().Evaluate(
                    ContractSequence(MaximumEntries + 2, counter),
                    Array.Empty<ProgressClaimLine>()));

            Equal(
                MaximumEntries + 1,
                counter.Produced,
                "Progress contract streaming bound requested an entry after the first disallowed item.");
        }

        private static void StreamingClaimOverflowStopsAtFirstDisallowedEntry()
        {
            var contracts = BuildContracts(MaximumEntries);
            var counter = new ProductionCounter();

            Throws<InvalidOperationException>(() =>
                new ProgressClaimService().Evaluate(
                    contracts,
                    ClaimSequence(MaximumEntries + 2, counter)));

            Equal(
                MaximumEntries + 1,
                counter.Produced,
                "Progress claim streaming bound requested an entry after the first disallowed item.");
        }

        private static void ExactlyMaximumEntriesRemainAccepted()
        {
            var contracts = BuildContracts(MaximumEntries);
            var claims = new List<ProgressClaimLine>(MaximumEntries);
            for (var i = 0; i < MaximumEntries; i++)
                claims.Add(new ProgressClaimLine(ItemCode(i), 0m, 1m));

            var result = new ProgressClaimService().Evaluate(contracts, claims);

            Equal(MaximumEntries, result.Lines.Count, "Progress input boundary line count changed.");
            Equal((decimal)MaximumEntries, result.GrossCertifiedThisPeriod, "Progress input boundary gross changed.");
            Equal(result.GrossCertifiedThisPeriod, result.NetCertifiedThisPeriod, "Zero-retention boundary net changed.");
        }

        private static void OrdinaryValidationSemanticsRemainStable()
        {
            Throws<ArgumentException>(() =>
                new ProgressClaimService().Evaluate(
                    new[]
                    {
                        new ProgressContractItem("A", "ea", 1m, 1m),
                        new ProgressContractItem("A", "ea", 1m, 1m)
                    },
                    Array.Empty<ProgressClaimLine>()));

            Throws<InvalidOperationException>(() =>
                new ProgressClaimService().Evaluate(
                    new[] { new ProgressContractItem("A", "ea", 1m, 1m) },
                    new[] { new ProgressClaimLine("B", 0m, 1m) }));

            var result = new ProgressClaimService().Evaluate(
                new[] { new ProgressContractItem("A", "ea", 10m, 2m) },
                new[] { new ProgressClaimLine("A", 3m, 9m) },
                retentionPercent: 10m);

            Equal(7m, result.Lines[0].CertifiedThisPeriodQuantity, "Ordinary progress capping changed.");
            Equal(2m, result.Lines[0].RejectedQuantity, "Ordinary progress rejection changed.");
            Equal(14m, result.GrossCertifiedThisPeriod, "Ordinary progress gross changed.");
            Equal(1.4m, result.RetentionThisPeriod, "Ordinary progress retention changed.");
            Equal(12.6m, result.NetCertifiedThisPeriod, "Ordinary progress net changed.");
        }

        private static List<ProgressContractItem> BuildContracts(int count)
        {
            var contracts = new List<ProgressContractItem>(count);
            for (var i = 0; i < count; i++)
                contracts.Add(new ProgressContractItem(ItemCode(i), "ea", 1m, 1m));
            return contracts;
        }

        private static IEnumerable<ProgressContractItem> ContractSequence(int count, ProductionCounter counter)
        {
            for (var i = 0; i < count; i++)
            {
                counter.Produced++;
                yield return new ProgressContractItem(ItemCode(i), "ea", 1m, 1m);
            }
        }

        private static IEnumerable<ProgressClaimLine> ClaimSequence(int count, ProductionCounter counter)
        {
            for (var i = 0; i < count; i++)
            {
                counter.Produced++;
                yield return new ProgressClaimLine(ItemCode(i), 0m, 1m);
            }
        }

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
