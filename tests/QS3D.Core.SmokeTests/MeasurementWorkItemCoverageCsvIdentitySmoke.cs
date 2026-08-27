using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageCsvIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            FormulaLeadingSemanticCellsFailClosed();
            InvalidIdentityFailsBeforeDirectoryCreation();
            InvalidIdentityPreservesExistingDestinationAndLeavesNoTempResidue();
            ValidUnicodeSemanticIdentityIsPreserved();
        }

        private static void FormulaLeadingSemanticCellsFailClosed()
        {
            AssertInvalidData(() => MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix(projectId: "=project")));
            AssertInvalidData(() => MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix(drawingFingerprint: "+drawing")));
            AssertInvalidData(() => MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix(elementId: "-E-1")));
            AssertInvalidData(() => MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix(measurementItemId: "@NetVolumeM3")));
            AssertInvalidData(() => MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix(mappingId: "=mapping")));
            AssertInvalidData(() => MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix(classificationId: "+classification")));
            AssertInvalidData(() => MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix(workItemId: "@work-item")));
        }

        private static void InvalidIdentityFailsBeforeDirectoryCreation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-coverage-csv-identity-" + Guid.NewGuid().ToString("N"));
            var destination = Path.Combine(root, "nested", "coverage.csv");
            try
            {
                AssertInvalidData(() => MeasurementWorkItemCoverageCsvExporter.Export(
                    destination,
                    BuildMatrix(projectId: "=project")));
                False(Directory.Exists(root), "Invalid semantic identity must fail before creating the destination directory.");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static void InvalidIdentityPreservesExistingDestinationAndLeavesNoTempResidue()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-coverage-csv-identity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var destination = Path.Combine(root, "coverage.csv");
            const string sentinel = "existing-workbook-content";
            File.WriteAllText(destination, sentinel);
            var before = Directory.GetFiles(root).Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray();

            try
            {
                AssertInvalidData(() => MeasurementWorkItemCoverageCsvExporter.Export(
                    destination,
                    BuildMatrix(workItemId: "=work-item")));
                Equal(sentinel, File.ReadAllText(destination), "Rejected identity must preserve the existing destination byte-for-byte.");

                var after = Directory.GetFiles(root).Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                SequenceEqual(before, after, "Rejected identity must not create a temporary CSV package.");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static void ValidUnicodeSemanticIdentityIsPreserved()
        {
            var matrix = BuildMatrix(
                projectId: "DựÁn-😀",
                drawingFingerprint: "BảnVẽ-梁",
                elementId: "E-😀",
                measurementItemId: "KhốiLượng-体積",
                mappingId: "ÁnhXạ-😀",
                classificationId: "PhânLoại-梁",
                workItemId: "CôngTác-😀");

            var csv = MeasurementWorkItemCoverageCsvExporter.ToCsv(matrix);
            Contains(csv, "\"DựÁn-😀\"", "Project provenance must preserve valid Unicode exactly.");
            Contains(csv, "\"BảnVẽ-梁\"", "Drawing provenance must preserve valid Unicode exactly.");
            Contains(csv, "\"E-😀\"", "Affected element identity must preserve valid Unicode exactly.");
            Contains(csv, "\"KhốiLượng-体積\"", "Measurement identity must preserve valid Unicode exactly.");
            Contains(csv, "\"ÁnhXạ-😀\"", "Mapping identity must preserve valid Unicode exactly.");
            Contains(csv, "\"PhânLoại-梁\"", "Classification identity must preserve valid Unicode exactly.");
            Contains(csv, "\"CôngTác-😀\"", "Work-item identity must preserve valid Unicode exactly.");
        }

        private static MeasurementWorkItemCoverageMatrix BuildMatrix(
            string projectId = "coverage-project",
            string drawingFingerprint = "coverage-drawing",
            string elementId = "E-1",
            string measurementItemId = "NetVolumeM3",
            string mappingId = "mapping-volume",
            string classificationId = "classification-volume",
            string workItemId = "work-volume")
        {
            var project = new ProjectState(projectId, "Coverage CSV identity");
            project.DrawingFingerprint = drawingFingerprint;

            var element = new ProjectElement(elementId, ElementCategory.Slab);
            element.SetQuantity(measurementItemId, 2d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var catalog = new MeasurementWorkItemMappingCatalog(new[]
            {
                new MeasurementWorkItemMapping(
                    mappingId,
                    ElementCategory.Slab,
                    measurementItemId,
                    classificationId,
                    workItemId)
            });
            var report = MeasurementWorkItemCoverageReport.Create(
                MeasurementWorkItemCoverageEvaluator.Evaluate(project, catalog));
            return MeasurementWorkItemCoverageMatrix.Create(project, report);
        }

        private static void AssertInvalidData(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidDataException)
            {
                return;
            }

            throw new InvalidOperationException("Expected InvalidDataException for formula-leading semantic identity.");
        }

        private static void Contains(string value, string expected, string message)
        {
            if (value.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Missing=" + expected + ".");
        }

        private static void False(bool condition, string message)
        {
            if (condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
        {
            if (!expected.SequenceEqual(actual)) throw new InvalidOperationException(message);
        }
    }
}
