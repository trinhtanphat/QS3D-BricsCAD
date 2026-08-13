using System;

namespace QS3D.Core.Domain
{
    public sealed class WallPropertySet
    {
        private double _thicknessMm = 110d;
        private double _axisToLeftMm;
        private double _axisToRightMm;
        private double _baseOffsetMm;
        private double _topOffsetMm;

        public double ThicknessMm
        {
            get => _thicknessMm;
            set => _thicknessMm = RequirePositiveFinite(value, nameof(ThicknessMm));
        }
        public double AxisToLeftMm
        {
            get => _axisToLeftMm;
            set => _axisToLeftMm = RequireFinite(value, nameof(AxisToLeftMm));
        }
        public double AxisToRightMm
        {
            get => _axisToRightMm;
            set => _axisToRightMm = RequireFinite(value, nameof(AxisToRightMm));
        }
        public bool CloseProfile { get; set; }
        public bool FreeformProfile { get; set; }
        public string TopLevel { get; set; } = "top_level";
        public string BottomLevel { get; set; } = "bottom_level";
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

        private static double RequirePositiveFinite(double value, string parameterName)
        {
            var finite = RequireFinite(value, parameterName);
            if (finite <= 0d)
                throw new ArgumentOutOfRangeException(parameterName, "Wall physical thickness must be greater than zero.");
            return finite;
        }

        private static double RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "Wall metric must be finite.");
            return value == 0d ? 0d : value;
        }
    }
}
