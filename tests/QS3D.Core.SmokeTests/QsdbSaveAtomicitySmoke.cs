using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbSaveAtomicitySmoke
    {
        public static void Run()
        {
            FailedDurableReplaceRestoresPersistenceState();
        }

        private static void FailedDurableReplaceRestoresPersistenceState()
        {
            var project = new ProjectState("save-rollback", "Save rollback")
            {
                SchemaVersion = 2,
                UpdatedUtc = new DateTime(2026, 8, 10, 4, 5, 6, DateTimeKind.Utc)
            };
            project.Metadata["marker"] = "unchanged";
            var beforeSchema = project.SchemaVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeChangeVersion = project.ChangeVersion;
            var destinationDirectory = Path.Combine(Path.GetTempPath(), "qs3d-save-destination-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(destinationDirectory);

            var failed = false;
            try
            {
                try
                {
                    new QsdbProjectStore().Save(project, destinationDirectory);
                }
                catch (IOException)
                {
                    failed = true;
                }
                catch (UnauthorizedAccessException)
                {
                    failed = true;
                }

                Require(failed, "Saving over an existing directory unexpectedly succeeded.");
                Require(project.SchemaVersion == beforeSchema, "Failed save changed the in-memory schema version.");
                Require(project.UpdatedUtc == beforeUpdatedUtc, "Failed save changed the in-memory UpdatedUtc timestamp.");
                Require(project.ChangeVersion == beforeChangeVersion, "Failed save changed the in-memory persistence version.");
                Require(project.Metadata.TryGetValue("marker", out var marker) && marker == "unchanged", "Failed save changed unrelated semantic state.");
                Require(Directory.Exists(destinationDirectory), "Failed save replaced the destination directory.");
            }
            finally
            {
                try { if (Directory.Exists(destinationDirectory)) Directory.Delete(destinationDirectory, true); } catch { }
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
