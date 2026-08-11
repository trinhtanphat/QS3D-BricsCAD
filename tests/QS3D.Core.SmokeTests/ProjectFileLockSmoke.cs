using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFileLockSmoke
    {
        public static void Run()
        {
            var projectPath = Path.Combine(Path.GetTempPath(), "qs3d-lock-rendezvous-" + Guid.NewGuid().ToString("N") + ".qsdb");
            var lockPath = Path.GetFullPath(projectPath) + ".lock";
            try
            {
                using (var first = ProjectFileLock.Acquire(projectPath))
                    Throws<InvalidOperationException>(() => { using var contender = ProjectFileLock.Acquire(projectPath); });

                Require(File.Exists(lockPath), "Project lock release removed the shared rendezvous path.");

                using (var next = ProjectFileLock.Acquire(projectPath))
                {
                    Require(File.Exists(lockPath), "Project lock reacquire lost the shared rendezvous path.");
                    Throws<InvalidOperationException>(() => { using var contender = ProjectFileLock.Acquire(projectPath); });
                }

                Require(File.Exists(lockPath), "Project lock release after reacquire removed the shared rendezvous path.");
            }
            finally
            {
                try { if (File.Exists(lockPath)) File.Delete(lockPath); } catch { }
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectFileLockSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectFileLockSmoke.Run();
    }
}
