using System;
using System.Collections.Generic;

namespace QS3D.Core.Rebar
{
    public sealed class RebarProcurementSummary
    {
        internal RebarProcurementSummary(RebarCuttingOptimizationResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var demand = result.Demand ?? throw new InvalidOperationException("Rebar cutting result is missing canonical demand.");
            var procurement = result.ProcurementQuantities ?? throw new InvalidOperationException("Rebar cutting result is missing canonical procurement quantities.");

            GroupId = demand.GroupId;
            Grade = demand.Grade;
            DiameterMm = demand.DiameterMm;
            StockLengthM = demand.StockLengthM;
            RequiredCutCount = demand.RequiredCutCount;
            RequiredLengthM = demand.RequiredCutLengthM;
            AllowanceLengthM = demand.AllowanceLengthM;
            DemandBeforeKerfM = demand.DemandLengthBeforeKerfM;
            StockBarCount = procurement.StockBarCount;
            KerfLengthM = procurement.KerfLengthM;
            OffCutLengthM = procurement.OffCutLengthM;
            ProcurementLengthM = procurement.ProcurementLengthM;
            WasteLengthM = RebarMath.Add(KerfLengthM, OffCutLengthM, "rebar procurement report waste length");
            UnitWeightKgM = RebarWeight.KilogramsPerMeter(DiameterMm);
            DemandWeightKg = RebarMath.Multiply(UnitWeightKgM, DemandBeforeKerfM, "rebar procurement report demand weight");
            ProcurementWeightKg = RebarMath.Multiply(UnitWeightKgM, ProcurementLengthM, "rebar procurement report procurement weight");
            WasteWeightKg = RebarMath.Multiply(UnitWeightKgM, WasteLengthM, "rebar procurement report waste weight");
            WastePercent = RebarMath.Multiply(
                RebarMath.Divide(WasteLengthM, ProcurementLengthM, "rebar procurement report waste ratio"),
                100d,
                "rebar procurement report waste percent");
        }

        public string AlgorithmId => RebarCuttingOptimizationResult.AlgorithmId;
        public string GroupId { get; }
        public string Grade { get; }
        public double DiameterMm { get; }
        public double StockLengthM { get; }
        public long RequiredCutCount { get; }
        public double RequiredLengthM { get; }
        public double AllowanceLengthM { get; }
        public double DemandBeforeKerfM { get; }
        public int StockBarCount { get; }
        public double KerfLengthM { get; }
        public double OffCutLengthM { get; }
        public double WasteLengthM { get; }
        public double ProcurementLengthM { get; }
        public double UnitWeightKgM { get; }
        public double DemandWeightKg { get; }
        public double ProcurementWeightKg { get; }
        public double WasteWeightKg { get; }
        public double WastePercent { get; }
    }

    public static class RebarProcurementReportBuilder
    {
        private const int MaxResultCount = 10000;

        public static IReadOnlyList<RebarProcurementSummary> Build(IEnumerable<RebarCuttingOptimizationResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            var rows = new List<RebarProcurementSummary>();
            var groupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var result in results)
            {
                if (rows.Count >= MaxResultCount)
                    throw new ArgumentOutOfRangeException(nameof(results), "Rebar procurement report exceeds the supported result bound of " + MaxResultCount + ".");
                if (result == null) throw new ArgumentException("Rebar procurement report cannot contain a null cutting result.", nameof(results));
                if (!groupIds.Add(result.Demand.GroupId))
                    throw new InvalidOperationException("Duplicate rebar procurement group identity: " + result.Demand.GroupId + ".");
                rows.Add(new RebarProcurementSummary(result));
            }

            rows.Sort(CompareRows);
            return rows.AsReadOnly();
        }

        private static int CompareRows(RebarProcurementSummary left, RebarProcurementSummary right)
        {
            var byGroupIgnoreCase = StringComparer.OrdinalIgnoreCase.Compare(left.GroupId, right.GroupId);
            if (byGroupIgnoreCase != 0) return byGroupIgnoreCase;
            var byGroup = StringComparer.Ordinal.Compare(left.GroupId, right.GroupId);
            if (byGroup != 0) return byGroup;
            var byGradeIgnoreCase = StringComparer.OrdinalIgnoreCase.Compare(left.Grade, right.Grade);
            if (byGradeIgnoreCase != 0) return byGradeIgnoreCase;
            var byGrade = StringComparer.Ordinal.Compare(left.Grade, right.Grade);
            if (byGrade != 0) return byGrade;
            var byDiameter = left.DiameterMm.CompareTo(right.DiameterMm);
            if (byDiameter != 0) return byDiameter;
            return left.StockLengthM.CompareTo(right.StockLengthM);
        }
    }
}
