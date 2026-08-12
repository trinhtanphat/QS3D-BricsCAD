using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticChangeReviewIdIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            BlankBeforeIdFailsClosed();
            PaddedAfterIdFailsClosed();
            CanonicalIdsArePreservedExactly();
        }

        private static void BlankBeforeIdFailsClosed()
        {
            Throws<InvalidOperationException>(() =>
                new SemanticChangeReviewBuilder().Build(Snapshot("   "), Snapshot("REV-AFTER")));
        }

        private static void PaddedAfterIdFailsClosed()
        {
            Throws<InvalidOperationException>(() =>
                new SemanticChangeReviewBuilder().Build(Snapshot("REV-BEFORE"), Snapshot(" REV-AFTER ")));
        }

        private static void CanonicalIdsArePreservedExactly()
        {
            const string beforeId = "REV-BEFORE/2026-08-12";
            const string afterId = "REV-AFTER/2026-08-12";
            var review = new SemanticChangeReviewBuilder().Build(Snapshot(beforeId), Snapshot(afterId));
            if (!string.Equals(review.BeforeRevisionId, beforeId, StringComparison.Ordinal) ||
                !string.Equals(review.AfterRevisionId, afterId, StringComparison.Ordinal))
                throw new InvalidOperationException("Semantic change review did not preserve canonical revision ids exactly.");
            if (review.HasChanges || review.Elements.Count != 0 || review.Summary.TotalElementCount != 0)
                throw new InvalidOperationException("Empty semantic snapshots must still produce an empty review after id validation.");
        }

        private static RevisionSnapshot Snapshot(string id) => new RevisionSnapshot
        {
            Id = id,
            CreatedUtc = DateTime.UtcNow
        };

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
