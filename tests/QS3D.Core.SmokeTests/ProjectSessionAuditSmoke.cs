using System;
using System.IO;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSessionAuditSmoke
    {
        public static void Run()
        {
            SavePersistsAuditAndReloadRebindsTrail();
            FailedReloadKeepsExistingSessionBinding();
        }

        private static void SavePersistsAuditAndReloadRebindsTrail()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-session-audit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "project.qsdb");
            try
            {
                var project = new ProjectState("P1", "Session audit");
                AuditTrail.ForProject(project).Record("BEFORE", string.Empty, "seed");

                using (var session = new ProjectSession(project, path))
                {
                    session.AcquireWriteLock();
                    session.Save();
                    RequireAction(session.Project, "BEFORE");
                    RequireAction(session.Project, "PROJECT_SAVE");

                    var persisted = new QsdbProjectStore().Load(path);
                    RequireAction(persisted, "BEFORE");
                    RequireAction(persisted, "PROJECT_SAVE");

                    session.Reload();
                    RequireAction(session.Project, "BEFORE");
                    RequireAction(session.Project, "PROJECT_SAVE");
                    RequireAction(session.Project, "PROJECT_RELOAD");

                    session.Audit.Record("AFTER_RELOAD", string.Empty, "bound");
                    RequireAction(session.Project, "AFTER_RELOAD");
                    if (session.Audit.Events.Count != session.Project.AuditEvents.Count)
                        throw new Exception("ProjectSession.Audit must remain bound to the current project after Reload.");

                    session.Save();
                    var persistedAgain = new QsdbProjectStore().Load(path);
                    RequireAction(persistedAgain, "PROJECT_RELOAD");
                    RequireAction(persistedAgain, "AFTER_RELOAD");
                    if (persistedAgain.AuditEvents.Count(x => string.Equals(x.Action, "PROJECT_SAVE", StringComparison.Ordinal)) != 2)
                        throw new Exception("Each successful session save must be persisted exactly once in project audit history.");
                }
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private static void FailedReloadKeepsExistingSessionBinding()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-session-reload-atomicity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "project.qsdb");
            try
            {
                File.WriteAllText(path,
                    "<qs3d schema=\"3\" projectId=\"reloaded\" name=\"Reloaded\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\" changeVersion=\"9223372036854775807\" drawingPath=\"\" drawingFingerprint=\"\" activeZoneId=\"\" activeFloorId=\"\">" +
                    "<metadata/><zones/><floors/><families/><rules/><elements/><audit/></qs3d>");

                var original = new ProjectState("original", "Original");
                using (var session = new ProjectSession(original, path))
                {
                    session.AcquireWriteLock();
                    var originalProject = session.Project;
                    var originalAudit = session.Audit;

                    Throws<OverflowException>(() => session.Reload());

                    if (!ReferenceEquals(originalProject, session.Project))
                        throw new Exception("Failed ProjectSession.Reload replaced the current project binding.");
                    if (!ReferenceEquals(originalAudit, session.Audit))
                        throw new Exception("Failed ProjectSession.Reload replaced the current audit binding.");
                    if (!string.Equals(session.Project.ProjectId, "original", StringComparison.Ordinal))
                        throw new Exception("Failed ProjectSession.Reload exposed the staged project.");
                    if (session.Audit.Events.Count != 0)
                        throw new Exception("Failed ProjectSession.Reload changed the original audit trail.");
                }
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private static void RequireAction(ProjectState project, string action)
        {
            if (!project.AuditEvents.Any(x => string.Equals(x.Action, action, StringComparison.Ordinal)))
                throw new Exception("Expected project audit action: " + action);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
