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
            Handle = CanonicalIdentifier(handle, nameof(handle), "Handle is required.", "Handle must not contain control characters.");
            EntityType = CanonicalIdentifier(entityType, nameof(entityType), "Entity type is required.", "Entity type must not contain control characters.");
            Layer = layer ?? string.Empty;
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

        private static string CanonicalIdentifier(string value, string parameterName, string requiredMessage, string controlMessage)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(requiredMessage, parameterName);
            var canonical = value.Trim();
            for (var index = 0; index < canonical.Length; index++)
            {
                if (char.IsControl(canonical[index]))
                    throw new ArgumentException(controlMessage, parameterName);
            }
            return canonical;
        }

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
