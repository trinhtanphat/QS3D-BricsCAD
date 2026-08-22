using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.Geometry
{
    public sealed class WallJunctionOwnerContext
    {
        public WallJunctionOwnerContext(
            string sourceSegmentId,
            string wallElementId,
            string projectId,
            string drawingFingerprint,
            double bottomM,
            double topM,
            double thicknessM)
        {
            SourceSegmentId = Required(sourceSegmentId, nameof(sourceSegmentId));
            WallElementId = Required(wallElementId, nameof(wallElementId));
            ProjectId = Required(projectId, nameof(projectId));
            DrawingFingerprint = Required(drawingFingerprint, nameof(drawingFingerprint));
            if (!Finite(bottomM) || !Finite(topM) || topM <= bottomM)
                throw new ArgumentOutOfRangeException(nameof(topM), "Wall owner vertical range must be finite and TopM > BottomM.");
            if (!Finite(thicknessM) || thicknessM <= 0d)
                throw new ArgumentOutOfRangeException(nameof(thicknessM), "Wall owner thickness must be finite and > 0.");
            BottomM = bottomM;
            TopM = topM;
            ThicknessM = thicknessM;
        }

        public string SourceSegmentId { get; }
        public string WallElementId { get; }
        public string ProjectId { get; }
        public string DrawingFingerprint { get; }
        public double BottomM { get; }
        public double TopM { get; }
        public double ThicknessM { get; }

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name);
            return value.Trim();
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class WallJunctionOwnershipPlan
    {
        internal WallJunctionOwnershipPlan(
            string projectId,
            string drawingFingerprint,
            WallJunctionKind junctionKind,
            Point2 junctionPoint,
            int occurrenceIndex,
            string groupKey,
            string groupToken,
            string ownerToken,
            string inputFingerprint,
            IReadOnlyList<string> ownerWallIds,
            IReadOnlyList<string> sourceSegmentIds,
            double bottomM,
            double topM,
            double minThicknessM,
            double maxThicknessM)
        {
            ProjectId = projectId;
            DrawingFingerprint = drawingFingerprint;
            JunctionKind = junctionKind;
            JunctionPoint = junctionPoint;
            OccurrenceIndex = occurrenceIndex;
            GroupKey = groupKey;
            GroupToken = groupToken;
            OwnerToken = ownerToken;
            InputFingerprint = inputFingerprint;
            OwnerWallIds = ownerWallIds;
            SourceSegmentIds = sourceSegmentIds;
            BottomM = bottomM;
            TopM = topM;
            MinThicknessM = minThicknessM;
            MaxThicknessM = maxThicknessM;
        }

        public string ProjectId { get; }
        public string DrawingFingerprint { get; }
        public WallJunctionKind JunctionKind { get; }
        public Point2 JunctionPoint { get; }
        public int OccurrenceIndex { get; }
        public string GroupKey { get; }
        public string GroupToken { get; }
        public string OwnerToken { get; }
        public string InputFingerprint { get; }
        public IReadOnlyList<string> OwnerWallIds { get; }
        public IReadOnlyList<string> SourceSegmentIds { get; }
        public double BottomM { get; }
        public double TopM { get; }
        public double MinThicknessM { get; }
        public double MaxThicknessM { get; }
    }

    public static class WallJunctionOwnershipPlanner
    {
        private const int MaxJunctions = 10000;
        private const int MaxOwnerMappings = 20000;
        private const int MaxIdentityLength = 256;
        private const string GroupTokenPrefix = "WJP1:";
        private const string OwnerTokenPrefix = "WJX1:";
        private const string FingerprintPrefix = "WJF1:";

        private sealed class NormalizedOwner
        {
            public string SourceSegmentId { get; set; } = string.Empty;
            public string WallElementId { get; set; } = string.Empty;
            public string ProjectId { get; set; } = string.Empty;
            public string DrawingFingerprint { get; set; } = string.Empty;
            public double BottomM { get; set; }
            public double TopM { get; set; }
            public double ThicknessM { get; set; }
        }

        private sealed class Candidate
        {
            public WallJunction Junction { get; set; } = null!;
            public string ProjectId { get; set; } = string.Empty;
            public string DrawingFingerprint { get; set; } = string.Empty;
            public string GroupKey { get; set; } = string.Empty;
            public List<NormalizedOwner> SourceOwners { get; set; } = new List<NormalizedOwner>();
            public List<string> OwnerWallIds { get; set; } = new List<string>();
            public List<string> SourceSegmentIds { get; set; } = new List<string>();
            public double BottomM { get; set; }
            public double TopM { get; set; }
            public double MinThicknessM { get; set; }
            public double MaxThicknessM { get; set; }
        }

        public static IReadOnlyList<WallJunctionOwnershipPlan> Plan(
            IEnumerable<WallJunction> junctions,
            IEnumerable<WallJunctionOwnerContext> ownerMappings,
            double pointToleranceM = 1e-6d,
            double verticalToleranceM = 1e-8d)
        {
            if (junctions == null) throw new ArgumentNullException(nameof(junctions));
            if (ownerMappings == null) throw new ArgumentNullException(nameof(ownerMappings));
            if (!Finite(pointToleranceM) || pointToleranceM <= 0d)
                throw new ArgumentOutOfRangeException(nameof(pointToleranceM), "Point tolerance must be finite and > 0.");
            if (!Finite(verticalToleranceM) || verticalToleranceM <= 0d)
                throw new ArgumentOutOfRangeException(nameof(verticalToleranceM), "Vertical tolerance must be finite and > 0.");

            var junctionList = junctions.Take(MaxJunctions + 1).ToList();
            if (junctionList.Count > MaxJunctions)
                throw new InvalidOperationException("Physical wall-junction ownership planning supports at most " + MaxJunctions + " junctions per batch.");
            var mappingList = ownerMappings.Take(MaxOwnerMappings + 1).ToList();
            if (mappingList.Count > MaxOwnerMappings)
                throw new InvalidOperationException("Physical wall-junction ownership planning supports at most " + MaxOwnerMappings + " source-owner mappings per batch.");

            var bySegment = new Dictionary<string, NormalizedOwner>(StringComparer.Ordinal);
            var byWall = new Dictionary<string, NormalizedOwner>(StringComparer.Ordinal);
            string? batchProjectId = null;
            string? batchDrawingFingerprint = null;

            foreach (var mapping in mappingList)
            {
                if (mapping == null) throw new InvalidOperationException("Wall-junction owner mapping contains null.");
                var normalized = Normalize(mapping);
                if (bySegment.ContainsKey(normalized.SourceSegmentId))
                    throw new InvalidOperationException("Duplicate source-segment owner mapping: " + normalized.SourceSegmentId + ".");
                bySegment.Add(normalized.SourceSegmentId, normalized);

                if (batchProjectId == null)
                {
                    batchProjectId = normalized.ProjectId;
                    batchDrawingFingerprint = normalized.DrawingFingerprint;
                }
                else if (!string.Equals(batchProjectId, normalized.ProjectId, StringComparison.Ordinal) ||
                         !string.Equals(batchDrawingFingerprint, normalized.DrawingFingerprint, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Wall-junction ownership planning cannot span multiple projects or drawings in one batch.");
                }

                if (byWall.TryGetValue(normalized.WallElementId, out var existing))
                {
                    if (!Near(existing.BottomM, normalized.BottomM, verticalToleranceM) ||
                        !Near(existing.TopM, normalized.TopM, verticalToleranceM) ||
                        !Near(existing.ThicknessM, normalized.ThicknessM, verticalToleranceM))
                    {
                        throw new InvalidOperationException(
                            "Semantic wall " + normalized.WallElementId + " has inconsistent vertical/profile data across source segments.");
                    }
                }
                else
                {
                    byWall.Add(normalized.WallElementId, normalized);
                }
            }

            var candidates = new List<Candidate>();
            foreach (var junction in junctionList)
            {
                if (junction == null) throw new InvalidOperationException("Wall-junction input contains null.");
                if (junction.Kind == WallJunctionKind.End || junction.Kind == WallJunctionKind.Straight) continue;
                ValidatePhysicalJunction(junction);

                var sourceOwners = new List<NormalizedOwner>();
                var seenSourceSegments = new HashSet<string>(StringComparer.Ordinal);
                foreach (var sourceSegmentId in junction.SegmentIds)
                {
                    var canonicalSegmentId = Canonical(sourceSegmentId, "wall source-segment id");
                    if (!seenSourceSegments.Add(canonicalSegmentId))
                        throw new InvalidOperationException("Wall junction contains duplicate source segment id: " + canonicalSegmentId + ".");
                    if (!bySegment.TryGetValue(canonicalSegmentId, out var owner))
                        throw new InvalidOperationException("Wall junction has no semantic owner mapping for source segment: " + canonicalSegmentId + ".");
                    sourceOwners.Add(owner);
                }

                var distinctOwners = sourceOwners
                    .GroupBy(x => x.WallElementId, StringComparer.Ordinal)
                    .Select(x => x.First())
                    .OrderBy(x => x.WallElementId, StringComparer.Ordinal)
                    .ToList();

                if (distinctOwners.Count < 2) continue;

                var bottomM = distinctOwners.Max(x => x.BottomM);
                var topM = distinctOwners.Min(x => x.TopM);
                if (!Finite(bottomM) || !Finite(topM) || topM - bottomM <= verticalToleranceM)
                {
                    throw new InvalidOperationException(
                        "Wall junction owners do not share a compatible vertical overlap; physical reconciliation is blocked.");
                }

                var projectId = distinctOwners[0].ProjectId;
                var drawingFingerprint = distinctOwners[0].DrawingFingerprint;
                var ownerWallIds = distinctOwners.Select(x => x.WallElementId).ToList();
                var sourceSegmentIds = sourceOwners.Select(x => x.SourceSegmentId).OrderBy(x => x, StringComparer.Ordinal).ToList();
                var groupKey = BuildGroupKey(projectId, drawingFingerprint, ownerWallIds);

                candidates.Add(new Candidate
                {
                    Junction = junction,
                    ProjectId = projectId,
                    DrawingFingerprint = drawingFingerprint,
                    GroupKey = groupKey,
                    SourceOwners = sourceOwners.OrderBy(x => x.SourceSegmentId, StringComparer.Ordinal).ToList(),
                    OwnerWallIds = ownerWallIds,
                    SourceSegmentIds = sourceSegmentIds,
                    BottomM = bottomM,
                    TopM = topM,
                    MinThicknessM = distinctOwners.Min(x => x.ThicknessM),
                    MaxThicknessM = distinctOwners.Max(x => x.ThicknessM)
                });
            }

            if (candidates.Count == 0) return Array.Empty<WallJunctionOwnershipPlan>();

            var result = new List<WallJunctionOwnershipPlan>(candidates.Count);
            var groupTokenKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var group in candidates.GroupBy(x => x.GroupKey, StringComparer.Ordinal).OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                var ordered = group.ToList();
                ordered.Sort(CompareCandidate);
                for (var index = 1; index < ordered.Count; index++)
                {
                    if (NearPoint(ordered[index - 1].Junction.Point, ordered[index].Junction.Point, pointToleranceM))
                    {
                        throw new InvalidOperationException(
                            "Wall-junction owner group contains duplicate/near-duplicate physical nodes; occurrence ownership is ambiguous.");
                    }
                }

                var groupHash = Sha256Hex(group.Key);
                var groupToken = GroupTokenPrefix + groupHash;
                if (groupTokenKeys.TryGetValue(groupToken, out var existingKey) && !string.Equals(existingKey, group.Key, StringComparison.Ordinal))
                    throw new InvalidOperationException("Wall-junction group-token hash collision detected; native ownership is intentionally blocked.");
                groupTokenKeys[groupToken] = group.Key;

                for (var occurrence = 0; occurrence < ordered.Count; occurrence++)
                {
                    var candidate = ordered[occurrence];
                    var ownerToken = OwnerTokenPrefix + groupHash + ":" + occurrence.ToString(CultureInfo.InvariantCulture);
                    var inputFingerprint = FingerprintPrefix + Sha256Hex(BuildFingerprint(candidate, occurrence));
                    result.Add(new WallJunctionOwnershipPlan(
                        candidate.ProjectId,
                        candidate.DrawingFingerprint,
                        candidate.Junction.Kind,
                        candidate.Junction.Point,
                        occurrence,
                        candidate.GroupKey,
                        groupToken,
                        ownerToken,
                        inputFingerprint,
                        candidate.OwnerWallIds.AsReadOnly(),
                        candidate.SourceSegmentIds.AsReadOnly(),
                        candidate.BottomM,
                        candidate.TopM,
                        candidate.MinThicknessM,
                        candidate.MaxThicknessM));
                }
            }

            return result.AsReadOnly();
        }

        private static NormalizedOwner Normalize(WallJunctionOwnerContext owner)
        {
            return new NormalizedOwner
            {
                SourceSegmentId = Canonical(owner.SourceSegmentId, "wall source-segment id"),
                WallElementId = Canonical(owner.WallElementId, "wall semantic element id"),
                ProjectId = Canonical(owner.ProjectId, "project id"),
                DrawingFingerprint = Canonical(owner.DrawingFingerprint, "drawing fingerprint"),
                BottomM = owner.BottomM,
                TopM = owner.TopM,
                ThicknessM = owner.ThicknessM
            };
        }

        private static void ValidatePhysicalJunction(WallJunction junction)
        {
            if (!Finite(junction.Point.X) || !Finite(junction.Point.Y))
                throw new ArgumentOutOfRangeException(nameof(junction), "Wall junction point must be finite.");
            if (junction.SegmentIds == null || junction.SegmentIds.Count < 2)
                throw new InvalidOperationException("Physical wall junction requires at least two source segments.");

            var validRayCount = junction.Kind == WallJunctionKind.L && junction.RayCount == 2 ||
                                junction.Kind == WallJunctionKind.T && junction.RayCount == 3 ||
                                junction.Kind == WallJunctionKind.X && junction.RayCount == 4 ||
                                junction.Kind == WallJunctionKind.Multi && junction.RayCount >= 5;
            if (!validRayCount)
                throw new InvalidOperationException("Wall junction kind/ray-count contract is inconsistent; physical ownership is blocked.");
        }

        private static string BuildGroupKey(string projectId, string drawingFingerprint, IEnumerable<string> ownerWallIds)
        {
            var owners = ownerWallIds.Select(x => Canonical(x, "wall semantic element id")).OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (owners.Count < 2) throw new InvalidOperationException("Wall-junction owner group requires at least two semantic walls.");
            if (owners.Distinct(StringComparer.Ordinal).Count() != owners.Count)
                throw new InvalidOperationException("Wall-junction owner group contains duplicate semantic wall ids.");

            var builder = new StringBuilder("WJ1|");
            AppendPacked(builder, Canonical(projectId, "project id"));
            AppendPacked(builder, Canonical(drawingFingerprint, "drawing fingerprint"));
            foreach (var owner in owners) AppendPacked(builder, owner);
            return builder.ToString();
        }

        private static string BuildFingerprint(Candidate candidate, int occurrence)
        {
            var builder = new StringBuilder("WJF1|");
            AppendPacked(builder, candidate.GroupKey);
            AppendPacked(builder, occurrence.ToString(CultureInfo.InvariantCulture));
            AppendPacked(builder, candidate.Junction.Kind.ToString());
            AppendPacked(builder, candidate.Junction.RayCount.ToString(CultureInfo.InvariantCulture));
            AppendPacked(builder, candidate.Junction.Point.X.ToString("R", CultureInfo.InvariantCulture));
            AppendPacked(builder, candidate.Junction.Point.Y.ToString("R", CultureInfo.InvariantCulture));
            AppendPacked(builder, candidate.BottomM.ToString("R", CultureInfo.InvariantCulture));
            AppendPacked(builder, candidate.TopM.ToString("R", CultureInfo.InvariantCulture));
            foreach (var owner in candidate.SourceOwners)
            {
                AppendPacked(builder, owner.SourceSegmentId);
                AppendPacked(builder, owner.WallElementId);
                AppendPacked(builder, owner.BottomM.ToString("R", CultureInfo.InvariantCulture));
                AppendPacked(builder, owner.TopM.ToString("R", CultureInfo.InvariantCulture));
                AppendPacked(builder, owner.ThicknessM.ToString("R", CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static int CompareCandidate(Candidate left, Candidate right)
        {
            var x = left.Junction.Point.X.CompareTo(right.Junction.Point.X);
            if (x != 0) return x;
            var y = left.Junction.Point.Y.CompareTo(right.Junction.Point.Y);
            if (y != 0) return y;
            var kind = left.Junction.Kind.CompareTo(right.Junction.Kind);
            if (kind != 0) return kind;
            return left.Junction.RayCount.CompareTo(right.Junction.RayCount);
        }

        private static bool NearPoint(Point2 first, Point2 second, double tolerance)
        {
            var dx = Math.Abs(first.X - second.X);
            var dy = Math.Abs(first.Y - second.Y);
            return Finite(dx) && Finite(dy) && dx <= tolerance && dy <= tolerance;
        }

        private static bool Near(double first, double second, double tolerance)
        {
            var delta = Math.Abs(first - second);
            return Finite(delta) && delta <= tolerance;
        }

        private static string Canonical(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(label + " is required.");
            var normalized = value.Trim();
            if (normalized.Length > MaxIdentityLength)
                throw new InvalidOperationException(label + " exceeds " + MaxIdentityLength + " characters.");
            return normalized.ToUpperInvariant();
        }

        private static void AppendPacked(StringBuilder builder, string value)
        {
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
            builder.Append('|');
        }

        private static string Sha256Hex(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
