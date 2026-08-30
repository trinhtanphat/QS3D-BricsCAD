using System;
using System.Collections;
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

            var expectedCount = RequireKnownCountWithinLimit(results);
            var observedCount = 0;
            var rows = new List<RebarProcurementSummary>();
            var groupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var enumerator = results.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownCount(results, expectedCount);
                    if (!enumerator.MoveNext())
                    {
                        RequireStableKnownCount(results, expectedCount);
                        break;
                    }
                    RequireStableKnownCount(results, expectedCount);

                    if (expectedCount.HasValue && observedCount >= expectedCount.Value)
                        throw CountMismatch(expectedCount.Value, observedCount + 1);
                    if (observedCount >= MaxResultCount)
                        throw TooManyResults();

                    var result = enumerator.Current;
                    RequireStableKnownCount(results, expectedCount);
                    if (result == null) throw new ArgumentException("Rebar procurement report cannot contain a null cutting result.", nameof(results));
                    if (!groupIds.Add(result.Demand.GroupId))
                        throw new InvalidOperationException("Duplicate rebar procurement group identity: " + result.Demand.GroupId + ".");
                    rows.Add(new RebarProcurementSummary(result));
                    observedCount++;
                }
            }

            RequireStableKnownCount(results, expectedCount);
            if (expectedCount.HasValue && observedCount != expectedCount.Value)
                throw CountMismatch(expectedCount.Value, observedCount);

            rows.Sort(CompareRows);
            return rows.AsReadOnly();
        }

        private static int? RequireKnownCountWithinLimit(IEnumerable<RebarCuttingOptimizationResult> results)
        {
            var counts = new List<int>(3);
            if (results is ICollection<RebarCuttingOptimizationResult> genericCollection)
                counts.Add(genericCollection.Count);
            if (results is IReadOnlyCollection<RebarCuttingOptimizationResult> readOnlyCollection)
                counts.Add(readOnlyCollection.Count);
            if (results is ICollection nonGenericCollection)
                counts.Add(nonGenericCollection.Count);

            if (counts.Count == 0) return null;

            var expected = counts[0];
            for (var index = 0; index < counts.Count; index++)
            {
                var count = counts[index];
                if (count < 0)
                    throw new InvalidOperationException("Rebar procurement report input exposes an invalid negative known Count.");
                if (count > MaxResultCount)
                    throw TooManyResults();
                if (count != expected)
                    throw new InvalidOperationException("Rebar procurement report input exposes conflicting known Count values.");
            }
            return expected;
        }

        private static void RequireStableKnownCount(
            IEnumerable<RebarCuttingOptimizationResult> results,
            int? expectedCount)
        {
            if (!expectedCount.HasValue) return;
            var currentCount = RequireKnownCountWithinLimit(results);
            if (!currentCount.HasValue || currentCount.Value != expectedCount.Value)
                throw new InvalidOperationException("Rebar procurement report input known Count changed during traversal.");
        }

        private static InvalidOperationException CountMismatch(int expected, int observed)
        {
            return new InvalidOperationException(
                "Rebar procurement report input known Count does not match traversal (expected " + expected + ", observed " + observed + ").");
        }

        private static ArgumentOutOfRangeException TooManyResults()
        {
            return new ArgumentOutOfRangeException(
                "results",
                "Rebar procurement report exceeds the supported result bound of " + MaxResultCount + ".");
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