using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSessionAuditPathRedactionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var marker = "private-session-path-" + Guid.NewGuid().ToString("N");
            var directory = Path.Combine(Path.GetTempPath(), marker);
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);

            try
            {
                using (var session = new ProjectSession(new ProjectState("SESSION-PATH", "Session path redaction"), path))
                {
                    session.AcquireWriteLock();
                    session.Save();
                    RequireRedacted(session.Project, path, expectedSaveCount: 1, expectedReloadCount: 0);

                    session.Reload();
                    RequireRedacted(session.Project, path, expectedSaveCount: 1, expectedReloadCount: 1);

                    session.Save();
                    RequireRedacted(session.Project, path, expectedSaveCount: 2, expectedReloadCount: 1);
                }

                var persisted = new QsdbProjectStore().Load(path);
                RequireRedacted(persisted, path, expectedSaveCount: 2, expectedReloadCount: 1);
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private static void RequireRedacted(ProjectState project, string path, int expectedSaveCount, int expectedReloadCount)
        {
            var saveEvents = project.AuditEvents.Where(x => string.Equals(x.Action, "PROJECT_SAVE", StringComparison.Ordinal)).ToList();
            var reloadEvents = project.AuditEvents.Where(x => string.Equals(x.Action, "PROJECT_RELOAD", StringComparison.Ordinal)).ToList();
            if (saveEvents.Count != expectedSaveCount)
                throw new InvalidOperationException("ProjectSession must retain the expected PROJECT_SAVE audit count.");
            if (reloadEvents.Count != expectedReloadCount)
                throw new InvalidOperationException("ProjectSession must retain the expected PROJECT_RELOAD audit count.");

            foreach (var item in saveEvents.Concat(reloadEvents))
            {
                if (!string.IsNullOrEmpty(item.Detail))
                    throw new InvalidOperationException("ProjectSession save/reload audit detail must not persist filesystem paths.");
                if (item.Detail.IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new InvalidOperationException("ProjectSession audit leaked its absolute QSDB path.");
            }
        }
    }
}
