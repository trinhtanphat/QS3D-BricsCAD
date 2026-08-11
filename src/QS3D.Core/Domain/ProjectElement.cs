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
        public const string GeneratedSlabMeshStateKey = "QS3D.GeneratedSlabMesh.State";
        public const string GeneratedWallMeshStateKey = "QS3D.GeneratedWallMesh.State";
        public const string GeneratedFoundationMeshStateKey = "QS3D.GeneratedFoundationMesh.State";
        public const string GeneratedCurtainFrameStateKey = "QS3D.GeneratedCurtainFrame.State";
        public const string GeneratedCurtainPanelStateKey = "QS3D.GeneratedCurtainPanel.State";
        public const string GeneratedSolidStaleSnapshotKey = "QS3D.GeneratedSolid.StaleSnapshot";
        public const string GeneratedRebarStaleSnapshotKey = "QS3D.GeneratedRebar.StaleSnapshot";
        public const string GeneratedShapeRebarStaleSnapshotKey = "QS3D.GeneratedShapeRebar.StaleSnapshot";
        public const string GeneratedTieRebarStaleSnapshotKey = "QS3D.GeneratedTieRebar.StaleSnapshot";
        public const string GeneratedBeamStirrupStaleSnapshotKey = "QS3D.GeneratedBeamStirrup.StaleSnapshot";
        public const string GeneratedSlabMeshStaleSnapshotKey = "QS3D.GeneratedSlabMesh.StaleSnapshot";
        public const string GeneratedWallMeshStaleSnapshotKey = "QS3D.GeneratedWallMesh.StaleSnapshot";
        public const string GeneratedFoundationMeshStaleSnapshotKey = "QS3D.GeneratedFoundationMesh.StaleSnapshot";
        public const string GeneratedCurtainFrameStaleSnapshotKey = "QS3D.GeneratedCurtainFrame.StaleSnapshot";
        public const string GeneratedCurtainPanelStaleSnapshotKey = "QS3D.GeneratedCurtainPanel.StaleSnapshot";

        private const string StaleValue = "stale";
        private const string GeneratedSolidHandleKey = "GeneratedSolidHandle";
        private const string GeneratedRebarHandlesKey = "GeneratedRebarHandles";
        private const string GeneratedShapeRebarHandlesKey = "GeneratedShapeRebarHandles";
        private const string GeneratedTieRebarHandlesKey = "GeneratedTieRebarHandles";
        private const string GeneratedBeamStirrupHandlesKey = "GeneratedBeamStirrupHandles";
        private const string GeneratedSlabMeshHandlesKey = "GeneratedSlabMeshHandles";
        private const string GeneratedWallMeshHandlesKey = "GeneratedWallMeshHandles";
        private const string GeneratedFoundationMeshHandlesKey = "GeneratedFoundationMeshHandles";
        private const string GeneratedCurtainFrameHandlesKey = "GeneratedCurtainFrameHandles";
        private const string GeneratedCurtainPanelHandlesKey = "GeneratedCurtainPanelHandles";
        private const string GeneratedCurtainPanelBuildStateKey = "GeneratedCurtainPanelBuildState";
        private const string GeneratedCurtainPanelBuildCompleteValue = "Complete";

        private ElementCategory _category;

        public ProjectElement(string id, ElementCategory category)
            : this(id, category, string.Empty, string.Empty, string.Empty)
        {
        }

        public ProjectElement(string id, ElementCategory category, string familyId, string floorId, string zoneId)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Element id is required.", nameof(id));
            Id = id.Trim();
            _category = RequireCategory(category);
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
        public ElementCategory Category
        {
            get => _category;
            set
            {
                var next = RequireCategory(value);
                if (_category == next) return;
                _category = next;
            }
        }
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
            MarkDirtyCore(
                flags,
                (flags & (ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties | ElementDirtyFlags.Relations)) != 0);
        }

        public void MarkClean(ElementDirtyFlags flags)
        {
            if ((flags & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(flags));
            if (flags == ElementDirtyFlags.None) return;
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
            var affectsGeneratedGeometry = ElementGeometryPolicy.AffectsGeneratedGeometry(Category, key);
            var affectsGeneratedOutput = ElementGeometryPolicy.AffectsGeneratedOutput(Category, key);
            var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if (affectsGeneratedGeometry) flags |= ElementDirtyFlags.Geometry;
            MarkDirtyCore(flags, affectsGeneratedOutput);
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
            marked |= MarkGeneratedOutputStale(GeneratedSlabMeshHandlesKey, GeneratedSlabMeshStateKey, GeneratedSlabMeshStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedWallMeshHandlesKey, GeneratedWallMeshStateKey, GeneratedWallMeshStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedFoundationMeshHandlesKey, GeneratedFoundationMeshStateKey, GeneratedFoundationMeshStaleSnapshotKey);
            marked |= MarkGeneratedOutputStale(GeneratedCurtainFrameHandlesKey, GeneratedCurtainFrameStateKey, GeneratedCurtainFrameStaleSnapshotKey);
            marked |= MarkGeneratedCurtainPanelOutputStale();
            if (!marked) return;
            SetAggregateStaleReason(reason);
        }

        public void MarkGeneratedCurtainFrameStale(string reason)
        {
            if (!MarkGeneratedOutputStale(GeneratedCurtainFrameHandlesKey, GeneratedCurtainFrameStateKey, GeneratedCurtainFrameStaleSnapshotKey)) return;
            SetAggregateStaleReason(reason);
        }

        public void MarkGeneratedCurtainPanelStale(string reason)
        {
            if (!MarkGeneratedCurtainPanelOutputStale()) return;
            SetAggregateStaleReason(reason);
        }

        public bool IsGeneratedGeometryStale()
        {
            return
                IsGeneratedSolidStale() ||
                IsGeneratedRebarStale() ||
                IsGeneratedShapeRebarStale() ||
                IsGeneratedTieRebarStale() ||
                IsGeneratedBeamStirrupStale() ||
                IsGeneratedSlabMeshStale() ||
                IsGeneratedWallMeshStale() ||
                IsGeneratedFoundationMeshStale() ||
                IsGeneratedCurtainFrameStale() ||
                IsGeneratedCurtainPanelStale();
        }

        public bool IsGeneratedSolidStale() => IsGeneratedOutputStale(GeneratedSolidHandleKey, GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey);
        public bool IsGeneratedRebarStale() => IsGeneratedOutputStale(GeneratedRebarHandlesKey, GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey);
        public bool IsGeneratedShapeRebarStale() => IsGeneratedOutputStale(GeneratedShapeRebarHandlesKey, GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey);
        public bool IsGeneratedTieRebarStale() => IsGeneratedOutputStale(GeneratedTieRebarHandlesKey, GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey);
        public bool IsGeneratedBeamStirrupStale() => IsGeneratedOutputStale(GeneratedBeamStirrupHandlesKey, GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey);
        public bool IsGeneratedSlabMeshStale() => IsGeneratedOutputStale(GeneratedSlabMeshHandlesKey, GeneratedSlabMeshStateKey, GeneratedSlabMeshStaleSnapshotKey);
        public bool IsGeneratedWallMeshStale() => IsGeneratedOutputStale(GeneratedWallMeshHandlesKey, GeneratedWallMeshStateKey, GeneratedWallMeshStaleSnapshotKey);
        public bool IsGeneratedFoundationMeshStale() => IsGeneratedOutputStale(GeneratedFoundationMeshHandlesKey, GeneratedFoundationMeshStateKey, GeneratedFoundationMeshStaleSnapshotKey);
        public bool IsGeneratedCurtainFrameStale() => IsGeneratedOutputStale(GeneratedCurtainFrameHandlesKey, GeneratedCurtainFrameStateKey, GeneratedCurtainFrameStaleSnapshotKey);
        public bool IsGeneratedCurtainPanelStale() => IsGeneratedCurtainPanelOutputStale();

        public void ClearGeneratedSolidStale() => ClearGeneratedOutputStale(GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey);
        public void ClearGeneratedRebarStale() => ClearGeneratedOutputStale(GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey);
        public void ClearGeneratedShapeRebarStale() => ClearGeneratedOutputStale(GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey);
        public void ClearGeneratedTieRebarStale() => ClearGeneratedOutputStale(GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey);
        public void ClearGeneratedBeamStirrupStale() => ClearGeneratedOutputStale(GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey);
        public void ClearGeneratedSlabMeshStale() => ClearGeneratedOutputStale(GeneratedSlabMeshStateKey, GeneratedSlabMeshStaleSnapshotKey);
        public void ClearGeneratedWallMeshStale() => ClearGeneratedOutputStale(GeneratedWallMeshStateKey, GeneratedWallMeshStaleSnapshotKey);
        public void ClearGeneratedFoundationMeshStale() => ClearGeneratedOutputStale(GeneratedFoundationMeshStateKey, GeneratedFoundationMeshStaleSnapshotKey);
        public void ClearGeneratedCurtainFrameStale() => ClearGeneratedOutputStale(GeneratedCurtainFrameStateKey, GeneratedCurtainFrameStaleSnapshotKey);
        public void ClearGeneratedCurtainPanelStale() => ClearGeneratedOutputStale(GeneratedCurtainPanelStateKey, GeneratedCurtainPanelStaleSnapshotKey);

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
            Remove(GeneratedSlabMeshStateKey);
            Remove(GeneratedSlabMeshStaleSnapshotKey);
            Remove(GeneratedWallMeshStateKey);
            Remove(GeneratedWallMeshStaleSnapshotKey);
            Remove(GeneratedFoundationMeshStateKey);
            Remove(GeneratedFoundationMeshStaleSnapshotKey);
            Remove(GeneratedCurtainFrameStateKey);
            Remove(GeneratedCurtainFrameStaleSnapshotKey);
            Remove(GeneratedCurtainPanelStateKey);
            Remove(GeneratedCurtainPanelStaleSnapshotKey);
            Remove(GeneratedGeometryStateKey);
            Remove(GeneratedGeometryStaleReasonKey);
        }

        internal void RestorePersistenceState(ElementDirtyFlags dirty, DateTime updatedUtc)
        {
            if ((dirty & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(dirty));
            Dirty = dirty;
            UpdatedUtc = updatedUtc.Kind == DateTimeKind.Utc ? updatedUtc : updatedUtc.ToUniversalTime();
        }

        private void MarkDirtyCore(ElementDirtyFlags flags, bool markGeneratedGeometryStale)
        {
            if ((flags & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(flags));
            if (flags == ElementDirtyFlags.None) return;
            if (markGeneratedGeometryStale)
                MarkGeneratedGeometryStale("Semantic/source state changed.");
            Dirty |= flags;
            UpdatedUtc = DateTime.UtcNow;
        }

        private static ElementCategory RequireCategory(ElementCategory value)
        {
            if (!Enum.IsDefined(typeof(ElementCategory), value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Element category must be a defined ElementCategory.");
            return value;
        }

        private bool MarkGeneratedOutputStale(string outputKey, string stateKey, string snapshotKey)
        {
            var signature = OutputSignature(outputKey);
            if (signature.Length == 0) return false;
            Properties[stateKey] = StaleValue;
            Properties[snapshotKey] = signature;
            return true;
        }

        private bool MarkGeneratedCurtainPanelOutputStale()
        {
            var signature = CurtainPanelOutputSignature();
            if (signature.Length == 0) return false;
            Properties[GeneratedCurtainPanelStateKey] = StaleValue;
            Properties[GeneratedCurtainPanelStaleSnapshotKey] = signature;
            return true;
        }

        private bool IsGeneratedCurtainPanelOutputStale()
        {
            if (!Properties.TryGetValue(GeneratedCurtainPanelStateKey, out var state) || !string.Equals(state, StaleValue, StringComparison.OrdinalIgnoreCase)) return false;
            var current = CurtainPanelOutputSignature();
            return Properties.TryGetValue(GeneratedCurtainPanelStaleSnapshotKey, out var snapshot) &&
                   !string.IsNullOrWhiteSpace(snapshot) &&
                   current.Length > 0 &&
                   string.Equals(snapshot.Trim(), current, StringComparison.OrdinalIgnoreCase);
        }

        private string CurtainPanelOutputSignature()
        {
            var handles = OutputSignature(GeneratedCurtainPanelHandlesKey);
            if (handles.Length > 0) return handles;
            return Properties.TryGetValue(GeneratedCurtainPanelBuildStateKey, out var state) &&
                   string.Equals((state ?? string.Empty).Trim(), GeneratedCurtainPanelBuildCompleteValue, StringComparison.OrdinalIgnoreCase)
                ? "@COMPLETE_EMPTY"
                : string.Empty;
        }

        private bool IsGeneratedOutputStale(string outputKey, string stateKey, string snapshotKey)
        {
            if (!Properties.TryGetValue(stateKey, out var state) || !string.Equals(state, StaleValue, StringComparison.OrdinalIgnoreCase)) return false;
            var current = OutputSignature(outputKey);
            return Properties.TryGetValue(snapshotKey, out var snapshot) &&
                   !string.IsNullOrWhiteSpace(snapshot) &&
                   current.Length > 0 &&
                   string.Equals(snapshot.Trim(), current, StringComparison.OrdinalIgnoreCase);
        }

        private string OutputSignature(string outputKey)
        {
            if (!Properties.TryGetValue(outputKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return string.Join(";", raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        private void SetAggregateStaleReason(string reason)
        {
            Properties[GeneratedGeometryStateKey] = StaleValue;
            Properties[GeneratedGeometryStaleReasonKey] = string.IsNullOrWhiteSpace(reason) ? "Semantic/source state changed." : reason.Trim();
            UpdatedUtc = DateTime.UtcNow;
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
