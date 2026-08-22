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
<<<<<<< Updated upstream
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
=======
        public const string GeneratedSolidStaleSnapshotKey = "QS3D.GeneratedSolid.StaleSnapshot";
        public const string GeneratedRebarStaleSnapshotKey = "QS3D.GeneratedRebar.StaleSnapshot";
        public const string GeneratedShapeRebarStaleSnapshotKey = "QS3D.GeneratedShapeRebar.StaleSnapshot";

        private const string GeneratedTieRebarStateKey = "QS3D.GeneratedTieRebar.State";
        private const string GeneratedTieRebarStaleSnapshotKey = "QS3D.GeneratedTieRebar.StaleSnapshot";
        private const string GeneratedBeamStirrupStateKey = "QS3D.GeneratedBeamStirrup.State";
        private const string GeneratedBeamStirrupStaleSnapshotKey = "QS3D.GeneratedBeamStirrup.StaleSnapshot";
        private const string StaleState = "stale";
>>>>>>> Stashed changes

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
<<<<<<< Updated upstream
            if ((flags & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(flags));
            if ((flags & (ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties | ElementDirtyFlags.Relations)) != 0)
                MarkGeneratedGeometryStale("Semantic/source state changed.");
=======
            if ((flags & ElementDirtyFlags.Geometry) != 0) MarkGeneratedGeometryStale("Generated geometry inputs changed.");
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
            var key = name.Trim();
            var normalized = value ?? string.Empty;
            if (Properties.TryGetValue(key, out var existing) && string.Equals(existing, normalized, StringComparison.Ordinal)) return;
            Properties[key] = normalized;
            MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
=======
            var normalized = name.Trim();
            Properties[normalized] = value ?? string.Empty;
            var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if (ElementGeometryPolicy.AffectsGeneratedGeometry(Category, normalized)) flags |= ElementDirtyFlags.Geometry;
            MarkDirty(flags);
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
            marked |= MarkGeneratedOutputStale(GeneratedSolidHandleKey, GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedRebarHandlesKey, GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedShapeRebarHandlesKey, GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedTieRebarHandlesKey, GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedBeamStirrupHandlesKey, GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey);
            if (!marked) return;
            Properties[GeneratedGeometryStateKey] = StaleValue;
            Properties[GeneratedGeometryStaleReasonKey] = string.IsNullOrWhiteSpace(reason) ? "Semantic/source state changed." : reason.Trim();
        }

        public bool IsGeneratedGeometryStale() =>
            IsGeneratedSolidStale() ||
            IsGeneratedRebarStale() ||
            IsGeneratedShapeRebarStale() ||
            IsGeneratedTieRebarStale() ||
            IsGeneratedBeamStirrupStale();

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
            Remove(GeneratedSolidStateKey);
            Remove(GeneratedSolidStaleSnapshotKey);
            Remove(GeneratedRebarStateKey);
            Remove(GeneratedRebarStaleSnapshotKey);
            Remove(GeneratedShapeRebarStateKey);
            Remove(GeneratedShapeRebarStaleSnapshotKey);
            Remove(GeneratedTieRebarStateKey);
            Remove(GeneratedTieRebarStaleSnapshotKey);
            Remove(GeneratedBeamStirrupStateKey);
            Remove(GeneratedBeamStirrupStaleSnapshotKey);
            Remove(GeneratedGeometryStateKey);
            Remove(GeneratedGeometryStaleReasonKey);
