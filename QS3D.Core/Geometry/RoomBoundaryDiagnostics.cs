using System;
using System.Collections.Generic;
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

            var segments = source.Take(MaxInputSegments + 1).ToList();
            if (segments.Count > MaxInputSegments)
                throw new InvalidOperationException("Room boundary input exceeds the supported segment limit.");
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

        private static string Fingerprint(IEnumerable<string> values)
        {
            var normalized = values
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal);
            var payload = string.Join("\n", normalized);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
