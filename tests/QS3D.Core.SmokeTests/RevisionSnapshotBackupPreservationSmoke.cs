using System;
using System.IO;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotBackupPreservationSmoke
    {
        internal static void Run()
        {
            CorruptPrimarySavePreservesValidatedBackup();
            ValidPrimarySaveStillRotatesBackup();
        }

        private static void CorruptPrimarySavePreservesValidatedBackup()
        {
            var root = TempDirectory();
            try
            {
                var path = Path.Combine(root, "baseline.qsrev");
                var backup = path + ".bak";
                var store = new RevisionSnapshotStore();

                store.Save(Snapshot("A", 1), path);
                store.Save(Snapshot("B", 2), path);
                Equal("B", store.Load(path).Id);
                Equal("A", store.Load(backup).Id);

                File.WriteAllText(path, "<corrupt");
                Equal("A", store.LoadWithBackupFallback(path).Id);

                store.Save(Snapshot("C", 3), path);
                Equal("C", store.Load(path).Id);
                Equal("A", store.Load(backup).Id);

                File.WriteAllText(path, "<corrupt-again");
                Equal("A", store.LoadWithBackupFallback(path).Id);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static void ValidPrimarySaveStillRotatesBackup()
        {
            var root = TempDirectory();
            try
            {
                var path = Path.Combine(root, "normal.qsrev");
                var backup = path + ".bak";
                var store = new RevisionSnapshotStore();

                store.Save(Snapshot("A", 1), path);
                store.Save(Snapshot("B", 2), path);
                store.Save(Snapshot("C", 3), path);

                Equal("C", store.Load(path).Id);
                Equal("B", store.Load(backup).Id);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static RevisionSnapshot Snapshot(string id, int hour)
        {
            return new RevisionSnapshot
            {
                Id = id,
                CreatedUtc = new DateTime(2026, 8, 12, hour, 0, 0, DateTimeKind.Utc)
            };
        }

        private static string TempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-revision-backup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
