using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageCsvBoundedWriteSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            StrictUtf8RoundTripPreservesProvenance();
            FormulaPrefixesRemainFailClosed();
            ExistingCoverageCsvSurvivesBudgetFailure();
            ToCsvHonorsTheSameByteCeiling();
        }

        private static void StrictUtf8RoundTripPreservesProvenance()
        {
            var path = Path.Combine(Path.GetTempPath(), "measurement-coverage-valid-" + Guid.NewGuid().ToString("N") + ".csv");
            try
            {
                var matrix = CreateMatrix(
                    new[] { CreateCell("QTY-界", "MAP-界", "CLASS-界", "WORK-界", "ELEMENT-界") },
                    CreateProvenance("Dự-án-界", "FP-界", 37L, new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc)));
                MeasurementWorkItemCoverageCsvExporter.Export(path, matrix);
                var csv = File.ReadAllText(path, new UTF8Encoding(false, true));
                foreach (var token in new[] { "QTY-界", "MAP-界", "WORK-界", "ELEMENT-界", "Dự-án-界", "FP-界", "37", "2026-09-10T08:00:00.0000000Z" })
                {
                    if (!csv.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException("Measurement coverage CSV strict UTF-8/provenance round-trip lost token: " + token);
                }
                if (!csv.Contains("\r\n", StringComparison.Ordinal))
                    throw new InvalidOperationException("Measurement coverage CSV bounded smoke: CRLF contract was not preserved.");
            }
            finally { TryDelete(path); }
        }

        private static void FormulaPrefixesRemainFailClosed()
        {
            var matrix = CreateMatrix(
                new[] { CreateCell("=SUM(A1:A2)", "MAP", "CLASS", "WORK", "ELEMENT") },
                null);
            try
            {
                MeasurementWorkItemCoverageCsvExporter.ToCsv(matrix);
                throw new InvalidOperationException("Measurement coverage CSV semantic formula prefix unexpectedly exported.");
            }
            catch (InvalidDataException error) when (error.Message.Contains("spreadsheet formula prefix", StringComparison.Ordinal)) { }
        }

        private static void ExistingCoverageCsvSurvivesBudgetFailure()
        {
            var path = Path.Combine(Path.GetTempPath(), "measurement-coverage-bounded-" + Guid.NewGuid().ToString("N") + ".csv");
            const string sentinel = "KEEP-EXISTING-MEASUREMENT-COVERAGE";
            File.WriteAllText(path, sentinel, Encoding.UTF8);
            try
            {
                try
                {
                    MeasurementWorkItemCoverageCsvExporter.Export(path, CreateHostileMultibyteMatrix());
                    throw new InvalidOperationException("Measurement coverage CSV byte budget unexpectedly published.");
                }
                catch (InvalidDataException error) when (error.Message.Contains("bounded output limit", StringComparison.Ordinal)) { }

                var preserved = File.ReadAllText(path, Encoding.UTF8);
                if (!preserved.Contains(sentinel, StringComparison.Ordinal))
                    throw new InvalidOperationException("Measurement coverage CSV bounded smoke: destination sentinel was replaced after failure.");
                var directory = Path.GetDirectoryName(path)!;
                if (Directory.GetFiles(directory, "." + Path.GetFileName(path) + ".*.tmp").Length != 0)
                    throw new InvalidOperationException("Measurement coverage CSV bounded smoke: owned temp artifact leaked after failure.");
            }
            finally
            {
                TryDelete(path);
                var directory = Path.GetDirectoryName(path)!;
                foreach (var temp in Directory.GetFiles(directory, "." + Path.GetFileName(path) + ".*.tmp")) TryDelete(temp);
            }
        }

        private static void ToCsvHonorsTheSameByteCeiling()
        {
            try
            {
                _ = MeasurementWorkItemCoverageCsvExporter.ToCsv(CreateHostileMultibyteMatrix());
                throw new InvalidOperationException("Measurement coverage ToCsv byte budget unexpectedly materialized.");
            }
            catch (InvalidDataException error) when (error.Message.Contains("bounded output limit", StringComparison.Ordinal)) { }
        }

        private static MeasurementWorkItemCoverageMatrix CreateHostileMultibyteMatrix()
        {
            const int cellCount = 150;
            var prefix = new string('界', 32750);
            var cells = new List<MeasurementWorkItemCoverageMatrixCell>(cellCount);
            for (var i = 0; i < cellCount; i++)
            {
                var token = prefix + i.ToString("D3", CultureInfo.InvariantCulture);
                cells.Add(CreateCell(token, token, token, token, token));
            }
            return CreateMatrix(cells, null);
        }

        private static MeasurementWorkItemCoverageMatrixCell CreateCell(
            string measurementItemId,
            string mappingId,
            string classificationId,
            string workItemId,
            string elementId)
        {
            var ctor = typeof(MeasurementWorkItemCoverageMatrixCell).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (MeasurementWorkItemCoverageMatrixCell)ctor.Invoke(new object[]
            {
                default(ElementCategory),
                measurementItemId,
                mappingId,
                classificationId,
                workItemId,
                true,
                Array.Empty<MeasurementWorkItemCoverageIssue>(),
                1,
                new[] { elementId }
            });
        }

        private static MeasurementWorkItemCoverageProvenance CreateProvenance(
            string projectId,
            string fingerprint,
            long changeVersion,
            DateTime updatedUtc)
        {
            var ctor = typeof(MeasurementWorkItemCoverageProvenance).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (MeasurementWorkItemCoverageProvenance)ctor.Invoke(new object[] { projectId, fingerprint, changeVersion, updatedUtc });
        }

        private static MeasurementWorkItemCoverageMatrix CreateMatrix(
            IEnumerable<MeasurementWorkItemCoverageMatrixCell> cells,
            MeasurementWorkItemCoverageProvenance? provenance)
        {
            var cellList = cells.ToList().AsReadOnly();
            var reportCtor = typeof(MeasurementWorkItemCoverageReport).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            var report = (MeasurementWorkItemCoverageReport)reportCtor.Invoke(new object[]
            {
                Array.Empty<MeasurementWorkItemCoverageReportRow>(), 0, 0, 0, 0
            });
            var matrixCtor = typeof(MeasurementWorkItemCoverageMatrix).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (MeasurementWorkItemCoverageMatrix)matrixCtor.Invoke(new object?[] { cellList, report, provenance });
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
