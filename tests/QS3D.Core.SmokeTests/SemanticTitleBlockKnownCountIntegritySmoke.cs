using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticTitleBlockKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            NegativeKnownCountFailsBeforeEnumeration();
            OversizedKnownCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            TraversalMismatchFailsClosed();
            ConsistentBoundaryCountRemainsSupported();
            EnumerableOnlyStreamingBoundRemainsSupported();
        }

        private static void NegativeKnownCountFailsBeforeEnumeration()
        {
            var source = new CountContractDefinitions(-1, -1, -1, new[] { Definition("NEG") }, throwOnEnumeration: true);
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "negative known Count",
                "negative title-block known Count");
            Equal(0, source.EnumerationStarts, "negative known Count must fail before enumeration");
        }

        private static void OversizedKnownCountFailsBeforeEnumeration()
        {
            var source = new CountContractDefinitions(129, 129, 129, new[] { Definition("OVER") }, throwOnEnumeration: true);
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "at most 128",
                "oversized title-block known Count");
            Equal(0, source.EnumerationStarts, "oversized known Count must fail before enumeration");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new CountContractDefinitions(1, 2, 1, new[] { Definition("CONFLICT") }, throwOnEnumeration: true);
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "conflicting known Count",
                "conflicting title-block known Counts");
            Equal(0, source.EnumerationStarts, "conflicting known Counts must fail before enumeration");
        }

        private static void TraversalMismatchFailsClosed()
        {
            var source = new CountContractDefinitions(
                1,
                1,
                1,
                new[] { Definition("MISMATCH_A"), Definition("MISMATCH_B") });
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "known Count does not match",
                "title-block traversal mismatch");
            Equal(1, source.EnumerationStarts, "mismatch source should be traversed exactly once");
        }

        private static void ConsistentBoundaryCountRemainsSupported()
        {
            var definitions = Enumerable.Range(0, 128)
                .Select(index => Definition("TAG_" + index.ToString("D3")))
                .ToArray();
            var source = new CountContractDefinitions(128, 128, 128, definitions);

            var map = SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source);

            Equal(128, map.Values.Count, "exact-at-limit title-block mapping count");
            Equal(1, source.EnumerationStarts, "exact-at-limit source should be traversed once");
            Equal("TAG_000", map.Values[0].DestinationTag, "deterministic first destination tag");
            Equal("TAG_127", map.Values[127].DestinationTag, "deterministic last destination tag");
        }

        private static void EnumerableOnlyStreamingBoundRemainsSupported()
        {
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), YieldDefinitions(129)),
                "at most 128",
                "enumerable-only title-block streaming bound");
        }

        private static IEnumerable<SemanticTitleBlockParameterDefinition> YieldDefinitions(int count)
        {
            for (var index = 0; index < count; index++)
                yield return Definition("STREAM_" + index.ToString("D3"));
        }

        private static SemanticTitleBlockParameterDefinition Definition(string tag)
        {
            return new SemanticTitleBlockParameterDefinition(tag, SemanticTitleBlockSheetField.SheetNumber);
        }

        private static SemanticSheetPlan Sheet()
        {
            return SemanticSheetPlanner.Build(
                new SemanticSheetDefinition(
                    "sheet-count-contract",
                    "A-001",
                    "Count contract sheet",
                    841d,
                    594d,
                    Array.Empty<SemanticSheetPlacementDefinition>(),
                    "A1"),
                Array.Empty<SemanticViewPlan>());
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

        private sealed class CountContractDefinitions :
            ICollection<SemanticTitleBlockParameterDefinition>,
            IReadOnlyCollection<SemanticTitleBlockParameterDefinition>,
            ICollection
        {
            private readonly int _collectionCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly IReadOnlyList<SemanticTitleBlockParameterDefinition> _items;
            private readonly bool _throwOnEnumeration;

            internal CountContractDefinitions(
                int collectionCount,
                int readOnlyCount,
                int nonGenericCount,
                IReadOnlyList<SemanticTitleBlockParameterDefinition> items,
                bool throwOnEnumeration = false)
            {
                _collectionCount = collectionCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _items = items;
                _throwOnEnumeration = throwOnEnumeration;
            }

            int ICollection<SemanticTitleBlockParameterDefinition>.Count => _collectionCount;
            int IReadOnlyCollection<SemanticTitleBlockParameterDefinition>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int EnumerationStarts { get; private set; }

            public IEnumerator<SemanticTitleBlockParameterDefinition> GetEnumerator()
            {
                EnumerationStarts++;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Enumeration must not start for a preflight-invalid Count contract.");
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(SemanticTitleBlockParameterDefinition item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(SemanticTitleBlockParameterDefinition item) => throw new NotSupportedException();
            public void CopyTo(SemanticTitleBlockParameterDefinition[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(SemanticTitleBlockParameterDefinition item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}