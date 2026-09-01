using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialCurrentCountAcceptanceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AuditBatchRejectsCurrentInducedCountDriftBeforeNullAcceptance();
            RevisionSnapshotRejectsCurrentInducedCountDriftBeforeNullAcceptance();
            StableCountedControlsRemainAccepted();
            Console.WriteLine("PASS commercial Current-induced Count acceptance boundary");
        }

        private static void AuditBatchRejectsCurrentInducedCountDriftBeforeNullAcceptance()
        {
            var source = new CurrentDriftCollection<CommercialAuditRecord>(null!);
            var log = new CommercialAuditLog();
            var baseline = Audit("BASE", Array.Empty<CommercialRevisionRef>());
            log.Append(baseline);

            ExpectCountDrift(() => log.AppendBatch(source), "audit batch");
            Equal(1, source.CurrentReads, "audit regression must read Current exactly once");
            Equal(1, log.Events.Count, "Current-induced Count drift must not partially publish audit events");
            Equal("BASE", log.Events[0].EventId, "Current-induced Count drift changed baseline audit state");
        }

        private static void RevisionSnapshotRejectsCurrentInducedCountDriftBeforeNullAcceptance()
        {
            var source = new CurrentDriftCollection<CommercialRevisionRef>(null!);
            ExpectCountDrift(() => Audit("REV-DRIFT", source), "revision snapshot");
            Equal(1, source.CurrentReads, "revision regression must read Current exactly once");
        }

        private static void StableCountedControlsRemainAccepted()
        {
            var log = new CommercialAuditLog();
            log.AppendBatch(new[]
            {
                Audit("STABLE-AUDIT", Array.Empty<CommercialRevisionRef>())
            });
            Equal(1, log.Events.Count, "stable counted audit input changed");

            var record = Audit("STABLE-REV", new[]
            {
                new CommercialRevisionRef("model", "stable-source", "r1")
            });
            Equal(1, record.SourceRevisions.Count, "stable counted revision input changed");
        }

        private static CommercialAuditRecord Audit(string eventId, IEnumerable<CommercialRevisionRef> revisions)
        {
            return new CommercialAuditRecord(
                eventId,
                "estimate",
                "entity-1",
                "update",
                "tester",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "Current count boundary",
                "corr-1",
                "before",
                "after",
                revisions);
        }

        private static void ExpectCountDrift(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("known Count changed during traversal", StringComparison.OrdinalIgnoreCase) >= 0)
                    return;
                throw new InvalidOperationException(
                    "CommercialCurrentCountAcceptanceSmoke: " + label +
                    " failed for the wrong reason: " + ex.Message,
                    ex);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    "CommercialCurrentCountAcceptanceSmoke: " + label +
                    " reached ordinary item acceptance before Count stability was rebound: " + ex.Message,
                    ex);
            }

            throw new InvalidOperationException(
                "CommercialCurrentCountAcceptanceSmoke: " + label + " accepted Current-induced Count drift.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    "CommercialCurrentCountAcceptanceSmoke: " + message +
                    ". Expected " + expected + ", got " + actual + ".");
        }

        private sealed class CurrentDriftCollection<T> : ICollection<T>
        {
            private readonly T _item;
            private bool _emitDrift;

            internal CurrentDriftCollection(T item)
            {
                _item = item;
            }

            internal int CurrentReads { get; private set; }

            public int Count
            {
                get
                {
                    if (_emitDrift)
                    {
                        _emitDrift = false;
                        return 2;
                    }
                    return 1;
                }
            }

            public bool IsReadOnly => true;
            public IEnumerator<T> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_item, item);
            public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _item;
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly CurrentDriftCollection<T> _owner;
                private int _state;

                internal Enumerator(CurrentDriftCollection<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    if (_state != 0)
                    {
                        _state = 2;
                        return false;
                    }
                    _state = 1;
                    return true;
                }

                public T Current
                {
                    get
                    {
                        if (_state != 1) throw new InvalidOperationException("Current accessed outside active item.");
                        _owner.CurrentReads++;
                        _owner._emitDrift = true;
                        return _owner._item;
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
