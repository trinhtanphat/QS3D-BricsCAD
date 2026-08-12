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
            BothInvalidReloadKeepsExistingSessionBinding();
            BackupRecoverySavePreservesValidatedBackupAndClearsMode();
            PrimaryReloadClearsRecoveryPublicationMode();
            FailedRecoverySaveRollsBackAndKeepsValidatedBackup();
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

                    Require(ReferenceEquals(originalProject, session.Project), "Failed ProjectSession.Reload replaced the current project binding.");
                    Require(ReferenceEquals(originalAudit, session.Audit), "Failed ProjectSession.Reload replaced the current audit binding.");
                    Require(string.Equals(session.Project.ProjectId, "original", StringComparison.Ordinal), "Failed ProjectSession.Reload exposed the staged project.");
                    Require(session.Audit.Events.Count == 0, "Failed ProjectSession.Reload changed the original audit trail.");
                }
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private static void BothInvalidReloadKeepsExistingSessionBinding()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-session-recovery-invalid-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "project.qsdb");
            try
            {
                File.WriteAllText(path, "<broken-primary");
                File.WriteAllText(path + ".bak", "<broken-backup");
                var original = new ProjectState("original-invalid", "Original invalid fallback");

                using (var session = new ProjectSession(original, path))
                {
                    session.AcquireWriteLock();
                    var originalProject = session.Project;
                    var originalAudit = session.Audit;

                    Throws<InvalidDataException>(() => session.Reload());

                    Require(ReferenceEquals(originalProject, session.Project), "Both-invalid recovery replaced the current project binding.");
                    Require(ReferenceEquals(originalAudit, session.Audit), "Both-invalid recovery replaced the current audit binding.");
                    Require(session.Audit.Events.Count == 0, "Both-invalid recovery changed the original audit trail.");
                }
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private static void BackupRecoverySavePreservesValidatedBackupAndClearsMode()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-session-recovery-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "project.qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(new ProjectState("recovery", "Known Good"), path);
                store.Save(new ProjectState("recovery", "Newer"), path);
                File.WriteAllText(path, "<broken-primary");

                using (var session = new ProjectSession(new ProjectState("seed", "Seed"), path))
                {
                    session.AcquireWriteLock();
                    session.Reload();
                    Require(session.Project.Name == "Known Good", "ProjectSession recovery did not bind the validated backup generation.");
                    RequireAction(session.Project, "PROJECT_RELOAD");

                    session.Save();
                    var healed = store.Load(path);
                    var validatedBackup = store.Load(path + ".bak");
                    Require(healed.Name == "Known Good", "Recovered ProjectSession save did not heal the primary.");
                    Require(validatedBackup.Name == "Known Good", "Recovered ProjectSession save replaced the validated backup.");
                    RequireAction(healed, "PROJECT_RELOAD");
                    RequireAction(healed, "PROJECT_SAVE");
                    Require(!validatedBackup.AuditEvents.Any(x => string.Equals(x.Action, "PROJECT_RELOAD", StringComparison.Ordinal)),
                        "First recovered save unexpectedly rotated the recovered primary into the validated backup.");

                    session.Audit.Record("AFTER_RECOVERY_SAVE", string.Empty, "second-publication");
                    session.Save();
                    var rotatedBackup = store.Load(path + ".bak");
                    RequireAction(rotatedBackup, "PROJECT_RELOAD");
                    RequireAction(rotatedBackup, "PROJECT_SAVE");
                    Require(!rotatedBackup.AuditEvents.Any(x => string.Equals(x.Action, "AFTER_RECOVERY_SAVE", StringComparison.Ordinal)),
                        "Second save backup was not the previously published primary generation.");
                }
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private static void PrimaryReloadClearsRecoveryPublicationMode()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-session-recovery-primary-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "project.qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(new ProjectState("recovery-primary", "Known Good"), path);
                store.Save(new ProjectState("recovery-primary", "Newer"), path);
                File.WriteAllText(path, "<broken-primary");

                using (var session = new ProjectSession(new ProjectState("seed", "Seed"), path))
                {
                    session.AcquireWriteLock();
                    session.Reload();
                    Require(session.Project.Name == "Known Good", "Precondition failed: session did not recover from backup.");

                    store.SavePreservingValidatedBackup(new ProjectState("recovery-primary", "External Primary"), path);
                    Require(store.Load(path + ".bak").Name == "Known Good", "External primary repair changed the validated backup precondition.");

                    session.Reload();
                    Require(session.Project.Name == "External Primary", "ProjectSession did not prefer the repaired primary on reload.");
                    session.Save();

                    Require(store.Load(path + ".bak").Name == "External Primary",
                        "Successful primary reload did not clear recovery mode before the next normal save rotation.");
                }
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private static void FailedRecoverySaveRollsBackAndKeepsValidatedBackup()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-session-recovery-save-failure-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "project.qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(new ProjectState("recovery-failure", "Known Good"), path);
                store.Save(new ProjectState("recovery-failure", "Newer"), path);
                File.WriteAllText(path, "<broken-primary");

                using (var session = new ProjectSession(new ProjectState("seed", "Seed"), path))
                {
                    session.AcquireWriteLock();
                    session.Reload();
                    var auditCountBeforeFailedSave = session.Audit.Events.Count;
                    var changeVersionBeforeFailedSave = session.Project.ChangeVersion;

                    File.Delete(path);
                    Directory.CreateDirectory(path);
                    ThrowsIoFailure(() => session.Save());

                    Require(session.Audit.Events.Count == auditCountBeforeFailedSave, "Failed recovered save leaked PROJECT_SAVE into the in-memory audit trail.");
                    Require(session.Project.ChangeVersion == changeVersionBeforeFailedSave, "Failed recovered save changed the in-memory project version.");
                    Require(store.Load(path + ".bak").Name == "Known Good", "Failed recovered save damaged the validated backup.");

                    Directory.Delete(path);
                    File.WriteAllText(path, "<broken-primary-again");
                    session.Save();

                    Require(store.Load(path).Name == "Known Good", "Retry after failed recovered save did not heal the primary.");
                    Require(store.Load(path + ".bak").Name == "Known Good",
                        "Retry after failed recovered save lost recovery provenance and rotated a corrupt primary into the backup.");
                    RequireAction(session.Project, "PROJECT_SAVE");
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

        private static void ThrowsIoFailure(Action action)
        {
            try
            {
                action();
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            throw new Exception("Expected an I/O failure.");
        }
    }
}
