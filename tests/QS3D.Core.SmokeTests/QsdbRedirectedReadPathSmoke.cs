using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbRedirectedReadPathSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RegularProjectFileRemainsReadable();
            RedirectedPrimaryIsRejectedWithoutBackupDowngrade();
            RedirectedBackupIsRejectedBeforeFallbackRead();
            RedirectedParentDirectoryIsRejected();
        }

        private static void RegularProjectFileRemainsReadable()
        {
            WithDirectory(dir =>
            {
                var path = Path.Combine(dir, "regular.qsdb");
                var store = new QsdbProjectStore();
                store.Save(NewProject("regular-project", "Regular project"), path);

                var loaded = store.Load(path);
                Require(loaded.ProjectId == "regular-project", "regular QSDB load changed behavior");
                Require(loaded.Name == "Regular project", "regular QSDB load returned the wrong project");
            });
        }

        private static void RedirectedPrimaryIsRejectedWithoutBackupDowngrade()
        {
            WithDirectory(dir =>
            {
                var target = Path.Combine(dir, "primary-target.qsdb");
                var redirected = Path.Combine(dir, "project.qsdb");
                var backup = redirected + ".bak";
                var store = new QsdbProjectStore();
                store.Save(NewProject("redirect-target", "Redirect target"), target);
                File.Copy(target, backup, overwrite: false);
                if (!TryCreateFileLink(redirected, target)) return;

                ExpectRedirectRefusal(
                    () => store.Load(redirected),
                    "Load accepted a redirected primary QSDB path");
                ExpectRedirectRefusal(
                    () => store.LoadWithBackupFallback(redirected),
                    "LoadWithBackupFallback downgraded a redirected primary path to a regular backup");

                Require(store.Load(backup).ProjectId == "redirect-target", "regular backup control was not readable");
            });
        }

        private static void RedirectedBackupIsRejectedBeforeFallbackRead()
        {
            WithDirectory(dir =>
            {
                var primary = Path.Combine(dir, "project.qsdb");
                var backup = primary + ".bak";
                var backupTarget = Path.Combine(dir, "backup-target.qsdb");
                var store = new QsdbProjectStore();
                store.Save(NewProject("backup-target", "Backup target"), backupTarget);
                File.WriteAllText(primary, "<broken");
                if (!TryCreateFileLink(backup, backupTarget)) return;

                ExpectRedirectRefusal(
                    () => store.LoadWithBackupFallback(primary),
                    "LoadWithBackupFallback accepted a redirected backup QSDB path");

                Require(store.Load(backupTarget).ProjectId == "backup-target", "redirect target control was not readable directly");
                Require(File.ReadAllText(primary) == "<broken", "fallback refusal mutated the corrupt primary");
            });
        }

        private static void RedirectedParentDirectoryIsRejected()
        {
            WithDirectory(dir =>
            {
                var realDirectory = Path.Combine(dir, "real");
                var redirectedDirectory = Path.Combine(dir, "redirected");
                Directory.CreateDirectory(realDirectory);
                var realProject = Path.Combine(realDirectory, "project.qsdb");
                var store = new QsdbProjectStore();
                store.Save(NewProject("directory-target", "Directory target"), realProject);
                if (!TryCreateDirectoryLink(redirectedDirectory, realDirectory)) return;

                var redirectedProject = Path.Combine(redirectedDirectory, "project.qsdb");
                ExpectRedirectRefusal(
                    () => store.Load(redirectedProject),
                    "Load accepted a QSDB path through a redirected parent directory");
                Require(store.Load(realProject).ProjectId == "directory-target", "real directory control was not readable");
            });
        }

        private static ProjectState NewProject(string id, string name)
        {
            return new ProjectState(id, name);
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

        private static void ExpectRedirectRefusal(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidDataException ex) when (
                ex.Message.IndexOf("redirected", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ex.Message.IndexOf("reparse-point", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            throw new InvalidOperationException("QsdbRedirectedReadPathSmoke: " + message + ".");
        }

        private static void WithDirectory(Action<string> action)
        {
            var dir = Path.Combine(Path.GetTempPath(), "qs3d-qsdb-read-redirect-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try { action(dir); }
            finally
            {
                try { Directory.Delete(dir, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("QsdbRedirectedReadPathSmoke: " + message + ".");
        }
    }
}
