using System;
using System.Collections.Generic;

namespace QS3D.Core.Rebar
{
    public sealed class LinearRebarLayoutInput
    {
        public double SpanM { get; set; }
        public double CoverM { get; set; }
        public double DiameterMm { get; set; }
        public double? SpacingMm { get; set; }
        public int? Count { get; set; }
    }

    public sealed class LinearRebarLayout
    {
        public LinearRebarLayout(IReadOnlyList<double> offsetsM, double usableSpanM, double actualSpacingM)
        {
            if (offsetsM == null) throw new ArgumentNullException(nameof(offsetsM));
            OffsetsM = new List<double>(offsetsM).AsReadOnly();
            UsableSpanM = usableSpanM;
            ActualSpacingM = actualSpacingM;
        }

        public IReadOnlyList<double> OffsetsM { get; }
        public int Count => OffsetsM.Count;
        public double UsableSpanM { get; }
        public double ActualSpacingM { get; }
    }

    public static class LinearRebarLayoutPlanner
    {
        private const int MaxBars = 10000;

        public static LinearRebarLayout Plan(LinearRebarLayoutInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var spanM = RebarMath.Positive(input.SpanM, nameof(input.SpanM));
            var coverM = RebarMath.NonNegative(input.CoverM, nameof(input.CoverM));
            var diameterMm = RebarMath.Positive(input.DiameterMm, nameof(input.DiameterMm));
            if (input.Count.HasValue == input.SpacingMm.HasValue)
                throw new InvalidOperationException("Specify exactly one of Count or SpacingMm for a linear rebar layout.");

            var diameterM = RebarMath.Divide(diameterMm, 1000d, "linear rebar diameter");
            var radiusM = RebarMath.Divide(diameterM, 2d, "linear rebar radius");
            var edgeClearanceM = RebarMath.Add(coverM, radiusM, "linear rebar edge clearance");
            var twoEdgeClearanceM = RebarMath.Multiply(edgeClearanceM, 2d, "linear rebar two-side clearance");
            var usableSpanM = spanM - twoEdgeClearanceM;
            if (double.IsNaN(usableSpanM) || double.IsInfinity(usableSpanM)) throw new OverflowException("Linear rebar usable span is not finite.");
            if (twoEdgeClearanceM > 0d && usableSpanM == spanM)
                throw new OverflowException("Linear rebar usable span lost positive edge clearance at the current numeric scale.");
            if (usableSpanM < 0d) throw new InvalidOperationException("Cover + bar radius leaves no usable linear rebar span inside the host.");

            int count;
            if (input.Count.HasValue)
            {
                count = input.Count.Value;
                if (count <= 0 || count > MaxBars) throw new ArgumentOutOfRangeException(nameof(input.Count));
            }
            else
            {
                var spacingMm = RebarMath.Positive(input.SpacingMm!.Value, nameof(input.SpacingMm));
                var usableMm = RebarMath.Multiply(usableSpanM, 1000d, "linear rebar usable span mm");
                var intervalRatio = RebarMath.Divide(usableMm, spacingMm, "linear rebar spacing intervals");
                var intervals = RebarMath.CeilingNearInteger(intervalRatio, "linear rebar spacing intervals");
                if (double.IsNaN(intervals) || double.IsInfinity(intervals) || intervals > MaxBars - 1d)
                    throw new InvalidOperationException("Linear rebar spacing requires too many bars.");
                count = checked((int)intervals + 1);
            }

            if (count == 1)
            {
                return new LinearRebarLayout(new[] { 0d }, usableSpanM, 0d);
            }
            if (!(usableSpanM > 0d)) throw new InvalidOperationException("Multiple linear rebars require a positive usable span.");

            var actualSpacingM = RebarMath.Divide(usableSpanM, count - 1d, "linear rebar actual spacing");
            if (actualSpacingM + 1e-12d < diameterM)
                throw new InvalidOperationException("Linear rebar centers are closer than one bar diameter.");
            var halfSpanM = RebarMath.Divide(usableSpanM, 2d, "linear rebar half span");
            var offsets = new List<double>(count);
            for (var index = 0; index < count; index++)
            {
                var offset = -halfSpanM + actualSpacingM * index;
                if (double.IsNaN(offset) || double.IsInfinity(offset)) throw new OverflowException("Linear rebar offset is not finite.");
                offsets.Add(offset);
            }
            offsets[0] = -halfSpanM;
            offsets[offsets.Count - 1] = halfSpanM;
            return new LinearRebarLayout(offsets.AsReadOnly(), usableSpanM, actualSpacingM);
        }
    }
}
