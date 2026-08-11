using System;
using System.IO;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSidecarRevisionStampSmoke
    {
        public static void Run()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-sidecar-stamp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var primary = Path.Combine(directory, "project.qsdb");
                var backup = primary + ".bak";

                var absent = ProjectSidecarRevisionStamp.Capture(primary);
                False(absent.HasAnyFile, "Absent sidecar pair was reported as present.");
                True(absent.MatchesCurrent(), "Stable absent sidecar pair did not match itself.");

                File.WriteAllText(primary, "primary-a");
                False(absent.MatchesCurrent(), "New primary sidecar did not invalidate an absent baseline.");
                var primaryA = ProjectSidecarRevisionStamp.Capture(primary);
                True(primaryA.HasAnyFile, "Primary sidecar was not captured.");

                File.WriteAllText(primary, "primary-a");
                True(primaryA.MatchesCurrent(), "Byte-identical primary rewrite changed the content revision.");
                File.WriteAllText(primary, "primary-b");
                False(primaryA.MatchesCurrent(), "Changed primary content retained the old revision.");

                var primaryB = ProjectSidecarRevisionStamp.Capture(primary);
                File.WriteAllText(backup, "backup-a");
                False(primaryB.MatchesCurrent(), "New backup sidecar did not invalidate the pair revision.");
                var withBackup = ProjectSidecarRevisionStamp.Capture(primary);
                File.WriteAllText(backup, "backup-b");
                False(withBackup.MatchesCurrent(), "Changed backup content retained the old pair revision.");

                var changedBackup = ProjectSidecarRevisionStamp.Capture(primary);
                File.Delete(backup);
                False(changedBackup.MatchesCurrent(), "Removed backup did not invalidate the pair revision.");
                var primaryOnly = ProjectSidecarRevisionStamp.Capture(primary);
                File.Delete(primary);
                False(primaryOnly.MatchesCurrent(), "Removed primary did not invalidate the pair revision.");

                var other = Path.Combine(directory, "other.qsdb");
                False(absent.IsForPath(other), "Revision stamp accepted another primary path.");

                using (var oversized = new FileStream(primary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    oversized.SetLength(64L * 1024L * 1024L + 1L);
                Throws<InvalidDataException>(() => ProjectSidecarRevisionStamp.Capture(primary));
                File.Delete(primary);
                Directory.CreateDirectory(primary);
                Throws<InvalidDataException>(() => ProjectSidecarRevisionStamp.Capture(primary));
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch { }
            }
        }

        private static void True(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void False(bool value, string message) => True(!value, message);

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
