using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditExistingHistoryIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNonCanonicalExistingActionWithoutMutation();
            RejectsNonUtcExistingTimestampWithoutMutation();
            AcceptsCanonicalHistoryAndNormalizesNewAction();
            RejectsMalformedHistoryBeforeClearMutation();
            ClearsCanonicalHistoryAtomically();
            EmptyClearRemainsNoOp();
        }

        private static void RejectsNonCanonicalExistingActionWithoutMutation()
        {
            var project = new ProjectState("AUDIT-ACTION", "Audit action history");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = " existing.action "
            });
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.AuditEvents.Count;

            Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Record("new.action", "E1", "detail"));
            Equal(beforeVersion, project.ChangeVersion, "padded existing action version");
            Equal(beforeCount, project.AuditEvents.Count, "padded existing action count");
        }

        private static void RejectsNonUtcExistingTimestampWithoutMutation()
        {
            var project = new ProjectState("AUDIT-UTC", "Audit timestamp history");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Unspecified),
                Action = "existing.action"
            });
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.AuditEvents.Count;

            Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Record("new.action", "E1", "detail"));
            Equal(beforeVersion, project.ChangeVersion, "non-UTC existing timestamp version");
            Equal(beforeCount, project.AuditEvents.Count, "non-UTC existing timestamp count");
        }

        private static void AcceptsCanonicalHistoryAndNormalizesNewAction()
        {
            var project = new ProjectState("AUDIT-VALID", "Audit valid history");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = "existing.action"
            });
            var beforeVersion = project.ChangeVersion;

            AuditTrail.ForProject(project).Record(" new.action ", "E1", "detail");

            Equal(beforeVersion + 1L, project.ChangeVersion, "canonical history version increment");
            Equal(2, project.AuditEvents.Count, "canonical history count");
            Equal("new.action", project.AuditEvents[1].Action, "new action normalization");
            Equal(DateTimeKind.Utc, project.AuditEvents[1].Utc.Kind, "new audit UTC kind");
        }

        private static void RejectsMalformedHistoryBeforeClearMutation()
        {
            AssertClearRejectedWithoutMutation(null, "null event");
            AssertClearRejectedWithoutMutation(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Local),
                Action = "existing.action"
            }, "non-UTC event");
            AssertClearRejectedWithoutMutation(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = " existing.action "
            }, "noncanonical action");
            AssertClearRejectedWithoutMutation(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = "existing.action",
                Detail = "bad\u0001detail"
            }, "XML-invalid detail");
        }

        private static void AssertClearRejectedWithoutMutation(AuditEvent item, string label)
        {
            var project = new ProjectState("AUDIT-CLEAR-BAD", "Audit invalid clear");
            project.AuditEvents.Add(item);
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.AuditEvents.Count;
            var beforeReference = project.AuditEvents[0];

            Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Clear());

            Equal(beforeVersion, project.ChangeVersion, label + " clear version");
            Equal(beforeCount, project.AuditEvents.Count, label + " clear count");
            if (!ReferenceEquals(beforeReference, project.AuditEvents[0]))
                throw new Exception("AuditExistingHistoryIntegritySmoke " + label + " clear replaced persisted evidence.");
        }

        private static void ClearsCanonicalHistoryAtomically()
        {
            var project = new ProjectState("AUDIT-CLEAR-OK", "Audit valid clear");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = "existing.action",
                ElementId = "E1",
                Detail = "detail",
                Actor = "agent",
                CorrelationId = "C1"
            });
            var beforeVersion = project.ChangeVersion;

            AuditTrail.ForProject(project).Clear();

            Equal(beforeVersion + 1L, project.ChangeVersion, "canonical clear version");
            Equal(0, project.AuditEvents.Count, "canonical clear count");
        }

        private static void EmptyClearRemainsNoOp()
        {
            var project = new ProjectState("AUDIT-CLEAR-EMPTY", "Audit empty clear");
            var beforeVersion = project.ChangeVersion;
            AuditTrail.ForProject(project).Clear();
            Equal(beforeVersion, project.ChangeVersion, "empty clear version");
            Equal(0, project.AuditEvents.Count, "empty clear count");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AuditExistingHistoryIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("AuditExistingHistoryIntegritySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
