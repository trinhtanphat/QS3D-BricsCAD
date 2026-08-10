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
        public const string GeneratedSolidStaleSnapshotKey = "QS3D.GeneratedSolid.StaleSnapshot";
        public const string GeneratedRebarStaleSnapshotKey = "QS3D.GeneratedRebar.StaleSnapshot";
        public const string GeneratedShapeRebarStaleSnapshotKey = "QS3D.GeneratedShapeRebar.StaleSnapshot";

        private const string GeneratedTieRebarStateKey = "QS3D.GeneratedTieRebar.State";
        private const string GeneratedTieRebarStaleSnapshotKey = "QS3D.GeneratedTieRebar.StaleSnapshot";
        private const string GeneratedBeamStirrupStateKey = "QS3D.GeneratedBeamStirrup.State";
        private const string GeneratedBeamStirrupStaleSnapshotKey = "QS3D.GeneratedBeamStirrup.StaleSnapshot";
        private const string StaleState = "stale";

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
            if ((flags & ElementDirtyFlags.Geometry) != 0) MarkGeneratedGeometryStale("Generated geometry inputs changed.");
            Dirty |= flags;
            UpdatedUtc = DateTime.UtcNow;
        }

        public void MarkClean(ElementDirtyFlags flags)
        {
            Dirty &= ~flags;
            UpdatedUtc = DateTime.UtcNow;
        }

        public void SetProperty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Property name is required.", nameof(name));
            var normalized = name.Trim();
            Properties[normalized] = value ?? string.Empty;
            var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if (ElementGeometryPolicy.AffectsGeneratedGeometry(Category, normalized)) flags |= ElementDirtyFlags.Geometry;
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
        }

        internal void RestorePersistenceState(ElementDirtyFlags dirty, DateTime updatedUtc)
        {
            if ((dirty & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(dirty));
            Dirty = dirty;
            UpdatedUtc = updatedUtc.Kind == DateTimeKind.Utc ? updatedUtc : updatedUtc.ToUniversalTime();
        }
    }
}
