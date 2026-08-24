using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Coordination
{
    /// <summary>
    /// Deterministic stateful coordinator over immutable <see cref="CoordinationSpatialIndex"/> snapshots.
    /// It never performs native geometry tests; it only decides which old broad-phase pairs must be
    /// invalidated and which current pairs require changed-only narrow-phase recomputation.
    /// </summary>
    public sealed class CoordinationIncrementalScanController
    {
        private readonly HashSet<string> _pendingDirtyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private CoordinationSpatialIndex? _current;

        public bool HasSnapshot => _current != null;
        public int PendingDirtyCount => _pendingDirtyIds.Count;

        public void MarkDirty(string itemId)
        {
            var id = RequiredId(itemId, nameof(itemId));
            _pendingDirtyIds.Add(id);
        }

        public void MarkDirty(IEnumerable<string> itemIds)
        {
            if (itemIds == null) throw new ArgumentNullException(nameof(itemIds));

            // Validate and fully enumerate the batch before mutating controller state. This keeps the
            // public bulk operation atomic when a later value is invalid or the source enumerable throws.
            var pending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var itemId in itemIds)
                pending.Add(RequiredId(itemId, nameof(itemId)));

            _pendingDirtyIds.UnionWith(pending);
        }

        public CoordinationIncrementalScanResult ApplySnapshot(
            double cellSize,
            IEnumerable<CoordinationSpatialItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var current = new CoordinationSpatialIndex(cellSize, items);

            if (_current == null)
            {
                ValidatePendingIds(null, current);
                var initialDelta = new CoordinationSpatialDelta(
                    current.Items.Select(item => item.ItemId),
                    Array.Empty<string>());
                var initialPairs = current.QueryAllPairs();
                Commit(current);
                return new CoordinationIncrementalScanResult(
                    true,
                    true,
                    initialDelta,
                    initialPairs,
                    Array.Empty<string>(),
                    current.Items.Count);
            }

            var previous = _current;
            ValidatePendingIds(previous, current);
            var delta = current.Diff(previous);
            var cellSizeChanged = !previous.CellSize.Equals(current.CellSize);

            if (!cellSizeChanged && delta.IsEmpty)
            {
                // A native event may mark an item dirty even when the stable source revision and bounds
                // did not change. Treat that as a no-op: consume the notification without invalidating
                // pair state or creating recomputation churn.
                Commit(current);
                return new CoordinationIncrementalScanResult(
                    false,
                    false,
                    delta,
                    Array.Empty<CoordinationCandidatePair>(),
                    Array.Empty<string>(),
                    current.Items.Count);
            }

            IReadOnlyList<CoordinationCandidatePair> candidates;
            IReadOnlyList<string> invalidated;
            if (cellSizeChanged)
            {
                candidates = current.QueryAllPairs();
                invalidated = Array.AsReadOnly(previous.QueryAllPairs()
                    .Select(pair => pair.PairKey)
                    .OrderBy(key => key, StringComparer.Ordinal)
                    .ToArray());
            }
            else
            {
                var previousIds = new HashSet<string>(
                    previous.Items.Select(item => item.ItemId),
                    StringComparer.OrdinalIgnoreCase);
                var previousDirty = delta.AllDirtyIds
                    .Where(previousIds.Contains)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToArray();

                invalidated = previousDirty.Length == 0
                    ? Array.Empty<string>()
                    : Array.AsReadOnly(previous.QueryChangedPairs(previousDirty)
                        .Select(pair => pair.PairKey)
                        .OrderBy(key => key, StringComparer.Ordinal)
                        .ToArray());

                candidates = delta.ChangedOrAddedIds.Count == 0
                    ? Array.Empty<CoordinationCandidatePair>()
                    : current.QueryChangedPairs(delta.ChangedOrAddedIds);
            }

            Commit(current);
            return new CoordinationIncrementalScanResult(
                false,
                cellSizeChanged,
                delta,
                candidates,
                invalidated,
                current.Items.Count);
        }

        public void Reset()
        {
            _current = null;
            _pendingDirtyIds.Clear();
        }

        private void ValidatePendingIds(CoordinationSpatialIndex? previous, CoordinationSpatialIndex current)
        {
            if (_pendingDirtyIds.Count == 0) return;
            var known = new HashSet<string>(current.Items.Select(item => item.ItemId), StringComparer.OrdinalIgnoreCase);
            if (previous != null)
                known.UnionWith(previous.Items.Select(item => item.ItemId));

            foreach (var id in _pendingDirtyIds.OrderBy(value => value, StringComparer.Ordinal))
                if (!known.Contains(id))
                    throw new KeyNotFoundException(
                        "Dirty ItemId is not present in the previous or current coordination snapshot: " + id + ".");
        }

        private void Commit(CoordinationSpatialIndex current)
        {
            _current = current;
            _pendingDirtyIds.Clear();
        }

        private static string RequiredId(string value, string parameterName)
        {
            var id = (value ?? string.Empty).Trim();
            if (id.Length == 0) throw new ArgumentException("Coordination dirty ItemId is required.", parameterName);
            if (id.Any(char.IsControl)) throw new ArgumentException("Control characters are not allowed.", parameterName);
            return id;
        }
    }

    public sealed class CoordinationIncrementalScanResult
    {
        internal CoordinationIncrementalScanResult(
            bool isInitial,
            bool requiresFullRescan,
            CoordinationSpatialDelta delta,
            IReadOnlyList<CoordinationCandidatePair> candidatePairs,
            IReadOnlyList<string> invalidatedPairKeys,
            int snapshotItemCount)
        {
            IsInitial = isInitial;
            RequiresFullRescan = requiresFullRescan;
            Delta = delta ?? throw new ArgumentNullException(nameof(delta));
            CandidatePairs = new ReadOnlyCollection<CoordinationCandidatePair>(
                (candidatePairs ?? throw new ArgumentNullException(nameof(candidatePairs)))
                    .OrderBy(pair => pair.PairKey, StringComparer.Ordinal)
                    .ToArray());
            InvalidatedPairKeys = new ReadOnlyCollection<string>(
                (invalidatedPairKeys ?? throw new ArgumentNullException(nameof(invalidatedPairKeys)))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(key => key, StringComparer.Ordinal)
                    .ToArray());
            SnapshotItemCount = snapshotItemCount;
        }

        public bool IsInitial { get; }
        public bool RequiresFullRescan { get; }
        public CoordinationSpatialDelta Delta { get; }
        public IReadOnlyList<CoordinationCandidatePair> CandidatePairs { get; }
        public IReadOnlyList<string> InvalidatedPairKeys { get; }
        public int SnapshotItemCount { get; }
        public bool IsNoOp => !RequiresFullRescan && Delta.IsEmpty && CandidatePairs.Count == 0 && InvalidatedPairKeys.Count == 0;
    }
}
