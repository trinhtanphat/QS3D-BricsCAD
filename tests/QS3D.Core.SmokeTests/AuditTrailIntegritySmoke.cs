using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SnapshotIsIsolatedFromStoredHistory();
            RecordRejectsCorruptStoredHistoryWithoutMutation();
            ClearRejectsCorruptStoredHistoryWithoutMutation();
            NonUtcHistoryFailsClosed();
            XmlInvalidHistoryFailsClosed();
            ValidRecordAndClearRemainCanonical();
        }

        private static void SnapshotIsIsolatedFromStoredHistory()
        {
            var project = ProjectWithValidEvent();
            var trail = AuditTrail.ForProject(project);
            var snapshot = trail.Events;

            Assert(snapshot.Count == 1, "Audit snapshot should expose the stored valid event.");
            snapshot[0].Action = "mutated-snapshot";
            snapshot[0].Detail = "changed";

            Assert(project.AuditEvents[0].Action == "Created", "AuditTrail.Events must deep-clone stored events.");
            Assert(project.AuditEvents[0].Detail == "initial", "AuditTrail.Events snapshot mutation must not change stored detail.");
        }

        private static void RecordRejectsCorruptStoredHistoryWithoutMutation()
        {
            var project = ProjectWithValidEvent();
            project.AuditEvents[0].Action = " non-canonical ";
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.AuditEvents.Count;
            var trail = AuditTrail.ForProject(project);

            var ex = ExpectInvalidOperation(() => trail.Record("Updated", "E-2", "next"));
            Assert(ex.Message.Contains("non-canonical action", StringComparison.Ordinal), "Record corruption error should identify the stored action invariant.");
            Assert(project.ChangeVersion == beforeVersion, "Failed Record must not touch project change version.");
            Assert(project.AuditEvents.Count == beforeCount, "Failed Record must not append an event.");
        }

        private static void ClearRejectsCorruptStoredHistoryWithoutMutation()
        {
            var project = ProjectWithValidEvent();
            project.AuditEvents.Add(null!);
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.AuditEvents.Count;
            var trail = AuditTrail.ForProject(project);

            var ex = ExpectInvalidOperation(trail.Clear);
            Assert(ex.Message.Contains("null event", StringComparison.Ordinal), "Clear corruption error should identify null stored history.");
            Assert(project.ChangeVersion == beforeVersion, "Failed Clear must not touch project change version.");
            Assert(project.AuditEvents.Count == beforeCount, "Failed Clear must preserve corrupt history for explicit repair rather than silently erase it.");
        }

        private static void NonUtcHistoryFailsClosed()
        {
            var project = ProjectWithValidEvent();
            project.AuditEvents[0].Utc = DateTime.SpecifyKind(project.AuditEvents[0].Utc, DateTimeKind.Local);
            var trail = AuditTrail.ForProject(project);

            var read = ExpectInvalidOperation(() => { _ = trail.Events; });
            Assert(read.Message.Contains("non-UTC", StringComparison.Ordinal), "Audit read must reject non-UTC stored timestamps.");
            var clear = ExpectInvalidOperation(trail.Clear);
            Assert(clear.Message.Contains("non-UTC", StringComparison.Ordinal), "Audit clear must reject non-UTC stored timestamps.");
        }

        private static void XmlInvalidHistoryFailsClosed()
        {
            var project = ProjectWithValidEvent();
            project.AuditEvents[0].Detail = "bad\u0001detail";
            var trail = AuditTrail.ForProject(project);

            var read = ExpectInvalidOperation(() => { _ = trail.Events; });
            Assert(read.Message.Contains("XML-invalid detail", StringComparison.Ordinal), "Audit read must reject XML-invalid stored detail.");
            var record = ExpectInvalidOperation(() => trail.Record("Updated", "E-2", "valid"));
            Assert(record.Message.Contains("XML-invalid detail", StringComparison.Ordinal), "Audit record must reject XML-invalid existing detail.");
        }

        private static void ValidRecordAndClearRemainCanonical()
        {
            var project = new ProjectState("audit-valid", "Audit valid");
            var trail = AuditTrail.ForProject(project);
            var beforeRecordVersion = project.ChangeVersion;

            trail.Record("  Updated  ", null!, null!, "Người dùng", "corr-1");
            Assert(project.ChangeVersion == beforeRecordVersion + 1L, "Valid project-backed Record should touch exactly once.");
            Assert(project.AuditEvents.Count == 1, "Valid Record should append one event.");
            var item = project.AuditEvents[0];
            Assert(item.Utc.Kind == DateTimeKind.Utc, "Recorded audit timestamp must be UTC.");
            Assert(item.Action == "Updated", "Recorded action should remain canonical/trimmed.");
            Assert(item.ElementId == string.Empty && item.Detail == string.Empty, "Null optional text should normalize to empty strings.");
            Assert(item.Actor == "Người dùng", "XML-safe Unicode actor text should be preserved.");

            var beforeClearVersion = project.ChangeVersion;
            trail.Clear();
            Assert(project.ChangeVersion == beforeClearVersion + 1L, "Valid non-empty Clear should touch exactly once.");
            Assert(project.AuditEvents.Count == 0, "Valid Clear should remove all audit events.");
            var emptyVersion = project.ChangeVersion;
            trail.Clear();
            Assert(project.ChangeVersion == emptyVersion, "Clearing an already-empty audit trail must remain a no-op.");
        }

        private static ProjectState ProjectWithValidEvent()
        {
            var project = new ProjectState("audit-integrity", "Audit integrity");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = DateTime.UtcNow,
                Action = "Created",
                ElementId = "E-1",
                Detail = "initial",
                Actor = "tester",
                CorrelationId = "corr-0"
            });
            return project;
        }

        private static InvalidOperationException ExpectInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected InvalidOperationException was not thrown.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
