using System;
using System.Globalization;
using System.IO;
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
            HostileMultibyteCoverageFailsClosed();
            ToCsvHonorsTheSameByteCeiling();
        }

        private static void StrictUtf8RoundTripPreservesProvenance()
        {
            var path = Path.Combine(Path.GetTempPath(), "measurement-coverage-valid-" + Guid.NewGuid().ToString("N") + ".csv");
            try
            {
                var matrix = BuildSingleMatrix(
                    "Dự-án-界",
                    "FP-界",
                    "ELEMENT-界",
                    "QTY-界",
                    "MAP-界",
                    "CLASS-界",
                    "WORK-界");
                var provenance = matrix.Provenance
                    ?? throw new InvalidOperationException("Measurement coverage bounded smoke: provenance was not captured.");
                MeasurementWorkItemCoverageCsvExporter.Export(path, matrix);
                var csv = File.ReadAllText(path, new UTF8Encoding(false, true));
                foreach (var token in new[]
                {
                    "QTY-界", "MAP-界", "CLASS-界", "WORK-界", "ELEMENT-界",
                    "Dự-án-界", "FP-界",
                    provenance.ChangeVersion.ToString(CultureInfo.InvariantCulture),
                    provenance.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture)
                })
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
            var matrix = BuildSingleMatrix(
                "=project-formula",
                "FP",
                "ELEMENT",
                "NetVolumeM3",
                "MAP",
                "CLASS",
                "WORK");
            try
            {
                MeasurementWorkItemCoverageCsvExporter.ToCsv(matrix);
                throw new InvalidOperationException("Measurement coverage CSV semantic formula prefix unexpectedly exported.");
            }
            catch (InvalidDataException error) when (error.Message.Contains("spreadsheet formula prefix", StringComparison.Ordinal)) { }
        }

        private static void HostileMultibyteCoverageFailsClosed()
        {
            ExistingCoverageCsvSurvivesBudgetFailure();
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
            const int elementCount = 700;
            const string quantityKey = "NetVolumeM3";
            var hostileSuffix = new string('界', 32750);
            var project = new ProjectState("coverage-bounded", "Coverage bounded hostile matrix");
            for (var i = 0; i < elementCount; i++)
            {
                var elementId = "E-" + i.ToString("D4", CultureInfo.InvariantCulture) + "-" + hostileSuffix;
                var element = new ProjectElement(elementId, ElementCategory.Slab);
                element.SetQuantity(quantityKey, 1d);
                element.MarkClean(ElementDirtyFlags.All);
                project.Elements.Add(element);
            }

            var report = MeasurementWorkItemCoverageReport.Create(
                MeasurementWorkItemCoverageEvaluator.Evaluate(project, Catalog(quantityKey, "MAP", "CLASS", "WORK")));
            return MeasurementWorkItemCoverageMatrix.Create(report);
        }

        private static MeasurementWorkItemCoverageMatrix BuildSingleMatrix(
            string projectId,
            string fingerprint,
            string elementId,
            string quantityKey,
            string mappingId,
            string classificationId,
            string workItemId)
        {
            var project = new ProjectState(projectId, "Coverage bounded smoke");
            project.DrawingFingerprint = fingerprint;
            var element = new ProjectElement(elementId, ElementCategory.Slab);
            element.SetQuantity(quantityKey, 1d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var report = MeasurementWorkItemCoverageReport.Create(
                MeasurementWorkItemCoverageEvaluator.Evaluate(
                    project,
                    Catalog(quantityKey, mappingId, classificationId, workItemId)));
            return MeasurementWorkItemCoverageMatrix.Create(project, report);
        }

        private static MeasurementWorkItemMappingCatalog Catalog(
            string quantityKey,
            string mappingId,
            string classificationId,
            string workItemId) =>
            new MeasurementWorkItemMappingCatalog(new[]
            {
                new MeasurementWorkItemMapping(
                    mappingId,
                    ElementCategory.Slab,
                    quantityKey,
                    classificationId,
                    workItemId)
            });

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
