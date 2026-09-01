using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditHistoryCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            EventsKnownCountOverrunRejectsBeforeCurrentRead();
            EventsPostTraversalCountDriftFailsClosed();
            RecordRejectsOverrunWithoutMutation();
            ClearRejectsOverrunWithoutMutation();
            StableHistoryStillReadsAndMutates();
        }

        private static void EventsKnownCountOverrunRejectsBeforeCurrentRead()
        {
            var source = new InstrumentedAuditList(new[] { Event("A"), Event("B") }, 1, 1);
            var trail = CreateTrail(source, null);

            ThrowsContaining<InvalidOperationException>(() => { var _ = trail.Events; }, "event count does not match");

            Equal(2, source.MoveNextReads);
            Equal(1, source.CurrentReads);
        }

        private static void EventsPostTraversalCountDriftFailsClosed()
        {
            var source = new InstrumentedAuditList(new[] { Event("A") }, 1, 2);
            var trail = CreateTrail(source, null);

            ThrowsContaining<InvalidOperationException>(() => { var _ = trail.Events; }, "event count does not match");

            Equal(2, source.CountReads);
            Equal(0, source.MoveNextReads);
            Equal(0, source.CurrentReads);
        }

        private static void RecordRejectsOverrunWithoutMutation()
        {
            var source = new InstrumentedAuditList(new[] { Event("A"), Event("B") }, 1, 1);
            var project = new ProjectState("P-AUDIT-COUNT", "Audit count integrity");
            var trail = CreateTrail(source, project);
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(() => trail.Record("record", "", ""), "event count does not match");

            Equal(0, source.AddCalls);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(1, source.CurrentReads);
        }

        private static void ClearRejectsOverrunWithoutMutation()
        {
            var source = new InstrumentedAuditList(new[] { Event("A"), Event("B") }, 1, 1);
            var project = new ProjectState("P-AUDIT-CLEAR", "Audit clear count integrity");
            var trail = CreateTrail(source, project);
            var beforeVersion = project.ChangeVersion;

            ThrowsContaining<InvalidOperationException>(() => trail.Clear(), "event count does not match");

            Equal(0, source.ClearCalls);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(1, source.CurrentReads);
        }

        private static void StableHistoryStillReadsAndMutates()
        {
            var source = new InstrumentedAuditList(new[] { Event("A") }, 1, 1);
            var trail = CreateTrail(source, null);

            Equal(1, trail.Events.Count);
            trail.Record("record", "", "");
            Equal(1, source.AddCalls);
            Equal(2, source.Count);
        }

        private static AuditTrail CreateTrail(IList<AuditEvent> source, ProjectState? project)
        {
            var constructor = typeof(AuditTrail).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(IList<AuditEvent>), typeof(ProjectState) },
                modifiers: null);
            if (constructor == null) throw new Exception("AuditTrail private backing-list constructor was not found.");
            return (AuditTrail)constructor.Invoke(new object?[] { source, project });
        }

        private static AuditEvent Event(string action)
        {
            return new AuditEvent { Utc = DateTime.UtcNow, Action = action };
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void ThrowsContaining<T>(Action action, string expectedText) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new Exception("Expected exception containing '" + expectedText + "', got '" + ex.Message + "'.");
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private sealed class InstrumentedAuditList : IList<AuditEvent>
        {
            private readonly List<AuditEvent> _items;
            private readonly int _initialCount;
            private readonly int _reboundCount;

            public InstrumentedAuditList(IEnumerable<AuditEvent> items, int initialCount, int reboundCount)
            {
                _items = new List<AuditEvent>(items);
                _initialCount = initialCount;
                _reboundCount = reboundCount;
            }

            public int CountReads { get; private set; }
            public int MoveNextReads { get; private set; }
            public int CurrentReads { get; private set; }
            public int AddCalls { get; private set; }
            public int ClearCalls { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    if (AddCalls > 0 || ClearCalls > 0) return _items.Count;
                    return CountReads == 1 ? _initialCount : _reboundCount;
                }
            }

            public bool IsReadOnly => false;
            public AuditEvent this[int index] { get => _items[index]; set => _items[index] = value; }

            public void Add(AuditEvent item) { AddCalls++; _items.Add(item); }
            public void Clear() { ClearCalls++; _items.Clear(); }
            public bool Contains(AuditEvent item) => _items.Contains(item);
            public void CopyTo(AuditEvent[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public int IndexOf(AuditEvent item) => _items.IndexOf(item);
            public void Insert(int index, AuditEvent item) => _items.Insert(index, item);
            public bool Remove(AuditEvent item) => _items.Remove(item);
            public void RemoveAt(int index) => _items.RemoveAt(index);

            public IEnumerator<AuditEvent> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<AuditEvent>
            {
                private readonly InstrumentedAuditList _owner;
                private int _index = -1;

                public Enumerator(InstrumentedAuditList owner) { _owner = owner; }

                public bool MoveNext()
                {
                    _owner.MoveNextReads++;
                    _index++;
                    return _index < _owner._items.Count;
                }

                public AuditEvent Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() { throw new NotSupportedException(); }
                public void Dispose() { }
            }
        }
    }
}
