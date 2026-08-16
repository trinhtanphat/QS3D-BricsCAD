using System;
using System.Collections.Generic;

namespace QS3D.Core.Rebar
{
    public sealed class RebarCutRequirement
    {
        public RebarCutRequirement(string cutId, double lengthM, int quantity)
        {
            CutId = RequireCanonicalText(cutId, nameof(cutId));
            LengthM = RebarMath.Positive(lengthM, nameof(lengthM));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Rebar cut quantity must be greater than zero.");
            Quantity = quantity;
        }

        public string CutId { get; }
        public double LengthM { get; }
        public int Quantity { get; }

        private static string RequireCanonicalText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Rebar cut identity is required.", parameterName);
            var trimmed = value.Trim();
            if (!string.Equals(value, trimmed, StringComparison.Ordinal))
                throw new ArgumentException("Rebar cut identity must not contain leading or trailing whitespace.", parameterName);
            return value;
        }
    }

    public sealed class RebarCutAllowancePolicy
    {
        public RebarCutAllowancePolicy(double kerfPerCutM = 0d, double allowancePerRequiredCutM = 0d)
        {
            KerfPerCutM = RebarMath.NonNegative(kerfPerCutM, nameof(kerfPerCutM));
            AllowancePerRequiredCutM = RebarMath.NonNegative(allowancePerRequiredCutM, nameof(allowancePerRequiredCutM));
        }

        public double KerfPerCutM { get; }
        public double AllowancePerRequiredCutM { get; }
    }

    public sealed class RebarStockDemand
    {
        private const int MaxCutRequirements = 10000;

        public RebarStockDemand(
            string groupId,
            string grade,
            double diameterMm,
            double stockLengthM,
            IReadOnlyList<RebarCutRequirement> requiredCuts,
            RebarCutAllowancePolicy allowancePolicy)
        {
            GroupId = RequireCanonicalText(groupId, nameof(groupId));
            Grade = RequireCanonicalText(grade, nameof(grade));
            DiameterMm = RebarMath.Positive(diameterMm, nameof(diameterMm));
            StockLengthM = RebarMath.Positive(stockLengthM, nameof(stockLengthM));
            if (requiredCuts == null) throw new ArgumentNullException(nameof(requiredCuts));
            if (requiredCuts.Count == 0)
                throw new ArgumentException("At least one required cut is required.", nameof(requiredCuts));
            if (requiredCuts.Count > MaxCutRequirements)
                throw new ArgumentOutOfRangeException(nameof(requiredCuts), "Rebar stock demand exceeds the supported cut-requirement bound.");
            if (allowancePolicy == null) throw new ArgumentNullException(nameof(allowancePolicy));

            var cuts = new List<RebarCutRequirement>(requiredCuts.Count);
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long cutCount = 0L;
            var requiredLength = new CompensatedFiniteSum();
            var allowanceLength = new CompensatedFiniteSum();

            foreach (var cut in requiredCuts)
            {
                if (cut == null)
                    throw new ArgumentException("Required cuts must not contain null entries.", nameof(requiredCuts));
                if (!identities.Add(cut.CutId))
                    throw new ArgumentException("Required cut identities must be unique (case-insensitive): " + cut.CutId + ".", nameof(requiredCuts));

                cuts.Add(cut);
                checked { cutCount += cut.Quantity; }

                requiredLength.Add(
                    RebarMath.Multiply(cut.LengthM, cut.Quantity, "required rebar cut length"),
                    "total required rebar cut length");
                allowanceLength.Add(
                    RebarMath.Multiply(allowancePolicy.AllowancePerRequiredCutM, cut.Quantity, "required rebar cut allowance"),
                    "total required rebar cut allowance");
            }

            var requiredLengthM = requiredLength.Value("total required rebar cut length");
            var allowanceLengthM = allowanceLength.Value("total required rebar cut allowance");
            RequiredCuts = cuts.AsReadOnly();
            AllowancePolicy = allowancePolicy;
            RequiredCutCount = cutCount;
            RequiredCutLengthM = requiredLengthM;
            AllowanceLengthM = allowanceLengthM;
            DemandLengthBeforeKerfM = RebarMath.Add(
                requiredLengthM,
                allowanceLengthM,
                "rebar demand length before cutting kerf");
        }

        public string GroupId { get; }
        public string Grade { get; }
        public double DiameterMm { get; }
        public double StockLengthM { get; }
        public IReadOnlyList<RebarCutRequirement> RequiredCuts { get; }
        public RebarCutAllowancePolicy AllowancePolicy { get; }
        public long RequiredCutCount { get; }
        public double RequiredCutLengthM { get; }
        public double AllowanceLengthM { get; }
        public double DemandLengthBeforeKerfM { get; }

        private static string RequireCanonicalText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Rebar stock-demand identity is required.", parameterName);
            var trimmed = value.Trim();
            if (!string.Equals(value, trimmed, StringComparison.Ordinal))
                throw new ArgumentException("Rebar stock-demand identity must not contain leading or trailing whitespace.", parameterName);
            return value;
        }

        private struct CompensatedFiniteSum
        {
            private double _sum;
            private double _compensation;

            public void Add(double value, string label)
            {
                var next = _sum + value;
                EnsureFinite(next, label);

                var correction = Math.Abs(_sum) >= Math.Abs(value)
                    ? (_sum - next) + value
                    : (value - next) + _sum;
                var compensation = _compensation + correction;
                EnsureFinite(compensation, label);

                _sum = next;
                _compensation = compensation;
            }

            public double Value(string label)
            {
                var result = _sum + _compensation;
                EnsureFinite(result, label);
                return result;
            }

            private static void EnsureFinite(double value, string label)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new OverflowException("Rebar addition overflow: " + label);
            }
        }
    }

    public sealed class RebarStockProcurementQuantities
    {
        public RebarStockProcurementQuantities(double stockLengthM, int stockBarCount, double kerfLengthM, double offCutLengthM)
        {
            StockLengthM = RebarMath.Positive(stockLengthM, nameof(stockLengthM));
            if (stockBarCount < 0)
                throw new ArgumentOutOfRangeException(nameof(stockBarCount), "Procurement stock-bar count must be non-negative.");
            StockBarCount = stockBarCount;
            KerfLengthM = RebarMath.NonNegative(kerfLengthM, nameof(kerfLengthM));
            OffCutLengthM = RebarMath.NonNegative(offCutLengthM, nameof(offCutLengthM));
            ProcurementLengthM = RebarMath.Multiply(StockLengthM, StockBarCount, "rebar procurement length");
            var wasteLengthM = RebarMath.Add(KerfLengthM, OffCutLengthM, "rebar procurement waste length");
            if (wasteLengthM > ProcurementLengthM)
                throw new ArgumentOutOfRangeException(nameof(offCutLengthM), "Kerf plus off-cut length cannot exceed procured stock length.");
        }

        public double StockLengthM { get; }
        public int StockBarCount { get; }
        public double ProcurementLengthM { get; }
        public double KerfLengthM { get; }
        public double OffCutLengthM { get; }
    }
}
