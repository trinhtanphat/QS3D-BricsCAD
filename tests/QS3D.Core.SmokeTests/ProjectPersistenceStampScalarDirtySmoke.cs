using System;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampScalarDirtySmoke
    {
        internal static void Run()
        {
            DetectsDirectPersistedScalarChangesWithoutRevisionAdvance();
            MarkSavedRefreshesPersistedScalarSnapshot();
        }

        private static void DetectsDirectPersistedScalarChangesWithoutRevisionAdvance()
        {
            AssertScalarMutationDetected(project => project.DrawingPath = "C:\\Models\\A.dwg", "DrawingPath");
            AssertScalarMutationDetected(project => project.DrawingFingerprint = "DWG-FP-1", "DrawingFingerprint");
            AssertScalarMutationDetected(project => project.ActiveZoneId = "ZONE-1", "ActiveZoneId");
            AssertScalarMutationDetected(project => project.ActiveFloorId = "FLOOR-1", "ActiveFloorId");
        }

        private static void MarkSavedRefreshesPersistedScalarSnapshot()
        {
            var project = new ProjectState("persistence-stamp-mark-saved", "Persistence stamp mark saved");
            var stamp = new ProjectPersistenceStamp(project);

            project.DrawingPath = "C:\\Models\\B.dwg";
            project.DrawingFingerprint = "DWG-FP-2";
            project.ActiveZoneId = "ZONE-2";
            project.ActiveFloorId = "FLOOR-2";

            Require(stamp.RequiresSave(project), "Persisted scalar changes were not detected before MarkSaved.");
            stamp.MarkSaved(project);
            Require(!stamp.RequiresSave(project), "MarkSaved did not refresh the persisted scalar snapshot.");
        }

        private static void AssertScalarMutationDetected(Action<ProjectState> mutate, string label)
        {
            var project = new ProjectState("persistence-stamp-" + label, "Persistence stamp " + label);
            var stamp = new ProjectPersistenceStamp(project);
            var beforeChangeVersion = project.ChangeVersion;

            Require(!stamp.RequiresSave(project), label + " baseline unexpectedly required save.");
            mutate(project);

            Require(project.ChangeVersion == beforeChangeVersion, label + " unexpectedly advanced ChangeVersion; regression no longer targets direct scalar mutation.");
            Require(stamp.RequiresSave(project), label + " direct persisted scalar change was reported clean.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
