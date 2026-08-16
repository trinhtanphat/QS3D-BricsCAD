using System;
using System.Collections.Generic;
using QS3D.Core.Geometry;

namespace QS3D.Core.Rebar
{
    public sealed class RectangularRebarLayoutInput
    {
        public double WidthM { get; set; }
        public double DepthM { get; set; }
        public double CoverM { get; set; }
        public double DiameterMm { get; set; }
        public int BarsAlongWidth { get; set; }
        public int BarsAlongDepth { get; set; }
    }

    public sealed class RectangularRebarLayout
    {
        public RectangularRebarLayout(IReadOnlyList<Point2> barCenters, double clearHalfWidthM, double clearHalfDepthM)
        {
            BarCenters = new List<Point2>(barCenters ?? throw new ArgumentNullException(nameof(barCenters))).AsReadOnly();
            ClearHalfWidthM = clearHalfWidthM;
            ClearHalfDepthM = clearHalfDepthM;
        }

        public IReadOnlyList<Point2> BarCenters { get; }
        public double ClearHalfWidthM { get; }
        public double ClearHalfDepthM { get; }
    }

    public static class RectangularRebarLayoutPlanner
    {
        private const int MaxBars = 10000;

        public static RectangularRebarLayout Plan(RectangularRebarLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            Positive(input.WidthM, nameof(input.WidthM));
            Positive(input.DepthM, nameof(input.DepthM));
            NonNegative(input.CoverM, nameof(input.CoverM));
            Positive(input.DiameterMm, nameof(input.DiameterMm));
            if (input.BarsAlongWidth < 2) throw new ArgumentOutOfRangeException(nameof(input.BarsAlongWidth));
            if (input.BarsAlongDepth < 2) throw new ArgumentOutOfRangeException(nameof(input.BarsAlongDepth));

            var projectedBars = 2L * input.BarsAlongWidth + 2L * Math.Max(0, input.BarsAlongDepth - 2);
            if (projectedBars > MaxBars) throw new InvalidOperationException("Rectangular rebar layout exceeds the supported bar count.");

            var diameterM = RebarMath.Divide(input.DiameterMm, 1000d, "rectangular rebar diameter");
            var radiusM = RebarMath.Divide(diameterM, 2d, "rectangular rebar radius");
            var halfWidth = input.WidthM / 2d - input.CoverM - radiusM;
            var halfDepth = input.DepthM / 2d - input.CoverM - radiusM;
            if (!(halfWidth > 0d) || !(halfDepth > 0d)) throw new InvalidOperationException("Cover + bar radius leaves no usable reinforcement envelope inside the host section.");

            var widthSpacingM = Finite((2d * halfWidth) / (input.BarsAlongWidth - 1d), "rectangular rebar width spacing");
            var depthSpacingM = Finite((2d * halfDepth) / (input.BarsAlongDepth - 1d), "rectangular rebar depth spacing");
            if (widthSpacingM + 1e-12d < diameterM)
                throw new InvalidOperationException("Rectangular rebar centers along width are closer than one bar diameter.");
            if (depthSpacingM + 1e-12d < diameterM)
                throw new InvalidOperationException("Rectangular rebar centers along depth are closer than one bar diameter.");

            var points = new List<Point2>((int)projectedBars);
            for (var i = 0; i < input.BarsAlongWidth; i++)
            {
                var x = Interpolate(-halfWidth, halfWidth, i, input.BarsAlongWidth);
                points.Add(new Point2(x, -halfDepth));
                points.Add(new Point2(x, halfDepth));
            }

            for (var i = 1; i < input.BarsAlongDepth - 1; i++)
            {
                var y = Interpolate(-halfDepth, halfDepth, i, input.BarsAlongDepth);
                points.Add(new Point2(-halfWidth, y));
                points.Add(new Point2(halfWidth, y));
            }

            return new RectangularRebarLayout(points.AsReadOnly(), halfWidth, halfDepth);
        }

        private static double Interpolate(double start, double end, int index, int count)
        {
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count));
            var t = index / (double)(count - 1);
            var value = start + (end - start) * t;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException("Rebar layout interpolation overflowed.");
            return value;
        }

        private static void Positive(double value, string name)
        {
            Finite(value, name);
            if (value <= 0d) throw new ArgumentOutOfRangeException(name);
        }

        private static void NonNegative(double value, string name)
        {
            Finite(value, name);
            if (value < 0d) throw new ArgumentOutOfRangeException(name);
        }

        private static double Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name);
            return value;
        }
    }
}
