using System;
using System.IO;
using System.IO.Compression;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageXlsxProvenanceSmoke
    {
        public static void Run()
        {
            ExportsProvenanceAfterStableQuantityColumns();
            RejectsElementCountMismatchBeforeReplacingDestination();
            RejectsOversizedProvenanceBeforeReplacingDestination();
            RejectsInvalidXmlControlCharacterBeforeReplacingDestination();
            RejectsUnpairedHighSurrogateBeforeCreatingDirectory();
            RejectsUnpairedLowSurrogateBeforeReplacingDestination();
            PreservesSupplementaryUnicodeProvenance();
            AcceptsExactExcelTextBoundary();
        }

        private static void ExportsProvenanceAfterStableQuantityColumns()
        {
            var path = TempPath();
            try
            {
                var row = NewRow();
                row.ProjectId = "P<&1";
                row.DrawingFingerprint = "DWG&<fingerprint>";
                row.SourceHandles.Add("BB2");
                row.SourceHandles.Add("AA1");

                MaterialUsageXlsxExporter.Export(path, new[] { row });
                var sheet = ReadSheet(path);

                Contains(sheet, "<dimension ref=\"A1:P2\"/>");
                Contains(sheet, ">Khối lượng (kg)</t>");
                Contains(sheet, ">Project ID</t>");
                Contains(sheet, ">Drawing fingerprint</t>");
                Contains(sheet, ">Element IDs</t>");
                Contains(sheet, ">Source Handles</t>");
                Contains(sheet, "P&lt;&amp;1");
                Contains(sheet, "DWG&amp;&lt;fingerprint&gt;");
                Contains(sheet, "E-002 | E-001");
                Contains(sheet, "BB2 | AA1");
                Contains(sheet, "<c r=\"H2\" s=\"2\"><v>2.5</v></c>");
                Contains(sheet, "<c r=\"K2\" s=\"2\"><v>2.5</v></c>");
            }
            finally
            {
                Delete(path);
            }
        }

        private static void RejectsElementCountMismatchBeforeReplacingDestination()
        {
            var path = TempPath();
            try
            {
                File.WriteAllText(path, "existing-destination");
                var row = NewRow();
                row.ElementIds.RemoveAt(row.ElementIds.Count - 1);

                Throws<ArgumentException>(() => MaterialUsageXlsxExporter.Export(path, new[] { row }));
                Equal("existing-destination", File.ReadAllText(path));
            }
            finally
            {
                Delete(path);
            }
        }

        private static void RejectsOversizedProvenanceBeforeReplacingDestination()
        {
            var path = TempPath();
            try
            {
                File.WriteAllText(path, "existing-destination");
                var row = NewRow();
                row.DrawingFingerprint = new string('x', 32768);

                Throws<ArgumentOutOfRangeException>(() => MaterialUsageXlsxExporter.Export(path, new[] { row }));
                Equal("existing-destination", File.ReadAllText(path));
            }
            finally
            {
                Delete(path);
            }
        }

        private static void RejectsInvalidXmlControlCharacterBeforeReplacingDestination()
        {
            var path = TempPath();
            try
            {
                File.WriteAllText(path, "existing-destination");
                var row = NewRow();
                row.ProjectId = "project\u0001bad";

                Throws<InvalidDataException>(() => MaterialUsageXlsxExporter.Export(path, new[] { row }));
                Equal("existing-destination", File.ReadAllText(path));
            }
            finally
            {
                Delete(path);
            }
        }

        private static void RejectsUnpairedHighSurrogateBeforeCreatingDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-material-usage-utf16-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "material-usage.xlsx");
            try
            {
                var row = NewRow();
                row.ProjectId = "PROJECT-\uD800";

                Throws<InvalidDataException>(() => MaterialUsageXlsxExporter.Export(path, new[] { row }));
                if (Directory.Exists(directory))
                    throw new Exception("Invalid provenance must fail before creating the destination directory.");
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
                catch { }
            }
        }

        private static void RejectsUnpairedLowSurrogateBeforeReplacingDestination()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-material-usage-utf16-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "material-usage.xlsx");
            try
            {
                File.WriteAllText(path, "existing-destination");
                var row = NewRow();
                row.DrawingFingerprint = "DRAWING-\uDC00";

                Throws<InvalidDataException>(() => MaterialUsageXlsxExporter.Export(path, new[] { row }));
                Equal("existing-destination", File.ReadAllText(path));
                var files = Directory.GetFiles(directory);
                if (files.Length != 1 || !string.Equals(files[0], path, StringComparison.Ordinal))
                    throw new Exception("Invalid provenance must not leave a temporary workbook package.");
            }
            finally
            {
                try { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
                catch { }
            }
        }

        private static void PreservesSupplementaryUnicodeProvenance()
        {
            var path = TempPath();
            try
            {
                var row = NewRow();
                row.ProjectId = "PROJECT-\U0001F680";
                row.ElementIds[0] = "E-\U0001F680";

                MaterialUsageXlsxExporter.Export(path, new[] { row });
                var sheet = ReadSheet(path);
                Contains(sheet, "PROJECT-\U0001F680");
                Contains(sheet, "E-\U0001F680 | E-001");
                if (sheet.IndexOf('\uFFFD') >= 0)
                    throw new Exception("Valid supplementary Unicode provenance must not be replaced.");
            }
            finally
            {
                Delete(path);
            }
        }

        private static void AcceptsExactExcelTextBoundary()
        {
            var path = TempPath();
            try
            {
                var row = NewRow();
                row.DrawingFingerprint = new string('f', 32767);
                MaterialUsageXlsxExporter.Export(path, new[] { row });
                if (!File.Exists(path)) throw new Exception("Expected exact-boundary workbook to be created.");
            }
            finally
            {
                Delete(path);
            }
        }

        private static MaterialUsageRow NewRow()
        {
            var row = new MaterialUsageRow
            {
                Floor = "Floor 1",
                MaterialName = "Concrete",
                UnitHint = "m3",
                Component = "Material",
                Category = "Slab",
                FamilyName = "Slab 200",
                ElementCount = 2,
                LengthM = 0d,
                AreaM2 = 10d,
                VolumeM3 = 2.5d,
                MassKg = 6000d,
                ProjectId = "PROJECT-1",
                DrawingFingerprint = "DRAWING-1"
            };
            row.ElementIds.Add("E-002");
            row.ElementIds.Add("E-001");
            return row;
        }

        private static string ReadSheet(string path)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (entry == null) throw new Exception("Material usage workbook is missing sheet1.xml.");
                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        private static string TempPath()
        {
            return Path.Combine(Path.GetTempPath(), "qs3d-material-usage-provenance-" + Guid.NewGuid().ToString("N") + ".xlsx");
        }

        private static void Delete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void Contains(string value, string expected)
        {
            if (value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected workbook XML to contain: " + expected);
        }

        private static void Equal(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
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
