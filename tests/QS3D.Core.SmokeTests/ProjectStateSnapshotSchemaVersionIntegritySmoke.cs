using System;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotSchemaVersionIntegritySmoke
    {
        internal static void Run()
        {
            RejectsUnsupportedSchemaVersionWithoutMutation(0);
            RejectsUnsupportedSchemaVersionWithoutMutation(-1);
            RejectsUnsupportedSchemaVersionWithoutMutation(ProjectState.CurrentSchemaVersion + 1);
            AcceptsCurrentSchemaVersion();
        }

        private static void RejectsUnsupportedSchemaVersionWithoutMutation(int schemaVersion)
        {
            var project = new ProjectState("P-SNAPSHOT-SCHEMA-" + schemaVersion, "Snapshot schema integrity")
            {
                SchemaVersion = schemaVersion
            };
            project.Touch();

            var beforeSchemaVersion = project.SchemaVersion;
            var beforeChangeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            Throws<InvalidOperationException>(() => ProjectStateSnapshot.Capture(project), "Capture accepted unsupported schema version " + schemaVersion + ".");
            Equal(beforeSchemaVersion, project.SchemaVersion, "Rejected Capture changed source SchemaVersion.");
            Equal(beforeChangeVersion, project.ChangeVersion, "Rejected Capture changed source ChangeVersion.");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Rejected Capture changed source UpdatedUtc.");

            Throws<InvalidOperationException>(() => ProjectStateSnapshot.CreateDetachedCopy(project), "CreateDetachedCopy accepted unsupported schema version " + schemaVersion + ".");
            Equal(beforeSchemaVersion, project.SchemaVersion, "Rejected detached copy changed source SchemaVersion.");
            Equal(beforeChangeVersion, project.ChangeVersion, "Rejected detached copy changed source ChangeVersion.");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Rejected detached copy changed source UpdatedUtc.");
        }

        private static void AcceptsCurrentSchemaVersion()
        {
            var project = new ProjectState("P-SNAPSHOT-SCHEMA-CURRENT", "Current schema control");
            project.SchemaVersion = ProjectState.CurrentSchemaVersion;

            _ = ProjectStateSnapshot.Capture(project);
            var copy = ProjectStateSnapshot.CreateDetachedCopy(project);

            Equal(ProjectState.CurrentSchemaVersion, copy.SchemaVersion, "Current schema version changed during detached copy.");
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception(message + " Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
