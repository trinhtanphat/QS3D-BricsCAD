using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageCsvExporterSmoke
    {
        internal static void Run()
        {
            ProjectionPreservesMatrixTruthAndEscapesText();
            ProjectionIsCultureIndependentAndUsesCanonicalLineEndings();
            InvalidInputFailsClosed();
        }

        private static void ProjectionPreservesMatrixTruthAndEscapesText()
        {
            var csv = MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix());

            True(csv.StartsWith(
                "Category,MeasurementItemId,MappingId,ClassificationId,WorkItemId,IsReady,Issues,FindingCount,AffectedElementCount,AffectedElementIds\r\n",
                StringComparison.Ordinal),
                "Coverage CSV header mismatch.");
            Contains(csv, "\"Slab\",\"NetVolumeM3\",\"map-slab-volume\",\"class-slab\",\"work-slab-volume\",true,\"\",2,2,\"A,ready|B\"\"ready\"",
                "Ready coverage CSV row must preserve compact matrix truth and RFC4180 quoting.");
            Contains(csv, "\"OtherVolumeM3\",\"\",\"\",\"\",false,\"UnmappedWorkItem\",1,1,\"'=D-unmapped\"",
                "Unmapped coverage CSV row must preserve empty mapping identities and neutralize spreadsheet formulas.");
            Contains(csv, "\"Column\",\"\",\"\",\"\",\"\",false,\"MissingQuantity\",1,1,\"E-missing\"",
                "Missing-quantity coverage CSV row must not invent measurement or mapping identities.");
        }

        private static void ProjectionIsCultureIndependentAndUsesCanonicalLineEndings()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var first = MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix());
                CultureInfo.CurrentCulture = new CultureInfo("vi-VN");
                var second = MeasurementWorkItemCoverageCsvExporter.ToCsv(BuildMatrix());
                Equal(first, second, "Coverage CSV must not depend on current culture.");
                True(!first.Replace("\r\n", string.Empty).Contains("\n"),
                    "Coverage CSV must use canonical CRLF line endings only.");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        private static void InvalidInputFailsClosed()
        {
            Throws<ArgumentNullException>(() => MeasurementWorkItemCoverageCsvExporter.ToCsv(null!));
            Throws<ArgumentException>(() => MeasurementWorkItemCoverageCsvExporter.Export(" ", BuildMatrix()));
        }

        private static MeasurementWorkItemCoverageMatrix BuildMatrix()
        {
            var project = new ProjectState("coverage-csv", "Coverage CSV");
            project.Elements.Add(CleanQuantityElement("A,ready", ElementCategory.Slab, "NetVolumeM3", 2d));
            project.Elements.Add(CleanQuantityElement("B\"ready", ElementCategory.Slab, "NetVolumeM3", 4d));
            project.Elements.Add(CleanQuantityElement("=D-unmapped", ElementCategory.Slab, "OtherVolumeM3", 5d));

            var missing = new ProjectElement("E-missing", ElementCategory.Column);
            missing.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(missing);

            var report = MeasurementWorkItemCoverageReport.Create(
                MeasurementWorkItemCoverageEvaluator.Evaluate(project, Catalog()));
            return MeasurementWorkItemCoverageMatrix.Create(report);
        }

        private static ProjectElement CleanQuantityElement(string id, ElementCategory category, string quantityKey, double value)
        {
            var element = new ProjectElement(id, category);
            element.SetQuantity(quantityKey, value);
            element.MarkClean(ElementDirtyFlags.All);
            return element;
        }

        private static MeasurementWorkItemMappingCatalog Catalog() =>
            new MeasurementWorkItemMappingCatalog(new[]
            {
                new MeasurementWorkItemMapping(
                    "map-slab-volume",
                    ElementCategory.Slab,
                    "NetVolumeM3",
                    "class-slab",
                    "work-slab-volume")
            });

        private static void Contains(string actual, string expected, string message)
        {
            if (actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Expected fragment=" + expected + ". Actual=" + actual + ".");
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
