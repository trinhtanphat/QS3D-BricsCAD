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
            RejectsMalformedHistoryOnClearWithoutMutation();
            RejectsNonCanonicalIdentityOnRecordWithoutMutation();
            RejectsNonCanonicalStoredIdentityWithoutMutation();
            AcceptsCanonicalHistoryAndNormalizesNewAction();
            AllowsFreeFormDetailAndEmptyIdentity();
            ClearsCanonicalHistoryAndTouchesOnce();
            EmptyClearIsNoOp();
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

        private static void RejectsMalformedHistoryOnClearWithoutMutation()
        {
            AssertClearRejected(null, "null event");
            AssertClearRejected(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Unspecified),
                Action = "existing.action"
            }, "non-UTC event");
            AssertClearRejected(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = " existing.action "
            }, "noncanonical action");
            AssertClearRejected(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = "existing.action",
                Detail = "bad\u0001detail"
            }, "XML-invalid detail");
        }

        private static void RejectsNonCanonicalIdentityOnRecordWithoutMutation()
        {
            AssertRecordIdentityRejected(" E1 ", "corr", "padded element id");
            AssertRecordIdentityRejected("E1\tchild", "corr", "control element id");
            AssertRecordIdentityRejected("E1", " corr ", "padded correlation id");
            AssertRecordIdentityRejected("E1", "corr\nchild", "control correlation id");
        }

        private static void AssertRecordIdentityRejected(string elementId, string correlationId, string label)
        {
            var project = new ProjectState("AUDIT-IDENTITY-" + label, "Audit identity integrity");
            var beforeVersion = project.ChangeVersion;

            Throws<ArgumentException>(() => AuditTrail.ForProject(project).Record("new.action", elementId, "detail", "actor", correlationId));
            Equal(beforeVersion, project.ChangeVersion, label + " record version");
            Equal(0, project.AuditEvents.Count, label + " record count");
        }

        private static void RejectsNonCanonicalStoredIdentityWithoutMutation()
        {
            AssertStoredIdentityRejected(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = "existing.action",
                ElementId = " E1 "
            }, "padded stored element id");
            AssertStoredIdentityRejected(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = "existing.action",
                CorrelationId = "corr\rchild"
            }, "control stored correlation id");
        }

        private static void AssertStoredIdentityRejected(AuditEvent item, string label)
        {
            var project = new ProjectState("AUDIT-STORED-" + label, "Audit stored identity integrity");
            project.AuditEvents.Add(item);
            var beforeVersion = project.ChangeVersion;

            Throws<InvalidOperationException>(() => _ = AuditTrail.ForProject(project).Events);
            Equal(beforeVersion, project.ChangeVersion, label + " read version");
            Equal(1, project.AuditEvents.Count, label + " read count");

            Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Record("new.action", "E2", "detail", correlationId: "corr"));
            Equal(beforeVersion, project.ChangeVersion, label + " record version");
            Equal(1, project.AuditEvents.Count, label + " record count");

            Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Clear());
            Equal(beforeVersion, project.ChangeVersion, label + " clear version");
            Equal(1, project.AuditEvents.Count, label + " clear count");
        }

        private static void AssertClearRejected(AuditEvent? item, string label)
        {
            var project = new ProjectState("AUDIT-CLEAR-" + label, "Audit clear integrity");
            project.AuditEvents.Add(item!);
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.AuditEvents.Count;

            Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Clear());
            Equal(beforeVersion, project.ChangeVersion, label + " clear version");
            Equal(beforeCount, project.AuditEvents.Count, label + " clear count");
            if (!ReferenceEquals(item, project.AuditEvents[0]))
                throw new Exception("AuditExistingHistoryIntegritySmoke " + label + " clear replaced persisted evidence.");
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

            AuditTrail.ForProject(project).Record(" new.action ", "E1", "detail", correlationId: "corr-1");

            Equal(beforeVersion + 1L, project.ChangeVersion, "canonical history version increment");
            Equal(2, project.AuditEvents.Count, "canonical history count");
            Equal("new.action", project.AuditEvents[1].Action, "new action normalization");
            Equal("E1", project.AuditEvents[1].ElementId, "canonical element id preservation");
            Equal("corr-1", project.AuditEvents[1].CorrelationId, "canonical correlation id preservation");
            Equal(DateTimeKind.Utc, project.AuditEvents[1].Utc.Kind, "new audit UTC kind");
        }

        private static void AllowsFreeFormDetailAndEmptyIdentity()
        {
            var project = new ProjectState("AUDIT-FREEFORM", "Audit free-form detail");
            AuditTrail.ForProject(project).Record("new.action", string.Empty, "line one\nline two\tcolumn", "agent", string.Empty);

            Equal(1, project.AuditEvents.Count, "free-form detail count");
            Equal(string.Empty, project.AuditEvents[0].ElementId, "empty element id");
            Equal(string.Empty, project.AuditEvents[0].CorrelationId, "empty correlation id");
            Equal("line one\nline two\tcolumn", project.AuditEvents[0].Detail, "free-form detail preservation");
        }

        private static void ClearsCanonicalHistoryAndTouchesOnce()
        {
            var project = new ProjectState("AUDIT-CLEAR-VALID", "Audit canonical clear");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc),
                Action = "existing.action",
                Detail = "canonical detail"
            });
            var beforeVersion = project.ChangeVersion;

            AuditTrail.ForProject(project).Clear();

            Equal(beforeVersion + 1L, project.ChangeVersion, "canonical clear version increment");
            Equal(0, project.AuditEvents.Count, "canonical clear count");
        }

        private static void EmptyClearIsNoOp()
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
