using System;
using System.Collections.Generic;

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
            Properties[name.Trim()] = value ?? string.Empty;
            MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
        }

        public void SetQuantity(string name, double value)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Quantity name is required.", nameof(name));
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Quantities[name.Trim()] = value;
            UpdatedUtc = DateTime.UtcNow;
        }

        internal void RestorePersistenceState(ElementDirtyFlags dirty, DateTime updatedUtc)
        {
            if ((dirty & ~ElementDirtyFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(dirty));
            Dirty = dirty;
            UpdatedUtc = updatedUtc.Kind == DateTimeKind.Utc ? updatedUtc : updatedUtc.ToUniversalTime();
        }
    }
}
