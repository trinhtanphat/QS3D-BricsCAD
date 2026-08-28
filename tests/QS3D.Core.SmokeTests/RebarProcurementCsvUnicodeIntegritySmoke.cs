using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarProcurementCsvUnicodeIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            LoneSurrogatesFailClosed();
            MalformedUnicodeHasNoFilesystemSideEffects();
            FormulaLeadingGradeFailsClosedWithoutFilesystemSideEffects();
            SupplementaryUnicodePreservesBomAndIdentity();
        }

        private static void LoneSurrogatesFailClosed()
        {
            Throws<EncoderFallbackException>(() =>
                RebarProcurementCsvExporter.ToCsv(new[] { BuildRow("group-high-\uD800", "CB400-V") }));
            Throws<EncoderFallbackException>(() =>
                RebarProcurementCsvExporter.ToCsv(new[] { BuildRow("group-low-\uDC00", "CB400-V") }));
        }

        private static void MalformedUnicodeHasNoFilesystemSideEffects()
        {
            var absentRoot = Path.Combine(
                Path.GetTempPath(),
                "qs3d-procurement-csv-unicode-absent-" + Guid.NewGuid().ToString("N"));
            var absentPath = Path.Combine(absentRoot, "nested", "procurement.csv");
            try
            {
                Throws<EncoderFallbackException>(() =>
                    RebarProcurementCsvExporter.Export(
                        absentPath,
                        new[] { BuildRow("group-high-\uD800", "CB400-V") }));
                True(!Directory.Exists(absentRoot),
                    "Malformed procurement CSV input must fail before creating the destination directory.");
            }
            finally
            {
                TryDeleteDirectory(absentRoot);
            }

            var existingRoot = Path.Combine(
                Path.GetTempPath(),
                "qs3d-procurement-csv-unicode-existing-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(existingRoot);
            var existingPath = Path.Combine(existingRoot, "procurement.csv");
            var sentinel = new byte[] { 0x51, 0x53, 0x33, 0x44 };
            File.WriteAllBytes(existingPath, sentinel);
            var beforeFiles = Directory.GetFiles(existingRoot).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            try
            {
                Throws<EncoderFallbackException>(() =>
                    RebarProcurementCsvExporter.Export(
                        existingPath,
                        new[] { BuildRow("group-low-\uDC00", "CB400-V") }));
                True(File.ReadAllBytes(existingPath).SequenceEqual(sentinel),
                    "Malformed procurement CSV input must not replace an existing destination.");
                var afterFiles = Directory.GetFiles(existingRoot).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                True(beforeFiles.SequenceEqual(afterFiles, StringComparer.Ordinal),
                    "Malformed procurement CSV input must not create a temporary publication file.");
            }
            finally
            {
                TryDeleteDirectory(existingRoot);
            }
        }

        private static void FormulaLeadingGradeFailsClosedWithoutFilesystemSideEffects()
        {
            foreach (var grade in new[] { "=SD390", "+SD390", "-SD390", "@SD390" })
            {
                Throws<InvalidDataException>(() =>
                    RebarProcurementCsvExporter.ToCsv(new[] { BuildRow("GROUP-1", grade) }));
            }
            Throws<InvalidDataException>(() =>
                RebarProcurementCsvExporter.ToCsv(new[] { BuildRow("=GROUP-1", "SD390") }));

            var absentRoot = Path.Combine(
                Path.GetTempPath(),
                "qs3d-procurement-csv-grade-identity-absent-" + Guid.NewGuid().ToString("N"));
            var absentPath = Path.Combine(absentRoot, "nested", "procurement.csv");
            try
            {
                Throws<InvalidDataException>(() =>
                    RebarProcurementCsvExporter.Export(
                        absentPath,
                        new[] { BuildRow("GROUP-1", "=SD390") }));
                True(!Directory.Exists(absentRoot),
                    "Formula-leading procurement grade must fail before creating the destination directory.");
            }
            finally
            {
                TryDeleteDirectory(absentRoot);
            }

            var existingRoot = Path.Combine(
                Path.GetTempPath(),
                "qs3d-procurement-csv-grade-identity-existing-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(existingRoot);
            var existingPath = Path.Combine(existingRoot, "procurement.csv");
            var sentinel = new byte[] { 0x52, 0x42, 0x41, 0x52 };
            File.WriteAllBytes(existingPath, sentinel);
            var beforeFiles = Directory.GetFiles(existingRoot).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            try
            {
                Throws<InvalidDataException>(() =>
                    RebarProcurementCsvExporter.Export(
                        existingPath,
                        new[] { BuildRow("GROUP-1", "@SD390") }));
                True(File.ReadAllBytes(existingPath).SequenceEqual(sentinel),
                    "Rejected procurement grade identity must preserve an existing destination.");
                var afterFiles = Directory.GetFiles(existingRoot).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                True(beforeFiles.SequenceEqual(afterFiles, StringComparer.Ordinal),
                    "Rejected procurement grade identity must not leave temporary publication files.");
            }
            finally
            {
                TryDeleteDirectory(existingRoot);
            }
        }

        private static void SupplementaryUnicodePreservesBomAndIdentity()
        {
            const string groupId = "group-rocket-\uD83D\uDE80";
            const string grade = "grade-rocket-\uD83D\uDE80";
            var row = BuildRow(groupId, grade);
            var expectedCsv = RebarProcurementCsvExporter.ToCsv(new[] { row });
            True(expectedCsv.IndexOf("\"" + groupId + "\"", StringComparison.Ordinal) >= 0,
                "Procurement CSV projection must preserve valid supplementary group identity ordinally.");
            True(expectedCsv.IndexOf("\"" + grade + "\"", StringComparison.Ordinal) >= 0,
                "Procurement CSV projection must preserve valid supplementary grade ordinally.");

            var root = Path.Combine(
                Path.GetTempPath(),
                "qs3d-procurement-csv-unicode-valid-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "procurement.csv");
            try
            {
                RebarProcurementCsvExporter.Export(path, new[] { row });
                var bytes = File.ReadAllBytes(path);
                True(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                    "Procurement CSV export must retain its UTF-8 BOM.");

                var strictUtf8 = new UTF8Encoding(false, true);
                var persisted = strictUtf8.GetString(bytes, 3, bytes.Length - 3);
                True(string.Equals(expectedCsv, persisted, StringComparison.Ordinal),
                    "Procurement CSV export must preserve valid supplementary Unicode ordinally.");
            }
            finally
            {
                TryDeleteDirectory(root);
            }
        }

        private static RebarProcurementSummary BuildRow(string groupId, string grade)
        {
            var demand = new RebarStockDemand(
                groupId,
                grade,
                16d,
                12d,
                new[] { new RebarCutRequirement("CUT-1", 6d, 1) },
                new RebarCutAllowancePolicy(0.01d, 0d));
            var result = RebarCuttingOptimizer.Plan(demand);
            return RebarProcurementReportBuilder.Build(new[] { result })[0];
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
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
