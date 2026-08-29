using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialCountNoOverreadSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AuditKnownCountOverrunStopsBeforeUnexpectedCurrent();
            AuditZeroCountOverrunNeverReadsCurrent();
            RevisionKnownCountOverrunStopsBeforeUnexpectedCurrent();
            RevisionZeroCountOverrunNeverReadsCurrent();
        }

        private static void AuditKnownCountOverrunStopsBeforeUnexpectedCurrent()
        {
            var source = new ObservedCountedCollection<CommercialAuditRecord>(
                new[] { Audit("A1"), Audit("A2") }, 1, throwOnUnexpectedCurrent: true);
            var log = new CommercialAuditLog();
            Throws<InvalidOperationException>(() => log.AppendBatch(source));
            Equal(2, source.MoveNextCalls, "audit Count=1 overrun must observe boundary MoveNext");
            Equal(1, source.CurrentReads, "audit Count=1 overrun must reject before Current N+1");
            Equal(0, log.Events.Count, "rejected audit batch must remain atomic");
        }

        private static void AuditZeroCountOverrunNeverReadsCurrent()
        {
            var source = new ObservedCountedCollection<CommercialAuditRecord>(
                new[] { Audit("Z1") }, 0, throwOnUnexpectedCurrent: true);
            var log = new CommercialAuditLog();
            Throws<InvalidOperationException>(() => log.AppendBatch(source));
            Equal(1, source.MoveNextCalls, "audit Count=0 overrun must observe first MoveNext");
            Equal(0, source.CurrentReads, "audit Count=0 overrun must reject before any Current");
            Equal(0, log.Events.Count, "rejected zero-count audit batch must remain atomic");
        }

        private static void RevisionKnownCountOverrunStopsBeforeUnexpectedCurrent()
        {
            var source = new ObservedCountedCollection<CommercialRevisionRef>(
                new[] { Revision("R1"), Revision("R2") }, 1, throwOnUnexpectedCurrent: true);
            Throws<InvalidOperationException>(() => Audit("REV1", source));
            Equal(2, source.MoveNextCalls, "revision Count=1 overrun must observe boundary MoveNext");
            Equal(1, source.CurrentReads, "revision Count=1 overrun must reject before Current N+1");
        }

        private static void RevisionZeroCountOverrunNeverReadsCurrent()
        {
            var source = new ObservedCountedCollection<CommercialRevisionRef>(
                new[] { Revision("R0") }, 0, throwOnUnexpectedCurrent: true);
            Throws<InvalidOperationException>(() => Audit("REV0", source));
            Equal(1, source.MoveNextCalls, "revision Count=0 overrun must observe first MoveNext");
            Equal(0, source.CurrentReads, "revision Count=0 overrun must reject before any Current");
        }

        private static CommercialAuditRecord Audit(string eventId) =>
            Audit(eventId, Array.Empty<CommercialRevisionRef>());

        private static CommercialAuditRecord Audit(string eventId, IEnumerable<CommercialRevisionRef> revisions) =>
            new CommercialAuditRecord(
                eventId,
                "estimate",
                "entity-1",
                "update",
                "tester",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "count no-overread",
                "corr-1",
                "before",
                "after",
                revisions);

        private static CommercialRevisionRef Revision(string id) =>
            new CommercialRevisionRef("model", id, "r1");

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
            throw new InvalidOperationException("CommercialCountNoOverreadSmoke: expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    "CommercialCountNoOverreadSmoke: " + message + ". Expected " + expected + ", got " + actual + ".");
        }

        private sealed class ObservedCountedCollection<T> : ICollection<T>
        {
            private readonly T[] _items;
            private readonly int _count;
            private readonly bool _throwOnUnexpectedCurrent;

            internal ObservedCountedCollection(T[] items, int count, bool throwOnUnexpectedCurrent)
            {
                _items = items;
                _count = count;
                _throwOnUnexpectedCurrent = throwOnUnexpectedCurrent;
            }

            public int Count => _count;
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }
            internal bool ThrowOnUnexpectedCurrent => _throwOnUnexpectedCurrent;

            public IEnumerator<T> GetEnumerator() => new ObservedEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class ObservedEnumerator : IEnumerator<T>
            {
                private readonly ObservedCountedCollection<T> _owner;
                private int _index = -1;

                internal ObservedEnumerator(ObservedCountedCollection<T> owner) => _owner = owner;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Length;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner.ThrowOnUnexpectedCurrent && _index >= _owner._count)
                            throw new InvalidOperationException("Unexpected Current read beyond admitted Count.");
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
