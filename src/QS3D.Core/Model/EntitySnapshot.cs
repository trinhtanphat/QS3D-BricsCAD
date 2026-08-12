using System;
using System.Collections.Generic;

namespace QS3D.Core.Model
{
    public sealed class EntitySnapshot
    {
        private double? _lengthDrawingUnits;
        private double? _areaDrawingUnitsSquared;
        private double? _surfaceAreaDrawingUnitsSquared;
        private double? _volumeDrawingUnitsCubed;

        public EntitySnapshot(string handle, string entityType, string layer)
        {
            if (string.IsNullOrWhiteSpace(handle)) throw new ArgumentException("Handle is required.", nameof(handle));
            if (string.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            Handle = handle.Trim(); EntityType = entityType.Trim(); Layer = layer ?? string.Empty;
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        public string Handle { get; }
        public string EntityType { get; }
        public string Layer { get; set; }
        public double? LengthDrawingUnits
        {
            get => _lengthDrawingUnits;
            set => _lengthDrawingUnits = RequireFinite(value, nameof(LengthDrawingUnits));
        }
        public double? AreaDrawingUnitsSquared
        {
            get => _areaDrawingUnitsSquared;
            set => _areaDrawingUnitsSquared = RequireFinite(value, nameof(AreaDrawingUnitsSquared));
        }
        public double? SurfaceAreaDrawingUnitsSquared
        {
            get => _surfaceAreaDrawingUnitsSquared;
            set => _surfaceAreaDrawingUnitsSquared = RequireFinite(value, nameof(SurfaceAreaDrawingUnitsSquared));
        }
        public double? VolumeDrawingUnitsCubed
        {
            get => _volumeDrawingUnitsCubed;
            set => _volumeDrawingUnitsCubed = RequireFinite(value, nameof(VolumeDrawingUnitsCubed));
        }
        public bool HasQs3dGeneratedOwnershipMarker { get; set; }
        public IDictionary<string, string> Metadata { get; }

        private static double? RequireFinite(double? value, string parameterName)
        {
            if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value)))
                throw new ArgumentOutOfRangeException(parameterName, "Entity snapshot metric must be finite when provided.");
            return value;
        }
    }
}
