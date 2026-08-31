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
            KnownCountOverrunFailsBeforeRetentionAndLaterTail();
            UnderYieldMismatchFailsClosed();
            PostTraversalUniformCountDriftFailsClosed();
            PostTraversalSingleSurfaceDriftFailsClosed();
            PostTraversalNegativeCountFailsClosed();
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

        private static void KnownCountOverrunFailsBeforeRetentionAndLaterTail()
        {
            var source = new OverrunThenThrowDefinitions();
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "known Count was exceeded",
                "title-block known Count overrun");
            Equal(2, source.MoveNextCalls, "known Count overrun must stop on first extra item");
            Equal(1, source.CurrentReads, "known Count overrun must reject the extra item before reading/retaining Current");
        }

        private static void UnderYieldMismatchFailsClosed()
        {
            var source = new CountContractDefinitions(
                2,
                2,
                2,
                new[] { Definition("MISMATCH_A") });
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "known Count does not match",
                "title-block under-yield mismatch");
            Equal(1, source.EnumerationStarts, "under-yield source should be traversed exactly once");
        }

        private static void PostTraversalUniformCountDriftFailsClosed()
        {
            var source = new CountContractDefinitions(
                1,
                1,
                1,
                new[] { Definition("DRIFT") },
                postCollectionCount: 2,
                postReadOnlyCount: 2,
                postNonGenericCount: 2);
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "known Count changed during traversal",
                "uniform post-traversal title-block Count drift");
            Equal(6, source.CollectionCountReads, "generic Count must be rebound through terminal traversal");
            Equal(6, source.ReadOnlyCountReads, "read-only Count must be rebound through terminal traversal");
            Equal(6, source.NonGenericCountReads, "non-generic Count must be rebound through terminal traversal");
        }

        private static void PostTraversalSingleSurfaceDriftFailsClosed()
        {
            AssertPostTraversalConflict(2, 1, 1, "generic Count drift");
            AssertPostTraversalConflict(1, 2, 1, "read-only Count drift");
            AssertPostTraversalConflict(1, 1, 2, "non-generic Count drift");
        }

        private static void AssertPostTraversalConflict(
            int postCollectionCount,
            int postReadOnlyCount,
            int postNonGenericCount,
            string label)
        {
            var source = new CountContractDefinitions(
                1,
                1,
                1,
                new[] { Definition("SURFACE_DRIFT") },
                postCollectionCount: postCollectionCount,
                postReadOnlyCount: postReadOnlyCount,
                postNonGenericCount: postNonGenericCount);
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "conflicting known Count values after traversal",
                label);
            Equal(6, source.CollectionCountReads, label + " generic Count rebound budget");
            Equal(6, source.ReadOnlyCountReads, label + " read-only Count rebound budget");
            Equal(6, source.NonGenericCountReads, label + " non-generic Count rebound budget");
        }

        private static void PostTraversalNegativeCountFailsClosed()
        {
            var source = new CountContractDefinitions(
                1,
                1,
                1,
                new[] { Definition("NEG_AFTER") },
                postCollectionCount: -1,
                postReadOnlyCount: 1,
                postNonGenericCount: 1);
            InvalidOperationContains(
                () => SemanticTitleBlockParameterMapBuilder.Build(Sheet(), source),
                "negative known Count value after traversal",
                "negative post-traversal title-block Count");
            Equal(6, source.CollectionCountReads, "negative post-traversal generic Count rebound budget");
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
            Equal(388, source.CollectionCountReads, "stable generic Count traversal rebound budget");
            Equal(388, source.ReadOnlyCountReads, "stable read-only Count traversal rebound budget");
            Equal(388, source.NonGenericCountReads, "stable non-generic Count traversal rebound budget");
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
            private readonly int? _postCollectionCount;
            private readonly int? _postReadOnlyCount;
            private readonly int? _postNonGenericCount;
            private readonly IReadOnlyList<SemanticTitleBlockParameterDefinition> _items;
            private readonly bool _throwOnEnumeration;
            private bool _enumerationCompleted;

            internal CountContractDefinitions(
                int collectionCount,
                int readOnlyCount,
                int nonGenericCount,
                IReadOnlyList<SemanticTitleBlockParameterDefinition> items,
                bool throwOnEnumeration = false,
                int? postCollectionCount = null,
                int? postReadOnlyCount = null,
                int? postNonGenericCount = null)
            {
                _collectionCount = collectionCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _items = items;
                _throwOnEnumeration = throwOnEnumeration;
                _postCollectionCount = postCollectionCount;
                _postReadOnlyCount = postReadOnlyCount;
                _postNonGenericCount = postNonGenericCount;
            }

            int ICollection<SemanticTitleBlockParameterDefinition>.Count
            {
                get
                {
                    CollectionCountReads++;
                    return _enumerationCompleted && _postCollectionCount.HasValue
                        ? _postCollectionCount.Value
                        : _collectionCount;
                }
            }

            int IReadOnlyCollection<SemanticTitleBlockParameterDefinition>.Count
            {
                get
                {
                    ReadOnlyCountReads++;
                    return _enumerationCompleted && _postReadOnlyCount.HasValue
                        ? _postReadOnlyCount.Value
                        : _readOnlyCount;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountReads++;
                    return _enumerationCompleted && _postNonGenericCount.HasValue
                        ? _postNonGenericCount.Value
                        : _nonGenericCount;
                }
            }

            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int EnumerationStarts { get; private set; }
            internal int CollectionCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }

            public IEnumerator<SemanticTitleBlockParameterDefinition> GetEnumerator()
            {
                EnumerationStarts++;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Enumeration must not start for a preflight-invalid Count contract.");
                return EnumerateItems().GetEnumerator();
            }

            private IEnumerable<SemanticTitleBlockParameterDefinition> EnumerateItems()
            {
                for (var index = 0; index < _items.Count; index++)
                    yield return _items[index];
                _enumerationCompleted = true;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(SemanticTitleBlockParameterDefinition item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(SemanticTitleBlockParameterDefinition item) => throw new NotSupportedException();
            public void CopyTo(SemanticTitleBlockParameterDefinition[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(SemanticTitleBlockParameterDefinition item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class OverrunThenThrowDefinitions :
            ICollection<SemanticTitleBlockParameterDefinition>,
            IReadOnlyCollection<SemanticTitleBlockParameterDefinition>,
            ICollection
        {
            int ICollection<SemanticTitleBlockParameterDefinition>.Count => 1;
            int IReadOnlyCollection<SemanticTitleBlockParameterDefinition>.Count => 1;
            int ICollection.Count => 1;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<SemanticTitleBlockParameterDefinition> GetEnumerator()
            {
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(SemanticTitleBlockParameterDefinition item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(SemanticTitleBlockParameterDefinition item) => throw new NotSupportedException();
            public void CopyTo(SemanticTitleBlockParameterDefinition[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(SemanticTitleBlockParameterDefinition item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<SemanticTitleBlockParameterDefinition>
            {
                private readonly OverrunThenThrowDefinitions _owner;
                private int _position;

                internal Enumerator(OverrunThenThrowDefinitions owner)
                {
                    _owner = owner;
                }

                public SemanticTitleBlockParameterDefinition Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_position == 1)
                            return Definition("FIRST");
                        if (_position == 2)
                            return Definition("OVERRUN");
                        throw new InvalidOperationException("Current is unavailable outside an active element.");
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _position++;
                    if (_position <= 2)
                        return true;
                    throw new InvalidOperationException("Known Count overrun must win before this later throwing tail.");
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}