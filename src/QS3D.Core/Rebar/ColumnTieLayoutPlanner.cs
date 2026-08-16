using System;
using System.Collections.Generic;
using QS3D.Core.Geometry;

namespace QS3D.Core.Rebar
{
    public sealed class ColumnTieLayoutInput
    {
        public double WidthM { get; set; }
        public double DepthM { get; set; }
        public double HeightM { get; set; }
        public double CoverM { get; set; }
        public double DiameterMm { get; set; }
        public double SpacingMm { get; set; }
        public double BottomClearanceM { get; set; }
        public double TopClearanceM { get; set; }
    }

    public sealed class ColumnTieLayout
    {
        public ColumnTieLayout(IReadOnlyList<Point2> closedPath, IReadOnlyList<double> elevationsM, double actualSpacingM, double pathPerimeterM)
        {
            if (closedPath == null) throw new ArgumentNullException(nameof(closedPath));
            if (elevationsM == null) throw new ArgumentNullException(nameof(elevationsM));
            ClosedPath = new List<Point2>(closedPath).AsReadOnly();
            ElevationsM = new List<double>(elevationsM).AsReadOnly();
            ActualSpacingM = actualSpacingM;
            PathPerimeterM = pathPerimeterM;
        }

        public IReadOnlyList<Point2> ClosedPath { get; }
        public IReadOnlyList<double> ElevationsM { get; }
        public double ActualSpacingM { get; }
        public double PathPerimeterM { get; }
    }

    public static class ColumnTieLayoutPlanner
    {
        private const int MaxTies = 5000;

        public static ColumnTieLayout Plan(ColumnTieLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            Positive(input.WidthM, nameof(input.WidthM));
            Positive(input.DepthM, nameof(input.DepthM));
            Positive(input.HeightM, nameof(input.HeightM));
            NonNegative(input.CoverM, nameof(input.CoverM));
            Positive(input.DiameterMm, nameof(input.DiameterMm));
            Positive(input.SpacingMm, nameof(input.SpacingMm));
            NonNegative(input.BottomClearanceM, nameof(input.BottomClearanceM));
            NonNegative(input.TopClearanceM, nameof(input.TopClearanceM));

            var diameterM = RebarMath.Divide(input.DiameterMm, 1000d, "column tie diameter");
            var radiusM = RebarMath.Divide(diameterM, 2d, "column tie radius");
            var halfWidth = input.WidthM / 2d - input.CoverM - radiusM;
            var halfDepth = input.DepthM / 2d - input.CoverM - radiusM;
            if (!(halfWidth > 0d) || !(halfDepth > 0d))
                throw new InvalidOperationException("Cover + tie radius leaves no usable tie envelope inside the column section.");

            var start = input.BottomClearanceM + input.CoverM + radiusM;
            var end = input.HeightM - input.TopClearanceM - input.CoverM - radiusM;
            Finite(start, "tie start elevation");
            Finite(end, "tie end elevation");
            if (end < start) throw new InvalidOperationException("Column height/clearances leave no usable vertical tie range.");

            var requestedSpacingM = input.SpacingMm / 1000d;
            var usable = end - start;
            int tieCount;
            double actualSpacing;
            if (usable <= 1e-12d)
            {
                tieCount = 1;
                actualSpacing = 0d;
            }
            else
            {
                var intervalRatio = RebarMath.Divide(usable, requestedSpacingM, "column tie spacing intervals");
                var intervalCountRaw = RebarMath.CeilingNearInteger(intervalRatio, "column tie spacing intervals");
                if (double.IsNaN(intervalCountRaw) || double.IsInfinity(intervalCountRaw) || intervalCountRaw >= MaxTies)
                    throw new InvalidOperationException("Column tie layout exceeds the supported tie count.");
                var intervals = Math.Max(1, (int)intervalCountRaw);
                tieCount = checked(intervals + 1);
                if (tieCount > MaxTies) throw new InvalidOperationException("Column tie layout exceeds the supported tie count.");
                actualSpacing = usable / intervals;
                Finite(actualSpacing, nameof(actualSpacing));
                if (actualSpacing > requestedSpacingM + 1e-12d)
                    throw new InvalidOperationException("Computed tie spacing exceeds the requested maximum spacing.");
                if (actualSpacing + 1e-12d < diameterM)
                    throw new InvalidOperationException("Column tie centers are closer than one tie diameter.");
            }

            var elevations = new List<double>(tieCount);
            for (var i = 0; i < tieCount; i++)
            {
                var elevation = tieCount == 1 ? start : start + actualSpacing * i;
                Finite(elevation, "tie elevation");
                elevations.Add(elevation);
            }
            if (tieCount > 1) elevations[elevations.Count - 1] = end;

            var path = new List<Point2>
            {
                new Point2(-halfWidth, -halfDepth),
                new Point2( halfWidth, -halfDepth),
                new Point2( halfWidth,  halfDepth),
                new Point2(-halfWidth,  halfDepth),
                new Point2(-halfWidth, -halfDepth)
            };
            var perimeter = 4d * (halfWidth + halfDepth);
            Finite(perimeter, nameof(perimeter));
            if (!(perimeter > 0d)) throw new InvalidOperationException("Tie path perimeter is degenerate.");

            return new ColumnTieLayout(path.AsReadOnly(), elevations.AsReadOnly(), actualSpacing, perimeter);
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

        private static void Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name);
        }
    }
}
