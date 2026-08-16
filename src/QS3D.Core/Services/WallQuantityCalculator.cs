using System;
using System.Collections.Generic;
using QS3D.Core.Measurement;

namespace QS3D.Core.Services
{
    public sealed class OpeningCut
    {
        public double WidthM { get; set; }
        public double HeightM { get; set; }
        public double AreaM2
        {
            get
            {
                RequireFiniteNonNegative(WidthM, nameof(WidthM));
                RequireFiniteNonNegative(HeightM, nameof(HeightM));
                var area = WidthM * HeightM;
                if (double.IsNaN(area) || double.IsInfinity(area)) throw new OverflowException("Opening area is not finite.");
                if (WidthM != 0d && HeightM != 0d && area == 0d) throw new OverflowException("Opening area underflowed to zero.");
                return area == 0d ? 0d : area;
            }
        }

        private static void RequireFiniteNonNegative(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(name, "Opening dimensions must be finite and non-negative.");
        }
    }

    public sealed class WallQuantities
    {
        public double GrossAreaM2 { get; set; }
        public double OpeningAreaM2 { get; set; }
        public double NetAreaM2 { get; set; }
        public double GrossVolumeM3 { get; set; }
        public double DeductionVolumeM3 { get; set; }
        public double NetVolumeM3 { get; set; }
        public double TwoSideFinishAreaM2 { get; set; }
    }

    public sealed class WallQuantityResultWithTrace
    {
        public WallQuantityResultWithTrace(
            WallQuantities quantities,
            MeasurementTrace netAreaTrace,
            MeasurementTrace netVolumeTrace)
        {
            Quantities = quantities ?? throw new ArgumentNullException(nameof(quantities));
            NetAreaTrace = netAreaTrace ?? throw new ArgumentNullException(nameof(netAreaTrace));
            NetVolumeTrace = netVolumeTrace ?? throw new ArgumentNullException(nameof(netVolumeTrace));
        }

        public WallQuantities Quantities { get; }
        public MeasurementTrace NetAreaTrace { get; }
        public MeasurementTrace NetVolumeTrace { get; }
    }

    public static class WallQuantityCalculator
    {
        private const int MaxOpeningInputCount = 10000;

        public static WallQuantities Calculate(double lengthM, double heightM, double thicknessM, IEnumerable<OpeningCut>? openings = null)
        {
            RequireFiniteNonNegative(lengthM, nameof(lengthM));
            RequireFiniteNonNegative(heightM, nameof(heightM));
            RequireFiniteNonNegative(thicknessM, nameof(thicknessM));

            var grossArea = FiniteProduct(lengthM, heightM, "gross wall area");
            var openingAreaSum = new CompensatedFiniteSum();
            if (openings != null)
            {
                EnsureKnownOpeningCountWithinBound(openings);
                var inputCount = 0;
                foreach (var opening in openings)
                {
                    if (inputCount >= MaxOpeningInputCount)
                        throw new InvalidOperationException("Wall opening collection cannot exceed " + MaxOpeningInputCount + " input entries.");
                    inputCount++;
                    if (opening == null)
                        throw new ArgumentException("Wall opening collection cannot contain null entries.", nameof(openings));
                    openingAreaSum.Add(opening.AreaM2);
                }
            }

            var openingArea = openingAreaSum.Value;
            var clampedOpeningArea = Math.Min(grossArea, openingArea);
            var netArea = grossArea - clampedOpeningArea;
            var grossVolume = FiniteProduct(grossArea, thicknessM, "gross wall volume");
            var deductionVolume = FiniteProduct(clampedOpeningArea, thicknessM, "wall deduction volume");
            var twoSideFinishArea = FiniteProduct(netArea, 2d, "two-side finish area");

            return new WallQuantities
            {
                GrossAreaM2 = grossArea,
                OpeningAreaM2 = clampedOpeningArea,
                NetAreaM2 = netArea,
                GrossVolumeM3 = grossVolume,
                DeductionVolumeM3 = deductionVolume,
                NetVolumeM3 = grossVolume - deductionVolume,
                TwoSideFinishAreaM2 = twoSideFinishArea
            };
        }

