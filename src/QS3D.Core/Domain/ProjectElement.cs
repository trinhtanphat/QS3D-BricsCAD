using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

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

        private readonly Dictionary<string, string> _properties;
        private ElementCategory _category;
        private string _familyId = string.Empty;
        private string _floorId = string.Empty;
        private string _zoneId = string.Empty;
        private string _drawingFingerprint = string.Empty;

        public ProjectElement(string id, ElementCategory category)
            : this(id, category, string.Empty, string.Empty, string.Empty)
        {
        }

        public ProjectElement(string id, ElementCategory category, string familyId, string floorId, string zoneId)
        {
            Id = RequireId(id);
            _category = RequireCategory(category);
            _familyId = NormalizeOptionalRelationId(familyId);
            _floorId = NormalizeOptionalRelationId(floorId);
            _zoneId = NormalizeOptionalRelationId(zoneId);
            SourceHandles = new List<string>();
            DependsOn = new List<string>();
            _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Properties = new ProjectElementPropertyDictionary(this, _properties);
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
                MarkDirtyCore(ElementDirtyFlags.All, true);
            }
        }
        public string FamilyId { get => _familyId; set => SetRelationId(ref _familyId, value); }
        public string FloorId { get => _floorId; set => SetRelationId(ref _floorId, value); }
        public string ZoneId { get => _zoneId; set => SetRelationId(ref _zoneId, value); }
        public string DrawingFingerprint
        {
            get => _drawingFingerprint;
            set
            {
                var next = NormalizeDrawingFingerprint(value);
                if (string.Equals(_drawingFingerprint, next, StringComparison.Ordinal)) return;
                _drawingFingerprint = next;
                MarkDirtyCore(ElementDirtyFlags.Relations, true);
            }
        }
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
            if ((Dirty & flags) == ElementDirtyFlags.None) return;
            Dirty &= ~flags;
            UpdatedUtc = DateTime.UtcNow;
        }

        public void SetProperty(string name, string value)
        {
            var key = RequirePropertyName(name);
            var normalized = RequireXmlText(value ?? string.Empty, nameof(value), "Property value");
            if (_properties.TryGetValue(key, out var existing) && string.Equals(existing, normalized, StringComparison.Ordinal)) return;
            _properties[key] = normalized;
            MarkPropertyChanged(key);
        }

        internal void AddProperty(string name, string value)
        {
            var key = RequirePropertyName(name);
            var normalized = RequireXmlText(value ?? string.Empty, nameof(value), "Property value");
            _properties.Add(key, normalized);
            MarkPropertyChanged(key);
        }

        internal bool RemoveProperty(string name)
        {
            var key = RequirePropertyName(name);
            if (!_properties.Remove(key)) return false;
            MarkPropertyChanged(key);
            return true;
        }

        internal void ClearProperties()
        {
            if (_properties.Count == 0) return;
            var affectsGeneratedGeometry = _properties.Keys.Any(key => ElementGeometryPolicy.AffectsGeneratedGeometry(Category, key));
            var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if (affectsGeneratedGeometry) flags |= ElementDirtyFlags.Geometry;
            _properties.Clear();
            MarkDirtyCore(flags, false);
        }

        public void SetQuantity(string name, double value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Quantity name is required.", nameof(name));
            if (name.Any(char.IsControl)) throw new ArgumentException("Quantity name cannot contain control characters.", nameof(name));
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new ArgumentOutOfRangeException(nameof(value));
            value = value == 0d ? 0d : value;
            var key = name.Trim();
            key = RequireXmlText(key, nameof(name), "Quantity name");
            if (Quantities.TryGetValue(key, out var existing) && existing.Equals(value)) return;
            Quantities[key] = value;
            MarkDirtyCore(ElementDirtyFlags.Quantity, false);
        }

        public void MarkGeneratedGeometryStale(string reason)
        {
            var normalizedReason = NormalizeStaleReason(reason);
            var changed = false;
            var hasOutput = false;
            bool outputPresent;
            changed |= MarkGeneratedOutputStale(GeneratedSolidHandleKey, GeneratedSolidStateKey, GeneratedSolidStaleSnapshotKey, out outputPresent); hasOutput |= outputPresent;
            changed |= MarkGeneratedOutputStale(GeneratedRebarHandlesKey, GeneratedRebarStateKey, GeneratedRebarStaleSnapshotKey, out outputPresent); hasOutput |= outputPresent;
            changed |= MarkGeneratedOutputStale(GeneratedShapeRebarHandlesKey, GeneratedShapeRebarStateKey, GeneratedShapeRebarStaleSnapshotKey, out outputPresent); hasOutput |= outputPresent;
            changed |= MarkGeneratedOutputStale(GeneratedTieRebarHandlesKey, GeneratedTieRebarStateKey, GeneratedTieRebarStaleSnapshotKey, out outputPresent); hasOutput |= outputPresent;
            changed |= MarkGeneratedOutputStale(GeneratedBeamStirrupHandlesKey, GeneratedBeamStirrupStateKey, GeneratedBeamStirrupStaleSnapshotKey, out outputPresent); hasOutput |= outputPresent;
            changed |= MarkGeneratedOutputStale(GeneratedSlabMeshHandlesKey, GeneratedSlabMeshStateKey, GeneratedSlabMeshStaleSnapshotKey, out outputPresent); hasOutput |= outputPresent;
            changed |= MarkGeneratedOutputStale(GeneratedWallMeshHandlesKey, GeneratedWallMeshStateKey, GeneratedWallMeshStaleSnapshotKey, out outputPresent); hasOutput |= outputPresent;
            changed |= MarkGeneratedOutputStale(GeneratedFoundationMeshHandlesKey, GeneratedFoundationMeshStateKey, GeneratedFoundationMeshStaleSnapshotKey, out outputPresent); hasOutput |= outputPresent;
            changed |= MarkGeneratedOutputStale(GeneratedCurtainFrameHandlesKey, GeneratedCurtainFrameStateKey, GeneratedCurtainFrameStaleSnapshotKey, out outputPresent); hasOutput |= outputPresent;
            changed |= MarkGeneratedCurtainPanelOutputStale(out outputPresent); hasOutput |= outputPresent;
            if (!hasOutput) return;
            changed |= SetAggregateStaleReason(normalizedReason);
            if (changed) UpdatedUtc = DateTime.UtcNow;
        }

        public void MarkGeneratedCurtainFrameStale(string reason)
        {
            var normalizedReason = NormalizeStaleReason(reason);
            var changed = MarkGeneratedOutputStale(
                GeneratedCurtainFrameHandlesKey,
                GeneratedCurtainFrameStateKey,
                GeneratedCurtainFrameStaleSnapshotKey,
                out var hasOutput);
            if (!hasOutput) return;
            changed |= SetAggregateStaleReason(normalizedReason);
            if (changed) UpdatedUtc = DateTime.UtcNow;
        }

        public void MarkGeneratedCurtainPanelStale(string reason)
        {
            var normalizedReason = NormalizeStaleReason(reason);
            var changed = MarkGeneratedCurtainPanelOutputStale(out var hasOutput);
            if (!hasOutput) return;
            changed |= SetAggregateStaleReason(normalizedReason);
            if (changed) UpdatedUtc = DateTime.UtcNow;
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
            var propertyCount = _properties.Count;
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
            if (_properties.Count != propertyCount) UpdatedUtc = DateTime.UtcNow;
        }

        internal void RestorePersistenceState(ElementDirtyFlags dirty, DateTime updatedUtc)
        {
            if ((dirty & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(dirty));
            if (updatedUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Element persistence timestamp must be UTC.", nameof(updatedUtc));
            Dirty = dirty;
            UpdatedUtc = updatedUtc;
        }

        internal void TouchPersistenceState()
        {
            UpdatedUtc = DateTime.UtcNow;
        }

        private void MarkPropertyChanged(string key)
        {
            var affectsGeneratedGeometry = ElementGeometryPolicy.AffectsGeneratedGeometry(Category, key);
            var affectsGeneratedOutput = ElementGeometryPolicy.AffectsGeneratedOutput(Category, key);
            var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if (affectsGeneratedGeometry) flags |= ElementDirtyFlags.Geometry;
            MarkDirtyCore(flags, affectsGeneratedOutput);
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

        private void SetRelationId(ref string field, string? value)
        {
            var next = NormalizeOptionalRelationId(value);
            if (string.Equals(field, next, StringComparison.Ordinal)) return;
            field = next;
            MarkDirtyCore(ElementDirtyFlags.Relations, true);
        }

        private static string RequirePropertyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Property name is required.", nameof(name));
            if (name.Any(char.IsControl)) throw new ArgumentException("Property name cannot contain control characters.", nameof(name));
            return RequireXmlText(name.Trim(), nameof(name), "Property name");
        }

        private static string NormalizeOptionalRelationId(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Element relation id cannot contain control characters.", nameof(value));
            return RequireXmlText(normalized, nameof(value), "Element relation id");
        }

        private static string NormalizeDrawingFingerprint(string? value)
        {
            var rawValue = value ?? string.Empty;
            if (rawValue.Any(char.IsControl)) throw new ArgumentException("Element drawing fingerprint cannot contain control characters.", nameof(value));
            return RequireXmlText(rawValue.Trim(), nameof(value), "Element drawing fingerprint");
        }

        private static string RequireId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Element id is required.", nameof(id));
            var normalized = id.Trim();
            if (normalized.Any(char.IsControl)) throw new ArgumentException("Element id cannot contain control characters.", nameof(id));
            return RequireXmlText(normalized, nameof(id), "Element id");
        }

        private static string RequireXmlText(string value, string parameterName, string label)
        {
            try
            {
                XmlConvert.VerifyXmlChars(value);
                return value;
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(label + " contains characters that are invalid in XML.", parameterName, ex);
            }
        }

        private static string NormalizeStaleReason(string? reason)
        {
            var normalized = string.IsNullOrWhiteSpace(reason) ? "Semantic/source state changed." : reason!.Trim();
            return RequireXmlText(normalized, nameof(reason), "Generated geometry stale reason");
        }

        private static ElementCategory RequireCategory(ElementCategory value)
        {
            if (!Enum.IsDefined(typeof(ElementCategory), value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Element category must be a defined ElementCategory.");
            return value;
        }

        private bool MarkGeneratedOutputStale(string outputKey, string stateKey, string snapshotKey, out bool hasOutput)
        {
            var signature = OutputSignature(outputKey);
            hasOutput = signature.Length > 0;
            if (!hasOutput) return false;

            var changed = false;
            if (!_properties.TryGetValue(stateKey, out var state) || !string.Equals(state, StaleValue, StringComparison.Ordinal))
            {
                _properties[stateKey] = StaleValue;
                changed = true;
            }
            if (!_properties.TryGetValue(snapshotKey, out var snapshot) ||
                !string.Equals(CanonicalHandleSignature(snapshot), signature, StringComparison.OrdinalIgnoreCase))
            {
                _properties[snapshotKey] = signature;
                changed = true;
            }
            return changed;
        }

        private bool MarkGeneratedCurtainPanelOutputStale(out bool hasOutput)
        {
            var signature = CurtainPanelOutputSignature();
            hasOutput = signature.Length > 0;
            if (!hasOutput) return false;

            var changed = false;
            if (!_properties.TryGetValue(GeneratedCurtainPanelStateKey, out var state) || !string.Equals(state, StaleValue, StringComparison.Ordinal))
            {
                _properties[GeneratedCurtainPanelStateKey] = StaleValue;
                changed = true;
            }
            if (!_properties.TryGetValue(GeneratedCurtainPanelStaleSnapshotKey, out var snapshot) ||
                !string.Equals(CanonicalHandleSignature(snapshot), signature, StringComparison.OrdinalIgnoreCase))
            {
                _properties[GeneratedCurtainPanelStaleSnapshotKey] = signature;
                changed = true;
            }
            return changed;
        }

        private bool IsGeneratedCurtainPanelOutputStale()
        {
            if (!_properties.TryGetValue(GeneratedCurtainPanelStateKey, out var state) || !string.Equals(state, StaleValue, StringComparison.OrdinalIgnoreCase)) return false;
            var current = CurtainPanelOutputSignature();
            return _properties.TryGetValue(GeneratedCurtainPanelStaleSnapshotKey, out var snapshot) &&
                   !string.IsNullOrWhiteSpace(snapshot) &&
                   current.Length > 0 &&
                   string.Equals(CanonicalHandleSignature(snapshot), current, StringComparison.OrdinalIgnoreCase);
        }

        private string CurtainPanelOutputSignature()
        {
            var handles = OutputSignature(GeneratedCurtainPanelHandlesKey);
            if (handles.Length > 0) return handles;
            return _properties.TryGetValue(GeneratedCurtainPanelBuildStateKey, out var state) &&
                   string.Equals((state ?? string.Empty).Trim(), GeneratedCurtainPanelBuildCompleteValue, StringComparison.OrdinalIgnoreCase)
                ? "@COMPLETE_EMPTY"
                : string.Empty;
        }

        private bool IsGeneratedOutputStale(string outputKey, string stateKey, string snapshotKey)
        {
            if (!_properties.TryGetValue(stateKey, out var state) || !string.Equals(state, StaleValue, StringComparison.OrdinalIgnoreCase)) return false;
            var current = OutputSignature(outputKey);
            return _properties.TryGetValue(snapshotKey, out var snapshot) &&
                   !string.IsNullOrWhiteSpace(snapshot) &&
                   current.Length > 0 &&
                   string.Equals(CanonicalHandleSignature(snapshot), current, StringComparison.OrdinalIgnoreCase);
        }

        private string OutputSignature(string outputKey)
        {
            if (!_properties.TryGetValue(outputKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return CanonicalHandleSignature(raw);
        }

        private static string CanonicalHandleSignature(string raw)
        {
            return string.Join(";", (raw ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(GeneratedHandleIdentity.Normalize).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        private bool SetAggregateStaleReason(string reason)
        {
            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Semantic/source state changed." : reason.Trim();
            var changed = false;
            if (!_properties.TryGetValue(GeneratedGeometryStateKey, out var state) || !string.Equals(state, StaleValue, StringComparison.Ordinal))
            {
                _properties[GeneratedGeometryStateKey] = StaleValue;
                changed = true;
            }
            if (!_properties.TryGetValue(GeneratedGeometryStaleReasonKey, out var existingReason) ||
                !string.Equals(existingReason, normalizedReason, StringComparison.Ordinal))
            {
                _properties[GeneratedGeometryStaleReasonKey] = normalizedReason;
                changed = true;
            }
            return changed;
        }

        private void ClearGeneratedOutputStale(string stateKey, string snapshotKey)
        {
            var propertyCount = _properties.Count;
            Remove(stateKey); Remove(snapshotKey); ClearAggregateStaleIfResolved();
            if (_properties.Count != propertyCount) UpdatedUtc = DateTime.UtcNow;
        }

        private void ClearAggregateStaleIfResolved()
        {
            if (IsGeneratedGeometryStale()) return;
            Remove(GeneratedGeometryStateKey); Remove(GeneratedGeometryStaleReasonKey);
        }

        private void Remove(string key)
        {
            _properties.Remove(key);
        }
    }
}
