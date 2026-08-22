using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Mapping;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasurementWorkItemCoverageMatrixSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            MatrixCompactsRepeatedCoverageStates();
            MatrixPreservesSummaryAndMissingIdentity();
            MatrixOutputIsDetachedAndReadOnly();
            InvalidInputFailsClosed();
        }

        private static void MatrixCompactsRepeatedCoverageStates()
        {
            var project = BuildProject();
            var report = MeasurementWorkItemCoverageReport.Create(
                MeasurementWorkItemCoverageEvaluator.Evaluate(project, Catalog()));
            var matrix = MeasurementWorkItemCoverageMatrix.Create(report);

            Equal(4, matrix.CellCount, "Coverage matrix must compact five findings into four distinct coverage states.");

            var ready = matrix.Cells.Single(x =>
                x.Category == ElementCategory.Slab &&
                x.MeasurementItemId == "NetVolumeM3" &&
                x.IsReady);
            Equal("map-slab-volume", ready.MappingId, "Ready matrix mapping id mismatch.");
            Equal("class-slab", ready.ClassificationId, "Ready matrix classification id mismatch.");
            Equal("work-slab-volume", ready.WorkItemId, "Ready matrix work-item id mismatch.");
            Equal(2, ready.FindingCount, "Repeated ready coverage rows must compact into one cell.");
            Equal(2, ready.AffectedElementCount, "Ready matrix affected-element count mismatch.");
            SequenceEqual(new[] { "A-ready", "b-ready" }, ready.AffectedElementIds,
                "Affected element ids must remain deterministic and actionable.");
            Equal(0, ready.Issues.Count, "Ready matrix cell must not invent issues.");

            var stale = matrix.Cells.Single(x =>
                x.Category == ElementCategory.Slab &&
                x.MeasurementItemId == "NetVolumeM3" &&
                !x.IsReady);
            Equal(1, stale.FindingCount, "Stale mapped state must remain separate from the ready state.");
            SequenceEqual(new[] { MeasurementWorkItemCoverageIssue.StaleQuantity }, stale.Issues,
                "Stale matrix issue mismatch.");
            SequenceEqual(new[] { "C-stale" }, stale.AffectedElementIds,
                "Stale matrix action target mismatch.");
        }

        private static void MatrixPreservesSummaryAndMissingIdentity()
        {
            var report = MeasurementWorkItemCoverageReport.Create(
                MeasurementWorkItemCoverageEvaluator.Evaluate(BuildProject(), Catalog()));
            var matrix = MeasurementWorkItemCoverageMatrix.Create(report);

            Equal(report.TotalCount, matrix.TotalFindingCount, "Matrix total finding count must preserve report truth.");
            Equal(report.ReadyCount, matrix.ReadyFindingCount, "Matrix ready count must preserve report truth.");
            Equal(report.NotReadyCount, matrix.NotReadyFindingCount, "Matrix not-ready count must preserve report truth.");
            Equal(report.MissingQuantityCount, matrix.MissingQuantityFindingCount, "Matrix missing-quantity count mismatch.");
            Equal(report.StaleQuantityCount, matrix.StaleQuantityFindingCount, "Matrix stale-quantity count mismatch.");
            Equal(report.UnmappedWorkItemCount, matrix.UnmappedWorkItemFindingCount, "Matrix unmapped count mismatch.");

            var missing = matrix.Cells.Single(x => x.Category == ElementCategory.Column);
            True(missing.MeasurementItemId == null, "Missing-quantity matrix cell must not invent a measurement-item id.");
            True(missing.MappingId == null && missing.ClassificationId == null && missing.WorkItemId == null,
                "Missing-quantity matrix cell must not invent mapping identity.");
            SequenceEqual(new[] { MeasurementWorkItemCoverageIssue.MissingQuantity }, missing.Issues,
                "Missing-quantity matrix issue mismatch.");

            var unmapped = matrix.Cells.Single(x => x.MeasurementItemId == "OtherVolumeM3");
            True(unmapped.MappingId == null && unmapped.WorkItemId == null,
                "Unmapped matrix cell must preserve explicit unmapped identity.");
            SequenceEqual(new[] { MeasurementWorkItemCoverageIssue.UnmappedWorkItem }, unmapped.Issues,
                "Unmapped matrix issue mismatch.");
        }

        private static void MatrixOutputIsDetachedAndReadOnly()
        {
            var project = BuildProject();
            var report = MeasurementWorkItemCoverageReport.Create(
                MeasurementWorkItemCoverageEvaluator.Evaluate(project, Catalog()));
            var matrix = MeasurementWorkItemCoverageMatrix.Create(report);
            var ready = matrix.Cells.Single(x => x.IsReady);

            project.Elements.Single(x => x.Id == "A-ready").MarkDirty(ElementDirtyFlags.Quantity);
            Equal(2, ready.FindingCount, "Matrix cell must remain detached from later project mutation.");
            SequenceEqual(new[] { "A-ready", "b-ready" }, ready.AffectedElementIds,
                "Matrix action targets must remain a detached snapshot.");

            var cells = matrix.Cells as IList<MeasurementWorkItemCoverageMatrixCell>;
            True(cells != null && cells.IsReadOnly, "Coverage matrix cells must expose a read-only collection.");
            Throws<NotSupportedException>(() => cells!.Add(ready));

            var ids = ready.AffectedElementIds as IList<string>;
            True(ids != null && ids.IsReadOnly, "Coverage matrix affected ids must expose a read-only collection.");
            Throws<NotSupportedException>(() => ids!.Add("mutated"));

            var issues = matrix.Cells.Single(x => x.MeasurementItemId == "OtherVolumeM3").Issues as IList<MeasurementWorkItemCoverageIssue>;
            True(issues != null && issues.IsReadOnly, "Coverage matrix issues must expose a read-only collection.");
            Throws<NotSupportedException>(() => issues!.Add(MeasurementWorkItemCoverageIssue.StaleQuantity));
        }

        private static void InvalidInputFailsClosed()
        {
            Throws<ArgumentNullException>(() => MeasurementWorkItemCoverageMatrix.Create(null!));

            var emptyReport = MeasurementWorkItemCoverageReport.Create(Array.Empty<MeasurementWorkItemCoverageFinding>());
            var empty = MeasurementWorkItemCoverageMatrix.Create(emptyReport);
            Equal(0, empty.CellCount, "Empty report must produce an empty matrix.");
            Equal(0, empty.TotalFindingCount, "Empty matrix total count mismatch.");
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("coverage-matrix", "Coverage Matrix");
            project.Elements.Add(CleanQuantityElement("A-ready", ElementCategory.Slab, "NetVolumeM3", 2d));
            project.Elements.Add(CleanQuantityElement("b-ready", ElementCategory.Slab, "NetVolumeM3", 4d));

            var stale = CleanQuantityElement("C-stale", ElementCategory.Slab, "NetVolumeM3", 3d);
            stale.MarkDirty(ElementDirtyFlags.Quantity);
            project.Elements.Add(stale);

            project.Elements.Add(CleanQuantityElement("D-unmapped", ElementCategory.Slab, "OtherVolumeM3", 5d));

            var missing = new ProjectElement("E-missing", ElementCategory.Column);
            missing.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(missing);
            return project;
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

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
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

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
