using System;
using System.IO;

namespace QS3D.Core.Persistence
{
    internal static class AtomicFileCommit
    {
        public static string CreateTempPath(string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path is required.", nameof(destinationPath));
            return Path.GetFullPath(destinationPath) + "." + Guid.NewGuid().ToString("N") + ".tmp";
        }

        public static void ReplaceWithBackup(string tempPath, string destinationPath, string backupPath)
        {
            Validate(tempPath, destinationPath);
            if (string.IsNullOrWhiteSpace(backupPath)) throw new ArgumentException("Backup path is required.", nameof(backupPath));
            var destination = Path.GetFullPath(destinationPath);
            var backup = Path.GetFullPath(backupPath);

            if (!File.Exists(destination))
            {
                File.Move(tempPath, destination);
                return;
            }

            try
            {
                File.Replace(tempPath, destination, backup, true);
            }
            catch (PlatformNotSupportedException)
            {
                CopyWithRecovery(tempPath, destination, backup, keepBackup: true);
            }
        }

        public static void ReplaceWithoutBackup(string tempPath, string destinationPath)
        {
            Validate(tempPath, destinationPath);
            var destination = Path.GetFullPath(destinationPath);
            if (!File.Exists(destination))
            {
                File.Move(tempPath, destination);
                return;
            }

            var safetyBackup = destination + "." + Guid.NewGuid().ToString("N") + ".replace.bak";
            try
            {
                try
                {
                    File.Replace(tempPath, destination, safetyBackup, true);
                }
                catch (PlatformNotSupportedException)
                {
                    CopyWithRecovery(tempPath, destination, safetyBackup, keepBackup: false);
                }
            }
            finally
            {
                TryDelete(safetyBackup);
            }
        }

        public static void TryDelete(string path)
        {
            try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void CopyWithRecovery(string tempPath, string destinationPath, string backupPath, bool keepBackup)
        {
            File.Copy(destinationPath, backupPath, true);
            try
            {
                File.Copy(tempPath, destinationPath, true);
                File.Delete(tempPath);
                if (!keepBackup) TryDelete(backupPath);
            }
            catch
            {
                try { if (File.Exists(backupPath)) File.Copy(backupPath, destinationPath, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                throw;
            }
        }

        private static void Validate(string tempPath, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(tempPath)) throw new ArgumentException("Temporary path is required.", nameof(tempPath));
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path is required.", nameof(destinationPath));
            if (!File.Exists(tempPath)) throw new FileNotFoundException("Temporary file was not found.", tempPath);
        }
    }
}
