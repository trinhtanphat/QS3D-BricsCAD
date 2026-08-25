using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSidecarRevisionRedirectSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsRedirectedPrimaryWhenSupported();
            RejectsRedirectedBackupWhenSupported();
            CapturesOrdinaryPrimaryBackupAndMissingPair();
            PreservesCanonicalPathIdentityAndFreshness();
        }

        private static void RejectsRedirectedPrimaryWhenSupported()
        {
            WithDirectory(dir =>
            {
                var target = Path.Combine(dir, "outside.bin");
                var primary = Path.Combine(dir, "project.qsdb");
                File.WriteAllText(target, "redirected-primary");
                if (!TryCreateSymbolicLink(primary, target)) return;

                ExpectInvalidData(
                    () => ProjectSidecarRevisionStamp.Capture(primary),
                    "redirected primary received revision authority");
            });
        }

        private static void RejectsRedirectedBackupWhenSupported()
        {
            WithDirectory(dir =>
            {
                var primary = Path.Combine(dir, "project.qsdb");
                var backupTarget = Path.Combine(dir, "outside-backup.bin");
                var backup = primary + ".bak";
                File.WriteAllText(primary, "primary");
                File.WriteAllText(backupTarget, "redirected-backup");
                if (!TryCreateSymbolicLink(backup, backupTarget)) return;

                ExpectInvalidData(
                    () => ProjectSidecarRevisionStamp.Capture(primary),
                    "redirected backup received revision authority");
            });
        }

        private static void CapturesOrdinaryPrimaryBackupAndMissingPair()
        {
            WithDirectory(dir =>
            {
                var primary = Path.Combine(dir, "ordinary.qsdb");
                var missing = Path.Combine(dir, "missing.qsdb");
                File.WriteAllText(primary, "primary-v1");
                File.WriteAllText(primary + ".bak", "backup-v1");

                var stamp = ProjectSidecarRevisionStamp.Capture(primary);
                Require(stamp.HasAnyFile, "ordinary primary/backup pair was reported missing");
                Require(stamp.MatchesCurrent(), "ordinary primary/backup pair did not match immediately after capture");

                var missingStamp = ProjectSidecarRevisionStamp.Capture(missing);
                Require(!missingStamp.HasAnyFile, "missing primary/backup pair unexpectedly reported a file");
                Require(missingStamp.MatchesCurrent(), "stable missing pair did not match current state");
            });
        }

        private static void PreservesCanonicalPathIdentityAndFreshness()
        {
            WithDirectory(dir =>
            {
                var primary = Path.Combine(dir, "freshness.qsdb");
                File.WriteAllText(primary, "v1");
                var stamp = ProjectSidecarRevisionStamp.Capture(primary);

                Require(stamp.IsForPath(primary), "canonical path identity was not retained");
                Require(!stamp.IsForPath(" " + primary), "padded path was accepted as canonical identity");

                File.WriteAllText(primary, "v2");
                Require(!stamp.MatchesCurrent(), "content mutation was not detected by revision freshness");
            });
        }

        private static bool TryCreateSymbolicLink(string linkPath, string targetPath)
        {
            try
            {
                File.CreateSymbolicLink(linkPath, targetPath);
                return true;
            }
            catch (PlatformNotSupportedException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (IOException) when (OperatingSystem.IsWindows()) { return false; }
        }

        private static void ExpectInvalidData(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidDataException)
            {
                return;
            }

            throw new InvalidOperationException("ProjectSidecarRevisionRedirectSmoke: " + message + ".");
        }

        private static void WithDirectory(Action<string> action)
        {
            var dir = Path.Combine(Path.GetTempPath(), "qs3d-sidecar-redirect-" + Guid.NewGuid().ToString("N"));
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
            if (!condition) throw new InvalidOperationException("ProjectSidecarRevisionRedirectSmoke: " + message + ".");
        }
    }
}
