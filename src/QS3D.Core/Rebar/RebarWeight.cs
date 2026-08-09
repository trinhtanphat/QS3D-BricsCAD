using System;

namespace QS3D.Core.Rebar
{
    public static class RebarWeight
    {
        public static double KilogramsPerMeter(double diameterMm) { if (diameterMm <= 0d) throw new ArgumentOutOfRangeException(nameof(diameterMm)); return diameterMm * diameterMm / 162d; }
        public static double TotalKilograms(double diameterMm, double totalLengthMeters, double wastePercent = 0d)
        {
            if (totalLengthMeters < 0d) throw new ArgumentOutOfRangeException(nameof(totalLengthMeters));
            if (wastePercent < 0d) throw new ArgumentOutOfRangeException(nameof(wastePercent));
            return KilogramsPerMeter(diameterMm) * totalLengthMeters * (1d + wastePercent / 100d);
        }
    }
}
