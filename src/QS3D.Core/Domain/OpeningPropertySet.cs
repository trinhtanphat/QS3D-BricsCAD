namespace QS3D.Core.Domain
{
    public sealed class OpeningPropertySet
    {
        public double WidthMm { get; set; } = 900d;
        public double HeightMm { get; set; } = 2200d;
        public double ThicknessMm { get; set; } = 110d;
        public string BottomLevel { get; set; } = "bottom_level";
        public double SillOffsetMm { get; set; }
    }
}
