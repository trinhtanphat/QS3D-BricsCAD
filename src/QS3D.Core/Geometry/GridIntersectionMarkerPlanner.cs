using System;
using System.Collections.Generic;
using System.Linq;

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

            var source = intersections.Take(MaxMarkers + 1).ToList();
            if (source.Count > MaxMarkers)
                throw new InvalidOperationException("Grid intersection marker plan exceeds the supported " + MaxMarkers + " marker limit.");

            var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            var owners = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<GridIntersectionMarkerPlan>(source.Count);
            foreach (var intersection in source)
            {
                if (intersection == null)
                    throw new InvalidOperationException("Grid intersection marker input contains a null intersection.");

                var canonical = GridIntersectionIdentityPlanner.CanonicalizePair(
                    intersection.FirstElementId,
                    intersection.SecondElementId);
                var pairToken = GridIntersectionIdentityPlanner.BuildPairToken(canonical.FirstGridId, canonical.SecondGridId);
                occurrences.TryGetValue(pairToken, out var occurrence);
                var ownerToken = GridIntersectionIdentityPlanner.BuildIntersectionOwner(
                    canonical.FirstGridId,
                    canonical.SecondGridId,
                    occurrence);
                if (!owners.Add(ownerToken))
                    throw new InvalidOperationException("Grid intersection marker plan produced duplicate owner token: " + ownerToken + ".");
                occurrences[pairToken] = checked(occurrence + 1);

                result.Add(new GridIntersectionMarkerPlan(
                    canonical.FirstGridId,
                    canonical.SecondGridId,
                    pairToken,
                    ownerToken,
                    occurrence,
                    intersection.Point));
            }

            return result.AsReadOnly();
        }
    }
}
