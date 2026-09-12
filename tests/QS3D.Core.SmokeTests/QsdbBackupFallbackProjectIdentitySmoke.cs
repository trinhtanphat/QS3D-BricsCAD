using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbBackupFallbackProjectIdentitySmoke
    {
        public static void Run()
        {
            CrossProjectBackupIsRejectedWhenPrimaryIdentityIsRecoverable();
            SameProjectBackupStillRecovers();
            MalformedPrimaryWithoutIdentityStillRecovers();
        }

        private static void CrossProjectBackupIsRejectedWhenPrimaryIdentityIsRecoverable()
        {
            WithTempDirectory(directory =>
            {
                var primary = Path.Combine(directory, "cross-project.qsdb");
                var backup = primary + ".bak";
                var store = new QsdbProjectStore();

                store.SaveNew(new ProjectState("primary-a", "Primary A"), primary);
                CorruptActiveZoneReference(primary);
                store.SaveNew(new ProjectState("backup-b", "Backup B"), backup);

                try
                {
                    var recovered = store.LoadWithBackupFallback(primary);
                    throw new InvalidOperationException(
                        "Cross-project backup fallback returned project " + recovered.Project.ProjectId +
                        " instead of rejecting the mismatched backup identity.");
                }
                catch (InvalidDataException ex)
                {
                    if (ex.Message.IndexOf("identity", StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("Cross-project backup rejection did not report a stable identity failure.", ex);
                }
            });
        }

        private static void SameProjectBackupStillRecovers()
        {
            WithTempDirectory(directory =>
            {
                var primary = Path.Combine(directory, "same-project.qsdb");
                var backup = primary + ".bak";
                var store = new QsdbProjectStore();

                store.SaveNew(new ProjectState("same-project", "Primary"), primary);
                CorruptActiveZoneReference(primary);
                store.SaveNew(new ProjectState("same-project", "Backup"), backup);

                var recovered = store.LoadWithBackupFallback(primary);
                Equal("same-project", recovered.Project.ProjectId, "same-project fallback identity");
                Equal(true, recovered.RecoveredFromBackup, "same-project fallback flag");
                Equal(Path.GetFullPath(backup), recovered.SourcePath, "same-project fallback source");
            });
        }

        private static void MalformedPrimaryWithoutIdentityStillRecovers()
        {
            WithTempDirectory(directory =>
            {
                var primary = Path.Combine(directory, "malformed-primary.qsdb");
                var backup = primary + ".bak";
                var store = new QsdbProjectStore();

                File.WriteAllText(primary, "<qs3d><broken>");
                store.SaveNew(new ProjectState("backup-only", "Backup only"), backup);

                var recovered = store.LoadWithBackupFallback(primary);
                Equal("backup-only", recovered.Project.ProjectId, "malformed-primary fallback identity");
                Equal(true, recovered.RecoveredFromBackup, "malformed-primary fallback flag");
            });
        }

        private static void CorruptActiveZoneReference(string path)
        {
            var text = File.ReadAllText(path);
            const string original = "activeZoneId=\"\"";
            if (text.IndexOf(original, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("QSDB identity regression fixture could not locate activeZoneId.");
            File.WriteAllText(path, text.Replace(original, "activeZoneId=\"missing-zone\""));
        }

        private static void WithTempDirectory(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-qsdb-backup-identity-" + Guid.NewGuid().ToString("N"));
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
