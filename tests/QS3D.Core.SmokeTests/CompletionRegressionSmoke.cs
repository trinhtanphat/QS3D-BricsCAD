using System;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Model;
using QS3D.Core.Rebar;
using QS3D.Core.Recognition;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class CompletionRegressionSmoke
    {
        public static void Run()
        {
            StairQuantities();
            RailingQuantities();
            EarthworkQuantities();
            CsvIsExcelSafeAndFinite();
            VietnameseRecognition();
        }

        private static void StairQuantities()
        {
            var project = new ProjectState("completion-stair", "Completion Stair");
            var stair = new ProjectElement("STAIR-1", ElementCategory.Stair, "", "", "");
            stair.Properties["WidthM"] = "1.2";
            stair.Properties["RunLengthM"] = "3";
            stair.Properties["TotalRiseM"] = "1.8";
            stair.Properties["ThicknessM"] = "0.15";
            stair.Properties["StepCount"] = "10";
            stair.Properties["TreadM"] = "0.3";
            stair.Properties["RiserM"] = "0.18";
            new StructuralRegenerator().Regenerate(project, stair);
            var expectedSlope = Math.Sqrt(3d * 3d + 1.8d * 1.8d);
            var expected = 1.2d * expectedSlope * 0.15d + 0.5d * 1.2d * 0.3d * 0.18d * 10d;
            Near(stair.Quantities["SlopeLengthM"], expectedSlope, "stair slope");
            Near(stair.Quantities["NetVolumeM3"], expected, "stair volume");
        }

        private static void RailingQuantities()
        {
            var project = new ProjectState("completion-railing", "Completion Railing");
            var railing = new ProjectElement("RAIL-1", ElementCategory.Railing, "", "", "");
            railing.Properties["LengthM"] = "5";
            railing.Properties["HeightM"] = "1.1";
            railing.Properties["PostSpacingM"] = "1.2";
            new StructuralRegenerator().Regenerate(project, railing);
            Near(railing.Quantities["PostCount"], 6d, "railing post count");
            Near(railing.Quantities["InfillAreaM2"], 5.5d, "railing infill area");
        }

        private static void EarthworkQuantities()
        {
            var project = new ProjectState("completion-earth", "Completion Earthwork");
            var earth = new ProjectElement("EARTH-1", ElementCategory.Earthwork, "", "", "");
            earth.Properties["ExcavationAreaM2"] = "20";
            earth.Properties["DepthM"] = "1.5";
            earth.Properties["BulkingFactor"] = "1.2";
            earth.Properties["BackfillM3"] = "5";
            new StructuralRegenerator().Regenerate(project, earth);
            Near(earth.Quantities["CutVolumeM3"], 30d, "earth cut");
            Near(earth.Quantities["BulkedVolumeM3"], 36d, "earth bulked");
            Near(earth.Quantities["NetExportM3"], 31d, "earth export");
        }

        private static void CsvIsExcelSafeAndFinite()
        {
            var row = new RebarScheduleRow
            {
                ElementId = "=1+1",
                BarMark = "+CMD",
                ShapeCode = "00",
                Notation = "2Ø16",
                DiameterMm = 16d,
                Quantity = 2,
                CuttingLengthM = 1d,
                TotalLengthM = 2d,
                UnitWeightKgM = 1.58d,
                NetWeightKg = 3.16d,
                WastePercent = 2d,
                TotalWeightKg = 3.2232d
            };
            var csv = RebarCsvExporter.ToCsv(new[] { row });
            if (!csv.Contains("\"'=1+1\"", StringComparison.Ordinal) || !csv.Contains("\"'+CMD\"", StringComparison.Ordinal)) throw new InvalidOperationException("CSV formula-injection guard failed.");
            row.TotalWeightKg = double.NaN;
            var threw = false;
            try { RebarCsvExporter.ToCsv(new[] { row }); } catch (ArgumentOutOfRangeException) { threw = true; }
            if (!threw) throw new InvalidOperationException("CSV non-finite guard failed.");
        }

        private static void VietnameseRecognition()
        {
            var snapshot = new EntitySnapshot("A1", "Polyline", "QS3D-Đào đất");
            var result = new RecognitionEngine().Suggest(snapshot);
            if (result.TopCandidate == null || result.TopCandidate.Category != ElementCategory.Earthwork) throw new InvalidOperationException("Vietnamese earthwork recognition failed.");
        }

        private static void Near(double actual, double expected, string name)
        {
            if (Math.Abs(actual - expected) > 1e-8) throw new InvalidOperationException(name + " mismatch: " + actual + " != " + expected);
        }
    }
}
