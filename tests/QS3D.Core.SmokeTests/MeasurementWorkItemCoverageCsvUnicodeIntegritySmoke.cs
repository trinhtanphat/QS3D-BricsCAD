using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageCsvUnicodeIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            LoneSurrogatesFailClosed();
            MalformedUnicodeHasNoFilesystemSideEffects();
            SupplementaryUnicodePreservesBomAndIdentity();
        }

        private static void LoneSurrogatesFailClosed()
        {
            ThrowsXmlInvalidMappingId(() =>
                MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix("map-high-\uD800")));
            ThrowsXmlInvalidMappingId(() =>
                MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix("map-low-\uDC00")));
        }

        private static void MalformedUnicodeHasNoFilesystemSideEffects()
        {
            var absentRoot = Path.Combine(
                Path.GetTempPath(),
                "qs3d-coverage-csv-unicode-absent-" + Guid.NewGuid().ToString("N"));
            var absentPath = Path.Combine(absentRoot, "nested", "coverage.csv");
            try
            {
                ThrowsXmlInvalidMappingId(() =>
                    MeasurementWorkItemCoverageCsvExporter.Export(absentPath, BuildMatrix("map-high-\uD800")));
                True(!Directory.Exists(absentRoot),
                    "Malformed coverage CSV input must fail before creating the destination directory.");
            }
            finally
            {
                TryDeleteDirectory(absentRoot);
            }

            var existingRoot = Path.Combine(
                Path.GetTempPath(),
                "qs3d-coverage-csv-unicode-existing-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(existingRoot);
            var existingPath = Path.Combine(existingRoot, "coverage.csv");
            var sentinel = new byte[] { 0x51, 0x53, 0x33, 0x44 };
            File.WriteAllBytes(existingPath, sentinel);
            var beforeFiles = Directory.GetFiles(existingRoot).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            try
            {
                ThrowsXmlInvalidMappingId(() =>
                    MeasurementWorkItemCoverageCsvExporter.Export(existingPath, BuildMatrix("map-low-\uDC00")));
                True(File.ReadAllBytes(existingPath).SequenceEqual(sentinel),
                    "Malformed coverage CSV input must not replace an existing destination.");
                var afterFiles = Directory.GetFiles(existingRoot).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                True(beforeFiles.SequenceEqual(afterFiles, StringComparer.Ordinal),
                    "Malformed coverage CSV input must not create a temporary publication file.");
            }
            finally
            {
                TryDeleteDirectory(existingRoot);
            }
        }

        private static void SupplementaryUnicodePreservesBomAndIdentity()
        {
            const string mappingId = "map-rocket-\uD83D\uDE80";
            var matrix = BuildMatrix(mappingId);
            var expectedCsv = MeasurementWorkItemCoverageCsvExporter.ToCsv(matrix);
            True(expectedCsv.IndexOf("\"" + mappingId + "\"", StringComparison.Ordinal) >= 0,
                "Coverage CSV projection must preserve valid supplementary mapping identity ordinally.");

            var root = Path.Combine(
                Path.GetTempPath(),
                "qs3d-coverage-csv-unicode-valid-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "coverage.csv");
            try
            {
                MeasurementWorkItemCoverageCsvExporter.Export(path, matrix);
                var bytes = File.ReadAllBytes(path);
                True(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                    "Coverage CSV export must retain its UTF-8 BOM.");

                var strictUtf8 = new UTF8Encoding(false, true);
                var persisted = strictUtf8.GetString(bytes, 3, bytes.Length - 3);
                True(string.Equals(expectedCsv, persisted, StringComparison.Ordinal),
                    "Coverage CSV export must preserve valid supplementary Unicode ordinally.");
            }
            finally
            {
                TryDeleteDirectory(root);
            }
        }

        private static MeasurementWorkItemCoverageMatrix BuildMatrix(string mappingId)
        {
            var project = new ProjectState("P-COVERAGE-CSV-UNICODE", "Coverage CSV Unicode");
            var element = new ProjectElement("E-COVERAGE-CSV-UNICODE", ElementCategory.Slab);
            element.SetQuantity("NetVolumeM3", 1d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var catalog = new MeasurementWorkItemMappingCatalog(new[]
            {
                new MeasurementWorkItemMapping(
                    mappingId,
                    ElementCategory.Slab,
                    "NetVolumeM3",
                    "class-slab-volume",
                    "work-slab-volume")
            });
            var report = MeasurementWorkItemCoverageReport.Create(
                MeasurementWorkItemCoverageEvaluator.Evaluate(project, catalog));
            return MeasurementWorkItemCoverageMatrix.Create(report);
        }

        private static void ThrowsXmlInvalidMappingId(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException ex) when (
                string.Equals(ex.ParamName, "mappingId", StringComparison.Ordinal) &&
                ex.InnerException is XmlException)
            {
                return;
            }
            throw new InvalidOperationException(
                "Expected canonical mappingId XML validation to reject malformed Unicode before CSV publication.");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch
            {
            }
        }
    }
}
