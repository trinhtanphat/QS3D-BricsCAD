using System;
using System.Collections.Generic;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationDirtyBatchAtomicitySmoke
    {
        public static void Run()
        {
            InvalidBatchDoesNotCommitPartialIds();
            ThrowingEnumerableDoesNotCommitPartialIds();
            FailedBatchPreservesExistingPendingState();
            SuccessfulBatchKeepsCanonicalSetSemantics();
        }

        private static void InvalidBatchDoesNotCommitPartialIds()
        {
            var controller = new CoordinationIncrementalScanController();

            Throws<ArgumentException>(() => controller.MarkDirty(new[] { "NEW", " " }));

            Equal(0, controller.PendingDirtyCount,
                "Failed bulk dirty validation retained a partial ItemId.");
        }

        private static void ThrowingEnumerableDoesNotCommitPartialIds()
        {
            var controller = new CoordinationIncrementalScanController();

            Throws<InvalidOperationException>(() => controller.MarkDirty(YieldThenThrow()));

            Equal(0, controller.PendingDirtyCount,
                "Throwing dirty enumerable retained values yielded before the exception.");
        }

        private static void FailedBatchPreservesExistingPendingState()
        {
            var controller = new CoordinationIncrementalScanController();
            controller.MarkDirty("EXISTING");

            Throws<ArgumentException>(() => controller.MarkDirty(new[] { "NEW", "\u0001" }));

            Equal(1, controller.PendingDirtyCount,
                "Failed bulk dirty validation changed pre-existing pending state.");
        }

        private static void SuccessfulBatchKeepsCanonicalSetSemantics()
        {
            var controller = new CoordinationIncrementalScanController();

            controller.MarkDirty(new[] { "A", "a", "B" });

            Equal(2, controller.PendingDirtyCount,
                "Successful bulk dirty marking stopped deduplicating canonical IDs case-insensitively.");
        }

        private static IEnumerable<string> YieldThenThrow()
        {
            yield return "NEW";
            throw new InvalidOperationException("Expected fixture enumeration failure.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
