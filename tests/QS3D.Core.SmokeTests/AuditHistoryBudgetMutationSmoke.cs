using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditHistoryBudgetMutationSmoke
    {
        private const int MaxStoredEvents = 10_000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            DirectAddRejectsAtCapacityWithoutMutation();
        }

        private static void DirectAddRejectsAtCapacityWithoutMutation()
        {
            var project = new ProjectState("AUDIT-BUDGET-DIRECT-ADD", "Audit budget direct add");
            var utc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);
            for (var index = 0; index < MaxStoredEvents; index++)
            {
                project.AuditEvents.Add(new AuditEvent
                {
                    Utc = utc,
                    Action = "a"
                });
            }

            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var candidate = new AuditEvent { Utc = utc, Action = "overflow" };

            Throws<InvalidOperationException>(() => project.AuditEvents.Add(candidate));

            Equal(MaxStoredEvents, project.AuditEvents.Count, "count after rejected direct add");
            Equal(beforeVersion, project.ChangeVersion, "version after rejected direct add");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "timestamp after rejected direct add");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("AuditHistoryBudgetMutationSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("AuditHistoryBudgetMutationSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
