using System;

namespace QS3D.Core.Domain
{
    public sealed class RoomPropertySet
    {
        private double _baseOffsetMm;
        private double _topOffsetMm;

        public string BottomLevel { get; set; } = "bottom_level";
        public string TopLevel { get; set; } = "top_level";
        public double BaseOffsetMm
        {
            get => _baseOffsetMm;
            set => _baseOffsetMm = RequireFinite(value, nameof(BaseOffsetMm));
        }
        public double TopOffsetMm
        {
            get => _topOffsetMm;
            set => _topOffsetMm = RequireFinite(value, nameof(TopOffsetMm));
        }
        public bool GenerateFloorFinish { get; set; } = true;
        public bool GenerateWaterproofing { get; set; } = true;
        public bool GenerateSkirting { get; set; } = true;
        public bool GenerateWallFinish { get; set; } = true;
        public bool GenerateCeilingFinish { get; set; } = true;

        private static double RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "Room metric must be finite.");
            return value == 0d ? 0d : value;
        }
    }
}
