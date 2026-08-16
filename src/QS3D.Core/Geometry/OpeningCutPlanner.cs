using System;

namespace QS3D.Core.Geometry
{
    public sealed class OpeningCutInput
    {
        public double HostLengthM { get; set; }
        public double HostThicknessM { get; set; }
        public double HostHeightM { get; set; }
        public double OpeningWidthM { get; set; }
        public double OpeningHeightM { get; set; }
        public double SillHeightM { get; set; }
        public double CenterAlongHostM { get; set; }
        public double ClearanceM { get; set; } = 0.01d;
    }

    public sealed class OpeningCutPlan
    {
        public double StartAlongHostM { get; set; }
        public double EndAlongHostM { get; set; }
        public double CutterWidthM { get; set; }
        public double CutterDepthM { get; set; }
        public double CutterHeightM { get; set; }
        public double BaseElevationM { get; set; }
        public double TopElevationM { get; set; }
        public double CenterAlongHostM { get; set; }
        public double CenterElevationM { get; set; }
    }

    public static class OpeningCutPlanner
    {
        public static OpeningCutPlan Plan(OpeningCutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            Positive(input.HostLengthM, nameof(input.HostLengthM));
            Positive(input.HostThicknessM, nameof(input.HostThicknessM));
            Positive(input.HostHeightM, nameof(input.HostHeightM));
            Positive(input.OpeningWidthM, nameof(input.OpeningWidthM));
            Positive(input.OpeningHeightM, nameof(input.OpeningHeightM));
            NonNegative(input.SillHeightM, nameof(input.SillHeightM));
            NonNegative(input.ClearanceM, nameof(input.ClearanceM));
            Finite(input.CenterAlongHostM, nameof(input.CenterAlongHostM));

            var halfWidth = input.OpeningWidthM / 2d;
            Finite(halfWidth, nameof(halfWidth));
            if (halfWidth == 0d)
                throw new OverflowException("Opening half-width underflowed to zero.");

            var start = input.CenterAlongHostM - halfWidth;
            var end = input.CenterAlongHostM + halfWidth;
            Finite(start, nameof(start));
            Finite(end, nameof(end));
            if (!(start < input.CenterAlongHostM) || !(end > input.CenterAlongHostM))
                throw new OverflowException("Opening width cannot be represented at the requested center coordinate.");
            if (start < 0d || end > input.HostLengthM)
                throw new InvalidOperationException("Opening width/position extends beyond the host wall length.");

            var openingTop = Add(input.SillHeightM, input.OpeningHeightM, "opening top");
            if (openingTop > input.HostHeightM)
                throw new InvalidOperationException("Opening height/sill extends above the host wall height.");

            var doubledClearance = Multiply(input.ClearanceM, 2d, "opening cutter clearance");
            var cutterWidth = Add(input.OpeningWidthM, doubledClearance, "cutter width");
            var cutterDepth = Add(input.HostThicknessM, doubledClearance, "cutter depth");
            var cutterHeight = Add(input.OpeningHeightM, doubledClearance, "cutter height");
            var baseElevation = input.SillHeightM - input.ClearanceM;
            Finite(baseElevation, nameof(baseElevation));
            var topElevation = Add(openingTop, input.ClearanceM, "cutter top");

            if (input.ClearanceM > 0d)
            {
                RequireStrictExpansion(input.OpeningWidthM, cutterWidth, "cutter width");
                RequireStrictExpansion(input.HostThicknessM, cutterDepth, "cutter depth");
                RequireStrictExpansion(input.OpeningHeightM, cutterHeight, "cutter height");
                if (!(baseElevation < input.SillHeightM))
                    throw new OverflowException("Positive cutter clearance cannot be represented below the opening sill.");
                if (!(topElevation > openingTop))
                    throw new OverflowException("Positive cutter clearance cannot be represented above the opening top.");
            }

            var centerElevation = Midpoint(baseElevation, topElevation, "cutter center elevation");

            return new OpeningCutPlan
            {
                StartAlongHostM = start,
                EndAlongHostM = end,
                CutterWidthM = cutterWidth,
                CutterDepthM = cutterDepth,
                CutterHeightM = cutterHeight,
                BaseElevationM = baseElevation,
                TopElevationM = topElevation,
                CenterAlongHostM = input.CenterAlongHostM,
                CenterElevationM = centerElevation
            };
        }

        private static void RequireStrictExpansion(double original, double expanded, string label)
        {
            if (!(expanded > original))
                throw new OverflowException("Positive clearance for " + label + " was lost to floating-point precision.");
        }

        private static double Midpoint(double a, double b, string label)
        {
            Finite(a, label); Finite(b, label);
            var value = a + (b - a) / 2d;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }

        private static double Add(double left, double right, string label)
        {
            Finite(left, label); Finite(right, label);
            var value = left + right;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " overflowed.");
            return value;
        }

        private static double Multiply(double left, double right, string label)
        {
            Finite(left, label); Finite(right, label);
            var value = left * right;
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new OverflowException(label + " overflowed.");
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

        private static void Finite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name);
        }
    }
}
