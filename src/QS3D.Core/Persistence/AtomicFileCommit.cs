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
            RequireSafe(temp, "temporary");
            RequireSafe(destination, "destination");
            RequireSafe(backup, "backup");

            if (!File.Exists(destination))
            {
                PublishMissingDestinationWithoutStaleBackup(temp, destination, backup);
                return;
            }

            try
            {
                RequireSafe(temp, "temporary");
                RequireSafe(destination, "destination");
                RequireSafe(backup, "backup");
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
                RequireSafe(temp, "temporary");
                RequireSafe(destination, "destination");
                File.Move(temp, destination);
                return;
            }

            var safetyBackup = destination + "." + Guid.NewGuid().ToString("N") + ".replace.bak";
            try
            {
                RequireSafe(temp, "temporary");
                RequireSafe(destination, "destination");
                RequireSafe(safetyBackup, "safety-backup");
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
            RequireSafe(temp, "temporary");
            RequireSafe(destination, "destination");
            RequireSafe(backup, "backup");
            if (File.Exists(destination) || Directory.Exists(destination) || File.Exists(backup) || Directory.Exists(backup))
                throw new IOException("QS3D refused to publish a new project over an existing sidecar pair.");

            // File.Move is the create-new conditional commit for the primary. The
            // caller holds ProjectFileLock, so cooperating QS3D writers cannot pass
            // an absence check and then overwrite one another.
            RequireSafe(temp, "temporary");
            RequireSafe(destination, "destination");
            RequireSafe(backup, "backup");
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
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var cleanupPath = path!;
                RequireSafe(cleanupPath, "cleanup");
                if (!File.Exists(cleanupPath)) return;
                RequireSafe(cleanupPath, "cleanup");
                File.Delete(cleanupPath);
            }
            catch (InvalidDataException) { }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void PublishMissingDestinationWithoutStaleBackup(string tempPath, string destinationPath, string backupPath)
        {
            RequireSafe(tempPath, "temporary");
            RequireSafe(destinationPath, "destination");
            RequireSafe(backupPath, "backup");
            if (Directory.Exists(backupPath))
                throw new IOException("QS3D refused to recreate a project while its backup path is a directory.");

            string? staleBackupSafety = null;
            if (File.Exists(backupPath))
            {
                staleBackupSafety = backupPath + "." + Guid.NewGuid().ToString("N") + ".stale";
                RequireSafe(staleBackupSafety, "stale-backup safety");
                RequireSafe(backupPath, "backup");
                File.Move(backupPath, staleBackupSafety);
            }

            var installed = false;
            try
            {
                RequireSafe(tempPath, "temporary");
                RequireSafe(destinationPath, "destination");
                RequireSafe(backupPath, "backup");
                if (File.Exists(backupPath) || Directory.Exists(backupPath))
                    throw new IOException("A QS3D backup appeared while recreating a missing project primary.");

                File.Move(tempPath, destinationPath);
                installed = true;

                // A normal replacement backup represents the immediately previous
                // primary generation. When the primary was already missing, an old
                // .bak cannot satisfy that contract and must never remain eligible
                // for LoadWithBackupFallback beside the newly published generation.
                if (File.Exists(backupPath) || Directory.Exists(backupPath))
                {
                    try { File.Delete(destinationPath); }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        throw new IOException("A QS3D backup appeared during primary recreation and the new primary could not be rolled back.", ex);
                    }
                    installed = false;
                    throw new IOException("A QS3D backup appeared during primary recreation; the new primary was rolled back.");
                }
            }
            finally
            {
                if (!installed)
                    RestorePreviousBackup(staleBackupSafety, backupPath);
                else if (!string.IsNullOrWhiteSpace(staleBackupSafety))
                    TryDelete(staleBackupSafety);
            }
        }

        private static void MoveWithRecovery(string tempPath, string destinationPath, string backupPath, bool keepBackup)
        {
            RequireSafe(tempPath, "temporary");
            RequireSafe(destinationPath, "destination");
            RequireSafe(backupPath, "backup");

            string? previousBackupSafety = null;
            if (File.Exists(backupPath))
            {
                previousBackupSafety = backupPath + "." + Guid.NewGuid().ToString("N") + ".previous";
                RequireSafe(previousBackupSafety, "previous-backup safety");
                RequireSafe(backupPath, "backup");
                File.Move(backupPath, previousBackupSafety);
            }

            var destinationStaged = false;
            var installed = false;
            try
            {
                RequireSafe(destinationPath, "destination");
                RequireSafe(backupPath, "backup");
                File.Move(destinationPath, backupPath);
                destinationStaged = true;
                RequireSafe(tempPath, "temporary");
                RequireSafe(destinationPath, "destination");
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
                            RequireSafe(destinationPath, "destination");
                            RequireSafe(backupPath, "backup");
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
            if (string.IsNullOrWhiteSpace(previousBackupSafety)) return;
            var previousBackupPath = previousBackupSafety!;
            if (!File.Exists(previousBackupPath)) return;
            try
            {
                RequireSafe(previousBackupPath, "previous-backup safety");
                RequireSafe(backupPath, "backup");
                if (!File.Exists(backupPath)) File.Move(previousBackupPath, backupPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void Validate(string tempPath, string destinationPath, out string temp, out string destination)
        {
            tempPath = RequireFullPath(tempPath, nameof(tempPath), "Temporary path is required.");
            destination = RequireFullPath(destinationPath, nameof(destinationPath), "Destination path is required.");
            RequireDistinct(tempPath, nameof(tempPath), destination, nameof(destinationPath));
            RequireSafe(tempPath, "temporary");
            RequireSafe(destination, "destination");
            if (!File.Exists(tempPath)) throw new FileNotFoundException("Temporary file was not found.", tempPath);
            temp = tempPath;
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

        private static void RequireSafe(string path, string role)
        {
            PersistencePathSafety.RequireNonRedirected(path, role);
        }
    }
}
