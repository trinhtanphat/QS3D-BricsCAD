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
                MoveWithRecovery(tempPath, destination, backup, keepBackup: true);
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
                File.Replace(tempPath, destination, safetyBackup, true);
                TryDelete(safetyBackup);
            }
            catch (PlatformNotSupportedException)
            {
                MoveWithRecovery(tempPath, destination, safetyBackup, keepBackup: false);
            }
        }

        public static void PublishNew(string tempPath, string destinationPath, string backupPath)
        {
            Validate(tempPath, destinationPath);
            if (string.IsNullOrWhiteSpace(backupPath)) throw new ArgumentException("Backup path is required.", nameof(backupPath));
            var destination = Path.GetFullPath(destinationPath);
            var backup = Path.GetFullPath(backupPath);
            if (File.Exists(destination) || Directory.Exists(destination) || File.Exists(backup) || Directory.Exists(backup))
                throw new IOException("QS3D refused to publish a new project over an existing sidecar pair.");

            // File.Move is the create-new conditional commit for the primary. The
            // caller holds ProjectFileLock, so cooperating QS3D writers cannot pass
            // an absence check and then overwrite one another.
            File.Move(tempPath, destination);
            if (!File.Exists(backup) && !Directory.Exists(backup)) return;

            try { File.Delete(destination); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new IOException("A QS3D backup appeared during create-new publication and the new primary could not be rolled back.", ex);
            }
            throw new IOException("A QS3D backup appeared during create-new publication; the new primary was rolled back.");
        }

        public static void TryDelete(string? path)
        {
            try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void MoveWithRecovery(string tempPath, string destinationPath, string backupPath, bool keepBackup)
        {
            string? previousBackupSafety = null;
            if (File.Exists(backupPath))
            {
                previousBackupSafety = backupPath + "." + Guid.NewGuid().ToString("N") + ".previous";
                File.Move(backupPath, previousBackupSafety);
            }

            var destinationStaged = false;
            var installed = false;
            try
            {
                File.Move(destinationPath, backupPath);
                destinationStaged = true;
                File.Move(tempPath, destinationPath);
                installed = true;
            }
            finally
            {
                if (!installed)
                {
                    if (destinationStaged)
                    {
                        try
                        {
                            if (!File.Exists(destinationPath) && File.Exists(backupPath))
                                File.Move(backupPath, destinationPath);
                        }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                    }
                    RestorePreviousBackup(previousBackupSafety, backupPath);
                }
                else if (!string.IsNullOrWhiteSpace(previousBackupSafety))
                {
                    TryDelete(previousBackupSafety);
                }
            }

            if (!keepBackup) TryDelete(backupPath);
        }

        private static void RestorePreviousBackup(string? previousBackupSafety, string backupPath)
        {
            if (string.IsNullOrWhiteSpace(previousBackupSafety) || !File.Exists(previousBackupSafety)) return;
            try
            {
                if (!File.Exists(backupPath)) File.Move(previousBackupSafety, backupPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void Validate(string tempPath, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(tempPath)) throw new ArgumentException("Temporary path is required.", nameof(tempPath));
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path is required.", nameof(destinationPath));
            if (!File.Exists(tempPath)) throw new FileNotFoundException("Temporary file was not found.", tempPath);
        }
    }
}
