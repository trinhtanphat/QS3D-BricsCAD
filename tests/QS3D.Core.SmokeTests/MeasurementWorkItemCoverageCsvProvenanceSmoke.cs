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
    internal static class MeasurementWorkItemCoverageCsvProvenanceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            LegacyCsvContractRemainsStable();
            ProvenanceIsDetachedAndDeterministic();
            FormulaLeadingProvenanceIdentityFailsClosed();
            InvalidProvenanceInputFailsClosed();
        }

        private static void LegacyCsvContractRemainsStable()
        {
            var project = BuildProject("legacy-project", "legacy-fingerprint");
            var report = Report(project);
            var csv = MeasurementWorkItemCoverageCsvExporter.ToCsv(
                MeasurementWorkItemCoverageMatrix.Create(report));

            True(csv.StartsWith(
                "Category,MeasurementItemId,MappingId,ClassificationId,WorkItemId,IsReady,Issues,FindingCount,AffectedElementCount,AffectedElementIds\r\n",
                StringComparison.Ordinal),
                "Legacy matrix CSV header must remain byte-for-byte compatible.");
            True(!csv.Contains("SourceProjectId"), "Legacy matrix must not silently opt into provenance columns.");
        }

        private static void ProvenanceIsDetachedAndDeterministic()
        {
            var project = BuildProject("project-a", "fingerprint-a");
            var report = Report(project);
            var expectedVersion = project.ChangeVersion;
            var expectedUpdatedUtc = project.UpdatedUtc;
            var matrix = MeasurementWorkItemCoverageMatrix.Create(project, report);
            var first = MeasurementWorkItemCoverageCsvExporter.ToCsv(matrix);

            project.DrawingFingerprint = "fingerprint-after-export-snapshot";
            project.Touch();

            var second = MeasurementWorkItemCoverageCsvExporter.ToCsv(matrix);
            Equal(first, second, "Coverage CSV must be deterministic after source project mutation.");
            True(matrix.Provenance != null, "Project-aware matrix must capture provenance.");
            Equal("project-a", matrix.Provenance!.ProjectId, "Project id provenance mismatch.");
            Equal("fingerprint-a", matrix.Provenance.DrawingFingerprint, "Drawing fingerprint provenance mismatch.");
            Equal(expectedVersion, matrix.Provenance.ChangeVersion, "Change-version provenance mismatch.");
            Equal(expectedUpdatedUtc, matrix.Provenance.UpdatedUtc, "UpdatedUtc provenance mismatch.");

            var header = first.Split(new[] { "\r\n" }, StringSplitOptions.None)[0];
            True(header.EndsWith(
                ",SourceProjectId,SourceDrawingFingerprint,SourceChangeVersion,SourceUpdatedUtc",
                StringComparison.Ordinal),
                "Project-aware CSV must append the trace columns after all legacy columns.");
            True(first.Contains("\"project-a\",\"fingerprint-a\"," + expectedVersion + ",\"" +
                expectedUpdatedUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture) + "\""),
                "Coverage CSV row must carry exact source provenance.");
        }

        private static void FormulaLeadingProvenanceIdentityFailsClosed()
        {
            var projectId = BuildProject("=project-formula", "fingerprint-formula");
            Throws<InvalidDataException>(() => MeasurementWorkItemCoverageCsvExporter.ToCsv(
                MeasurementWorkItemCoverageMatrix.Create(projectId, Report(projectId))));

            var drawing = BuildProject("project-formula", "+fingerprint-formula");
            Throws<InvalidDataException>(() => MeasurementWorkItemCoverageCsvExporter.ToCsv(
                MeasurementWorkItemCoverageMatrix.Create(drawing, Report(drawing))));
        }

        private static void InvalidProvenanceInputFailsClosed()
        {
            Throws<ArgumentNullException>(() => MeasurementWorkItemCoverageProvenance.Capture(null!));
            var report = MeasurementWorkItemCoverageReport.Create(Array.Empty<MeasurementWorkItemCoverageFinding>());
            Throws<ArgumentNullException>(() => MeasurementWorkItemCoverageMatrix.Create(null!, report));
            Throws<ArgumentNullException>(() => MeasurementWorkItemCoverageMatrix.Create(report, null!));
        }

        private static ProjectState BuildProject(string id, string fingerprint)
        {
            var project = new ProjectState(id, "Coverage Provenance");
            project.DrawingFingerprint = fingerprint;
            var element = new ProjectElement("slab-a", ElementCategory.Slab);
            element.SetQuantity("NetVolumeM3", 3.25d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return project;
        }

        private static MeasurementWorkItemCoverageReport Report(ProjectState project)
        {
            var catalog = new MeasurementWorkItemMappingCatalog(new[]
            {
                new MeasurementWorkItemMapping(
                    "map-slab-volume",
                    ElementCategory.Slab,
                    "NetVolumeM3",
                    "class-slab",
                    "work-slab-volume")
            });
            return MeasurementWorkItemCoverageReport.Create(
                MeasurementWorkItemCoverageEvaluator.Evaluate(project, catalog));
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
