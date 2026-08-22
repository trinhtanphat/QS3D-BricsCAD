using System;
using System.IO;

namespace QS3D.Core.Persistence
{
    internal static class AtomicFileCommit
    {
        private static readonly StringComparison PathComparison =
            Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        public static string CreateTempPath(string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Destination path is required.", nameof(destinationPath));
            return Path.GetFullPath(destinationPath) + "." + Guid.NewGuid().ToString("N") + ".tmp";
        }

        public static void ReplaceWithBackup(string tempPath, string destinationPath, string backupPath)
        {
            Validate(tempPath, destinationPath, out var temp, out var destination);
            var backup = RequireFullPath(backupPath, nameof(backupPath), "Backup path is required.");
            RequireDistinct(temp, nameof(tempPath), backup, nameof(backupPath));
            RequireDistinct(destination, nameof(destinationPath), backup, nameof(backupPath));

            if (!File.Exists(destination))
            {
                File.Move(temp, destination);
                return;
            }

            try
            {
                File.Replace(temp, destination, backup, true);
            }
            catch (PlatformNotSupportedException)
            {
                MoveWithRecovery(temp, destination, backup, keepBackup: true);
            }
        }

        public static void ReplaceWithoutBackup(string tempPath, string destinationPath)
        {
            Validate(tempPath, destinationPath, out var temp, out var destination);
            if (!File.Exists(destination))
            {
                File.Move(temp, destination);
                return;
            }

            var safetyBackup = destination + "." + Guid.NewGuid().ToString("N") + ".replace.bak";
            try
            {
                File.Replace(temp, destination, safetyBackup, true);
                TryDelete(safetyBackup);
            }
            catch (PlatformNotSupportedException)
            {
                MoveWithRecovery(temp, destination, safetyBackup, keepBackup: false);
            }
        }

        public static void PublishNew(string tempPath, string destinationPath, string backupPath)
        {
            Validate(tempPath, destinationPath, out var temp, out var destination);
            var backup = RequireFullPath(backupPath, nameof(backupPath), "Backup path is required.");
            RequireDistinct(temp, nameof(tempPath), backup, nameof(backupPath));
            RequireDistinct(destination, nameof(destinationPath), backup, nameof(backupPath));
            if (File.Exists(destination) || Directory.Exists(destination) || File.Exists(backup) || Directory.Exists(backup))
                throw new IOException("QS3D refused to publish a new project over an existing sidecar pair.");

            // File.Move is the create-new conditional commit for the primary. The
            // caller holds ProjectFileLock, so cooperating QS3D writers cannot pass
            // an absence check and then overwrite one another.
            File.Move(temp, destination);
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

        private static void Validate(string tempPath, string destinationPath, out string temp, out string destination)
        {
            temp = RequireFullPath(tempPath, nameof(tempPath), "Temporary path is required.");
            destination = RequireFullPath(destinationPath, nameof(destinationPath), "Destination path is required.");
            RequireDistinct(temp, nameof(tempPath), destination, nameof(destinationPath));
            if (!File.Exists(temp)) throw new FileNotFoundException("Temporary file was not found.", temp);
        }

        private static string RequireFullPath(string path, string paramName, string requiredMessage)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(requiredMessage, paramName);
            return Path.GetFullPath(path);
        }

        private static void RequireDistinct(string leftPath, string leftName, string rightPath, string rightName)
        {
            if (string.Equals(leftPath, rightPath, PathComparison))
                throw new ArgumentException(leftName + " and " + rightName + " must resolve to distinct paths.", rightName);
        }
    }
}
