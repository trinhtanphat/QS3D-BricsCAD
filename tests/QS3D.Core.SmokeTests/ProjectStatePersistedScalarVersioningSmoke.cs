using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStatePersistedScalarVersioningSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PersistedScalarsAdvanceVersionExactlyOnce();
            SnapshotRestorePreservesPersistenceStamp();
        }

        private static void PersistedScalarsAdvanceVersionExactlyOnce()
        {
            AssertScalarMutation(
                "DrawingPath",
                project => project.DrawingPath,
                (project, value) => project.DrawingPath = value,
                " drawing/path.dwg ",
                " drawing/path.dwg ");
            AssertScalarMutation(
                "DrawingFingerprint",
                project => project.DrawingFingerprint,
                (project, value) => project.DrawingFingerprint = value,
                "fingerprint-value",
                "fingerprint-value");
            AssertScalarMutation(
                "ActiveZoneId",
                project => project.ActiveZoneId,
                (project, value) => project.ActiveZoneId = value,
                "zone-a",
                "zone-a");
            AssertScalarMutation(
                "ActiveFloorId",
                project => project.ActiveFloorId,
                (project, value) => project.ActiveFloorId = value,
                "floor-a",
                "floor-a");
        }

        private static void AssertScalarMutation(
            string label,
            Func<ProjectState, string> read,
            Action<ProjectState, string> write,
            string value,
            string expectedStoredValue)
        {
            var project = new ProjectState("scalar-" + label, "Persisted scalar " + label);
            var oldTimestamp = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            project.UpdatedUtc = oldTimestamp;
            var beforeVersion = project.ChangeVersion;

            write(project, value);

            Require(project.ChangeVersion == beforeVersion + 1L, label + " real change must advance ChangeVersion exactly once.");
            Require(project.UpdatedUtc != oldTimestamp, label + " real change must refresh UpdatedUtc.");
            Require(string.Equals(read(project), expectedStoredValue, StringComparison.Ordinal), label + " stored value did not match its persistence contract.");

            var changedVersion = project.ChangeVersion;
            var changedTimestamp = project.UpdatedUtc;
            write(project, value);

            Require(project.ChangeVersion == changedVersion, label + " same-value assignment must not advance ChangeVersion.");
            Require(project.UpdatedUtc == changedTimestamp, label + " same-value assignment must not refresh UpdatedUtc.");
        }

        private static void SnapshotRestorePreservesPersistenceStamp()
        {
            var project = new ProjectState("scalar-snapshot", "Persisted scalar snapshot")
            {
                DrawingPath = "original-path",
                DrawingFingerprint = "original-fingerprint"
            };
            project.Zones.Add(new ZoneDefinition("original-zone", "Original zone"));
            project.Floors.Add(new FloorDefinition("original-floor", "Original floor", 0d));
            project.ActiveZoneId = "original-zone";
            project.ActiveFloorId = "original-floor";

            var persistedTimestamp = new DateTime(2026, 8, 12, 6, 0, 0, DateTimeKind.Utc);
            project.UpdatedUtc = persistedTimestamp;
            var persistedVersion = project.ChangeVersion;
            var snapshot = ProjectStateSnapshot.Capture(project);

            project.DrawingPath = "changed-path";
            project.DrawingFingerprint = "changed-fingerprint";
            project.ActiveZoneId = "changed-zone";
            project.ActiveFloorId = "changed-floor";
            Require(project.ChangeVersion == persistedVersion + 4L, "Four real persisted-scalar changes must advance ChangeVersion four times before restore.");

            snapshot.Restore(project);

            Require(project.DrawingPath == "original-path", "Snapshot restore did not restore DrawingPath.");
            Require(project.DrawingFingerprint == "original-fingerprint", "Snapshot restore did not restore DrawingFingerprint.");
            Require(project.ActiveZoneId == "original-zone", "Snapshot restore did not restore ActiveZoneId.");
            Require(project.ActiveFloorId == "original-floor", "Snapshot restore did not restore ActiveFloorId.");
            Require(project.ChangeVersion == persistedVersion, "Snapshot restore did not restore the captured ChangeVersion after setter hydration.");
            Require(project.UpdatedUtc == persistedTimestamp, "Snapshot restore did not restore the captured UpdatedUtc after setter hydration.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
