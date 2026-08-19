using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetPlacementKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            NegativeKnownCountFailsBeforeEnumeration();
            OversizedKnownCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            TraversalMismatchFailsClosed();
            ExactBoundaryCountRemainsSupported();
            EnumerableOnlyStreamingBoundRemainsSupported();
        }

        private static void NegativeKnownCountFailsBeforeEnumeration()
        {
            var source = new CountContractPlacements(-1, -1, -1, new[] { Placement(0) }, throwOnEnumeration: true);
            InvalidOperationContains(
                () => CreateSheet(source),
                "negative known Count",
                "negative semantic-sheet placement Count");
            Equal(0, source.EnumerationStarts, "negative placement Count must fail before enumeration");
        }

        private static void OversizedKnownCountFailsBeforeEnumeration()
        {
            var source = new CountContractPlacements(129, 129, 129, new[] { Placement(0) }, throwOnEnumeration: true);
            InvalidOperationContains(
                () => CreateSheet(source),
                "at most 128",
                "oversized semantic-sheet placement Count");
            Equal(0, source.EnumerationStarts, "oversized placement Count must fail before enumeration");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new CountContractPlacements(1, 2, 1, new[] { Placement(0) }, throwOnEnumeration: true);
            InvalidOperationContains(
                () => CreateSheet(source),
                "conflicting known Count",
                "conflicting semantic-sheet placement Counts");
            Equal(0, source.EnumerationStarts, "conflicting placement Counts must fail before enumeration");
        }

        private static void TraversalMismatchFailsClosed()
        {
            var source = new CountContractPlacements(1, 1, 1, new[] { Placement(0), Placement(1) });
            InvalidOperationContains(
                () => CreateSheet(source),
                "known Count does not match",
                "semantic-sheet placement traversal mismatch");
            Equal(1, source.EnumerationStarts, "mismatch placement source should be traversed once");
        }

        private static void ExactBoundaryCountRemainsSupported()
        {
            var placements = Enumerable.Range(0, 128).Select(Placement).ToArray();
            var source = new CountContractPlacements(128, 128, 128, placements);

            var sheet = CreateSheet(source);

            Equal(128, sheet.Placements.Count, "exact-at-limit semantic-sheet placement count");
            Equal(1, source.EnumerationStarts, "exact-at-limit placement source should be traversed once");
        }

        private static void EnumerableOnlyStreamingBoundRemainsSupported()
        {
            InvalidOperationContains(
                () => CreateSheet(YieldPlacements(129)),
                "at most 128",
                "enumerable-only semantic-sheet placement streaming bound");
        }

        private static SemanticSheetDefinition CreateSheet(IEnumerable<SemanticSheetPlacementDefinition> placements)
        {
            return new SemanticSheetDefinition(
                "sheet-count-contract",
                "A-001",
                "Count contract sheet",
                841d,
                594d,
                placements,
                "A1");
        }

        private static SemanticSheetPlacementDefinition Placement(int index)
        {
            return new SemanticSheetPlacementDefinition(
                "view-" + index.ToString("D3"),
                0d,
                0d,
                10d,
                10d);
        }

        private static IEnumerable<SemanticSheetPlacementDefinition> YieldPlacements(int count)
        {
            for (var index = 0; index < count; index++)
                yield return Placement(index);
        }

        private static void InvalidOperationContains(Action action, string expectedText, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(label + ": unexpected message: " + ex.Message, ex);
            }

            throw new InvalidOperationException(label + ": expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", got " + actual + ".");
        }

        private sealed class CountContractPlacements :
            ICollection<SemanticSheetPlacementDefinition>,
            IReadOnlyCollection<SemanticSheetPlacementDefinition>,
            ICollection
        {
            private readonly int _collectionCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly IReadOnlyList<SemanticSheetPlacementDefinition> _items;
            private readonly bool _throwOnEnumeration;

            internal CountContractPlacements(
                int collectionCount,
                int readOnlyCount,
                int nonGenericCount,
                IReadOnlyList<SemanticSheetPlacementDefinition> items,
                bool throwOnEnumeration = false)
            {
                _collectionCount = collectionCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _items = items;
                _throwOnEnumeration = throwOnEnumeration;
            }

            int ICollection<SemanticSheetPlacementDefinition>.Count => _collectionCount;
            int IReadOnlyCollection<SemanticSheetPlacementDefinition>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int EnumerationStarts { get; private set; }

            public IEnumerator<SemanticSheetPlacementDefinition> GetEnumerator()
            {
                EnumerationStarts++;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Enumeration must not start for a preflight-invalid placement Count contract.");
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(SemanticSheetPlacementDefinition item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(SemanticSheetPlacementDefinition item) => throw new NotSupportedException();
            public void CopyTo(SemanticSheetPlacementDefinition[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(SemanticSheetPlacementDefinition item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
