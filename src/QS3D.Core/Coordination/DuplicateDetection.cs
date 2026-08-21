using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Coordination
{
    [Flags]
    public enum DuplicateMatchKind
    {
        None = 0,
        ExactGeometry = 1,
        NearGeometry = 2,
        SemanticIdentity = 4
    }

    public sealed class DuplicateDetectionOptions
    {
        public double CoordinateToleranceM { get; set; } = 0.001d;
        public bool RequireSameDisciplineForGeometry { get; set; } = true;
        public bool RequireSameCategoryForGeometry { get; set; } = true;
        public bool EnableSemanticIdentity { get; set; } = true;
    }

    public sealed class DuplicateCandidate
    {
        public DuplicateCandidate(CoordinationElement element, string sourceId = null)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
            SourceId = NormalizeOptionalIdentity(sourceId, nameof(sourceId));
        }

        public CoordinationElement Element { get; }
        public string SourceId { get; }

        private static string NormalizeOptionalIdentity(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var normalized = value.Trim();
            for (var index = 0; index < normalized.Length; index++)
            {
                if (char.IsControl(normalized[index]))
                    throw new ArgumentException("Duplicate source identity must not contain control characters.", parameterName);
            }
            return normalized;
        }
    }

    public sealed class DuplicatePair
    {
        internal DuplicatePair(
            DuplicateCandidate left,
            DuplicateCandidate right,
            DuplicateMatchKind matchKinds)
        {
            Left = left;
            Right = right;
            MatchKinds = matchKinds;
        }

        public DuplicateCandidate Left { get; }
        public DuplicateCandidate Right { get; }
        public DuplicateMatchKind MatchKinds { get; }
        public string LeftElementId => Left.Element.ElementId;
        public string RightElementId => Right.Element.ElementId;
        public string PairKey => LeftElementId + "|" + RightElementId;
        public bool IsExactGeometry => (MatchKinds & DuplicateMatchKind.ExactGeometry) != 0;
        public bool IsNearGeometry => (MatchKinds & DuplicateMatchKind.NearGeometry) != 0;
        public bool IsSemanticIdentity => (MatchKinds & DuplicateMatchKind.SemanticIdentity) != 0;
    }

    public sealed class DuplicateSummary
    {
        internal DuplicateSummary(int pairCount, int exactGeometryCount, int nearGeometryCount, int semanticIdentityCount)
        {
            PairCount = pairCount;
            ExactGeometryCount = exactGeometryCount;
            NearGeometryCount = nearGeometryCount;
            SemanticIdentityCount = semanticIdentityCount;
        }

        public int PairCount { get; }
        public int ExactGeometryCount { get; }
        public int NearGeometryCount { get; }
        public int SemanticIdentityCount { get; }
    }

    public sealed class DuplicateDetectionResult
    {
        internal DuplicateDetectionResult(IReadOnlyList<DuplicatePair> pairs, DuplicateSummary summary)
        {
            Pairs = pairs;
            Summary = summary;
        }

        public IReadOnlyList<DuplicatePair> Pairs { get; }
        public DuplicateSummary Summary { get; }
    }

    public sealed class DuplicateDetectionService
    {
        private const int MaximumElements = 500;
        private const int MaximumResults = 10000;

        public DuplicateDetectionResult Detect(
            IEnumerable<CoordinationElement> elements,
            DuplicateDetectionOptions options = null)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            return Detect(ProjectCandidates(elements), options);
        }

        public DuplicateDetectionResult Detect(
            IEnumerable<DuplicateCandidate> candidates,
            DuplicateDetectionOptions options = null)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            var effective = options ?? new DuplicateDetectionOptions();
            ValidateOptions(effective);

            var expectedCount = RequireKnownCountWithinLimit(candidates);
            var snapshot = new List<DuplicateCandidate>();
            var elementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var candidate in candidates)
            {
                if (index == MaximumElements) throw TooManyElements();
                if (candidate == null)
                    throw new ArgumentException("Duplicate-detection input contains a null candidate at index " + index + ".", nameof(candidates));
                if (!elementIds.Add(candidate.Element.ElementId))
                    throw new ArgumentException("Duplicate coordination element id: " + candidate.Element.ElementId + ".", nameof(candidates));
                snapshot.Add(candidate);
                index++;
            }

            if (expectedCount.HasValue && snapshot.Count != expectedCount.Value)
                throw new InvalidOperationException("Duplicate-detection input enumeration count did not match its known element count.");

            snapshot.Sort(CompareCandidates);
            var pairs = new List<DuplicatePair>();
            var exactCount = 0;
            var nearCount = 0;
            var semanticCount = 0;

            for (var i = 0; i < snapshot.Count; i++)
            {
                for (var j = i + 1; j < snapshot.Count; j++)
                {
                    var kinds = Evaluate(snapshot[i], snapshot[j], effective);
                    if (kinds == DuplicateMatchKind.None) continue;
                    if (pairs.Count == MaximumResults)
                        throw new InvalidOperationException("Duplicate detection supports at most " + MaximumResults + " results per operation.");

                    pairs.Add(new DuplicatePair(snapshot[i], snapshot[j], kinds));
                    if ((kinds & DuplicateMatchKind.ExactGeometry) != 0) exactCount++;
                    if ((kinds & DuplicateMatchKind.NearGeometry) != 0) nearCount++;
                    if ((kinds & DuplicateMatchKind.SemanticIdentity) != 0) semanticCount++;
                }
            }

            var frozenPairs = new ReadOnlyCollection<DuplicatePair>(pairs.ToArray());
            return new DuplicateDetectionResult(
                frozenPairs,
                new DuplicateSummary(frozenPairs.Count, exactCount, nearCount, semanticCount));
        }

        private static IEnumerable<DuplicateCandidate> ProjectCandidates(IEnumerable<CoordinationElement> elements)
        {
            var index = 0;
            foreach (var element in elements)
            {
                if (element == null)
                    throw new ArgumentException("Duplicate-detection input contains a null element at index " + index + ".", nameof(elements));
                yield return new DuplicateCandidate(element);
                index++;
            }
        }

        private static DuplicateMatchKind Evaluate(
            DuplicateCandidate left,
            DuplicateCandidate right,
            DuplicateDetectionOptions options)
        {
            var kinds = DuplicateMatchKind.None;

            if (options.EnableSemanticIdentity &&
                left.SourceId.Length > 0 &&
                StringComparer.OrdinalIgnoreCase.Equals(left.SourceId, right.SourceId))
            {
                kinds |= DuplicateMatchKind.SemanticIdentity;
            }

            if (!GeometryClassificationMatches(left.Element, right.Element, options))
                return kinds;

            if (BoundsEqual(left.Element.Bounds, right.Element.Bounds))
                return kinds | DuplicateMatchKind.ExactGeometry;

            if (BoundsWithinTolerance(left.Element.Bounds, right.Element.Bounds, options.CoordinateToleranceM))
                kinds |= DuplicateMatchKind.NearGeometry;

            return kinds;
        }

        private static bool GeometryClassificationMatches(
            CoordinationElement left,
            CoordinationElement right,
            DuplicateDetectionOptions options)
        {
            if (options.RequireSameDisciplineForGeometry &&
                !StringComparer.OrdinalIgnoreCase.Equals(left.Discipline, right.Discipline))
                return false;
            if (options.RequireSameCategoryForGeometry &&
                !StringComparer.OrdinalIgnoreCase.Equals(left.Category, right.Category))
                return false;
            return true;
        }

        private static bool BoundsEqual(AxisAlignedBox left, AxisAlignedBox right)
        {
            return left.MinX == right.MinX && left.MinY == right.MinY && left.MinZ == right.MinZ &&
                   left.MaxX == right.MaxX && left.MaxY == right.MaxY && left.MaxZ == right.MaxZ;
        }

        private static bool BoundsWithinTolerance(AxisAlignedBox left, AxisAlignedBox right, double tolerance)
        {
            return WithinTolerance(left.MinX, right.MinX, tolerance) &&
                   WithinTolerance(left.MinY, right.MinY, tolerance) &&
                   WithinTolerance(left.MinZ, right.MinZ, tolerance) &&
                   WithinTolerance(left.MaxX, right.MaxX, tolerance) &&
                   WithinTolerance(left.MaxY, right.MaxY, tolerance) &&
                   WithinTolerance(left.MaxZ, right.MaxZ, tolerance);
        }

        private static bool WithinTolerance(double left, double right, double tolerance)
        {
            if (left == right) return true;
            var delta = left - right;
            if (double.IsInfinity(delta)) return false;
            return Math.Abs(delta) <= tolerance;
        }

        private static int CompareCandidates(DuplicateCandidate left, DuplicateCandidate right)
        {
            var comparison = StringComparer.OrdinalIgnoreCase.Compare(left.Element.ElementId, right.Element.ElementId);
            if (comparison != 0) return comparison;
            return StringComparer.Ordinal.Compare(left.Element.ElementId, right.Element.ElementId);
        }

        private static void ValidateOptions(DuplicateDetectionOptions options)
        {
            if (double.IsNaN(options.CoordinateToleranceM) ||
                double.IsInfinity(options.CoordinateToleranceM) ||
                options.CoordinateToleranceM < 0d)
                throw new ArgumentOutOfRangeException(nameof(options.CoordinateToleranceM));
        }

        private static int? RequireKnownCountWithinLimit(IEnumerable<DuplicateCandidate> candidates)
        {
            int? genericCount = null;
            int? readOnlyCount = null;
            int? nonGenericCount = null;

            if (candidates is ICollection<DuplicateCandidate> collection) genericCount = collection.Count;
            if (candidates is IReadOnlyCollection<DuplicateCandidate> readOnlyCollection) readOnlyCount = readOnlyCollection.Count;
            if (candidates is ICollection nonGenericCollection) nonGenericCount = nonGenericCollection.Count;

            if ((genericCount.HasValue && genericCount.Value > MaximumElements) ||
                (readOnlyCount.HasValue && readOnlyCount.Value > MaximumElements) ||
                (nonGenericCount.HasValue && nonGenericCount.Value > MaximumElements))
                throw TooManyElements();

            if ((genericCount.HasValue && genericCount.Value < 0) ||
                (readOnlyCount.HasValue && readOnlyCount.Value < 0) ||
                (nonGenericCount.HasValue && nonGenericCount.Value < 0))
                throw new InvalidOperationException("Duplicate-detection input reported an invalid negative element count.");

            int? expected = null;
            RequireConsistentKnownCount(genericCount, ref expected);
            RequireConsistentKnownCount(readOnlyCount, ref expected);
            RequireConsistentKnownCount(nonGenericCount, ref expected);
            return expected;
        }

        private static void RequireConsistentKnownCount(int? candidate, ref int? expected)
        {
            if (!candidate.HasValue) return;
            if (!expected.HasValue)
            {
                expected = candidate;
                return;
            }
            if (expected.Value != candidate.Value)
                throw new InvalidOperationException("Duplicate-detection input reported conflicting known element counts.");
        }

        private static InvalidOperationException TooManyElements()
        {
            return new InvalidOperationException("Duplicate detection supports at most " + MaximumElements + " elements per operation.");
        }
    }
}
