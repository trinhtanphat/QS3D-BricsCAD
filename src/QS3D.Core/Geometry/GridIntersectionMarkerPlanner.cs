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
    }
}
