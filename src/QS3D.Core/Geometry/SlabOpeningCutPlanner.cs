using System;

namespace QS3D.Core.Geometry
{
    public sealed class SlabOpeningCutInput
    {
        public double HostBottomM { get; set; }
        public double HostThicknessM { get; set; }
        public double ClearanceM { get; set; }
    }

    public sealed class SlabOpeningCutPlan
    {
        public double CutterTopM { get; set; }
        public double CutterBottomM { get; set; }
        public double CutterHeightM { get; set; }
        public double ExtrusionZM { get; set; }
    }

    /// <summary>
    /// Plans a slab cutter from the top face downward. Positive clearance is mandatory so the
    /// cutter starts above the slab and finishes below its bottom, making the extrusion explicitly
    /// negative-Z instead of relying on a coincident face at the slab origin.
    /// </summary>
    public static class SlabOpeningCutPlanner
    {
        public static SlabOpeningCutPlan Plan(SlabOpeningCutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var bottom = Finite(input.HostBottomM, nameof(input.HostBottomM));
            var thickness = Positive(input.HostThicknessM, nameof(input.HostThicknessM));
            var clearance = Positive(input.ClearanceM, nameof(input.ClearanceM));
            var top = Add(Add(bottom, thickness, "slab top"), clearance, "cutter top");
            var cutterBottom = Add(bottom, -clearance, "cutter bottom");
            var cutterHeight = Positive(Add(thickness, 2d * clearance, "cutter height"), "cutter height");
            var extrusionZ = Finite(-cutterHeight, "negative-Z extrusion");
            if (!(cutterBottom < bottom))
                throw new InvalidOperationException("slabOpen cutter must extend below the slab bottom.");
            if (!(extrusionZ < 0d))
                throw new InvalidOperationException("slabOpen cutter extrusion must be negative Z.");
            return new SlabOpeningCutPlan
            {
                CutterTopM = top,
                CutterBottomM = cutterBottom,
                CutterHeightM = cutterHeight,
                ExtrusionZM = extrusionZ
            };
        }

        private static double Positive(double value, string label)
        {
            value = Finite(value, label);
            if (!(value > 0d)) throw new ArgumentOutOfRangeException(label, label + " must be > 0.");
            return value;
        }

        private static double Add(double first, double second, string label)
        {
            return Finite(Finite(first, label + "/first") + Finite(second, label + "/second"), label);
        }

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(label, label + " must be finite.");
            return value == 0d ? 0d : value;
        }
    }
}
