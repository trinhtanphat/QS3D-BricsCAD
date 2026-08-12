using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFileLockErrorRedactionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ContentionDoesNotExposeProjectPath();
        }

        private static void ContentionDoesNotExposeProjectPath()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-project-file-lock-redaction-" + Guid.NewGuid().ToString("N"));
            var projectPath = Path.Combine(root, "private-project.qs3d");

            try
            {
                using (ProjectFileLock.Acquire(projectPath))
                {
                    try
                    {
                        var unexpected = ProjectFileLock.Acquire(projectPath);
                        unexpected.Dispose();
                        throw new Exception("Second project lock acquisition must fail while the first lock is held.");
                    }
                    catch (InvalidOperationException ex)
                    {
                        const string expected = "Unable to acquire exclusive QS3D project write lock.";
                        if (!string.Equals(ex.Message, expected, StringComparison.Ordinal))
                            throw new Exception("Project lock contention must expose only the stable redacted public message.");
                        if (ex.Message.IndexOf(root, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            ex.Message.IndexOf(projectPath, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            ex.Message.IndexOf(".lock", StringComparison.OrdinalIgnoreCase) >= 0)
                            throw new Exception("Project lock contention message must not disclose filesystem paths.");
                        if (!(ex.InnerException is IOException))
                            throw new Exception("Project lock contention must preserve the originating IOException as InnerException.");
                    }
                }

                using (ProjectFileLock.Acquire(projectPath))
                {
                }
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
