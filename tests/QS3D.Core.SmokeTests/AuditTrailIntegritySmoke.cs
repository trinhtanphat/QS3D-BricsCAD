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
            SnapshotIsDeepCopy();
            ClearRejectsCorruptHistoryAtomically();
            RecordRejectsCorruptHistoryAtomically();
            StoredActionMatrixFailsClosed();
            NonUtcHistoryFailsClosed();
            XmlInvalidStoredFieldsFailClosed();
            InvalidNewRecordDoesNotMutate();
            ValidRecordAndClearRemainCompatible();
        }

        private static void SnapshotIsDeepCopy()
        {
            var project = ProjectWithValidEvent();
            var trail = AuditTrail.ForProject(project);
            var snapshot = trail.Events;
            Assert(snapshot.Count == 1, "Expected one stored audit event.");
            snapshot[0].Action = "Changed";
            snapshot[0].Detail = "Changed detail";
            Assert(project.AuditEvents[0].Action == "Created", "Events must deep-copy stored audit action.");
            Assert(project.AuditEvents[0].Detail == "initial", "Events must deep-copy stored audit detail.");
        }

        private static void ClearRejectsCorruptHistoryAtomically()
        {
            var project = ProjectWithValidEvent();
            project.AuditEvents.Add(null!);
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.AuditEvents.Count;
            var first = project.AuditEvents[0];

            var ex = ExpectInvalidOperation(AuditTrail.ForProject(project).Clear);
            Assert(ex.Message.Contains("null event", StringComparison.Ordinal), "Clear should identify corrupt stored history.");
            Assert(ex.Message.Contains("before clearing", StringComparison.Ordinal), "Clear should explain destructive repair boundary.");
            Assert(project.ChangeVersion == beforeVersion, "Failed Clear must not touch project version.");
            Assert(project.AuditEvents.Count == beforeCount, "Failed Clear must not erase history.");
            Assert(ReferenceEquals(project.AuditEvents[0], first), "Failed Clear must preserve existing event identity/order.");
            Assert(project.AuditEvents[1] == null, "Failed Clear must preserve corrupt evidence for repair.");
        }

        private static void RecordRejectsCorruptHistoryAtomically()
        {
            var project = ProjectWithValidEvent();
            project.AuditEvents[0].Action = " non-canonical ";
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.AuditEvents.Count;

            var ex = ExpectInvalidOperation(() => AuditTrail.ForProject(project).Record("Updated", "E-2", "next"));
            Assert(ex.Message.Contains("non-canonical action", StringComparison.Ordinal), "Record should identify stored action corruption.");
            Assert(ex.Message.Contains("before recording", StringComparison.Ordinal), "Record should explain append repair boundary.");
            Assert(project.ChangeVersion == beforeVersion, "Failed Record must not touch project version.");
            Assert(project.AuditEvents.Count == beforeCount, "Failed Record must not append partial history.");
        }

        private static void StoredActionMatrixFailsClosed()
        {
            foreach (var action in new string?[] { null, string.Empty, "   ", " Padded ", "Bad\tAction", "Bad\u0001Action" })
            {
                var project = ProjectWithValidEvent();
                project.AuditEvents[0].Action = action!;
                var beforeVersion = project.ChangeVersion;
                var trail = AuditTrail.ForProject(project);
                Assert(ExpectInvalidOperation(() => { _ = trail.Events; }).Message.Contains("non-canonical action", StringComparison.Ordinal), "Read should reject corrupt stored action.");
                Assert(ExpectInvalidOperation(trail.Clear).Message.Contains("non-canonical action", StringComparison.Ordinal), "Clear should reject corrupt stored action.");
                Assert(ExpectInvalidOperation(() => trail.Record("Updated", "E-2", "valid")).Message.Contains("non-canonical action", StringComparison.Ordinal), "Record should reject corrupt stored action.");
                Assert(project.ChangeVersion == beforeVersion && project.AuditEvents.Count == 1, "Stored action corruption must remain non-destructive.");
            }
        }

        private static void NonUtcHistoryFailsClosed()
        {
            foreach (var kind in new[] { DateTimeKind.Local, DateTimeKind.Unspecified })
            {
                var project = ProjectWithValidEvent();
                project.AuditEvents[0].Utc = DateTime.SpecifyKind(project.AuditEvents[0].Utc, kind);
                var beforeVersion = project.ChangeVersion;
                var trail = AuditTrail.ForProject(project);
                Assert(ExpectInvalidOperation(() => { _ = trail.Events; }).Message.Contains("non-UTC", StringComparison.Ordinal), "Read must reject non-UTC timestamp.");
                Assert(ExpectInvalidOperation(trail.Clear).Message.Contains("non-UTC", StringComparison.Ordinal), "Clear must reject non-UTC timestamp.");
                Assert(ExpectInvalidOperation(() => trail.Record("Updated", "E-2", "valid")).Message.Contains("non-UTC", StringComparison.Ordinal), "Record must reject non-UTC timestamp.");
                Assert(project.ChangeVersion == beforeVersion && project.AuditEvents.Count == 1, "Non-UTC corruption must remain non-destructive.");
            }
        }

        private static void XmlInvalidStoredFieldsFailClosed()
        {
            AssertStoredFieldRejected(x => x.ElementId = "bad\u0001id", "XML-invalid element id");
            AssertStoredFieldRejected(x => x.Detail = "bad\u0001detail", "XML-invalid detail");
            AssertStoredFieldRejected(x => x.Actor = "bad\u0001actor", "XML-invalid actor");
            AssertStoredFieldRejected(x => x.CorrelationId = "bad\u0001corr", "XML-invalid correlation id");
        }

        private static void AssertStoredFieldRejected(Action<AuditEvent> corrupt, string marker)
        {
            var project = ProjectWithValidEvent();
            corrupt(project.AuditEvents[0]);
            var beforeVersion = project.ChangeVersion;
            var trail = AuditTrail.ForProject(project);
            Assert(ExpectInvalidOperation(() => { _ = trail.Events; }).Message.Contains(marker, StringComparison.Ordinal), "Read should identify XML-invalid field.");
            Assert(ExpectInvalidOperation(trail.Clear).Message.Contains(marker, StringComparison.Ordinal), "Clear should identify XML-invalid field.");
            Assert(ExpectInvalidOperation(() => trail.Record("Updated", "E-2", "valid")).Message.Contains(marker, StringComparison.Ordinal), "Record should identify XML-invalid field.");
            Assert(project.ChangeVersion == beforeVersion && project.AuditEvents.Count == 1, "XML-invalid history must remain non-destructive.");
        }

        private static void InvalidNewRecordDoesNotMutate()
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
            Assert(project.ChangeVersion == beforeVersion && project.AuditEvents.Count == beforeCount, "Invalid new records must fail before project/history mutation.");
        }

        private static void ValidRecordAndClearRemainCompatible()
        {
            var project = new ProjectState("audit-valid", "Audit valid");
            var trail = AuditTrail.ForProject(project);
            var v0 = project.ChangeVersion;
            trail.Record("  Updated  ", null!, null!, "Người dùng", "corr-1");
            Assert(project.ChangeVersion == v0 + 1 && project.AuditEvents.Count == 1, "Valid Record should touch exactly once and append once.");
            var item = project.AuditEvents[0];
            Assert(item.Utc.Kind == DateTimeKind.Utc && item.Action == "Updated", "Recorded event must be canonical UTC with trimmed action.");
            Assert(item.ElementId == string.Empty && item.Detail == string.Empty, "Null optional record text must normalize to empty strings.");
            Assert(item.Actor == "Người dùng" && item.CorrelationId == "corr-1", "Valid Unicode/metadata must be preserved.");

            var v1 = project.ChangeVersion;
            trail.Clear();
            Assert(project.ChangeVersion == v1 + 1 && project.AuditEvents.Count == 0, "Valid non-empty Clear should touch once and clear history.");
            var v2 = project.ChangeVersion;
            trail.Clear();
            Assert(project.ChangeVersion == v2, "Empty Clear must remain a no-op.");
        }

        private static ProjectState ProjectWithValidEvent()
        {
            var project = new ProjectState("audit-integrity", "Audit integrity");
            project.AuditEvents.Add(new AuditEvent { Utc = DateTime.UtcNow, Action = "Created", ElementId = "E-1", Detail = "initial", Actor = "tester", CorrelationId = "corr-0" });
            return project;
        }

        private static InvalidOperationException ExpectInvalidOperation(Action action)
        {
            try { action(); }
            catch (InvalidOperationException ex) { return ex; }
            throw new InvalidOperationException("Expected InvalidOperationException was not thrown.");
        }

        private static void ExpectArgument(Action action, string parameterName)
        {
            try { action(); }
            catch (ArgumentException ex)
            {
                Assert(ex.ParamName == parameterName, "Argument validation should identify parameter " + parameterName + ".");
                return;
            }
            throw new InvalidOperationException("Expected ArgumentException for " + parameterName + " was not thrown.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
