namespace QS3D.Core.Rebar
{
    public sealed class RebarGroup
    {
        public int? Quantity { get; set; }
        public int? Sets { get; set; }
        public int? BarsPerSet { get; set; }
        public double DiameterMm { get; set; }
        public double? SpacingMm { get; set; }
        public override string ToString()
        {
            if (SpacingMm.HasValue) return $"D{DiameterMm:g}@{SpacingMm.Value:g}";
            if (Sets.HasValue && BarsPerSet.HasValue) return $"{Sets.Value}x{BarsPerSet.Value}D{DiameterMm:g}";
            if (Quantity.HasValue) return $"{Quantity.Value}D{DiameterMm:g}";
            return $"D{DiameterMm:g}";
        }
    }
}
