using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageReportSmoke
    {
        internal static void Run()
        {
            ProjectionPreservesEvaluatorTruthAndCounts();
            ProjectionOrderingIsDeterministicAndCultureIndependent();
            ProjectionIsDetachedAndReadOnly();
            InvalidInputFailsClosed();
        }

        private static void ProjectionPreservesEvaluatorTruthAndCounts()
        {
            var project = BuildCoverageProject();
            var findings = MeasurementWorkItemCoverageEvaluator.Evaluate(project, Catalog());
            var report = MeasurementWorkItemCoverageReport.Create(findings);

            Equal(5, report.TotalCount, "Coverage report total count mismatch.");
            Equal(1, report.ReadyCount, "Coverage report ready count mismatch.");
            Equal(4, report.NotReadyCount, "Coverage report not-ready count mismatch.");
            Equal(1, report.MissingQuantityCount, "Coverage report missing-quantity count mismatch.");
            Equal(2, report.StaleQuantityCount, "Coverage report stale-quantity count mismatch.");
            Equal(2, report.UnmappedWorkItemCount, "Coverage report unmapped-work-item count mismatch.");

            var ready = report.Rows.Single(x => x.ElementId == "A-ready");
            True(ready.IsReady, "Mapped fresh evaluator finding must remain ready in the projection.");
            Equal("NetVolumeM3", ready.QuantityKey, "Ready report quantity key mismatch.");
            Equal(2d, ready.QuantityValue, "Ready report quantity value mismatch.");
            Equal("map-slab-volume", ready.MappingId, "Ready report mapping id mismatch.");
            Equal("class-slab", ready.ClassificationId, "Ready report classification id mismatch.");
            Equal("work-slab-volume", ready.WorkItemId, "Ready report work-item id mismatch.");
            Equal(0, ready.Issues.Count, "Ready report row must preserve the evaluator's empty issue list.");

            var staleUnmapped = report.Rows.Single(x => x.ElementId == "D-stale-unmapped");
            True(!staleUnmapped.IsReady, "Stale unmapped evaluator finding must remain not ready in the projection.");
            SequenceEqual(
                new[]
                {
                    MeasurementWorkItemCoverageIssue.StaleQuantity,
                    MeasurementWorkItemCoverageIssue.UnmappedWorkItem
                },
                staleUnmapped.Issues,
                "Coverage report must preserve overlapping evaluator reasons.");
            True(staleUnmapped.MappingId == null && staleUnmapped.ClassificationId == null && staleUnmapped.WorkItemId == null,
                "Unmapped coverage row must not invent mapping identity.");

            var missing = report.Rows.Single(x => x.ElementId == "E-missing");
            True(!missing.IsReady, "Missing-quantity evaluator finding must remain not ready in the projection.");
            True(missing.QuantityKey == null && !missing.QuantityValue.HasValue,
                "Missing-quantity report row must not invent quantity data.");
            SequenceEqual(
                new[] { MeasurementWorkItemCoverageIssue.MissingQuantity },
                missing.Issues,
                "Missing-quantity report reason mismatch.");
        }

        private static void ProjectionOrderingIsDeterministicAndCultureIndependent()
        {
            var findings = MeasurementWorkItemCoverageEvaluator.Evaluate(BuildCoverageProject(), Catalog()).ToArray();
            var reversed = (MeasurementWorkItemCoverageFinding[])findings.Clone();
            Array.Reverse(reversed);

            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var first = MeasurementWorkItemCoverageReport.Create(reversed);
                CultureInfo.CurrentCulture = new CultureInfo("vi-VN");
                var second = MeasurementWorkItemCoverageReport.Create(findings);

                SequenceEqual(
                    first.Rows.Select(Signature),
                    second.Rows.Select(Signature),
                    "Coverage report ordering/content must not depend on input order or current culture.");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        private static void ProjectionIsDetachedAndReadOnly()
        {
            var project = BuildCoverageProject();
            var findings = MeasurementWorkItemCoverageEvaluator.Evaluate(project, Catalog());
            var report = MeasurementWorkItemCoverageReport.Create(findings);
            var ready = report.Rows.Single(x => x.ElementId == "A-ready");

            var source = project.Elements.Single(x => x.Id == "A-ready");
            source.Quantities["NetVolumeM3"] = 99d;
            source.MarkDirty(ElementDirtyFlags.Quantity);

            Equal(2d, ready.QuantityValue, "Coverage report row must remain detached from later project quantity mutation.");
            True(ready.IsReady, "Coverage report readiness must be a snapshot of evaluator truth, not a live project query.");

            var rows = report.Rows as IList<MeasurementWorkItemCoverageReportRow>;
            True(rows != null && rows.IsReadOnly, "Coverage report rows must expose a read-only collection.");
            Throws<NotSupportedException>(() => rows!.Add(ready));

            var issues = report.Rows.Single(x => x.ElementId == "D-stale-unmapped").Issues as IList<MeasurementWorkItemCoverageIssue>;
            True(issues != null && issues.IsReadOnly, "Coverage report issue projection must expose a read-only collection.");
            Throws<NotSupportedException>(() => issues!.Add(MeasurementWorkItemCoverageIssue.MissingQuantity));
        }

        private static void InvalidInputFailsClosed()
        {
            Throws<ArgumentNullException>(() => MeasurementWorkItemCoverageReport.Create(null!));
            Throws<ArgumentException>(() => MeasurementWorkItemCoverageReport.Create(
                new MeasurementWorkItemCoverageFinding[] { null! }));

            var empty = MeasurementWorkItemCoverageReport.Create(Array.Empty<MeasurementWorkItemCoverageFinding>());
            Equal(0, empty.TotalCount, "Empty coverage report total count mismatch.");
            Equal(0, empty.ReadyCount, "Empty coverage report ready count mismatch.");
            Equal(0, empty.NotReadyCount, "Empty coverage report not-ready count mismatch.");
        }

        private static ProjectState BuildCoverageProject()
        {
            var ready = CleanQuantityElement("A-ready", ElementCategory.Slab, "NetVolumeM3", 2d);
            var unmapped = CleanQuantityElement("b-unmapped", ElementCategory.Slab, "OtherVolumeM3", 4d);

            var staleMapped = CleanQuantityElement("C-stale-mapped", ElementCategory.Slab, "NetVolumeM3", 3d);
            staleMapped.MarkDirty(ElementDirtyFlags.Quantity);

            var staleUnmapped = CleanQuantityElement("D-stale-unmapped", ElementCategory.Beam, "CustomLengthM", 5d);
            staleUnmapped.MarkDirty(ElementDirtyFlags.Quantity);

            var missing = new ProjectElement("E-missing", ElementCategory.Column);
            missing.MarkClean(ElementDirtyFlags.All);

            var project = new ProjectState("coverage-report", "Coverage Report");
            project.Elements.Add(ready);
            project.Elements.Add(unmapped);
            project.Elements.Add(staleMapped);
            project.Elements.Add(staleUnmapped);
            project.Elements.Add(missing);
            return project;
        }

        private static ProjectElement CleanQuantityElement(
            string id,
            ElementCategory category,
            string quantityKey,
            double value)
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

        private static string Signature(MeasurementWorkItemCoverageReportRow row) =>
            row.ElementId + "\u001f" +
            row.Category + "\u001f" +
            (row.QuantityKey ?? "<missing>") + "\u001f" +
            (row.QuantityValue.HasValue ? row.QuantityValue.Value.ToString("R", CultureInfo.InvariantCulture) : "<missing>") + "\u001f" +
            (row.MappingId ?? "<unmapped>") + "\u001f" +
            row.IsReady + "\u001f" +
            string.Join(",", row.Issues.Select(x => x.ToString()));

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
        {
            if (!expected.SequenceEqual(actual))
                throw new InvalidOperationException(message);
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
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
