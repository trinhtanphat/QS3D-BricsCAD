using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.Coordination
{
    public sealed class DuplicateCluster
    {
        internal DuplicateCluster(
            string clusterId,
            IReadOnlyList<string> elementIds,
            IReadOnlyList<DuplicatePair> pairs,
            DuplicateMatchKind matchKinds)
        {
            ClusterId = clusterId;
            ElementIds = elementIds;
            Pairs = pairs;
            MatchKinds = matchKinds;
        }

        public string ClusterId { get; }
        public IReadOnlyList<string> ElementIds { get; }
        public IReadOnlyList<DuplicatePair> Pairs { get; }
        public DuplicateMatchKind MatchKinds { get; }
        public bool HasExactGeometry => (MatchKinds & DuplicateMatchKind.ExactGeometry) != 0;
        public bool HasNearGeometry => (MatchKinds & DuplicateMatchKind.NearGeometry) != 0;
        public bool HasSemanticIdentity => (MatchKinds & DuplicateMatchKind.SemanticIdentity) != 0;
    }

    /// <summary>
    /// Converts canonical duplicate pairs into deterministic connected components. This class does
    /// not re-run duplicate classification and therefore cannot diverge from DuplicateDetectionService.
    /// </summary>
    public sealed class DuplicateClusterService
    {
        public IReadOnlyList<DuplicateCluster> Build(DuplicateDetectionResult detection)
        {
            if (detection == null) throw new ArgumentNullException(nameof(detection));
            if (detection.Pairs.Count == 0)
                return new ReadOnlyCollection<DuplicateCluster>(new DuplicateCluster[0]);

            var union = new UnionFind();
            foreach (var pair in detection.Pairs)
            {
                if (pair == null) throw new InvalidOperationException("Duplicate detection result contains a null pair.");
                union.Add(pair.LeftElementId);
                union.Add(pair.RightElementId);
                union.Union(pair.LeftElementId, pair.RightElementId);
            }

            var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var elementId in union.ElementIds)
            {
                var root = union.Find(elementId);
                if (!grouped.TryGetValue(root, out var members))
                {
                    members = new List<string>();
                    grouped.Add(root, members);
                }
                members.Add(elementId);
            }

            var clusters = new List<DuplicateCluster>();
            foreach (var members in grouped.Values)
            {
                members.Sort(CompareIdentity);
                var memberSet = new HashSet<string>(members, StringComparer.OrdinalIgnoreCase);
                var pairs = detection.Pairs
                    .Where(pair => memberSet.Contains(pair.LeftElementId) && memberSet.Contains(pair.RightElementId))
                    .OrderBy(pair => pair.PairKey, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(pair => pair.PairKey, StringComparer.Ordinal)
                    .ToArray();

                var matchKinds = DuplicateMatchKind.None;
                foreach (var pair in pairs) matchKinds |= pair.MatchKinds;

                clusters.Add(
                    new DuplicateCluster(
                        BuildClusterId(members),
                        new ReadOnlyCollection<string>(members.ToArray()),
                        new ReadOnlyCollection<DuplicatePair>(pairs),
                        matchKinds));
            }

            clusters.Sort(CompareClusters);
            return new ReadOnlyCollection<DuplicateCluster>(clusters.ToArray());
        }

        private static string BuildClusterId(IReadOnlyList<string> sortedElementIds)
        {
            var builder = new StringBuilder();
            foreach (var elementId in sortedElementIds)
            {
                builder.Append(elementId.Length).Append(':').Append(elementId).Append(';');
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                var hex = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) hex.Append(value.ToString("x2"));
                return "DUPCL-" + hex;
            }
        }

        private static int CompareClusters(DuplicateCluster left, DuplicateCluster right)
        {
            var comparison = CompareIdentity(left.ElementIds[0], right.ElementIds[0]);
            if (comparison != 0) return comparison;
            return StringComparer.Ordinal.Compare(left.ClusterId, right.ClusterId);
        }

        private static int CompareIdentity(string left, string right)
        {
            var comparison = StringComparer.OrdinalIgnoreCase.Compare(left, right);
            return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left, right);
        }

        private sealed class UnionFind
        {
            private readonly Dictionary<string, string> _parent =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public IEnumerable<string> ElementIds => _parent.Keys;

            public void Add(string elementId)
            {
                if (!_parent.ContainsKey(elementId)) _parent.Add(elementId, elementId);
            }

            public string Find(string elementId)
            {
                var parent = _parent[elementId];
                if (StringComparer.OrdinalIgnoreCase.Equals(parent, elementId)) return parent;
                var root = Find(parent);
                _parent[elementId] = root;
                return root;
            }

            public void Union(string left, string right)
            {
                var leftRoot = Find(left);
                var rightRoot = Find(right);
                if (StringComparer.OrdinalIgnoreCase.Equals(leftRoot, rightRoot)) return;

                if (CompareIdentity(leftRoot, rightRoot) <= 0)
                    _parent[rightRoot] = leftRoot;
                else
                    _parent[leftRoot] = rightRoot;
            }
        }
    }

    public sealed class DuplicateRemediationEvidence
    {
        public DuplicateRemediationEvidence(
            string elementId,
            string? semanticOwnerId,
            string? quantityOwnerId,
            bool isStale = false,
            bool isAmbiguous = false,
            bool isProtectedFromRemoval = false)
        {
            ElementId = Required(elementId, nameof(elementId));
            SemanticOwnerId = Optional(semanticOwnerId, nameof(semanticOwnerId));
            QuantityOwnerId = Optional(quantityOwnerId, nameof(quantityOwnerId));
            IsStale = isStale;
            IsAmbiguous = isAmbiguous;
            IsProtectedFromRemoval = isProtectedFromRemoval;
        }

        public string ElementId { get; }
        public string SemanticOwnerId { get; }
        public string QuantityOwnerId { get; }
        public bool IsStale { get; }
        public bool IsAmbiguous { get; }
        public bool IsProtectedFromRemoval { get; }

        private static string Required(string value, string parameterName)
        {
            var raw = value ?? string.Empty;
            RejectControlCharacters(raw, parameterName);
            var normalized = raw.Trim();
            if (normalized.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            return normalized;
        }

        private static string Optional(string? value, string parameterName)
        {
            if (value == null) return string.Empty;
            RejectControlCharacters(value, parameterName);
            var normalized = value.Trim();
            if (normalized.Length == 0) return string.Empty;
            return normalized;
        }

        private static void RejectControlCharacters(string value, string parameterName)
        {
            if (value.Any(char.IsControl))
                throw new ArgumentException("Control characters are not allowed.", parameterName);
        }
    }

    public enum DuplicateRemediationBlockedReason
    {
        MissingElementEvidence = 0,
        StaleEvidence = 1,
        AmbiguousEvidence = 2,
        ConflictingSemanticOwnership = 3,
        ConflictingQuantityOwnership = 4,
        MissingOwnershipAuthority = 5,
        AmbiguousRepresentative = 6,
        PreferredRepresentativeNotAuthoritative = 7,
        ProtectedRemoval = 8
    }

    public sealed class DuplicateRemediationPlan
    {
        internal DuplicateRemediationPlan(
            string clusterId,
            string representativeElementId,
            IReadOnlyList<string> removableElementIds,
            IReadOnlyList<DuplicateRemediationBlockedReason> blockedReasons)
        {
            ClusterId = clusterId;
            RepresentativeElementId = representativeElementId;
            RemovableElementIds = removableElementIds;
            BlockedReasons = blockedReasons;
        }

        public string ClusterId { get; }
        public string RepresentativeElementId { get; }
        public IReadOnlyList<string> RemovableElementIds { get; }
        public IReadOnlyList<DuplicateRemediationBlockedReason> BlockedReasons { get; }
        public bool CanApply => BlockedReasons.Count == 0;
        public bool IsDryRun => true;
    }

    /// <summary>
    /// Produces a fail-closed remediation preview only. Native deletion/merge is deliberately not
    /// represented here; a host must require explicit confirmation and revalidate live state later.
    /// </summary>
    public sealed class DuplicateRemediationPlanner
    {
        public DuplicateRemediationPlan Plan(
            DuplicateCluster cluster,
            IEnumerable<DuplicateRemediationEvidence> evidence,
            string? preferredRepresentativeElementId = null)
        {
            if (cluster == null) throw new ArgumentNullException(nameof(cluster));
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));

            var byElement = SnapshotEvidence(cluster, evidence);
            var blocked = new List<DuplicateRemediationBlockedReason>();

            if (cluster.ElementIds.Any(id => !byElement.ContainsKey(id)))
                AddBlocked(blocked, DuplicateRemediationBlockedReason.MissingElementEvidence);

            var available = cluster.ElementIds
                .Where(byElement.ContainsKey)
                .Select(id => byElement[id])
                .ToArray();

            if (available.Any(item => item.IsStale))
                AddBlocked(blocked, DuplicateRemediationBlockedReason.StaleEvidence);
            if (available.Any(item => item.IsAmbiguous))
                AddBlocked(blocked, DuplicateRemediationBlockedReason.AmbiguousEvidence);

            var semanticOwners = DistinctOwners(available.Select(item => item.SemanticOwnerId));
            var quantityOwners = DistinctOwners(available.Select(item => item.QuantityOwnerId));
            if (semanticOwners.Length > 1)
                AddBlocked(blocked, DuplicateRemediationBlockedReason.ConflictingSemanticOwnership);
            if (quantityOwners.Length > 1)
                AddBlocked(blocked, DuplicateRemediationBlockedReason.ConflictingQuantityOwnership);
            if (semanticOwners.Length == 0 && quantityOwners.Length == 0)
                AddBlocked(blocked, DuplicateRemediationBlockedReason.MissingOwnershipAuthority);

            if (blocked.Count > 0)
                return Blocked(cluster.ClusterId, blocked);

            var semanticOwner = semanticOwners.Length == 1 ? semanticOwners[0] : string.Empty;
            var quantityOwner = quantityOwners.Length == 1 ? quantityOwners[0] : string.Empty;
            var authoritative = available
                .Where(item => OwnerMatches(item.SemanticOwnerId, semanticOwner) &&
                               OwnerMatches(item.QuantityOwnerId, quantityOwner))
                .OrderBy(item => item.ElementId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.ElementId, StringComparer.Ordinal)
                .ToArray();

            DuplicateRemediationEvidence? representative = null;
            var preferred = NormalizePreferred(preferredRepresentativeElementId);
            if (preferred.Length > 0)
            {
                representative = authoritative.FirstOrDefault(
                    item => StringComparer.OrdinalIgnoreCase.Equals(item.ElementId, preferred));
                if (representative == null)
                    AddBlocked(blocked, DuplicateRemediationBlockedReason.PreferredRepresentativeNotAuthoritative);
            }
            else
            {
                var protectedCandidates = authoritative.Where(item => item.IsProtectedFromRemoval).ToArray();
                if (protectedCandidates.Length == 1)
                    representative = protectedCandidates[0];
                else if (protectedCandidates.Length > 1)
                    AddBlocked(blocked, DuplicateRemediationBlockedReason.AmbiguousRepresentative);
                else if (authoritative.Length == 1)
                    representative = authoritative[0];
                else
                    AddBlocked(blocked, DuplicateRemediationBlockedReason.AmbiguousRepresentative);
            }

            if (representative == null || blocked.Count > 0)
                return Blocked(cluster.ClusterId, blocked);

            var removable = cluster.ElementIds
                .Where(id => !StringComparer.OrdinalIgnoreCase.Equals(id, representative.ElementId))
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(id => id, StringComparer.Ordinal)
                .ToArray();

            if (removable.Any(id => byElement[id].IsProtectedFromRemoval))
            {
                AddBlocked(blocked, DuplicateRemediationBlockedReason.ProtectedRemoval);
                return Blocked(cluster.ClusterId, blocked);
            }

            return new DuplicateRemediationPlan(
                cluster.ClusterId,
                representative.ElementId,
                new ReadOnlyCollection<string>(removable),
                new ReadOnlyCollection<DuplicateRemediationBlockedReason>(new DuplicateRemediationBlockedReason[0]));
        }

        private static Dictionary<string, DuplicateRemediationEvidence> SnapshotEvidence(
            DuplicateCluster cluster,
            IEnumerable<DuplicateRemediationEvidence> evidence)
        {
            var clusterMembers = new HashSet<string>(cluster.ElementIds, StringComparer.OrdinalIgnoreCase);
            var snapshot = new Dictionary<string, DuplicateRemediationEvidence>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var item in evidence)
            {
                if (item == null)
                    throw new ArgumentException("Remediation evidence contains a null item at index " + index + ".", nameof(evidence));
                if (!clusterMembers.Contains(item.ElementId))
                    throw new ArgumentException("Remediation evidence references an element outside the cluster: " + item.ElementId + ".", nameof(evidence));
                if (snapshot.ContainsKey(item.ElementId))
                    throw new ArgumentException("Duplicate remediation evidence for element: " + item.ElementId + ".", nameof(evidence));
                snapshot.Add(item.ElementId, item);
                index++;
            }
            return snapshot;
        }

        private static string[] DistinctOwners(IEnumerable<string> ownerIds)
        {
            return ownerIds
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool OwnerMatches(string actual, string required)
        {
            return required.Length == 0 || StringComparer.OrdinalIgnoreCase.Equals(actual, required);
        }

        private static string NormalizePreferred(string? value)
        {
            if (value == null) return string.Empty;
            if (value.Any(char.IsControl))
                throw new ArgumentException("Preferred representative contains control characters.", nameof(value));
            return value.Trim();
        }

        private static DuplicateRemediationPlan Blocked(
            string clusterId,
            List<DuplicateRemediationBlockedReason> blocked)
        {
            blocked.Sort();
            return new DuplicateRemediationPlan(
                clusterId,
                string.Empty,
                new ReadOnlyCollection<string>(new string[0]),
                new ReadOnlyCollection<DuplicateRemediationBlockedReason>(blocked.ToArray()));
        }

        private static void AddBlocked(
            List<DuplicateRemediationBlockedReason> blocked,
            DuplicateRemediationBlockedReason reason)
        {
            if (!blocked.Contains(reason)) blocked.Add(reason);
        }
    }
}
