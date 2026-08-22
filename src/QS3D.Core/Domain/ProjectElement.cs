using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Domain
{
    [Flags]
    public enum ElementDirtyFlags
    {
        None = 0,
        Geometry = 1,
        Properties = 2,
        Relations = 4,
        Quantity = 8,
        All = Geometry | Properties | Relations | Quantity
    }

    public sealed class ProjectElement
    {
        public const string GeneratedGeometryStateKey = "QS3D.GeneratedGeometry.State";
        public const string GeneratedGeometryStaleReasonKey = "QS3D.GeneratedGeometry.StaleReason";
        public const string GeneratedSolidStateKey = "QS3D.GeneratedSolid.State";
        public const string GeneratedRebarStateKey = "QS3D.GeneratedRebar.State";
        public const string GeneratedShapeRebarStateKey = "QS3D.GeneratedShapeRebar.State";
        public const string GeneratedTieRebarStateKey = "QS3D.GeneratedTieRebar.State";
        public const string GeneratedBeamStirrupStateKey = "QS3D.GeneratedBeamStirrup.State";
        public const string GeneratedSolidStaleSnapshotKey = "QS3D.GeneratedSolid.StaleSnapshot";
        public const string GeneratedRebarStaleSnapshotKey = "QS3D.GeneratedRebar.StaleSnapshot";
        public const string GeneratedShapeRebarStaleSnapshotKey = "QS3D.GeneratedShapeRebar.StaleSnapshot";
        public const string GeneratedTieRebarStaleSnapshotKey = "QS3D.GeneratedTieRebar.StaleSnapshot";
        public const string GeneratedBeamStirrupStaleSnapshotKey = "QS3D.GeneratedBeamStirrup.StaleSnapshot";

        private const string StaleValue = "stale";
        private const string GeneratedSolidHandleKey = "GeneratedSolidHandle";
        private const string GeneratedRebarHandlesKey = "GeneratedRebarHandles";
        private const string GeneratedShapeRebarHandlesKey = "GeneratedShapeRebarHandles";
        private const string GeneratedTieRebarHandlesKey = "GeneratedTieRebarHandles";
        private const string GeneratedBeamStirrupHandlesKey = "GeneratedBeamStirrupHandles";

        public ProjectElement(string id, ElementCategory category, string familyId, string floorId, string zoneId)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Element id is required.", nameof(id));
            Id = id.Trim();
            Category = category;
            FamilyId = familyId?.Trim() ?? string.Empty;
            FloorId = floorId?.Trim() ?? string.Empty;
            ZoneId = zoneId?.Trim() ?? string.Empty;
            SourceHandles = new List<string>();
            DependsOn = new List<string>();
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Quantities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            Dirty = ElementDirtyFlags.All;
        }

        public string Id { get; }
        public ElementCategory Category { get; set; }
        public string FamilyId { get; set; }
        public string FloorId { get; set; }
        public string ZoneId { get; set; }
        public string DrawingFingerprint { get; set; } = string.Empty;
        public IList<string> SourceHandles { get; }
        public IList<string> DependsOn { get; }
        public IDictionary<string, string> Properties { get; }
        public IDictionary<string, double> Quantities { get; }
        public ElementDirtyFlags Dirty { get; private set; }
        public DateTime UpdatedUtc { get; private set; } = DateTime.UtcNow;

        public void MarkDirty(ElementDirtyFlags flags)
        {
            if ((flags & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(flags));
            if ((flags & (ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties | ElementDirtyFlags.Relations)) != 0)
                MarkGeneratedGeometryStale("Semantic/source state changed.");
            Dirty |= flags;
            UpdatedUtc = DateTime.UtcNow;
        }

        public void MarkClean(ElementDirtyFlags flags)
        {
            if ((flags & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(flags));
            Dirty &= ~flags;
            UpdatedUtc = DateTime.UtcNow;
        }

        public void SetProperty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Property name is required.", nameof(name));
            var key = name.Trim();
            var normalized = value ?? string.Empty;
            if (Properties.TryGetValue(key, out var existing) && string.Equals(existing, normalized, StringComparison.Ordinal)) return;
            Properties[key] = normalized;
            var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if (ElementGeometryPolicy.AffectsGeneratedGeometry(Category, key)) flags |= ElementDirtyFlags.Geometry;
            MarkDirty(flags);
        }

        public void SetQuantity(string name, double value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Quantity name is required.", nameof(name));
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Quantities[name.Trim()] = value;
            UpdatedUtc = DateTime.UtcNow;
        }

        public void MarkGeneratedGeometryStale(string reason)
        {
            var marked = false;
            marked |= MarkGeneratedOutputStale(GeneratedSolidHandleKey, GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedRebarHandlesKey, GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedShapeRebarHandlesKey, GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedTieRebarHandlesKey, GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedBeamStirrupHandlesKey, GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey);
            if (!marked) return;
            Properties[GeneratedGeometryStateKey] = StaleValue;
            Properties[GeneratedGeometryStaleReasonKey] = string.IsNullOrWhiteSpace(reason) ? "Semantic/source state changed." : reason.Trim();
            UpdatedUtc = DateTime.UtcNow;
        }

<<<<<<< Updated upstream
        public bool IsGeneratedGeometryStale()
        {
            var stale =
                IsGeneratedSolidStale() ||
                IsGeneratedRebarStale() ||
                IsGeneratedShapeRebarStale() ||
                IsGeneratedTieRebarStale() ||
                IsGeneratedBeamStirrupStale();
            if (!stale)
            {
                Remove(GeneratedGeometryStateKey);
                Remove(GeneratedGeometryStaleReasonKey);
            }
            return stale;
        }
=======
        public bool IsGeneratedGeometryStale() =>
            IsGeneratedSolidStale() || IsGeneratedRebarStale() || IsGeneratedShapeRebarStale() ||
            IsGeneratedTieRebarStale() || IsGeneratedBeamStirrupStale();
>>>>>>> Stashed changes

        public bool IsGeneratedSolidStale() => IsGeneratedOutputStale(GeneratedSolidHandleKey, GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey);
        public bool IsGeneratedRebarStale() => IsGeneratedOutputStale(GeneratedRebarHandlesKey, GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey);
        public bool IsGeneratedShapeRebarStale() => IsGeneratedOutputStale(GeneratedShapeRebarHandlesKey, GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey);
        public bool IsGeneratedTieRebarStale() => IsGeneratedOutputStale(GeneratedTieRebarHandlesKey, GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey);
        public bool IsGeneratedBeamStirrupStale() => IsGeneratedOutputStale(GeneratedBeamStirrupHandlesKey, GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey);

        public void ClearGeneratedSolidStale() => ClearGeneratedOutputStale(GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey);
        public void ClearGeneratedRebarStale() => ClearGeneratedOutputStale(GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey);
        public void ClearGeneratedShapeRebarStale() => ClearGeneratedOutputStale(GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey);
        public void ClearGeneratedTieRebarStale() => ClearGeneratedOutputStale(GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey);
        public void ClearGeneratedBeamStirrupStale() => ClearGeneratedOutputStale(GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey);

        public void ClearGeneratedGeometryStale()
        {
            Remove(GeneratedSolidStateKey); Remove(GeneratedSolidStaleSnapshotKey);
            Remove(GeneratedRebarStateKey); Remove(GeneratedRebarStaleSnapshotKey);
            Remove(GeneratedShapeRebarStateKey); Remove(GeneratedShapeRebarStaleSnapshotKey);
            Remove(GeneratedTieRebarStateKey); Remove(GeneratedTieRebarStaleSnapshotKey);
            Remove(GeneratedBeamStirrupStateKey); Remove(GeneratedBeamStirrupStaleSnapshotKey);
            Remove(GeneratedGeometryStateKey); Remove(GeneratedGeometryStaleReasonKey);
        }

        internal void RestorePersistenceState(ElementDirtyFlags dirty, DateTime updatedUtc)
        {
            if ((dirty & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(dirty));
            Dirty = dirty;
            UpdatedUtc = updatedUtc.Kind == DateTimeKind.Utc ? updatedUtc : updatedUtc.ToUniversalTime();
        }

        private bool MarkGeneratedOutputStale(string outputKey, string stateKey, string snapshotKey)
        {
            var signature = OutputSignature(outputKey);
            if (signature.Length == 0) return false;
            Properties[stateKey] = StaleValue;
            Properties[snapshotKey] = signature;
            return true;
        }

        private bool IsGeneratedOutputStale(string outputKey, string stateKey, string snapshotKey)
        {
            if (!Properties.TryGetValue(stateKey, out var state) || !string.Equals(state, StaleValue, StringComparison.OrdinalIgnoreCase)) return false;
<<<<<<< Updated upstream
            if (!Properties.TryGetValue(snapshotKey, out var snapshot) || string.IsNullOrWhiteSpace(snapshot) ||
                !Properties.TryGetValue(outputKey, out var current) || string.IsNullOrWhiteSpace(current) ||
                !string.Equals(snapshot.Trim(), current.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                Remove(stateKey);
                Remove(snapshotKey);
                return false;
            }
            return true;
=======
            if (!Properties.TryGetValue(snapshotKey, out var snapshot) || string.IsNullOrWhiteSpace(snapshot)) return false;
            return string.Equals(snapshot.Trim(), OutputSignature(outputKey), StringComparison.OrdinalIgnoreCase);
        }

        private string OutputSignature(string outputKey)
        {
            if (!Properties.TryGetValue(outputKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return string.Join(";", raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
>>>>>>> Stashed changes
        }

        private void ClearGeneratedOutputStale(string stateKey, string snapshotKey)
        {
            Remove(stateKey); Remove(snapshotKey); ClearAggregateStaleIfResolved();
        }

        private void ClearAggregateStaleIfResolved()
        {
            if (IsGeneratedGeometryStale()) return;
            Remove(GeneratedGeometryStateKey); Remove(GeneratedGeometryStaleReasonKey);
        }

        private void Remove(string key)
        {
            if (Properties.ContainsKey(key)) Properties.Remove(key);
        }
    }
}
