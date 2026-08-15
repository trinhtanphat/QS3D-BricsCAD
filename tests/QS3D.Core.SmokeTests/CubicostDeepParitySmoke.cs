using System;
using System.Collections.Generic;
using QS3D.Core.Cost;
using QS3D.Core.Recognition;

namespace QS3D.Core.SmokeTests
{
    internal static class CubicostDeepParitySmoke
    {
        internal static void Run()
        {
            IdentificationOptions();
            RateReferenceAndBuildUpAnalysis();
            CostAdjustment();
            HistoricalBenchmarkOverflowSafety();
            TradeAnalysis();
            BqLibraryReuse();
        }

        private static void IdentificationOptions()
        {
            var options = new CadIdentificationOptions(
                importHatches: false,
                selectByColor: true,
                beamSizeReadingMode: BeamSizeReadingMode.HeightByWidth,
                beamEndExtensionMode: BeamEndExtensionMode.NearestHostFace,
                beamAutoExtensionTolerance: 0.15d,
                identifyPdfText: true,
                allowCadEntityRestore: true,
                colorRules: new[] { new IdentificationColorRule(1, "Column") });
            var planner = new CadIdentificationPlanner();

            True(!planner.ShouldImport(CadImportEntityKind.Hatch, options), "hatch import filter");
            Equal("Column", planner.ResolveClassificationByColor(1, options), "color classification");
            var size = planner.ReadBeamSize(300d, 600d, options);
            Near(600d, size.Width, 1e-12, "beam width mode");
            Near(300d, size.Height, 1e-12, "beam height mode");
            Near(0.2d, planner.ResolveBeamExtensionDistance(0.5d, 0.2d, options), 1e-12, "beam extension mode");
            True(planner.CanAutoExtendBeamEnd(0.1d, options), "beam extension tolerance pass");
            True(!planner.CanAutoExtendBeamEnd(0.2d, options), "beam extension tolerance reject");
            True(planner.CanIdentifyPdfText(options), "pdf text identification option");
            True(planner.CanRestoreCadEntity(options), "cad restore option");
        }

        private static void RateReferenceAndBuildUpAnalysis()
        {
            var graph = new RateReferenceGraph(new[]
            {
                new RateReferenceEdge("MAT-A", RateReferenceTargetKind.BillItem, "BQ-01"),
                new RateReferenceEdge("MAT-A", RateReferenceTargetKind.UnitRate, "UR-02"),
                new RateReferenceEdge("LAB-B", RateReferenceTargetKind.BillItem, "BQ-02")
            });

            var mark = graph.GetMark("MAT-A");
            True(mark.UsedInBillItems, "rate BQ reference mark");
            True(mark.UsedInUnitRates, "rate UR reference mark");
            Equal(1, graph.GetReverseReferences("MAT-A", RateReferenceTargetKind.BillItem).Count, "reverse BQ lookup");
            Equal("BQ-01", graph.GetReverseReferences("MAT-A", RateReferenceTargetKind.BillItem)[0], "reverse BQ item");

            var rows = new BuildUpAnalysisService().Analyze(new[]
            {
                new BuildUpRateSnapshot("MAT-A", 20m),
                new BuildUpRateSnapshot("LAB-B", 30m),
                new BuildUpRateSnapshot("UNUSED", 5m)
            }, graph);
            Equal(2, rows.Count, "adopted build-up rate count");
            Equal("LAB-B", rows[0].Rate.RateCode, "deterministic build-up sort");
            Equal("MAT-A", rows[1].Rate.RateCode, "deterministic build-up sort second");
        }

        private static void CostAdjustment()
        {
            var service = new CostAdjustmentService();
            var byRatios = service.AdjustByRatios(100m, 10m, 5m);
            Equal(115.5m, byRatios.AdjustedTotal, "adjust cost by ratios");
            Equal(15.5m, byRatios.CombinedRatioPercent, "combined adjustment ratio");

            var byTotal = service.AdjustToTotal(200m, 230m);
            Equal(15m, byTotal.CombinedRatioPercent, "adjust total derived ratio");
            Equal(230m, byTotal.AdjustedTotal, "adjust total target");
        }

        private static void HistoricalBenchmarkOverflowSafety()
        {
            var catalog = new HistoricalCostCatalog(new[]
            {
                new HistoricalCostRecord("H-MAX-1", "MAX-COST", "REGION=TEST", 1m, decimal.MaxValue, "VND", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                new HistoricalCostRecord("H-MAX-2", "MAX-COST", "REGION=TEST", 1m, decimal.MaxValue, "VND", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            });

            var result = new CostBenchmarkService().Analyze(
                catalog,
                "MAX-COST",
                "REGION=TEST",
                "VND",
                decimal.MaxValue);

            Equal(2, result.SampleCount, "high-value benchmark sample count");
            Equal(decimal.MaxValue, result.MinimumUnitCost, "high-value benchmark minimum");
            Equal(decimal.MaxValue, result.MaximumUnitCost, "high-value benchmark maximum");
            Equal(decimal.MaxValue, result.AverageUnitCost, "high-value benchmark average");
            Equal(decimal.MaxValue, result.MedianUnitCost, "high-value benchmark median");
            Equal(0m, result.DeviationFromAveragePercent!.Value, "high-value benchmark deviation");
        }

        private static void TradeAnalysis()
        {
            var rows = new TradeCostAnalysisService().Analyze(new[]
            {
                new TradeCostItem("A", "STR", 200m),
                new TradeCostItem("B", "STR", 100m),
                new TradeCostItem("C", null, 50m)
            }, 100m);

            Equal(2, rows.Count, "trade analysis group count");
            Equal("STR", rows[0].TradeCode, "trade sorted row");
            Equal(300m, rows[0].TotalCost, "trade total cost");
            Equal(3m, rows[0].CostPerCfaM2!.Value, "trade cost per CFA");
            Equal("Unclassified", rows[1].TradeCode, "unclassified trade bucket");
        }

        private static void BqLibraryReuse()
        {
            var library = new BqLibraryCatalog("LIB-1", new[]
            {
                new BqLibraryEntry("BQ-A", "Concrete", "m3", "Structure/Concrete", 100m)
            });
            var imported = library.ImportFromProject(new[]
            {
                new BqLibraryEntry("BQ-B", "Rebar", "kg", "Structure/Rebar", 2m)
            }, replaceExisting: false);

            Equal(2, imported.Entries.Count, "BQ library project import count");
            Equal("BQ-A", imported.Entries[0].ItemCode, "BQ library deterministic category sort first");
            Equal("BQ-B", imported.Entries[1].ItemCode, "BQ library deterministic category sort second");

            Throws<ArgumentException>(() => library.ImportFromProject(new[]
            {
                new BqLibraryEntry("BQ-C", "Formwork", "m2", "Structure/Formwork", 3m),
                new BqLibraryEntry("BQ-C", "Formwork duplicate", "m2", "Structure/Formwork", 4m)
            }, replaceExisting: true), "BQ library duplicate project payload");
        }

        private static void True(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label + " failed.");
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

        private static void Throws<TException>(Action action, string label)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException(label + " did not throw " + typeof(TException).Name + ".");
        }
    }
}