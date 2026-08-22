using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.Geometry
{
    public sealed class GridIntersectionIdentity
    {
        internal GridIntersectionIdentity(
            string firstElementId,
            string secondElementId,
            Point2 point,
            int occurrenceIndex,
            string pairKey,
            string pairToken,
            string ownerToken)
        {
            FirstElementId = firstElementId;
            SecondElementId = secondElementId;
            Point = point;
            OccurrenceIndex = occurrenceIndex;
            PairKey = pairKey;
            PairToken = pairToken;
            OwnerToken = ownerToken;
        }

        public string FirstElementId { get; }
        public string SecondElementId { get; }
        public Point2 Point { get; }
        public int OccurrenceIndex { get; }
        public string PairKey { get; }
        public string PairToken { get; }
        public string OwnerToken { get; }
    }

    public static class GridIntersectionIdentityPlanner
    {
        private const int MaxIntersections = 100000;
        private const int MaxElementIdLength = 128;
        private const int MaxIntersectionsPerPair = 2;
        private const string PairTokenPrefix = "GIP1:";
        private const string OwnerTokenPrefix = "GIX1:";

        public static IReadOnlyList<GridIntersectionIdentity> Assign(
            IEnumerable<GridIntersection> intersections,
            double pointTolerance = 1e-8)
        {
            if (intersections == null) throw new ArgumentNullException(nameof(intersections));
            if (!Finite(pointTolerance) || pointTolerance <= 0d)
                throw new ArgumentOutOfRangeException(nameof(pointTolerance), "Grid intersection identity tolerance must be finite and > 0.");

            var input = intersections.Take(MaxIntersections + 1).ToList();
            if (input.Count > MaxIntersections)
                throw new InvalidOperationException("Grid intersection identity supports at most " + MaxIntersections + " intersections.");
            if (input.Count == 0) return Array.Empty<GridIntersectionIdentity>();

            var groups = new Dictionary<string, PairGroup>(StringComparer.Ordinal);
            var pairTokens = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var intersection in input)
            {
                if (intersection == null) throw new InvalidOperationException("Grid intersection identity input contains null.");
                var first = CanonicalElementId(intersection.FirstElementId, "first Grid element id");
                var second = CanonicalElementId(intersection.SecondElementId, "second Grid element id");
                if (string.Equals(first, second, StringComparison.Ordinal))
                    throw new InvalidOperationException("Grid intersection pair must reference two distinct semantic Grid ids: " + first + ".");
                EnsureFinitePoint(intersection.Point);

                if (string.CompareOrdinal(first, second) > 0)
                {
                    var swap = first;
                    first = second;
                    second = swap;
                }

                var pairKey = BuildPairKey(first, second);
                var pairToken = PairTokenPrefix + Sha256Hex(pairKey);
                if (pairTokens.TryGetValue(pairToken, out var existingKey) && !string.Equals(existingKey, pairKey, StringComparison.Ordinal))
                    throw new InvalidOperationException("Grid intersection pair-token hash collision detected; marker ownership is intentionally blocked.");
                pairTokens[pairToken] = pairKey;

                if (!groups.TryGetValue(pairKey, out var group))
                {
                    group = new PairGroup(first, second, pairKey, pairToken);
                    groups.Add(pairKey, group);
                }
                group.Points.Add(intersection.Point);
                if (group.Points.Count > MaxIntersectionsPerPair)
                    throw new InvalidOperationException(
                        "Grid pair " + first + " × " + second + " produced more than " + MaxIntersectionsPerPair +
                        " intersections; owner identity would be ambiguous.");
            }

            var result = new List<GridIntersectionIdentity>(input.Count);
            foreach (var group in groups.Values.OrderBy(x => x.PairKey, StringComparer.Ordinal))
            {
                group.Points.Sort(ComparePoint);
                for (var index = 1; index < group.Points.Count; index++)
                {
                    if (Near(group.Points[index - 1], group.Points[index], pointTolerance))
                        throw new InvalidOperationException(
                            "Grid pair " + group.FirstElementId + " × " + group.SecondElementId +
                            " contains duplicate/near-duplicate intersection points within tolerance; owner occurrence is ambiguous.");
                }

                for (var index = 0; index < group.Points.Count; index++)
                {
                    var ownerToken = OwnerTokenPrefix + group.PairToken.Substring(PairTokenPrefix.Length) + ":" + index;
                    result.Add(new GridIntersectionIdentity(
                        group.FirstElementId,
                        group.SecondElementId,
                        group.Points[index],
                        index,
                        group.PairKey,
                        group.PairToken,
                        ownerToken));
                }
            }

            return result.AsReadOnly();
        }

        public static string BuildPairKey(string firstElementId, string secondElementId)
        {
            var first = CanonicalElementId(firstElementId, nameof(firstElementId));
            var second = CanonicalElementId(secondElementId, nameof(secondElementId));
            if (string.Equals(first, second, StringComparison.Ordinal))
                throw new InvalidOperationException("Grid pair requires two distinct semantic Grid ids.");
            if (string.CompareOrdinal(first, second) > 0)
            {
                var swap = first;
                first = second;
                second = swap;
            }
            return first.Length + ":" + first + "|" + second.Length + ":" + second;
        }

        public static string BuildPairToken(string firstElementId, string secondElementId)
        {
            return PairTokenPrefix + Sha256Hex(BuildPairKey(firstElementId, secondElementId));
        }

        private static string CanonicalElementId(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Grid " + label + " is required.", label);
            var normalized = value.Trim();
            if (normalized.Length > MaxElementIdLength)
                throw new ArgumentException("Grid " + label + " exceeds " + MaxElementIdLength + " characters.", label);
            return normalized.ToUpperInvariant();
        }

        private static string Sha256Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var item in hash) builder.Append(item.ToString("x2"));
                return builder.ToString();
            }
        }

        private static int ComparePoint(Point2 left, Point2 right)
        {
            var x = left.X.CompareTo(right.X);
            return x != 0 ? x : left.Y.CompareTo(right.Y);
        }

        private static bool Near(Point2 first, Point2 second, double tolerance)
        {
            return Math.Abs(first.X - second.X) <= tolerance && Math.Abs(first.Y - second.Y) <= tolerance;
        }

        private static void EnsureFinitePoint(Point2 point)
        {
            if (!Finite(point.X) || !Finite(point.Y))
                throw new InvalidOperationException("Grid intersection identity point must contain finite coordinates.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private sealed class PairGroup
        {
            public PairGroup(string firstElementId, string secondElementId, string pairKey, string pairToken)
            {
                FirstElementId = firstElementId;
                SecondElementId = secondElementId;
                PairKey = pairKey;
                PairToken = pairToken;
                Points = new List<Point2>(MaxIntersectionsPerPair);
            }

            public string FirstElementId { get; }
            public string SecondElementId { get; }
            public string PairKey { get; }
            public string PairToken { get; }
            public List<Point2> Points { get; }
        }
    }
}
