namespace QS3D.Core.Rebar
{
    public static class RebarWeight
    {
        public static double KilogramsPerMeter(double diameterMm)
        {
            var diameter = RebarMath.Positive(diameterMm, nameof(diameterMm));
            return RebarMath.Divide(RebarMath.Multiply(diameter, diameter, nameof(diameterMm)), 162d, nameof(diameterMm));
        }

        public static double TotalKilograms(double diameterMm, double totalLengthMeters, double wastePercent = 0d)
        {
            var length = RebarMath.NonNegative(totalLengthMeters, nameof(totalLengthMeters));
            var waste = RebarMath.NonNegative(wastePercent, nameof(wastePercent));
            var net = RebarMath.Multiply(KilogramsPerMeter(diameterMm), length, "rebar net weight");
            var wasteFactor = RebarMath.Add(1d, RebarMath.Divide(waste, 100d, nameof(wastePercent)), nameof(wastePercent));
            return RebarMath.Multiply(net, wasteFactor, "rebar total weight");
        }
    }
}
