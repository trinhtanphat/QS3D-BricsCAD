using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class TbqWorkspaceNestedKnownGenerationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsRateReferenceContentDriftAcrossCountedGeneration();
            RejectsLibraryEntryContentDriftAcrossCountedGeneration();
            AcceptsHonestCountedNestedSources();
            LeavesUncountedNestedSourcesSinglePass();
            Console.WriteLine("PASS TBQ workspace nested known generation");
        }

        private static void RejectsRateReferenceContentDriftAcrossCountedGeneration()
        {
            var rates = new DriftingCountedSource<RateReferenceEdge>(
                new[] { new RateReferenceEdge("R-1", RateReferenceTargetKind.BillItem, "B-1") },
                new[] { new RateReferenceEdge("R-2", RateReferenceTargetKind.BillItem, "B-1") });

            ExpectInvalidOperation(
                () => Workspace(rates, HonestLibrary()),
                "rate reference content drift must be rejected");
            Require(rates.EnumerationCount == 2, "counted rate references must receive semantic replay");
        }

        private static void RejectsLibraryEntryContentDriftAcrossCountedGeneration()
        {
            var library = new DriftingCountedSource<BqLibraryEntry>(
                new[] { new BqLibraryEntry("B-1", "Original", "m", "Trade/A", 10m) },
                new[] { new BqLibraryEntry("B-1", "Changed", "m", "Trade/A", 10m) });

            ExpectInvalidOperation(
                () => Workspace(HonestReferences(), library),
                "BQ library content drift must be rejected");
            Require(library.EnumerationCount == 2, "counted BQ library entries must receive semantic replay");
        }

        private static void AcceptsHonestCountedNestedSources()
        {
            var references = HonestReferences();
            var library = HonestLibrary();
            var workspace = Workspace(references, library);

            Require(workspace.RateReferences.Edges.Count == 1, "honest counted rate reference must survive snapshot");
            Require(workspace.Library.Entries.Count == 1, "honest counted library entry must survive snapshot");
            Require(references.EnumerationCount == 2, "honest counted references must be replayed exactly once");
            Require(library.EnumerationCount == 2, "honest counted library must be replayed exactly once");
        }

        private static void LeavesUncountedNestedSourcesSinglePass()
        {
            var references = new UncountedSource<RateReferenceEdge>(
                new[] { new RateReferenceEdge("R-1", RateReferenceTargetKind.BillItem, "B-1") });
            var library = new UncountedSource<BqLibraryEntry>(
                new[] { new BqLibraryEntry("B-1", "Library item", "m", "Trade/A", 10m) });

            var workspace = Workspace(references, library);

            Require(workspace.RateReferences.Edges.Count == 1, "uncounted reference must survive snapshot");
            Require(workspace.Library.Entries.Count == 1, "uncounted library entry must survive snapshot");
            Require(references.EnumerationCount == 1, "uncounted references must remain single-pass at TBQ boundary");
            Require(library.EnumerationCount == 1, "uncounted library must remain single-pass at TBQ boundary");
        }

        private static TbqProjectWorkspaceState Workspace(
            IEnumerable<RateReferenceEdge> references,
            IEnumerable<BqLibraryEntry> libraryEntries)
        {
            return new TbqProjectWorkspaceState(
                "USD",
                100m,
                new[] { new TbqBillItem("B-1", "Bill item", "m", "Trade/A", 1m, 10m, "R-1") },
                new[] { new BuildUpRateSnapshot("R-1", 10m) },
                references,
                "LIB-1",
                libraryEntries);
        }

        private static DriftingCountedSource<RateReferenceEdge> HonestReferences()
        {
            var generation = new[] { new RateReferenceEdge("R-1", RateReferenceTargetKind.BillItem, "B-1") };
            return new DriftingCountedSource<RateReferenceEdge>(generation, generation);
        }

        private static DriftingCountedSource<BqLibraryEntry> HonestLibrary()
        {
            var generation = new[] { new BqLibraryEntry("B-1", "Library item", "m", "Trade/A", 10m) };
            return new DriftingCountedSource<BqLibraryEntry>(generation, generation);
        }

        private static void ExpectInvalidOperation(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class DriftingCountedSource<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly IReadOnlyList<T> _first;
            private readonly IReadOnlyList<T> _second;

            internal DriftingCountedSource(IReadOnlyList<T> first, IReadOnlyList<T> second)
            {
                _first = first;
                _second = second;
                if (first.Count != second.Count)
                    throw new ArgumentException("Test generations must keep a stable Count.");
            }

            public int EnumerationCount { get; private set; }
            public int Count => _first.Count;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                var generation = EnumerationCount++ == 0 ? _first : _second;
                return generation.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => throw new NotSupportedException();
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class UncountedSource<T> : IEnumerable<T>
        {
            private readonly IReadOnlyList<T> _items;

            internal UncountedSource(IReadOnlyList<T> items)
            {
                _items = items;
            }

            public int EnumerationCount { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationCount++;
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
