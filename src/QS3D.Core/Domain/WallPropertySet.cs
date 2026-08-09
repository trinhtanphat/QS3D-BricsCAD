namespace QS3D.Core.Domain
{
    public sealed class WallPropertySet
    {
        public double ThicknessMm { get; set; } = 110d;
        public double AxisToLeftMm { get; set; }
        public double AxisToRightMm { get; set; }
        public bool CloseProfile { get; set; }
        public bool FreeformProfile { get; set; }
        public string TopLevel { get; set; } = "top_level";
        public string BottomLevel { get; set; } = "bottom_level";
        public double BaseOffsetMm { get; set; }
        public double TopOffsetMm { get; set; }
    }
}
