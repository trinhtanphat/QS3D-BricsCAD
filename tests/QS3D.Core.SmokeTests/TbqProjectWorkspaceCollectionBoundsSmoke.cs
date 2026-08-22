using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TbqProjectWorkspaceCollectionBoundsSmoke
    {
        private const int MaxBillItems = 10000;
        private const int MaxBuildUpRates = 10000;
        private const int MaxRateReferences = 50000;
        private const int MaxLibraryEntries = 10000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            ExactPersistenceBoundariesRemainSupported();
            BillItemsFailAtFirstExcessItem();
            BuildUpRatesFailAtFirstExcessRate();
            RateReferencesFailAtFirstExcessEdge();
            LibraryEntriesFailAtFirstExcessEntry();
            OrdinaryWorkspaceRemainsStable();
        }

        private static void ExactPersistenceBoundariesRemainSupported()
        {
            var state = State(
                BillItems(MaxBillItems),
                BuildUpRates(MaxBuildUpRates),
                RateReferences(MaxRateReferences),
                LibraryEntries(MaxLibraryEntries));

            Assert(state.BillItems.Count == MaxBillItems, "TBQ exact bill-item persistence limit must remain supported.");
            Assert(state.BuildUpRates.Count == MaxBuildUpRates, "TBQ exact build-up-rate persistence limit must remain supported.");
            Assert(state.RateReferences.Edges.Count == MaxRateReferences, "TBQ exact rate-reference persistence limit must remain supported.");
            Assert(state.Library.Entries.Count == MaxLibraryEntries, "TBQ exact library-entry persistence limit must remain supported.");
        }

        private static void BillItemsFailAtFirstExcessItem()
        {
            var seen = 0;
            Capture<InvalidOperationException>(() => State(
                Counted(BillItems(MaxBillItems + 1), () => seen++),
                Array.Empty<BuildUpRateSnapshot>(),
                Array.Empty<RateReferenceEdge>(),
                Array.Empty<BqLibraryEntry>()));
            Assert(seen == MaxBillItems + 1, "TBQ bill-item bound must fail while enumerating the first excess item.");
        }

        private static void BuildUpRatesFailAtFirstExcessRate()
        {
            var seen = 0;
            Capture<InvalidOperationException>(() => State(
                Array.Empty<TbqBillItem>(),
                Counted(BuildUpRates(MaxBuildUpRates + 1), () => seen++),
                Array.Empty<RateReferenceEdge>(),
                Array.Empty<BqLibraryEntry>()));
            Assert(seen == MaxBuildUpRates + 1, "TBQ build-up-rate bound must fail while enumerating the first excess rate.");
        }

        private static void RateReferencesFailAtFirstExcessEdge()
        {
            var seen = 0;
            Capture<InvalidOperationException>(() => State(
                Array.Empty<TbqBillItem>(),
                Array.Empty<BuildUpRateSnapshot>(),
                Counted(RateReferences(MaxRateReferences + 1), () => seen++),
                Array.Empty<BqLibraryEntry>()));
            Assert(seen == MaxRateReferences + 1, "TBQ rate-reference bound must fail while enumerating the first excess edge.");
        }

        private static void LibraryEntriesFailAtFirstExcessEntry()
        {
            var seen = 0;
            Capture<InvalidOperationException>(() => State(
                Array.Empty<TbqBillItem>(),
                Array.Empty<BuildUpRateSnapshot>(),
                Array.Empty<RateReferenceEdge>(),
                Counted(LibraryEntries(MaxLibraryEntries + 1), () => seen++)));
            Assert(seen == MaxLibraryEntries + 1, "TBQ library-entry bound must fail while enumerating the first excess entry.");
        }

        private static void OrdinaryWorkspaceRemainsStable()
        {
            var state = State(
                BillItems(2),
                BuildUpRates(2),
                RateReferences(3),
                LibraryEntries(1));
            Assert(state.BillItems.Count == 2, "Ordinary TBQ bill items changed unexpectedly.");
            Assert(state.BuildUpRates.Count == 2, "Ordinary TBQ build-up rates changed unexpectedly.");
            Assert(state.RateReferences.Edges.Count == 3, "Ordinary TBQ rate references changed unexpectedly.");
            Assert(state.Library.Entries.Count == 1, "Ordinary TBQ library entries changed unexpectedly.");
        }

        private static TbqProjectWorkspaceState State(
            IEnumerable<TbqBillItem> billItems,
            IEnumerable<BuildUpRateSnapshot> buildUpRates,
            IEnumerable<RateReferenceEdge> references,
            IEnumerable<BqLibraryEntry> libraryEntries) =>
            new TbqProjectWorkspaceState(
                "VND",
                0m,
                billItems,
                buildUpRates,
                references,
                "LIB",
                libraryEntries);

        private static IEnumerable<TbqBillItem> BillItems(int count)
        {
            for (var i = 0; i < count; i++)
                yield return new TbqBillItem("B" + i, "Bill item", "m3", "Trade", 1m, 1m);
        }

        private static IEnumerable<BuildUpRateSnapshot> BuildUpRates(int count)
        {
            for (var i = 0; i < count; i++)
                yield return new BuildUpRateSnapshot("R" + i, 1m);
        }

        private static IEnumerable<RateReferenceEdge> RateReferences(int count)
        {
            for (var i = 0; i < count; i++)
                yield return new RateReferenceEdge("R", RateReferenceTargetKind.BillItem, "T" + i);
        }

        private static IEnumerable<BqLibraryEntry> LibraryEntries(int count)
        {
            for (var i = 0; i < count; i++)
                yield return new BqLibraryEntry("L" + i, "Library item", "m3", "Category", 1m);
        }

        private static IEnumerable<T> Counted<T>(IEnumerable<T> source, Action onYield)
        {
            foreach (var item in source)
            {
                onYield();
                yield return item;
            }
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
