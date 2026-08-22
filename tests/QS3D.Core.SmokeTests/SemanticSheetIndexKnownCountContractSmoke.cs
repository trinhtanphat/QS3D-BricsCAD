using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetIndexKnownCountContractSmoke
    {
        private const int Limit = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNonGenericCountBeforeEnumeration();
            RejectsOversizedConflictingCountContractsBeforeEnumeration();
            RejectsNegativeGenericCountBeforeEnumeration();
            RejectsNegativeReadOnlyCountBeforeEnumeration();
            RejectsNegativeNonGenericCountBeforeEnumeration();
            RejectsInBoundConflictingCountContractsBeforeEnumeration();
            RejectsUnderEnumerationAgainstKnownCount();
            RejectsOverEnumerationAgainstKnownCount();
            AcceptsConsistentKnownCountContracts();
            AcceptsHonestNonEmptyKnownCount();
            AcceptsPureStreamingTraversal();
        }

        private static void RejectsNonGenericCountBeforeEnumeration()
        {
            var source = new NonGenericOversizedSource();
            ThrowsLimit(() => SemanticSheetIndexBuilder.Build(source), "non-generic ICollection");
            if (source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke enumerated an oversized non-generic ICollection.");
        }

        private static void RejectsOversizedConflictingCountContractsBeforeEnumeration()
        {
            var source = new MultiCountSource(1, Limit + 1, 1, throwOnEnumeration: true);
            ThrowsLimit(() => SemanticSheetIndexBuilder.Build(source), "conflicting Count contracts with an oversized contract");
            if (source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke enumerated a source whose IReadOnlyCollection Count exceeded the limit.");
        }

        private static void RejectsNegativeGenericCountBeforeEnumeration()
        {
            var source = new NegativeGenericCountSource();
            ThrowsDiagnostic(() => SemanticSheetIndexBuilder.Build(source), "negative known count", "negative ICollection<T> Count");
            if (source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke enumerated a negative ICollection<T> source.");
        }

        private static void RejectsNegativeReadOnlyCountBeforeEnumeration()
        {
            var source = new NegativeReadOnlyCountSource();
            ThrowsDiagnostic(() => SemanticSheetIndexBuilder.Build(source), "negative known count", "negative IReadOnlyCollection<T> Count");
            if (source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke enumerated a negative IReadOnlyCollection<T> source.");
        }

        private static void RejectsNegativeNonGenericCountBeforeEnumeration()
        {
            var source = new NegativeNonGenericCountSource();
            ThrowsDiagnostic(() => SemanticSheetIndexBuilder.Build(source), "negative known count", "negative ICollection Count");
            if (source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke enumerated a negative non-generic ICollection source.");
        }

        private static void RejectsInBoundConflictingCountContractsBeforeEnumeration()
        {
            var source = new MultiCountSource(0, 1, 0, throwOnEnumeration: true);
            ThrowsDiagnostic(() => SemanticSheetIndexBuilder.Build(source), "conflicting known counts", "in-bound conflicting Count contracts");
            if (source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke enumerated a source with contradictory in-bound Count contracts.");
        }

        private static void RejectsUnderEnumerationAgainstKnownCount()
        {
            var source = new TraversalCountSource(2, CreateSheet("S-UNDER-1", "U-001"));
            ThrowsDiagnostic(
                () => SemanticSheetIndexBuilder.Build(source),
                "traversal count does not match",
                "known Count greater than traversal");
        }

        private static void RejectsOverEnumerationAgainstKnownCount()
        {
            var source = new TraversalCountSource(
                1,
                CreateSheet("S-OVER-1", "O-001"),
                CreateSheet("S-OVER-2", "O-002"));
            ThrowsDiagnostic(
                () => SemanticSheetIndexBuilder.Build(source),
                "traversal count does not match",
                "known Count smaller than traversal");
        }

        private static void AcceptsConsistentKnownCountContracts()
        {
            var source = new MultiCountSource(0, 0, 0, throwOnEnumeration: false);
            var index = SemanticSheetIndexBuilder.Build(source);
            if (!source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke did not enumerate a consistent known-count source.");
            if (index.Rows.Count != 0)
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke expected an empty index for an empty consistent source.");
        }

        private static void AcceptsHonestNonEmptyKnownCount()
        {
            var source = new TraversalCountSource(1, CreateSheet("S-HONEST-1", "H-001"));
            var index = SemanticSheetIndexBuilder.Build(source);
            if (index.Rows.Count != 1 || index.Rows[0].SheetId != "S-HONEST-1")
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke rejected or changed an honest counted source.");
        }

        private static void AcceptsPureStreamingTraversal()
        {
            var source = new StreamingSource(
                CreateSheet("S-STREAM-2", "P-002"),
                CreateSheet("S-STREAM-1", "P-001"));
            var index = SemanticSheetIndexBuilder.Build(source);
            if (index.Rows.Count != 2 ||
                index.Rows[0].SheetId != "S-STREAM-1" ||
                index.Rows[1].SheetId != "S-STREAM-2")
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke changed pure streaming support or deterministic ordering.");
        }

        private static SemanticSheetPlan CreateSheet(string id, string number)
        {
            var definition = new SemanticSheetDefinition(
                id,
                number,
                "Sheet " + number,
                1000d,
                1000d,
                Array.Empty<SemanticSheetPlacementDefinition>());
            return SemanticSheetPlanner.Build(definition, Array.Empty<SemanticViewPlan>());
        }

        private static void ThrowsLimit(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("10000", StringComparison.Ordinal) >= 0 &&
                    ex.Message.IndexOf("at most", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(
                    "SemanticSheetIndexKnownCountContractSmoke " + label + " returned the wrong diagnostic: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException(
                "SemanticSheetIndexKnownCountContractSmoke " + label + " did not fail closed.");
        }

        private static void ThrowsDiagnostic(Action action, string expected, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(
                    "SemanticSheetIndexKnownCountContractSmoke " + label + " returned the wrong diagnostic: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException(
                "SemanticSheetIndexKnownCountContractSmoke " + label + " did not fail closed.");
        }

        private sealed class NonGenericOversizedSource : IEnumerable<SemanticSheetPlan>, ICollection
        {
            public bool Enumerated { get; private set; }
            public int Count => Limit + 1;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Oversized non-generic ICollection must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class NegativeGenericCountSource : ICollection<SemanticSheetPlan>
        {
            public bool Enumerated { get; private set; }
            public int Count => -1;
            public bool IsReadOnly => true;

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Negative ICollection<T> must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(SemanticSheetPlan item) => false;
            public void CopyTo(SemanticSheetPlan[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(SemanticSheetPlan item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(SemanticSheetPlan item) => throw new NotSupportedException();
        }

        private sealed class NegativeReadOnlyCountSource : IReadOnlyCollection<SemanticSheetPlan>
        {
            public bool Enumerated { get; private set; }
            public int Count => -1;

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Negative IReadOnlyCollection<T> must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NegativeNonGenericCountSource : IEnumerable<SemanticSheetPlan>, ICollection
        {
            public bool Enumerated { get; private set; }
            public int Count => -1;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Negative non-generic ICollection must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class MultiCountSource :
            ICollection<SemanticSheetPlan>,
            IReadOnlyCollection<SemanticSheetPlan>,
            ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            public MultiCountSource(int genericCount, int readOnlyCount, int nonGenericCount, bool throwOnEnumeration)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            public bool Enumerated { get; private set; }
            int ICollection<SemanticSheetPlan>.Count => _genericCount;
            int IReadOnlyCollection<SemanticSheetPlan>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                Enumerated = true;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Malformed known Count contracts must fail before enumeration.");
                return ((IEnumerable<SemanticSheetPlan>)Array.Empty<SemanticSheetPlan>()).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(SemanticSheetPlan item) => false;
            public void CopyTo(SemanticSheetPlan[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            public void Add(SemanticSheetPlan item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(SemanticSheetPlan item) => throw new NotSupportedException();
        }

        private sealed class TraversalCountSource : ICollection<SemanticSheetPlan>
        {
            private readonly SemanticSheetPlan[] _items;
            private readonly int _knownCount;

            public TraversalCountSource(int knownCount, params SemanticSheetPlan[] items)
            {
                _knownCount = knownCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count => _knownCount;
            public bool IsReadOnly => true;
            public IEnumerator<SemanticSheetPlan> GetEnumerator() => ((IEnumerable<SemanticSheetPlan>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(SemanticSheetPlan item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(SemanticSheetPlan[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(SemanticSheetPlan item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(SemanticSheetPlan item) => throw new NotSupportedException();
        }

        private sealed class StreamingSource : IEnumerable<SemanticSheetPlan>
        {
            private readonly SemanticSheetPlan[] _items;

            public StreamingSource(params SemanticSheetPlan[] items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public IEnumerator<SemanticSheetPlan> GetEnumerator() => ((IEnumerable<SemanticSheetPlan>)_items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
