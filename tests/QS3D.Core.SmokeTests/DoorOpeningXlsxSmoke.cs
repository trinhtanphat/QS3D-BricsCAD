using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningXlsxSmoke
    {
        public static void Run()
        {
            WritesTraceableWorkbookAndPreservesNumericRoundTrip();
            PreservesDisplayTextSanitization();
            AcceptsZeroHostRows();
            RejectsCardinalityMismatchBeforePublication();
            RejectsInvalidProvenanceBeforePublication();
            RejectsInvalidProvenanceBeforeDirectoryMutation();
        }

        private static void WritesTraceableWorkbookAndPreservesNumericRoundTrip()
        {
            var path = TempWorkbookPath("traceable");
            try
            {
                var row = new DoorOpeningScheduleRow
                {
                    ProjectId = "project-a",
                    DrawingFingerprint = "drawing-fingerprint-a",
                    Floor = "Tầng 1",
                    Category = "Door",
                    FamilyName = "Cửa D1",
                    Material = "Gỗ",
                    WidthM = 1e-9d,
                    HeightM = 2.2d,
                    SillHeightM = 0d,
                    ThicknessM = 0.1d,
                    Count = 2,
                    OpeningAreaM2 = 3.9d,
                    HostCount = 2
                };
                row.ElementIds.Add("d1");
                row.ElementIds.Add("d2");
                row.HostIds.Add("wall-a");
                row.HostIds.Add("wall-b");
                row.SourceHandles.Add("A1");
                row.SourceHandles.Add("B2");

                DoorOpeningXlsxExporter.Export(path, new List<DoorOpeningScheduleRow> { row });
                if (!File.Exists(path)) throw new Exception("Door/opening XLSX was not created.");
                using (var archive = ZipFile.OpenRead(path))
                {
                    var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new Exception("Door/opening XLSX worksheet is missing.");
                    if (archive.GetEntry("xl/workbook.xml") == null) throw new Exception("Door/opening XLSX workbook is missing.");
                    using (var reader = new StreamReader(worksheet.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        Contains(xml, "DT mở", "Door/opening XLSX area header is missing.");
                        Contains(xml, "3.9", "Door/opening XLSX numeric payload is missing.");
                        Contains(xml, "d1;d2", "Door/opening XLSX element provenance is missing.");
                        Contains(xml, "wall-a;wall-b", "Door/opening XLSX host provenance is missing.");
                        Contains(xml, "project-a", "Door/opening XLSX project provenance is missing.");
                        Contains(xml, "drawing-fingerprint-a", "Door/opening XLSX drawing provenance is missing.");
                        Contains(xml, "A1;B2", "Door/opening XLSX source-handle provenance is missing.");

                        var document = new XmlDocument();
                        document.LoadXml(xml);
                        var namespaces = new XmlNamespaceManager(document.NameTable);
                        namespaces.AddNamespace("s", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                        var widthNode = document.SelectSingleNode("/s:worksheet/s:sheetData/s:row[@r='2']/s:c[@r='E2']/s:v", namespaces);
                        if (widthNode == null) throw new Exception("Door/opening XLSX width cell is missing.");
                        var storedWidth = double.Parse(widthNode.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture);
                        if (storedWidth != row.WidthM)
                            throw new Exception("Door/opening XLSX numeric payload did not round-trip the source double.");
                    }
                }
            }
            finally { SafeDelete(path); }
        }

        private static void PreservesDisplayTextSanitization()
        {
            var path = TempWorkbookPath("display-sanitize");
            try
            {
                var row = ValidRow();
                row.FamilyName = "Invalid\u0001Family";
                DoorOpeningXlsxExporter.Export(path, new List<DoorOpeningScheduleRow> { row });
                using (var archive = ZipFile.OpenRead(path))
                {
                    var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new Exception("Sanitized door/opening XLSX worksheet is missing.");
                    using (var reader = new StreamReader(worksheet.Open()))
                    {
                        var xml = reader.ReadToEnd();
                        if (xml.IndexOf('\u0001') >= 0) throw new Exception("Door/opening XLSX retained an XML-invalid display control character.");
                        if (xml.IndexOf('\uFFFD') < 0) throw new Exception("Door/opening XLSX did not preserve the display-text sanitization replacement marker.");
                    }
                }
            }
            finally { SafeDelete(path); }
        }

        private static void AcceptsZeroHostRows()
        {
            var path = TempWorkbookPath("zero-host");
            try
            {
                var row = ValidRow();
                if (row.HostCount != 0 || row.HostIds.Count != 0) throw new Exception("Zero-host fixture is invalid.");
                DoorOpeningXlsxExporter.Export(path, new List<DoorOpeningScheduleRow> { row });
                if (!File.Exists(path)) throw new Exception("Door/opening XLSX rejected a valid zero-host row.");
            }
            finally { SafeDelete(path); }
        }

        private static void RejectsCardinalityMismatchBeforePublication()
        {
            VerifyInvalidInputPreservesDestination(
                "count-mismatch",
                row => row.Count = 2,
                "Count/Element IDs mismatch must fail closed.");

            VerifyInvalidInputPreservesDestination(
                "host-count-mismatch",
                row => row.HostCount = 1,
                "HostCount/Host IDs mismatch must fail closed.");
        }

        private static void RejectsInvalidProvenanceBeforePublication()
        {
            VerifyInvalidInputPreservesDestination(
                "project-control",
                row => row.ProjectId = "project\u0001bad",
                "Project ID XML control must fail closed.");

            VerifyInvalidInputPreservesDestination(
                "fingerprint-control",
                row => row.DrawingFingerprint = "drawing\u0001bad",
                "Drawing fingerprint XML control must fail closed.");

            VerifyInvalidInputPreservesDestination(
                "element-control",
                row => row.ElementIds[0] = "door\u0001bad",
                "Element ID XML control must fail closed.");

            VerifyInvalidInputPreservesDestination(
                "host-control",
                row =>
                {
                    row.HostIds.Add("wall\u0001bad");
                    row.HostCount = 1;
                },
                "Host ID XML control must fail closed.");

            VerifyInvalidInputPreservesDestination(
                "handle-control",
                row => row.SourceHandles[0] = "A1\u0001bad",
                "Source Handle XML control must fail closed.");
        }

        private static void RejectsInvalidProvenanceBeforeDirectoryMutation()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-door-opening-invalid-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "result.xlsx");
            try
            {
                var row = ValidRow();
                row.DrawingFingerprint = "drawing\u0001bad";
                Throws<ArgumentException>(
                    () => DoorOpeningXlsxExporter.Export(path, new List<DoorOpeningScheduleRow> { row }),
                    "Invalid provenance must fail before filesystem mutation.");
                if (Directory.Exists(directory))
                    throw new Exception("Invalid Door/opening XLSX provenance created the destination directory before failing.");
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
            }
        }

        private static void VerifyInvalidInputPreservesDestination(string suffix, Action<DoorOpeningScheduleRow> mutate, string message)
        {
            var path = TempWorkbookPath(suffix);
            try
            {
                File.WriteAllText(path, "ORIGINAL");
                var row = ValidRow();
                mutate(row);
                Throws<ArgumentException>(
                    () => DoorOpeningXlsxExporter.Export(path, new List<DoorOpeningScheduleRow> { row }),
                    message);
                if (!string.Equals(File.ReadAllText(path), "ORIGINAL", StringComparison.Ordinal))
                    throw new Exception(message + " Existing destination was modified.");
            }
            finally { SafeDelete(path); }
        }

        private static DoorOpeningScheduleRow ValidRow()
        {
            var row = new DoorOpeningScheduleRow
            {
                ProjectId = "project",
                DrawingFingerprint = "fingerprint",
                Floor = "Tầng 1",
                Category = "Door",
                FamilyName = "Door",
                Material = "Material",
                WidthM = 0.9d,
                HeightM = 2.2d,
                SillHeightM = 0d,
                ThicknessM = 0.1d,
                Count = 1,
                OpeningAreaM2 = 1.98d,
                HostCount = 0
            };
            row.ElementIds.Add("door-1");
            row.SourceHandles.Add("A1");
            return row;
        }

        private static string TempWorkbookPath(string suffix) =>
            Path.Combine(Path.GetTempPath(), "qs3d-door-opening-" + suffix + "-" + Guid.NewGuid().ToString("N") + ".xlsx");

        private static void Contains(string value, string token, string message)
        {
            if (value.IndexOf(token, StringComparison.Ordinal) < 0) throw new Exception(message);
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception(message + " Expected " + typeof(T).Name + ".");
        }

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
