using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishXlsxNumericFidelitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PreservesTinyFiniteValuesAcrossCulture();
            CanonicalizesSignedZero();
        }

        private static void PreservesTinyFiniteValuesAcrossCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            var path = TempPath();
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                var row = NewRow();
                row.PrimaryQuantity = 1e-9;
                row.LengthM = 2e-12;
                row.AreaM2 = 3e-15;

                RoomFinishXlsxExporter.Export(path, new[] { row });
                var sheet = ReadSheet(path);

                AssertRoundTrips(sheet, "H2", row.PrimaryQuantity);
                AssertRoundTrips(sheet, "I2", row.LengthM);
                AssertRoundTrips(sheet, "J2", row.AreaM2);
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
                row.PrimaryQuantity = -0d;
                RoomFinishXlsxExporter.Export(path, new[] { row });
                var value = CellValue(ReadSheet(path), "H2");
                Assert(value == "0", "Signed zero must serialize as canonical numeric text 0, got: " + value);
            }
            finally
            {
                SafeDelete(path);
            }
        }

        private static RoomFinishScheduleRow NewRow()
        {
            var row = new RoomFinishScheduleRow
            {
                Floor = "F1",
                Room = "R1",
                Category = "FloorFinish",
                FamilyName = "Finish",
                Material = "Paint",
                UnitHint = "m2",
                Count = 1
            };
            row.ElementIds.Add("E1");
            row.RoomIds.Add("R1");
            return row;
        }

        private static string ReadSheet(string path)
        {
            using var archive = ZipFile.OpenRead(path);
            var entry = archive.GetEntry("xl/worksheets/sheet1.xml")
                ?? throw new InvalidOperationException("Room-finish XLSX worksheet entry is missing.");
            using var reader = new StreamReader(entry.Open());
            return reader.ReadToEnd();
        }

        private static void AssertRoundTrips(string sheet, string cellRef, double expected)
        {
            var text = CellValue(sheet, cellRef);
            Assert(text.IndexOf(',') < 0, "Numeric worksheet text must be culture invariant: " + text);
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var actual))
                throw new InvalidOperationException("Numeric worksheet text is not invariant-double parseable: " + text);
            Assert(BitConverter.DoubleToInt64Bits(actual) == BitConverter.DoubleToInt64Bits(expected),
                "Numeric worksheet value lost round-trip fidelity for " + cellRef + ": " + text);
        }

        private static string CellValue(string sheet, string cellRef)
        {
            var cellStart = sheet.IndexOf("<c r=\"" + cellRef + "\"", StringComparison.Ordinal);
            Assert(cellStart >= 0, "Missing worksheet cell " + cellRef + ".");
            var valueStart = sheet.IndexOf("<v>", cellStart, StringComparison.Ordinal);
            Assert(valueStart >= 0, "Missing numeric value for worksheet cell " + cellRef + ".");
            valueStart += 3;
            var valueEnd = sheet.IndexOf("</v>", valueStart, StringComparison.Ordinal);
            Assert(valueEnd >= valueStart, "Unterminated numeric value for worksheet cell " + cellRef + ".");
            return sheet.Substring(valueStart, valueEnd - valueStart);
        }

        private static string TempPath() => Path.Combine(Path.GetTempPath(), "qs3d-room-finish-numeric-" + Guid.NewGuid().ToString("N") + ".xlsx");

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
