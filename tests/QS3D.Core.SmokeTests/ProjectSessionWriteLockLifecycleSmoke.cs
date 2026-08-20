using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSessionWriteLockLifecycleSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SaveAndReloadRequireWriteLockBeforeSideEffects();
            RepeatedAcquireIsIdempotentAndKeepsExclusiveOwnership();
            DisposeIsIdempotentAndReleasesOwnership();
            ReacquiredLockSupportsSuccessfulSaveReloadLifecycle();
        }

        private static void SaveAndReloadRequireWriteLockBeforeSideEffects()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "no-lock.qsdb");
                var project = new ProjectState("SESSION-NO-LOCK", "No lock");
                using (var session = new ProjectSession(project, path))
                {
                    var versionBefore = project.ChangeVersion;
                    var updatedBefore = project.UpdatedUtc;
                    var auditCountBefore = project.AuditEvents.Count;

                    Capture<InvalidOperationException>(() => session.Save());
                    Equal(false, File.Exists(path), "Save without a write lock published a project file.");
                    Equal(versionBefore, project.ChangeVersion, "Save without a write lock changed project version.");
                    Equal(updatedBefore, project.UpdatedUtc, "Save without a write lock changed project timestamp.");
                    Equal(auditCountBefore, project.AuditEvents.Count, "Save without a write lock changed audit history.");
                    Equal(false, session.HasWriteLock, "Rejected Save unexpectedly acquired a write lock.");

                    Capture<InvalidOperationException>(() => session.Reload());
                    Equal(versionBefore, project.ChangeVersion, "Reload without a write lock changed project version.");
                    Equal(updatedBefore, project.UpdatedUtc, "Reload without a write lock changed project timestamp.");
                    Equal(auditCountBefore, project.AuditEvents.Count, "Reload without a write lock changed audit history.");
                    Same(project, session.Project, "Reload without a write lock replaced the authoritative project.");
                    Equal(false, session.HasWriteLock, "Rejected Reload unexpectedly acquired a write lock.");
                }
            });
        }

        private static void RepeatedAcquireIsIdempotentAndKeepsExclusiveOwnership()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "repeated-acquire.qsdb");
                using (var owner = NewSession("SESSION-OWNER", path))
                using (var contender = NewSession("SESSION-CONTENDER", path))
                {
                    owner.AcquireWriteLock();
                    Equal(true, owner.HasWriteLock, "First AcquireWriteLock did not publish ownership.");
                    owner.AcquireWriteLock();
                    Equal(true, owner.HasWriteLock, "Repeated AcquireWriteLock lost ownership.");

                    Capture<InvalidOperationException>(() => contender.AcquireWriteLock());
                    Equal(false, contender.HasWriteLock, "Failed contender acquisition published ownership.");

                    owner.Save();
                    Equal(true, File.Exists(path), "Owner could not save after repeated lock acquisition.");
                    Equal(true, owner.HasWriteLock, "Successful save released the owner write lock.");
                }
            });
        }

        private static void DisposeIsIdempotentAndReleasesOwnership()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "dispose-release.qsdb");
                var owner = NewSession("SESSION-DISPOSE-OWNER", path);
                owner.AcquireWriteLock();
                Equal(true, owner.HasWriteLock, "Owner did not acquire the write lock before Dispose coverage.");

                owner.Dispose();
                Equal(false, owner.HasWriteLock, "Dispose did not clear HasWriteLock.");
                owner.Dispose();
                Equal(false, owner.HasWriteLock, "Repeated Dispose resurrected write-lock state.");

                using (var successor = NewSession("SESSION-DISPOSE-SUCCESSOR", path))
                {
                    successor.AcquireWriteLock();
                    Equal(true, successor.HasWriteLock, "Successor could not acquire the lock after owner Dispose.");
                    successor.Save();
                    Equal(true, File.Exists(path), "Successor could not save after acquiring released ownership.");
                }
            });
        }

        private static void ReacquiredLockSupportsSuccessfulSaveReloadLifecycle()
        {
            WithTemporaryDirectory(root =>
            {
                var path = Path.Combine(root, "reacquire-save-reload.qsdb");
                using (var first = NewSession("SESSION-REACQUIRE", path))
                {
                    first.AcquireWriteLock();
                    first.Project.Name = "Persisted by first owner";
                    first.Save();
                }

                using (var second = NewSession("SESSION-REACQUIRE", path))
                {
                    second.AcquireWriteLock();
                    second.Reload();
                    Equal("Persisted by first owner", second.Project.Name,
                        "Reacquired lock could not reload the prior owner's persisted project.");
                    Equal(true, second.HasWriteLock, "Successful reload released the reacquired write lock.");

                    second.Project.Name = "Persisted by successor";
                    second.Save();
                    second.Reload();
                    Equal("Persisted by successor", second.Project.Name,
                        "Reacquired lock did not support a repeated save/reload lifecycle.");
                    Equal(true, second.HasWriteLock, "Repeated save/reload released the successor write lock.");
                }
            });
        }

        private static ProjectSession NewSession(string projectId, string path) =>
            new ProjectSession(new ProjectState(projectId, projectId), path);

        private static void WithTemporaryDirectory(Action<string> action)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-session-lock-smoke-" + Guid.NewGuid().ToString("N"));
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
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Same(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual)) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }
    }
}
