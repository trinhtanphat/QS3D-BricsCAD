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
            RejectsConflictingCountContractsBeforeEnumeration();
        }

        private static void RejectsNonGenericCountBeforeEnumeration()
        {
            var source = new NonGenericOversizedSource();
            ThrowsLimit(() => SemanticSheetIndexBuilder.Build(source), "non-generic ICollection");
            if (source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke enumerated an oversized non-generic ICollection.");
        }

        private static void RejectsConflictingCountContractsBeforeEnumeration()
        {
            var source = new ConflictingCountSource();
            ThrowsLimit(() => SemanticSheetIndexBuilder.Build(source), "conflicting Count contracts");
            if (source.Enumerated)
                throw new InvalidOperationException("SemanticSheetIndexKnownCountContractSmoke enumerated a source whose IReadOnlyCollection Count exceeded the limit.");
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

        private sealed class ConflictingCountSource :
            ICollection<SemanticSheetPlan>,
            IReadOnlyCollection<SemanticSheetPlan>,
            ICollection
        {
            public bool Enumerated { get; private set; }
            int ICollection<SemanticSheetPlan>.Count => 1;
            int IReadOnlyCollection<SemanticSheetPlan>.Count => Limit + 1;
            int ICollection.Count => 1;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<SemanticSheetPlan> GetEnumerator()
            {
                Enumerated = true;
                throw new InvalidOperationException("Conflicting known Count contracts must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(SemanticSheetPlan item) => false;
            public void CopyTo(SemanticSheetPlan[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            public void Add(SemanticSheetPlan item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(SemanticSheetPlan item) => throw new NotSupportedException();
        }
    }
}
