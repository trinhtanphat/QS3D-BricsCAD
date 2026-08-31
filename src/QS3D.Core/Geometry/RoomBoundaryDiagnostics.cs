using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.Geometry
{
    public enum RoomBoundaryDiagnosticReason
    {
        Ready,
        NoInput,
        InsufficientSegments,
        NoClosedFace,
        BelowMinimumArea
    }

    public sealed class RoomBoundaryDiagnosticFace
    {
        internal RoomBoundaryDiagnosticFace(string faceFingerprint, string sourceFingerprint, double area, double perimeter, int sourceCount, bool accepted)
        {
            FaceFingerprint = faceFingerprint;
            SourceFingerprint = sourceFingerprint;
            Area = area;
            Perimeter = perimeter;
            SourceCount = sourceCount;
            Accepted = accepted;
        }

        public string FaceFingerprint { get; }
        public string SourceFingerprint { get; }
        public double Area { get; }
        public double Perimeter { get; }
        public int SourceCount { get; }
        public bool Accepted { get; }
    }

    public sealed class RoomBoundaryDiagnosticReport
    {
        internal RoomBoundaryDiagnosticReport(
            RoomBoundaryDiagnosticReason reason,
            int inputSegmentCount,
            int uniqueSourceCount,
            double minimumArea,
            int acceptedBoundaryCount,
            IReadOnlyList<RoomBoundaryDiagnosticFace> faces)
        {
            Reason = reason;
            InputSegmentCount = inputSegmentCount;
            UniqueSourceCount = uniqueSourceCount;
            MinimumArea = minimumArea;
            Faces = faces;
            CandidateBoundaryCount = faces.Count;
            AcceptedBoundaryCount = acceptedBoundaryCount;
            RejectedByMinimumAreaCount = faces.Count(x => !x.Accepted);
            MaxCandidateArea = faces.Count == 0 ? 0d : faces.Max(x => x.Area);
        }

        public RoomBoundaryDiagnosticReason Reason { get; }
        public int InputSegmentCount { get; }
        public int UniqueSourceCount { get; }
        public double MinimumArea { get; }
        public int CandidateBoundaryCount { get; }
        public int AcceptedBoundaryCount { get; }
        public int RejectedByMinimumAreaCount { get; }
        public double MaxCandidateArea { get; }
        public IReadOnlyList<RoomBoundaryDiagnosticFace> Faces { get; }
        public bool HasAcceptedBoundaries => AcceptedBoundaryCount > 0;
    }

    public sealed class RoomBoundaryDiagnosticAnalysis
    {
        internal RoomBoundaryDiagnosticAnalysis(RoomBoundaryDiagnosticReport report, IReadOnlyList<RoomBoundary> acceptedBoundaries)
        {
            Report = report ?? throw new ArgumentNullException(nameof(report));
            AcceptedBoundaries = acceptedBoundaries ?? throw new ArgumentNullException(nameof(acceptedBoundaries));
        }

        public RoomBoundaryDiagnosticReport Report { get; }

        // Authoring handoff only. The privacy-safe presentation contract is Report/Faces;
        // these canonical RoomBoundary objects intentionally retain source provenance so
        // the existing Room lifecycle can continue without a second topology discovery.
        public IReadOnlyList<RoomBoundary> AcceptedBoundaries { get; }

        public static implicit operator RoomBoundaryDiagnosticReport(RoomBoundaryDiagnosticAnalysis analysis)
        {
            if (analysis == null) throw new ArgumentNullException(nameof(analysis));
            return analysis.Report;
        }
    }

    /// <summary>
    /// Read-only presentation/authoring adapter over RoomBoundaryEngine. It does not
    /// maintain a second topology engine: discovery runs once at zero minimum area so
    /// the same candidate set can explain topology failure and feed accepted faces to
    /// the existing authoring workflow. Raw source IDs are omitted from Report/Faces.
    /// </summary>
    public sealed class RoomBoundaryDiagnosticService
    {
        private const int MaxInputSegments = 5000;

        public RoomBoundaryDiagnosticAnalysis Analyze(
            IEnumerable<BoundarySegment> source,
            double tolerance = 0.001d,
            double minimumArea = 0.01d)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (double.IsNaN(minimumArea) || double.IsInfinity(minimumArea) || minimumArea < 0d)
                throw new ArgumentOutOfRangeException(nameof(minimumArea));
            if (double.IsNaN(tolerance) || double.IsInfinity(tolerance) || tolerance <= 0d)
                throw new ArgumentOutOfRangeException(nameof(tolerance));

            var knownCount = GetKnownInputCount(source);
            if (knownCount.HasValue && knownCount.Value > MaxInputSegments)
                ThrowTooManySegments();

            var segments = MaterializeBoundedSegments(source, knownCount);
            ValidateSourceProvenance(segments);

            var candidates = new RoomBoundaryEngine().Discover(segments, tolerance, 0d)
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .ToList();
            var accepted = candidates.Where(x => x.Area > minimumArea).ToList();
            var faces = candidates.Select(x => new RoomBoundaryDiagnosticFace(
                    Fingerprint(new[] { x.Key, Fingerprint(x.SourceIds) }),
                    Fingerprint(x.SourceIds),
                    x.Area,
                    x.Perimeter,
                    x.SourceIds.Count,
                    x.Area > minimumArea))
                .ToList();

            RoomBoundaryDiagnosticReason reason;
            if (segments.Count == 0) reason = RoomBoundaryDiagnosticReason.NoInput;
            else if (segments.Count < 3) reason = RoomBoundaryDiagnosticReason.InsufficientSegments;
            else if (candidates.Count == 0) reason = RoomBoundaryDiagnosticReason.NoClosedFace;
            else if (accepted.Count == 0) reason = RoomBoundaryDiagnosticReason.BelowMinimumArea;
            else reason = RoomBoundaryDiagnosticReason.Ready;

            var uniqueSourceCount = segments
                .Select(x => x.SourceId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var report = new RoomBoundaryDiagnosticReport(
                reason,
                segments.Count,
                uniqueSourceCount,
                minimumArea,
                accepted.Count,
                faces.AsReadOnly());
            return new RoomBoundaryDiagnosticAnalysis(report, accepted.AsReadOnly());
        }

        private static List<BoundarySegment> MaterializeBoundedSegments(
            IEnumerable<BoundarySegment> source,
            int? knownCount)
        {
            var segments = new List<BoundarySegment>();
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
                        throw new InvalidOperationException("Room boundary diagnostic source known count does not match traversal.");
                    if (observedCount >= MaxInputSegments)
                        ThrowTooManySegments();

                    var segment = enumerator.Current;
                    segments.Add(segment);
                    observedCount++;
                }
            }

            RequireStableKnownInputCount(source, knownCount);
            if (knownCount.HasValue && observedCount != knownCount.Value)
                throw new InvalidOperationException("Room boundary diagnostic source known count does not match traversal.");
            return segments;
        }

        private static void RequireStableKnownInputCount(
            IEnumerable<BoundarySegment> source,
            int? knownCount)
        {
            if (!knownCount.HasValue) return;
            var currentCount = GetKnownInputCount(source);
            if (!currentCount.HasValue || currentCount.Value != knownCount.Value)
                throw new InvalidOperationException("Room boundary diagnostic source known count changed during traversal.");
        }

        private static int? GetKnownInputCount(IEnumerable<BoundarySegment> source)
        {
            var hasKnownCount = false;
            var firstKnownCount = 0;
            var maximumKnownCount = 0;
            var conflictingKnownCounts = false;

            if (source is ICollection<BoundarySegment> collection)
                ObserveKnownCount(collection.Count, ref hasKnownCount, ref firstKnownCount, ref maximumKnownCount, ref conflictingKnownCounts);
            if (source is IReadOnlyCollection<BoundarySegment> readOnlyCollection)
                ObserveKnownCount(readOnlyCollection.Count, ref hasKnownCount, ref firstKnownCount, ref maximumKnownCount, ref conflictingKnownCounts);
            if (source is ICollection nonGenericCollection)
                ObserveKnownCount(nonGenericCollection.Count, ref hasKnownCount, ref firstKnownCount, ref maximumKnownCount, ref conflictingKnownCounts);

            if (maximumKnownCount > MaxInputSegments)
                return maximumKnownCount;
            if (conflictingKnownCounts)
                throw new InvalidOperationException("Room boundary diagnostic source reports conflicting known counts.");
            return hasKnownCount ? firstKnownCount : (int?)null;
        }

        private static void ObserveKnownCount(
            int candidate,
            ref bool hasKnownCount,
            ref int firstKnownCount,
            ref int maximumKnownCount,
            ref bool conflictingKnownCounts)
        {
            if (candidate < 0)
                throw new InvalidOperationException("Room boundary diagnostic source reports an invalid negative known count.");

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

        private static void ValidateSourceProvenance(IEnumerable<BoundarySegment> segments)
        {
            foreach (var segment in segments)
            {
                if (segment == null) continue;
                var value = segment.SourceId ?? string.Empty;
                for (var index = 0; index < value.Length; index++)
                {
                    if (char.IsHighSurrogate(value[index]))
                    {
                        if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                            throw new ArgumentException("Room boundary diagnostic source provenance must not contain malformed UTF-16.", nameof(segments));
                        index++;
                        continue;
                    }

                    if (char.IsLowSurrogate(value[index]))
                        throw new ArgumentException("Room boundary diagnostic source provenance must not contain malformed UTF-16.", nameof(segments));
                }
            }
        }

        private static void ThrowTooManySegments()
        {
            throw new InvalidOperationException("Room boundary input exceeds the supported segment limit.");
        }

        private static string Fingerprint(IEnumerable<string> values)
        {
            var normalized = values
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal);
            var payload = new StringBuilder();
            foreach (var value in normalized)
            {
                payload.Append(value.Length.ToString(CultureInfo.InvariantCulture));
                payload.Append(':');
                payload.Append(value);
            }
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload.ToString()));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
