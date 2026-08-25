using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampSchemaVersionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SchemaVersionOnlyChangeIsDirty();
            MarkSavedRefreshesSchemaVersion();
            OrdinaryCleanAndScalarDirtyBehaviorRemainsIntact();
        }

        private static void SchemaVersionOnlyChangeIsDirty()
        {
            var project = NewProject("schema-only");
            var stamp = new ProjectPersistenceStamp(project);
            var originalChangeVersion = project.ChangeVersion;

            project.SchemaVersion = ProjectState.CurrentSchemaVersion - 1;

            Require(project.ChangeVersion == originalChangeVersion,
                "schema-version mutation unexpectedly changed ChangeVersion, so the regression no longer isolates stamp coverage");
            Require(stamp.RequiresSave(project),
                "schema-version-only mutation was not detected as persisted dirty state");
        }

        private static void MarkSavedRefreshesSchemaVersion()
        {
            var project = NewProject("mark-saved");
            var stamp = new ProjectPersistenceStamp(project);

            project.SchemaVersion = ProjectState.CurrentSchemaVersion - 1;
            Require(stamp.RequiresSave(project), "precondition: legacy schema value should be dirty");

            stamp.MarkSaved(project);
            Require(!stamp.RequiresSave(project), "MarkSaved did not refresh the saved schema version");

            project.SchemaVersion = ProjectState.CurrentSchemaVersion;
            Require(stamp.RequiresSave(project), "subsequent schema-version change was not detected");
        }

        private static void OrdinaryCleanAndScalarDirtyBehaviorRemainsIntact()
        {
            var project = NewProject("ordinary-control");
            var stamp = new ProjectPersistenceStamp(project);

            Require(!stamp.RequiresSave(project), "fresh persistence stamp should be clean");

            project.DrawingFingerprint = "fingerprint-v2";
            Require(stamp.RequiresSave(project), "ordinary persisted scalar mutation stopped being detected");

            stamp.MarkSaved(project);
            Require(!stamp.RequiresSave(project), "ordinary MarkSaved clean control regressed");
        }

        private static ProjectState NewProject(string id)
        {
            return new ProjectState(id, "Persistence stamp schema-version smoke");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("ProjectPersistenceStampSchemaVersionSmoke: " + message + ".");
        }
    }
}
