using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityEvidenceXlsxHardeningSmoke
    {
        internal static void Run()
        {
            ProjectedRowCapacityAcceptsExactBoundaryAndRejectsOverflow();
            WorkbookPreservesDeterministicEvidenceOrderAndProvenance();
            PublicationFailurePreservesExistingDestination();
        }

        private static void ProjectedRowCapacityAcceptsExactBoundaryAndRejectsOverflow()
        {
            var exact = InvokeAddProjectedRows(0L, 1048574, 0);
            Require(exact == 1048575L, "Quantity evidence XLSX exact data-row boundary was rejected.");

            var cumulativeExact = InvokeAddProjectedRows(1048574L, 0, 0);
            Require(cumulativeExact == 1048575L, "Quantity evidence XLSX cumulative exact boundary was rejected.");

            RequireProjectedRowOverflow(0L, 1048575, 0);
            RequireProjectedRowOverflow(1048575L, 0, 0);
        }

        private static long InvokeAddProjectedRows(long current, int contributions, int adjustments)
        {
            var method = typeof(XlsxQuantityEvidenceExporter).GetMethod(
                "AddProjectedRows",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new Exception("Missing quantity evidence XLSX projected-row capacity helper.");

            try
            {
                return (long)(method.Invoke(null, new object[] { current, contributions, adjustments })
                    ?? throw new Exception("Projected-row capacity helper returned null."));
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void RequireProjectedRowOverflow(long current, int contributions, int adjustments)
        {
            var rejected = false;
            try
            {
                InvokeAddProjectedRows(current, contributions, adjustments);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                rejected = ex.Message.IndexOf("1048575", StringComparison.Ordinal) >= 0;
            }

            Require(rejected, "Quantity evidence XLSX accepted a projected row count above Excel's data-row ceiling.");
        }

        private static void WorkbookPreservesDeterministicEvidenceOrderAndProvenance()
        {
            var root = TempDirectory("quantity-evidence-xlsx-hardening");
            try
            {
                var first = QuantityContribution.Create(
                    "gross-a",
                    "Gross A",
                    QuantityEvidenceOperation.Add,
                    "A",
                    2m,
                    QuantityEvidenceSelector.ForEntity("HANDLE-A"),
                    new[] { new QuantityEvidenceOperand("length", 2m, "m") });
                var second = QuantityContribution.Create(
                    "gross-b",
                    "Gross B",
                    QuantityEvidenceOperation.Add,
                    "B",
                    3m,
                    QuantityEvidenceSelector.ForEntity("HANDLE-B"));
                var explanation = QuantityExplanation.Create(
                    "ELEMENT-1",
                    "Beam",
                    "Length",
                    "m",
                    5m,
                    5m,
                    new[] { second, first });

                var expected = QuantityEvidenceExportProjection.Create(explanation);
                var path = Path.Combine(root, "evidence.xlsx");
                XlsxQuantityEvidenceExporter.Export(path, new[] { explanation });
                var sheet = ReadEntry(path, "xl/worksheets/sheet1.xml");

                var previous = -1;
                foreach (var row in expected)
                {
                    var position = sheet.IndexOf(row.EvidenceId, StringComparison.Ordinal);
                    Require(position > previous, "Quantity evidence XLSX changed deterministic projection row order.");
                    previous = position;
                }

                Require(sheet.IndexOf("ELEMENT-1", StringComparison.Ordinal) >= 0,
                    "Quantity evidence XLSX lost subject provenance.");
                Require(sheet.IndexOf("HANDLE-A", StringComparison.Ordinal) >= 0,
                    "Quantity evidence XLSX lost selector provenance.");
                Require(sheet.IndexOf("length=2 m", StringComparison.Ordinal) >= 0,
                    "Quantity evidence XLSX lost operand provenance.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static void PublicationFailurePreservesExistingDestination()
        {
            var root = TempDirectory("quantity-evidence-xlsx-atomic");
            try
            {
                var path = Path.Combine(root, "existing.xlsx");
                const string sentinel = "existing-workbook-must-survive";
                File.WriteAllText(path, sentinel, new UTF8Encoding(false));

                var rows = new List<QuantityEvidenceExportRecord>
                {
                    new QuantityEvidenceExportRecord
                    {
                        EvidenceId = "qe_atomic",
                        RecordKind = "Summary",
                        SubjectKey = "ELEMENT-ATOMIC",
                        Category = "Beam",
                        Metric = "Length",
                        Unit = "m",
                        GrossValue = 1m,
                        NetValue = 1m,
                        Value = 1m
                    }
                };

                var method = typeof(XlsxQuantityEvidenceExporter)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                    .Single(candidate => candidate.Name == "WritePackage" && candidate.GetParameters().Length == 3);

                Action<string, string> failCommit = (temporaryPath, destinationPath) =>
                {
                    Require(File.Exists(temporaryPath), "Quantity evidence XLSX temp package was not built before commit handoff.");
                    Require(string.Equals(Path.GetFullPath(path), destinationPath, StringComparison.Ordinal),
                        "Quantity evidence XLSX commit handoff targeted an unexpected destination.");
                    throw new IOException("Injected publication failure.");
                };

                var failed = false;
                try
                {
                    method.Invoke(null, new object[] { path, rows, failCommit });
                }
                catch (TargetInvocationException ex) when (ex.InnerException is IOException)
                {
                    failed = true;
                }

                Require(failed, "Injected quantity evidence XLSX publication failure did not surface.");
                Require(File.ReadAllText(path, Encoding.UTF8) == sentinel,
                    "Quantity evidence XLSX publication failure destroyed or changed the existing destination.");
                Require(Directory.GetFiles(root).Length == 1,
                    "Quantity evidence XLSX publication failure left a temporary package behind.");
            }
            finally
            {
                DeleteDirectory(root);
            }
        }

        private static string ReadEntry(string path, string entryName)
        {
            using (var archive = ZipFile.OpenRead(path))
            {
                var entry = archive.GetEntry(entryName) ?? throw new Exception("Missing XLSX entry: " + entryName + ".");
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        private static string TempDirectory(string name)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-smoke-" + name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}
