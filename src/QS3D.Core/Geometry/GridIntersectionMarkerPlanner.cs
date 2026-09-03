using System;
using System.Collections;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    public sealed class GridIntersectionMarkerPlan
    {
        public GridIntersectionMarkerPlan(
            string firstElementId,
            string secondElementId,
            string pairToken,
            string ownerToken,
            int occurrence,
            Point2 point)
        {
            FirstElementId = firstElementId ?? throw new ArgumentNullException(nameof(firstElementId));
            SecondElementId = secondElementId ?? throw new ArgumentNullException(nameof(secondElementId));
            PairToken = pairToken ?? throw new ArgumentNullException(nameof(pairToken));
            OwnerToken = ownerToken ?? throw new ArgumentNullException(nameof(ownerToken));
            if (occurrence < 0) throw new ArgumentOutOfRangeException(nameof(occurrence));
            Occurrence = occurrence;
            Point = point;
        }

        public string FirstElementId { get; }
        public string SecondElementId { get; }
        public string PairToken { get; }
        public string OwnerToken { get; }
        public int Occurrence { get; }
        public Point2 Point { get; }
    }

    /// <summary>
    /// Converts geometric Grid intersections into stable pair-owned marker identities.
    /// The occurrence belongs to a canonical Grid pair, never to either Grid alone.
    /// </summary>
    public static class GridIntersectionMarkerPlanner
    {
        private const int MaxMarkers = 100000;

        public static IReadOnlyList<GridIntersectionMarkerPlan> Plan(IEnumerable<GridIntersection> intersections)
        {
            if (intersections == null) throw new ArgumentNullException(nameof(intersections));

            var knownCount = GetKnownInputCount(intersections);
            if (knownCount.HasValue && knownCount.Value > MaxMarkers)
                ThrowTooManyMarkers();

            var source = MaterializeBounded(intersections, knownCount);
            var identities = GridIntersectionIdentityPlanner.Assign(source);
            var result = new List<GridIntersectionMarkerPlan>(identities.Count);
            foreach (var identity in identities)
            {
                result.Add(new GridIntersectionMarkerPlan(
                    identity.FirstElementId,
                    identity.SecondElementId,
                    identity.PairToken,
                    identity.OwnerToken,
                    identity.OccurrenceIndex,
                    identity.Point));
            }

            return result.AsReadOnly();
        }

        private static List<GridIntersection> MaterializeBounded(
            IEnumerable<GridIntersection> intersections,
            int? knownCount)
        {
            var source = new List<GridIntersection>();
            var observedCount = 0;
            using (var enumerator = intersections.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownInputCount(intersections, knownCount);
                    if (!enumerator.MoveNext())
                    {
                        RequireStableKnownInputCount(intersections, knownCount);
                        break;
                    }
                    RequireStableKnownInputCount(intersections, knownCount);

                    if (knownCount.HasValue && observedCount >= knownCount.Value)
                        throw new InvalidOperationException("Grid intersection marker source known count does not match traversal.");
                    if (observedCount >= MaxMarkers)
                        ThrowTooManyMarkers();

                    var intersection = enumerator.Current;
                    source.Add(intersection);
                    observedCount++;
                }
            }

            RequireStableKnownInputCount(intersections, knownCount);
            if (knownCount.HasValue && observedCount != knownCount.Value)
                throw new InvalidOperationException("Grid intersection marker source known count does not match traversal.");
            return source;
        }

        private static int? GetKnownInputCount(IEnumerable<GridIntersection> intersections)
        {
            var hasKnownCount = false;
            var firstKnownCount = 0;
            var maximumKnownCount = 0;
            var conflictingKnownCounts = false;

            if (intersections is ICollection<GridIntersection> collection)
                ObserveKnownCount(collection.Count, ref hasKnownCount, ref firstKnownCount, ref maximumKnownCount, ref conflictingKnownCounts);
            if (intersections is IReadOnlyCollection<GridIntersection> readOnlyCollection)
                ObserveKnownCount(readOnlyCollection.Count, ref hasKnownCount, ref firstKnownCount, ref maximumKnownCount, ref conflictingKnownCounts);
            if (intersections is ICollection nonGenericCollection)
                ObserveKnownCount(nonGenericCollection.Count, ref hasKnownCount, ref firstKnownCount, ref maximumKnownCount, ref conflictingKnownCounts);

            if (maximumKnownCount > MaxMarkers)
                return maximumKnownCount;
            if (conflictingKnownCounts)
                throw new InvalidOperationException("Grid intersection marker source reports conflicting known counts.");
            return hasKnownCount ? firstKnownCount : (int?)null;
        }

        private static void RequireStableKnownInputCount(
            IEnumerable<GridIntersection> intersections,
            int? knownCount)
        {
            if (!knownCount.HasValue) return;
            var currentCount = GetKnownInputCount(intersections);
            if (!currentCount.HasValue || currentCount.Value != knownCount.Value)
                throw new InvalidOperationException("Grid intersection marker source known count changed during traversal.");
        }

        private static void ObserveKnownCount(
            int candidate,
            ref bool hasKnownCount,
            ref int firstKnownCount,
            ref int maximumKnownCount,
            ref bool conflictingKnownCounts)
        {
            if (candidate < 0)
                throw new InvalidOperationException("Grid intersection marker source reports an invalid negative known count.");

            if (!hasKnownCount)
            {
                hasKnownCount = true;
                firstKnownCount = candidate;
                maximumKnownCount = candidate;
                return;
            }

            if (candidate != firstKnownCount)
                conflictingKnownCounts = true;
            if (candidate > maximumKnownCount)
                maximumKnownCount = candidate;
        }

        private static void ThrowTooManyMarkers()
        {
            throw new InvalidOperationException("Grid intersection marker plan exceeds the supported " + MaxMarkers + " marker limit.");
        }
    }
}
