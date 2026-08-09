using System;

namespace QS3D.Core.Services
{
    public sealed class GeometryTolerancePolicy
    {
        public GeometryTolerancePolicy(double pointToleranceM = 0.0005, double boundaryGapToleranceM = 0.002, double tinySegmentM = 0.001)
        {
            if (pointToleranceM <= 0) throw new ArgumentOutOfRangeException(nameof(pointToleranceM));
            if (boundaryGapToleranceM < pointToleranceM) throw new ArgumentOutOfRangeException(nameof(boundaryGapToleranceM));
            if (tinySegmentM <= 0) throw new ArgumentOutOfRangeException(nameof(tinySegmentM));
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
    }
}
