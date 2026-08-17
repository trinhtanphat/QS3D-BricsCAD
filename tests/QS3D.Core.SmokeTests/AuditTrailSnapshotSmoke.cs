using System;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class AuditTrailSnapshotSmoke
    {
        public static void Run()
        {
            EventsDoNotLeakBackingCollectionOrMutableEntries();
            ClearRejectsMalformedPersistedHistoryAtomically();
            CanonicalClearStillMutatesOnce();
            BoundMutationsFailAtomicallyAtMaxChangeVersion();
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

        private static void ClearRejectsMalformedPersistedHistoryAtomically()
        {
            AssertClearRejects(new AuditEvent
            {
                Utc = DateTime.UtcNow,
                Action = " padded ",
                Detail = "malformed"
            }, "non-canonical action");

            AssertClearRejects(new AuditEvent
            {
                Utc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local),
                Action = "local-time",
                Detail = "malformed"
            }, "non-UTC timestamp");

            AssertClearRejects(new AuditEvent
            {
                Utc = DateTime.UtcNow,
                Action = "xml-invalid-detail",
                Detail = "bad\u0001detail"
            }, "XML-invalid detail");

            var nullProject = new ProjectState("audit-null", "Audit null");
            nullProject.AuditEvents.Add(null!);
            var nullVersion = nullProject.ChangeVersion;
            Throws<InvalidOperationException>(() => AuditTrail.ForProject(nullProject).Clear());
            Require(nullProject.AuditEvents.Count == 1, "Rejected null audit history was cleared.");
            Require(nullProject.AuditEvents[0] == null, "Rejected null audit history was replaced.");
            Require(nullProject.ChangeVersion == nullVersion, "Rejected null audit history advanced project version.");
        }

        private static void AssertClearRejects(AuditEvent malformed, string scenario)
        {
            var project = new ProjectState("audit-malformed-" + Guid.NewGuid().ToString("N"), "Audit malformed");
            project.AuditEvents.Add(malformed);
            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => AuditTrail.ForProject(project).Clear());

            Require(project.AuditEvents.Count == 1, "Rejected " + scenario + " audit history was cleared.");
            Require(ReferenceEquals(project.AuditEvents[0], malformed), "Rejected " + scenario + " audit history was replaced.");
            Require(project.ChangeVersion == version, "Rejected " + scenario + " audit history advanced project version.");
            Require(project.UpdatedUtc == updatedUtc, "Rejected " + scenario + " audit history changed UpdatedUtc.");
        }

        private static void CanonicalClearStillMutatesOnce()
        {
            var project = new ProjectState("audit-clear", "Audit clear");
            var trail = AuditTrail.ForProject(project);
            trail.Record("seed", "E1", "before");
            var version = project.ChangeVersion;

            trail.Clear();

            Require(project.AuditEvents.Count == 0, "Canonical audit history was not cleared.");
            Require(project.ChangeVersion == version + 1, "Canonical non-empty audit clear should advance project version exactly once.");

            var emptyVersion = project.ChangeVersion;
            trail.Clear();
            Require(project.ChangeVersion == emptyVersion, "Clearing empty audit history should remain a no-op.");
        }

        private static void BoundMutationsFailAtomicallyAtMaxChangeVersion()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-audit-overflow-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                File.WriteAllText(path,
                    "<qs3d schema=\"3\" projectId=\"audit-overflow\" name=\"Audit overflow\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\" changeVersion=\"9223372036854775807\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                    "<metadata/><zones/><floors/><families/><rules/><elements/><audit>" +
                    "<event utc=\"2026-08-11T00:00:00.0000000Z\" action=\"seed\" elementId=\"\" detail=\"before\" actor=\"\" correlationId=\"\"/>" +
                    "</audit></qs3d>");
                var project = new QsdbProjectStore().Load(path);
                var trail = AuditTrail.ForProject(project);
                var expectedUpdatedUtc = project.UpdatedUtc;

                Throws<OverflowException>(() => trail.Record("overflow", string.Empty, "must-not-commit"));
                Require(project.AuditEvents.Count == 1, "Failed bound audit Record appended an event before version overflow.");
                Require(project.AuditEvents[0].Action == "seed", "Failed bound audit Record changed authoritative audit history.");
                Require(project.ChangeVersion == long.MaxValue, "Failed bound audit Record changed the maximum project version.");
                Require(project.UpdatedUtc == expectedUpdatedUtc, "Failed bound audit Record changed UpdatedUtc.");

                Throws<OverflowException>(() => trail.Clear());
                Require(project.AuditEvents.Count == 1, "Failed bound audit Clear removed history before version overflow.");
                Require(project.AuditEvents[0].Action == "seed", "Failed bound audit Clear changed authoritative audit history.");
                Require(project.ChangeVersion == long.MaxValue, "Failed bound audit Clear changed the maximum project version.");
                Require(project.UpdatedUtc == expectedUpdatedUtc, "Failed bound audit Clear changed UpdatedUtc.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
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
