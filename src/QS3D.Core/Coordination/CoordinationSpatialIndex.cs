using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Coordination
{
    public struct CoordinationBounds : IEquatable<CoordinationBounds>
    {
        public CoordinationBounds(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            ValidateFinite(minX, nameof(minX));
            ValidateFinite(minY, nameof(minY));
            ValidateFinite(minZ, nameof(minZ));
            ValidateFinite(maxX, nameof(maxX));
            ValidateFinite(maxY, nameof(maxY));
            ValidateFinite(maxZ, nameof(maxZ));
            if (maxX < minX || maxY < minY || maxZ < minZ)
                throw new ArgumentException("Coordination bounds max values must be greater than or equal to min values.");
            MinX = minX; MinY = minY; MinZ = minZ;
            MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
        }

        public double MinX { get; }
        public double MinY { get; }
        public double MinZ { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public double MaxZ { get; }

        public bool Intersects(CoordinationBounds other)
        {
            return MinX <= other.MaxX && MaxX >= other.MinX &&
                   MinY <= other.MaxY && MaxY >= other.MinY &&
                   MinZ <= other.MaxZ && MaxZ >= other.MinZ;
        }

        public bool Equals(CoordinationBounds other)
        {
            return MinX.Equals(other.MinX) && MinY.Equals(other.MinY) && MinZ.Equals(other.MinZ) &&
                   MaxX.Equals(other.MaxX) && MaxY.Equals(other.MaxY) && MaxZ.Equals(other.MaxZ);
        }

        public override bool Equals(object obj) => obj is CoordinationBounds && Equals((CoordinationBounds)obj);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = MinX.GetHashCode();
                hash = (hash * 397) ^ MinY.GetHashCode();
                hash = (hash * 397) ^ MinZ.GetHashCode();
                hash = (hash * 397) ^ MaxX.GetHashCode();
                hash = (hash * 397) ^ MaxY.GetHashCode();
                hash = (hash * 397) ^ MaxZ.GetHashCode();
                return hash;
            }
        }

        private static void ValidateFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "Coordination bounds must be finite.");
        }
    }

    public sealed class CoordinationSpatialItem
    {
        public CoordinationSpatialItem(string itemId, string revision, CoordinationBounds bounds)
        {
            ItemId = Required(itemId, nameof(itemId));
            Revision = Required(revision, nameof(revision));
            Bounds = bounds;
        }

        public string ItemId { get; }
        public string Revision { get; }
        public CoordinationBounds Bounds { get; }

        private static string Required(string value, string parameterName)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Control characters are not allowed.", parameterName);
            return normalized;
        }
    }

    public sealed class CoordinationCandidatePair
    {
        internal CoordinationCandidatePair(string firstId, string secondId)
        {
            if (StringComparer.Ordinal.Compare(firstId, secondId) <= 0)
            {
                LeftId = firstId;
                RightId = secondId;
            }
            else
            {
                LeftId = secondId;
                RightId = firstId;
            }
        }

        public string LeftId { get; }
        public string RightId { get; }
        public string PairKey => LeftId + "\u001f" + RightId;
    }

    public sealed class CoordinationSpatialDelta
    {
        internal CoordinationSpatialDelta(IEnumerable<string> changedOrAdded, IEnumerable<string> removed)
        {
            ChangedOrAddedIds = Array.AsReadOnly(changedOrAdded.OrderBy(id => id, StringComparer.Ordinal).ToArray());
            RemovedIds = Array.AsReadOnly(removed.OrderBy(id => id, StringComparer.Ordinal).ToArray());
            AllDirtyIds = Array.AsReadOnly(ChangedOrAddedIds.Concat(RemovedIds).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.Ordinal).ToArray());
        }

        public IReadOnlyList<string> ChangedOrAddedIds { get; }
        public IReadOnlyList<string> RemovedIds { get; }
        public IReadOnlyList<string> AllDirtyIds { get; }
        public bool IsEmpty => AllDirtyIds.Count == 0;
    }

    /// <summary>
    /// Immutable host-neutral uniform-grid broad phase. It returns deterministic AABB-overlap
    /// candidates only; exact native geometry tests remain the responsibility of the host adapter.
    /// </summary>
    public sealed class CoordinationSpatialIndex
    {
        private const long MaxCellsPerItem = 1000000;
        private readonly Dictionary<string, CoordinationSpatialItem> _items;
        private readonly Dictionary<CellKey, List<string>> _cells;
        private readonly ReadOnlyCollection<CoordinationSpatialItem> _orderedItems;

        public CoordinationSpatialIndex(double cellSize, IEnumerable<CoordinationSpatialItem> items)
        {
            if (double.IsNaN(cellSize) || double.IsInfinity(cellSize) || cellSize <= 0d)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be finite and positive.");
            if (items == null) throw new ArgumentNullException(nameof(items));
            CellSize = cellSize;

            _items = new Dictionary<string, CoordinationSpatialItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (item == null) throw new ArgumentException("Spatial index cannot contain null items.", nameof(items));
                if (_items.ContainsKey(item.ItemId))
                    throw new ArgumentException("Spatial index contains duplicate ItemId: " + item.ItemId + ".", nameof(items));
                _items.Add(item.ItemId, item);
            }

            _orderedItems = Array.AsReadOnly(_items.Values.OrderBy(item => item.ItemId, StringComparer.Ordinal).ToArray());
            _cells = new Dictionary<CellKey, List<string>>();
            foreach (var item in _orderedItems)
            {
                foreach (var cell in CellsFor(item.Bounds))
                {
                    List<string> bucket;
                    if (!_cells.TryGetValue(cell, out bucket))
                    {
                        bucket = new List<string>();
                        _cells.Add(cell, bucket);
                    }
                    bucket.Add(item.ItemId);
                }
            }
            foreach (var bucket in _cells.Values) bucket.Sort(StringComparer.Ordinal);
        }

        public double CellSize { get; }
        public IReadOnlyList<CoordinationSpatialItem> Items => _orderedItems;

        public IReadOnlyList<CoordinationCandidatePair> QueryAllPairs()
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var pairs = new List<CoordinationCandidatePair>();
            foreach (var item in _orderedItems)
                AddPairsForItem(item, null, keys, pairs);
            return Array.AsReadOnly(pairs.OrderBy(pair => pair.PairKey, StringComparer.Ordinal).ToArray());
        }

        public IReadOnlyList<CoordinationCandidatePair> QueryChangedPairs(IEnumerable<string> changedItemIds)
        {
            if (changedItemIds == null) throw new ArgumentNullException(nameof(changedItemIds));
            var snapshot = CoordinationRuleCollectionContract.MaterializeBounded(
                changedItemIds,
                "Coordination changed-item IDs");

            var changed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in snapshot)
            {
                var id = (raw ?? string.Empty).Trim();
                if (id.Length == 0) throw new ArgumentException("Changed item ID is required.", nameof(changedItemIds));
                if (!_items.ContainsKey(id)) throw new KeyNotFoundException("Changed ItemId is not present in the current spatial snapshot: " + id + ".");
                changed.Add(id);
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            var pairs = new List<CoordinationCandidatePair>();
            foreach (var id in changed.OrderBy(value => value, StringComparer.Ordinal))
                AddPairsForItem(_items[id], changed, keys, pairs);
            return Array.AsReadOnly(pairs.OrderBy(pair => pair.PairKey, StringComparer.Ordinal).ToArray());
        }

        public CoordinationSpatialDelta Diff(CoordinationSpatialIndex previous)
        {
            if (previous == null) throw new ArgumentNullException(nameof(previous));
            var changedOrAdded = new List<string>();
            var removed = new List<string>();

            foreach (var current in _orderedItems)
            {
                CoordinationSpatialItem old;
                if (!previous._items.TryGetValue(current.ItemId, out old) ||
                    !string.Equals(current.ItemId, old.ItemId, StringComparison.Ordinal) ||
                    !string.Equals(current.Revision, old.Revision, StringComparison.Ordinal) ||
                    !current.Bounds.Equals(old.Bounds))
                    changedOrAdded.Add(current.ItemId);
            }
            foreach (var old in previous._orderedItems)
                if (!_items.ContainsKey(old.ItemId)) removed.Add(old.ItemId);

            return new CoordinationSpatialDelta(changedOrAdded, removed);
        }

        private void AddPairsForItem(
            CoordinationSpatialItem item,
            HashSet<string>? changed,
            HashSet<string> keys,
            List<CoordinationCandidatePair> pairs)
        {
            var neighborIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in CellsFor(item.Bounds))
            {
                List<string> bucket;
                if (!_cells.TryGetValue(cell, out bucket)) continue;
                foreach (var id in bucket) neighborIds.Add(id);
            }

            foreach (var neighborId in neighborIds.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (string.Equals(item.ItemId, neighborId, StringComparison.OrdinalIgnoreCase)) continue;
                var neighbor = _items[neighborId];
                if (!item.Bounds.Intersects(neighbor.Bounds)) continue;
                if (changed != null && !changed.Contains(item.ItemId) && !changed.Contains(neighborId)) continue;
                var pair = new CoordinationCandidatePair(item.ItemId, neighborId);
                if (keys.Add(pair.PairKey)) pairs.Add(pair);
            }
        }

        private IEnumerable<CellKey> CellsFor(CoordinationBounds bounds)
        {
            var minX = CellCoordinate(bounds.MinX);
            var minY = CellCoordinate(bounds.MinY);
            var minZ = CellCoordinate(bounds.MinZ);
            var maxX = CellCoordinate(bounds.MaxX);
            var maxY = CellCoordinate(bounds.MaxY);
            var maxZ = CellCoordinate(bounds.MaxZ);

            var xCount = CheckedCellCount(minX, maxX);
            var yCount = CheckedCellCount(minY, maxY);
            var zCount = CheckedCellCount(minZ, maxZ);
            if (xCount > MaxCellsPerItem / yCount || xCount * yCount > MaxCellsPerItem / zCount)
                throw new InvalidOperationException("Spatial item spans too many grid cells; increase cell size or partition the input.");

            for (var xi = 0L; xi < xCount; xi++)
            {
                var x = minX + xi;
                for (var yi = 0L; yi < yCount; yi++)
                {
                    var y = minY + yi;
                    for (var zi = 0L; zi < zCount; zi++)
                    {
                        var z = minZ + zi;
                        yield return new CellKey(x, y, z);
                    }
                }
            }
        }

        private long CellCoordinate(double value)
        {
            var coordinate = Math.Floor(value / CellSize);
            if (coordinate < long.MinValue || coordinate > long.MaxValue)
                throw new InvalidOperationException("Spatial coordinate exceeds supported grid range.");
            return (long)coordinate;
        }

        private static long CheckedCellCount(long min, long max)
        {
            if (max < min) throw new InvalidOperationException("Spatial grid range is invalid.");
            var count = (decimal)max - (decimal)min + 1m;
            if (count <= 0m || count > MaxCellsPerItem)
                throw new InvalidOperationException("Spatial item spans too many grid cells.");
            return decimal.ToInt64(count);
        }

        private struct CellKey : IEquatable<CellKey>
        {
            public CellKey(long x, long y, long z) { X = x; Y = y; Z = z; }
            private long X { get; }
            private long Y { get; }
            private long Z { get; }
            public bool Equals(CellKey other) => X == other.X && Y == other.Y && Z == other.Z;
            public override bool Equals(object obj) => obj is CellKey && Equals((CellKey)obj);
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = X.GetHashCode();
                    hash = (hash * 397) ^ Y.GetHashCode();
                    hash = (hash * 397) ^ Z.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