=======
            marked |= MarkOutputStale("GeneratedSolidHandle", GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey);
            marked |= MarkOutputStale("GeneratedRebarHandles", GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey);
            marked |= MarkOutputStale("GeneratedShapeRebarHandles", GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey);
            marked |= MarkOutputStale("GeneratedTieRebarHandles", GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey);
            marked |= MarkOutputStale("GeneratedBeamStirrupHandles", GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey);
            if (!marked) return;
            Properties[GeneratedGeometryStateKey] = StaleState;
            Properties[GeneratedGeometryStaleReasonKey] = string.IsNullOrWhiteSpace(reason) ? "Generated geometry inputs changed." : reason.Trim();
            UpdatedUtc = DateTime.UtcNow;
        }

        public bool IsGeneratedSolidStale() => IsOutputStale("GeneratedSolidHandle", GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey);

        public bool IsGeneratedRebarStale() =>
            IsOutputStale("GeneratedRebarHandles", GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey) ||
            IsOutputStale("GeneratedTieRebarHandles", GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey) ||
            IsOutputStale("GeneratedBeamStirrupHandles", GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey);

        public bool IsGeneratedShapeRebarStale() => IsOutputStale("GeneratedShapeRebarHandles", GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey);

        public bool IsGeneratedGeometryStale() => IsGeneratedSolidStale() || IsGeneratedRebarStale() || IsGeneratedShapeRebarStale();

        public void ClearGeneratedSolidStale()
        {
            ClearOutputStale(GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey);
            RefreshGeneratedGeometryState();
        }

        public void ClearGeneratedRebarStale()
        {
            ClearOutputStale(GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey);
            RefreshGeneratedGeometryState();
        }

        public void ClearGeneratedShapeRebarStale()
        {
            ClearOutputStale(GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey);
            RefreshGeneratedGeometryState();
        }

        public void ClearGeneratedTieRebarStale()
        {
            ClearOutputStale(GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey);
            RefreshGeneratedGeometryState();
        }

        public void ClearGeneratedBeamStirrupStale()
        {
            ClearOutputStale(GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey);
            RefreshGeneratedGeometryState();
        }

        public void ClearGeneratedGeometryStale()
        {
            ClearOutputStale(GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey);
            ClearOutputStale(GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey);
            ClearOutputStale(GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey);
            ClearOutputStale(GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey);
            ClearOutputStale(GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey);
            RefreshGeneratedGeometryState();
        }

        private bool MarkOutputStale(string handleKey, string stateKey, string snapshotKey)
        {
            var signature = OutputSignature(handleKey);
            if (signature.Length == 0) return false;
            Properties[stateKey] = StaleState;
            Properties[snapshotKey] = signature;
            return true;
        }

        private bool IsOutputStale(string handleKey, string stateKey, string snapshotKey)
        {
            if (!Properties.TryGetValue(stateKey, out var state) || !string.Equals(state, StaleState, StringComparison.OrdinalIgnoreCase)) return false;
            if (!Properties.TryGetValue(snapshotKey, out var snapshot) || string.IsNullOrWhiteSpace(snapshot)) return false;
            return string.Equals(snapshot, OutputSignature(handleKey), StringComparison.OrdinalIgnoreCase);
        }

        private string OutputSignature(string handleKey)
        {
            if (!Properties.TryGetValue(handleKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return string.Join(";", raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        private void ClearOutputStale(string stateKey, string snapshotKey)
        {
            Properties.Remove(stateKey);
            Properties.Remove(snapshotKey);
        }

        private void RefreshGeneratedGeometryState()
        {
            if (IsGeneratedGeometryStale()) return;
            Properties.Remove(GeneratedGeometryStateKey);
            Properties.Remove(GeneratedGeometryStaleReasonKey);
>>>>>>> Stashed changes
        }

        internal void RestorePersistenceState(ElementDirtyFlags dirty, DateTime updatedUtc)
        {
            if ((dirty & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(dirty));
            Dirty = dirty;
            UpdatedUtc = updatedUtc.Kind == DateTimeKind.Utc ? updatedUtc : updatedUtc.ToUniversalTime();
        }

        private bool MarkGeneratedOutputStale(string outputKey, string stateKey, string snapshotKey)
        {
            if (!Properties.TryGetValue(outputKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return false;
            Properties[stateKey] = StaleValue;
            Properties[snapshotKey] = raw.Trim();
            return true;
        }

        private bool IsGeneratedOutputStale(string outputKey, string stateKey, string snapshotKey)
        {
            if (!Properties.TryGetValue(stateKey, out var state) || !string.Equals(state, StaleValue, StringComparison.OrdinalIgnoreCase)) return false;
            if (!Properties.TryGetValue(snapshotKey, out var snapshot) || string.IsNullOrWhiteSpace(snapshot)) return false;
            if (!Properties.TryGetValue(outputKey, out var current) || string.IsNullOrWhiteSpace(current)) return false;
            return string.Equals(snapshot.Trim(), current.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private void ClearGeneratedOutputStale(string stateKey, string snapshotKey)
        {
            Remove(stateKey);
            Remove(snapshotKey);
            ClearAggregateStaleIfResolved();
        }

        private void ClearAggregateStaleIfResolved()
        {
            if (IsGeneratedGeometryStale()) return;
            Remove(GeneratedGeometryStateKey);
            Remove(GeneratedGeometryStaleReasonKey);
        }

        private void Remove(string key)
        {
            if (Properties.ContainsKey(key)) Properties.Remove(key);
        }
    }
}
