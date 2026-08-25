using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class PersistenceRedirectedPathSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var atomicType = typeof(QsdbProjectStore).Assembly
                .GetType("QS3D.Core.Persistence.AtomicFileCommit", throwOnError: true)!;
            var replaceWithoutBackup = GetMethod(atomicType, "ReplaceWithoutBackup");
            var replaceWithBackup = GetMethod(atomicType, "ReplaceWithBackup");

            RegularPathsRemainUsable(replaceWithoutBackup);
            RejectsRedirectedTemporaryFileBeforeMutation(replaceWithoutBackup);
            RejectsRedirectedBackupFileBeforeMutation(replaceWithBackup);
            RejectsRedirectedDirectoryComponentBeforeMutation(replaceWithoutBackup);
            RejectsRedirectedProjectLockBeforeTruncation();
        }

        private static void RegularPathsRemainUsable(MethodInfo replaceWithoutBackup)
        {
            WithDirectory(dir =>
            {
                var temp = Path.Combine(dir, "regular.tmp");
                var destination = Path.Combine(dir, "regular.qsdb");
                File.WriteAllText(temp, "new");
                File.WriteAllText(destination, "old");

                Invoke(replaceWithoutBackup, temp, destination);
                Equal("new", File.ReadAllText(destination), "regular atomic replace changed behavior");

                var project = Path.Combine(dir, "locked.qsdb");
                using (ProjectFileLock.Acquire(project))
                {
                    var lockPath = project + ".lock";
                    Require(File.Exists(lockPath), "regular project lock was not created");
                    Require(new FileInfo(lockPath).Length > 0L, "regular project lock payload was not written");
                }
            });
        }

        private static void RejectsRedirectedTemporaryFileBeforeMutation(MethodInfo replaceWithoutBackup)
        {
            WithDirectory(dir =>
            {
                var target = Path.Combine(dir, "temp-target.bin");
                var tempLink = Path.Combine(dir, "redirected.tmp");
                var destination = Path.Combine(dir, "project.qsdb");
                File.WriteAllText(target, "new-target");
                File.WriteAllText(destination, "old-project");
                if (!TryCreateFileLink(tempLink, target)) return;

                ExpectRedirectRefusal(
                    () => Invoke(replaceWithoutBackup, tempLink, destination),
                    "AtomicFileCommit accepted a redirected temporary file");

                Equal("new-target", File.ReadAllText(target), "redirected temporary target was mutated");
                Equal("old-project", File.ReadAllText(destination), "destination changed after redirected-temp refusal");
            });
        }

        private static void RejectsRedirectedBackupFileBeforeMutation(MethodInfo replaceWithBackup)
        {
            WithDirectory(dir =>
            {
                var temp = Path.Combine(dir, "replace.tmp");
                var destination = Path.Combine(dir, "project.qsdb");
                var backupTarget = Path.Combine(dir, "backup-target.bin");
                var backupLink = Path.Combine(dir, "project.qsdb.bak");
                File.WriteAllText(temp, "new-project");
                File.WriteAllText(destination, "old-project");
                File.WriteAllText(backupTarget, "protected-backup-target");
                if (!TryCreateFileLink(backupLink, backupTarget)) return;

                ExpectRedirectRefusal(
                    () => Invoke(replaceWithBackup, temp, destination, backupLink),
                    "AtomicFileCommit accepted a redirected backup file");

                Equal("new-project", File.ReadAllText(temp), "temporary file changed after redirected-backup refusal");
                Equal("old-project", File.ReadAllText(destination), "destination changed after redirected-backup refusal");
                Equal("protected-backup-target", File.ReadAllText(backupTarget), "redirected backup target was mutated");
            });
        }

        private static void RejectsRedirectedDirectoryComponentBeforeMutation(MethodInfo replaceWithoutBackup)
        {
            WithDirectory(dir =>
            {
                var realDirectory = Path.Combine(dir, "real");
                var redirectedDirectory = Path.Combine(dir, "redirected");
                Directory.CreateDirectory(realDirectory);
                var realTemp = Path.Combine(realDirectory, "project.tmp");
                var realDestination = Path.Combine(realDirectory, "project.qsdb");
                File.WriteAllText(realTemp, "new-project");
                File.WriteAllText(realDestination, "old-project");
                if (!TryCreateDirectoryLink(redirectedDirectory, realDirectory)) return;

                var redirectedTemp = Path.Combine(redirectedDirectory, "project.tmp");
                var redirectedDestination = Path.Combine(redirectedDirectory, "project.qsdb");
                ExpectRedirectRefusal(
                    () => Invoke(replaceWithoutBackup, redirectedTemp, redirectedDestination),
                    "AtomicFileCommit accepted a redirected parent directory");

                Equal("new-project", File.ReadAllText(realTemp), "temporary file changed through redirected directory");
                Equal("old-project", File.ReadAllText(realDestination), "destination changed through redirected directory");
            });
        }

        private static void RejectsRedirectedProjectLockBeforeTruncation()
        {
            WithDirectory(dir =>
            {
                var project = Path.Combine(dir, "project.qsdb");
                var lockPath = project + ".lock";
                var target = Path.Combine(dir, "lock-target.bin");
                File.WriteAllText(target, "do-not-truncate");
                if (!TryCreateFileLink(lockPath, target)) return;

                ExpectRedirectRefusal(
                    () =>
                    {
                        using (ProjectFileLock.Acquire(project)) { }
                    },
                    "ProjectFileLock accepted a redirected lock path");

                Equal("do-not-truncate", File.ReadAllText(target), "redirected lock target was truncated or overwritten");
            });
        }

        private static bool TryCreateFileLink(string linkPath, string targetPath)
        {
            try
            {
                File.CreateSymbolicLink(linkPath, targetPath);
                return true;
            }
            catch (PlatformNotSupportedException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (IOException) { return false; }
        }

        private static bool TryCreateDirectoryLink(string linkPath, string targetPath)
        {
            try
            {
                Directory.CreateSymbolicLink(linkPath, targetPath);
                return true;
            }
            catch (PlatformNotSupportedException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (IOException) { return false; }
        }

        private static MethodInfo GetMethod(Type type, string name)
        {
            return type.GetMethod(name, BindingFlags.Static | BindingFlags.Public)
                ?? throw new InvalidOperationException("PersistenceRedirectedPathSmoke: " + name + " was not found.");
        }

        private static void Invoke(MethodInfo method, params string[] paths)
        {
            method.Invoke(null, paths);
        }

        private static void ExpectRedirectRefusal(Action action, string message)
        {
            try
            {
                action();
            }
            catch (TargetInvocationException ex) when (IsRedirectRefusal(ex.InnerException))
            {
                return;
            }
            catch (Exception ex) when (IsRedirectRefusal(ex))
            {
                return;
            }

            throw new InvalidOperationException("PersistenceRedirectedPathSmoke: " + message + ".");
        }

        private static bool IsRedirectRefusal(Exception? exception)
        {
            if (exception is InvalidDataException) return true;
            return exception is InvalidOperationException && exception.InnerException is InvalidDataException;
        }

        private static void WithDirectory(Action<string> action)
        {
            var dir = Path.Combine(Path.GetTempPath(), "qs3d-persistence-redirect-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try { action(dir); }
            finally
            {
                try { Directory.Delete(dir, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("PersistenceRedirectedPathSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("PersistenceRedirectedPathSmoke: " + message + ".");
        }
    }
}
