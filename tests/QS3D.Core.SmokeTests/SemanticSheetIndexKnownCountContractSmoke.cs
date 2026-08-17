using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Documentation;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSheetIndexKnownCountContractSmoke
    {
        internal static void Run()
        {
            NegativeGenericCountFailsBeforeEnumeration();
            NegativeReadOnlyCountFailsBeforeEnumeration();
            NegativeNonGenericCountFailsBeforeEnumeration();
            ConflictingKnownCountsFailBeforeEnumeration();
            ConsistentKnownCountsRemainAccepted();
        }

        private static void NegativeGenericCountFailsBeforeEnumeration()
        {
            var source = new NegativeGenericCountSource();
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Equal(1, source.CountReads, "ICollection<T>.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative ICollection<T>.Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative generic known count must fail closed explicitly.");
        }

        private static void NegativeReadOnlyCountFailsBeforeEnumeration()
        {
            var source = new NegativeReadOnlyCountSource();
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Equal(1, source.CountReads, "IReadOnlyCollection<T>.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative IReadOnlyCollection<T>.Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative read-only known count must fail closed explicitly.");
        }

        private static void NegativeNonGenericCountFailsBeforeEnumeration()
        {
            var source = new NegativeNonGenericCountSource();
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Equal(1, source.CountReads, "ICollection.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Negative ICollection.Count must fail before enumeration.");
            Contains("negative known count", error.Message, "Negative non-generic known count must fail closed explicitly.");
        }

        private static void ConflictingKnownCountsFailBeforeEnumeration()
        {
            var source = new MultiCountSource(1, 2, 1, Sheet("S-1", "A-001"));
            var error = Capture<InvalidOperationException>(() => SemanticSheetIndexBuilder.Build(source));
            Equal(1, source.GenericCountReads, "ICollection<T>.Count must be inspected exactly once.");
            Equal(1, source.ReadOnlyCountReads, "IReadOnlyCollection<T>.Count must be inspected exactly once.");
            Equal(1, source.NonGenericCountReads, "ICollection.Count must be inspected exactly once.");
            Equal(0, source.GetEnumeratorCalls, "Conflicting known counts must fail before enumeration.");
            Contains("conflicting known counts", error.Message, "Conflicting known counts must fail closed explicitly.");
        }

        private static void ConsistentKnownCountsRemainAccepted()
        {
            var sheet = Sheet("S-1", "A-001");
            var source = new MultiCountSource(1, 1, 1, sheet);
            var index = SemanticSheetIndexBuilder.Build(source);
            Equal(1, source.GenericCountReads, "Consistent ICollection<T>.Count must be inspected exactly once.");
            Equal(1, source.ReadOnlyCountReads, "Consistent IReadOnlyCollection<T>.Count must be inspected exactly once.");
            Equal(1, source.NonGenericCountReads, "Consistent ICollection.Count must be inspected exactly once.");
            Equal(1, source.GetEnumeratorCalls, "Consistent known counts must still enumerate once.");
            Equal(1, index.Rows.Count, "Consistent counted source should produce one index row.");
            Equal(sheet.Id, index.Rows[0].SheetId, "Accepted row must preserve sheet identity.");
        }

        private static SemanticSheetPlan Sheet(string id, string number)
        {
            var definition = new SemanticSheetDefinition(
                id,
                number,
                "Sheet " + number,
                841d,
                594d,
                Array.Empty<SemanticSheetPlacementDefinition>());
            return SemanticSheetPlanner.Build(definition, Array.Empty<SemanticViewPlan>());
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
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

        private sealed class NegativeGenericCountSource : ICollection<SemanticSheetPlan>
        {
            public int Count { get { CountReads++; return -1; } }
            public bool IsReadOnly => true;
            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }
            public IEnumerator<SemanticSheetPlan> GetEnumerator() { GetEnumeratorCalls++; throw new InvalidOperationException("Must not enumerate."); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(SemanticSheetPlan item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(SemanticSheetPlan item) => false;
            public void CopyTo(SemanticSheetPlan[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(SemanticSheetPlan item) => throw new NotSupportedException();
        }

        private sealed class NegativeReadOnlyCountSource : IReadOnlyCollection<SemanticSheetPlan>
        {
            public int Count { get { CountReads++; return -1; } }
            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }
            public IEnumerator<SemanticSheetPlan> GetEnumerator() { GetEnumeratorCalls++; throw new InvalidOperationException("Must not enumerate."); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NegativeNonGenericCountSource : IEnumerable<SemanticSheetPlan>, ICollection
        {
            public int Count { get { CountReads++; return -1; } }
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            internal int CountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }
            public IEnumerator<SemanticSheetPlan> GetEnumerator() { GetEnumeratorCalls++; throw new InvalidOperationException("Must not enumerate."); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private sealed class MultiCountSource : ICollection<SemanticSheetPlan>, IReadOnlyCollection<SemanticSheetPlan>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly SemanticSheetPlan[] _items;

            internal MultiCountSource(int genericCount, int readOnlyCount, int nonGenericCount, params SemanticSheetPlan[] items)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _items = items;
            }

            int ICollection<SemanticSheetPlan>.Count { get { GenericCountReads++; return _genericCount; } }
            int IReadOnlyCollection<SemanticSheetPlan>.Count { get { ReadOnlyCountReads++; return _readOnlyCount; } }
            int ICollection.Count { get { NonGenericCountReads++; return _nonGenericCount; } }
            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            internal int GenericCountReads { get; private set; }
            internal int ReadOnlyCountReads { get; private set; }
            internal int NonGenericCountReads { get; private set; }
            internal int GetEnumeratorCalls { get; private set; }
            public IEnumerator<SemanticSheetPlan> GetEnumerator() { GetEnumeratorCalls++; return ((IEnumerable<SemanticSheetPlan>)_items).GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(SemanticSheetPlan item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(SemanticSheetPlan item) => ((ICollection<SemanticSheetPlan>)_items).Contains(item);
            public void CopyTo(SemanticSheetPlan[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public bool Remove(SemanticSheetPlan item) => throw new NotSupportedException();
            public void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }
    }

    internal static class SemanticSheetIndexKnownCountContractRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticSheetIndexKnownCountContractSmoke.Run();
    }
}
