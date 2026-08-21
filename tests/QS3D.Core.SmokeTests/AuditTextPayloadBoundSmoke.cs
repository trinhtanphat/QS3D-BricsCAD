using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTextPayloadBoundSmoke
    {
        private const int TextBudget = 8 * 1024 * 1024;

        [ModuleInitializer]
        internal static void Initialize()
        {
            ExactAggregateBudgetIsAcceptedAndNextAppendIsRefused();
            OversizedExistingAggregateFailsClosedWithoutMutation();
        }

        private static void ExactAggregateBudgetIsAcceptedAndNextAppendIsRefused()
        {
            var project = new ProjectState("AUDIT-TEXT-EXACT", "Audit text exact boundary");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc),
                Action = "a",
                Detail = new string('d', TextBudget - 2)
            });
            var beforeVersion = project.ChangeVersion;
            var trail = AuditTrail.ForProject(project);

            trail.Record("b", string.Empty, string.Empty);

            Equal(beforeVersion + 1L, project.ChangeVersion, "exact-bound append version");
            Equal(2, project.AuditEvents.Count, "exact-bound append count");
            Equal(2, trail.Events.Count, "exact-bound snapshot count");

            var exactVersion = project.ChangeVersion;
            Throws<InvalidOperationException>(() => trail.Record("c", string.Empty, string.Empty));
            Equal(exactVersion, project.ChangeVersion, "one-over append version");
            Equal(2, project.AuditEvents.Count, "one-over append count");
        }

        private static void OversizedExistingAggregateFailsClosedWithoutMutation()
        {
            var project = new ProjectState("AUDIT-TEXT-EXISTING", "Audit text existing overflow");
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 21, 0, 0, 1, DateTimeKind.Utc),
                Action = "a",
                Detail = new string('x', (TextBudget / 2) - 1)
            });
            project.AuditEvents.Add(new AuditEvent
            {
                Utc = new DateTime(2026, 8, 21, 0, 0, 2, DateTimeKind.Utc),
                Action = "b",
                Detail = new string('y', TextBudget / 2)
            });
            var first = project.AuditEvents[0];
            var second = project.AuditEvents[1];
            var beforeVersion = project.ChangeVersion;
            var trail = AuditTrail.ForProject(project);

            Throws<InvalidOperationException>(() => _ = trail.Events);
            Equal(beforeVersion, project.ChangeVersion, "oversized read version");
            Equal(2, project.AuditEvents.Count, "oversized read count");

            Throws<InvalidOperationException>(() => trail.Clear());
            Equal(beforeVersion, project.ChangeVersion, "oversized clear version");
            Equal(2, project.AuditEvents.Count, "oversized clear count");

            Throws<InvalidOperationException>(() => trail.Record("new.action", "E1", "detail"));
            Equal(beforeVersion, project.ChangeVersion, "oversized append version");
            Equal(2, project.AuditEvents.Count, "oversized append count");
            if (!ReferenceEquals(first, project.AuditEvents[0]) || !ReferenceEquals(second, project.AuditEvents[1]))
                throw new Exception("AuditTextPayloadBoundSmoke oversized history was replaced during refusal.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AuditTextPayloadBoundSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("AuditTextPayloadBoundSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
