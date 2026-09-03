using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace QS3D.Core.Geometry
{
    public enum OpeningHostMatchStatus
    {
        Matched,
        NoMatch,
        Ambiguous
    }

    public sealed class OpeningHostSegment
    {
        public OpeningHostSegment(string hostElementId, Point2 start, Point2 end, double thicknessM)
        {
            if (string.IsNullOrWhiteSpace(hostElementId)) throw new ArgumentException("Host element id is required.", nameof(hostElementId));
            ValidatePoint(start, nameof(start));
            ValidatePoint(end, nameof(end));
            if (!Finite(thicknessM) || thicknessM <= 0d) throw new ArgumentOutOfRangeException(nameof(thicknessM));
            var length = start.DistanceTo(end);
            if (!Finite(length) || length <= 1e-9d) throw new ArgumentException("Host segment must be non-degenerate.", nameof(end));
            HostElementId = hostElementId.Trim();
            Start = start;
            End = end;
            ThicknessM = thicknessM;
        }

        public string HostElementId { get; }
        public Point2 Start { get; }
        public Point2 End { get; }
        public double ThicknessM { get; }

        private static void ValidatePoint(Point2 point, string name)
        {
            if (!Finite(point.X) || !Finite(point.Y)) throw new ArgumentOutOfRangeException(name, "Point coordinates must be finite.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class OpeningHostMatchResult
    {
        internal OpeningHostMatchResult(
            OpeningHostMatchStatus status,
            string hostElementId,
            string secondaryHostElementId,
            double gapM,
            double secondaryGapM,
            double centerlineDistanceM,
            Point2 closestPoint,
            int candidateHostCount)
        {
            Status = status;
            HostElementId = hostElementId ?? string.Empty;
            SecondaryHostElementId = secondaryHostElementId ?? string.Empty;
            GapM = gapM;
            SecondaryGapM = secondaryGapM;
            CenterlineDistanceM = centerlineDistanceM;
            ClosestPoint = closestPoint;
            CandidateHostCount = candidateHostCount;
        }

        public OpeningHostMatchStatus Status { get; }
        public string HostElementId { get; }
        public string SecondaryHostElementId { get; }
        public double GapM { get; }
        public double SecondaryGapM { get; }
        public double CenterlineDistanceM { get; }
        public Point2 ClosestPoint { get; }
        public int CandidateHostCount { get; }
    }

    public sealed class OpeningHostMatcher
    {
        private const int MaxSegments = 20000;

        private sealed class Candidate
        {
            public string HostElementId { get; set; } = string.Empty;
            public double GapM { get; set; }
            public double CenterlineDistanceM { get; set; }
            public Point2 ClosestPoint { get; set; }
        }

        public OpeningHostMatchResult Match(
            Point2 openingCenter,
            IEnumerable<OpeningHostSegment> source,
            double maxGapM = 0.25d,
            double ambiguityToleranceM = 0.02d)
        {
            ValidatePoint(openingCenter, nameof(openingCenter));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!Finite(maxGapM) || maxGapM < 0d) throw new ArgumentOutOfRangeException(nameof(maxGapM));
            if (!Finite(ambiguityToleranceM) || ambiguityToleranceM < 0d) throw new ArgumentOutOfRangeException(nameof(ambiguityToleranceM));

            var knownCount = GetKnownInputCount(source);
            if (knownCount.HasValue && knownCount.Value > MaxSegments)
                ThrowTooManySegments();

            var segments = MaterializeBoundedSegments(source, knownCount);
            if (segments.Any(x => x == null)) throw new ArgumentException("Host segment collection contains null.", nameof(source));

            var bestByHost = new Dictionary<string, Candidate>(StringComparer.OrdinalIgnoreCase);
            foreach (var segment in segments)
            {
                var closest = ClosestPointOnSegment(openingCenter, segment.Start, segment.End);
                var centerlineDistance = openingCenter.DistanceTo(closest);
                var gap = Math.Max(0d, centerlineDistance - segment.ThicknessM / 2d);
                if (!Finite(gap)) throw new OverflowException("Opening host gap calculation overflowed.");
                if (gap > maxGapM) continue;

                var candidate = new Candidate
                {
                    HostElementId = segment.HostElementId,
                    GapM = gap,
                    CenterlineDistanceM = centerlineDistance,
                    ClosestPoint = closest
                };
                if (!bestByHost.TryGetValue(candidate.HostElementId, out var existing) || Compare(candidate, existing) < 0)
                    bestByHost[candidate.HostElementId] = candidate;
            }

            var ordered = bestByHost.Values
                .OrderBy(x => x.GapM)
                .ThenBy(x => x.CenterlineDistanceM)
                .ThenBy(x => x.HostElementId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ordered.Count == 0)
                return new OpeningHostMatchResult(OpeningHostMatchStatus.NoMatch, string.Empty, string.Empty, double.NaN, double.NaN, double.NaN, openingCenter, 0);

            var first = ordered[0];
            if (ordered.Count > 1)
            {
                var second = ordered[1];
                var delta = second.GapM - first.GapM;
                if (!Finite(delta)) throw new OverflowException("Opening host ambiguity calculation overflowed.");
                if (delta <= ambiguityToleranceM)
                    return new OpeningHostMatchResult(OpeningHostMatchStatus.Ambiguous, first.HostElementId, second.HostElementId, first.GapM, second.GapM, first.CenterlineDistanceM, first.ClosestPoint, ordered.Count);
            }

            return new OpeningHostMatchResult(OpeningHostMatchStatus.Matched, first.HostElementId, string.Empty, first.GapM, double.NaN, first.CenterlineDistanceM, first.ClosestPoint, ordered.Count);
        }

        private static List<OpeningHostSegment> MaterializeBoundedSegments(
            IEnumerable<OpeningHostSegment> source,
            int? knownCount)
        {
            var segments = new List<OpeningHostSegment>();
            var observedCount = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownInputCount(source, knownCount);
                    if (!enumerator.MoveNext())
                    {
                        RequireStableKnownInputCount(source, knownCount);
                        break;
                    }
                    RequireStableKnownInputCount(source, knownCount);

                    if (knownCount.HasValue && observedCount >= knownCount.Value)
                        throw new InvalidOperationException("Opening host source known count does not match traversal.");
                    if (observedCount >= MaxSegments)
                        ThrowTooManySegments();

                    var segment = enumerator.Current;
                    RequireStableKnownInputCount(source, knownCount);
                    segments.Add(segment);
                    observedCount++;
                }
            }

            RequireStableKnownInputCount(source, knownCount);
            if (knownCount.HasValue && observedCount != knownCount.Value)
                throw new InvalidOperationException("Opening host source known count does not match traversal.");
            return segments;
        }

        private static int? GetKnownInputCount(IEnumerable<OpeningHostSegment> source)
        {
            var hasKnownCount = false;
            var firstKnownCount = 0;
            var maximumKnownCount = 0;
            var conflictingKnownCounts = false;

            if (source is ICollection<OpeningHostSegment> collection)
                ObserveKnownCount(collection.Count, ref hasKnownCount, ref firstKnownCount, ref maximumKnownCount, ref conflictingKnownCounts);
            if (source is IReadOnlyCollection<OpeningHostSegment> readOnlyCollection)
                ObserveKnownCount(readOnlyCollection.Count, ref hasKnownCount, ref firstKnownCount, ref maximumKnownCount, ref conflictingKnownCounts);
            if (source is ICollection nonGenericCollection)
                ObserveKnownCount(nonGenericCollection.Count, ref hasKnownCount, ref firstKnownCount, ref maximumKnownCount, ref conflictingKnownCounts);

            if (maximumKnownCount > MaxSegments)
                return maximumKnownCount;
            if (conflictingKnownCounts)
                throw new InvalidOperationException("Opening host source reports conflicting known counts.");
            return hasKnownCount ? firstKnownCount : (int?)null;
        }

        private static void RequireStableKnownInputCount(
            IEnumerable<OpeningHostSegment> source,
            int? knownCount)
        {
            if (!knownCount.HasValue) return;
            var currentCount = GetKnownInputCount(source);
            if (!currentCount.HasValue || currentCount.Value != knownCount.Value)
                throw new InvalidOperationException("Opening host source known count changed during traversal.");
        }

        private static void ObserveKnownCount(
            int candidate,
            ref bool hasKnownCount,
            ref int firstKnownCount,
            ref int maximumKnownCount,
            ref bool conflictingKnownCounts)
        {
            if (candidate < 0)
                throw new InvalidOperationException("Opening host source reports an invalid negative known count.");

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

        private static void ThrowTooManySegments()
        {
            throw new InvalidOperationException("Opening host matching supports at most " + MaxSegments.ToString(CultureInfo.InvariantCulture) + " wall segments per opening.");
        }

        private static int Compare(Candidate left, Candidate right)
        {
            var gap = left.GapM.CompareTo(right.GapM);
            if (gap != 0) return gap;
            var distance = left.CenterlineDistanceM.CompareTo(right.CenterlineDistanceM);
            if (distance != 0) return distance;
            return StringComparer.OrdinalIgnoreCase.Compare(left.HostElementId, right.HostElementId);
        }

        private static Point2 ClosestPointOnSegment(Point2 point, Point2 start, Point2 end)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            if (!Finite(dx) || !Finite(dy)) throw new OverflowException("Host segment delta exceeds the supported numeric range.");
            var length = start.DistanceTo(end);
            if (!(length > 0d) || !Finite(length)) throw new InvalidOperationException("Host segment is degenerate.");
            var ux = dx / length;
            var uy = dy / length;
            if (!Finite(ux) || !Finite(uy)) throw new OverflowException("Host segment direction is not finite.");

            var qx = point.X - start.X;
            var qy = point.Y - start.Y;
            if (!Finite(qx) || !Finite(qy)) throw new OverflowException("Opening-to-host delta exceeds the supported numeric range.");
            var scale = Math.Max(Math.Abs(qx), Math.Abs(qy));
            if (!Finite(scale)) throw new OverflowException("Opening host projection scale overflowed.");
            if (scale == 0d) return start;

            var scaledAlong = qx / scale * ux + qy / scale * uy;
            if (!Finite(scaledAlong)) throw new OverflowException("Opening host scaled projection overflowed.");
            if (scaledAlong <= 0d) return start;

            var scaledLength = length / scale;
            if (scaledAlong >= scaledLength) return end;

            var along = scaledAlong * scale;
            if (!Finite(along)) throw new OverflowException("Opening host projection overflowed.");
            var x = start.X + ux * along;
            var y = start.Y + uy * along;
            if (!Finite(x) || !Finite(y)) throw new OverflowException("Opening host closest-point calculation overflowed.");
            return new Point2(x, y);
        }

        private static void ValidatePoint(Point2 point, string name)
        {
            if (!Finite(point.X) || !Finite(point.Y)) throw new ArgumentOutOfRangeException(name, "Point coordinates must be finite.");
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
