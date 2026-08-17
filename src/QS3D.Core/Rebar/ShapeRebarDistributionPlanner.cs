using System;
using System.Collections.Generic;

namespace QS3D.Core.Rebar
{
    public sealed class ShapeRebarDistributionInput
    {
        public double Span { get; set; }
        public double Cover { get; set; }
        public double Radius { get; set; }
        public int Count { get; set; }
        public bool Centered { get; set; }
    }

    public sealed class ShapeRebarDistributionResult
    {
        public ShapeRebarDistributionResult(double centerClearance, IReadOnlyList<double> offsets)
        {
            CenterClearance = centerClearance;
            Offsets = new List<double>(offsets ?? throw new ArgumentNullException(nameof(offsets))).AsReadOnly();
        }

        public double CenterClearance { get; }
        public IReadOnlyList<double> Offsets { get; }
    }

    public static class ShapeRebarDistributionPlanner
    {
        private const int MaxBars = 10000;

        public static ShapeRebarDistributionResult Plan(ShapeRebarDistributionInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var span = RebarMath.Positive(input.Span, nameof(input.Span));
            var cover = RebarMath.NonNegative(input.Cover, nameof(input.Cover));
            var radius = RebarMath.Positive(input.Radius, nameof(input.Radius));
            if (input.Count <= 0 || input.Count > MaxBars) throw new ArgumentOutOfRangeException(nameof(input.Count));

            var clearance = RebarMath.Add(cover, radius, "shape rebar center clearance");
            var twoSideClearance = RebarMath.Multiply(2d, clearance, "shape rebar two-side clearance");
            var usable = SubtractFinite(span, twoSideClearance, "shape rebar usable span");
            if (usable < 0d) throw new InvalidOperationException("Cover + bar radius leaves no usable shape-rebar distribution span inside the host.");

            var offsets = new double[input.Count];
            if (input.Count == 1)
            {
                offsets[0] = input.Centered ? 0d : RebarMath.Divide(span, 2d, "shape rebar single offset");
                return new ShapeRebarDistributionResult(clearance, Array.AsReadOnly(offsets));
            }
            if (!(usable > 0d)) throw new InvalidOperationException("Multiple shape rebars require a positive usable distribution span.");

            var step = RebarMath.Divide(usable, input.Count - 1d, "shape rebar distribution step");
            var halfSpan = RebarMath.Divide(span, 2d, "shape rebar half span");
            for (var index = 0; index < input.Count; index++)
            {
                var edgeOffset = RebarMath.Add(clearance, RebarMath.Multiply(step, index, "shape rebar distribution index"), "shape rebar edge offset");
                offsets[index] = input.Centered
                    ? SubtractFinite(edgeOffset, halfSpan, "shape rebar centered offset")
                    : edgeOffset;
            }
            return new ShapeRebarDistributionResult(clearance, Array.AsReadOnly(offsets));
        }

        private static double SubtractFinite(double left, double right, string label)
        {
            if (double.IsNaN(left) || double.IsInfinity(left) || double.IsNaN(right) || double.IsInfinity(right))
                throw new ArgumentOutOfRangeException(label, "Shape rebar values must be finite.");
            var result = left - right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException("Shape rebar subtraction overflow: " + label);
            if (right != 0d && result == left) throw new OverflowException("Shape rebar subtraction lost a nonzero value: " + label);
            return result;
        }
    }
}
