using System;
using System.Collections.Generic;
using QS3D.Core.Coordination;
using QS3D.Core.Cost;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class CubicostParitySmoke
    {
        internal static void Run()
        {
            MepAggregation();
            ClashDetection();
            ClashDetectionLargeFiniteClearance();
            RateBuildUp();
            HistoricalBenchmark();
            TenderEvaluation();
            ProgressClaim();
            CubicostDeepParitySmoke.Run();
        }

        private static void MepAggregation()
        {
            var rows = new MepQuantityService().Aggregate(new[]
            {
                new MepElement("D-1", MepElementKind.Duct, "SA", "500x300", "ZONE-A", 1, 10d, 16d),
                new MepElement("D-2", MepElementKind.Duct, "sa", "500X300", "zone-a", 1, 5d, 8d),
                new MepElement("FCU-1", MepElementKind.Equipment, "CHW", "FCU-05", "ZONE-A")
            });

            Equal(2, rows.Count, "MEP grouping count");
            var duct = Find(rows, MepElementKind.Duct);
            Equal(2, duct.ElementCount, "MEP duct element count");
            Equal(2, duct.QuantityCount, "MEP duct quantity count");
            Near(15d, duct.LengthM, 1e-12, "MEP duct length");
            Near(24d, duct.AreaM2, 1e-12, "MEP duct area");
        }

        private static void ClashDetection()
        {
            var clashes = new ClashDetectionService().Detect(new[]
            {
                new CoordinationElement("S-1", "STRUCTURE", "BEAM", "STRUCT", "ZONE-A", new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d)),
                new CoordinationElement("M-1", "MEP", "DUCT", "SA", "ZONE-A", new AxisAlignedBox(0.5d, 0.2d, 0.2d, 1.5d, 0.8d, 0.8d)),
                new CoordinationElement("M-2", "MEP", "PIPE", "CHW", "ZONE-A", new AxisAlignedBox(1.1d, 2d, 0d, 2d, 3d, 1d))
            }, 1.05d);

            Equal(2, clashes.Count, "clash result count");
            Equal(ClashKind.Hard, clashes[0].Kind, "hard clash classification");
            Equal(ClashKind.Clearance, clashes[1].Kind, "clearance clash classification");
            Near(Math.Sqrt(1.01d), clashes[1].SeparationM, 1e-12, "clearance distance");
        }

        private static void ClashDetectionLargeFiniteClearance()
        {
            const double largeGap = 1e200d;
            var clashes = new ClashDetectionService().Detect(new[]
            {
                new CoordinationElement("S-LARGE", "STRUCTURE", "BEAM", "STRUCT", "ZONE-A", new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d)),
                new CoordinationElement("M-LARGE", "MEP", "DUCT", "SA", "ZONE-A", new AxisAlignedBox(largeGap, 0d, 0d, 1.01e200d, 1d, 1d))
            }, 1.1e200d);

            Equal(1, clashes.Count, "large finite clearance result count");
            Equal(ClashKind.Clearance, clashes[0].Kind, "large finite clearance classification");
            if (double.IsNaN(clashes[0].SeparationM) || double.IsInfinity(clashes[0].SeparationM))
                throw new InvalidOperationException("large finite clearance separation must stay finite.");
            Near(largeGap, clashes[0].SeparationM, 1e185d, "large finite clearance distance");
        }

        private static void RateBuildUp()
        {
            var buildUp = new CostRateBuildUp(
                "BU-001",
                new CostCode("CONC-30"),
                "m3",
                "VND",
                new[]
                {
                    new CostResourceComponent("LAB", "Labour", "hr", 2m, 50m),
                    new CostResourceComponent("MAT", "Material", "kg", 3m, 20m)
                },
                10m,
                5m);

            Equal(160m, buildUp.DirectUnitCost, "direct unit cost");
            Equal(16m, buildUp.OverheadUnitCost, "overhead unit cost");
            Equal(8.8m, buildUp.ProfitUnitCost, "profit unit cost");
            Equal(184.8m, buildUp.UnitRate, "built-up unit rate");
        }

        private static void HistoricalBenchmark()
        {
            var catalog = new HistoricalCostCatalog(new[]
            {
                new HistoricalCostRecord("H-1", "CONC-30", "ASSET=APT|REGION=HN", 10m, 100m, "VND", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                new HistoricalCostRecord("H-2", "CONC-30", "ASSET=APT|REGION=HN", 20m, 240m, "VND", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            });
            var result = new CostBenchmarkService().Analyze(catalog, "CONC-30", "ASSET=APT|REGION=HN", "VND", 12.1m);

            Equal(2, result.SampleCount, "benchmark sample count");
            Equal(10m, result.MinimumUnitCost, "benchmark minimum");
            Equal(12m, result.MaximumUnitCost, "benchmark maximum");
            Equal(11m, result.AverageUnitCost, "benchmark average");
            Equal(11m, result.MedianUnitCost, "benchmark median");
            Equal(10m, result.DeviationFromAveragePercent!.Value, "benchmark deviation");
        }

        private static void TenderEvaluation()
        {
            var requirements = new[]
            {
                new TenderRequirement("A", "Concrete", "m3", 2m),
                new TenderRequirement("B", "Rebar", "kg", 1m)
            };
            var bids = new[]
            {
                new TenderBid("BID-1", "Contractor One", "VND", new[] { new TenderQuoteLine("A", 10m), new TenderQuoteLine("B", 20m) }),
                new TenderBid("BID-2", "Contractor Two", "VND", new[] { new TenderQuoteLine("A", 12m), new TenderQuoteLine("B", 10m) }),
                new TenderBid("BID-3", "Contractor Three", "VND", new[] { new TenderQuoteLine("A", 9m) })
            };
            var result = new TenderEvaluationService().Evaluate(requirements, bids);

            Equal(3, result.Count, "tender result count");
            Equal(2, result[0].Rank, "tender bid 1 rank");
            Equal(40m, result[0].EvaluatedTotal, "tender bid 1 total");
            Equal(1, result[1].Rank, "tender bid 2 rank");
            Equal(34m, result[1].EvaluatedTotal, "tender bid 2 total");
            Equal(0, result[2].Rank, "incomplete tender rank");
            Equal(1, result[2].MissingItemCodes.Count, "incomplete tender missing count");
        }

        private static void ProgressClaim()
        {
            var result = new ProgressClaimService().Evaluate(
                new[] { new ProgressContractItem("A", "m3", 10m, 100m) },
                new[] { new ProgressClaimLine("A", 8m, 4m) },
                10m);

            Equal(1, result.Lines.Count, "progress claim line count");
            Equal(2m, result.Lines[0].CertifiedThisPeriodQuantity, "certified quantity cap");
            Equal(2m, result.Lines[0].RejectedQuantity, "rejected overclaim quantity");
            Equal(200m, result.GrossCertifiedThisPeriod, "gross progress value");
            Equal(20m, result.RetentionThisPeriod, "retention value");
            Equal(180m, result.NetCertifiedThisPeriod, "net progress value");
        }

        private static MepQuantityGroup Find(IReadOnlyList<MepQuantityGroup> rows, MepElementKind kind)
        {
            for (var i = 0; i < rows.Count; i++)
                if (rows[i].Kind == kind) return rows[i];
            throw new InvalidOperationException("Expected MEP quantity group was not found: " + kind + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
