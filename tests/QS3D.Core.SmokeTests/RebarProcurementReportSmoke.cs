using System;
using System.Globalization;
using System.IO;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarProcurementReportSmoke
    {
        public static void Run()
        {
            ProjectionConsumesCanonicalOptimizerResult();
            GroupOrderingIsDeterministic();
            DuplicateGroupIdentityFailsClosed();
            CsvUsesProjectedQuantitiesAndRejectsFormulaGradeIdentity();
            CsvRejectsNullRows();
        }

        private static void ProjectionConsumesCanonicalOptimizerResult()
        {
            var result = Optimize(
                "G-01",
                "CB400-V",
                16d,
                12d,
                0.01d,
                new RebarCutRequirement("LONG", 6d, 1),
                new RebarCutRequirement("SHORT", 5.5d, 1));

            var rows = RebarProcurementReportBuilder.Build(new[] { result });
            Equal(1, rows.Count);
            var row = rows[0];
            Equal("BestFitDecreasingV1", row.AlgorithmId);
            Equal("G-01", row.GroupId);
            Equal("CB400-V", row.Grade);
            Equal(2L, row.RequiredCutCount);
            Equal(1, row.StockBarCount);
            Near(11.5d, row.RequiredLengthM);
            Near(0d, row.AllowanceLengthM);
            Near(0.02d, row.KerfLengthM);
            Near(0.48d, row.OffCutLengthM);
            Near(0.5d, row.WasteLengthM);
            Near(12d, row.ProcurementLengthM);
            Near(RebarWeight.KilogramsPerMeter(16d), row.UnitWeightKgM);
            Near(row.UnitWeightKgM * row.DemandBeforeKerfM, row.DemandWeightKg);
            Near(row.UnitWeightKgM * row.ProcurementLengthM, row.ProcurementWeightKg);
            Near(row.UnitWeightKgM * row.WasteLengthM, row.WasteWeightKg);
            Near((row.WasteLengthM / row.ProcurementLengthM) * 100d, row.WastePercent);
        }

        private static void GroupOrderingIsDeterministic()
        {
            var groupB = Optimize("B", "CB400-V", 16d, 10d, 0d, new RebarCutRequirement("B1", 10d, 1));
            var groupA = Optimize("A", "CB400-V", 16d, 10d, 0d, new RebarCutRequirement("A1", 10d, 1));
            var rows = RebarProcurementReportBuilder.Build(new[] { groupB, groupA });
            Equal("A", rows[0].GroupId);
            Equal("B", rows[1].GroupId);
        }

        private static void DuplicateGroupIdentityFailsClosed()
        {
            var first = Optimize("G", "CB400-V", 16d, 10d, 0d, new RebarCutRequirement("A", 10d, 1));
            var second = Optimize("g", "CB500-V", 20d, 12d, 0d, new RebarCutRequirement("B", 12d, 1));
            Throws<InvalidOperationException>(() => RebarProcurementReportBuilder.Build(new[] { first, second }));
        }

        private static void CsvUsesProjectedQuantitiesAndRejectsFormulaGradeIdentity()
        {
            var result = Optimize("G", "CB400-V", 16d, 12d, 0.01d, new RebarCutRequirement("A", 6d, 1));
            var row = RebarProcurementReportBuilder.Build(new[] { result })[0];
            var csv = RebarProcurementCsvExporter.ToCsv(new[] { row });

            Require(csv, "AlgorithmId,GroupId,Grade,DiameterMm");
            Require(csv, "\"G\"");
            Require(csv, "\"CB400-V\"");
            Require(csv, row.KerfLengthM.ToString("R", CultureInfo.InvariantCulture));
            Require(csv, row.OffCutLengthM.ToString("R", CultureInfo.InvariantCulture));
            Require(csv, row.ProcurementLengthM.ToString("R", CultureInfo.InvariantCulture));
            Require(csv, row.WastePercent.ToString("R", CultureInfo.InvariantCulture));

            var formulaGrade = Optimize("G-FORMULA", "+CB400-V", 16d, 12d, 0.01d, new RebarCutRequirement("A", 6d, 1));
            var formulaRow = RebarProcurementReportBuilder.Build(new[] { formulaGrade })[0];
            Throws<InvalidDataException>(() => RebarProcurementCsvExporter.ToCsv(new[] { formulaRow }));
        }

        private static void CsvRejectsNullRows()
        {
            Throws<ArgumentException>(() => RebarProcurementCsvExporter.ToCsv(new RebarProcurementSummary[] { null! }));
        }

        private static RebarCuttingOptimizationResult Optimize(
            string groupId,
            string grade,
            double diameterMm,
            double stockLengthM,
            double kerfM,
            params RebarCutRequirement[] cuts)
        {
            var demand = new RebarStockDemand(
                groupId,
                grade,
                diameterMm,
                stockLengthM,
                cuts,
                new RebarCutAllowancePolicy(kerfM, 0d));
            return RebarCuttingOptimizer.Plan(demand);
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-10d)
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(long expected, long actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(int expected, int actual)
        {
            if (expected != actual) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Require(string text, string token)
        {
            if (!text.Contains(token)) throw new Exception("Expected procurement CSV token: " + token);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
