using System;
using System.Collections.Generic;

namespace QS3D.Core.Model
{
    public sealed class EntitySnapshot
    {
        private string _layer = string.Empty;
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
        public string Layer
        {
            get => _layer;
            set => _layer = value ?? string.Empty;
        }
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
            if (!value.HasValue) return null;
            var metric = value.Value;
            if (double.IsNaN(metric) || double.IsInfinity(metric) || metric < 0d)
                throw new ArgumentOutOfRangeException(parameterName, "Entity snapshot metric must be finite and non-negative when provided.");
            return metric == 0d ? 0d : metric;
        }
    }
}
