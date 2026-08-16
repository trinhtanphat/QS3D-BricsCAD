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
            StoredActionCorruptionMatrixFailsClosed();
            NonUtcHistoryFailsClosed();
            XmlInvalidOptionalFieldMatrixFailsClosed();
            InvalidNewRecordFailsBeforeProjectMutation();
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
            var first = project.AuditEvents[0];
            var trail = AuditTrail.ForProject(project);

            var ex = ExpectInvalidOperation(trail.Clear);
            Assert(ex.Message.Contains("null event", StringComparison.Ordinal), "Clear corruption error should identify null stored history.");
            Assert(ex.Message.Contains("before clearing", StringComparison.Ordinal), "Clear corruption error should explain the destructive-operation repair boundary.");
            Assert(project.ChangeVersion == beforeVersion, "Failed Clear must not touch project change version.");
            Assert(project.AuditEvents.Count == beforeCount, "Failed Clear must preserve corrupt history for explicit repair rather than silently erase it.");
            Assert(ReferenceEquals(project.AuditEvents[0], first), "Failed Clear must retain the original stored events and ordering.");
            Assert(project.AuditEvents[1] == null, "Failed Clear must retain the corrupt null event for explicit repair.");
        }

        private static void StoredActionCorruptionMatrixFailsClosed()
        {
            AssertStoredActionRejected(null!, "non-canonical action");
            AssertStoredActionRejected(string.Empty, "non-canonical action");
            AssertStoredActionRejected("   ", "non-canonical action");
            AssertStoredActionRejected(" Padded ", "non-canonical action");
            AssertStoredActionRejected("Bad\tAction", "non-canonical action");
            AssertStoredActionRejected("Bad\u0001Action", "non-canonical action");
        }

        private static void AssertStoredActionRejected(string action, string expectedMessage)
        {
            var project = ProjectWithValidEvent();
            project.AuditEvents[0].Action = action;
            var beforeVersion = project.ChangeVersion;
            var trail = AuditTrail.ForProject(project);

            var read = ExpectInvalidOperation(() => { _ = trail.Events; });
            Assert(read.Message.Contains(expectedMessage, StringComparison.Ordinal), "Audit read should reject corrupt stored action.");
            var clear = ExpectInvalidOperation(trail.Clear);
            Assert(clear.Message.Contains(expectedMessage, StringComparison.Ordinal), "Audit clear should reject corrupt stored action.");
            Assert(project.ChangeVersion == beforeVersion, "Stored action corruption must not mutate project version.");
            Assert(project.AuditEvents.Count == 1, "Stored action corruption must not erase history.");
        }

        private static void NonUtcHistoryFailsClosed()
        {
            foreach (var kind in new[] { DateTimeKind.Local, DateTimeKind.Unspecified })
            {
                var project = ProjectWithValidEvent();
                project.AuditEvents[0].Utc = DateTime.SpecifyKind(project.AuditEvents[0].Utc, kind);
                var beforeVersion = project.ChangeVersion;
                var trail = AuditTrail.ForProject(project);

                var read = ExpectInvalidOperation(() => { _ = trail.Events; });
                Assert(read.Message.Contains("non-UTC", StringComparison.Ordinal), "Audit read must reject non-UTC stored timestamps.");
                var record = ExpectInvalidOperation(() => trail.Record("Updated", "E-2", "valid"));
                Assert(record.Message.Contains("non-UTC", StringComparison.Ordinal), "Audit record must reject non-UTC existing timestamps.");
                var clear = ExpectInvalidOperation(trail.Clear);
                Assert(clear.Message.Contains("non-UTC", StringComparison.Ordinal), "Audit clear must reject non-UTC stored timestamps.");
                Assert(project.ChangeVersion == beforeVersion, "Non-UTC corruption must leave project version unchanged.");
                Assert(project.AuditEvents.Count == 1, "Non-UTC corruption must preserve stored history.");
            }
        }

        private static void XmlInvalidOptionalFieldMatrixFailsClosed()
        {
            AssertXmlInvalidStoredField(item => item.ElementId = "bad\u0001id", "XML-invalid element id");
            AssertXmlInvalidStoredField(item => item.Detail = "bad\u0001detail", "XML-invalid detail");
            AssertXmlInvalidStoredField(item => item.Actor = "bad\u0001actor", "XML-invalid actor");
            AssertXmlInvalidStoredField(item => item.CorrelationId = "bad\u0001corr", "XML-invalid correlation id");
        }

        private static void AssertXmlInvalidStoredField(Action<AuditEvent> mutate, string expectedMessage)
        {
            var project = ProjectWithValidEvent();
            mutate(project.AuditEvents[0]);
            var beforeVersion = project.ChangeVersion;
            var trail = AuditTrail.ForProject(project);

            var read = ExpectInvalidOperation(() => { _ = trail.Events; });
            Assert(read.Message.Contains(expectedMessage, StringComparison.Ordinal), "Audit read should identify XML-invalid stored field.");
            var record = ExpectInvalidOperation(() => trail.Record("Updated", "E-2", "valid"));
            Assert(record.Message.Contains(expectedMessage, StringComparison.Ordinal), "Audit record should identify XML-invalid existing field.");
            var clear = ExpectInvalidOperation(trail.Clear);
            Assert(clear.Message.Contains(expectedMessage, StringComparison.Ordinal), "Audit clear should identify XML-invalid existing field.");
            Assert(project.ChangeVersion == beforeVersion, "XML-invalid stored history must leave project version unchanged.");
            Assert(project.AuditEvents.Count == 1, "XML-invalid stored history must be preserved for repair.");
        }

        private static void InvalidNewRecordFailsBeforeProjectMutation()
        {
            var project = ProjectWithValidEvent();
            var trail = AuditTrail.ForProject(project);
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.AuditEvents.Count;

            ExpectArgument(() => trail.Record("   ", "E-2", "detail"), "action");
            ExpectArgument(() => trail.Record("Bad\tAction", "E-2", "detail"), "action");
            ExpectArgument(() => trail.Record("Updated", "bad\u0001id", "detail"), "elementId");
            ExpectArgument(() => trail.Record("Updated", "E-2", "bad\u0001detail"), "detail");
            ExpectArgument(() => trail.Record("Updated", "E-2", "detail", "bad\u0001actor"), "actor");
            ExpectArgument(() => trail.Record("Updated", "E-2", "detail", "actor", "bad\u0001corr"), "correlationId");

            Assert(project.ChangeVersion == beforeVersion, "Invalid new audit records must not touch project version.");
            Assert(project.AuditEvents.Count == beforeCount, "Invalid new audit records must not append partial events.");
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
            Assert(item.CorrelationId == "corr-1", "Correlation id should be preserved.");

            var snapshot = trail.Events;
            Assert(snapshot.Count == 1 && snapshot[0].Action == "Updated", "Valid audit snapshot should expose the new canonical event.");

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

        private static ArgumentException ExpectArgument(Action action, string parameterName)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex)
            {
                Assert(ex.ParamName == parameterName, "Argument validation should identify parameter " + parameterName + ".");
                return ex;
            }

            throw new InvalidOperationException("Expected ArgumentException for " + parameterName + " was not thrown.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
