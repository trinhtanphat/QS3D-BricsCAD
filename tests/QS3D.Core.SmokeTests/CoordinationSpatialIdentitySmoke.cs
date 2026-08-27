using System;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationSpatialIdentitySmoke
    {
        internal static void Run()
        {
            RejectsPaddedSpatialIdentity();
            RejectsPaddedChangedIdsWithoutAliasing();
            RejectsPaddedDirtyIdsAtomically();
            PreservesCanonicalCaseInsensitiveMembership();
            PreservesOrdinalRevisionDiff();
        }

        private static void RejectsPaddedSpatialIdentity()
        {
            Throws<ArgumentException>(
                () => new CoordinationSpatialItem(" E1", "r1", Bounds()),
                "Spatial ItemId accepted leading whitespace.");
            Throws<ArgumentException>(
                () => new CoordinationSpatialItem("E1 ", "r1", Bounds()),
                "Spatial ItemId accepted trailing whitespace.");
            Throws<ArgumentException>(
                () => new CoordinationSpatialItem("E1", " r1", Bounds()),
                "Spatial revision accepted leading whitespace.");
            Throws<ArgumentException>(
                () => new CoordinationSpatialItem("E1", "r1 ", Bounds()),
                "Spatial revision accepted trailing whitespace.");
        }

        private static void RejectsPaddedChangedIdsWithoutAliasing()
        {
            var index = CreateOverlappingIndex("r1");
            Throws<ArgumentException>(
                () => index.QueryChangedPairs(new[] { " E1 " }),
                "Changed-pair lookup silently canonicalized a padded ItemId.");
        }

        private static void RejectsPaddedDirtyIdsAtomically()
        {
            var controller = new CoordinationIncrementalScanController();
            Throws<ArgumentException>(
                () => controller.MarkDirty(" E1 "),
                "Single dirty marking silently canonicalized a padded ItemId.");
            Equal(0, controller.PendingDirtyCount, "Rejected single dirty ItemId mutated pending state.");

            Throws<ArgumentException>(
                () => controller.MarkDirty(new[] { "E1", " E2 " }),
                "Bulk dirty marking silently canonicalized a padded ItemId.");
            Equal(0, controller.PendingDirtyCount, "Rejected bulk dirty batch partially mutated pending state.");
        }

        private static void PreservesCanonicalCaseInsensitiveMembership()
        {
            var index = CreateOverlappingIndex("r1");
            var pairs = index.QueryChangedPairs(new[] { "e1" });
            Equal(1, pairs.Count, "Canonical case-insensitive changed ItemId lookup no longer resolves the current item.");

            var controller = new CoordinationIncrementalScanController();
            controller.MarkDirty("e1");
            Equal(1, controller.PendingDirtyCount, "Canonical case-insensitive dirty ItemId was not retained.");
            controller.ApplySnapshot(1d, new[]
            {
                new CoordinationSpatialItem("E1", "r1", Bounds()),
                new CoordinationSpatialItem("E2", "r1", Bounds())
            });
            Equal(0, controller.PendingDirtyCount, "Accepted dirty ItemId was not consumed by snapshot commit.");
        }

        private static void PreservesOrdinalRevisionDiff()
        {
            var lower = CreateOverlappingIndex("r1");
            var same = CreateOverlappingIndex("r1");
            True(same.Diff(lower).IsEmpty, "Stable canonical revisions unexpectedly produced a spatial delta.");

            var differentCase = CreateOverlappingIndex("R1");
            var delta = differentCase.Diff(lower);
            Equal(2, delta.ChangedOrAddedIds.Count, "Revision comparison stopped using exact ordinal semantics.");
        }

        private static CoordinationSpatialIndex CreateOverlappingIndex(string revision)
        {
            return new CoordinationSpatialIndex(1d, new[]
            {
                new CoordinationSpatialItem("E1", revision, Bounds()),
                new CoordinationSpatialItem("E2", revision, Bounds())
            });
        }

        private static CoordinationBounds Bounds()
        {
            return new CoordinationBounds(0d, 0d, 0d, 0.5d, 0.5d, 0.5d);
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception(message);
        }
    }
}
