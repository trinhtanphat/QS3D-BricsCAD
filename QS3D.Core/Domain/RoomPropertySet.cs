namespace QS3D.Core.Domain
{
    public sealed class RoomPropertySet
    {
        public string BottomLevel { get; set; } = "bottom_level";
        public string TopLevel { get; set; } = "top_level";
        public double BaseOffsetMm { get; set; }
        public double TopOffsetMm { get; set; }
        public bool GenerateFloorFinish { get; set; } = true;
        public bool GenerateWaterproofing { get; set; } = true;
        public bool GenerateSkirting { get; set; } = true;
        public bool GenerateWallFinish { get; set; } = true;
        public bool GenerateCeilingFinish { get; set; } = true;
    }
}
