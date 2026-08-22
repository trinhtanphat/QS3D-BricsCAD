using System;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailNullBackingIntegritySmoke
    {
        public static void Run()
        {
            NullBackingEventBlocksRecordBeforeProjectMutation();
        }

        private static void NullBackingEventBlocksRecordBeforeProjectMutation()
        {
            var project = new ProjectState("audit-null-backing", "Audit null backing");
            var seed = new AuditEvent
            {
                Utc = DateTime.UtcNow,
                Action = "seed",
                ElementId = "E1",
                Detail = "before"
            };
            project.AuditEvents.Add(seed);
            project.AuditEvents.Add(null!);
            var trail = AuditTrail.ForProject(project);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeCount = project.AuditEvents.Count;

            Throws<InvalidOperationException>(() => trail.Record("valid.action", "E2", "must-not-append"));

            Require(project.ChangeVersion == beforeVersion, "Rejected audit Record changed project ChangeVersion.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "Rejected audit Record changed project UpdatedUtc.");
            Require(project.AuditEvents.Count == beforeCount, "Rejected audit Record changed authoritative audit count.");
            Require(ReferenceEquals(project.AuditEvents[0], seed), "Rejected audit Record replaced or reordered the existing valid event.");
            Require(project.AuditEvents[1] == null, "Rejected audit Record mutated the existing null corruption instead of leaving repair explicit.");

            project.AuditEvents.RemoveAt(1);
            trail.Record("valid.action", "E2", "after-repair");
            Require(project.ChangeVersion == checked(beforeVersion + 1L), "Valid audit Record after repair did not advance ChangeVersion exactly once.");
            Require(project.AuditEvents.Count == 2, "Valid audit Record after repair did not append exactly one event.");
            Require(project.AuditEvents[1].Action == "valid.action", "Valid audit Record after repair stored the wrong action.");
            Require(project.AuditEvents[1].Detail == "after-repair", "Valid audit Record after repair stored the wrong detail.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
