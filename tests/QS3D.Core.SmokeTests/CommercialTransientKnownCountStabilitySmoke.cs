using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialTransientKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            AuditBatchTransientGrowthRejectsBeforeCurrent();
            SourceRevisionTransientNegativeRejectsBeforeCurrent();
            SourceRevisionTransientConflictRejectsBeforeCurrent();
            StableMultiSurfaceCountRemainsAccepted();
        }

        private static void AuditBatchTransientGrowthRejectsBeforeCurrent()
        {
            var log = new CommercialAuditLog();
            var records = new TransientCountCollection<CommercialAuditRecord>(Record("EVENT-GROWTH"), transientCount: 2);

            var error = Capture<InvalidOperationException>(() => log.AppendBatch(records));

            Contains("known Count changed during traversal", error.Message);
            Equal(0, records.CurrentReads, "Transient audit Count growth must fail before semantic Current.");
            Equal(0, log.Events.Count, "Rejected transient audit Count drift must remain failure-atomic.");
        }

        private static void SourceRevisionTransientNegativeRejectsBeforeCurrent()
        {
            var revisions = new TransientCountCollection<CommercialRevisionRef>(Revision("REV-NEGATIVE"), transientCount: -1);

            var error = Capture<InvalidOperationException>(() => Record("EVENT-NEGATIVE", revisions));

            Contains("invalid negative known Count", error.Message);
            Equal(0, revisions.CurrentReads, "Transient negative source-revision Count must fail before semantic Current.");
        }

        private static void SourceRevisionTransientConflictRejectsBeforeCurrent()
        {
            var revisions = new TransientConflictingCountCollection<CommercialRevisionRef>(Revision("REV-CONFLICT"));

            var error = Capture<InvalidOperationException>(() => Record("EVENT-CONFLICT", revisions));

            Contains("conflicting known Count values", error.Message);
            Equal(0, revisions.CurrentReads, "Transient conflicting Count surfaces must fail before semantic Current.");
        }

        private static void StableMultiSurfaceCountRemainsAccepted()
        {
            var revisions = new StableMultiSurfaceCollection<CommercialRevisionRef>(Revision("REV-STABLE"));
            var record = Record("EVENT-STABLE", revisions);

            Equal(1, record.SourceRevisions.Count, "Stable multi-interface Count evidence changed accepted snapshot cardinality.");
            Equal(1, revisions.CurrentReads, "Stable source-revision input must be consumed exactly once.");
        }

        private static CommercialAuditRecord Record(string eventId, IEnumerable<CommercialRevisionRef>? revisions = null)
        {
            return new CommercialAuditRecord(
                eventId,
                "Element",
                "ELEMENT-1",
                "Update",
                "SmokeTest",
                new DateTime(2026, 8, 30, 0, 0, 0, DateTimeKind.Utc),
                "Transient Count stability regression",
                "CORRELATION-1",
                "Before",
                "After",
                revisions ?? Array.Empty<CommercialRevisionRef>());
        }

        private static CommercialRevisionRef Revision(string revisionId) =>
            new CommercialRevisionRef("Model", "MODEL-1", revisionId);

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Expected diagnostic fragment '" + expected + "'. Actual: " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private sealed class TransientCountCollection<T> : ICollection<T>
        {
            private readonly T _item;
            private readonly int _transientCount;
            private bool _afterMove;
            private bool _currentRead;

            internal TransientCountCollection(T item, int transientCount)
            {
                _item = item;
                _transientCount = transientCount;
            }

            public int Count => _afterMove && !_currentRead ? _transientCount : 1;
            public bool IsReadOnly => true;
            public int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly TransientCountCollection<T> _owner;
                private int _state;

                internal Enumerator(TransientCountCollection<T> owner) => _owner = owner;

                public bool MoveNext()
                {
                    if (_state != 0)
                        return false;
                    _state = 1;
                    _owner._afterMove = true;
                    return true;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._currentRead = true;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class TransientConflictingCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T _item;
            private bool _afterMove;
            private bool _currentRead;

            internal TransientConflictingCountCollection(T item) => _item = item;

            int ICollection<T>.Count => 1;
            int IReadOnlyCollection<T>.Count => _afterMove && !_currentRead ? 2 : 1;
            public bool IsReadOnly => true;
            public int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly TransientConflictingCountCollection<T> _owner;
                private int _state;

                internal Enumerator(TransientConflictingCountCollection<T> owner) => _owner = owner;

                public bool MoveNext()
                {
                    if (_state != 0)
                        return false;
                    _state = 1;
                    _owner._afterMove = true;
                    return true;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._currentRead = true;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StableMultiSurfaceCollection<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly T _item;

            internal StableMultiSurfaceCollection(T item) => _item = item;

            int ICollection<T>.Count => 1;
            int IReadOnlyCollection<T>.Count => 1;
            public bool IsReadOnly => true;
            public int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                CurrentReads++;
                yield return _item;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
