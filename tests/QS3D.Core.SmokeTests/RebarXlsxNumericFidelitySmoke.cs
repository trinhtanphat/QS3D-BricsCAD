using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarXlsxNumericFidelitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PreservesFiniteValuesAcrossCulture();
            CanonicalizesSignedZero();
        }

        private static void PreservesFiniteValuesAcrossCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            var path = TempPath();
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                var row = NewRow();
                row.CuttingLengthM = 0.123456789d;

                XlsxRebarScheduleExporter.Export(path, new[] { row });
                AssertRoundTrips(ReadSheet(path), "G2", row.CuttingLengthM);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
                SafeDelete(path);
            }
        }

        private static void CanonicalizesSignedZero()
        {
            var path = TempPath();
            try
            {
                var row = NewRow();
                row.WastePercent = -0d;

                XlsxRebarScheduleExporter.Export(path, new[] { row });
                var value = CellValue(ReadSheet(path), "K2");
                Assert(value == "0", "Rebar XLSX signed zero must serialize as canonical numeric text 0, got: " + value);
            }
            finally
            {
                SafeDelete(path);
            }
        }

        private static RebarScheduleRow NewRow()
        {
            return new RebarScheduleRow
            {
                ElementId = "E1",
                BarMark = "B1",
                ShapeCode = "00",
                Notation = "1T16",
                DiameterMm = 16d,
                Quantity = 1,
                CuttingLengthM = 1d,
                TotalLengthM = 1d,
                UnitWeightKgM = 1d,
                NetWeightKg = 1d,
                WastePercent = 0d,
                TotalWeightKg = 1d,
                FabricationStatus = string.Empty,
                FabricationStandardCode = string.Empty,
                FabricationDetailingRevision = string.Empty
            };
        }

        private static string ReadSheet(string path)
        {
            using var archive = ZipFile.OpenRead(path);
            var entry = archive.GetEntry("xl/worksheets/sheet1.xml")
                ?? throw new InvalidOperationException("Rebar XLSX worksheet entry is missing.");
            using var reader = new StreamReader(entry.Open());
            return reader.ReadToEnd();
        }

        private static void AssertRoundTrips(string sheet, string cellRef, double expected)
        {
            var text = CellValue(sheet, cellRef);
            Assert(text.IndexOf(',') < 0, "Rebar XLSX numeric worksheet text must be culture invariant: " + text);
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var actual))
                throw new InvalidOperationException("Rebar XLSX numeric worksheet text is not invariant-double parseable: " + text);
            Assert(BitConverter.DoubleToInt64Bits(actual) == BitConverter.DoubleToInt64Bits(expected),
                "Rebar XLSX numeric worksheet value lost round-trip fidelity for " + cellRef + ": " + text);
        }

        private static string CellValue(string sheet, string cellRef)
        {
            var cellStart = sheet.IndexOf("<c r=\"" + cellRef + "\"", StringComparison.Ordinal);
            Assert(cellStart >= 0, "Missing Rebar XLSX worksheet cell " + cellRef + ".");
            var valueStart = sheet.IndexOf("<v>", cellStart, StringComparison.Ordinal);
            Assert(valueStart >= 0, "Missing Rebar XLSX numeric value for worksheet cell " + cellRef + ".");
            valueStart += 3;
            var valueEnd = sheet.IndexOf("</v>", valueStart, StringComparison.Ordinal);
            Assert(valueEnd >= valueStart, "Unterminated Rebar XLSX numeric value for worksheet cell " + cellRef + ".");
            return sheet.Substring(valueStart, valueEnd - valueStart);
        }

        private static string TempPath() => Path.Combine(Path.GetTempPath(), "qs3d-rebar-numeric-" + Guid.NewGuid().ToString("N") + ".xlsx");

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
