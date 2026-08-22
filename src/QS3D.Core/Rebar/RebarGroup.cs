using System.Globalization;

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
            var diameter = DiameterMm.ToString("R", CultureInfo.InvariantCulture);
            if (SpacingMm.HasValue) return "D" + diameter + "@" + SpacingMm.Value.ToString("R", CultureInfo.InvariantCulture);
            if (Sets.HasValue && BarsPerSet.HasValue) return Sets.Value.ToString(CultureInfo.InvariantCulture) + "x" + BarsPerSet.Value.ToString(CultureInfo.InvariantCulture) + "D" + diameter;
            if (Quantity.HasValue) return Quantity.Value.ToString(CultureInfo.InvariantCulture) + "D" + diameter;
            return "D" + diameter;
        }
    }
}
