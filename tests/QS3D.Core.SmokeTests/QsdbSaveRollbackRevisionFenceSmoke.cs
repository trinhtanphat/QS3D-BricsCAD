using System;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbSaveRollbackRevisionFenceSmoke
    {
        internal static void Run()
        {
            RestoresOnlyOwnedSaveRevision();
            RefusesToClobberAdvancedRevision();
        }

        private static void RestoresOnlyOwnedSaveRevision()
        {
            var project = new ProjectState("qsdb-rollback-owned", "Owned rollback")
            {
                SchemaVersion = ProjectState.CurrentSchemaVersion - 1
            };
            var previousSchemaVersion = project.SchemaVersion;
            var previousUpdatedUtc = project.UpdatedUtc;
            var previousChangeVersion = project.ChangeVersion;

            project.SchemaVersion = ProjectState.CurrentSchemaVersion;
            project.Touch();
            var saveOwnedSchemaVersion = project.SchemaVersion;
            var saveOwnedUpdatedUtc = project.UpdatedUtc;
            var saveOwnedChangeVersion = project.ChangeVersion;

            var restored = InvokeRollback(
                project,
                previousSchemaVersion,
                previousUpdatedUtc,
                previousChangeVersion,
                saveOwnedSchemaVersion,
                saveOwnedUpdatedUtc,
                saveOwnedChangeVersion);

            Require(restored, "Failed-save rollback should restore the exact revision owned by the save attempt.");
            Require(project.SchemaVersion == previousSchemaVersion, "Owned rollback did not restore schema version.");
            Require(project.UpdatedUtc == previousUpdatedUtc, "Owned rollback did not restore timestamp.");
            Require(project.ChangeVersion == previousChangeVersion, "Owned rollback did not restore change version.");
        }

        private static void RefusesToClobberAdvancedRevision()
        {
            var project = new ProjectState("qsdb-rollback-drift", "Drift rollback")
            {
                SchemaVersion = ProjectState.CurrentSchemaVersion - 1
            };
            var previousSchemaVersion = project.SchemaVersion;
            var previousUpdatedUtc = project.UpdatedUtc;
            var previousChangeVersion = project.ChangeVersion;

            project.SchemaVersion = ProjectState.CurrentSchemaVersion;
            project.Touch();
            var saveOwnedSchemaVersion = project.SchemaVersion;
            var saveOwnedUpdatedUtc = project.UpdatedUtc;
            var saveOwnedChangeVersion = project.ChangeVersion;

            project.Touch();
            var advancedSchemaVersion = project.SchemaVersion;
            var advancedUpdatedUtc = project.UpdatedUtc;
            var advancedChangeVersion = project.ChangeVersion;

            var restored = InvokeRollback(
                project,
                previousSchemaVersion,
                previousUpdatedUtc,
                previousChangeVersion,
                saveOwnedSchemaVersion,
                saveOwnedUpdatedUtc,
                saveOwnedChangeVersion);

            Require(!restored, "Failed-save rollback must refuse a project revision advanced after the save-owned Touch().");
            Require(project.SchemaVersion == advancedSchemaVersion, "Refused rollback changed the advanced schema version.");
            Require(project.UpdatedUtc == advancedUpdatedUtc, "Refused rollback changed the advanced timestamp.");
            Require(project.ChangeVersion == advancedChangeVersion, "Refused rollback changed the advanced change version.");
        }

        private static bool InvokeRollback(
            ProjectState project,
            int previousSchemaVersion,
            DateTime previousUpdatedUtc,
            long previousChangeVersion,
            int saveOwnedSchemaVersion,
            DateTime saveOwnedUpdatedUtc,
            long saveOwnedChangeVersion)
        {
            var method = typeof(QsdbProjectStore).GetMethod(
                "RestoreFailedSavePersistenceState",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("QSDB rollback helper was not found.");

            return (bool)(method.Invoke(null, new object[]
            {
                project,
                previousSchemaVersion,
                previousUpdatedUtc,
                previousChangeVersion,
                saveOwnedSchemaVersion,
                saveOwnedUpdatedUtc,
                saveOwnedChangeVersion
            }) ?? false);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
