using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSessionTransitionAtomicitySmoke
    {
        private const int MaximumAuditEvents = 10_000;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            FailedSaveRollsBackAuditAndVersionAndRetainsLock();
            BackupRecoveredFailedSaveKeepsRecoveryStateAndValidatedBackup();
            ReloadAuditCapacityFailureKeepsOldSessionAuthoritative();
            SuccessfulRepeatedSaveReloadTransitionsRemainCoherent();
        }

        private static void FailedSaveRollsBackAuditAndVersionAndRetainsLock()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "save-rollback.qsdb");
                var project = new ProjectState("SESSION-SAVE-ROLLBACK", "Save rollback");
                using (var session = new ProjectSession(project, path))
                {
                    session.AcquireWriteLock();
                    var projectBefore = session.Project;
                    var auditBefore = session.Audit;
                    var versionBefore = project.ChangeVersion;
                    var updatedBefore = project.UpdatedUtc;
                    var auditCountBefore = project.AuditEvents.Count;

                    Directory.CreateDirectory(path);
                    Capture<IOException>(() => session.Save());

                    Same(projectBefore, session.Project, "Failed save replaced the authoritative Project instance.");
                    Same(auditBefore, session.Audit, "Failed save replaced the authoritative Audit instance.");
                    Equal(true, session.HasWriteLock, "Failed save released the session write lock.");
                    Equal(versionBefore, project.ChangeVersion, "Failed save did not restore ChangeVersion exactly.");
                    Equal(updatedBefore, project.UpdatedUtc, "Failed save did not restore UpdatedUtc exactly.");
                    Equal(auditCountBefore, project.AuditEvents.Count, "Failed save did not roll back PROJECT_SAVE audit state.");

                    Directory.Delete(path);
                    session.Save();
                    Equal(true, File.Exists(path), "Successful retry after failed save did not publish the project.");
                    Equal(auditCountBefore + 1, project.AuditEvents.Count, "Successful retry did not retain exactly one PROJECT_SAVE audit event.");
                    Equal("PROJECT_SAVE", project.AuditEvents[project.AuditEvents.Count - 1].Action,
                        "Successful retry did not retain the expected PROJECT_SAVE audit event.");
                }
            });
        }

        private static void BackupRecoveredFailedSaveKeepsRecoveryStateAndValidatedBackup()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "backup-recovery.qsdb");
                var backupPath = path + ".bak";
                var project = new ProjectState("SESSION-BACKUP-RECOVERY", "Backup v1");
                var store = new QsdbProjectStore();

                using (var session = new ProjectSession(project, path, store))
                {
                    session.AcquireWriteLock();
                    session.Save();
                    project.Name = "Backup v2";
                    session.Save();
                    Equal(true, File.Exists(backupPath), "Second save did not create the validated backup fixture.");

                    File.WriteAllText(path, "not qsdb");
                    session.Reload();
                    Equal("Backup v1", session.Project.Name, "Reload did not recover the expected validated backup state.");
                    Equal(true, session.HasWriteLock, "Backup recovery reload released the session write lock.");

                    var recoveredProject = session.Project;
                    var recoveredAudit = session.Audit;
                    var versionBeforeFailure = recoveredProject.ChangeVersion;
                    var updatedBeforeFailure = recoveredProject.UpdatedUtc;
                    var auditCountBeforeFailure = recoveredProject.AuditEvents.Count;
                    var validatedBackupBefore = File.ReadAllBytes(backupPath);

                    File.Delete(path);
                    Directory.CreateDirectory(path);
                    Capture<IOException>(() => session.Save());

                    Same(recoveredProject, session.Project, "Failed recovery-safe save replaced the recovered Project instance.");
                    Same(recoveredAudit, session.Audit, "Failed recovery-safe save replaced the recovered Audit instance.");
                    Equal(true, session.HasWriteLock, "Failed recovery-safe save released the write lock.");
                    Equal(versionBeforeFailure, recoveredProject.ChangeVersion,
                        "Failed recovery-safe save did not restore ChangeVersion exactly.");
                    Equal(updatedBeforeFailure, recoveredProject.UpdatedUtc,
                        "Failed recovery-safe save did not restore UpdatedUtc exactly.");
                    Equal(auditCountBeforeFailure, recoveredProject.AuditEvents.Count,
                        "Failed recovery-safe save did not roll back PROJECT_SAVE audit state.");
                    BytesEqual(validatedBackupBefore, File.ReadAllBytes(backupPath),
                        "Failed recovery-safe save mutated the validated backup.");

                    Directory.Delete(path);
                    File.WriteAllText(path, "sentinel primary that must not replace the validated backup");
                    session.Save();

                    BytesEqual(validatedBackupBefore, File.ReadAllBytes(backupPath),
                        "Recovery flag was lost after failed save; the next save replaced the validated backup.");
                    Equal("Backup v1", store.Load(backupPath).Name,
                        "Validated backup was not readable after recovery-safe retry.");
                    Equal("Backup v1", store.Load(path).Name,
                        "Recovery-safe retry did not publish the recovered project as primary.");
                }
            });
        }

        private static void ReloadAuditCapacityFailureKeepsOldSessionAuthoritative()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "reload-audit-capacity.qsdb");
                var oldProject = new ProjectState("SESSION-OLD", "Old authoritative session");
                var store = new QsdbProjectStore();

                using (var session = new ProjectSession(oldProject, path, store))
                {
                    session.AcquireWriteLock();
                    var replacement = new ProjectState("SESSION-REPLACEMENT", "Replacement at audit capacity");
                    for (var index = 0; index < MaximumAuditEvents; index++)
                    {
                        replacement.AuditEvents.Add(new AuditEvent
                        {
                            Utc = DateTime.UtcNow,
                            Action = "FIXTURE",
                            Detail = index.ToString()
                        });
                    }
                    store.SaveNew(replacement, path);

                    var projectBefore = session.Project;
                    var auditBefore = session.Audit;
                    var versionBefore = projectBefore.ChangeVersion;
                    var updatedBefore = projectBefore.UpdatedUtc;
                    var auditCountBefore = projectBefore.AuditEvents.Count;

                    var error = Capture<InvalidOperationException>(() => session.Reload());
                    Contains("10000", error.Message,
                        "Reload audit-capacity failure did not identify the supported history bound.");
                    Same(projectBefore, session.Project, "Failed reload replaced the authoritative Project instance.");
                    Same(auditBefore, session.Audit, "Failed reload replaced the authoritative Audit instance.");
                    Equal(true, session.HasWriteLock, "Failed reload released the session write lock.");
                    Equal(versionBefore, projectBefore.ChangeVersion, "Failed reload mutated the old Project ChangeVersion.");
                    Equal(updatedBefore, projectBefore.UpdatedUtc, "Failed reload mutated the old Project UpdatedUtc.");
                    Equal(auditCountBefore, projectBefore.AuditEvents.Count, "Failed reload mutated the old Project audit history.");
                }
            });
        }

        private static void SuccessfulRepeatedSaveReloadTransitionsRemainCoherent()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "successful-transitions.qsdb");
                var project = new ProjectState("SESSION-SUCCESS", "Initial");
                using (var session = new ProjectSession(project, path))
                {
                    session.AcquireWriteLock();
                    session.Save();
                    var firstProject = session.Project;
                    var firstAudit = session.Audit;

                    session.Reload();
                    Equal(false, ReferenceEquals(firstProject, session.Project),
                        "Successful reload did not replace the Project with persisted state.");
                    Equal(false, ReferenceEquals(firstAudit, session.Audit),
                        "Successful reload did not replace Audit with the persisted project binding.");
                    Equal(true, session.HasWriteLock, "Successful reload released the write lock.");
                    Equal(true, HasAudit(session.Project, "PROJECT_SAVE"),
                        "Successful reload lost the prior PROJECT_SAVE event.");
                    Equal(true, HasAudit(session.Project, "PROJECT_RELOAD"),
                        "Successful reload did not record PROJECT_RELOAD.");

                    session.Project.Name = "Repeated";
                    session.Save();
                    session.Reload();
                    Equal("Repeated", session.Project.Name, "Repeated save/reload did not preserve the persisted project state.");
                    Equal(true, session.HasWriteLock, "Repeated save/reload released the write lock.");
                    Equal(true, HasAudit(session.Project, "PROJECT_SAVE"),
                        "Repeated transition lost PROJECT_SAVE audit state.");
                    Equal(true, HasAudit(session.Project, "PROJECT_RELOAD"),
                        "Repeated transition lost PROJECT_RELOAD audit state.");
                }
            });
        }

        private static bool HasAudit(ProjectState project, string action)
        {
            foreach (var item in project.AuditEvents)
                if (string.Equals(item.Action, action, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void WithTemporaryDirectory(Action<string> action)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-session-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                action(root);
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Same(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual)) throw new InvalidOperationException(message);
        }

        private static void BytesEqual(byte[] expected, byte[] actual, string message)
        {
            if (expected.Length != actual.Length) throw new InvalidOperationException(message);
            for (var index = 0; index < expected.Length; index++)
                if (expected[index] != actual[index]) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
