using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailMutationIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ClearRejectsCorruptHistoryWithoutMutation();
            ClearRejectsXmlInvalidHistoryWithoutMutation();
            ClearAcceptsCanonicalHistoryAndTouchesProjectOnce();
            SnapshotIsDeepCopyIsolated();
            RecordAcceptsOptionalNullsAndXmlSafeUnicode();
        }

        private static void ClearRejectsCorruptHistoryWithoutMutation()
        {
            var project = CreateProject("AUDIT-CLEAR-UTC");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 16, 1, 2, 3, DateTimeKind.Unspecified),
                Action = "existing.action"
            });
            var beforeVersion = project.ChangeVersion;

            var error = Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Clear());

            Contains(error.Message, "non-UTC event timestamp", "clear non-UTC error");
            Contains(error.Message, "before clearing it", "clear repair instruction");
            Equal(beforeVersion, project.ChangeVersion, "clear corrupt version");
            Equal(1, project.AuditEvents.Count, "clear corrupt count");
            Equal("existing.action", project.AuditEvents[0].Action, "clear corrupt event retained");
        }

        private static void ClearRejectsXmlInvalidHistoryWithoutMutation()
        {
            var project = CreateProject("AUDIT-CLEAR-XML");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 16, 1, 2, 3, DateTimeKind.Utc),
                Action = "existing.action",
                Detail = "bad\u0001detail"
            });
            var beforeVersion = project.ChangeVersion;

            var error = Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Clear());

            Contains(error.Message, "XML-invalid detail", "clear XML error");
            Equal(beforeVersion, project.ChangeVersion, "clear XML version");
            Equal(1, project.AuditEvents.Count, "clear XML count");
        }

        private static void ClearAcceptsCanonicalHistoryAndTouchesProjectOnce()
        {
            var project = CreateProject("AUDIT-CLEAR-VALID");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 16, 1, 2, 3, DateTimeKind.Utc),
                Action = "existing.action",
                Detail = "Định mức ✓"
            });
            var beforeVersion = project.ChangeVersion;

            AuditTrail.ForProject(project).Clear();

            Equal(beforeVersion + 1L, project.ChangeVersion, "clear valid version");
            Equal(0, project.AuditEvents.Count, "clear valid count");
        }

        private static void SnapshotIsDeepCopyIsolated()
        {
            var project = CreateProject("AUDIT-SNAPSHOT");
            var stored = new AuditEvent
            {
                Utc = new DateTime(2026, 8, 16, 1, 2, 3, DateTimeKind.Utc),
                Action = "existing.action",
                Detail = "original"
            };
            project.AuditEvents.Add(stored);
            var trail = AuditTrail.ForProject(project);

            var first = trail.Events;
            first[0].Action = "caller.changed";
            first[0].Detail = "caller changed detail";
            stored.Detail = "storage changed detail";
            var second = trail.Events;

            Equal("existing.action", project.AuditEvents[0].Action, "snapshot caller cannot mutate stored action");
            Equal("storage changed detail", second[0].Detail, "fresh snapshot reflects current storage");
            Equal("original", first[0].Detail == "caller changed detail" ? "original" : first[0].Detail, "snapshot mutation remains local");
        }

        private static void RecordAcceptsOptionalNullsAndXmlSafeUnicode()
        {
            var project = CreateProject("AUDIT-UNICODE");
            var beforeVersion = project.ChangeVersion;
            var trail = AuditTrail.ForProject(project);

            trail.Record(" audit.unicode ", null!, "Khối lượng 日本語 ✓", null!, null!);

            Equal(beforeVersion + 1L, project.ChangeVersion, "record Unicode version");
            Equal(1, project.AuditEvents.Count, "record Unicode count");
            var item = project.AuditEvents[0];
            Equal(DateTimeKind.Utc, item.Utc.Kind, "record UTC kind");
            Equal("audit.unicode", item.Action, "record normalized action");
            Equal(string.Empty, item.ElementId, "record null element id");
            Equal("Khối lượng 日本語 ✓", item.Detail, "record Unicode detail");
            Equal(string.Empty, item.Actor, "record null actor");
            Equal(string.Empty, item.CorrelationId, "record null correlation");
        }

        private static ProjectState CreateProject(string id)
        {
            return new ProjectState(id, id);
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
            throw new Exception("AuditTrailMutationIntegritySmoke expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string actual, string expectedFragment, string label)
        {
            if (actual.IndexOf(expectedFragment, StringComparison.Ordinal) < 0)
                throw new Exception("AuditTrailMutationIntegritySmoke " + label + ": expected fragment='" + expectedFragment + "', actual='" + actual + "'.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AuditTrailMutationIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
