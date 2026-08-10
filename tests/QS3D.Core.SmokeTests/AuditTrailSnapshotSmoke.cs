using System;
using System.Collections.Generic;
using QS3D.Core.Audit;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailSnapshotSmoke
    {
        public static void Run()
        {
            EventsDoNotLeakBackingCollectionOrMutableEntries();
        }

        private static void EventsDoNotLeakBackingCollectionOrMutableEntries()
        {
            var project = new ProjectState("audit-snapshot", "Audit snapshot");
            var trail = AuditTrail.ForProject(project);
            trail.Record("first", "E1", "before", "actor", "corr");

            var exposed = trail.Events;
            Require(exposed.Count == 1, "Audit snapshot should contain the recorded event.");
            Require(!(exposed is List<AuditEvent>), "Audit Events must not expose the mutable backing List.");

            exposed[0].Action = "MUTATED";
            exposed[0].Detail = "MUTATED";
            Require(project.AuditEvents[0].Action == "first", "Mutating an exposed AuditEvent changed project audit state.");
            Require(project.AuditEvents[0].Detail == "before", "Mutating exposed audit detail changed project audit state.");

            trail.Record("second", "E2", "after");
            Require(exposed.Count == 1, "An Audit Events read should be an immutable point-in-time snapshot.");
            Require(trail.Events.Count == 2, "A fresh Audit Events snapshot should include later records.");
            Require(trail.Events[0].Action == "first", "Fresh audit snapshot did not preserve authoritative event values.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
