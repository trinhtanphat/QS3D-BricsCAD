using System;

namespace QS3D.Core.Domain
{
    public sealed class OpeningPropertySet
    {
        private double _widthMm = 900d;
        private double _heightMm = 2200d;
        private double _thicknessMm = 110d;
        private double _sillOffsetMm;

        public double WidthMm
        {
            get => _widthMm;
            set => _widthMm = RequireFinite(value, nameof(WidthMm));
        }
        public double HeightMm
        {
            get => _heightMm;
            set => _heightMm = RequireFinite(value, nameof(HeightMm));
        }
        public double ThicknessMm
        {
            get => _thicknessMm;
            set => _thicknessMm = RequireFinite(value, nameof(ThicknessMm));
        }
        public string BottomLevel { get; set; } = "bottom_level";
        public double SillOffsetMm
        {
            get => _sillOffsetMm;
            set => _sillOffsetMm = RequireFinite(value, nameof(SillOffsetMm));
        }

        private static double RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, "Opening metric must be finite.");
            return value;
        }
    }
}
