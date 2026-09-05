using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialTransientKnownCountStabilitySmoke
    {
        internal static void Run()
        {
            AuditRecordRejectsTransientKnownCountDrift();
            StableMultiSurfaceCountRemainsAccepted();
        }

        private static void AuditRecordRejectsTransientKnownCountDrift()
        {
            var revisions = new TransientKnownCountCollection<CommercialRevisionRef>(Revision("REV-DRIFT"));

            ExpectInvalidOperation(
                () => Record("EVENT-DRIFT", revisions),
                "Commercial audit record accepted source revisions whose known Count changed after enumeration began.");
        }

        private static void StableMultiSurfaceCountRemainsAccepted()
        {
            var revisions = new StableMultiSurfaceCollection<CommercialRevisionRef>(Revision("REV-STABLE"));
            var record = Record("EVENT-STABLE", revisions);

            Equal(1, record.SourceRevisions.Count, "Stable multi-interface Count evidence changed accepted snapshot cardinality.");
            Equal(2, revisions.CurrentReads, "Stable known-count source-revision input must be replayed exactly once for semantic-generation validation.");
        }

        private static CommercialAuditRecord Record(
            string eventId,
            IEnumerable<CommercialRevisionRef> revisions)
        {
            return new CommercialAuditRecord(
                eventId,
                "Estimate",
                "EST-COUNT-STABILITY",
                "Updated",
                "qa",
                new DateTime(2026, 8, 18, 11, 0, 0, DateTimeKind.Utc),
                "Known Count stability smoke",
                "CORR-COUNT-STABILITY",
                "Before",
                "After",
                revisions);
        }

        private static CommercialRevisionRef Revision(string revisionId)
        {
            return new CommercialRevisionRef("Estimate", "EST-COUNT-STABILITY", revisionId);
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

            throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private sealed class StableMultiSurfaceCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;

            internal StableMultiSurfaceCollection(params T[] items)
            {
                _items = items ?? Array.Empty<T>();
            }

            internal int CurrentReads { get; private set; }

            public int Count => _items.Length;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                CurrentReads++;
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
            public bool Contains(T item) => ((ICollection<T>)_items).Contains(item);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class TransientKnownCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly T[] _items;
            private bool _enumerationStarted;

            internal TransientKnownCountCollection(params T[] items)
            {
                _items = items ?? Array.Empty<T>();
            }

            public int Count => _enumerationStarted ? _items.Length + 1 : _items.Length;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator()
            {
                _enumerationStarted = true;
                return ((IEnumerable<T>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
            public bool Contains(T item) => ((ICollection<T>)_items).Contains(item);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
