using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AuditBatchGenericCountDriftFailsAtomically();
            AuditBatchReadOnlyCountDriftFailsAtomically();
            AuditBatchNonGenericCountDriftFailsAtomically();
            AuditBatchNegativePostTraversalCountFailsAtomically();
            AuditBatchConflictingPostTraversalCountsFailAtomically();
            AuditBatchUnderYieldFailsAtomically();
            AuditBatchOverrunFailsAtomically();
            RevisionSnapshotGenericCountDriftFailsClosed();
            RevisionSnapshotReadOnlyCountDriftFailsClosed();
            RevisionSnapshotNonGenericCountDriftFailsClosed();
            RevisionSnapshotNegativePostTraversalCountFailsClosed();
            RevisionSnapshotConflictingPostTraversalCountsFailClosed();
            StableCountedInputsSucceed();
            StreamingInputsSucceed();
        }

        private static void AuditBatchGenericCountDriftFailsAtomically()
        {
            AssertAuditBatchFailureAtomic(new GenericDriftCollection<CommercialAuditRecord>(Records(2, "G"), 2, 3));
        }

        private static void AuditBatchReadOnlyCountDriftFailsAtomically()
        {
            AssertAuditBatchFailureAtomic(new ReadOnlyDriftCollection<CommercialAuditRecord>(Records(2, "R"), 2, 1));
        }

        private static void AuditBatchNonGenericCountDriftFailsAtomically()
        {
            AssertAuditBatchFailureAtomic(new NonGenericDriftCollection<CommercialAuditRecord>(Records(2, "N"), 2, 4));
        }

        private static void AuditBatchNegativePostTraversalCountFailsAtomically()
        {
            AssertAuditBatchFailureAtomic(new GenericDriftCollection<CommercialAuditRecord>(Records(1, "NEG"), 1, -1));
        }

        private static void AuditBatchConflictingPostTraversalCountsFailAtomically()
        {
            AssertAuditBatchFailureAtomic(new ConflictingAfterTraversalCollection<CommercialAuditRecord>(Records(2, "C"), 2, 2, 3));
        }

        private static void AuditBatchUnderYieldFailsAtomically()
        {
            AssertAuditBatchFailureAtomic(new GenericDriftCollection<CommercialAuditRecord>(Records(1, "UNDER"), 2, 2));
        }

        private static void AuditBatchOverrunFailsAtomically()
        {
            AssertAuditBatchFailureAtomic(new GenericDriftCollection<CommercialAuditRecord>(Records(2, "OVER"), 1, 1));
        }

        private static void RevisionSnapshotGenericCountDriftFailsClosed()
        {
            Throws<InvalidOperationException>(() => Record(
                "S-GENERIC",
                new GenericDriftCollection<CommercialRevisionRef>(Revisions(2, "SG"), 2, 3)));
        }

        private static void RevisionSnapshotReadOnlyCountDriftFailsClosed()
        {
            Throws<InvalidOperationException>(() => Record(
                "S-READONLY",
                new ReadOnlyDriftCollection<CommercialRevisionRef>(Revisions(2, "SR"), 2, 1)));
        }

        private static void RevisionSnapshotNonGenericCountDriftFailsClosed()
        {
            Throws<InvalidOperationException>(() => Record(
                "S-NONGENERIC",
                new NonGenericDriftCollection<CommercialRevisionRef>(Revisions(2, "SN"), 2, 4)));
        }

        private static void RevisionSnapshotNegativePostTraversalCountFailsClosed()
        {
            Throws<InvalidOperationException>(() => Record(
                "S-NEGATIVE",
                new GenericDriftCollection<CommercialRevisionRef>(Revisions(1, "SNEG"), 1, -1)));
        }

        private static void RevisionSnapshotConflictingPostTraversalCountsFailClosed()
        {
            Throws<InvalidOperationException>(() => Record(
                "S-CONFLICT",
                new ConflictingAfterTraversalCollection<CommercialRevisionRef>(Revisions(2, "SC"), 2, 2, 3)));
        }

        private static void StableCountedInputsSucceed()
        {
            var log = new CommercialAuditLog();
            log.AppendBatch(new GenericDriftCollection<CommercialAuditRecord>(Records(2, "STABLE"), 2, 2));
            Equal(2, log.Events.Count, "stable counted audit batch did not publish both records");

            var record = Record(
                "S-STABLE",
                new GenericDriftCollection<CommercialRevisionRef>(Revisions(2, "SS"), 2, 2));
            Equal(2, record.SourceRevisions.Count, "stable counted revision snapshot did not retain both revisions");
        }

        private static void StreamingInputsSucceed()
        {
            var log = new CommercialAuditLog();
            log.AppendBatch(StreamRecords());
            Equal(2, log.Events.Count, "streaming audit batch did not publish both records");

            var record = Record("S-STREAM", StreamRevisions());
            Equal(2, record.SourceRevisions.Count, "streaming revision snapshot did not retain both revisions");
        }

        private static void AssertAuditBatchFailureAtomic(IEnumerable<CommercialAuditRecord> source)
        {
            var log = new CommercialAuditLog();
            log.Append(Record("BASE", Array.Empty<CommercialRevisionRef>()));
            Throws<InvalidOperationException>(() => log.AppendBatch(source));
            Equal(1, log.Events.Count, "failed audit batch mutated the published audit log");
            Equal("BASE", log.Events[0].EventId, "failed audit batch changed the baseline audit event");
        }

        private static CommercialAuditRecord[] Records(int count, string prefix)
        {
            var result = new CommercialAuditRecord[count];
            for (var i = 0; i < count; i++)
                result[i] = Record(prefix + "-" + i, Array.Empty<CommercialRevisionRef>());
            return result;
        }

        private static CommercialRevisionRef[] Revisions(int count, string prefix)
        {
            var result = new CommercialRevisionRef[count];
            for (var i = 0; i < count; i++)
                result[i] = new CommercialRevisionRef("model", prefix + "-" + i, "r1");
            return result;
        }

        private static CommercialAuditRecord Record(string eventId, IEnumerable<CommercialRevisionRef> revisions)
        {
            return new CommercialAuditRecord(
                eventId,
                "estimate",
                "entity-1",
                "update",
                "tester",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "count stability",
                "corr-1",
                "before",
                "after",
                revisions);
        }

        private static IEnumerable<CommercialAuditRecord> StreamRecords()
        {
            yield return Record("STREAM-1", Array.Empty<CommercialRevisionRef>());
            yield return Record("STREAM-2", Array.Empty<CommercialRevisionRef>());
        }

        private static IEnumerable<CommercialRevisionRef> StreamRevisions()
        {
            yield return new CommercialRevisionRef("model", "stream-1", "r1");
            yield return new CommercialRevisionRef("model", "stream-2", "r1");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("CommercialCountStabilitySmoke: expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    "CommercialCountStabilitySmoke: " + message + ". Expected " + expected + ", got " + actual + ".");
        }

        private sealed class GenericDriftCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int _beforeCount;
            private readonly int _afterCount;
            private bool _traversed;

            public GenericDriftCollection(T[] items, int beforeCount, int afterCount)
            {
                _items = items;
                _beforeCount = beforeCount;
                _afterCount = afterCount;
            }

            public int Count => _traversed ? _afterCount : _beforeCount;
            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i];
                }
                finally
                {
                    _traversed = true;
                }
            }

            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyDriftCollection<T> : IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _beforeCount;
            private readonly int _afterCount;
            private bool _traversed;

            public ReadOnlyDriftCollection(T[] items, int beforeCount, int afterCount)
            {
                _items = items;
                _beforeCount = beforeCount;
                _afterCount = afterCount;
            }

            public int Count => _traversed ? _afterCount : _beforeCount;
            public IEnumerator<T> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i];
                }
                finally
                {
                    _traversed = true;
                }
            }
        }

        private sealed class NonGenericDriftCollection<T> : IEnumerable<T>, ICollection
        {
            private readonly T[] _items;
            private readonly int _beforeCount;
            private readonly int _afterCount;
            private bool _traversed;

            public NonGenericDriftCollection(T[] items, int beforeCount, int afterCount)
            {
                _items = items;
                _beforeCount = beforeCount;
                _afterCount = afterCount;
            }

            public int Count => _traversed ? _afterCount : _beforeCount;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public IEnumerator<T> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i];
                }
                finally
                {
                    _traversed = true;
                }
            }

            public void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        }

        private sealed class ConflictingAfterTraversalCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T[] _items;
            private readonly int _beforeCount;
            private readonly int _genericAfterCount;
            private readonly int _readOnlyAfterCount;
            private bool _traversed;

            public ConflictingAfterTraversalCollection(T[] items, int beforeCount, int genericAfterCount, int readOnlyAfterCount)
            {
                _items = items;
                _beforeCount = beforeCount;
                _genericAfterCount = genericAfterCount;
                _readOnlyAfterCount = readOnlyAfterCount;
            }

            int ICollection<T>.Count => _traversed ? _genericAfterCount : _beforeCount;
            int IReadOnlyCollection<T>.Count => _traversed ? _readOnlyAfterCount : _beforeCount;
            bool ICollection<T>.IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() => Enumerate().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private IEnumerable<T> Enumerate()
            {
                try
                {
                    for (var i = 0; i < _items.Length; i++)
                        yield return _items[i];
                }
                finally
                {
                    _traversed = true;
                }
            }

            bool ICollection<T>.Contains(T item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        }
    }
}
