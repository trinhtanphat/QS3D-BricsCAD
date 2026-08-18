using System;
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
