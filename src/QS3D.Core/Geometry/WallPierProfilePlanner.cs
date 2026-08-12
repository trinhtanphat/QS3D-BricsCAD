using System;

namespace QS3D.Core.Geometry
{
    public enum WallPierProfileMode
    {
        Rectangular = 0,
        Chamfered = 1
    }

    public sealed class WallPierProfileInput
    {
        public WallPierProfileMode Mode { get; set; } = WallPierProfileMode.Rectangular;
        public double WidthM { get; set; }
        public double DepthM { get; set; }
        public double HeightM { get; set; }
        public double ChamferM { get; set; }
    }

    public sealed class WallPierProfile
    {
        internal WallPierProfile(
            WallPierProfileMode mode,
            double widthM,
            double depthM,
            double heightM,
            double chamferM,
            double crossSectionAreaM2,
            double crossSectionPerimeterM,
            double volumeM3,
            double lateralAreaM2)
        {
            Mode = mode;
            WidthM = widthM;
            DepthM = depthM;
            HeightM = heightM;
            ChamferM = chamferM;
            CrossSectionAreaM2 = crossSectionAreaM2;
            CrossSectionPerimeterM = crossSectionPerimeterM;
            VolumeM3 = volumeM3;
            LateralAreaM2 = lateralAreaM2;
        }

        public WallPierProfileMode Mode { get; }
        public double WidthM { get; }
        public double DepthM { get; }
        public double HeightM { get; }
        public double ChamferM { get; }
        public double CrossSectionAreaM2 { get; }
        public double CrossSectionPerimeterM { get; }
        public double VolumeM3 { get; }
        public double LateralAreaM2 { get; }
    }

    public static class WallPierProfilePlanner
    {
        public static WallPierProfile Plan(WallPierProfileInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var width = Positive(input.WidthM, nameof(input.WidthM));
            var depth = Positive(input.DepthM, nameof(input.DepthM));
            var height = Positive(input.HeightM, nameof(input.HeightM));

            switch (input.Mode)
            {
                case WallPierProfileMode.Rectangular:
                    return BuildRectangular(width, depth, height);
                case WallPierProfileMode.Chamfered:
                    return BuildChamfered(width, depth, height, Positive(input.ChamferM, nameof(input.ChamferM)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(input.Mode), "Unsupported wall-pier profile mode: " + input.Mode);
            }
        }

        private static WallPierProfile BuildRectangular(double width, double depth, double height)
        {
            var area = Multiply(width, depth, "wall-pier rectangular cross-section area");
            var perimeter = Multiply(2d, Add(width, depth, "wall-pier rectangular half perimeter"), "wall-pier rectangular perimeter");
            var volume = Multiply(area, height, "wall-pier rectangular volume");
            var lateral = Multiply(perimeter, height, "wall-pier rectangular lateral area");
            return new WallPierProfile(WallPierProfileMode.Rectangular, width, depth, height, 0d, area, perimeter, volume, lateral);
        }

        private static WallPierProfile BuildChamfered(double width, double depth, double height, double chamfer)
        {
            var maximumChamfer = Math.Min(width, depth) / 2d;
            if (!(chamfer < maximumChamfer))
                throw new InvalidOperationException("Wall-pier chamfer must be smaller than half the minimum profile dimension.");

            // Four right-triangle corners are removed: 4 * (c*c/2) = 2*c*c.
            var rectangleArea = Multiply(width, depth, "wall-pier chamfer base area");
            var removedArea = Multiply(2d, Multiply(chamfer, chamfer, "wall-pier chamfer square"), "wall-pier chamfer removed area");
            var area = SubtractPositive(rectangleArea, removedArea, "wall-pier chamfer cross-section area");

            // Each corner replaces two c-long orthogonal segments with one c*sqrt(2) diagonal.
            var rectanglePerimeter = Multiply(2d, Add(width, depth, "wall-pier chamfer half perimeter"), "wall-pier chamfer base perimeter");
            var removedPerimeter = Multiply(8d, chamfer, "wall-pier chamfer removed perimeter");
            var diagonalPerimeter = Multiply(4d * Math.Sqrt(2d), chamfer, "wall-pier chamfer diagonal perimeter");
            var perimeter = Add(SubtractPositive(rectanglePerimeter, removedPerimeter, "wall-pier chamfer reduced perimeter"), diagonalPerimeter, "wall-pier chamfer perimeter");
            var volume = Multiply(area, height, "wall-pier chamfer volume");
            var lateral = Multiply(perimeter, height, "wall-pier chamfer lateral area");
            return new WallPierProfile(WallPierProfileMode.Chamfered, width, depth, height, chamfer, area, perimeter, volume, lateral);
        }

        private static double Positive(double value, string label)
        {
            value = Finite(value, label);
            if (!(value > 0d)) throw new ArgumentOutOfRangeException(label, "Value must be greater than zero.");
            return value;
        }

        private static double Add(double left, double right, string label)
        {
            var result = Finite(left, label + " left") + Finite(right, label + " right");
            return Finite(result, label);
        }

        private static double Multiply(double left, double right, string label)
        {
            left = Finite(left, label + " left");
            right = Finite(right, label + " right");
            var result = Finite(left * right, label);
            if (left != 0d && right != 0d && result == 0d)
                throw new OverflowException(label + " underflowed below the representable positive range.");
            return result;
        }

        private static double SubtractPositive(double left, double right, string label)
        {
            var result = Finite(left, label + " left") - Finite(right, label + " right");
            result = Finite(result, label);
            if (!(result > 0d)) throw new InvalidOperationException(label + " must remain positive.");
            return result;
        }

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " must be finite.");
            return value;
        }
    }
}
