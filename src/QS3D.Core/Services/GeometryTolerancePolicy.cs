using System;

namespace QS3D.Core.Services
{
    public sealed class GeometryTolerancePolicy
    {
        public GeometryTolerancePolicy(double pointToleranceM = 0.0005, double boundaryGapToleranceM = 0.002, double tinySegmentM = 0.001)
        {
            if (!IsFinitePositive(pointToleranceM)) throw new ArgumentOutOfRangeException(nameof(pointToleranceM));
            if (!IsFinitePositive(boundaryGapToleranceM) || boundaryGapToleranceM < pointToleranceM) throw new ArgumentOutOfRangeException(nameof(boundaryGapToleranceM));
            if (!IsFinitePositive(tinySegmentM)) throw new ArgumentOutOfRangeException(nameof(tinySegmentM));
            PointToleranceM = pointToleranceM;
            BoundaryGapToleranceM = boundaryGapToleranceM;
            TinySegmentM = tinySegmentM;
        }

        public double PointToleranceM { get; }
        public double BoundaryGapToleranceM { get; }
        public double TinySegmentM { get; }
        public bool NearlyEqual(double a, double b) => Math.Abs(a - b) <= PointToleranceM;
        public bool CanAutoClose(double gapM) => gapM >= 0 && gapM <= BoundaryGapToleranceM;
        public bool IsTiny(double lengthM) => lengthM >= 0 && lengthM <= TinySegmentM;

        private static bool IsFinitePositive(double value) => value > 0d && !double.IsInfinity(value);
    }
}
