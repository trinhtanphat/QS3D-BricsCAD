using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbBackupFallbackRecoveryReasonSmoke
    {
        private const string StableReason = "Primary QSDB was invalid; loaded validated backup.";

        public static void Run()
        {
            MalformedPrimaryUsesStableReason();
            StructurallyInvalidPrimaryUsesStableReason();
            MissingBackupPreservesPrimaryFailure();
            ValidPrimaryDoesNotReportRecovery();
        }

        private static void MalformedPrimaryUsesStableReason()
        {
            WithTempDirectory(directory =>
            {
                var primary = Path.Combine(directory, "malformed.qsdb");
                var backup = primary + ".bak";
                var store = new QsdbProjectStore();
                store.SaveNew(new ProjectState("backup-project", "Backup project"), backup);
                File.WriteAllText(primary, "<qs3d><attacker-sentinel>");

                var result = store.LoadWithBackupFallback(primary);
                Equal("backup-project", result.Project.ProjectId, "malformed fallback project identity");
                Equal(Path.GetFullPath(backup), result.SourcePath, "malformed fallback source path");
                Equal(true, result.RecoveredFromBackup, "malformed fallback flag");
                Equal(StableReason, result.PrimaryFailureMessage, "malformed fallback stable reason");
                if (result.PrimaryFailureMessage.IndexOf("attacker-sentinel", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("Malformed primary parser detail leaked into the public recovery reason.");
            });
        }

        private static void StructurallyInvalidPrimaryUsesStableReason()
        {
            WithTempDirectory(directory =>
            {
                var primary = Path.Combine(directory, "invalid-structure.qsdb");
                var backup = primary + ".bak";
                var store = new QsdbProjectStore();
                store.SaveNew(new ProjectState("backup-structure", "Backup structure"), backup);
                File.WriteAllText(primary, "<qs3d schema=\"4\" name=\"hostile-project-name\" />");

                var result = store.LoadWithBackupFallback(primary);
                Equal("backup-structure", result.Project.ProjectId, "structural fallback project identity");
                Equal(true, result.RecoveredFromBackup, "structural fallback flag");
                Equal(StableReason, result.PrimaryFailureMessage, "structural fallback stable reason");
                if (result.PrimaryFailureMessage.IndexOf("projectId", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    result.PrimaryFailureMessage.IndexOf("hostile-project-name", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("Structural validation detail leaked into the public recovery reason.");
            });
        }

        private static void MissingBackupPreservesPrimaryFailure()
        {
            WithTempDirectory(directory =>
            {
                var primary = Path.Combine(directory, "missing-backup.qsdb");
                File.WriteAllText(primary, "<qs3d>");
                var store = new QsdbProjectStore();
                try
                {
                    _ = store.LoadWithBackupFallback(primary);
                }
                catch (Exception ex) when (ex is InvalidDataException || ex is System.Xml.XmlException || ex is FormatException || ex is FileNotFoundException)
                {
                    return;
                }
                throw new InvalidOperationException("Missing backup unexpectedly converted a primary failure into a recovery result.");
            });
        }

        private static void ValidPrimaryDoesNotReportRecovery()
        {
            WithTempDirectory(directory =>
            {
                var primary = Path.Combine(directory, "valid-primary.qsdb");
                var store = new QsdbProjectStore();
                store.SaveNew(new ProjectState("primary-project", "Primary project"), primary);

                var result = store.LoadWithBackupFallback(primary);
                Equal("primary-project", result.Project.ProjectId, "valid primary project identity");
                Equal(Path.GetFullPath(primary), result.SourcePath, "valid primary source path");
                Equal(false, result.RecoveredFromBackup, "valid primary recovery flag");
                Equal(string.Empty, result.PrimaryFailureMessage, "valid primary recovery reason");
            });
        }

        private static void WithTempDirectory(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-qsdb-recovery-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try { action(directory); }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
