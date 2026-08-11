using System;
using System.Collections.Generic;

namespace QS3D.Core.Model
{
    public sealed class EntitySnapshot
    {
        public EntitySnapshot(string handle, string entityType, string layer)
        {
            if (string.IsNullOrWhiteSpace(handle)) throw new ArgumentException("Handle is required.", nameof(handle));
            if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            Handle = handle; EntityType = entityType; Layer = layer ?? string.Empty;
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        public string Handle { get; }
        public string EntityType { get; }
        public string Layer { get; set; }
        public double? LengthDrawingUnits { get; set; }
        public double? AreaDrawingUnitsSquared { get; set; }
        public double? SurfaceAreaDrawingUnitsSquared { get; set; }
        public double? VolumeDrawingUnitsCubed { get; set; }
        public bool HasQs3dGeneratedOwnershipMarker { get; set; }
        public IDictionary<string, string> Metadata { get; }
    }
}
