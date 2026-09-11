using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace QS3D.Core.SmokeTests
{
    internal static class AtomicFileCommitRollbackFailureSmoke
    {
        internal static void Run()
        {
            var assembly = typeof(QS3D.Core.Persistence.QsdbProjectStore).Assembly;
            var atomicType = assembly.GetType("QS3D.Core.Persistence.AtomicFileCommit", throwOnError: true)
                ?? throw new InvalidOperationException("AtomicFileCommit type was not found.");
            var helper = atomicType.GetMethod(
                "CreateRollbackFailure",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("AtomicFileCommit rollback evidence helper was not found.");

            var publicationFailure = new IOException("publication sentinel");
            var rollbackFailure = new UnauthorizedAccessException("rollback sentinel");
            var result = helper.Invoke(null, new object[] { publicationFailure, rollbackFailure }) as IOException
                ?? throw new InvalidOperationException("AtomicFileCommit rollback evidence helper did not return IOException.");

            Require(result.Message.IndexOf("rollback", StringComparison.OrdinalIgnoreCase) >= 0,
                "Combined atomic publication failure must identify rollback failure in its message.");
            var aggregate = result.InnerException as AggregateException
                ?? throw new InvalidOperationException("Combined atomic publication failure did not retain both failures.");
            var failures = aggregate.InnerExceptions;
            Require(failures.Count == 2, "Combined atomic publication failure must contain exactly publication and rollback failures.");
            Require(failures.Any(x => ReferenceEquals(x, publicationFailure)),
                "Combined atomic publication failure lost the original publication exception.");
            Require(failures.Any(x => ReferenceEquals(x, rollbackFailure)),
                "Combined atomic publication failure lost the rollback exception.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
