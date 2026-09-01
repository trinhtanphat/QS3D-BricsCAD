using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbSaveAtomicitySmoke
    {
        public static void Run()
        {
            FailedDurableReplaceRestoresPersistenceState();
            SuccessfulSaveRoundTripsChangeVersion();
            MissingPrimaryReplacementRetiresStaleBackup();
            MissingCurrentChangeVersionIsRejected();
            InvalidPersistedChangeVersionIsRejected();
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

        private static void SuccessfulSaveRoundTripsChangeVersion()
        {
            var path = TempProjectPath("version-roundtrip");
            try
            {
                var project = new ProjectState("version-roundtrip", "Version roundtrip");
                project.Touch();
                project.Touch();
                var beforeSaveVersion = project.ChangeVersion;
                var store = new QsdbProjectStore();

                store.Save(project, path);
                Require(project.ChangeVersion == beforeSaveVersion + 1L, "Successful save did not advance the in-memory change version exactly once.");

                var loaded = store.Load(path);
                Require(loaded.ChangeVersion == project.ChangeVersion, "QSDB load did not restore the persisted change version.");
                Require(loaded.UpdatedUtc == project.UpdatedUtc, "QSDB load did not restore the persisted UpdatedUtc timestamp.");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void MissingPrimaryReplacementRetiresStaleBackup()
        {
            var path = TempProjectPath("missing-primary-stale-backup");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(new ProjectState("generation", "First"), path);
                store.Save(new ProjectState("generation", "Second"), path);
                Require(File.Exists(path + ".bak"), "Replacement setup did not create the prior-generation backup.");
                Require(store.Load(path + ".bak").Name == "First", "Replacement setup did not preserve the expected first generation in backup.");

                File.Delete(path);
                Require(!File.Exists(path) && File.Exists(path + ".bak"), "Missing-primary setup did not leave only the stale backup generation.");

                store.Save(new ProjectState("generation", "Third"), path);
                Require(store.Load(path).Name == "Third", "Missing-primary replacement did not publish the new primary generation.");
                Require(!File.Exists(path + ".bak"), "Missing-primary replacement left the stale prior-generation backup eligible for fallback.");

                File.WriteAllText(path, "<broken");
                var rejected = false;
                try
                {
                    store.LoadWithBackupFallback(path);
                }
                catch (Exception ex) when (ex is InvalidDataException || ex is System.Xml.XmlException)
                {
                    rejected = true;
                }
                Require(rejected, "Corrupt recreated primary resurrected a stale backup generation through fallback.");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void MissingCurrentChangeVersionIsRejected()
        {
            var path = TempProjectPath("missing-current-version");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(new ProjectState("missing-current-version", "Missing current version"), path);
                var document = XDocument.Load(path);
                document.Root?.Attribute("changeVersion")?.Remove();
                document.Save(path, SaveOptions.DisableFormatting);

                Throws<InvalidDataException>(
                    () => store.Load(path),
                    "Current schema-3 QSDB without changeVersion was accepted instead of failing the strict persistence boundary.");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void InvalidPersistedChangeVersionIsRejected()
        {
            foreach (var invalid in new[] { "-1", "1.5", " 1", "9223372036854775808" })
            {
                var path = TempProjectPath("invalid-version");
                try
                {
                    var store = new QsdbProjectStore();
                    store.Save(new ProjectState("invalid-version", "Invalid version"), path);
                    var document = XDocument.Load(path);
                    var root = document.Root ?? throw new Exception("Saved QSDB has no root element.");
                    root.SetAttributeValue("changeVersion", invalid);
                    document.Save(path, SaveOptions.DisableFormatting);

                    Throws<InvalidDataException>(() => store.Load(path), "Invalid QSDB changeVersion was accepted: " + invalid);
                }
                finally
                {
                    Cleanup(path);
                }
            }
        }

        private static string TempProjectPath(string prefix) =>
            Path.Combine(Path.GetTempPath(), "qs3d-" + prefix + "-" + Guid.NewGuid().ToString("N") + ".qsdb");

        private static void Cleanup(string path)
        {
            foreach (var candidate in new[] { path, path + ".bak", path + ".tmp", path + ".lock" })
            {
                try { if (File.Exists(candidate)) File.Delete(candidate); } catch { }
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception(message);
        }
    }
}