        public static WallQuantityResultWithTrace CalculateWithTrace(
            string semanticIdentity,
            string sourceIdentity,
            double lengthM,
            double heightM,
            double thicknessM,
            IEnumerable<OpeningCut>? openings = null)
        {
            var quantities = Calculate(lengthM, heightM, thicknessM, openings);
            var facts = new[]
            {
                new MeasurementTraceFact("LengthM", lengthM, "m", sourceIdentity),
                new MeasurementTraceFact("HeightM", heightM, "m", sourceIdentity),
                new MeasurementTraceFact("ThicknessM", thicknessM, "m", sourceIdentity)
            };

            var areaAdjustments = quantities.OpeningAreaM2 > 0d
                ? new[]
                {
                    new MeasurementTraceAdjustment(
                        MeasurementTraceAdjustmentKind.Deduction,
                        quantities.OpeningAreaM2,
                        "m2",
                        "Wall opening area deduction",
                        sourceIdentity)
                }
                : Array.Empty<MeasurementTraceAdjustment>();

            var volumeAdjustments = quantities.DeductionVolumeM3 > 0d
                ? new[]
                {
                    new MeasurementTraceAdjustment(
                        MeasurementTraceAdjustmentKind.Deduction,
                        quantities.DeductionVolumeM3,
                        "m3",
                        "Wall opening volume deduction",
                        sourceIdentity)
                }
                : Array.Empty<MeasurementTraceAdjustment>();

            var areaTrace = new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                "NetAreaM2",
                facts,
                quantities.GrossAreaM2,
                areaAdjustments,
                quantities.NetAreaM2,
                "m2",
                "none");

            var volumeTrace = new MeasurementTrace(
                semanticIdentity,
                sourceIdentity,
                "NetVolumeM3",
                facts,
                quantities.GrossVolumeM3,
                volumeAdjustments,
                quantities.NetVolumeM3,
                "m3",
                "none");

            return new WallQuantityResultWithTrace(quantities, areaTrace, volumeTrace);
        }

        private static void EnsureKnownOpeningCountWithinBound(IEnumerable<OpeningCut> openings)
        {
            if (openings is ICollection<OpeningCut> collection && collection.Count > MaxOpeningInputCount)
                throw new InvalidOperationException("Wall opening collection cannot exceed " + MaxOpeningInputCount + " input entries.");
            if (openings is IReadOnlyCollection<OpeningCut> readOnlyCollection && readOnlyCollection.Count > MaxOpeningInputCount)
                throw new InvalidOperationException("Wall opening collection cannot exceed " + MaxOpeningInputCount + " input entries.");
        }

        private static void RequireFiniteNonNegative(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(name, "Wall dimensions must be finite and non-negative.");
        }

        private static double FiniteProduct(double left, double right, string label)
        {
            var result = left * right;
            if (double.IsNaN(result) || double.IsInfinity(result)) throw new OverflowException(label + " is not finite.");
            if (left != 0d && right != 0d && result == 0d) throw new OverflowException(label + " underflowed to zero.");
            return result == 0d ? 0d : result;
        }

        private struct CompensatedFiniteSum
        {
            private double _sum;
            private double _compensation;

            public void Add(double value)
            {
                var next = _sum + value;
                EnsureFinite(next);

                var correction = Math.Abs(_sum) >= Math.Abs(value)
                    ? (_sum - next) + value
                    : (value - next) + _sum;
                var compensation = _compensation + correction;
                EnsureFinite(compensation);

                _sum = next;
                _compensation = compensation;
            }

            public double Value
            {
                get
                {
                    var result = _sum + _compensation;
                    EnsureFinite(result);
                    return result;
                }
            }

            private static void EnsureFinite(double value)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new OverflowException("Total opening area is not finite.");
            }
        }
    }
}
