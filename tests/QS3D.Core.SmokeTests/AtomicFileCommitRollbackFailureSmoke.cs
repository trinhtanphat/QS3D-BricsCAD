using System;
using System.IO;
using System.Reflection;

namespace QS3D.Core.SmokeTests
{
    internal static class AtomicFileCommitRollbackFailureSmoke
    {
        private const string RollbackFailureDataKey = "QS3D.AtomicFileCommit.RollbackFailure";

        internal static void Run()
        {
            var assembly = typeof(QS3D.Core.Persistence.QsdbProjectStore).Assembly;
            var atomicType = assembly.GetType("QS3D.Core.Persistence.AtomicFileCommit", throwOnError: true)
                ?? throw new InvalidOperationException("AtomicFileCommit type was not found.");
            var helper = atomicType.GetMethod(
                "RecordRollbackFailure",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("AtomicFileCommit rollback evidence helper was not found.");
            var restorePreviousBackup = atomicType.GetMethod(
                "RestorePreviousBackup",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("AtomicFileCommit previous-backup recovery helper was not found.");

            var publicationFailure = CapturePublicationFailureWithStack();
            var rollbackFailure = new UnauthorizedAccessException("rollback sentinel");
            var originalStack = publicationFailure.StackTrace;
            Require(!string.IsNullOrWhiteSpace(originalStack),
                "Publication sentinel must carry real stack evidence before rollback metadata is attached.");

            var result = helper.Invoke(null, new object[] { publicationFailure, rollbackFailure });

            Require(result == null, "Rollback evidence helper must not replace the primary publication exception.");
            Require(ReferenceEquals(publicationFailure.Data[RollbackFailureDataKey], rollbackFailure),
                "Rollback evidence helper did not retain the exact rollback exception on the primary failure.");
            Require(publicationFailure.StackTrace == originalStack,
                "Rollback evidence helper changed publication exception stack evidence.");

            var secondRollbackFailure = new IOException("second rollback sentinel");
            helper.Invoke(null, new object[] { publicationFailure, secondRollbackFailure });
            if (publicationFailure.Data[RollbackFailureDataKey] is not AggregateException aggregate)
            {
                throw new InvalidOperationException(
                    "Multiple rollback failures must retain prior evidence instead of overwriting it.");
            }
            Require(aggregate.InnerExceptions.Count == 2,
                "Multiple rollback failures did not retain both rollback exceptions.");
            Require(ReferenceEquals(aggregate.InnerExceptions[0], rollbackFailure),
                "First rollback failure evidence was lost or reordered.");
            Require(ReferenceEquals(aggregate.InnerExceptions[1], secondRollbackFailure),
                "Second rollback failure evidence was lost or reordered.");

            PreviousBackupRestoreFailureIsRecorded(restorePreviousBackup);
        }

        private static void PreviousBackupRestoreFailureIsRecorded(MethodInfo restorePreviousBackup)
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "qs3d-rollback-evidence-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var previousBackup = Path.Combine(directory, "project.qsdb.bak.previous");
                var canonicalBackup = Path.Combine(directory, "project.qsdb.bak");
                File.WriteAllText(previousBackup, "older-good-backup");
                File.WriteAllText(canonicalBackup, "unexpected-occupant");

                var publicationFailure = CapturePublicationFailureWithStack();
                var originalStack = publicationFailure.StackTrace;
                var result = restorePreviousBackup.Invoke(
                    null,
                    new object?[] { previousBackup, canonicalBackup, publicationFailure });

                Require(result == null,
                    "Previous-backup recovery must remain best-effort and must not replace the primary publication exception.");
                Require(publicationFailure.StackTrace == originalStack,
                    "Previous-backup recovery changed publication exception stack evidence.");
                if (publicationFailure.Data[RollbackFailureDataKey] is not IOException recoveryFailure)
                {
                    throw new InvalidOperationException(
                        "Occupied previous-backup destination must be retained as rollback evidence on the primary failure.");
                }
                Require(
                    recoveryFailure.Message.IndexOf("backup", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Previous-backup recovery evidence did not identify the backup recovery failure.");
                Require(File.Exists(previousBackup),
                    "Failed previous-backup recovery must preserve the staged older backup for diagnosis/retry.");
                Require(File.Exists(canonicalBackup),
                    "Best-effort previous-backup recovery must not overwrite an unexpected canonical backup occupant.");
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static IOException CapturePublicationFailureWithStack()
        {
            try
            {
                throw new IOException("publication sentinel");
            }
            catch (IOException ex)
            {
                return ex;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
