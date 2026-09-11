using System;
using System.Collections;
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

            var publicationFailure = new IOException("publication sentinel");
            var rollbackFailure = new UnauthorizedAccessException("rollback sentinel");
            var originalStack = publicationFailure.StackTrace;

            var result = helper.Invoke(null, new object[] { publicationFailure, rollbackFailure });

            Require(result == null, "Rollback evidence helper must not replace the primary publication exception.");
            Require(ReferenceEquals(publicationFailure.Data[RollbackFailureDataKey], rollbackFailure),
                "Rollback evidence helper did not retain the exact rollback exception on the primary failure.");
            Require(publicationFailure.StackTrace == originalStack,
                "Rollback evidence helper changed publication exception stack evidence.");

            var secondRollbackFailure = new IOException("second rollback sentinel");
            helper.Invoke(null, new object[] { publicationFailure, secondRollbackFailure });
            Require(publicationFailure.Data[RollbackFailureDataKey] is AggregateException aggregate,
                "Multiple rollback failures must retain prior evidence instead of overwriting it.");
            Require(aggregate.InnerExceptions.Count == 2,
                "Multiple rollback failures did not retain both rollback exceptions.");
            Require(ReferenceEquals(aggregate.InnerExceptions[0], rollbackFailure),
                "First rollback failure evidence was lost or reordered.");
            Require(ReferenceEquals(aggregate.InnerExceptions[1], secondRollbackFailure),
                "Second rollback failure evidence was lost or reordered.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
