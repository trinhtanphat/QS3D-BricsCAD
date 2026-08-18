using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailHistoryBoundSmoke
    {
        private const int MaxStoredEvents = 10_000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            ReadsExactBoundWithoutMutation();
            RejectsOversizedReadWithoutMutation();
            SnapshotUsesValidatedCountOnce();
            RejectsRecordAtCapacityWithoutMutation();
            RecordsIntoLastAvailableSlot();
            RejectsOversizedClearWithoutMutation();
        }

        private static void ReadsExactBoundWithoutMutation()
        {
            var project = BuildProject("AUDIT-BOUND-READ", MaxStoredEvents);
            var beforeVersion = project.ChangeVersion;

            var events = AuditTrail.ForProject(project).Events;

            Equal(MaxStoredEvents, events.Count, "exact-bound read count");
            Equal(beforeVersion, project.ChangeVersion, "exact-bound read version");
            if (ReferenceEquals(project.AuditEvents[0], events[0]))
                throw new Exception("AuditTrailHistoryBoundSmoke exact-bound read leaked mutable stored event reference.");
        }

        private static void RejectsOversizedReadWithoutMutation()
        {
            var project = BuildProject("AUDIT-BOUND-OVERSIZE-READ", MaxStoredEvents + 1);
            var beforeVersion = project.ChangeVersion;
            var first = project.AuditEvents[0];

            Throws<InvalidOperationException>(() => _ = AuditTrail.ForProject(project).Events);

            Equal(beforeVersion, project.ChangeVersion, "oversized read version");
            Equal(MaxStoredEvents + 1, project.AuditEvents.Count, "oversized read count");
            if (!ReferenceEquals(first, project.AuditEvents[0]))
                throw new Exception("AuditTrailHistoryBoundSmoke oversized read replaced stored evidence.");
        }

        private static void SnapshotUsesValidatedCountOnce()
        {
            var history = new SingleCountReadHistory(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc),
                Action = "history.event",
                ElementId = "E1",
                Detail = "canonical"
            });
            var constructor = typeof(AuditTrail).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(IList<AuditEvent>), typeof(ProjectState) },
                modifiers: null);
            if (constructor == null)
                throw new Exception("AuditTrailHistoryBoundSmoke could not resolve the bounded-history constructor.");

            var trail = (AuditTrail)constructor.Invoke(new object?[] { history, null });
            var events = trail.Events;

            Equal(1, events.Count, "single-count snapshot count");
            Equal(1, history.CountReads, "single-count snapshot Count reads");
        }

        private static void RejectsRecordAtCapacityWithoutMutation()
        {
            var project = BuildProject("AUDIT-BOUND-RECORD-FULL", MaxStoredEvents);
            var beforeVersion = project.ChangeVersion;
            var last = project.AuditEvents[MaxStoredEvents - 1];

            Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Record("new.action", "E1", "detail"));

            Equal(beforeVersion, project.ChangeVersion, "record-at-capacity version");
            Equal(MaxStoredEvents, project.AuditEvents.Count, "record-at-capacity count");
            if (!ReferenceEquals(last, project.AuditEvents[MaxStoredEvents - 1]))
                throw new Exception("AuditTrailHistoryBoundSmoke record-at-capacity replaced stored evidence.");
        }

        private static void RecordsIntoLastAvailableSlot()
        {
            var project = BuildProject("AUDIT-BOUND-RECORD-LAST", MaxStoredEvents - 1);
            var beforeVersion = project.ChangeVersion;

            AuditTrail.ForProject(project).Record("new.action", "E1", "detail", correlationId: "corr");

            Equal(beforeVersion + 1L, project.ChangeVersion, "last-slot version");
            Equal(MaxStoredEvents, project.AuditEvents.Count, "last-slot count");
            Equal("new.action", project.AuditEvents[MaxStoredEvents - 1].Action, "last-slot action");
        }

        private static void RejectsOversizedClearWithoutMutation()
        {
            var project = BuildProject("AUDIT-BOUND-OVERSIZE-CLEAR", MaxStoredEvents + 1);
            var beforeVersion = project.ChangeVersion;
            var first = project.AuditEvents[0];

            Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Clear());

            Equal(beforeVersion, project.ChangeVersion, "oversized clear version");
            Equal(MaxStoredEvents + 1, project.AuditEvents.Count, "oversized clear count");
            if (!ReferenceEquals(first, project.AuditEvents[0]))
                throw new Exception("AuditTrailHistoryBoundSmoke oversized clear replaced stored evidence.");
        }

        private static ProjectState BuildProject(string id, int count)
        {
            var project = new ProjectState(id, "Audit bound smoke");
            var utc = new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc);
            for (var index = 0; index < count; index++)
            {
                project.AuditEvents.Add(new AuditEvent
                {
                    Utc = utc,
                    Action = "history.event",
                    ElementId = "E" + index,
                    Detail = "canonical"
                });
            }
            return project;
        }

        private sealed class SingleCountReadHistory : IList<AuditEvent>
        {
            private readonly List<AuditEvent> _items;

            internal SingleCountReadHistory(params AuditEvent[] items)
            {
                _items = new List<AuditEvent>(items);
            }

            internal int CountReads { get; private set; }

            public int Count
            {
                get
                {
                    CountReads++;
                    if (CountReads > 1)
                        throw new InvalidOperationException("Audit history Count was read more than once before snapshot traversal.");
                    return _items.Count;
                }
            }

            public bool IsReadOnly => false;
            public AuditEvent this[int index] { get => _items[index]; set => _items[index] = value; }
            public void Add(AuditEvent item) => _items.Add(item);
            public void Clear() => _items.Clear();
            public bool Contains(AuditEvent item) => _items.Contains(item);
            public void CopyTo(AuditEvent[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public IEnumerator<AuditEvent> GetEnumerator() => _items.GetEnumerator();
            public int IndexOf(AuditEvent item) => _items.IndexOf(item);
            public void Insert(int index, AuditEvent item) => _items.Insert(index, item);
            public bool Remove(AuditEvent item) => _items.Remove(item);
            public void RemoveAt(int index) => _items.RemoveAt(index);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AuditTrailHistoryBoundSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("AuditTrailHistoryBoundSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
