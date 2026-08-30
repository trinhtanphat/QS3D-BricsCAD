using System;
using System.Collections;
using System.Collections.Generic;

namespace QS3D.Core.Export
{
    internal static class IfcRoundTripKnownCountContract
    {
        internal static void RequireStableDuringTraversal<T>(
            IEnumerable<T> values,
            int? admittedCount,
            string collectionLabel)
        {
            RequireStable(
                values,
                admittedCount,
                collectionLabel,
                "during traversal");
        }

        internal static void RequireStableAfterTraversal<T>(
            IEnumerable<T> values,
            int? admittedCount,
            string collectionLabel)
        {
            RequireStable(
                values,
                admittedCount,
                collectionLabel,
                "after traversal");
        }

        private static void RequireStable<T>(
            IEnumerable<T> values,
            int? admittedCount,
            string collectionLabel,
            string phase)
        {
            if (!admittedCount.HasValue)
                return;

            var reboundCount = TryGetKnownCount(
                values,
                out var conflictingKnownCounts,
                out var negativeKnownCount);

            if (negativeKnownCount)
                throw new InvalidOperationException(
                    collectionLabel + " source exposes an invalid negative known Count value " + phase + ".");
            if (conflictingKnownCounts)
                throw new InvalidOperationException(
                    collectionLabel + " source exposes conflicting known Count values " + phase + ".");
            if (!reboundCount.HasValue || reboundCount.Value != admittedCount.Value)
                throw new InvalidOperationException(
                    collectionLabel + " source Count changed during traversal.");
        }

        private static int? TryGetKnownCount<T>(
            IEnumerable<T> values,
            out bool conflictingKnownCounts,
            out bool negativeKnownCount)
        {
            conflictingKnownCounts = false;
            negativeKnownCount = false;
            int? knownCount = null;

            if (values is ICollection<T> collection)
                knownCount = ObserveKnownCount(
                    knownCount,
                    collection.Count,
                    ref conflictingKnownCounts,
                    ref negativeKnownCount);
            if (values is IReadOnlyCollection<T> readOnlyCollection)
                knownCount = ObserveKnownCount(
                    knownCount,
                    readOnlyCollection.Count,
                    ref conflictingKnownCounts,
                    ref negativeKnownCount);
            if (values is ICollection nonGenericCollection)
                knownCount = ObserveKnownCount(
                    knownCount,
                    nonGenericCollection.Count,
                    ref conflictingKnownCounts,
                    ref negativeKnownCount);

            return knownCount;
        }

        private static int ObserveKnownCount(
            int? current,
            int observed,
            ref bool conflictingKnownCounts,
            ref bool negativeKnownCount)
        {
            if (observed < 0)
                negativeKnownCount = true;
            if (current.HasValue && current.Value != observed)
                conflictingKnownCounts = true;
            return !current.HasValue || observed > current.Value ? observed : current.Value;
        }
    }
}
