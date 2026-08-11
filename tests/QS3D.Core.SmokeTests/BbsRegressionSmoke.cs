using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using QS3D.Core.Diagnostics;
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
            MalformedCompoundNotationRejected();
            ScheduleRejectsArithmeticOverflow();
            SpacingRejectsArithmeticOverflow();
            SpacingNearIntegerDoesNotAddPhantomBar();
            SpacingRealOverrunIsNotSnappedAtLargeScale();
            DecimalNotationIsInvariantAndRoundTrips();
            AggregateRejectsOverflow();
            ProjectScheduleRejectsNullSemanticEntry();
            ProjectScheduleRejectsDuplicateSemanticIdentity();
            CuttingLengthFallbackIsLazy();
            FabricationProvenanceFlowsToExports();
            CsvRejectsInvalidRowsBeforeReplace();
            XlsxRejectsWorksheetOverflowBeforeMutation();
        }

        private static void RebarWeightRejectsNonFiniteValues()
        {
            Throws<ArgumentOutOfRangeException>(() => RebarWeight.KilogramsPerMeter(double.NaN));
            Throws<ArgumentOutOfRangeException>(() => RebarWeight.TotalKilograms(16d, double.PositiveInfinity));
            Throws<OverflowException>(() => RebarWeight.KilogramsPerMeter(double.MaxValue));
        }

        private static void MalformedCompoundNotationRejected()
        {
            Throws<FormatException>(() => RebarNotationParser.Parse("2D10++2D12"));
            Throws<FormatException>(() => RebarNotationParser.Parse("+2D10"));
            Throws<FormatException>(() => RebarNotationParser.Parse("2D10+"));
            Equal(2, RebarNotationParser.Parse("2D10+2D12").Count);
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

        private static void SpacingNearIntegerDoesNotAddPhantomBar()
        {
            var exact = RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { ElementId = "EXACT", Notation = "D8@150", CuttingLengthM = 1d, DistributionLengthM = 16.35d }
            }).Single();
            Equal(110, exact.Quantity);

            var actualOverrun = RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { ElementId = "OVER", Notation = "D8@150", CuttingLengthM = 1d, DistributionLengthM = 16.350001d }
            }).Single();
            Equal(111, actualOverrun.Quantity);
        }

        private static void SpacingRealOverrunIsNotSnappedAtLargeScale()
        {
            var actualOverrun = RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput
                {
                    ElementId = "LARGE-OVER",
                    Notation = "D8@1",
                    CuttingLengthM = 1d,
                    DistributionLengthM = 2000000.000001d
                }
            }).Single();

            Equal(2000000002, actualOverrun.Quantity);
        }

        private static void DecimalNotationIsInvariantAndRoundTrips()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("vi-VN");
                var text = RebarNotationParser.Parse("D12.5@150.5").Single().ToString();
                Equal("D12.5@150.5", text);
                var reparsed = RebarNotationParser.Parse(text).Single();
                Near(12.5d, reparsed.DiameterMm);
                Near(150.5d, reparsed.SpacingMm!.Value);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        private static void AggregateRejectsOverflow()
        {
            Throws<OverflowException>(() => RebarScheduleBuilder.Build(new[]
            {
                new RebarScheduleInput { ElementId = "Q1", Notation = "2147483647D1", CuttingLengthM = 1d },
                new RebarScheduleInput { ElementId = "Q2", Notation = "1D1", CuttingLengthM = 1d }
            }));
        }

        private static void ProjectScheduleRejectsNullSemanticEntry()
        {
            var project = new ProjectState("bbs-null", "BBS Null");
            project.Elements.Add(ScheduledElement("B1"));
            project.Elements.Add(null!);
            Throws<InvalidOperationException>(() => ProjectRebarScheduleBuilder.Build(project));
        }

        private static void ProjectScheduleRejectsDuplicateSemanticIdentity()
        {
            var project = new ProjectState("bbs-duplicate", "BBS Duplicate");
            project.Elements.Add(ScheduledElement("B1"));
            project.Elements.Add(new ProjectElement("b1", ElementCategory.Room, string.Empty, string.Empty, string.Empty));
            Throws<InvalidOperationException>(() => ProjectRebarScheduleBuilder.Build(project));
        }

        private static ProjectElement ScheduledElement(string id)
        {
            var element = new ProjectElement(id, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["RebarNotation"] = "1D16";
            element.Properties["RebarCuttingLengthM"] = "2";
            return element;
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

        private static void FabricationProvenanceFlowsToExports()
        {
            const string standard = "STD-X";
            const string revision = "REV-A";
            var project = new ProjectState("bbs-fab", "BBS Fabrication");
            var element = new ProjectElement("B-FAB", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties["RebarNotation"] = "1D16";
            element.Properties["RebarCuttingLengthM"] = "2";
            element.Properties[RebarFabricationQualificationHealthService.StatusPropertyKey] = "Approved";
            element.Properties[RebarFabricationQualificationHealthService.StandardCodePropertyKey] = standard;
            element.Properties[RebarFabricationQualificationHealthService.DetailingRevisionPropertyKey] = revision;
            project.Elements.Add(element);

            var rows = ProjectRebarScheduleBuilder.Build(project);
            var row = rows.Single();
            Equal("Approved", row.FabricationStatus);
            Equal(standard, row.FabricationStandardCode);
            Equal(revision, row.FabricationDetailingRevision);

            var csv = RebarCsvExporter.ToCsv(rows);
            Require(csv, "FabricationStatus,FabricationStandardCode,FabricationDetailingRevision");
            Require(csv, "\"Approved\",\"" + standard + "\",\"" + revision + "\"");

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-bbs-fab-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "bbs.xlsx");
            try
            {
                XlsxRebarScheduleExporter.Export(path, rows);
                using (var stream = File.OpenRead(path))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
                using (var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open()))
                {
                    var sheet = reader.ReadToEnd();
                    Require(sheet, "Fabrication Status");
                    Require(sheet, "Standard Code");
                    Require(sheet, "Detailing Revision");
                    Require(sheet, "Approved");
                    Require(sheet, standard);
                    Require(sheet, revision);
                }
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
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

        private static void XlsxRejectsWorksheetOverflowBeforeMutation()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-bbs-xlsx-limit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "bbs.xlsx");
            try
            {
                File.WriteAllText(path, "ORIGINAL");
                var oversized = new OversizedBbsRows(1048576);
                Throws<ArgumentOutOfRangeException>(() => XlsxRebarScheduleExporter.Export(path, oversized));
                Equal(0, oversized.IndexerReads);
                Equal("ORIGINAL", File.ReadAllText(path));
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private sealed class OversizedBbsRows : IReadOnlyList<RebarScheduleRow>
        {
            public OversizedBbsRows(int count) { Count = count; }
            public int Count { get; }
            public int IndexerReads { get; private set; }
            public RebarScheduleRow this[int index]
            {
                get
                {
                    IndexerReads++;
                    throw new InvalidOperationException("Oversized BBS rows must be rejected before indexing.");
                }
            }
            public IEnumerator<RebarScheduleRow> GetEnumerator() => throw new InvalidOperationException("Oversized BBS rows must be rejected before enumeration.");
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static void Require(string text, string token)
        {
            if (!text.Contains(token)) throw new Exception("Expected BBS export token: " + token);
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
