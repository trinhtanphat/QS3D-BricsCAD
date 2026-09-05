using System;
using System.IO;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFileLockLifecycleSmoke
    {
        public static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-project-lock-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var projectPath = Path.Combine(root, "project.qsdb");
            var lockPath = Path.GetFullPath(projectPath) + ".lock";

            try
            {
                var first = ProjectFileLock.Acquire(projectPath);
                try
                {
                    ExpectSecondAcquireRejected(projectPath);
                }
                finally
                {
                    first.Dispose();
                }

                // Successful release must be idempotent and must actually release the
                // canonical rendezvous so the same project can be acquired again.
                first.Dispose();
                using (ProjectFileLock.Acquire(Path.GetFullPath(projectPath)))
                {
                }
            }
            finally
            {
                try { File.Delete(lockPath); } catch { }
                try { Directory.Delete(root, true); } catch { }
            }
        }

        private static void ExpectSecondAcquireRejected(string projectPath)
        {
            try
            {
                using (ProjectFileLock.Acquire(projectPath))
                {
                }
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("ProjectFileLock admitted a second writer for the same canonical project path.");
        }
    }
}
