using System;
using System.IO;

namespace QS3D.Core.Persistence
{
    internal static class AtomicFileCommit
    {
        private const string RollbackFailureDataKey = "QS3D.AtomicFileCommit.RollbackFailure";

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
            try
            {
                RequireSafe(backup, "backup");
            }
            catch (Exception publicationFailure) when (publicationFailure is IOException || publicationFailure is UnauthorizedAccessException || publicationFailure is InvalidDataException)
            {
                try
                {
                    RequireSafe(destination, "destination");
                    File.Delete(destination);
                }
                catch (Exception rollbackFailure) when (rollbackFailure is IOException || rollbackFailure is UnauthorizedAccessException || rollbackFailure is InvalidDataException)
                {
                    RecordRollbackFailure(publicationFailure, rollbackFailure);
                }
                throw;
            }
            if (!File.Exists(backup) && !Directory.Exists(backup)) return;

            try
            {
                RequireSafe(destination, "destination");
                File.Delete(destination);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
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
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
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
            Exception? publicationFailure = null;
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
                RequireSafe(backupPath, "backup");
                if (File.Exists(backupPath) || Directory.Exists(backupPath))
                {
                    try
                    {
                        // From this point the newly installed primary is rejected and
                        // rollback owns cleanup. Flip the state before File.Delete so a
                        // delete failure cannot send finally down the committed-install
                        // cleanup path and discard the staged older backup.
                        installed = false;
                        RequireSafe(destinationPath, "destination");
                        File.Delete(destinationPath);
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
                    {
                        throw new IOException("A QS3D backup appeared during primary recreation and the new primary could not be rolled back.", ex);
                    }
                    throw new IOException("A QS3D backup appeared during primary recreation; the new primary was rolled back.");
                }
            }
            catch (Exception ex)
            {
                publicationFailure = ex;
                throw;
            }
            finally
            {
                if (!installed)
                    RestorePreviousBackup(staleBackupSafety, backupPath, publicationFailure);
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
            Exception? publicationFailure = null;
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
            catch (Exception ex)
            {
                publicationFailure = ex;
                throw;
            }
            finally
            {
                if (!installed)
                {
                    if (destinationStaged && publicationFailure != null)
                    {
                        try
                        {
                            RequireSafe(destinationPath, "destination");
                            RequireSafe(backupPath, "backup");

                            if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                            {
                                RecordRollbackFailure(
                                    publicationFailure,
                                    new IOException("QS3D could not restore the staged primary because the destination became occupied during rollback."));
                            }
                            else if (!File.Exists(backupPath))
                            {
                                RecordRollbackFailure(
                                    publicationFailure,
                                    new IOException("QS3D could not restore the staged primary because its rollback backup is missing."));
                            }
                            else
                            {
                                // Revalidate after the final filesystem observations and
                                // immediately before moving the staged primary back.
                                RequireSafe(backupPath, "backup");
                                RequireSafe(destinationPath, "destination");
                                File.Move(backupPath, destinationPath);
                            }
                        }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
                        {
                            RecordRollbackFailure(publicationFailure, ex);
                        }
                    }
                    RestorePreviousBackup(previousBackupSafety, backupPath, publicationFailure);
                }
                else if (!string.IsNullOrWhiteSpace(previousBackupSafety))
                {
                    TryDelete(previousBackupSafety);
                }
            }

            if (!keepBackup) TryDelete(backupPath);
        }

        private static void RecordRollbackFailure(Exception publicationFailure, Exception rollbackFailure)
        {
            var existing = publicationFailure.Data[RollbackFailureDataKey];
            if (existing is AggregateException aggregate)
            {
                var failures = new Exception[aggregate.InnerExceptions.Count + 1];
                for (var i = 0; i < aggregate.InnerExceptions.Count; i++)
                    failures[i] = aggregate.InnerExceptions[i];
                failures[failures.Length - 1] = rollbackFailure;
                publicationFailure.Data[RollbackFailureDataKey] = new AggregateException(failures);
                return;
            }

            if (existing is Exception existingFailure)
            {
                publicationFailure.Data[RollbackFailureDataKey] = new AggregateException(existingFailure, rollbackFailure);
                return;
            }

            publicationFailure.Data[RollbackFailureDataKey] = rollbackFailure;
        }

        private static void RestorePreviousBackup(
            string? previousBackupSafety,
            string backupPath,
            Exception? publicationFailure = null)
        {
            if (string.IsNullOrWhiteSpace(previousBackupSafety)) return;
            var previousBackupPath = previousBackupSafety!;
            if (!File.Exists(previousBackupPath))
            {
                if (publicationFailure != null)
                {
                    RecordRollbackFailure(
                        publicationFailure,
                        new IOException("QS3D could not restore the previous backup because its staged safety file is missing."));
                }
                return;
            }

            try
            {
                RequireSafe(previousBackupPath, "previous-backup safety");
                RequireSafe(backupPath, "backup");
                if (File.Exists(backupPath) || Directory.Exists(backupPath))
                {
                    if (publicationFailure != null)
                    {
                        RecordRollbackFailure(
                            publicationFailure,
                            new IOException("QS3D could not restore the previous backup because the canonical backup path became occupied during rollback."));
                    }
                    return;
                }

                RequireSafe(previousBackupPath, "previous-backup safety");
                RequireSafe(backupPath, "backup");
                File.Move(previousBackupPath, backupPath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
            {
                if (publicationFailure != null)
                    RecordRollbackFailure(publicationFailure, ex);
            }
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
