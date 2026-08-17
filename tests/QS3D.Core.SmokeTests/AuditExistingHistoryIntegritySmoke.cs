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
            ClearRejectsNullExistingEventWithoutMutation();
            ClearRejectsNonCanonicalExistingActionWithoutMutation();
            ClearRejectsNonUtcExistingTimestampWithoutMutation();
            ClearRejectsXmlInvalidExistingFieldsWithoutMutation();
            ClearAcceptsCanonicalHistoryAndAdvancesVersionOnce();
            ClearEmptyHistoryIsNoOp();
        }

        private static void RejectsNonCanonicalExistingActionWithoutMutation()
        {
            var project = new ProjectState("AUDIT-ACTION", "Audit action history");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = Utc(0),
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
                Utc = Utc(0),
                Action = "existing.action"
            });
            var beforeVersion = project.ChangeVersion;

            AuditTrail.ForProject(project).Record(" new.action ", "E1", "detail");

            Equal(beforeVersion + 1L, project.ChangeVersion, "canonical history version increment");
            Equal(2, project.AuditEvents.Count, "canonical history count");
            Equal("new.action", project.AuditEvents[1].Action, "new action normalization");
            Equal(DateTimeKind.Utc, project.AuditEvents[1].Utc.Kind, "new audit UTC kind");
        }

        private static void ClearRejectsNullExistingEventWithoutMutation()
        {
            var project = new ProjectState("AUDIT-CLEAR-NULL", "Audit clear null history");
            project.AuditEvents.Add(null!);

            AssertClearRejectedWithoutMutation(project, "null existing event");
        }

        private static void ClearRejectsNonCanonicalExistingActionWithoutMutation()
        {
            foreach (var action in new[] { " padded.action ", "\t", "bad\u0001action" })
            {
                var project = new ProjectState("AUDIT-CLEAR-ACTION", "Audit clear action history");
                project.AuditEvents.Add(new AuditEvent { Utc = Utc(1), Action = action });

                AssertClearRejectedWithoutMutation(project, "non-canonical clear action");
            }
        }

        private static void ClearRejectsNonUtcExistingTimestampWithoutMutation()
        {
            foreach (var kind in new[] { DateTimeKind.Local, DateTimeKind.Unspecified })
            {
                var project = new ProjectState("AUDIT-CLEAR-UTC", "Audit clear timestamp history");
                project.AuditEvents.Add(new AuditEvent
                {
                    Utc = new DateTime(2026, 8, 12, 1, 2, 3, kind),
                    Action = "existing.action"
                });

                AssertClearRejectedWithoutMutation(project, "non-UTC clear timestamp " + kind);
            }
        }

        private static void ClearRejectsXmlInvalidExistingFieldsWithoutMutation()
        {
            AssertXmlInvalidClearRejected(item => item.ElementId = "E\u0001", "element id");
            AssertXmlInvalidClearRejected(item => item.Detail = "detail\u0001", "detail");
            AssertXmlInvalidClearRejected(item => item.Actor = "actor\u0001", "actor");
            AssertXmlInvalidClearRejected(item => item.CorrelationId = "corr\u0001", "correlation id");
        }

        private static void AssertXmlInvalidClearRejected(Action<AuditEvent> poison, string label)
        {
            var project = new ProjectState("AUDIT-CLEAR-XML", "Audit clear XML history");
            var item = CanonicalEvent(2);
            poison(item);
            project.AuditEvents.Add(item);

            AssertClearRejectedWithoutMutation(project, "XML-invalid " + label);
        }

        private static void AssertClearRejectedWithoutMutation(ProjectState project, string label)
        {
            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.AuditEvents.Count;
            var first = beforeCount == 0 ? null : project.AuditEvents[0];

            var ex = Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Clear());

            Equal(beforeVersion, project.ChangeVersion, label + " version");
            Equal(beforeCount, project.AuditEvents.Count, label + " count");
            if (beforeCount != 0 && !ReferenceEquals(first, project.AuditEvents[0]))
                throw new Exception("AuditExistingHistoryIntegritySmoke " + label + ": clear replaced persisted audit evidence before rejection.");
            Contains(ex.Message, "Repair the existing audit history before clearing it.", label + " repair guidance");
        }

        private static void ClearAcceptsCanonicalHistoryAndAdvancesVersionOnce()
        {
            var project = new ProjectState("AUDIT-CLEAR-VALID", "Audit clear valid history");
            project.AuditEvents.Add(CanonicalEvent(3));
            project.AuditEvents.Add(CanonicalEvent(4));
            var beforeVersion = project.ChangeVersion;

            AuditTrail.ForProject(project).Clear();

            Equal(beforeVersion + 1L, project.ChangeVersion, "valid clear version increment");
            Equal(0, project.AuditEvents.Count, "valid clear event count");
        }

        private static void ClearEmptyHistoryIsNoOp()
        {
            var project = new ProjectState("AUDIT-CLEAR-EMPTY", "Audit clear empty history");
            var beforeVersion = project.ChangeVersion;

            AuditTrail.ForProject(project).Clear();

            Equal(beforeVersion, project.ChangeVersion, "empty clear version");
            Equal(0, project.AuditEvents.Count, "empty clear count");
        }

        private static AuditEvent CanonicalEvent(int second)
        {
            return new AuditEvent
            {
                Utc = Utc(second),
                Action = "existing.action",
                ElementId = "E" + second,
                Detail = "Chi tiết hợp lệ " + second,
                Actor = "tester",
                CorrelationId = "corr-" + second
            };
        }

        private static DateTime Utc(int second)
        {
            return new DateTime(2026, 8, 12, 0, 0, second, DateTimeKind.Utc);
        }

        private static void Contains(string value, string expected, string label)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("AuditExistingHistoryIntegritySmoke " + label + ": expected message to contain '" + expected + "', actual='" + value + "'.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AuditExistingHistoryIntegritySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
            throw new Exception("AuditExistingHistoryIntegritySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
