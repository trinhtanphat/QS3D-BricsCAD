using System;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class BbsRegressionSmoke
    {
        public static void Run()
        {
            RebarWeightRejectsNonFiniteValues();
            ScheduleRejectsArithmeticOverflow();
            SpacingRejectsArithmeticOverflow();
            AggregateRejectsOverflow();
            CuttingLengthFallbackIsLazy();
            CsvRejectsInvalidRowsBeforeReplace();
        }

        private static void RebarWeightRejectsNonFiniteValues()
        {
            Throws<ArgumentOutOfRangeException>(() => RebarWeight.KilogramsPerMeter(double.NaN));
            Throws<ArgumentOutOfRangeException>(() => RebarWeight.TotalKilograms(16d, double.PositiveInfinity));
            Throws<OverflowException>(() => RebarWeight.KilogramsPerMeter(double.MaxValue));
        }

        private static void ScheduleRejectsArithmeticOverflow()
        {
            Throws<OverflowException>(() => RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { ElementId = "ADD", Notation = "1D16", CuttingLengthM = double.MaxValue, LapLengthM = double.MaxValue }
            }));

            Throws<OverflowException>(() => RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { ElementId = "MUL", Notation = "2D16", CuttingLengthM = double.MaxValue }
            }));
        }

        private static void SpacingRejectsArithmeticOverflow()
        {
            Throws<OverflowException>(() => RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { ElementId = "SPACE", Notation = "D8@100", CuttingLengthM = 1d, DistributionLengthM = double.MaxValue }
            }));
        }

        private static void AggregateRejectsOverflow()
        {
            Throws<OverflowException>(() => RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { ElementId = "Q1", Notation = "2147483647D1", CuttingLengthM = 1d },
                new RebarScheduleInput { ElementId = "Q2", Notation = "1D1", CuttingLengthM = 1d }
            }));
        }

        private static void CuttingLengthFallbackIsLazy()
        {
            var project = new ProjectState("bbs-lazy", "BBS Lazy");
            var element = new ProjectElement("B1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["RebarNotation"] = "1D16";
            element.Properties["RebarCuttingLengthM"] = "2";
            element.Properties["LengthM"] = "not-a-number";
            project.Elements.Add(element);

            var rows = ProjectRebarScheduleBuilder.Build(project);
            Equal(1, rows.Count);
            Near(2d, rows.Single().CuttingLengthM);
        }

        private static void CsvRejectsInvalidRowsBeforeReplace()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-bbs-csv-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "bbs.csv");
            try
            {
                File.WriteAllText(path, "ORIGINAL");
                var invalid = new RebarScheduleRow
                {
                    ElementId = "E1", BarMark = "B1", ShapeCode = "00", Notation = "1D16",
                    DiameterMm = 16d, Quantity = 0, CuttingLengthM = 2d, TotalLengthM = 2d,
                    UnitWeightKgM = RebarWeight.KilogramsPerMeter(16d), NetWeightKg = 3.16d, WastePercent = 0d, TotalWeightKg = 3.16d
                };
                Throws<ArgumentOutOfRangeException>(() => RebarCsvExporter.Export(path, new[] { invalid }));
                Equal("ORIGINAL", File.ReadAllText(path));
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
